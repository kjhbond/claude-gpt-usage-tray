# 문제 해결

## 독수리 아이콘만 보이고 숫자 위젯이 없어요

독수리는 Windhawk 실행 아이콘이고 한도 위젯이 아닙니다. [v1.1.2 이상 설치 파일](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest/download/CodexClaudeQuotaTray-Setup.exe)을 **작업 표시줄을 사용하는 Windows 계정**에서 다시 실행하세요. 이 버전은 독수리 아이콘을 숨기고, 작업 표시줄의 `Grid`·`StackPanel` 구조와 Explorer 시작 지연에 대응합니다. 설치 후 숫자 버튼이 실제로 보이는지 확인하며, 확인하지 못하면 성공 대신 경고를 표시하고 종료 코드 2를 반환합니다. 별도 트레이 아이콘으로 대체하지 않습니다.

경고가 나오면 `%TEMP%\CodexClaudeQuotaTray-Setup.log`의 마지막 줄에서 Windows 빌드와 `native widget visible=False`를 확인하세요. 모드가 작업 표시줄 기호를 받아야 하는 Windows 빌드에서는 인터넷 연결과 보안 프로그램의 `explorer.exe` 통신 차단 여부가 영향을 줄 수 있습니다. 일부 Windows 빌드에는 Microsoft 디버그 기호가 공개되지 않아 작업 표시줄 모드가 동작하지 않을 수 있다는 [Windhawk 개발자의 설명](https://github.com/ramensoftware/windhawk-mods/discussions/4543)도 있습니다. 이 경우 [오류 신고](https://github.com/kjhbond/claude-gpt-usage-tray/issues/new/choose)에 Windows 빌드와 **개인 경로를 지운** 설치 로그 마지막 줄을 남기세요.

## 두 아이콘이 모두 안 보여요

1. Windows 11 x64에서 설치했는지 확인합니다.
2. 작업 관리자에서 `windhawk.exe`, `pythonw.exe`가 실행 중인지 확인합니다.
3. `%TEMP%\CodexClaudeQuotaTray-Setup.log`와 `%USERPROFILE%\CodexQuotaWidget\startup-error.txt`가 있으면 내용을 확인합니다.
4. Windows에서 로그아웃한 뒤 다시 로그인합니다. Windows 업데이트 직후에는 Windhawk가 작업 표시줄 기호를 새로 찾는 데 인터넷 연결이 필요할 수 있습니다.

설치 파일은 숫자 버튼을 확인해야 성공을 표시합니다. 확인 실패 경고는 파일 설치 후 작업 표시줄 연결을 검증하지 못했다는 뜻이며, `--%`로 표시되는 계정 조회 실패와는 다른 문제입니다.

## 한쪽 또는 양쪽이 `--%`로 보여요

`--%`는 아직 성공한 조회가 없거나 현재 값을 읽을 수 없다는 뜻입니다. 설치 직후라면 첫 조회가 끝날 때까지 기다려 보세요. `~83%`처럼 앞에 `~`가 붙었다면 같은 계정의 30분 이내 **마지막 정상값**이며, 일시적 조회 실패 상태입니다. 오른쪽 클릭의 **상태**와 **마지막 정상 조회** 시각을 먼저 확인하세요.

1. PowerShell에서 `codex --version`, `claude --version`을 실행합니다.
2. `codex`와 `claude`를 각각 실행해 원하는 구독 계정으로 로그인되어 있는지 확인합니다.
3. 인터넷 연결과 서비스 상태를 확인합니다. Claude 사용량 API의 `429`는 일시적인 요청 제한입니다. 최근 정상값이 있으면 `~값%`으로 표시되고 다음 주기에 재시도합니다. 해당 계정이 해제됐다는 뜻은 아닙니다.
4. 설치 폴더의 `quota.json`(Codex), `claude-quota.json`(Claude)에서 `reason` 필드를 확인합니다. **파일 전체를 공개하지 마세요.** 계정 ID와 조회 시각이 포함될 수 있습니다.
5. 로그인 상태를 새로 고쳤다면 Windows에서 로그아웃·로그인하여 위젯을 다시 시작합니다.

표시 설정을 저장해도 이미 실행 중인 다른 서비스의 조회기는 다시 시작하지 않습니다. 빠르게 켰다 끄는 동작이 불필요한 조회 요청을 만들지 않도록 한 것입니다. [계정 연동 방식](ACCOUNT.md)에서 각 CLI 로그인의 사용 경로를 볼 수 있습니다.

## CLI 설치 경로와 로그인

현재 조회기는 기본 Windows 사용자 npm 전역 폴더인 `%APPDATA%\npm\node_modules` 아래에서 CLI 실행 파일을 찾습니다. npm 외 방식으로 설치했거나 npm 전역 폴더를 바꿨거나 WSL에서만 로그인했다면 찾지 못할 수 있습니다. Codex에는 7일 한도 버킷이, Claude에는 `claude.ai` 구독 계정 로그인과 `seven_day` 사용량이 필요합니다. API 키 또는 다른 인증 방식에서는 이 값이 제공되지 않을 수 있습니다.

Claude Code의 설치 구조 또는 내부 사용량 API가 바뀌면 기존 로그인에서도 값이 표시되지 않을 수 있습니다. 이 경우 [이슈를 열어](https://github.com/kjhbond/claude-gpt-usage-tray/issues/new/choose) 버전과 `reason`만 알려주세요.

## 숫자나 리셋 시간이 예상과 달라요

표시값은 조회 시점의 **주간 잔여율**을 소수점 아래 버림하여 나타냅니다. 조회 간격은 약 2분입니다. 리셋 시각은 한국시간(UTC+9)으로 변환하며 연도는 생략합니다. 두 서비스의 한도 기준과 리셋 시각은 서로 다를 수 있습니다.

## 설치 또는 업데이트가 실패해요

설치기는 Python 3과 `pythonw.exe`를 찾지 못하면 설치를 중단합니다. Python을 설치한 뒤 PowerShell에서 `python --version` 또는 `py -3 --version`이 동작하는지 확인하고 다시 실행하세요. 자세한 오류는 `%TEMP%\CodexClaudeQuotaTray-Setup.log`에 남습니다. 업데이트 중에는 기존 위젯을 중지한 뒤 파일을 복사하므로, 파일 잠금이나 보안 프로그램 차단 메시지도 확인하세요.

## 이슈에 첨부할 내용

Windows 11 빌드, 위젯 릴리스 버전, `codex --version`·`claude --version` 결과, 어느 아이콘이 문제인지, `reason` 한 줄, 설치 로그의 **개인정보를 지운 오류 부분**을 알려주세요. 토큰, `.credentials.json`, CLI 설정 전체, 계정 이메일, `*-info.ini`, `*-quota.json` 전체 파일은 올리지 마세요.
