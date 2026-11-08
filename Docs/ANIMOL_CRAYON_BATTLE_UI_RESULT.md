# 별비 크레파스 전투 UI — 독립 씬 적용 결과

2026-11-09. 마지막 요청에 따라 **별도 전투 UI 씬을 먼저 만들고 실제 전투·메인 진입은 추후 연결**하도록 구성했다. 이번 결과는 플레이 가능한 전투 구현이 아닌, 실제 PNG·재질을 사용하는 UI 미리보기다.

## 열기

- 씬: `Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity`
- 프리팹: `Assets/ANIMOL/Prefabs/UI/Battle/BattleCrayonUI_ArtPreview.prefab`
- 재생 후 `시작 → 일시정지/계속하기`, `증강 → 선택/돌아가기`, `결과 미리보기 → 다시 시작`으로 화면을 확인한다.
- `움직임 줄이기`는 장식 Image만 대응하는 Still 재질로 바꾼다. 기존 `NotebookRoot.SetReducedMotion(bool)`에도 같은 설정을 전달하는 한 줄을 연결했다.
- 재생성 메뉴: `ANIMOL > Crayon UI > Build Standalone Battle Art Preview`. 이 생성기는 위 두 출력만 저장하며 Build Settings나 기존 메인 씬을 생성/교체하지 않는다.

## 사전 조사와 보존

실제 프로젝트는 Unity **6000.3.8f1**, **Built-in Render Pipeline**, 검수 PC의 그래픽 API는 **Direct3D12**다. 활성 씬은 `Scenes/Main/NotebookUI.unity`, Canvas는 `William Notebook UI`, 설계 루트는 `Design 1080 Safe Area`였다. `NotebookCanvasLayout`과 ConstantPixelSize CanvasScaler, 기존 Galmuri14 폰트를 확인했다. Prefab Stage 및 실제 별비 전투 씬/Canvas는 없었다. `NotebookProjectGateway.OpenBattle`도 아직 빈 메서드다.

작업 전 `git status --short` 939개 항목을 기록했다. 기존 변경은 이번 커밋에 포함하지 않는다. 원본 메인 씬·NotebookCanvas 프리팹·NotebookRoot/NotebookCanvasLayout·Build Settings와 메타 사본은 다음 로컬 경로에 보관했다.

`C:\Users\user\Documents\ANIMOL_Backups\CrayonUI_20261109_8ef3615a`

메인 씬, NotebookCanvas 프리팹, NotebookCanvasLayout 소스, Build Settings는 작업 전후 SHA-256이 같다. 메인 전후 캡처도 바이트 단위로 동일하다. 기존 메인 장식 변경은 이번 독립 씬 작업에 포함하지 않았으며, 살구색 배경·얼굴 스크롤·원화·별자리 좌표/텍스처·SpriteSwap·TMP·클릭 이벤트·Safe Area를 유지했다. `Build Fresh Scene`은 실행하지 않았다. 새 전투 씬은 Build Settings에 추가하지 않았다.

## 패치 임포트

원본: `MCP_INBOX/ANIMOL_Crayon_UI_UnityPatch_v1.zip`. 패치의 `Assets/ANIMOL/BattleCrayonUI`와 제공 메타만 병합하고 루트 `manifest.json`도 해당 폴더에 복사했다. `William_Notebook_UI.zip`을 프로젝트로 취급하거나 덮어쓰지 않았다.

- 18세트의 A/B/C PNG 54개, 선택 장면 PNG 4개: **58개 원본 PNG 해시 모두 일치**.
- 재질 36개, 셰이더 1개. **제공 메타 GUID 95개 모두 보존**.
- 원본 메타의 메시 설정을 실제 Unity API로 확인해 58개를 Full Rect로 교정했다. Single Sprite, PPU 100, Bilinear, Clamp, 알파, 압축/밉맵 없음, 빈 packing tag를 적용했다.
- 프레임별 크기·Border는 manifest 기준이다. Frame A는 Image.sprite, B/C는 대응 재질의 `_FrameB`/`_FrameC`에 연결된다.
- A/B/C의 논리 캔버스·Full Rect UV·Border는 동일하다. 원본의 실제 획 알파 경계에는 1~3px 차이가 있으며, 이를 미세한 선 변화로 유지했다. 그림을 재가공하거나 RectTransform을 흔들지 않는다.
- SpriteAtlas에 포함하지 않았다. Shader messages 0개. `_FrameRate=1.666667`, Animated `_Motion=1`, Still `_Motion=0`을 검사했다.

## 오브젝트 매핑

프리팹 루트는 `BattleCrayonUI_ArtPreview`, 아래 Canvas는 `Battle Canvas`, 배치 루트는 `Design 1080 Safe Area`다. 크레파스 장식 Image는 41개이며 모두 Raycast Target=false다. 글자·HP 채움·클릭 그래픽은 장식과 분리한다.

| 경로/역할 | 사용 자산 | 연결된 UI 참조 |
|---|---|---|
| `Battle HUD/Isol HUD`, `Curse HUD` | Frame_HUD, Icon_Isol/Curse | `isolHealthText`, `curseHealthText` |
| 각 HUD의 `Health` | Frame_Health + 정적 Fill_Isol/Curse | `isolHealthFill`, `curseHealthFill` |
| `Start Card Overlay/Start Card` | Frame_Card, 정적 Isol_CrayonPortrait | `startPanel`, `startButton` |
| `Pause Card Overlay/Pause Card` | Frame_Card, ButtonWide, Resume/Retry | `pausePanel`, `resumeButton`, `retryButton` |
| `Result Card Overlay/Result Card` | Frame_Card, ModeCard, Shield/Misblock, Retry | `resultPanel`, `resultRetryButton` |
| `Augment Card Overlay/Augment Card` | Frame_Card, 민트 ModeCard, Icon_Augment | `augmentPanel`, `augmentSelectButton`, `augmentSelection` |
| `Block Record` | Frame_Score, Shield/Misblock | `shieldCount`, `misblockCount` |
| `Pause`, `Augment` | Frame_ButtonSquare + 대응 아이콘 | `pauseButton`, `augmentButton` |
| `Reduced Motion`, `Preview Results` | Frame_TopPill | `reducedMotionToggle`, `resultsPreviewButton` |
| `Static Constellation Sky` | 정적 ConstellationSky_Dark | 배경 Image |

흰/크림 프레임, 이솔의 금색, 저주의 분홍색, 증강의 민트색을 사용한다. 새 버튼의 원래 입력 Image와 ColorTint는 유지하고, 장식 자식 뒤에 TMP를 배치했다. 비활성 HUD 조작은 CanvasGroup 알파로 구분한다. 버튼의 위치·클릭 영역·글자·HP 채움은 A/B/C 전환과 무관하다.

`CrayonUIMotion`은 재질 참조만 바꾼다. Update로 프레임을 교체하거나 런타임 Texture2D/Sprite/선/질감/AnimationClip을 생성하지 않는다. 셰이더가 제공된 그림 한 장을 선택한다. 미리보기의 일시정지는 화면 상태만 바꾸며 Time.timeScale을 변경하지 않는다.

## 검수

Unity 재컴파일: 오류 0, 컴파일 경고 0. 새 셰이더 메시지 0. 새 장식·버튼·스크립트의 필수 참조 누락 0.

- **CrayonUIAssetTests 4/4 EditMode 통과:** manifest/임포트/재질/atlas/셰이더, 프리팹 참조·입력 분리, 런타임 생성 금지, GPU A→B→C 불연속 선택 및 Still=A.
- **CrayonUIPlayTests 3/3 PlayMode 통과:** 실제 Raycast와 PointerDown/Up/Click, 누름·비활성·선택, Start/Retry·Pause/Resume·민트 증강, 기존 메인↔지도 이동, NotebookRoot의 모션 설정 전달, 세로 배치, Stencil/RectMask2D 픽셀 클립.
- PNG 캡처는 실제 uGUI를 카메라로 렌더했다. 1080×1920 및 1080×2340, 720×1280, 1080×1600을 확인했다. 후자의 안전영역은 인셋을 준 합성 Rect로 UI 구성을 검수한 것으로, 물리 기기의 노치/Device Simulator 검수와 구분한다.
- 이전 `CommercialPolishPolicyTests`는 `Legacy/PreviousProject_20261013`의 보관본이라 실행하지 않았다. 새 UI의 실제 에셋/PlayMode 검증을 위 테스트로 수행했다. 새 강한 피드백·입력 잠금·게임 시간 변경은 없다.

증거의 해시·원본 알파 경계·해상도 목록: [validation.json](Validation/CrayonUI/validation.json).

| 화면 | 캡처 |
|---|---|
| 시작 / 누름 / 정지 재질 | [시작](Validation/CrayonUI/Battle_Start.png), [누름](Validation/CrayonUI/Battle_Start_Pressed.png), [모션 줄이기](Validation/CrayonUI/Battle_Start_Still.png) |
| HUD / 일시정지 / 결과 / 증강 | [HUD](Validation/CrayonUI/Battle_HUD.png), [일시정지](Validation/CrayonUI/Battle_Pause.png), [결과](Validation/CrayonUI/Battle_Results.png), [증강 선택](Validation/CrayonUI/Battle_Mint_Augment_Selected.png) |
| 세로 안전영역 | [긴 화면](Validation/CrayonUI/Battle_Start_Tall_1080x2340.png), [좁은 화면](Validation/CrayonUI/Battle_Start_Narrow_720x1280.png), [짧은 화면](Validation/CrayonUI/Battle_Start_Short_1080x1600.png) |
| 클립 | [Stencil](Validation/CrayonUI/Mask_Stencil.png), [RectMask2D](Validation/CrayonUI/Mask_RectMask2D.png) |
| 기존 화면 보존 | [메인 전](Validation/CrayonUI/Main_Before.png), [메인 후](Validation/CrayonUI/Main_After.png), [기존 지도](Validation/CrayonUI/Existing_Constellation_Map.png) |

## 추후 연결할 항목

아래는 **현재 존재한다고 보고하는 컴포넌트명이 아니라 필요한 역할**이다. 현재 씬에는 `BattleCrayonArtPreview`만 UI 전환을 담당한다.

| 필요한 실제 컴포넌트/이벤트 | 연결 위치 |
|---|---|
| 전투 세션 Start/Retry/결과 데이터 | `startButton`, `retryButton`, `resultRetryButton`의 미리보기 리스너 교체, 결과 텍스트 |
| 전투 Pause/Resume 제어 | `pauseButton`, `resumeButton`; 실제 전투의 입력/시간 정책은 별도 구현 |
| 권위 있는 이솔/저주 HP 변경 이벤트 | 두 Filled Image의 fillAmount와 별도 TMP 값 |
| 차단/실패 기록 이벤트 | `shieldCount`, `misblockCount`, 결과 카드의 값 |
| 실제 증강 선택 데이터/효과 | `augmentSelectButton`, `augmentSelection` |
| 별 연결·0.5초 유지 판정, 별똥별 생성/이동/충돌 | UI 밖의 실제 전투 로직; 이번 씬에는 없음 |
| 메인 전투 진입 | 기존 `NotebookProjectGateway.OpenBattle`, `CanOpenBattle` 및 실제 진입/복귀 흐름 |

표시용 HP 100/100과 기록 0은 정적 예시다. 연결 판정·별똥별 속도/충돌·HP 계산·밸런스·보상은 구현하거나 변경하지 않았다. 실제 전투 PlayMode 및 모바일 GPU 성능/실기기 가독성 검수는 연결 후 남아 있다.
