# OVG04_EDITOR_PLACEMENT_CHECKPOINT_JOIN

실행: 2026-10-07, Unity 6000.3.8f1. **기존 Scene 제작기 → 저장 맵 → 기존 플레이어/캠페인 런 owner 연결 구현 및 자동 검증 완료.** 사용자 후속 지시에 따라 폐기 지형 요구를 제거하고 V6를 유일한 실행 정본으로 사용한다. 데스크톱 GUI 조작/포커스 복귀 및 실기기 검증 상태는 아래에 기록한다.

## 실제 정본과 연결

AGENTS.md, 현재 진행 보고서 §4, M7 결과 §2/5/6, M9B 결과 §3, OVG02 결과, 최신 소스 및 시작 시 Git 상태 529개 항목을 확인했다. 과거 보고서의 45타입/OBJ_SIDE_SPRING 팔레트는 현재 실행 정본과 다르다. 현재 활성 공통 장치는 기존 `CommonObstacleCatalog`의 9개 C 타입이며, 이전 enum과 지역 후보를 재활성화하거나 재번호화하지 않았다.

| 책임 | 실제 파일/타입 |
|---|---|
| 저장 정본 | `Assets/ANIMOL/Scripts/Core/StageMapDefinition.cs`, 기존 `StageMapObjectSettings`에 안전 발바닥 오프셋 추가 |
| 맵 선택 창 | 기존 `Scripts/Editor/CampaignMapEditorWindow.cs` — Scene 제작기 연결을 그대로 사용 |
| 포커스 작업대 | `Scripts/Editor/TerrainEditorV4/TerrainEditorSceneEditor.cs` — 기존 편집기에 `ANIMOL > Map Editor > Open Focused Workspace` 메뉴 별칭 복원 |
| 배치/선택/이동/삭제/복제/방향/속성 | 기존 `TerrainEditorObjectAdapter`, `TerrainEditorSceneEditor`, `StageMapObjectAuthoringOperations` 및 `StageMapAuthoringOperations` 계약 재사용 |
| 저장/Undo/백업/지형 검증 | 기존 `AnimolTerrainMapEditorBridge`, `AnimolTerrainPlacementEngine`, `StageTerrainStructureIntegration`, `CampaignMapBackupStore` |
| Save + Local Play | 기존 Scene 툴바 → `TerrainEditorSession.OpenForSceneTest` → `TerrainEditorScreen.EnterTest` → 저장 owner의 DEV 복사본 → `StageMapRuntimeLoader` |
| 마커 런타임 | 기존 `CampaignMapCheckpoint`, `CampaignMapBubblePickup`, `CampaignMapExit`, `CampaignMapDevSession` 확장. `CampaignMapMarkerGeometry`는 좌표/그림 보조 함수이며 별도 체크포인트 owner가 아님 |
| 런/방울/출구 판정 | 기존 `RunSessionController`, `CheckpointRespawnState`, `CheckpointBubbleCollectionState`, `BubbleObjectiveService`, `ExitGateController` |
| 플레이어/입력/카메라 | 기존 `DevPlayerController` 프리팹, `DevMobileInputRouter`, `DevMobileTouchControl`, `DirectionalCameraFollow`, `PortraitWorldCameraPolicy`, `ExitRevealCameraController` |
| 장애물/접합 | 기존 `CommonObstacleObject`, `ObstacleSnapshotAdapter`, `ObstacleVisualResolver`, `ObstacleRuntimeGraphics`, `FreeShapeTerrainRenderer`, `FreeShapeArtRegistry`, `ObstacleArtV1Builder`, `StageTerrainStructureRuntime` |

운영 `CampaignRuntimeBootstrap`은 수정하지 않았다. Local Play에는 캠페인 DEV session 하나를 연결하고 기존 시험용 `ObjectLabSession` 추가를 제거했다. `UnityTemporaryPlayer2D`, 임시 objective owner, 고정 createQAStage/QA JSON 로더를 설치하지 않았다. DEV 결과는 로컬 화면에 머물며 운영 Results/보상/진행도/Ready/ReleaseCandidate로 쓰지 않는다.

## stable type ID 매핑

| 그래픽 | 현재 저장 stable type ID | 기존 enum 숫자 | 런타임 owner |
|---|---|---:|---|
| C01 | C01 | 1001 | CommonObstacleObject |
| C02 LEFT/RIGHT | C02 | 1002 | CommonObstacleObject |
| C03 UP | C03 | 1003 | CommonObstacleObject |
| C04 상시 AIR 위험 | C04 | 1004 | CommonObstacleObject |
| C05 | C05 | 1005 | CommonObstacleObject |
| C06 | C06 | 1006 | CommonObstacleObject |
| C07 UP/LEFT/RIGHT | C07 | 1007 | CommonObstacleObject |
| C08 LEFT/RIGHT | C08 | 1008 | CommonObstacleObject |
| C10 | C10 | 1010 | CommonObstacleObject |

C09/1009는 삭제 진단용 예약 값 그대로이며 활성 목록에 없다. 옛 `SideSpring` enum 11과 `OBJ_SIDE_SPRING`을 C02로 자동 변환하지 않는다. START/CP/BUBBLE/EXIT는 각각 기존 enum 0/1/2/3, 인스턴스 StableId가 실제 체크포인트/방울/출구 ID다. 이전 지역 후보의 구현 수준/운영 제외 상태는 유지한다.

## 변경 동작

- 기존 4종 캠페인 마커를 Scene 팔레트에서 직접 배치한다. 새 패키지 start/bell/goal PNG를 아이콘·고스트·Scene·런타임에서 재사용한다. ZIP에 체크포인트 PNG는 없으므로 start 그림에 청색 tint를 적용하고 기존 마커 ID와 접촉/안전 위치 표시를 사용한다.
- PlayerStart와 체크포인트 안전 위치는 셀 원점 기준 발바닥이다. 실제 BoxCollider2D의 offset/크기/Transform으로 몸체 위치를 계산한다. 프리팹의 시각 mesh scale을 collider 크기로 혼동하지 않는다.
- 체크포인트 접촉 영역은 기존 footprint 설정에 저장하고 런타임 trigger에 반영한다. 안전 위치 오프셋도 같은 Settings에 저장한다. 접촉 영역, 발바닥, 몸체 영역을 Scene에서 표시한다.
- C02/C07/C08은 배치 전 방향 선택과 배치 후 속성 수정이 같은 데이터로 연결된다. 선택 장치 조회 누락을 수정해 이동/복제/삭제 경로가 타입을 찾는다. 복제는 새 GUID StableId이며 START/EXIT 중복은 기존 검증으로 거절한다.
- 안전 리스폰은 지지 바닥, 맵 경계, 실제 DEV 몸체 영역의 고체/위험/장치 겹침을 검사한다. 지형 Commit도 새 안전 위치 침범을 거절한다. 기존 맵의 이미 존재하는 오류는 숨기지 않되 무관한 수정에 새 오류처럼 재분류하지 않는다. Local Play는 전체 마커 안전 검사와 시작/출구 각 1개, 방울 후보 최소 3개를 요구한다.
- C02는 폭 4px의 앞 접촉판만 collider로 만들고 천장/바닥/전체 셀 몸체를 합성하지 않는다. C03은 독립 코일과 OneWay 발사판이다. 렌더·애니메이션 프레임은 collider/루트/저장 셀을 이동하지 않는다.
- C02 SIDE, C03 TOP으로 그래픽 분류를 변경했다. 지형 47마스크/4위상/전역 seed/스타일 경계와 3×3 셀·36개 문양 앵커 갱신 경로는 재사용한다. 같은 셀의 지형/장치 이중 소유는 기존 검증에서 거절한다.

## 체크포인트와 reset 경로

처음에는 활성 체크포인트 ID가 비어 있고 PlayerStart의 실제 몸체 위치를 사용한다. 유효한 접촉 시 기존 checkpoint/bubble 상태 owner에 기록한다. 같은 활성 체크포인트의 반복 접촉은 session에서 거절하므로 기록을 계속 덮어쓰지 않는다. 공유 `CheckpointRespawnState.TryActivate`의 다른 호출자 계약은 유지했다.

체크포인트 복귀는 기록 이후 획득 ID만 `CheckpointBubbleCollectionState.Respawn → BubbleObjectiveService.TryReset`으로 취소하고 해당 pickup을 다시 활성화한다. 기록된 방울은 유지한다. 고유 ID 중복 획득은 거절한다. 3개 미만이면 기존 ExitIneligible 이벤트가 RunSession의 자격을 철회하고, 추가한 session 연결이 출구 그림도 잠근다. 열림 연출은 완료 판정이 아니며 실제 Exit trigger 접촉이 필요하다.

Local Play의 하단 경계에 기존 `CampaignMapHazard`를 연결해 실제 Physics2D 추락 접촉이 같은 복귀 경로로 이어진다. 캠페인 씬의 경계는 바꾸지 않았다. 복귀 시 순간 장치 효과만 기존 player/motion 경로로 비우고 스태미나를 충전하거나 타이머를 재설정하지 않는다. 기존 main/escape/reveal 시간 정책을 유지한다. 출구 재잠금도 escape 타이머를 새로 채우지 않는다.

수동 **처음부터 / R**은 기존 `ReturnToEdit(false) → EnterTest`로 런/플레이어/맵 스냅샷을 새로 만든다. 같은 시드 선택 정책을 사용하며 체크포인트/방울/출구가 런 초기값으로 돌아간다. 공통 장치의 기존 `resetPolicy=MapReload`를 따라 C05·벨트·스프링과 stage VFX pool을 새로 구성한다. 체크포인트 복귀에서는 공통 장치를 전역 reset하지 않는다. 새 무한 시간/스태미나 회복 정책은 없다.

## 아트 버전과 입력 한계

입력 ZIP 전체를 `Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v3`에 보존하고 manifest 637개 SHA256/크기를 검증했다. 장애물 visualVersion=3, 393개 32px/PPU32 프레임, C05 조각20개 및 효과 데이터를 기존 Registry/atlas GUID 경로에 설치했다. Point/Uncompressed/mipmap Off/FullRect/physics shape 없음 및 원본 Sweetie16 픽셀을 유지한다.

**V6가 유일한 실행 지형 정본이다.** 사용자 후속 지시로 폐기 지형 버전 요구와 원본 대기를 제거했다. 장애물은 visualVersion=3이며 설치 카탈로그와 Registry의 terrainArtVersion은 모두 6이다. 사용하지 않는 sourceTerrainArtVersion 필드를 제거했다. 입력 ZIP과 그 manifest는 감사용 원본 바이트를 유지하며, 패키지 이름은 실행 지형 버전을 선택하지 않는다.

## DEV 맵과 실제 실행 결과

시험 맵은 `Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OVG04-JOIN.asset`이다. T01-S01을 덮어쓰지 않았다. 기존 Scene 편집기 `SelectPart → UpdateCandidate → Commit`으로 시작1, CP1, 후보/활성 방울3, 출구1, C02/C03/C05를 배치했다. 방울1 뒤 CP는 지지 바닥 위 (6,0), 하단 실제 추락 trigger와 바닥 구멍은 DEV 맵에만 있다. `Ovg04ValidationSetup`은 이 시험 구성과 캡처용 유틸리티이며 운영 제작기나 런타임 생성기가 아니다.

**V6 후속 최종 Unity 고유 테스트 111개 PASS — EditMode85 / PlayMode26.** 240개 픽셀 샘플은 EditMode 테스트 1개 안의 검수이며 테스트 개수에 더하지 않았다. 최종 실행 XML과 클래스별 숫자는 `Docs/Validation/OVG04/{editmode-v6-final.xml,playmode-v6-final.xml,final-test-summary.json}`이다. 앞선 84/26 실행 XML도 과거 검증 기록으로 유지한다.

| 실제 검사 | 결과/근거 |
|---|---|
| Scene handler 배치/이동/삭제/복제/방향 수정 → 저장/재열기, Undo/Redo, JSON 백업, 맵 전환 | Ovg04EditorJoinTests 7 PASS; 기존 제작기16 PASS |
| 안전 리스폰/지형 침범/시작 개수 제한/원본 owner 보존 | 신규 검증 및 기존 지형 통합13 PASS |
| CP 미접촉 실패, 방울1→CP→방울2→하단 trigger | 실제 Physics2D 접촉: 시작 또는 CP 복귀, 기록1 유지·이후 방울 복원 PASS |
| 기록1 상태에서 총3 수집→출구 개방→추락 | 집계1, Playing, 출구 그림 잠금, 출구 접촉 거절 PASS |
| 반복 방울/CP 접촉, 3개 수집/카메라 공개/실제 Exit 접촉 | 중복 거절, 연출만으로 완료되지 않음, 실제 접촉 후 로컬 Result PASS |
| 기존 Save + Local Play 화면 | TerrainEditorScreen의 실제 EnterTest로 저장 맵 복사본/동일 ID, controller1, CampaignMapDevSession1, ObjectLab0 확인 PASS |
| 수동 처음부터와 현재 맵 보존 | 동일 시드/슬롯, 새 player, CP없음/집계0/출구잠김/장치복구, 원본 맵 파일 불변 PASS |
| C02 앞면 좌우 반사/천장 없음, C03 아래 상승 통과/발사판 착지 | 실제 Physics2D 및 기존 공통 장치16 PASS |
| C05 점유 보호/이탈/붕괴/VFX 중복/복구/reset | 기존 CommonObstaclePlayModeTests 및 ObstacleV2LifecycleTests PASS |
| 모바일 swipe/jump/down, 9:16 카메라 및 공개 후 플레이어 복귀 | 실제 DEV router 호출, 기존 portrait 입력4 PASS. 실기기 터치 아님 |
| C02/C03 원본 픽셀 | 5테마×3방향 종류×8프레임×1×/8× = 240 비교, 차이0 |
| 연출 정책 | CommercialPolishPolicyTests 3 PASS; BlocksInput/시간 배율 정책 변경 없음 |

저장 맵 Editor 렌더와 실제 PlayMode 카메라 캡처는 같은 검증 폴더 PNG에 있다. 앞선 `scene-authored.png`와 PlayMode PNG는 offscreen/world camera 캡처다. 후속 `gui-*.png`는 실제 GUI Editor의 Scene/Game 창 렌더 버퍼이며 팔레트·속성·HUD·모바일 조작 UI를 포함한다. 운영 화면이나 실기기 사진으로 혼동하지 않는다.

## V6 후속 실제 GUI 검증과 보완

원본 복구 자료와 사용자 작업을 보호하기 위해 Assets/Packages/ProjectSettings/Tools 및 캐시를 임시 검증 사본 `C:/Users/user/AppData/Local/Temp/OVG04-V6-GUI`에 복사했다. Unity 6000.3.8f1 GUI Editor(Application.isBatchMode=false)를 실행하고 기존 Pipeline으로 제어했다. 새 GUI 시험 맵은 사본 안의 `DEV-OVG04-GUI.asset`이며 기존 DEV 맵의 복사본이다. 운영 맵을 변경하지 않는다.

- 실제 메뉴 `ExecuteMenuItem` 성공 → 기존 포커스 Scene 창 열기.
- `SceneView.SendEvent` 마우스 down/up으로 CP 배치, Ctrl+Z/Y Undo/Redo, Delete 및 삭제 Undo/Redo 성공. 실제 창의 기본 26pt toolbar를 반영한 좌표를 사용한다.
- 같은 Scene 창 마우스 drag로 C02를 (21,1)→(22,1) 이동하고 Undo/Redo, LEFT 속성 반영, Ctrl+S 저장·AssetDatabase 재열기 후 같은 StableId/좌표/방향을 확인했다.
- 실제 `Save + Local Play` 툴바 버튼 이벤트로 Unity Play Mode 진입. 방금 수정한 C02 (22,1)/LEFT 및 전체 ID가 런타임 DEV 스냅샷에 일치한다. 시작 발바닥 y=0, 몸체 x=.5, 이동 controller1/캠페인 objective owner1/ObjectLab0, 활성 방울3이다.
- 540×960 Game View에서 기존 swipe/jump/down router와 전체 세로 UI를 검증했다. 실제 Physics2D 접촉으로 방울1→CP→방울2→추락, 기록1 유지/이후 복원, 집계3 개방→추락→집계1/재잠금, 실제 Exit 접촉 클리어를 확인·캡처했다. 카메라 연출만으로 완료하지 않는다. UI `처음부터` 버튼 후 초기 상태와 동일 seed/슬롯을 확인했다.
- UI `편집 복귀` 버튼으로 Play 종료 후 **같은 Scene 창 ID -5374, 같은 선택 맵, 같은 저작 JSON, 실제 창 포커스**가 모두 복원됐다.

GUI 검증에서 발견한 네 가지 문제를 수정했다. 선택 시 나타난 정보 패널이 진행 중인 드래그를 취소하지 않도록 canvas capture를 유지한다(회귀 테스트 추가). Scene 마커 팔레트는 런타임과 같은 Sprite/tint 아이콘을 표시한다. Local Play의 offscreen 카메라와 Canvas 사이에 cullingMask=0 화면 출력 카메라를 연결해 Game View의 `No cameras rendering` 가림을 제거하고 세로 테스트 글자 크기를 조정한다. Play 종료/컴파일 직후 Unity가 아직 준비되지 않은 경우 다음 ready update에서 기존 Scene 작업대를 복구한다. 플레이어 카메라·런/입력 owner는 추가로 복제하지 않는다.

GUI 결과는 `gui-{menu,placement,drag-save,save-local-play,runtime-portrait,physics-checkpoint,return-focus}-v6.json`, 전체 UI는 `gui-scene-v6.png`, `gui-mobile-full-v6.png` 및 상태별 PNG에 있다. GUI 조작은 자동으로 보낸 실제 Editor/UI 이벤트이며 사람의 물리 마우스 조작은 아니다. 캡처는 [Unity의 창 렌더 버퍼 API](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/InternalEditorUtility.cs)를 사용한다. 숨김 창에 대한 초기 화면 좌표 캡처는 다른 창을 읽어 즉시 삭제했고 결과물이나 커밋에 포함하지 않았다.

초기 실행의 fixture 오류, 0.01unit 위치 비교 오차, 이전 FULL 스프링 기대값과 지지 바닥 없는 marker fixture를 수정하고 재실행했다. 최초 추가 M7CampaignMapRunTests 검사에서는 4/5 PASS, `T01S01HasM8ProductDataButRemainsOperationallyBlockedWithoutHumanRun`이 현재 사용자 맵의 빈 고정 동물 목록 때문에 실패했다. 현재 T01-S01을 고치거나 이 실패를 숨기지 않았다. 초기 XML도 보존한다.

## 미실행/남은 항목

- 모바일 실제 기기 터치·네트워크·운영 전체 맵/모든 동물 완주·보상/진행 연동: 미실행. 이번 연결 검증은 DEV와 실제 Unity 세로 미리보기 범위이며 운영 승격은 수행하지 않았다.
- 실제 UI를 통한 안전 위치/접촉 영역 사람 검수: 자동 검사와 Scene 표시까지만 구현/검증.

## Git 작업 경계

시작 시 기존 미커밋 변경은 보존했다. 이번에 만든 파일과 작업 전 HEAD와 같았던 변경 파일만 직접 스테이징한다. 이미 dirty였던 `CampaignRunRules.cs`는 이번 생성자 변경 두 줄만 HEAD에 투영해 기존의 미커밋 M7 추가 코드를 커밋에 섞지 않는다.

작업 시작부터 untracked였던 `CampaignMap{BubblePickup,Checkpoint,DevSession,Exit}.cs`, `ExitRevealCameraController.cs`는 원파일에 변경을 적용한 상태로 작업 폴더에 남긴다. 그 전체 기존 구현은 이번 커밋에 편입하지 않고 **OVG04 변경분만 `Docs/Validation/OVG04/preexisting-owner-integration.patch`로 커밋**한다. 원본/수정 SHA256은 `owner-patch-boundary.json`에 있다. 현재 작업 폴더에서 `git apply --reverse --check --ignore-space-change`로 적용 상태를 확인했다. 이 커밋은 기존 미커밋 M7 owner가 없는 새 checkout만으로 전체 프로젝트를 재현한다는 의미가 아니다.

스테이징 경계/초기 상태/변경 파일 목록은 같은 폴더 JSON/TXT에 기록한다. 임시 테스트 맵·캐시·전체 Unity 로그를 커밋하지 않는다. 커밋 해시/제목은 최종 작업 보고에 기록한다.

원본 v3 패키지는 `.gitattributes`의 `-text`로 줄바꿈 변환을 차단했고, Git index의 manifest 637개 파일도 ZIP과 바이트 단위 SHA256이 일치한다(`staged-package-verification.json`).
