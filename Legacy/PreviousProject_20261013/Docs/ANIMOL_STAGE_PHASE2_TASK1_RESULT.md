# ANIMOL 스테이지 동물 확인 2단계 · 작업 1 결과

작성: 2026-10-04 / 현재 ANIMOL 작업 트리 / Unity 6000.3.8f1

## 결과

**작업 1의 화면 가져오기와 기존 스테이지 목록 진입/반환을 적용했다.** 실제 운영 Lobby에서 15종 열람과 두 Game View 해상도, 모의 Safe Area를 검사했다. 실제 입장 서비스는 연결하지 않았으며 운영 시작 버튼은 비활성이다. 2단계 전체 운영 수용 완료를 뜻하지 않는다.

현재 실제 스테이지 asset 100개 중 고정 동물이 있는 것은 T01-S01의 `RABBIT` 한 종뿐이다. 나머지 99개는 빈 목록이다. 최신 지상/특수/공중 3종 편성을 임의 생성하지 않았다. T01-S01은 토끼/설정 대기/설정 대기로 표시하고, 다른 스테이지는 세 슬롯 모두 설정 대기다. [실제 Editor 카탈로그 조회](StagePhase2Task1/actual-stage-catalog.json).

## 확인한 프로젝트와 패키지

- 저장소 `AGENTS.md`, Unity 버전, 기존 `UiBuildPipeline`, `ProductionBuilder/ProductionController`, `CampaignUiPresenter`, `UiNavigationService`, `CampaignStageDefinition`, `CampaignStageRuntimePolicy`, `CampaignProgressionService`, `StageLaunchUseCase`, `CampaignRosterResolver` 및 작업 1/2/3 강화 보고서를 확인했다.
- 기존 1단계 운영 프리팹과 `AnimalUpgradeProjectAdapter`는 이미 적용되어 있었다. 실제 동물 성장 서비스는 미연결이라는 기존 한계를 유지했다. 1단계 소유 파일은 수정하지 않았다.
- 패키지 `README_PHASE2.md`, `UNITY_APPLY_PHASE2.md`, `Unity/README_PHASE2_UNITY.md`, `Unity/README_UNITY_INTEGRATION.md`, `Docs/acceptance-phase2.json`과 최신 Unity 소스를 비교했다. ZIP SHA-256: `909263b813b2d8d196396171063291d15e110dd99a7e440696708cfdbaa2e721`.
- 기존 uGUI 2.0.0, Input Manager, 단일 EventSystem/StandaloneInputModule, 1080×1920 width-match CanvasScaler와 `SafeAreaLayout`을 사용한다. 운영 Text에는 기존 `PixelTypographyProfile`/`PixelTextBridge`를 연결했다. 새 입력·폰트 패키지는 설치하지 않았다.
- 기존 변경이 많은 작업 트리에서 시작했다. 기존 ProductionV1·캠페인·씬·v7 변경을 덮어쓰거나 이번 커밋에 포함하지 않았다. 테스트가 만든 폰트 atlas와 아트 참조 캐시는 작업 전 바이트로 복원했다.

## 생성물과 연결 위치

요청한 `ANIMOL > Animal UI v2 > Build Stage Phase 2` 메뉴를 실제 Editor에서 실행했다.

| 용도 | 경로 (`Assets/ANIMOL/` 기준) |
|---|---|
| 생성 프리팹 | `AnimalUiV2/Generated/StageAnimalSelect.prefab` |
| 생성 데모 | `AnimalUiV2/Generated/AnimalStagePhase2Demo.unity` |
| 운영 프리팹 | `UI/AnimalStagePhase2/Resources/ANIMOLAnimalStagePhase2/StageAnimalSelect.prefab` |
| 운영 아트 카탈로그 | `UI/AnimalStagePhase2/AnimalCatalog.asset` |
| 운영 양방향 ID 매핑 | `UI/AnimalStagePhase2/AnimalStageIdMap.asset` |
| 누락된 운영 asset만 만드는 메뉴 | `AnimalStagePhase2Builder.CreateMissingProductionAssets` |

실제 경로는 `ProductionController.SelectTheme → SelectStage → SelectedStage → UiNavigationService.Navigate("SC04_StageDetail") → AnimalStagePhase2Host.OpenSelectedStage → AnimalUiPresenter.OpenCampaign(request)`다. 기존 `SC03_StageSelect`의 `PV1_Stage_0…19` 버튼과 선택 상태를 재사용한다. `AnimalStagePhase2Entry`가 초기 기존 스타일 적용 뒤 SC04의 표시 자식을 비활성화하고 운영 프리팹을 설치한다. 기존 컨트롤러의 참조와 로더 코드는 유지한다.

뒤로는 Presenter.Cancelled → 기존 `UiNavigationService.Back()`으로 목록에 복귀한다. 실제 선택 스테이지와 목록 스크롤 보존 회귀 검사가 통과했다. 운영 프리팹에는 별도 Canvas/CanvasScaler/EventSystem, DemoSwitcher, Phase2DemoHost, 중복 Safe Area가 없다. 데모 씬은 운영 씬이나 Build Settings에 추가하지 않았다.

## 실제 문맥과 데이터 경계

`AnimalStageContext.Read`는 선택한 실제 asset의 StageId·DisplayName, FixedAllowedAnimalIds와 FixedAnimalDefinitions, InitialAnimalId/Definition, ContentVersion 및 RuntimePolicy.Version을 읽는다. 두 고정 편성 표현에 일치하고 명시적으로 매핑된 종만 슬롯에 넣는다. 같은 역할에 여러 후보가 있거나 미매핑이면 빈 슬롯을 유지한다.

Policy는 Fixed, RequiredRoles는 이번 단계의 Ground/Special/Air다. 대표 역할은 실제 시작 동물의 명시적 매핑에서 얻으며 미설정이면 유효하지 않은 역할로 남겨 시작 검증을 통과시키지 않는다. InitialLoadout은 카탈로그 고정값의 독립 복사다. 열람/취소는 저장 편성·보유·해금·성장 데이터를 쓰지 않는다.

정책 버전은 실제 로컬 버전 두 개를 보존한 `content:{ContentVersion};runtime:{RuntimePolicy.Version}`이며 T01-S01은 `content:2;runtime:2`다. 필수 버전이 없으면 null이다. **서버 권한이나 조회 Revision을 합성한 것이 아니다.** 접근 규칙의 PrerequisiteStageId와 ContentAvailabilityResolver 결과는 호스트가 읽어 화면 안내로 표시한다. 공용 StageSelectionRequest에는 접근 grant 필드가 없으므로 계정별 권한은 부여하지 않는다. 작업 2에서 실제 서비스가 StageId로 현재 접근 정책을 재조회해야 한다.

배경은 실제 선택 asset의 ThemeId로 기존 `ProductionCatalog.Find("BG_UI_Campaign_" + stage.ThemeId)`에서 읽어 `View.CampaignBackground`에 넣는다. T01~T05의 실제 첫 스테이지 진입으로 서로 대응하는 기존 배경 참조를 검사했다. 운영 프리팹 자체에는 T01 기본 배경을 저장하지 않았고, 매핑 실패 시 기본 T01로 대체하지 않는다.

| 역할 | 안정 아트 ID → 실제 프로젝트 ID |
|---|---|
| 지상 | Rabbit → RABBIT; Wolf → 미매핑; WhiteFerret → 미매핑; MountainGoat → 미매핑; Otter → 미매핑 |
| 특수 | DreamFox, StarCat, MirrorDeer, DreamMole, ClockMoth → 각각 미매핑 |
| 공중 | Swallow, Owl, FlyingSquirrel, Hummingbird, Bat → 각각 미매핑 |

15개 매핑 행은 명시적으로 저장했다. DEV_GROUND/DEV_SPECIAL/DEV_GLIDER를 실제 종으로 전용하지 않는다. RABBIT의 생산 능력도 미설정이고 실제 액티브/패시브 레벨·설명 서비스가 없어 화면은 `-- · 설정 대기`다. 아트 존재로 Implemented/Unlocked/HasContextPermission/CanUseInContext를 true로 만들지 않는다. 운영 Backend는 null이고 입장/강화/방 요청 연결은 없다.

## 공용 소스 병합과 수정

- 최신 패키지의 `AnimalStageBackendAdapterBase`, 읽기 전용 데모 host, Fixed 문맥/버전/승인 receipt 검사, 요청 복사, 스테이지 동물 정보 모달과 생성/검증 메뉴를 병합했다.
- 기존 강화의 거절 이유 유지 및 재조회, 알려진 레벨 표시, 비용 미설정 표시, 장문 효과·비용 축약과 전체 모달, 모달 스크롤 초기화를 보존했다.
- 열람은 큰 초상·이름·설명만 바꾸며 Fixed Draft/슬롯을 바꾸지 않는다. 스테이지 액티브/패시브는 본문 요약과 전체 정보 모달로 표시한다. 장문은 모달로 안내한다.
- 독립 스크롤이 없는 카드 ScrollRect를 비활성화해 카드 위 드래그가 바깥 BodyScroll로 전달되게 했다. 정책 안내를 두 줄 높이로 확보했다.
- 회귀 검사에서 재진입 시 이미지가 늘어나는 문제를 발견해 카드 재사용으로 수정했다. 운영 프리팹은 5개 카드를 미리 보유한다. 기존 `ProductionFlowTests`를 변경하지 않고 최종 7/7 통과했다.
- 공용 파일 6개 수정, 공용 신규 runtime 파일 2개와 meta, 생성 stage 프리팹/씬, 운영 `UI/AnimalStagePhase2` 코드·카탈로그·매핑·프리팹·EditMode/PlayMode 테스트, 이 보고서 및 증빙을 추가했다. 정확한 목록은 [변경 파일](StagePhase2Task1/changed-files.txt)에 있다.

## 실제 실행 검증

| 검사 | 실행 결과 / 증빙 |
|---|---|
| Unity 실제 컴파일 | [compilationFailed=false, compiling=false, Console Error 0](StagePhase2Task1/unity-compile.json) |
| StagePhase2AssetTests EditMode | [3/3](StagePhase2Task1/editmode-stage.json): 독립 운영 구성, 재생성 보호, 실제 불완전 편성, 15종 매핑/import |
| StagePhase2FlowTests PlayMode | [3/3](StagePhase2Task1/playmode-stage.json): 5테마 진입/반환, 전 동물·해상도·Safe Area, 별도 표시 fixture |
| 기존 강화 EditMode | [asset 3/3](StagePhase2Task1/editmode-upgrade-assets.json), [backend 4/4](StagePhase2Task1/editmode-upgrade-backend.json) |
| 기존 강화 PlayMode | [11/11](StagePhase2Task1/playmode-upgrade.json), 공용 카드 재사용 수정 후 재실행 |
| 기존 ProductionFlow | [7/7](StagePhase2Task1/playmode-production.json): 계정 성장·상세 반환·스크롤·기존 로더 실패 처리 등 |
| v7 로비 / 기존 메뉴 진입 | [3/3](StagePhase2Task1/playmode-v7.json), [3/3](StagePhase2Task1/playmode-entry.json) |
| CommercialPolishPolicyTests | [3/3](StagePhase2Task1/editmode-feedback.json). 새 피드백/진동/카메라 효과 추가 없음 |
| 패키지 Editor 검증 메뉴 | [Stage Phase2 117개, Upgrade Phase1 94개 검사 통과](StagePhase2Task1/unity-menu-validation.json). 운영 입장 승인을 검증한 결과는 아님 |
| 아트 파일 검사 | [15개 원본 SHA-256 일치, 전부 128×160, 사용 색상 합집합=Sweetie16](StagePhase2Task1/portrait-byte-audit.json). 원본 PNG 수정 없음 |

최종 고유 테스트 **40/40 통과**. 최초 프레임 전환 중 위치 측정 실패는 테스트를 전환 완료 후 측정하도록 보완했고, 이미지 증가 실패는 실제 카드 재사용 수정 후 통과했다. 빈 정규식 조합으로 선택된 테스트 0개 실행은 통과 수에 넣지 않았다. 기존 `PlayOneShot was called with a null AudioClip` [경고](StagePhase2Task1/known-audio-warning.json)는 남아 있으며 오디오 소스는 변경하지 않았다.

## Game View 증빙

운영 Lobby에서 1080×1920/1080×2400 각각 정상 영역과 모의 Safe Area를 실행했다. Safe Area는 좌우 48px, 하단 96px, 상단 120px을 기존 SafeAreaLayout 결과 rect에 적용한 Editor 검사다. 실제 기기 검사는 아니다.

15종 큰 초상 60장, 역할별 카드 12장, 운영 정보 모달 4장, 표시 fixture 2장으로 총 78장의 실제 Game View 캡처를 생성했다. 원본 전체는 `%TEMP%/ANIMOL-StagePhase2-Captures`에 있으며 아래 대표 증빙을 저장소에 보관한다.

- [15종 큰 초상 모음](StagePhase2Task1/all-15-hero-contact.png): 실제 캡처의 큰 초상 패널 영역을 모은 검토용 합성. 원본 아트 변형 아님.
- [1920 토끼 전체 화면](StagePhase2Task1/1920_full_Rabbit.png), [1920 Safe Area 시계나방](StagePhase2Task1/1920_safe_ClockMoth.png), [특수 5종 카드](StagePhase2Task1/1920_safe_cards_Special.png).
- [2400 하늘다람쥐](StagePhase2Task1/2400_full_FlyingSquirrel.png), [2400 Safe Area 공중 5종](StagePhase2Task1/2400_safe_cards_Air.png), [운영 정보 모달](StagePhase2Task1/2400_safe_modal.png).
- [표시 fixture: 장문 스크롤](StagePhase2Task1/fixture_only_long_detail.png), [표시 fixture: 지정 3종 확인](StagePhase2Task1/fixture_only_fixed_roster_confirmation.png).

카드와 큰 초상의 Sprite 참조, preserveAspect, Single/Full Rect, Point, 무압축, mipmap 없음 및 초상 rect의 viewport 포함을 검사했다. 귀·클로버 핀·뿔·날개를 추가 크롭하지 않는다. 스크롤 경계 밖의 카드가 일시적으로 가려지는 것은 정상 스크롤 동작이며 끝까지 이동하면 카드 전체가 들어온다. 긴 실제 이름, 텍스트 overflow, EventSystem 카드 드래그와 고정 하단 버튼의 안전 영역 포함을 검사했다.

운영 화면에서 Primary를 강제로 Invoke해도 모달/로더 요청이 발생하지 않고 `ProductionController.LoadRequests=0`이다. 15종 탐색 전후 Draft·고정 문맥·stage asset·ID map 직렬화값이 같다. 실제 보유/성장/재화 서비스 호출 경로를 설치하지 않았다.

장문과 int.MaxValue 레벨, 전체 지정 편성 확인은 **테스트 전용 backend**로만 검사했다. 늑대를 열람해도 확인 모달은 테스트 지정 토끼/몽환여우/제비 전체를 표시하고, 취소는 제출 0회다. 이 fixture는 운영 프리팹·카탈로그·서비스에 저장되지 않는다. 실제 최대 레벨·효과 또는 실제 입장 가능 스테이지의 증거로 해석하지 않는다.

## 작업 2 후보와 미검증

실제 입장 후보 경로는 `ProductionController.BeginLoading/LoadSelected → StageLaunchUseCase.Launch → CampaignRosterResolver.Resolve → CampaignLaunchContext.Set → SceneManager.LoadSceneAsync(snapshot.Setup.ThemeRuntimeSceneName)`다. 접근 판단 후보는 기존 CampaignUiPresenter가 소유하고 ProductionController가 재사용하는 세션 `CampaignProgressionService.IsUnlocked`와 `ContentAvailabilityResolver`다. 호스트에서 별도 빈 계정 진행도를 만들어 권한으로 사용하지 않았다.

`StageLaunchUseCase`는 현재 로컬 런 구성 생성이며 ActionId, 스냅샷/정책 버전, 서버 입장 receipt, 멱등 요청/결과 조회를 제공하지 않는다. 새 UI에서 이를 바로 호출해 승인으로 간주하지 않았다. 작업 2에서 실제 서비스 계약을 확인한 뒤 파생 `AnimalStageBackendAdapterBase.ReadStageSnapshotAsync/SubmitFixedCampaignAsync`와 검증된 CampaignAccepted → 기존 로더의 단일 연결을 구현해야 한다.

미검증/후속 사항:

1. 승인된 지상/특수/공중 3종 실제 편성, 나머지 14종 종 ID, 생산 구현과 액티브/패시브 데이터가 필요하다. 임의 편성·설명으로 메우지 않았다.
2. 계정별 접근·스테이지 체험 grant, 최신 조회 Revision, 실제 승인·거절·타임아웃·멱등 결과 조회·앱 복귀 결과 보존·성공 맵 로딩은 작업 2/3 범위로 미검증이다.
3. 실제 기기 notch·터치와 실제 운영 최대 숫자/최장 효과는 미검증이다. 현재 데이터 표시 및 Editor 모의 영역/표시 fixture 결과만 보고했다.
4. 기존 멀티플레이 진입 UI의 미연결 상태와 로비/계정 성장 UI를 보존했으며 새 방/스테이지 요청은 추가하지 않았다. 멀티플레이 서버 성공을 검증한 것은 아니다.

완료 판정은 이번 **작업 1 화면·진입·반환 범위**에 한정한다. 패키지 독립 생성 검사, 실제 프로젝트 UI 검사, 아직 하지 않은 운영 서비스 검증을 분리했다.
