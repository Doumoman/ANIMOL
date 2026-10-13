# NET02 프로젝트 적용 명령 — 기존 UI에 DEV 서버 연결

이 파일의 TASK 01~06을 순서대로 수행한다. 정본은 `ANIMOL_UI_IMPLEMENTATION_AND_SERVER_HANDOFF.md`(2026-10-05)이며 이전 UI 현황 보고서로 상태를 되돌리지 않는다. 프로젝트는 `C:/Users/user/Documents/GitHub/ANIMOL`, Unity 6000.3.8f1이다. 현재 미커밋 변경도 보존한다.

## 공통 작업 규칙

- 목적은 기존 일반/랭크/커스텀 UI와 Phase3 동물 선택에 최대4인 DEV 대기방 서버를 연결하는 것이다. 실제 경기, Google 로그인, 코인/BM/결제/광고, 협동 서비스는 범위 밖이다.
- 이 패키지의 Assets는 프로젝트 루트에 병합한다. 기존 파일을 확인 없이 덮어쓰지 않는다. Server/ServerTests/Tools/Docs는 저장소의 별도 `Tools/ANIMOLNet02/` 또는 충돌 없는 디렉터리에 보관한다.
- Unity 클라이언트는 `ANIMOL_NET_UI_DEV` 및 Development 빌드에서만 사용할 수 있어야 한다. 실제 서버 연결 플래그를 true로 바꾸지 않는다. 기존 로비 온라인 제한의 DEV 진입은 별도 명시적 조건으로 처리한다.
- 프로젝트 측 NET02 타입 참조도 같은 `#if ANIMOL_NET_UI_DEV`로 격리하고 define 없는 출시 컴파일에서 기존 disconnected 경계를 확인한다.
- 기존 Missing UI Hub/Custom/Create/Join/Room 및 Phase3 프리팹을 사용한다. SC07 또는 NET01 독립 대기방으로 연결하지 않는다. 새 Canvas/EventSystem/플레이어·가짜 참가자를 만들지 않는다.
- 동물15 PNG, v7 연출, 1080×1920 CanvasScaler, SafeAreaLayout, pixelroborobo/TMP/PixelTextBridge, EventSystem/StandaloneInputModule, 기존 네비게이션/모달을 유지한다.
- 원본 타입을 읽고 메서드/필드/enum 시그니처를 확인한다. 문서 이름을 근거로 override 시그니처를 발명하거나 컴파일 오류를 숨기지 않는다.
- config가 비어 있거나 실제 종/권한/모드가 없으면 Unconfigured/Unavailable/`--`를 유지한다. TEST_* fixture를 운영 IdMap에 넣지 않는다.

## TASK 01 — 현 프로젝트 조사·충돌 확인

다음 파일을 읽고 실제 이벤트와 호출자를 기록한다.

1. `Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/MissingMultiplayerEntry.cs`, `MissingMultiplayerView.cs`, `MissingRoomCodeInput.cs`.
2. `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalMultiplayerProjectAdapter.cs`, `AnimalMultiplayerPhase3Host.cs`, 운영 IdMap/Catalog.
3. `Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiContracts.cs`, `AnimalUiPresenter.cs`, `AnimalUiRules.cs`.
4. `Assets/ANIMOL/Scripts/UI/UiNavigationService.cs`, `UiModalStack.cs`, `MultiplayerUiPresenter.cs` 및 새 Room 프리팹의 SC06 컨테이너.
5. `Assets/ANIMOL/Scripts/Core/MultiplayerMatchContracts.cs`, 실제 모드/진입 의도 소유자, 기존 빌드 씬/로비 게스트·온라인 차단 경계.

출력: `NET02_TASK01_INSPECTION.md`에 정확한 타입/시그니처/현재 구독·반환 경로/미확정 데이터/수정 파일 목록을 기록한다. 조사 후 후속 연결에 필요한 최소 변경을 진행한다. 실제 콘텐츠 데이터 부재는 blocked로 기록하고 독립 서버/클라이언트 작업은 계속한다.

## TASK 02 — 경쟁 4인 정책 이관

- 경쟁의 기존 `(4,8)` 및8인 생성/렌더링/검사 기준을 최신 최대4 정책과 일치시킨다.
- 대상: MultiplayerMatchContracts, MultiplayerUiPresenter, UiBuildPipeline.M6Multiplayer, M6MultiplayerContractTests와 실제 영향 있는 DEV 데이터/검사.
- 기존 최소4 공개 경쟁과 커스텀 최소 인원은 정책 소유자를 구분한다. 커스텀 최소2는 NET02 DEV config 제안이며 운영 규칙으로 임의 확정하지 않는다.
- 협동 2~4·별도 편성/중복 정책을 변경하지 않는다.
- 서버 동시5번째 입장도 거절되며 UI4행 밖으로 참가자가 사라지는 표시 오류가 없어야 한다.

## TASK 03 — 실제 데이터와 모드 문맥 공급

- `Server/API.md`와 `Docs/DATA_MAPPING.md`를 읽고 실제 종ID/역할/아트ID를 명시적으로 매핑한다. 기존 asset에는 RABBIT만 확인되어 있어 Ground/Special/Air 완전한 편성이 현재 없다.
- 실제 ModeId·표시명·PolicyRevision·초기 편성·허용 풀·대표 역할·opaque EntryIntent 소유자를 찾는다. 라벨/배열 순서/DEVPreview ID를 운영ID로 삼지 않는다.
- EntryIntent는 서버 설정에서 create/join/public 처리 종류에 연결한다. 일반과 랭크의 ModeId 및 성장 정책을 보존하고 커스텀 진입 방식과 모드 규칙을 혼합하지 않는다.
- 코드 참가: 기존 단일 TMP 입력에서 서버 lookup을 호출하고 방의 실제 mode/policy/참가 조건을 확인한 후 Phase3 문맥을 만든다. 입력만으로 방 확인/입장을 완료 처리하지 않는다.
- 허브는 문맥이 완전할 때 기존 `AnimalMultiplayerPhase3Host.Open(request)`로 진입하고 불완전할 때 `OpenUnconfigured()` 열람만 제공한다.
- 서버 DEV 계정 자격 증명은 로컬 테스트용이다. 기존 AccountIdentityState를 Linked로 바꾸지 않는다. 연결 대상/계정 설정은 별도 DEV 설정에서만 제공한다.

출력: `NET02_DATA_BINDING.md`에 실제 공급자, 명시적 왕복 매핑, 미설정 항목을 기록한다. 자료가 없으면 fixture 테스트와 운영 UI 차단 테스트를 구분한다.

## TASK 04 — Phase3 어댑터와 중앙 입장 관리 연결

패키지의 `NetUiDevCoordinator` 및 HTTP DTO/서비스를 사용하고 프로젝트 타입에 맞는 얇은 변환 코드를 작성한다. 기존 Bootstrap이 전용 서비스 GameObject에 한 번 부착하며 기존 Canvas/화면 루트 전체를 영속화하지 않는다. 이미 있는 중앙 관리자가 확인되면 요청/receipt 책임을 통합하고 두 관리자가 동시에 제출·소비하지 않게 한다.

1. DEV 활성·세션 유효 상태에서만 HTTP snapshot을 기존 `ReadMultiplayerSnapshotAsync`에 변환한다. revision·선택3종의 Implemented/HasContextPermission/CanUseInContext·서버 허용풀을 검증한다.
2. 실제 성장/능력 데이터가 공급되지 않은 필드는 `--`/미설정으로 남긴다. 요청을 통과시키려고 효과나 성장 최대치·보유치를 합성하지 않는다. 입장에 필수인 공급이 없으면 차단한다.
3. 기존 `SubmitRoomEntryAsync`는 확인한 불변 요청을 중앙 관리자에1회 전달한다. 기존 ActionId를 그대로 사용한다. 별도 create/join/Ready 호출을 추가하지 않는다.
4. 중앙 관리자는 제출 전에 계정/endpoint/ActionId/전체 payload를 저장한다. timeout/예외/응답 유실은 Unknown이며 신규 입장과 편성 변경으로 재요청을 만들지 않는다.
5. 결과 확인은 같은 ActionId·전체 payload를 사용한다. 서버 영속 기록과 복구 경로를 사용하고 receipt를 UI 요청값으로 합성하지 않는다.
6. 정확한 AcceptanceToken/AcceptedContextId/AcceptedPolicyRevision/AcceptedEntryIntent/AcceptedLoadout를 검증해 기존 Accepted 타입으로 전달한다. ContextId는 실제 ModeId다. RoomId는 별도 방 식별자다.
7. central receipt 이벤트와 기존 `MultiplayerAccepted`가 두 번 이동하지 않도록 최종 소비자를 한 곳으로 정한다. 제출/확인/표시/소비 카운터를 토큰 원문 없이 남긴다.
8. Rejected/Unavailable은 사유를 표시하고 명시적 최신 조회 후 새 확인을 요구한다. 거절된 내용 변경에 같은 ActionId를 재사용하지 않는다.
9. 계정/endpoint 변경은 pending/Unknown/미탈퇴 방이 있는 동안 차단한다. 승인된 나가기 또는 결과 확정 후에만 전환한다.

`IMatchGateway.RequestReady(string[])`는 Phase3 입장 계약의 대체물이 아니다. 동물/계정 구매/광고의 운영 G-04까지 이 DEV 관리자가 완료했다고 보고하지 않는다.

## TASK 05 — 승인 소비·SC06 상태·복귀 연결

- 실제 승인 receipt를 SC06 Room 뷰에 전달하고 서버 RoomState의 참가자 배열만 렌더링한다. 본인/방장/연결/Ready/3종 편성/4슬롯과 승인된 capability를 표시한다.
- `AcceptedReceiptReady` 알림 후 `PollRoomAsync()`로 방을 검증하고 `TryDeliverAccepted`에 기존 SC06 소비자를 연결한다. 실제 바인딩/네비게이션 성공 뒤 소비 확인을 저장한다. 처리 중 구독 중복과 씬 파괴/재진입을 검사한다.
- 소비된 receipt가 남은 앱 재시작은 새 입장이 아니다. 현재 방 상태를 재조회해 SC06을 복원하고 accepted 이벤트/거래를 다시 실행하지 않는다.
- 대기방 활성 상태에서 단일 polling 루프를 사용하고 이전 room/revision의 늦은 응답이 새 화면을 덮어쓰지 않게 한다. 숨김/씬 전환 구독은 해제한다.
- Ready는 서버에 원하는 bool을 전달하고 승인 상태로 다시 그린다. 응답 전 임의 Ready 성공을 만들지 않는다.
- 시작은 방장/최소인원/전원Ready/대기상태 검증 후 서버 호출1회. Started이면 대기방에 `시작 승인 · 경기 연결 대기`를 표시한다. Gameplay 로더/3맵 경기로 보내지 않는다.
- 코드 복사는 서버 조회 결과의 실제 코드만 사용한다. 초대 기능은 미제공 capability면 비활성으로 둔다.
- 나가기는 승인된 leave 후 중앙 현재방·receipt 상태 정리 및 원래 허브/진입 화면 복귀. 응답이 유실되거나 앱 재시작 후 방 조회가404면 저장한 승인 RoomId로 LeaveRoomAsync를 반복해 Accepted/ALREADY_LEFT를 확인한다. 조회404만으로 요청 보존을 해제하지 않는다.
- 연결 끊김은 실제 heartbeat/조회 실패 기준으로 표시한다. 자동 충원·가짜 참가자·운영 재접속 성공을 합성하지 않는다.

## TASK 06 — 검증·Android DEV 빌드·결과

1. `python -m unittest discover -s ServerTests -v`와 Unity 컴파일을 실행한다. 의미 있는 기존 Phase3 거래/네비게이션/포트레이트 검사도 실행한다.
2. 새 검증: 취소요청0/확정1/연타추가0, 실제 서버 receipt와3종 일치, 다른 플레이어 동일종 허용, stale revision/권한 거절, 동시5번째 거절, Unknown 앱재시작 복구, 중앙 소비1회, 승인전 이동0.
3. 기존 허브→Phase3→승인→SC06→Ready→방장시작→나가기 흐름을 실제 HTTP 서버로 검사한다. 기본 미설정 config의 차단도 검증한다.
4. 기존 Bootstrap/Lobby 등 enabled 씬을 사용하는 패키지 DEV 빌드 메뉴로 Windows·Android APK를 생성한다. 별도 테스트 앱 ID와 debug 서명, Portrait, Development, INTERNET, HTTP DevelopmentOnly만 임시 사용하고 설정을 복원한다.
5. phone1+PC3부터 phone4 테스트를 진행한다. 같은 LAN의 PC 사설 IP:8080을 사용한다. UDP7777을 여는 NET01 절차는 사용하지 않는다.
6. 실제 notch/SafeArea/TMP 키보드·붙여넣기·OS Back·동시터치·백그라운드/복귀·프로세스 재시작을 실행한다. Editor 결과를 기기 pass로 확대하지 않는다.
7. SDK/기기/매핑이 없으면 APK나운영입장 성공을 만들지 않고 blocked 사유를 기록한다. 가능한 소스/서버/Unity 검증은 계속한다.

최종 출력: `NET02_RESULT.md`, Unity/서버 검사 로그, 요청/승인/소비 카운터, 캡처, 실제 APK 절대 경로. `Docs/NET02_RESULT_TEMPLATE.md` 형식을 사용한다. 새 Known issue와 기존 실패를 구분한다. 실제 경기·운영 인증/재화는 범위 밖이라고 명시한다.

## 적용 담당자에게 보낼 짧은 명령

> `ANIMOL_NET02_UI_SERVER_LINK.zip`을 확인하고 `README_FIRST.md`와 `Docs/NET02_PROJECT_APPLY.md`의 TASK01~06을 순서대로 실행해줘. 최신 정본은 `ANIMOL_UI_IMPLEMENTATION_AND_SERVER_HANDOFF.md`야. 기존 Missing Multiplayer UI·Animal Phase3·SC06에 DEV HTTP 대기방 서버를 연결하고 경쟁 최대4로 이관해줘. ActionId/Unknown 복구/서버 receipt/단일 소비를 검증하고 기존 UI·아트·캠페인·BM·협동·미커밋 변경을 보존해줘. 실제 데이터가 없으면 차단을 유지하고 TEST_*를 운영ID에 넣지 마. Windows와 Android DEV 빌드 및 LAN 테스트까지 가능한 범위를 실행한 뒤 NET02_RESULT.md와 로그/APK 경로를 제공해줘. Started는 대기방 시작 승인까지만 처리해줘.
