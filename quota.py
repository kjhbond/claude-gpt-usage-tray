"""Read-only Codex app-server quota poller. Never reads or exports auth tokens."""
import json, os, pathlib, queue, subprocess, sys, threading, time, math, ctypes
from metadata import write_metadata
ROOT=pathlib.Path(__file__).resolve().parent
INTERVAL=120

def weekly(result):
    buckets=result.get('rateLimitsByLimitId')
    bucket=buckets.get('codex') if isinstance(buckets,dict) else None
    if bucket is None:
        bucket=result.get('rateLimits') or {}
        if bucket.get('limitId') not in (None,'codex'): raise ValueError('No Codex quota bucket')
    windows=[bucket.get(k) for k in ('primary','secondary')]
    windows=[w for w in windows if isinstance(w,dict) and w.get('windowDurationMins')==10080]
    if len(windows)!=1: raise ValueError('Weekly quota unavailable')
    w=windows[0];used=w.get('usedPercent')
    if isinstance(used,bool) or not isinstance(used,(int,float)) or not math.isfinite(used) or used<0 or used>100:
        raise ValueError('Invalid weekly quota')
    return {'remainingPercent':100-used,'resetsAt':w.get('resetsAt'),'windowDurationMins':10080}

class Server:
    def __init__(self):
        candidates=list((pathlib.Path(os.environ['APPDATA'])/'npm/node_modules/@openai/codex/node_modules').glob('@openai/codex-win32-*/vendor/*/bin/codex.exe'))
        if not candidates: raise RuntimeError('Codex executable missing')
        self.p=subprocess.Popen([str(candidates[0]),'app-server'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,encoding='utf-8',creationflags=subprocess.CREATE_NO_WINDOW,cwd=ROOT)
        self.q=queue.Queue();self.id=0
        threading.Thread(target=self.reader,daemon=True).start()
        self.request('initialize',{'clientInfo':{'name':'codex_weekly_taskbar','title':'Codex Weekly Taskbar','version':'1.0.0'}})
        self.send({'method':'initialized','params':{}})
    def reader(self):
        try:
            for line in self.p.stdout:
                try:self.q.put(json.loads(line))
                except ValueError:pass
        finally:self.q.put(None)
    def send(self,m):
        self.p.stdin.write(json.dumps(m)+'\n');self.p.stdin.flush()
    def request(self,method,params=None):
        self.id+=1; ident=self.id
        m={'id':ident,'method':method}
        if params is not None:m['params']=params
        self.send(m);deadline=time.monotonic()+40
        while time.monotonic()<deadline:
            msg=self.q.get(timeout=max(.1,deadline-time.monotonic()))
            if msg is None:raise RuntimeError('App-server exited')
            if msg.get('id')==ident and 'method' not in msg:
                if 'error' in msg:raise RuntimeError('Quota request failed')
                return msg['result']
            if 'id' in msg and 'method' in msg:
                self.send({'id':msg['id'],'error':{'code':-32601,'message':'Unsupported client request'}})
        raise TimeoutError('App-server timed out')
    def close(self):
        if self.p.poll() is None:
            self.p.terminate()
            try:self.p.wait(timeout=5)
            except subprocess.TimeoutExpired:self.p.kill();self.p.wait()

def write_state(state):
    state['checkedAt']=int(time.time());state['refreshSeconds']=INTERVAL
    write_metadata('codex',state)
    tmp=ROOT/'quota.json.tmp';tmp.write_text(json.dumps(state),encoding='utf-8');os.replace(tmp,ROOT/'quota.json')
    # Plain display file consumed by the in-process taskbar mod.
    text=(str(math.floor(state['remainingPercent']))+'%') if state.get('status')=='ok' else '--%'
    tmp=ROOT/'display.txt.tmp';tmp.write_text(text,encoding='ascii');os.replace(tmp,ROOT/'display.txt')

if __name__=='__main__':
    ctypes.windll.kernel32.CreateMutexW.restype=ctypes.c_void_p
    mutex=ctypes.windll.kernel32.CreateMutexW(None,False,'Local\\CodexWeeklyTaskbarPoller')
    if ctypes.windll.kernel32.GetLastError()==183:sys.exit(0)
    server=None
    try:
        while True:
            started=time.monotonic()
            try:
                if server is None:server=Server()
                account=server.request('account/read',{'refreshToken':False}).get('account') or {}
                state=weekly(server.request('account/rateLimits/read'))
                state['accountId']=account.get('email')
                state['status']='ok';write_state(state)
            except Exception as e:
                write_state({'status':'unavailable','reason':str(e) if isinstance(e,ValueError) else type(e).__name__})
                if server:server.close();server=None
            if '--once' in sys.argv:
                print((ROOT/'quota.json').read_text());break
            time.sleep(max(1,INTERVAL-(time.monotonic()-started)))
    finally:
        if server:server.close()
