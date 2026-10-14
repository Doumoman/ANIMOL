# 가변 크기 탑뷰 퍼즐 구현 및 Game View 검수

2026-10-14 · Unity 6000.3.8f1

새 플레이 씬은 `Assets/ANIMOL/Scenes/ANIMOL_Puzzle.unity`다. 기존 플랫포머 제작기나 격리한 프로젝트를 복원하지 않고 새 격자 데이터, 퍼즐 판정, 카메라, 편집기를 구현했다. 이전 아트 QA 씬은 그대로 보존했다.

## 사용 방법

1. `ANIMOL/Puzzle/Map Editor`를 연다. `Assets/ANIMOL/Data/Puzzle/Maps`의 스테이지를 선택하거나 New로 만든다.
2. Width/Height를 1–256 범위에서 지정하고 Apply dimensions를 누른다. 테마, 목표 방울 수, 카메라를 설정한다.
3. Terrain / Device / Decor / Markers를 선택한다. 왼쪽 드래그로 배치, 오른쪽 드래그로 해당 레이어만 지운다. Terrain의 Blocked cell로 통과 금지 칸을 만든다. 미배치 지형은 통과할 수 없다.
4. Markers에서 Start / Bubble / Exit를 배치한다. Portal에는 출구 좌표를 지정하고 Conveyor / Wind에는 N/E/S/W 방향을 지정한다.
5. Undo / Redo와 Unity의 일반 Undo를 사용할 수 있다. 하나의 드래그가 하나의 Undo 작업이다. Save는 에셋을 저장한다.
6. Play Preview는 현재 스테이지를 실제 Play 모드에서 실행한다. 종료하면 이전 씬 구성과 Play 시작 씬을 복원한다. 저장되지 않은 현재 씬은 먼저 저장해야 한다.

실행 중 한 손가락 스와이프로 한 칸 이동한다. 주 이동축으로 상하좌우를 정하고 손을 뗄 때 한 번만 처리한다. 짧은 드래그·UI에서 시작한 제스처·취소된 터치·다중 터치는 이동으로 처리하지 않는다. Editor Game View에서는 마우스 드래그로 같은 입력 경로를 검사한다. 방울 목표를 달성한 뒤 출구 칸에 도착하면 퍼즐 상태의 승리가 확정된다. 자동 연쇄 중 스와이프는 마지막 방향 하나를 예약하며, 연쇄가 끝나면 처리한다.

기존 `Assets/ANIMOL/Prefabs/hud_visual.prefab`를 수정 없이 사용한다. 배치·이미지·폰트·CanvasScaler·Safe Area를 바꾸지 않았다. 실제 방울 수만 원래 카운터에 연결하며 코스트 규칙이 없는 현재 스테이지의 COST -- 표시는 유지한다. 새 HUD, 방향 버튼, Restart 버튼, 결과 팝업을 넣지 않는다. 맵을 다시 시작할 때는 Play Preview를 다시 실행한다.

`ANIMOL/Puzzle/Create playable puzzle and validation maps`는 누락된 초기 에셋을 생성한다. 이미 편집한 맵·테마·씬은 덮어쓰지 않는다. 테마는 Create / ANIMOL / Puzzle Theme로 추가하고 재질별 16개 외곽 그림과 4개 안쪽 모서리 그림, 플레이어·방울·출구·배경을 연결한다. 기본 테마의 지형 그림은 코드로 생성한 32px 타일이며 기존 원본 그림을 변경하지 않았다.

## 데이터와 축소 정책

- `PuzzleMap`: width/height, 테마, 목표, 카메라, 시작점/출구, 방울, 지형/장치/장식의 독립 목록을 저장한다.
- 축소 전에 범위 밖으로 나갈 항목 수를 알리고 Resize and retain / Cancel을 제공한다. 축소 후에도 모든 원본 좌표와 자산 참조가 에셋에 남는다.
- 범위 밖 데이터는 비활성이다. 편집기는 경고, 좌표 목록, 격자 바깥 보관 영역을 표시한다. 다시 확대하면 복구된다.
- 시작점·출구·장치의 포털 목적지가 범위 밖이거나 막혔으면 미리보기를 거절한다. 목표 수보다 유효한 방울 배치가 적어도 거절한다. 이 검사는 배치 유효성 검사이며 전체 퍼즐 풀이 가능성을 증명하지 않는다.
- 런타임은 스테이지 에셋을 복제한다. 수집, 런타임 지형 변화, 재시작이 원본 맵을 수정하지 않는다.

## 카메라와 이동

셀 한 칸은 월드 1 단위이며, 전체 맵 경계는 `[-0.5, width-0.5] × [-0.5, height-0.5]`다. 직교 카메라 크기는 스테이지의 visibleRows / 2로만 정한다. 맵 크기를 보고 줌을 바꾸지 않는다. 기본값은 세로 11칸이다.

카메라는 플레이어 Transform의 실제 보간 위치를 따라가며 프레임 시간에 따른 지수 감쇠를 사용한다. 목표 위치와 최종 카메라 위치 모두 맵 경계로 제한한다. 화면보다 작은 축은 `(size-1)/2`에 고정하고 다른 축은 계속 추적한다. 해상도·Safe Area 변경 시 표시 영역과 경계를 재계산한다.

기존 HUD가 맵 끝의 플레이어를 가리지 않도록 게임 카메라의 viewport에 상단 여백만 둔다. 기본 topInset=90, bottomInset=0은 가로 540px 기준 단위다. 1080×1920에서는 게임 영역이 y=0–1740이고, 그 영역에 세로 11칸을 표시한다. 작은 맵도 이 플레이 영역 중앙에 놓인다. 배경은 별도 카메라로 전체 화면을 채우므로 HUD 뒤까지 원본 구도를 유지한다.

한 칸 이동은 기본 0.18초이며 Conveyor, Wind, Ice, Brake를 포함한 자동 연쇄도 같은 이동 경로를 사용한다. 실제 도착 시에만 셀·수집·승리를 갱신한다. 반복된 자동 상태는 감지해 연쇄를 종료하고 다시 조작할 수 있게 한다.

포털 진입 시 플레이어와 카메라를 목적지로 즉시 전환한다. 먼 중간 지형을 가로지르지 않는다. 출구 프레임을 먼저 표시하고 다음 프레임부터 보존된 방향의 미끄럼을 이어간다. 포털 출구의 재진입은 그 도착에 한해 억제해 양방향 포털 사이에서 즉시 왕복하지 않는다. Brake, 비미끄럼 바닥, 막힌 다음 칸에 따라 연쇄가 정상 종료된다.

현재 연출은 입력·게임 시간을 차단하지 않는다. `BlocksInput=false`이며 Hit Stop, 카메라 Kick, 플래시·햅틱을 추가하지 않았다. 이동 판정은 카메라나 그림에서 결정하지 않는다.

## 패럴랙스와 지형 그림

배경은 맵 크기와 무관한 **화면 고정 구도**다. 기존 `spr_bg_ranch_far_night_sky_idle.png`의 **948×1659 원본**과 기존 Mid/Near 그림을 사용한다. 원본 PNG·임포트 해상도를 바꾸지 않고, 화면 채우기에도 x/y에 같은 배율만 사용한다. Far에는 미세한 움직임을 위한 4% 여유를 두고, Mid/Near는 원본 PPU와 종횡비를 유지한다. 맵 폭·높이에 맞춰 이미지를 늘리거나 같은 배경을 반복 배열하지 않는다.

Far / Mid / Near의 깊이 계수는 0.08 / 0.30 / 0.65다. 각 층의 변위는 화면 크기의 `(0.003 + 0.02 × 계수)` 이내로 제한한다. 카메라의 이동 방향을 완만하게 반영하고 느린 idle 드리프트를 합성하며, 보간해 작은 움직임만 재생한다. 1080×1920의 최대 범위는 Far 약 5×9px, Mid 10×18px, Near 18×31px 이내다. 긴 맵 이동과 먼 포털도 배경 전체를 흘려 보내지 않는다. [정지 중 배경 재생](Validation/Puzzle/fixed_background_idle.gif)을 함께 저장했다.

배경 카메라는 화면에 고정되고 게임 카메라와 다른 렌더 층을 사용한다. 지형·장치·장식·플레이어·판정 격자에는 배경 변위를 적용하지 않는다. 실제 도착과 판정은 배경 애니메이션과 독립이다.

`PuzzleAutotile.Resolve`는 같은 재질이 없는 상하좌우를 노출로 계산한다. N=1, E=2, S=4, W=8을 OR한 0–15가 외곽 그림 인덱스다. 맵 밖과 다른 재질은 노출로 처리한다. 통과 금지 플래그는 재질 연결 조건에 포함하지 않아 충돌 판정이 그림 선택과 독립이다.

NE / SE / SW / NW는 각각 두 인접 상하좌우가 같은 재질이고 해당 대각선만 다를 때 적용된다. 타일을 네 사분면으로 표시하고 해당 사분면에 안쪽 모서리 그림을 사용한다. 잘린 부분의 알파를 유지하므로 실제 배경이 보인다. 여러 안쪽 모서리가 동시에 존재해도 처리한다.

편집기 그림은 현재 이웃 데이터로 즉시 계산한다. SetTerrain / RemoveTerrain은 변경 칸과 주변 8칸을 갱신하며, 리사이즈는 표시 영역과 경계 그림을 재구성한다. 런타임도 같은 코드를 사용한다. 지형 렌더러는 카메라 주변과 2칸 여유 영역만 생성한다. 카메라 밖 지형을 렌더하지 않아도 논리 데이터와 충돌 격자는 그대로 유지된다.

## 실제 Game View 검사

Unity Editor의 실제 Play 모드, 1080×1920 세로 Game View에서 이동 명령을 실행했다. `ScreenCapture.CaptureScreenshot`으로 UI를 포함한 원본 18장을 저장했다. 별도 Camera.Render나 오프스크린 렌더를 검수 근거로 사용하지 않았다. [전체 캡처 보기](Validation/Puzzle/contact_sheet.png), [캡처와 이동 프레임 기록](Validation/Puzzle/gameview.json), [요약](Validation/Puzzle/capture_summary.json).

| 경우 | 맵 크기 | 검수 결과 및 근거 |
|---|---:|---|
| 작은 맵 | 4×4 | 배율 유지, 양 축 중앙 배치, 실제 방울 수집과 출구 승리. [시작](Validation/Puzzle/small_centered.png), [승리](Validation/Puzzle/small_victory.png) |
| 긴 가로 맵 | 40×7 | y 중앙 유지, Conveyor/Wind 연쇄 후 x31, 오른쪽 끝에서 카메라 정지. [연쇄](Validation/Puzzle/horizontal_chain_end.png), [끝](Validation/Puzzle/horizontal_edge.png) |
| 긴 세로 맵 | 7×40 | 국소 격자 유지, 연쇄 후 y31, 위쪽 끝에서 정지, 기존 HUD 영역과 플레이 영역 분리. [시작](Validation/Puzzle/vertical_start.png), [끝](Validation/Puzzle/vertical_edge.png) |
| 1칸 통로와 섬 | 14×18 | 1칸 폭 접합·외곽과 독립 섬의 사방 노출을 표시. [통로](Validation/Puzzle/corridor_start.png), [섬](Validation/Puzzle/corridor_island.png) |
| U자 구멍 | 14×18 | 좌우 안쪽 모서리와 열린 홈의 알파를 확인. [왼쪽](Validation/Puzzle/u_inner_left.png), [오른쪽](Validation/Puzzle/u_inner_right.png) |
| 재질 경계 | 14×18 | Grass/Stone 경계가 양쪽에서 노출되고 통과는 유지. Sand의 막힌 칸은 별도 판정. [경계](Validation/Puzzle/material_boundary.png) |
| 런타임 변화 | 14×18 | (7,9) 제거로 3×3 갱신, 복원 시 주변 그림 회복, 원본 에셋·플레이어 셀 유지. [제거](Validation/Puzzle/runtime_terrain_change.png), [복원](Validation/Puzzle/runtime_terrain_restored.png) |
| 맵 끝 포털 | 40×12 | (3,5)에서 (38,5)로 컷 후 동쪽 미끄럼으로 (39,5) 도착·방울 수집. 이동 프레임에 먼 중간 카메라 위치 없음. [입구](Validation/Puzzle/portal_entrance.png), [출구 컷](Validation/Puzzle/portal_exit_cut.png), [연속 이동](Validation/Puzzle/portal_slide_end.png) |

모든 캡처에서 세로 표시 칸 수는 11로 유지된다. 실제 이동은 스와이프의 시작/종료 경로로 실행했다. 10,188개 이동 프레임에서 카메라 범위, 지형 Root 고정, 배경 변위 상한, 원본 그림의 균일 배율을 검증했다. 관측된 배경 최대 변위는 Far 약 3.4×7.0px, Mid 6.6×13.6px, Near 11.8×24.2px로 화면 구도를 유지했다. 기존 UI와 배경 파일에는 Git diff가 없다. 처음 추가했던 임의의 HUD·방향 버튼과 맵 크기에 따른 배경 늘리기는 제거하고 재검수했다.

재검수는 ANIMOL_Puzzle 씬을 열고 세로 1080×1920을 선택한 뒤 Play에서 `PuzzleGameViewValidation.Start()`를 실행한다. `Tools/Puzzle/inspect_captures.py`는 원본 캡처 크기를 확인하고 보기용 연락 시트와 요약을 만든다.

## 테스트와 보존 범위

- 전체 EditMode **57/57 통과**: 새 PuzzleMapTests 34개와 기존 아트 테스트 23개. [전체 결과](Validation/Puzzle/editmode.json).
- 전체 PlayMode **14/14 통과**: 새 PuzzlePlayTests 6개와 기존 아트 테스트 8개. [전체 결과](Validation/Puzzle/playmode.json).
- 중간 전체 실행에서 기존 TwilightSea 나룻배의 0.3초 후 terminal 프레임 검사 1건이 실패했다. 해당 원본·테스트를 수정하지 않고 전체를 재실행했으며 최종 14/14를 확인했다. 이 시간 의존 실패는 별도의 기존 테스트 안정성 검토 대상이다.
- 16가지 노출 마스크, 4방향 안쪽 모서리, 재질/충돌 독립, 크기 축소와 모든 레이어 보존, Undo/Redo, 여러 종횡비의 카메라 경계, 포털 방향·미끄럼, 실제 수집·승리, 자동 순환 종료, 입력 예약, 런타임 3×3 갱신, 기존 HUD 가림 방지, 스와이프 방향·짧은 입력·UI 제외·한 제스처 한 이동, 배경 비율 보존을 검증했다.
- 편집기 Play Preview도 실제 실행하여 Small 스테이지 시작과 종료 후 원래 씬 복원을 확인했다.
- `CommercialPolishPolicyTests`는 격리된 이전 프로젝트의 테스트이므로 실행하지 않았다. 현재 활성 ArtAssetTests / ArtQaPlayTests를 실제 실행했고 레거시 통과 수와 합치지 않았다.
- 기존 사용자 변경과 격리 폴더는 이번 커밋에 포함하지 않는다. 기존 아트의 PNG/클립·QA 씬·제작기는 수정하지 않았다.

구현의 진입점은 `PuzzleController`, 판정은 `PuzzleSession`, 표시 갱신은 `PuzzleMapView`, 제작은 `PuzzleMapEditor`다. 카메라 및 그림의 변경만으로 충돌·수집·목표·승리 상태가 바뀌지 않는다.

Unity API 근거: [Undo.RecordObject](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/undo/recordobject), [ScreenCapture](https://docs.unity.com/en-us/engine/7000.0/script-reference/unityengine/screencapture). 실제 컴파일과 실행 검증은 위 Unity 6000.3.8f1 결과를 기준으로 한다.
