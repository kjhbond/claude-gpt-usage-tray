# Codex + Claude weekly quota tray

A Windows 11 x64 taskbar widget that shows the remaining **weekly** Codex and Claude Code usage next to the system tray. Hovering over either icon shows its reset date as `MM-DD(요일) HH:MM 한국시간`. Left-click opens the provider's web chat; right-click shows the account label, reset time, and last check time.

![ChatGPT and Claude quota widget](docs/taskbar-preview.png)

## Install

1. Download **CodexClaudeQuotaTray-Setup.exe** from the [latest release](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest).
2. Run it under the Windows account whose taskbar should show the widget. The installer puts the widget in `%USERPROFILE%\CodexQuotaWidget` and registers it to start at sign-in.
3. Allow the first quota lookup to finish. A provider shows `--%` when its login or quota lookup is unavailable.

The installer needs Python 3 with `pythonw.exe` available through `python` or `py`. Codex CLI and Claude Code must already be installed **via npm** and signed in to the accounts whose limits you want to see. The installer does not install or sign in to either CLI. It includes a portable Windhawk runtime. On a new Windows build, Windhawk may need internet access to resolve taskbar symbols. See [INSTALL.txt](installer/INSTALL.txt) for the stop/uninstall command.

The installer is an unsigned local build. It has been smoke tested on the maintainer's Windows 11 x64 taskbar; other Windows builds and display configurations have not been verified.

## How it works

- The Codex poller uses the local Codex app-server's `account/rateLimits/read` weekly (`10080` minute) window. It does not start model turns.
- The Claude poller reads the existing Claude Code login and calls Anthropic's `seven_day` usage endpoint. This endpoint is not a documented public quota API and may change.
- Both pollers check every two minutes. The displayed percentage is the remaining weekly allowance rounded down. Reset times are converted to Korea Standard Time, independent of the PC's time zone.
- Account tokens are never included in this repository or the installer. Runtime account labels and quota results are stored only on the installed computer. The Claude access token is used in memory only for the Anthropic request.

## Source and build

The source is here so the installer can be inspected and rebuilt. `codex-weekly-quota.wh.cpp` is the Windhawk taskbar mod. `quota.py`, `claude_quota.py`, and `metadata.py` are the pollers; `Launcher.cs` starts the portable runtime and pollers; `installer/Setup.cs` builds the installer.

To rebuild, place the official Windhawk 1.7.3 **portable** files under `Windhawk/`, including its compiler. Then run these commands from the repository root on Windows:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:CodexQuotaWidget.exe Launcher.cs
python build.py codex-weekly-quota-portable.dll
python installer\build-installer.py
```

The installer will be written to `release/CodexClaudeQuotaTray-Setup.exe`. The build script packages only the named source assets and portable runtime files; it excludes account identity, login credentials, and quota caches. Run `python -m unittest test_metadata.py test_quota.py test_claude_quota.py` to check the quota and date formatting logic.

## Credits and license

The taskbar access helpers are adapted from [m417z's Windhawk mod](https://github.com/ramensoftware/windhawk-mods/blob/main/mods/taskbar-tray-system-icon-tweaks.wh.cpp). The taskbar mod and this repository's original source are published under [GPL-3.0](LICENSE). The installer bundles [Windhawk](https://github.com/ramensoftware/windhawk); provider logos identify their respective services. See [third-party notices](THIRD_PARTY_NOTICES.md).
