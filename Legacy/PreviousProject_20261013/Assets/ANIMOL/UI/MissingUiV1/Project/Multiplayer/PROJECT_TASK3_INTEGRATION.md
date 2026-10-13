# Missing UI V1 작업 3 운영 적용

메뉴: **ANIMOL > Missing UI V1 > Build Project Task 3 Multiplayer**.
작업 1의 공용 Art와 작업 2의 UtilityArt가 먼저 있어야 한다. 메뉴는
`Project/Resources/ANIMOLMissingUiV1/Multiplayer`의 전용 프리팹 5개와 MultiplayerArt만 생성한다.
기존 씬, 동물 UI/어댑터/카탈로그, 작업 1·2 프리팹, 패키지 Generated를 재생성하지 않는다.

`MissingMultiplayerEntry`가 Lobby의 기존 SC05에 허브를 설치한다.
추가 화면 ID `MissingUiV1_Custom/Create/Join`은 로컬 navigation ID일 뿐 ModeId/EntryIntent가 아니다.
기존 SC06에는 서버 상태 미조회 프레임을 설치한다. 입장 승인이 없으므로 새 허브에서 SC06으로
이동하는 경로는 없으며, 기존 개발용 협동/경쟁 미리보기는 원래 SC06 자식을 사용한다.
개발용 참가자/ready를 운영 프레임에 복사하지 않는다.

일반/랭크 및 방 옵션의 동물 열람 → 기존 `AnimalMultiplayerPhase3Host.OpenUnconfigured()`.
실제 모드/진입 의도/정책 공급자가 없으므로 선택한 카드 이름을 임의 문맥 값으로 만들지 않는다.
실제 공급자가 생기면 기존 `host.Open(MultiplayerSelectionRequest)`에 공급자가 소유한 깊은 복사
문맥을 전달해야 한다. 여기서 별도 create/join을 실행해서는 안 된다.

현재 조회/생성/참가/Ready/시작/초대/방 나가기/코드 복사는 비활성이고 리스너도 없다.
`ExternalServiceConfiguration.MatchServerConnected` 플래그만으로 이 기능을 활성화하지 않는다.
실제 방 snapshot/capability, 승인 receipt 소비자, 중앙 미확인 요청/복귀 계약은 미지원이다.
단순 화면 이동/로컬 선택을 승인이나 취소 성공으로 표시하지 않는다.

방 코드는 원래 문자열을 받는 TMP_InputField다. 임의 길이·대문자화·정규식 검증을 추가하지 않는다.
입력/붙여넣기는 로컬 UI에만 남고 계정 저장/조회/참가를 발생시키지 않는다.
TMP의 모바일 키보드 숨김 setter는 Windows Editor에서 true를 강제하므로 생성 중 호출하지 않는다.
모바일 표시 설정 false를 직렬화해 보존하되 실제 Android/iOS 키보드 동작은 별도 기기 검사해야 한다.

최신 화면 기준은 최대 4인이다. 기존 `MultiplayerModeRules.CompetitiveParticipants`의 4~8과
M6 개발용 생성/검사 코드는 이번에 덮어쓰지 않았다. 운영 정책 공급자를 연결하기 전에
해당 소유자·기존 데이터·테스트를 함께 이관해야 한다. 협동 2~4 규칙과 동물 중복 허용을 보존한다.

검사: `MissingMultiplayerFlowTests`, 기존 `MultiplayerPhase3TransactionTests`,
`CommercialPolishPolicyTests` 및 작업 1·2/동물 Phase1~3 회귀.
전체 결과와 남은 계약은 루트 `ANIMOL_MISSING_UI_V1_TASK3_RESULT.md` 참조.

기존 UiButtonFeedback은 다른 파일의 보조 MonoBehaviour 클래스여서 프리팹 MonoScript 참조를 저장할 수 없다.
이번 빌더는 해당 컴포넌트를 저장 전에 제거하고 view가 실행 시 1회 붙인다. 새 프리팹 하위 전체에 누락 스크립트가 없어야 한다.
