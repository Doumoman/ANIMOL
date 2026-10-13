# ANIMOL MISSING_UI_V1 — 작업 2 결과

작성: 2026-10-05 / Unity **6000.3.8f1** / 현재 작업 트리 기준.

## 판정

**기존 로비의 설정·조작·접근성·계정 화면과 반환 동선을 적용했다.** 현재 설정 저장 소유자의 8개 preferences를 연결하고 초기화 확인창을 추가했다. 실제 Unity PlayMode에서 설정 진입, 저장/씬 재진입, 미리보기, 입력, 초기화 취소/확정을 검사했다.

**오디오 설정·Google 로그인·계정/클라우드 저장 조회·충돌 해결은 미지원이다.** 알려지지 않은 정보는 `--` 및 연결 대기로 표시하고 실행을 비활성화했다. 서비스 성공 또는 전체 패키지 완료 판정이 아니다. 경쟁전/커스텀 방/대기방 적용은 다음 작업 3 범위다.

## 확인한 실제 소유자

작업 1 결과, `UNITY_APPLY_MISSING_UI_V1.md` 작업 2, 패키지 수용기준 SET-01~07/ACCOUNT-01~03, AGENTS.md와 현행 소스를 확인했다. 시작 시 기존 변경 494개 상태 항목을 분리했다.

| 실제 파일/소유자 | 확인·적용 |
|---|---|
| `UI/PortraitArtV1/PortraitEntryController.cs` | `ModeScreen.Settings` → 기존 SC13 진입을 유지. |
| `Scripts/UI/UiNavigationService.cs` | SC13_Settings, SC19_ControlSettings, SC20_AccountAndSave의 기존 navigation/history/Back 사용. |
| `Scripts/UI/MobileControlPreferences.cs` | 실제 `ANIMOL.M6.*` PlayerPrefs 8개와 clamp/default/Reset 재사용. 새 저장 체계 없음. |
| `Scripts/UI/MobileControlSettingsPresenter.cs` | 기존 저장/적용 경로를 확인. 레거시 버튼을 비활성으로 보존하고 새 위젯은 동일 preference 소유자에 연결. |
| `Scripts/UI/MobileControlLayoutApplier.cs` | 현재 6개 HUD 부품의 크기·좌우손·안전한 가로 위치·투명도·하향 버튼 표시 계산 사용. |
| `Scripts/UI/PortraitAccessibilityLayout.cs` | 기존 접근성 적용 유지. 신규 본문은 PixelTextBridge의 측정 높이로 스크롤하며 큰 글씨를 실제 픽셀 격자에 맞춰 확대. |
| `Scripts/UI/CampaignUiPresenter.cs` | 기존 SettingsReset은 토스트만 표시함을 확인. 전체 초기화 성공으로 연결하지 않았다. |
| `Scripts/UI/GrowthEconomyUiPresenter.cs` | 공개 `IdentityState`의 실제 Guest 상태만 표시. 계정 이름/잔액/저장 시각을 추정하지 않음. |
| 기존 `SaveConflictSelectionService` 및 DevPreview | 샘플 충돌 선택 계약은 실제 저장 조회/원자적 덮어쓰기/동기화 계약이 아니다. 운영 저장 선택은 차단. |
| `Scripts/Gameplay/GameplayFeedbackDirector.cs` | 실제 진동 preference가 기존 중요 사건의 햅틱 조건에 사용됨. 일반 설정 버튼에 새 햅틱/플래시/입력 차단 연출 없음. |
| 기존 UI_CommonRoot / Typography | CanvasScaler 1080×1920, width 기준, SafeAreaLayout, StandaloneInputModule, pixelroborobo/TMP 재사용. |

## 운영 프리팹과 진입/반환

운영 생성 메뉴: **ANIMOL > Missing UI V1 > Build Project Task 2 Settings and Account**.

`Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/`에 다음을 생성했다. Generated 밖에서 관리한다.

- `Settings.prefab`: 오디오 미지원 행, 실제 진동, 조작/접근성/계정/도움말/기록 링크, 전체 초기화 미지원 안내.
- `Controls.prefab`: 실제 HUD 배치 미리보기와 8개 설정 위젯, 초기화 확인 진입.
- `Account.prefab`: 실제 게스트 상태, 미조회 값 `--`, 로컬/클라우드 비교 카드, 미지원 실행 버튼.
- `ControlReset.prefab`: 초기화 범위 본문 스크롤 및 고정 취소/초기화 버튼.
- `UtilityArt.asset`: 작업 1 공용 Art, 패키지 설정 스프라이트, 실제 SC15 HUD Image 참조.

`MissingUtilityEntry`가 기존 UI 후처리 이후 SC13/19/20 자식을 비활성으로 보존하고 운영 프리팹을 설치한다. 기존 씬/프리팹 YAML을 직접 편집하거나 전체 UI를 재생성하지 않았다. 기존 공용 모달 호스트에 확인창 1개만 설치하며 중복 설치·구독을 막는다.

동선: **로비 설정 → SC13 → SC19/SC20/SC14 도움말/SC17 기록 → 기존 history Back**. 접근성 링크는 SC19 하단의 접근성·언어 영역으로 이동한다. 활성 뒤로 버튼 이름은 기존 SettingsBackButton/ControlSettingsBackButton/AccountSaveBackButton 계약을 유지한다.

## 저장·미리보기·초기화 계약

| 설정 | 실제 범위 또는 값 |
|---|---|
| 버튼 크기 | 기존 .75~1.35 |
| 가로 위치 | 기존 -.08~+.08, HUD 계산이 전체 컨트롤을 화면 안에 제한 |
| 불투명도 | 기존 .3~1 |
| 좌우손 / 전용 하향 버튼 | 기존 enum / bool |
| 큰 글씨 / 진동 | 기존 bool |
| 언어 | 기존 Korean/English preference; 전체 게임 번역 완료 의미 아님 |

값 변경은 기존 setter → PlayerPrefs.Save → 실제 HUD/접근성 적용 및 화면 재조회다. 재진입 시 위젯은 실제 값을 읽으며 이벤트 없는 값 설정으로 재저장 루프를 방지한다.

미리보기는 패키지의 같은 기기 그림 안에 `SC15_GameplayHud.prefab`의 SwipeMoveRegion/JumpActionButton/SpecialActionButton/AbilityActionButton/AnimalChangeButton/DropActionButton 시각 요소를 별도 레이어로 배치한다. 현재 실제 HUD의 6개 Image는 sprite가 없는 색상 패널이며 이를 임의 그림으로 교체하지 않았다. 기존 layout applier를 사용하지만 게임플레이 버튼/능력/입력 스크립트는 복제하지 않는다. 작은 도해는 배치·크기·투명도 확인용이며 실제 크기의 HUD 가독성 검증을 대신하지 않는다.

초기화는 **확인 후 기존 MobileControlPreferences.Reset()**만 호출한다. 대상은 위 8개 설정이며 계정 진행도/동물 성장/재화/구매/로컬·클라우드 게임 저장은 대상이 아니다. 취소 시 값 보존, 확정 시 기존 기본값 복원, 모달 중 배경 위젯/뒤로/중복 확인 차단을 검사했다. 전체 SettingsReset은 지원되지 않으므로 비활성이다.

BGM/SFX는 값·핸들·채움을 숨겨 0%로 오해하지 않게 하고, 음소거는 미조회 상태를 OFF로 표시하지 않는다. 계정 로그인 버튼은 공식 마크를 새로 만들지 않고 이유와 비활성 영역만 제공한다. 예시 진행도/코인/시간 및 로컬 선택에 의한 동기화 성공을 만들지 않았다.

## 실제 실행한 Unity 검사

개별 실행 증거는 [Docs/MissingUiV1Task2](Docs/MissingUiV1Task2)에 저장했다. CLI의 복합 필터가 0개 테스트를 선택한 시도는 통과 수에 포함하지 않으며, 아래 클래스는 각각 별도로 실행했다.

| 검사 | 결과 |
|---|---|
| 현재 프로젝트 Unity C# recompile | 오류 0, compilationFailed=false |
| MissingUtilityFlowTests | 7/7 통과 |
| CommercialPolishPolicyTests | 3/3 통과 |
| M9PortraitPolicyTests | 8/9 통과, 기존 캠페인 ID 기대값 불일치 1개 실패 |
| MissingUiStoreFlowTests | 5/5 통과 |
| PortraitEntryFlowTests | 3/3 통과 |
| ProductionFlowTests | 7/7 통과 |
| MenuThemeFlowTests | 3/3 통과 |
| UpgradePhase1FlowTests | 2/2 통과 |
| StagePhase2FlowTests | 9/9 통과 |
| MultiplayerPhase3FlowTests | 3/3 통과 |

최종 고유 테스트 **51개 중 50개 통과, 기존 캠페인 검사 1개 실패**다. 작업 2 전용 생성 메뉴를 재실행한 후 신규 7개 검사를 다시 수행하여 생성 프리팹의 연결도 확인했다. 중복 실행은 고유 테스트 수에 더하지 않는다.

신규 7개 검사는 실제 Lobby를 열어 다음을 실행했다.

- 로비 Settings 버튼으로 진입, 하위 메뉴와 뒤로 이동, 비지원 위젯 차단, EventSystem 1개.
- 실제 Slider/Toggle/언어 버튼으로 8개 preference 저장 후 Lobby 씬 재로드/SC19 재진입 값 비교. 미리보기 6개 부품의 실제 ReferenceRect/alpha/활성 상태 비교.
- 초기화 취소 전후 값 동일, 확정 후 8개 기존 기본값, persistentDataPath 파일 목록/바이트 불변.
- EventSystem PointerEventData로 Slider down/drag/up, Toggle click, ScrollRect wheel 입력 전달. 모달 뒤로 버튼 위치의 Raycast가 배경 버튼에 도달하지 않음.
- 화면 반복 재진입과 설치 호출 후 언어 변경 1회, view 3개/확인창 1개/installer 1개 유지.
- 실제 Editor Game View 1080×1920/1080×2400, 일반 영역과 좌우 48px/상단 120px/하단 96px의 모의 Safe Area, 긴 영문·큰 글씨·스크롤 끝·고정 버튼 경계.

신규 검사는 사용한 8개 PlayerPrefs의 원래 값과 키 존재 여부를 종료 시 복원한다. 프로세스 종료/재실행이나 모바일 터치 장치 자체의 검사는 아니다.

### 기존 실패 구분

`M9PortraitPolicyTests.CampaignIdentityAndReadinessRemainAtM8Boundary`의 171행은 T01-S01에 START/CP-START/B-A 등 과거 StableId가 있어야 한다고 요구한다. 현재 맵에는 다른 START ID와 POUNDER 등이 있다. **해당 테스트 소스와 T01-S01.asset 모두 작업 시작 시 SHA-256과 동일하다.** 이 EditMode 검사는 신규 설정 UI를 호출하지 않는다. 사용자 맵/테스트를 이번 범위에서 수정하지 않았으며 전체 회귀 성공으로 보고하지 않는다.

작업 1에서 기록한 M6GrowthEconomyUiFlowTests의 기존 실패 3개는 이번에 재실행하지 않았다. 이번 실행 결과와 섞지 않는다.

## 화면 증거

[캡처 폴더](Docs/MissingUiV1Task2/Captures)에 **28장**을 남겼다. 각 해상도에서 한국어 일반 글씨/오른손 최소 크기와 Safe Area 영어 큰 글씨/왼손 최대 크기를 사용했다. 이는 QA용으로 실제 preference API에 설정한 값이며 운영 기본값을 바꾸지 않는다.

| 대표 캡처 | 내용 |
|---|---|
| [1920 조작 미리보기](Docs/MissingUiV1Task2/Captures/1920_full_korean_Controls_top.png) | 현재 HUD 부품, 오른손·최소 크기 |
| [1920 영어 접근성](Docs/MissingUiV1Task2/Captures/1920_safe_english_large_Controls_bottom.png) | 큰 글씨·언어 안내·고정 Back |
| [1920 계정 비교](Docs/MissingUiV1Task2/Captures/1920_safe_english_large_Account_bottom.png) | 미조회 데이터/차단 사유, 장문 스크롤 |
| [1920 초기화](Docs/MissingUiV1Task2/Captures/1920_safe_english_large_reset.png) | 긴 본문 스크롤 후 취소/확정 고정 |
| [2400 조작 미리보기](Docs/MissingUiV1Task2/Captures/2400_safe_english_large_Controls_top.png) | 왼손·최대 크기·최소 불투명도 |
| [2400 초기화](Docs/MissingUiV1Task2/Captures/2400_safe_english_large_reset.png) | 긴 영문 전체 범위와 버튼 |

대표 캡처를 직접 열어 글자 대비, 장문 줄바꿈, 도해 배치, Safe Area 내 하단 버튼을 확인했다. 잘린 스크롤의 중간 행은 스크롤로 접근 가능하며 고정 하단 버튼과 겹치지 않는다. 기기 notch는 Editor inset 모사이고 실기기 검증은 아니다.

초기 캡처 검토 중 Editor 포커스 복귀의 `PortraitDisplayController`가 모의 inset을 되돌리는 경우를 발견했다. 테스트 전용 LateUpdate probe로 여백을 유지하고 캡처 프레임의 anchorMin/anchorMax를 명시적으로 검증하도록 보완한 뒤 7개 검사를 다시 실행했다. 최종 캡처와 `utility-regenerated-playmode.json`은 이 보완 이후 결과다. 운영 SafeArea 코드는 변경하지 않았다.

## 작업 2 수용기준과 남은 일

| 기준 | 판정 | 근거/남은 일 |
|---|---|---|
| SET-01 진입/하위 메뉴 | pass | 실제 Lobby→SC13→하위 화면→Back 검사 |
| SET-02 오디오 | blocked | 적용·저장 소유자 없음. 미조회 표시/비활성은 검사. 실제 오디오 서비스 연결 필요 |
| SET-03 preferences | pass | 현재 PlayerPrefs 저장/재조회·씬 재진입 검사 |
| SET-04 미리보기 | pass | 실제 HUD 시각 요소·동일 layout 계산/alpha/좌우손 검사 |
| SET-05 접근성 | partial | 설정 저장/큰 글씨/언어 화면 확인. 실제 장치 진동과 전체 콘텐츠 번역은 미검증/미완료 |
| SET-06 초기화 | pass | 대상 명시·취소 불변·기존 8개 Reset·게임 저장 파일 보존 |
| SET-07 시안 분리 | pass | 시안 값·저장소 사용 없음, 기존 defaults/아트 보존 |
| ACCOUNT-01 계정 상태 | partial | 현재 실제 Guest 및 미조회 `--` 확인. 연결됨/조회 실패 provider 계약과 실제 응답 필요 |
| ACCOUNT-02 저장 비교 | blocked | 카드/미지원 차단 적용. 실제 저장 요약 조회·충돌 해결/commit 계약 필요 |
| ACCOUNT-03 Google | blocked | 실제 인증 계약/공식 브랜드 리소스 없어 로그인 비활성. 임의 성공/새 로고 없음 |
| UX-01~04 | partial | 이번 3화면/모달 Editor 검사 완료. 전체 패키지·모바일 장치 미검증 |
| REGRESS-01 | partial | 컴파일/관련 UI 검사 실행, 기존 캠페인 ID 검사 1개 실패. 전체 M1~M9 재생성 미실행 |

추가로 Android/iOS 빌드, 앱 종료 후 prefs 복원, 실제 손가락 조작, OS 뒤로/백그라운드 복귀, 실제 계정 인증·저장 동기화는 실행하지 않았다. 현재 서비스 차단 상태를 실제 서버 검증으로 간주하지 않는다. HTML/독립 데모 실행 결과도 이번 Unity 운영 검사로 대체하지 않았다.

## 변경 경계와 재생성

이번 변경은 MissingUiV1/Project의 신규 코드 10개, 운영 프리팹 4개, UtilityArt, Unity meta, 통합 안내와 검사 증거다. [전체 파일 목록](Docs/MissingUiV1Task2/changed-files.txt)을 참조한다.

작업 1 상점/공용 모달·패키지 Generated, 동물 Phase1~3 공용/어댑터/운영 프리팹·카탈로그·IdMap, 기존 UI 생성 코드·씬·Canvas·입력 시스템을 수정하지 않았다. 보존 비교는 작업 시작 상태를 기준으로 수행한다. 기존 v7 소스 패키지와 런타임 토끼 시트의 이전 차이를 이번 작업에서 복원하거나 대체하지 않았다.

기존 파일 3,618개를 시작 시 해시로 기록했다. 테스트 중 생성된 TMP 동적 글리프 캐시 4개와 NamedArt registry 저장은 작업 시작 복사본으로 복원하고 폰트를 Unity에 재import했다. 최종 비교에서 **기존 3,618개 파일 모두 시작 시 바이트와 동일**했다. 개별 전후 SHA-256은 [preservation.json](Docs/MissingUiV1Task2/preservation.json)에 기록했다. Unity 자산 조회에서도 신규 운영 프리팹 4개의 Canvas/EventSystem이 각각 0개이고 UtilityArt의 6개 HUD 참조가 실제 SC15 프리팹에 연결됨을 확인했다.

재생성/후속 연결 안내: [PROJECT_TASK2_INTEGRATION.md](Assets/ANIMOL/UI/MissingUiV1/Project/PROJECT_TASK2_INTEGRATION.md).
