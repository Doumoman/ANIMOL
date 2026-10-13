# ANIMOL 80타일 러닝 프로토타입 검증

기존 16×16타일 청크 저장 구조를 유지한 채, 연속 주로와 층 전환을 검증하는 독립 씬 두 개를 만들었다. 각 씬은 좌→우 80타일과 우→좌 80타일 주로를 가진다. 두 주로의 합은 160타일이며 전환 진입부와 착지 구간은 여기에 포함하지 않는다. 운영 스테이지나 기존 월궁 그래픽 대표 씬에는 적용하지 않았다.

핵심 검토 대상은 기본 능력치에서의 완주, 걷기와 달리기에 따른 스태미나 변화, 스프링 전환 후 실제 착지다. 이 문서의 완주 기록은 자동 입력에 의한 물리 검증이며 사람의 조작감 평가나 운영 승인으로 대신하지 않는다.

## 저장 구조와 경로 예약

- 저장 단위: 기존 `StageMapDefinition.TilesPerChunk = 16`, 청크당 16×16셀. 1셀은 월드 1단위다.
- `RunRoutePlan.Reserve()`가 두 `RunLeg`, `TurnConnector`, 선택 구역을 먼저 예약한다. `PopulateEmptyMap()`은 그 뒤 지형을 채운다. 비어 있지 않은 맵은 거부한다.
- 각 주로는 x=[0,80), 정확히 5개 청크를 가로지른다. 주로의 바닥과 위쪽 3셀 공간에는 구멍·장애물·필수 점프가 없다.
- 전환 예약은 x=[80,98)이고 주로와 겹치지 않는다. 진입부는 12타일, 착지·회복 구간은 16타일이다.
- 시작과 도착 쪽의 4타일 여유 공간도 주로 길이에서 제외한다. 선택적인 일방향 징검다리 연습 구역은 시작점 왼쪽에 분리했다.

| 씬 | 주로 A | 주로 B | 전환 |
|---|---|---|---|
| `RunDown.unity` | 바닥 y=8, 좌→우 80타일 | 바닥 y=0, 우→좌 80타일 | 오른쪽 스프링에서 8타일 아래층으로 전환 |
| `RunUp.unity` | 바닥 y=0, 좌→우 80타일 | 바닥 y=8, 우→좌 80타일 | 오른쪽 스프링에서 8타일 위층으로 전환 |

전환 진입부는 기존 `PlatformEffector2D`의 일방향 규칙을 사용한다. 같은 높이의 고체 바닥에서 옆으로 접촉하면 충돌 무시가 지속되는 문제가 실제 Game View에서 발견되어, 진입부 상면을 0.125타일 낮췄다. 이는 주로 밖의 작은 내려가는 턱이며 점프 입력은 필요 없다. 반동 후 진입부 아래를 통과할 때도 고체 모서리에 걸리지 않는다. 상승 도착층 역시 일방향 바닥으로 만들어 아래에서 통과하고 위에 착지한다.

## 유지한 캐릭터 규칙

기존 `DevPlayer.prefab`, `DevPlayerController`, `SwipeLocomotionState`, `SharedStaminaState`를 사용하며 원본을 수정하지 않았다.

| 항목 | 현재 설정 |
|---|---|
| 걷기와 달리기 목표 속도 | 3.5 / 5.5셀/초 |
| 지상 가속·감속 / 공중 가속 | 30 / 24 / 18 |
| 점프 초기 속도 / 중력 배율 | 8.5 / 2.5 |
| 점프 수 / 코요테·버퍼 | 1회 / 각각 0.12초 |
| 최대 스태미나 | 100 |
| 달리기 소모 / 지상 걷기 회복 | 초당 18 / 초당 15 |
| 소진 | 기존 3초 기절 및 이동 정지 |
| 일방향 하향 통과 | 현재 밟은 발판과 플레이어의 충돌만 기본 0.25초 무시 후 복구 |

첫 방향 입력은 걷기, 같은 방향 추가 입력은 달리기, 반대 방향은 정지다. 달리기에서 걷기로 낮출 때도 별도 능력치를 조작하지 않고 반대 입력 후 원래 방향 입력을 보낸다. 혼합 검증은 스태미나 30 미만에서 걷고 85 초과에서 다시 달리며, x≥80인 전환·착지 구역에서는 걷는다. 걷기 검증은 처음부터 끝까지 걷기만 사용한다. 필요한 이동은 일반 입력 API로 수행하며 경로 진행 중 좌표 이동, 충돌 무시, 스태미나 주입, 점프 입력을 하지 않는다.

## 스프링과 카메라

전환 스프링은 기존 `SideSpringObject`의 접촉 방향 판정, 접촉당 1회 발동, 방향 반전과 수평 충격을 사용한다. 수평 충격은 −8이며 하강 사례의 수직 충격은 0, 상승 사례는 23이다. 이는 캐릭터 능력치가 아니라 프로토타입 장치 설정이다.

현재 캐릭터는 점프 버튼이 해제되면 모든 양의 수직 속도를 매 물리 틱 절반으로 줄인다. 외부 스프링까지 이 규칙이 적용되므로, 상승 장치에만 작동하는 `RunSpringLift` 처리를 추가했다. 이 컴포넌트는 상승 중 외부 발사 속도에 기존 중력을 적용해 Rigidbody2D에 전달하고 정점·고체 천장 접촉·기절에서 종료한다. 일방향 발판의 통과 가능한 밑면 접촉은 고체 천장으로 처리하지 않는다. 위치를 지정하거나 충돌을 끄지 않으며 수평 입력과 스태미나는 기존 컨트롤러가 계속 처리한다. 일반 점프와 공용 스프링 코드는 그대로다. 이 외부 발사 채널을 공용 기능으로 채택할지는 별도 검토 대상이다.

카메라는 기존 12셀 가로 시야와 세로 화면 정책을 사용한다. 평지에서는 진행 방향으로 2.2셀을 앞서 보고, 전환부에서는 플레이어와 예상 착지 지점을 함께 프레이밍한다. 입력 잠금, 카메라 킥, 전체 화면 플래시, 게임 시간 변경은 추가하지 않았다. HUD는 실제 경과 시간·보행 상태·스태미나·착지 상태를 표시하고 기존 Safe Area와 터치 입력 소유권을 사용한다.

## 물리 측정 결과

최종 측정값은 `RunPrototype/Telemetry/`의 시나리오별 JSON과 0.2초 간격 CSV, [검증 대조 기록](RunPrototype/final-verification.json)에 보관한다. 기본 캐릭터로 네 경로 실행 모두 완주했고, 스프링 발동은 각 1회, 점프 입력·리스폰·주로 내부 비접지 틱은 모두 0이었다.

| 사례·전략 | 전체 시간 | A 연속 바닥·통과 시간 | B 연속 바닥·통과 시간 | 스태미나 최저 → 완주 | 착지 위치 x / y |
|---|---:|---|---|---|---|
| 하강·혼합 | 45.18초 | 80셀 · 18.34초 | 80셀 · 18.19초 | 29.68 → 43.48 | 90.02 / 1.61, 성공 |
| 상승·혼합 | 45.14초 | 80셀 · 18.34초 | 80셀 · 18.18초 | 29.68 → 43.48 | 88.23 / 9.46, 성공 |
| 하강·걷기 | 57.16초 | 80셀 · 24.21초 | 80셀 · 24.22초 | 100 → 100 | 90.03 / 1.61, 성공 |
| 상승·걷기 | 57.12초 | 80셀 · 24.21초 | 80셀 · 24.21초 | 100 → 100 | 88.23 / 9.46, 성공 |

주로 통과 시간은 x=0·80 경계를 지나는 CSV 샘플을 선형 보간한 근삿값이다. 전체 시간에는 시작 여유 공간과 전환부도 포함된다. 원시 기록과 계산은 [주로별 측정 JSON](RunPrototype/leg-measurements.json), `summarize_telemetry.py`에 남겼다. 지상 속도 중앙값은 걷기 3.304, 달리기 5.304셀/초였다. 설정 목표값과 실제 충돌·마찰이 적용된 속도를 구분한다.

혼합 전략의 A 주로 스태미나는 경계 기준 약 94.42→48.48이며, 전환부의 걷기로 회복해 네 실행 모두 착지 시 100이었다. B 주로는 혼합 약 100→41.10, 걷기 100→100이었다. 상승 정점의 플레이어 중심 높이는 y=10.98이다. 착지 x=88.23 또는 90.02는 예약된 [80,96) 안에 있고, 복귀 주로 전까지 약 8.2~10.0셀의 재출발 공간이 남는다.

달리기만 유지한 별도 소진 검사는 5.88초, x=27.84에서 스태미나 0으로 기절했다. 따라서 80셀은 **끊기지 않는 바닥 길이**이지, 현 능력치로 달리기 상태를 끊임없이 유지할 수 있는 거리가 아니다. 스태미나를 바꾸지 않아 걷기·달리기 선택의 차이가 드러나며, 혼합 전략은 소진 없이 걷기보다 약 12초 빠르다.

## 세로 화면 증거

`Docs/RunPrototype/Captures/`에 Unity Game View 원본 PNG를 저장한다. `ScreenCapture.CaptureScreenshotAsTexture`를 프레임 종료 시 호출하므로 오버레이 HUD가 포함된다. `start`, `run`, `recover_walk`, `approach`, `launch`, `flight`, `apex`, `landing`, `return_run`, `finish`는 해당 상태가 실제 발생했을 때만 기록된다. 캡처 시 사용한 혼합 입력 기록도 같은 폴더에 남긴다. 이미지 생성이나 재구성으로 실제 플레이를 대체하지 않는다.

1080×1920과 1080×2400 각각 19장, 총 38장의 픽셀 크기를 확인했다. 두 씬 모두 두 해상도에서 혼합 전략으로 완주했다. 캡처 폴더의 시나리오별 CSV·JSON은 마지막 1080×2400 실행 기록이며, 전체 테스트 측정은 별도 `Telemetry/`에 보관한다.

| 사례 | 1080×1920 | 1080×2400 |
|---|---|---|
| 하강 | [달리기](RunPrototype/Captures/down_run_1080x1920.png) · [비행](RunPrototype/Captures/down_flight_1080x1920.png) · [착지](RunPrototype/Captures/down_landing_1080x1920.png) | [달리기](RunPrototype/Captures/down_run_1080x2400.png) · [착지](RunPrototype/Captures/down_landing_1080x2400.png) |
| 상승 | [스프링 발사](RunPrototype/Captures/up_launch_1080x1920.png) · [정점](RunPrototype/Captures/up_apex_1080x1920.png) · [착지](RunPrototype/Captures/up_landing_1080x1920.png) | [정점](RunPrototype/Captures/up_apex_1080x2400.png) · [착지](RunPrototype/Captures/up_landing_1080x2400.png) |

## 검사 범위와 원본 보존

- 시작 커밋: `3c1a7e79f62e4466ecf08e3a9060ac0c0b138ee4`.
- [작업 시작 기록](RunPrototype/baseline.json)에 기존 미커밋 파일 1,032개와 T01-S01·MoonGraphicsQA 해시를 기록했다.
- 신규 테스트는 80타일 연속성과 5청크 횡단, 예약 영역 분리, 맵 덮어쓰기 거부, 두 방향·두 전략의 실제 완주, 소진, 일방향 진입·하향 통과, 두 세로 해상도의 카메라를 확인한다.
- 사전 전체 PlayMode는 70/70 통과했다. 일부 물리 검사만 함께 실행한 결과는 12/14였으며, 그 결과도 `baseline-physics.xml`에 보존한다. 기존 검사들이 남아 있는 씬에 영향을 받는 정황이 있어 새 프로토타입 검사는 시작·종료 씬을 격리했다. 기존 검사를 고쳐 통과시키지 않았다.
- 전체 EditMode의 기존 실패는 직전 그래픽 작업의 최종 XML과 이름·실패 메시지까지 비교한다. 수동 맵을 과거 기대값으로 되돌리지 않는다.
- 씬과 맵은 `Assets/ANIMOL/RunPrototype/`에만 생성한다. Build Settings, 캠페인 등록, Ready·보상·사람 검토 상태를 변경하지 않는다.

최종 검증은 Unity **6000.3.8f1**에서 수행했다. 최신 스크립트 컴파일 성공, 컴파일 오류 0건이다. 전체 EditMode **159/165 통과**, PlayMode **79/79 통과**, 신규 프로토타입 검사 **12/12 통과**, `CommercialPolishPolicyTests` **3/3 통과**다. EditMode 실패 6건은 기존 실패와 이름·메시지가 모두 같고 새 회귀는 0건이다. 보존 검사에서 기존 1,032개 파일 및 보호 대상 두 파일의 SHA-256 변경도 0건이었다.

기존 EditMode 실패 6건은 다음과 같다. 비교 원본은 `Docs/GraphicsQA/final-editmode.xml`이다.

1. `M8MoonPalaceStageTests.AllTenBubbleTriplesFitConservativeAutomatedRouteBudgetWithoutCreatingHumanReview`
2. `M9PortraitPolicyTests.CampaignIdentityAndReadinessRemainAtM8Boundary`
3. `M9T01ContentTests.AllTenTriplesHaveARepeatableSeedAndFitTwoDimensionalBudgets`
4. `M9T01ContentTests.AuthoredMapExpandsToFourByTwoChunksWithoutChangingStableIdentity`
5. `M9T01ContentTests.LearningRoutePreservesEveryM8ObjectIdAndKeepsRabbitOnly`
6. `T01S01ManualAuthoringTests.InitialBackup_MatchesCurrentLayoutObjectsAndContentHash`

재검증 명령은 저장소 루트에서 실행한다. 전체 검사는 열린 Unity Editor를 먼저 정상 종료한 뒤 실행한다.

```powershell
unity test . --mode EditMode --output Docs/RunPrototype/final-editmode.xml --timeout 600 -- -logFile Logs/run-prototype-final-edit.log
unity test . --mode PlayMode --output Docs/RunPrototype/final-playmode.xml --timeout 600 -- -logFile Logs/run-prototype-final-play.log
powershell -NoProfile -ExecutionPolicy Bypass -File Docs/RunPrototype/verify.ps1
python Docs/RunPrototype/summarize_telemetry.py
```

EditMode CLI는 실패 시 라이선스 토큰 경고도 함께 출력하지만 테스트 XML은 끝까지 생성되었다. 판정은 콘솔 문구가 아니라 개별 테스트 결과와 기존 실패 메시지 대조를 기준으로 한다.

## 실행과 변경 파일

Unity에서 `Assets/ANIMOL/RunPrototype/RunDown.unity` 또는 `RunUp.unity`를 열고 Play로 실행한다. 기본은 수동 조작이다. 좌우 방향키/A·D와 기존 터치 스와이프, Space 점프, 터치 하향 스와이프를 사용한다. `R`은 재시작, `F1`은 걷기 검증, `F2`는 혼합 검증, `F3`은 소진 검증이다. `ANIMOL/Run Prototype` 메뉴로 두 씬을 열거나 캡처할 수 있다. 생성 메뉴는 저장하지 않은 씬 변경이 있으면 중단한다.

- `Scripts/Core/RunRoutePlan.cs`: 거시 경로 예약과 기존 맵 형식으로의 지형 생성.
- `Scripts/Gameplay/RunPrototypeSession.cs`: 맵 기반 런타임 지형, 기본 플레이어, HUD, 입력 검증, 기록·캡처.
- `Scripts/Gameplay/RunSpringLift.cs`, `RunPrototypeCamera.cs`: 한정된 외부 발사 채널과 진행·착지 프레이밍.
- `Scripts/Editor/RunPrototypeBuilder.cs`: 독립 씬·맵·예약 계획 생성과 메뉴.
- `Tests/EditMode/RunPrototypeTests.cs`, `Tests/PlayMode/RunPrototypePlayModeTests.cs`: 구조·물리 회귀 검사.
- `Assets/ANIMOL/RunPrototype/`: 생성된 두 씬, 두 맵, 두 예약 계획 및 메타 파일.
- `Docs/RunPrototype/`와 이 보고서: 기준 기록, 테스트 결과, CSV·JSON, 캡처, 보존 검증 스크립트.

위 스크립트·테스트 경로는 모두 `Assets/ANIMOL/` 기준이다. 이번 변경은 기존 미커밋 자산·코드를 참조하므로 이 커밋만 깨끗한 체크아웃에 가져오는 것으로 전체 워크트리를 재현할 수는 없다. 기존 작업은 별도 커밋에 섞지 않는다.

## 남은 검토

그래픽은 이동 판독용 임시 블록과 기존 플레이어 표현이다. 80타일 평지의 지루함, 걷기 전환 입력의 편의성, 손가락 가림과 실기기 Safe Area·성능은 사람이 실제 기기에서 평가해야 한다. 자동 완주만으로 재미나 조작감이 검증되었다고 판단하지 않는다. 전체 테마나 운영 스테이지로의 적용, 스태미나 밸런스 변경, 공용 스프링 입력 규칙 변경은 이번 범위에 포함하지 않는다.
