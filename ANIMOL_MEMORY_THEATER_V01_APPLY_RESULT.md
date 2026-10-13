# 기억 극장 실제 적용 결과

2026-10-13, Unity 6000.3.8f1, 연결 Editor 7800, 현재 Android Build Target. 기준 커밋 `8c7a409a41a047e433f60f4f6e285072f24a01d7`.

**기억 극장 아트·테마 데이터·프리팹·QA 씬 및 실행 검수를 완료했다. 현재 활성 프로젝트에는 퍼즐 플레이 씬/게임 판정 구현이 없어 실제 게임 사건 연결은 미완료다.** QA 보드는 전시용이며 preview/PLAYFIELD.png를 새 레벨로 구현하지 않았다. 기존 V04와 장치8 아트/프리팹/씬/데이터를 보존했다.

## 조사 및 기존 구현

AGENTS.md 전체와 ProjectVersion/패키지, Scripts/Scenes/Data를 확인했다. 활성 씬은 기존 ANIMOL_Starway_Art_QA와 ANIMOL_Device8_Art_QA였고 런타임 코드는 표시 컴포넌트/QA/HUD뿐이었다. 테마 선택기, 레벨 데이터, 입력→유효 행동→자동 연쇄→장치 턴→결과 호출자는 없다. 활성 Grid도 0개다. 기존 QA의 1 world unit 셀 간격과 80 PPU 그림을 기준으로 s=1을 사용했다. 실제 게임 Grid/Collider를 생성·변경하지 않았다.

`MCP_INBOX/ANIMOL_MEMORY_THEATER_V01.zip`을 외부 임시 폴더에 해제하고 요청한 README_KO.txt, manifest.json, unity_import_manifest.json, DEVICE_EVENT_BINDINGS.csv 전부, DICE_ORIENTATIONS.json, QA_REPORT.json, ADDED_DECISIONS_KO.txt 및 동봉 적용 지시문 전체를 읽었다. [원문](Docs/Inbox/MemoryTheater)을 보존했다. 제작자의 QA_REPORT/PNG 검사는 아래 실제 Unity 검수와 구분한다. source/preview/reference/시트/GIF/생성 원화는 활성 Sprite로 임포트하지 않았다. 입력 ZIP은 커밋에 포함하지 않았다.

시작 시 Assets는 Git 변경이 없었고 사용자 ProjectSettings 및 레거시 미커밋 작업을 보존했다. [기준 바이트](Docs/Validation/MemoryTheater/baseline.json)의 새 아트 복사 전 샘플에서 이미 초기 패치가 적용된 깨끗했던 C# 3개는 최초 HEAD의 원본 해시로 보정했으며 이 시점을 메타데이터에 명시했다. 폰트/기존 아트/설정은 실제 작업 시작 바이트로 비교했다.

## 자산과 경로

테마 ID는 사용자 지정 `memory_theater`다. 기존 Sprites 루트와 M## 장치 등록표를 유지하면서 요청한 공통/전용 테마 경로를 추가했다. 이전 ranch/common 아트를 이동하거나 덮어쓰지 않았다. 활성 이름에는 V01 폴더를 붙이지 않았다.

| 분류 | 실제 루트 |
|---|---|
| 공통 4종 변형 | Assets/ANIMOL/Sprites/Obstacles/common/memory_theater/M##_EnglishName/Frames, Animations |
| 전용 7종 | Assets/ANIMOL/Sprites/Obstacles/memory_theater/M##_EnglishName/Frames, Animations |
| 바닥·벽 | Assets/ANIMOL/Sprites/Tiles/Floor/memory_theater, Tiles/Wall/memory_theater |
| 상자 | Assets/ANIMOL/Sprites/Props/memory_theater |
| 배경·막·기둥·등·테두리 | Assets/ANIMOL/Sprites/Background/memory_theater/Far, Mid, Near |
| 프리팹 | Assets/ANIMOL/Prefabs/Themes/memory_theater |
| Sequence | Assets/ANIMOL/Data/AnimationSequences/memory_theater |
| 테마/주사위 데이터 | Assets/ANIMOL/Data/Themes/memory_theater.asset, memory_theater_dice.asset |

예: `spr_obstacle_memory_theater_m22_dice_block_roll_t1_n5_e3_e_f00.png`, `anim_obstacle_memory_theater_m08_rotating_bookshelf_rotate_0_cw.anim`. 프레임은 f00부터 2자리다. 모서리 nw/ne/se/sw와 책장 0/90/180/270은 실제 상태/외형 식별자로 보존했다.

**정적 PNG 143 + 프레임 1242 = PNG 1385, Sequence/AnimationClip 각각 211, Prefab 365**(장치 전체 상태 11 + 정적 역할 143 + 클립 역할 211). 원본→정본 전수 경로·해시는 [source_path_map.json](Docs/Validation/MemoryTheater/source_path_map.json)에 있다. 기존 포함 정본 총량은 PNG 2016, 클립 285다. 같은 칸에 시트/프레임 또는 단일 PNG/클립을 겹쳐 연결하지 않는다. 개별 역할 프리팹은 QA/정적 상태/부품 배치용이다.

## 임포트 / 픽셀 / 피벗

1385 PNG SHA-256이 원본과 일치한다. 불투명 RGB는 Sweetie16의 정확한 16색 안에 있고 alpha는 0/255, 투명 RGB는 0이다. 축소/팔레트 재변환을 하지 않았다. 96px 캔버스와 80px 논리 셀을 구분했다. 바닥의 실제 불투명 영역은 (8,8)~(88,88) 즉 80×80이며, 가로/세로 반복 경계 색과 완전 불투명을 확인했다. PNG 자체를 자르지 않는다.

정규화 피벗은 (pivotX/width,1−pivotY/height). 책장 256×256 중앙 (128,128)→(0.5,0.5), 인형 (48,82)→(0.5,0.1458333), 막 (64,17)/128×192→(0.5,0.9114583). 모든 개별 피벗·크기를 Unity에서 검사했다. PPU=80/s=80, Point, Sprite Single/FullRect, 무압축, mipmap 없음, Clamp/NPOT 없음, alpha transparency, max 8192, 모바일 축소 override 없음. 기존 일괄 임포터에 기억 극장 매니페스트 조회를 추가해 32 PPU 기본값 덮어쓰기를 막고 재임포트했다.

1080×1920 카메라 orthographicSize=12이므로 1-cell=80 screen pixels, 새 PNG는 정수 1x다. 책장 4x 상세는 1920 정사각 캡처/size3이며 256px 전체 캔버스를 유지한다. 책장/막을 셀 마스크/RectMask로 자르지 않는다. 평평한 정사각 보드이며 원근을 적용하지 않았다.

대표 47개(책장/문/모서리/인형/막 중간 프레임)를 Android 대상 SpriteAtlas로 실제 패킹했다. Point, RGBA32 무압축, mipmap 없음, tight/rotation 없음, padding4, 1페이지에서 불투명 픽셀 새 RGB 0을 확인했다. [검사](Docs/Validation/MemoryTheater/atlas_qa.json), [패킹 원본](Docs/Validation/MemoryTheater/atlas_snapshot.spriteatlas), [Atlas 페이지](Docs/Validation/MemoryTheater/atlas_page_0.png). 이 Atlas는 검수용 임시 자산이며 최종 활성 Assets/빌드에는 없다.

실제 Game View에서 검수용 excluded Atlas가 일부 static Sprite를 런타임에 비워 버리는 문제를 발견했다. Atlas 스냅샷을 Assets 밖으로 보존하고 검수용 자산을 제거한 뒤 관련 원본을 재임포트해 바인딩을 복구했다. PlayMode 검수에 **모든 상태/프레임의 sprite.texture 비누락 검사**를 추가하고 다시 통과시켰다. 실제 화면에서 재확인한다. Atlas를 쓰는 향후 빌드는 별도 실제 포함/바인딩 검증이 필요하며 임시 pack 성공을 실기기 완료로 간주하지 않는다.

## 표현 및 사건 연결점

공통 07/09/18/19는 기존 장치8 프리팹에서 생성한 실제 Prefab Variant다. 전용 7종도 기존 DeviceVisualView/SpriteFrameSequence를 재사용한다. Renderer 하나와 프레임 플레이어 하나만 있고 Animator/추가 타이머/이벤트 구독을 만들지 않았다. 각 상태에 PNG·Sequence·동일 프레임 AnimationClip·CSV 사건 문자열·완료 Sprite를 직렬화했다. 클립 211개는 AnimationEvent 0개다. JSON 프레임별 duration/start를 보존하고 1000 sampleRate의 종료 키 보정으로 정확한 총 길이를 유지한다.

`DeviceThemeCatalog.Apply(view,deviceId)`는 같은 장치의 아트 데이터만 교체한다. Root/Device ID, 표시 상태·경과시간·pause와 이미 소비한 행동 ID를 보존한다. 추가 Renderer/구독이 없다. `DeviceVisualView.Notify(actionId,event,state)`는 최근 256 사건의 동일 ID+사건+상태 중복을 거부한다. Retry/OnEnable/ResetVisual에서 캐시를 초기화한다. QA 재생 버튼은 기존 ID 없는 표시 API를 사용해 의도적인 미리보기를 반복할 수 있다.

실제 게임 호출자가 생기면 비용/턴/좌표를 커밋한 뒤 CSV 사건을 위 API에 연결해야 한다. 위치 보간은 기존 엔진의 from/to가 없으므로 이번에는 연결하지 않았다. 무대/주사위/석상/인형 Root는 고정이고 프레임이 논리 이동을 만들지 않는다. BlocksInput=false, Time.timeScale 변경 없음. 예고/idle 시간으로 셔터 위상·석상 돌진·인형 행동을 확정하지 않는다. 오디오/햅틱 자산/서비스가 활성 코드에 없어 임의 효과를 추가하지 않았다.

`DiceOrientationTable`은 전체 6면을 보존하는 24상태 데이터와 96전이 클립을 참조한다. FindFaces는 top만 비교하지 않는다. PresentCommittedRoll은 권위 있는 from/to+방향과 전이표가 일치할 때 해당 roll 클립만 선택한다. 주사위 논리 상태나 위치를 스스로 굴리지 않는다. 무효 입력·순간이동은 게임 호출자가 roll을 호출하지 않아야 하며 실제 게임 통합 검수는 미실행이다.

## 11종 평가

| 장치 | 정적/클립 수 | 표현 연결·개선 | 기존 퍼즐 구현 / 적용 | 게임 연결 난이도 / 우선순위 |
|---|---:|---|---|---|
| 07 무빙워크 | 4 / 4 | 4방향 run, 기존 프리팹 Variant | 없음 / 표시 연결 완료 | 낮음 / P1 |
| 09 단방향 문 | 8 / 12 | open/close/blocked·terminal, 기존 Variant | 없음 / 표시 연결 완료 | 중간 / P1 |
| 18 튕김 모서리 | 4 / 4 | 4모서리 impact, 의도된 빈 홈, 기존 Variant | 없음 / 표시 연결 완료 | 낮음 / P1 |
| 19 제동판 | 4 / 2 | H/V brake·idle, 기존 Variant | 없음 / 표시 연결 완료 | 낮음 / P1 |
| 04 교대 셔터 | 8 / 16 | A/B×H/V open/close/warning/blocked_open | 없음 / 표시 연결 완료 | 높음 / P0 |
| 06 이동 무대 | 12 / 8 | 고정 rail과 표시 root 분리, advance/blocked | 없음 / 표시 연결 완료 | 중간 / P0 |
| 08 회전 책장 | 8 / 10 | 중앙 피벗·완료 4상태·8회전·발판 | 없음 / 표시 연결 완료 | 높음 / P0 |
| 13 도미노벽 | 8 / 4 | 조각별 fold 종료 folded 유지 | 없음 / 표시 연결 완료 | 중간 / P0 |
| 17 돌진 석상 | 16 / 24 | warning hold가 시간만으로 charge하지 않음 | 없음 / 표시 연결 완료 | 높음 / P0 |
| 22 주사위 블록 | 43 / 108 | 전체 6면·24상태·96전이 렌더 어댑터 | 없음 / 표시 연결 완료 | 높음 / P0 |
| 23 겁쟁이 인형 | 16 / 16 | 4방향 idle/startle/retreat/blocked·발 피벗 | 없음 / 표시 연결 완료 | 중간 / P1 |

셔터 점유 보류 위상, 무대 막힘 이후 방향/재시도, 책장 점유 충돌/반복, 도미노 차단, 석상 실제 정지/접촉, 인형 실제 인접·후퇴 정책은 활성 코드에 없어 미정이다. 압착/추가 코스트/먼 경로/스태미나 등 새 규칙을 넣지 않았다. NW N→E/W→S 등 외형-벡터 표는 문서와 아트에 보존했지만 기존 반사 enum이 없어 게임 대조는 미실행이다.

## 씬 / 현재 실행

새 씬 `Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Art_QA.unity`: 11종 전체 상태 프리팹, 고정 레일, 배경·막·등·파괴 표시, 모든 143 static+211 clip 역할, 기존 rabbit_front/soap_bubble_pink/exit_visual_root/hud_visual을 실제 참조한다. 총 DeviceVisualView 369개다. 메인 전시 페이지와 상태 갤러리 15페이지를 기존 QA 컨트롤로 선택할 수 있다. 상태/장치 선택·preview/pause/restart/reset은 검수 조작이다.

상단 pause·cost·bubble UI는 기존 hud_visual을 그대로 사용하며 COST -- / --/--로 미연결 상태를 표시한다. 플레이어·분홍 방울·출구와 기존 QA 씬/빌드 씬 설정은 바꾸지 않았다. 에디터는 새 QA 씬과 1080×1920 Game View/Play 시작 씬을 설정했다. **기존 퍼즐 플레이 씬이 없어 실제 플레이 씬에 11종을 연결하는 항목은 수행하지 못했다.**

## 실행 검수 및 증거

| 실제 검사 | 결과 |
|---|---|
| 전체 EditMode | 13/13 통과: V04 4 + 장치8 4 + 기억 극장 5. [최종 로그](Docs/Validation/MemoryTheater/all_editmode.json) |
| 전체 PlayMode | 4/4 통과: V04 1 + 장치8 1 + 기억 극장 2. [최종 로그](Docs/Validation/MemoryTheater/all_playmode.json) |
| 주사위 렌더 데이터 | 24상태/96전이의 실제 clip→terminal Sprite, 6면/반대면 합7, 초기 우4/좌3/상2/하5, 반대 방향 복귀, 같은 방향4번 복귀 통과 |
| 새 실제 씬 | 1080×1920, 11종/369객체, Root 불변, pause/restart/reset, Sprite.texture 누락0, 단일 writer·무 Animator·무 Collider, scene 재진입 객체/컨트롤 잔류 없음 |
| 상태 표시 | warning hold→시간만으로 charge하지 않음, 셔터 blocked_open/도미노 folded/책장 완료 유지, 미확정 사건 거부, 행동 ID 중복 거부 및 reset, 테마 swap pause/시간/ID 유지 통과 |
| 기존 회귀 | 기존 수풀/방울280ms FX 재사용/일시정지/HUD 상태 및 1080×1920·360×640 SafeArea 테스트 포함 |
| 임포트·컴파일 | 1385 Sprite/211클립 검사 통과, 컴파일 오류0. [임포트](Docs/Validation/MemoryTheater/import_validation.json), [최종 Console](Docs/Validation/MemoryTheater/console_final.json) |
| 참조·Material | 씬 누락 스크립트0, 누락/지원 불가 Material0. [씬 검사](Docs/Validation/MemoryTheater/scene_validation.json) |
| 파일 감사 | 원본 hash 일치, 기존 아트/설정 바이트 보존, 기존 meta 변경0, Assets 중복 GUID0, 직렬화 GUID 누락0, 바닥 반복 경계/전체 PNG alpha·팔레트 검사 통과. [감사](Docs/Validation/MemoryTheater/audit.json) |

화면: 48개 1080×1920 시작/중간/종료 포즈 + 8개 책장 회전 4x 상세 + 실제 Play Mode 화면. [전시](Screenshots/MemoryTheater/page_0_pose_0.png), [중간 책장](Screenshots/MemoryTheater/book_rotate_0_cw_mid_4x.png), [실제 Game View](Screenshots/MemoryTheater/qa_playmode.png). 책장 중앙축과 캔버스 잘림, 고정 UI, 열린 홈, 타일 반복을 확인했다. 모든 PNG/프레임은 파일/참조/시간을 전수 검사했으나 211종 전 프레임의 수동 육안 승인까지 했다고 쓰지 않는다.

초기 EditMode terminal 기대값 조회 실패는 독립적인 원본 매니페스트의 Sprite로 기대값을 검사하도록 고쳐 최종 통과했다. 긴 재임포트 한 번은 CLI 60초 응답 제한을 넘겼으나 실제 완료 후 전수 Validate와 최종 테스트를 실행했다. QA Atlas의 실제 표시 실패와 복구는 위에 별도로 기록했다. source=screen 캡처의 `..` 경로는 CLI가 거절했으며 허용한 Assets 임시 경로로 캡처 후 Assets 밖으로 이동했다. 이 초기 시도는 최종 통과 수에 섞지 않는다.

## 미실행 / 다음 작업

실제 비용1/무효0·turn0/자동 연쇄 추가0, 방울3개+출구, 셔터 점유 보호·무대 끝/막힘·책장 충돌·도미노 순차 차단·석상 한 턴 고정 경로·숫자 발판 실제 점유·인형 인접/대각선/막힘/행동당1회 판정은 게임 호출자/엔진이 없어 미연결·미실행이다. 해당 표시 상태/데이터 테스트와 구분한다. 실제 게임 Retry/테마 선택도 없고 QA reset/skin swap/scene 재진입만 검수했다. 모바일 Android 빌드/실기기 실행은 미실행이다. CommercialPolishPolicyTests/과거 로비 테스트는 비활성 레거시라 미실행이다.

1. 즉시 효과 큰 수정: 실제 탑뷰 입력·행동 ID·턴·좌표·결과 이벤트를 구현/확인하고 11종의 CSV 호출자에 연결. P0 미정 충돌 정책을 확정해 플레이 씬/레벨을 구성한다.
2. 적은 비용 개선: 현재 표현 카탈로그를 실제 테마 선택 데이터에 연결하고 비용/HUD·허용한 목적 칸/경고 셀 표시를 권위 상태로 갱신한다. 실제 from/to 보간과 80px 셀 맞춤을 붙인다.
3. 나중 디테일: 실제 빌드용 Atlas의 포함/바인딩 재검수, Android 실기기/다중 해상도, 기존 사운드·햅틱과 충돌 사건 통합, 남은 미술 승인. 원본 책장의 강체 회전 픽셀 형태를 임의 재색상/축소하지 않는다.

## 변경 범위

새 코드: MemoryTheaterArtBuilder, DeviceThemeCatalog, DiceOrientationTable, MemoryTheaterArtTests, MemoryTheaterArtPlayTests. 기존 코드 수정: ArtAssetBuilder의 임포트 조회, DeviceVisualView의 ID 중복 방지/테마 재바인딩, DeviceArtQaController의 제목/페이지/위치 매개변수(기존 기본값 보존). 새 테마 Sprites·프리팹·Sequence·데이터·QA 씬, 원문 Docs, 검증/캡처, Tools/MemoryTheater와 정본 규칙/현재 구조 문서를 추가/갱신했다.

정확한 파일 목록: [changed_files.txt](Docs/Validation/MemoryTheater/changed_files.txt). 사용자 설정/폰트/레거시 변경·입력 ZIP은 커밋에서 제외한다.
