# Android 40스테이지 임시 빌드

2026-11-12. 별길 목장 20개와 잔영 극장 20개를 기존 로비에서 실행하는 임시 빌드 설정이다.

## 빌드 방법

- Unity 메뉴 `ANIMOL > Build > Prepare Android 40 Stage Preview`: 임시 캠페인과 Android 설정을 다시 적용한다.
- 다른 체크아웃에서는 이 Prepare 메뉴를 실행한 뒤 설정 검사를 실행한다. 작업 전부터 존재하던 사용자의 portrait 설정 변경은 미커밋 상태로 유지했다.
- `ANIMOL > Build > Build Android 40 Stage Preview APK`: 설정 후 `Builds/Android/ANIMOL_40Stage_Portrait.apk`를 생성한다.
- 일반 Build Profiles에서도 Android를 선택한 현재 공용 씬 목록으로 빌드할 수 있다. 씬 순서는 `NotebookUI`, `ANIMOL_Echo_Theater`다. 별길 퍼즐은 로비에서 기존 런타임 세션을 생성한다. Art QA 씬은 빌드 목록에서 제외했으며 삭제하지 않았다.

## 플레이 범위

- 별길: 화면의 SW01~SW20은 현재 `SW00_MapData`~`SW19_MapData` 파일명에 연결한다. 원본 MapData, 시나리오, GUID, verificationRoute는 수정하지 않는다. 기존 sourceId 기반 클리어 기록을 유지한다.
- 극장: 기존 EchoTheaterMapSet의 ET-01~ET-20 참조와 순서를 사용한다. ET-21 이후는 진입이나 다음 스테이지 대상이 아니다.
- 임시 접근 설정은 `Data/Puzzle/Resources/NotebookCampaignPreview.asset`이다. 잔영 극장을 구매 없이 이용할 수 있으나 구매 기록이나 구매 성공을 생성하지 않는다. 설정의 enabled를 끄면 임시 접근도 끝난다.
- 각 월드는 이전 스테이지의 실제 클리어 후 다음 스테이지가 열린다. 20번에서는 지도 복귀만 제공한다. 두 월드 사이의 이동은 별자리 지도에서 한다. 기존 망원경 입장 비용과 회복 규칙은 유지한다.
- 낙일 성소, 별너울 해안, 결제, 광고, 대전 등 미연결 기능은 그대로 미연결 상태다.

## 화면과 Android 설정

- Portrait 고정, 설계 기준 1080×1920(가로:세로 9:16), ARM64 / IL2CPP / APK / LZ4HC 플레이어 데이터 압축.
- 화면 해상도를 1080×1920으로 강제하지 않는다. Android의 실제 전체 화면과 노치 영역까지 배경을 그리며, 조작 UI는 기존 Safe Area 배치를 사용한다.
- Android의 지원 화면비를 Custom 1~3으로 설정하고 전체 화면과 화면 크기 변경을 허용한다. 스플래시 배경은 Scale to Fill이다. [Unity의 Android 화면비 API 근거](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Editor/Mono/PlayerSettingsAndroid.bindings.cs).
- 두 월드의 기존 불투명 하늘 Cover 구현을 유지한다. 카메라 이동·화면비에 따라 원본 비율을 유지하며 확대해 채운다. 게임 카메라 viewport는 전체 화면이다. 아트 원본이나 게임 판정을 바꾸지 않는다.

## 검증

검증 결과와 실제 GameView 캡처는 [Validation/AndroidCampaign](Validation/AndroidCampaign)에 있다. 40개 맵 로드 검사는 풀이·최단 경로 증명이 아니며, 다음 스테이지 검사는 클리어 상태 fixture로 네비게이션을 검증한다.

- EditMode 22/22 통과: AndroidCampaignBuildTests 3, Notebook 관련 검사 13, ArtAssetTests 4, ArtCatalogBuildValidationTests 2.
- PlayMode 12/12 통과: 실제 로비·40맵·배경 검사 1, NotebookCampaignPlayTests 5, NotebookUiPlayTests 5, 세로 ArtQaPlayTests 1.
- 1080×1920과 1080×2400의 로비·별길·극장 캡처를 직접 확인했다. 40개 맵에서 전체 viewport와 불투명 하늘의 화면 덮임을 검사했다.
- 맵 데이터와 메타 122개 파일은 검증 전후 SHA-256이 모두 같다. 첫 맵 SW00과 메타의 해시도 별도 기록했다.
- 기존 CommercialPolishPolicyTests는 보관본에 있어 실행 대상에 넣지 않았다. 현재 ArtAssetTests / ArtQaPlayTests의 실제 통과 결과와 구분한다.
- Android 실기기 설치·터치 조작은 이번 검증에 포함하지 않았다.

APK 생성은 성공했다. 빌드 오류 0개, 경고 2개이며 경고는 Player용 Pipeline 비활성 상태와 진단 심볼 설정 안내다. 실제 APK는 `Builds/Android/ANIMOL_40Stage_Portrait.apk`, **883,798,377 bytes (842.86 MiB)**다. BuildReport의 totalSize는 심볼 등 산출물 합계이므로 APK 파일 크기는 `apk-audit.json` 값을 사용한다.

실제 APK에서 ARM64, Target SDK 36, Portrait, 전체 화면, resizeableActivity, 화면비 1~3 설정을 확인했다. APK v2 서명 검증도 통과했다. SHA-256: `17e79545358dda6b72d5337d71cd92d5540581dab38538522035c0458effdf02`.

압축하지 않은 중간 빌드 데이터는 Gradle의 4GB ZIP 한도를 넘었다. 이미지·임포터·스프라이트 메타를 변경하지 않고, 빌드 메뉴와 Android Compression Method에 무손실 LZ4HC를 적용했다.

### 기존 빌드 차단 오류 수정

첫 APK 시도에서 기존 아트 검증기 9개가 정리 전의 `Sprites` / 패키지 루트와 삭제된 작성용 매니페스트를 요구해 실패했다. 빌드 진입점은 현재 카탈로그의 런타임 구조 검증과 활성 `Art` 에셋의 실제 Sprite GUID·fileID 검증을 사용하도록 수정했다. 원본 패키지를 대상으로 한 작성 도구의 매니페스트 검증은 유지했다. 아트·GUID·카탈로그 원본이나 보관 폴더를 복원하거나 변경하지 않았다.

9개 카탈로그의 실제 참조를 검사하는 테스트와 GUID를 손상시킨 복사본의 빌드 거부 테스트가 통과했다.
