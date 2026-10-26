# 별길 목장 연결 장식 V02 실제 적용 결과

검사일: 2026-10-27 (KST)
Unity Editor: 6000.3.8f1, 연결된 Editor의 Unity CLI 사용
packageId: `ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02`
manifest SHA-256: `770e8085e6cd8c6804c062742f44888ab0fd395282bee413366888ea2d32e38c`

## 결과 요약

| 항목 | 결과 | 실제 근거 |
|---|---|---|
| 전달 ZIP CRC·manifest·패키지 오프라인 감사 | PASS_OFFLINE | ZIP 590항목, CRC 오류 0; 독립 감사 7,177검사, 오류·경고 0. [감사 JSON](Docs/QA/RanchConnectedDecorV02/offline_audit.json) |
| Unity V02 카탈로그·임포트 | PASS_IMPORT | Sprite 362개, 그룹 20개, 각 GUID/fileID·PPU·피벗·Single/FullRect/Bilinear/Clamp/무압축/no mipmap 확인. [전체 Sprite 표](Docs/QA/RanchConnectedDecorV02/sprite_guid_fileid.csv) |
| C# 컴파일 | PASS_COMPILE | Unity `recompile_status`: completed, `compilationFailed=false`, 오류 0. [상태](Docs/QA/RanchConnectedDecorV02/unity_compile.json) |
| Stage / All26 생성 | PASS_GENERATION | 실제 BoardAsset, Prefab, Scene 생성. Stage 65연결칸+20묶음=120점유, All26 96+20=151점유. [_Generated 보고서](Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/InspectionBoards/inspection_build_report.json) |
| V02 EditMode | PASS_TEST | 최신 실행 75/75. [TestRunner 결과](Docs/QA/RanchConnectedDecorV02/ranch_editmode_tests.json) |
| 목장 V02 PlayMode | PASS_TEST | Stage/All26 실제 Scene 및 목장 프로토타입 2/2. [TestRunner 결과](Docs/QA/RanchConnectedDecorV02/ranch_playmode_tests.json) |
| 다른 연결 장식 호스트 EditMode | PASS_TEST | 16/16. [회귀 결과](Docs/QA/RanchConnectedDecorV02/connected_decor_host_regressions.json) |
| V12 목장·장치 아트 EditMode | PASS_TEST | `StarwayRanchV12Tests` 4/4, `DeviceArtTests` 4/4. |
| 전체 `PuzzleMapTests` | FAIL_UNRELATED | 48/49. `PuzzleMapf.asset`가 시작 전부터 수정 중이던 `starway_Scenario.asset`의 맵 밖 필수 목표 장치를 참조한다. 이 장식 작업에서 해당 시나리오/맵은 수정하지 않았다. |
| 실제 화면 | PASS_CAPTURE | Unity Game/카메라 캡처 6장. 아래 링크 참조. |
| 연속 화면 녹화 | NOT_RUN | 이 실행에서 실제 동영상 녹화는 수행하지 않았다. 정지 화면이나 패키지 PNG를 동영상 실행 증거로 바꾸지 않았다. |

원본 ZIP 크기는 66,998,688바이트, SHA-256은 `a3d758bb1c197984832498f99e6791733152948dd0fb1889f06df7d14e409c30`이다. ZIP은 `Assets` 밖 `Temp/RanchV02Extract`에 먼저 압축 해제해 감사한 뒤 새 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02`에 설치했다. V01 폴더와 `.meta`를 복제하지 않았다. 설치 후 패키지 체크섬 589개 중 586개 원본 바이트가 유지되며, 아래 Unity 연결에 필요한 C# 3개만 의도적으로 수정했다: `RanchConnectedDecorV02BoardTools.cs`(검사 Scene의 소유 Scene 명시), `RanchConnectedDecorV02Binding.cs`와 `RanchConnectedDecorV02SnapshotSource.cs`(EditMode 생명주기 처리). 따라서 설치 후 원본 체크섬 589개 전체 일치를 주장하지 않는다.

## 실제 프로젝트 연결

실제 테마 식별자는 `starway`이며 목장 판정은 `PuzzleTheme.starwayRanch`다. Sorting Layer는 `Default`다. 두 실제 목장 테마 자산 `Data/Puzzle/Prototypes/starway_Theme.asset`와 `Data/Puzzle/Themes/StarGarden.asset`에 생성된 V02 Catalog와 현재 pin을 Unity API로 연결했다. 기존 V01 Controller 생성 호출은 생산 경로에서 제거했다. `PuzzleMapView`는 장식 전용 한 GameObject에 V02 Controller, PublishedSnapshotSource, Binding을 붙이고 전체 `PuzzleMap.decorations`와 실제 목장 허용 칸에서 만든 스냅샷을 발행한다. 프로덕션 `allowExampleSource=false`다. Source.Changed를 받는 Binding이 해당 Controller만 갱신한다.

| 실제 권위 타입/사건 | 어댑터 입력과 갱신 |
|---|---|
| `PuzzleMap` 생성·로드 → `PuzzleMapView.Bind/Rebuild` | `RanchConnectedDecorV02Host.TryCreateSnapshot` → `PublishedSnapshotSource.PublishSnapshot` → `Binding.Bind/RefreshNow` |
| `PuzzleMap.DecorationsChanged` (확정 배치·삭제·연결 설정) | 리비전 증가, 기존 예외 소품만 재표시, 최신 전체 Snapshot 발행 → Source.Changed 구독 갱신 |
| `PuzzleMapEditor.Undone` (Undo/Redo) | `map.NotifyDecorationsChanged()`를 통해 동일 확정 경로 호출. 이벤트 연결은 소스에서 확인; 별도 수동 Undo 화면 녹화는 NOT_RUN |
| `PuzzleMap.TerrainChanged`, `Resized` | `PuzzleMapView.Rebuild`로 현재 전체 맵 재조회·발행 |
| 비활성/재활성 및 풀 재사용 | Source/Binding 비활성 때 이전 시각 제거; `PuzzleMapView.OnEnable`이 현재 `map`에서 재구성·freshPublish. 패키지 생명주기와 실제 프로토타입 Play 테스트 통과 |
| `PuzzleMap.DecorationAt`, `OutsideCount`, `ValidateMap`; `PuzzleMapEditor.TryPlaceDecoration/TryEraseDecoration` | V02 Host의 실제 게임 점유, 지면 크기 검사, 원자적 후보 스냅샷 검사 사용. 확정 전 순수 허용 조회는 맵/예약/코스트를 수정하지 않음 |

호스트 소스는 [RanchConnectedDecorV02Host.cs](Assets/ANIMOL/Scripts/Runtime/RanchConnectedDecorV02Host.cs)다. `PuzzleMapView`의 `objectsRoot.TransformPoint(-0.5,-0.5,0)`가 셀 (0,0)의 왼쪽 아래 world origin이며 `cellSize`는 기존 맵의 world cell 크기를 따른다. 이 게임의 셀 좌표는 월드 +Y가 북쪽이라 행 아래 방향 변환은 호출하지 않는다. 장식 logicalLayer/heightKey와 sortingOrder는 별도 필드다. 꽃/보조 기둥은 네트워크 셀 위에만 그리며 추가 게임 점유를 만들지 않는다. 풍차의 기존 몸체+회전 날개는 V12 예외 시각으로 유지한다.

## 시각 키와 실제 저장 ID 매핑

새 네트워크 6계열은 `cream_fence`, `meadow_stone_wall`, `raised_flower_meadow`, `orchard_trellis`, `herb_planter_strip`, `ranch_lantern_strip`이다. 앞 5계열 중 화단을 제외한 계열은 n00..n15, 화단은 원본 8비트 n000..n255를 사용한다. 목장 기존 V01의 7키(울타리·돌담·화단과 `hay_store_2x1`, `feeding_corner_3x1`, `tree_flower_corner_2x2`, `barnyard_3x2`)는 같은 V02 시각 키로 유지된다. 새 키 19개(네트워크 3계열, 묶음/한 칸 소품 16개)를 추가한다. V01 Sprite는 저장된 배치의 식별용으로만 읽고 실제 렌더러에는 사용하지 않는다.

| 확인된 V12 `props/` ID | V02 시각 키 | 점유·예외 |
|---|---|---|
| `fence_horizontal`, `fence_picket_horizontal`, `fence_vertical` | `cream_fence` | 각 2×1 또는 1×2 원본 셀을 한 연결 그룹으로 확장 |
| `stone_wall` | `meadow_stone_wall` | 2×1 연결 |
| `yellow_flowers`, `clover_flowers` | `raised_flower_meadow` | 각 1×1, 이웃 8비트 원본 상태 |
| `hay_pair` | `hay_store_2x1` | 2×1 |
| `water_trough` | `feeding_corner_3x1` | 3×1 전체가 허용되지 않으면 거절; 2×1로 축소하지 않음 |
| `clover_tree` | `tree_flower_corner_2x2` | 2×2 |
| `ranch_barn` | `barnyard_3x2` | 그림 지면 3×2, 앞서 확정된 게임 이동 점유 3×1 유지 |
| `milk_cans` | `milk_can_pair` | 확인된 1×1 |

새 V02 묶음/한 칸 키 전부: `hay_store_2x1`, `feeding_corner_3x1`, `tree_flower_corner_2x2`, `barnyard_3x2`, `orchard_bench_3x1`, `seedling_rack_3x1`, `harvest_cart_2x2`, `chicken_coop_2x2`, `apiary_corner_2x2`, `picnic_arbor_3x2`, `produce_stall_3x2`, `garden_workbench_2x1`, `milk_can_pair`, `tool_basket`, `ranch_notice_sign`, `watering_can`, `feed_sack_stack`, `flower_bucket`, `harvest_crate`, `honey_jar_stand`.

실제 저장된 목장 `PuzzleMap`은 23개, 장식 placement는 90개다. 전체 맵으로 V02 스냅샷을 재검사한 결과 거절 0개였다. 그중 V01 Sprite 저장 참조 12개, V12 Sprite 74개, Sprite가 비어 있는 기존 레코드 4개다. V02 연결·묶음으로 해석되는 placement는 77개이며, 남은 13개는 기존 예외 시각이다. [맵별 목록](Docs/QA/RanchConnectedDecorV02/ranch_map_inventory.csv), [각 배치의 실제 source ID→V02 visual key·지면·게임 점유](Docs/QA/RanchConnectedDecorV02/ranch_placement_mappings.csv), [스냅샷 오류 목록](Docs/QA/RanchConnectedDecorV02/ranch_snapshot_failures.txt)을 남겼다. 빈 Sprite 레코드와 V12 `bench` 2×1, `hay_bale` 1×1, `slate_rocks` 1×1, `windmill_body` 2×1은 임의 크기 변경이나 기능 전환 없이 기존 시각 경로에 남겼다. V12 `ranch_gate`, `birdhouse`, `direction_sign`, `wheelbarrow`, `crate_pair`, `lantern_post`, `clover_tuft` 등도 확인되지 않은 V02 기능/점유로 자동 이관하지 않았다. V01 패키지는 저장 Sprite 식별에 실제로 필요하므로 삭제 대상 0건이다. 새 Controller가 V01 Sprite를 렌더링하지 않는다는 것과 프로젝트에서 V01 참조가 완전히 0이라는 것은 다른 주장이다.

## 생성 자산과 화면

정확한 GUID/fileID 10개 자산 표는 [generated_ids.csv](Docs/QA/RanchConnectedDecorV02/generated_ids.csv)이며, 모든 362 Sprite의 표는 위 CSV에 있다. 주요 GUID: Catalog `083bd230f0b040b4ab738c48b9ad3ae7`, Stage Board/Prefab/Scene `6104280a8eb34d641ba526bd341efbb1` / `dc0579ae68c411e4f99950578828184d` / `346c437d20b73e547ae0105ccbd7e35f`, All26 `ea4b42d34e5a91c47a35e4e699640f2c` / `178e30dd34d5e474183dbad7e3032f30` / `14b95f41f66be924785e18963091ade9`. 두 Prefab의 Missing Script 0을 검사했고, Play 테스트에서 실제 활성 SpriteRenderer 수와 `OwnedVisualCount` 일치, Sprite 존재·PPU256·V02 경로를 확인했다.

- [Stage 전체 Play 화면](Docs/QA/RanchConnectedDecorV02/stage_play.png)
- [연결부 확대 Play 화면](Docs/QA/RanchConnectedDecorV02/stage_connections_play.png)
- [Stage 묶음 확대 Play 화면](Docs/QA/RanchConnectedDecorV02/stage_groups_play.png)
- [All26 전체 Play 화면](Docs/QA/RanchConnectedDecorV02/all26_play.png)
- [All26 묶음 확대 Play 화면](Docs/QA/RanchConnectedDecorV02/all26_groups_play.png)
- [실제 목장 프로토타입의 임시 V02 3×1 장식 Play 화면](Docs/QA/RanchConnectedDecorV02/prototype_live_safe_play.png). 실행 중 맵에서만 배치했고 Scene·저장 자산은 수정하지 않았다. 코스트 0, V02 소유 renderer 1개를 확인했다.

검사 Scene은 원본 프로덕션 Scene/Build Settings/카메라를 수정하지 않았다. 화면 검수 뒤 처음 열려 있던 `ANIMOL_twilight_sea_Prototype` Scene을 다시 열었고 dirty=false를 확인했다. 패키지 생성 보고서의 `actualProductionBindingsVerified=false`와 `actualPlayModeVerified=false`는 생성기 자체의 사전 보고 필드이며, 위 Play 시험은 그 이후 별도로 수행한 결과다.

## 범위와 남은 검증

- 기존 문·바람·병아리·풍차 로터·공통 장치·배경·다른 테마의 데이터와 코드는 이 작업에서 수정하지 않았다. Starway V12 및 장치 아트 EditMode 회귀 8개와 다른 연결 장식 호스트 16개가 통과했다. 전체 기기·스토리·대전 플레이 시나리오를 수동 완주한 결과는 **NOT_RUN**이다.
- Undo/Redo는 실제 Editor의 `Undo.undoRedoPerformed → PuzzleMapEditor.Undone → DecorationsChanged` 연결을 확인했다. 자동 테스트는 배치·삭제·재발행·풀 재활성의 동작을 확인했으며, 저장/로드와 별도 UI Undo 화면 녹화는 **NOT_RUN**이다.
- `PuzzleMapTests`의 남은 1개 실패는 [위치가 확인된 `PuzzleMapf.asset` 검증 실패](Assets/ANIMOL/Data/Puzzle/Maps/PuzzleMapf.asset)다. `starway_Scenario.asset`은 작업 시작 시 이미 미커밋 변경이 있었고 이 작업에서 손대지 않았다. 사용자 변경의 의도와 목표 장치 위치를 판단해 임의 수정하지 않았다.
- 2026-10-27 실행의 연속 영상 파일은 **NOT_RUN**. 실제 화면 6장과 Play/TestRunner 결과를 제출한다.

## 변경 파일 범위

새 V02 패키지는 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02` 하나다. 기존 프로젝트 수정은 `PuzzleTheme`, `PuzzleMap`, `PuzzleMapView`, `RanchConnectedDecorV02Host`, `PuzzleMapEditor`, 두 목장 Theme 자산, 관련 asmdef와 EditMode/PlayMode 테스트다. 검수 산출물은 이 문서와 `Docs/QA/RanchConnectedDecorV02`에 둔다. V01 폴더 및 다른 테마의 장치·타일·배경 `.meta`는 이번 작업에서 정리하지 않았다.
