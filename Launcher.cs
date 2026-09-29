using System;
using System.Diagnostics;
using System.IO;
class Launcher {
 [STAThread] static void Main() {
  string root=AppDomain.CurrentDomain.BaseDirectory;
  try {
   Start(Path.Combine(root,"Windhawk","windhawk.exe"),"-tray-only",root);
   string python=File.ReadAllText(Path.Combine(root,"pythonw-path.txt")).Trim();
   Start(python,"\""+Path.Combine(root,"quota.py")+"\"",root);
   Start(python,"\""+Path.Combine(root,"claude_quota.py")+"\"",root);
  } catch(Exception ex) {File.WriteAllText(Path.Combine(root,"startup-error.txt"),DateTime.Now.ToString("O")+" "+ex.GetType().Name+": "+ex.Message);}
 }
 static void Start(string exe,string args,string dir) {
  var p=new ProcessStartInfo(exe,args);p.WorkingDirectory=dir;p.UseShellExecute=false;p.CreateNoWindow=true;p.WindowStyle=ProcessWindowStyle.Hidden;
  using(var process=Process.Start(p)){}
 }
}
