# ANIMOL 테마 퍼즐 프로토타입 적용 결과

## 현재 장치 로직

작업 전 활성 퍼즐에는 무빙워크, 바람, 얼음, 브레이크, 포털 5종의 이동 판정이 있었다. 테마 아트의 나머지 상태와 클립은 QA 표시용이었고 판정·코스트·승패와 연결되지 않았다. 이번에는 `PuzzleScenario`가 지정된 맵에 24종 장치의 격자 판정을 연결했다. 기존 맵의 `PuzzleSession`과 아트 QA 씬은 그대로 남아 있다. 기존 장치 프리팹은 연출 자료로 사용하며, 판정은 `PuzzleDeviceSession`에서 확정한다.

| 테마 | 플레이 가능한 조합 씬 | 크기 | 조합 맵에서 확인하는 장치 | 장치별 연습 맵 |
|---|---|---:|---|---|
| 별길 목장 | `Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity` | 9×63 | 01, 02, 07, 09, 18, 19, 20, 24 | 동일 |
| 잔영 극장 | `Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity` | 9×87 | 04, 06, 07, 08, 09, 13, 17, 18, 19, 22, 23 | 동일 |
| 낙일 성소 (`letter_observatory` 경로 유지) | `Assets/ANIMOL/Scenes/Prototype/ANIMOL_letter_observatory_Prototype.unity` | 9×71 | 03, 07, 09, 10, 14, 16, 18, 19, 21 | 동일 |
| 별너울 해안 (`twilight_sea` 경로 유지) | `Assets/ANIMOL/Scenes/Prototype/ANIMOL_twilight_sea_Prototype.unity` | 9×63 | 05, 07, 09, 11, 12, 15, 18, 19 | 동일 |

`Assets/ANIMOL/Data/Puzzle/Prototypes/`에 조합 맵 4개와 01~24 장치 연습 맵 24개, 대응 시나리오와 테마 참조를 저장했다. 24개는 전체 테마에서 중복되는 장치를 합친 수다. 각 조합 씬에는 해당 테마의 모든 장치 계열을 8행 간격의 연결된 시험 방으로 배치했다. 별돌/소켓, 판/다리, 광학 부품 등은 하나의 장치 유형에 속하는 여러 역할이다. 벽·금 간 벽·나무/금속 상자는 보드 부품이다.

맵에는 출발점, 방울, 출구와 검증용 이동 경로가 있다. 네 조합 시험 맵의 비용 제한은 0(무제한)이며 장치별 연습 맵의 기존 제한은 유지한다. 유효한 스와이프가 코스트 1과 장치 턴 1을 진행한다. 막힌 입력은 상태를 되돌리고 코스트·타이머를 올리지 않는다. 자동 연쇄는 한 행동에 포함된다. 배경 바다는 시각 요소이며 배의 논리적 물 칸은 시나리오의 `water` 목록만 사용한다. 포털은 출구로 즉시 화면을 전환한 다음 실제 이동을 표시한다. 플레이어 Transform을 따라가는 기존 카메라와 기존 HUD를 사용한다.

## 편집과 플레이

프로젝트에서 원하는 조합 씬을 열고 Play를 누르면 된다. `ANIMOL/Puzzle/Map Editor`에서 위 맵을 선택해 크기·테마·카메라·지형·장식·시작점·방울·출구를 편집할 수 있다. `Themed devices / water / goals`에서 장치의 역할, 방향, 연결 그룹, 선로/발판, 물 칸, 폭발 범위와 목표를 편집한다. `Device` 레이어에서는 설정된 장치를 복사 배치한다. Undo/Redo와 `Play Preview`를 사용할 수 있다. 크기를 줄여 범위 밖으로 나간 시나리오 장치·물 칸·선로도 보존하고 편집기에 표시한다. 필수 목표 장치가 범위 밖이면 미리보기를 막는다.

`ANIMOL/Puzzle/Rebuild themed device test maps`는 기존 테마 아트와 씬 참조를 보존하면서 네 조합 맵·시나리오만 재생성한다. `ANIMOL/Puzzle/Build themed prototypes`는 테마 에셋과 장치별 연습 맵까지 다시 작성하는 개발용 명령이다. 맵을 직접 수정한 뒤에는 재생성 명령을 실행하지 않는다. 이번 작업에서 Build Settings의 시작 씬은 바꾸지 않았다.

## 원본에서 미정이었던 프로토타입 규칙

자료에 없는 선택은 `PuzzleScenario`에 저장했다. 점유 중인 덩굴 다리는 플레이어가 벗어난 뒤 접히고, 셔터의 전역 위상은 점유 중에도 진행하지만 점유된 셀은 열어 둔다. 도약은 중간 두 칸을 건너뛰며 착지 칸이 막히면 진입을 거절한다. 거울 앞 스와이프는 제자리 회전 행동이고, 자석 스위치는 켜진 상태를 유지한다. 폭탄 범위는 각 폭탄의 `blastOffsets`에 명시한다. 이 프로토타입에서는 그 범위가 플레이어를 해치고 일반 상자는 기본적으로 파괴하지 않는다. 석상은 부순 벽의 칸에서 멈춘다. 광선과 바람은 바닥이 있는 칸에서 전파하며, 유리는 빛을 통과시키고 몸체와 바람을 막는다. 병아리는 기록된 실제 인접 이동만 따라가며 포털·도약으로 생긴 먼 좌표는 건너뛴다. 금속 상자는 바람에 밀리지 않고 나무 상자는 밀린다. 코스트 0은 무제한이다.

이 선택들은 상용 레벨 규칙 확정이 아니다. 특히 폭탄의 기본 범위와 피해 대상, 거울 회전 입력, 석상 파괴 후 정지 위치, 점유된 셔터의 위상은 원본 팩만으로 결정되지 않았다. 맵별 시나리오를 바꾸면 테스트 경로도 다시 검증해야 한다.

## 이전 적용 당시 검증

- Unity 6000.3.8f1 Editor, EditMode 128/128 통과. 24개 연습 맵과 4개 조합 맵의 출구 도달, 장치 상태와 클립 참조, 주사위 96개 전이, 막힌 행동의 코스트/타이머와 기존 거절 클립, 배, 폭탄, 포털, 셔터, 가시, 다리, 자석, 석상, 유리 등을 검사했다.
- Unity PlayMode 27/27 통과. 4개 조합 맵을 실제 `PuzzleSwipeInput`으로 완료했고 배 이동 중 플레이어 Transform과 먼 포털의 카메라 컷을 확인했다.
- 실제 Editor Game View의 1080×1920에서 28개 맵의 저장된 경로를 스와이프로 실행해 모두 클리어했고 장치 아트 바인딩 누락 0을 확인했다. `Docs/Validation/Prototypes/inspection_gameview.json`에 맵별 결과가 있다. 캡처 25장은 같은 폴더에 있으며 조합 씬 시작/완료, 주요 장치, 540×960의 1배와 2160×3840의 4배, 배경 샘플 `(0,0)`, `(640,480)`, `(-400,960)`을 포함한다.
- 기존 아트 원본과 이전 맵 자산은 수정하지 않았다. Android/iOS 실기기 입력, 성능 및 출시용 난도 검수는 이 작업에서 실행하지 않았다. 예시 경로로 통과했다는 결과가 모든 입력 순서에서 막다른 상태가 없다는 증명은 아니다.

이전 배치의 대표 캡처: [별길 목장 시작](Validation/Prototypes/starway_start.png), [기억 극장 시작](Validation/Prototypes/memory_theater_start.png), [시계 돔 시작](Validation/Prototypes/letter_observatory_start.png), [바다의 배 조종](Validation/Prototypes/device11_action.png), [4배 표시](Validation/Prototypes/starway_4x.png).

## 2026-10-19 전체 장치 배치 검증

- Unity Editor에서 네 조합 맵을 기존 GUID에 갱신하고 각 `.unity` 씬을 열어 원래 맵 참조가 유지되는지 확인했다.
- EditMode `PuzzleDeviceLogicTests` 43/43, `PuzzleMapTests` 37/37 통과. 네 맵의 전체 장치 계열·프리팹 바인딩·유효한 점유·출구 경로를 확인했다.
- PlayMode `PuzzlePrototype` 4/4 통과. 네 씬 직접 로드와 모든 조합 맵의 스와이프 완주를 포함한다. `EchoTheaterV02PlayTests` 1/1 통과.
- 새 배치의 Game View 수동 캡처와 Android/iOS 실기기 검증은 진행하지 않았다.
