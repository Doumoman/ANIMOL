# ANIMOL MISSING_UI_V1 — 작업 1 결과

작성: 2026-10-05 / Unity **6000.3.8f1** / 현재 작업 트리 기준.

## 판정

**기존 로비의 상점 진입에 새 상품 카드와 공용 모달 판면을 적용했다.** 실제 Unity 컴파일, 로비 진입/반환, 모달 취소/재진입, 스크롤·Raycast 및 두 세로 해상도 검사를 실행했다.

**실제 상품 조회·구매·복원 성공은 구현/검증되지 않았다.** 현재 프로젝트에 인증 상품 공급자 및 구매·복원 실행/receipt 계약이 없어 가격·보유·잔액을 `--`로 표시하고 실행 버튼을 비활성화했다. 전체 패키지 또는 모든 수용기준 완료 판정이 아니다. 작업 2~5의 운영 UI 적용도 이번 결과에 포함하지 않는다.

## 조사한 현재 연결과 적용 방법

패키지 `README_KO.md`, `manifest.json`, `source/screen-catalog.json`, `Docs/acceptance-missing-ui.json`, `Docs/UNITY_NATIVE_INTEGRATION.md`, import 스크립트 및 최신 Unity 소스와 저장소 AGENTS.md를 읽었다. 시작 시 기존 변경 494개 상태 항목을 확인했으며 사용자 변경은 이번 커밋에 포함하지 않는다.

| 소유자/위치 | 확인 및 연결 |
|---|---|
| `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M1.cs`, M3/M4/M6/M6Economy/M6Multiplayer/M9Portrait | 기존 공용 루트·화면·씬 생성 단계. 기존 수정 중인 생성물은 재생성하지 않았다. |
| `Assets/ANIMOL/UI/PortraitArtV1/PortraitEntryController.cs` | 현재 `ModeScreen.Shop` → `Navigation.Navigate("SC11_Store")` 동선을 그대로 사용. |
| `Assets/ANIMOL/Scripts/UI/UiNavigationService.cs` | 기존 `SafeArea/ScreenHost/SC11_Store`와 history 기반 Back 유지. 활성 상점 뒤로 버튼 이름도 `StoreBackButton` 유지. |
| `Assets/ANIMOL/Scripts/UI/MetaUiPresenter.cs` | 기존 상품/오류/광고 모달의 닫기 및 기타 리스너 유지. |
| `Assets/ANIMOL/Scripts/UI/GrowthEconomyUiPresenter.cs` | 계정 공통 3트랙 팩 정책, 인증 공급 미연결 및 기존 구매 차단을 확인. |
| `Assets/ANIMOL/Scripts/Core/GrowthEconomyContracts.cs` | `GrowthCompletionPackContract`, `AuthenticatedOfferSnapshot`은 존재. 실제 플랫폼 조회·구매·복원 공급자로 간주하지 않았다. |
| `Assets/ANIMOL/Scripts/Core/ExternalServiceConfiguration.cs` | SDK 설정 플래그만으로 실제 승인 가능 상태를 만들지 않았다. |
| `Assets/ANIMOL/Typography/PixelTypographyInstaller.cs`, `PixelTextBridge.cs`, `PixelTypographyProfile.asset` | 현재 pixelroborobo TMP 및 fallback 체계를 재사용. 긴 본문은 실제 TMP 측정 높이로 스크롤 콘텐츠 크기 계산. |
| 기존 `UI_CommonRoot` | CanvasScaler 1080×1920, ScaleWithScreenSize, width 기준(match=0), 기존 `SafeAreaLayout` 유지. |
| 기존 EventSystem | `StandaloneInputModule`, ProjectSettings activeInputHandler=0. 운영 프리팹은 Canvas/EventSystem을 추가하지 않는다. 실제 로비에서 EventSystem 1개 확인. |
| `UiScenePolish` | 기존 후처리 이후 설치. 새 버튼 스킨과 모달 본문 색 유지 처리로 재스타일링 후에도 글자 대비 보존. |

기존 Phase1~3과 동일한 `sceneLoaded` 설치 방식을 사용한다. `MissingUiProjectEntry`가 기존 SC11 자식들을 비활성으로 보존하고 별도 운영 Store 프리팹을 붙인다. 기존 부모 화면, navigation, 공용 modal root, Text/Button 인스턴스와 기존 리스너는 유지한다. 레거시 버튼은 비활성 자식에 남으므로 새 코드에서는 `MissingStoreView.Back` 또는 **활성 화면 범위**로 버튼을 찾는다.

수동으로 기존 prefab YAML을 고치지 않았다. 새 생성 메뉴와 런타임 설치 코드가 배치의 소유자다. 새 메뉴 재생성 후에도 실제 진입 검사를 통과했다. 다만 사용자 변경이 있는 **기존 M1~M9 전체 재생성은 실행하지 않았으므로** 그 전체 재생성 검사는 미검증이다.

## 변경 파일과 생성물

기존 운영 소스·프리팹 파일은 수정하지 않았다. 신규 작업 영역은 다음과 같다. 개별 파일 전체 목록은 [changed-files.txt](Docs/MissingUiV1Task1/changed-files.txt)에 기록한다.

| 경로 | 내용 |
|---|---|
| `Assets/ANIMOL/UI/MissingUiV1/Runtime`, `Editor`, `Sprites`, JSON | 패키지 공용 코드, PNG 55개, manifest/catalog 도입. |
| `.../Editor/MissingUiV1Builder.cs` | Unity 6 호환 수정: TextureImporterSettings를 통한 Full Rect/pivot, `UnityEngine.UI.Image.Type` 이름 충돌 해소, 임시 씬 종료 후 데모 씬 생성. 전역 SaveAssets 제거로 다른 dirty asset 저장 방지. 밝은 시안 카드의 글자 대비 수정. |
| `.../Generated/Prefabs/*.prefab` | 독립 읽기 전용 시안 23개. 운영 적용과 구분. |
| `.../Generated/ANIMOL_MissingUiV1_ReadOnlyPreview.unity` | 독립 시안 씬. 운영 씬/빌드 설정으로 교체하지 않았다. |
| `.../Project/Resources/ANIMOLMissingUiV1/Store.prefab` | **Generated 밖 운영 상점 프리팹**. 자체 Canvas/EventSystem 없음. |
| `.../Project/Resources/ANIMOLMissingUiV1/Art.asset` | 기존 ProductionV1 공용 panel/button 및 현재 typography GUID 참조, 신규 상품/행/가격/스크롤 그림 참조. |
| `.../Project/MissingUiProjectEntry.cs` | 기존 SC11 및 공용 모달에 런타임 설치. 중복 설치 방지. |
| `.../Project/MissingStoreView.cs`, `MissingUiLayout.cs` | 두 상품 카드, 원본 비율 그림, 가격·보유 자리, 상세/복원 안내, 스크롤 및 고정 뒤로 버튼. |
| `.../Project/MissingCommonModalSkin.cs`, `MissingUiButtonSkin.cs`, `MissingUiArt.cs` | 공용 판면·스크롤·버튼 대비 및 자산 참조. |
| `.../Project/Editor/MissingUiProjectBuilder.cs` | 현재 프로젝트 폰트를 지정한 패키지 생성 + 운영 프리팹 생성. |
| `.../Project/Tests/EditMode`, `.../Project/Tests/PlayMode` | 실제 import/prefab/로비/모달/입력/화면 검사. |
| `.../PROJECT_INTEGRATION.md` | 재생성 및 후속 연결 안내. |
| `Docs/MissingUiV1Task1` | 원본 아트 비교, Unity 검사 JSON, Game View 캡처, 보존 비교 증거. |

사용 메뉴: **ANIMOL > Missing UI V1 > Build Project Task 1 Store and Modals**. 내부에서 패키지 Build Missing UI V1 및 Validate Missing UI V1을 실행한다. 메뉴를 실제 재실행해 생성 완료를 확인했다.

공용 모달 적용 대상은 `ProductDetailModal`, `ServiceErrorModal`, `RewardedAdModal`, `ConfirmExitModal`, `MultiplayerPauseModal`이다. 본문은 스크롤되고 주/보조 버튼은 하단에 고정된다. 진행 중 거래를 취소한 것처럼 표시하는 기능을 추가하지 않았다.

## 현재 가능한 동작과 차단한 동작

| 항목 | 결과 |
|---|---|
| 로비 상점 → 상품 카드 → 전체 설명 → 닫기 → 로비 | 실제 Unity에서 통과. 기존 Bootstrap→로비→상점의 Raycast 클릭 검사도 통과. |
| 성장 완료 팩 | 계정 공통 최대 스태미나/재생/소모 감소 3트랙만 설명. 현재/적용 후 값은 `--`. 동물 성장·해금 미포함을 명시. |
| 이모티콘 번들 | 전체 구성, 실제 가격, 보유권 공급이 없어 설정 대기/`--`. 임의 유료 이모티콘이나 보유권을 추가하지 않았다. |
| 상품 가격·보유 코인·보유 상태 | `--`/연결 대기. 미조회 값을 0/무료/미보유로 표시하지 않는다. |
| 구매 복원 | 안내 열람 가능. 실행 버튼은 비활성. 실제 조회·복원 요청이나 성공 토스트 없음. |
| 상세 모달 취소/중복 열기 | 취소는 기존 닫기 리스너 사용. 열린 동안 상점의 상세/뒤로 액션 재호출을 차단. 모달 뒤 클릭은 Raycast로 차단. |
| 저장 데이터 | 상점 상세·반환을 3회 반복한 뒤 persistentDataPath의 파일 목록/내용이 동일함을 검사. 서버 저장/계정 비교는 미연결로 미검증. |
| 처리 중/Rejected/Unknown·ActionId 보존 | **실제 구매·복원 계약 부재로 미지원.** 현재 실행 진입 자체가 차단되어 있다. 승인/거절을 흉내 내거나 가짜 ActionId를 발급하지 않았다. |

실제 제공자 호출 카운터는 존재하지 않는다. “서버 요청 0회 실측”, “확정 1회”, “구매 성공”으로 보고하지 않는다. 실행 버튼 비활성 및 현재 열람 코드에 구매·복원 API 호출이 없음을 각각 확인했다. 후속 서비스 연결 시 인증된 `DisplayPrice`/보유권, 전체 비용/구성, 단일 확정, 불변 ActionId, 멱등 결과 조회·receipt 및 앱 복귀 계약을 함께 연결해야 한다.

## 실제 실행한 검사

최종 검사 묶음 기준 **42건 중 39 통과, 기존 실패 3건**. 아래 합계는 초기 수정 중 실행 및 비활성 대조 실행을 중복 합산하지 않는다.

| 검사 | 결과 | 증거 |
|---|---:|---|
| Unity C# 컴파일 | 오류 0 | [unity-compile.json](Docs/MissingUiV1Task1/unity-compile.json) |
| 신규 MissingUiAssetTests | 3/3 | [asset-tests.json](Docs/MissingUiV1Task1/asset-tests.json) |
| 신규 MissingUiStoreFlowTests | 5/5 | [store-playmode-tests.json](Docs/MissingUiV1Task1/store-playmode-tests.json) |
| CommercialPolishPolicyTests | 3/3 | [feedback-policy-tests.json](Docs/MissingUiV1Task1/feedback-policy-tests.json) |
| ProductionFlowTests | 7/7 | [결과](Docs/MissingUiV1Task1/regression-ProductionFlowTests.json) |
| PortraitEntryFlowTests | 3/3 | [결과](Docs/MissingUiV1Task1/regression-PortraitEntryFlowTests.json) |
| MenuThemeFlowTests | 3/3 | [결과](Docs/MissingUiV1Task1/regression-MenuThemeFlowTests.json) |
| UpgradePhase1FlowTests | 2/2 | [결과](Docs/MissingUiV1Task1/regression-UpgradePhase1FlowTests.json) |
| StagePhase2FlowTests | 9/9 | [결과](Docs/MissingUiV1Task1/regression-StagePhase2FlowTests.json) |
| MultiplayerPhase3FlowTests | 3/3 | [결과](Docs/MissingUiV1Task1/regression-MultiplayerPhase3FlowTests.json) |
| M6GrowthEconomyUiFlowTests | 1/4 | [현재 결과](Docs/MissingUiV1Task1/regression-M6GrowthEconomyUiFlowTests.json), [새 연결 비활성 대조](Docs/MissingUiV1Task1/baseline-disabled-M6GrowthEconomyUiFlowTests.json) |

M6 실패 3건은 현재 포트레이트 로비의 비활성 레거시 버튼을 활성 자식에서 찾는 검사에서 `Sequence contains no matching element`로 실패한다(24/50/83행에서 공통 Button helper 124행). 새 설치 훅을 일시적으로 제외한 대조 실행에서도 동일한 3건이 실패했다. 최종 소스에서는 설치 훅을 복구하고 컴파일/상점/로비 진입 검사를 다시 통과했다. 이 기존 테스트 실패를 해결 완료로 처리하지 않는다.

초기 패키지의 컴파일 오류 4개와 미저장 임시 씬 중첩 생성 오류를 수정했다. 초기 모달 라벨 이동 오류 및 상점 뒤로 버튼 이름 회귀도 수정 후 재검사했다. 기존 `PlayOneShot` null AudioClip 경고는 별도 기존 문제로 남긴다.

### 화면 증거

실제 Game View 캡처 **29장**: [Captures](Docs/MissingUiV1Task1/Captures). 1080×1920/1080×2400 각각 전체 화면과 좌/우 48px, 하단 96px, 상단 120px inset을 적용한 **Editor Safe Area 모사**를 실행했다. 물리 Android notch 검증은 아니다.

- [1920 상점 상단](Docs/MissingUiV1Task1/Captures/1920_full_store_top.png), [1920 상점 하단](Docs/MissingUiV1Task1/Captures/1920_full_store_bottom.png)
- [2400 Safe Area 상점](Docs/MissingUiV1Task1/Captures/2400_safe_store_bottom.png)
- [1920 Safe Area 성장 팩 전체 설명](Docs/MissingUiV1Task1/Captures/1920_safe_growth_bottom.png)
- [이모티콘 상세](Docs/MissingUiV1Task1/Captures/2400_safe_emote.png), [복원 미연결 안내](Docs/MissingUiV1Task1/Captures/1920_safe_restore.png)
- `fixture_*` 5장은 **합성 장문 QA 데이터**를 넣은 공용 모달 스크롤 하단 캡처다. 실제 상품/서버 오류 응답이 아니다.

고정 하단 버튼이 Safe Area 안에 있는지, 장문 본문 콘텐츠가 스크롤 가능한지, 기존 닫기 리스너/재진입이 유지되는지 검사했다. EventSystem Raycast로 모달 아래 뒤로 버튼 차단 및 ScrollRect wheel 이벤트 동작을 확인했다. 물리 터치 제스처·키보드·실제 최장 상품 가격/오류는 미검증이다.

## 원본 및 기존 변경 보존

- 신규 PNG **55/55**: 패키지 원본 바이트와 SHA-256, manifest 크기, Sweetie16 일치. [파일 검사](Docs/MissingUiV1Task1/sprite-audit.json). Unity Single/Full Rect, Point, 무압축, mipmap off, manifest border도 별도 EditMode 검사 통과.
- 프레임은 Sliced, 상품 그림은 preserveAspect. 기존 ProductionV1 공용 자산 GUID를 참조하며 기존 atlas/meta를 교체하지 않았다.
- 작업 전 기록한 기존 `UI`, `AnimalUiV2`, `Data`, `Scenes`, `Prefabs`, `Typography` **1,288개 파일**의 최종 SHA 비교: [preservation-audit.json](Docs/MissingUiV1Task1/preservation-audit.json).
- 비교 범위에 15종 초상, v7 20배경/토끼 2시트, 로비 버튼, Phase1~3 운영 프리팹/Catalog/IdMap, 폰트, 성장·재화·스테이지 asset 포함. 테스트 중 갱신된 동적 fallback 폰트 캐시와 NamedArt 등록 캐시는 작업 시작 직전 사본으로 돌리고 Unity에 재import했다. 사용자 작업 이전 버전으로 되돌린 것이 아니다.
- v7 토끼 시트의 과거 패키지 원본과 현 작업 트리 간 기존 불일치는 복원/수정하지 않았다. 이번 비교는 **사용자가 작업하던 시작 시점의 바이트 보존**이다.

## 수용기준 및 후속 작업

| 기준 | 판정 |
|---|---|
| BASE-01~05 | 작업 1의 현재 경로/보존/폰트/운영 입력 범위 확인. |
| BASE-06 | 새 메뉴 재생성 및 기존 SC11 런타임 설치 통과. 기존 사용자 수정 M1~M9 전체 재생성은 미검증. |
| ART-01~03 | 신규 PNG 파일 및 Unity import 검사 통과. 작업 1 화면의 프레임/비율 확인. 다른 작업의 모든 화면 시각 검증은 미실행. |
| STORE-01~02 | 현재 로비 동선과 3트랙 한정 설명 통과. |
| STORE-03~05 | 미연결/미설정 값 차단과 전체 안내 스크롤 확인. 실제 상품 데이터의 조회/보유/빈 목록/실패 상태 전이는 공급자 부재로 미검증. |
| STORE-06 | 열람/취소 동선 및 중복 모달 차단 확인. 실제 구매 확정/처리 중 요청 횟수·중복 차감 검사는 차단. |
| STORE-07~08 | 구매·복원 서비스, receipt, ActionId/Unknown 결과 조회·중앙 복귀 계약 없음. 미지원/미검증. |
| UX/회귀 | 두 Game View/Safe Area 모사/장문/모달/기본 입력 및 기재한 회귀 검사만 수행. 실제 기기 검증은 남음. |
| SET/MULTI/EMOTE/기록/협동/이야기 | 이번에는 원본 패키지와 독립 시안만 도입. 작업 2~4 운영 적용 전. |

최신 **경쟁 4인·커스텀 방** 결정은 문서에서 확인했다. 이번 상점/모달 작업에서 기존 경쟁 인원/모드/방 정책이나 협동 정책을 변경하지 않았다. 현재 계약의 과거 4~8인 요소를 실제 정책 소유자와 맞추는 작업은 지정된 작업 3에 남긴다.

다음 필수 연결은 인증 상품 공급자, 구매/복원 SDK와 승인 receipt, 계정 성장 팩의 보상/판매 정책, 이모티콘 번들 카탈로그, 결과 미확인 요청을 보존하는 중앙 관리 계약이다. 이 계약이 없는 상태에서 구매·복원을 활성화하지 않는다.

이번 검증은 기존 단계의 미커밋 파일을 포함한 현재 작업 트리에서 수행했다. 기존 사용자 파일을 이번 커밋에 섞지 않으므로, 이번 커밋 하나만 새 checkout에 적용한 재현 검증은 별도 수행하지 않았다.
