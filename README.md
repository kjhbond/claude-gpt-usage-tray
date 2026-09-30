# Codex + Claude 주간 한도 작업 표시줄 위젯

Windows 11 작업 표시줄의 알림 영역 옆에서 **Codex와 Claude Code의 주간 잔여 한도**를 확인하는 개인용 오픈 소스 위젯입니다. 두 서비스 중 **사용하는 것만 표시**할 수 있습니다. 아이콘에 마우스를 올리면 다음 리셋 시각을 `월-일(요일) 시:분 한국시간`으로 보여줍니다.

**독수리 모양 아이콘은 Windhawk 실행 아이콘이며 사용량 표시가 아닙니다.** 최신 설치 파일은 이 아이콘을 숨깁니다. Windows 작업 표시줄 모드가 붙지 못하면 알림 영역에 Codex·Claude의 숫자 아이콘을 대신 표시합니다. 마우스를 올리면 어느 서비스의 잔여율인지 확인할 수 있고, 모드가 동작하면 예비 아이콘은 자동으로 사라집니다. 숫자가 `--%`이면 [계정·조회 문제 해결](docs/TROUBLESHOOTING.md)을 확인하세요.

예비 숫자 아이콘은 Windows의 숨겨진 아이콘 메뉴(`^`)에 들어갈 수 있습니다. 항상 보이게 하려면 해당 아이콘을 작업 표시줄 알림 영역으로 끌어 놓으세요.

> 이 숫자는 일반 ChatGPT·Claude 웹 채팅 사용량이 아닙니다. 각 CLI 계정의 **주간 코딩 도구 한도**입니다. OpenAI나 Anthropic의 공식 앱이 아닙니다.

![작업 표시줄에서 Codex 87%, Claude Code 99%가 표시된 실제 화면](docs/taskbar-preview.png)

| 표시 | 의미 | 동작 |
| --- | --- | --- |
| ChatGPT 아이콘 `87%` | Codex 주간 한도 87% 남음 | 왼쪽 클릭: ChatGPT 열기 |
| Claude 아이콘 `99%` | Claude Code 주간 한도 99% 남음 | 왼쪽 클릭: Claude 열기 |
| 각 아이콘에 마우스 올리기 | `09-30(수) 18:00 한국시간`처럼 주간 리셋 시각 표시 | 오른쪽 클릭: 계정·리셋·최근 조회 시각 |
| `~83%` | 같은 계정의 30분 이내 마지막 정상값. 일시적 조회 실패 중 | 오른쪽 클릭: 상태·마지막 정상 조회 시각 |
| `--%` | 아직 성공한 조회가 없거나 현재 값을 읽지 못함 | 오른쪽 클릭: 상태 확인 후 [문제 해결](docs/TROUBLESHOOTING.md) |

**한 가지만 쓰는 경우:** 남겨 둘 아이콘을 오른쪽 클릭 → **표시 설정...** → Codex 또는 Claude Code만 선택 → **저장**. 숨긴 서비스의 한도 조회도 중지됩니다. 언제든 남은 아이콘에서 설정을 다시 열어 둘 다 표시할 수 있습니다.

![Codex와 Claude Code 중 표시할 서비스를 선택하는 설정 창](docs/settings-preview.png)

예시 숫자와 날짜는 실제 계정의 값이 아닙니다. 표시 중인 서비스는 2분마다 조회하며, 남은 비율은 소수점 아래를 버려 표시합니다.

## 다운로드와 설치

**[최신 Windows 설치 파일 다운로드](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest/download/CodexClaudeQuotaTray-Setup.exe)** · [릴리스와 체크섬 보기](https://github.com/kjhbond/claude-gpt-usage-tray/releases/latest)

현재 배포판은 **Windows 11 x64**용입니다. 설치 전에 같은 Windows 사용자 계정에서 아래를 준비하세요.

1. `python` 또는 `py` 명령으로 실행할 수 있고 `pythonw.exe`가 함께 있는 Python 3
2. Codex를 표시한다면 **npm으로 설치한** [Codex CLI](https://learn.chatgpt.com/docs/codex/cli)와 ChatGPT 계정 로그인
3. Claude Code를 표시한다면 **npm으로 설치한** [Claude Code](https://code.claude.com/docs/en/setup)와 Claude 계정 로그인
4. 처음 조회와 Windows 작업 표시줄 기호 검색에 필요한 인터넷 연결

설치 파일을 실행하면 `%USERPROFILE%\CodexQuotaWidget`에 파일을 놓고 현재 사용자 로그인 시 자동 실행을 등록합니다. 처음에는 두 서비스가 모두 보이며, 설치 후 표시 설정에서 하나를 숨길 수 있습니다. CLI와 Python을 자동 설치하거나 로그인시키지는 않습니다. Windows 보안 경고가 표시될 수 있는 **서명되지 않은** 개인 빌드입니다.

처음 설정하는 분은 **[설치·사용 안내](docs/START_HERE.md)**를 순서대로 읽어 주세요. 현재 버전은 CLI가 npm 전역 패키지로 설치된 기본 경로를 사용합니다. 다른 설치 방식은 CLI 자체가 정상 작동해도 위젯에서 `--%`로 보일 수 있습니다.

## 안내 자료

- [제품 소개와 화면 읽는 법](docs/OVERVIEW.md): 무엇을 보여주는지, 클릭·마우스 오버 사용법
- [처음 설치와 업데이트·중지](docs/START_HERE.md): 준비 명령, 설치, 재시작, 제거
- [문제 해결](docs/TROUBLESHOOTING.md): `--%`, 아이콘 없음, 로그인·네트워크·로그 확인
- [개인정보와 데이터 흐름](docs/PRIVACY.md): 로컬에 저장되는 내용과 외부 요청
- [계정 연동 방식](docs/ACCOUNT.md): 별도 로그인 없이 CLI 계정을 읽는 방식, `--%`와 `~%`의 차이
- [소스 구조와 데이터 흐름](docs/ARCHITECTURE.md): 구성 파일, 조회 경로와 설정 반영 방식
- [버그 신고](https://github.com/kjhbond/claude-gpt-usage-tray/issues/new/choose): 공유하면 좋은 정보와 가려야 할 정보

## 작동 방식과 제한

Codex 조회기는 로컬 Codex app-server의 `account/rateLimits/read` 결과에서 7일(`10080`분) 한도를 읽습니다. Claude 조회기는 기존 Claude Code 로그인 정보를 이용해 Anthropic의 `seven_day` 사용량을 읽습니다. **Claude 사용량 주소는 공개 문서화된 안정 API가 아니므로 변경될 수 있습니다.** 두 조회기는 새 대화나 모델 작업을 시작하지 않습니다.

별도의 계정 연결은 필요하지 않습니다. 설치한 Windows 계정에서 각 CLI에 로그인한 계정을 이용합니다. Claude가 일시적인 요청 제한(`429`)을 받으면 동일 계정의 최근 정상값을 `~83%`처럼 표시하고, 오른쪽 클릭 메뉴에 조회 상태와 마지막 성공 시각을 보여줍니다. 표시 설정 변경 시 이미 실행 중인 조회기는 다시 시작하지 않습니다. 자세한 연결 방식은 [계정 연동 안내](docs/ACCOUNT.md)를 보세요.

계정 토큰, 계정 이름, 이전 한도 결과는 저장소나 설치 파일에 포함되지 않습니다. 설치 후 계정 표시와 조회 결과는 설치된 PC에만 기록됩니다. 숨긴 서비스의 조회기는 실행하지 않습니다. 자세한 내용은 [개인정보와 데이터 흐름](docs/PRIVACY.md)에 있습니다.

## 소스와 재빌드

`codex-weekly-quota.wh.cpp`가 작업 표시줄을 그리는 Windhawk 모드입니다. `quota.py`, `claude_quota.py`, `display_state.py`, `metadata.py`가 한도·상태·시간을 처리하고, `Launcher.cs`가 실행을 시작합니다. [소스 구조](docs/ARCHITECTURE.md)와 [빌드 절차](docs/BUILD.md)를 공개했습니다.

| 소스 | 확인할 내용 |
| --- | --- |
| [작업 표시줄 모드](codex-weekly-quota.wh.cpp) | 아이콘·백분율·툴팁·오른쪽 클릭 메뉴 |
| [Codex 조회](quota.py) · [Claude 조회](claude_quota.py) | 로컬 CLI 계정에서 주간 한도를 읽는 경로 |
| [조회 상태](display_state.py) · [시간 형식](metadata.py) | `--%`와 `~이전값%` 처리, 한국시간 변환 |
| [표시 설정](Settings.cs) · [조회기 동기화](sync-pollers.ps1) | 선택한 서비스만 표시·조회하고, 켜진 조회기는 유지 |
| [설치기](installer/Setup.cs) | 현재 사용자 설치, 업데이트 때 표시 설정 보존 |

작업 표시줄 접근 코드는 [m417z의 Windhawk 모드](https://github.com/ramensoftware/windhawk-mods/blob/main/mods/taskbar-tray-system-icon-tweaks.wh.cpp)를 바탕으로 합니다. 소스 라이선스는 [GPL-3.0](LICENSE), 포함된 프로그램과 로고의 출처는 [제3자 고지](THIRD_PARTY_NOTICES.md)를 참고하세요.
