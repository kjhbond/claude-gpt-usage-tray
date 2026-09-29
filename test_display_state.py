import unittest

from display_state import prepare


class DisplayStateTest(unittest.TestCase):
    def setUp(self):
        self.success, self.text = prepare({
            'status': 'ok', 'accountId': 'same-account',
            'remainingPercent': 83.4, 'resetsAt': '2026-10-06T00:00:00Z',
        }, now=1000)

    def test_transient_rate_limit_keeps_marked_recent_value(self):
        state, text = prepare({
            'status': 'unavailable', 'reason': 'rate_limited',
            'accountId': 'same-account',
        }, self.success, now=1120)
        self.assertEqual(text, '~83%')
        self.assertEqual(state['status'], 'stale')
        self.assertEqual(state['lastSuccess']['checkedAt'], 1000)
        again, text = prepare({
            'status': 'unavailable', 'reason': 'rate_limited',
            'accountId': 'same-account',
        }, state, now=1240)
        self.assertEqual(text, '~83%')
        self.assertEqual(again['resetsAt'], '2026-10-06T00:00:00Z')

    def test_login_change_and_expiry_never_show_old_account_value(self):
        for reason, account, now in [
            ('login_required', 'same-account', 1120),
            ('rate_limited', 'different-account', 1120),
            ('rate_limited', None, 1120),
            ('rate_limited', 'same-account', 1000 + 30 * 60 + 1),
        ]:
            with self.subTest(reason=reason, account=account, now=now):
                state, text = prepare({
                    'status': 'unavailable', 'reason': reason,
                    'accountId': account,
                }, self.success, now=now)
                self.assertEqual(text, '--%')
                self.assertEqual(state['status'], 'unavailable')

    def test_success_replaces_stale_value(self):
        stale, _ = prepare({
            'status': 'unavailable', 'reason': 'rate_limited',
            'accountId': 'same-account',
        }, self.success, now=1120)
        recovered, text = prepare({
            'status': 'ok', 'accountId': 'same-account',
            'remainingPercent': 81.9, 'resetsAt': '2026-10-06T00:00:00Z',
        }, stale, now=1240)
        self.assertEqual(text, '81%')
        self.assertEqual(recovered['lastSuccess']['checkedAt'], 1240)


if __name__ == '__main__':
    unittest.main()
