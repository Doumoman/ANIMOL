# 별길 목장 연결·여러 칸 장식 적용 결과

## 실제 호스트 조사

| 항목 | 확인한 실제 경로와 동작 |
|---|---|
| 테마 | `PuzzleMapEditor.ThemeId`의 `starway`; `StarGarden.asset`과 `starway_Theme.asset`이 같은 `StarwayRanchArt`를 참조한다. 두 테마에 새 catalog와 manifest 핀을 직렬화했다. |
| 장식 ID | `StarwayRanchArt.Find("props/<id>.png")`가 V12 Sprite를 반환한다. `PuzzleMapEditor.DecorationChoices`는 이 ID의 큐레이션 목록만 표시한다. PNG 파일명을 gameplay ID로 직접 사용하지 않는다. |
| 저장 데이터 | `PuzzleMap.decorations`의 `DecorationCell`은 앵커 한 개를 저장한다. 새 `placementId`, `connectionGroup`, `logicalLayer`, `heightKey`, 새 1칸 브러시의 `visualFamily`를 더했다. 전체 지면 예약은 매 snapshot에서 앵커와 확인된 footprint로 계산한다. |
| 제작기 | `PuzzleMapEditor.Paint` → `TryPlaceDecoration`/`TryEraseDecoration`. 전체 후보 칸을 검사한 후 한 앵커만 확정하며, 안쪽 칸을 지워도 placement 전체를 삭제한다. Undo 콜백은 장식 갱신 이벤트를 보낸다. |
| 게임 표시 | `PuzzleController.Load`/`PuzzlePrototypeController.Load` → `PuzzleMapView.Bind` → `Rebuild` → `RanchConnectedDecorV01Controller.Configure`/`SetLayout`. 최신 장식은 controller만 그린다. 옛 `PuzzleMapView.Sprite`의 bounds 기반 `.8`칸 자동 축소를 통과하지 않는다. |
| 청크·이웃 | `StarwayRanchMapRenderer`는 전체 지형에서 V12 바닥과 `bridge_decor`를 계획하고 화면 청크를 표시한다. 연결 장식 Planner는 화면 청크가 아닌 `PuzzleMap.decorations` 전체를 읽는다. |
| 통행·충돌 | `PuzzleMap.Walkable`, `PuzzleSession`, `PuzzleDeviceSession`은 장식 예약을 읽지 않는다. 새 `RanchDecorPlan` claims는 시각 장식 예약이며 collider나 게임 점유를 만들지 않는다. |
| 로드·풀·삭제 | `Load`가 맵을 복제해 새 View를 바인딩한다. 장식 전용 Addressables/Resources/Prefab 풀 경로는 없다. `Rebuild`는 옛 `objectsRoot`를 즉시 비활성화하고 지연 Destroy 전에 시각을 숨긴다. View 재활성 시 현재 맵으로 다시 계획한다. |

V12 `props_manifest.json`의 원래 지면도 확인했다. `fence_horizontal`, `fence_picket_horizontal`, `stone_wall`, `hay_pair`, `water_trough`, `bench`, `ranch_gate`는 2×1, `fence_vertical`은 1×2, `clover_tree`는 2×2, `ranch_barn`은 3×2다. 따라서 원본 전체가 1칸이었던 것은 아니다. 실제 결함은 기존 장식 표시의 bounds 기반 자동 축소, 앵커 한 칸만 검사하던 제작기 배치, 연결 이웃을 읽지 않던 단독 Sprite 선택이었다. 기존 제작기의 맵 안 배치 범위를 유지하면서 V12 `bridge_decor`가 생기는 Stone/Sand의 노출 경계만 새 시각 배치에서 제외한다.

## 기존 ID → 새 시각

매핑은 `RanchConnectedDecorHost.Mappings`의 **정확한 V12 Sprite identity**로만 이루어진다. 다른 테마와 장치 ID는 치환하지 않았다. 현재 커밋된 별길 목장 맵에는 이 대상 장식의 기존 배치가 없어 저장 앵커를 대량 변환하지 않았다. 새 QA 맵으로 실제 배치 경로를 검증했다.

| 확인된 V12 `props/` ID | 원래 지면 | 새 family/group | 새 지면 |
|---|---:|---|---:|
| `fence_horizontal.png`, `fence_picket_horizontal.png` | 2×1 | `cream_fence` 연결 셀 2개 | 2×1 |
| `fence_vertical.png` | 1×2 | `cream_fence` 연결 셀 2개 | 1×2 |
| `stone_wall.png` | 2×1 | `meadow_stone_wall` 연결 셀 2개 | 2×1 |
| `yellow_flowers.png`, `clover_flowers.png` | 1×1 | `raised_flower_meadow` 연결 셀 | 1×1 |
| `hay_pair.png` | 2×1 | `hay_store_2x1` | 2×1 |
| `clover_tree.png` | 2×2 | `tree_flower_corner_2x2` | 2×2 |
| `ranch_barn.png` | 3×2 | `barnyard_3x2` | 3×2 |
| `water_trough.png` | 2×1 | `feeding_corner_3x1` | **3×1**; 세 번째 칸도 검사하고 불가능하면 전체 거절 |

`bench`, `ranch_gate`, `wheelbarrow`, `crate_pair`의 확인된 V12 footprint는 유지하고 원래 Sprite를 native PPU로 그린다. 기존 풍차 회전 표시와 장치, 병아리/둥지, 문, 바람 및 장치 비용 로직은 매핑에서 제외했다. 색상 변경된 옛 장식은 기존 tint 표현을 보존하며 새 family 매핑 대상이 아니다. 해당 배치가 추가되면 별도 시각 승인과 변환이 필요하다.

제작기에는 새 catalog의 정확한 `n00` Sprite로 시작하는 `ranch:cream_fence`, `ranch:meadow_stone_wall`, `ranch:raised_flower_meadow` **1칸 브러시**도 추가했다. 이것은 새로 명시한 장식 ID이며 V12 `props/` ID라고 주장하지 않는다. 한 칸 배치 후 이웃이 생기면 Planner가 실제 mask의 Sprite를 고른다. 저장된 `visualFamily`로 새 시각의 소유권을 유지하므로 catalog/pin이 깨져도 그 `n00` 원본을 옛 장식 renderer로 보여주지 않는다.

연결 판정은 family, 공유 `connectionGroup`, 논리 `logicalLayer`, 정수 `heightKey`가 모두 같을 때만 한다. `placementId`는 각 큰 장식에 유일하며 연결 그룹으로 쓰지 않는다. 좌표는 기존 맵의 +Y 위 방향을 그대로 사용하고, `worldOrigin`은 셀 `(0,0)`의 왼쪽 아래인 `(-0.5,-0.5)`다. Planner의 전체 맵 mask/고정 seed·4칸 보강 기둥 선택을 사용한다. `hostAllowDecorCell`은 순수 조회이며, 실패한 후보는 저장 데이터를 바꾸지 않는다.

## 설치·검증

| 상태 | 수행 내용과 결과 |
|---|---|
| **PASS_OFFLINE** | 설치 경로 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01`에서 `python QA/verify_addon.py --package . --runtime-only`: runtime PNG 298개 디코딩, 체크섬 381개, 3 family, 4 group 통과. 이 검사는 Unity 결과와 별개다. |
| **PASS** | Unity 6000.3.8f1에서 제공 `RanchConnectedDecorV01CatalogBuilder.BuildFromManifest` 실행. `_Generated/RanchConnectedDecorV01Catalog.asset`과 `decor_catalog_build_report.json` 생성: Sprite 298, group 4, catalog 저장 성공. 제공 명령 진입점 `BuildFromCommandLine`과 `-ranchDecorManifest` 인자가 최종 소스와 일치한다. |
| **PASS** | Unity catalog `ValidateIntegrity`와 298개 Sprite의 importer Single/FullRect/PPU256/Bilinear/Clamp/무압축/mipmap 없음 및 GUID/fileID를 실제 `AssetDatabase`에서 검사. SHA pin: `cef943a1410a4b8924b32e0d8727696d91b0220c98d5afffcbfaa34b9cb4ac95`. |
| **PASS** | `sources`, `Authoring`, `Inspection`의 비런타임 텍스처 27개는 Sprite 후보 0개. 패키지 내부 소스는 핀 체크섬을 유지했고, Inspection GIF의 일반 텍스처 설정은 호스트 importer로 보완했다. |
| **PASS** | Unity `recompile_status`: `completed`, `failed:false`, `errors:[]`. 제공 Runtime/Editor 코드와 실제 호스트가 컴파일되었다. |
| **PASS** | Planner EditMode 10/10; 호스트 EditMode 6/6; V12 EditMode 4/4; V12 PlayMode 6/6; Art QA PlayMode 1/1; DevicePatchP01 PlayMode 2/2. |
| **PASS** | 실제 게임 `ANIMOL_Starway_ConnectedDecor_QA` Scene Play: network 93, group 4, claims 108, 소유 renderer 103. 단독 셀부터 네 꺾임·네 T·십자·다른 layer/높이 경계까지 배치했고 울타리 mask `n00..n15`를 모두 확인했다. 장식 안쪽 칸 삭제·복구, View 비활성/재활성, 게임 맵 재로드 후 원래 claim 108과 renderer 103으로 복원되는 것을 실행 확인했다. |
| **PASS** | QA 화면: [전체](QA/StarwayConnectedDecor/full_game.png), [그리드](QA/StarwayConnectedDecor/full_grid.png), [긴 연결부 확대](QA/StarwayConnectedDecor/long_connection.png), [네 큰 장식의 지면 테두리](QA/StarwayConnectedDecor/group_footprints.png). 그리드와 지면 테두리는 Play 중 임시 QA overlay이며 Scene 파일에는 없다. |
| **FAILED_EXISTING** | `PuzzleMapTests` 47/48: 미추적 사용자 `Assets/ANIMOL/Data/Puzzle/Maps/PuzzleMapf.asset`에 필수 목표 장치가 맵 밖에 있어 전체 맵 검증 1건 실패. 새 장식 관련 기존 테스트는 통과하도록 native 지면 기대치를 갱신했다. |
| **FAILED_EXISTING** | `ArtAssetTests` 3/4: `StarSwellCoastV01Device` Sprite 누락. `DevicePatchP01HostTests` 1/5: 기존 gate/wind 기대 좌표·경로 4건 실패. 이번 변경은 해당 에셋/장치 구현을 수정하지 않는다. |
| **NOT_RUN** | 기기별 화면 비율·Bilinear 확대/축소 전체 조합, 실제 광고/서비스/해금 경로, 미추적 사용자 맵의 정리, 이전 Legacy `CommercialPolishPolicyTests`. 제공 원화 11개까지 요구하는 오프라인 전체판 검사도 실행하지 않았다. |

실제 QA 장면의 새 시각에는 대상 V12 단독 renderer가 생성되지 않음을 `PuzzleMapView`의 분기와 Play hierarchy로 확인했다. 다른 맵/틴트의 모든 과거 참조가 0이라고 주장하지 않는다. `manifest_sha256.txt`와 catalog를 동시에 연결했고, 잘못된 pin/snapshot은 옛 Sprite로 되돌리지 않고 현재 controller 시각을 지우는 정책을 테스트했다.

실행 출력은 [오프라인 검사](QA/StarwayConnectedDecor/offline.json), [Unity import 검사](QA/StarwayConnectedDecor/unity_import.json), [비런타임 텍스처 검사](QA/StarwayConnectedDecor/unity_nonruntime.json), [Unity 컴파일 상태](QA/StarwayConnectedDecor/unity_compile.json), [연결 mask 검사](QA/StarwayConnectedDecor/unity_masks.json), [새 장식 EditMode](QA/StarwayConnectedDecor/unity_editmode.json), [실제 Scene Play 계층](QA/StarwayConnectedDecor/unity_scene_play.json), [Play 중 삭제·재생성](QA/StarwayConnectedDecor/unity_scene_mutations.json), [기존 목장 PlayMode](QA/StarwayConnectedDecor/unity_v12_playmode.json)에 저장했다. 실제 QA Scene의 대상 V12 단독 renderer 수는 0이었다.
