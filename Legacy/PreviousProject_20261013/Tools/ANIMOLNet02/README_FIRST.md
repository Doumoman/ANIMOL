# ANIMOL NET02 — 기존 UI 기반 DEV 대기방 서버 연동

기준: 2026-10-05 · Unity 6000.3.8f1 · `ANIMOL_UI_IMPLEMENTATION_AND_SERVER_HANDOFF.md` · 인수인계 HEAD 82e777e + 미커밋 작업.

## 제공하는 것

Python 표준 라이브러리 HTTP/SQLite 대기방 서버, Unity C# HTTP 클라이언트와 중앙 입장 요청/승인 관리 소스, 기존 UI 적용 작업 지시, Android/LAN 테스트 절차를 제공한다. 대기방의 최대 참가자는 서버에서 4명으로 검증한다. 커스텀 생성/코드 조회/참가, 일반·랭크별 대기방 배정, Ready, 방장 시작, 나가기와 방장 이전을 테스트하는 범위다.

**이 ZIP은 APK나 기존 프로젝트에 이미 적용된 패치가 아니다.** 실제 프로젝트 원본과 Unity 실행 환경이 이 제작 환경에 없었다. 제공된 클라이언트 API를 기존 Phase3 어댑터·호스트·SC06에 연결하는 프로젝트 작업이 필요하며, 그 작업을 `Docs/NET02_PROJECT_APPLY.md`에 지정했다. 프로젝트 적용 결과를 받기 전까지 실제 UI 연결/Unity 컴파일/Android 성공으로 보고하지 않는다.

서버의 입장 Accepted는 DEV 서버의 방 입장 승인이다. Google 인증, 운영 계정 동물 권한, 실제 경기/랭킹/재화 승인이 아니다. `Started`는 대기방 시작 게이트 통과이며 Gameplay 씬을 로드하거나 3맵 경기를 실행하지 않는다.

## 기존 UI를 연결하는 구조

| 기존 지점 | NET02 연결 내용 |
|---|---|
| MissingMultiplayerEntry/View | 실제 모드 카탈로그·진입 의도, 커스텀 생성 또는 코드 조회를 공급 |
| AnimalMultiplayerPhase3Host | 서버 문맥을 기존 `Open(request)`로 전달. 실제 승인 전 대기방 이동 금지 |
| AnimalMultiplayerProjectAdapter | 권한 snapshot 조회, 정확한 3역할 편성의 단일 입장 요청과 결과 조회 |
| 중앙 관리자 | 불변 ActionId/전체 요청 보존, Unknown 복구, 실제 receipt 검증·한 번 처리 |
| SC06 / 새 Room 프리팹 | 서버 참가자/방장/Ready/연결 상태와 버튼 권한 표시 |
| SC07 | 기존 레거시/DEV 경로로 보존. 새 입장 승인 경로에 사용하지 않음 |

소스는 Canvas/EventSystem/독립 대기방 UI를 생성하지 않는다. UI 바인딩은 실제 프로젝트 타입을 확인해 작성한다. 기존 Source에 없는 override·메서드 시그니처를 존재한다고 가정하지 않는다.

## NET01과 관계

NET01의 독립 NGO/IMGUI 대기방은 기존 UI 통합 대상에서 제외한다. NET02 대기방은 HTTP API를 사용한다. 입장·권한·Ready의 상태 변경에는 HTTP가 적합하며 실제 이동/방울/출구 동기화용 Unity 게임 서버는 후속 작업이다. 두 대기방을 동시에 연결하거나 같은 확인 동작에서 각각 create/join을 실행하지 않는다.

## 시작 순서

1. `Docs/NET02_PROJECT_APPLY.md`의 TASK 01~06을 프로젝트 작업 담당자에게 적용한다.
2. 서버 단독 검증: 패키지 루트에서 `python3 -m unittest discover -s ServerTests -v`.
3. 기본 서버 실행은 `Server/config.empty.json`을 사용한다. 데이터 미설정으로 입장을 차단하는 것이 정상이다.
4. 실제 IdMap/모드/정책 소유자가 확인한 데이터를 별도 로컬 config에 제공한다. `Docs/DATA_MAPPING.md` 참고.
5. 서버 실행: `python3 Server/server.py --config Server/config.empty.json --database Runtime/net02.sqlite3 --bind 0.0.0.0 --port 8080`.
6. 기존 Bootstrap→Lobby 동선으로 Windows/Android DEV 빌드를 생성하고 `Docs/ANDROID_LAN_TEST.md`대로 테스트한다.

Windows의 Python 실행 명령은 설치 환경에 따라 `py -3` 또는 `python`이다. 서버 코드는 Python 3.10 이상을 기준으로 작성되며 별도 pip 의존성이 없다.

## 데이터 차단과 검증

- config는 실제 animalId↔stableArtId↔role, 구현 여부, DEV 계정별 허용 동물, 초기 편성, 실제 modeId·정책 revision·opaque EntryIntent의 명시적 공급을 요구한다.
- 실제 종 asset은 인수인계 기준 RABBIT만 확인되며 특수/공중 14종은 미매핑이다. 이미지 ID로 실제 종 ID를 추정하지 않는다. 자료가 없으면 운영 화면의 3종 편성 입장 테스트는 blocked로 남긴다.
- `ServerTests/fixture_config.json`은 서버 프로토콜 검증용 TEST_* 데이터다. 운영 IdMap·Catalog·계정 해금 상태로 복사하거나 실제 15종 구현으로 보고하지 않는다.
- `ExternalServiceConfiguration.asset`의 account/match/purchase/ad 플래그를 true로 바꾸지 않는다. DEV 연결은 별도 define과 Development 빌드에서만 사용한다.
- HTTP/DEV 세션과 로컬 journal은 출시 인증·암호화 저장의 대체물이 아니다. 공개 운영 배포는 후속 인증/TLS/서비스 교체를 요구한다.

## 파일과 결과

- `Server/`: 대기방 상태/SQLite/HTTP API와 설정 설명.
- `Assets/ANIMOL/NetUiDev/`: Unity DTO/클라이언트/중앙 입장 관리 소스와 DEV 메뉴.
- `Docs/`: 적용 순서, 데이터 책임, Android/LAN·클라우드 전환, 결과 작성 양식.
- `ServerTests/`: 실제 HTTP/동시 요청/DB 재개방 검증.
- `Docs/VALIDATION_STATUS.md`: 이 제작 환경에서 실제 실행한 검사와 미실행 항목.

적용 과정에서 기존 UI 아트·15종 PNG·v7 배경·1080×1920 CanvasScaler·Safe Area·폰트·기존 EventSystem·캠페인 로더·BM·협동 정책·사용자 미커밋 작업을 보존한다.
