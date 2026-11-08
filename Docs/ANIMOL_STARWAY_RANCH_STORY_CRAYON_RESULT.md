# 별길 목장 스토리 UI v0.29 크레파스 적용 결과

2026-11-09, 실제 ANIMOL Unity **6000.3.8f1 / Built-in / Direct3D 12 Editor**에서 적용·저장했다. 입력은 `MCP_INBOX/ANIMOL_Story_UI_Crayon_Patch_v029.zip`이다.

화면 **01→SW00, 02→SW01 … 20→SW19**의 기존 연결 20개를 조회해 확인했다. 맵과 진행 기록은 변경하지 않았다. 별도 전투는 향후 큰 이야기 분기에서 다룰 예정이며, 이번 패치에 전투 구현이나 전투 진입은 추가하지 않았다.

## 실제 연결한 대상

| 대상 | 연결 |
| --- | --- |
| `Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab` | 대화 패널, 우상단 `>>`, 별도 행동, 기록 전용 계속의 프레임 **4개** |
| `Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab` | 메인 `NotebookProjectGateway.ranchStoryPrefab`이 실제 다시보기에 생성하는 프리팹. 위 UI와 환경 기록 팝업/버튼 **5개**를 포함해 총 **9개** |
| `Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity` | 기존 스토리 호스트·장면 연출·저장 계약 유지, 같은 **9개** 프레임 |

두 프리팹을 Prefab Mode에서 열고 해당 StoryDialogueView 하나를 선택해 `Tools > ANIMOL > Story > Apply Crayon Skin To Selected Story View`의 설치기를 실행했다. 기존 미리보기 씬에서도 정확한 뷰에 연결했다. Undo/Redo·재적용과 기존 참조를 검사한 뒤 명시적으로 저장했다. 새 프리팹 생성 메뉴는 실행하지 않았다.

뷰 밖의 기존 `Optional Environment Reading`에는 `Environment Text` 패널과 기름 묻은 종이·열쇠 모양 종이·다음 문장·닫기 버튼이 있다. 같은 StoryCrayonFrame/PNG 세트를 연결하고, 뷰의 모션 감소가 이 프레임에도 전달되도록 외부 참조 5개를 연결했다. 기존 팝업의 `Backdrop` Image는 새 테두리를 덮지 않도록 시각 알파만 0으로 바꾸고 컴포넌트와 입력 설정을 유지했다. 기존 지도 다시보기 버튼은 이미 적용된 지도 스킨을 유지했다.

실제 다시보기에서는 호스트가 `Return to Constellation Map` 버튼을 추가로 복제한다. 이 복제에만 남아 있던 지도 장식 어댑터를 비활성화하고, 스토리 `>>`의 Utility PNG/재질 세트를 재사용하는 StoryCrayonFrame을 연결했다. 원래 Button·클릭 이벤트·RectTransform·화살표를 유지하며, 다시보기의 실제 총 프레임은 **10개**다. 진입 시 기존 움직임 감소 값을 StoryRunner에 전달한다. 이 연결은 정적 Image 자식과 기존 자산 참조만 만들며 그림·질감·아이콘을 생성하지 않는다. 저장 비활성화를 먼저 설정하고 UI 옵션을 전달하므로 다시보기 저장 계약도 유지한다.

## 자산과 소스 병합

- `Unity/Crayon/` 전체를 `.meta`와 함께 `Assets/ANIMOL/Art/UI/Story/Crayon/`에 가져왔다. 완성 PNG **27장**, Animated/Still 재질 **18개**, Story 전용 셰이더와 제공 검증/manifest 파일이다. PNG 바이트와 제공 메타 **46개**의 GUID는 ZIP과 동일하다.
- 기존 클래스는 `Assets/ANIMOL/Scripts/Packages/WilliamParadoxStoryV14/Runtime/`에 있다. StoryDialogueView에 스킨 참조·모션 감소 연결과 구 네온 비활성화만 병합했다. 새 StoryCrayonFrame/Skin도 같은 Runtime 어셈블리에 두었다. 중복 StoryDialogueView/ChronicleView는 만들지 않았다.
- 제공 Chronicle 소스를 그대로 덮으면 기존 호스트 일시정지·입력 검증·재개 델타 제한·진행 훅이 사라진다. 현재 Chronicle 구현은 요청한 두 기록 흐름을 이미 포함하므로 해당 소스와 저장/진행 호스트는 보존했다. 기존 CreateStoryDialoguePrefab API도 유지하고 크레파스 메뉴·스킨 연결만 병합했다.
- 설치기는 Play Mode·복수/모호한 뷰 선택·닫힌 프리팹 에셋을 거절한다. Undo 생성/참조 기록 순서를 보완해 Redo 뒤 그림·재질 참조가 유지되고 재적용 시 장식이 중복되지 않게 했다.
- `ranch_dialogue.json`과 `ranch_chronicle.json`은 v0.29와 이미 바이트 단위로 동일하다. 기존 TextAsset 연결을 유지했다. 일반 대사 **178개**와 마지막 기록 진입 명령 **1개**, R01~R17 shot/cue를 재생성하지 않았다.

## 보존과 동작

작업 전 상태와 두 프리팹·씬·Galmuri14·관련 소스는 `C:\Users\user\ANIMOL_Backups\StoryCrayon_20261109_041310`에 백업했다. 기존 미커밋 변경은 이번 커밋에 포함하지 않는다.

원본 직렬화 레코드 **663개**, 원본 RectTransform **160개**, 기존 프리팹 override·Button 이벤트·targetGraphic·SpriteSwap·TMP의 실제 폰트/문자/크기/색상/스타일을 비교했다. 허용된 변경은 스킨 컴포넌트와 장식 자식, 구 네온 비활성화, 기존 그림의 시각 알파, 스킨/외부 참조뿐이다. Unity가 TMP의 파생 색상/기본 스타일 캐시와 TMP용 Canvas 추가 정점 채널을 정규화한 것도 구분해 기록했다. **의도하지 않은 변경은 0건**이다.

기존 지도/HUD 팩·숫자 폰트·맵·대사 데이터 등 **1,361개 파일**의 작업 전 해시가 유지된다. 테스트 중 TMP가 추가한 글리프는 백업 원본으로 Editor에서 복원·재임포트하여 기존 Galmuri14와 메타도 바이트 단위로 유지했다. [보존 결과](Validation/StoryCrayon/SourcePreservation.json), [패키지 결과](Validation/StoryCrayon/PackagePreservation.json)

테스트 전환 중 저장된 Main 씬의 자동 레이아웃 값도 작업 시작 해시와 일치하는 백업으로 복원했다. 사용자의 기존 Main 씬 미커밋 내용은 그대로이며 이번 커밋에서 제외했다. 이전 검수 보고서의 캡처는 보존하고 새 캡처를 이번 결과 폴더에 별도로 기록했다.

대화창은 안전 영역 하단 20px·높이 422px, 우상단은 `>>` 그대로다. 카운터와 일화 안내를 새로 만들지 않았다. CrayonOutline/QuietFill은 입력을 받지 않는 장식이며 TMP와 버튼 판정 영역은 흔들리지 않는다. 새 그림 생성, 노이즈, UV 왜곡, 알파 깜빡임을 사용하지 않는다. 기존 StoryNeonPanel은 호환을 위해 남기되 비활성화했다.

기록은 아스테르인 전체 상승 → 같은 열 하강·글자만 정정 → 상위 존재 전체 상승 → 별도 계속 순서다. 홀드는 기록 시간만 3배로 가속한다. 스킵은 완독을 지급하지 않으며, 홀드 해제는 계속 입력이 아니다. 기록이 끝나도 호스트 진행 노드는 유지되고 새 Continue 이후 기존 ActionConfirmed/SceneFinished 및 저장 검증으로 이어진다. 새 저장 플래그·보상·조기 확인은 추가하지 않았다. 멧돼지, 별도 등불 행동, 이솔 귀환, 토리의 눈물 없는 안도 표정도 유지했다.

## 실제 Unity 검증

| 검사 | 결과 |
| --- | --- |
| Story EditMode | **65/65 통과** — 대사/shot/cue, 단계별 입력·저장 계약, 토리 표정, 기존 연출 정책 포함 |
| ArtAssetTests EditMode | **4/4 통과** |
| RanchStoryUiPlayTests | **4/4 통과** — 전체 178발화의 Galmuri14 줄바꿈/250px 본문, 등불 별도 행동, 스킵·재진입, 마지막 계속 |
| StoryChroniclePlayTests | **4/4 통과** — 두 기록 전체 상승/하강, 동일 문단 슬롯, 홀드·포커스·일시정지, 12개 정적 페이지, 스킵≠완독, 새 Continue |
| RanchArtIntegrationPlayTests | **2/2 통과** — 거리/귀환 연출의 중지·재진입·모션 감소와 기존 FX 클리핑 |
| StoryCrayonPlayTests | **2/2 통과** — 실제 호스트 9개 프레임, 기존 포인터 Raycast/클릭과 눌림·비활성 PNG, 외부 팝업 모션 감소, 9-slice 및 Mask/RectMask2D·CanvasGroup 클리핑 |
| 실제 Notebook 다시보기·입장 PlayMode | **1/1 통과** — 런타임 복귀 버튼의 Story 프레임/정적 A, 원래 복귀 클릭, 무차감 다시보기, SW00 실제 입장과 45→44 턴 반영 |
| 설치기 | 실제 세 대상의 Undo/Redo·재적용 통과, 장식 자식 중복 없음, 기존 참조/RectTransform/TMP 유지 |
| PNG/재질/셰이더 | 27장 import 규격·border·크기, 18개 A/B/C/Animated/Still 참조 통과, SpriteAtlas 포함 없음 |
| GPU 선택 | 0/0.3초 A 동일, 0.61초 B, 1.21초 C, 1.81초 A 반복. `_Motion=0`은 정적 A와 동일 픽셀 |
| 컴파일 | C# 오류/경고 **0**, 전용 셰이더 메시지 **0** |

합계 **EditMode 69개, PlayMode 13개 통과**다. [실행 결과 폴더](Validation/StoryCrayon), [GPU/임포트 결과](Validation/StoryCrayon/AssetGpuAudit.json), [실제 적용/Undo 결과](Validation/StoryCrayon/ApplyValidation.json)

검수 중 발견한 Undo/Redo 그림 참조 누락과 외부 팝업의 배경 겹침을 수정하고 재실행했다. 포인터 테스트는 기존 테스트와 같이 OS 포커스 상태와 분리해 입력을 준비하며, 실제 포커스 상실에 따른 입력 정지는 기존 호스트 정책 그대로다.

1080×1920 실제 GameView와 Canvas Safe Area에서 확인했다. [일반 대화](Validation/StoryCrayon/R01_1080x1920.png), [환경 기록 팝업](Validation/StoryCrayon/HostEnvironmentPopup.png), [아스테르인](Validation/StoryCrayon/Chronicle_asteran.png), [상위 기록](Validation/StoryCrayon/Chronicle_higher.png), [별도 계속](Validation/StoryCrayon/Chronicle_await_continue.png), [등불 행동](Validation/StoryCrayon/R15_LampGate.png), [거리 장면](Validation/StoryCrayon/R15_Far.png), [수리 컷](Validation/StoryCrayon/R13_Repair.png).

패키지의 소스 검증 결과와 이번 실제 Unity 결과는 별개다. **Android/iOS 실기기 터치·노치 기기·모바일 GPU, APK 셰이더 스트리핑/포함 여부는 검증하지 않았다.** 보관된 CommercialPolishPolicyTests는 활성 테스트가 아니므로 실행하지 않았으며, 활성 Story 연출 정책 검사와 ArtAssetTests를 실행했다. 기존 장면 음원·실제 게임의 챕터 완료 조건도 이번 스킨 변경으로 새로 구현했다고 보고하지 않는다.
