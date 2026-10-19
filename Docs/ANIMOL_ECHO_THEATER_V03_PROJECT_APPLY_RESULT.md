# 잔영 극장 V03 프로젝트 적용 결과 (2026-10-19)

## 적용 범위

- 전달 ZIP은 `MCP_INBOX/ANIMOL_ECHO_THEATER_CARTOON_V03_INGAME.zip`이다. CRC 검사를 거쳐 Assets 밖 `ANIMOL_EchoTheaterV03_Extracted`에 풀었고, 변경 전 극장 아트·프리팹·카탈로그는 Assets 밖 `ANIMOL_EchoTheaterV03_Before_20261019/theater_art_prefabs_catalog.zip`에 보관했다.
- 런타임 PNG 1,969개를 바이트 그대로 `Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03`에 설치했다. `sources/`, `examples/`, `Tests~/`, 영상 등은 런타임 아트 폴더에 넣지 않았다. 1,969개 경로의 SHA-256와 치수·PPU·피벗은 `EchoTheaterV03ProjectBuilder.ApplyEnvironment()`가 manifest와 대조했다.
- 열린 Unity 6000.3.8f1 Editor에서 V03 빌더를 실행해 187개 프레임 Sprite를 참조하는 카탈로그와 190개 `.anim`, 190개 `SpriteFrameSequence`를 생성했다. [빌드 보고서](../Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03/_Generated/animation_build_report.json)의 성공 기록과 생성 목록을 확인했다. 애니메이션 카탈로그 SHA-256은 `5ca9f27d31e7f7f103a27349090a4803a8c06e7239d194d23c5838410a479752`, 주사위 맵 SHA-256은 `9520c3b8278590579d8fbecbac7bbd362126ada93419ccc962f770508c993a12`다.
- 기존 `EnvironmentArt.asset` GUID와 `memory_theater` ID를 유지하면서 지형·소품·독립 배경 lookup을 V03으로 교체했다. 기존 직교 카메라/맵 점유를 쓰는 렌더러에 V03 8이웃 상면·하부·난간 선택기와 배경 Rig/Cover를 연결했다. V02 메뉴가 활성 V03 카탈로그를 되돌리지 못하도록 막았다.
- 극장 프리팹 315개의 상태 592개를 V03 키/스프라이트/클립/시퀀스로 연결했다. `PuzzleDeviceView.Bind`가 실제 시나리오에서 생성된 핵심 11개 장치의 기존 SpriteRenderer에 V03 재생자를 붙인다. 실제 행동의 시각 이벤트에서 주사위 before/after 6면과 굴림 방향을 대조한다. 인형은 실제 후퇴 벡터와 별도로 플레이어 쪽 시선을 사용한다. 비용·점유·저장 ID·월드 이동 규칙은 바꾸지 않았다.

## 확인된 바인딩과 규칙 차이

원본 템플릿을 프로젝트 소스로 채운 [11계열 바인딩 표](Validation/EchoTheaterV03/device_bindings_verified.json)와 [변경 전후 GUID/fileID 1,474개 슬롯 대조표](Validation/EchoTheaterV03/guid_fileid_migration.csv)를 남겼다. 실제 생성은 `PuzzlePrototypeController.Load` → `PuzzleDeviceView.Bind` → `Instantiate(PrototypeDevice.visualPrefab)`이며, 확인한 활성 코드에는 이 경로의 오브젝트 풀이 없다. `PuzzleMap.scenario`가 없는 기본 런타임 씬은 `PuzzleController` 경로를 쓴다.

| V03 계열 | 실제 `PrototypeRole` | 현재 논리 점유 | 연결 현황 |
|---|---|---|---|
| 07 무빙워크 | `Conveyor` | 1×1 | 4방향 V03 |
| 09 단방향 문 | `OneWayGate` | 1×1 | 4방향 V03 |
| 18 모서리 튕김 | `Bounce` | 1×1 | 4모서리 V03 |
| 19 제동판 | `Brake` | 1×1 | 가로/세로 V03 |
| 04 교대 셔터 | `Shutter` | 1×1 | H/V 단계 프레임 V03 |
| 06 이동 무대 | `MovingStage` | **1×1** | 4방향 `move_wide` V03, 논리 점유 차이 남음 |
| 08 회전 책장 | `Bookshelf` | **3×1 ↔ 1×3** | 회전 프레임 V03, 누름판 및 논리 점유 차이 남음 |
| 13 도미노 벽 | `Domino` | 1×1/넘어진 후 통행 | 방향별 V03 |
| 17 돌진 석상 | `Statue` | 1×1 | 경고·돌진·충돌·복귀 V03 |
| 22 주사위 블록 | `Dice` | 1×1 | 24안정 자세/96굴림 V03, 목표 표시 원화 미대응 |
| 23 겁쟁이 인형 | `CowardDoll` | 1×1 | 시선/후퇴 V03 |

위 굵은 점유는 제공 명세의 06 `2×1`, 08 `2×1 ↔ 1×2`와 **기존 권위 시뮬레이션이 다르다**. 근거는 `PuzzleDeviceSession.Footprint`: 이동 무대는 `s.cell` 한 칸, 책장은 중심과 축 방향 양옆까지 세 칸을 반환한다. 이 작업은 아트 연결 중 게임 규칙을 변경하지 않았다. 따라서 06·08의 아트 범위와 논리 점유가 완전히 일치한다고 판정할 수 없다.

## 실행 검증

| 실제 실행 | 결과 |
|---|---|
| Unity 컴파일/Console | 최종 `compilationFailed=false`, Editor Console 오류 0 |
| V03 EditMode TestRunner | 29/29 통과. 방향·주사위 24×4·카탈로그·누락 키 음성 검사 포함 |
| `PuzzleDeviceLogicTests` EditMode | 43/43 통과. 비용·셔터 점유·석상 직선·주사위 96굴림·네 테마 경로 포함 |
| `MemorySkyTheaterTests` EditMode | 10/10 통과 |
| 극장 프로토타입 PlayMode | 1/1 통과. 실제 생성된 핵심 11계열 Renderer가 V03 경로를 가리키고 누락 바인딩 0 |
| `MemorySkyTheaterPlayTests` | 3/3 통과 |
| `PuzzlePrototypePlayTests` | 3/3 통과. 네 테마의 검증 경로 완주, 극장 포함 누락 바인딩 0 |
| 실제 Editor Game View | 극장 프로토타입을 Play로 실행하고 [1080×1920 캡처](Validation/EchoTheaterV03/theater_v03_game_view_1080x1920.png) 확인. 이후 원래 열려 있던 별길 프로토타입 씬 복구 |

## 남은 문제와 미실행 검사

- 필수 입력으로 지정된 `inspection/atlas_logic_audit.json`은 ZIP에 없다. 이 파일에 의존한 검수는 수행할 수 없었다.
- 책장 누름판 `plate_*`, 주사위 `blank_body`, `next_top_1..6`, `target_1..6`에 해당하는 V03 카탈로그 키/원화가 없다. 70개 보조 상태 alias는 이관하지 않았다. 극장 범위 참조 그래프의 [dry-run 감사](Validation/EchoTheaterV03/legacy_reachability_dry_run.json)에는 **구 자산 100개에 이르는 225개 참조**가 남는다. 따라서 `reachable legacy 0` 및 구 시각 자산 삭제를 완료로 표시할 수 없다. 참조가 남아 있어 삭제는 0개다. `device_bindings_verified.json`의 전체 프로젝트 바인딩 플래그도 false로 유지했다.
- 06·08 점유 차이와 보조 상태 미대응 때문에 모든 방향/상태의 최종 가독성과 접근 가능한 구 외형 참조 0을 검증하지 못했다. 최신 키 누락에 대한 재생기 단위 음성 검사는 통과했지만, 보조 표시에는 아직 기존 시각 경로가 있다.
- 현재 활성 프로젝트에서 확인한 시작 씬은 `ANIMOL_Main`/`ANIMOL_Puzzle` 및 프로토타입·QA 씬이다. `ANIMOL_Puzzle`의 기본 stage는 `StarGarden`; 캠페인·대전·저장 로드·풀 프리웜의 실제 극장 인스턴스 실행 경로는 확인되지 않았다. 이전 프로젝트는 `Legacy/PreviousProject_20261013`에 격리되어 있으며 활성 Assets로 복원하지 않았다.
- 실제 모바일 기기, 장시간 Pause/재개·풀 재사용·씬 재진입·저장 로드, 전체 장치의 중간 동작 영상은 실행하지 않았다. 1080×1920 Game View 캡처는 단일 프레임이며 영상이나 모바일 검증 증거가 아니다.

기존 다른 세 테마의 아트/저장 ID는 이번 변경에 포함하지 않았다. 위 미해결 조건을 해결하기 전에는 V03 전면 교체 완료나 구 외형 참조 0으로 판정하지 않는다.
