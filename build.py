"""Compile the native taskbar mod and prepare portable Windhawk imports."""

from pathlib import Path
import shutil
import subprocess
import sys

from windhawk_imports import normalize_windhawk_imports

root = Path(__file__).resolve().parent
windhawk = root / "Windhawk"
compiler = windhawk / "Compiler"
mods = windhawk / "AppData/Engine/Mods/64"
mods.mkdir(parents=True, exist_ok=True)

for source_name, portable_name in (
    ("libc++.dll", "libc++.whl"),
    ("libunwind.dll", "libunwind.whl"),
    ("windhawk-mod-shim.dll", "windhawk-mod-shim.dll"),
):
    destination = mods / portable_name
    if destination.exists():
        continue
    sources = [p for p in compiler.glob("**/" + source_name) if "x86_64-w64-mingw32" in str(p)]
    if not sources:
        raise FileNotFoundError(f"Windhawk compiler runtime missing: {source_name}")
    shutil.copy2(sources[0], destination)

name = sys.argv[1] if len(sys.argv) > 1 else "codex-weekly-quota.dll"
if Path(name).name != name or not name.lower().endswith(".dll"):
    raise ValueError("Output must be a DLL filename in the Windhawk mods directory")
output = mods / name
args = [
    str(compiler / "bin/clang++.exe"), "-std=c++23", "-O2", "-shared",
    f"-ffile-prefix-map={root.as_posix()}=.",
    "-DUNICODE", "-D_UNICODE", "-DWINVER=0x0A00", "-D_WIN32_WINNT=0x0A00",
    "-D_WIN32_IE=0x0A00", "-DNTDDI_VERSION=0x0A000008",
    "-D__USE_MINGW_ANSI_STDIO=0", "-DWH_MOD",
    '-DWH_MOD_ID=L"codex-weekly-quota"', '-DWH_MOD_VERSION=L"1.1.3"',
    str(windhawk / "Engine/1.7.3/64/windhawk.lib"),
    str(root / "codex-weekly-quota.wh.cpp"), "-include", "windhawk_api.h",
    "-target", "x86_64-w64-mingw32", "-Wl,--export-all-symbols",
    "-o", str(output), "-lole32", "-loleaut32", "-lruntimeobject", "-lshell32",
]
result = subprocess.run(args, cwd=compiler, capture_output=True, text=True)
if result.returncode:
    print(result.stdout, result.stderr)
    raise SystemExit(result.returncode)

changes = normalize_windhawk_imports(output)
print(f"Built {output.name}; portable imports verified; patched: {', '.join(changes) or 'none'}")
