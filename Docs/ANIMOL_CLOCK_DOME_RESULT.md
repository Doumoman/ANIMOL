# CLOCK DOME V01 Unity 적용 결과

2026-10-14 / Unity 6000.3.8f1 / Android 대상 Editor / Built-in Render Pipeline / Linear.

## 실제 구조와 적용 범위

시작 씬은 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity`였다. 가변 크기 정사각형 탑뷰 스와이프 퍼즐이며, `PuzzleMap.terrain`의 위치·재질·blocked가 지형 판정이다. 장치·방울·시작점·출구는 별도 데이터다. 플레이 씬은 `PuzzleController`가 실행 중 지형 렌더러를 만들고, `PuzzleFollowCamera`가 실제 플레이어 이동을 추적한다. 카메라 추적·줌·스와이프·연쇄·수집·출구 코드는 수정하지 않았다.

편지 천문대의 기존 `ANIMOL_LetterObservatory_Art_QA.unity`에는 Tilemap 없이 개별 바닥 SpriteRenderer 165개, 고정 배경, 장치 프리젠터 255개와 마스터 9개가 있었다. 9개의 ID는 07/09/18/19/03/10/14/16/21이다. 이 장치는 기존 상태·이벤트의 **아트 프리뷰**이며 실제 퍼즐의 권위 있는 장치 판정 호출에 아직 연결되지 않았다. 실제 퍼즐 장치 종류는 Conveyor/Wind/Ice/Brake/Portal이다. 코스트 판정도 아직 없으며 HUD는 기존 `COST --`를 유지한다. 새 기믹이나 UI를 추가하지 않았다.

`README_KO.txt`, `manifest.json`, `qa_report.json`을 확인하고 원본 팩을 `Docs/Inbox/ClockDome/ANIMOL_CLOCK_DOME_V01`에 보관했다. 제작·inspection·camera·examples 자료는 활성 Assets에 등록하지 않았다. ZIP은 기존 `MCP_INBOX`에 그대로 있다.

적용 씬:

- [기존 편지 천문대 아트 QA](../Assets/ANIMOL/Scenes/QA/ANIMOL_LetterObservatory_Art_QA.unity): 평면 바닥·배경을 교체했다. 기존 장치 프리팹·상태·이벤트·위치·광선·캐릭터·HUD·QA 도구를 보존했다. 기본 패널의 이전 망원경 그림만 실행 중 숨기며 프리젠터 참조는 유지한다.
- [퍼즐 QA](../Assets/ANIMOL/Scenes/QA/ANIMOL_ClockDome_Puzzle_QA.unity): 기존 퍼즐 씬과 Small 맵을 복사하고 환경 테마만 연결했다. 원래 레벨과 다른 테마는 변경하지 않았다.
- [접합 QA](../Assets/ANIMOL/Scenes/QA/ANIMOL_ClockDome_Environment_QA.unity): 제공 접합 시트와 동일한 검수 지형이다. 플레이 레벨을 대체하지 않는다.

Build Settings의 기본 씬은 변경하지 않았다.

## 임포트와 연결 내용

활성 자산은 버전 접미사 없이 `Sprites/Tiles/Floor/letter_observatory/ClockDome`, `Sprites/Props/letter_observatory/ClockDome`, `Sprites/Background/letter_observatory/ClockDome`에 있다. blob47 바닥/단면 94개, parts 11개, pendulum 개별 프레임 8개, 배경 3개, 총 PNG 116개를 바이트 그대로 복사했다.

모든 PNG는 Sprite Single / PPU80 / Full Rect / Point / Compression None / Mipmap Off / NPOT None이다. 타일 피벗은 중앙, parts는 manifest 피벗, pendulum 프레임은 위 중앙이다. 캔버스와 투명 여백을 보존하고 플랫폼 압축 override와 알파 색 번짐을 껐다. 이 환경은 Sprite Atlas를 사용하지 않는다. 원본 212개 파일과 런타임 PNG의 SHA-256, 승인 색과 알파 0/255, 투명 픽셀 RGB를 [감사 기록](Validation/ClockDome/audit.json)에 남겼다.

지형 점유에서 o=N1/E2/S4/W8, i=NW1/NE2/SE4/SW8을 구한다. 레벨 밖은 빈 칸이며 장치나 재질 경계로 구조 바닥을 끊지 않는다. 256개 이웃 조합이 47개 완성 타일을 선택함을 검사했다. 회전·반전·inner4 알파 합성은 사용하지 않는다. 바닥과 단면은 같은 점유를 사용하며 셀 간격은 1unit, 단면 local Y는 -0.15unit이다. 단면 전체(-20)를 바닥 전체(0)보다 먼저 렌더링하고 Collider를 붙이지 않는다. 선택 격자는 꺼 두었다.

편집기 변경·리사이즈는 원래 MapView의 Rebuild/갱신 경로를 거쳐 새 Tilemap을 갱신한다. 실행 중 지형 변경은 변경 칸과 주변 8칸을 갱신한다. 리사이즈 시 범위 밖 지형·장치 레코드를 보존하는 기존 데이터 동작을 유지한다. 시각 전용 망원경 데크는 별도 점유·Tilemap이며 실제 맵 데이터에 추가하지 않는다.

돔은 고정 12unit 폭이다. 기본은 960×400 단일 캡이며, 분할 대안은 320px 세 장을 0/320/640px에 놓는다. 두 버전을 동시에 표시하지 않는다. 좌우 기둥은 X=0/800px, Y=400px부터 80px 간격, 발은 Y=400+80Npx에 놓는다. 0/4/12행 프리팹과 넓은 맵 중앙 돔을 만들었으며 cap을 반복하거나 늘이지 않는다. 돔·시계·사슬은 전체 바닥보다 뒤에 있다. 반복 벽 바탕은 Far/Mid/Near가 담당한다.

시계는 정적 SpriteRenderer로 코스트·시간·턴·목표·UI와 연결하지 않았다. 사슬 추는 천장 고정점에 붙이고 8프레임×400ms=3.2초를 sprite 교체로만 재생한다. 위 12행은 모든 원본 프레임에서 동일하다. root 회전·추가 sway·root motion이 없고 화면 밖에서는 재생 시간을 진행하지 않으며 `motionEnabled=false`일 때 정지 프레임을 사용한다. 망원경은 실제 지형 점유 바깥의 2×2 시각 데크에 받침을 닿게 놓는다.

Far/Mid/Near는 모두 판 뒤에서 960px X/Y 반복, 시차 0.15/0.30/0.45, 최종 정수 픽셀 반올림과 floor-mod를 사용한다. 기존 정수 확대/Point 합성기를 재사용했으며 새 설정은 인스턴스별이다. 별길·기억 극장 기본 시차는 그대로다. 돔과 지형은 패럴랙스를 적용하지 않으며 기존 카메라 transform·추적·viewport·UI 좌표를 유지한다. 후처리·조명 효과·소리·햅틱·파티클·셰이크·입력 잠금을 추가하지 않았다.

## 실행한 검증

적용 전 [EditMode 73/73](Validation/ClockDome/baseline_editmode.json), [PlayMode 18/18](Validation/ClockDome/baseline_playmode.json)을 실행했다. 적용 후 전체 [EditMode 80/80](Validation/ClockDome/editmode.json), [PlayMode 21/21](Validation/ClockDome/playmode.json)을 실행했고 실패·스킵은 0이다. 기존 ArtAssetTests/ArtQaPlayTests와 다른 테마·퍼즐 테스트도 포함된다. 보관된 CommercialPolishPolicyTests는 현재 활성 테스트가 아니므로 실행하지 않았다.

실제 Play mode에서 ScreenCapture로 최종 Game View를 저장했다. 카메라의 RenderTexture만 저장한 화면으로 대체하지 않았다.

| 항목 | 실제 확인 및 근거 |
|---|---|
| 좁은/넓은/가로 긴/세로 긴/불규칙 맵 | Small, Horizontal 40×7, Vertical 7×40, CorridorIslands, UHole, MaterialBoundary, EdgePortal, StarGarden. [19장·9,606 이동 프레임·27개 검사](Validation/ClockDome/Portrait/gameview.json) |
| 실제 입력과 기존 판정 | 스와이프 시작/종료 API, 한 칸 이동, 무빙워크·바람 자동 연쇄, 수집과 출구 승리. 모든 이동 프레임의 카메라 경계·고정 지형·정수 확대·배경 위상을 검사 |
| 맵 끝 포털 | 출구로 카메라 cut 후 방향/미끄럼/수집 유지. 카메라가 먼 중간 구간을 통과하지 않음. [출구 cut](Validation/ClockDome/Portrait/portal_exit_cut.png), [미끄럼 끝](Validation/ClockDome/Portrait/portal_slide_end.png) |
| 섬·통로·네 방향 끝점·안쪽 구멍·오목/볼록 접합 | 실제 접합 씬 [1x](Validation/ClockDome/floor_join_1x.png), [4x](Validation/ClockDome/floor_join_4x.png)가 제공 시트와 RGB 픽셀 차이 0. 알파 가장자리·단면 겹침·타일 틈·두꺼운 이음선 확인 |
| 기둥 반복·분할 캡 | [0행](Validation/ClockDome/room_0.png), [4행](Validation/ClockDome/room_4.png), [12행](Validation/ClockDome/room_12.png), [분할 4행](Validation/ClockDome/room_split_4.png) 모두 제공 조립 시트와 픽셀 차이 0. 비교용 촬영에서만 시계/사슬을 숨김 |
| 배경 연속성 | (0,0), (640,480), (-400,960)의 [A](Validation/ClockDome/background_A.png)/[B](Validation/ClockDome/background_B.png)/[C](Validation/ClockDome/background_C.png) 모두 제공 합성과 픽셀 차이 0. 2080×1600 화면과 (-7040,-5120)에서도 여러 반복 주기를 비교하여 차이 0·빈틈 없음 |
| 사슬 추 | 실제 Game View의 8개 개별 프레임이 원본과 픽셀 차이 0. 고정 캔버스/위 12행, 400ms 타이밍 데이터, 루프와 root 위치 불변/정지 설정 확인 |
| 모바일 세로 Game View | 720×1280, 1080×1920, 1080×2400. 확대 1x/2x/2x, 화면 전체 배경, 스케일·캐릭터·수집·출구 가독성 확인. [검수 26장·32개 검사](Validation/ClockDome/inspection_gameview.json) |
| 기존 장치와 정적 시계의 구분 | [기본 카메라](Validation/ClockDome/observatory_default_camera.png), [돔 시계](Validation/ClockDome/observatory_dome_clock.png), [장치](Validation/ClockDome/observatory_devices.png), [캐릭터·출구](Validation/ClockDome/observatory_character_exit.png), [데크 접촉](Validation/ClockDome/observatory_telescope_deck.png). 시계는 월드 뒤 장식이며 수집 목표/HUD 이벤트 없음 |
| 기존 장치 참조와 이벤트 | 기존 prefab instance 문서 259개가 바이트 동일. QA 도구의 기존 필드/255 views/9 masters 유지. 저장 과정에서 기존 기본값 필드 `previews: []`, `adaptPortraitToolbar: 0`만 명시됨. 9종 모두 기존 Notify/중복 사건 거절/고정 root/단일 sprite writer 검사. [참조 감사](Validation/ClockDome/device_reference_audit.json) |

총 18개 Game View 원본 비교가 픽셀 차이 0이다. 최종 Editor console에는 컴파일/런타임 오류가 없다. 최초 CLI Apply 요청은 5초 응답 제한을 넘었으나 작업 자체는 완료됐고 생성 자산·씬·테스트로 확인했다. 도메인 reload 중 MCP 연결의 일시적 transport 오류는 아트/퍼즐 실패와 구분했다.

## 변경 파일과 재검증

새 런타임 코드는 `ClockDomeArt`, `ClockDomeRenderer`, `ClockDomeRoom`, `ClockDomeAnimation`, `ClockDomeInspection`이다. 새 Editor 코드는 `ClockDomeBuilder`, `ClockDomeGameViewValidation`, 테스트는 `ClockDomeTests`, `ClockDomePlayTests`다. 기존 코드 변경은 시각 연결용 `PuzzleTheme`, `PuzzleMapView`, `PuzzleBackdrop`, 합성기 `SkyIslandBackdrop` 4개뿐이다. Data/Art/ClockDome에 타일 94개·환경 카탈로그·시퀀스·검수 맵/테마, Prefabs/Environment/ClockDome에 0/4/12행 돔과 망원경이 있다.

작업 전 Assets 12,955개 파일의 SHA-256을 기록했다. 변경된 기존 자산은 위 코드 4개와 편지 천문대 아트 QA 씬 하나뿐이다. 기존 모든 맵·이동·스와이프·수집·출구·카메라 추적 코드·HUD·장치 자산·다른 테마는 동일하다. 사용자 미커밋 변경은 보존했다.

재검증 명령:

```powershell
# 저장된 씬 / Play 종료 상태에서 자산 연결
unity command eval 'ANIMOL.Presentation.Editor.ClockDomeBuilder.Apply(); return 116;' 60000
# 퍼즐 QA Play, 1080×1920에서 실제 이동 검사
unity command eval 'ANIMOL.Presentation.Editor.ClockDomeGameViewValidation.Start(); return 1;'
# Play mode에서 실제 Game View 접합/배경/조립/세로 화면 검사
unity command run_script --file Tools/ClockDome/GameViewInspection.cs --entry ClockDomeGameViewInspection.Start
python Tools/ClockDome/audit.py
```

## 미확인·남은 한계

- 실제 Android/iOS 기기 빌드·터치·성능·다른 GPU/URP/HDRP는 실행하지 않았다. 위 세로 검증은 Android 대상 Unity Editor Game View다.
- Sprite Atlas를 사용하지 않으므로 Atlas packing 경계 검증은 해당하지 않는다. 향후 Atlas 도입 시 회전/Tight Packing 금지·padding/extrusion·실제 기기 표시를 다시 검사해야 한다.
- 현재 실제 코스트 계산과 편지 천문대 전용 장치의 권위 있는 퍼즐 판정은 미구현이다. 이를 이번 환경 작업의 검증 통과로 보고하지 않는다. 새 코스트·실패 조건도 만들지 않았다.
- 1080×2400에서 HUD 우측 수집 패널 일부가 잘리는 기존 문제가 남아 있다. 별길 작업의 적용 전 캡처에서도 같은 문제가 확인됐고 UI 변경 금지 범위에 따라 그대로 뒀다.
- 고정 960px 돔의 일부가 좁은 화면/카메라 위치에 따라 잘릴 수 있다. 원본을 축소하거나 cap을 반복하지 않으며 무한 반복 바탕이 화면을 계속 채운다. 원래 퍼즐 레벨은 새 테마로 일괄 덮어쓰지 않았다.
