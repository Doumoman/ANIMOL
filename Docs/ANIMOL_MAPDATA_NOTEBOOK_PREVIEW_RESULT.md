# MapData 맵 에디터 미리보기와 인게임 UI 연결 (2026-11-04)

## 원인과 변경

- 맵 에디터의 `플레이 미리보기`는 `PuzzlePreview.Play`로 빈 임시 씬을 만든 뒤 구형 `hud_visual.prefab`을 붙이고 있었다. 이전 Notebook GameplayCanvas 적용은 잔영 극장 캠페인 씬에만 연결돼 있어 맵 에디터 미리보기에는 나타나지 않았다.
- 임시 씬 생성 시 선택된 `PuzzleMap` 객체가 언로드되어 `AssetDatabase.GetAssetPath`가 빈 값이 되는 것도 확인했다. 씬을 바꾸기 전 정본 `.asset` 경로를 보관하고, 현재 편집 중인 선택 맵과 연결 시나리오가 dirty 상태일 때 그 두 에셋만 저장한 다음 새 씬에서 동일 에셋을 다시 로드하도록 수정했다.
- `Assets/ANIMOL/Data/Puzzle/MapData/` 아래 맵을 실행하면 `GameplayCanvas.prefab`과 `CollectibleStarVisual.prefab`을 사용한다. 구형 HUD는 이 경로에 중복 생성하지 않는다. 다른 위치의 작업용 맵은 기존 미리보기 방식을 유지한다. 맵 에셋·시나리오·판정 데이터는 수정하지 않았다.
- `NotebookMapDataPreviewBridge`는 실제 `PuzzleDeviceSession`에서 코스트, 목표 별, 수집, 승리 상태를 읽어 HUD에 반영한다. Undo/Reset에서 값이 줄면 HUD를 실제 세션 상태로 다시 초기화한다. 같은 `PuzzleMapView`의 별 시각 자식은 최초 한 번만 교체한다.
- 일시정지는 기존 `PuzzleSwipeInput`과 `PuzzlePrototypeController` 입력 경계에 연결했다. 미리보기의 지도 버튼은 Play Mode를 종료하고 맵 제작 씬을 복원한다. 재시작 승인 원장이 없으므로 재시작 버튼은 비활성 상태다. 클리어 패널은 실제 세션의 승리와 출구 도달이 확인된 경우에만 열린다.

## 검증

- Unity 6000.3.8f1에서 `MapData`의 `PuzzleMap` 56개를 조회했다. 모두 시나리오 맵이며 `ValidateMap()` 오류 0건, 목표 별 분포는 1개 5맵·2개 7맵·3개 44맵이다.
- Unity C# 재컴파일 완료, 오류 0건.
- 맵 에디터가 쓰는 동일한 `PuzzlePreview.Play` 경로를 실제 Play Mode에서 실행했다. `SW01_MapData`는 새 HUD 1개/구형 HUD 0개, 목표 별 3개, 실제 이동 후 세션 코스트 0→1과 HUD 잔여 코스트 52→51을 확인했다. 일시정지 중 이동 요청은 거절됐고 재개 시 복원됐다. Reset 후 코스트가 52로 돌아오며 별 시각 자식 수는 3개로 유지됐다.
- `ET-01_MapData`는 목표 별 2개, `SW12_MapData`는 목표 별 1개와 `StarDock_1_0` 및 월드 별 1개를 확인했다. 390×844 세로 GameView에서 SW01과 ET-01의 실제 화면을 확인했다.
- 미리보기의 지도 버튼으로 Play Mode를 종료했을 때 원래 `NotebookUI.unity` 씬이 수정 표시 없이 복원됐다. 세 맵에서 실제 퍼즐 승리까지 진행하지 않았으므로 클리어 팝업의 실승리 화면은 이번 검증 결과로 기록하지 않는다.
