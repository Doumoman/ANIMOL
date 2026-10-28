# 잔영 극장 V3 Unity 적용 결과

2026-10-29 · Unity 6000.3.8f1 · 입력: `MCP_INBOX/ANIMOL_Echo_Theater_35_Package.zip`

## 적용 데이터와 경로

- 패키지 `README.md`, `MAPS_AND_SOLUTIONS.md`, 최종 `maps/ET-XX.json`, `verification/ET-XX_trace.json`을 확인했다. `generation_input/`과 생성기는 사용하지 않았다.
- 최종 JSON 35개와 trace 35개는 `Assets/ANIMOL/Scripts/Editor/EchoTheaterV3Data`에 보관한다. 임포터는 `Assets/ANIMOL/Data/Puzzle/MapData/memory_theater`에 `ET-01_MapData.asset`~`ET-35_MapData.asset` 및 대응 `ET-XX_Scenario.asset`을 upsert한다. 같은 71개 에셋 메타의 재임포트 전후 해시가 모두 같다.
- `EchoTheaterMapSet.asset`은 ET-01~ET-35를 순서대로 참조한다. `ANIMOL_Echo_Theater.unity`가 빌드 씬에 등록되어 있고 로비의 잔영 극장 캠페인 버튼에서 진입한다. 씬의 스테이지 선택 UI는 35개를 로드하며 `ANIMOL.EchoTheater.Cleared.ET-XX` 키로 승리한 스테이지만 저장한다.
- 좌하단 (1,1) 원본 좌표를 유지하도록 네이티브 보드 크기에 0번 외곽을 포함했다. 바닥·내부 벽·빈 영역을 각각 원본대로 저장한다. 방울 3개+출구가 승리 조건이며 장치 목표와 코스트 제한은 추가하지 않았다.
- 주사위 40개/홈 40개는 24개 맵의 입력 데이터 그대로다. 주사위 면 배열은 `[위,아래,북,남,동,서]`로 저장하고 매 칸 굴림은 기존 여섯 면 회전 함수를 사용한다. `dice_socket`은 기존 `target` 판정과 분리했다. 윗면·마지막 진입 방향이 일치할 때만 고정·개방되며, 실패 입력은 상태와 코스트를 보존한다. 홈 판정에는 주사위 ID를 사용하지 않는다.
- 극장 V3 규칙은 `PuzzleScenario.echoTheaterV3Rules`가 켜진 맵에만 적용한다. 기존 프로토타입과 다른 테마의 판정은 유지한다. 도미노는 같은 그룹의 인접 조각만 저장된 방향으로 접고, 접힌 조각의 충돌과 화면 가림이 사라진다.

## 실제 Unity 검증

| 검증 | 결과 |
|---|---|
| 컴파일 | 오류 0 |
| 메뉴 `ANIMOL > Echo Theater V3 > 1 Import 35 Maps` | 35쌍 생성, 재실행 시 71개 메타 동일 |
| 메뉴 `2 Replay 35 Native Maps` | 실제 `PuzzleDeviceSession`에서 정답 35/35 승리 |
| 매 입력 trace | 플레이어의 모든 이동 칸, 방울 마스크, 승리, 주사위 위치·전체 6면·고정, 홈 개방, 도미노 마스크, 셔터·책장 위상, 이동 무대·인형 위치 일치 |
| 주사위 밀기 | 제공 정답의 주사위마다 가로·세로 사용 및 축 전환 최소 2회 확인 |
| EditMode `EchoTheaterV3Tests` | 5/5: 24자세·4회·역방향, ET-05 잘못된 숫자/방향 취소와 Undo/Reset, ET-03 연쇄 접기 Undo/Reset, 34번째 주사위/홈 독립 상태와 ID 비고정 |
| PlayMode `EchoTheaterV3PlayTests` | 3/3: 35맵 화면 바인딩 누락 0, 캠페인 씬·ET-35 선택·ET-01 클리어 저장, ET-03/05 장치 개방 |
| 기존 `PuzzleDeviceLogicTests` | 65/65: 기존 주사위 목표판·극장 프로토타입 포함 |
| 기존 `Starway20NativeTests` | 2/2 |

ET-05의 기본 면에서 R→D→R 윗면은 4→2→6이고 D→R→R의 최종 윗면은 5임을 EditMode에서 확인했다. 입력별 세션 비교 기록은 실행 시 `Logs/echo-theater-v3-replay.txt`에 생성된다.

## Unity 화면 확인

- ET-03: [접기 전](EchoTheaterV3QA/ET-03-before.png) · [접기 후](EchoTheaterV3QA/ET-03-after.png). 세 북향 도미노가 사라져 중심 (14,14)의 바닥 통로가 보인다.
- ET-05: [고정 전](EchoTheaterV3QA/ET-05-before.png) · [고정 후](EchoTheaterV3QA/ET-05-after.png). 주사위의 현재 위·북·동 숫자가 보이고 홈이 열린 6번 상태로 바뀐다.

## 남은 표현 차이

기존 V03 주사위 그림 24자세는 이번 V3 초기 배열과 북/남 방향 배치가 달라 정확한 면 숫자를 표시할 수 없었다. V3 맵에서는 판정 상태에서 위·북·동 숫자를 그리는 주사위 화면과 방향별 짧은 굴림 동작을 사용한다. 전용으로 그린 V3 여섯 면 스프라이트 시트는 패키지에 없으므로, 향후 제작되면 이 화면 표현을 교체할 수 있다. 판정과 저장은 화면 애니메이션에 의존하지 않는다.
