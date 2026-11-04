# 별길 목장 최단 입력 경로

## 원본과 실제 입장 연결

- 첫 맵: `Assets/ANIMOL/Data/Puzzle/MapData/starway/SW00_MapData.asset`
- 보존 SHA-256: `d70c951d63876b5b73b1ac13fa3ae7ac029aee991dd4dd5e24bff9267d414292`
- 보존 GUID: `fc12b62ee8aa3ae41a9cda34eaa35cd6`
- `.gitattributes`에서 SW00의 체크아웃 줄바꿈 변환을 끄고 원본 바이트를 보존한다. 보존 커밋은 `8e96b90c8`이다.
- 활성 경로: `NotebookUI.unity` → `NotebookCanvas.prefab` → `NotebookProjectGateway` → `NotebookMapDataPreviewBridge` → `PuzzlePrototypeController` / `PuzzleDeviceSession`.
- 화면상 `SW01`~`SW20`은 원본 `SW00`~`SW19`에 대응한다. Bridge의 스테이지 ID는 화면상 ID를 유지한다.
- 기존 `ANIMOL.Starway.Cleared.SWxx` 기록은 같은 원본 맵의 다음 화면상 스테이지로 읽는다. 새 클리어는 `ANIMOL.Starway.MapCleared.SWxx`에 원본 ID로 저장한다. 기존 키는 삭제하지 않는다.
- 원본 MapData와 작성된 `verificationRoute`는 경로 계산으로 수정하지 않는다. 결과는 `Assets/ANIMOL/Data/Puzzle/MapData/starway/StarwayShortestPaths.json`에 별도로 저장한다.

## 편집기 사용

`ANIMOL > 맵 제작기`에서 맵을 선택하면 설정 패널의 **별길 목장 최단 경로**에 순서, 최단 입력 수, 복사 가능한 경로와 탐색 상태 수가 표시된다. 개별 맵 또는 `SW00`~`SW19` 전체 탐색을 실행할 수 있다. Edit Mode의 저장된 맵에서만 계산한다.

탐색 상한을 조절해 더 큰 계산을 실행할 수 있다. 전체 탐색은 같은 원본·규칙에서 이미 확인한 상한 이하의 미확정 계산을 반복하지 않는다. 완료된 확정 기록도 재사용한다. 결과 JSON은 임시 파일을 완성한 뒤 교체해 이전 기록을 보존한다.

경로 문자는 `N=위`, `E=오른쪽`, `S=아래`, `W=왼쪽`이다. 연속 방향은 이동 애니메이션 중 다음 입력을 예약하는 순서다. `.`은 바람 자동 이동 한 번을 기다리는 것으로 입력 비용이 0이다. 거리에는 N/E/S/W 입력만 포함하며, 한 입력에서 발생하는 미끄러짐·장치 이동은 추가 입력으로 세지 않는다.

## 탐색과 검증

실제 `PuzzleDeviceSession`의 네 방향 이동을 모두 검사하는 0-1 BFS를 사용한다. 막힌 입력과 실패 상태는 버리고, 방향 입력은 비용 1, 선택적인 `AdvanceWind()`는 비용 0으로 확장한다. 런타임 `Load()`의 첫 바람 이동도 반영한다. 예약 입력이 `ContinueWind()`보다 먼저 처리되는 순서를 포함한다.

플레이어·물체·장치·방울·목표 방문·둥지·양·연못·풀 상태를 비교한다. 판정에 사용하지 않는 표시 방향은 비교에서 제외한다. 개별 목표나 연결이 없는 동일 금속 상자는 위치 순서만 다른 중복 상태를 합친다. 가장 낮은 입력 비용으로 꺼낸 클리어 상태만 최단 경로로 확정한다. 휴리스틱이나 수동 추정은 사용하지 않는다.

저장 전 실제 공개 `Apply()` / `AdvanceWind()` API로 전체 경로를 다시 실행하여 클리어와 비용을 확인한다. 원본 SHA-256과 탐색 규칙 버전이 다르거나 미저장 편집이 있으면 이전 결과를 현재 최단 경로로 표시하지 않는다.

`limit`은 탐색 상한까지 답이 확정되지 않았다는 뜻이다. `unreachable`은 도달 가능한 전체 상태를 모두 검사한 경우에만 기록한다. 취소·상한 도달을 클리어나 도달 불가로 취급하지 않는다.

## 실행한 Unity 검증

- Unity 6000.3.8f1에서 Import / C# 컴파일.
- `PuzzleShortestPathTests` 8개 통과: 이동 장애물 우회, 최초 무료 바람 이동, 예약 입력 우선 처리, 완료 콜백 1회와 오래된 업데이트 무시, 상태 중복 처리, 특정 상자 목표 구분, 현재 기록 경로 재생.
- `NotebookIntegrationTests.StarwayStageOrderPreservesSourceMapProgress`.
- 실제 활성 Scene의 Play Mode 및 `NotebookUiPlayTests.ClearedStarwayReplayAndEntryUseRealAssetsWithoutReplayCharges`: 첫 스테이지가 SW00을 열고, 다시보기·지도 복귀·재화 정책 유지.
- `ArtAssetTests` 4개: 활성 Build Scene 참조와 기존 아트 동작.
- 맵 편집기 창의 실제 API 연결과 컴파일을 확인했다. Unity 창의 시각 캡처는 확보하지 못했으므로 화면의 직접 시각 검토는 완료로 보고하지 않는다.

기존 `verificationRoute`는 자동 바람 이동을 포함해 재생해도 현재 20개 맵에서 클리어까지 이어지지 않았다. 이 작성 기록을 덮어쓰거나 최단 경로로 재사용하지 않았다.

## 계산 결과

<!-- RESULTS_START -->
현재 계산 중이다. 최신 상태와 원본 해시는 JSON 기록 및 맵 편집기에서 확인한다.
<!-- RESULTS_END -->
