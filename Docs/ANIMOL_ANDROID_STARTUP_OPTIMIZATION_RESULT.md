# Android 시작 로딩과 텍스처 메모리 최적화

2026-11-12. Galaxy S25 FE에서 기존 APK가 시작 직후 종료된다는 제보에 따라 조사하고 수정했다. USB 연결 기기가 없어 실제 Android 종료 로그는 수집하지 못했다. 아래에서 확인한 자산 부하는 종료 원인 후보이며, 기기에서 크래시가 해결됐다는 검증 결과는 아니다.

## 확인한 부하와 수정

- 기존 로비 프리팹은 별길 20개 MapData와 스토리 프리팹을 직접 참조했다. 진행 기록 확인도 원본 맵 카탈로그를 열었고, 임시 접근 설정은 극장 맵 세트를 직접 참조했다. 이 참조들은 맵·스토리의 이미지까지 로비 의존성에 포함했다.
- 로비의 무거운 참조를 제거했다. 별길 맵, 극장 맵 세트, 스토리는 각각 독립된 `NotebookDeferredAssets` Resources 자산에서 실제 진입 시 불러온다. 아직 한 월드 안의 맵별 로딩까지 분리한 것은 아니다.
- `NotebookStageIndex`는 문자열 형태의 스테이지 ID·원본 sourceId·파일명만 가진다. 로비의 접근 가능 여부와 클리어 기록 조회는 이 인덱스를 사용한다. 첫 맵, 파일명에 따른 배치, 원본 sourceId와 클리어 키를 유지한다. 인덱스와 지연 로딩 컨테이너는 맵 변경·플레이 진입·빌드 전에 동기화한다.
- 활성 아트의 Android 임포트에 ASTC 4×4 GPU 압축을 적용했다. 원본 파일, 기본 해상도 제한, 필터, PPU, 피벗, 스프라이트 ID와 GUID는 유지한다. 압축은 손실 압축이며 원본 이미지를 다시 그리거나 축소하지 않는다. Desktop의 실제 임포트 설정과 iPhone 설정은 바꾸지 않는다. 중앙 정책은 `NotebookAndroidTexturePolicy`다.
- 화면 해상도, Overlay Canvas, 카메라 구성, 맵 판정, 보상·입장 비용, 입력·시간·연출 정책은 수정하지 않았다.

## 측정

`startup-metrics.json`은 메인 씬의 고유 Texture2D 의존성에 대해 에디터의 `Profiler.GetRuntimeMemorySizeLong`을 합산한 값이다. Android 프로세스의 실제 최대 메모리 측정이 아니다.

| 항목 | 변경 전 | 변경 후 |
|---|---:|---:|
| 메인 씬 참조 Texture2D | 4,335개 | 808개 |
| 에디터 텍스처 메모리 합산 | 5,659,692,919 bytes (5.27 GiB) | 560,286,295 bytes (534.33 MiB) |
| 메인 씬의 MapData `.asset` 의존성 | 있음 | 0개 |
| 메인 씬의 `Art/Maps` 의존성 | 있음 | 0개 |

대표 2048×2048 패턴 이미지가 같은 크기의 `ASTC_4x4`로 임포트된 것도 확인했다. 상세 검증 자료는 [Validation/AndroidStartupOptimization](Validation/AndroidStartupOptimization)에 둔다.

## 빌드와 검증 범위

사용자가 직접 빌드하기로 했으므로 APK 빌드는 실행하지 않았다. 기존 `Builds/Android/ANIMOL_40Stage_Portrait.apk`는 이전 APK이며, 최적화된 코드와 자산을 포함하지 않는다. 새 APK의 파일 크기, 실제 기기의 시작 성공 여부와 최고 메모리는 새로 빌드·설치한 뒤 확인해야 한다. 기존 Android Build 버튼을 사용할 수 있으며 준비와 검증 콜백이 새 인덱스와 컨테이너를 동기화한다.

현재 실행한 테스트와 원본 보존 결과는 같은 검증 폴더의 JSON으로 확인한다. Legacy에 격리된 `CommercialPolishPolicyTests`는 실행하지 않으며 활성 `ArtAssetTests`와 세로 `ArtQaPlayTests` 결과와 구분한다. `Docs/ART_PRESERVATION.json`의 71개 해시가 모두 일치하고, 작업 시작 때 기록한 원본 맵·메타 122개 파일 해시도 모두 같다. Android용 메타 10,092개에 압축을 적용했으며, 원본 이미지와 GUID는 바꾸지 않았다. 구형 스토리 UI 메타 27개는 Unity가 최신 직렬화 형식으로 저장하면서 기본값과 빈 메타데이터 필드를 명시했다. 기존 비 Android 임포트 값과 스프라이트 ID·fileID 변경은 없다.

- EditMode 26/26: Notebook 17(시작 의존성·지연 로딩·압축 회귀 검사 4개 포함), AndroidCampaignBuildTests 3, ArtAssetTests 4, ArtCatalogBuildValidationTests 2.
- PlayMode 12/12: NotebookUiPlayTests 5, NotebookCampaignPlayTests 5, 실제 40맵 진입·배경 검사 1, 세로 ArtQaPlayTests 1. 스토리 누락 시 거절 처리와 실제 스토리 재생도 다시 통과했다.
- 실제 GameView 1080×1920 / 1080×2400의 로비·별길·극장 캡처를 보관했다. 이전 검증 이미지와 사용자의 기존 씬 변경은 유지했다.

추가로 `Notebook` 문자열 필터를 넓게 실행할 때 빌드에 포함되지 않는 독립 대전 프리뷰의 `CrayonUIPlayTests.ExistingNotebookNavigationAndReducedMotionWorkWithTheStandalonePreview`도 실행됐다. 이 검사의 Pause 버튼 raycast는 실패했으며 별도 자료 `extra-preview-check.json`에 남겼다. 해당 씬·코드는 수정하지 않았고 위 26/12개의 현재 캠페인 검증에 포함하지 않았다. 이 실패의 원인이나 수정 완료를 주장하지 않는다.

작업 시작 때 이미 존재한 씬·맵·프로젝트 설정·보관 자료 변경은 커밋에 섞지 않는다. 기존 맵 편집에 따라 자동 갱신된 `StarwayStageCatalog.asset`의 파일명·해시 변경도 이번 최적화 커밋에서 제외한다.
