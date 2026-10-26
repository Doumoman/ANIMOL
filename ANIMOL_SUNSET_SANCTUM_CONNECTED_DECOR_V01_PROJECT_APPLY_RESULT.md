# ANIMOL 낙일 성소 연결 장식 V01 — 프로젝트 적용 결과

실행일: 2026-10-27 · Unity 6000.3.8f1 · 실제 테마/맵 ID: `letter_observatory` · Sorting Layer: `Default` (장식 sortingOrder 15)

## 적용 범위와 실제 데이터 경로

| 단계 | 확인한 타입/이벤트 | 이번 연결 |
|---|---|---|
| 원본 맵/로드 | `PuzzleMap` / `PuzzlePrototypeController.Load` → `Instantiate(data)` → `PuzzleMapView.Bind` | 테마 자산의 최신 Catalog와 pin을 읽고 장식 전용 GameObject에 Controller·PublishedSnapshotSource·Binding을 함께 생성. 전체 맵을 최초 Publish한 후 `Bind(..., allowExampleSource:false)` |
| 확정 배치·삭제 | `PuzzleMapEditor.TryPlaceDecoration` / `TryEraseDecoration` → `PuzzleMap.NotifyDecorationsChanged` | 모든 지면 칸과 전체 계획을 먼저 순수 검사. 확정 후 `PuzzleMapView.DecorationsUpdated`가 전체 Snapshot을 재발행. Binding의 `Source.Changed`가 Controller를 갱신 |
| 지형·크기 변경 | `PuzzleMap.TerrainChanged` / `Resized` | 성소 지형 갱신 후 허용 공간을 재조회해 Publish; 크기 변경은 `Rebuild`와 새 Publish |
| Editor Undo/Redo | `PuzzleMapEditor.Undone` → `NotifyDecorationsChanged` | 기존 확정 데이터의 최신 전체 Snapshot을 다시 전달. 별도 Undo 데이터 구조 없음 |
| 재시작·씬 재진입 | `PuzzlePrototypeController.Load` / `PuzzleMapView.Bind` | 새 런타임 맵과 장식 소스를 구성해 Publish |
| 비활성·재사용 | `PublishedSnapshotSource.OnDisable`, `Binding.OnDisable`, `PuzzleMapView.OnEnable` | 기존 Snapshot/자기 소유 시각을 비우고, View 활성 시 현재 맵으로 다시 생성·발행. `ExecuteAlways`를 Source/Binding에 적용해 Editor 검사에서도 같은 수명주기를 검증 |

얇은 어댑터는 [SanctumConnectedDecorHost.cs](Assets/ANIMOL/Scripts/Runtime/SanctumConnectedDecorHost.cs)다. 실제 `PuzzleMap.decorations`의 Sprite/앵커/placementId/connectionGroup/논리층/높이를 DTO로 바꾼다. `AllowDecorCell`은 맵 경계, V02 다리 가장자리 소유, 기존 비연결 장식만 조회하며 데이터를 변경하지 않는다. 연결 셀과 그룹은 동일한 전체 보드 Snapshot에 들어간다. `BoardSnapshot.isExampleOnly=false`, `isWholeBoard=true`, `revision`은 확정 갱신마다 증가한다. 원점은 셀 (0,0)의 왼쪽 아래 경계이며 View에서 `objectsRoot.TransformPoint(-0.5,-0.5)`로 한 번 맞춘다.

새 장식의 다칸 예약은 **시각 계획과 에디터 배치 검증**에 사용한다. `PuzzleMap.DecorationAt`/`Walkable`의 기존 게임 통행 판정은 변경하지 않았다. Collider, 장치, 턴, 코스트, 입력, 저장 ID, 배경, 광학, 애니메이션은 수정하지 않았다. Play 확인에서 임시 3×2 제단과 연결 난간 3칸의 비용은 `0→0`이었다.

## 설치·생성 자산

- [패키지](Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01)만 새 경로에 설치했다. 성소 V02 전체를 재설치하지 않았다. 패키지 Runtime/Editor asmdef를 만들고 Presentation 및 테스트 어셈블리가 참조한다. 같은 패키지 클래스의 두 번째 활성 복사본은 없다.
- [성소 테마 자산](Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset)에 실제 Catalog와 `manifest_sha256.txt` TextAsset을 Unity API로 연결했다. ID는 그대로 `letter_observatory`다.
- [맵 제작기](Assets/ANIMOL/Scripts/Editor/PuzzleMapEditor.cs)의 성소 팔레트는 Catalog의 6 연결 계열과 20 소품만 표시한다. native 크기·pivot으로 그리드에서 그리며, 다칸의 일부만 가능한 배치는 거절한다. V02 성소 소품 Sprite는 검토 없는 자동 배치/표시를 거절한다.
- 제공 BoardTools는 이 Editor에서 `SceneManager.SetActiveScene`이 유효한 새 additive 씬에도 `false`를 반환해 처음에는 Stage BoardAsset 생성 후 실패했다. [수정한 BoardTools](Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01/Editor/SanctumConnectedDecorV01BoardTools.cs)는 새 root/카메라/선택 조명을 `MoveGameObjectToScene`으로 검사 씬에 명시적으로 옮긴다. 재실행에서 Stage/All26 BoardAsset·Prefab·Scene 및 [실제 생성 보고서](Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01/_Generated/InspectionBoards/inspection_build_report.json)가 생성됐다. 기존 열린 사용자 씬은 깨끗한 상태였고, 작업 후 원래 `ANIMOL_Starway_ConnectedDecor_QA`를 다시 열었다. BuildSettings는 바꾸지 않았다.

| Unity 생성 자산 | GUID | fileID |
|---|---|---:|
| Catalog | `803cb6992c6dbb840905ba8f2148ead3` | 11400000 |
| Stage BoardAsset | `9d4a0234397493145bb108059c7eeca6` | 11400000 |
| Stage Prefab root | `58c1a481f70a300418c777b786ec8528` | 1277572618834234409 |
| Stage SceneAsset | `6c7c07d2e312b7c4daa9afa75b6a729c` | 102900000 |
| All26 BoardAsset | `9559cefff9297104893349b0edfbc86e` | 11400000 |
| All26 Prefab root | `c81037d795c38fd46b3ef832220c5898` | 3788088599069549299 |
| All26 SceneAsset | `8a26fac04743fb040afc61eae337827b` | 102900000 |
| manifest pin TextAsset | `bde76daa775a8744f8641419c422cf1f` | 4900000 |

manifest의 실제 SHA-256, pin TextAsset 및 Catalog source pin은 모두 `db4fc32203d547866b9cecf72f17a5919ba261ed872f5b24479eb98b5f5970f8`이다. Unity API로 읽은 새 118개와 V02 소품 12개의 각 GUID/fileID/PNG SHA는 [SpriteIdentity_Unity.csv](Docs/QA/SanctumConnectedDecorV01/SpriteIdentity_Unity.csv)에 기록했다. 설치본 118개 전부 Single/FullRect, PPU256, 개별 pivot, Bilinear, Clamp, 무압축, mipmap 없음이며 Sprite GUID/fileID와 Catalog가 일치했다([Unity 자산 감사](Docs/QA/SanctumConnectedDecorV01/AssetAudit_UnityCLI.json)).

## 새 26종 visual key

| 종류 | 실제 key와 지면 |
|---|---|
| 연결 셀 6 | `ivory_star_balustrade`, `brass_lattice_screen`, `archive_shelf_run`, `star_banner_partition`, `blue_crystal_planter`, `votive_light_channel` — 각 1×1, 같은 family·connectionGroup·logicalLayer·heightKey일 때만 N1/E2/S4/W8로 접합 |
| 다칸 12 | `archive_worktable_3x1` 3×1, `banner_dais_2x1` 2×1, `celestial_chart_table_2x2` 2×2, `faceted_armillary_2x2` 2×2, `folio_display_2x1` 2×1, `offering_steps_2x1` 2×1, `prayer_pew_3x1` 3×1, `reliquary_case_2x1` 2×1, `ruined_colonnade_3x1` 3×1, `ruined_mosaic_2x2` 2×2, `sanctum_altar_3x2` 3×2, `scroll_archive_2x2` 2×2 |
| 한 칸 8 | `book_pedestal`, `broken_pillar`, `copper_fern`, `scribe_inkstand`, `scroll_basket`, `star_boundary_marker`, `stone_lantern`, `votive_cluster` — 각 1×1 |

Stage는 **55 연결 셀 / 20 묶음 / 102 claim / 활성 Renderer 79**, All26은 **78 / 20 / 125 / 98**이었다. 보조 기둥은 새 종류에 포함하지 않았다. 두 검사는 `isExampleOnly=true` 검사 보드이며 게임 저장에 사용하지 않았다. 검사 화면은 [Stage Play](Docs/QA/SanctumConnectedDecorV01/Stage_Play.png), [All26 Play](Docs/QA/SanctumConnectedDecorV01/All26_Play.png)다. 실제 `letter_observatory` 프로토타입의 런타임 맵에 임시 배치한 [전](Docs/QA/SanctumConnectedDecorV01/Prototype_Before_Play.png)·[후](Docs/QA/SanctumConnectedDecorV01/Prototype_After_Play.png) 화면도 남겼다. 후자는 3×2 제단 1개와 연결 셀 3개, 9 claim/Renderer 4개이며 원본 맵 자산에는 기록하지 않았다.

## 기존 V02 소품 참조 검토

V02 manifest의 원래 footprint와 Unity Sprite GUID/fileID를 읽었다. 아래는 **의미상 시각 후보**이며, 현재 실제 배치가 없어 자동 치환하지 않았다. 크기가 달라지는 후보는 새로운 모든 칸의 허용 위치가 정해져야 한다. 각 구 GUID/fileID와 새 파일 해시는 위 CSV에 있다.

| V02 파일명 (기존 지면) | V01 검토 후보 / 보류 이유 |
|---|---|
| `arch_doorway` 2×2 | 문/아치와 동등한 정적 visual key 미확인 — 보류 |
| `armillary_instrument` 1×1 | `faceted_armillary_2x2` 후보, 지면 확장 필요 |
| `books_and_scroll` 1×1 | `book_pedestal`/`scroll_basket` 중 의미 미확인 — 보류 |
| `broken_column` 1×1 | `broken_pillar` 후보 |
| `library_shelf` 2×1 | `archive_shelf_run` 연결 셀 2개 후보, 연결 그룹 설계 필요 |
| `moss_bud_clump` 1×1 | 동등 visual key 미확인 — 보류 |
| `raised_copper_foliage` 1×1 | `copper_fern` 후보 |
| `raised_planter` 1×1 | `blue_crystal_planter`는 다른 visual 계열 — 보류 |
| `reading_lectern` 1×1 | `book_pedestal` 후보, 그림 의미 확인 필요 |
| `sanctum_column` 1×1 | `ruined_colonnade_3x1`은 지면·의미가 다름 — 보류 |
| `sanctum_lantern` 1×1 | 정적 `stone_lantern` 후보; 기능 장치로 매핑하지 않음 |
| `stone_balustrade` 2×1 | `ivory_star_balustrade` 연결 셀 2개 후보, 다리 난간 owner와 중복 검토 필요 |

12개 옛 PNG GUID 전체를 대상으로 `Assets`의 직렬화 텍스트/코드/Atlas 경로를 검색했다. 정의 `.meta`를 제외한 일치 파일은 **V02 생성 Catalog와 V02 ArtReview 씬 두 곳뿐**이었다. 현재 `letter_observatory.asset`의 `decorations`는 빈 배열이고, 활성 Data/Scenes/Prefabs/Scripts에 옛 소품 GUID나 `props/sanctum/` 로딩 문자열은 없었다. **현재 맵 배치·렌더 경로의 옛 소품 사용은 0**이지만, 테마가 사용하는 V02 Catalog 안에는 옛 Sprite 참조 12개가 여전히 직렬화돼 있으므로 **전체 자산 참조 0은 아니다**. V02 Catalog는 지형·장치도 공유하므로 임의 재생성/삭제하지 않았다. 원격 로딩·외부 저장 파일은 이 저장소 검사로 0을 증명하지 않는다.

## 검증 기록

| 항목 | 실제 결과 / 근거 |
|---|---|
| 원본 ZIP 오프라인 감사 | `PASS_OFFLINE`, 2254 검사, runtime PNG 118, semantic 26 — [원본 배포 감사](Docs/QA/SanctumConnectedDecorV01/OfflineAudit_Delivery.json). Unity 실행 근거가 아님 |
| 설치 후 같은 오프라인 감사 | `FAIL_OFFLINE` — 감사기는 실제 Unity `_Generated` 자산을 배포본의 “fabricated” 항목으로 보고, BoardTools/Binding/SnapshotSource 소스 패치 3개의 원본 패키지 checksum 차이를 보고한다. [설치본 감사](Docs/QA/SanctumConnectedDecorV01/OfflineAudit_Installed.json). 아트 manifest/pin과 118 PNG는 그대로 보존 |
| Unity import/컴파일 | Catalog integrity 성공; 런타임 Sprite 118개 import 설정·pivot·GUID/fileID 오류 0; `recompile_status`: `failed=false`, errors `[]` — [컴파일 로그](Docs/QA/SanctumConnectedDecorV01/Compile_UnityCLI.json) |
| Unity 생성 | Stage/All26 BoardAsset·Prefab·Scene 모두 생성, 생성기 `Bind`/`RefreshNow`와 활성 Sprite 검증 통과 — [생성 보고서](Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01/_Generated/InspectionBoards/inspection_build_report.json), [Editor 로그 발췌](Docs/QA/SanctumConnectedDecorV01/BuildLog_Excerpt.txt) |
| Missing Script/Sprite | 검사 씬 두 개의 Missing Script 0, SpriteRenderer의 Missing Sprite 0 — [자산 감사](Docs/QA/SanctumConnectedDecorV01/AssetAudit_UnityCLI.json) |
| Unity TestRunner EditMode | **55/55 PASS**. 패키지 `Tests~` 소스를 실제 호스트 테스트 어셈블리에 넣고, 팔레트·실제 맵 Publish·원자적 거절·구 V02 Sprite 거절을 추가 — [결과](Docs/QA/SanctumConnectedDecorV01/SanctumEdit_TestRunner.json) |
| Unity TestRunner PlayMode | **3/3 PASS**. 두 검사 씬의 26종/연결·점유·Sprite 표시, 실제 프로토타입 Publish/비용 유지, source 비활성/재사용 fresh Publish — [결과](Docs/QA/SanctumConnectedDecorV01/SanctumPlay_TestRunner.json) |
| 관련 회귀 | LetterObservatory Play **5/5**, Puzzle Play **7/7**, TheaterConnectedDecor Play **3/3** — [성소 기존 테스트](Docs/QA/SanctumConnectedDecorV01/LetterObservatoryPlay_TestRunner.json), [공통 퍼즐](Docs/QA/SanctumConnectedDecorV01/PuzzlePlay_TestRunner.json), [극장](Docs/QA/SanctumConnectedDecorV01/TheaterPlay_TestRunner.json) |
| 실제 Play 화면 | Stage/All26 검사 씬과 기존 `ANIMOL_letter_observatory_Prototype` 씬에서 캡처. 프로토타입의 Publish 뒤 제단 1개·연결 난간 3칸이 표시되고 비용 `0→0` |
| 화면 갱신 영상 | `NOT_RUN` — 정지 캡처만 생성 |
| 저장 파일에서 실제 구 소품 이관 | `BLOCKED_PRECONDITION` — 현재 도달 가능한 성소 맵에 구 소품 배치가 없어 위치·허용 지면을 확정할 실제 대상이 없음 |
| 전체 옛 Sprite 참조 0 및 삭제 | `BLOCKED_PRECONDITION` — 공유 V02 Catalog와 V02 ArtReview 씬에 참조가 남아 있으며, 지형·장치용 공유 Catalog를 재작성하지 않고 해당 소품만 안전하게 분리할 권위 이관 경로가 없음. 삭제는 실행하지 않음 |
| 전 캠페인/대전/원격 저장·모든 기능 장치 수동 플레이 | `NOT_RUN` — 현재 저장소의 검사/프로토타입 경로에서 위 항목만 실행. 별도 출시 환경의 외부 상태는 확인하지 않음 |

처음 생성 시 Unity CLI의 5초 `eval_file` 호출 제한과 위 additive 씬 활성화 실패가 있었다. 재실행은 `unity command run_script`의 실제 장시간 실행 경로로 완료했으며, 이전 실패를 성공으로 표시하지 않았다. 실제 수정 후 Catalog/생성/Play/테스트 결과는 각각 위 근거 파일에 분리해 기록했다.
