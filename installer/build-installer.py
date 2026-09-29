"""Build a redistributable Windows installer without local account or quota data."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'release'
OUT.mkdir(exist_ok=True)
PAYLOAD = OUT / 'payload.zip'
SETUP = OUT / 'CodexClaudeQuotaTray-Setup.exe'

files = {
    'CodexQuotaWidget.exe': ROOT / 'CodexQuotaWidget.exe',
    'QuotaSettings.exe': ROOT / 'QuotaSettings.exe',
    'Launcher.cs': ROOT / 'Launcher.cs',
    'Settings.cs': ROOT / 'Settings.cs',
    'quota.py': ROOT / 'quota.py',
    'claude_quota.py': ROOT / 'claude_quota.py',
    'metadata.py': ROOT / 'metadata.py',
    'layout.ini': ROOT / 'layout.ini',
    'chatgpt.png': ROOT / 'chatgpt.png',
    'claude.png': ROOT / 'claude.png',
    'install-startup.ps1': ROOT / 'install-startup.ps1',
    'restart-pollers.ps1': ROOT / 'restart-pollers.ps1',
    'uninstall.ps1': ROOT / 'uninstall.ps1',
    'INSTALL.txt': ROOT / 'installer/INSTALL.txt',
    'reference/COPYING': ROOT / 'LICENSE',
    'Windhawk/windhawk.exe': ROOT / 'Windhawk/windhawk.exe',
    'Windhawk/windhawk-x64-helper.exe': ROOT / 'Windhawk/windhawk-x64-helper.exe',
    'Windhawk/windhawk.ini': ROOT / 'Windhawk/windhawk.ini',
    'Windhawk/AppData/Engine/Mods/codex-weekly-quota.ini': ROOT / 'mod-config.ini',
}
mods = ROOT / 'Windhawk/AppData/Engine/Mods/64'
for name in ('libc++.whl', 'libunwind.whl', 'windhawk-mod-shim.dll'):
    files['Windhawk/AppData/Engine/Mods/64/' + name] = mods / name
files['Windhawk/AppData/Engine/Mods/64/codex-weekly-quota.dll'] = mods / 'codex-weekly-quota-portable.dll'
for folder in ('Engine', 'UI'):
    base = ROOT / 'Windhawk' / folder
    for path in base.rglob('*'):
        if path.is_file(): files[path.relative_to(ROOT).as_posix()] = path

for name, path in files.items():
    if not path.is_file(): raise FileNotFoundError(name + ': ' + str(path))

with ZipFile(PAYLOAD, 'w', ZIP_DEFLATED, compresslevel=6) as zip_file:
    for name, path in sorted(files.items()): zip_file.write(path, name)
    zip_file.writestr('Windhawk/AppData/settings.ini', '[Settings]\nLanguage=ko\nLoggingVerbosity=0\nHideTrayIcon=0\nAlwaysCompileModsLocally=0\n')
    zip_file.writestr('Windhawk/AppData/Engine/settings.ini', '[Settings]\nLoggingVerbosity=0\n')

compiler = Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
args = [str(compiler), '/nologo', '/target:winexe', '/optimize+', '/r:System.Windows.Forms.dll',
        '/r:System.IO.Compression.dll', '/resource:' + str(PAYLOAD) + ',Payload',
        '/out:' + str(SETUP), str(ROOT / 'installer/Setup.cs')]
subprocess.run(args, check=True)
print(SETUP)
print('Payload files:', len(files) + 2)
print('Installer bytes:', SETUP.stat().st_size)
