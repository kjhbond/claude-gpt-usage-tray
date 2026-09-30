# 개발자용 빌드와 확인

소스는 [GPL-3.0](../LICENSE)으로 공개합니다. Windhawk 1.7.3 portable 파일과 컴파일러는 저장소에 포함하지 않습니다. [Windhawk 공식 저장소](https://github.com/ramensoftware/windhawk)에서 받아 저장소 루트의 `Windhawk/`에 배치하세요. 작업 표시줄 모드의 빌드 입력과 런타임 구조는 `build.py`, `installer/build-installer.py`를 확인하세요.

Windows x64에서 저장소 루트로 이동한 뒤:

```powershell
python -m unittest test_metadata.py test_quota.py test_claude_quota.py test_display_state.py
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:CodexQuotaWidget.exe Launcher.cs
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:QuotaSettings.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll Settings.cs
python build.py codex-weekly-quota-portable.dll
python installer\build-installer.py
```

결과는 `release/CodexClaudeQuotaTray-Setup.exe`입니다. 설치 파일은 지정된 실행·소스 파일과 Windhawk portable 구성만 묶습니다. 실제 배포 전에는 설치 파일 안에 자격 증명, 계정 정보, 개인 경로, 기존 조회 결과가 없는지 확인해야 합니다. 설치기는 UI Automation으로 작업 표시줄의 숫자 버튼이 화면에 나타났는지 확인하며, 확인하지 못하면 종료 코드 `2`를 반환합니다. Windows 작업 표시줄의 실제 동작은 단위 테스트와 별도로 설치 후 확인해야 합니다.

한도 처리: `quota.py`, `claude_quota.py` · 정상값/이전값 처리: `display_state.py` · 한국시간 처리: `metadata.py` · 작업 표시줄: `codex-weekly-quota.wh.cpp` · 표시 설정: `Settings.cs`, `sync-pollers.ps1` · 설치: `installer/Setup.cs`
