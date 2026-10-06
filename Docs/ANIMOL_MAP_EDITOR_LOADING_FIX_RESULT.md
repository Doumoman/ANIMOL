# ANIMOL 맵 에디터 배치 후 반복 로딩 수정

2026-10-07 / 실제 대상 `C:/Users/user/Documents/GitHub/ANIMOL` / Unity `6000.3.8f1`.

## 확인한 원인과 변경

장애물 한 개의 변경에도 기존 편집 경로가 `StageMapBackupService`를 통해 `Assets/ANIMOL/MapBackups`에 JSON을 쓰고 `AssetDatabase.ImportAsset(...ForceUpdate)`를 실행했다. 범용 객체 작업과 에디터 adapter가 Undo·저장을 중복 처리했다. Scene 프리뷰의 정적 서명에는 모든 장애물이 들어 있어, 배치·이동·삭제·설정 변경마다 전체 프리뷰를 새로 만들었다.

`TerrainEditorObjectAdapter`가 기존 `StageMapDefinition` 변경 API와 Validate/Undo/실패 복구를 소유하는 단일 트랜잭션을 사용하도록 정리했다. 백업은 이미 있는 `CampaignMapBackupStore`로 `UserSettings/ANIMOL/MapBackups`에 보관한다. 변경 프리뷰를 먼저 검증하고 성공한 맵만 한 번 저장한다. 실패 시 메모리·revision·디스크·프리뷰를 복구한다. 마커와 현재 C01~C10을 같은 경로로 처리하며, 현재 카탈로그에 없는 레거시 pair 작업은 거부한다.

Scene의 정적 그림 서명에는 지형과 캠페인 마커만 포함한다. 장애물 변경은 동일한 `FreeShapeTerrainRenderer`와 Tilemap/Sprite 캐시를 유지하면서 기존 전역 점유 기반 갱신을 사용한다. 지형이 없는 맵에 첫 장애물을 놓아도 프리뷰를 추가하며, 이동·삭제·Undo/Redo 후 선택 상태를 갱신한다. hover 검사에서 전체 ScriptableObject 복제와 불필요한 지형 DTO 생성을 제거했다.

`TerrainEditorArt.ObstaclePreview`는 지형 인덱스를 한 번 만들고 장애물을 추가할 때 영향받는 3×3의 장치만 검사한다. 이전의 유효한 장치를 순서대로 유지하고 막힌 장치를 배제하는 규칙은 그대로이며, 커밋 전 전체 후보 검증은 유지한다. 그래픽 오류에 레거시 아트로 fallback하지 않는다.

위 변경 후에도 실제 배치에 약 2초가 걸리는 원인을 추가로 추적했다. `NamedArtAutomation`이 맵 저장/임포트마다 24,233,753바이트의 `ProjectSettings/ANIMOLNamedArtReferences.json`을 다시 읽고 자동 인덱싱을 예약했다. `StageMapDefinition`은 직접 Sprite/Texture 슬롯이 아닌 styleId와 게임 데이터/물리 prefab 소유자를 저장한다. 파일명이나 폴더 대신 실제 main asset 타입을 확인해 이 데이터만 자동 아트 인덱싱에서 제외한다. Sprite 원본·아트 Registry·prefab·Scene의 기존 인덱싱은 유지한다. 실제 새 Sprite의 자동 인덱싱 테스트도 통과했다.

## 실제 실행 결과

| 검사 | 결과 |
|---|---|
| Unity 컴파일 | 완료, compilationFailed=false, compiling=false |
| TerrainEditorV4Tests | 최종 16/16 PASS, 17.14초 |
| ObstacleGraphicsTests | 12/12 PASS: 20스타일·raw256·4위상, 접합, dirty 갱신, hash/Undo/rollback 포함 |
| NamedArtReferenceTests.GameplayMapImportsAndSavesDoNotParseOrQueueTheArtReferenceIndex | 1/1 PASS: 잘못된 JSON조차 맵 저장/임포트에서 읽지 않고 예약도 하지 않음 |
| NamedArtReferenceTests.NewImportedSpritesAreAutomaticallyIndexedForFutureReplacement | 1/1 PASS: 실제 Sprite 자동 인덱싱 보존 |
| 사용자 기존 변경 보존 | 시작 당시 dirty/untracked 파일 1,727개의 SHA256 비교에서 변경 0개 |

총 30개 고유 테스트가 통과했다. 자동 백업이 Assets에 생성되지 않는지, 빈 지형 맵 첫 배치, 배치·이동·삭제 동안 Scene 프리뷰 root/renderer 동일성, 주변 9/12셀 갱신, 저장과 Undo/Redo, 프리뷰 실패 시 메모리·파일·revision 복구를 실제 Editor 테스트로 확인했다.

별도의 폐기 가능한 맵(지형 512셀, 초기 C01 40개, T01_A)에서 측정했다. 기존 프리뷰 계산의 세 번 측정은 7.9701/16.8528/8.9452ms이고, 개선 후는 0.2897/0.2770/0.3152ms다. 중앙값 8.9452→0.2897ms는 **프리뷰 계산만**의 결과다.

로컬 백업/Scene 최적화 후에도 남아 있던 전역 아트 인덱싱을 포함한 배치·검증·백업·Undo·프리뷰·자동 저장 시간은 1899.9492/2152.3631/2154.2114ms였다. 아트 인덱싱 제외 후 같은 fixture의 전체 배치는 43.4232/44.1439/50.3933ms이며, 각 배치의 dirty 셀은 9개다. 이 전후 비교의 시작점은 첫 번째 최적화 이후의 중간 상태이며, 수정 전 원래 전체 배치 시간을 측정했다고 보고하지 않는다.

증거는 `Docs/Validation/MapEditorLoading/`의 테스트 결과, 컴파일 상태, `preview-timings.json`, `commit-timings.json`에 보관했다. 중간 측정의 긴 eval은 CLI의 5초 제한을 넘겼으나 main-thread 작업은 계속 실행되어 측정 파일과 임시 에셋 정리를 완료했다. 최종 측정은 정상 반환했다.

## 변경 파일과 범위

- `Assets/ANIMOL/Scripts/Editor/TerrainEditorV4/TerrainEditorObjectAdapter.cs`
- `Assets/ANIMOL/Scripts/Editor/TerrainEditorV4/TerrainEditorSceneEditor.cs`
- `Assets/ANIMOL/Scripts/Development/TerrainEditorV4/TerrainEditorArt.cs`
- `Assets/ANIMOL/NamedArt/Editor/NamedArtAutomation.cs`
- `Assets/ANIMOL/Tests/EditMode/TerrainEditorV4Tests.cs`
- `Assets/ANIMOL/NamedArt/Tests/Editor/NamedArtReferenceTests.cs`

맵을 처음 여는 에셋 읽기와 실제 선택 맵 저장은 남는다. 설치·창 열기로 사용자 맵을 변환하거나 저장하지 않는다. V6 계약·아틀라스·테마·물리·Ready·보상 규칙은 변경하지 않는다. 이번 변경은 반복 로딩 경로 제거이며, 사용자 기존 변경에 속하는 `StageMapAuthoringOperations.cs`의 레거시 backup service나 과거 에셋을 일괄 삭제하지 않았다. 현재 Scene 맵 에디터의 장애물·마커 커밋에서는 그 service를 호출하지 않는다.

미실행: Unity Profiler UI 캡처, 모든 캠페인/대형 맵의 성능 측정, PlayMode 전체/전체 프로젝트 테스트, 아트 재생성 및 Node 패키지 검사. 렌더 원본은 바꾸지 않아 픽셀 비교판을 새로 만들지 않았다. 위 제한된 fixture와 실제 실행한 테스트의 PASS를 모든 맵의 무로딩이나 미관 승인으로 확대하지 않는다.
