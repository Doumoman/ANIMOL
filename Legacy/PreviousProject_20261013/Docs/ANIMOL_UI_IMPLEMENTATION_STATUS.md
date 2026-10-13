# ANIMOL UI 구현 상태

최종 갱신: 2026-09-28 (M0~M5 기존 사실 보존, M6 증분 구현·검증 완료)

## 정본과 연결 환경

- M6 최신 정본: 저장소 루트의 `ANIMOL_55문항_확정사항과_UI_후속작업_명령_v1.1.md`를 끝까지 읽었다. 요청의 `Docs/` 경로에는 없었으며 루트 파일이 실제 위치다.
- M0~M5 기존 주문서: 저장소 루트의 `ANIMOL_UNITY_CLI_UI_CORE_IMPLEMENTATION_ORDER_v1.0.md`. 요청에 적힌 `Docs/ANIMOL_UNITY_CLI_UI_CORE_IMPLEMENTATION_ORDER_v1.0.md`는 존재하지 않는다.
- 기존 상태 보고서 실제 위치: `Docs/ANIMOL_UI_IMPLEMENTATION_STATUS.md`.
- Unity 프로젝트: `C:/Users/user/Documents/GitHub/ANIMOL`
- Unity Editor: 6000.3.8f1
- Unity CLI: 1.0.0-beta.11 / Pipeline 0.8.0-exp.1 / 포트 7800 / 연결 `ready`
- MCP for Unity: Editor 패키지·서버 v9.7.1 / 인스턴스 `ANIMOL@0bba0934` / 포트 6400 / 연결 `running`
- UI·입력·물리: uGUI 2.0.0, Legacy Input Manager, Physics2D

## 기존 자산 재사용 조사

| 대상 | 조사 결과 |
|---|---|
| 기존 런타임 C#·서비스 | 없음 |
| 기존 UI·프리팹 | 없음 |
| 기존 게임 씬 | `Assets/Scenes/SampleScene.unity` 템플릿만 존재 |
| 기존 캠페인·동물·계정 데이터 | 없음 |
| 재사용 기반 | Unity 2D URP 템플릿, Physics2D, 기본 Input Manager |

## 마일스톤 완료·차단 상태

| 단계 | 상태 | 컴파일·검증 결과 |
|---|---|---|
| M0 현황·코어 계약 | **Implemented** | 컴파일 성공. 5테마×20스테이지=100 안정 ID, 중복 0, 테마당 20. 생성기 재실행 시 수동 이름 보존. 운영 슬롯 Ready 0 / Unassigned 100. |
| M1 공통 UI 틀 | **Implemented** | 컴파일 성공. Bootstrap/Lobby/공통 Canvas/Safe Area/네비게이션/모달/토스트/설정/도움말 생성. 실행 중 Bootstrap→Lobby 및 로비 루트 버튼 응답 확인. |
| M2 캠페인 UI | **Implemented** | 컴파일 성공. 실행 중 5개 테마 각각 정확히 20개 스테이지 카드를 표시. `T05-S20` 상세에서 동물 선택 버튼 0, 고정 구성 읽기 전용, 미할당 시작 버튼 비활성 확인. |
| M2 개발 코어 런 | **DevOnly** | `DEV-TEST-01`을 운영 100슬롯과 별도 생성. 실제 Rigidbody2D/Collider2D로 후보 방울 4개 중 고유 3개→출구 개방→실제 출구 접촉→결과 씬 흐름 통과. |
| M3 성장·소셜·상점 | **Implemented** | 계정 공통 3트랙 Lv.0~10 버전 데이터, 동물별 성장 확장 자리/미설정 잠금, 기본 무료 이모티콘 3개와 퀵 슬롯 4개, 상점·상품 상세·선택 광고·복원·프로필 화면 연결. 외부 승인 없이는 구매/광고 성공 없음. |
| M4 멀티 UI | **Implemented / DevOnly preview** | 경쟁·협동·비공개 허브, 룸, 멀티 전용 동물 선택, 경쟁/협동 HUD, 온라인 메뉴와 오류 모달 연결. 운영 매칭/Ready는 차단하고 `DEV ONLY` 선택·HUD 미리보기만 격리 제공. |
| M5 검증·인수인계 | **Implemented / Pass** | 컴파일 오류 0, EditMode 14/14, PlayMode 4/4. 루트 화면 20/20·모달 5/5·무반응 버튼 0, 5×20·선택 경로 0, 16:9/20:9 Game View 시각 검사 통과. 실기기 노치/하드웨어 Back은 환경상 차단. |
| 운영 캠페인 맵·동물 | **Unconfigured** | 실제 맵 템플릿과 고정 동물 데이터가 제공되지 않아 운영 100개는 의도대로 `제작 중`이며 실행 불가. |
| 외부 계정·코인·결제·광고·매치 서버 | **Blocked** | SDK·서버·상품/환급 정책 미제공. UI와 포트는 구현됐지만 승인·잔액 변경·보상·실매치·순위 성공은 의도적으로 발생하지 않음. |

## M6 신규 진행 표 (v1.1 최신 결정)

이 표는 위 M0~M5 완료 사실과 구분되는 이번 증분 작업만 기록한다. M6 생성기는 기존 M0~M5 전체 생성기를 재실행하지 않았고, 수작업 캠페인 필드와 `DEV-TEST-01`을 보존한 채 새 필드가 비어 있을 때만 보강했다.

| M6 항목 | 상태 | 실제 구현·검증 결과 |
|---|---|---|
| 기존 자산 조사·보존 | **Pass** | 기존 Core/Presenter/Editor 생성기/UI 프리팹/Gameplay 씬을 재사용. `m0ToM5Regenerated=false`; 5×20 안정 ID와 수작업 필드 보존 테스트 통과, `DEV-TEST-01`은 운영 카탈로그 밖에 유지. |
| 캠페인 순차 해금·고정 동물 | **Implemented / Pass** | `CampaignProgressionService`와 고정 로스터를 유지하고 테마/스테이지 카드에 잠금 상태를 텍스트+기호로 표시. 캠페인 동물 선택 진입점 0. |
| 운영 스테이지 런 규칙 | **Contract Implemented / Unconfigured** | 제한시간, 고정 체크포인트, 동일 재시작 배치, 접촉 기반 방울 공개, 선택 목표, 튜토리얼, 20번째 이야기 키를 담는 데이터 계약을 추가. 실제 운영 맵 100개에는 근거 없는 값을 넣지 않아 RuntimePolicy 0, Ready 0이다. |
| 시간 초과·클리어·최고 시간 | **Implemented / Pass** | `TimeExpired`를 클리어와 분리하고 시간 초과가 진행/최고 기록을 만들지 않도록 검증. 검증된 출구 클리어만 첫 클리어·최고 시간 기록 가능. 운영 데이터는 미설정. |
| 방울 3개→실제 출구 | **DevOnly / Pass** | `DEV-TEST-01`에서 실제 Physics2D 접촉으로 고유 방울 3개를 얻은 뒤 실제 출구 트리거에 다시 접촉해야 결과 화면으로 이동. 방울 3개 전 출구 접촉은 거절. |
| 체크포인트·동일 재시작 | **DevOnly / Pass** | 고정 `CHECKPOINT_START`, `CHECKPOINT_MID`와 물리 트리거/리스폰 구현. 재시작 식별자는 같은 스테이지 및 `mapSeed=601`, `bubbleSeed=603`을 유지. |
| 미발견 방울 비공개 | **Pass** | 접촉 전 세계 위치·지도 마커·방울 방향 화살표 공개를 금지하는 가드 적용. HUD 누출 위젯 0이며 잠금 문구도 위치 비공개를 명시. 출구가 열린 뒤에는 출구 방향만 안내. |
| 튜토리얼 콘텐츠 계약 | **Implemented / Unconfigured content** | 이동→점프→방울 접촉→출구 접촉→직접 동물 선택→상황 능력 순서 계약을 생성. `autoShowMandatoryPopup=false`; 실제 초반 운영 맵 콘텐츠가 없으므로 완료로 꾸미지 않음. |
| 20번째 이야기/연출 | **Slot Implemented / Unconfigured content** | `SC18_CampaignMilestone`에 이야기/연출 슬롯을 연결하고 `콘텐츠 미작성`으로 표시. 임의 스토리나 완성 상태 없음. |
| 모바일 HUD·조작 | **Implemented / Pass** | 좌측 스와이프 이동, 우측 독립 점프/특수, 좌하단 직접 동물 2개, 우하단 능력의 6개 액션. 시간·방울·스테미나·현재/인접 동물·능력·체크포인트를 상시 갱신. 특수/능력 코어는 `미연결·실행 안 함`. |
| 멀티터치 | **Pass** | 포인터 ID별 캡처 레지스트리로 이동/점프/특수/능력 4개 동시 입력 및 동일 ID 탈취 거절을 EditMode와 PlayMode에서 확인. |
| 조작·접근성 | **Editor Pass / Device Blocked** | 크기·좌우 위치·투명도, 큰 글씨, 진동, 한국어/영어 설정과 `SC19_ControlSettings` 실제 버튼 연결. Safe Area와 색 외 텍스트/✓/✕ 상태 구분 확인. 실제 노치·하드웨어 진동은 기기 부재로 차단. |
| 16:9·20:9 월드 시야 | **Pass (Editor/Game View)** | 카메라 rect를 전체 화면으로 유지하고 20:9 가로 월드 폭이 16:9보다 커지는 수식/캡처 확인. 기존 `랭크 공통 16:9 경기 시야` 문구 제거. 현재 실제 월드 씬은 Gameplay 하나이며 경쟁·협동 운영 월드는 서버/맵 미제공 상태다. |
| 경쟁 룸·해금 동물 선택 | **Implemented / Pass (Dev preview)** | 경쟁 4~8명 가변 룸과 지상·공중·특수 3역할 선택 계약을 연결. 캠페인 진행 스냅샷에 해금된 동물만 유효하고, 참가자 간 같은 특수 동물 선택 후 각자 Ready 조건 충족을 허용. 캠페인 스테이지 고정 동물 경로에는 연결하지 않음. 운영 Ready는 서버 미연결로 계속 비활성. |
| 경쟁 3맵·방울·완주 권위 | **Contract/UI Implemented / Operational Blocked** | 현재 맵/3, 맵별 상태·서버 수행 점수·총합, 동일 방울 슬롯 선점/서버 확정 재생성 대기, 개인 방울 0/3·출구 자격, 첫 완주 후 추가 시간, 타임아웃 점수, 끊김→재접속→복구를 HUD에 분리. 서버 확인 전 결과/방울/랭킹을 변경하지 않으며 몸 공격·넉백은 모두 false. 실제 운영 매치는 매치 서버 부재로 차단. |
| 협동 룸·HUD | **Implemented / Pass (Dev preview)** | 협동 2~4명 가변 렌더링, 팀 방울 0/3, 탈출 0/n, 팀원 재접속, 이모티콘·위치 핑 포트를 연결. 기존 `DEV-MULTI-COOP`의 2슬롯·중복 금지 모드 데이터를 그대로 보존. 서버 전송은 성공으로 표시하지 않고 오류 모달로 귀결. |
| 성장 프리셋·운영 진실성 | **Pass** | 랭크는 전원 성장 최대치 프리셋, 일반 경쟁·협동은 보유 성장치로 분기. 개발 미리보기와 운영 매칭을 분리하고 운영 Ready/순위/보상은 false·미확정 상태 유지. |
| 멀티 화면 비율·상태 캡처 | **Pass (Editor/Game View)** | 4명 경쟁·2명 협동 1920×1080, 8명 경쟁·4명 협동 2400×1080 레이아웃을 검사하고 상단 겹침을 수정. 3맵 총합+복구 상태 랭크 HUD 및 실제 Gameplay 월드가 보이는 20:9 랭크 오버레이를 캡처. |
| 컴파일·자동 테스트 | **Pass** | Unity 컴파일 오류/현재 경고 0. EditMode 29/29, PlayMode 11/11. M6 정적 검증 `valid=True`. |

## 핵심 산출물

### M0

- 코어 계약: `Assets/ANIMOL/Scripts/Core/`
- 카탈로그 생성·검증: `Assets/ANIMOL/Scripts/Editor/CampaignCatalogGenerator.cs`, `UiBuildPipeline.cs`
- 카탈로그: `Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset`
- 테마 5개: `Assets/ANIMOL/Data/Campaign/Themes/`
- 스테이지 100개: `Assets/ANIMOL/Data/Campaign/Stages/`
- 검증 로그: `Logs/animol-m0-validation.txt`

### M1

- UI 런타임: `Assets/ANIMOL/Scripts/UI/`
- 공통 UI·화면 생성기: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M1.cs`
- 씬: `Assets/ANIMOL/Scenes/Bootstrap.unity`, `Lobby.unity`
- 프리팹: `Assets/ANIMOL/Prefabs/UI/UI_CommonRoot.prefab`, `SC00_Boot.prefab`, `SC01_Lobby.prefab`, `SC02_ThemeSelect.prefab`, `SC03_StageSelect.prefab`, `SC04_StageDetail.prefab`, `SC13_Settings.prefab`, `SC14_Help.prefab`, `ConfirmExitModal.prefab`, `ToastAndErrorOverlay.prefab`
- 검증 로그: `Logs/animol-m1-validation.txt`
- 화면 증거: `Assets/ANIMOL/Screenshots/M1_Bootstrap_1920x1080.png`, `M1_Lobby_1920x1080.png`

### M2

- 개발 런 코드: `Assets/ANIMOL/Scripts/Gameplay/`
- 생성·검증기: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M2.cs`
- 개발 데이터: `Assets/ANIMOL/Data/Development/DEV-TEST-01.asset`
- 씬: `Assets/ANIMOL/Scenes/Gameplay.unity`, `Results.unity`
- 게임플레이 프리팹: `Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab`, `DevBubble.prefab`, `DevExitGate.prefab`
- UI 프리팹: `SC09_MapLoading.prefab`, `SC15_GameplayHud.prefab`, `SC16_Result.prefab`, `SC18_CampaignMilestone.prefab`, `PauseModal.prefab`
- 검증 로그: `Logs/animol-m2-validation.txt`
- 화면 증거: `Assets/ANIMOL/Screenshots/M2_Gameplay_16x9.png`, `M2_Gameplay_20x9.png`, `M2_Result_16x9.png`

### M3

- 성장·계정 계약: `AccountProgression.cs`, `AccountUpgradeCatalog.cs`, `CharacterUpgradeDefinition.cs`, `CharacterUpgradeCatalog.cs`, `ExternalServiceConfiguration.cs`
- 이모티콘 계약: `EmoteCollection.cs`, `EmoteCatalog.cs`
- 프레젠터: `Assets/ANIMOL/Scripts/UI/MetaUiPresenter.cs`
- 데이터: `Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset`, `CharacterUpgradeCatalog.asset`, `EmoteCatalog.asset`, `Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset`
- UI: `SC10_UpgradeHub`, `SC10A_AccountUpgrade`, `SC10B_CharacterUpgrade`, `SC11_Store`, `SC12_EmoteCollection`, `SC17_ProfileRecords`, `RewardedAdModal`, `ProductDetailModal`, `ServiceErrorModal`
- 생성·검증: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M3.cs`, `Logs/animol-m3-validation.txt`
- 화면 증거: `Assets/ANIMOL/Screenshots/M3_AccountUpgrade_16x9.png`

### M4

- 멀티 계약: `ModeLoadoutRuleSet.cs`, `MultiplayerPreviewDefinition.cs`, `MultiplayerLoadoutService.cs`
- 프레젠터: `Assets/ANIMOL/Scripts/UI/MultiplayerUiPresenter.cs`
- 개발 전용 룰: `Assets/ANIMOL/Data/Development/DEV-MULTI-COMPETITIVE.asset`, `DEV-MULTI-COOP.asset`
- UI: `SC05_CompetitiveHub`, `SC08_CoopHub`, `SC06_MatchRoom`, `SC07_MultiplayerAnimalSelect`, `HUD_Competitive`, `HUD_Coop`, `MultiplayerPauseModal`
- 생성·검증: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M4.cs`, `Logs/animol-m4-validation.txt`
- 화면 증거: `Assets/ANIMOL/Screenshots/M4_CompetitiveHud_20x9.png`

### M5

- 정적 수락 검증기: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M5.cs`
- 수락 상호작용 테스트: `Assets/ANIMOL/Tests/PlayMode/M5AcceptanceFlowTests.cs`
- 성장 상태 보강: `AccountUpgradeStateResolver`가 `Available`, `InsufficientCoins`, `Max`, `Unavailable`을 분리하며 EditMode에서 검증.
- 복귀 경로 보강: `SC09_MapLoading`의 로비 복귀, `SC18_CampaignMilestone`의 로비 복귀 및 검증 전/최종 맵 기본 다음 버튼 비활성.
- 검증 로그: `Logs/animol-m5-validation.txt`, `Logs/animol-m5-test-results.txt`, `Logs/animol-m5-visual-validation.txt`
- 화면 증거: `Assets/ANIMOL/Screenshots/M5_Lobby_16x9.png`, `M5_CampaignDetail_16x9.png`, `M5_StageCatalog_20x9.png`, `M5_GameplayTouch_20x9.png`

### M6

- 캠페인 런 계약: `CampaignStageRuntimePolicy.cs`, `CampaignTutorialContentContract.cs`, `CampaignRunRules.cs`
- 변경 코어: `CampaignStageDefinition.cs`, `ContentAvailabilityResolver.cs`, `RunSessionController.cs`, `CampaignProgressionService.cs`
- DEV 런: `DevTestRunDefinition.cs`, `DevTestSession.cs`, `DevPlayerController.cs`, `DevCheckpoint.cs`, `WideWorldCameraPolicy.cs`
- 모바일 입력: `MobileGameplayControls.cs`, `DevMobileInputRouter.cs`, `DevMobileTouchControl.cs`
- 조작·접근성 UI: `MobileControlPreferences.cs`, `MobileControlLayoutApplier.cs`, `MobileControlSettingsPresenter.cs`
- 캠페인/결과 UI 변경: `CampaignUiPresenter.cs`, `DevResultsPresenter.cs`
- 증분 생성·검증기: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6.cs`
- 신규 데이터: `Assets/ANIMOL/Data/Campaign/CampaignTutorialContentContract.asset`; 기존 `Assets/ANIMOL/Data/Development/DEV-TEST-01.asset`에는 시간/체크포인트/고정 배치 seed만 증분 보강.
- 변경 씬·프리팹: `Lobby.unity`, `Gameplay.unity`, `SC04_StageDetail`, `SC05_CompetitiveHub`, `SC13_Settings`, `SC15_GameplayHud`, `SC16_Result`, `SC18_CampaignMilestone`, 신규 `SC19_ControlSettings`.
- 테스트: `CampaignCoreTests.cs`, `M6CampaignMobileFlowTests.cs`; 기존 `DevTestFlowTests.cs` 물리 흐름도 함께 통과.
- 멀티 계약·프레젠터: `Assets/ANIMOL/Scripts/Core/MultiplayerMatchContracts.cs`, `Assets/ANIMOL/Scripts/UI/MultiplayerUiPresenter.cs`.
- 멀티 증분 생성기: `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs`; 기존 M4 프리팹과 개발 데이터에 증분 적용.
- 멀티 테스트: `Assets/ANIMOL/Tests/EditMode/M6MultiplayerContractTests.cs`, `Assets/ANIMOL/Tests/PlayMode/M6MultiplayerUiFlowTests.cs`.
- 로그: `Logs/animol-m6-compile.txt`, `Logs/animol-m6-validation.txt`, `Logs/animol-m6-test-results.txt`, `Logs/animol-m6-visual-validation.txt`, `Logs/animol-m6-multiplayer-validation.txt`.
- 화면 증거: `Assets/ANIMOL/Screenshots/M6_Gameplay_16x9.png`, `M6_Gameplay_20x9.png`, `M6_ControlSettings_16x9.png`, `M6_CompetitiveRoom_4P_16x9.png`, `M6_CompetitiveRoom_8P_20x9_Disconnected.png`, `M6_CoopRoom_2P_16x9.png`, `M6_CoopRoom_4P_20x9_Disconnected.png`, `M6_RankedHud_3Map_Restored_20x9.png`, `M6_RankedWorld_20x9.png`.

## 최종 검증 결과

- Unity 컴파일: 성공, 프로젝트 스크립트 오류 0.
- M0 정적 검증: `themes=5`, `stages=100`, `duplicateStageIds=0`, `readyStages=0`, `unassignedStages=100`.
- M1 정적 검증: 테마 카드 5, 스테이지 카드 20, 캠페인 동물 선택 버튼 0.
- M1 실행 검증: 테마 01~05 각각 `20개 스테이지`와 카드 20개 표시. `T05-S20` 상세 `animalButtons=0`, `start=False`.
- M3 정적 검증: 계정 트랙 3, 각 Lv.0~10/가격 10개, 동물별 정의 0(Unconfigured), 무료 이모티콘 3, 퀵 슬롯 4, 상품 구매 버튼 비활성, 계정·결제·광고 연결 false.
- M4 정적 검증: 매치 연결 false, 운영 Ready 비활성, 개발 미리보기 룰 슬롯 2/허용 동물 2, 캠페인 동물 선택 진입점 0, 경쟁·협동 HUD 퀵 슬롯 각각 4.
- EditMode: **29 passed / 0 failed / 0 skipped**. 기존 21개에 참가자 범위, 캠페인 해금 스냅샷, 경쟁 역할·타 참가자 중복, 협동 중복 규칙 보존, 서버 확정 3맵·방울·협동 목표, 성장 프리셋 검증을 추가.
- PlayMode: **11 passed / 0 failed / 0 skipped**. 기존 8개에 경쟁 4↔8명·3맵, 랭크 성장·끊김/재접속/복구, 협동 2↔4명·팀 목표·핑 오류 흐름 검증을 추가.
- PlayMode 물리 검증: 3개 전 출구 접촉 거절, 허용 외 동물 전환 거절, A/B/C 방울의 실제 트리거 접촉 후 출구 개방, 개방만으로 미완료, 출구 재접촉 때 `Results` 로드, DEV 결과 3개 기록.
- 일시정지: HUD 버튼→`Time.timeScale=0`과 모달 표시→계속 버튼→`Time.timeScale=1`을 같은 PlayMode 테스트에서 확인.
- M3·M4 UI 실행 검증: 성장/상점/상품/오류 왕복, 구매 비활성, 이모티콘 4슬롯, 경쟁 운영 실패 모달, DEV 선택→HUD, 온라인 메뉴 `Time.timeScale=1`, HUD 이모티콘 전송 실패를 실제 버튼으로 확인.
- M6 멀티 UI 실행 검증: 경쟁 4/8명·협동 2/4명 행 수, 참가자 간 동일 특수 동물 허용 계약, 랭크 최대 성장/일반·협동 보유 성장, 3맵 결과 총합 600점(DEV 주입임을 명시), 운영 Ready 비활성, 팀 방울·탈출 x/n, 끊김/재접속/복구 표기, 이모티콘·위치 핑 실패 모달을 실제 버튼으로 확인.
- 16:9 / 20:9: M6 Gameplay를 1920×1080과 2400×1080으로 새로 캡처. 여섯 입력 영역과 HUD 잘림이 없고 20:9 월드 폭이 확장되며 카메라 크롭이 없음. 조작·접근성 화면도 1920×1080 Safe Area 안에서 확인.
- 멀티 16:9 / 20:9: 경쟁 4/8명과 협동 2/4명 룸에서 잘림·겹침 없음. 랭크 20:9 캡처에서 월드 좌우 폭 확장과 최대 성장 프리셋 문구를 동시에 확인.
- M5 실패 항목: **0**. 현재 Unity Console의 프로젝트 컴파일 오류도 0.
- 빌드 설정: `Bootstrap`, `Lobby`, `Gameplay`, `Results` 순서로 등록.

## 정본 수락 시나리오 대응

| 시나리오 | 결과 | 근거 |
|---|---|---|
| A1 5×20·중복 0·생성기 보존 | **Pass** | M0/M5 검증 로그, 생성기 재실행 후 수동 이름·맵 ID·고정 동물·초기 동물 보존 EditMode 테스트 |
| A2 캠페인 고정 동물·선택 UI 0·미준비 거절 | **Pass** | 5개 테마 실행 검사, 상세 버튼 검사, 고정 로스터 단위 테스트 |
| A3 방울 3→실제 출구·DEV 진행 격리 | **Pass** | Physics2D PlayMode 전체 흐름, DEV 진행 차단 단위 테스트 |
| A4 테마 경계·최종 다음 맵 없음 | **Pass** | `T01-S20→T02-S01`, `T05-S20→없음` EditMode 테스트와 `SC18` 프리팹 |
| A5 멀티 전용 선택·Ready·서버 진실성 | **Pass (UI/오프라인 계약)** | 캠페인 선택 경로 0, 경쟁 4~8·협동 2~4, 캠페인 해금 동물/3역할, 타 참가자 중복 허용, 협동 모드 중복 규칙 보존, 3맵·서버 권위·복구 UI, 운영 Ready 차단, DEV 미리보기 격리, 순위/보상 성공 없음 |
| A6 3트랙·동물 성장·이모티콘·상점 | **Pass (UI/오프라인 계약)** | 3트랙 데이터/중복 요청 테스트, 동물 정의 0 잠금, 무료 3/슬롯 4, SDK 없는 구매·광고 차단 |
| A7 M0~M6 네비게이션·다중 터치·비율 | **Pass (Editor/Game View 범위)** | 루트 화면/모달 왕복, 조작 설정 실제 버튼, 독립 pointer 액션 6개와 4개 동시 ID, 로컬/온라인 pause 규칙, 오류·로딩·빈 상태 복귀, 16:9/20:9 확장 캡처 |

## 남은 차단 사항

- 운영 100개 캠페인 슬롯을 `Ready`로 바꾸려면 각 슬롯의 실제 맵 템플릿, 도달 가능한 방울 슬롯, 검증된 출구 연결, 고정 허용 동물과 초기 동물이 필요하다.
- 같은 운영 슬롯에 제한시간, 고정 체크포인트, 동일 재시작 배치 ID, 선택 목표, 초반 튜토리얼 배치, 테마 20번째 이야기/연출 데이터가 제공되기 전까지 M6 RuntimePolicy는 0개로 유지한다.
- `DEV-TEST-01`은 위 운영 콘텐츠의 대체물이나 100맵 완성 증거가 아니며, 결과는 `CampaignProgress`·코인 원장을 변경하지 않는다.
- 계정 서버·코인 원장·결제/광고 SDK·상품 ID/현지 가격·환급 정책·매치 서버가 연결되기 전까지 구매, 보상, Ready, 실매치, 순위와 전적 확정은 계속 `Blocked`다.
- 실물 Android/iOS 기기가 연결되지 않아 노치·제스처 인셋이 있는 실제 Safe Area, Android 하드웨어 Back, 앱 백그라운드/복귀는 **환경상 Blocked**다. Editor의 `SafeAreaLayout`·CanvasScaler와 16:9/20:9 Game View 검사는 통과했지만 실기기 통과로 확대 기록하지 않는다.

## M6 성장/경제 후속 진행 표 (2026-09-28)

아래 표는 기존 M0~M5 완료 사실을 변경하지 않고, 이번 성장/경제 후속 작업에서 새로 구현·검증한 항목만 기록한다.

| 항목 | 상태 | 실제 구현·검증 결과 |
|---|---|---|
| 계정 공통 성장 3트랙 | **Implemented / Pass** | `AccountUpgradeCatalog`에 프로토타입 표식을 추가하고 최종 변화량을 `+8% / +8% / -6%`로 표시했다. 서버 미연결 상태에서는 구매와 적용이 계속 잠긴다. |
| 동물별 성장·해금 계약 | **Contract Implemented / Locked** | 동물마다 액티브/패시브 2트랙, 각 Lv.5 상한, 공유 코인·동물 숙련도 비용 포트를 추가했다. 동물 해금은 캠페인 완료 스테이지 ID만 사용한다. 실제 효과 키·숙련도 획득 정책·단계별 가격이 비어 있으므로 구매/강화 적용은 비활성이다. |
| 성장 완료 팩 | **Contract Implemented / Sale Blocked** | 계정 공통 3트랙만 포함하며 동물 성장과 동물 해금은 제외한다. 이미 사용한 코인의 코스메틱 교환 재화 보전 산식과 인증 판매 공급이 모두 미설정이므로 실판매를 차단했다. |
| 게스트·Google 계정 게이트 | **Implemented / Pass** | 게스트 캠페인 시작은 허용했다. Google SDK, 계정 서버, 결제 SDK가 없으므로 로그인 성공 화면·온라인·구매·정식 코인은 만들지 않는다. |
| 로컬/클라우드 저장 선택 | **Implemented / Pass (DEV preview)** | 로컬 기록과 클라우드 기록을 동시에 표시하고 사용자가 하나를 고르는 화면을 연결했다. 클라우드 권위와 실제 동기화는 연출하지 않으며 DEV 충돌 미리보기만 별도로 제공한다. |
| 이모티콘 공급 분리 | **Implemented / Pass** | 무료 3, 코인 교환 1, 유료 1 경로를 데이터로 분리했다. 운영 가격과 보유 상태는 인증 공급 스냅샷만 표시할 수 있고 현재 코인/유료 상품 버튼은 잠김 상태다. |
| 공용 선택형 광고 한도 | **Contract/UI Implemented / Operational Blocked** | 로비 일반 코인 광고와 캠페인 결과 기본 B 추가 광고가 계정당 일일 3회를 공유한다. 일반 광고 약 150코인은 튜닝 초안 데이터이며 런 중 HUD 진입점은 0개다. 잔여 횟수와 초기화 시각은 계정 서버 권위만 허용한다. |
| 결과 보상 산식 | **Implemented / Pass** | 결과 UI에 `기본 B / 시간 T / 부가 O / 광고 추가 B / 총액 2B+T+O / 오늘 남은 횟수`를 분리했다. 첫 클리어와 재클리어 B가 달라도 승인된 B만 한 번 추가하고 T/O는 그대로 두는 계약을 검증했다. |
| 지급 멱등성·동시 입력 | **Implemented / Pass (DEV ONLY)** | `accountId + resultId + rewardType`, 광고 증명 ID, 지급 요청 ID를 사용한다. 동일 결과 중복은 `Duplicate`, 동시 탭은 `Busy`; 성공 확정만 한도를 소진하고 실패·취소는 소진하지 않는다. DEV 어댑터에서 잔액 400, 승인 3/3, 중복·실패·취소·로그인 전 차단을 검증했다. |
| 운영 코인·광고 지급 | **Blocked** | 계정 서버와 광고 SDK가 없으므로 실코인 잔액, 광고 성공, 일일 카운터를 Unity 로컬 시계로 확정하지 않는다. 운영 지급 성공 상태는 없다. |
| 컴파일·정적·PlayMode | **Pass** | Unity 컴파일 오류 0. M6 정적 검증 `valid=True`, EditMode 38/38, PlayMode 15/15. 기존 물리 방울 3개→출구, 타이머, 체크포인트, 멀티터치, 멀티 UI 회귀도 포함해 통과했다. |
| 화면 비율·시각 확인 | **Pass (Editor/Game View)** | `Assets/ANIMOL/Screenshots/M6_AccountSave_16x9.png`, `M6_AnimalGrowth_16x9.png`, `M6_EconomyDev_20x9.png`를 실제 렌더링해 Safe Area 내 배치와 상태 문구를 확인했다. |

### 이번 후속 변경 파일과 로그

- 계약/데이터: `Assets/ANIMOL/Scripts/Core/GrowthEconomyContracts.cs`, `GrowthEconomyPolicyCatalog.cs`, `Assets/ANIMOL/Data/Meta/GrowthEconomyPolicyCatalog.asset`
- Presenter/생성기: `Assets/ANIMOL/Scripts/UI/GrowthEconomyUiPresenter.cs`, `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Economy.cs`
- 신규 UI: `Assets/ANIMOL/Prefabs/UI/SC20_AccountAndSave.prefab`, `SC21_EconomyDevPreview.prefab`; 기존 성장·상점·이모티콘·결과·광고 모달은 증분 수정
- 테스트: `Assets/ANIMOL/Tests/EditMode/M6GrowthEconomyContractTests.cs`, `Assets/ANIMOL/Tests/PlayMode/M6GrowthEconomyUiFlowTests.cs`
- 검증 로그: `Logs/animol-m6-validation.txt`, `Logs/animol-m6-economy-validation.txt`, `Logs/animol-m6-test-results.txt`, `Logs/animol-m6-compile.txt`, `Logs/animol-m6-visual-validation.txt`

## M6 전체 Unity Editor/CLI/MCP 재검증 (2026-09-28)

이 절은 기존 M0~M5 결과를 재분류하거나 M6 Pass로 복사하지 않고, 현재 프로젝트의 M6 전체를 다시 실행한 결과다. 검증 전후 M0~M5 로그 8개의 SHA-256이 모두 동일하며 삭제·덮어쓰기·내용 복사가 없었다.

### 환경과 수치

- Unity Editor/프로젝트: `6000.3.8f1`; Unity CLI `1.0.0-beta.11`; Pipeline `0.8.0-exp.1` 포트 7800 `ready`; MCP for Unity `ANIMOL@0bba0934` 포트 6400 `running`.
- 실제 컴파일: 프로젝트 오류 0, 경고 0. M6 정적 검증 `valid=True`.
- EditMode: **38/38 passed, 0 failed, 0 skipped**, 0.273초.
- PlayMode: **17/17 passed, 0 failed, 0 skipped**, 2.217초. 기존 15개에 Bootstrap 전체 왕복과 Safe Area/언어/뒤로/재진입 2개를 추가했다.
- 월드 폭: orthographic size 5.4 기준 16:9 `19.20`, 20:9 `24.00`, 증가 `4.80`(25%).
- 빌드 씬: `Bootstrap → Lobby → Gameplay → Results`.

### 항목별 최종 상태

| 수락 항목 | 상태 | 실제 검증 결과 |
|---|---|---|
| 5×20 고정 ID·캠페인 선택 경로 | **Implemented** | 테마 5, 스테이지 100, 중복 ID 0, 캠페인 동물 선택 진입점 0. 운영 Ready는 0으로 유지. |
| 운영 캠페인 콘텐츠 | **Unconfigured** | 운영 100개에 실제 맵·고정 동물·제한시간·체크포인트·스토리 근거가 없어 RuntimePolicy 0, Ready 0. 임의 완성 상태를 만들지 않음. |
| 타이머·체크포인트·동일 배치·숨김 방울·실제 출구 | **DevOnly** | `DEV-TEST-01`에서 45초, 체크포인트 2, seed 601/603, 미발견 위치/지도/화살표 누출 0, 고유 방울 3개 실제 접촉 후 실제 출구 재접촉만 완료. 시간 초과는 실패이며 진행/최고 기록을 만들지 않음. |
| 경쟁 4/8명·동일 동물·3맵 | **Implemented / DevOnly** | 4/8행 렌더링, 캠페인 해금 스냅샷과 3역할, 참가자 간 동일 특수 동물 Ready 조건 허용, 맵 1~3/개별 결과/총합/끊김 복구 UI 통과. 몸 공격·넉백 없음. |
| 협동 2/4명·팀 목표·전원 출구 | **Implemented / DevOnly** | 2/4행과 팀 방울/탈출 x/n UI 통과. 새 `IsComplete` 계약은 팀 방울 3/3과 참가자 전원 출구를 모두 요구하며 2인·4인 경계를 검증. |
| 운영 경쟁·협동 매치 | **Blocked** | 매치/권위 서버가 없어 운영 Ready, 랭킹, 보상, 방울 권위, 핑/이모티콘 전송 성공을 만들지 않음. DEV 미리보기만 분리. |
| 결과 B/T/O·광고 원장 | **Implemented / DevOnly** | B/T/O와 광고 추가 B를 분리하고 승인 시 `2B+T+O`; 일반 광고와 공용 3회. 동일 resultId 중복, 동시 탭, 로그인 전을 차단하고 실패·취소는 미차감. DEV 원장 잔액 400/승인 3회 확인. |
| 운영 광고·코인 지급 | **Blocked** | 광고 SDK·계정 서버·원장이 없어 실코인·광고 성공·일일 카운터를 로컬 시계로 확정하지 않음. |
| 동물 성장·상품 | **Unconfigured / Blocked** | 액티브/패시브 Lv.5 스키마와 캠페인 해금 UI는 구현. 실제 효과·숙련도 획득량·가격·인증 공급이 비어 구매/강화/실판매 잠금. |
| 게스트·Google·저장 선택 | **Implemented / Blocked** | 게스트 캠페인, 로컬/클라우드 기록 동시 표시와 명시 선택은 통과. Google SDK가 없어 로그인 성공/클라우드 동기화 성공은 표시하지 않음. |
| 동시 터치·KO/EN·접근성·HUD | **Implemented** | 액션 6개, 서로 다른 pointer ID 4개 동시 캡처, 동일 ID 탈취 거절. 한국어↔영어 왕복, 큰 글씨, 색 외 상태 기호, 진동 설정, 조작 크기/위치/투명도, HUD를 PlayMode에서 검증. |
| Safe Area·뒤로·재진입 | **Implemented (Editor)** | `Screen.safeArea`와 앵커 일치, CanvasScaler 1920×1080 기준, 캠페인/경쟁/협동/성장/상점/설정을 각 2회 왕복, Results→Lobby 복귀 통과. |
| 실기기 Safe Area·Back·진동 | **Blocked (Environment)** | 연결된 Android/iOS 기기가 없어 노치/제스처 인셋, 하드웨어 Back, 햅틱, 백그라운드/복귀는 실행 불가. Editor 결과를 실기기 Pass로 확대하지 않음. |
| 16:9·20:9 Game View | **Implemented (Editor)** | 1920×1080과 2400×1080 PNG를 실제 렌더링. HUD/Safe Area 잘림 없음, 20:9 월드 가로 폭이 25% 확장되고 랭크 HUD에도 운영 미연결 상태가 유지됨. |

### 이번 검증에서 변경·확인한 파일

- 변경 스크립트: `Assets/ANIMOL/Scripts/Core/MultiplayerMatchContracts.cs`(`CoopTeamObjectiveState.IsComplete`), `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Multiplayer.cs`(2/4인 전원 출구 정적 검증), `Assets/ANIMOL/Scripts/Editor/UiBuildPipeline.M6Economy.cs`(UI 포함 캡처 준비 루틴 공개).
- 신규/변경 테스트: `Assets/ANIMOL/Tests/PlayMode/M6FullAcceptanceFlowTests.cs`, `Assets/ANIMOL/Tests/EditMode/M6MultiplayerContractTests.cs`.
- 프리팹·씬·데이터: 이번 재검증에서 내용 변경 없음. 기존 `UI_CommonRoot`, `SC01~SC21`, `HUD_Competitive`, `HUD_Coop`, `DEV-TEST-01`, `DEV-MULTI-*`, `CampaignCatalog`, `GrowthEconomyPolicyCatalog`을 실제 로드해 검증했다.
- M0~M5 로그 해시: `m0 17386538…`, `m1 8AA1B009…`, `m2 B3EB5FC4…`, `m3 C2CDFAD9…`, `m4 E20CA839…`, `m5-validation A5079083…`, `m5-tests 89725B0A…`, `m5-visual 5D2B8081…`; 검증 전후 동일.

### 로그와 화면 캡처

- 최종 로그: `Logs/animol-m6-full-acceptance.txt`, `Logs/animol-m6-validation.txt`, `Logs/animol-m6-test-results.txt`, `Logs/animol-m6-compile.txt`, `Logs/animol-m6-visual-validation.txt`, `Logs/animol-m6-multiplayer-validation.txt`, `Logs/animol-m6-economy-validation.txt`.
- 16:9: `Assets/ANIMOL/Screenshots/M6_Acceptance_Lobby_16x9.png`(1920×1080), `M6_Acceptance_GameplayHUD_16x9.png`(1920×1080).
- 20:9: `Assets/ANIMOL/Screenshots/M6_Acceptance_ControlSettings_20x9.png`(2400×1080), `M6_Acceptance_RankedWorld_20x9.png`(2400×1080).
