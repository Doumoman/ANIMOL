# 잔영 극장 V3 Unity 적용 결과

2026-10-29 · Unity 6000.3.8f1 · 입력: `MCP_INBOX/ANIMOL_Echo_Theater_35_Package.zip`

## 적용 데이터와 경로

- 패키지 `README.md`, `MAPS_AND_SOLUTIONS.md`, 최종 `maps/ET-XX.json`, `verification/ET-XX_trace.json`을 확인했다. `generation_input/`과 생성기는 사용하지 않았다.
- 최종 JSON 35개와 trace 35개는 `Assets/ANIMOL/Scripts/Editor/EchoTheaterV3Data`에 보관한다. 임포터는 `Assets/ANIMOL/Data/Puzzle/MapData/memory_theater`에 `ET-01_MapData.asset`~`ET-35_MapData.asset` 및 대응 `ET-XX_Scenario.asset`을 upsert한다. 같은 71개 에셋 메타의 재임포트 전후 해시가 모두 같다.
- `EchoTheaterMapSet.asset`은 ET-01~ET-35를 순서대로 참조한다. `ANIMOL_Echo_Theater.unity`가 빌드 씬에 등록되어 있고 로비의 잔영 극장 캠페인 버튼에서 진입한다. 씬의 스테이지 선택 UI는 35개를 로드하며 `ANIMOL.EchoTheater.Cleared.ET-XX` 키로 승리한 스테이지만 저장한다.
- V3 coordinates remain bottom-left 1-based. Native terrain contains exactly the JSON floor cells; interior_walls and all other absent cells have no terrain and remain impassable. Victory remains three bubbles plus the exit, with no added cost limit.
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
| EditMode `EchoTheaterV3Tests` | 6/6, including all 35 floor and interior-hole comparisons, 35/35 solution replays and dice/domino regression checks |
| PlayMode `EchoTheaterV3PlayTests` | 3/3: 35맵 화면 바인딩 누락 0, 캠페인 씬·ET-35 선택·ET-01 클리어 저장, ET-03/05 장치 개방 |
| 기존 `PuzzleDeviceLogicTests` | 65/65: 기존 주사위 목표판·극장 프로토타입 포함 |
| 기존 `Starway20NativeTests` | 2/2 |

ET-05의 기본 면에서 R→D→R 윗면은 4→2→6이고 D→R→R의 최종 윗면은 5임을 EditMode에서 확인했다. 입력별 세션 비교 기록은 실행 시 `Logs/echo-theater-v3-replay.txt`에 생성된다.

## Unity 화면 확인

- ET-03: [접기 전](EchoTheaterV3QA/ET-03-before.png) · [접기 후](EchoTheaterV3QA/ET-03-after.png). 세 북향 도미노가 사라져 중심 (14,14)의 바닥 통로가 보인다.
- ET-05: [before](EchoTheaterV3QA/ET-05-before.png) · [after](EchoTheaterV3QA/ET-05-after.png). The original V03 24-orientation dice sprite and covered roll frames now follow the committed V3 six-face state; the socket opens on insertion.


## V3 art mapping

V3 swaps the north/south face labels relative to the older V03 art orientation table. The view converts that complete six-face state only for V03 sprite and roll-clip lookup; gameplay continues to use the authoritative V3 faces. No generated square or text dice is used. The interior wall cells are omitted from native terrain, so the existing theater renderer shows them as holes.

## V3 floating-decoration cleanup

All 35 V3 stages omit Curtain lintel, Left curtain, Right curtain, Static footlights, Left seats, Right seats, and Organ from the map artwork. The background rig omits only velvet_swag_lower_right. Source art and layouts remain available for other theater content. Unity 6000.3.8f1 compiled with zero errors; PlayMode EchoTheaterV3PlayTests passed 3/3, including an active-hierarchy check of all 35 stages. ET-03 and ET-05 Unity captures were refreshed.
