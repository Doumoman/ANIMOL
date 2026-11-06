# 별길 목장 v0.25 Unity 적용 결과

검증 환경: Unity 6000.3.8f1, Android 활성 타깃, Windows Editor. 이 결과는 패키지에 동봉된 브라우저/제작 검수와 별개다.

## 적용 경로와 보존 범위

- 기존 진입: `NotebookProjectGateway.RequestStoryReplay` → 기존 `ranchStoryPrefab` → `StoryRunner` → `StoryView` / `StoryPresentationPlayer` / `StoryCuePlayer`. 이 호출 경로와 기존 프리팹 GUID를 유지했다. NotebookUI 씬의 기존 사용자 변경을 저장하거나 커밋에 포함하지 않았다.
- 적용 프리팹: `Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab` (GUID `da99f509c70d5d042836af7cfd5a8d6d`). 적용 씬: 같은 패키지의 `Scenes/RanchStoryPreview.unity`. 두 출력은 Unity PrefabUtility/EditorSceneManager로 갱신했다.
- 기존 `ranch_story.json`, `story_presentation.json`의 위치와 Resources 호출 ID를 유지했다. 기존 `packageId`, `contentVersion`, 저장 키를 보존했다. 교체 구간에서 사라진 12개 노드는 `saveNodeAliases`로 현재 노드에 연결한다. 기존 플래그의 정확한 집합 검사도 계속 적용하므로 별도 등불 입력을 조작해 복원할 수 없다.
- R01 원문 21발화의 ID·화자·문자열 SHA-256은 `6e17c0b02e7d8189c1be0f0f3880c04449f036324f3a818198d39e73b72818f7`로 유지했다. R12~R13B만 동봉 개정본으로 교체했다. R16의 물리적 장벽을 뜻하던 한 줄은 “안 돼, 아무리 가도 가까워지지 않아……!”로 고쳤다.
- 기존 퍼즐 인계 게이트 세 개와 `CURRENT_CALLS_DONE → ISOL_WANTS_RETURN → PLAYER_BLOCKS_LAMP → RETURN_PATH_USED`, R17의 `ISOL_PRESENT`, 기록문 완료 `RANCH_RULE_EPILOGUE_SEEN`을 유지했다. 기록문 여섯 문단만 패키지 원문으로 교체했다. 이 경로는 기존 스토리 재생/미리보기이며 새 퍼즐 판정이나 챕터 보상을 구현했다고 보고하지 않는다.
- 사용자 원본 `SW00_MapData.asset`와 `.meta`는 HEAD 원본과 동일하다. 다른 맵·재화·보상·빌드 씬 목록을 수정하지 않았다.

## 패키지와 아트 매핑

입력은 `MCP_INBOX/William_Starway_Ranch_Art_v025.zip`이다. 원화 22장 + 레이어 7장 + 인물 레퍼런스 4장의 SHA-256을 검사했다. 런타임 PNG 29장은 원본 바이트를 그대로 가져왔다. 그레인·질감 마스크·색 보정·이미지 절단을 추가하지 않았다. 레퍼런스 4장은 ZIP의 제작 기준으로 남기고 런타임에 불필요하게 복제하지 않았다.

새 활성 경로는 `Assets/ANIMOL/Art/Story/starway/<컷>/Resources/ANIMOL/Story/Starway/`다. 기존 Resources 기반 로더를 유지하기 위해 컷 폴더 안에 Resources를 두었다. 폴더에 버전 이름을 새로 붙이지 않았다. PNG는 Single/FullRect, mipmap 없음, 원본 크기, Bilinear, 무압축으로 가져왔다. 활성/Archive의 다른 아트는 이동하지 않았다.

전체 경로·체크섬 정본: `Assets/ANIMOL/Data/StarwayRanch/Story/ArtBindings.json`. 패키지 원본 목록과 큐는 같은 폴더의 `asset_manifest.json`, `ranch_cues.json`이다. 아래 표의 Resources 접두사는 `ANIMOL/Story/Starway/`다.

| 패키지 파일 | 런타임 ID | 표시/사용 |
|---|---|---|
| `art/R01.png` | `R01` | 완성 CG 한 장 |
| `art/R02.png` | `R02` | 완성 CG 한 장 |
| `art/R03.png` | `R03` | 완성 CG 한 장 |
| `art/R04.png` | `R04` | 완성 CG 한 장 |
| `art/R05.png` | `R05` | 완성 CG 한 장 |
| `art/R06.png` | `R06` | 완성 CG 한 장 |
| `art/R06B.png` | `R06B` | 칼국수 일화 전체 CG; 옛 패널 rect 사용 안 함 |
| `art/R07.png` | `R07` | 완성 CG 한 장 |
| `art/R08.png` | `R08` | 완성 CG 한 장 |
| `art/R08B.png` | `R08B` | 완성 CG 한 장 |
| `art/R09.png` | `R09` | 완성 CG 한 장 |
| `art/R10.png` | `R10` | 완성 CG 한 장 |
| `art/R11.png` | `R11` | 완성 CG 한 장 |
| `art/R12.png` | `R12` | 완성 CG 한 장 |
| `art/R12B.png` | `R12B` | 완성 CG 한 장 |
| `art/R13.png` | `R13` | 완성 CG 한 장 |
| `art/R13B.png` | `R13B` | 완성 CG 한 장 |
| `art/R14.png` | `R14` | 완성 CG 한 장 |
| `art/R15.png` | `R15` | 완성 CG 등록; 실제 거리 연출은 아래 3장 합성 |
| `art/R16.png` | `R16` | 완성 CG 한 장 |
| `art/R17.png` | `R17` | 완성 CG 한 장 |
| `art/R17B.png` | `R17B` | BG14_epilogue_book 별칭; 책 정지 / TMP 기록문만 스크롤 |
| `layers/R08_FrontPlate.png` | `R08_FrontPlate` | 전경 질주 배경판 |
| `layers/R08_IsolRun.png` | `R08_IsolRun` | 실제 투명 원본의 어두운 전경 질주 |
| `layers/R12_IsolReaction.png` | `R12_IsolReaction` | 실제 투명 원본의 표정 팝업 |
| `layers/R12_ToriReaction.png` | `R12_ToriReaction` | 실제 투명 원본의 표정 팝업 |
| `layers/R15_BG.png` | `R15_BG` | R15~R16 입력 전 배경 |
| `layers/R15_Friends.png` | `R15_Friends` | 전체 투명 캔버스 contain; 친구만 축소/이동 |
| `layers/R15_IsolHand.png` | `R15_IsolHand` | 고정 크기의 우하단 전경 손 |

`noodle_01`~`noodle_06`와 `CG14_noodle_comic` 호환 ID도 전체 R06B를 가리키지만 현재 노드에서는 잘린 만화 인서트를 호출하지 않는다. 오래된 저장의 인서트·줌·암전 값은 새 원화에 재적용하지 않고 대사 페이지와 진행만 복원한다. 이전 아트를 Archive에서 복원하지 않았다.

## 실제 런타임 큐

`RanchStoryStage`는 타이머/Update/코루틴을 갖지 않는다. 기존 `StoryCuePlayer.Tick(delta)`의 `ranchCut` 작업이 `Sample()`을 호출하고, 이것이 두 Overlay의 `SetCue(progress, sealOpen, viewDrift, visibility, useReducedMotion)`을 같이 갱신한다. 장시간 읽기·일시정지·앱 이탈만으로 새 사건이나 저장 플래그가 생기지 않는다.

| 실제 노드 | 이벤트 | 초 |
|---|---|---|
| `R08_B4_HOLD` | `isol_crosses_foreground` | 1.00 (지연 0.00) |
| `R12_B1_L001` | `reactions` | 0.30 (지연 0.00) |
| `R12_B2_L002` | `impact_left` | 0.10 (지연 0.20) |
| `R12_B2_L003` | `impact_right` | 0.10 (지연 0.20) |
| `R12_B2_L004` | `impact_center` | 0.10 (지연 0.20) |
| `R13B_LATCH_SEATED` | `latch_seats` | 0.20 (지연 0.00) |
| `R15_B1_L001` | `distance_increases` | 1.60 (지연 0.00) |
| `R16_B1_L001` | `enter_distance` | 0.00 (지연 0.00) |
| `R16_B4_L001` | `distance_returns_and_grasp` | 0.42 (지연 0.00) |

- R08: 전경 질주는 약 0.34초 동안 좌→우. 나머지 약 0.66초는 같은 방향의 R08B 정지 컷으로 유지한다. 원경 축소 이동을 별도 투명 인물처럼 위조하지 않았다.
- R12: 두 투명 표정만 팝업. R12B 세 발화의 좌/우/중앙 타격 도형은 각 0.10초, 진입 지연 0.20초다. 다음 대사로 넘어가는 간격은 사용자 입력에 따르며 자동 타격 연속 재생 타이머를 만들지 않았다. 화면 전체 점멸이나 시간 정지를 사용하지 않는다. 네 컷에는 초아/여울 대사·초상·실루엣이 없다.
- R13: 사냥꾼의 꾸중 3줄과 두 소년의 사과 2줄을 모두 넘긴 뒤 R13B로 간다. 수리 첫 줄 → 별도 `R13B_LATCH_SEATED` → 농담 3줄의 순서다. 빗장 확인에는 작은 합성 접촉음 하나를 연결했다. 이 소리는 제공된 녹음이 아니다.
- R15: 1.6초의 유한한 진입 큐. 친구 원본 전체를 1080×1920에 contain하고 원점 (50%,59.3%)를 사용한다. scale 0.94→0.56, 화면 Y -6%→-11%, 불투명도 1. 손은 폭 64%, 오른쪽 -2%, 바닥 -6%에서 크기·위치를 고정한다. 원/중/근 시차는 셰이더의 0.12/0.38/1.00 그대로다. `viewDrift=(0.05,-0.08)`, `visibility=0.70`.
- R16: `enter_distance`는 입력 전 먼 상태를 유지한다. 등불 게이트를 실제로 완료한 뒤에만 `distance_returns_and_grasp`가 0.42초 동안 sealOpen 0→1과 친구의 거리 복귀를 샘플한다. 끝에서 별층을 지우고 R16 손잡기 CG를 표시한다. `_SealOpen` 이름은 유지하지만 중앙 구멍/문/유리 효과는 없다.
- 다음 노드·건너뛰기·비활성·씬 이탈에서 큐를 끝 상태로 정리한다. 재진입은 해당 노드의 정착 상태를 재구성한다. 모션 감속은 별층 이동을 멈추고 정지 구도/짧은 기존 전환을 쓴다. FX가 입력이나 플래그를 확정하지 않는다.
- `RanchStoryCuePolicy`에 강도·시간과 입력 차단/전체 플래시/햅틱 없음이 정의되어 있고 정책 테스트가 연결되어 있다. `BlocksInput=false`. 반환 외에는 Strong 효과로 분류하지 않았다.

## Canvas / UI / 셰이더

기존 CanvasScaler 1080×1920, `StorySafeArea`, `StoryArtViewport`를 재사용했다. 기존 대화 UI는 조사 시 불투명에 가까운 남색 패널이었으며, 같은 프리팹의 바탕을 반투명 짙은 남색으로 바꾸고 청록/보라 림을 적용했다. 한국어 화자/본문은 기존 런타임 TMP다. 별도 신규 대화 프리팹이 이미 있었다고 가정하지 않았다.

거리 합성의 순서는 배경 → 원/중 성좌 (1,1,0) → 친구 → 근 성좌 (0,0,1) → 고정 손 → 대화 UI다. 성좌 선으로 친구 얼굴을 가리지 않는다. 세계 표현은 ArtViewport의 RectMask2D 아래이며 화자·본문은 SafeArea의 별도 형제다. R07/R11/R15/입력 전 R16/R17은 빈 하늘 쪽에 대화판을 배치해 주요 손과 얼굴을 피한다. 다른 컷은 하단 대화판을 쓴다.

`StarwayConstellationParallax.shader`는 `Assets/ANIMOL/Art/Shaders/`, `RanchConstellationOverlay.cs`는 기존 런타임 어셈블리에 추가했다. 두 RawImage의 `shaderAsset`에 직접 참조를 직렬화했다. 무텍스처(흰 기본값), UV(0,0,1,1), Raycast Target=false다. 아틀라스 부분 UV를 사용하지 않는다. 프리팹 의존성에 셰이더가 포함되고 Editor 셰이더 메시지는 0개다.

## 선택 열람

`LORE_CLOCK`/`LORE_ROOM`의 각 다섯 문장을 `RanchOptionalLore`의 선택 열람으로 연결했다. R01/R04/R08의 “기름 묻은 종이”, “열쇠 모양 종이” 버튼에서 읽고 닫는다. 번호나 일화 제목을 강제로 표시하지 않고 표식과 한 문장만 보여 준다. 이 UI는 대사 노드·퍼즐·수집·저장 플래그를 바꾸지 않으며 본편 진입 조건도 아니다.

활성 코드에서는 기존 환경 문구 열람 API/공간 배치 데이터가 발견되지 않았다. 따라서 실제 월드 오브젝트와 연결된 기존 환경 열람 경로를 재사용했다는 주장은 하지 않는다. 현재 연결은 스토리 내부의 선택 열람이며, 실제 환경 오브젝트로의 연결은 미연결 사항이다.

## 대체 컷 / 미연결 원본·음향

| 대상 | 이번 실제 결과 | 추가 원본이 필요한 부분 |
|---|---|---|
| R01 | 새 완성 CG와 원문 21발화 | 독립 배우의 반응, 하늘/찻상 분리 컷은 없음. 완성 CG의 배우를 개별 스프라이트로 움직이지 않음 |
| R02~R03, R07, R11, R14 | 새 현재 시점 CG | 손 페이드·독립 눈동자·잔디/배우별 거리 이동용 분리 원본 없음 |
| R04 | 대사 유지, 모든 비트에 새 R04를 정지 대체 컷으로 표시 | 일하는 초아·빈 컵·문턱 손의 별도 신규 컷 없음. 옛 배경/소품을 최신 아트인 것처럼 혼합하지 않음 |
| R05~R06 | 새 정지 CG + 전체 R06B 일화 | 앉기·웃음 등의 독립 배우 애니메이션 없음 |
| R08 | 실제 전경 질주, R08B 역방향 정지 컷 | R08B에 이솔이 이미 그려져 있으므로 원경 축소 이동용 빈 배경/역방향 인물은 미연결 |
| R09~R10 | 새 구출 전후 CG | 발 미끄러짐/실제 손 접촉의 별도 단계 이미지·독립 손 없음 |
| R12~R13B | 개정 대사, 두 표정, 암전 원화, 국소 타격, 꾸중→수리→빗장→농담 | 원화 속 몸·멧돼지·빗장을 개별 스프라이트로 취급하지 않음 |
| R15~R16 | 실제 3장 합성, 입력 전 거리 유지, 별층 전체 귀환, R16 손잡기 CG | 완성 R16 몸/잔디 자체의 자동 굴절/관절 손잡기 애니메이션 없음 |
| R17 | 새 네 사람 CG와 고정 책, TMP 기록문 | 독립 손/배우 움직임 없음 |
| 음향 | 작은 빗장 합성 접촉음 | 패키지에 음성/음악/생활음 녹음은 없음. O.S. 호명·기억 잔향은 기존 대사 텍스트로 유지되며 실제 음성 재생은 미연결 |

## 손 / 팔레트 검수

`Validation/StarwayRanchArt/Hands_200pct_1~3.jpg`는 원본 일부를 정확히 200%로 확대한 검수용 파생 이미지다. 런타임 PNG에는 적용하지 않았다. R02의 흐려지는 손, R03·R07의 뻗은 손, R09의 난간 그립, R11의 전경·바닥 손, R14 이솔 손, R15 CG/분리 손, R16 손목 그립, R17 어깨/바닥 손을 확인했다.

펼친 주요 손은 엄지와 나머지 네 손가락으로 읽히며 돌출된 여섯째 손가락·복제·융합은 보이지 않았다. R09 난간과 R16 손목·R17 어깨 그립은 가려진 엄지를 억지로 노출하지 않았다. 레퍼런스의 초아 주황/초록, 이솔 흰 스카프/호박색 눈, 토리 파란 모자/뿔, 여울 분홍 리본을 대조했다. 가족/사냥꾼은 인간형 얼굴과 동물 귀·뿔을 유지한다. 실제 기기에서의 알파 경계/화질은 아래 미검증 범위를 따른다.

## 실행 검증

- 스토리 EditMode: 57/57 통과. 새 검사는 원문 21발화 해시, 수정 대사 순서, 12개 저장 별칭, 등불 입력, 별도 기록문 완료, 중앙 큐 정책, 실제 Sprite 로드를 포함한다.
- `ArtAssetTests`: 4/4 통과. 활성 빌드 씬 의존성과 기존 표현 정책을 확인했다.
- PlayMode: 5/5 통과. `RanchStoryPreviewPlayTests`의 전체 노드 진행(R01~R17), 명시적 등불 입력, 저장 후 기록문 복원, 마지막 기록 유지/입력, 1080×2400 비율을 확인했다. `RanchArtIntegrationPlayTests`는 R08/R12/R15/16 샘플·정지·비활성/재진입·모션 감속·선택 열람을 검사했다. `ArtQaPlayTests`는 1080×1920과 작은 세로 GameView에서 통과했다. 테스트는 기존 플레이어 저장 키를 백업/복원한다.
- Mask/RectMask2D: 실제 256×256 RenderTexture에서 별 픽셀이 보이고 경계 밖 알파가 0임을 두 방식 모두 확인했다. sealOpen=1에서 전체 별층 알파가 0임을 확인했다.
- 실제 1080×1920 캡처: `Validation/StarwayRanchArt/R01.png`, `R13B.png`, `R15_start.png`, `R15_far.png`. 대화판 가독성과 주요 얼굴/손 위치를 시각 검수했다. 저장 결과와 노드별 화자/그림/큐/플래그 목록은 같은 폴더의 JSON에 기록했다.
- 과거 `CommercialPolishPolicyTests`는 Legacy 격리 상태라 실행 대상에 포함하지 않았다. 이번 활성 큐 정책/아트/스토리 테스트의 실제 실행과 구분한다.
- Android 빌드: 확인 중. 실기기 `adb devices -l`은 연결 장치가 없어 설치/FPS/발열/기기 알파 경계 검증을 실행하지 못했다. Editor 테스트 통과를 모바일 성능 통과로 해석하지 않는다.

초기 작업 트리의 다른 수정/삭제/미추적 파일과 보호 맵을 이번 커밋에 섞지 않는다. 작업 단위 커밋과 최종 Android 빌드 결과는 완료 시 갱신한다.
