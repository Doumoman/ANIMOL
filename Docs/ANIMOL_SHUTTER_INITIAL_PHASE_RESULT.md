# 셔터 초기 활성화와 반대 주기 유지

잔영 극장의 셔터 시작 위상을 ‘처음 활성화’ 체크에서 정하도록 수정했다. 이전에는 처음 그림과 통행 상태만 체크를 따르고 주기는 별도의 orientation을 사용해, 체크가 다른 두 셔터도 같은 위상으로 합쳐질 수 있었다.

| 처음 활성화 | 시작 | 유효 입력 1회 뒤 | 유효 입력 2회 뒤 |
|---|---|---|---|
| 체크 | 열림 | 닫힘 | 열림 |
| 해제 | 닫힘 | 열림 | 닫힘 |

비어 있는 셔터는 이 반대 주기를 계속 유지한다. 열린 시작은 phase 0, 닫힌 시작은 phase 1로 초기화하며, 기존 셔터 orientation 값은 시작 위상에 사용하지 않는다. 다른 장치의 orientation 처리와 유효 입력당 코스트는 유지한다. 제작기의 ‘처음 활성화’ 체크에는 주기 설명을 추가했고, 셔터의 무의미한 회전값 입력은 숨겼다. 기존 맵 파일을 변환하거나 저장하지 않는다.

점유 중인 셔터가 플레이어나 물체 위에서 닫히지 않는 안전 규칙은 유지한다. 따라서 점유 때문에 일시적으로 둘 다 열릴 수 있다. 기본 정책은 점유 중에도 위상 시계를 진행하므로 비면 원래 주기에 따른다. `shutterPhaseAdvancesWhenOccupied=false`를 별도로 설정한 시나리오는 기존처럼 점유 중 시계를 멈추며, 이 경우 점유 이력에 따라 상대 위상이 달라질 수 있다. 거절된 입력과 단순 대기는 주기를 진행시키지 않는다.

## 실제 적용 확인

현재 ET-08 원본의 두 셔터는 저장된 orientation이 모두 1이었다. 수정 후 같은 원본을 복제해 실제 세션을 초기화했을 때 `shutter_1`은 체크 해제·phase 1·닫힘, `shutter_2`는 체크·phase 0·열림으로 시작했다. 원본 직렬화와 미저장 표시는 바뀌지 않았다. [실제 ET-08 초기화 기록](Validation/ShutterInitialPhase/et08_runtime_initialization.json).

시작 위상 규칙은 기존 초기값이 체크 상태와 맞지 않던 셔터의 판정 그래프를 바꾼다. 해당 맵의 이전 정답을 새 규칙의 최단 경로로 단정하지 않으며, 다시 계산할 때 원본과 규칙을 재검증해야 한다. 이번 작업은 기존 경로 카탈로그·이동 예산·진행 기록을 덮어쓰지 않는다.

기존 경로 기록 25개도 확인했다. 원본 해시가 같고 셔터 초기 위상이 기존과 동등한 22개는 현재 런타임 API 재생으로 클리어·입력 수를 재확인했다. 이들은 이번 변경으로 판정 그래프가 바뀌지 않아 기존 최단 증거를 유지한다. SW03·SW05·ET-01은 이번 작업 이전에 원본 해시가 기록과 달라져 있어 현재 정답으로 재사용하지 않았다. 새 최단 탐색을 25개 모두 다시 실행한 것은 아니다. [경로 기록 재검증](Validation/ShutterInitialPhase/shortest_record_revalidation.json).

## 실행한 검증

Unity 6000.3.8f1에서 최종 컴파일 오류 0개를 확인했다.

- EditMode 11개 통과: 같은 legacy orientation 0/1/2/3에서 체크가 다른 두 셔터가 8턴 동안 반대 주기 유지, 거절 입력·Undo·Reset·탐색 스냅샷 복원, 점유 안전과 두 가지 점유 시계 정책, 기존 셔터 판정 및 한 칸 아트 검사.
- 새 PlayMode 검사 1개 통과: 가로와 세로 각각 6턴 동안 실제 표시 프레임과 판정이 반대이며, 한 칸 크기·Undo·Reset·재입장·MissingBindings 0·BlocksInput false 유지.
- 기존 실제 ET-16 한 칸 셔터 바인딩 PlayMode 검사 1개 통과.

EditMode에서 함께 선택된 구버전 `MemoryTheaterArtTests.ShutterDominoBookshelfAndTransparentBreakHoldTheirCommittedTerminal` 1개는 실패했다. 이 검사는 현재 없는 `Assets/ANIMOL/Prefabs/Themes/memory_theater/` 아래의 구 프리팹들을 읽는다. 보관된 아트를 복원하거나 테스트를 임의로 건너뛰도록 바꾸지 않았다. 전체 검사가 통과했다고 보고하지 않는다.

새 연출 효과나 피드백 사건을 추가하지 않았다. 보관된 CommercialPolishPolicyTests와 모바일 GPU 검증은 실행하지 않았다. 증거: [컴파일](Validation/ShutterInitialPhase/compile.json), [EditMode 결과](Validation/ShutterInitialPhase/editmode_tests.json), [엇박 PlayMode](Validation/ShutterInitialPhase/playmode_opposite_phase.json), [ET-16 바인딩](Validation/ShutterInitialPhase/playmode_authored_binding.json), [원본 보존 확인](Validation/ShutterInitialPhase/preservation.json).
