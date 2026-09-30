from pathlib import Path
import subprocess,shutil,sys
r=Path(__file__).resolve().parent;w=r/'Windhawk';compiler=w/'Compiler';out=w/'AppData/Engine/Mods/64';out.mkdir(parents=True,exist_ok=True)
for src,dst in [('libc++.dll','libc++.whl'),('libunwind.dll','libunwind.whl'),('windhawk-mod-shim.dll','windhawk-mod-shim.dll')]:
 for p in compiler.glob('**/'+src):
  if 'x86_64-w64-mingw32' in str(p):
   if not (out/dst).exists():shutil.copy2(p,out/dst)
   break
output=out/(sys.argv[1] if len(sys.argv)>1 else 'codex-weekly-quota.dll')
args=[str(compiler/'bin/clang++.exe'),'-std=c++23','-O2','-shared',f'-ffile-prefix-map={r.as_posix()}=.', '-DUNICODE','-D_UNICODE','-DWINVER=0x0A00','-D_WIN32_WINNT=0x0A00','-D_WIN32_IE=0x0A00','-DNTDDI_VERSION=0x0A000008','-D__USE_MINGW_ANSI_STDIO=0','-DWH_MOD','-DWH_MOD_ID=L"codex-weekly-quota"','-DWH_MOD_VERSION=L"1.1.1"',str(w/'Engine/1.7.3/64/windhawk.lib'),str(r/'codex-weekly-quota.wh.cpp'),'-include','windhawk_api.h','-target','x86_64-w64-mingw32','-Wl,--export-all-symbols','-o',str(output),'-lole32','-loleaut32','-lruntimeobject','-lshell32']
p=subprocess.run(args,cwd=compiler,capture_output=True,text=True)
print(p.stdout,p.stderr);raise SystemExit(p.returncode)
