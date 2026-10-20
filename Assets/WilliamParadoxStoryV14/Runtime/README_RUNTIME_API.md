# 별길 목장 v14 · Story runtime 계약

Unity 6 / UGUI / TextMeshPro용 **대화 전용 미리보기** 소스다. 이 폴더는 프로젝트 패키지·입력 설정·폰트·캠페인 저장을 바꾸지 않는다. 이 작업 환경에는 Unity Editor 및 C# 컴파일러가 없으므로 실제 Unity 컴파일, Play Mode, 디바이스 검증을 완료한 상태로 표현하면 안 된다.

## 파일 책임

| 파일 | 역할 |
|---|---|
| `StoryData.cs` | JsonUtility용 flat DTO와 로컬 미리보기 저장 DTO |
| `StoryGraph.cs` | ID·참조·선형 경로·flag 도달성 검증, 순수 진행 모델 |
| `StoryRunner.cs` | 수동 진행, 입력 debounce, pause/focus, 저장, 미리보기 hook |
| `StoryView.cs` | 독립 artwork/dialogue/book UI, Resources sprite 캐시 |
| `StoryCuePlayer.cs` | 전달된 delta만 사용하는 dim/zoom/shake/popup/transition/inset |
| `StoryCreditsPlayer.cs` | 고정된 책 + 본문만 스크롤 / 감소 모션 문단 읽기 |
| `StoryCoverImage.cs` | 세로 CG의 균일한 cover-fit. 비율을 늘이지 않음 |
| `StorySafeArea.cs` | UI safe-area anchors를 한 번 적용 |
| `StoryInputBootstrap.cs` | 새 입력 모듈을 택한 경우에만 기본 UI actions를 reflection으로 연결 |
| `StorySaveStore.cs` | 패키지별 PlayerPrefs에만 쓰는 별도 미리보기 저장 |

## 가져오기와 리소스

기본 Runtime asmdef 이름은 `WilliamParadox.Story.Runtime`, 직접 참조는 `Unity.ugui`, `Unity.TextMeshPro`다. 기존 프로젝트에 두 패키지가 있어야 한다. Input System 어셈블리를 직접 참조하지 않는다.

Editor Builder의 루트는 `Assets/WilliamParadoxStoryV14`다. `v14/data/ranch_story.json`을 `Resources/WilliamParadox/V14/Story/ranch_story.json`으로 복사한다. 그림의 Resources 경로는 JSON의 `assets[].resourcePath`와 정확히 같아야 한다. 예: `WilliamParadox/V14/Art/Scenes/R01`. 확장자는 쓰지 않는다. 실제 파일 매핑은 별도 `import_manifest.json`을 사용하며, `StoryPackage`에는 로컬 절대경로를 넣지 않는다.

일반 CG는 Sprite Single을 먼저 읽고, 없으면 Texture2D에서 FullRect sprite를 만든다. `rect != null`인 패널은 Texture2D에서 그 pixel rect만 Sprite.Create한다. rect의 원점은 **좌측 하단**, width/height는 양수, pivot은 0~1이다. whole texture는 `rect=null`이다. runtime이 atlas 원본 파일을 편집하거나 alpha를 새로 만드는 일은 없다.

이 런타임은 완성된 세로 CG 한 장과 선택적인 comic inset을 표시한다. 분리된 캐릭터 파츠를 자동 조립하거나 skeletal animation을 재생하지 않는다.

## UI 결합

참조 Canvas는 1080×1920이다. SafeArea를 중복 적용하지 않는다. `ArtViewport`에는 RectMask2D를 두고, 그 안의 `ArtworkRoot`만 확대·흔들림을 받는다. 긴 화면에서는 `StoryArtViewport`가 원화 영역을 상단 9:16으로 제한해 가장자리의 얼굴과 손을 보존한다. 남은 하단은 남색 바탕이며 대사와 버튼은 독립 루트다. `StoryCoverImage`는 **artImage에 부착**, `viewport`는 ArtViewport, `image`는 artImage로 연결한다. 이 컴포넌트는 source aspect를 유지하고 미세한 넘침만 viewport가 자른다.

국수 comic inset은 상단 `insetRoot`의 고정 슬롯 안에서 preserveAspect contain으로 표시한다. 권장 슬롯은 논리 880×820, 중앙 기준 y=260이다. 중요한 얼굴·손이 있는 하단 대사 영역을 덮지 않도록 Builder에서 확인한다. 원본 atlas가 가로/세로 어느 비율이든 패널 자체를 늘이지 않는다.

`creditsViewport`는 RectMask2D가 있는 고정 책 본문 영역이다. `creditsContent`는 anchor/pivot (0.5,1), `creditsText`는 그 안에 stretch한다. 배경 책 artImage는 스크롤하지 않는다. status bar, 다음/일시정지/다시시작/감소모션/계속 버튼은 dialogueGroup과 creditsGroup 밖에 둔다. 모든 그림과 TMP_Text는 raycastTarget=false, 버튼만 입력을 받는다. 하단 다음 버튼 앞에 다른 버튼이 겹치지 않도록 배치한다.

`StoryView`의 public 필드는 Builder가 직접 연결한다:

```csharp
RectTransform artworkRoot;
CanvasGroup artworkGroup;
Image artImage, dimImage;
CanvasGroup dialogueGroup;
TMP_Text speakerText, bodyText, statusText;
Button advanceButton, gateButton, pauseButton, restartButton, creditsContinueButton;
TMP_Text gateButtonLabel, pauseButtonLabel, advanceButtonLabel;
Image insetImage;
CanvasGroup insetGroup;
RectTransform insetRoot;
CanvasGroup creditsGroup;
RectTransform creditsViewport, creditsContent;
TMP_Text creditsText;
Toggle reducedMotionToggle;
```

`StoryRunner.Awake()`가 위 버튼과 toggle listener를 붙인다. Builder는 동일 onClick 영구 listener를 다시 추가하면 안 된다. `StoryInputBootstrap`은 EventSystem과 같은 오브젝트에 붙인다. Builder가 설치된 InputSystemUIInputModule을 골랐을 때만 `preferNewInputSystem=true`를 설정한다. 기존 StandaloneInputModule 경로는 그대로 둔다. 새 모듈은 public `AssignDefaultActions()`를 runtime reflection으로 호출하므로 transient Editor action asset에 의존하지 않는다.

## 공개 API

Namespace: `WilliamParadox.Story`.

```csharp
// StoryRunner inspector inputs
public TextAsset storyJson;
public string defaultResourcePath = "WilliamParadox/V14/Story/ranch_story";
public StoryView view;
public bool restoreOnStart = true, reducedMotion;

// Runner controls
public void StartStory(bool restore);
public void Advance();
public void ActivateGate();
public void ContinueCredits();
public void TogglePause();
public void SetReducedMotion(bool value);
public void RestartStory();
public void SaveCheckpoint();
public bool JumpTo(string nodeId);
public bool HasFlag(string flag);

// Read-only inspector / integration access
public StoryPackage Package { get; }
public StoryNode CurrentNode { get; }
public string CurrentNodeId { get; }
public string CurrentSceneId { get; }
public bool IsPaused { get; }
public IReadOnlyList<string> ValidationErrors { get; }

// Validation
public static List<string> StoryGraph.Validate(StoryPackage package);
public static bool StoryGraph.TryCreate(StoryPackage package,
    out StoryGraph graph, out List<string> errors);
```

`JumpTo`는 Debug.isDebugBuild일 때만 동작한다. 필요한 flag를 만들지 않으며, 현재 flag 상태와 일치하는 구간으로만 이동한다. 구조 확인용이다. R16 버튼을 누르지 않고 R17로 뛰어가게 하는 API가 아니다.

`onNodeChanged(string nodeId)`, `onCompleted()`는 선택적인 **미리보기 hook**이다. 라이브 캠페인 보상, 퍼즐 성공, 인벤토리 지급에 연결하지 않는다. `onCompleted`는 최종 gate를 거친 end에서 한 번 호출한 뒤 저장한다. 완료된 end 저장을 재개해도 다시 호출하지 않는다.

## JSON 스키마

모든 DTO는 `[Serializable]` public field이며 JsonUtility에서 dictionary나 polymorphism을 요구하지 않는다.

| DTO | 필드 |
|---|---|
| StoryPackage | schemaVersion=14, packageId, contentVersion, startNodeId, assets[], nodes[] |
| StoryAsset | id, resourcePath, kind, rect? |
| StorySpriteRect | x, y, width, height, pivotX=.5, pivotY=.5 |
| StoryNode | id, sceneId, beatId, kind, speaker, text, artId, nextId, cues[], setFlags[], requiredFlags[], gateId, gateLabel, credits[] |
| StoryCue | type, target, duration, delay, amount, assetId, x, y, scale=1 |

node kind는 `line`, `cue`, `gate`, `credits`, `end`다. line에는 text가 있어야 한다. 모든 non-end에는 유효한 nextId가 필요하다. 이번 패키지는 한 경로이므로 중복 ID, 순환, 도달할 수 없는 노드도 거절한다. 이야기 속 반복은 서로 다른 node ID로 작성한다.

| cue | 값과 동작 |
|---|---|
| dim | amount 0~1, 검은 overlay alpha |
| zoom | amount >0, target의 균일 scale |
| shake | amount ≥0 논리 px, 마지막에 원래 위치로 복원 |
| popup | scale을 시작값으로 target을 1배까지 확대 |
| transition | artworkGroup의 alpha를 0→1로 전환 |
| inset | assetId 필수, x/y는 중앙 원점 논리 px(+y 위), scale은 insetRoot scale, duration은 fade-in |
| hideInset | inset fade-out 후 숨김 |

target은 빈 값/`artwork` 또는 `inset`이다. inset x=y=0은 Builder의 기본 위치를 유지한다. 모든 duration/delay는 음수가 될 수 없다. cue는 node에 들어올 때 시작하고 **자동으로 다음 node로 이동하지 않는다**.

## 진행 규칙과 R16

대사는 기본으로 즉시 전부 표시한다. 타자 효과를 저장하거나 복원하지 않는다. 효과 재생 중 다음 버튼을 누르면 그 효과만 끝까지 정리하고 멈춘다. 다음 입력에서만 다음 node로 간다. 동일 프레임 입력과 0.15초 이내 중복 입력을 막는다.

일반 line/cue/credits/end의 setFlags는 **진입 시** 적용한다. gate의 setFlags는 **ActivateGate 성공 시**에만 적용한다. requiredFlags를 만족하지 못한 node에는 진입할 수 없다. `Advance()`는 gate와 credits를 통과하지 못한다.

R16의 의도는 다음처럼 별도 단계로 유지한다.

1. 이솔의 돌아가고 싶다는 대사를 읽고 다음으로 넘긴다.
2. WISH_CONFIRMED cue가 ISOL_WANTS_RETURN을 기록한다.
3. 별도 `BLOCK_LAMP` gate의 **등불 가리기** 버튼을 누른다. 이때 PLAYER_BLOCKS_LAMP를 기록한다.
4. 다음 node에서 이솔 자신의 손 뻗기/움직임을 본다.

일반 다음 버튼이 플레이어 행동을 대신하지 않는다. R07/R11/R14의 PREVIEW_ONLY gate는 퍼즐 연결 지점의 **대화 미리보기 계속** 버튼이다. 실제 퍼즐 완료나 보상은 쓰지 않는다.

## pause와 저장

효과와 책 스크롤은 `StoryRunner.Update`가 넘긴 `Time.unscaledDeltaTime`만 사용한다. 수동 pause, focus loss, app pause 동안 delta를 넘기지 않고, 돌아온 첫 프레임의 벽시계 시간도 버린다. 프로젝트 전체 `Time.timeScale`을 바꾸지 않는다.

저장 키는 `WilliamParadox.Story.V14.<packageId>`다. package/schema/contentVersion, current node, flags, 완료 hook 여부, 감소 모션 설정, 정적인 그림 상태, 책 읽기 위치를 저장한다. flag 목록을 해당 node까지의 정상 경로와 대조하므로 미래의 PLAYER_BLOCKS_LAMP 같은 값을 외부에서 끼워 넣은 저장은 재개하지 않는다. JSON 파손, 버전 차이, 없는 node, NaN, 잘못된 그림 참조는 안전한 첫 node로 돌아간다. cue가 자동 완료되는 프레임에도 즉시 저장한다. 저장 복원은 마지막 시각 상태를 먼저 적용한 뒤 현재 node cue를 즉시 최종 상태로 정리한다. 따라서 fade 시작(alpha=0)이나 지연 이전의 checkpoint라도 패널이 빠지지 않고, 흔들림·전환을 다시 재생하지 않는다.

R17의 ISOL_PRESENT / RETURN_PATH_USED는 크레딧에 진입하기 전에 별도 RESCUE_SAVED cue에서 저장된다. 크레딧을 닫거나 일시정지해도 구조 완료가 취소되지 않는다. 기록 완료 flag는 마지막 최종 계속 gate에서만 기록한다.

## 역사책 크레딧

기본 모드에서는 여섯 원문 문단이 고정 책 위에서 초당 36 논리 px로 올라간다. 다음 버튼은 **기록 일시정지 / 기록 재개**다. 마지막 문단이 중앙에 도착하고 최소 3초를 머문 뒤에만 별도 계속 버튼이 나타난다. 다음 버튼 연타로 장을 종료할 수 없다.

감소 모션에서는 한 문단씩 고정 표시하고 **다음 기록** 버튼으로 읽는다. 마지막 문단에서는 버튼을 비활성화하고 3초 후 별도 계속 버튼을 보여준다. 모드 전환은 첫 문단부터 다시 읽게 하며, 지나치지 않은 기록을 완료 처리하지 않는다. 계속 버튼 뒤 PREVIEW_FINAL_CONTINUE gate에서는 책과 마지막 문단을 유지한다.

## 검증 상태와 테스트

Runtime validator는 asset/node IDs, 참조, 기본 필수 필드, timing의 유한값/음수, cue 종류, 선형 경로, gate 의도와 flag 도달성을 검사한다. 실제 파일 존재·PNG 크기/alpha·TMP font coverage는 Editor importer의 책임이다.

`../Tests/EditMode`에는 NUnit 그래프/게이트/저장/외부 flag 차단 및 cue freeze/분리-root 테스트 소스가 있다. 기본 Builder 복사 대상에는 테스트 폴더가 없다. Unity Test Framework가 있는 프로젝트에 테스트 폴더를 복사한 뒤 Edit Mode에서 실행한다. 테스트 asmdef는 Editor 전용, UNITY_INCLUDE_TESTS 제약과 optional TestAssemblies 참조를 사용한다. 실제 이 환경에서 테스트를 실행한 것으로 기록하지 않는다.

현재 완료한 로컬 확인: ranch_story의 210 node / 28 asset을 독립 Python 경로 검사로 대조했고 누락 참조·순환·미도달 flag는 없었다. C# 파일의 delimiter 및 asmdef JSON도 정적 검사했다. 이 확인은 C# 실행 또는 Unity Play Mode를 대신하지 않는다.

Unity 공식 근거: [Assembly definition file format](https://docs.unity3d.com/kr/6000.0/Manual/assembly-definition-file-format.html), [Adding tests to a package](https://docs.unity3d.com/kr/current/Manual/cus-tests.html).
