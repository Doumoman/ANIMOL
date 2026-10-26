# 잔영 극장 연결 장식 V01 R2 실제 Unity 적용 결과

2026-10-27. Unity 6000.3.8f1의 이 저장소에서 실행했다. 테마 ID는 `memory_theater`, 실제 Sorting Layer는 `Default`다. 이 문서의 PASS는 아래에 적은 실행만 뜻한다. 검사 보드의 예시 데이터는 게임 맵에 저장하지 않았다.

## 설치와 변경 범위

- `Assets/ANIMOL/Art/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/`에 제공 패키지 한 벌을 설치했다. 기존 동일 패키지 경로는 없었고, 이전 극장 V03을 재설치하거나 삭제하지 않았다. Runtime/Editor asmdef를 더하고 프로젝트 asmdef에 정확한 참조를 연결했다.
- 원본 `manifest.json` SHA-256과 `manifest_sha256.txt`의 값은 모두 `8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069`이다. 장식 원본 26종, 런타임 PNG 118개를 유지했다. 카탈로그의 Unity 실물 Sprite GUID/fileID와 PNG별 SHA/PPU/pivot은 [Unity 자산 감사표](Docs/QA/TheaterConnectedDecorR2_UnityAssetAudit.tsv)에 118개 전부 기록했다.
- `TheaterConnectedDecorV01BoardTools.BuildExamples`를 실제 연결된 Editor에서 Stage/All26 typed JSON, `Default`, 검사 씬 생성 `true`로 실행했다. 처음 실행은 이미 활성화된 새 씬에 `SetActiveScene`이 `false`를 반환하여 실패했다. BoardTools가 이미 활성 씬인 경우를 받아들이도록 수정하고 재실행해 [생성 보고서](Assets/ANIMOL/Art/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/_Generated/InspectionBoards/inspection_build_report.json)를 얻었다. 이 수정 때문에 제공 오프라인 `verify_completion.py`의 **소스 동결 해시 검사는 FAIL** (`Editor/TheaterConnectedDecorV01BoardTools.cs`)이다. 이 실패를 Unity 실행 결과로 대체하지 않는다.
- 실제 두 극장 `PuzzleTheme` 에셋에 현재 카탈로그와 핀을 연결했다. 목장, 낙일 성소, 다른 테마의 데이터·씬·장치·Collider·턴·코스트 코드는 수정하지 않았다.

### Unity가 생성한 주요 ID

| 자산 | GUID | fileID |
|---|---|---:|
| Catalog | `187dc2c0c5e357848935f936786bef39` | `11400000` |
| Stage BoardAsset | `dc69220e607a1cb4a80bb0e378009d09` | `11400000` |
| Stage Prefab | `5e1718af6a2f6654a94a5f1c5e5e1b4b` | `3162674585994184109` |
| Stage Scene | `5f9c6a6ca168d20468a40772bae0f29c` | `102900000` |
| All26 BoardAsset | `7d022c1e344f2b2429d4d7f579433103` | `11400000` |
| All26 Prefab | `6fb6389b170dd364aa44c9f8094a1a67` | `8162907735720797728` |
| All26 Scene | `2a141af9f49c64a42b8f6719062b5484` | `102900000` |

해당 자산의 개별 파일 SHA는 위 Unity 자산 감사표에 있다. 검사 보드의 에셋·Prefab·Scene은 패키지의 `_Generated/InspectionBoards/Stage` 및 `All26` 아래에 있다. [이전 14 Sprite의 실제 ID](Docs/QA/TheaterConnectedDecorR2_OldPropAudit.tsv)와 [현재 참조 조사](Docs/QA/TheaterConnectedDecorR2_OldReferences.tsv)도 Unity AssetDatabase와 활성 Assets 검색으로 남겼다.

## 실제 호스트 연결

| 실제 타입/경로 | 확정 사건과 처리 |
|---|---|
| `PuzzleMap`, `DecorationCell` | 장식 앵커·고유 `placementId`·연결 `connectionGroup`·`logicalLayer`·`heightKey`는 기존 모델 필드를 쓴다. 크기는 최신 카탈로그가 지정한다. `NotifyDecorationsChanged`와 `Resized`가 확정 변경을 알린다. |
| `TheaterConnectedDecorHost` | 현재 극장 Sprite/명시 키만 6개 network 또는 20개 group/point에 대응한다. 전체 맵 셀에서 기존 장식 허용 정책을 순수 조회해 `schemaVersion=1`, `isWholeBoard=true`, `isExampleOnly=false` 스냅샷을 만든 뒤 전체 Planner를 검사한다. 기존 V03 `visualDeck` 소유 칸에는 새 장식을 허용하지 않는다. 게임 점유는 쓰지 않는다. |
| `PuzzleMapView.Bind/Rebuild` | 실제 맵 로드와 뷰 생성 시 같은 장식 GameObject에 Controller, PublishedSnapshotSource, Binding을 만들고 현재 스냅샷을 Publish한 다음 `Bind(..., "Default", source)`한다. `allowExampleSource`는 기본값 `false`다. 프레임 폴링은 없다. |
| `PuzzleMapView.DecorationsUpdated` | 확정 장식 변경 시 revision을 증가시키고 전체 최신 맵을 다시 Publish한다. Binding은 Source.Changed를 받아 자기 Controller의 `Configure/SetLayout`을 호출한다. 기존 V03 단독 장식 뷰도 별도로 즉시 갱신한다. |
| `PuzzleMapEditor` | 극장 팔레트에 최신 26 그림을 보여준다. 전체 footprint 배치 가능성을 확정 전 검사하고 묶음당 앵커 하나를 저장한다. 삭제는 지면 전체에서 찾는다. `Undo.undoRedoPerformed → Undone → NotifyDecorationsChanged`를 사용한다. 편집 그리드는 최신 플랜을 native 지면 크기로 그린다. |
| `PuzzlePrototypeController.Load`, `PuzzleController.Load` | 기존 런타임 맵 복제 후 `PuzzleMapView.Bind`를 호출하므로 새 뷰가 현재 데이터를 Publish한다. 풀 재활성화는 뷰 `OnEnable` 재구성으로 신선한 Publish를 만든다. |

`allowedDecorCells`는 예시의 1920칸을 복사하지 않는다. 기존 호스트의 맵 안쪽/기존 장식 중복/기존 객석 다리 `visualDeck` 소유를 조회한 결과다. 셀 검사 중 모델이나 점유를 변경하지 않는다. Player가 장식을 통과하는지 등 게임 규칙도 이 적용에서 바꾸지 않았다.

## 26종 대응과 보류한 이전 시각

다음은 **새 팔레트의 실제 key**와 이전 V03 소품 ID의 후보 대조다. 후보는 자동 이관 완료를 뜻하지 않는다. 활성 `PuzzleMap` 극장 맵의 `decorations`는 비어 있었고, 이전 14 Sprite 모두 `EnvironmentArt.asset`에서 아직 참조된다. V03 `EchoTheaterV02MapRenderer`가 `MemorySkyTheaterLayout`의 경계 소품과 W 다리 난간을 계속 그린다. 이들은 새 보드 장식과 배치 좌표·소유자가 달라 이번 작업에서 제거하지 않았다. 따라서 “이전 시각 참조 0”이라고 주장하지 않는다.

| 새 key | 이전 ID 후보 / 처리 |
|---|---|
| `velvet_aisle_barrier` | 없음. 새 연결 띠 |
| `carved_stage_balustrade` | 기존 W 다리 난간과 소유 중복 금지 |
| `prop_shelf_run` | 작동 회전 책장과 별개인 정적 장식 |
| `velvet_curtain_screen` | `curtain_wing_left`, `curtain_wing_right`, `curtain_lintel_repeat`와 모양 후보이나 기존 경계 구조·1×2 크기 때문에 자동 교체 보류 |
| `brass_footlight_strip` | `footlights_on/dim/off`의 상태/기능 확인 전 자동 교체 금지 |
| `ivy_planter_strip` | 없음. 새 연결 띠 |
| `grand_piano_3x2` | `organ` 2×2 → 3×2: 추가 칸 검증·재배치 필요 |
| `audience_bench_3x1` | `seat_pair` 2×1 → 3×1: 추가 칸 검증·재배치 필요 |
| `ticket_booth_2x2` | 없음 |
| `costume_rack_3x1` | `costume_trunk`와 용도가 다름, 자동 매핑 없음 |
| `vanity_dressing_2x1` | 없음 |
| `phonograph_table_2x1` | 없음 |
| `scenery_flat_3x1` | `scenery_flat` 2×2 → 3×1: 배치 재작성 필요 |
| `stacked_props_2x2` | `costume_trunk` 2×1과 시각 후보이나 크기·용도 미확인 |
| `mask_display_2x1` | `mask_stand` 1×1과 시각 후보이나 크기 다름 |
| `puppet_storage_2x2` | 겁쟁이 인형 장치와 별개인 정적 장식 |
| `backstage_spotlight_duo_2x1` | `spotlight_stand` 1×1 → 2×1: 추가 칸 검증 필요 |
| `stage_steps_2x1` | 없음 |
| `ticket_stand` | 없음 |
| `folded_chair` | 없음; All26에 포함 |
| `music_stand` | 없음 |
| `tall_candelabra` | 없음 |
| `mask_pedestal` | `mask_stand`와 후보이나 용도 미확인; All26에 포함 |
| `script_pile` | `book_stack`과 후보이나 내용 다름; All26에 포함 |
| `sandbag_stack` | 없음; All26에 포함 |
| `flower_urn` | 없음 |

이전 `rope_pulley`는 위 새 정적 장식에 대응시키지 않았다. 구 장식 GUID/fileID 또는 문자열을 전역 치환하지 않았고, 이전 V03 Sprite와 `.meta`를 삭제하지 않았다.

## 실제 검증과 자료

| 항목 | 결과 |
|---|---|
| Unity import/compile | **PASS**: 연결된 Unity Editor `recompile_status`가 `completed`, 오류 0. [상태](Docs/QA/TheaterConnectedDecorR2_CompileStatus.json) |
| BoardTools 생성 | **PASS**: Stage 48 network/16 placement/88 claim, All26 78/20/123. 카탈로그·BoardAsset·Prefab·Scene이 실제 Unity GUID/fileID로 생성됨. 검사 자산의 missing Sprite/Script를 Play 테스트에서 검사. [보고서](Assets/ANIMOL/Art/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/_Generated/InspectionBoards/inspection_build_report.json) |
| 검사 Scene Play | **PASS**: 두 Scene을 열어 Play. 실제 Source 캡처와 Binding.RefreshNow, Controller plan, 활성 SpriteRenderer, Sprite 경로/PPU256 검사. [Stage 화면](Docs/QA/TheaterConnectedDecorR2_Stage_Play.png), [All26 화면](Docs/QA/TheaterConnectedDecorR2_All26_Play.png) |
| 기존 극장 프로토타입 Play | **PASS**: 실제 `memory_theater` Scene의 런타임 맵에 3×2 피아노를 임시로 Publish하여 묶음 하나/renderer 하나, 삭제 후 0, 코스트 0 유지. 씬 에셋은 저장하지 않음. [변경 전](Docs/QA/TheaterConnectedDecorR2_Prototype_Before.png), [변경 후](Docs/QA/TheaterConnectedDecorR2_Prototype_Play.png) |
| R2/호스트 EditMode | **47 PASS / 3 FAIL**. 실패 3개는 제공 `CompletionTests`가 EditMode GameObject에서 `OnDisable`/`OnEnable` 수명주기 호출을 기대한 항목이다. 같은 비활성/재활성 시나리오는 PlayMode에서 PASS. [전체 결과](Docs/QA/TheaterConnectedDecorR2_EditModeResults.json) |
| 새 PlayMode | **3 PASS / 0 FAIL**: Stage/All26 실제 표시, 실제 극장 프로토타입, 확정 장식 갱신·이전 단독 장식·풀 비활성 후 재Publish. [결과](Docs/QA/TheaterConnectedDecorR2_PlayModeResults.json) |
| 기존 극장 V03 PlayMode | **1 PASS / 0 FAIL**. [결과](Docs/QA/TheaterConnectedDecorR2_V03RegressionResults.json) |
| `PuzzleMapTests` | **48 PASS / 1 FAIL**. `AllValidationMapsHaveValidPlacementsAndCompleteTheme`의 사용자 작업 파일 `PuzzleMapf`에 필수 목표 장치가 맵 밖에 있다는 기존 검증 실패. 이 파일은 수정하지 않았다. [결과](Docs/QA/TheaterConnectedDecorR2_PuzzleMapResults.json) |
| 설치본 오프라인 `verify_completion.py` | **FAIL**: BoardTools의 실제 Unity 버그 수정 때문에 동봉된 정적 소스 해시와 달라짐. PNG/pin 또는 Unity Play 성공의 대체 증거로 쓰지 않음 |
| 배치 명령의 별도 `-batchmode`/`-logFile` 실행 | **NOT_RUN**: 같은 프로젝트가 열린 실제 Editor에 연결해 `BuildExamples`를 실행했다. 이 문서의 생성·컴파일·TestRunner/Play 결과는 그 Editor의 실제 결과다. |
| 캠페인/대전/저장 로드/재시작/수동 Undo·Redo/빠른 연속 편집 영상 | **NOT_RUN**. 해당 경로가 `PuzzleMapView`를 재사용하는 소스 연결은 확인했으나 모든 모드의 조작 영상을 실행 증거로 만들지 않았다. |
| 이전 V03 경계 소품의 전면 시각 이관 | **BLOCKED_PRECONDITION**: 실제 보드 장식 앵커가 없는 경계 Prefab과 크기·기능이 달라 안전한 자동 대응/점유 승인을 정할 수 없다. 기존 최신 V03 시각과 게임 동작을 유지한다. |

검사 생성 로그 자체의 `actualPlayModeVerified:false`, `actualProductionBindingsVerified:false`는 **생성 시점**의 보수적 표시다. 위의 별도 실제 Play 검증은 이후에 실행됐으며 생성 보고서를 성공으로 소급 수정하지 않았다. 갱신 영상은 **NOT_RUN**이고, 실제 Play의 전후 화면과 TestRunner 결과를 제출한다.
