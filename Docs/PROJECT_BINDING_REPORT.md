# 별길 목장 V12 실제 프로젝트 바인딩

2026-10-19 Unity 6000.3.8f1에서 확인. 활성 테마 ID는 `starway`이며 전달물의 `starway_meadow`를 저장 키로 추가하지 않았다. 프로토타입 진입은 `Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity` → `PuzzlePrototypeController.Load` → `PuzzleDeviceView.Bind` → `PuzzleDeviceSession`이다. 캠페인 성격의 기존 맵은 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity` → `PuzzleController.Load` → `PuzzleMapView.DrawDevice`이며 `StarGarden.asset`의 고전 `DeviceCell` 20개를 사용한다. 둘 다 V12 카탈로그를 참조한다.

프로토타입 장치는 `PrototypeDevice.role`과 기존 `id`, `variant`, `direction`, `orientation`, `group`을 그대로 쓴다. 움직임은 `PuzzleDeviceSession`의 확정된 격자 벡터를 `Dir`로 east/north/west/south에 대응시킨다. 저장 enum 숫자는 소스에서 확인한 `PrototypeRole` 값이며 도감 번호를 새 ID로 만들지 않았다. 프리팹의 SpriteRenderer 경로는 모두 `Visual`이다. `DeviceVisualView`가 상태를 받고 `StarwayRanchV12AnimatedVisual`이 Sprite만 쓴다. Animator/OverrideController는 이 8개 원본 프리팹에 없었다.

| 원화 계열 | 실제 역할 / 프리팹 | 기존 이벤트와 V12 바인딩 |
|---|---|---|
| conveyor | `PrototypeRole.Conveyor=7`, `d07_conveyor` | `conveyor_visual_running` → `run_e/n/w/s` → 방향별 `conveyor_*_transport`; 정지는 `idle_*` 첫 프레임 |
| oneway_gate | `OneWayGate=9`, `d09_one_way_gate` | `allowed_passage_begin/clear` → 방향별 opening/closing; 막힌 입력은 기존 `existing_rejected_gate_contact` |
| bounce_corner | `Bounce=18`, `d18_bounce_corner` | `existing_bounce_resolved`; 기존 `orientation` → ne/nw/se/sw rebound |
| brake_patch | `Brake=19`, `d19_brake_pad` | `forced_motion_stopped`; 기존 horizontal/vertical 변형 → press_release |
| star_stone / star_socket | `StarSocket=1`, `d01_star_socket` | `existing_push_visual` → push, `matching_star_socket_committed` → seat; `variant=socket` 유지 |
| pressure_plate / vine_bridge | `VineBridge=2`, `d02_pressure_vine_bridge` | 점유 증감 → plate press/release; 기존 연결 그룹의 enable/disable → 실제 가로/세로 bridge open/close |
| wind_corridor | `Wind=20`, `d20_wind_corridor` | `valid_action_wind_resolved`; 본체 active와 확정 `WindCells`의 별도 flow effect. 동작 후 idle로 복귀하고 effect Renderer를 숨김 |
| chick | `Chick=24`, `d24_follower_chick` | `existing_recorded_path_following`; 확정된 각 Move 벡터별 walk, 정지 시 마지막 방향 idle |

`PuzzleDeviceView`는 별길 목장 8계열을 전용 프리팹으로 생성한다. 에디터의 `PuzzlePrototypeBuilder.Art`도 다음 생성 시 같은 전용 경로를 사용한다. 기존 공유 `Assets/ANIMOL/Prefabs/Devices`는 다른 테마 때문에 그대로 보존했다. 공통 금속 상자에는 V12 원화가 없으므로 공유 원화를 유지한다. 바람 효과는 본체 Renderer와 다른 GameObject/Renderer에만 연결했다. 풀 구현은 현재 활성 경로에서 발견되지 않았다. `PuzzlePrototypeController.Load`가 표현 루트를 다시 만들고 `DeviceVisualView.OnEnable` 및 `PuzzleDeviceView.Settle`이 권위 상태로 복귀시킨다.

고전 `PuzzleController` 경로의 `DeviceKind.Conveyor/Wind/Brake`는 `PuzzleMapView.DrawDevice`에서 V12 방향 클립을 직접 사용한다. 바람은 `PuzzleSession.NextAutomatic`이 이동을 확정한 경우에만 body active와 별도 effect를 시작하고, 그 이동이 끝나면 idle 및 effect off로 복귀한다. 원래 `PuzzleSession`의 좌표·충돌·이동 판정은 변경하지 않았다. `Ice/Portal`은 V12 대상 원화가 없어서 기존 표시를 유지한다.

개별 null/verified 항목을 실제 소스와 프리팹 GUID/fileID로 채운 JSON은 [device_bindings_project.json](Validation/StarwayRanchV12/device_bindings_project.json)에 있다. 참조 이관의 원본/신규 GUID와 fileID는 [프리팹 대응표](Validation/StarwayRanchV12/prefab_guid_fileid_mapping.csv), [V10 PNG 대응표](Validation/StarwayRanchV12/legacy_guid_fileid_dry_run.csv), [V12 프레임 표](Validation/StarwayRanchV12/catalog_frame_guid_fileid.csv), [V12 클립 표](Validation/StarwayRanchV12/catalog_clip_guid_fileid.csv)에 있다.

현재 활성 Assets에서 별길 목장 전용 PvP 진입 씬, Addressables 주소, 원격/AssetBundle 카탈로그 또는 장치 풀이 확인되지 않았다. 해당 실행 경로의 런타임 검증은 수행하지 않았다. 다른 테마의 공유 프리팹과 아트는 수정하지 않았다.
