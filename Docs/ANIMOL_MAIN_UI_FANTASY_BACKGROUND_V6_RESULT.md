# ANIMOL 메인 로비 v6 적용 결과

작업: `ANIMOL_MAIN_UI_FANTASY_BACKGROUND_V6_INTEGRATION` · 2026-10-02

## 적용 결과

Unity **6000.3.8f1 (1c7db571dde0)**에서 기존 Bootstrap → Lobby 진입 경로에 v6 에셋을 적용했다. 프로젝트 버전을 변경하지 않았다. 작업 시작 시 지정 문서는 아직 압축 해제되지 않아 저장소 루트의 `ANIMOL_Unity_Main_UI_v6.zip`에서 전체 지시서를 읽고 `Docs/Inbox/ANIMOL_main_ui_v6`에 풀었다.

기존 `FiveThemeBackdrop`의 합성 배경·LightVeil·구형 토끼와 숨겨진 `PA1_LegacyLobby` 인스턴스는 두 메뉴 씬에서 제거했다. 구형 배경 구현과 캡처 코드를 교체했다. 재생성 후 런타임에서도 이전 로비를 숨겨 둔 채 보관하지 않고 폐기하도록 연결했다. 공용 프리팹과 다른 화면에서 쓰는 버튼·폰트 자산은 보존했다. 최신 패키지는 배경·토끼 에셋이므로 기존 버튼의 모양·문구·기능·Safe Area를 유지했다.

## 조사한 실제 연결

| 항목 | 실제 구현 |
|---|---|
| 최초 진입 | `Assets/ANIMOL/Scenes/Bootstrap.unity` → `PA1_Start`의 Play → `BootstrapPresenter.Continue()` → `Lobby.unity` |
| 기존 로비 | `UI_CommonRoot/SafeArea/ScreenHost/SC01_Lobby`, `PortraitEntryController.ModeScreen` |
| 기존 화면 자산 | `UI/PortraitArtV1/Prefabs/Start.prefab`, `ModeSelect.prefab`, `Resources/ANIMOLPortraitEntryV1.prefab` |
| 새 배경 | `UI_CommonRoot/MainUiV6Backdrop`의 RawImage, `MainUiV6Cycle`의 `MenuThemeCycle` |
| 배치 | SafeArea 앞 형제인 배경 → SafeArea 안 ScreenHost → ModalHost → OverlayHost |
| 기준 해상도 | 기존 CanvasScaler 1080×1920, `PortraitDisplayController`와 `SafeAreaLayout` 유지 |
| 조작 연출 | 기존 `UiScenePolish`, `UiButtonFeedback`, `UiMotionElement` 유지. 배경은 버튼이나 화면 모션의 자식이 아니므로 tint/scale/위치 모션을 받지 않음 |

기존 이벤트는 Start Play, Lobby Back, Campaign, Shop, Settings 및 서비스 상태에 따른 Competition/Cooperation이다. 광고·온라인 모드는 미연결 상태를 그대로 표시하며 재화는 검증된 잔액이 없으면 `--`다. Campaign → ThemeCard → StageCard → 상세 화면의 운영 상태 판정도 변경하지 않았다.

## 구현과 재생성

- 원본 PNG 22개를 `Assets/ANIMOL/UI/MainUiV6/Textures`에 같은 이름·바이트로 복사했다. 20개 배경은 352×704, 두 토끼 시트는 512×96이다. `.meta`와 `Resources/ANIMOLMainUiV6.asset` 참조를 함께 관리한다.
- Texture/Point/Uncompressed/No mipmap/NPOT None/Clamp로 가져오며 Standalone·Android·iPhone·WebGL·Windows Store override를 해제한다. PNG alpha는 변경하지 않는다.
- `FantasyBackground.shader`가 원본 레이어를 GPU에서 352×704로 합성한다. 순서는 불투명 `#1a1c2c` → far → mid → platform → rabbit → near이다. 실제 프로젝트는 Built-in 렌더 파이프라인/Linear 색공간이며 정확한 sRGB 변환과 소스 texel 중심 샘플링으로 팔레트를 보존한다.
- 현재·직전 화면과 최종 출력용 RenderTexture 3개(각 352×704 ARGB32, 합계 약 2.84 MiB)와 Material 1개를 재사용한다. 별도 카메라·EventSystem·오디오·Physics·패키지를 추가하지 않았다. 일반 프레임에 1개 장면, 전환 중에만 2개 장면을 그린다.
- 최종 출력 RawImage는 흰 tint, raycast off이며 1:2 비율의 중앙 cover crop이다. 1080×1920에서 1080×2160(위아래 120px crop), 1080×2400에서 1200×2400(좌우 60px crop)이다. 네이티브 발선 Y=526은 각각 화면 위에서 약 1494px, 1793px에 놓인다.
- `FantasyBackgroundPolicy`가 정수 좌표, 5초 슬롯, 14fps/8프레임, 셀 좌상 Y=436, 발선 Y=526, 시차 및 독립 방향을 정의한다. 제작 과정의 shift 값이나 추가 bob을 적용하지 않았다. seed 기본값 0과 슬롯별 독립 salt(카메라 91, 토끼 314)로 재현 가능하며 기본값은 동봉 JS 방향 규칙과 일치한다.
- 슬롯 시작의 0.36초 동안 8×8 대각선 binary wipe를 적용한다. 이전 화면은 직전 슬롯 진행률 0.999, 새 화면은 현재 진행률이며 첫 프레임은 T01이다. 알파 crossfade를 사용하지 않는다.
- 로컬 `Time.unscaledDeltaTime` 누적 시계다. 같은 씬에서 다른 메뉴로 나갔다 복귀하면 이어 재생하고, 씬 재진입은 T01부터 다시 시작한다. 숨김·포커스 상실·앱 pause 중에는 시계를 멈추고 복귀 첫 프레임은 delta를 더하지 않는다. OnDisable/OnDestroy에서 소유 렌더 자원을 해제한다.
- 좁은 적용 메뉴: `ANIMOL/Main UI V6/Import and apply to existing menus` → `MainUiV6Builder.Build()`는 원본 import와 Bootstrap/Lobby의 배경 교체만 수행한다. 기존 `MenuHierarchyBuilder.Build()`도 V6 적용 함수를 호출하도록 수정했다. 씬에 직렬화된 배경이 없는 재생성 경로에는 기존 sceneLoaded 설치 훅과 Resources 카탈로그를 통한 fallback이 있다.
- 전체 `UiBuildPipeline` 씬 생성이나 지도 재생성 메뉴는 실행하지 않았다. 공용 UI 프리팹의 작업 전 미커밋 변경도 이번 커밋에 포함하지 않았다.

## 현장에서 실행한 검증

| 검증 | 최종 결과 및 증거 |
|---|---|
| Unity C# / Shader 컴파일 | 실제 6000.3.8f1 Editor에서 완료. 최종 오류 0 |
| EditMode | 8개 통과: V6 자산·타이밍·방향, 저장된 두 씬 계층, `CommercialPolishPolicyTests` 3개 포함 |
| PlayMode | 6개 통과: V6 팔레트·생명주기·fallback 3개, 기존 `PortraitEntryFlowTests` 3개 |
| 원본 패키지 | SHA256SUMS 39개 모두 일치. PNG 22개 모두 규격·Sweetie16·0/255 alpha 충족. 원본 대 복사본 바이트 동일 |
| 실제 GPU 픽셀 | 5테마와 전환 4개, 총 9장 × 247,808 = **2,230,272픽셀**을 독립 합성 결과와 비교해 차이 0. [비교 결과](MainUiV6/native-reference-comparison.json), [재현 스크립트](MainUiV6/validate_native.py) |
| 실제 Game View | 1080×1920 및 1080×2400에서 Start, 5테마 Lobby, wipe, 모달 PNG 확보. cover·1:2 비율·발선 화면 내 위치 확인 |
| 실제 26초 순환 | QA 시간 고정 없이 unscaled 로컬 시계로 실행. 5테마, 8프레임, 독립 방향 기록. [CSV](MainUiV6/Captures/Live_26s.csv), [요약](MainUiV6/live-validation.json) |
| 포인터 경로 | EventSystem.RaycastAll로 최상위 클릭 대상 확인 후 실제 PointerClick 처리: Play, Campaign, Shop, Settings, Back, ExitCancel, ThemeCard, StageCard, DEV 진입 |
| 생명주기 | 메뉴 숨김 시 시계 정지·자원 재사용, 포커스/pause 콜백 주입 후 시간 점프 없음, disable 시 해제, enable 시 복구, 씬 재진입 후 단일 컴포넌트 확인 |
| 게임 화면 격리 | 기존 DEV 진입 버튼으로 MapDevTest 실행 후 메뉴 배경 없음. Lobby 재진입 후 단일 배경 확인 |
| 보호 파일 | 아래 범위의 **678개** 경로와 SHA-256 동일. [검증 결과](MainUiV6/protected-verification.json) |

테스트 상세는 [unity-test-results.json](MainUiV6/unity-test-results.json), 실제 화면 경로 검증은 [1920 체크](MainUiV6/Captures/1080x1920_checks.txt) 및 [2400 체크](MainUiV6/Captures/1080x2400_checks.txt)에 기록했다. 동봉 `reference/*verification.json`은 패키지 제작 당시 검증이며 위 Unity 검증과 별개다.

## 보호 범위와 기존 변경

시작 전 `git status --short`를 확인했다. Campaign 데이터, Gameplay 및 공용 UI 프리팹 등 다수의 기존 변경이 있어 해당 파일을 스테이징하지 않았다. Bootstrap/Lobby와 이번 UI 구현·테스트·에셋·증거만 커밋한다.

해시는 `Assets/ANIMOL/Scenes/Campaign`, `Data/Campaign`, `Maps`, `MapBackups` 및 메뉴 외 최상위 씬을 대상으로 비교했다. 최초 넓은 수집 목록은 679개로 적용 대상 **Bootstrap도 포함**했다. 원본 [before](MainUiV6/protected-before.json) 기록을 유지하고, 명시적인 적용 대상 Bootstrap 1개를 제외한 678개를 보호 판정했다. Bootstrap의 해시 변경은 의도한 UI 교체이며 지도 변경으로 숨기지 않았다. 경로 추가·삭제도 함께 비교했다.

## 수정 과정과 남은 제한

- 초기 GPU 픽셀 테스트에서 바탕색의 근사 gamma 변환 및 wipe 입력 바인딩 오류가 발견되어 정확한 변환·명시적 shader property로 수정했다. 독립 합성 비교에서 발견한 확대 경계 샘플링 차이도 소스 texel 중심 샘플링으로 해결했다. 최종 검증은 수정 후 다시 수행했다.
- 추가 fallback 테스트 작성 중 using 누락으로 컴파일 오류가 발생한 실행은 0 tests 상태여서 통과로 세지 않았다. 수정 후 재컴파일하고 중단된 MCP 테스트 작업을 정리한 뒤 최종 테스트를 재실행했다.
- **운영 캠페인 시작은 미완료가 아니라 기존 정책에 의해 차단된 상태다.** T01-S01의 빠른 클리어·재플레이 보상 정책 승인 및 해당 버전/해시의 사람 완료 검수가 없어 시작 버튼이 비활성이다. 상세까지의 클릭과 비활성 버튼의 입력 거절을 확인했으며, 운영 시작 성공으로 보고하지 않는다. [실제 사유](MainUiV6/Captures/operational-start-blocked.txt). DEV 진입 성공은 운영 시작 성공과 구분한다.
- DEV 맵 진입에서 기존 `StageMapRuntimeLoader`의 unsupported object 경고 8개가 발생했다. 이 작업의 배경 렌더 오류는 아니며 맵을 수정하지 않았다. 검증 중 도메인 전환에서 MCP client disposed-object 오류가 한 차례 있었고 연결 복구 후 최종 화면 캡처에는 런타임 오류가 없었다.
- Android/iOS 실기기 빌드, GPU 성능 프로파일링 및 OS가 실제로 앱을 background로 이동시키는 검증은 수행하지 않았다. 앱 pause/focus는 실제 컴포넌트 콜백을 주입해 검사했다. 장기 메모리 누수 스트레스 테스트를 수행했다고 주장하지 않는다.
- 영상은 **실제 Unity Game View를 약 2fps로 샘플링한 26초 검토용 영상**(540×960)이다. 14fps 애니메이션 전체 프레임을 담은 고프레임 화면 녹화는 아니며 원색 판정은 native PNG/테스트를 기준으로 한다.

## 검토 자료

- [1080×1920 로비](MainUiV6/Captures/Lobby_T01_1080x1920.png)
- [1080×2400 로비](MainUiV6/Captures/Lobby_T03_1080x2400.png)
- [실제 모달 화면](MainUiV6/Captures/Modal_1080x1920.png)
- [26초 Unity 순환 영상](MainUiV6/Unity_Lobby_26s.mp4)
- [전체 캡처 폴더](MainUiV6/Captures)

재검증: Unity Test Runner의 `ANIMOL.FiveThemeMenu.Tests`, `ANIMOL.PortraitArtV1.Tests.PortraitEntryFlowTests`, `ANIMOL.Tests.EditMode.CommercialPolishPolicyTests`를 실행한다. 캡처는 원하는 Game View 해상도에서 Bootstrap Play 후 `ANIMOL.FiveThemeMenu.MainUiV6Capture.Begin()`을 실행한다. 독립 픽셀 비교는 Pillow가 있는 Python으로 `python Docs/MainUiV6/validate_native.py`를 실행한다.
