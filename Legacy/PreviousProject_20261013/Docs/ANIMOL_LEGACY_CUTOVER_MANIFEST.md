# ANIMOL legacy cutover — 2026-10-13

플랫포머 파일을 참조 관계에 따라 분리하는 작업이다. 탑뷰 퍼즐 구현이나 가상의 P01 씬을 만들지 않는다.

## 실제 조사

- 최초 인벤토리: Assets 파일 4,989개, GUID 2,632개, asmdef 52개. 중복 GUID 0개.
- `git status --porcelain=v1 -z --untracked-files=all` 기준 기존 변경 1,736개(이번 작업의 조사 도구 제외). 미추적 폴더를 펼친 파일 수이므로 `git status --short`의 행 수와 다르다.
- 탑뷰/스와이프 시제품과 P01 QA 씬은 실제 파일 검색에서 발견되지 않았다.
- Phase1은 동물 업그레이드 UI로 존재한다. `UI/AnimalUpgradePhase1` 및 `AnimalUiV2`의 실제 공용 UI로 분류하고 유지한다.
- Unity 6000.3.8f1 배치 모드의 작업 전 재임포트·컴파일 성공.

## 이동과 유지

정확한 원본→대상 경로, GUID, 유지 이유, 차단 파일은 [기계 판독 인벤토리](ANIMOL_LEGACY_CUTOVER_INVENTORY.json)를 정본으로 한다.

- 보관 정본: `Legacy/Platformer_20261013/<원래 상대 경로>`.
- 원래 파일명은 보관본에서 유지한다. 파일·인접 `.meta`를 함께 이동하고 폴더 `.meta`도 같은 계층에 보존한다.
- 플랫폼 전용 후보 가운데 활성 파일의 GUID, C# 타입 또는 명시 경로 참조가 남는 항목은 이동하지 않는다. UI·서비스의 계약 타입, 데이터와 연결된 런타임은 유지 이유를 인벤토리에 기록한다.
- 런 프로토타입, 참조가 분리된 전용 코드·맵·아트·테스트, Gameplay/MapDevTest/MapAuthoringWorkspace 및 전용 실험 씬을 보관한다.
- 공용 UI, 서비스, Bootstrap/Lobby/Results, Typography, 메인 UI V7 배경 패키지는 유지한다. `ThemeRuntime_T01`은 외부 경로 참조 때문에 파일 자체는 유지하지만 빌드 목록에서 제외한다.
- `MainUiV7`의 배경 20개 레이어와 토끼 시트 2개는 기존 기준 패키지에 남는다. 폐기 배경을 가져오지 않는다.

## Unity 이동 절차

`AnimolTopdownCutoverBatch.Execute`는 사용 중인 자산을 `AssetDatabase.MoveAsset`으로 이동하고 GUID 일치를 확인한다. 저장소 루트는 Unity 자산 경로가 아니므로 보관 자산은 Unity 내부 임시 경로로 이동한 뒤 파일과 `.meta`를 루트의 Legacy로 내보낸다. 완료 후 임시 자산 폴더를 제거하고 재임포트한다.

공용 UI 스프라이트는 `AssetDatabase.MoveAsset`으로 `Assets/ANIMOL/Sprites`로 직접 이동한다. 기존 프리팹·씬은 GUID 참조를 유지한다. 소스 패키지의 manifest와 sprite 키는 호환 입력이며, 실제 정본 경로는 `sprite_path_manifest.json`에 기록한다. 빌더의 계산 경로는 `CanonicalSpritePaths.Resolve`로 변환한다. Resources의 기존 프리팹·카탈로그 키는 바꾸지 않는다.

`ProjectSettings/ANIMOLNamedArtReferences.json`도 갱신한다. 보관 자산의 Sources/Importers/Containers와 해당 캐시 슬롯을 제거하고, 이관한 UI의 Path/FileName/Key/Name 및 연결 슬롯 키를 실제 임포트 자산 이름에 맞춘다. 변경 전 사용자 인덱스는 Legacy의 같은 ProjectSettings 경로에 보존한다. 실제 씬·프리팹 슬롯은 수정하지 않는다.

## 구형 진입점

- 맵 제작기 클래스와 메뉴/자동 복구/실험 씬 진입점은 활성 Assets에서 보관 위치로 이동한다.
- 남는 `UiBuildPipeline` 부분 클래스는 보관 위치 주석과 `#if ANIMOL_LEGACY_MAP_EDITOR`로 차단한다. 해당 define은 기본 설정에 추가하지 않는다.
- 로비에서 기존 Gameplay/MapDevTest/캠페인 씬으로 넘어가는 코드는 동일 define 안에 둔다. 기본 빌드에서는 준비 중 메시지로 종료한다.
- 활성 빌드 목록에서는 보관 씬과 ThemeRuntime 씬을 제외한다. 기존 사용자 씬 목록은 `Legacy/Platformer_20261013/ProjectSettings/EditorBuildSettings.asset`에 보존한다.
- define만 켠다고 보관 자산이 복원되지는 않는다. 구형 제작 환경 복원은 별도 브랜치에서 필요한 Legacy 파일을 원래 경로로 복원하고 참조·빌드를 검증하는 작업이다.

## 기존 작업과 커밋 경계

파일 이동은 현재 작업 트리의 내용을 그대로 보존한다. 기존 수정 파일의 이동을 커밋할 때에는 원래 Git 인덱스의 blob을 대상 경로로 옮겨 기존 수정 내용 자체가 이번 커밋에 섞이지 않도록 한다. 수정 내용은 새 경로의 미커밋 변경으로 남는다. 이전부터 미추적이던 파일은 이동 후에도 미추적 상태를 유지한다. 원래 상대 경로는 이동 인벤토리에서 추적한다.

새 차단/경로 변경은 원래 HEAD에도 동일 변환을 적용해 스테이징한다. 외부 작업 저널에는 변경 전 파일이 보존되며, 최종 인벤토리는 이동한 원본 내용과 `.meta`, 비대상 파일, 차단 내부 내용의 일치를 검사한다.

최종 실행 검증과 제한은 [작업 결과](../MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md)에 기록한다.

## 추가 지시 기준 실측 분류

추가 작업의 기준 커밋은 `87725aaa`이며 Unity는 `6000.3.8f1`, 활성 타깃은 Android다. 루트는 `C:/Users/user/Documents/GitHub/ANIMOL`이다. 추가 작업 시작 Git 상태·인덱스·기존 파일 해시는 별도 작업 저널에 보존했으며 `git reset/clean`을 사용하지 않았다.

현재 시작 씬은 `Assets/ANIMOL/Scenes/Bootstrap.unity`다. 활성 빌드 씬은 Bootstrap/Lobby/Results이며 기존 제작기·실험 씬을 등록하지 않는다. asmdef 전체 내용, 빌드 설정, 분류마다 경로·GUID·참조자·조치 이유는 [현재 분류 정본](ANIMOL_CUTOVER_CLASSIFICATION.json)에 기록했다. 이전 이동 GUID·원래 경로는 [최초 이동 인벤토리](ANIMOL_LEGACY_CUTOVER_INVENTORY.json)에도 보존한다.

| 분류 | 기록 수 | 조치 |
|---|---:|---|
| `platformer-only` | 654 | 기존 620개 이동, 생성기 원본 17개 추가 사본과 활성 차단 파일 17개 포함 |
| `shared-or-still-referenced` | 1292 | UI·서비스·GUID/C# 타입·Resources/주소 참조를 따라 유지 |
| `topdown-puzzle` | 0 | 정확한 Phase1 루트와 P01 QA 씬 없음 |
| `unknown` | 391 | 판정 보류, 파일 유지 및 컴파일 검사 |

기록 수는 활성 파일과 Legacy 파일/사본 기록을 합친 수다. 같은 원본의 활성 차단 파일과 Legacy 사본은 별도 기록이다. `.meta`와 폴더는 GUID 검사에 포함하며 분류 행과 따로 집계한다. C# 참조는 주석·문자열을 제거한 타입 토큰 후보 분석으로 보수적으로 추적하며 모호한 항목을 삭제하지 않는다.

과거 Campaign/Stages는 T01~T05 각각 20개, 총 100개 슬롯이다. 점프·활강·스태미나·동물 전환 관련 `DevPlayerController`, `DevTestSession`, `MobileGameplayControls`, 동물 정의와 MapObjects는 공유 계약·HUD·캠페인 데이터의 참조를 따라 유지한다. 방울 서비스는 신규 게임에도 적용할 수 있는 별도 서비스이며 플랫폼 동작과 같은 파일로 추정하지 않는다. UI/AnimalUpgradePhase1은 실제 계정/동물 업그레이드 화면이고 `Assets/ANIMOL/Phase1`은 존재하지 않는다.

UiBuildPipeline 부분 클래스 17개는 과거 UI/맵/실험 씬을 함께 재생성하는 비활성 bulk generator다. 현재 UI 런타임이나 MainUiV7Builder의 외부 호출자가 없으며 원본을 Legacy의 동일 경로에 보존했다. 현재 UI 렌더러·서비스·MainUiV7Builder는 유지한다. 파일 전체가 Editor 생성 파이프라인인 이 그룹에만 전체 조건부 컴파일을 유지하며, CampaignUiPresenter는 이미 분리한 과거 씬 호출 함수 본문만 조건부 컴파일한다.

CampaignUiPresenter의 과거 호출부를 복원할 수 있는 원본 사본도 `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs`에 보존했다. 활성 공용 UI는 그대로 두고 세 함수의 호출 본문과 보관 위치 주석만 유지한다.

### 보관 생성기 원본과 비활성 지점

| 활성 파일 | GUID | 원본 사본·조치 |
|---|---|---|
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.CommercialPolish.cs` | `3adc2bbdcb3c67b42a9e84237c6fecdd` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.CommercialPolish.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M1.cs` | `5438d3026339fa443a2261a5d75be19f` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M1.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M2.cs` | `a8011d4264b5bf34ebaf2e15141b8dde` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M2.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M3.cs` | `e1b2ab5ab7a992c42a7b5ac252746d52` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M3.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M4.cs` | `803c0d9b6533ab94d9989c6c3fd1ce4a` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M4.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M5.cs` | `1044285e066a35b499c7d9b5a2e6beb3` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M5.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6.cs` | `9e39c565c9f5e114c892e879f193abd5` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Economy.cs` | `369d9732820528149b256299326834ee` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Economy.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs` | `bddbc5d474b4fba45a9d4cda25f00fbc` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M7.cs` | `c5433766719e8684686b28164d28994b` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M7.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M8.cs` | `3920883086894b4499d1e91a5921bf95` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M8.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9.cs` | `39265a1c500a9a745975f551fb81eec0` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9BThemePlatforms.cs` | `607c29c9f06fc5947bac074224ab9d38` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9BThemePlatforms.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9CMoonObjects.cs` | `4dd618bd9cb568f40b51364a6b78a585` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9CMoonObjects.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9Objects.cs` | `686229f9c592e474f813cac92785770c` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9Objects.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9Portrait.cs` | `641b5bf4c84d51141a42f56d0c9a8cdf` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M9Portrait.cs`; LEGACY 주석 + 기본 비활성 define |
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.cs` | `2754f021c50c35042849f413532fbe8a` | `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.cs`; LEGACY 주석 + 기본 비활성 define |

### 공용 유지 증거

| 파일 | GUID | 직접 참조자 예시·이유 |
|---|---|---|
| `Assets/ANIMOL/Scripts/Core/BubbleObjectiveService.cs` | `f9a0335416475a24dba6953af9833b10` | `Assets/ANIMOL/Scripts/Core/ExitGateController.cs`, `Assets/ANIMOL/Scripts/Gameplay/CampaignMapDevSession.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevTestSession.cs`, `Assets/ANIMOL/Tests/EditMode/CampaignCoreTests.cs` |
| `Assets/ANIMOL/Scripts/Core/CampaignStageDefinition.cs` | `5c40b45fcda5c004cba1ddfc13879780` | `Assets/ANIMOL/Data/Campaign/Stages/T01-S01.asset`, `Assets/ANIMOL/Data/Campaign/Stages/T01-S02.asset`, `Assets/ANIMOL/Data/Campaign/Stages/T01-S03.asset`, `Assets/ANIMOL/Data/Campaign/Stages/T01-S04.asset` |
| `Assets/ANIMOL/Scripts/Gameplay/DevPlayerController.cs` | `524c0fdd6a02d754187377de2e00bfa8` | `Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab`, `Assets/ANIMOL/Scripts/Development/TerrainEditorV4/TerrainEditorTestMode.cs`, `Assets/ANIMOL/Scripts/Gameplay/CampaignMapBubblePickup.cs`, `Assets/ANIMOL/Scripts/Gameplay/CampaignMapCheckpoint.cs` |
| `Assets/ANIMOL/Scripts/Gameplay/DevTestSession.cs` | `d7db835d081664b45a3b9d5b1f97474f` | `Assets/ANIMOL/Scripts/Gameplay/DevBubblePickup.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevCheckpoint.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevExitGateView.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevPlayerController.cs` |
| `Assets/ANIMOL/Scripts/Gameplay/MobileGameplayControls.cs` | `b4f010f4e5a42954cac20e1db853365e` | `Assets/ANIMOL/Scripts/Development/TerrainEditorV4/TerrainEditorContract.cs`, `Assets/ANIMOL/Scripts/Development/TerrainEditorV4/TerrainEditorTestMode.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevMobileInputRouter.cs`, `Assets/ANIMOL/Scripts/Gameplay/DevMobileTouchControl.cs` |
| `Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs` | `a439540e4894dc84b9ce2bd54c2eb5e0` | `Assets/ANIMOL/Scenes/Lobby.unity`, `Assets/ANIMOL/UI/AnimalUpgradePhase1/Tests/PlayMode/UpgradePhase1AcceptanceTests.cs`, `Assets/ANIMOL/UI/ProductionV1/ProductionController.cs` |
| `Assets/ANIMOL/Scripts/UI/UiNavigationService.cs` | `3749c30322905134db4c32466b348dd2` | `Assets/ANIMOL/NetUiDev/Project/NetUiProjectEntry.cs`, `Assets/ANIMOL/NetUiDev/Project/NetUiRoomController.cs`, `Assets/ANIMOL/NetUiDev/Tests/PlayMode/NetUiAdmissionHttpTests.cs`, `Assets/ANIMOL/NetUiDev/Tests/PlayMode/NetUiProjectBindingFlowTests.cs` |
| `Assets/ANIMOL/UI/FiveThemeMenuMaps/Editor/MainUiV7Builder.cs` | `7ff37acbc9e581b409911695fbdfbb4b` | `Assets/ANIMOL/UI/FiveThemeMenuMaps/Editor/MenuHierarchyBuilder.cs` |

현재 unknown 전체 경로와 참조자는 분류 정본에서 `category == unknown`으로 조회한다. 해당 파일을 이번 작업에서 옮기거나 승인된 탑뷰 콘텐츠로 표시하지 않는다.
