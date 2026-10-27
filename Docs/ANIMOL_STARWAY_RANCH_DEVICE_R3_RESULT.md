# ANIMOL 별길 목장 장치 V13 R3 적용 결과

2026-10-27. Unity 6000.3.8f1 연결 에디터에서 실제 임포트, 컴파일, 빌드, EditMode 및 PlayMode를 실행했다.

## 설치 및 연결

- 제공 ZIP `MCP_INBOX/ANIMOL_STARWAY_RANCH_DEVICES_V13.zip`의 `SHA256SUMS.txt` 391개 항목은 원본에서 모두 일치했다. 기존 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13` 한 곳에 병합했고 기존 Sprite `.meta` 변경은 0건이다. 새 패키지 폴더를 만들지 않았다.
- 설치본의 빌더는 기존 검사 프리팹의 Unity 에셋 이름 `InspectionOnly_AllClips`를 소유 자산으로 허용하도록 수정했다. 기존 에셋을 보존하며 재빌드하기 위한 변경이다. 설치본 `SHA256SUMS.txt`에서 이 파일의 해시를 갱신했고 설치본 391/391개를 재검증했다. 원본 ZIP의 카탈로그 해시 `5c82bb2c11ae11911f59291bd23c6421810a50a140a62e3084cac030b63db933`는 유지했다.
- `Runtime/ANIMOL.Art.RanchDevicesV13.asmdef`에서 순수 `ANIMOL.RanchDeviceRulesR2` 어셈블리를 참조한다. `RanchPondPresentationR3`와 `RanchPondSluiceVisualR3`를 포함해 C# 컴파일 오류는 0건이다.
- `RanchDevicesV13Builder.Build`를 현재 `animation_catalog.json`으로 실행했다. 생성 카탈로그는 최신 Pin으로 `Validate`를 통과했고, Sprite **325개**, `.anim` **158개**, `InspectionOnly_AllClips.prefab`의 시각 **158개**를 Unity에서 확인했다. 325개 모두 Single, FullRect, PPU 256, 중앙 Pivot, Bilinear, 무압축, Mipmap Off, Clamp 및 footprint에 맞는 320×320 / 576×320 / 320×576 캔버스다. 예전 `InspectionOnly_All137Clips.prefab`은 최신 검사 보드로 사용하지 않았다.
- 공식 장치 번호는 추가하지 않았다. 기존 수문·양 배치의 `family`와 `instanceId`를 유지한다. 기존 수평 QA 맵과 한 방향 문 V12 바인딩도 유지했다.

## 실제 프로젝트 바인딩

| 역할 | 경로와 변경 |
|---|---|
| 수문 배치·검증 | `Assets/ANIMOL/Scripts/Runtime/PuzzleScenario.cs`: A=`position`, 새 배치는 B=`pondB`와 `hasPondB`, 위상=`rightState`(wetA). 이전 배치는 기존 `direction`으로 B를 읽어 호환한다. A/B 순서를 정렬하지 않는다. 상하좌우 정확히 한 칸만 허용한다. |
| 입력·Undo·회계 | `Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSession.cs`: 일반 물 진입 판정보다 먼저 `TryCrossPond`를 적용한다. 변경 전/후 쌍은 별도 복사본으로 이벤트에 담고, 기존 세션의 한 입력 비용만 반영한다. `waterRoute`와 `actualRoute`를 서로 바꾸지 않는다. 양 직접 밀기·풀과 양 동시 제거·바람 연쇄는 R2 규칙 그대로다. |
| 시각·이동 | `Assets/ANIMOL/Scripts/Runtime/PuzzleMapView.cs`, `Assets/ANIMOL/Scripts/Runtime/PuzzlePrototypeController.cs`: 두 칸 중심에 전용 자식 렌더러 하나를 두고 축별 R3 정지/물 이동 클립을 쓴다. 성공이 확정된 뒤 물 클립과 반대 방향의 플레이어 시각 이동을 같은 1초에 시작한다. Undo·Reset·Load·재활성화는 현재 권한 상태로 다시 표시한다. |
| 배치 제작기 | `Assets/ANIMOL/Scripts/Editor/PuzzleMapEditor.cs`: B를 A의 북·동·남·서 이웃으로 선택·변경한다. 실제 두 칸 점유, 미리보기 중심 및 선택 테두리를 축에 맞추고, 잘못된 인접과 젖은 시작 칸을 거절한다. |
| 검사 맵 | 기존 `Assets/ANIMOL/Data/Puzzle/Prototypes/StarwayRanchDeviceQA.asset`은 수평 검사로 유지했다. 새 `StarwayRanchVerticalQA.asset`과 `_Scenario.asset`은 명시적 A=(2,2), B=(2,3), wetA=false인 수직 검사 맵이다. 기존 캠페인 배치를 대체하지 않는다. |

## 실제 실행한 검증

| 검증 | 결과 |
|---|---|
| 에디터 컴파일 / 카탈로그 / 임포터 / 프리팹 | 오류 0 / 유효 / Sprite 325·클립 158·프리팹 시각 158 확인 |
| 제공 순수 규칙 `RanchDeviceRulesR2Tests` + `RanchPondR3Tests` | 21/21 + 10/10 통과. 양축, 네 물 방향, A/B 역순, 플레이어 반대 경로, 차단 시 물 효과·비용 0 포함. 두 `.cs`를 기존 Editor 테스트 어셈블리에 복사해 실행했다. 기존 테스트 asmdef가 규칙 어셈블리를 이미 참조하므로 중복 asmdef는 만들지 않았다. |
| `RanchR2HostTests` | 14/14 통과. 수평·수직, A/B 반대 저장, 명시 B 우선, 역방향, 한 번 과금, Undo, 양·풀·바람 포함. |
| `RanchR2PlayTests` | 7/7 통과. 저장된 수평·수직 QA 맵 왕복, 단일 렌더러, 물/플레이어 동시 반대 이동, 1초 구간, 양 제거, Undo·Reset·스냅샷 복구·비활성화/재활성화·Load 포함. |
| `RanchDevicesV13ClipPlayTests` | 1/1 통과. 양·병아리 네 방향, 양 먹기, 둥지 앉기, 가로·세로 수문 정지/물 방향, 바람개비 off/3/6/9, 12프레임/12fps 재생 확인. |
| `PuzzleMapTests` | 54/55 통과. R3 수문 배치/렌더링과 기존 한 방향 문 V12 바인딩은 통과. 유일한 실패는 기존 사용자 미커밋 `PuzzleMapf`의 맵 밖 필수 목표이며 이번 장치 변경과 무관하다. 해당 맵을 수정하지 않았다. |
| 실제 게임 뷰 540×960 | 저장된 수평·수직 QA 맵에서 교환 전, 물 흐름 중, 교환 후를 캡처해 확인했다. 수직은 북쪽 물→남쪽 구덩이, 플레이어 남→북; 수평은 동쪽 물→서쪽 구덩이, 플레이어 서→동으로 보였다. 화면에 설명 화살표나 P 표시는 추가하지 않았다. |

## 미실행·미확정

- 캠페인 전체 스테이지의 각 수문 배치와 모든 캐릭터 앞뒤 가림, 중간 프레임 콜라이더를 전수 확인하지 않았다. QA 맵의 수평·수직 시각과 세션 판정은 확인했다.
- 목적지 차단 시 물 흐름 없음은 제공 순수 규칙 테스트로 확인했다. 실제 저장 QA 맵의 점유 차단 조합은 별도 스테이지가 없어 PlayMode에서 재현하지 않았다.
- 병아리 등 추가 침수 객체의 정책, 수문 정지 뒤 재추종, 바람·컨베이어·제동판 경합 순서는 확정하지 않았다. 커널의 `floodPolicyHolds` 진단을 유지했다.
