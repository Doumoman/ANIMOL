# 별길 목장 UI v0.27 적용 결과

입력은 `MCP_INBOX/William_Starway_Ranch_Art_v027.zip`이다. `Unity/UI_STORY_APPLY.md`를 먼저 읽고 적용했다. 패키지의 UI 버전은 0.27, 아트·임시 대사 버전은 0.26이다. Unity 6000.3.8f1 Windows Editor, Android 활성 타깃에서 실행했다. 패키지 제작 검수와 이번 실제 에디터 검증은 별개다.

## 적용과 입력 계약

- 기존 `NotebookProjectGateway.RequestStoryReplay → ranchStoryPrefab → StoryRunner` 경로를 유지했다. 진입 프리팹은 `Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab`이며 GUID `da99f509c70d5d042836af7cfd5a8d6d`를 유지했다. 같은 패키지의 `Scenes/RanchStoryPreview.unity`도 Unity API로 갱신했다. 메인 NotebookUI 씬, 퍼즐 맵과 보상은 수정하지 않았다.
- 새 UI는 `Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab`이다. 기존 Canvas 아래에 중첩하고 `RanchStoryDialogueBridge`로 기존 검증된 StorySession에 연결했다. 패키지 자체 진행기는 실제 저장 흐름을 대신하지 않는다. 프리팹 생성 메뉴는 `Tools/ANIMOL/Story/Create Neon Dialogue Prefab`을 유지한다.
- 기존 `Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset`를 모든 대화·조작·선택 열람 TMP 라벨에 연결했다. 본문·화자는 45px 고정, Regular, 합성 Bold/Italic 없음, 자동 축소 없음이다. 동적 글리프 캐시의 에디터 변경은 작업 전 사용자 파일 바이트로 복구하며 폰트 파일을 이번 커밋에 포함하지 않는다.
- 1080×1920 CanvasScaler와 Safe Area를 유지했다. 남색 `#0D152B` alpha 0.73, 2px 아이보리 림과 낮은 아이스블루 후광을 사용한다. 좌상단 장면 ID는 표시하지 않는다. 우상단 Safe Area 안의 104×88 버튼에는 `>>`만 보이고 `StoryDialogueView.SkipCurrentScene()`을 호출한다.
- 첫 다음 입력은 타이핑을 펼치며 두 번째 입력이 진행한다. 모든 연출은 대사 큐 진입에서 시작하고 다음 발화까지 자동 진행하지 않는다. 스킵은 현재 논리 장면만 넘기고 생략된 발화의 FX를 재생하지 않는다. 퍼즐 인계, 등불 입력, 기록문 완료, 마지막 계속 게이트에서 멈춘다. 기존 퍼즐 세 곳은 여전히 미리보기 인계이며 새 퍼즐 판정을 구현하지 않았다.
- 필수 순서 `CURRENT_CALLS_DONE → ISOL_WANTS_RETURN → PLAYER_BLOCKS_LAMP → RETURN_PATH_USED`, R17 `ISOL_PRESENT`, 별도 `RANCH_RULE_EPILOGUE_SEEN` 저장을 유지했다. 기록문 여섯 문단은 수동 다음으로 읽고 마지막 문단의 기존 3초 확인 뒤 **기록 읽기 완료**, 이어서 별도 **계속**을 눌러야 한다. 일반 다음과 스킵은 이 두 완료 입력을 대신하지 않는다. 새 `CHAPTER_R_COMPLETE` 보상 플래그를 발명하지 않았다.
- 기존 packageId/contentVersion/저장 키를 유지했다. 교체 전 199개 노드와 이전 별칭 12개, 총 211개 체크포인트의 정확한 플래그 집합을 복원 검사했다. 현재 그래프는 188개 호스트 노드, 저장 별칭 182개다. 임의의 등불 플래그 주입은 계속 거부한다.

## 데이터·아트와 실제 큐

`Assets/ANIMOL/Data/StarwayRanch/Story/ranch_dialogue.json`은 동봉 원문 그대로다. R01 21발화를 포함한 17장면 **임시 184발화**를 현재 런타임에 매핑했다. 따라서 이전 v0.25의 R01 원문 해시 계약은 이번 임시 대사 적용으로 교체되었다. R13 첫 기록은 “그날 저녁”, R13B 진행 라벨은 “다음 날”이다. R13의 발화자는 사냥꾼·이솔·토리이며 첫 서술만 기록 화자다. 꾸중 종료 후 수리 컷으로 바뀌고 빗장 큐 이후 농담을 시작한다.

원화 24장과 투명 레이어 8장, 총 32개 런타임 PNG를 검증했다. v0.25의 28개는 바이트를 유지하고 R17 한 장을 교체했으며 `R12A`, `R12C`, `R12_BoarRush` 세 장을 추가했다. 패키지 인물 레퍼런스 네 장과 UI 레퍼런스 한 장도 원본 해시를 확인했다. PNG 절단·질감·그레인·색 보정은 추가하지 않았다. 완성 CG는 한 장으로 사용한다.

활성 경로는 `Assets/ANIMOL/Art/Story/starway/<컷>/Resources/ANIMOL/Story/Starway/`이며 기존 Resources 호출을 유지한다. 기존 아트 GUID를 보존했고 모든 32장에 진입 프리팹의 Sprite 직접 참조를 연결했다. 경로·체크섬 정본은 `ArtBindings.json`, 실제 노드·화자·그림·큐·플래그 전체 매핑은 [runtime_mapping.json](Validation/StarwayRanchUi/runtime_mapping.json), 원본 일치 결과는 [asset_verification.json](Validation/StarwayRanchUi/asset_verification.json)이다.

| 소스 발화/호스트 노드 | 실제 큐 | 길이 / 지연(초) |
|---|---|---|
| R08_001 / R08_B1_L001 | foreground_cross | .35 / 0 |
| R08_003 | runner_reverse | .65 / 0, 역방향 정지 컷 대체 |
| R12_001 / R12_B1_L001 | rustle | .12 / 0, 별도 녹음 없음 |
| R12_002 | eyes_glint | .12 / 0, 국소 눈빛 |
| R12_003 | reactions | .30 / 0 |
| R12_005 | boar_charge | .35 / 0, 실제 투명 R12_BoarRush |
| R12_006, 008, 010 | impact_left, right, center | 각각 .10 / .20 |
| R12_011 | boar_settle | .20 / 0, 새 R12C |
| R13B_LATCH_SEATED | latch_seats | .20 / 0 |
| R15_001 / R15_B1_L001 | distance_increases | 1.60 / 0 |
| R16_001 / R16_B1_L001 | enter_distance | 0 / 0 |
| R16_004 / R16_B4_L001 | distance_returns_and_grasp | .42 / 0, 등불 입력 뒤 |

기존 StoryCuePlayer의 delta만 월드 FX를 진행시킨다. 별도 타이머나 시간 기반 침식 누적을 추가하지 않았다. R15의 BG/Friends/IsolHand 실제 세 원본을 사용하며 친구만 .94→.56으로 이동·축소한다. 친구 alpha=1, 손 크기·위치 고정, 원점 (50%,59.3%), Y -6%→-11%, 시차 .12/.38/1, drift (.05,-.08), visibility .70을 유지했다. 두 Overlay에 같은 SetCue 입력을 전달하고 XYZ 가중치는 (1,1,0)/(0,0,1)이다. `_SealOpen`은 별층 전체 귀환 블렌드이며 중앙 구멍·유리·문은 없다.

셰이더 직접 참조, 흰 기본 텍스처, UV(0,0,1,1), Raycast Target=false를 유지했다. 배경→원/중 FX→친구→근 FX→고정 손→대화 순이며 UI는 월드 왜곡·암전 밖에 있다. R07/R11/R15/R16 입력 전·귀환 첫 컷/R17은 상단 대화판으로 손·얼굴을 피한다. 모션 감속은 별자리 이동을 멈추고, 스킵·비활성·씬 이탈은 진행 중인 임시 큐를 취소/정리한다. `BlocksInput=false`, 전체 화면 플래시·햅틱·시간 정지 없음이다.

## 대체 컷과 미연결 범위

- R01의 별도 하늘/찻상·배우 반응, R02~R03의 손/눈동자, R04의 일/푸딩 컵/문턱 손, R05~R11의 독립 몸/손 애니메이션은 분리 원본이 없어 완성 CG 정지 컷이다. 원화 속 인물을 추출된 스프라이트로 취급하지 않았다.
- R08 전경 질주만 실제 투명 이솔 소스다. R08B에는 원경 이솔이 합쳐져 있어 .65초 컷 유지로 대체한다. 원경 축소 이동용 빈 배경·역방향 투명 이솔은 미연결이다.
- R12~R13B에는 초아·여울 화자/인물/화면 밖 음성이 없다. R17은 패키지에서 교체한 눈물 없는 토리 표정을 사용한다. 관절 손잡기나 원화 몸/잔디 자체 왜곡은 없다.
- 음성·생활음·음악 녹음은 패키지에 없다. 친구 호명은 텍스트로 계속되며 음성 재생은 미연결이다. 기존 작은 합성 빗장 접촉음만 유지했다.
- LORE_CLOCK/LORE_ROOM은 기존 스토리 선택 열람 버튼을 유지한다. 실제 월드 환경 오브젝트의 문구 열람 API가 없어 그 경로로의 공간 연결은 여전히 미연결이다.

## 실제 검증과 한계

- **EditMode 66/66 통과**: 스토리 62, 활성 ArtAssetTests 4. 대사 원문·화자·순서·211개 이전 체크포인트·폰트·프리팹 실제 참조·큐 정책을 검사했다. [결과 JSON](Validation/StarwayRanchUi/editmode_result.json).
- **PlayMode 7/7 통과**: 스토리 6, 활성 ArtQaPlayTests 1. 새 UI의 184발화 전체, 두 단계 타이핑, 별도 등불 입력, 기록문 저장/복원, 마지막 계속, R08/R12/R15~16의 스킵·일시정지·재진입·모션 감속, 세로 GameView를 검사했다. 전체 발화 순회는 OS 포커스와 디바운스를 테스트 헬퍼로 초기화하며, 별도 테스트에서 실제 일시정지/반복 입력을 검사한다. 기존 PlayerPrefs는 백업/복원했다. [결과 JSON](Validation/StarwayRanchUi/playmode_result.json), [184발화 기록](Validation/StarwayRanchUi/utterance_coverage.json).
- TMP 45px에서 모든 표시 페이지가 4줄 이내임을 확인했다. 실제 1080×1920 캡처 R01/R12C/R13B/R15 시작·원경/R16 등불/R17 토리·기록문을 [검증 폴더](Validation/StarwayRanchUi/)에 저장했다. 원본 200% R12C 손과 R17 토리 얼굴을 확인했고, 유지 아트의 주요 손/팔레트 검수는 이전 결과와 함께 대조했다. 노출된 손의 추가 손가락·복제는 보이지 않으며 가려진 엄지는 원본대로 유지했다.
- 실제 RenderTexture에서 Mask/RectMask2D 두 방식의 별층·네온 패널 클립 바깥 alpha=0, CanvasGroup alpha 전파, sealOpen=1 별층 전체 alpha=0을 검사했다. 네온 패널의 빠진 CanvasRenderer와 중첩 프리팹 패널 참조 누락은 수정 후 재실행했다.
- 처음 D3D12 PlayMode 시도에서 Unity 네이티브 GfxDeviceWorker가 종료됐다. 이후 **DX11 재시작 환경**에서 위 최종 테스트를 통과했다. DX12 문제를 해결했다고 보고하지 않는다. 복구 씬 백업은 보존하고 커밋에서 제외했다.
- **Android 프리팹/셰이더 AssetBundle 빌드 성공**, 셰이더 메시지 0개다. [결과](Validation/StarwayRanchUi/android_shader_bundle.json). 이는 APK/실기기 검증을 대신하지 않는다.
- **Android APK 빌드는 실패**했다. 기존 빌드 전처리기가 폐기된 아트 경로의 9개 카탈로그/manifest를 요구해 Player 컴파일 전에 중단됐다. 이전 아트 복원이나 검증기 비활성화는 하지 않았다. [현재 빌드 결과](Validation/StarwayRanchUi/android_player_build.json).
- adb 연결 장치가 없어 설치·실기기 셰이더·FPS·발열·점멸 검증은 미실행이다. 과거 CommercialPolishPolicyTests는 Legacy 격리 상태여서 실행하지 않았으며 활성 큐 정책/ArtAssetTests/ArtQaPlayTests와 구분한다.

작업 전 다른 사용자 변경은 스테이징하지 않는다. 보호 맵 SW00과 `.meta`, NotebookUI 씬, 폰트의 작업 전 바이트 보존 검사는 별도 [preservation.json](Validation/StarwayRanchUi/preservation.json)에 기록한다.
