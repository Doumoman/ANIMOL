# 별너울 해안 연결 장식 V01 — 프로젝트 적용 결과

## 범위와 실제 ID

- 적용 프로젝트: Unity 6000.3.8f1, `C:/Users/user/Documents/GitHub/ANIMOL`.
- 실제 테마·맵·규칙 ID: `twilight_sea`. 저장 ID를 새 이름으로 바꾸지 않았다.
- 패키지: `Assets/ANIMOL/Art/ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01`, 네임스페이스 `ANIMOL.Art.CoastConnectedDecorV01`.
- 실제 Sorting Layer: `Default` (`Background`도 존재). 장식의 논리 `logicalLayer`와 구분한다.
- `PuzzleMap`의 좌표는 월드 +Y가 북쪽이다. 셀 (0,0)의 왼쪽 아래 경계를 `(-0.5,-0.5)`로 넘겼다. 행이 아래로 증가하는 별도 변환은 이 호스트에 적용하지 않는다.
- `StarSwellCoastV01MapRenderer`의 A는 Grass/해안 모래, W는 Sand/나무다리, S는 Stone/돌다리다. `PuzzleScenario.water`는 지형이 없는 실제 물칸으로 조회한다. 원경 `StarSwellCoastV01OceanField`는 장식 허용 영역에 포함하지 않는다.

## 설치 및 권위 데이터 연결

| 경로/타입 | 실제 변경 및 호출 |
|---|---|
| `Assets/ANIMOL/Scripts/Runtime/CoastConnectedDecorHost.cs` | `PuzzleMap.decorations`의 Sprite를 **정확한 최신 Catalog 레코드**와 대조해 6개 network/20개 group DTO로 변환한다. 전체 보드의 지형·물·기존 장식·다리 가장자리 허용 조건을 순수 조회한다. 전체 footprint, 중복 셀, 층 간 겹침을 검증한 뒤 snapshot을 만든다. 게임 점유·비용을 수정하지 않는다. |
| `Assets/ANIMOL/Scripts/Runtime/PuzzleMapView.cs` | 동일 장식 전용 GameObject에 Controller/PublishedSource/Binding을 둔다. 전체 snapshot `PublishSnapshot` 후 `Binding.Bind(..., allowExampleSource:false)`; Binding의 `Source.Changed`가 `Configure/SetLayout`을 호출한다. `ClearLayout`/Disable 시 소유 시각만 제거한다. |
| `Assets/ANIMOL/Scripts/Editor/PuzzleMapEditor.cs` | 실제 `twilight_sea` 팔레트를 Catalog의 26개 장식 Sprite로 제한한다. 네트워크 그룹과 다칸 점유를 표시·선택·검사한다. 배치/삭제/설정 변경은 새 전체 snapshot이 유효할 때만 커밋하며 Undo를 등록한다. 물칸 편집도 확정 데이터 갱신을 알린다. |
| `PuzzleMap`, `PuzzleTheme`, `twilight_sea_Theme.asset` | 크롭 검사에 해안 group footprint를 포함하고 테마에 최신 Catalog와 manifest SHA pin을 연결한다. `PuzzleMap.DecorationAt`/Walkable의 기존 게임 점유 규칙은 바꾸지 않았다. |
| Add-on Editor/Runtime | 패키지 전용 asmdef 설치. 검사 씬 생성기에서 루트·카메라·선택 조명을 명시적으로 **검사 씬으로 이동**시켜 기존 열린 씬을 건드리지 않게 했다. Source/Binding은 EditMode 생명주기 검사를 위해 `ExecuteAlways`를 사용한다. |

| 확정 호스트 사건 | 실제 경로 | 결과 |
|---|---|---|
| 맵 로드·생성·재시작·씬 재진입 | `PuzzlePrototypeController.Load → PuzzleMapView.Bind → Rebuild` | 현재 전체 장식 모델을 새로 Publish |
| 확정 장식 편집·삭제·Undo/Redo | `PuzzleMap.DecorationsChanged`, 제작기 `Undone` | revision 증가 후 전체 snapshot Publish; 취소/거절 편집은 발행 없음 |
| 지형/물칸 편집 | `TerrainChanged → RefreshNeighborhood`, 제작기 물칸 확정 통지 | 순수 허용 셀 재조회 및 Publish |
| 풀 비활성·재활성 | `PublishedSnapshotSource.OnDisable`, `PuzzleMapView.OnEnable → Rebuild` | 이전 권위 snapshot 무효화; 현재 맵에서 새 Source/Binding과 fresh Publish |

기존 장치 Renderer, Animator, 바람·문·나룻배·폭탄·구르는 통의 상태 및 코스트 소유권은 새 정적 장식으로 넘기지 않았다. `beached_skiff_3x2`는 육지 수리선이며 실제 1칸 나룻배가 아니다. 물칸과 기존 W/S 다리 경계 난간에는 새 장식을 승인하지 않는다.

## 새 26종과 기존 시각물 검토

현재 `twilight_sea.asset`의 `decorations`는 **0개**다. 따라서 실제 배치 ID의 자동 이관은 0건이다. 아래 표는 제작기에서 실제 선택되는 새 key와 footprint이며, 원본 파일명만으로 저장 ID를 만들어 이관하지 않았다.

| 분류 | 새 key | 지면 |
|---|---|---|
| 연결 | `rope_dock_fence`, `driftwood_palisade`, `fishing_net_screen`, `raised_dune_grass`, `coral_rock_border`, `harbor_lantern_channel` | 각 셀 1×1; 같은 family/group/layer/height의 N1 E2 S4 W8 |
| 다칸 | `harbor_market_stall_3x2`, `beached_skiff_3x2` | 각 3×2 |
| 다칸 | `fisher_workbench_3x1`, `driftwood_bench_3x1`, `net_drying_rack_3x1` | 각 3×1 |
| 다칸 | `cargo_pallet_2x2`, `folded_sail_store_2x2`, `tidepool_garden_2x2` | 각 2×2 |
| 다칸 | `shell_display_2x1`, `picnic_setting_2x1`, `dock_notice_board_2x1`, `shore_steps_2x1` | 각 2×1 |
| 한칸 | `dock_lantern`, `mooring_bollard`, `flotsam_bundle`, `coral_planter`, `shell_cairn`, `message_post`, `folded_deck_chair`, `beacon_marker` | 각 1×1 |

기존 V01 카탈로그의 12개 소품은 아래처럼 실제 Unity GUID/fileID를 조사했다. `props/coast/` 경로와 배치는 모두 현재 0건이다. 예를 들어 옛 `driftwood_bench`는 2×1, 새 `driftwood_bench_3x1`은 3×1이므로 자동 치환할 수 없다. `rope_fence`는 2×1 이미지였고 새 `rope_dock_fence`는 연결 셀 데이터가 필요하다. `coast_palm`은 기존 움직이는 야자수 리그와 혼동하지 않는다. 나머지도 외형·지면을 실제 배치마다 검토할 때만 옮길 수 있다.

| 옛 `props/coast/*.png` | GUID (fileID는 모두 21300000) | 자동 매핑 |
|---|---|---|
| broken_stone_pillar | deba35cd80c91654981ee3951cc6951a | 없음 |
| coast_palm | 957159a205abb1346b14700dbd496086 | 없음; 움직이는 리그 유지 |
| coral_fan | ad379929085556448a450526b12815c7 | 없음 |
| dock_crates | 3852dfc07aaa1614fb9b42d526cd1f8e | 없음; 옛 2×1 |
| dock_lantern | a5b071c44bdeb544ab36e37dbf17f9a1 | 없음; 새 한칸 후보만 확인 |
| driftwood_bench | 1ba89c125fa461b47809f92f4c04e2aa | 없음; 2×1→3×1 검토 필요 |
| folded_net_basket | 16a949687fb5df74eb6272e74a6152dd | 없음 |
| mooring_post | b89c002321026a048b97bf92ebfa72a0 | 없음; `mooring_bollard` 후보만 확인 |
| raised_sea_agave | 90b6fb8187005364cb0daa947c538c13 | 없음 |
| rope_fence | 6a06c091fc14be4408a01eb049567c6a | 없음; 2×1→연결 데이터 검토 필요 |
| slate_shell_rocks | 9248b585b3eebbb44a7a58349d407de5 | 없음 |
| small_lighthouse | 580c76e3efd36b84d8c4a20b83920d7b | 없음 |

옛 12 GUID의 텍스트 Scene/Prefab/SO 검색 결과는 기존 `StarSwellCoastV01VisualCatalog.asset` 한 곳뿐이다. 이 카탈로그는 지형·장치 아트에도 사용되므로 삭제하지 않았다. 별도 `TwilightCoast` QA 씬/Prefab/EnvironmentArt에는 이전 3개 일반 해안 소품 참조가 남아 있으며 이번 생산 장식 이관 범위 밖이다. 원격/Addressables/캐시/풀에서 **전체 옛 참조 0**은 증명하지 않았다. 실제 기존 맵 장식 배치가 0이므로 dry-run 삭제 대상도 없다. `Docs/QA/CoastConnectedDecorV01/legacy_sprite_ids.json`에 실제 ID 읽기 결과를 보존했다.

## Unity 실행과 자산 ID

연결된 실제 Editor에서 `CoastConnectedDecorV01BoardTools.BuildExamples(manifest, stage_snapshot, all26_snapshot, "Default", true)`를 Unity CLI `run_script`로 실행했다. 열린 `ANIMOL_Starway_ConnectedDecor_QA` 씬은 깨끗한 상태였고 생성 뒤에도 생산 씬/BuildSettings는 수정하지 않았다. CatalogBuilder가 정확한 manifest를 임포트했고 생성 Prefab은 `Binding.Bind/RefreshNow`, 계획 수, 활성 Renderer/Sprite를 통과했다.

| 자산 | 실제 GUID:fileID |
|---|---|
| Catalog | e91329ae1a93b994d9a6b5f330989280:11400000 |
| Stage BoardAsset / Prefab / Scene | 5a7b6fd530f31684fb487fcfe250564a:11400000 / fd8e9084235466f428d072af36bf6d2d:9168428368161719446 / 7fe0aa2e0705c324f8b24a019f98dfa2:102900000 |
| All26 BoardAsset / Prefab / Scene | 5169ac0e59ed74040b48dd3c3c39b64c:11400000 / 43afab4dfc6afc64ca602d35fee140b4:5979310214496337536 / cb51cd9dbaf113444ada577abe181903:102900000 |
| pin TextAsset | 7d8552edb5e5d9e4980e26478ed324cc:4900000 |
| manifest SHA256 = pin | `e97145e09c089d39307f1cad34190ac1350eaaab4f2cfc1266bebfa668897767` |

실제 Editor 감사: Catalog Sprite 118개/묶음 20개, `Single/FullRect/PPU256/Bilinear/Clamp/Uncompressed/mipmap 없음`, 선언된 pivot, Sprite GUID/fileID와 Catalog 일치. importer 오류 0, Sprite 메타 오류 0, 검사 씬 Missing Script 0/Missing Sprite 0. Stage는 network55/group20/claim104, All26은 network78/group20/claim127. [Unity 자산 감사](Docs/QA/CoastConnectedDecorV01/unity_asset_audit.json), [생성 보고서](Assets/ANIMOL/Art/ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01/_Generated/InspectionBoards/inspection_build_report.json), [컴파일 상태](Docs/QA/CoastConnectedDecorV01/compile_status.json).

| 실제 검증 | 결과와 증거 |
|---|---|
| C# 컴파일 | **PASS** — Unity `recompile_status.completed`, `compilationFailed:false`, errors 0 |
| 새 장식 EditMode | **PASS** — 55/55: 엄격 JSON/복사/부분 거절/중복 ID·셀/핀 오류/부모 변환/구독/풀/실제 제작기 팔레트·허용 정책. [결과](Docs/QA/CoastConnectedDecorV01/editmode_test_status.json) |
| 새 장식 PlayMode | **PASS** — 3/3: 두 검사 Scene 실제 Sprite, 생산 프로토타입 Publish→Binding→Controller, 비용 유지, 재활성 fresh Publish. [결과](Docs/QA/CoastConnectedDecorV01/playmode_test_status.json) |
| 해안·공통 장치 EditMode | **PASS** — 해안 11/11, 공통 장치 57/57. [해안](Docs/QA/CoastConnectedDecorV01/coast_logic_regression.json), [공통](Docs/QA/CoastConnectedDecorV01/device_logic_regression.json) |
| 다른 테마 공용 경로 | **PASS** — Ranch/Theater/Sanctum/Coast 호스트 18/18, Theater/Sanctum/Coast Play 9/9. [호스트](Docs/QA/CoastConnectedDecorV01/other_theme_host_regression.json), [Play](Docs/QA/CoastConnectedDecorV01/other_theme_play_regression.json) |
| 기존 해안 PlayMode | **PASS** — 2/2. 기존 한 테스트는 통로 칸에 정지한다는 오래된 기대값으로 처음 실패했다. 현재 확정 규칙인 문을 통과해 출구 칸에 바로 도착하고 닫힌 시각이 되는 것으로 테스트만 수정해 재실행했다. [결과](Docs/QA/CoastConnectedDecorV01/coast_play_regression.json) |
| 실제 게임 화면 | **PASS** — Play 중 현재 `twilight_sea` 런타임 맵의 허용 육지 (1,1)에 임시 `dock_lantern` 배치, Renderer 1, Cost 0 유지. 맵 자산은 변경하지 않았다. [화면](Docs/QA/CoastConnectedDecorV01/prototype_live_lantern.png) |
| 검사 화면 | **PASS** — [Stage](Docs/QA/CoastConnectedDecorV01/stage_play.png), [All26](Docs/QA/CoastConnectedDecorV01/all26_play.png) 실제 Play 캡처 |
| 실제 화면 갱신 영상·그리드 확대 캡처 | **NOT_RUN** — 정지 화면과 호출·TestRunner 근거까지만 기록했다. |

## ZIP 복구와 이번 추가 검증 (2026-10-27)

첫 적용 때 전달된 ZIP은 끝부분이 잘려 51,153,744바이트였고 322개 항목만 CRC 검증 후 추출할 수 있었다. 당시 [오프라인 검사](Docs/QA/CoastConnectedDecorV01/pristine_offline_audit.json)의 **FAIL_OFFLINE**은 당시 손상본의 실제 결과다. 이 기록은 삭제하지 않는다.

이번에 받은 동일 파일명 ZIP은 **62,639,527바이트, SHA-256 `085058cb2642fa281a5e6a2550ea7731176988822d440ab40dc01321a892f400`**으로 사용자 제공 정상 값과 일치했다. Assets 밖의 `Temp/CoastRepairedZipExtraction`에 안전한 경로만 추출했다. ZIP 330개 항목은 중복/누락 없이 전체 CRC를 통과했고, `package_checksums_sha256.txt`의 329개 해시도 전부 일치했다. PNG 199개 디코딩/JSON 81개 파싱 후 제공 독립 검사기를 그 추출본에서 실행해 **PASS_OFFLINE, 2,269개 검사, 오류 0, 경고 0**을 얻었다. [이번 원본 패키지 검사 보고서](Docs/QA/CoastConnectedDecorV01/COAST_REPAIRED_PACKAGE_QA.json)는 패키지 밖에 보관했다.

기존 설치 위치의 `packageId=ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01`, manifest SHA-256 `e97145e09c089d39307f1cad34190ac1350eaaab4f2cfc1266bebfa668897767`, 런타임 PNG 118개 바이트 일치를 확인했다. 기존 330개 배포 파일과 대조하면 의도적으로 프로젝트에 맞게 고친 C# 3개(`BoardTools`, `Binding`, `SnapshotSource`)만 달랐다. 이 파일은 덮어쓰지 않았다. 이전에 없던 아래 8개만 정확한 원본 바이트로 보충했다. 기존 설치 폴더와 기존 `.meta`/생성 자산은 유지했다.

| 이번에 보충한 경로 | 결과 |
|---|---|
| `sources/harbor_lantern_channel_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/harbor_market_stall_3x2_draft_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/harbor_market_stall_3x2_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/net_drying_rack_3x1_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/raised_dune_grass_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/rope_dock_fence_parts_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sources/shell_display_2x1_native.png` | 원본 SHA 일치, Unity 신규 `.meta` |
| `sprite_import_settings.json` | 원본 SHA 일치, Unity 신규 `.meta` |

Unity에서 보충 8개를 실제 임포트했고 sources PNG 7개는 Sprite로 등록되지 않았다. 기존 Catalog GUID `e91329ae1a93b994d9a6b5f330989280`, Stage Scene GUID `7fe0aa2e0705c324f8b24a019f98dfa2`, All26 Scene GUID `cb51cd9dbaf113444ada577abe181903`이 그대로다. [이번 임포트 감사](Docs/QA/CoastConnectedDecorV01/repair_unity_import_audit.json).

이번 Refresh 후 첫 재컴파일에서 앞선 커밋의 두 C# 파일 끝에 잘못 남은 문자 때문에 CS1056/CS0116이 드러났다. 두 파일의 끝 문자만 바로잡고 재컴파일했다. **최종 컴파일 PASS**, 오류 0이며 [이번 컴파일 상태](Docs/QA/CoastConnectedDecorV01/repair_compile_status.json)에 남겼다. 이 문자 수정은 이번 복구 커밋에 포함한다.

이번 실행의 독립 결과는 **EditMode 55/55**, **PlayMode 3/3** 통과다. 이전 실행 로그를 재사용하지 않았다. [이번 EditMode](Docs/QA/CoastConnectedDecorV01/repair_editmode_test_status.json), [이번 PlayMode](Docs/QA/CoastConnectedDecorV01/repair_playmode_test_status.json). 실제 `twilight_sea` Play에서 허용 육지 (1,1)에 임시 등불을 넣어 Renderer 1개와 Cost 0을 다시 확인했고 맵 자산은 변경하지 않았다. [이번 호출 결과](Docs/QA/CoastConnectedDecorV01/repair_live_play_audit.json), [이번 화면](Docs/QA/CoastConnectedDecorV01/repair_prototype_live.png).

### 남은 확인

- **NOT_RUN:** 이번 복구 턴에서 장시간 수동 플레이의 모든 나룻배 탑승/하선·폭탄 카운트 경로, 변경 영상, 원격/Addressables 자산의 별도 도달 가능성 감사. 해당 장치 소스/자산은 이번에 수정하지 않았다.
