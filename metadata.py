"""Only public account labels and reset timestamps are persisted for the UI."""
from datetime import datetime, timedelta, timezone
import os,pathlib
ROOT=pathlib.Path(__file__).resolve().parent
KST=timezone(timedelta(hours=9), name="KST")

def local_time(value):
    try:
        if isinstance(value,bool) or value is None:return '확인 불가'
        if isinstance(value,(int,float)):date=datetime.fromtimestamp(value,KST)
        else:
            date=datetime.fromisoformat(value.replace('Z','+00:00'))
            if date.tzinfo is None:return '확인 불가'
            date=date.astimezone(KST)
        weekday=('월','화','수','목','금','토','일')[date.weekday()]
        return date.strftime('%m-%d')+f'({weekday}) '+date.strftime('%H:%M')+' 한국시간'
    except (ValueError,TypeError,OverflowError,OSError):return '확인 불가'

def clean(value):
    return ''.join(c for c in str(value or '확인 불가') if c.isprintable())[:240]

def write_metadata(provider,state):
    rows={'AccountId':clean(state.get('accountId')),
          'ResetLocal':local_time(state.get('resetsAt')) if state.get('status')=='ok' else '확인 불가',
          'CheckedLocal':local_time(state.get('checkedAt')),
          'Status':clean(state.get('status'))}
    text='[Account]\n'+''.join(k+'='+v+'\n' for k,v in rows.items())
    target=ROOT/(provider+'-info.ini');temp=target.with_suffix('.tmp')
    temp.write_text(text,encoding='utf-16');os.replace(temp,target)
