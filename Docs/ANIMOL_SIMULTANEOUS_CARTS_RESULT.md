# 잔영 극장 카트 동시 이동

같은 턴에 움직이는 카트들을 하나의 시간축으로 재생하도록 수정했다. 이전에는 각 카트의 이동 보간을 끝낸 뒤 다음 카트로 넘어가 카트 수에 비례한 지연이 생겼다. 이제 움직일 수 있는 카트들은 함께 출발하고 함께 도착하며, 카트 이동 자체는 한 대일 때와 같은 `secondsPerCell` 동안 진행된다.

실제 게임에서 사용하는 `PuzzlePrototypeController`에 적용했다. 카트 단계의 이벤트에 표시용 `cartTurn` 표식을 붙이고, 카트 Transform 이동만 공통 코루틴에서 처리한다. 기존 카트 판정 순서, 한 칸 이동·막힘·플레이어 밀기·입력 시작 칸 P0 복귀·코스트는 유지한다. 표식은 보드 상태나 탐색 키에 포함되지 않는다.

주사위/도미노 밀기, 목표 흡수와 밀기 자세 복귀가 끝나면 카트들이 함께 출발한다. 카트가 만드는 플레이어·병아리 이동은 확정된 사건 순서를 유지한다. 두 카트가 플레이어를 연속으로 밀 때에도 최종 판정 위치에 도착한다. 다음 장치 단계와 예약 입력은 카트 공통 이동이 끝난 뒤 처리한다. Undo/Reset/다시 로드는 기존 취소 경로로 모든 카트 코루틴을 함께 중단한다. `BlocksInput=false`를 유지하며 시간 배율은 변경하지 않는다.

## 실행한 검증

Unity 6000.3.8f1에서 컴파일 오류 0개를 확인했다.

| 검사 | 통과 | 실패 | 실패 이유 |
|---|---:|---:|---|
| EchoRailCartV01RulesTests EditMode | 17 | 1 | 기존 ET-06 전용 검사가 읽는 원본 맵 파일이 없음 |
| EchoRailCartV01PlayTests PlayMode | 11 | 1 | 기존 M06 재로드 검사가 같은 ET-06 원본을 복제할 수 없음 |

추가한 PlayMode 검사 3개는 모두 통과했다.

- 카트 4대의 이동 진행률이 매 프레임 같고 함께 도착한다. 연출 중 확정된 세션 상태 키와 코스트가 변하지 않는다.
- 공통 이동 도중 Undo를 하면 카트 4대가 전부 복원되고, 기다린 뒤에도 취소된 보간이 다시 진행되지 않는다.
- 서로 다른 방향의 두 카트가 동시에 움직이면서 플레이어를 연속으로 미는 경우에도 두 밀기와 최종 위치가 보존된다.

기존 검사에서도 목표 주사위 흡수·밀기 복귀 후 카트 출발, 카트/플레이어 동반 보간, 예약 입력, 폐곡선 회전, 레일 재로드, 이동 도중 Undo, 카트에 닿지 않은 플레이어의 도착 시 걷기 종료를 확인했다.

실패한 두 검사는 `Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-06_MapData.asset`를 요구한다. 이 파일은 테스트 전 MapData 해시 목록에도 없었다. 누락된 사용자 맵을 복원하거나 다른 맵으로 대체하지 않았다. 전체 검사를 통과했다고 보고하지 않는다.

MapData 폴더의 기존 파일 124개는 테스트 전후 SHA-256이 같다. 원본 맵·메타·경로 기록을 저장하지 않았고, 기존 작업 트리 항목 949개도 유지했다. 테스트 종료 후 Editor는 Play Mode 밖으로 돌아왔다. 새 연출 효과나 정책 사건을 추가하지 않아 보관된 CommercialPolishPolicyTests는 실행하지 않았다. 모바일 GPU와 실제 ET-06 재로드 검증은 완료하지 않았다.

근거: [컴파일](Validation/SimultaneousCarts/compile.json), [EditMode 결과](Validation/SimultaneousCarts/editmode_tests.json), [PlayMode 결과](Validation/SimultaneousCarts/playmode_tests.json), [파일 보존 검사](Validation/SimultaneousCarts/preservation.json).
