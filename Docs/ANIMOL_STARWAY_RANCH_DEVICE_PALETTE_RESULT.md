# 별길 목장 V13 테마 장치 목록 연결 결과

## 적용

- `PuzzleMapEditor`의 별길 목장 **테마 장치 목록** 맨 앞에 별양, 가로 수문, 세로 수문, 키 큰 풀, 병아리 둥지, 물칸을 추가했다. 기존 별돌·상자·바람개비·벨트 등 장치 항목은 같은 목록에 이어서 표시한다.
- 별양·수문·풀은 기존 `ranchV13Art` 배치와 `PuzzleDeviceSession` 판정에 연결한다. 둥지는 선택한 병아리의 `target` 좌표, 물칸은 기존 `scenario.water`에 저장한다. 바람 범위는 바람개비 판정 결과로 표시하므로 별도 배치 항목을 만들지 않았다.
- 둥지는 V13 `nest/idle`·`nest/settled`, 고정 물칸은 V13 물 셀 표현을 사용한다. 물 셀 표현은 카메라에 보이는 칸만 생성한다. 수문의 두 칸과 고정 물칸 표현은 겹치지 않는다.
- 병아리의 이동·길막기·둥지 귀환은 V12 판정을 유지하고 병아리 본체의 V12 그림도 유지한다. 일방향 문도 기존 V12 표현을 유지한다. 이번 작업에서 캠페인 맵 배치 데이터는 변경하지 않았다.

## 실제 검증

- Unity 6000.3.8f1 Editor 재컴파일: 오류 0.
- Unity 편집기 `StarwayRanchVerticalQA`를 열고 테마 장치 목록에서 위 여섯 항목의 그림과 이름이 보이는 것을 확인했다.
- EditMode `ThemeDeviceListIncludesEveryPlaceableRanchFamilyWithCatalogArt`, `RanchPaletteNestAndWaterUseExistingScenarioRulesAndV13Visuals`: 각각 통과.
- PlayMode `RanchR2PlayTests`: 9/9 통과. 물칸 재로드, 둥지 정착, 수문/양 기존 동작 포함.
- `PuzzleMapTests` 전체: 56/57 통과. 별도 `AllValidationMapsHaveValidPlacementsAndCompleteTheme`는 작업 전부터 있던 미추적 `PuzzleMapf`의 필수 목표 범위 오류로 실패했다. 이 맵은 변경하지 않았다.
- `StarwayRanchV12PlayTests`: 5/6 통과. `CommittedWindUsesSameRouteAndRetainsStaticRangeOnSettle`은 V12 바람 범위 렌더러 개수 기대값 1에 대해 실제 0으로 실패했다. 이번 변경 대상인 목록·둥지·물 표현과는 별도 경로이며 원인은 추가 조사 대상이다.

## 연결 경로

- 목록·배치·Undo: `Assets/ANIMOL/Scripts/Editor/PuzzleMapEditor.cs`
- V13 목록 그림 및 병아리 V12 예외: `Assets/ANIMOL/Scripts/Editor/RanchDevicesV13EditorPreview.cs`
- 둥지·물칸 PlayMode 표현: `Assets/ANIMOL/Scripts/Runtime/PuzzleMapView.cs`
- 검증: `Assets/ANIMOL/Scripts/Tests/Editor/PuzzleMapTests.cs`, `Assets/ANIMOL/Scripts/Tests/PlayMode/RanchR2PlayTests.cs`
