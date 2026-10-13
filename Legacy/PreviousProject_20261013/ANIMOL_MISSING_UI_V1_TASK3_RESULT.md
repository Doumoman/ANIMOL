# ANIMOL MISSING_UI_V1 — 작업 3 결과

작성: 2026-10-05 / Unity **6000.3.8f1** / 현재 작업 트리 기준.

## 판정

**경쟁 허브·커스텀 분기·방 옵션·코드 입력과 기존 Phase3 동물 열람/반환 동선을 적용했다.** 기존 SC06에는 최대 4인 기준의 미조회 대기방 틀을 설치했다. 운영 화면은 실제 데이터가 없는 참가자·방장·코드·정책을 `--`로 표시한다.

**실제 방 생성/조회/참가, Ready/시작/초대/나가기, 승인 receipt 소비, 중앙 복귀 계약은 미연결이다.** 기능 버튼은 비활성이며 승인 없이 새 화면에서 대기방으로 이동하지 않는다. 따라서 **작업 3의 화면 적용과 미지원 차단을 완료한 것이며, 멀티플레이 운영 기능 전체 완료가 아니다.**

## 조사한 현재 계약

AGENTS.md, 작업 1·2 결과, `UNITY_APPLY_MISSING_UI_V1.md` 작업 3, 패키지 `acceptance-missing-ui.json` MULTI-01~14와 현재 코드, 동물 Phase3 작업 2·3 결과를 확인했다. 시작 시 기존 변경 494개 상태 항목을 분리했다. Unity CLI로 실행 중인 Unity 6000.3.8f1을 사용했다.

| 실제 소유자 | 확인 결과 |
|---|---|
| `UI/PortraitArtV1/PortraitEntryController.cs` | 현재 `ModeScreen.Competition` → SC05 진입. 기존 browse 가능 상태를 유지. |
| `Scripts/UI/MultiplayerUiPresenter.cs` | 기존 경쟁/협동 UI와 개발 preview 소유자. 실제 운영 ModeId/opaque EntryIntent/방 lookup/receipt 소유자가 아님. |
| `Scripts/Core/MultiplayerMatchContracts.cs` | 경쟁 참가 범위 4~8, 협동 2~4, 참가자 간 동일 동물 허용. 참가자/방 preview는 DevelopmentPreviewOnly. |
| `Scripts/Core/MultiplayerLoadoutService.cs` | `IMatchGateway.RequestReady(string[])` 및 DisconnectedMatchGateway. ActionId/ModeId/정책·조회 버전/입장 receipt/결과 조회 없음. |
| `Scripts/Core/ExternalServiceConfiguration.cs` | 연결 여부 플래그만으로 인증·방 capability·서버 승인을 만들 수 없음. |
| `UI/AnimalMultiplayerPhase3/AnimalMultiplayerPhase3Entry.cs` | 기존 호스트 및 운영 동물 선택 프리팹 설치. 유지. |
| `UI/AnimalMultiplayerPhase3/AnimalMultiplayerPhase3Host.cs` | `Open(request)`와 `OpenUnconfigured()`, 기존 Cancelled/Back 반환. 운영 accepted consumer가 없음. |
| `UI/AnimalMultiplayerPhase3/AnimalMultiplayerProjectAdapter.cs` | 실제 조회 서비스 부재 오류, 보호 입장 메서드 Unavailable. 기존대로 유지. |
| `AnimalUiV2` presenter/backend | 문맥·권한·편성 검증, 확인/처리 중 변경 차단, 같은 객체 범위의 Unknown 요청 보존. 기존 구현 유지. |
| G-04 중앙 계약 | 영속 중앙 요청 보존·receipt 1회 소비·씬 파괴/앱 복귀 후 결과 조회 소유자가 없음. 새 대체 관리자를 만들지 않음. |

과거 클래스명으로 연결을 가정하지 않았으며 실제 검색 결과에 없는 서버/카탈로그/방 데이터 공급자를 만들어 넣지 않았다.

## 화면과 동선

운영 생성 메뉴: **ANIMOL > Missing UI V1 > Build Project Task 3 Multiplayer**. 현재 프로젝트에서 실제 실행하고 수정 후 재생성했다.

운영 프리팹 경로: `Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/Multiplayer/`.

| 프리팹 | 적용 위치와 내용 |
|---|---|
| `Hub.prefab` | 기존 SC05_CompetitiveHub. 일반/랭크 규칙 카드와 커스텀 입장 방식 카드, 최대 4인/역할/동물 중복 허용 안내. |
| `Custom.prefab` | 로컬 화면 `MissingUiV1_Custom`. 방 옵션 보기 / 코드 입력 분기. |
| `Create.prefab` | `MissingUiV1_Create`. 실제 허용 옵션·모드·정책·원래 편성 미조회 표시, 기존 동물 열람 링크, 생성/입장 차단. |
| `Join.prefab` | `MissingUiV1_Join`. TMP 단일 코드 입력, 실제 clipboard 붙여넣기, 조회/참가 미지원 사유 및 미조회 방 요약. |
| `Room.prefab` | 기존 SC06_MatchRoom. 미조회 참가자 4슬롯, 본인/방장/연결/Ready/3종 요약 `--`, capability 없는 동작 차단. |
| `MultiplayerArt.asset` | 기존 UtilityArt/공용 폰트와 도입된 원본 multiplayer 스프라이트 참조. |

`MissingMultiplayerEntry`는 기존 후처리와 Phase3 설치 후 새 화면을 붙이고 기존 navigation index를 갱신한다. SC05/06의 이전 자식은 삭제하지 않고 비활성 보존한다. 새 Canvas/EventSystem/데모 host는 생성하지 않는다.

실제 동선은 **기존 로비 경쟁 → SC05 → 커스텀 → 방 옵션/코드 입력 → 기존 history Back**이다. 일반/랭크 및 방 옵션의 **동물 열람**은 기존 `AnimalMultiplayerPhase3Host.OpenUnconfigured()`를 열고 이전 화면으로 돌아간다. 모드 카탈로그와 진입 의도가 없으므로 카드의 일반/랭크 표시명을 ModeId로 변환하지 않았다. 해당 열람 문맥의 ModeId/EntryIntent/PolicyRevision은 null, 원래 편성은 미조회 상태 그대로다.

새 로컬 화면 ID는 navigation 용도이며 서버 ModeId/EntryIntent가 아니다. 실제 공급자가 연결되기 전에는 진입용 문맥을 만들 수 없으므로 **실제 문맥 전달은 미완료**다. 후속 공급자는 기존 host.Open(request)에 실제 값을 전달해야 한다.

SC06은 기존 개발용 협동/경쟁 preview와 공유된다. preview 참가자가 있는 개발 경로에서는 기존 자식과 자체 정책을 유지한다. 개발용 P1~P8/Ready/Connected 예시를 새 운영 틀에 복사하지 않았다. 운영 허브에는 개발 참가자 생성 버튼이 없으며 **승인 없는 SC06 이동 버튼도 없다**. 이번 SC06 캡처는 검사가 직접 기존 화면을 열어 찍은 미조회 틀이다.

## 최신 4인 정책과 기존 코드 차이

새 운영 UI는 **최대 4인, 4슬롯**이고 8인 옵션/인원 토글/자동 충원 연출을 추가하지 않았다. 미조회 `-- / 4`의 4는 최신 화면 수용 기준이며 실제 서버 capacity 응답으로 주장하지 않는다.

다음 기존 소유자는 아직 과거 4~8 기준이다. 이번 작업의 지시대로 차이와 영향 범위를 기록하며 사용자 작업 중인 기존 소스/생성물을 덮어쓰지 않았다.

| 이관 대상 | 영향 |
|---|---|
| `MultiplayerModeRules.CompetitiveParticipants` (MultiplayerMatchContracts.cs:28) | 현재 (4,8). 경쟁 참가자 검증, CompetitiveParticipantSelectionService 생성, MultiplayerRoomPreviewState 범위의 단일 소유자. 운영 정책 확정 시 4인 규칙과 함께 변경 필요. |
| `MultiplayerUiPresenter.RefreshRoom` | 개발용 4~8 문구, 8개 참가자 라벨, 4↔8 토글. 새 운영 허브에서는 노출하지 않지만 기존 개발 코드에는 남음. |
| `Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs` | 기존 개발 화면 8슬롯/토글 생성 및 8인 유효성 확인. 전체 재생성 전에 함께 이관해야 함. |
| `Tests/EditMode/M6MultiplayerContractTests.cs` | Contains(8)=true 기대값. 최신 경쟁 정책에 맞춰 갱신 필요. |
| 기존 competitive preview 데이터/SC06 생성물 | 운영 정책과 개발 시안을 분리해 재생성·검증 필요. 협동 2~4 범위는 변경 대상 아님. |

다른 참가자가 같은 동물을 사용해도 선택을 잠그는 코드나 옵션을 추가하지 않았다. 현재 실제 다계정/4인 서버 경기는 검증하지 못했다.

## 입력·권한·거래 경계

코드는 원래 문자열을 받는 **TMP_InputField, Standard, SingleLine, characterLimit=0, validation=None**이다. 6자리 규칙, 대문자화, 방 코드 발행이나 클라이언트 유효 판정을 추가하지 않았다. 입력/붙여넣기는 조회 결과가 아니며 방 요약의 코드 `--`를 바꾸지 않는다. 빈 clipboard는 로컬 안내만 표시한다. 화면을 떠나면 입력 포커스를 해제한다.

첫 검사에서 TMP의 `shouldHideSoftKeyboard` getter가 Windows에서는 true인 점과 setter가 true를 저장하는 점을 확인했다. 현재 패키지 캐시의 TMP_InputField 소스를 읽어 생성 코드의 setter 호출을 제거하고 **직렬화된 m_HideSoftKeyboard/m_HideMobileInput=false**를 검사했다. 이는 모바일 키보드 설정 보존 검증이며 실기기 키보드 표시 성공 증거는 아니다.

현재 lookup 공급자가 없으므로 조회 중/실패/유효 방 상태를 가짜로 전이시키지 않는다. 조회·참가 버튼을 비활성화하고 사유를 표시한다. 실제 provider가 생기면 조회 중/실패/권한 변경 상태와 모바일 키보드 가림 대응을 이어서 검증해야 한다.

생성/입장/준비/시작/초대/나가기/코드 복사는 실제 계약이 없어 **비활성 + 실행 리스너 없음**이다. 계정 편성을 저장하지 않으며 신규 UI에 구매/계정/방 서비스 호출이 없다. 연결 플래그나 그림 존재만으로 활성화하지 않는다.

새 UI의 열람/화면 이동은 기존 Presenter.IsCommitting/IsMultiplayerModalOpen 및 UiModalStack을 확인한다. 화면 비활성화 시 이벤트 구독을 해제하며 중복 설치를 방지한다. 기존 adapter/presenter의 깊은 복사·권한·버전·receipt 검증/Unknown 보존을 교체하지 않았다.

**실제 Accepted/receipt가 없으므로 새 승인 응답을 만들거나 MultiplayerAccepted를 운영 대기방에 임의 연결하지 않았다.** 유효 receipt의 기존 운영 대기방 1회 전달은 여전히 미지원이다. 중앙 보존 계약이 없어서 씬 파괴/앱 재시작 후 요청 복구도 완료로 판정하지 않는다.

## 실제 실행한 검증

Unity 컴파일 및 테스트 결과 JSON은 [Docs/MissingUiV1Task3](Docs/MissingUiV1Task3)에 기록한다.

신규 `MissingMultiplayerFlowTests`는 수정 후 **8/8 통과**했다.

최종 선택 실행은 **58/58 통과**이며 전체 프로젝트 테스트 스위트를 실행한 것은 아니다. Unity 재컴파일 결과는 오류 0개다([compile.json](Docs/MissingUiV1Task3/compile.json)).

| Unity 검사 클래스 | 최종 통과 |
|---|---:|
| MissingMultiplayerFlowTests | 8/8 |
| CommercialPolishPolicyTests (EditMode) | 3/3 |
| MissingUtilityFlowTests | 7/7 |
| MissingUiStoreFlowTests | 5/5 |
| PortraitEntryFlowTests | 3/3 |
| ProductionFlowTests | 7/7 |
| MenuThemeFlowTests | 3/3 |
| UpgradePhase1FlowTests | 2/2 |
| StagePhase2FlowTests | 9/9 |
| MultiplayerPhase3FlowTests | 3/3 |
| MultiplayerPhase3TransactionTests | 8/8 |

EditMode로 표기한 3개 외에는 모두 실제 Unity PlayMode 검사다. 기존 강화·캠페인 고정 편성·로비/테마 동선을 회귀 검사했지만, 이 통과가 미지원 운영 서버 입장이나 거래의 성공을 뜻하지 않는다.

- 실제 Lobby의 Competition → 새 SC05/커스텀/방 옵션/코드 입력/Back, 기존 Phase3 열람 및 원래 빈 편성 반환.
- 코드 원문 붙여넣기, 빈 clipboard 안내, 입력 포커스/해제, 임의 길이·문자 제한 없음, Enter/조회/참가로 입장 없음.
- SC06 미조회 4슬롯, Ready/시작/초대/복사/나가기 비활성 및 직접 이벤트 호출에도 실행 없음.
- 재진입/중복 Install 후 installer 1개·신규 view 5개·기존 Phase3 host 1개, 공용 모달 중 다른 동선 차단.
- 합성 어댑터에서 확인 취소 요청 0회 → 확정 요청 1회 → 연타/뒤로/새 UI 재진입 추가 0회. Unknown 뒤 같은 객체 비활성화/복귀에도 원래 전체 요청 보존, 결과 조회 미지원으로 추가 보호 호출 0회, 대기방 이동 0회.
- 기존 협동 개발 preview의 SC06 경로와 자체 2→4 참가자 토글 유지. 이것은 온라인 협동 성공 검증이 아님.
- 두 실제 Game View에서 스크롤/하단 Back Raycast/경계, 영어 큰 글씨, 모의 Safe Area 캡처 순간 anchors 검사.
- 테스트 전후 게임 저장 파일 목록/바이트 동일. Unity 자체 Analytics/Insights 회전 로그만 검사 대상에서 제외했다. 테스트가 사용한 언어/큰 글씨 PlayerPrefs 및 clipboard 값 복원.

최초 실행은 [7/8](Docs/MissingUiV1Task3/initial-playmode.json)이었다. 잘못된 desktop 키보드 기대값 및 생성 설정을 고치고 대기방 장문 안내의 낮은 대비도 수정했다. [최초 대비 문제 캡처](Docs/MissingUiV1Task3/initial-room-low-contrast.png)를 최종 캡처와 구분해 보관한다. 수정 후 재생성한 프리팹으로 8개 검사를 다시 실행했다.

| 거래 관찰 범위 | 입장 호출 | 대기방 이동 | 의미 |
|---|---:|---:|---|
| 실제 운영의 미지원 UI/adapter | 발행 경로 없음 | 0 | 운영 서비스를 호출할 수 없는 차단 상태. 서버 요청 카운터를 측정한 것으로 주장하지 않음 |
| 신규 UI에 주입한 Unity fixture | 보호 메서드 1 | 0 | 연타/Unknown 후 요청 불변 확인. [요청 원문 및 카운터](Docs/MissingUiV1Task3/fixture-pending-unknown.json) |
| 실제 승인 receipt → 운영 대기방 | 미검증 | 미검증 | 실제 서비스/consumer/G-04 계약 없음 |

기존 Phase3 transaction fixture도 **8/8 재실행 통과**해 확인·늦은 응답·잘못된 receipt·미확인 결과 경계의 회귀를 확인했다. [fixture별 요청/승인 이벤트/이동 카운터](Docs/MissingUiV1Task3/ExistingPhase3Fixtures)를 보관했다. fixture Accepted 이벤트는 실제 서버 승인 또는 중앙 receipt 소비의 증거가 아니다.

설정 회귀 검사의 첫 실행은 6/7이었다. Unity가 자체 Insights 로그를 자동 정리하면서 게임 저장 검사에 섞인 것이 원인이었다. 기존 `MissingUtilityFlowTests`의 파일 비교에서 엔진 전용 `Unity/**/Editor/Analytics/**`, `Unity/**/Insights/**`만 제외했다. 게임 저장 파일과 PlayerPrefs 검증은 유지했으며 재실행은 **7/7 통과**했다. [초기 실패](Docs/MissingUiV1Task3/initial-utility-analytics-failure.json)도 보관한다.

프리팹의 모든 하위 컴포넌트를 검사하면서 기존 공용 `UiButtonFeedback`이 `UiPolishRuntime.cs`의 보조 클래스라 저장된 MonoScript 참조가 없어지는 문제를 확인했다. 신규 5개 프리팹에서는 해당 컴포넌트를 저장하지 않고 활성화할 때 기존 클래스를 1회 추가한다. [재생성 후 검사](Docs/MissingUiV1Task3/native-assets.json)에서 5개 모두 누락 스크립트 0개, 추가 Canvas/EventSystem 0개를 확인했다. **기존 작업 1의 Store.prefab에는 4개, 작업 2의 Settings.prefab에는 7개의 누락 참조가 남아 있다.** 이번에 이전 생성물을 일괄 덮어쓰지 않았으며, 후속으로 공용 클래스의 Unity 직렬화 구조를 정리하고 해당 프리팹을 제한적으로 재생성해야 한다. 기존 화면 회귀 통과가 이 직렬화 문제의 해결을 뜻하지 않는다.

## Game View 증거

[최종 캡처 40장](Docs/MissingUiV1Task3/Captures): 5화면 × 2해상도 × 일반 한국어/모의 Safe Area 영어 큰 글씨 × 스크롤 상·하단.

- 해상도: **1080×1920, 1080×2400**.
- 모의 Safe Area: 좌우 48px, 상단 120px, 하단 96px. Editor 포커스 복귀에도 테스트 전용 probe가 inset을 유지하고 캡처 시 검증한다.
- [경쟁 허브](Docs/MissingUiV1Task3/Captures/1920_full_korean_Hub_top.png), [커스텀 분기](Docs/MissingUiV1Task3/Captures/2400_safe_english_large_Custom_bottom.png), [방 옵션](Docs/MissingUiV1Task3/Captures/1920_safe_english_large_Create_bottom.png), [코드 입력](Docs/MissingUiV1Task3/Captures/1920_safe_english_large_Join_top.png), [4슬롯/권한 안내](Docs/MissingUiV1Task3/Captures/2400_full_korean_Room_bottom.png).

대표 캡처를 직접 열어 장문/큰 글씨/스크롤/고정 Back과 대비를 확인했다. 긴 코드는 단일 입력 영역 내 가로 스크롤 대상이다. `TEST ONLY` 코드 문자열은 검사에서만 넣었고 운영 prefab의 입력 기본값은 빈 문자열이다.

## 수용기준과 미완료 사항

| 기준 | 판정 | 근거/후속 |
|---|---|---|
| MULTI-01 | partial | 새 운영 UI 최대4/4슬롯. 기존 Core/DEV 4~8 이관과 서버 정책 확인 필요 |
| MULTI-02 | partial | 일반/랭크 카드와 커스텀 입장 방식 분리. 실제 ModeId/intent 공급자 필요 |
| MULTI-03 | pass | 실제 SC05 분기/반환 검사 |
| MULTI-04 | pass | 허용 옵션 미조회 `--`, 미정 방 이름/공개/인원/비밀번호/투표 등 편집 UI 없음 |
| MULTI-05 | partial | 실제 TMP 입력/붙여넣기 검사. 서버 코드 정책·조회 상태·모바일 키보드 실기기 검증 필요 |
| MULTI-06 | blocked | 실제 room lookup/초대 capability 없음. 정보 `--`, 실행 차단 |
| MULTI-07 | partial | 기존 Phase3 열람/반환 재사용. 실제 문맥·정책·권한 전달은 공급자 부재로 미연결 |
| MULTI-08 | pass (현재 차단 경계) | 새 UI에서 별도 생성/참가/저장/대기방 이동 없음. fixture 확인 취소 0회 |
| MULTI-09~11 | blocked | 실제 입장 서비스·승인 receipt·중앙 1회 소비·운영 대기방 consumer 없음 |
| MULTI-12 | partial | 4슬롯 미조회 틀·미지원 실행 차단. 실제 참가자/본인/방장/ready 상태 조회 미연결 |
| MULTI-13 | partial | 신규 중복 금지/선점 없음. 실제 다계정 동일 동물 경쟁 시나리오 미검증 |
| MULTI-14 | partial | 동일 객체의 기존 Unknown 보존과 재진입 차단 fixture 검사. 중앙 결과 조회/앱 복귀 미지원 |

Android/iOS 빌드·실제 모바일 키보드/노치/OS 뒤로/백그라운드 복귀, 실제 4인 방/일반·랭크 규칙/초대/Ready/재접속/나가기 검증은 실행하지 않았다. 독립 HTML/패키지 모델 검사를 실제 Unity·서버 검증으로 대체하지 않았다. 새 매칭 서버·방 목록·친구/채팅/팀·새 동물 선택 화면을 추가하지 않았다.

## 변경 파일·보존·후속 연결

신규 코드/테스트/전용 assembly는 `Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer`, 프리팹/Art는 위 전용 Resources 하위 경로에 관리한다. 자세한 파일 목록은 [changed-files.txt](Docs/MissingUiV1Task3/changed-files.txt), 재생성 안내는 [PROJECT_TASK3_INTEGRATION.md](Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/PROJECT_TASK3_INTEGRATION.md)를 참조한다.

기존 Assets/ANIMOL·ProjectSettings·Docs의 5,370개 파일을 작업 시작 상태로 해시 기록했다. 테스트가 만드는 동적 폰트 캐시·NamedArt registry 및 이전 작업 캡처는 시작 상태로 복원했다. **5,369개는 시작 해시와 동일**하며, 기존 파일의 의도된 변경은 위 `MissingUtilityFlowTests.cs`의 엔진 로그 제외 수정 1개뿐이다([preservation.json](Docs/MissingUiV1Task3/preservation.json)). 기존 동물 PNG/IdMap/카탈로그/운영 어댑터·프리팹, v7 배경·토끼·발판, 작업 1·2 런타임 코드를 보존했다. 신규 캡처의 크기와 SHA-256은 [captures.json](Docs/MissingUiV1Task3/captures.json)에 기록했다.

필수 후속은 운영 모드·진입 의도 공급자, 4인 정책 소유자 이관, 실제 계정 권한/성장·방 조회/입장·멱등 결과 조회 서비스, 완전한 receipt와 기존 운영 대기방 소비자, G-04 중앙 보존/복귀 계약이다. 이들이 없는 상태에서 화면만으로 입장을 활성화하지 않는다.
