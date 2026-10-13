# ANIMOL 동물 강화 1단계 · 작업 1 결과

작업일: 2026-10-04. 작업 1의 화면 가져오기 및 기존 강화 허브 진입/반환 연결을 적용했다. 실제 성장 조회·견적·거래 어댑터는 연결하지 않았다. 현재 운영 화면은 15종 열람 전용이다.

## 확인한 현재 프로젝트

- 저장소 `AGENTS.md`를 확인했다. 작업 전 `git status --short`에 다수의 기존 변경이 있었다. 기존 소스, 계정 성장 화면, 운영 씬, v7 로비 에셋을 이번 커밋에 포함하지 않는다.
- Unity **6000.3.8f1 (1c7db571dde0)**. 같은 버전 Editor를 실행하여 가져오기, 메뉴, 컴파일, EditMode/PlayMode 검사를 수행했다.
- uGUI 2.0.0, 기존 Input Manager(`activeInputHandler: 0`), 운영 `EventSystem` + `StandaloneInputModule`을 유지한다. 새 입력 패키지를 설치하지 않았다.
- 기존 UI 생성: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.*.cs`. 현 운영 계층에는 `PortraitEntryController`, `MenuHierarchyBuilder` / `MainUiV7Builder`, `ProductionBuilder` / `ProductionController`가 추가되어 있다.
- 운영 Canvas는 1080×1920, ScaleWithScreenSize, width match 0이며 `SafeArea/ScreenHost` 아래 화면을 `UiNavigationService`가 전환한다. `SafeAreaLayout`을 그대로 사용한다.
- 운영 한글 글꼴은 `Typography/PixelTypographyProfile.asset`와 `PixelTextBridge`다. 운영 프리팹에 이 프로필을 연결했다. 패키지의 uGUI Text 참조는 유지하고 기존 TMP 픽셀 글꼴로 표시한다. 생성 데모에는 패키지 NotoSansKR가 남아 있다.
- 패키지 루트/Docs의 `README_PHASE1.md`, `Docs/UNITY_APPLY_PHASE1.md`, `Unity/README_PHASE1_UNITY.md`, `Unity/README_UNITY_INTEGRATION.md` 및 생성·런타임 소스를 읽었다. 기존 `Assets/ANIMOL/AnimalUiV2`는 없었다.

## 가져온 파일과 생성 결과

| 경로 | 내용 |
|---|---|
| `Assets/ANIMOL/AnimalUiV2/Art` | 제공 배경·프레임·아이콘·폰트와 원본 초상 15개 |
| `Assets/ANIMOL/AnimalUiV2/Runtime` | 공용 v2 계약, 카탈로그, presenter, view, 읽기 전용 데모 컴포넌트 |
| `Assets/ANIMOL/AnimalUiV2/Editor` | 제공 생성/검증 메뉴와 이 모듈에만 적용되는 importer |
| `Assets/ANIMOL/AnimalUiV2/Generated/AnimalCatalogV2.asset` | 패키지 생성 카탈로그 |
| `Assets/ANIMOL/AnimalUiV2/Generated/CharacterUpgrade.prefab` | 패키지 생성 화면 |
| `Assets/ANIMOL/AnimalUiV2/Generated/AnimalUpgradePhase1Demo.unity` | 독립 데모. 운영 씬 및 빌드 목록에 연결하지 않음 |
| `Assets/ANIMOL/UI/AnimalUpgradePhase1/Resources/ANIMOLAnimalUpgradePhase1/CharacterUpgrade.prefab` | **운영 프리팹** |
| `Assets/ANIMOL/UI/AnimalUpgradePhase1/AnimalCatalog.asset` | 생성본과 분리된 운영 초상 카탈로그 |
| `Assets/ANIMOL/UI/AnimalUpgradePhase1/AnimalUpgradeIdMap.asset` | 15개 아트 ID의 명시적 매핑 자리 및 확인된 Rabbit 연결 |

`ANIMOL > Animal UI v2 > Build Upgrade Phase 1` 메뉴를 실제 실행했다. 전체 `Build Preview`는 실행하지 않았으며 스테이지/멀티플레이 선택 화면도 생성하지 않았다.

운영본은 `ANIMOL > Animal UI v2 > Create missing production Upgrade Phase 1 assets`로 최초 작성했다. 이 메뉴는 이미 존재하는 운영 프리팹·카탈로그·ID 매핑을 덮어쓰지 않는다. 재실행 전후 세 파일의 바이트가 같음을 EditMode 테스트로 확인했다. 향후 운영 수정은 운영 경로에서 수행한다.

## 운영 진입/반환 연결

기존 연결을 조사한 결과 두 경로 모두 같은 화면 ID를 사용한다.

1. `Scripts/UI/MetaUiPresenter.cs`: `UpgradeHubCharacterButton` → `navigation.Navigate("SC10B_CharacterUpgrade")`.
2. `UI/ProductionV1/ProductionController.cs`: 운영 허브의 `PV1_Character` → 동일한 화면 ID. 계정 성장은 `PV1_Account` → `SC10A_AccountUpgrade`를 유지한다.

새 `AnimalUpgradePhase1Host`의 씬 로드 등록이 기존 navigation 객체에 `AnimalUpgradePhase1Entry`를 설치한다. Entry는 공용 스타일러의 초기 처리 이후 `SafeArea/ScreenHost/SC10B_CharacterUpgrade` 안에 운영 프리팹을 로드한다. 기존 컨테이너와 프리젠터 참조를 보존하고 이전 화면 자식은 런타임에 비활성화한다. 기존 미커밋 씬/프리팹을 저장하거나 생성기로 덮어쓰지 않았다. 기존 UI 재생성 후에도 같은 화면 ID와 host 계층이 있으면 다시 연결된다.

운영 `AnimalUiPresenter.OpenReadonlyPreviewOnStart=false`; Host가 활성화 때 `OpenUpgrade`를 호출한다. `Cancelled`는 `UiNavigationService.Back()`으로 기존 강화 허브에 돌아간다. Entry는 presenter가 뒤로 가기에서 자기 자신을 비활성화한 이후의 재진입도 다시 활성화한다. 이벤트는 해제 시 구독을 정리한다.

운영 프리팹에는 별도 Canvas/CanvasScaler/EventSystem, `AnimalUiSafeArea`, `AnimalUiDemoSwitcher`, `AnimalUpgradePhase1DemoHost`가 없다. 상위 운영 Canvas와 Safe Area만 사용한다. `AnimalUpgradeWidthFit`이 사용 가능한 안전 영역 너비에 맞춰 1080 기준 화면을 균일 축소하여 좌우 inset에서도 패널이 잘리지 않게 한다.

초기 검사에서 발견하고 고친 사항:

- 재진입 시 presenter의 비활성 상태가 남던 문제를 상위 Entry의 `ScreenChanged` 연결로 해결했다.
- 기존 `UiScenePolish`가 새 버튼·투명 클릭 영역을 다시 칠하던 문제를 설치 순서로 해결했다.
- Safe Area 좌우 inset에서 고정 폭 패널이 잘리던 문제를 운영 `AnimalUpgradeWidthFit`으로 해결했다.
- 패키지 공용 소스는 `AnimalUiScreenView.SetMode`의 한 곳만 변경했다. 강화 모드의 내부 `RosterScroll`을 비활성화하여 카드 위 드래그가 바깥 본문 스크롤로 전달되게 했다. 선택 화면의 동작은 유지한다.

## 아트와 표시 상태

| 탭 | 5종 |
|---|---|
| 지상 | Rabbit / Wolf / WhiteFerret / MountainGoat / Otter |
| 특수 | DreamFox / StarCat / MirrorDeer / DreamMole / ClockMoth |
| 공중 | Swallow / Owl / FlyingSquirrel / Hummingbird / Bat |

15개 PNG 각각을 입력 ZIP 안의 원본과 바이트 단위로 비교했고 모두 일치했다. v17 토끼의 클로버 핀을 포함한 원본 128×160 캔버스·투명 여백을 보존했다. SHA-256은 [portrait-byte-audit.json](UpgradePhase1/portrait-byte-audit.json)에 있다.

Single Sprite / Full Rect / Point / Uncompressed / mipmap off / NPOT None. 카드와 큰 초상은 `preserveAspect=true`, `useSpriteMesh=false`다. 초상 자체에 마스크를 추가하지 않았다. 스크롤 viewport의 경계와 초상 사각형의 포함 관계를 검사했고, 귀·핀·날개는 15종 캡처로 확인했다. 원본에 이미 있는 실루엣 경계는 수정하지 않았다.

성장 backend는 **null**이다. 화면 선택은 열람 동물만 바꾸며 실제 편성·해금·성장 서비스에 요청하지 않는다. 큰 초상, 이름, 역할, 사용 상태, 동물별 숙련도 표시와 두 트랙이 선택을 따라 갱신된다. 알려지지 않은 레벨·효과·비용·코인·숙련도는 `--` / `설정 대기`로 표시하고 두 강화 버튼과 재조회 버튼을 비활성화한다. 아트 존재를 근거로 `Implemented`, `Unlocked`, 구매 가능 상태를 설정하지 않는다.

ID 매핑은 `Rabbit` → 실제 존재하는 `Data/Campaign/Animals/RABBIT.asset`만 기록했다. 해당 정의도 placeholder visual이며 운영 액티브/패시브 설정과 purchasable은 false다. 다른 14종의 실제 종별 정의는 확인되지 않아 null이다. `GrowthServiceId`는 전부 빈 값이다. `DEV_GROUND`, `DEV_GLIDER`, `DEV_SPECIAL`을 종 ID로 추정하지 않았다. 매핑 에셋 자체는 상태나 권한을 부여하지 않는다.

## 실제 실행한 검증

결과 원문 요약: [unity-verification-results.json](UpgradePhase1/unity-verification-results.json).

| 검사 | 실제 결과 |
|---|---|
| 현재 프로젝트 Unity 컴파일 | 성공, 최종 `compilationFailed=false`, 컴파일 중 아님, Console Error 0 |
| `Validate Upgrade Phase 1` 메뉴 | 94개 계약/import/prefab 검사 통과 |
| `UpgradePhase1AssetTests` EditMode | 3/3 통과 |
| `CommercialPolishPolicyTests` EditMode | 3/3 통과 |
| `UpgradePhase1FlowTests` PlayMode | 2/2 통과 |
| 기존 `ProductionFlowTests` PlayMode | 7/7 통과 |
| 기존 `MenuThemeFlowTests` PlayMode | 3/3 통과 |
| 기존 `MenuThemePolicyTests` EditMode | 2/3 통과, 아래 기존 아트 불일치 1건 실패 |
| 입력 ZIP 대비 15개 초상 바이트·크기 | 15/15 일치 |

PlayMode에서는 실제 Lobby 씬을 로드하고 두 허브 버튼 경로, 뒤로/재진입, 계정 성장/로비 반환, 단일 EventSystem, 데모 컴포넌트 부재를 확인했다. 1080×1920, 1080×2400 Game View 각각에서 15종을 순회했다. 일반 화면과 Safe Area 모의 inset(좌/우 48px, 상단 120px, 하단 96px)을 모두 적용했다.

탭별 5종, 초상/이름/역할/사용 상태/숙련도/트랙 갱신, 미정 값과 강화 비활성화, 픽셀 글꼴 텍스트 overflow 없음, 카드 위 EventSystem 드래그 전달, 본문 실제 스크롤 이동, 스크롤 후 고정 하단 버튼 위치, 패시브 트랙 전체 노출, 상세 모달 열기/닫기를 검사했다. 실제 강화는 실행하지 않았다.

Game View 원본 캡처 **64장**은 [Captures](UpgradePhase1/Captures)에 있다(15종 × 2해상도 × 2안전영역 + 하단 4장). 직접 확인한 전체 초상 모음:

- [1080×1920](UpgradePhase1/1920_full_15_portraits.png), [1080×1920 Safe Area](UpgradePhase1/1920_safe_15_portraits.png)
- [1080×2400](UpgradePhase1/2400_full_15_portraits.png), [1080×2400 Safe Area](UpgradePhase1/2400_safe_15_portraits.png)

이 캡처는 패키지 HTML 미리보기가 아닌 **실제 운영 Lobby 진입 후 Unity Game View**다. Safe Area는 Editor에서 기존 SafeAreaLayout의 결과 rect에 inset을 넣은 모의 검사이며 실제 모바일 기기의 notch/터치 검사는 수행하지 않았다. 드래그는 EventSystem PointerEventData로 수행했다.

### 기존 v7 검사 실패와 보존 범위

`V7ImportsPreserveAllTwentyTwoOriginalPNGs`는 기존 `rabbit-run-left.png`, `rabbit-run-right.png`가 `Docs/Inbox/ANIMOL_main_ui_v7/runtime/assets`와 바이트가 달라 실패했다. 두 파일은 **작업 시작 전 git status에도 이미 수정 상태**였으며 이번 작업에서 수정/복원/스테이징하지 않았다. 불일치 해시는 [preexisting-v7-mismatch.json](UpgradePhase1/preexisting-v7-mismatch.json)에 기록했다. 새 UI 문제로 간주하여 이전 토끼 에셋으로 교체하지 않았다.

v7 실행 흐름·원래 네이티브 출력·팔레트·리소스 재사용 3개 PlayMode 테스트와 고정 발판/방향 정책 2개 EditMode 테스트는 통과했다. 따라서 새 화면 검증은 통과했지만 전체 저장소 테스트가 모두 성공했다고 보고하지 않는다. 기존 UI 테스트 중 `PlayOneShot was called with a null AudioClip` 경고도 관찰했으며 이번 작업에서 오디오 시스템을 변경하지 않았다.

## 작업 2의 연결 후보와 미완료 범위

| 현재 존재하는 파일/계약 | 다음 작업에서 확인할 내용 |
|---|---|
| `AnimalUiV2/Runtime/AnimalUpgradeBackendAdapterBase.cs` | 프로젝트 어댑터의 진입 경계: ReadSnapshotAsync / QuoteUpgradeAsync / TryUpgradeAsync. 선택 제출은 Unavailable 유지 |
| `Scripts/Core/CharacterUpgradeCatalog.cs`, `CharacterUpgradeDefinition.cs` | 종별 강화 정의 구조는 존재하나 현 카탈로그 definitions는 비어 있음. DEV preview IDs를 운영 값으로 사용 금지 |
| `Scripts/Core/GrowthEconomyPolicyCatalog.cs`, `GrowthEconomyContracts.cs` | 액티브/패시브와 공유 코인·동물별 숙련도 계약. 현재 DEV 3종의 효과/가격/숙련도 획득 규칙은 미설정 |
| `Scripts/Core/AccountProgression.cs`의 `IAccountGateway`, `UpgradePurchaseUseCase` | 계정 성장 요청/중복 ID 차단 구조. 현재 DisconnectedAccountGateway이며 동물별 거래 서비스로 완성된 것이 아님 |
| `Scripts/UI/GrowthEconomyUiPresenter.cs` | 기존 상태 표시 담당. 개발 보상 ledger를 실제 성장 잔액/승인 공급자로 사용 금지 |
| `UI/ProductionV1/ProductionController.cs` | 기존 계정 공통 성장 표시/승인 스냅샷 흐름. 별도 동물 backend와 구분 |
| `Data/Campaign/Animals/RABBIT.asset`, `CampaignAnimalDefinition.cs` | 확인된 실제 Rabbit 종 ID/구현 설정. 플레이어 계정의 해금/성장 상태는 별도 서비스 필요 |

실제 계정/동물 진행도 저장소, 동물별 액티브·패시브 견적, 서버 또는 권한 있는 원자적 거래, 지속적인 ActionId/미확인 결과 관리자는 작업 2에서 결정해야 한다. 작업 1은 가격·효과·레벨 상한·해금 조건을 새로 만들지 않았다. 구매 승인/차감/재시작 저장/네트워크 실패·중복 요청 검증은 수행하지 않았다.

## 변경 파일 목록과 재검증

신규 모듈 두 디렉터리 및 `.meta`, 이 결과 문서와 `Docs/UpgradePhase1`의 검증 자료가 이번 작업 변경이다. 전체 경로는 [changed-files.txt](UpgradePhase1/changed-files.txt)에 있다. 기존 계정 성장 UI, v7 배경 20장/토끼 시트 2장, 로비/Bootstrap 씬, 플레이어·맵·재화·보상 코드는 변경하지 않았다.

재검증 명령(연결된 Unity Editor 필요):

```powershell
unity command menu --path 'ANIMOL/Animal UI v2/Validate Upgrade Phase 1'
unity command run_tests --mode editor --filter UpgradePhase1AssetTests
unity command run_tests --mode editor --filter CommercialPolishPolicyTests
unity command run_tests --mode playmode --filter UpgradePhase1FlowTests
unity command run_tests --mode playmode --filter ProductionFlowTests
```

Game View 검사 테스트의 원본 출력은 `%TEMP%/ANIMOL-UpgradePhase1-Captures`다. 운영 프리팹/어댑터를 이후 수정하더라도 제공 생성 메뉴의 산출물로 덮어쓰지 않는다.
