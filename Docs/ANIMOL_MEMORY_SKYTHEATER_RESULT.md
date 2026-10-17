# 기억 극장 공중 무대 환경팩 V02 적용 결과

2026-10-14 / Unity 6000.3.8f1 / Android 타깃 Editor / Built-in Render Pipeline / Linear.

## 실제 프로젝트 확인과 범위

시작 씬은 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity`, 스테이지는 `StarGarden`이었다. 현재 퍼즐은 가변 크기 정사각형 탑뷰 스와이프 방식이다. `PuzzleMap.terrain`의 위치/재질/blocked가 지형 판정이며 `devices`, `bubbles`, `start`, `exit`는 독립 목록이다. 탑뷰 퍼즐 코드를 사용했고 횡스크롤 러너나 보관된 제작기를 복원하지 않았다.

기억 극장의 실제 기존 씬은 `ANIMOL_MemoryTheater_Art_QA.unity`였다. 개별 SpriteRenderer 바닥 260개, 별도 큰 배경, 11종 마스터와 369개 `DeviceVisualView`가 있었으며 Tilemap/RuleTile은 없었다. 11종은 기존 상태/이벤트를 받는 **표시 프리젠터**다. 현재 실제 퍼즐 런타임의 장치 종류는 Conveyor/Wind/Ice/Brake/Portal이며 기억 극장 11종의 권위 있는 게임 판정 호출자는 아직 연결되어 있지 않다. 이번 환경 작업은 그 상태를 보존하며 새 장치 판정을 만들지 않았다.

적용 씬:

- 기존 `ANIMOL_MemoryTheater_Art_QA.unity`: 바닥/배경을 새 환경으로 교체. 11종·369개 프리젠터와 기존 QA UI/상태/이벤트/위치는 유지. 기존 기본 패널의 옛 커튼/램프 그림 두 개는 새 환경에서만 렌더링을 숨긴다. 프리젠터 자체와 다른 검수 패널의 원본 아트는 남긴다.
- 신규 `ANIMOL_MemoryTheater_Environment_QA.unity`: 충돌/퍼즐 조작자가 없는 접합 검사 씬.
- 신규 `ANIMOL_MemoryTheater_Puzzle_QA.unity`: 기존 `ANIMOL_Puzzle` 씬과 `Small` 맵을 복사해 환경만 연결한 플레이 검수 씬. 지형·장치·방울·시작·출구·목표·카메라 설정은 원본과 같다. 기존 레벨을 대체하거나 Build Settings에 새 레벨로 등록하지 않았다.

작업 전 전체 Assets 12,265개 파일의 SHA256을 기록했다. [최종 감사](Validation/MemorySkyTheater/audit.json)에서 기존 변경은 시각 연결용 런타임 4개(`PuzzleTheme`, `PuzzleMapView`, `PuzzleBackdrop`, 공용 픽셀 표시기 `SkyIslandBackdrop`)와 기억 극장 아트 QA 씬 하나뿐이다. 기존 이동/스와이프/코스트/HUD/수집/출구/카메라 추적 코드, 모든 기존 맵, 다른 테마/장치 프리팹·코드·스프라이트·임포트 메타는 그대로다. 사용자의 기존 미커밋 파일도 건드리지 않는다.

## 원본과 임포트

사용자 ZIP `MCP_INBOX/ANIMOL_MEMORY_SKYTHEATER_V02.zip`을 `Docs/Inbox/MemorySkyTheater/ANIMOL_MEMORY_SKYTHEATER_V02`에 풀었다. README_KO, manifest, qa_report를 읽고 SHA256SUMS의 **292개** 파일을 검증했다. 출처 폴더의 `.gitattributes`는 텍스트/팔레트까지 자동 개행 변환을 막는다. 원본 qa_report의 PASS/6144 pairCases는 제작 단계 PNG 검사이며 Unity 실행 결과와 구분한다.

런타임 개별 PNG **165개**를 원본 바이트 그대로 복사했다: Wood/Underside/Velvet blob47 각 47개(141), 소품 8개, 애니메이션 프레임 13개, 배경 3개. source/inspection/examples/camera와 애니메이션 sheet는 Assets에 자동 임포트하거나 충돌 자산으로 등록하지 않았다.

- 바닥: `Sprites/Tiles/Floor/memory_theater/SkyTheater/{wood,underside,velvet}/blob47`
- 소품/프레임: `Sprites/Props/memory_theater/SkyTheater`, 하위 `Animation`
- 배경: `Sprites/Background/memory_theater/SkyTheater`
- 카탈로그/Tile/시퀀스/시각 배치/검수 맵: `Data/Art/MemorySkyTheater`
- 환경 프리팹: `Prefabs/Environment/MemoryTheater`

실제 임포터를 검사했다: Sprite / Single / PPU80 / Full Rect / Point / Compression None / Mipmap off / NPOT None. 타일/배경 피벗은 (.5,.5), 기둥·오르간·객석·각광은 manifest의 아래 중앙(.5,0), 가로대는 위 중앙(.5,1)이다. 각 애니메이션 프레임도 소품과 같은 피벗이다. Full Rect 4개 정점과 전체 원본 캔버스를 유지한다. Android/iPhone/Standalone/WebGL 압축 override가 없다.

`alphaIsTransparency=false`로 투명 RGB의 임포트 확장을 막았고 재보간·재압축·trim·회전·반전을 하지 않았다. Atlas는 사용하지 않는다. 타일/소품/프레임의 승인 7색, 배경의 Sweetie16, 이진 알파 및 투명 RGB 0을 검사했다. 임포트 PNG 165개 모두 원본 SHA256과 같다.

## 연결 구조와 카펫

`TheaterTopology`는 노출 o=N1/E2/S4/W8, 안쪽 i=NW1/NE2/SE4/SW8을 계산한다. 안쪽 코너는 두 직교 이웃이 있고 대각선만 없을 때다. `oXX_iYY`를 직접 선택하며 connected-mask나 이전 코너 순서를 섞지 않는다. 같은 바닥 위의 장치/방울은 이웃을 끊지 않는다. 구조 점유 밖은 빈칸이다. RuleTile이 없으므로 별도 connected-mask 변환도 없다.

하나의 Grid(셀 X/Y=1unit)에 Underside(-20), Wood(0), Velvet(2) 세 Tilemap을 둔다. Underside 전체가 Wood 전체보다 먼저 그려지며 local Y는 **-0.175unit**이다. 타일/레이어의 transform은 회전/반전하지 않는다. 세 레이어와 소품에는 Collider/Collider2D/Rigidbody/Animator를 추가하지 않았다. Tile 자산의 colliderType은 None이다.

Wood와 Underside는 같은 구조 점유를 사용한다. 구조에는 기존 지형과 명시적인 **비이동 시각 데크**가 포함되며 실제 `Walkable`을 수정하지 않는다. Velvet는 별도 HashSet/시각 배치 좌표에서 선택하고 구조에 없는 곳에는 그리지 않는다. 카펫 경계는 그림만 바꾸며 높이·충돌·장치를 만들지 않는다. `SetCarpet`은 시각 마스크만 편집한다. 런타임 카펫 좌표는 리사이즈/재바인드에도 보존되며 활성 점유에 따라 숨겨지거나 복구된다.

지형 변경 시 변경 셀과 주변 8칸을 갱신한다. 실제 구조 외곽 범위가 변하면 외곽 데크를 다시 배치한다. 리사이즈 역시 그림과 데크만 재구성하며 기존 범위 밖 게임 데이터를 삭제하지 않는다. 기존 blocked 셀 표시와 # 피드백은 유지한다.

기본 경로는 blob47이며 inner4 패치의 알파 합성을 하지 않는다. 선택 격자는 끈 상태다. 원본 판자 구분선을 그대로 두었으며 이를 80px 이동 격자로 표시하거나 판정을 바꾸지 않았다.

## 커튼과 소품

3/4/5/9/13칸 프리팹을 제공한다. 각 틀은 왼쪽 기둥 + (너비-2)개 가로대 + 오른쪽 기둥이며 최소 3칸이다. `MemorySkyTheaterFrame.SetWidth`는 가로대만 가로로 반복한다. 기둥/천/가로대 스케일은 1이며 기둥 높이를 늘이지 않는다. 왼쪽 위 기준 피벗 위치는 왼쪽(.5,-2), 가로대(1.5+k,0), 오른쪽(N+1.5,-2)다.

`TheaterTopology.PropPosition`은 제공된 이미지 좌표 변환을 그대로 구현한다:

```text
((x + u*w)/80, -(y + (1-v)*h)/80)
```

예시의 structuralGround/carpet/walkableExample을 읽고 구분했다. 예시 walkableExample을 기존 맵에 적용하지 않았다. 접합 검사 마스크는 아트 QA 전용이다.

플레이용 기본 시각 배치는 현재 **인바운드 실제 지형의 범위**를 따라 뒤/앞 데크를 만든다. 빈 여백이 큰 맵에서도 맵 크기 숫자에만 의존해 소품을 따로 띄우지 않는다. 커튼/오르간은 뒤 데크, 객석은 앞 데크, 각광은 앞 턱에 배치한다. 틀 폭은 실제 구조 폭에 맞추되 1/2칸 폭의 경우 장식용 폭을 확보한다. 3칸 틀에서는 폭 2칸 오르간이 기둥을 가리지 않도록 오르간을 생략한다. 명시적 시각 배치 자산으로 별도 배치도 가능하다.

1×1, 2×5, 9×4의 오프셋/빈 여백 맵에서 소품 bounds가 이동 칸 중심을 가리지 않고 발이 데크 위에 놓이는 것을 검사했다. 모든 장식은 비충돌이며 기존 장치를 덮는 새 장애물을 만들지 않는다. 기존 QA의 장치 위에 새 소품을 뿌리지 않았다.

## 애니메이션과 배경

커튼 좌/우는 제공된 5프레임과 [.9,.35,.7,.35,.9]초(3.2초) 시퀀스를 사용한다. 천의 제공 프레임만 재생하고 기둥/가로대/루트는 움직이지 않는다. 원본 각 프레임의 지정 천 영역 밖 RGBA가 동일함을 검사했다. optionalLoop를 끄면 원래 첫 프레임으로 고정할 수 있다.

각광은 [off,dim,on] / [.06,.06,.12]초, 240ms 단발이며 끝나면 on을 유지한다. 환경 프리팹의 진입 때 시작하고 이동/입력/수집/충돌 이벤트에는 구독하지 않는다. 크기/외곽 변경으로 장식 인스턴스를 재구성할 때 기존 재생 시각을 넘겨 점등을 다시 시작하지 않는다. 프레임의 RGBA는 해당 off/dim/on 소품과 동일하다. Unity Light, 발광, 파티클, 새 사운드/햅틱, 카메라 Kick/흔들림, 시간 변경, 입력 잠금을 추가하지 않았다.

Far → Mid → Near → Underside → Wood → Velvet → 장식 → 기존 장치/캐릭터 → 기존 UI 순서다. 새 배경 960×960을 X/Y 양축 반복하고 화면 밖 복사본도 둔다. Sprite scale=1, 시차=.15/.35/.65, 원본 샘플 좌표를 정수 반올림 후 floor-mod 960으로 계산한다. 소품은 맵 좌표이며 화면에 고정한 큰 극장 그림이 아니다. 지형/장식/충돌에는 패럴랙스를 적용하지 않는다.

별길 목장에서 검증한 공용 네이티브 픽셀 표시기를 재사용했다. Sprite 3장/재질을 받는 오버로드만 추가했으며 기존 Starway 설정은 같은 경로로 전달된다. 원본 게임 카메라 transform·추적·클램프·orthographicSize·HUD viewport는 변경하지 않는다. 표시용 카메라만 80px/unit 픽셀에 정렬하고 Point 버퍼를 정수배로 표시한다. 1080×1920은 2배, 720×1280은 1배, 1080×2400은 2배와 여백이다. 기존 정책의 양축 5% 이하 정수 확대 crop만 허용하며 전체 맵 fit-to-screen을 하지 않는다.

| 샘플 카메라 px (오른쪽/아래) | Far | Mid | Near |
|---|---|---|---|
| (0,0) | (0,0) | (0,0) | (0,0) |
| (640,480) | (96,72) | (224,168) | (416,312) |
| (-400,960) | (900,144) | (820,336) | (700,624) |

## 실행 검증

모든 최종 PNG는 Unity **Play mode / Game View / ScreenCapture**이다. 특정 카메라의 오프스크린 이미지를 검수 화면이라고 기록하지 않았다. 런타임 RenderTexture는 픽셀 표시 구현이며 캡처는 그 뒤의 최종 출력과 기존 UI를 포함한다.

| 항목 | 실제 결과와 근거 |
|---|---|
| 한 칸 섬·1칸 통로·구멍·U/오목/볼록 접합 | [Wood 1x](Validation/MemorySkyTheater/stage_join_1x.png), [4x](Validation/MemorySkyTheater/stage_join_4x.png), [Velvet 1x](Validation/MemorySkyTheater/velvet_join_1x.png), [4x](Validation/MemorySkyTheater/velvet_join_4x.png). 각각 제공 검사 시트와 RGB 차이 **0픽셀**. 검사 시트 투명 영역은 검은 clear와 비교 |
| 네 방향 끝점 | [십자 검사](Validation/MemorySkyTheater/four_endpoints.png). 회전/반전 없이 끝점과 중앙 네 안쪽 코너를 표시 |
| 커튼 틀 3/5/9칸 | [3](Validation/MemorySkyTheater/proscenium_3.png), [5](Validation/MemorySkyTheater/proscenium_5.png), [9](Validation/MemorySkyTheater/proscenium_9.png). 제공 inspection과 RGB 차이 **0픽셀**. 검사는 선택 루프를 끄고 첫 프레임으로 고정 |
| 배경 3좌표 | [A](Validation/MemorySkyTheater/background_A.png), [B](Validation/MemorySkyTheater/background_B.png), [C](Validation/MemorySkyTheater/background_C.png). 제공 composite와 RGB 차이 **0픽셀**, 반복 경계/음수 좌표에 틈이나 밝은 줄 없음 |
| 소품·발·이동 칸 가림 | [플레이 환경](Validation/MemorySkyTheater/playable_environment.png). 배경과 별개인 맵 좌표/외곽 데크에 배치. 원본 게임 데이터 유지와 중심 가림 0을 테스트 |
| 기존 11종 이벤트 | [기존 QA](Validation/MemorySkyTheater/existing_device_qa.png), [이벤트 후](Validation/MemorySkyTheater/existing_device_events_final.png), [기록](Validation/MemorySkyTheater/existing_device_events.txt). 11개 기존 이벤트 수락, rootDelta=0, writer=1, BlocksInput=false. 369 프리젠터/11 마스터 유지. 이는 기존 표시 이벤트 검증이며 11종의 새 게임 판정 구현이 아님 |
| 세로 플레이 동작 | [실행 기록](Validation/MemorySkyTheater/Portrait/gameview.json). 기존 8개 맵의 임시 복사본에 환경만 연결. 스와이프로 수집/출구 승리, 가로/세로 무빙워크·바람 연쇄 x31/y31, 한 칸 통로/섬/U자/재질 경계, 런타임 변경을 확인 |
| 포털 | [출구 전환 프레임](Validation/MemorySkyTheater/Portrait/portal_exit_cut.png), [미끄럼 끝](Validation/MemorySkyTheater/Portrait/portal_slide_end.png). cut 1회, 오른쪽 방향/미끄럼 보존, (39,5) 도착과 수집. 먼 중간 영역을 가로지르지 않음 |
| 세로 해상도 | [720×1280](Validation/MemorySkyTheater/portrait_720x1280.png), [1080×1920](Validation/MemorySkyTheater/playable_environment.png), [1080×2400](Validation/MemorySkyTheater/portrait_1080x2400.png). 원본 캔버스/정수 픽셀 유지, 새 접합의 알파 테두리/겹친 단면/타일 틈/두꺼워진 연결선 없음 |
| 최종 전체 EditMode | [**73/73 Passed**](Validation/MemorySkyTheater/editmode_release.json). 기존 아트/다른 테마/장치/퍼즐 테스트와 새 환경 테스트 포함 |
| 최종 전체 PlayMode | [**18/18 Passed**](Validation/MemorySkyTheater/playmode_release.json). 기존 11종 QA/재진입, 퍼즐, 다른 테마, 새 카펫/리사이즈/점등 상태/옛 환경 숨김 테스트 포함 |

새 테스트는 256개 이웃 조합에서 47종의 Wood/Under/Velvet 선택과 정체성 변환, 실제 임포터 165개, 3/5/9칸 조립, 피벗 변환, 원본 맵의 모든 게임 필드 보존, 작은/긴/빈 여백 맵의 데크와 가림, Collider 부재, 카펫 편집과 리사이즈 복구, 단발 점등 시각의 연속성을 확인한다.

실제 세로 플레이 검사는 **19캡처 / 8,607이동 프레임 / 27체크**다. 매 이동 프레임에서 카메라 클램프와 원본 카메라 좌표에서 계산한 시차를 확인했고 모든 맵의 11행 시야를 유지했다. 입력 차단=false, timeScale=1이었다. 최종 원본/임포트/화면/보호 파일 감사는 `python -X utf8 Tools/MemorySkyTheater/audit.py`로 재현한다.

기존 옛 커튼/램프의 `forceRenderingOff`가 씬 저장 시 영속화되지 않는 것을 실제 화면에서 발견했다. 씬에 두 렌더러 참조를 저장하고 진입 때 표시만 숨기도록 수정한 뒤 전체 테스트를 재실행하고 최종 QA 캡처를 다시 얻었다. 기존 프리젠터/이벤트를 삭제하지 않았다.

## 미확인/남은 범위

- 실제 Android/iOS 빌드·물리 기기·GPU 성능·기기 Safe Area는 실행하지 않았다. 모바일 세로 검증은 실제 Unity Editor Game View의 세 해상도다. Built-in 외 파이프라인과 외부 카메라 스택은 검증하지 않았다.
- 코스트는 기존처럼 미연결 `COST --`이며, 아트 QA의 방울 HUD는 `--/--`다. 플레이 검수의 방울/출구는 실제 PuzzleSession 판정으로 확인했다. 코스트가 새로 구현되거나 검증됐다고 주장하지 않는다.
- 기억 극장 11종의 기존 상태·이벤트·표시 프리젠터를 보존/실행했지만, 현재 퍼즐 런타임으로 이어지는 11종 전체의 권위 있는 게임 판정 연결은 원래 미완료다. 환경팩 적용 때문에 이를 새 기믹으로 구현하지 않았다.
- 원래 UI를 유지해 1080×2400의 우측 방울 패널 일부 잘림이 남는다. [이전 동일 HUD 기준 캡처](Validation/StarwayIsland/baseline_tall_hud.png)와 같은 기존 문제다. 큰 맵의 장식/데크가 기존 카메라 시야 밖에 있는 경우도 있으며 이를 전부 보이게 하려고 추적/시야를 확대하지 않았다.
- 기존 QA UI가 상단 장식 일부를 덮는 배치도 유지했다. 검사 시트의 독립 커튼 틀 캡처에서는 전체 캔버스와 연결을 확인했다. 선택 격자는 사용하지 않았다.
- 보관된 CommercialPolishPolicyTests는 활성 테스트가 아니어서 실행하지 않았다. 기존 중복 `Rebuild memory theater QA` MenuItem 경고는 별도 기존 문제로 유지하며 새 메뉴는 다른 경로다.

## 수정 파일과 재현

기존 변경: `Scripts/Runtime/{PuzzleTheme,PuzzleMapView,PuzzleBackdrop,SkyIslandBackdrop}.cs`, `Scenes/QA/ANIMOL_MemoryTheater_Art_QA.unity`, 활성 경로 설명을 추가한 `Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md`.

새 런타임: `MemorySkyTheaterArt`, `MemorySkyTheaterLayout`, `MemorySkyTheaterRenderer`, `MemorySkyTheaterFrame`, `MemorySkyTheaterAnimation`, `MemorySkyTheaterInspection`. 새 Editor: `MemorySkyTheaterBuilder`, `MemorySkyTheaterGameViewValidation`. 새 테스트: `MemorySkyTheaterTests`, `MemorySkyTheaterPlayTests`. 신규 그림/데이터/프리팹/씬 경로는 위 목록이며 출처/검증/도구는 `Docs/Inbox/MemorySkyTheater`, `Docs/Validation/MemorySkyTheater`, `Tools/MemorySkyTheater`다. 활성 이름에는 버전 접미사를 붙이지 않았다.

`ANIMOL/Art QA/Apply memory theater environment`로 가져온다. 기존 대상 PNG가 원본과 다르면 덮어쓰지 않고 중단한다. 접합 씬을 Play하고 Game View 880×654 / 3520×2616으로 검사한다. 카펫 시트 비교는 Wood/Underside 렌더러를 끄고 Velvet만 켠다. 배경만 비교할 때 픽셀 표시기의 backgroundOnly/overrideSample을 사용한다. 커튼 틀 검사는 해당 프리팹을 원점에 두고 240×160 / 400×160 / 720×160 시야로 첫 프레임을 검사한다.

플레이 검수 씬을 1080×1920으로 Play한 뒤 `MemorySkyTheaterGameViewValidation.Start()`를 호출하면 세로 동작 검사를 재실행한다. 실행 후 원래 `ANIMOL_Puzzle` 씬과 1080×1920 Game View로 복귀했고 원래 씬은 dirty 상태가 아니다. 기존 Build Settings는 변경하지 않았다.
