# NET02 TASK 01 — 현 프로젝트 조사·충돌 확인

기준일: 2026-10-05. 조사 시작 HEAD: `2792361`. 사용자 요청 범위는 TASK 01부터이며, 이번 작업은 조사와 후속 연결용 패키지 배치까지다. TASK 02~06의 UI/HTTP 실연결 완료를 뜻하지 않는다.

정본: [패키지 적용 지시](../Tools/ANIMOLNet02/Docs/NET02_PROJECT_APPLY.md), [2026-10-05 인수인계](../Tools/ANIMOLNet02/Docs/ANIMOL_UI_IMPLEMENTATION_AND_SERVER_HANDOFF.md). 아래는 문서의 가정 대신 현재 소스·프리팹·asset을 대조한 결과다.

## 1. 충돌과 이번 최소 변경

- 시작 시 `git status --short` 확인. Core/UI/Editor/테스트/프리팹/씬/ProjectSettings 등에 기존 변경이 있고 ZIP과 많은 미추적 파일이 있었다. 기존 변경·미추적 파일 1,411개의 바이트 해시를 로컬 기록해 작업 종료 시 대조한다.
- ZIP의 40개 파일을 목적지별로 검사했으며 같은 이름의 기존 파일은 0개였다. Assets의 11개 파일은 `Assets/ANIMOL/NetUiDev/`, 나머지 29개는 `Tools/ANIMOLNet02/`에 배치했다. 후속 문서 진입점은 `Docs/NET02_PROJECT_APPLY.md`다.
- 프로젝트 배치에 필요한 유일한 패키지 소스 조정은 `Tools/validate_package.py`의 Assets 탐색 경로와 Windows UTF-8 읽기다. 로컬 DB/자격증명/캐시 제외용 패키지 `.gitignore`와 [배치 설명](../Tools/ANIMOLNet02/PROJECT_IMPORT.md)을 추가했다.
- 새 Canvas/EventSystem/씬/플레이어/가짜 참가자/서비스 GameObject를 생성하지 않았다. 기존 UI, 프리팹, 종 매핑, 서비스 연결 플래그는 수정하지 않았다. Unity가 새 소스에 생성한 `.meta`만 패키지에 포함한다.
- 원본 `MANIFEST.json`, `Docs/Evidence/`는 제작 당시 증거로 보존한다. 현재 검증과 구분하며 변경한 validator의 해시는 원본과 다르다.

## 2. 실제 진입·구독·복귀 경로

### Missing Multiplayer

소스: `Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/`.

| 타입/정확한 멤버 | 현재 호출자와 동작 | 후속 연결 시 고려사항 |
|---|---|---|
| `MissingMultiplayerEntry.Install(Scene scene, LoadSceneMode mode)` (`public static void`) | `BeforeSceneLoad`에서 등록한 `SceneManager.sceneLoaded`; Lobby의 `UiNavigationService`에 `MultiplayerUiPresenter`가 있을 때 한 번 추가 | 별도 씬/Canvas 생성 경로가 아님 |
| `MissingMultiplayerEntry.Start()` (`private IEnumerator`) | 3프레임 후 기존 `SafeArea/ScreenHost`에 Resources의 Hub/Custom/Create/Join/Room 삽입; `RebuildIndex()` 후 `ScreenChanged += OnScreenChanged` | `OnDestroy`에서 해제. 기존 컨테이너 자식들을 비활성화하며 Room은 원래 active 상태를 보관 |
| `OnScreenChanged(string id)` | SC06일 때 `PreviewParticipantCount > 0`이면 레거시 자식 표시, 새 Room 숨김 | 이전 DEV 미리보기 상태가 남으면 NET02 Room도 숨겨질 수 있음. TASK 05에서 서버 방 표시 우선순위를 명시해야 함 |
| `MissingMultiplayerView.ScreenId(MissingMultiplayerScreen kind)` (`public static string`) | Hub=`SC05_CompetitiveHub`, Room=`SC06_MatchRoom`, 나머지=`MissingUiV1_Custom/Create/Join` | 화면 ID는 실제 ModeId/EntryIntent가 아님 |
| `Host`, `CanNavigate` (`public` properties) | 상위 navigation의 `AnimalMultiplayerPhase3Entry.Host` 사용. 활성 화면/Host 존재/공통 모달 0/거래·Phase3 모달 없음 확인 | 중앙 Pending/Unknown은 아직 검사하지 않음 |
| `OnEnable()` / `OnDisable()` | Back→`navigation.Back()`, Custom/Create/Join→`Navigate(kind)`, NormalBrowse/RankedBrowse/Browse→`BrowseOnly()`, Paste→`PasteCode()` 등록/모두 제거 | Entry/Lookup/Ready/Start/Invite/Leave/Copy에는 실행 리스너가 없고 현재 비활성 |
| `BrowseOnly()` (`private void`) | `Host.OpenUnconfigured()`만 호출 | 일반·랭크 모두 같은 미설정 열람. 서버 문맥 공급자가 아직 없음 |
| `MissingRoomCodeInput.Build(Transform parent, MissingMultiplayerArt art)` (`public static MissingRoomCodeInput`) | `BuildJoin`이 호출. 단일 `TMP_InputField Field` 소유 | SingleLine/Standard, characterLimit=0, validation=None. `onSubmit`/`onValueChanged`에 조회 리스너 없음 |
| `PasteCode()` (`private void`) | `GUIUtility.systemCopyBuffer`→`Code.Field.text`, 입력 포커스와 안내 변경 | 입력값을 조회 결과/receipt로 취급하지 않음. `OnDisable`에서 필드 비활성화 |

현재 동선: 로비 모드 선택 → SC05 → Custom → Create/Join; SC05/생성 화면 동물 열람 → Phase3 → 취소 시 직전 화면. 서버 lookup/create/join/Ready는 호출하지 않는다.

### Phase3 host와 프로젝트 어댑터

소스: `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/`.

- `AnimalMultiplayerPhase3Entry`는 실행 순서 1100, Lobby에서 2프레임 후 `Resources/ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect`를 기존 ScreenHost에 추가한다. Missing entry는 1102/3프레임이다.
- Entry는 레거시 `CompetitiveMatchButton`, `RankedMatchButton`, `CompetitivePrivateButton`의 `onClick`을 저장하고 `Host.OpenUnconfigured()`로 교체한다. 파괴 시 원래 이벤트 복원. `PortraitEntryController.CompetitiveBrowsingAvailable=true`도 설정하며 파괴 시 해제한다. 새 Missing Hub가 설치되면 레거시 Hub 자식은 숨겨진다.
- `AnimalMultiplayerPhase3Host.Open(MultiplayerSelectionRequest request)`는 `public bool`. 문맥 복사, 기존 navigation으로 화면 열기, `Presenter.OpenMultiplayer(context)` 순서다. null/readonly fixture/거래 중/멀티 모달 중에는 false. 실제 서비스 검증은 backend/rules의 책임이다.
- `OpenUnconfigured()`도 `public bool`. ModeId/EntryIntent/PolicyRevision 없음, 빈 편성, AllowedAnimalIds=null, 3역할과 대표 역할 -1을 전달한다. 열람은 가능하고 입장은 차단된다.
- host `OnEnable`에서 `Presenter.Cancelled += Return`, `OnDisable`에서 제거. `private void Return(AnimalUiMode mode, AnimalLoadout original)`은 `LastReturnedLoadout`과 `event Action<AnimalLoadout> Returned`에 복사본을 전달하고 `navigation.Back()` 실패 시 저장된 `returnScreen`으로 이동한다.
- 운영 코드에서 `MultiplayerAccepted +=` 또는 host `Returned +=` 구독자는 발견되지 않았다. 승인 후 SC06으로 이동하는 소비자는 아직 없다. 테스트 구독은 별개다.
- `AnimalMultiplayerProjectAdapter : AnimalMultiplayerBackendAdapterBase`는 sealed. `public AnimalMultiplayerIdMap IdMap`, `public string DescribeContext(MultiplayerSelectionRequest context)`가 있다.
- 정확한 override는 `protected override Task<AnimalUiSnapshot> ReadMultiplayerSnapshotAsync(MultiplayerSelectionRequest context, CancellationToken cancellationToken)`와 `protected override Task<AnimalUiCommitResult> SubmitRoomEntryAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken)`다.
- Read는 문맥/카탈로그/IdMap을 확인한 뒤 미연결 예외 반환, Submit은 `CommitStatus.Unavailable` 반환이다. 실제 방 생성/Ready는 없다.

## 3. 공용 계약과 중복 방지 책임

소스: `Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiContracts.cs`, `AnimalUiPresenter.cs`, `AnimalUiRules.cs`, `AnimalMultiplayerBackendAdapterBase.cs`.

| 계약 | 실제 필드/행동 |
|---|---|
| `MultiplayerSelectionRequest` | `ModeId`, `DisplayName`, `AllowedAnimalIds`, `InitialLoadout`, `Requirements`, `EntryIntent`; `Clone()`은 배열/편성을 복사 |
| `SelectionRequirements` | `AnimalRole[] RequiredRoles`, `AnimalRole RepresentativeRole`, `string PolicyRevision` |
| `SelectionCommitRequest` | `ActionId`, `ContextId`, `SnapshotRevision`, `PolicyRevision`, `Loadout`, `EntryIntent`; ContextId는 ModeId |
| `AnimalUiSnapshot` | `Revision`, `ContextId`, `PolicyRevision`, `EntryIntent`, nullable `CoinBalance`, `List<AnimalProgress> Animals` |
| `AnimalProgress` | AnimalId, Unlocked, Implemented, HasContextPermission, CanUseInContext; 레벨 기본 -1, nullable MasteryBalance, 설명/사유 |
| `AnimalUiCommitResult` | Status/Message/Snapshot, AcceptanceToken/AcceptedContextId/AcceptedPolicyRevision/AcceptedEntryIntent/AcceptedLoadout. **RoomId/RoomCode/ActionId 결과 필드는 없음** |
| `AnimalLoadout` | Ground/Special/Air 문자열; UI catalog ID(예: Rabbit)를 사용. 서버 종 ID(RABBIT)와 명시적 왕복 변환 필요 |

`AnimalUiPresenter`의 공개 포트는 `bool OpenMultiplayer(MultiplayerSelectionRequest)`, `void SetBackend(AnimalUiBackendBehaviour)`, `void Refresh()`, `void Back()`, `event Action<AnimalUiCommitResult> MultiplayerAccepted`, `event Action<AnimalUiMode, AnimalLoadout> Cancelled`다.

`Awake`에서 View 버튼을 한 번 구독한다. Primary는 전체 편성 확인 모달을 열고, 확인 콜백 `CommitSelection(false)`에서 Guid ActionId/현재 revision/intent/편성을 고정한 뒤 `Backend.SubmitMultiplayerAsync(..., CancellationToken.None)` 한 번 호출한다. 취소는 제출하지 않는다. `_pending`/`_uncertain` 중 재진입·편성변경·뒤로·backend 교체를 막으며 결과 재확인은 `CommitSelection(true)`로 같은 요청을 사용한다. Accepted는 rules를 통과한 뒤 `_selectionCompleted=true`와 이벤트만 발생시키고 화면을 이동시키지 않는다.

`OnDisable`은 epoch를 증가시키고 읽기를 취소한다. 진행 중 요청은 uncertain으로 남긴다. 늦은 완료는 epoch/활성 검사로 UI에 적용되지 않는다. 이 보호는 객체 수명 범위이며 앱 재시작 저장이 아니다. 확인 모달은 **View.Modal 자체 상태**이므로 공통 `UiModalStack.Count`만으로 확인 여부를 알 수 없다.

base의 `ReadSnapshotAsync(AnimalUiReadRequest, CancellationToken)`/`SubmitMultiplayerAsync(MultiplayerSelectionRequest, SelectionCommitRequest, CancellationToken)`는 sealed override다. 보호된 두 메서드로 연결한다. `protected virtual bool SupportsRoomEntryReconciliation => false`가 실제 확장점이며, 별도 결과조회 override는 없다. 연결 시 같은 protected Submit 경로에서 중앙 pending 결과를 구분해 조회해야 한다. `_unresolved`, `_completed`, `_dispatching`은 인스턴스 메모리 보호이므로 서버/영속 중앙 journal을 대체하지 않는다.

rules는 문맥·3역할·대표 역할·revision·허용 풀을 검사한다. `AllowedAnimalIds=null`은 추가 클라이언트 풀 제한 없음, 빈 배열은 허용 없음이다. 선택한 각 종의 Implemented/HasContextPermission/CanUseInContext가 필요하고 Unlocked만으로 입장권을 만들지 않는다. 다른 참가자의 동일종을 잠그지 않는다. Accepted는 토큰 존재와 ContextId/PolicyRevision/EntryIntent/3종 일치가 필수다. 현재 rules는 입장에 레벨/효과 설명을 요구하지 않으며 공급 없는 수치는 -1/`--`로 유지해야 한다.

## 4. SC06 컨테이너와 navigation

- 기존 `Assets/ANIMOL/Prefabs/UI/SC06_MatchRoom.prefab`은 레거시 DEV 컨테이너다. `RoomParticipant_01`~`08`, 카운트 토글, SC07 선택 경로가 남아 있다.
- 새 `Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/Multiplayer/Room.prefab`은 `MissingMultiplayerView.Screen=Room(4)`, `ParticipantSlots` 정확히 4개다. 런타임에 `SafeArea/ScreenHost/SC06_MatchRoom/MissingMultiplayerRoom`으로 삽입된다. 기존 CanvasScaler/SafeArea/EventSystem을 재사용한다.
- `UiNavigationService`의 `public bool Navigate(string screenId, bool rememberCurrent = true)`, `public bool Back()`, `public void RebuildIndex()`, `event Action<string> ScreenChanged`를 사용한다. Navigate는 현재 화면을 history에 push하고 화면 활성화 뒤 이벤트를 발생시킨다. Back은 history를 pop한다.
- `UiModalStack`의 `public bool Push(string modalName)`, `public bool Pop()`, `public int Count`는 기존 ModalHost를 관리한다. 서버나 탈퇴 승인 책임은 없다.
- `MultiplayerUiPresenter`는 OnEnable에서 레거시 버튼을 구독하고 OnDisable에서 자신의 리스너를 해제한다. `private void OpenPreview(MultiplayerPreviewDefinition preview, bool ranked)`만 현재 레거시 SC06 입장 상태를 만든다. `OpenAnimalSelect()`는 SC07로, `LeavePreview()`는 모달 종료 후 SC01로 이동한다. NET02 승인·나가기 대체 경로로 재사용하면 안 된다.
- 새 Room의 Back은 현재 단순 `navigation.Back()`. 활성 서버 방 도입 시 승인된 leave 이전에 로컬 뒤로만 진행하지 않게 TASK 05에서 제어해야 한다. Phase3가 남는 history와 원래 Hub/Create/Join 복귀도 명시적으로 처리해야 한다.

## 5. 모드·종·정책 소유자와 미확정 데이터

| 항목 | 현재 확인 | 판정 |
|---|---|---|
| 실제 종 식별 | `AnimalMultiplayerIdMap.asset`의 15행 중 Rabbit만 `Data/Campaign/Animals/RABBIT.asset` 참조. `ToArtId(string projectId)`/`ToProjectAnimal(string artId)`는 유일한 왕복 매핑만 허용하고 DEV_ 거절 | Rabbit↔RABBIT만 확인 |
| 실제 구현 | RABBIT도 placeholderVisual=1, productionActiveAbilityConfigured=0, productionPassiveAbilityConfigured=0 | 매핑 존재를 전체 능력 구현/사용권으로 승격 금지 |
| 15종 catalog | `UI/AnimalMultiplayerPhase3/AnimalCatalog.asset`에 Ground 5, Special 5, Air 5 아트 ID/역할 | Special/Air 실제 종 공급 없어서 운영 3종 편성 **blocked** |
| 모드 소유자 | Core `GameModeKind { Campaign, Competitive, Ranked, Coop, PrivateLobby }` 및 DEV `MultiplayerPreviewDefinition`; 운영 ModeId/표시명/policy revision/opaque intent 공급자는 발견 못함 | TASK 03 데이터 **blocked**. enum/라벨/화면명으로 ID 합성 금지 |
| 성장 구분 | `MultiplayerRoomPreviewState`는 ranked 여부로 `OwnedProgress` / `RankedMaximumPreset` 선택 | 정책 표현만 존재; 실계정 레벨/최대치 공급 아님 |
| 계정·편성·권한 | 저장된 운영 초기 3종, 허용 풀, 대표 역할, snapshot revision, DEV 계정 credential 공급 없음 | 실제 운영 진입 **blocked** |
| 경쟁 인원 | Core `MultiplayerModeRules.CompetitiveParticipants=(4,8)` vs 새 Missing Room/NET02 서버 최대4 | TASK 02에서 이관. 공개 최소4와 custom DEV config 최소값은 다른 책임 |
| 협동 | `CoopParticipants=(2,4)`, 별도 편성/중복 규칙 | 변경하지 않음 |

**역할 변환 주의:** `AnimalRole`은 Ground=0/Special=1/Air=2, Core `MultiplayerAnimalRole`은 Ground=0/Air=1/Special=2다. 정수 캐스팅/배열 순서로 서로 변환하지 않는다. NET02 DTO의 문자열 Ground/Special/Air를 명시적으로 대조한다.

현재 카탈로그 Ground: Rabbit/Wolf/WhiteFerret/MountainGoat/Otter, Special: DreamFox/StarCat/MirrorDeer/DreamMole/ClockMoth, Air: Swallow/Owl/FlyingSquirrel/Hummingbird/Bat. TEST_*와 DEV_*는 운영 매핑에 추가하지 않았다. 빈 서버 config는 Animals/Modes/Accounts 모두 빈 배열이다. 데이터 차단과 별개로 서버/클라이언트 기반 검증은 가능하다.

## 6. Bootstrap·빌드·출시 차단 경계

- `BootstrapPresenter.Continue()` (`public void`) → `SceneManager.LoadScene("Lobby")`. 현재 네트워크 중앙 관리자 설치는 없음.
- enabled 빌드 씬 13개: Bootstrap, Lobby, Gameplay, Results, MapDevTest, Campaign/ThemeRuntime_T01~T05, ObjectLab, ThemePlatformLab, MoonObjectLab. `MapAuthoringWorkspace`는 목록에 없음.
- `DevelopmentBuildScenePolicy.GetPlayerScenePaths(bool developmentBuild, EditorBuildSettingsScene[] scenes = null)` 및 빌드 전후 filter가 DEV 실험 씬 포함/제외 책임을 가진다. NET02 build 메뉴는 기존 enabled 씬을 사용하고 Bootstrap 선두/Lobby 존재를 확인한다. 이번에 씬 순서나 enabled 값은 바꾸지 않았다.
- `ExternalServiceConfiguration.asset`의 account/match/purchase/rewardedAd는 모두 0. `MultiplayerUiPresenter.OperationalReadyEnabled`는 MatchServerConnected만 읽고, Portrait의 `OnlineModesAvailable`이 이를 사용한다. `CompetitiveBrowsingAvailable`은 열람 허용일 뿐 온라인 승인 아님.
- 별도 `AccountAccessSnapshot.CanUseOnline`은 `IdentityState==Linked && AccountServerConnected`, `CanPurchase`는 여기에 PurchaseSdkConnected를 요구한다. 현재 GrowthEconomy UI는 GuestDisconnected로 시작한다. NET02 세션으로 Linked를 만들지 않는다.
- `IMatchGateway`는 `MatchRequestResult RequestReady(string[] selectedAnimalIds)`만 있고 `DisconnectedMatchGateway`가 실패를 반환한다. ModeId/ActionId/입장 receipt API가 아니므로 Phase3 입장에 전용할 수 없다.
- NET02 runtime asmdef는 `defineConstraints: ["ANIMOL_NET_UI_DEV"]`, 테스트는 여기에 `UNITY_INCLUDE_TESTS`, Editor 메뉴 asmdef는 Editor 한정/define 제약 없음. namespace는 **`Animol.NetUiDev`**다(기존 ANIMOL과 대소문자 다름).
- `NetUiHttpClient.EnsureDevelopment()`는 Editor 또는 Debug.isDebugBuild에서만 허용한다. coordinator Awake도 비개발 player에서 비활성화한다. `link.xml`은 ignoreIfMissing=1. 프로젝트 측 참조/using/바인딩도 같은 define으로 격리해야 한다.
- 현재 기존 asmdef에는 NET02 참조가 없다. TASK 04에서 reference를 추가하거나 전용 bridge assembly를 구성할 때 define 없는 기존 disconnected 컴파일을 다시 확인해야 한다.

## 7. NET02 중앙 관리 연결 지점과 충돌 위험

패키지 `NetUiDevCoordinator`는 전용 빈 루트 서비스 객체(Transform+자신만, 자식 없음)를 요구하고 `DontDestroyOnLoad`를 호출한다. 기존 Canvas 루트에 부착하면 guard에 걸린다. Bootstrap에서 한 번 설치하고 UI 수명과 분리할 대상이다.

실제 API:

```csharp
void ConfigureConnection(string url, string devAccountId, NetUiIdMapEntry[] projectMap)
Task<NetUiCatalog> ConnectAsync(string devAccessKey)
Task<NetUiCatalog> GetCatalogAsync()
Task<NetUiLookupResponse> LookupRoomAsync(string userInput)
Task<NetUiContextSnapshot> ReadContextAsync(string modeId, string opaqueEntryIntent, string confirmedRoomCode = "")
NetUiEntryRequest PrepareEntry(string actionId, NetUiLoadout actualLoadout, NetUiOptionValue[] confirmedOptions = null)
Task<NetUiEntryResult> SubmitEntryAsync(NetUiEntryRequest confirmedRequest)
Task<NetUiEntryResult> ResolvePendingAsync()
Task<NetUiEntryResult> ResubmitUnknownAsync()
bool TryDeliverAccepted(Func<NetUiEntryResult, NetUiArtLoadout, bool> existingSc06Consumer)
Task<NetUiRoomState> PollRoomAsync()
Task<NetUiRoomState> SetReadyAsync(bool ready)
Task<NetUiRoomState> StartRoomAsync()
Task LeaveRoomAsync()
void BeginRoomPolling()
void StopRoomPolling()
void ReplayCurrentState()
```

이벤트는 `Action<string,string> OperationStateChanged`, `Action<NetUiRoomState> RoomStateChanged`, `Action AcceptedReceiptReady`다. 프로젝트 측 구독자는 아직 없으며 Coordinator는 화면을 이동시키지 않는다.

- journal은 endpoint/DEV account/client slot 단위의 PlayerPrefs 저장이다. 요청 전체를 송신 전에 저장하고 Unknown/Accepted 미탈퇴 동안 신규 입장을 차단한다. 운영 인증·암호화 저장·동물 강화/G-04 전체 구현은 아니다.
- `TryDeliverAccepted`는 room 조회와 receipt 검증을 요구하며 소비자가 true를 반환한 뒤 소비를 저장한다. 기존 SC06 소비자가 없으므로 얇은 바인더를 새로 연결해야 한다. `MultiplayerAccepted`와 central 이벤트 양쪽에서 Navigate하지 않는다.
- central 이벤트는 Submit 완료 전에 통지될 수 있다. 여기서 즉시 화면을 숨기면 Presenter의 epoch/active 검사가 완료 처리를 버린다. TASK 04/05에서 Presenter 결과 반환과 중앙 소비 순서를 하나로 정해야 한다.
- `AnimalUiCommitResult`에 RoomId가 없으므로 NET02의 실제 receipt/RoomState를 중앙에 보존한다. UI의 AcceptedContextId에 RoomId를 넣지 않는다.
- 기존 권한·성장·policy/ModeId 공급 부재를 변환 코드로 채우지 않는다. 서버 lookup 응답의 실제 mode/policy를 대조해야 한다.
- 새 Room의 수명·표시·Back/leave·DEV preview 잔존·poll 구독 해제를 TASK 05에서 함께 처리한다. Started는 대기방 시작 승인 표시까지이며 Gameplay 로더에 보내지 않는다.

## 8. 후속 수정 대상

| TASK | 최소 후보와 목적 |
|---|---|
| 02 | `Scripts/Core/MultiplayerMatchContracts.cs`, `Scripts/UI/MultiplayerUiPresenter.cs`, `Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs`, `Tests/EditMode/M6MultiplayerContractTests.cs`. 8인 생성/렌더링/토글/검사/증거 문자열을 4인으로 이관. 레거시 SC06과 실제 사용 DEV 생성물은 제한적 재생성 검토 |
| 03 | `MissingMultiplayerView.cs`와 전용 DEV 문맥 공급 bridge, `AnimalMultiplayerPhase3Host.cs`의 필요한 부분. IdMap/Catalog/server local config는 실제 데이터 공급 후에만 보완 |
| 04 | `AnimalMultiplayerProjectAdapter.cs`, DEV bridge/Bootstrap installer, 필요한 asmdef, 중앙 관리 검증과 계수. base의 기존 sealed API와 ActionId 유지 |
| 05 | `MissingMultiplayerEntry.cs`, `MissingMultiplayerView.cs` 또는 전용 Room binder, host/nav와 단일 소비 계약. Room.prefab은 필요할 때만 제한적으로 생성 |
| 06 | 새 DEV 통합 테스트, 기존 Phase3/네비게이션/포트레이트 검사, 기존 시작 씬으로 Development 빌드 및 실기기 결과 |

위 Core/UI/Editor/테스트/SC06 다수에는 시작 전 미커밋 변경이 있다. TASK 02 이후에도 파일 전체를 되돌리거나 일괄 staging하지 않는다. 이번 커밋 대상은 신규 NET02 Assets/Tools, 안내 문서, 본 조사서와 검증 요약뿐이다.

## 9. 검증 결과

Unity 6000.3.8f1, Python 3.14.5. [기계 판독 검증 요약](NET02Task01/validation.json)에 실제 테스트 묶음·결과·원본 XML 해시와 보존 결과를 기록했다. 전체 로그/XML은 저장소의 `Logs/NET02Task01/`에 있으며 임시 로그는 커밋하지 않는다.

| 검사 | 결과와 증거 |
|---|---|
| 패키지 정적 검사 | PASS, C# 7개/asmdef 3개; 원본 대비 validator 외 39개 파일 동일 |
| 서버 HTTP/SQLite | 25/25 PASS, `server-tests.txt`. 빈 config 차단/영속 멱등 처리/동시 5번째 거절/Ready/방장 시작/탈퇴 등 fixture 범위 |
| DEV define 활성 컴파일 + EditMode | 46/46 PASS, `dev-editmode.xml`: NET02 18 + Phase3 어댑터 25 + CommercialPolishPolicyTests 3 |
| DEV PlayMode, 캡처 없는 검사 | 11/11 PASS, `dev-playmode.xml`: Missing UI 6 + Phase3 거래 5. 허브/복귀/붙여넣기/미설정 4슬롯/중복 설치/Unknown/협동 분리/거절·늦은 결과·권한 검증 |
| define 제거 후 컴파일 + EditMode | 28/28 PASS, `release-editmode.xml`: Phase3 어댑터 25 + 연출 정책 3. Editor 컴파일 검증이며 release Player/IL2CPP 빌드 결과는 아님 |
| 기존 변경 보존 | 기존 변경·미추적 1,411개 파일의 바이트 동일. ProjectSettings도 시작 전 바이트로 복원. 테스트가 새로 쓴 기존 fixture 증거와 폰트 줄바꿈만 되돌림 |

첫 EditMode 실행의 `-extraScriptingDefines=...`는 asmdef 활성화에 반영되지 않아 28개만 실행됐다. 이를 DEV 검증으로 계산하지 않았으며 실제 `NetUiDevBuild.Enable()` 후 46개 실행을 확인했다. 검증 후 `Disable()`과 검사된 임시 define 직렬화 차이 복원으로 기본 미활성 상태를 유지했다. 위 반복 실행 수를 고유 테스트 수로 합산하지 않는다.

초기 PlayMode 전체 선택은 캡처 검사의 `WaitForEndOfFrame`에서 배치 실행이 진행되지 않아 이번 작업이 시작한 프로세스만 종료했다. 그 실행은 통과로 보고하지 않는다. 기존 테스트 코드를 바꾸지 않고 캡처 없는 11개를 골라 다시 실행했다. 화면 캡처/기기 검사는 미검증이다.

기존 utility/store 프리팹의 missing script 경고가 관측됐으며 이번 패키지 컴파일 오류는 없었다. Python 3.14 서버 테스트에서 HTTPError 응답 객체 정리에 관한 ResourceWarning이 있었지만 25개 모두 통과했다. 두 경고를 새 UI 연결 완료나 기존 결함 해결로 해석하지 않는다.

실제 종/모드/권한 공급이 없어 운영 편성 입장은 blocked다. UI→실제 HTTP→SC06 연결, 앱 재시작 통합, Windows/Android 빌드, LAN 실기기 검증은 후속 TASK 범위로 남는다. TASK 01의 조사·충돌 없는 배치·기반 검증은 완료했고 다음은 TASK 02 경쟁 최대4 정책 이관이다.
