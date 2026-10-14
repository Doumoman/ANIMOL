# 황혼의 바다 석양 해변 V02 Unity 적용 결과

2026-10-14 / Unity 6000.3.8f1 / Android 대상 Editor / Built-in Render Pipeline / Linear.

## 실제 구조와 적용 범위

작업 시작 씬은 `ANIMOL_ClockDome_Puzzle_QA.unity`였고 Play 종료·저장 상태였다. ANIMOL의 활성 플레이 구현은 가변 크기 정사각형 탑뷰 스와이프 퍼즐이다. `PuzzleMap.terrain`의 위치·재질·blocked가 지형 점유이며 장치·방울·시작점·출구는 별도 데이터다. 씬에 미리 만든 Tilemap 대신 실행 중 `PuzzleMapView`가 지형을 생성한다. `PuzzleFollowCamera`는 실제 플레이어 위치를 추적하고 맵 경계에서 멈춘다. 이 추적·줌·스와이프·자동 연쇄·수집·출구·승리 구현은 변경하지 않았다. 과거 러너/플랫포머를 복원하지 않았다.

기존 `ANIMOL_TwilightSea_Art_QA.unity`에는 평면 바닥 SpriteRenderer 165개, 고정 배경, 프리젠터 307개와 마스터 8개(ID 07/09/18/19/05/11/12/15), 나룻배 `FerryVisualView` 하나와 기존 `ResolvedCellPreview` 하나가 있었다. 이탈 가시 발판·조종 나룻배·구르는 통·3턴 폭탄·공통 장치의 프리팹/상태/이벤트/동기화/위치/범위 프리뷰를 보존했다. 이 테마 장치는 현재 **아트 프리뷰**이며 권위 있는 퍼즐 판정에는 아직 연결되지 않았다. 실제 플레이 장치 종류는 Conveyor/Wind/Ice/Brake/Portal이다. 코스트 계산도 아직 없으며 기존 `COST --`를 유지한다. 미구현 장치 판정·물리·수영·낙사·실패 조건·UI를 새로 만들지 않았다.

`ANIMOL_TWILIGHT_COAST_V02`의 README_KO.txt, manifest.json, qa_report.json을 읽고 원본을 `Docs/Inbox/TwilightCoast/ANIMOL_TWILIGHT_COAST_V02`에 보관했다. source/inspection/examples/camera와 GIF·시트는 검수 자료로만 보관하며 활성 충돌 타일로 등록하지 않았다. 원래 레벨을 examples/walkableExample로 덮어쓰지 않았다.

연결한 씬:

- [기존 황혼의 바다 아트 QA](../Assets/ANIMOL/Scenes/ANIMOL_TwilightSea_Art_QA.unity): 기본 패널의 평면 바닥과 고정 배경을 새 환경으로 교체했다. 장치·나룻배·판정 범위 프리뷰·캐릭터·방울·출구·HUD·기존 QA 도구를 유지했다. 다른 패널의 기존 상태/원본 아트 검수 참조도 유지한다.
- [새 퍼즐 QA](../Assets/ANIMOL/Scenes/ANIMOL_TwilightCoast_Puzzle_QA.unity): 기존 `ANIMOL_Puzzle` 씬과 Small 맵을 복사하여 환경 테마만 연결했다. 시작점·이동 칸·장치·수집물·출구·목표·카메라 설정은 원본과 같다.
- [새 접합 QA](../Assets/ANIMOL/Scenes/ANIMOL_TwilightCoast_Environment_QA.unity): 제공 coast_join 시트와 같은 검수 지형이다. 플레이 레벨이 아니다.

기본 Build Settings는 변경하지 않았다.

## 자산·렌더링·애니메이션

런타임 PNG 124개를 원본 바이트 그대로 복사했다. 경로는 `Sprites/Tiles/Floor/twilight_sea/TwilightCoast`, `Sprites/Props/twilight_sea/TwilightCoast`, `Sprites/Background/twilight_sea/TwilightCoast`다. sand/wet_sand_body blob47 94개, props 8개, 개별 애니메이션 프레임 15개, 반복 배경 3개와 노을 4개다. 활성 경로에 버전 접미사를 붙이지 않았다.

Sprite Single / PPU80 / Full Rect / Point / Compression None / Mipmap Off / NPOT None을 적용했다. 플랫폼 압축 override와 알파 색 번짐을 껐고 전체 캔버스·투명 여백을 유지한다. 타일과 반복 바다는 중앙 피벗, 소품은 manifest 피벗, 야자수 프레임은 아래 중앙, 노을 건축 이미지와 해/반사광은 왼쪽 위 피벗이다. Atlas는 사용하지 않는다. 원본 221개 파일의 SHA-256, 런타임 PNG 124개의 원본 일치·13색 승인 팔레트·이진 알파·투명 RGB를 [감사 기록](Validation/TwilightCoast/audit.json)으로 검증했다.

노출 o=N1/E2/S4/W8, 대각선 i=NW1/NE2/SE4/SW8을 실제 지형 점유에서 계산한다. 레벨 밖은 빈 칸이며 장치 점유나 재질 구분 때문에 구조 바닥을 끊지 않는다. 256개 이웃 조합이 47개 올바른 이름의 사전 조립 타일을 선택함을 검사했다. 회전·반전·inner4 alpha-over 합성은 사용하지 않는다.

Wet Sand Body 전체(-20) → Sand 전체(0) → 기존 장치/캐릭터/UI 순서다. 단면 local Y=-0.1unit, 셀 간격은 1unit이다. 모든 Tilemap/소품/배경은 Collider 없이 시각 전용이다. 선택 격자는 꺼 두었다. 지형 변경은 변경 칸과 주변 8칸, 리사이즈는 전체 지형 그림을 갱신한다. 범위 밖 레코드를 보존하는 기존 리사이즈 동작을 유지한다.

Far/Mid/Near는 960px X/Y 반복, 시차 0.12/0.30/0.50, 정수 픽셀 반올림과 floor-mod를 사용한다. 기존 정수 확대/Point 합성기에 Near sprite 교체 기능만 추가했다. 정적 Near와 animated Near가 동시에 표시되지 않는다. 기존 다른 테마의 기본 시차와 표시를 유지했다. 지형·소품·노을에는 패럴랙스를 적용하지 않는다.

노을은 맵 위쪽의 고정 월드 장식이다. 960×320 core를 한 번 표시하고 좌측에 left 연장, 우측에 right 연장을 80px 간격으로 붙인다. 좌우 이미지를 바꾸거나 반전하지 않는다. 대안인 without_sun + setting_sun(360,80) + reflection(360,218)도 별도 조립 검수했으며 동시에 표시하지 않는다. 카메라를 수평선에 고정하지 않는다.

야자수·바위/조개·로프는 이동 영역 밖의 **별도 시각 모래 점유**에 놓았다. 실제 맵에 장식 이동 칸을 추가하지 않는다. 위쪽은 좌우 모래 조각으로 나누어 중앙 해/반사광을 비우고, 아래쪽은 시각 모래 띠에 배치했다. 뿌리/받침/기둥 발이 모래에 닿으며 플레이 칸과 수집물·출구를 가리지 않는다. 좁은 카메라 시야에서는 바깥 장식 일부가 화면 밖으로 나갈 수 있다. 크기를 축소하거나 캔버스를 trim하지 않았다.

`palm_idle`은 5프레임 [800,400,800,400,800]ms, 3.2초 반복이다. 아래 48행을 원본 그대로 고정하고 sprite 하나만 교체한다. 현재 장식 접촉 이벤트가 없어 `palm_touch`는 연결하지 않았다. touch의 6프레임 [40,60,70,80,80,100]ms와 최종 원래 자세는 **자산 검사**로만 확인했다. 게임에서 touch→idle 전환을 실행했다고 보고하지 않는다.

`water_ripples`는 4프레임×600ms=2.4초 반복으로 Near 전체의 모든 사본을 같은 프레임으로 교체한다. 960×960 캔버스와 반복 위상을 유지하며 추가 transform 흔들림이나 알파 페이드는 없다. 야자수는 화면 밖에서 재생 시간을 멈출 수 있고 motionEnabled=false일 때 정지한다. root motion·새 효과음·햅틱·파티클·셰이크·줌·조명/후처리 효과를 추가하지 않았다.

## 실제 실행과 검사 결과

실제 Game View 캡처는 총 53장이다(스와이프 검수 19장, 환경 검수 34장).

적용 전 전체 [EditMode 80/80](Validation/TwilightCoast/baseline_editmode.json), [PlayMode 21/21](Validation/TwilightCoast/baseline_playmode.json)을 실행했다. 최종 전체 [EditMode 86/86](Validation/TwilightCoast/editmode.json), [PlayMode 24/24](Validation/TwilightCoast/playmode.json)은 실패/스킵 0이다. 기존 활성 ArtAssetTests/ArtQaPlayTests, 다른 테마, 퍼즐, 나룻배/범위 프리뷰 테스트도 포함한다. 보관된 CommercialPolishPolicyTests는 현재 활성 테스트가 아니므로 실행하지 않았다.

실제 Play mode의 최종 Game View를 ScreenCapture로 촬영했다. 오프스크린 카메라 RenderTexture만 저장한 캡처로 대체하지 않았다.

| 항목 | 확인 결과와 근거 |
|---|---|
| 좁은/넓은/가로 긴/세로 긴/불규칙 지형 | Small, Horizontal 40×7, Vertical 7×40, CorridorIslands, UHole, MaterialBoundary, EdgePortal, StarGarden. [스와이프 검수 19장·6,945 이동 프레임·27개 검사](Validation/TwilightCoast/Portrait/gameview.json) |
| 스와이프·연쇄·수집·승리 | 기존 BeginGesture/EndGesture로 한 칸 이동, 무빙워크/바람 연쇄, 실제 방울 수집과 출구 승리. 카메라 경계·11행 시야·고정 지형·정수 확대·배경 위상을 모든 이동 프레임에서 확인 |
| 맵 끝 포털 | 출구 화면 cut, 방향 보존·미끄럼·수집 유지. 먼 중간 구간으로 카메라가 이동하지 않음. [출구 cut](Validation/TwilightCoast/Portrait/portal_exit_cut.png), [미끄럼 끝](Validation/TwilightCoast/Portrait/portal_slide_end.png) |
| 통로·끝점·섬·오목 구멍·모서리 | [접합 1x](Validation/TwilightCoast/coast_join_1x.png) / [정수 4x](Validation/TwilightCoast/coast_join_4x.png): 제공 시트와 RGB 픽셀 차이 0. 연결선 끊김·단면 겹침·알파 가장자리·타일 틈 없음 |
| 해 1개·좌우 이음새 | [단일 core](Validation/TwilightCoast/horizon_wide.png), [분리형](Validation/TwilightCoast/horizon_separated.png): 모두 제공 1280×320 노을 시트와 픽셀 차이 0. 비균등 확대·중복 해 없음 |
| 바다 반복과 음수 좌표 | [A](Validation/TwilightCoast/background_A.png), [B](Validation/TwilightCoast/background_B.png), [C](Validation/TwilightCoast/background_C.png): 제공 합성과 차이 0. 2080×1600과 (-7040,-5120)에서도 여러 X/Y 주기에 빈틈 없음 |
| animated Near 반복 | 2080×1600의 4개 잔물결 프레임을 Far+Mid+각 Near 프레임 원본 합성과 비교하여 모두 차이 0. 정적 Near 중복 없음 |
| 야자수 애니메이션 | idle 5개와 미연결 touch 6개의 실제 Game View 프레임이 원본과 차이 0. 아래 48행·캔버스·복귀 자세·루프/개별 타이밍·root 위치 불변 검사 |
| 노을의 월드 고정 | [위쪽 카메라](Validation/TwilightCoast/vertical_horizon_upper.png) / [아래쪽 카메라](Validation/TwilightCoast/vertical_ocean_lower.png): 아래로 이동하면 노을이 사라지고 바다가 계속 채움. 이 두 장은 위치/시야 검사이며 별도 스와이프 성공 증거로 사용하지 않음 |
| 모바일 세로 Game View | 720×1280, 1080×1920, 1080×2400에서 정수 1x/2x/2x, 화면 덮기·기존 HUD·입력 비차단 확인. [환경 검수 기록](Validation/TwilightCoast/inspection_gameview.json) |
| 장식과 장치 가림 | [플레이 판과 해](Validation/TwilightCoast/Portrait/small_centered.png), [기존 8종 장치](Validation/TwilightCoast/sea_devices.png), [캐릭터/방울/출구](Validation/TwilightCoast/sea_character_exit.png), [야자수 접촉](Validation/TwilightCoast/sea_palm_contact.png), [울타리 받침](Validation/TwilightCoast/sea_fence_contact.png). 모래 장식의 점유·Collider·목표/출구 가림 없음 |
| 원래 장치 참조 | [기존 prefab instance 문서 312개 바이트 동일](Validation/TwilightCoast/device_reference_audit.json). QA 도구의 기존 직렬화 필드·307 views·8 masters·나룻배·범위 프리뷰 유지 |

최종 원본 비교는 23개이며 모두 차이 0이다. 밝은 모래 위의 기존 방울과 황색 이동 장치 글리프는 대비가 낮다. 장식 idle이 가리지는 않지만 시각적 구분 개선은 남아 있다. 기존 방울/장치/UI를 임의로 교체하거나 새 강조 효과를 추가하지 않았다.

검수 중 발견한 두 문제는 수정 후 다시 확인했다. 위쪽 장식 띠가 해를 덮어 좌우 모래 조각으로 재배치했고, 접합 검수 맵의 한 칸이 제공 시트와 달라 검수 맵만 수정했다. 원본 PNG와 플레이 레벨은 바꾸지 않았다. 최초 CLI Apply는 시간 초과로 실행되지 않았고, 생성 자산이 없는 상태에서 새 6개 EditMode 검사가 실패했다. 연결된 Editor API로 적용한 뒤 다시 실행한 최종 전체 검사는 모두 통과했다. 도메인 reload 중 일시적 MCP transport 오류와 기존 중복 Memory Theater 메뉴 메시지는 아트/퍼즐 실패와 구분했다.

## 수정 파일과 재검증

새 런타임: `TwilightCoastArt`, `TwilightCoastRenderer`, `TwilightCoastHorizon`, `TwilightCoastPalm`, `TwilightCoastInspection`. 새 Editor: `TwilightCoastBuilder`, `TwilightCoastGameViewValidation`. 새 테스트: `TwilightCoastTests`, `TwilightCoastPlayTests`.

기존 수정 코드는 시각 연결용 `PuzzleTheme`, `PuzzleMapView`, `PuzzleBackdrop`, Near 애니메이션을 위한 `SkyIslandBackdrop` 4개다. Data/Art/TwilightCoast에 타일 94개·환경 카탈로그·시퀀스 3개·검수 맵/테마가 있고 Prefabs/Environment/TwilightCoast에는 Horizon·palm·rocks·rope_fence 4개가 있다. 기존 황혼의 바다 아트 QA 씬과 신규 씬 2개를 연결했다.

작업 전 Assets 13,429개 파일을 해시 기록했다. 변경된 기존 자산은 위 코드 4개와 황혼의 바다 아트 QA 씬 하나뿐이다. 모든 기존 레벨·다른 테마·이동/입력/카메라 추적/수집/출구 코드·HUD·장치 프리팹/시퀀스는 그대로다. 사용자 미커밋 변경을 보존했고 이번 작업 경로만 커밋했다.

```powershell
# Play 종료 / 저장된 씬에서 적용. 긴 최초 임포트는 CLI 응답 제한에 유의한다.
unity command eval 'ANIMOL.Presentation.Editor.TwilightCoastBuilder.Apply(); return 124;' 60000
# 퍼즐 QA, 실제 Play mode / Game View 1080×1920
unity command eval 'ANIMOL.Presentation.Editor.TwilightCoastGameViewValidation.Start(); return 1;'
# Play mode에서 접합·노을·애니메이션·카메라·장식 검수
unity command run_script --file Tools/TwilightCoast/GameViewInspection.cs --entry TwilightCoastGameViewInspection.Start
python Tools/TwilightCoast/audit.py
```

## 남은 한계

- 실제 Android/iOS 기기 빌드·터치·성능·다른 GPU/URP/HDRP는 실행하지 않았다. 위 결과는 Android 대상 Editor Game View다.
- Atlas를 사용하지 않아 Atlas 경계 검증은 해당하지 않는다. 도입 시 회전/Tight Packing 금지, padding/extrusion과 실기기 표시를 다시 검사해야 한다.
- 코스트와 황혼의 바다 8종 장치의 권위 있는 게임 판정은 기존 미구현/미연결 상태다. 아트 프리뷰 테스트를 그 판정의 검증으로 보고하지 않는다. 새로운 물 판정도 만들지 않았다.
- 현재 장식 접촉 이벤트가 없어 touch를 연결하지 않았다. 향후 기존 접촉 이벤트가 생기면 단일 sprite writer의 touch→idle 전환을 별도 연결/검증해야 한다.
- 밝은 모래 위 기존 방울/황색 글리프의 낮은 대비와 1080×2400의 기존 HUD 우측 잘림을 남겼다. UI·장치·수집물 변경 금지 범위에 따라 별도로 보고한다.
- 고정 월드 노을과 바깥 장식은 카메라 위치/좁은 시야에 따라 잘린다. 항상 수평선을 보이는 카메라 고정이나 원본 축소를 추가하지 않았다.
