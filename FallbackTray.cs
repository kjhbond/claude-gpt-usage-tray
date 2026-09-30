using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

// Shows the quota in ordinary notification-area icons when the Windhawk XAML
// taskbar mod cannot attach (for example while taskbar symbols are unavailable).
internal sealed class FallbackTray : ApplicationContext {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string className, string title);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern uint GetPrivateProfileInt(string section, string key, uint fallback, string path);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern uint GetPrivateProfileString(string section, string key, string fallback, StringBuilder value, uint size, string path);

    readonly string root = AppDomain.CurrentDomain.BaseDirectory;
    readonly NotifyIcon codex = new NotifyIcon();
    readonly NotifyIcon claude = new NotifyIcon();
    readonly Timer timer = new Timer();
    string codexValue = "", claudeValue = "";
    [STAThread] static void Main() {
        bool created;
        using (var mutex = new System.Threading.Mutex(true, "Local\\CodexClaudeQuotaFallbackTray", out created)) {
            if (!created) return;
            Application.EnableVisualStyles();
            Application.Run(new FallbackTray());
        }
    }

    FallbackTray() {
        codex.ContextMenuStrip = MakeMenu("https://chatgpt.com/");
        claude.ContextMenuStrip = MakeMenu("https://claude.ai/");
        codex.DoubleClick += (s, e) => Open("https://chatgpt.com/");
        claude.DoubleClick += (s, e) => Open("https://claude.ai/");
        timer.Interval = 3000;
        timer.Tick += (s, e) => Refresh();
        timer.Start();
        Refresh();
    }

    ContextMenuStrip MakeMenu(string url) {
        var menu = new ContextMenuStrip();
        menu.Items.Add("웹 채팅 열기", null, (s, e) => Open(url));
        menu.Items.Add("표시 설정...", null, (s, e) => Open(Path.Combine(root, "QuotaSettings.exe")));
        menu.Items.Add("문제 해결 안내", null, (s, e) => Open("https://github.com/kjhbond/claude-gpt-usage-tray/blob/main/docs/TROUBLESHOOTING.md"));
        return menu;
    }

    static void Open(string target) {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch { }
    }

    bool NativeWidgetPresent() {
        try {
            var handle = FindWindow("Shell_TrayWnd", null);
            if (handle == IntPtr.Zero) return false;
            var tray = AutomationElement.FromHandle(handle);
            var codexId = new PropertyCondition(AutomationElement.AutomationIdProperty, "CodexQuotaButton");
            var claudeId = new PropertyCondition(AutomationElement.AutomationIdProperty, "ClaudeQuotaButton");
            return tray.FindFirst(TreeScope.Descendants, codexId) != null ||
                   tray.FindFirst(TreeScope.Descendants, claudeId) != null;
        } catch { return false; }
    }

    static string Display(string path) {
        try {
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromMinutes(5)) return "--%";
            var value = File.ReadAllText(path).Trim();
            if (value.Length < 2 || value.Length > 5 || !value.EndsWith("%")) return "--%";
            foreach (char c in value.Substring(0, value.Length - 1))
                if (!Char.IsDigit(c) && c != '-' && c != '~') return "--%";
            return value;
        } catch { return "--%"; }
    }

    static Icon DrawIcon(string value, Color color) {
        using (var bitmap = new Bitmap(32, 32))
        using (var graphics = Graphics.FromImage(bitmap)) {
            graphics.Clear(color);
            string number = value.TrimEnd('%');
            using (var font = new Font("Segoe UI", number.Length > 3 ? 14 : 18, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                graphics.DrawString(number, font, brush, new RectangleF(0, 0, 32, 32), format);
            IntPtr handle = bitmap.GetHicon();
            try { return (Icon)Icon.FromHandle(handle).Clone(); }
            finally { DestroyIcon(handle); }
        }
    }
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);

    string Tooltip(string name, string value, string infoFile) {
        var reset = new StringBuilder(128);
        GetPrivateProfileString("Account", "ResetLocal", "", reset, 128, Path.Combine(root, infoFile));
        string tip = name + " " + value;
        if (reset.Length != 0) tip += " · 리셋 " + reset;
        return tip.Length > 63 ? tip.Substring(0, 63) : tip;
    }

    void SetIcon(NotifyIcon item, bool enabled, string name, string value, string infoFile, Color color, ref string previous) {
        if (!enabled) { item.Visible = false; return; }
        if (value != previous || item.Icon == null) {
            Icon old = item.Icon;
            item.Icon = DrawIcon(value, color);
            previous = value;
            if (old != null) old.Dispose();
        }
        string tip = Tooltip(name, value, infoFile);
        if (item.Text != tip) item.Text = tip;
        item.Visible = true;
    }

    void Refresh() {
        // Pollers remain independent: an unavailable taskbar mod cannot stop quota reads.
        bool show = !NativeWidgetPresent();
        string layout = Path.Combine(root, "layout.ini");
        bool showCodex = GetPrivateProfileInt("Layout", "ShowCodex", 1, layout) != 0;
        bool showClaude = GetPrivateProfileInt("Layout", "ShowClaude", 1, layout) != 0;
        if (!showCodex && !showClaude) showCodex = true;
        SetIcon(codex, show && showCodex, "Codex", Display(Path.Combine(root, "display.txt")), "codex-info.ini", Color.FromArgb(35, 42, 48), ref codexValue);
        SetIcon(claude, show && showClaude, "Claude", Display(Path.Combine(root, "claude-display.txt")), "claude-info.ini", Color.FromArgb(193, 99, 69), ref claudeValue);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            timer.Dispose();
            codex.Visible = false; claude.Visible = false;
            if (codex.Icon != null) codex.Icon.Dispose();
            if (claude.Icon != null) claude.Icon.Dispose();
            codex.Dispose(); claude.Dispose();
        }
        base.Dispose(disposing);
    }
}
