# ANIMOL 멀티플레이 동물 선택 3단계 · 작업 1 결과

작성: 2026-10-04 / Unity 6000.3.8f1 / 현재 ANIMOL 작업 트리

## 적용 결과와 범위

**화면, 기존 경쟁 허브의 진입/반환, Generated 밖의 운영 프리팹을 적용했다.** 실제 Lobby에서 역할별 5종·총 15종 열람, 1080×1920/1080×2400 및 모의 Safe Area를 확인했다. Unity 컴파일 성공, 관련 고유 테스트 **67/67 통과**다.

**실제 운영 모드 문맥 연결은 미완료다.** 현재 저장소에 운영 ModeId/DisplayName 카탈로그, EntryIntent, 저장된 실제 종 편성, 대표 역할, 정책 버전 소유 계약이 없다. 기존 코드는 개발용 방 미리보기와 서버 미연결 표시다. 일반/랭크/비공개 경쟁 버튼은 새 열람 화면으로 연결했지만, 운영 요청에는 ModeId·EntryIntent·PolicyRevision을 합성하지 않고 설정 대기로 남겼다. 실제 권한/입장 adapter는 null이고 입장은 비활성이다. 실제 방 입장·대기방 연결은 작업 2에서 다룬다.

## 먼저 확인한 자료와 현재 코드

AGENTS.md, Unity 버전, 기존 UiBuildPipeline/ProductionBuilder와 운영 프리팹 설치 방식, PortraitEntryController, MultiplayerUiPresenter, ModeLoadoutRuleSet, MultiplayerPreviewDefinition, MultiplayerMatchContracts, MultiplayerLoadoutService, UiNavigationService를 확인했다. 강화 작업 결과와 캠페인 작업 1/2/3 결과 및 현재 소스도 읽었다. 기존 강화의 실제 동물 성장 서비스와 캠페인의 입장 서비스가 미연결인 한계는 유지한다.

패키지 README_PHASE3.md, UNITY_APPLY_PHASE3.md, Unity/README_PHASE3_UNITY.md, README_UNITY_INTEGRATION.md와 Unity 소스를 읽고 공용 소스를 비교했다. ZIP SHA-256은 `834731a8c51b62fe8d05e130d19b6378de7ded386ab633358cdfac8a1b72ca09`이다. 기존 .meta와 PNG를 덮어쓰지 않았다.

| 확인한 실제 소유자 | 현재 계약과 한계 |
|---|---|
| `Scripts/UI/MultiplayerUiPresenter.cs` | 일반/랭크/비공개 경쟁, 협동, 개발용 방/Ready의 기존 UI host. 운영 버튼은 이전에 ShowMatchUnavailable만 호출했다. |
| `Scripts/Core/ModeLoadoutRuleSet.cs` | GameModeKind enum과 슬롯/중복/pool 규칙. 안정 ModeId·표시 이름·정책 버전·대표 역할·진입 의도 계약은 없음. enum 이름을 운영 ModeId로 전용하지 않음. |
| `MultiplayerPreviewDefinition`, `Data/Development/DEV-MULTI-*.asset` | Competitive/Coop 두 개발 전용 asset. DEV 폼과 PreviewId는 운영 값으로 사용하지 않음. [Editor 조회](MultiplayerPhase3Task1/project-data-audit.json) |
| `CompetitiveParticipantSelectionService` | 참가자별 개발용 3역할 선택과 참가자 간 중복 허용. 캠페인 해금 스냅샷만으로 실제 문맥 권한을 만들지 않음. |
| `MultiplayerLoadoutService`, `IMatchGateway.RequestReady` | 선택 배열의 Ready 요청 계약. ModeId/EntryIntent/정책·조회 버전/입장 receipt 계약이 아님. 구현은 DisconnectedMatchGateway. |
| `SC06_MatchRoom`, `SC07_MultiplayerAnimalSelect` | 기존 개발 미리보기 흐름. 새 운영 열람 화면과 분리해 유지. 방 찾기/초대의 실제 처리 메서드·카탈로그는 찾지 못함. |

기존 uGUI 2.0.0, Input Manager, 단일 EventSystem/StandaloneInputModule, 1080×1920 width-match CanvasScaler와 SafeAreaLayout을 재사용한다. 운영 텍스트는 기존 PixelTypographyProfile/PixelTextBridge를 사용한다. 새 서버·방 목록·대기방·입력 시스템을 만들지 않았다.

## 생성물과 운영 진입/반환

요청한 `ANIMOL > Animal UI v2 > Build Multiplayer Phase 3` 메뉴와 별도 운영 자산 생성 메뉴를 실제 Editor에서 실행했다.

| 용도 | 경로 (`Assets/ANIMOL/` 기준) |
|---|---|
| 생성 프리팹 | `AnimalUiV2/Generated/MultiplayerAnimalSelect.prefab` |
| 생성 데모 | `AnimalUiV2/Generated/AnimalMultiplayerPhase3Demo.unity` |
| 운영 프리팹 | `UI/AnimalMultiplayerPhase3/Resources/ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect.prefab` |
| 운영 아트 카탈로그 | `UI/AnimalMultiplayerPhase3/AnimalCatalog.asset` |
| 운영 양방향 종 매핑 | `UI/AnimalMultiplayerPhase3/AnimalMultiplayerIdMap.asset` |
| 누락 자산만 생성하는 메뉴 | `ANIMOL/Animal UI v2/Create missing production Multiplayer Phase 3 assets` |

운영 builder는 기존 자산이 있으면 재생성하지 않는다. 기존 운영 prefab/catalog/map의 바이트가 재실행 후 같은지 테스트했다. 생성 메뉴를 실행한 뒤 기존 강화·캠페인 Generated 결과와 운영 자산의 해시도 동일했다.

실제 경로는 다음과 같다.

`Lobby 경쟁 버튼 → 기존 SC05_CompetitiveHub → CompetitiveMatchButton / RankedMatchButton / CompetitivePrivateButton → AnimalMultiplayerPhase3Host.OpenUnconfigured → Open(request의 복사본) → AnimalUiPresenter.OpenMultiplayer`

`AnimalMultiplayerPhase3Entry`가 Lobby의 기존 MultiplayerUiPresenter와 UiNavigationService를 확인한 뒤 설치한다. 기존 세 버튼의 이전 이벤트 객체를 보관하고 창 열기로 연결하며, 해제 시 돌려준다. 기존 DEV 방 버튼·Ready·협동 흐름의 바인딩은 건드리지 않는다. 새 `AnimalMultiplayerPhase3`는 화면 경로 이름이며 모드/방 ID가 아니다.

PortraitEntryController에는 열람 가능 여부를 별도로 추가했다. 로비 경쟁 버튼은 서버 미연결이어도 기존 경쟁 허브를 열 수 있고 `동물 열람 · 입장 미연결`을 표시한다. OnlineModesAvailable은 여전히 false다. 광고·협동의 기존 미연결 차단은 유지했다. 이에 맞춰 기존 메뉴 테스트의 경쟁 버튼 기대값만 변경하고 회귀를 실행했다.

뒤로는 `Presenter.Cancelled → Host.Return → UiNavigationService.Back → 기존 경쟁 허브`다. 원래 편성 복사본을 LastReturnedLoadout 및 Returned 이벤트로 반환한다. 새 host는 편성 저장이나 계정 데이터 쓰기를 하지 않는다. 공개 `Open(MultiplayerSelectionRequest)`는 전달받은 ModeId/DisplayName/EntryIntent/역할/대표 역할/정책/InitialLoadout/AllowedAnimalIds를 복사하고 바꾸지 않는다. 현재 기존 host에는 이 값을 제공하는 운영 계약이 없어 실제 버튼 경로에서는 누락 상태로 연다.

운영 프리팹은 자동 시안 열기가 꺼져 있고 Backend=null이다. 별도 Canvas, CanvasScaler, EventSystem, DemoSwitcher, 데모 Host, 중복 SafeArea를 포함하지 않는다. 실제 host만 창을 연다. PREVIEW_ONLY 문맥은 운영 host에서 거절한다. 데모 씬은 운영 씬/Build Settings에 추가하지 않았다.

## 공용 모듈 병합과 선택 동작

패키지의 멀티플레이 전용 adapter base·데모 host·Editor 검사, EntryIntent/AcceptedEntryIntent 계약, 모드·권한·receipt 검증, 상세 모달과 레이아웃 변경을 반영했다. 패키지에 없는 프로젝트의 다음 수정은 보존했다.

- 캠페인 snapshot ContextId/PolicyRevision 대조, 잘못된 조회가 이전 권한을 무효화하는 검사, 실제 host의 수동 문맥 새로고침.
- 강화의 실제 거절 이유 보존·미설정 값·상세 효과/비용 표시, 카드 재사용, 스크롤 입력 전달.
- 장문 모달의 실제 TMP 높이 측정. 새 멀티플레이 모달에도 해당 옵션을 적용했다.

선택은 최신 snapshot을 읽은 뒤 `Implemented && HasContextPermission && CanUseInContext` 및 역할/pool을 통과할 때만 Draft에 반영된다. 실제 조회 전에는 카드 열람만 가능하다. Unlocked만으로 선택을 승인하지 않고 다른 참가자의 선택은 제한 조건으로 사용하지 않는다. AllowedAnimalIds의 null과 빈 배열을 Clone/Open에서 구분한다.

확인 모달은 모드 이름과 Draft의 지상/특수/공중 전체를 표시한다. 현재 열람 동물로 편성을 바꾸어 표시하지 않는다. 액티브/패시브는 본문 요약과 전체 상세 모달로 확인한다. 미정 설명·레벨은 `--/설정 대기`다. 실제 adapter가 없으므로 운영에서 슬롯 변경·확인 제출·방 이동은 발생하지 않는다.

## ID·아트 보존

| 역할 | 안정 아트 ID → 확인된 프로젝트 종 ID |
|---|---|
| 지상 | Rabbit → RABBIT; Wolf/WhiteFerret/MountainGoat/Otter → 각각 미매핑 |
| 특수 | DreamFox/StarCat/MirrorDeer/DreamMole/ClockMoth → 각각 미매핑 |
| 공중 | Swallow/Owl/FlyingSquirrel/Hummingbird/Bat → 각각 미매핑 |

15행을 명시적으로 생성했고 RABBIT ↔ Rabbit 왕복, DEV_GROUND 거절, 미매핑 종의 null을 검사했다. RABBIT은 기존 CampaignAnimalDefinition에 있는 공용 종 ID다. 멀티플레이 서비스에서 승인한 ID/권한이라는 뜻은 아니다. 미확인 14종이나 DEV 폼을 새 운영 ID로 만들지 않았다.

[15 PNG 원본 감사](MultiplayerPhase3Task1/portrait-byte-audit.json): 최신 패키지와 SHA-256 모두 일치, 128×160 전체 캔버스 보존, 불투명 색상의 합집합이 Sweetie16 16색과 일치한다. Rabbit 해시는 `8d51fbdd019aa964980ac30ed49f3f97a80d190840055de52e8044d16dd32257`이다. Unity에서 Single/Full Rect, Point, 무압축, mipmap 없음과 preserveAspect를 검사했다. 귀·클로버 핀·날개에 추가 프레임 크롭이 보이지 않았다.

## 실제 실행 검사와 Game View

| 검사 | 최종 결과 / 증빙 |
|---|---|
| Unity 컴파일 | [compilationFailed=false, compiling=false, Console Error 0](MultiplayerPhase3Task1/unity-compile.json) |
| 새 운영 asset EditMode | [2/2](MultiplayerPhase3Task1/editmode-multiplayer.json): 독립 운영 구성/재생성 보호·15종 import/명시적 매핑 |
| 새 화면 PlayMode | [3/3](MultiplayerPhase3Task1/playmode-multiplayer.json): 실제 세 진입·반환, 15종/두 해상도/Safe Area, 표시 전용 fixture |
| 캠페인 회귀 | [EditMode 11/11](MultiplayerPhase3Task1/editmode-stage.json), [PlayMode 9/9](MultiplayerPhase3Task1/playmode-stage.json) |
| 강화 회귀 | [asset 3/3](MultiplayerPhase3Task1/editmode-upgrade-assets.json), [backend 4/4](MultiplayerPhase3Task1/editmode-upgrade-backend.json), [PlayMode 11/11](MultiplayerPhase3Task1/playmode-upgrade.json) |
| 기존 Production/v7/메뉴 진입 | [7/7](MultiplayerPhase3Task1/playmode-production.json), [3/3](MultiplayerPhase3Task1/playmode-v7.json), [3/3](MultiplayerPhase3Task1/playmode-entry.json) |
| 기존 멀티플레이 계약 | [M6MultiplayerContractTests 8/8](MultiplayerPhase3Task1/editmode-legacy-multiplayer.json) |
| 연출 정책 | [CommercialPolishPolicyTests 3/3](MultiplayerPhase3Task1/editmode-feedback.json) |
| Editor 메뉴 | [Phase3 147개 / Phase2 119개 / Phase1 94개 통과](MultiplayerPhase3Task1/unity-menu-validation.json). Phase3는 synthetic 계약 75개와 import/prefab/demo 검사 포함 |

반복 실행을 제외한 고유 테스트 합계는 67개다. Console 누적 버퍼와 현재 컴파일 상태를 구분했으며 기존 null AudioClip 경고는 남아 있다. 새로운 입력 차단 연출·진동·카메라 효과는 추가하지 않았다.

1080×1920/1080×2400 각각 정상 영역과 좌우 48px·하단 96px·상단 120px의 Editor 모의 Safe Area에서 실행했다. 15종 이름과 카드/큰 초상 참조, 스크롤 드래그 라우팅, 고정 하단 버튼의 안전 영역 포함을 검사했다. 긴 설명과 최대 정수 표시는 테스트 fixture로 확인했다. 실제 최대 성장 수치나 실제 모드 이름 데이터 검증은 아니다.

- [15종 큰 초상](MultiplayerPhase3Task1/all-15-hero-contact.png), [15종 카드](MultiplayerPhase3Task1/all-15-card-contact.png).
- [1920 토끼](MultiplayerPhase3Task1/1920_full_Rabbit.png), [1920 Safe Area 시계나방](MultiplayerPhase3Task1/1920_safe_ClockMoth.png), [2400 하늘다람쥐](MultiplayerPhase3Task1/2400_full_FlyingSquirrel.png), [2400 Safe Area 공중 카드](MultiplayerPhase3Task1/2400_safe_cards_Air.png), [운영 상세 모달](MultiplayerPhase3Task1/2400_safe_modal.png).
- 실제 버튼 진입: [일반](MultiplayerPhase3Task1/CompetitiveMatchButton.png), [랭크](MultiplayerPhase3Task1/RankedMatchButton.png), [비공개](MultiplayerPhase3Task1/CompetitivePrivateButton.png). 모두 실제 문맥 미설정 상태다.
- 표시 fixture: [최대 정수/장문 처음](MultiplayerPhase3Task1/fixture_1920_detail_top.png), [장문 끝 패시브](MultiplayerPhase3Task1/fixture_1920_detail_bottom.png), [모드와 선택 3종 확인](MultiplayerPhase3Task1/fixture_2400_confirmation.png).

총 85개 실제 Game View 캡처를 생성했다. [전체 파일명/크기/해시](MultiplayerPhase3Task1/capture-manifest.json), 전체 원본은 `%TEMP%/ANIMOL-MultiplayerPhase3-Captures`, 대표 원본과 검토용 합성 14개는 증빙 폴더에 보관했다. 합성은 캡처 검토용이며 운영 PNG를 변형하지 않았다.

운영 검사에서 시작 강제 Invoke에도 모달이 열리지 않고 기존 방 preview 참가자 수·CampaignLaunchContext.Pending·host 직렬화값이 유지됐다. 15종 열람 전후 Draft와 ID map도 동일했다. 입장 서비스가 설치되지 않아 서비스 요청 경로 자체가 없다.

별도 fixture에서는 Unlocked=false인 사용 허용 동물의 선택, 미구현 수달 열람 시 기존 선택 유지, 원래 토끼 편성의 불변/반환, null pool과 빈 pool 구분을 확인했다. 수달을 열람하면서 확인 모달에는 선택한 늑대/몽환여우/제비가 보였다. 취소 전후 제출 카운터 0이었다. 해당 backend·모드·편성·효과는 테스트 어셈블리에서만 사용하며 운영에 저장하지 않았다.

## 변경 파일·보존·작업 2 후보

[정확한 변경 파일](MultiplayerPhase3Task1/changed-files.txt). 공용 수정 5개, 신규 공용 소스 3개와 meta, 생성 멀티플레이 prefab/demo, 새 운영 모듈·자산·테스트, 로비 경쟁 열람 연결과 관련 기대값, 보고서/증빙을 포함한다. 기존 미커밋 MultiplayerUiPresenter·ProductionV1·씬을 편집하거나 이번 커밋에 포함하지 않았다.

작업 전 기록한 v7·강화/캠페인 운영 자산·캠페인 데이터·씬·기존 Generated 파일 총 418개는 SHA-256 변경 0개였다. [보존 결과](MultiplayerPhase3Task1/preservation-result.json), [기준 해시](MultiplayerPhase3Task1/preservation-before.json). 테스트가 만든 폰트 atlas와 아트 참조 캐시는 시작 바이트로 복원했다. 기존 강화/캠페인 adapter와 운영 prefab을 새 생성물로 덮어쓰지 않았다.

작업 2에서 확인할 후보는 기존 `MultiplayerUiPresenter`, `IMatchGateway`, `MultiplayerLoadoutService`, `CompetitiveParticipantSelectionService`, `SC06_MatchRoom`이다. 현재 RequestReady는 입장 승인/room receipt로 대체할 수 없다. 운영 카탈로그와 진입 의도 소유자가 확인되면 `Host.Open(request)`에 실제 문맥을 전달하고 `AnimalMultiplayerBackendAdapterBase.ReadMultiplayerSnapshotAsync/SubmitRoomEntryAsync`를 실제 서비스에 연결해야 한다.

미완료/미검증 항목:

1. 실제 ModeId·표시 이름·EntryIntent·원래 계정 편성·허용 풀·대표 역할·PolicyRevision 소유자 및 14종 실제 ID가 필요하다. 기존 enum/preview ID/버튼 이름은 운영 프로토콜로 사용하지 않았다.
2. 실제 권한/성장 조회·입장 승인·서버 재검증·멱등 결과 조회·중앙 요청 보존·승인 후 기존 대기방 전달은 작업 2/3에서 연결·검증해야 한다. 현재 MultiplayerAccepted를 방 이동에 구독하지 않는다.
3. 실제 방 생성/찾기/초대 성공, 대기방 참가자 상태, 타임아웃/앱 복귀/중복 승인 처리는 이번 작업에서 실행하지 않았다. 기존 DEV 대기방을 운영 성공 증거로 사용하지 않았다.
4. 실제 기기 notch/터치, 실제 최장 모드명/능력/최대 숫자는 미검증이다. Editor 모의 영역과 표시 fixture 결과만 보고했다.

이번 결과는 화면과 현재 가능한 진입/반환 연결에 한정한다. 운영 문맥 연결 및 3단계 전체 입장 수용 완료를 뜻하지 않는다. 패키지 독립 HTML/JavaScript 검사는 이번에 실행하지 않았고, 패키지 작성자의 검사 결과를 현재 Unity/서비스 결과에 합산하지 않았다.
