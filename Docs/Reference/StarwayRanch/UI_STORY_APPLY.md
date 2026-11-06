# 별길 목장 v0.27 — 네온 대화 UI 적용

## 포함 범위

이 폴더는 실제 Unity 프로젝트에 가져올 **대화 표시용 소스**다. 기존 씬, 대화 컨트롤러, 입력 시스템, 저장 데이터는 수정하지 않는다. Unity에서 컴파일·Play Mode 실행을 마친 프로젝트나 `.unitypackage`는 아니다.

| 파일 | 역할 |
|---|---|
| `StoryNeonPanel.cs` | 기본 UGUI 머티리얼로 반투명 남색 면, 얇은 밝은 테두리, 작은 선택적 후광을 생성 |
| `StoryDialogueData.cs` | `JsonUtility`용 루트/씬/대사 배열 모델과 문자열 이벤트 |
| `StoryDialogueView.cs` | Galmuri14 TMP 타이핑, 두 단계 다음 입력, 우상단 스킵, 명시적 행동 확인, 연출 이벤트 연결 |
| `Editor/CreateStoryDialoguePrefab.cs` | 기존 에셋을 덮어쓰지 않는 프리팹 생성 메뉴 |
| `../data/ranch_dialogue.json` | 17개 씬의 임시 한국어 대사. Unity에선 TextAsset으로 지정 |

기존 `RanchConstellationOverlay.cs`, `StarwayConstellationParallax.shader`는 그대로 사용한다. 대화 뷰가 이 효과를 자동으로 켜거나 진행도를 정하지 않는다.

## 1. 가져오기와 프리팹 생성

1. 프로젝트의 `Assets/ANIMOL/Story/`에 위 런타임 C# 3개를 복사한다.
2. `CreateStoryDialoguePrefab.cs`는 그 아래 `Editor/`에 둔다. 런타임 빌드에 Editor API를 넣지 않는다.
3. UGUI와 TextMeshPro가 있는 프로젝트에서 기존에 사용하는 **Galmuri14 TMP Font Asset**을 Project 창에서 선택한다. 새 폰트를 강제로 만들지 않고 그 에셋을 재사용한다. 에셋이 아직 없다면 동봉한 `fonts/Galmuri14.ttf`로 TMP Font Asset을 만든 뒤 선택한다. 기본 TMP 폰트로 자동 대체하지 않는다.
4. `ranch_dialogue.json`을 프로젝트 Assets 내부에 복사한다.
5. **정확한 메뉴:** `Tools → ANIMOL → Story → Create Neon Dialogue Prefab`

폰트 선택 우선순위는 Project에서 선택한 Galmuri14 TMP 에셋 → 선택한 UI에 이미 연결된 Galmuri14 → 프로젝트에 유일하게 존재하는 Galmuri14다. 이름을 바꾼 에셋도 폰트 family/source 이름으로 확인한다. 후보가 여러 개면 사용할 에셋을 직접 선택하라는 안내 후 생성을 중단한다.

생성 위치는 `Assets/ANIMOLGenerated/StoryUI/StoryDialogueUI.prefab`이다. 이미 있으면 Unity가 `StoryDialogueUI 1.prefab`처럼 새 경로를 선택한다. 생성 과정은 프리팹만 저장하며, 열려 있는 씬이나 EventSystem을 바꾸지 않는다.

## 2. Canvas와 가독성

생성된 프리팹을 **기존 전체 화면 Canvas 아래**에 배치하고 RectTransform을 전체 스트레치한다.

| 항목 | 기준 |
|---|---|
| CanvasScaler | Scale With Screen Size |
| Reference Resolution | 1080 × 1920 |
| Match | 0.5를 시작값으로 사용하고 실제 기기에서 검토 |
| 대화 패널 | 좌우 44, 안전 영역 하단 54, 높이 422 기준 픽셀 |
| 화자 | Galmuri14 45 px, Regular |
| 본문 | Galmuri14 45 px, 고정 크기, `<line-height=135%>` |
| 본문 여백 | 좌우 38, 본문 상단 97 |
| 우상단 스킵 | SafeArea 안쪽 오른쪽 44·위 42, 104×88 입력 영역, `>>`만 표시 |
| 좌상단 | R01 등 장면 ID 표시 없음 |
| 테두리 | 2 px, `#ECEEE7`, 작은 균일 곡률 |
| 본문 색 | `#F5F3E9` |
| 후광 | `#B7D5EE`, 4 px 안의 낮은 알파 2개 띠; 선택적으로 완전히 끌 수 있음 |
| 바탕 | `#0D152B`, 알파 0.73; 그림을 볼 수 있는 투명도 |

폰트는 Speaker, Body, Progress, Next, ActionButton/Label, SkipButton/Label 모두 같은 **Galmuri14**를 사용한다. `Galmuri14 Font` 필드에 기존 에셋을 지정하거나 `view.SetFont(existingFontAsset)`로 한 번에 연결한다. 합성 Bold/Italic은 사용하지 않는다. 스킵 버튼은 `>>`만 보이며 “건너뛰기”라는 글자를 그리지 않는다. 참고 화면의 `Display 1 / No cameras rendering` 문구는 UI가 아니므로 포함하지 않았다.

SafeArea는 `Screen.safeArea`를 읽어 대화 조작 영역만 안쪽으로 옮긴다. 이 계산은 프리팹 루트가 전체 화면 Canvas를 채운다는 전제다. 배경 일러스트는 SafeArea 바깥에 두어 화면 끝까지 보이게 한다. 분할 화면·월드 스페이스 Canvas에는 기존 프로젝트의 안전 영역 처리기를 사용한다.

대사는 자동 축소하지 않는다. 현재 임시 대사는 명시적 줄바꿈을 포함하며 4줄 이내를 목표로 한다. Unity 프리팹은 최대 4줄을 위한 높이를 고정하고, HTML은 대사 길이에 따라 패널 높이가 늘어난다. 실제 TMP 한글 폰트에서 넘침이 생기면 대사를 나누고, 자간/폰트 크기를 무작정 줄이지 않는다. 사용자 정의 폰트의 실제 줄 수와 기기별 가독성은 Unity에서 확인해야 한다.

HTML 미리보기는 내용에 따라 패널 높이를 조정하며, 이 Unity 기본 프리팹은 4줄까지 안정적으로 담을 고정 높이 422를 사용한다. 색상 토큰은 같다. 프로젝트의 대화 길이가 확정되면 기존 레이아웃 시스템에 맞춰 높이만 조정할 수 있다.

## 3. 데이터 연결과 시작

프리팹의 StoryDialogueView에 `Dialogue Json` TextAsset을 지정한다. 별도 파일 경로나 Resources 폴더는 필요 없다.

- 개별 씬 실행: `view.ShowScene("R01")` 또는 `view.OpenScene("R01")`.
- 코드에서 공급: `view.SetData(data)` 후 `view.OpenScene("R01")`.
- 다른 JSON 읽기: `view.LoadJson(textAsset)`.
- 다시 보기: `view.Replay()`는 현재 씬 처음부터 UI 상태를 초기화한다.
- 닫기: `view.Close()`는 타이핑, 행동 대기, 텍스트, 클릭 영역을 해제한다. 저장 상태를 초기화하지 않는다.
- 다음 씬: `view.OpenNextScene()`는 현재 씬이 끝난 뒤에만 동작한다.

Inspector의 **Play First Scene On Start**는 기본 꺼짐이다. 임시 대사만 연속 확인하려면 켜고, **Advance Between Scenes**도 켠다. 실제 스테이지 연결에서는 둘 다 끄고 기존 컨트롤러에서 원하는 시점에 `ShowScene`을 호출한다. 비활성 GameObject/컴포넌트를 먼저 활성화한 뒤 씬을 연다.

일반 대화 패널은 타이핑 도중 첫 입력에 해당 대사를 전부 펼치고, 이후 입력에 다음 대사로 간다. UTF-16 문자열을 잘라 붙이지 않고 TMP의 `maxVisibleCharacters`를 사용한다. 한글/서로게이트 문자가 중간 바이트로 잘리는 방식은 피하지만, 모든 복합 이모지의 글자군 단위 표시는 TMP 버전에 따라 별도 확인이 필요하다.

### 우상단 `>>` 버튼의 동작

`SkipCurrentScene()`은 **현재 장면의 남은 대사**를 넘긴다. 데이터 전체나 챕터 저장을 한 번에 끝내는 동작은 아니다.

- 남은 줄에 필수 action이 있으면 첫 action 줄까지만 이동해 본문을 전부 펼친다. `block_lamp`와 `chapter_complete`는 여전히 별도 버튼/호스트 확인을 기다린다.
- 이미 action 줄에 있으면 타이핑만 즉시 끝내고 그 줄에서 기다린다. 반복해서 `>>`를 눌러도 통과하지 않는다.
- 남은 action이 없으면 기존 `On Scene Finished` 경로로 현재 장면을 끝낸다. `Advance Between Scenes`가 켜진 미리보기에서는 다음 장면을 열고, 실제 게임에서는 기존 호스트가 다음 스테이지를 결정한다.
- 생략한 줄들의 Shot/Cue/Emotion을 차례로 호출하지 않는다. 이동한 대상 줄의 이벤트만 발생한다. 진행 플래그와 보상은 자동 기록하지 않는다.
- `On Scene Finished`와 다음 대상 줄의 `On Line Shown`에 기존 컨트롤러의 예약 FX/팝업 정리를 연결한다. 빠르게 넘긴다고 이전 멧돼지 타격 큐가 뒤늦게 재생되지 않아야 한다.

## 4. 기존 연출 컨트롤러에 연결할 이벤트

모든 이벤트는 문자열 한 개를 받는 Inspector UnityEvent이며, 실제 일러스트와 게임 상태의 책임은 기존 컨트롤러에 있다.

| 이벤트 | 전달값 | 연결할 일 |
|---|---|---|
| On Scene Entered | `R01` 등 | 씬별 컷신 초기화 |
| On Line Shown | `R01_001` 등 | 현재 대사 추적, 배우 식별은 `CurrentLine.speaker` 참고 |
| On Shot Requested | `R12A`, `R13B` 등 | 배경/레이어 전환; 같은 shot 반복 시 불필요하게 재시작하지 않기 |
| On Cue Requested | `boar_charge` 등 | 지정된 단발 연출 실행; 빈 값은 단발 상태 정리에 사용 가능 |
| On Emotion Requested | `frightened`, `relieved` 등 | 현재 화자 표정 선택; `CurrentLine.speaker`와 함께 사용 |
| On Action Requested | `block_lamp` | 실제 게임 행동을 요청; 성공 여부 검증 전 자동 진행 금지 |
| On Action Confirmed | 확인된 action | 호스트가 조건/저장을 처리할 연결점 |
| On Scene Finished | 종료한 scene ID | 다음 스테이지/씬 연결 |
| On Sequence Finished | 마지막 scene ID | 데이터 전체 재생 종료 알림 |
| On Closed | 닫은 scene ID | 컷신의 일시적 셰이더/팝업/음향 상태 정리 |

한 줄 진입 시 Scene(새 씬일 때) → Line → Shot → Cue → Emotion 순서다. 빈 cue가 다음 줄에서 전달되었다고 지속 중인 별의 침식 배경을 반드시 끄는 것은 아니다. **지속 배경은 shot/호스트 타임라인, 단발 이펙트는 cue**로 분리한다.

표정 이벤트는 화자 한 명을 대상으로 한다. 이솔 침식 중 세 친구의 공포·당황은 shot의 전체 배우 상태로 설정한다. 마지막 R17에서 토리의 `relieved`는 **눈물 없이, 긴장이 풀린 눈썹과 어깨, 안도한 작은 미소**다. 다른 인물의 `tearful`을 토리에게 일괄 복사하지 않는다.

### 멧돼지 장면 연결

| shot/cue | 권장 연결 |
|---|---|
| R12A / rustle | 어두운 수풀의 작은 이동; 사람은 화면에 보이지 않고 두 남자의 목소리만 들림 |
| R12A / eyes_glint | 수풀 속 눈 두 점만 반짝임 |
| R12 / boar_reveal | 위협적인 멧돼지 실루엣 전체 공개 + 이솔·토리 놀란 얼굴 팝업 |
| R12B / boar_charge | `layers/R12_BoarRush.png` 전경 확대 → 암전 |
| R12B / impact_left, impact_right, impact_final | 암전 위 서로 다른 위치의 짧은 타격 도형·효과음; 대사는 읽을 수 있게 유지 |
| R12C / boar_settle | 새 사후 컷, 헐떡이는 두 사람과 멧돼지의 표식 인지 |
| R13 → R13B | 사냥꾼의 실제 분노와 꾸중 → 우리 수리 중 둘의 작은 빈정거림 |

위 cue는 제공 뷰에서 자동 애니메이션으로 구현하지 않는다. 기존 타임라인/셰이더 컨트롤러에 연결한다. Reduced Motion에서는 전경 돌진·화면 흔들기를 정지 컷 전환과 짧은 밝기 변화로 대체한다.

## 5. 행동 잠금과 저장 조건

**block_lamp** 줄은 별도 `등불 가리기` 버튼을 보인다. 버튼 클릭은 On Action Requested만 호출하고 대사는 그대로 기다린다. 기존 입력 시스템에서 등불 가리기가 실제 완료되었을 때 `view.ConfirmAction("block_lamp")`를 호출한다. Inspector 문자열 이벤트로 연결하려면 void 래퍼 `ConfirmActionFromHost(string)`를 사용한다. 잘못된 action, 타이핑 중 입력, 일반 패널 클릭으로 잠금이 해제되지 않는다.

**chapter_complete** 줄은 대사를 펼친 뒤 별도 `계속` 버튼을 보인다. 그 버튼을 직접 눌러야 action이 확인된다. 보통 패널 클릭으로는 끝나지 않는다. On Action Confirmed에서 호스트가 필요한 조건을 확인하고 챕터 저장을 처리한다. 버튼은 저장의 성공 자체를 판단하지 않는다.

기존 진행 조건 `CURRENT_CALLS_DONE → ISOL_WANTS_RETURN → PLAYER_BLOCKS_LAMP → RETURN_PATH_USED`와 `ISOL_PRESENT`, `RANCH_RULE_EPILOGUE_SEEN`, `CHAPTER_R_COMPLETE`는 이 뷰에 하드코딩하지 않았다. 기존 컨트롤러가 관리하며, 이 뷰의 Replay로 이 플래그들을 자동 재지급하면 안 된다. 컷신 다시 보기 모드에선 호스트가 저장 이벤트를 무시하도록 분리한다.

이벤트 리스너가 Close/OpenScene을 호출하면 이전 줄의 후속 이벤트/타이핑은 중단된다. 닫기에서 배경/오버레이를 복원하려면 On Closed에 기존 컨트롤러의 정리 메서드를 연결한다.

## 6. 정적인 대화와 접근성

- 이 UI는 화자 스프라이트 바운스를 자체 실행하지 않는다. 기존 허용 씬 R01, R05, R06, R08, R10, R13, R17에서만 호스트가 적용한다.
- `SetReducedMotion(true)`는 대사를 즉시 펼치고 현재 타이핑도 끝낸다. 화면/배우 모션은 호스트에도 같은 설정을 전달해야 한다.
- `SetHaloEnabled(false)`로 테두리 후광을 끈다. 선과 반투명 바탕은 유지한다.
- 각 네온 패널은 기본 설정에서 253개 정점, 최대 cornerSegments 16에서도 477개 정점이다. 개별 패널은 UGUI 정점 한도보다 충분히 작다. 실제 배칭 수는 Canvas/Mask/머티리얼 구성에 따른다.
- 장식 패널과 모든 TMP 라벨은 raycast를 받지 않는다. 투명 hit Image 두 개만 입력을 받으며, 닫힐 때 CanvasGroup이 raycast와 상호작용을 해제한다.
- MaskableGraphic 기본 경로를 사용해 Mask/RectMask2D 및 CanvasGroup 알파와 호환되도록 작성했다. 프로젝트의 커스텀 UI 머티리얼을 덮어쓰지 않는다.

## 7. 프로젝트에서 확인할 항목

소스/JSON 구조 및 게이트 흐름은 검토했으나, 이 작업 환경에는 Unity 실행기와 참조 어셈블리가 없어 **Unity 컴파일 및 Play Mode 검증은 실행하지 않았다**.

가져온 뒤 실제 한글 폰트에서 9:16의 3~4줄 가독성, 기기 안전 영역, 첫 탭 펼치기/두 번째 탭 진행, 우상단 스킵과 action 정지, block_lamp 외부 확인, 마지막 계속 버튼, 닫고 다시 보기, Mask 하위 배치, CanvasGroup 알파, 기존 EventSystem 입력을 확인한다. 새 EventSystem을 중복 생성할 필요는 없다.
