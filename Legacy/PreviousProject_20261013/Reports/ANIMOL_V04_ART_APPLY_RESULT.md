# ANIMOL 별길 목장 V04 실제 적용 결과

기준일: 2026-10-13. 대상: Unity 6000.3.8f1 ANIMOL. 작업 시작 커밋: `8d091151`.

## 적용 범위와 실제 구현 경계

V04 게임용 PNG를 `Assets/ANIMOL/Art/StarwayPasture/V04`에 임포트하고, 프레임 시퀀스·AnimationClip·표현 프리팹과 `Scenes/ANIMOL_Starway_Art_QA.unity`를 Unity API로 생성했다. 현재 탑뷰 퍼즐 구현과 플레이 씬은 없다. 따라서 사용자가 명시한 “탑뷰 구현이 없다면 아트 임포트와 QA 씬 완성” 범위를 적용했다. 활성 Bootstrap/Lobby/Results 및 MainUiV7 로비 배경을 다른 게임 화면으로 교체하지 않았다. 이 QA 씬은 퍼즐 성공을 흉내 내는 플레이 씬이 아니다.

현재 빌드 씬은 `Assets/ANIMOL/Scenes/{Bootstrap,Lobby,Results}.unity`다. `Assets/ANIMOL/Phase1`, P01 QA 씬은 없다. 동물 업그레이드 UI의 Phase1 이름은 탑뷰 P01 구현이 아니다. `Packages/manifest.json`의 기존 Pipeline/MCP/UGUI 및 실제 패키지를 유지했고 새 JSON 패키지는 추가하지 않았다. GraphicsSettings의 렌더 파이프라인은 기본 Built-in이다.

## 입력과 임포트

- 원본: `MCP_INBOX/ANIMOL_REFERENCE_SPRITES_V04.zip`. 압축은 저장소 밖 임시 폴더에 안전한 상대 경로를 검사한 뒤 풀었다.
- 적용 지시·README·두 manifest·추가 제작 결정을 먼저 읽고 JSON은 전체 파싱했다. 입력 문서 사본은 `Docs/Inbox/ANIMOL_REFERENCE_SPRITES_V04`에 보존했다.
- 정적 PNG 34개, 개별 애니메이션 프레임 142개, 게임용 night_sky 1개: 총 PNG 177개. 원본 바이트·크기·색상을 유지했다. `_sheet.png`, reference, preview, 검수 합성 이미지는 활성 Assets에 가져오지 않았다.
- 새 클립 15개 및 같은 프레임·시간을 사용하는 `StarwayFrames` 15개. AnimationClip은 외부 Animator 연결용이며 프리팹은 `StarwayVisual` 한 경로로만 재생한다. Animator와 자체 프레임 재생을 동시에 연결하지 않는다.
- 이번 팩의 사용자가 지정한 V04 경로·원본 파일명을 유지했다. 이전 문서의 미래 32px 정본 기본값으로 이 고해상도 팩을 축소하거나 이름·피벗을 바꾸지 않았다.
- 기존 탑뷰 셀 폭을 측정할 대상이 없어 독립 QA의 논리 셀 폭을 1 unit으로 명시했다. 일반 80 PPU, 월드 방울 128 PPU. 기존 Grid/물리/Collider는 변경하지 않았다. 향후 실제 셀 폭 s가 확인되면 PPU를 pixelsPerCell/s로 적용해야 한다.
- Sprite Single, Full Rect, 지정 Custom Pivot, 입력 알파/AlphaIsTransparency, Point(월드 방울 및 해당 프레임만 Bilinear), 무압축, mipmap 없음, Clamp, NPOT 없음, Max Size 8192, 플랫폼 리사이즈 override 제거. manifest 좌상단 피벗의 정규화 값을 그대로 사용한다.
- `V04ImportPolicy`의 후순위 임포트 설정으로 V04 범위만 관리한다. `NamedArtIndex.InScope`는 V04를 제외해 과거 파일명 기반 설정 복원과 자동 저장 큐가 새 자산을 덮어쓰지 않게 했다. 기존 Atlas/FreeShape 제작기의 32 PPU 수동 설정 경로는 V04를 호출하지 않는다.

## 실제 표현 연결

`Assets/ANIMOL/StarwayV04`에 독립 Runtime/Editor/Tests 어셈블리를 두었다. 기존 미커밋 서비스/UI 파일에는 표현 코드를 삽입하지 않았다.

| 요소 | 실제 연결 | 게임 상태 연결 여부 |
|---|---|---|
| 식물 | 각 표현 프리팹의 고정 Root → Visual/SpriteRenderer → StarwayVisual → idle/touch 시퀀스 | 기존 접촉 사건이 없어 미연결; QA에서 시각 접촉만 실행 |
| 방울 | soap_bubble_pink idle, 별도 bubble_collect_fx 프리팹의 비상호작용 280ms one-shot | 수집 권한 없음; 실제 수집 이벤트 연결은 미실행 |
| HUD | hud_visual.prefab, StarwayHud.Present/Unbind, 기존 Pixelroborobo_UI 폰트, SafeArea | unbound는 COST -- / --/-- 및 pause disabled; 숫자 위조 없음 |
| 일시정지 | normal/press/release/disabled Sprite와 PauseRequested 표현 호출부 | 실제 퍼즐 일시정지 소유자 없음; QA는 표현만 정지 |
| 출구 | exit_visual_root 아래 아치·계단·별 별도 레이어, 별의 아치 기준 [112,82] 반영 | 성공 판단 없음 |

`StarwayVisual`은 프레임만 바꾼다. Root/Collider/코스트/턴/RNG/Time.timeScale을 수정하지 않는다. touch는 재접촉 시 재시작하고 450ms 뒤 idle로 돌아온다. 별도 collect FX는 충돌 컴포넌트 없이 마지막 투명 프레임을 거친 뒤 비활성화하고 재사용 시 idle/표시 상태를 복원한다. AnimationEvent는 없다. 수집 사운드·햅틱·승리·보상은 실제 사건이 연결되지 않아 재생하지 않는다.

## 조사한 기존 코드 연결점

- `Scripts/Core/BubbleObjectiveService.cs`: TryCollect의 키 중복 방지, ProgressChanged, 3개 기준 ExitEligible. 공유 계약을 보존했다.
- `Scripts/Core/ExitGateController.cs`: IsOpen 및 TryEnter의 resolved 가드. 기존 세션 상태와 함께 단 한 번 ResolveMap을 호출한다.
- `Scripts/Core/RunSessionController.cs`: Loading/Countdown/Playing/ExitEligible/TimeExpired/MapResolved/Result 상태. 탑뷰 유효 행동·코스트·장치 턴·자동 연쇄 소유자가 아니다.
- 과거 DevPlayerController/DevTestSession/HUD의 이동·점프·스태미나·시간제 규칙을 새 퍼즐 구현으로 재사용하지 않았다.
- 기존 GameplayFeedbackDirector/FeedbackIntensityPolicy에는 추가 사건을 넣지 않았다. 새 표현의 입력 차단·카메라 kick·줌·진동·Hit Stop은 없다.

## 수풀 연결 QA

단일 96×96 프레임, 첫 셀 중심 피벗, 80px/1unit 간격을 적용했다. 96px 간격이나 80px 잘라내기를 사용하지 않는다. 세로 묶음은 PNG의 아래 행을 월드 -Y로 이어 배치한다. 단일 조립과 묶음은 서로 다른 공간에 놓아 같은 칸 중복 배치가 없다. 밝음/어두움 단색 바탕과 grass_mid 바탕에 단일·가로 2개·세로 묶음·가로 묶음·세로 3개·3×3 배열을 배치했다. Renderer sortingOrder=10이며 SpriteMask/RectMask2D/Tilemap은 없다. 기존 맵의 잘림 원인은 활성 탑뷰 맵이 없어 특정할 수 없다. 이 새 QA에는 셀 경계 마스크·잘린 Sprite Rect·96px 셀 간격이 없다.

SpriteAtlas는 새 경로에서 사용하지 않으므로 회전/타이트 패킹에 의한 잘림은 해당 없음이다. Tilemap Culling 검수와 기존 맵 행 방향 비교는 대상이 없어 미실행이다. 기존 레거시 자산은 아직 참조 가능하므로 일괄 삭제하지 않았다. V04 원본 시트 및 참고 합성 이미지 제외로 이번 재생 경로의 중복을 방지했다.

## 검증 결과

| 검사 | 실제 결과 |
|---|---|
| Unity 최종 생성/렌더 실행 | exit 0. `V04_VALIDATED textures=176 clips=15`; 배경 PNG는 별도 1개 |
| C# 컴파일 | 최종 생성·EditMode·PlayMode 실행의 CS 오류 0 |
| EditMode | **9/9 통과**: V04 4개, CommercialPolishPolicyTests 3개, CanonicalSpritePathTests 2개 |
| PlayMode | **4/4 통과**: 실제 V04 QA 씬 표현 수명주기 1개, 기존 MenuThemeFlowTests 3개 |
| 실제 화면 크기 | PlayMode에서 Game View 1080×1920 및 360×640 확인; 작은 화면 Safe Area 앵커/텍스트 영역 검사 통과 |
| 누락 스크립트/Sprite | 생성 프리팹 검사 통과, QA 씬의 Sprite 시퀀스 실행 통과 |
| GUID | Assets 중복 GUID 0; V04 직렬화 참조 1,087건의 누락 GUID 0. 패키지 GUID도 실제 PackageCache에서 확인 |
| 원본 | PNG 177개 SHA-256 일치, 리사이즈/팔레트 변환 없음 |
| 화면 | 캡처 7장; 분홍 오류색 픽셀 0. 수풀 전체 외곽과 3×3 전체가 프레임 안에 보이는 1×/2×/4× 캡처 확인 |
| 기존 변경 보존 | 시작 전 Assets/ProjectSettings/Packages 파일 3,820개의 해시 비교에서 기존 파일 변경은 의도한 NamedArtIndex.cs 한 곳뿐 |

검증 근거: [EditMode](Docs/Validation/StarwayV04/editmode.xml), [PlayMode](Docs/Validation/StarwayV04/playmode.xml), [GUID/기존 파일 감사](Docs/Validation/StarwayV04/reference_audit.json), [원본 해시](Docs/Validation/StarwayV04/source_hashes.json), [화면 검사](Docs/Validation/StarwayV04/screenshot_audit.json), [변경 파일 전체 목록](Docs/Validation/StarwayV04/changed_files.json).

처음 실행은 전체 재임포트 중 240초 제한으로 종료했다. 이어 생성 검사에서 Sprite PPtr 종료 키 뒤에 Unity가 1ms 샘플을 더하는 길이 차이를 발견하고, 종료 키를 total-1ms에 두고 stopTime을 manifest 종료 시간으로 지정했다. 모든 시작 키와 프레임 시간은 그대로다. 수정 후 정확한 clip.length 검사가 통과했다. EditMode 첫 실행의 pool 재활성화 검사는 비-ExecuteAlways MonoBehaviour의 OnEnable을 EditMode에서도 기대한 테스트 오류로 8/9였다. EditMode는 명시적 ResetVisual을 검사하도록 수정했고 실제 OnEnable 재활성화는 PlayMode에서 별도로 통과했다. [첫 결과](Docs/Validation/StarwayV04/editmode_first_attempt.xml)도 보존했다.

캡처: [정적](Screenshots/StarwayV04/qa_static.png), [idle](Screenshots/StarwayV04/qa_idle.png), [touch](Screenshots/StarwayV04/qa_touch.png), [전체 자산/출구](Screenshots/StarwayV04/qa_gallery.png), [1×](Screenshots/StarwayV04/qa_foliage_1x.png), [2×](Screenshots/StarwayV04/qa_foliage_2x.png), [4×](Screenshots/StarwayV04/qa_foliage_4x.png). 1×/2×/4×는 같은 4unit 프레임을 320/640/1280px로 렌더해 정확한 정수 픽셀 배율이다. 초기 캡처의 낮은 HUD 글자 대비와 붙어 보이는 가로 묶음/3×3 영역을 수정하고 최종 캡처를 다시 생성했다. 색 검사만으로 모든 미세한 아트 결함이 없다고 단정하지 않는다.

**남은 아트 검토:** 단일 vine_tile을 3×3 조립하면 접촉 포즈에서 연결부에 작은 배경 투과 틈이 보인다. Full Rect/정확한 피벗/80px 간격에서도 관찰되며 원본 프레임의 알파 형태에 해당한다. 묶음의 내부 채움과 단일 반복의 경계가 완전히 동일하지 않다. 원본 PNG를 임의로 수정하거나 마스크로 가리지 않았고, “구멍 0”으로 보고하지 않는다. [프레임 알파 경계](Docs/Validation/StarwayV04/foliage_alpha_bounds.json) 및 4× 캡처에서 재검토할 수 있다. 이 자산 정리는 최종 아트 승인까지 뜻하지 않는다.

재현 명령(프로젝트 루트):

```powershell
unity run . --editor-version 6000.3.8f1 --timeout 1200 -- -executeMethod ANIMOL.StarwayV04.Editor.StarwayV04FinalBatch.ApplyAndCapture
unity test . --editor-version 6000.3.8f1 --mode EditMode --filter 'StarwayV04Tests;CommercialPolishPolicyTests;CanonicalSpritePathTests' --output Docs/Validation/StarwayV04/editmode.xml --timeout 300
unity test . --editor-version 6000.3.8f1 --mode PlayMode --filter 'StarwayV04PlayTests;MenuThemeFlowTests' --output Docs/Validation/StarwayV04/playmode.xml --timeout 300
python Tools/audit_starway_v04.py
```

실제 변경은 새 V04 자산(177 PNG, 15 시퀀스, 15 클립, 37 프리팹, 2 QA 머티리얼, 1 씬 및 .meta), 독립 표현/임포터/생성기/테스트 소스, 입력 문서 사본, 검증 XML/JSON, 캡처 7장, 보고서와 감사 도구 및 NamedArtIndex의 경로 제외 변경이다. 입력 ZIP·기존 미커밋 파일·기존 레거시 보관본은 이번 커밋에 섞지 않았다. 자동 생성 QA 씬은 기존 빌드 목록에 등록하지 않았다.

## 미연결/미실행 항목

탑뷰 유효 이동/밀기 cost -1, 무효 입력 cost/장치 턴 0, 자동 연쇄 추가 cost 0, 실제 방울 중복 접촉 및 3개 수집→출구→성공 1회, 실제 리셋/실패/일시정지, 연출 on/off의 퍼즐 결과 동등성은 대상 구현이 없어 실행할 수 없다. 아트 테스트를 이 게임플레이 검수의 통과로 간주하지 않는다. 실제 사건이 존재하면 승인된 수집 커밋 후 독립 FX를 시작하고 HUD 상태를 즉시 Present하며, 입력/연쇄는 FX 종료를 기다리지 않아야 한다.

실기기 모바일 빌드·디스플레이 검수는 아직 실행하지 않았다. QA 씬은 자동으로 기존 빌드 씬 목록이나 정상 부트 경로를 바꾸지 않는다. Unity에서 QA 씬을 직접 열고 Play해 Touch/Pause visuals/Reset visuals를 실행할 수 있다. 이것은 아트 표현 검수 조작이며 퍼즐의 입력·수집·승리 조작이 아니다.
