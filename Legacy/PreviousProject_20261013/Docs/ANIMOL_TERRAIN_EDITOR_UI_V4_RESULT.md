# ANIMOL Terrain Editor V4 구현 결과

2026-10-05 · Unity **6000.3.8f1** · 작업 폴더 `C:/Users/user/Documents/GitHub/ANIMOL`

## 최종 작업 방식

사용자의 마지막 요청에 따라 V4 문서의 Game View 편집 요구를 **Edit Mode의 Scene View 편집**으로 변경했다. 새 화면은 맵 캔버스, 하단 두 줄 실루엣 팔레트, 배치·선택·삭제, 좌표/객체 설정, 저장·Undo·Redo를 한 Scene View에 제공한다. **Game View는 시험 플레이에만 사용**한다.

- 진입: `ANIMOL > Map Editor > Open Scene Editor V4` (`Ctrl+Shift+M`). Project에서 StageMapDefinition을 선택하거나 기존 맵 선택 창의 **새 맵 제작기 열기**를 사용한다.
- 기존 CampaignMapEditorWindow는 맵 선택/진입만 담당한다. 기존 authoring 패널과 그 창의 Scene GUI 편집 구독은 제거했다. 기존 저장 API 및 호출 호환용 코드는 유지한다.
- 씬 Transform은 저장 데이터가 아니다. 별도 preview scene의 렌더 전용 복제 그림이며, 사용자 씬을 열거나 저장하지 않는다.
- 시험 플레이 버튼은 저장·구조 검증 후 임시 개발 Play 세션을 시작한다. **편집 복귀**는 Play Mode를 종료하고 선택·필터·카메라를 복원한다.
- 개발 씬과 검증용 `DEV-UI-V4` 맵은 운영 진입점/빌드 목록에 추가하지 않았다.

## UI01 → UI04 구현

| 단계 | 실제 구현 |
|---|---|
| UI01 | GUID로 정본 asset을 찾는 Editor adapter, 단일 편집 화면, 맵별 바인딩과 해제, domain reload 복구. 최종 화면은 Scene View로 전환. |
| UI02 | 원본 60부품, 기존 registry 45객체, 기존 논리 marker 6종. 테마·분류·A/B/C/D·검색, 두 줄 팔레트, 높이 변경/접기, 선택 컬러 표시와 실루엣. |
| UI03 | 원본 Sprite ghost, 정수 스냅, 마스크·원점·불가 사유, 클릭 배치, 전체 선택/이동/삭제, 좌표 입력, 객체 설정, 네이티브 Undo/Redo, 저장. Space+drag 및 Scene View pan/zoom. |
| UI04 | 기존 loader/physics owner를 사용하는 시험 세션, 기존 플레이어·모바일 입력·portrait camera·객체 reset, 편집 잠금, 복귀 및 미리보기 수명 관리. |

팔레트의 105개 아트 미리보기는 실제 frame/prefab SpriteRenderer를 렌더해 생성하며 ID+asset dependency hash로 캐시한다. 원본 Material의 alpha clip/exclusion을 적용한다. prefab의 게임 스크립트·콜라이더는 미리보기에 생성하지 않는다. 추가 6개 marker에는 원래 Sprite가 없어 진단 표시를 제공한다.

## 실제 연결 경로와 저장

```text
CampaignMapEditorWindow / Scene Editor V4 메뉴
  → TerrainEditorSceneEditor (Edit Mode SceneView)
  → TerrainEditorAdapter (asset GUID → 기존 StageMapDefinition)
  → ReadTerrain → AnimolTerrainPlacementEngine
  → AnimolTerrainMapEditorBridge.Commit
  → StageTerrainStructureIntegration.Validate / Write
  → StageMapDefinition.EditorApplyTerrainCandidate

객체 → TerrainEditorObjectAdapter
     → StageMapObjectAuthoringOperations / 기존 marker authoring API

시험 → TerrainEditorSession → TerrainEditorScreen.EnterTest
     → 정본의 일회성 clone → StageMapRuntimeLoader.Load
     → StageTerrainStructureRuntime / 기존 operational terrain owner
     → 기존 DevPlayerController + ObjectLabSession
```

**저장 schema 변경 없음.** UI V4는 UI 작업 버전이며 terrain schema/catalog version은 **3**이다. 기존 placement의 instanceId/catalogId/version/hash/theme/style/origin/orientation을 유지한다. 기존 terrain cell 필드와 tileId/variant/9방향 값, unknown raw/settings 보존 경로를 재사용한다. UI 카메라·선택·검색은 SessionState이며 맵 asset에 넣지 않는다.

후보 검사에는 엔진 점유 규칙과 호스트 bounds/객체 footprint/swept path 검사를 함께 사용한다. 변경 전 백업·Undo·기존 authoring API·asset 저장을 유지한다. 연결 객체의 생성/연쇄 삭제에는 authored-change batch를 적용해 revision이 한 번 증가한다. 변경 후 derived collision revision을 무효화한다.

실패 주입 검사에서 발견한 복원 문제를 수정했다. Bridge가 transaction의 owner/callback을 고정하고, 실패 시 Undo와 저장 snapshot으로 메모리/디스크를 복원한다. 중간 rollback에 일반 Undo preview callback이 재진입하지 않게 한다. 객체 transaction도 동일하게 복원한다. `changed`는 그림/캐시 재생성만 한다.

시험 카메라는 편집 렌더 layer를 제외한다. 기존 workspace의 `[MAP PREVIEW - NOT SAVED]` 루트가 일반 씬 목록 밖에 남는 경우도 별도로 일시 정지/복원한다. 시험 월드에 편집용 그림이 섞이지 않게 실제 화면으로 확인했다.

## 변경 파일

- `Assets/ANIMOL/Scripts/Editor/TerrainEditorV4/`: 새 Scene 편집기, adapter, 객체 adapter, silhouette cache, 시험 세션/서비스/단축키 연결 7개 C# 파일.
- `Assets/ANIMOL/Scripts/Development/TerrainEditorV4/`: render-only art, UI 계약/화면/팔레트/입력/격자/설정, 시험 모드 8개 C# 파일. UnityEditor 참조 없이 기존 Runtime assembly에 포함된다. 최초 Game View 편집 구현의 공용 UI 코드는 시험 화면의 기반으로 남으며 사용자 진입점은 Scene 편집기다.
- `StageMapDefinition.cs`, `StageMapDefinition.TerrainStructures.cs`: batch revision과 derived cache 무효화 API.
- `CampaignMapEditorWindow.cs`, `.TerrainStructures.cs`: 새 Scene 진입점과 바인딩 소유권 보호.
- `StageMapAuthoringWorkspace.cs`: V4 세션 중 이전 Scene handle/delete 입력의 중복 처리 차단.
- `StageMapObjectAuthoringOperations.cs`: 검증된 객체 설정 저장, 기존 종류별 초기 path/settings 제공.
- `StageTerrainStructureIntegration.cs`: 호스트 후보 검증을 hover/commit에서 공유.
- `StageMapRuntimeLoader.TerrainOwner.cs`: 개발 시험의 일반 셀 전용 맵에도 기존 operational owner 생성.
- `TerrainStructure/Editor/AnimolTerrainMapEditorBridge.cs`: 실패 시 asset/preview 복원 및 Undo 재진입 처리.
- `TerrainStructure/Shaders/AnimolSourceArtSweetie16.shader`: ghost 전용 `_PreviewOpacity` 추가, 기본값 1. 원본 clip·palette·PNG는 변경하지 않음.
- `Tests/EditMode/TerrainEditorV4Tests.cs`: 14개 Unity EditMode 검사.
- `Scenes/Development/TerrainEditorV4.unity`, `Data/Development/TerrainEditorV4/DEV-UI-V4.asset`: 개발 시험 씬/검증 fixture와 meta.
- 이 보고서, 검증 JSON, 아래 작동 화면과 meta.

## 실행한 검증

모두 실제 프로젝트의 Unity/C# 실행 결과이다. Python reference model을 사용하지 않았다. 런타임과 Editor 코드가 각 기존 asmdef 경계에서 컴파일되며 최종 C# compile error는 **0**이다.

| Unity 테스트 | 결과 |
|---|---|
| TerrainEditorV4Tests | 14 / 14 PASS |
| TerrainStructureIntegrationTests | 13 / 13 PASS |
| CampaignCoreTests | 22 / 22 PASS |
| M7MapEditorTests | 11 / 11 PASS |
| M7CampaignMapRunTests | 5 / 5 PASS |
| CommercialPolishPolicyTests | 3 / 3 PASS |
| TerrainStructurePlayModeTests | 1 / 1 PASS: 내부 반복으로 5테마 × 주요 4부품 접지/점유/아트 Collider 없음 검사 |

`ANIMOL/Terrain Structure/Validate Saved Map Contract` 메뉴를 실제 실행했다. Unity 로그: `ANIMOL saved-map contract checks PASS: 5 real JSON round trips and pure engine transactions.` 이 메뉴 자체는 물리/Shader/PlayMode 검사가 아니므로 위 PlayMode와 화면 검사를 별도로 수행했다.

포함된 계약 검증: 60 frame/40 stamp, Structure 40개의 셀 collider 점유, Fill 20개의 추가 충돌 0/받침/삭제, T01_D의 12×5·52고체·8빈 셀, T03_D 닫힌 바닥, 음수 청크/경계跨 배치, chunk unload owner 수명, # 부분 편집 거절과 . 셀 칠하기, 전체 이동/삭제, 장식 cascade Undo, 잘못된 theme/ID/hash/version/orientation/owner 거절, 기존 variant/raw/객체 보존, 저장 재import/reopen/runtime round trip.

실제 SceneView 입력(`EditorWindow.SendEvent`)과 Game View 버튼(`ExecuteEvents`)으로 별도 검증했다:

- `(-1,-1)` 배치: mouse-down revision 49 유지, mouse-up 50. 생성 ID `cb769f80644748909612812097a71a13`.
- Ctrl+Z: 50 → 49, Ctrl+Y: 49 → 50. 각각 placement 한 개 변화.
- 전체 이동: drag 중 revision 51 유지, drop 52. 같은 ID가 `(7,-1)`로 이동. 삭제 53, Undo 52에서 같은 ID/위치 복원.
- Esc로 진행 중 이동 후보 `(8,-1)` 취소: hash/revision 불변, hot control 0. Space pan과 wheel zoom도 정본 불변.
- 저장 후 다시 열기/domain reload 연결 복원. 시험 전후 hash/revision 52 및 선택 ID/카메라 유지.
- 시험에서 기존 플레이어를 구조물 위에 낙하시켜 실제 접지 확인: `(10,4.61)`, 접지 True, operational `Terrain` collider. 완성 instance 3개, 기존 one-way effector 1개, 아트 자식 Collider 0인 기존 계약 유지.
- UI 화면은 Unity 실제 렌더/GUI 버퍼를 캡처했다. Windows computer-use helper는 연결되지 않아 Unity Editor 캡처 API를 사용했다.

## 기존 작업 보존

작업 시작 git status를 기록하고 이번 작업의 파일만 커밋했다. 사용자 기존 수정, 폰트 생성물, 다른 맵/프리팹/로비/UI/보상 변경을 되돌리거나 함께 스테이징하지 않았다.

- 시작 시 T01-S01: 전체 cell **134**, 객체 **10**, placement **0**, revision **199**. 보고서의 과거 셀 수로 강제 복원하지 않았다.
- 시작 시 T01-S02: cell **22**, 객체 **8**, placement **5**, revision **171**. 기존 `WorldUnitsPerCell=0`도 그대로 보존했다. 편집은 1unit fallback을 사용하며 시험 시작에는 맵 설정의 유효한 셀 크기가 필요하다.
- 기준에 기록한 기존 맵 **14개 모두 content hash/revision 불변**.
- 원래 `MapAuthoringWorkspace.unity` 유지, scene dirty=False. SourceArt 5장, catalog v3 및 mask는 변경하지 않았다.
- 신규 `DEV-UI-V4`만 배치·이동·삭제·시험 검증에 사용했다. 운영 맵이나 운영 Ready/보상 상태로 승격하지 않았다.

## 화면과 범위

- [최종 Scene 편집 화면](../Assets/ANIMOL/Screenshots/TerrainEditorV4/SceneEditor.png): 기존 T01-S02를 읽은 상태.
- [원본 다리 ghost와 마스크](../Assets/ANIMOL/Screenshots/TerrainEditorV4/SceneGhost.png): DEV fixture의 음수 원점, 고체/빈 셀 표시.
- [선택과 좌표 편집](../Assets/ANIMOL/Screenshots/TerrainEditorV4/SceneSelection.png).
- [실제 시험 플레이](../Assets/ANIMOL/Screenshots/TerrainEditorV4/SceneTest.png): 기존 개발 플레이어 사용.
- [600×850 작은 Scene View](../Assets/ANIMOL/Screenshots/TerrainEditorV4/SceneNarrow.png): 상단 도구 두 줄 재배치 확인.

시험 세션은 기존 ObjectLab/물리/입력을 사용하는 개발 시험이다. 캠페인 전체 클리어·결과 지급·운영 Ready 승인 흐름을 실행하는 화면은 아니다. 해당 정책은 위 기존 단위 테스트로 회귀 확인했고 실제 보상 지급이나 사람의 완주 승인은 수행하지 않았다. 모바일 기기 빌드/터치 실기기 QA, 모든 객체 45종의 개별 기믹 플레이, 모든 해상도의 시각 QA는 수행하지 않았다. 원본 아트의 별도 재디자인 및 마스크 catalog version 변경도 이 작업에 포함하지 않았다.

상세 실행 결과는 [검증 기록](ANIMOL_TERRAIN_EDITOR_UI_V4_VALIDATION.json)에 보관한다.
