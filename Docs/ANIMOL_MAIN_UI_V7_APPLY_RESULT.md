# ANIMOL 메인 UI v7 Unity 적용 결과

2026-10-17. 입력은 `MCP_INBOX/ANIMOL_Main_UI_v7.zip`이며, ZIP 내부 최상위 폴더는 `ANIMOL_Main_UI_v1/`이다. 사용자가 지정한 이름의 ZIP은 없었지만 이 패키지의 README, v7 인물 문서와 JSON, 프롬프트 JSON, VALIDATION, `source/` 구현을 확인해 내부 v7로 판정했다. ZIP SHA-256은 `7e8e2c4a8ced0a8ce7addc90b56e15d9ef4dd1c8a7ecb47a65629c334b93dc40`이다. 웹 검수 결과를 Unity 검수 결과로 대체하지 않았다.

## 적용 경로와 범위

- 활성 로비 씬은 [ANIMOL_Main.unity](../Assets/ANIMOL/Scenes/ANIMOL_Main.unity), 진입점은 [MainLobbyController.cs](../Assets/ANIMOL/Scripts/Runtime/MainLobbyController.cs)다. `EditorBuildSettings`의 첫 씬으로 등록했고 기존 Art QA 씬은 유지했다. [MainLobbyBuilder.cs](../Assets/ANIMOL/Scripts/Editor/MainLobbyBuilder.cs)의 `ANIMOL/UI/Build Main Lobby`로 같은 구조를 재생성할 수 있다.
- 기존 활성 프로젝트에 메인 로비와 메인 일러스트 경로가 없었다. 따라서 패키지의 메인 일러스트 17개를 `Assets/ANIMOL/Sprites/UI/Main/Illustrations/`, 통합 풍경 4개를 `Assets/ANIMOL/Sprites/UI/Main/Backgrounds/`에 신규 등록했다. 기존 활성 `.meta`/GUID가 없어 갱신 유지 대상은 없었다. 생성된 GUID는 카탈로그와 씬에서 참조하며 재생성 시 유지된다. 게임용 160px PNG, PPU, 피벗, 클립, 캐릭터/저장 ID, 콜라이더와 판정은 변경하지 않았다.
- 입력의 v6 인벤토리와 SHA-256 비교 시 6개(`mion`, `piko`, `lumi`, `soyul`, `haerin`, `stranger`)만 변경됐고, 나머지 11개 메인 일러스트와 배경 4개는 v6와 바이트가 같다. 변경된 6개는 모두 원본 1024×1536 RGBA PNG다. 배경 4개는 1122×1402 RGB 통합 풍경이며 별도 전경을 연결하지 않았다.
- 게임 카탈로그 4개와 대조한 [MainMenuCatalog.asset](../Assets/ANIMOL/Data/UI/MainMenuCatalog.asset)에 4테마, 저장 인물 ID 16개, 메뉴 외형 17개를 연결했다. `eden`과 `edenpuppy`는 같은 `eden` 인물의 rabbit/puppy 외형이다. 루미의 페럿은 PNG 내부에만 있다. `soyul`은 별도 ID이며 `doyun`을 덮지 않았다. 이방인의 표시 이름은 임의 확정하지 않았다.
- 메뉴 일러스트/풍경은 Sprite Single, FullRect, Bilinear, 무압축, 밉맵 끔, Max Size 2048, Android RGBA32 무압축이다. 사용 중인 메인 UI SpriteAtlas가 없음을 확인했다. 기존 게임 스프라이트 임포터가 이 폴더에 Point/PPU32를 강제하던 충돌을 [ArtAssetBuilder.cs](../Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs)에서 메인 UI 경로만 제외해 해결했다. 원본 PNG는 재가공하지 않았다.
- UI 글꼴은 패키지의 `ui-ko.woff`에서 원형 글리프를 TTF로 변환한 [ANIMOLMenuKorean.ttf](../Assets/ANIMOL/Fonts/ANIMOLMenuKorean.ttf)이며 OFL 문서를 함께 두었다.

## 실제 로비 동작

- 새 로비 방문에 테마를 균등 추첨하고 해당 테마의 인물 4명 셔플 백에서 인물을 고른 다음 외형을 고른다. 2026-10-17 추가 요청에 따라 로비가 열린 동안 5초마다 다음 테마·인물로 바뀐다. 테마 4개를 섞어 한 차례씩 보여주며 같은 배경이 연속 나오지 않는다. 이든의 외형 2개가 인물 확률을 늘리지 않는다. 팝업/캠페인 닫기로 별도 재추첨하지 않는다.
- 평상시 통합 배경 한 장이 Scenery 마스크 안에서 28초 X ±2%, 36초 Y ±0.9%로 움직인다. 전환하는 0.35초 동안만 두 번째 배경과 캐릭터 일러스트를 겹쳐 알파로 전환한다. 마스크의 cover 크기에 1.052 배를 적용한다. 캐릭터의 위치·로고·메뉴·광고 칸은 고정이다. 캠페인/팝업/앱 숨김/포커스 해제/모션 줄이기에서는 배경 이동과 5초 타이머가 멈추며 복귀 시 누적 시간을 따라잡지 않는다. 모션 줄이기를 켜면 진행 중 전환을 끝내고 정지한다. 전환은 일반(`Subtle`) 연출로서 입력 차단, 진동, 카메라 흔들림, 전체 화면 플래시, 게임 시간 변경이 없다.
- CanvasScaler와 Safe Area 앵커를 사용하고 PC 가로 창에서는 중앙 9:16 프레임을 쓴다. 전신 일러스트는 원본 비율을 유지하며 로고 그룹 아래 18 기준 단위, 하단 메뉴 위 20 기준 단위의 여유를 둔다. 캠페인 카드의 풍경도 비율 유지 cover와 마스크를 사용한다.
- 메인에서 캠페인을 누르면 4테마 선택 화면이 열린다. 현재 활성 프로젝트에는 정식 캠페인 스테이지 목록·완료 저장·필수 스테이지 ID, 대전/커스텀/광고·구매 서비스가 없다. 스테이지 버튼과 서비스 버튼은 연결 전 상태를 명확히 알리는 팝업으로 끝난다. 대전은 필수 ID와 완료 소스가 모두 실제로 제공되기 전까지 잠긴다. 시험용 소스로 unknown/실패/미완료 잠금, 전부 완료 시 4테마 제공, unknown 재잠금을 검증했다. 시험용 소스나 QA 이벤트를 실제 진도로 저장하지 않았다.

## Unity 검수

- Unity 6000.3.8f1 배치 Editor에서 빌더 실행 및 컴파일 성공. 빌드 로그 `Logs/animol-main-v7-build.log`에 `Exiting batchmode successfully`; C# 컴파일 오류/미처리 예외 없음. 추적 가능한 핵심 결과는 [Unity 빌드 요약](Validation/MainUiV7/Unity-build-summary.txt)에 보존했다.
- 최초 적용 때 [EditMode 메인 아트 4/4](Validation/MainUiV7/EditMode-targeted.xml), [PlayMode 로비 3/3](Validation/MainUiV7/PlayMode-targeted.xml), 기존 [ArtAssetTests 4/4](Validation/MainUiV7/EditMode-art.xml), [ArtQaPlayTests 1/1](Validation/MainUiV7/PlayMode-art.xml) 통과. 5초 전환 적용 뒤 연결된 Editor에서 컴파일 성공, 메인 EditMode 4/4, 기존 ArtAssetTests 4/4, 기존 ArtQaPlayTests 1/1, [로비 PlayMode 5/5](Validation/MainUiV7/PlayMode-rotation.json) 통과. 레거시 CommercialPolishPolicyTests는 격리된 보관본이므로 활성 테스트로 실행하지 않았다.
- PlayMode에서 1080×1920 세로 Game View의 17외형을 실제 렌더링하고 각각의 새 Sprite 참조, 4테마 배경, 네 이동 끝의 cover 기하, 로고/메뉴 간격, 씬 재진입을 확인했다. [6명 캡처](Validation/MainUiV7/Portrait/piko.png): `mion.png`, `piko.png`, `lumi.png`, `soyul.png`, `haerin.png`, `stranger.png`. 같은 폴더에 나머지 11외형, [캠페인](Validation/MainUiV7/Portrait/campaign.png), [설정](Validation/MainUiV7/Portrait/settings.png), [1440×900 PC 창](Validation/MainUiV7/Portrait/pc_window.png) 캡처가 있다. 실제 캡처에서 전신·페럿·모자·가방·꼬리·신발 잘림과 오른쪽 남색 빈 띠는 보이지 않았다.
- 최초 적용 씬의 Android APK 빌드 성공. `Logs/animol-main-v7-android.log`의 `Main lobby Android validation build succeeded`; 결과는 임시 경로 `%TEMP%/ANIMOL_Main_UI_v7.apk`(135,342,391바이트)다. 5초 전환을 포함한 최종 APK의 빌드 결과는 아래 추가 검수에 기록한다. 기기 연결과 ADB가 없어 실기기 Safe Area·DPI·실행 프레임·터치·광고/구매/서버 동작은 미검증이다. 데스크톱 Game View의 Safe Area 계산과 창 리사이즈는 검증했지만 물리적 노치 검증을 대신하지 않는다.

### 5초 전환 추가 검수

- PlayMode에서 5초 전에는 동일한 테마·인물, 5초에 다음 테마·인물, 0.35초 알파 전환의 중간값과 종료 후 실제 Sprite 교체를 확인했다. 5.6초 실제 시간 동안 `Update`를 실행한 검수에서도 새 배경과 캐릭터 참조를 확인했다. 첫 순환에서 네 테마가 한 번씩 나오고 순환 경계에 같은 배경이 연속되지 않았다. [실제 전환 중 화면](Validation/MainUiV7/Portrait/crossfade.png)에는 이전/다음 통합 풍경과 전신이 함께 보이며 UI와 광고 칸은 고정이다.
- 캠페인·팝업·앱 숨김·포커스 해제·모션 줄이기에서 전환 시간이 진행되지 않고 인물이 재추첨되지 않음을 확인했다. 전환 Image의 `raycastTarget=false`, 캠페인 버튼 조작 가능, 배경 cover 유지도 확인했다.
- 열린 Unity Editor에서 [Android 재빌드](Validation/MainUiV7/Android-rotation-build.json) 성공: `ANIMOL_Main_UI_5s.apk` 138,766,765바이트, 오류 0. 빌드에 기존 QA 씬도 포함됐다. Pipeline Runtime 설정 부재와 진단 심볼 설정에 관한 경고 2개가 있었으며 실기기 실행은 수행하지 못했다.

## 남은 연결

정식 스테이지 카탈로그·완료 저장 소스와 캠페인 라우팅이 추가되면 `MainMenuCatalog.requiredStageIds`와 `MainLobbyController.requiredStageProgress`를 실제 정책에 연결해야 한다. 대전·커스텀·광고 제거는 각각 서버/구매 SDK 계약이 생기기 전까지 완료로 표시하지 않는다. 현재 APK는 로비 화면과 안전한 미연결 상태를 검수한 결과물이다.
