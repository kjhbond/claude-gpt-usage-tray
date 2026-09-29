# 처음 설치하고 사용하기

## 1. 준비

**Windows 11 x64**에서 설치할 Windows 계정으로 PowerShell을 엽니다. 다음 명령으로 Python 3, Node/npm, **사용할 서비스의 CLI**를 확인합니다.

```powershell
python --version
py -3 --version
npm --version
codex --version
claude --version
```

`python`과 `py -3` 중 하나만 동작해도 됩니다. Python 설치 폴더에 `pythonw.exe`도 있어야 합니다. `npm`이 없다면 [Node.js](https://nodejs.org/en/download)를 설치하세요. Python은 [python.org](https://www.python.org/downloads/windows/)의 Windows 설치 방법을 참고하세요.

이 배포판은 CLI의 **Windows 사용자 npm 전역 설치 경로**에서 실행 파일을 찾습니다. 처음 설치한다면 사용할 서비스의 명령만 실행하고 로그인하세요.

Codex를 사용할 때:

```powershell
npm install -g @openai/codex
codex
```

Claude Code를 사용할 때:

```powershell
npm install -g @anthropic-ai/claude-code
claude
```

[Codex CLI 공식 안내](https://learn.chatgpt.com/docs/codex/cli) · [Claude Code 공식 안내](https://code.claude.com/docs/en/setup)

> 이미 다른 방식으로 CLI를 쓰고 있다면 덮어쓰기 전에 [현재 배포판의 경로 제한](TROUBLESHOOTING.md)을 확인하세요. 로그인은 위젯이 아니라 각 CLI에서 진행합니다. API 키 로그인이나 기업용 인증 방식은 이 위젯의 주간 계정 한도와 다르게 동작할 수 있습니다.

## 2. 설치

1. [최신 설치 파일](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest/download/CodexClaudeQuotaTray-Setup.exe)을 다운로드합니다.
2. 원하면 [릴리스 페이지](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest)의 SHA-256과 다운로드 파일을 비교합니다.
3. 파일을 실행합니다. 관리자 권한은 필요하지 않습니다. 파일은 `%USERPROFILE%\CodexQuotaWidget`에 설치됩니다.
4. 처음에는 작업 표시줄 오른쪽에 ChatGPT·Claude 아이콘이 모두 나타납니다. 아래의 표시 설정에서 사용하지 않는 서비스를 숨깁니다. 표시 중인 서비스는 약 2분마다 다시 조회합니다.

체크섬 확인 명령:

```powershell
Get-FileHash "$env:USERPROFILE\Downloads\CodexClaudeQuotaTray-Setup.exe" -Algorithm SHA256
```

다운로드 위치가 다르면 명령의 경로도 바꾸세요. 설치 파일은 현재 **코드 서명이 없습니다**. 출처와 체크섬을 확인한 뒤 실행하세요.

## 3. 사용

- **마우스 오버:** 해당 서비스의 다음 주간 리셋을 `09-30(수) 18:00 한국시간` 형식으로 확인합니다.
- **오른쪽 클릭:** 계정 ID, 리셋 시각, 마지막 조회 시각을 확인합니다.
- **계정 확인:** 오른쪽 클릭의 **계정 ID·상태**를 확인합니다. 별도 연동 화면 없이 각 CLI의 현재 로그인을 이용합니다. [연동 방식 자세히 보기](ACCOUNT.md).
- **한 서비스 숨기기:** 남길 아이콘을 오른쪽 클릭 → **표시 설정...** → 사용할 서비스만 체크 → **저장**. 숨긴 서비스의 조회도 중지됩니다. 다시 표시할 때는 남아 있는 아이콘에서 설정을 엽니다.
- **왼쪽 클릭:** 해당 서비스의 웹 채팅 페이지를 엽니다.
- **`--%`:** 최근 조회가 성공하지 않았다는 뜻입니다. [문제 해결](TROUBLESHOOTING.md)을 확인하세요.
- **`~83%`:** 일시적 조회 실패 중 같은 계정의 최근 정상값을 표시한다는 뜻입니다. 오른쪽 클릭으로 마지막 정상 조회 시각을 확인하세요.

표시 시각은 PC 시간대와 관계없이 한국시간(UTC+9)입니다. 예시 날짜와 숫자는 설명용입니다.

## 4. 업데이트·중지

업데이트는 새 릴리스의 설치 파일을 **같은 Windows 계정에서 다시 실행**합니다. 기존 자동 실행과 위젯 파일을 갱신하고, 선택한 서비스 표시 설정은 유지합니다.

위젯을 중지하고 로그인 자동 실행을 해제하려면:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$env:USERPROFILE\CodexQuotaWidget\uninstall.ps1"
```

이 명령은 설치 폴더를 남깁니다. 파일까지 지우려면 중지 후 `%USERPROFILE%\CodexQuotaWidget` 폴더를 직접 삭제하세요. 이 폴더에는 설치 후 만들어진 계정 표시와 조회 결과가 들어갈 수 있습니다. 설치 기록은 `%TEMP%\CodexClaudeQuotaTray-Setup.log`에 있습니다.
