# ANIMOL 새 맵 제작 요청문

> 2026-11-10 정본 갱신: 아래 25개 맵 분석과 기존 LLM ZIP/JSON은 이전 배치의 참고 자료다. 현재 SW03/SW05 이름 이동·새 SW05 배치·ET-11까지의 현존 맵은 [최신 배치 보고서](ANIMOL_MAP_LAYOUT_REPORT.md), [별길 목장 최단 경로](STARWAY_SHORTEST_PATHS.md), [잔영 극장 최단 경로](ECHO_THEATER_SHORTEST_PATHS.md)를 우선한다. 가중치는 제작 제안이며 실제 입력 비용을 바꾸지 않는다.

아래 요청문을 복사하고 첫 줄의 제작 조건을 바꿔 사용한다. 함께 전달할 파일은 `ANIMOL_LLM_MAP_DESIGN_GUIDE.md`, `ANIMOL_LLM_MAP_REFERENCE.json`, `ANIMOL_MAP_DESIGN_ATLAS.md`, `ANIMOL_DEVICE_RULES_CURRENT.md`다. 자료 묶음에는 원래 배치도와 최단 경로 보고서도 들어 있다.

---

ANIMOL에 사용할 새 퍼즐 맵 **1개**를 설계해줘. 기본 제작 조건은 **별길 목장, 18×20 격자, 목표 최단 입력 35~50, 핵심 패턴 P04 양 자원 배정, 보조 패턴 P05 병아리 지연 추종**이다. 이 조건을 별도로 지정했다면 새 조건을 우선해.

첨부 가이드의 현재 동작과 맵 데이터를 기준으로 설계하고, 기존 맵은 참고 자료로만 사용해. 맵 제목이나 아트만 보고 판정을 추측하지 마. 설명은 한국어로, 판정용 필드와 역할 이름은 현재 데이터 이름으로 적어줘.

다음 조건을 지켜줘.

1. 좌표는 왼쪽 아래 `(0,0)`, 오른쪽 +x, 위 +y다. 유효 방향 입력 한 번의 게임 비용은 1이다. 상자·양·별돌 밀기로 플레이어가 제자리에 있어도 1이다. 바람 자동 이동은 0이며 `.`로 기록할 수 있다. 벽에 부딪히는 입력은 턴을 진행하지 않는다.
2. 양은 풀을 먹으면 사라지고, 병아리는 플레이어가 떠난 칸의 지연 이력을 따른다. 양 밀기 같은 제자리 행동을 병아리의 일반 이동으로 처리하지 마. 수문·바람·주사위·카트·석상은 첨부된 현재 규칙을 그대로 써.
3. 핵심 패턴 하나와 보조 패턴 하나가 서로의 결과에 영향을 주게 배치해. 별을 주워 출구로 가는 길에 핵심 판단이 필요해야 한다. 선택 별이 있으면 별 배치 수와 목표 수를 구분해.
4. 핵심 장치가 반드시 쓰여야 한다면 ‘양이 특정 풀을 먹음’처럼 검사할 사건을 명시해. 해당 사건이 없는 승리 경로의 검사를 요청해. 설치된 장치 수나 작성한 후보 경로만으로 필수 사용을 주장하지 마.
5. 실제 비용 L과 설계 가중합 W를 분리해. 가이드의 기본 가중치와 수정치 T/H/R/C를 사용하고, 수정치마다 입력 번호와 이유를 적어. 자동 반응하는 장치 수만큼 매 턴 점수를 더하지 마.
6. 새 맵 ID는 `NEW_SW_A` 또는 `NEW_ET_A`처럼 기존 원본과 구별해. SW00~19와 ET 원본, 메타/GUID를 덮어쓰지 마. UI·진행·재화·물리 시스템의 변경을 요구하지 마.
7. 실제 판정이나 탐색기를 실행할 수 없다면 후보 정답과 추정 입력 수만 제시하고, `candidate`/`unverified` 상태로 남겨. 실행하지 않은 검증을 통과했다고 쓰거나 최단성을 확정하지 마.

다음 순서로 결과를 작성해줘.

**1. 설계 의도:** 어떤 규칙을 배우고 어떤 상태를 기억해야 하는지, 핵심 결정 2~4개와 참고 원본 맵 ID를 설명해.

**2. 전체 배치 JSON:** 아래 필드를 빠짐없이 채워 `ANIMOL_MapProposal_v1` 규약의 제안 데이터를 만들어. 좌표는 모두 `{"x": 정수, "y": 정수}` 형식으로 통일하고, 빈 목록도 생략하지 마. 이 규약은 Unity 자동 가져오기 형식이 아니라 구현 검토용 명세야.

| 필드 | 형식과 내용 |
|---|---|
| format / proposalId / themeId | `ANIMOL_MapProposal_v1` / 새 ID / `starway` 또는 `memory_theater` |
| width / height | 양의 정수, 모든 좌표가 이 범위 안에 있어야 함 |
| start / exit | 시작·출구 좌표 |
| terrain | `{position, blocked, material}` 목록. 통행 가능한 바닥을 전부 적고 지형 없는 칸과 구분 |
| bubbles / targetBubbles | 별 좌표 목록 / 필요한 수집 수 |
| decorations | `{id, position, occupiedCells}` 목록. 사용할 아트는 논리 이름으로 표시하고 GUID를 만들지 않기 |
| devices | `{id, role, position, direction, variant, group, target, orientation, number, active, cells, initialDiceFaces, socketReward, socketBridgeId, socketBubbleCell}` 목록 |
| ranchV13Art | `{family, instanceId, position, direction, hasPondB, pondB, rightState}` 목록. family는 `sheep`, `sluice`, `tall_grass` |
| nests / railPaths / water | 둥지 좌표 / `{id,cells}`의 순서 있는 레일 / 물 좌표 목록 |
| goals | `{kind,deviceId,cell,number}` 목록. 지원하는 Active/Removed/Position/DiceTop/Visited 조건만 사용 |
| policies | 첨부 참고 데이터의 해당 테마 정책값. echoTheaterV3Rules와 셔터·다리·석상 등의 기본 정책을 명시 |
| intendedRequiredEvents | 필수 행동을 식별할 장치 ID, 사건과 목적 |
| candidateSolution | `{route, claimedInputCount, status:"candidate"}`. NESW 및 무료 바람 `.` 표기 |
| validation | `{status:"unverified", requestedChecks:[...]}`. 실제 실행했다면 도구·버전·결과를 명시 |

장치 역할은 현재 enum 이름을 사용해. 별길 목장의 현재 참고 범위는 StarSocket, MetalCrate, Chick, OneWayGate, Wind, Bounce, Conveyor와 양·수문·풀이다. 잔영 극장은 Dice, Domino, Glass, Shutter, MovingStage, CowardDoll, Statue가 중심이다. 다른 장치를 쓰려면 동봉 규칙에서 판정을 확인하고 추가 검증을 요구해. 데이터의 모든 기본 필드는 `ANIMOL_LLM_MAP_REFERENCE.json`의 같은 테마 예시와 대조해.

**3. 읽기 쉬운 배치도와 범례:** JSON과 같은 좌표를 사용해. 겹치는 장치·별·바닥·장식 점유를 빠뜨리지 말고 각 장치 ID의 연결과 작동 방향을 설명해.

**4. 후보 정답과 행동 원장:** 전체 입력열, 입력별 비용, 플레이어 위치, 직접 조작, 수집 변화, 바뀐 핵심 상태를 표로 적어. 무료 바람은 별도 행으로 표시해. W0와 중요 입력의 T/H/R/C 수정치, 최종 W를 제시해.

**5. 실패 예시와 검증 계획:** 잘못된 선택 최소 2개, 그 결과, Undo/재시작 또는 다른 경로로 복구하는 방법을 설명해. 핵심 장치 미사용 해답, 자원 배정, 대체 수집 경로, 입력 예약·자동 이동, 주사위 면·카트 P0 복귀를 관련 범위에서 검사하도록 요청해. 재생 확인과 최단성 확정을 구분해.

초안의 해답을 쓰기 전에 JSON의 배치와 규칙으로 끝까지 따라가 봐. 모순이 있으면 초안 안에서 고친 뒤 하나의 완성된 제안만 반환해.
