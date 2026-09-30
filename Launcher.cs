using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
class Launcher {
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
 static extern uint GetPrivateProfileInt(string section,string key,uint fallback,string path);
 [STAThread] static void Main() {
  string root=AppDomain.CurrentDomain.BaseDirectory;
  try {
   Start(Path.Combine(root,"Windhawk","windhawk.exe"),"-tray-only",root);
   string python=File.ReadAllText(Path.Combine(root,"pythonw-path.txt")).Trim();
   string layout=Path.Combine(root,"layout.ini");
   bool codex=GetPrivateProfileInt("Layout","ShowCodex",1,layout)!=0;
   bool claude=GetPrivateProfileInt("Layout","ShowClaude",1,layout)!=0;
   if(!codex&&!claude)codex=true;
   if(codex)Start(python,"\""+Path.Combine(root,"quota.py")+"\"",root);
   if(claude)Start(python,"\""+Path.Combine(root,"claude_quota.py")+"\"",root);
  } catch(Exception ex) {File.WriteAllText(Path.Combine(root,"startup-error.txt"),DateTime.Now.ToString("O")+" "+ex.GetType().Name+": "+ex.Message);}
 }
 static void Start(string exe,string args,string dir) {
  string command="\""+exe+"\""+(String.IsNullOrWhiteSpace(args)?"":" "+args);
  using(var processes=new ManagementClass("Win32_Process"))
  using(var input=processes.GetMethodParameters("Create")) {
   input["CommandLine"]=command;
   input["CurrentDirectory"]=dir;
   using(var output=processes.InvokeMethod("Create",input,null)) {
    uint code=Convert.ToUInt32(output["ReturnValue"]);
    if(code!=0)throw new InvalidOperationException("Detached launch failed: "+code);
   }
  }
 }
}
