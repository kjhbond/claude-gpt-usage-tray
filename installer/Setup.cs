using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

internal static class Setup {
    static readonly string LogPath = Path.Combine(Path.GetTempPath(), "CodexClaudeQuotaTray-Setup.log");
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string className, string title);

    [STAThread]
    static int Main(string[] args) {
        bool quiet = Array.IndexOf(args, "--quiet") >= 0;
        try {
            if (args.Length == 2 && args[0] == "--extract") {
                Extract(args[1]);
                return 0;
            }
            if (args.Length != 0 && !quiet) throw new ArgumentException("Unknown installer option.");
            if (!Environment.Is64BitOperatingSystem) throw new Exception("Windows x64 is required.");
            string profile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (String.IsNullOrEmpty(profile) || !Directory.Exists(profile)) throw new Exception("Windows user profile was not found.");
            string target = Path.Combine(profile, "CodexQuotaWidget");
            string python = FindPython();
            string stage = Path.Combine(Path.GetTempPath(), "CodexClaudeQuotaTray-" + Guid.NewGuid().ToString("N"));
            try {
                Extract(stage);
                CloseInstalledSettings(target);
                string oldUninstall = Path.Combine(target, "uninstall.ps1");
                if (File.Exists(oldUninstall)) RunPowerShell(oldUninstall, "", 60000);
                foreach (string obsolete in new[] { "FallbackTray.exe", "FallbackTray.cs" }) {
                    string obsoleteFile = Path.Combine(target, obsolete);
                    if (File.Exists(obsoleteFile)) File.Delete(obsoleteFile);
                }
                CopyTree(stage, target);
                RunPowerShell(Path.Combine(target, "install-startup.ps1"), "-PythonPath \"" + python + "\"", 60000);
                bool visible = WaitForWidget(45000);
                string windowsBuild = Convert.ToString(Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "unknown"));
                File.AppendAllText(LogPath, DateTime.Now.ToString("O") + " Installed to " + target +
                    "; Windows build " + windowsBuild + "; native widget visible=" + visible + Environment.NewLine);
                if (!visible) {
                    if (!quiet) MessageBox.Show("파일은 설치했지만 작업 표시줄 숫자 위젯을 확인하지 못했습니다.\n\nWindows 작업 표시줄과 Windhawk 모드의 호환성 또는 기호 다운로드를 확인해야 합니다. 독수리 아이콘만 보이는 경우에도 설치 성공으로 판단하지 않습니다.\n\n진단 로그: " + LogPath,
                        "Codex + Claude Quota Tray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 2;
                }
                if (!quiet) MessageBox.Show("설치가 완료되었고 작업 표시줄 숫자 위젯을 확인했습니다.\n\n아이콘을 오른쪽 클릭하고 '표시 설정...'에서 사용할 서비스를 선택하세요.", "Codex + Claude Quota Tray");
                return 0;
            } finally {
                if (Directory.Exists(stage)) Directory.Delete(stage, true);
            }
        } catch (Exception ex) {
            File.AppendAllText(LogPath, DateTime.Now.ToString("O") + " " + ex + Environment.NewLine);
            if (!quiet) MessageBox.Show("설치하지 못했습니다.\n" + ex.Message + "\n\n로그: " + LogPath, "Codex + Claude Quota Tray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    static bool WaitForWidget(int timeoutMs) {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var codex = new PropertyCondition(AutomationElement.AutomationIdProperty, "CodexQuotaButton");
        var claude = new PropertyCondition(AutomationElement.AutomationIdProperty, "ClaudeQuotaButton");
        while (DateTime.UtcNow < deadline) {
            try {
                IntPtr handle = FindWindow("Shell_TrayWnd", null);
                if (handle != IntPtr.Zero) {
                    var taskbar = AutomationElement.FromHandle(handle);
                    foreach (var condition in new[] { codex, claude }) {
                        var button = taskbar.FindFirst(TreeScope.Descendants, condition);
                        if (button != null && !button.Current.IsOffscreen &&
                            button.Current.BoundingRectangle.Width > 0) return true;
                    }
                }
            } catch (Exception) {
                // Explorer can replace its taskbar while Windhawk is attaching.
            }
            Thread.Sleep(1000);
        }
        return false;
    }

    static string FindPython() {
        foreach (var candidate in new[] { new[] { "python", "-c \"import sys; print(sys.executable)\"" }, new[] { "py", "-3 -c \"import sys; print(sys.executable)\"" } }) {
            try {
                string output = Run(candidate[0], candidate[1], 10000).Trim();
                if (File.Exists(output) && File.Exists(Path.Combine(Path.GetDirectoryName(output), "pythonw.exe"))) return output;
            } catch { }
        }
        throw new Exception("Python과 pythonw.exe가 필요합니다. Python을 설치한 후 다시 실행하세요.");
    }

    static void Extract(string destination) {
        Directory.CreateDirectory(destination);
        string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload")) {
            if (stream == null) throw new Exception("Installer payload is missing.");
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read)) {
                foreach (var entry in zip.Entries) {
                    string file = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new Exception("Invalid installer payload path.");
                    if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(file); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    using (var input = entry.Open()) using (var output = File.Create(file)) input.CopyTo(output);
                }
            }
        }
    }

    static void CopyTree(string source, string destination) {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source)) {
            string output = Path.Combine(destination, Path.GetFileName(file));
            if (String.Equals(Path.GetFileName(output), "layout.ini", StringComparison.OrdinalIgnoreCase)
                && File.Exists(output)) continue;
            if (File.Exists(output) && FilesEqual(file, output)) continue;
            if (File.Exists(output)) File.SetAttributes(output, FileAttributes.Normal);
            File.Copy(file, output, true);
        }
        foreach (string directory in Directory.GetDirectories(source)) CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    static void CloseInstalledSettings(string target) {
        string expected = Path.Combine(target, "QuotaSettings.exe");
        foreach (var process in Process.GetProcessesByName("QuotaSettings")) {
            try {
                if (!String.Equals(process.MainModule.FileName, expected, StringComparison.OrdinalIgnoreCase)) continue;
                if (!process.HasExited && process.CloseMainWindow()) process.WaitForExit(3000);
                if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
            } catch (InvalidOperationException) {
                // The settings window exited while the installer was checking it.
            } finally {
                process.Dispose();
            }
        }
    }

    static bool FilesEqual(string first, string second) {
        if (new FileInfo(first).Length != new FileInfo(second).Length) return false;
        byte[] a = new byte[65536], b = new byte[65536];
        using (var one = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var two = new FileStream(second, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            int count;
            while ((count = one.Read(a, 0, a.Length)) != 0) {
                if (two.Read(b, 0, count) != count) return false;
                for (int i = 0; i < count; i++) if (a[i] != b[i]) return false;
            }
        }
        return true;
    }

    static void RunPowerShell(string script, string extra, int timeout) {
        if (!File.Exists(script)) throw new FileNotFoundException("Installer script missing", script);
        Run("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" " + extra, timeout);
    }

    static string Run(string executable, string arguments, int timeout) {
        var start = new ProcessStartInfo(executable, arguments);
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.WindowStyle = ProcessWindowStyle.Hidden;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        using (var process = Process.Start(start)) {
            if (!process.WaitForExit(timeout)) { process.Kill(); throw new TimeoutException(executable + " timed out."); }
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0) throw new Exception(executable + " failed: " + errors + output);
            return output;
        }
    }
}
