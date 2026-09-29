using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

internal static class Setup {
    static readonly string LogPath = Path.Combine(Path.GetTempPath(), "CodexClaudeQuotaTray-Setup.log");

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
                string oldUninstall = Path.Combine(target, "uninstall.ps1");
                if (File.Exists(oldUninstall)) RunPowerShell(oldUninstall, "", 60000);
                CopyTree(stage, target);
                RunPowerShell(Path.Combine(target, "install-startup.ps1"), "-PythonPath \"" + python + "\"", 60000);
                File.AppendAllText(LogPath, DateTime.Now.ToString("O") + " Installed to " + target + Environment.NewLine);
                if (!quiet) MessageBox.Show("설치가 완료되었습니다. ChatGPT·Claude 주간 한도가 트레이에 표시됩니다.\n\n각 CLI 로그인 상태에 따라 첫 조회까지 잠시 걸릴 수 있습니다.", "Codex + Claude Quota Tray");
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
            if (File.Exists(output) && FilesEqual(file, output)) continue;
            if (File.Exists(output)) File.SetAttributes(output, FileAttributes.Normal);
            File.Copy(file, output, true);
        }
        foreach (string directory in Directory.GetDirectories(source)) CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
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
