"""Read Claude's weekly quota. Credentials stay in Claude's own credential store."""
import ctypes,json,math,os,pathlib,subprocess,sys,time,urllib.request,urllib.error
from metadata import write_metadata
ROOT=pathlib.Path(__file__).resolve().parent
INTERVAL=120
ENDPOINT='https://api.anthropic.com/api/oauth/usage'

class Unavailable(Exception): pass

def weekly(data):
    w=data.get('seven_day')
    if not isinstance(w,dict):raise ValueError('Weekly quota unavailable')
    used=w.get('utilization')
    if isinstance(used,bool) or not isinstance(used,(int,float)) or not math.isfinite(used) or used<0 or used>100:
        raise ValueError('Invalid weekly quota')
    return {'remainingPercent':100-used,'resetsAt':w.get('resets_at'),'windowDurationMins':10080}

def credentials():
    p=pathlib.Path(os.environ.get('CLAUDE_CONFIG_DIR',str(pathlib.Path.home()/'.claude')))/'.credentials.json'
    try:
        d=json.loads(p.read_text(encoding='utf-8')).get('claudeAiOauth',{})
        if not isinstance(d.get('accessToken'),str) or not d['accessToken']:raise Unavailable('login_required')
        return d
    except (OSError,ValueError):raise Unavailable('login_required')

def read_usage():
    auth=credentials()
    # Let the official client maintain its own credentials; never rotate or copy its refresh token.
    if auth.get('expiresAt',0)<time.time()*1000:
        cli=pathlib.Path(os.environ['APPDATA'])/'npm/node_modules/@anthropic-ai/claude-code/node_modules/@anthropic-ai/claude-code-win32-x64/claude.exe'
        if cli.exists():
            subprocess.run([str(cli),'auth','status','--json'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,timeout=20,creationflags=subprocess.CREATE_NO_WINDOW)
        auth=credentials()
    request=urllib.request.Request(ENDPOINT,headers={'Authorization':'Bearer '+auth['accessToken'],'anthropic-beta':'oauth-2025-04-20','User-Agent':'claude-weekly-taskbar/1.0'})
    # Refuse redirects so credentials are only sent to the intended Anthropic host.
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self,*args,**kwargs):return None
    try:
        with urllib.request.build_opener(NoRedirect).open(request,timeout=25) as response:
            return weekly(json.load(response))
    except urllib.error.HTTPError as e:
        raise Unavailable('login_required' if e.code in (401,403) else 'rate_limited' if e.code==429 else 'service_unavailable')

def account_id():
    cli=pathlib.Path(os.environ['APPDATA'])/'npm/node_modules/@anthropic-ai/claude-code/node_modules/@anthropic-ai/claude-code-win32-x64/claude.exe'
    try:
        proc=subprocess.run([str(cli),'auth','status','--json'],capture_output=True,text=True,encoding='utf-8',timeout=20,creationflags=subprocess.CREATE_NO_WINDOW)
        data=json.loads(proc.stdout)
        return data.get('email') if data.get('loggedIn') and data.get('authMethod')=='claude.ai' else None
    except (OSError,ValueError,subprocess.TimeoutExpired):return None

def write_state(state):
    now=int(time.time()); state.update(checkedAt=now,refreshSeconds=INTERVAL)
    write_metadata('claude',state)
    previous={}
    try:previous=json.loads((ROOT/'claude-quota.json').read_text())
    except (OSError,ValueError):pass
    if previous.get('status')=='ok':state['previousSuccessfulCheckAt']=previous.get('checkedAt')
    tmp=ROOT/'claude-quota.json.tmp';tmp.write_text(json.dumps(state),encoding='utf-8');os.replace(tmp,ROOT/'claude-quota.json')
    display=str(math.floor(state['remainingPercent']))+'%' if state.get('status')=='ok' else '--%'
    tmp=ROOT/'claude-display.txt.tmp';tmp.write_text(display,encoding='ascii');os.replace(tmp,ROOT/'claude-display.txt')

if __name__=='__main__':
    ctypes.windll.kernel32.CreateMutexW.restype=ctypes.c_void_p
    mutex=ctypes.windll.kernel32.CreateMutexW(None,False,'Local\\ClaudeWeeklyTaskbarPoller')
    if ctypes.windll.kernel32.GetLastError()==183:sys.exit(0)
    while True:
        start=time.monotonic()
        try:
            identity=account_id();state=read_usage();state.update(status='ok',accountId=identity);write_state(state)
        except Exception as e:
            write_state({'status':'unavailable','reason':str(e) if isinstance(e,(ValueError,Unavailable)) else type(e).__name__})
        if '--once' in sys.argv:print((ROOT/'claude-quota.json').read_text());break
        time.sleep(max(1,INTERVAL-(time.monotonic()-start)))
