# Missing UI V1 작업 2 운영 연결

Unity 메뉴: **ANIMOL > Missing UI V1 > Build Project Task 2 Settings and Account**.
먼저 작업 1의 `Resources/ANIMOLMissingUiV1/Art.asset`이 있어야 한다.
이 메뉴는 Settings/Controls/Account/ControlReset 프리팹과 UtilityArt만 생성한다.
기존 전체 UI 생성 메뉴나 패키지 시안 생성 메뉴를 대신 실행하지 않는다.

`MissingUtilityEntry`가 현재 Lobby의 UiNavigationService 아래 SC13/SC19/SC20에 설치한다.
기존 자식은 비활성으로 보존하며 기존 navigation/history, Canvas, SafeArea, EventSystem을 사용한다.
운영 프리팹에 Canvas, EventSystem, 데모 host를 추가하지 않는다.
비활성 레거시 버튼이 남으므로 버튼은 새 `MissingUtilityView`의 명시적 참조로 찾는다.

설정 저장 소유자는 기존 `MobileControlPreferences`다. 새 저장소/기본값/계정 저장을 만들지 않는다.
변경 시 기존 HUD `MobileControlLayoutApplier`와 접근성 적용기를 호출한다.
기기 미리보기는 실제 SC15 HUD Image 6개를 참조해 시각 요소만 복제하며 같은 layout 계산을 사용한다.
게임플레이 Button/입력/능력 스크립트는 미리보기에 복제하지 않는다.

초기화 확인은 `UiModalStack`의 MissingControlResetModal → 기존 `MobileControlPreferences.Reset()`이다.
크기/위치/투명도/좌우손/하향 버튼/큰 글씨/진동/언어 8개 설정이 대상이다.
전체 설정 초기화, 오디오 설정, Google 인증, 저장 비교/충돌 해결 실행은 실제 서비스가 없어 비활성이다.
이 버튼들을 활성화하려면 실제 조회·저장·실행 결과 계약을 먼저 연결해야 한다.
기존 DevPreview의 예시 SaveConflict 값을 운영 계정 데이터로 사용하지 않는다.

검사: `MissingUtilityFlowTests` (PlayMode), `CommercialPolishPolicyTests` (EditMode).
테스트는 사용한 8개 PlayerPrefs의 존재 여부/값을 복원한다.
실행 결과와 Game View 증거는 루트 `ANIMOL_MISSING_UI_V1_TASK2_RESULT.md` 참조.
