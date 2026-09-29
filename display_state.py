"""Keep a clearly marked recent quota value during temporary lookup failures."""
import math
import time

STALE_SECONDS = 30 * 60
TRANSIENT_REASONS = {
    'rate_limited', 'service_unavailable', 'TimeoutError', 'URLError',
    'ConnectionError', 'ConnectionResetError',
}


def prepare(current, previous=None, now=None, refresh_seconds=120):
    state = dict(current)
    previous = previous if isinstance(previous, dict) else {}
    now = int(time.time() if now is None else now)
    state.update(checkedAt=now, refreshSeconds=refresh_seconds)

    if state.get('status') == 'ok':
        success = {
            'checkedAt': now,
            'remainingPercent': state['remainingPercent'],
            'resetsAt': state.get('resetsAt'),
            'accountId': state.get('accountId'),
        }
        state['lastSuccess'] = success
        return state, str(math.floor(state['remainingPercent'])) + '%'

    success = previous.get('lastSuccess')
    if not isinstance(success, dict) and previous.get('status') == 'ok':
        success = {
            key: previous.get(key)
            for key in ('checkedAt', 'remainingPercent', 'resetsAt', 'accountId')
        }
    if (state.get('reason') in TRANSIENT_REASONS
            and isinstance(success, dict)
            and state.get('accountId')
            and state['accountId'] == success.get('accountId')
            and isinstance(success.get('checkedAt'), int)
            and 0 <= now - success['checkedAt'] <= STALE_SECONDS
            and isinstance(success.get('remainingPercent'), (int, float))):
        state.update(
            status='stale',
            remainingPercent=success['remainingPercent'],
            resetsAt=success.get('resetsAt'),
            lastSuccess=success,
        )
        return state, '~' + str(math.floor(success['remainingPercent'])) + '%'
    return state, '--%'
