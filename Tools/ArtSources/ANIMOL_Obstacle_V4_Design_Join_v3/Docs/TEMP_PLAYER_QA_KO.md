# 임시 플레이어 · 시작 → 방울 3개 → 목적지

실제로 조작 가능한 HTML과 Unity 이식용 코드를 제공한다. HTML의 입력·충돌·방울 집계·목적지 흐름은 실행해 검증했다. 실제 ANIMOL 저장소와 Unity 실행 환경은 여기에서 접근할 수 없으므로 Unity 컴파일·Play Mode 검증은 MCP 적용 단계에서 수행한다.

## 바로 플레이

`ANIMOL_Obstacle_V4_Preview.html`의 위쪽 플레이 화면을 사용한다. 모든 그림과 로직이 들어 있는 단일 HTML이며 외부 다운로드가 필요 없다.

| 조작 | 입력 |
|---|---|
| 좌우 이동 | ← → / A D / 화면 좌우 버튼 |
| 점프 | Space / ↑ / W / 점프 버튼 |
| 얇은 발판 아래 통과 | ↓ / S / 아래 통과 버튼 |
| 처음부터 | R / 처음부터 버튼 |
| 일시정지·재개 | 일시정지 버튼 |
| 검증된 경로 보기 | 경로 재생 버튼; 수동 입력 시 직접 조작으로 전환 |

화면 버튼은 누름·뗌·취소·포인터 캡처 해제를 처리한다. 키를 누른 채 창을 떠나도 이동이 계속되지 않는다. 브라우저 탭을 오래 비운 뒤 돌아와도 큰 시간 차이를 한 번에 물리 계산하지 않는다.

## 테스트 배치

좌표는 한 칸=1유닛, Y가 위로 증가하는 논리 좌표다. 플레이어는 중심 X와 발바닥 Y를 사용한다.

| 대상 | 좌표·크기 |
|---|---|
| 시작 발바닥 | (2.5, 1) |
| 상향 스프링 셀 | (4,1), (9,4), (14,7); 실제 발사면 Y=2,5,8 |
| 옆스프링 셀 | (20,2), LEFT; 뒤쪽 지지 벽은 X=21 |
| 방울 1 | 중심 (6.5,4.65), 반경 0.28 |
| 방울 2 | 중심 (11.5,7.65), 반경 0.28 |
| 방울 3 | 중심 (16.5,10.65), 반경 0.28 |
| 목적지 | 왼쪽 아래 (19,10), 폭 0.8, 높이 1.4 |
| 바닥·계단 | 전체 배치는 `Data/temporary_qa_stage.json` |

시작 지점 → 점프해 첫 스프링 위에 착지 → 방울 1 → 두 번째 스프링 → 방울 2 → 세 번째 스프링 → 방울 3 → 열린 목적지로 이어진다. 방울은 접촉 시 부서져 숨겨지고 ID당 한 번만 센다. 0~2개이면 목적지는 잠겨 있으며 3개가 된 뒤 실제 목적지 영역에 닿아야 클리어한다. 위험칸 접촉·추락·수동 재시작은 시작 위치, 방울, 목적지, 소멸판, 장치 상태를 초기화한다.

## 임시 동작

몸체는 폭 0.55·높이 0.88이다. 걷기 4.2, 점프 초기 속도 8.5, 중력 22, 상향 스프링 13, 떡 반동 6을 기본값으로 둔다. 코요테 시간과 점프 입력 버퍼는 각각 0.1초다. 영구 밸런스 값으로 확정한 수치가 아니다.
C02는 지정 접촉판에 들어오는 수평 속도의 크기를 보존하고 부호를 반전한다. C03은 위 발사판으로 내려오는 착지를 판정한다. C02 빈 코일 공간에 보이지 않는 천장·바닥·전체 셀 벽을 만들지 않는다. C03의 코일 아래 공간도 전체 셀 고체로 채우지 않는다.
C01/C03/C05/C06 윗면은 올라갈 때 통과하고 내려올 때 지지한다. C01/C05/C06은 아래 입력으로 통과할 수 있다. C05는 올라선 동안 지지하며 마지막 이탈 때 사라진다. C06은 하강 중 착지 때만 약한 반동을 준다. C08 벨트, C10 저마찰, C07 바람, C04 상시 위험도 임시 테스트에 포함했다.

JS는 1/120초 고정 스텝의 자체 충돌 계산이다. Unity 코드는 Rigidbody2D·CapsuleCollider2D·PlatformEffector2D를 사용하는 별도 이식이다. 두 환경의 접촉 솔버는 다르므로 Unity에서 실제 경로와 접촉면을 다시 검사해야 한다.

## Unity 코드와 씬 생성

| 파일 | 역할 |
|---|---|
| `UnityTemporaryPlayer2D.cs` | 이동·점프·버퍼·접촉판 발사·하향 통과·추락 복귀 |
| `UnityTemporaryRun2D.cs` | 시작점·3개의 고유 방울·목적지 조건·전체 리셋·임시 HUD |
| `UnityTemporaryBell2D.cs` | 접촉 트리거·1회 집계·재시작 복원 |
| `UnityTemporaryGoal2D.cs` | 잠금/열림 표시·실제 목적지 접촉 |
| `UnityTemporaryObstacle2D.cs` | 장치 이벤트·프레임·바람·위험·소멸판·Unity 붕괴 연결 |
| `UnityTemporaryStageVisuals2D.cs` | QA 소멸판의 이웃 상판 복원 |
| `UnityTemporaryCamera2D.cs` | 세로 화면 추적 카메라 |
| `Editor/UnityTemporaryQASceneBuilder.cs` | JSON·원본 v4 그림으로 플레이어/시작점/장치/방울/목적지/카메라 씬 생성 |

`Data/temporary_qa_graphics.json`은 같은 JS 선택기에서 만든 초기·C05 소멸 후의 v4 body/cap rect와 기능 프레임 경로다. 이 표는 C05 한 개인 고정 테스트 배치에 사용하며 운영 맵의 선택기를 대신하지 않는다.

새 `Assets/AnimolObstacleQA_v3` 폴더에 Art/Data, `Runtime/UnityTemporary*.cs`, Editor 생성기를 넣는다. `UnityCrumbleVfx`는 기존 정의가 있으면 재사용하고 중복 클래스를 추가하지 않는다. 기존 `ObstacleVisualResolver`는 해당 파일을 개정하며 같은 네임스페이스에 복사본을 추가하지 않는다. Tests는 기존 EditMode 테스트 어셈블리에만 넣는다.
컴파일이 정상인 뒤 다음 호출 또는 폴더 선택 후 `ANIMOL/QA/Create Temporary Player Scene v3`를 실행한다.

```csharp
UnityTemporaryQASceneBuilder.Create("Assets/AnimolObstacleQA_v3");
```

새 QA 씬 경로를 반환하며 기존 씬을 편집하지 않는다. 생성한 씬 하나만 열어 Play Mode로 테스트한다. 기존 씬과 QA 씬을 동시에 켜 플레이어·카메라가 겹치지 않게 한다.
씬 생성기는 PNG를 PPU32의 캐시 Sprite로 만들고 Point·압축 None·mipmap Off·NPOT None을 적용한다. C02는 접촉판만 BoxCollider2D, C03은 폭 28/32·두께 4/32의 OneWay 상판이다. 그림 압축으로 충돌체를 움직이지 않는다. 프로젝트 렌더 파이프라인에서 Sprite 머티리얼·정렬·카메라를 확인한다. 기존 Canvas를 쓰려면 Run.Changed/HUD와 Player의 SetLeftHeld/SetRightHeld/SetDownHeld/QueueJump/ReleaseInput을 연결한다.

프로젝트의 현재 Input System을 따른다. New Input System이 활성화되면 Keyboard.current, Legacy만 활성화된 경우 Input.GetKey를 사용한다. Both일 때 두 입력을 중복 처리하지 않는다. 임시 플레이어 하나에 이동 컴포넌트는 이 코드 하나만 둔다. 실제 운영 플레이어의 상태머신·체력·동물 전환·네트워크 입력과 섞어 적용하지 않는다.

## 실행 증거

`Validation/player_audit.json`과 `player_route_inputs.json`에 좌표를 강제로 바꾸지 않은 입력 경로를 기록했다. 입력으로 스프링 세 번 발사·방울 세 개 집계·목적지 접촉을 완료했으며 사망은 0회였다. 키보드·화면 버튼·키 해제·취소·재시작 콜백을 실제 embedded HTML 스크립트에서 실행했다. 전체 브라우저 레이아웃과 Unity 물리는 이 검증에 포함하지 않는다.

Unity 공식 참고: [Rigidbody2D.linearVelocity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody2D-linearVelocity.html), [Collider2D.GetContacts](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider2D.GetContacts.html), [Keyboard](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Keyboard.html).
