# 별길 목장 SW01~SW20 Unity 적용 결과

2026-10-28, Unity 6000.3.8f1. 입력은 `MCP_INBOX/ANIMOL_Starway_Unity_Integration_v1.zip`이다. 패키지의 `README_KO.md`, `Editor/Data/SOURCE_DEVICE_RULES.md`, `Documentation/ProjectAdapter.example.cs.txt`를 읽고 현재 `PuzzleMap`, `PuzzleScenario`, `PuzzleDeviceSession`, `PuzzleFollowCamera`, `StarwayV3Importer`와 맵 선택 데이터를 대조했다.

## 적용 범위

- 패키지의 `Assets/ANIMOL/StarwayMapPack`을 복사했다. 에디터 전용 설계 소스 20개와 `Assets/ANIMOL/Data/Puzzle/Starway20/SW01~SW20.asset`, 대응 시나리오 20개를 생성했다. `Starway20MapSet.asset`은 `SW01`부터 `SW20`까지 한 번씩 순서대로 참조한다.
- 프로젝트 어댑터는 기존 `StarwayV3Importer.FindVisualTemplate`을 공유해 실제 목장 visual prefab을 사용한다. `regionCells`만 지형으로 저장하고, 그 안에서 `walkableCells`는 잔디 바닥, `blockedCells`는 돌벽이다. `regionCells` 바깥은 지형이 없어 통행할 수 없다. 시작/출구/방울 3개, 장치, 양·풀, 수문 A/B와 `wetA`, 병아리 둥지 좌표를 원본에서 변환한다. `themeId=starway`, `goals=[]`, `costLimit=0`, `targetBubbles=3`이다.
- 기존 `PuzzleFollowCamera`가 SW 맵에서 패키지 `StarwayCameraMath`를 사용한다. 안전 영역에서 HUD 상단/하단 여백을 제외한 사각형 안에 7×11 시야를 맞추고 `regionCells`의 AABB로 중심을 제한한다. 진입 시 `Cut()`으로 시작 칸에 즉시 맞추며 기존 이동 추적을 유지한다.
- Unity `AssetDatabase.MoveAsset`으로 기존 추적 중인 `1-2~1-21` 맵 20개와 시나리오 20개를 `Assets/ANIMOL/Data/Puzzle/LegacyStarwayV3`로 옮겨 GUID를 보존했다. 기존 V3 맵 집합과 그 검증 코드는 레거시 에셋을 계속 참조한다. 새 SW 맵은 `ANIMOL > Starway 20 > Play Canonical Maps`에서 선택해 플레이한다. 기존 생성기의 에셋 작성 관례와 프리팹 탐색을 계승했다.

## 실행과 확인

| 항목 | 결과 |
|---|---|
| Unity 컴파일 | 통과, 오류 0 |
| `ANIMOL > Starway 20` 메뉴 1·2·3 | 소스 import, 소스 검증, 7×11 미리보기 창 호출 통과 |
| 메뉴 4·5 | 20쌍 네이티브 에셋 생성·readback 통과 |
| 메뉴 4·5 재실행 | SW01 map GUID 유지, 중복 생성 없음 |
| 실제 `PuzzleDeviceSession` 재생 | `UnityReplay/SW*.json`과 `Solutions/SW*.json` 입력 20/20 완주. 각 입력의 플레이어 경로, 방울 마스크, 병아리 위치·귀환·추종 기록, 양·풀, 수문 물 위상, 바람 단계, 별 홈 상태, 코스트와 최종 출구 승리를 비교. 첫 행동 Undo 및 각 맵 Reset 확인 |
| `Starway20NativeTests` EditMode | 2/2 통과 |
| `Starway20PlayTests` PlayMode | 1/1 통과. 20맵 로드, 장치 바인딩 누락 0, 시작 `0/3` HUD, 카메라 `orthographicSize=5.5` 확인 |
| 기존 V3 회귀 | EditMode 1/1, PlayMode 2/2 통과 |
| `PuzzleDeviceLogicTests` | 65/65 통과 |
| `PuzzleMapTests` | 57/58 통과. 미추적 `Assets/ANIMOL/Data/Puzzle/Maps/PuzzleMapf.asset`의 필수 목표 장치가 맵 경계 밖인 기존 데이터 오류 1건. 이번 SW 맵과 무관하며 수정하지 않음 |

## 남은 차이와 경계

- 프로젝트의 로비에는 현재 실제 캠페인 스테이지 목록·진행 저장 서비스가 없고, `MainMenuCatalog.requiredStageIds`는 대전 잠금용 빈 목록이다. 이번 등록은 검증 가능한 `Starway20MapSet`과 에디터 선택 창이다. 플레이어용 캠페인 진행·저장 UI의 SW ID 교체는 아직 연결되지 않았다. 기존 저장 키를 임의로 새 키로 덮지 않았다.
- 기존 미추적 `1-1.asset`, `1-1_Scenario.asset`, 별도 `1-2_Scenario.asset`은 Unity의 GUID 보존 이동으로 같은 레거시 폴더에 옮겼다. 이 세 파일은 사용자 미커밋 작업으로 남기며 이번 커밋에 포함하지 않는다. 다른 미추적 시험 맵은 건드리지 않았다.
- 패키지의 `verificationScope=REPORT_MODEL_ONLY`는 설계 자료 자체의 표기다. 위 20/20은 현재 Unity `PuzzleDeviceSession`을 실제 실행해 확인한 결과다. 이 결과는 정답 경로 검증이며 모든 오답 가지나 출시 난도·실기기 화면 검증을 뜻하지 않는다.
- 광고 힌트 연동에 사용할 SDK나 기존 캠페인 힌트 연결은 확인되지 않았다. 강제 광고와 실패 직후 자동 재시작은 추가하지 않았다.
