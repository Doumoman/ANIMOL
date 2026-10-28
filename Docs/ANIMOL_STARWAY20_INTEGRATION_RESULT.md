# 별길 목장 v7 Unity 적용 결과

2026-10-28 · Unity 6000.3.8f1 · 입력: `MCP_INBOX/ANIMOL_Starway_Unity_WindAndStars_v7.zip`

## 적용

- README와 `Reference/StarwayDesignPack/SOURCE_DEVICE_RULES.md`, 실제 `PuzzleMap`·`PuzzleScenario`·`PuzzleDeviceSession`, 카메라와 에셋 등록 코드를 확인했다. 패키지 소스 20개를 `Assets/ANIMOL/StarwayMapPack`에 갱신했다.
- 정본 맵은 `Assets/ANIMOL/Data/Puzzle/MapData/starway/SW##_MapData.asset`, 시나리오는 같은 폴더의 `SW##_Scenario.asset`이다. 맵·시나리오 20쌍의 메타 GUID가 이동 전과 바이트 단위로 같다. `Starway20MapSet.asset`은 SW01~SW20을 순서대로 한 번씩 참조한다.
- `MapData` 아래에 `memory_theater`, `letter_observatory`, `twilight_sea` 폴더도 마련했다. 해당 테마의 기존 프로토타입 맵은 생성기 및 미커밋 사용자 변경과 연결되어 있어 이번 이동에서 제외했다.
- 지형은 region 안의 walkable=바닥, blocked=벽이며 region 밖은 통행 불가다. 시작·출구·방울 3개, 별돌·홈, 병아리·둥지, 양·풀, 수문, 바람 장치와 실제 `visualPrefab`을 저장했다. `themeId=starway`, `goals=[]`, `costLimit=0`이다. 출구는 처음부터 표시하고 승리는 방울 3개와 출구 진입으로 판정한다.
- 기존 `PuzzleFollowCamera`의 7×11 시야, UI 안전 영역, 지형 AABB 제한을 사용한다. 레거시 1-1~1-21은 `LegacyStarwayV3`에 보관하고 저장 ID는 변경하지 않았다.

## 실제 Unity 검증

| 항목 | 결과 |
|---|---|
| 컴파일 | 오류 0 |
| 메뉴 1 Import, 2 Validate, 3 Preview, 6 Source and Camera Checks | 통과 |
| 메뉴 4 Build Native Maps, 5 Validate Native Maps | 통과; 이동 후 재실행도 통과 |
| `PuzzleDeviceSession` UnityReplay/Solutions 재생 | 20/20 승리, 방울 3개·출구, 매 입력 경로·상태 비교, 첫 입력 Undo·최종 Reset |
| 별돌 | 12개 모두 실제 밀기 순서 확인, 각 3회 이상·2방향 이상. 시작 칸은 대응 홈과 x·y 모두 다름 |
| 홈 통행 | 실제 세션 분기에서 빈 홈 진입 거절, 활성 홈 진입 허용을 홈마다 확인 |
| 별돌 없는 대체 해법 | 입력에 경로가 있는 맵은 실제 세션에서 방울 3개·출구 승리 확인. 별돌 맵은 각 홈을 막은 바닥 연결 검사에서 필수 방울 또는 출구가 단절됨 |
| 바람 | 8회 실제 탑승 경로의 모든 칸이 설계 audit와 일치. 세기=phase×3, 반사 확인. SW10·SW18은 각각 2회 탑승 |
| 병아리·양·수문 | 매 입력의 병아리 순서별 위치·trail, 양 위치, 풀 소모, 수문 물 위상 비교. 이동 이벤트 중 병아리 상호 및 플레이어 겹침 없음 |
| EditMode `Starway20NativeTests` | 2/2 |
| PlayMode `Starway20PlayTests` | 1/1, 20개 로드·HUD·카메라 확인 |

### 별돌별 실제 밀기 방향

| 맵 | 별돌별 순서 |
|---|---|
| SW04 | #1 N,E,E |
| SW07 | #1 N,E,E |
| SW08 | #1 S,S,E,E |
| SW09 | #1 W,S,S |
| SW11 | #1 W,S,S |
| SW12 | #1 S,W,W · #2 E,S,S |
| SW16 | #1 E,E,S · #2 W,W,S,S |
| SW20 | #1 S,E,S,E,E · #2 E,E,S,S · #3 E,N,N |

### 바람 탑승

| 맵·입력 | 출발 → 착지 | 세기 | 반사 | 제동판 착지 |
|---|---|---:|---|---|
| SW06·30 | (5,3) → (9,3) | 6 | 아니오 | 예 |
| SW10·25 | (14,6) → (14,8) | 3 | 아니오 | 아니오 |
| SW10·41 | (14,6) → (14,13) | 9 | 아니오 | 예 |
| SW14·48 | (4,3) → (10,5) | 9 | 예 | 아니오 |
| SW15·47 | (5,14) → (4,16) | 6 | 예 | 예 |
| SW18·65 | (13,19) → (11,19) | 3 | 아니오 | 아니오 |
| SW18·72 | (13,19) → (8,16) | 9 | 예 | 예 |
| SW20·77 | (5,3) → (10,6) | 9 | 예 | 예 |

세부 이벤트 기록은 `Logs/starway20-replay-result.txt`에 생성된다. 위 기록은 Unity 세션 실행 결과를 설계 자료와 대조한 것이다.

## 남은 확인 범위

- 별돌 맵의 모든 가능한 행동 경로를 완전 탐색한 것은 아니다. 필수 경로 검사는 지형 연결성 기준이다.
- 로비의 SW 진행·저장 서비스와 광고 힌트 SDK 연결은 현재 없다. `Starway20MapSet`과 에디터 선택 창까지만 연결했고 기존 저장 키는 유지했다.
