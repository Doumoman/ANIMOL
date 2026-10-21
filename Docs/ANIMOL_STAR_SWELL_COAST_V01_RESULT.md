# ANIMOL 별너울 해안 V01 실제 적용 결과 (2026-10-21)

## 적용 범위와 원본 보존

- Unity **6000.3.8f1**, Built-in Render Pipeline, orthographic 플레이 카메라에서 작업했다. 작업 시작 시 약 811개의 기존 미커밋 변경이 있었고 이번 변경에 섞거나 되돌리지 않았다.
- `MCP_INBOX`의 `01_CORE`·`02_FLOOR`·`03_BRIDGES`를 하나의 `Assets/ANIMOL/Art/ANIMOL_STAR_SWELL_COAST_CARTOON_V01` 폴더에 설치했다. `04_SOURCES`는 Assets 밖의 임시 검수 폴더에 합쳐 전체 원화 검수에 사용했다. 패키지 PNG의 RGB/alpha는 변경하지 않았다.
- 기존 `twilight_sea` key, 맵과 저장 식별자, 시나리오의 ID·방향·점유·폭발 offset·비용·승리 규칙, 현재 해안 캐릭터 카탈로그를 유지했다. 성소·목장·극장과 공용 자산을 삭제하지 않았다. 변경 전 테마와 시나리오는 `Assets` 밖의 `%LOCALAPPDATA%/Temp/ANIMOL_CoastV01_20261021/project_backup`에 보관했다.
- 실제 해안 시나리오의 V01 대상 14개 Prefab 참조를 새 `StarSwellCoastV01Device.prefab`으로 바꾸고 테마에 최신 catalog와 배경 layout을 연결했다. 구 `TwilightCoastArt`, 일반 지형·배경 fallback, 해안에 필요 없는 옛 beam prefab 참조를 이 테마/시나리오에서 해제했다. 캐릭터·HUD·출구·공용 고체 벽은 보존했다.

## 실제 계약 매핑

`PuzzleDeviceSession`의 실제 `Footprint`를 조회했다. 아래 모든 장치의 점유는 **표시된 한 칸**이다. `+Y`가 North이고, 방향 `(1,0)`은 East다. 도감 번호가 아닌 실제 `PrototypeRole` enum과 ID를 사용했다.

| 실제 ID | Role | 앵커/점유 | 방향·상태 연결 |
|---|---|---|---|
| `0_spikes` | DepartureSpikes (5) | (2,2) | flat → occupied_armed → departure_rise → raised |
| `1_belt1`, `1_belt2` | Conveyor (7) | (2,10), (3,10) | East idle/move |
| `2_gate` | OneWayGate (9) | (3,18) | East closed/opening/open/closing/blocked; 출구는 점유 아님 |
| `3_boat` | Boat (11) | (2,26) | 실제 진행 East, 같은 배우가 배와 함께 이동 |
| `4_barrel` | Barrel (12) | (2,34) | East roll/settle; 기존 host 경로 사용 |
| `4_stopper` | Wall (100) | (6,34) | 공용 고체 벽. 패키지에 대응하는 별도 완성 벽 원화가 없어 유지 |
| `5_bomb` | Bomb (15) | (2,42) | host timer 3/2/1/폭발과 exact V01 클립 |
| `5_cracked` | CrackedWall (101) | (4,42) | host 파괴 확정 뒤 break → open |
| `6_belt`, `6_bounce`, `6_brake` | Conveyor (7), Bounce (18), Brake (19) | (2,50), (3,50), (3,51) | East, 실제 orientation 2 → SE, horizontal |
| `7_belt1`, `7_belt2`, `7_brake` | Conveyor (7), Brake (19) | (2,58), (3,58), (4,58) | East, horizontal |

`PuzzlePrototypeController`의 단일 커밋 경로가 이동·충돌·비용·자동 연쇄를 계속 소유한다. V01 어댑터는 확정 이벤트와 상태를 정확한 clip key에만 전달한다. `PlayForCommittedSequence`는 유효 행동에 사용하고, 거절 표시만 별도 재생한다. 배에서는 기존 플레이어의 실제 SpriteRenderer와 Transform을 계속 사용하며 복제 배우를 만들지 않았다. 문은 실제 배우가 문 칸에 있는 동안 열려 있고 이탈 완료 뒤 닫힌다. 깨진 벽은 host가 제거를 확정한 뒤 V01 break를 보여주고 open 상태를 유지한다.

지형은 기존 육지 `Grass → coast_sand`, `Sand → wood_bridge`, `Stone → stone_bridge`로만 매핑하고 물은 육지 8이웃에서 제외했다. 전체 맵으로 `full256` 마스크와 안정된 2~4칸 연속 하부를 고른다. 기존 `scenario.water`의 칸에만 256×256 carrier와 마스크 안 파도·해안선을 표시하며, 별도의 원경 바다는 한 world 수평선 아래를 채운다. 단일 해와 수평선은 world 고정, 별·구름·섬은 독립 sorting/parallax다.

## 자산·참조 검증

| 항목 | 결과 | 근거 |
|---|---|---|
| 01~03 runtime 패키지 QA | **PASS** | 1097 PNG decode, 105 clips, 1097 importer entries, alias/지형/물/layout 검사. [원본 JSON](QA/StarSwellCoastV01_20261021/coast_runtime_check.json) |
| 04 포함 full QA | **PASS** | native source 27개, source/production 참조 81개 검수. 외부 옛 frozen export와의 비교는 제공되지 않았다. [원본 JSON](QA/StarSwellCoastV01_20261021/coast_full_check.json) |
| Catalog SHA/pin | **PASS** | 둘 다 `1ed830968ef21165b574f9534903fd2a7ad94957c381aed1879d319f2bffa9d2`. Unity catalog fingerprint `9e9b4520b588839b1262b7f0f9e21ac5f63f99b8f388e32035037499f6fa499f`. |
| Unity import/생성 | **PASS** | `_Generated/StarSwellCoastV01VisualCatalog.asset`, 105 `.anim`, 1097 GUID/fileID 확인. 첫 프레임 `animation/coast_palm/wind/frame_00.png` GUID `9ffe24e3dd72b05419ea29b30d8d61d4`, fileID `21300000`. 1097 importer 설정 불일치 0. [Unity 원본 응답](QA/StarSwellCoastV01_20261021/import_audit.json) |
| 생성 clip 범위 | **PASS** | 105개 모두 SpriteRenderer `m_Sprite`만 키프레임으로 소유, animation event 0, Transform 이동 0. |
| Unity C# 컴파일 | **PASS** | `recompile_status` failed=false, compilationFailed=false, console error 0. |
| Editor NUnit | **PASS** | 패키지 순수 계약 11/11, 기존 퍼즐 장치 44/44. [11개 로그](QA/StarSwellCoastV01_20261021/CoastLogicEditMode.json), [44개 로그](QA/StarSwellCoastV01_20261021/PuzzleDeviceEditMode_final.json) |
| Play Mode | **PASS** | 실제 해안 씬 전용 2/2(가시판·문·물 마스크·V01 바인딩), 기존 네 테마 전체 경로 3/3, 네 테마 씬 로딩 1/1. [해안 로그](QA/StarSwellCoastV01_20261021/CoastPlayMode_gate_final.json), [전체 경로 로그](QA/StarSwellCoastV01_20261021/PrototypePlayMode_gate_final.json), [씬 로그](QA/StarSwellCoastV01_20261021/PrototypeScenePlayMode.json) |
| 실제 배/폭탄 화면 | **PASS** | [승선](QA/StarSwellCoastV01_20261021/boat_boarded.png), [이동](QA/StarSwellCoastV01_20261021/boat_ride.png), [하선](QA/StarSwellCoastV01_20261021/boat_landed.png), [3](QA/StarSwellCoastV01_20261021/bomb_3.png), [2](QA/StarSwellCoastV01_20261021/bomb_2.png), [1](QA/StarSwellCoastV01_20261021/bomb_1.png), [폭발 후](QA/StarSwellCoastV01_20261021/bomb_after.png), [물/육지](QA/StarSwellCoastV01_20261021/water_cells_clipped.png). 실제 폭발 뒤 cost 4, 폭탄/벽 removed=true, 바인딩 누락 0. |

이전 `TwilightCoast` 아트 폴더 종속성은 활성 해안 씬·맵·테마·시나리오 범위에서 **0**이다. 같은 범위에 공용 UI/캐릭터/출구·장식 10개와 기존 `twilight_sea` 고체 벽 1개가 남는다. 고체 벽에는 대응 V01 Sprite가 없으므로 다른 장치로 잘못 바인딩하지 않았다. [종속성 조사](QA/StarSwellCoastV01_20261021/legacy2.json). 기존 Prefab의 옛 Sprite를 덮어쓰는 Animator, Addressables, SpriteAtlas 사용 경로는 활성 V01 바인딩에 없으며, V01 대상은 단일 `StarSwellCoastV01AnimatedVisual`이 Renderer를 소유한다.

## 남은 검증

- **NOT_RUN:** 제공된 `actual_gameplay_checks_NOT_RUN.json`의 76개 항목을 각각 독립 시나리오로 실행한 것은 아니다. 실제 프로젝트의 전체 해안 경로와 위 테스트·수동 배/폭탄 동선만 PASS다. 활성 시나리오는 대부분 East이며 North/West/South의 실제 호스트 장치·복합 체인·pause/undo/restart/풀 재사용·기기 화면 검증은 남아 있다. [4방향 배](QA/StarSwellCoastV01_20261021/offline_boat_four_headings.png), [4방향 문](QA/StarSwellCoastV01_20261021/offline_gate_contact_sheet.png), [다리](QA/StarSwellCoastV01_20261021/offline_bridge_topology.png)는 **패키지 오프라인 검토 자료**이며 Unity Play 증거가 아니다.
- **BLOCKED_PRECONDITION:** 실제 `twilight_sea` 맵에는 wood/stone bridge 칸이 없으므로 해당 지형의 실게임 연결·카메라 검증은 불가하다. 별도 완성 고체 벽 V01 Sprite도 없어 `4_stopper`의 옛 참조 1개를 보존했다.
- **BLOCKED_PRECONDITION:** 선택적 `_Generated/StarSwellCoastV01ArtReview.unity` 생성은 연결된 Unity 6 Editor에서 빈 additive scene의 `SceneManager.SetActiveScene`이 false를 반환해 중단됐다. 기존 플레이 씬은 clean 상태로 보존됐고, 이 실패를 실제 게임 적용 PASS로 대체하지 않았다.
- **NOT_RUN:** 새 셰이더가 패키지에 없어 셰이더 컴파일/2D Renderer 패스 테스트는 수행하지 않았다. 실제 기기 빌드·화면비·pause/undo/load·Addressables 런타임 재진입을 개별 실행하지 않았다. 외부 옛 픽셀 baseline과의 RGBA 동일성 비교도 수행하지 않았다.

## 이번 변경 파일

- 신규 패키지 `Assets/ANIMOL/Art/ANIMOL_STAR_SWELL_COAST_CARTOON_V01/` (runtime PNG, Runtime/Editor 코드, manifest, generated catalog/clip/prefab, `.meta`)
- 실제 연결: `PuzzleTheme.cs`, `PuzzleMapView.cs`, `PuzzleDeviceView.cs`, `PuzzlePrototypeController.cs`, 신규 `StarSwellCoastV01PuzzleBinding.cs`, `StarSwellCoastV01MapRenderer.cs`, `StarSwellCoastV01ProjectInstaller.cs`, 해당 asmdef
- 직렬화 참조: `twilight_sea_Theme.asset`, `twilight_sea_Scenario.asset` (다른 themeId의 단일 장치 연습 시나리오는 변경하지 않음)
- 테스트: `StarSwellCoastV01LogicTests.cs`, `StarSwellCoastV01PlayTests.cs`, `PuzzleDeviceLogicTests.cs`, PlayMode asmdef
- 검증 산출물: `Docs/QA/StarSwellCoastV01_20261021/`
