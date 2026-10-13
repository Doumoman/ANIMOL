# 편지 천문대 실제 적용 결과

2026-10-14. Unity 6000.3.8f1, 연결 Editor 7800. 기준 커밋 `fa57d6cd6da8b67b706c6f02261b1b720736053e`.

**소스 패키지에서 완성 자산을 재생성해 임포트·테마/프리팹·QA 씬·실행 검수를 완료했다. 실제 퍼즐 로직/플레이 씬은 활성 저장소에 없어 게임 사건 통합은 미완료다.** 기존 별길 목장·장치8·기억 극장과 사용자 변경을 보존했다.

## 이전 입력 문제와 해소

이전 ZIP은 여전히 미완성 상태다. 중앙 디렉터리와 런타임 PNG가 없던 진단은 [이전 기록](Docs/Validation/LetterObservatory/input_audit.json)에 유지한다. 사용자가 새로 제공한 `MCP_INBOX/ANIMOL_LETTER_OBSERVATORY_V01`은 제작 소스 패키지였다. README에 안내한 authored build 절차를 외부 Temp 복사본에서 실행했다. 원화/참고/preview를 Unity에 직접 임포트하지 않았다.

- Node 24.16.0 + 패키지 지정 sharp 0.35.4, 격리 Python 환경의 Pillow/numpy/scipy로 제공 build/document/qa 스크립트를 실행했다.
- 12종 원화와 배경 수정 원화가 존재했다. 새 그림을 생성하거나 임의로 색/크기/게임 규칙을 정하지 않고 동봉된 결정적 제작 스크립트를 사용했다.
- 생성 manifest.json은 손상 ZIP에서 CRC 검증 후 복구한 manifest.json과 **전체 JSON 내용이 정확히 일치**했다. [재생성 증거](Docs/Validation/LetterObservatory/Applied/regeneration.json).
- 동봉 제작 QA의 Windows 경로 비교만 Temp에서 `relative_to().as_posix()`로 보정했다. 픽셀 제작 코드는 변경하지 않았다. 이후 제작 QA passed=true, 오류0. [제작 QA](Docs/Inbox/LetterObservatory/Regenerated/QA_REPORT.json)는 아래 Unity 검사와 구분한다.
- README/두 manifest/OPTICS/MOTION/120행 사건 CSV/POLISH_REVIEW/ADDED_DECISIONS/적용 지시 전체를 읽고 [재생성 원문](Docs/Inbox/LetterObservatory/Regenerated)에 보존했다. 사용자 입력 폴더·ZIP은 수정하거나 커밋하지 않았다.

## 현재 프로젝트와 사건 조사

AGENTS.md·Unity/패키지·Scenes/Scripts/Data를 재확인했다. 활성 런타임은 DeviceVisualView, SpriteFrameSequence/Player, DeviceThemeCatalog, 주사위 렌더 데이터, QA/HUD다. 스와이프 확정→자동 연쇄→장치 턴→결과 호출자, 실제 이동/충돌/코스트/수집 판정은 없다. Grid도 0개다. 현재 QA의 셀 간격 1 world unit를 기준으로 s=1, PPU80을 유지했다. Grid/Collider를 아트 크기로 생성하거나 수정하지 않았다. 레거시 러너·맵 제작기를 복원하지 않았다.

## 실제 정본 / 임포트

테마 `letter_observatory`를 등록하고 버전 폴더 없이 정본에 추가했다.

- 공통4종: `Assets/ANIMOL/Sprites/Obstacles/common/letter_observatory/M##_EnglishName/Frames, Animations`
- 전용5종: `Assets/ANIMOL/Sprites/Obstacles/letter_observatory/{M03_CrescentJump,M10_LightSensor,M14_Magnet,M16_PairedPortal,M21_GlassWall}/Frames, Animations`
- 바닥/벽: `Sprites/Tiles/{Floor,Wall}/letter_observatory`; 배경/기둥/망원경/편지/등: `Sprites/Background/letter_observatory/{Far,Mid,Near}`
- 프리팹: `Assets/ANIMOL/Prefabs/Themes/letter_observatory`; Sequence: `Data/AnimationSequences/letter_observatory`
- 테마/광학 표시 자료: `Data/Themes/letter_observatory.asset`, `letter_observatory_optics.asset`; raw 규칙은 Data의 letter_observatory_optics_rules.json/motion_bindings.json.

**정적 PNG118 + 프레임609 = PNG727, Sequence/AnimationClip 각각120, 프리팹247**(전체 장치 상태9 + static 역할118 + clip 역할120). 전체 활성 정본은 PNG2743·클립405다. 개별 PNG 방식 하나만 사용하고 시트는 임포트하지 않았다. [원본→정본 전체 경로 및 SHA](Docs/Validation/LetterObservatory/Applied/source_path_map.json).

파일명은 `spr_obstacle_letter_observatory_m16_pair_portal_a_enter_e_f00.png` / `anim_obstacle_letter_observatory_m21_glass_wall_break_square_e.anim` 같은 ASCII 소문자 snake_case, f00 시작이다. 도약 궤적과 장치 구성품은 실제 원본 상태 이름을 유지한다.

Point, Sprite Single/FullRect, 입력 alpha/transparency, 무압축, mipmap 없음, Clamp, NPOT 없음, max8192, Android/iPhone/Standalone/WebGL 축소 override 없음. PPU80/s=80이며 미등록 PPU32 정책이 덮어쓰지 않도록 기존 임포터에 테마 매니페스트 조회를 추가하고 재임포트했다. 96/128/160×128/64px 및 1024×1792 실제 크기를 유지했다.

JSON 좌상단 피벗을 (x/width,1−y/height)로 변환했다. 도약 e/w의 256×96 피벗은 (8,48)/(248,48), s/n의 96×256 Unity 픽셀 피벗은 (48,248)/(48,8)이며 중앙으로 덮어쓰지 않았다. 240px=3cell은 렌더 자료로 검증했다. 실제 도약 이동을 구현한 것은 아니다. 망원경 발점과 별등 등 각 metadata 피벗도 검사했다.

PNG는 재생성 완료본과 모두 hash 일치, 불투명 색13개(Sweetie16 subset), alpha0/255·투명 RGB0, 의도치 않은 캔버스 경계 잘림0이다. 바닥 opaque80×80의 반복 경계가 일치한다. 기존 고해상도 V04/프레임/클립/프리팹/씬/폰트/설정·meta는 그대로다.

## 기존 프리팹 / 실제 표시 연결

공통 07/09/18/19는 기존 장치8 프리팹의 실제 Variant다. 전용5종은 동일 DeviceVisualView/SpriteFrameSequence를 사용하고 각 PNG·Sequence·clip·CSV 사건·terminal Sprite를 연결했다. Animator를 추가하지 않았다. 런타임 렌더러를 갱신하는 것은 Sequence 플레이어 하나이며 clip은 같은 프레임을 참조하는 authoring 자산이다. duration/start ms 및 총 시간을 보존한다. clip frameRate1000은 정확한 키 시각 표현용이고 실제 재생은 각 프레임의 서로 다른 ms를 사용한다. AnimationEvent는0개다.

기존 Notify(actionId,event,state)·중복 방지256사건·ResetVisual·RebindArt/DeviceThemeCatalog를 재사용했다. 입력 차단/시간 조작/코스트/턴/수집/좌표 커밋이 없다. 정적 완료 상태를 유지하며 idle 시간이 장치를 갱신하지 않는다. Frame PNG에는 셀 간 이동이 없고 실제 from/to 호출자가 없어 이동 보간은 미연결이다.

새 `OpticsPresentationRules`는 raw 규칙과 8개의 진행 방향→반사 결과/corner Sprite를 참조한다. slash E→N/N→E/W→S/S→W, backslash E→S/S→E/W→N/N→W를 검사했다. ray tracer를 구현하거나 현재 보드의 경로를 계산하지 않는다.

새 `BeamPathView.Present(cells)`는 호출자가 확정한 위치/부품 목록만 표시한다. 풀은 씬에 직렬화해 재진입 때 중복 renderer를 만들지 않는다. 새 짧은 경로를 넣으면 남은 구간을 즉시 숨기며 Clear/OnDisable에서 잔상을 제거한다. Collider/상태 구독/게임 RNG/턴 갱신은 없다. 같은 경로 재표시는 기존 renderer를 재사용한다.

유리10 < 광선15 < 불투명 몸체20의 실제 정렬을 적용했다. 최초 Play 검수에서 유리가20이었던 authoring override 오류를 발견하고10으로 고친 뒤 프리팹/씬을 재생성하고 검수를 다시 통과시켰다. mirror_slash/backslash와 회전 클립은 합성 완료본 하나만 사용하며 base/bar/pivot을 동시에 겹치지 않는다. 표시는 metadata 축을 사용하고 Transform 전체를 회전하지 않는다.

광원 source_off는 QA 갤러리에서만 원본 상태로 전시하며 새로운 광원 토글 기능을 만들지 않았다. sensor/연결문/금속·목재/포털 A/B/유리 broken 상태는 CSV 표시 API에 준비됐지만 실제 논리 소유자가 없어 게임 통합은 미완료다. cracked는 파괴의 중간 표시이며 새 내구도를 추가하지 않았다. 유리 broken/마지막 투명 프레임은 정상 Sprite 참조를 유지한다.

## 씬 / 실제 실행

`Assets/ANIMOL/Scenes/ANIMOL_LetterObservatory_Art_QA.unity`를 생성했다. DeviceVisualView255개: 9장치 전체 상태 + 장식3 + 광학 fixture5 + 모든 clip/static 역할238. 광선 경로는6개의 표시 renderer로 구성된 **명시적인 아트 fixture**이고 실제 추적/새 레벨이 아니다. 12페이지에는 전시, 상태 갤러리, 4방향 전체 도약 궤적이 있다. 비대칭 궤적이 겹치거나 화면 밖으로 잘리지 않도록 시작점 배치를 분리했다.

기존 rabbit_front/soap_bubble_pink/exit_visual_root/hud_visual을 직접 참조했다. pause/cost/bubble는 기존 UI를 유지하며 미연결 COST -- / --/--다. 봉투/별등은 보드 밖에 둔다. 카메라 ortho12·1080×1920에서80px/cell, 즉 새 아트 정수1x다. 기존3 QA 씬/빌드 씬 설정을 유지하고 에디터의 현재/Play 시작 씬을 새 QA로 설정했다. 실제 퍼즐 플레이 씬은 없어 그 연결은 수행하지 못했다.

## 실제 검수 / 실패 복구

| 검사 | 실제 결과 |
|---|---|
| 제작 재생성 | metadata 전체 일치, 제작 QA 오류0; 실제 Unity 검사와 별도 |
| EditMode | 전체18/18 통과(기존13+천문대5). 임포트/시간,8반사 자료,4방향 비대칭 피벗·3셀,광선 풀/잔상,Variant/포털 표시 ID 중복 거부. [로그](Docs/Validation/LetterObservatory/Applied/all_editmode.json) |
| PlayMode | 전체6/6 통과(기존4+천문대2). 실제 새 씬1080×1920,255 root불변·pause/reset/restart,모든 state/frame texture 연결,정렬10/15/20,씬 재진입·풀6개 유지·clear,기존HUD. [로그](Docs/Validation/LetterObservatory/Applied/all_playmode.json) |
| Sprite/clip |727 Sprite·120 정확한 timing/clip·247 단일 writer 프리팹 검사 통과. [검사](Docs/Validation/LetterObservatory/Applied/validation.json) |
| Atlas | 대표64개의 비대칭 궤적/광학/유리 중간 프레임을 Android 대상으로 실제 pack. Point·RGBA32무압축·mip/tight/rotation 없음·padding4,1page,새 불투명 RGB0. snapshot을 Assets 밖으로 보존하고 임시 Atlas를 제거/원본 reimport해 런타임 바인딩 복구. [결과](Docs/Validation/LetterObservatory/Applied/atlas_qa.json) |
| 보존/참조 | 기존 Asset/설정 바이트 보존, 기존 meta 변경0,Assets 중복 GUID0,직렬화 GUID누락0,새 PNG 원본hash 일치. [감사](Docs/Validation/LetterObservatory/Applied/audit.json) |
| 씬/컴파일 | 누락 script0,누락/지원 불가 material0,compilationFailed=false. [씬](Docs/Validation/LetterObservatory/Applied/scene_validation.json),[Console](Docs/Validation/LetterObservatory/Applied/console_final.json) |

화면: 36개 시작/중간/종료 portrait pose, 유리 파괴12클립의 전체128px 중간 frame4x, 별도 실제 Play Game View. [실제 Play](Screenshots/LetterObservatory/qa_playmode.png),[광학/유리 fixture](Screenshots/LetterObservatory/page_0_pose_0.png),[4방향 궤적](Screenshots/LetterObservatory/page_11_pose_0.png). 경계·의도된 투명 홈·픽셀·배경 뒤/보드 평면을 확인했다. 120클립의 전 프레임 수동 육안 승인까지 했다고 주장하지 않는다.

초기 새 씬 생성은 원본 lamp_idle 역할 이름을 star_lamp_idle로 잘못 조회해 실패했다. 이름을 실제 매니페스트에 맞춰 수정하고 이번 작업이 만든 미저장 partial 씬만 폐기한 뒤 새 QA를 완성했다. 유리 layer 실패는 위 실제 Play 검사로 발견/복구했다. 처음 실패한 시도는 최종 통과 수에 포함하지 않는다.

## 장치별 실제 평가

| 장치 | 정적/클립 | 실제 논리 구현 / 사건 통합 | 표현 개선 / 난이도 / 우선순위 |
|---|---:|---|---|
| 07 무빙워크 | 4 / 4 | 표시만 / 사건 호출자 없음 | 기존 Variant 재사용 / 낮음~중간 / P1 |
| 09 단방향 문 | 8 / 12 | 표시만 / 사건 호출자 없음 | 기존 Variant 재사용 / 낮음~중간 / P1 |
| 18 튕김 모서리 | 4 / 4 | 표시만 / 사건 호출자 없음 | 기존 Variant 재사용 / 낮음~중간 / P1 |
| 19 제동판 | 4 / 2 | 표시만 / 사건 호출자 없음 | 기존 Variant 재사용 / 낮음~중간 / P1 |
| 03 초승달 도약판 | 16 / 16 | 표시만 / 사건 호출자 없음 | 새 역할·상태 연결 / 중간~높음 / P0 |
| 10 광원·거울·감지기 | 29 / 22 | 표시만 / 사건 호출자 없음 | 새 역할·상태 연결 / 중간~높음 / P0 |
| 14 자석 | 16 / 22 | 표시만 / 사건 호출자 없음 | 새 역할·상태 연결 / 중간~높음 / P0 |
| 16 짝 포털 | 15 / 20 | 표시만 / 사건 호출자 없음 | 새 역할·상태 연결 / 중간~높음 / P0 |
| 21 유리벽 | 9 / 15 | 표시만 / 사건 호출자 없음 | 새 역할·상태 연결 / 중간~높음 / P0 |

공통4종은 방향/terminal을 재사용했고 도약은 출발/착지/거부·전체 궤적,광학군은 역할 분리/완성 거울/8반사/레이어,자석은4방향 및금속·목재/스위치,포털은A/B·4방향enter/exit/blocked,유리는3외형·방향 파괴·투명terminal을 준비했다. 실제 확정된 사건을 전달할 코드가 없어 개선은 현재 표현/자료 범위다.

## 미실행과 남은 작업

유효 행동 cost1/무효0·turn0/연쇄 추가0, 실제3방울+출구,도약 이동·막힘·중간 칸,실제 ray blocker·sensor/문,자석 처리순서·실제 견인,포털 걷기/미끄러짐·불발·재진입 방지,유리 몸체/바람 차단·폭탄/석상/파괴 칸 플레이어 차단은 해당 게임 로직이 없어 미연결·미실행이다. 8반사 자료와광선 잔상 표시 검수를 게임 광선 추적 검수로 대신하지 않았다. Retry/테마 선택 실게임도 없고 QA reset/표시 skin 교체/씬 재진입만 검수했다. Android 빌드·실기기는 미실행이며 세로 Game View/기존360×640 SafeArea 회귀만 실행했다. 오디오/햅틱은 활성 대응 서비스가 없어 미연결이다. CommercialPolishPolicyTests/과거로비는 비활성 레거시라 미실행이다.

1. 즉시 효과 큰 수정: 실제 탑뷰 행동 ID/이동/코스트/연쇄·장치 판정/결과 호출자를 구현·확인해 CSV120개 표시 사건과연결한다. 도약/거울 조작 등의 미정 분기를 코드 정책으로 확정한다.
2. 적은 비용 개선: 실제 trace 결과를 BeamPathView에 공급하고 sensor/문/포털 preview·blocked 표시를권위 상태로 갱신한다. from/to 보간·테마 선택/level data를 연결한다.
3. 나중 디테일: 실제빌드 Atlas의 포함/바인딩·모바일 검수,기존 사운드/햅틱의 단일 사건 통합,미술 육안 승인. 임의추가cost/압착/새버튼/스태미나를 넣지 않는다.

## 변경 범위

기존 코드 변경은 ArtAssetBuilder의 새 테마 임포트 조회 한 파일이다. 새 코드 LetterObservatoryArtBuilder/BeamPathView/OpticsPresentationRules 및 Edit/Play 테스트, 새 theme Sprite/clip/Sequence/Variant/catalog/rawdata/QA, regenerated docs·Applied validation·screenshots·Tools가 산출물이다. 정확한 파일 목록은 [changed_files.txt](Docs/Validation/LetterObservatory/Applied/changed_files.txt). 입력 폴더/ZIP·기존 설정/폰트/레거시는 커밋에서 제외한다.
