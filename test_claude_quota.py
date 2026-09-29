import unittest
from claude_quota import weekly
class ClaudeQuotaTests(unittest.TestCase):
 def test_weekly_not_five_hour(self):self.assertEqual(weekly({'five_hour':{'utilization':85},'seven_day':{'utilization':1}})['remainingPercent'],99)
 def test_full_remaining(self):self.assertEqual(weekly({'seven_day':{'utilization':0}})['remainingPercent'],100)
 def test_exhausted(self):self.assertEqual(weekly({'seven_day':{'utilization':100}})['remainingPercent'],0)
 def test_missing_not_zero(self):
  for data in [{},{'seven_day':None},{'seven_day':{}}]:
   with self.subTest(data=data),self.assertRaises(ValueError):weekly(data)
 def test_invalid(self):
  for v in [True,-1,101,'1',float('nan'),float('inf')]:
   with self.subTest(v=v),self.assertRaises(ValueError):weekly({'seven_day':{'utilization':v}})
if __name__=='__main__':unittest.main()
