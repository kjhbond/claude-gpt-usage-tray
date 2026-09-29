import unittest
from quota import weekly
class WeeklyQuotaTests(unittest.TestCase):
 def window(self,used,mins):return {'usedPercent':used,'windowDurationMins':mins,'resetsAt':2000000000}
 def test_weekly_can_be_primary(self):
  self.assertEqual(weekly({'rateLimits':{'primary':self.window(13,10080),'secondary':self.window(99,300)}})['remainingPercent'],87)
 def test_selects_codex_bucket(self):
  self.assertEqual(weekly({'rateLimitsByLimitId':{'codex':{'secondary':self.window(100,10080)},'other':{'secondary':self.window(3,10080)}}})['remainingPercent'],0)
 def test_zero_used(self):self.assertEqual(weekly({'rateLimits':{'secondary':self.window(0,10080)}})['remainingPercent'],100)
 def test_missing_week_is_not_short_quota(self):
  with self.assertRaises(ValueError):weekly({'rateLimits':{'primary':self.window(25,300)}})
 def test_invalid_usage(self):
  for v in [None,-1,101,True,float('nan'),'5']:
   with self.subTest(v=v),self.assertRaises(ValueError):weekly({'rateLimits':{'secondary':self.window(v,10080)}})
 def test_other_bucket_not_mislabelled(self):
  with self.assertRaises(ValueError):weekly({'rateLimits':{'limitId':'other','secondary':self.window(4,10080)}})
if __name__=='__main__':unittest.main()
