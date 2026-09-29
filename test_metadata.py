import unittest
from datetime import datetime,timezone
from metadata import local_time,clean,status_text
class MetadataTests(unittest.TestCase):
 def test_iso_and_epoch_same_instant(self):
  instant=datetime(2026,10,4,7,11,52,tzinfo=timezone.utc)
  self.assertEqual(local_time(instant.timestamp()),local_time('2026-10-04T07:11:52Z'))
  self.assertEqual(local_time(instant.timestamp()),'10-04(일) 16:11 한국시간')
 def test_korea_date_rollover_from_offset(self):
  self.assertEqual(local_time('2026-10-04T23:30:00-07:00'),'10-05(월) 15:30 한국시간')
 def test_invalid_reset_is_not_current_time(self):
  for v in [None,True,'invalid',1e99]:self.assertEqual(local_time(v),'확인 불가')
 def test_labels_cannot_inject_ini_lines(self):self.assertNotIn('\n',clean('name@example.com\nResetLocal=fake'))
 def test_stale_and_login_status_are_distinct(self):
  self.assertEqual(status_text({'status':'stale','reason':'rate_limited'}),'요청 제한 · 이전 조회값')
  self.assertEqual(status_text({'status':'unavailable','reason':'login_required'}),'CLI 로그인 필요')
if __name__=='__main__':unittest.main()
