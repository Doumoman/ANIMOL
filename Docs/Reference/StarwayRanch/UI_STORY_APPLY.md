# 별길 목장 v0.28 — 하단 대화와 두 시점의 기록

## 제공 범위

**UGUI + TextMeshPro 소스와 프리팹 생성 도구**의 제공본 설명이다. ANIMOL 프로젝트에는 기존 호스트에 통합했으며, 아래의 범용 가져오기 경로 대신 `Assets/ANIMOL/Scripts/Packages/WilliamParadoxStoryV14/{Runtime,Editor}`와 `Assets/ANIMOL/Data/StarwayRanch/Story`를 사용한다. 실제 적용·검증 결과는 [프로젝트 결과 보고](../../ANIMOL_STARWAY_RANCH_CHRONICLE_RESULT.md)를 따른다.

프로젝트에서는 `RanchStoryUiInstaller.ApplyChronicle()`이 기존 UI/진입 프리팹과 미리보기 씬의 GUID를 유지하며 갱신한다. 생성 메뉴는 제공본과 동일하게 새 고유 이름으로 만든다. `Preview/index.html`은 저장소의 기존 원화·폰트·데이터를 상대 경로로 읽는 오프라인 검토본이다.

| 파일 | 역할 |
|---|---|
| `StoryNeonPanel.cs` | 반투명 남색 면과 얇은 네온 테두리 |
| `StoryDialogueData.cs` | 대사 JSON 모델, 문자열 이벤트 |
| `StoryDialogueView.cs` | 하단 대화, 타이핑, `>>`, 행동 잠금, 기록 진입 |
| `StoryChronicleData.cs` | 두 관점이 짝을 이루는 기록 JSON 모델 |
| `StoryChronicleView.cs` | 상향 크레딧, 하향 되감기·문자 교체, 홀드, 별도 계속 |
| `Editor/CreateStoryDialoguePrefab.cs` | 기존 Galmuri14를 재사용하는 새 프리팹 생성 |
| `../data/ranch_dialogue.json` | R01–R17 대사 178줄과 마지막 기록 진입 큐 1개 |
| `../data/ranch_chronicle.json` | 아스테르인 6문단 / 상위 존재 6문단과 재생 속도 |

기존 `RanchConstellationOverlay.cs`, `StarwayConstellationParallax.shader`는 그대로 사용할 수 있다. 대화 뷰가 배경 효과나 퍼즐 성공을 자동 처리하지 않는다.

## 1. 가져오기와 생성

1. 런타임 C# 다섯 개를 `Assets/ANIMOL/Story/`에 넣는다. Editor 파일은 그 아래 `Editor/`에 넣는다.
2. JSON 두 개도 Assets 안에 복사한다. `Resources`나 하드코딩한 파일 경로는 필요 없다.
3. Project 창에서 **기존에 사용하는 Galmuri14 TMP Font Asset**을 선택한다. 새 폰트나 기본 TMP 폰트로 자동 대체하지 않는다.
4. `Tools → ANIMOL → Story → Create Neon Dialogue Prefab`을 실행한다.
5. 생성 프리팹을 기존 전체 화면 Canvas 아래에 놓고 루트를 전체 스트레치한다. 기존 EventSystem과 UI 입력 모듈을 사용한다.

새 프리팹은 `Assets/ANIMOLGenerated/StoryUI/`에 저장된다. 같은 파일이 있으면 새 이름을 선택하며 기존 프리팹이나 열린 씬을 덮어쓰지 않는다. 폰트 탐색은 선택된 Galmuri14 에셋 → 선택된 UI에서 사용 중인 Galmuri14 → 프로젝트의 유일한 Galmuri14 순이다. 후보가 여러 개면 사용할 에셋을 선택해야 한다.

JSON은 파일 이름이 정확히 `ranch_dialogue.json`, `ranch_chronicle.json`이고 각각 하나만 있을 때 자동 연결한다. 없거나 중복이면 Inspector에서 직접 지정한다.

### 생성되는 주요 계층

- `StoryDialogueUI`: CanvasGroup, StoryDialogueView
  - `SafeArea`
    - `DialoguePanel`: 화자, 본문, 다음 표시, 투명 탭 영역
    - `ActionButton`: `block_lamp` 전용 행동 표시
    - `ChronicleLayer`: 전체 안전 영역 입력·반투명 배경, CanvasGroup, StoryChronicleView
      - `TextViewport`: RectMask2D
        - `ScrollingColumn`: 제목과 재사용 문단 템플릿
      - `HoldHint`
      - `ChronicleContinue`: 기록이 끝난 뒤 한 번만 사용하는 `계속`
    - `SkipButton`: 가장 나중에 그려지는 우상단 `>>`

**Progress, 장면 ID, 일화 안내 버튼을 생성하지 않는다.** 구버전 프리팹에 연결된 Progress 참조는 호환을 위해 숨겨진 필드로 남지만 실행 시 비활성화한다. 새 계층으로 교체하려면 생성 메뉴를 다시 실행한다.

## 2. 화면 규격

| 항목 | 1080 × 1920 기준 |
|---|---|
| CanvasScaler | Scale With Screen Size, 기준 해상도 1080 × 1920 |
| 대화 패널 | 안전 영역 하단 20px, 좌우 44px, 높이 422px 고정 |
| 대화 본문 | Galmuri14 45px, Regular, 행 높이 135%, 자동 글자 축소 없음 |
| 본문 영역 | 좌우 38px, 상단 97px, 높이 250px |
| 다음 표시 | 패널 우하단; 본문과 분리 |
| 우상단 스킵 | 우측 44px·위 42px, 104 × 88px, `>>` |
| 기록 글자 | 동일한 Galmuri14 45px, Regular, 행 높이 160% |
| 기록 읽기 폭 | 안전 영역 폭의 84% |
| 기록 뷰포트 | 안전 영역 y 14%–89%, RectMask2D로 실제 클리핑 |
| 아스테르인 글자 | `#F5F3E9` |
| 상위 존재 글자 | `#CDDBF3` |
| 글자 표면 | 깨끗한 단색; 종이 입자·한지 노이즈 없음 |

대화창은 대사 길이에 따라 위로 이동하지 않는다. 현재 본문은 3–4줄을 담는 고정 영역이며, 실제 프로젝트 폰트의 줄바꿈·폰트 메트릭은 Unity에서 확인한다. 넘치면 대사를 나눈다. SafeArea는 `Screen.safeArea`를 반영하며 배경 일러스트는 바깥에 놓아 전체 화면을 채운다.

`SetFont(existingGalmuri14)`는 일반 대사와 기록의 모든 라벨을 함께 변경한다. 합성 Bold/Italic을 사용하지 않는다. `SetHaloEnabled(false)`는 테두리의 작은 후광만 끈다.

## 3. 대화 시작과 행동 잠금

```csharp
view.LoadJson(dialogueTextAsset);            // Inspector로 연결했다면 Start에서 자동 읽음
view.Chronicle.LoadJson(chronicleTextAsset); // Inspector 연결 시 첫 Begin에서 자동 읽음
view.OpenScene("R01");
```

`Play First Scene On Start`, `Advance Between Scenes`는 기본 꺼짐이다. 순수 컷신 확인용일 때만 켠다. 실제 스테이지에서는 호스트가 `OpenScene` / `OpenNextScene` 시점을 결정한다.

일반 대화의 첫 탭은 타이핑을 모두 펼치고, 다음 탭이 다음 줄로 이동한다. `block_lamp`에서는 별도 `등불 가리기` 버튼이 ActionRequested를 전달한다. 실제 행동을 호스트가 검증한 뒤 `view.ConfirmAction("block_lamp")`를 호출한다. 타이핑 중 또는 잘못된 action으로 확인할 수 없다.

`>>`는 현재 장면의 남은 대사를 넘기되 첫 필수 action에서 멈춘다. R17의 대사를 건너뛰면 기록을 시작한다. **기록 재생 중 다시 `>>`를 누르면 기록을 스킵하고 별도 `계속` 입력을 기다린다.** 기록을 읽었다고 자동 기록하지 않는다.

`Close`, `Replay`, 다른 장면 열기, 데이터 교체는 진행 중인 크레딧과 홀드를 취소한다. 일반 패널은 복원되며 닫힌 전체 뷰의 CanvasGroup은 입력을 해제한다. 모달/메뉴 전환 시 호스트는 `view.Close()`를 호출한다.

## 4. 책 위의 두 관점

마지막 `R17_008`은 일반 나레이션 대사가 아닌 다음 진입 큐다.

```json
{"id":"R17_008","speaker":"","text":"","shot":"R17B","cue":"dual_record","emotion":"narration","action":"play_chronicle"}
```

호스트는 ShotRequested `R17B`에서 책 배경을 보여준다. 그 뒤 StoryDialogueView가 일반 대화 패널만 숨기고 ChronicleLayer를 시작한다. **UI 루트 전체를 끄면 기록도 꺼지므로 끄지 않는다.** `dual_record`는 음향 등 호스트 연출을 위한 알림이며 별도의 크레딧 플레이어를 또 만들지 않는다.

| 순서 | 실제 구현 |
|---|---|
| 진입 | 0.6초 기록 레이어 페이드 |
| 첫 기록 | 아스테르인 제목과 6문단이 아래에서 위로 이동 |
| 여운 | 마지막 문단까지 화면 위로 완전히 나간 뒤 1초 |
| 정정 | **같은 ScrollingColumn**이 4.2초 동안 위에서 아래로 되감김 |
| 문자 침범 | 중앙을 통과하는 문단만 일부 문자가 `/ _ ~ =`로 흔들리고 가로 6px 이내 이동; 짝이 되는 상위 존재 문단으로 교체 |
| 정정 완료 | 전체 상위 존재 기록이 화면 아래에 놓인 상태로 0.65초 |
| 두 번째 기록 | 바뀐 제목과 6문단이 다시 아래에서 위로 이동하며 충분히 읽히게 함 |
| 종료 대기 | 끝까지 지나간 뒤 `계속` 하나만 표시 |

기본 속도는 기준 화면에서 초당 84px다. 문단의 두 버전 중 더 긴 높이로 공통 슬롯을 만들므로 정정 중 나머지 문단이 갑자기 재배치되지 않는다. 글자 색과 일부 글자만 변한다. 전체 화면 플래시, 배경 노이즈, 책 질감 변형은 하지 않는다.

### 홀드와 접근성

- 기록의 전체 안전 영역에서 마우스 왼쪽 버튼 또는 터치를 누르고 있으면 3배속이다. 물리 키를 직접 폴링하지 않고 EventSystem 포인터 이벤트를 사용한다.
- 손을 떼거나 영역 밖으로 나가거나 Cancel 이벤트, 비활성화, 앱 포커스 상실, 일시정지가 발생하면 홀드를 즉시 해제한다.
- 포커스 상실/앱 일시정지 중에는 기록도 정지한다. 복귀 첫 프레임의 시간값은 최대 0.05초로 제한한다.
- 홀드 상태로 마지막에 도달해도 그 손을 떼는 동작은 챕터 완료로 취급하지 않는다. 별도 `계속` 버튼을 새로 눌러야 한다.
- `view.SetReducedMotion(true)`는 정적인 아스테르인 6페이지 → 상위 존재 6페이지로 바꾼다. 짧은 화면 탭마다 다음 문단으로 이동하며 끝난 후 별도 `계속`을 누른다. 이동과 노이즈가 없다.
- 재생 도중 모션 축소 설정을 바꾸면 첫 기록부터 다시 시작한다. 보지 않은 문단을 읽음으로 처리하지 않는다.

## 5. 호스트 이벤트와 저장 연결

StoryDialogueView의 Line → Shot → Cue → Emotion 이벤트 이후 기록이 시작된다. 기존 SceneEntered / SceneFinished / SequenceFinished / Closed도 유지한다. 이벤트 안에서 다른 씬을 열거나 닫으면 이전 재생의 후속 처리를 중단한다.

| 이벤트 / 상태 | 의미 |
|---|---|
| `view.Chronicle.ReadThrough` | 두 기록이 모두 지나갔거나 정적 12페이지를 모두 넘김 |
| `view.Chronicle.Skipped` | 기록 중 `>>`로 생략함 |
| `WasReadThrough`, `WasSkipped` | 마지막 기록 결과. 새 Begin에서 초기화, Cancel 직후에도 결과 확인 가능 |
| `ContinueRequested` | 기록 전용 계속 입력. StoryDialogueView가 내부 연결하므로 호스트가 중복 연결할 필요 없음 |
| `view.ActionConfirmed("play_chronicle")` | 기록 종료 대기에서 별도 계속을 누름. 읽었는지 여부는 Chronicle 상태로 구분 |
| `SceneFinished("R17")` | 위 확인 후 챕터 컷신이 종료됨 |

호스트가 `ConfirmAction("play_chronicle")`만 조기 호출해 진행할 수 없게 되어 있다. 기록이 종료 대기 상태이고 전용 Continue가 실제로 요청되어야 확인된다. 이전 `chapter_complete` 대사와 중복 계속 버튼은 현재 데이터에서 제거했다.

### 연결 예시

아래 예시는 이벤트 구독과 결과 구분만 한다. 프로젝트의 실제 저장 함수는 주석 위치에 연결한다. 람다를 매번 새로 등록하지 않고 이름 있는 메서드를 대칭 해제한다.

```csharp
using Animol.Starway;
using UnityEngine;

public sealed class RanchStoryBridge : MonoBehaviour
{
    [SerializeField] private StoryDialogueView view;
    [SerializeField] private bool replayMode;

    private void OnEnable()
    {
        view.Chronicle.ReadThrough.AddListener(OnRecordRead);
        view.Chronicle.Skipped.AddListener(OnRecordSkipped);
        view.ActionConfirmed.AddListener(OnActionConfirmed);
        view.Closed.AddListener(OnStoryClosed);
    }
    private void OnDisable()
    {
        view.Chronicle.ReadThrough.RemoveListener(OnRecordRead);
        view.Chronicle.Skipped.RemoveListener(OnRecordSkipped);
        view.ActionConfirmed.RemoveListener(OnActionConfirmed);
        view.Closed.RemoveListener(OnStoryClosed);
        view.Close();
    }
    private void OnRecordRead()
    {
        if (replayMode) return;
        // 저장소에 RANCH_RULE_EPILOGUE_SEEN을 기록할 수 있는 지점.
        // 스킵 이벤트에서는 이 플래그를 기록하지 않는다.
    }
    private void OnRecordSkipped()
    {
        if (replayMode) return;
        // 필요한 경우 별도의 '기록 생략' 상태만 기록한다.
    }
    private void OnActionConfirmed(string action)
    {
        if (action != "play_chronicle" || replayMode) return;
        bool recordRead = view.Chronicle.WasReadThrough;
        bool recordSkipped = view.Chronicle.WasSkipped;
        // 이 입력은 챕터 종료 요청이다. 실제 귀환·퍼즐 조건을 확인한 뒤
        // 저장하고 다음 화면으로 전환한다. recordSkipped를 recordRead로 바꾸지 않는다.
        // 프로젝트가 기록 열람을 완료 조건으로 삼는다면 recordRead를 별도로 검사한다.
    }
    private void OnStoryClosed(string sceneId)
    {
        // 기존 호스트의 예약 FX, 팝업, 사운드 상태를 정리한다.
    }
}
```

`CURRENT_CALLS_DONE → ISOL_WANTS_RETURN → PLAYER_BLOCKS_LAMP → RETURN_PATH_USED`, `ISOL_PRESENT`, `CHAPTER_R_COMPLETE`는 계속 호스트 책임이다. UI는 게임 플래그를 쓰지 않으며 Replay에서 보상을 재지급하지 않는다.

## 6. 유지되는 장면 연출 계약

화자 바운스는 기존 허용 씬 R01, R05, R06, R08, R10, R13, R17에서만 호스트가 적용한다. 기록 크레딧에는 배우 바운스를 적용하지 않는다. 멧돼지 전경 돌진·타격 위치·배경 전환도 기존 shot/cue 호스트에 연결한다.

마지막 R17에서 토리의 `relieved`는 눈물 없는 안도 표정이다. 다른 인물의 `tearful`을 일괄 적용하지 않는다. 책 배경 R17B는 새로 그리거나 색을 바꾸지 않는다.

## 7. 가져온 뒤 확인

소스 계약·레이아웃 계산·재진입/입력 게이트를 검토했지만 실제 Unity 검증은 남아 있다. 다음을 프로젝트에서 확인한다.

- Galmuri14의 한글 글리프와 45px 문단 3줄, 실제 안전 영역에서의 읽기 폭.
- 일반 대화 패널 하단 20px 고정, 대사/다음 표시 겹침 없음, 카운터·일화 버튼 없음.
- R17B 책 배경 이후 실제 상향 → 하향 정정 → 상향 흐름, RectMask2D 밖 텍스트 가림.
- 마우스/터치 3배속, 해제·포커스 상실·메뉴 닫기 후 정상 속도.
- `>>` 생략과 전체 읽기의 서로 다른 이벤트, 마지막 홀드 해제가 계속 버튼을 대신 누르지 않음.
- 모션 축소 12문단과 마지막 별도 계속, 기록 중 닫기/Replay/다른 씬 열기의 정리.
- 기존 CanvasGroup, Input System UI 모듈 또는 StandaloneInputModule, 프로젝트 TMP 버전 호환.
