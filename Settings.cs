using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Settings {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern uint GetPrivateProfileInt(string section, string key, uint fallback, string path);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool WritePrivateProfileString(string section, string key, string value, string path);

    static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
    static readonly string Layout = Path.Combine(Root, "layout.ini");

    [STAThread]
    static int Main(string[] args) {
        if (args.Length > 0) {
            bool codex, claude;
            switch (args.Length == 1 ? args[0] : "") {
                case "--codex-only": codex = true; claude = false; break;
                case "--claude-only": codex = false; claude = true; break;
                case "--both": codex = true; claude = true; break;
                default: return 2;
            }
            try { Save(codex, claude); return 0; }
            catch { return 1; }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var form = new Form {
            Text = "Codex + Claude 한도 표시 설정",
            ClientSize = new Size(420, 205),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            Font = new Font("Segoe UI", 10),
        };
        var heading = new Label {
            Text = "작업 표시줄에 표시할 서비스를 선택하세요.",
            Location = new Point(24, 20), AutoSize = true,
        };
        var codexCheck = new CheckBox {
            Text = "Codex 주간 한도",
            Location = new Point(28, 57), AutoSize = true,
            Checked = GetPrivateProfileInt("Layout", "ShowCodex", 1, Layout) != 0,
        };
        var claudeCheck = new CheckBox {
            Text = "Claude Code 주간 한도",
            Location = new Point(28, 88), AutoSize = true,
            Checked = GetPrivateProfileInt("Layout", "ShowClaude", 1, Layout) != 0,
        };
        var explanation = new Label {
            Text = "선택하지 않은 서비스는 표시하거나 조회하지 않습니다.",
            Location = new Point(24, 124), AutoSize = true,
            ForeColor = SystemColors.GrayText, Font = new Font("Segoe UI", 9),
        };
        var save = new Button {
            Text = "저장", Location = new Point(230, 160), Size = new Size(80, 30),
        };
        var cancel = new Button {
            Text = "취소", Location = new Point(318, 160), Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
        };
        save.Click += (sender, e) => {
            if (!codexCheck.Checked && !claudeCheck.Checked) {
                MessageBox.Show(form, "최소 한 가지 서비스는 선택해야 합니다.", form.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try {
                Save(codexCheck.Checked, claudeCheck.Checked);
                form.DialogResult = DialogResult.OK;
                form.Close();
            } catch (Exception ex) {
                MessageBox.Show(form, "표시 설정을 저장하지 못했습니다.\n" + ex.Message,
                    form.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        form.Controls.AddRange(new Control[] {
            heading, codexCheck, claudeCheck, explanation, save, cancel,
        });
        form.AcceptButton = save;
        form.CancelButton = cancel;
        Application.Run(form);
        return 0;
    }

    static void Save(bool codex, bool claude) {
        if (!codex && !claude) throw new ArgumentException("At least one provider is required.");
        if (!WritePrivateProfileString("Layout", "ShowCodex", codex ? "1" : "0", Layout) ||
            !WritePrivateProfileString("Layout", "ShowClaude", claude ? "1" : "0", Layout))
            throw new IOException("layout.ini could not be updated.");

        string script = Path.Combine(Root, "restart-pollers.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException("Restart script is missing.", script);
        var start = new ProcessStartInfo(
            "powershell.exe",
            "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\""
        ) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        using (var process = Process.Start(start)) {
            if (!process.WaitForExit(30000)) {
                process.Kill();
                throw new TimeoutException("Restart timed out.");
            }
            if (process.ExitCode != 0) throw new IOException("The quota pollers could not restart.");
        }
    }
}
