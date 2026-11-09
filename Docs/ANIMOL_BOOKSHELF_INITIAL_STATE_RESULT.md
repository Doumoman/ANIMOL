# 잔영 극장 책장 초기 세로 상태 선택

맵 제작기의 잔영 극장 장치 목록에 `08 책장 · 세로 · 활성`을 추가했다. 기존 가로 책장과 함께 선택할 수 있으며, 배치한 책장의 `책장 시작 상태`에서도 `가로 · 기본`과 `세로 · 활성`을 선택할 수 있다. 기존 4방향 회전을 사용하는 시나리오는 반전 상태 선택도 유지한다.

세로 선택은 기존 판정 필드 `orientation=1`과 첫 그림 `vertical`을 사용한다. 책장 위아래를 포함한 세 칸 점유, 제작기 미리보기, 런타임 초기 그림이 같은 상태를 따른다. 별도의 장치 역할이나 새 활성화 판정은 만들지 않았다. 일반 `active` 값, 연결 그룹과 발판의 회전 규칙은 유지한다.

장치 목록의 두 책장 선택지는 연습용 원본을 복제한 데이터다. 목록을 읽거나 세로 항목을 배치하는 동작이 원본 연습 시나리오를 바꾸지 않는다. 기존 MapData, 씬, 프리팹과 제공 아트는 수정하지 않았다.

## 사용 방법

1. `ANIMOL > 맵 제작기`에서 잔영 극장 맵을 연다.
2. `테마 장치 목록`에서 `08 책장 · 세로 · 활성`을 선택해 배치한다.
3. 이미 배치한 책장은 선택 후 `책장 시작 상태`를 바꾼다. 검토 후 맵을 직접 저장한다.

## 실행한 검증

Unity 6000.3.8f1에서 프로젝트 C# 컴파일 오류 0개를 확인했다.

- 새 EditMode 테스트 3개: 원본을 바꾸지 않는 가로/세로 선택지, 세로 미리보기와 배치 점유, 직렬화 및 Undo/Redo, 세로로 시작한 책장의 연결 발판 회전.
- 기존 EditMode 테스트 2개: 양 방향 책장 끝칸의 통행 차단과 테마에 맞는 장치 목록 필터링.
- 실제 `PuzzleDeviceView.Bind` / `Settle`을 Edit Mode의 임시 복제본에서 실행했다. 초기 phase 1, 표시 상태 `vertical`, V03 사용, MissingBindings 0을 확인했다. 표시 스프라이트는 `rotating_bookcase/sweep/rotate/frame_04.png`이며 원본 이미지도 확인했다.
- 현재 열린 제작기 창은 `Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-07_MapData.asset`를 편집 중이다. 이 창의 실제 목록에 가로 0과 세로 1 선택지가 있고 각각 frame_00과 frame_04를 사용하는 것을 읽어 확인했다. ET-07은 기존 미저장 상태로 유지했으며 자동 저장하지 않았다.

전체 게임 Play Mode나 모바일 표시 검증은 이번 편집기 선택 기능 작업에서 실행하지 않았다. 런타임 판정·피드백 코드를 수정하지 않았으므로 보관된 CommercialPolishPolicyTests는 실행 대상에 포함하지 않았다.

검증 증거는 [새 테스트](Validation/BookshelfInitialState/new_editmode_tests.json), [점유 회귀 테스트](Validation/BookshelfInitialState/footprint_regression.json), [테마 목록 회귀 테스트](Validation/BookshelfInitialState/theme_palette_regression.json), [런타임 뷰 초기 바인딩](Validation/BookshelfInitialState/runtime_visual_binding.json), [현재 제작기 연결](Validation/BookshelfInitialState/editor_connection.json)에 있다.
