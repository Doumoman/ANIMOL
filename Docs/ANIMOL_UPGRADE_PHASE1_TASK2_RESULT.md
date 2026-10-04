# ANIMOL 동물 강화 1단계 · 작업 2 결과

작성: 2026-10-04 / 현재 작업 트리 / Unity 6000.3.8f1

## 결과와 미완료 범위

작업 1은 `3e45e3e feat(ui): integrate read-only animal upgrade phase 1`로 이미 적용되어 있었다. 기존 화면을 재생성하지 않고 운영 CharacterUpgrade 프리팹에 `AnimalUpgradeProjectAdapter : AnimalUpgradeBackendAdapterBase`를 연결했다.

**실제 동물 구매 연결은 완료되지 않았다.** 현재 저장소에는 동물별 레벨·숙련도·보유 상태 저장소와 견적/원자적 구매/거래 결과 조회 서비스가 없다. 기존 데이터 경계를 읽고 미설정·서비스 미지원 상태에서 차단하는 연결까지 완료했다. 가격, 효과, 성장 상한, 해금 조건, 가짜 승인 결과나 대체 성장 서비스를 만들지 않았다. 실제 서비스/별도 브랜치 위치에 관한 질문을 남겼으며 이 보고 시점에는 추가 위치가 제공되지 않았다.

## 확인한 현재 계약과 데이터 소유자

경로는 별도 표시가 없으면 `Assets/ANIMOL/` 기준이다.

| 실제 소스/asset | 확인 결과와 사용 여부 |
|---|---|
| `UI/PortraitArtV1/PortraitEntryController.cs` | `Ledger`와 `BindVerifiedLedger(IRewardLedgerAdapter)`가 기존 로비 코인 경계. 부모 `UiNavigationService` 아래 해당 controller를 찾아 현재 ledger를 매 조회 시 읽는다. |
| `Scripts/Core/GrowthEconomyContracts.cs` | `IRewardLedgerAdapter.IsAvailable && !IsDevelopmentOnly`일 때만 `VerifiedCoinBalance` → `Snapshot.CoinBalance`. 현재 기본값은 `DisconnectedRewardLedgerAdapter`이므로 코인은 `--`. `ConfirmApprovedGrant`는 호출하지 않는다. |
| `Scripts/Core/CampaignAnimalDefinition.cs`, `Data/Campaign/Animals/RABBIT.asset` | 유일하게 확인된 실명 종 정의는 `RABBIT`. 액티브/패시브 production configured 플래그와 purchasable은 false. 공용 이동 구현은 동물 고유 능력·성장 구현의 증거가 아니다. |
| `Scripts/Core/CharacterUpgradeCatalog.cs`, `Data/Meta/CharacterUpgradeCatalog.asset` | definitions가 비어 있다. preview ID는 DEV 폼 ID다. `CharacterUpgradeDefinition`에는 active/passive 구분이나 레벨별 표시 효과 해석기가 없다. |
| `Scripts/Core/GrowthEconomyPolicyCatalog.cs`, `Data/Meta/GrowthEconomyPolicyCatalog.asset` | `FindAnimal`은 정확한 ID 조회. DEV_GROUND/DEV_GLIDER/DEV_SPECIAL만 있고 효과 키·코인 비용·숙련도 비용·숙련도 획득 규칙이 미설정. 15종으로 재해석하지 않았다. |
| `Scripts/Core/AccountProgression.cs` | `IAccountGateway.RequestUpgrade(transactionId, trackId, expectedLevel, catalogVersion)`는 계정 공통 성장용. 동물 ID/트랙/숙련도/견적 토큰 계약이 없다. 기본 구현은 disconnected. 동물 강화에 전용하지 않았다. |
| `UI/ProductionV1/ProductionController.cs` | 계정 성장 `ApprovedGrowthSnapshot`과 `BindAccountGateway`가 존재한다. 동물별 진행도 소유자가 아니다. 수정하지 않았다. |
| `Scripts/UI/GrowthEconomyUiPresenter.cs` | DEV ledger/미리보기 경로. 운영 코인이나 동물 보유 상태로 사용하지 않았다. |
| `Scripts/Core/CampaignProgressionService.cs` | 캠페인 클리어 기록의 세션 메모리 서비스. 동물 레벨/숙련도 저장소가 아니다. |
| `GrowthEconomyContracts.cs`의 `SaveConflictSelectionService` | 로컬/클라우드 저장 선택 계약이며 실제 동물 성장 영속 저장 구현은 없다. |

저장소의 실제 클래스·호출·asset을 기준으로 확인했다. 과거 문서의 이름만으로 메서드를 가정하거나 계정 BM 가격을 변경하지 않았다.

## 명시적 ID 매핑

운영 매핑 소유자: `UI/AnimalUpgradePhase1/AnimalUpgradeIdMap.asset`. Generated 밖의 작업 1 매핑을 그대로 보존했다. `CampaignAnimal` 참조와 `GrowthServiceId`는 별개다. 존재하지 않는 성장 ID를 종 이름으로 만들어 넣지 않는다.

| 안정 아트 ID | 탭 | 확인된 프로젝트 종 ID | 실제 성장 서비스 ID |
|---|---|---|---|
| Rabbit | 지상 | RABBIT | 미확인/비어 있음 |
| Wolf | 지상 | 미매핑 | 미확인/비어 있음 |
| WhiteFerret | 지상 | 미매핑 | 미확인/비어 있음 |
| MountainGoat | 지상 | 미매핑 | 미확인/비어 있음 |
| Otter | 지상 | 미매핑 | 미확인/비어 있음 |
| DreamFox | 특수 | 미매핑 | 미확인/비어 있음 |
| StarCat | 특수 | 미매핑 | 미확인/비어 있음 |
| MirrorDeer | 특수 | 미매핑 | 미확인/비어 있음 |
| DreamMole | 특수 | 미매핑 | 미확인/비어 있음 |
| ClockMoth | 특수 | 미매핑 | 미확인/비어 있음 |
| Swallow | 공중 | 미매핑 | 미확인/비어 있음 |
| Owl | 공중 | 미매핑 | 미확인/비어 있음 |
| FlyingSquirrel | 공중 | 미매핑 | 미확인/비어 있음 |
| Hummingbird | 공중 | 미매핑 | 미확인/비어 있음 |
| Bat | 공중 | 미매핑 | 미확인/비어 있음 |

정확한 대소문자/ordinal ID만 조회하며 중복 매핑은 무효 처리한다. Rabbit 이외 14종은 현재 종 구현이 확인되지 않았다. Rabbit도 고유 능력·성장 구현이 미설정이다. 15종 모두 열람할 수 있지만 현재 강화할 수 없다. 해금·소유·context permission은 확인되지 않아 false를 유지한다. 이것은 서버가 잠김을 확정했다는 뜻이 아니다. UI 상태에는 `성장 설정 대기` / `ID 설정 대기`를 표시한다. 열람은 편성·보유·저장 데이터를 변경하지 않는다.

## 어댑터 동작과 호출 경로

운영 프리팹: `UI/AnimalUpgradePhase1/Resources/ANIMOLAnimalUpgradePhase1/CharacterUpgrade.prefab`.

기존 `MetaUiPresenter.UpgradeHubCharacterButton` 및 `ProductionController`의 `PV1_Character` → `SC10B_CharacterUpgrade` → `AnimalUpgradePhase1Entry`/`AnimalUpgradePhase1Host` → `AnimalUiPresenter` → `AnimalUpgradeProjectAdapter` 경로를 유지한다. 뒤로는 기존 허브로 돌아간다.

- `ReadSnapshotAsync`: 기존 검증된 코인 ledger만 읽는다. 미연결/DEV ledger의 값을 채택하지 않는다. 각 동물의 실제 숙련도와 레벨을 제공하는 저장소가 없으므로 `MasteryBalance=null`, 레벨=-1, 효과=null이다. 계정 레벨이나 다른 동물 숙련도를 복사하지 않는다. 가짜 snapshot revision을 발행하지 않는다.
- `QuoteUpgradeAsync`: 미매핑/성장 ID/카탈로그 누락이면 `Unconfigured`. ID와 카탈로그가 채워지더라도 실제 서비스가 없는 현재 구현은 `Unavailable`로 차단한다. 상세 모달에서 구체적인 사유를 읽을 수 있다. 현재 운영 15종 견적은 모두 `Unconfigured`다.
- 실제 진행도가 없으므로 `InsufficientFunds`, `Maximum`, `Locked`를 추측해 반환하지 않는다. 이 상태들과 `Ready`를 실제 서비스로부터 변환하는 연결은 미완료다. 현재 `QuoteToken`과 비용 배열은 비어 있고 `IsFree=false`; 비용 누락을 무료로 취급하지 않는다.
- `TryUpgradeAsync`: 강제로 호출해도 `Unavailable`과 명확한 메시지를 반환한다. 어떠한 gateway/저장/차감 요청도 발행하지 않는다. 실제 서비스가 확정한 거래가 없으므로 `Accepted`를 반환하지 않는다.
- `SubmitCampaignAsync` / `SubmitMultiplayerAsync`: 기반 클래스의 sealed 구현으로 `Unavailable`, `이 어댑터는 1단계 동물 강화만 연결합니다.`를 반환한다. 스테이지/방 생성 요청 및 운영 진입 연결은 없다.

메뉴 `ANIMOL > Animal UI v2 > Connect production Upgrade Phase 1 project backend`를 실제 Editor에서 실행했다. 기존 운영 프리팹에 어댑터 컴포넌트와 참조만 추가한다. 다른 종류의 기존 backend가 연결되어 있으면 예외로 중단하고 덮어쓰지 않는다. 이미 지정한 프로젝트 asset 참조도 보존한다. 기존 missing-only 생성 메뉴는 이미 존재하는 운영 프리팹·매핑·카탈로그를 다시 만들지 않는다. Generated 데모와 원본 PNG에는 변경이 없다.

## 확인·중복 방지와 미확인 결과의 한계

공용 presenter의 기존 확인 모달은 동물/트랙/레벨 변화/전체 비용/현재·다음 효과를 표시한다. 긴 효과와 비용은 기존 상세 스크롤 모달에서 읽는다. 비용 표시 영역에는 미설정 안내를 짧게 유지하고 상세 실패 사유는 모달에 둔다.

확정 시 presenter는 `ActionId`, `AnimalId`, `Track`, `QuoteToken`, `ExpectedCurrentLevel`을 보관한다. 취소는 서비스 호출이 없다. `_pending`/`_uncertain` 동안 중복 확정, 동물·트랙 변경, 뒤로 이동을 막으며 코인·숙련도·레벨을 낙관적으로 변경하지 않는다. 알 수 없는 응답/예외는 Unknown 상태로 동일한 요청 내용을 유지한다. 확정 거절 후에는 이번 변경으로 snapshot과 견적을 다시 조회하고 실제 거절 메시지를 유지한다. 다음 구매는 다시 확인해야 한다.

**현재 서비스에 멱등 처리나 결과 조회가 존재하지 않는다.** `UpgradePurchaseUseCase`의 메모리 HashSet 중복 거절은 영속적인 거래 결과 조회가 아니다. 중앙 동물 요청 관리자도 발견하지 못했다. presenter의 같은 인스턴스 비활성/재활성 동안 요청 보존은 테스트했지만, 씬 파괴·앱 종료·도메인 재로드를 넘는 보존은 구현/검증하지 않았다. 운영 어댑터가 요청을 아예 전송하지 않으므로 현재 미확인 실제 거래를 만들지 않는다. 실서비스 연결 시 이 미완료 계약을 해결하기 전 구매를 활성화하면 안 된다.

실제 서비스 연결에 필요한 것은 기존 서비스의 위치/API, 동물별 권한 있는 진행도/해금 조회, 전체 효과/가격/상한과 버전이 있는 견적, 토큰 검증, 최신 레벨·가격 버전·잔액 재검증과 원자적 차감/증가, 동일 ActionId 결과 조회, 계정별 요청 영속 관리다. 이번 작업에서 이를 가짜 서비스로 대체하지 않았다.

## 실제 실행한 검증

실행 Editor: Unity 6000.3.8f1, 기존 연결된 Editor, CLI port 7800. 검증 결과 원문: [unity-verification-results.json](UpgradePhase1Task2/unity-verification-results.json).

| 검사 | 실제 결과 |
|---|---|
| 현재 프로젝트 AssetDatabase.Refresh 후 Unity 컴파일 | compilationFailed=false, compiling=false, consoleErrors=0 |
| EditMode `UpgradePhase1` | 7/7 통과: 운영 asset 경계, 생성 시 보존, 15종 importer/매핑, 진행도 unknown/구매 불가, verified/DEV/미연결 ledger 구분, 강제·반복 구매/선택 차단, 잘못된 ID 차단 |
| PlayMode `UpgradePhase1` | 4/4 통과: 두 허브 경로/반환·기존 계정 화면, 15종 Game View/Safe Area/스크롤, 취소·중복·비낙관적 UI·거절 재조회, Unknown 동일 요청 재확인 |
| EditMode `CommercialPolishPolicyTests` | 3/3 통과 |
| Game View | 1080×1920 및 1080×2400, 기본과 좌48/우48/아래96/위120px 모의 Safe Area에서 15종 열람; 초상 영역·이름 overflow·실제 drag 전달·고정 하단 버튼 검증 통과. 64개 캡처 생성 |
| 육안 재검토 | 이번 실행의 `1920_safe_Rabbit.png`, `2400_safe_ClockMoth.png` 확인: 토끼 귀/나방 날개가 잘리지 않고 설정 대기/알 수 없는 값 표시. 두 캡처를 증빙에 보관 |

거래 UI 테스트의 `UpgradeTransactionTestBackend`와 ledger fixture는 **테스트 전용 가상 값**이다. 운영 asset에는 연결되지 않는다. 이 통과 결과는 실제 서버 구매·차감·원자성·멱등 저장·재시작 복구를 검증한 것이 아니다. 실제 서비스가 없어 해당 검증은 수행하지 못했다. 64개 캡처 전체를 이번 작업에서 육안 재검토했다고 주장하지 않는다.

검증 후 기존 Lobby 씬으로 복귀했고 `isPlaying=false`, scene dirty=false를 확인했다. 테스트가 만든 TMP fallback atlas/자동 아트 참조 캐시 변경은 검사 직전 바이트로 복원했다. 작업 시작부터 있던 다른 미커밋 변경은 포함하지 않았다. 계정 성장 UI·v7 배경/발판/토끼 달리기 에셋에는 이번 변경이 없다.

## 이번 변경 파일

- `UI/AnimalUpgradePhase1/AnimalUpgradeProjectAdapter.cs`와 `.meta`: 현재 프로젝트의 검증된 조회 경계 및 거래 차단.
- `UI/AnimalUpgradePhase1/ANIMOL.AnimalUpgradePhase1.asmdef`: 기존 PortraitArtV1 데이터 경계 참조.
- `UI/AnimalUpgradePhase1/Editor/AnimalUpgradePhase1Builder.cs`: 운영 backend 연결 메뉴와 누락 시 생성 연결.
- `UI/AnimalUpgradePhase1/Resources/ANIMOLAnimalUpgradePhase1/CharacterUpgrade.prefab`: 어댑터 및 기존 데이터 asset 참조.
- `UI/AnimalUpgradePhase1/AnimalUpgradePhase1Host.cs`: 작업 1 전용이던 주석 갱신.
- `AnimalUiV2/Runtime/AnimalUiPresenter.cs`: 실제 availability 사유, 미설정 비용 표시, 확정 거절 후 재조회/메시지 보존.
- `UI/AnimalUpgradePhase1/Tests/EditMode/UpgradePhase1AssetTests.cs`, `UpgradePhase1BackendTests.cs` 및 새 `.meta`.
- `UI/AnimalUpgradePhase1/Tests/PlayMode/UpgradePhase1FlowTests.cs`, `UpgradePhase1TransactionUiTests.cs`, `UpgradeTransactionTestBackend.cs` 및 새 `.meta`.
- 이 보고서 및 `Docs/UpgradePhase1Task2/`의 검증 JSON/대표 캡처 2개.

커밋은 이번 목적의 파일만 명시적으로 스테이징한다. 운영 실제 강화는 서비스/데이터 제공 후 이어서 연결해야 한다.
