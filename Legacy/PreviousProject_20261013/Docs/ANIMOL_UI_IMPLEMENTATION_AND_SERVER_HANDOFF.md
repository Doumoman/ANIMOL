# ANIMOL UI 구현 현황 및 서버 연동 인수인계

기준일: **2026-10-05** · Unity **6000.3.8f1** · 기준 HEAD: **82e777e**.

현재 작업 트리의 코드·asset과 단계별 결과를 대조한 통합 문서다. 기존 미커밋 작업도 현재 실행 환경에 포함되어 있으므로 HEAD 단독 checkout의 상태와 같다고 가정하지 않는다. 이번 작업은 문서 작성이며 코드·프리팹·데이터를 변경하거나 Unity/서버 검사를 새로 실행하지 않았다.

## 1. 현재 상태

**로비, 캠페인 목록, 강화, 동물 15종 열람, 상점, 설정, 경쟁 허브와 커스텀 진입 화면은 적용되어 있다. 실제 동물 성장 거래·캠페인 승인 입장·멀티플레이 방 입장·상품 구매/복원은 운영 서비스가 없어 차단되어 있다.**

실제로 저장에 연결된 대표 기능은 `MobileControlPreferences`의 조작·접근성 설정 8개다. 동물 열람이나 카드 선택을 실제 보유권·계정 편성·재화 저장으로 연결한 상태가 아니다. UI의 확인/처리/성공 모달이나 테스트용 Accepted 응답이 있다는 사실도 운영 성공의 증거가 아니다.

| 구분 | 이 문서의 의미 |
|---|---|
| 화면 적용 | 현재 프로젝트의 기존 진입 경로에서 화면을 여는 연결 또는 운영 프리팹이 있음 |
| 로컬 동작 | 기존 로컬 코드/설정 저장에서 실제 동작함. 서버·기기간 동기화 보장은 별개 |
| 연결 경계 있음 | adapter/interface/검증 코드가 있지만 실제 공급자는 미연결 |
| 미지원 차단 | 미조회 값은 `--`/설정 대기, 실행 버튼은 비활성. 차단 자체는 의도된 동작 |
| 독립 시안 | 패키지 Generated/데모/HTML에만 존재. 운영 적용으로 계산하지 않음 |
| 미검증 | 실제 서비스·기기 또는 해당 조건에서 실행 증거가 없음 |

외부 서비스 설정 asset의 `accountServerConnected`, `matchServerConnected`, `purchaseSdkConnected`, `rewardedAdSdkConnected`는 현재 모두 false다. **플래그를 true로 바꾸는 것은 서비스 구현이나 승인 연결이 아니다.**

## 2. 전체 화면별 구현표

아래 SC 번호는 기존 화면 컨테이너 ID다. 새 모듈은 이 컨테이너의 자식을 교체/보강하거나 기존 navigation에 연결한다. 비활성 레거시 화면이 남아 있을 수 있으므로 프리팹 파일 존재만으로 실제 노출 화면을 판정하지 않는다.

| 화면/기능 | 현재 구현 | 실제 동작·데이터 범위 | 남은 일 |
|---|---|---|---|
| 시작/부트 `SC00`, `PA1_Start` | 포트레이트 시작 화면, Play 및 기존 Bootstrap→Lobby 연결 | 로컬 진입 | 인증/버전 점검을 실제 서버 절차로 연결한 상태 아님 |
| 메인 로비 `SC01`, 포트레이트 모드 선택 | 캠페인·경쟁·상점·설정·강화 동선, 코인 조회 경계 | 경쟁은 열람 가능, 협동은 현재 온라인 미지원 차단, 코인은 검증된 ledger만 수용 | 실제 계정·잔액 조회, 광고·온라인 가용성 |
| v7 로비 배경/발판/토끼 | 20개 배경 레이어+좌우 토끼 2시트, 5테마 순환·픽셀 합성 | 기존 연출과 진입/반환 유지 | 토끼 시트의 기준 원본과 현 작업 트리 간 기존 해시 차이 별도 해결 |
| `SC02` 테마 선택 | ProductionV1 5테마 카드/진행/잠금/스크롤 | 실제 CampaignCatalog 및 세션 진행도 | 계정 저장/서버 진행도 복원 |
| `SC03` 스테이지 목록 | 테마별 실제 20개 스테이지, 잠김/제작 중/완료 표시 | 현재 총 100개 asset | 콘텐츠 준비와 승인 입장 조건 완성 |
| `SC04` 상세 | 목표·시간·기록·사용/시작 동물·강화 왕복 | 실제 선택 stage와 정적 준비 상태 | 고정 3종·권한·승인 데이터 |
| Phase2 스테이지 동물 확인 | 지정 3슬롯, 역할 탭 15종, 큰 초상, 상세/전체 편성 확인, 기존 목록 복귀 | `OpenCampaign(request)`, Fixed 문맥, 실제 stage/테마 연결. 열람으로 고정 편성 불변 | 현재 유효한 고정 3종/권한 조회/입장 receipt가 없어 시작 차단 |
| `SC09` 로딩 | 기존 로딩/진행률/실패·복귀/중복 시작 차단 경로 있음 | `StageLaunchUseCase`와 기존 SceneManager 경계 | Phase2 승인 receipt→기존 로더 1회 전달 미연결 |
| `SC10` 강화 허브 | 계정 공통/동물 성장 분기 및 기존 화면 복귀 | 기존 허브/선택 경로 | 실제 잔액·진행도 공급 |
| `SC10A` 계정 공통 성장 | 최대 스태미나/재생/소모 감소 3트랙, 확인·상태 모달 | 실제 카탈로그 참조, `BindAccountGateway` 경계. 기본 disconnected, 초안 카탈로그 차단 | 계정 승인 조회·원자적 구매·멱등 결과 확인. 가격/BM 재설계 대상 아님 |
| `SC10B` Phase1 동물 성장 | 15종/3탭, 큰 초상, 액티브/패시브, 전체 효과·비용 상세, 강화 확인 | 명시적 map와 프로젝트 adapter 연결, 미조회 값/비용 누락 차단 | 15종 실제 성장 조회·견적·거래·저장 전부 미연결 |
| `SC11` 상점 | 성장 완료 팩/이모티콘 번들 카드, 상세 스크롤, 복원 안내, 고정 뒤로 | 상품 설명 열람. 성장 팩 범위는 계정 공통 3트랙 | 실제 상품 구성·플랫폼 가격·보유권·구매/복원 receipt |
| `SC12` 이모티콘 | 기존 무료 3종 버튼 및 4 퀵 슬롯의 메모리 장착 코드 | 실제 무료 ID 존재. 새 PNG의 카탈로그 연결은 아직 없음 | Missing UI 작업 4 카드/선택 보강, 영속 장착·해제 계약 확인, 유료 보유권 별도 |
| `SC13` 설정 | 새 목록, 진동, 조작/접근성/계정/도움말/기록 진입 | 기존 진동 preference 및 하위 메뉴 연결 | BGM/SFX/음소거 적용·저장, 전체 설정 초기화 범위 |
| `SC19` 조작·접근성 | Slider/Toggle/언어, 실제 HUD 배치 미리보기, 초기화 모달 | 8개 PlayerPrefs 저장·재조회·씬 재진입·기존 Reset | 실기기 터치/진동·프로세스 재시작, 전체 번역 |
| `SC20` 계정·저장 | 게스트 상태, 로컬/클라우드 카드, 미조회 `--` | 현재 Guest만 실제 상태. 로그인/충돌 선택 실행 차단 | 인증·저장 요약 조회·충돌 commit·복구 |
| `SC14` 도움말 | 기존 화면과 새 설정 진입 링크 있음 | 기존 설명 | 작업 4 도해/카드/스크롤 및 현행 규칙과 문구 대조 |
| `SC17` 프로필·기록 | 기존 화면과 설정 진입 링크 있음 | 경쟁 기록은 서비스 미연결 안내 | 실제 프로필·캠페인/경쟁 기록 조회, 작업 4 빈 상태/오류 표현 |
| `SC05` 경쟁 허브 | 일반/랭크 카드와 커스텀 입장 방식 분리, 최대 4인 안내 | 기존 Phase3 열람 및 반환. 운영 ModeId/intent는 미조회 | 실제 모드 카탈로그/진입 의도/정책 연결 |
| 커스텀 분기/방 옵션 | 신규 운영 Custom/Create 프리팹, 옵션·편성 요약 | 옵션 미조회, 동물 열람 가능. 별도 방 생성 없음 | 허용 옵션·모드·계정 편성 공급 |
| 코드 참가 | 신규 Join 프리팹, 단일 TMP 입력·붙여넣기 | 문자열 입력만. 6자리/대문자 규칙 강제 없음 | 실제 코드 lookup/방 요약/오류/키보드 실기기 대응 |
| Phase3 멀티 동물 선택 | 15종 열람·3슬롯·전체 확인·원래 편성 반환·중복/Unknown 차단 | 현재 `OpenUnconfigured()`로 열람만 가능. 유효 권한을 넣은 선택/거래는 fixture에서 검증 | 실제 문맥/권한/입장/receipt/중앙 소비자 |
| `SC06` 대기방 | 미조회 4슬롯 틀, 본인/방장/연결/Ready/편성 `--` | 승인 전 새 허브에서 이 화면으로 이동하지 않음. 기존 DEV 협동 preview는 별도 유지 | 실제 참가자/방 상태 구독, Ready/시작/초대/코드 복사/나가기 권한과 처리 |
| `SC07` 기존 멀티 편성 | 레거시/DEV 경로 보존 | 운영 Phase3 화면과 혼동 금지 | 운영 방 승인 소비 경로 아님. 중복 진입 경로로 사용 금지 |
| `SC08` 협동 허브 | 기존 개발 preview/정책 유지 | 포트레이트 로비에서는 온라인 미지원 차단 | 작업 4 UI 보강, 협동 전용 서비스/정책. 경쟁 규칙 복사 금지 |
| `SC15` HUD/일시정지 | 기존 실제 플레이 표시·조작·폰트·Safe Area·설정 적용 | 현재 로컬/개발 플레이 경로 | 네트워크 매치 상태·권위 있는 결과 연결은 별개 |
| `SC16` 결과/보상 | 기존 플레이 결과 화면·연출/모달 | 로컬 결과/보상 계산과 DEV 지급 시험 구분 | 운영 결과 확정·원장 지급·중복 방지·광고 추가 지급 |
| `SC18` 이야기/마일스톤 | 기존 컨테이너/반환 경로 | 신규 대본/장면 연결 확인 안 됨 | 작업 4 공용 틀 보강, 실제 대본·진행 공급 |
| `SC21` 경제 DEV preview | 개발 계약/보상/저장 충돌 시험 UI | `DevRewardLedgerAdapter` 등 개발 데이터 | 출시 운영 데이터/실제 승인으로 승격 금지 |
| 공용 모달/오버레이 | 기존 navigation/modal stack, 오류/상품/광고/종료/멀티 일시정지의 스크롤 판면 보강 | 취소·닫기·중복 열기/Raycast 차단. 별도 동물/계정 확인 모달 유지 | 실제 오류·처리·최종 거절·Unknown과 서비스 상태 연결 |

**대기방 틀 적용과 실제 방 입장 성공은 별개다.** 최신 경쟁 UI 기준은 최대 4인이다. 과거 문서·DEV의 4~8인을 최신 출시 화면 기준으로 사용하지 않는다. 현재 Core의 4~8 계약은 아직 남아 있어 8절의 별도 이관이 필요하다.

## 3. 단계별 완료 범위

| 작업 묶음 | 끝낸 범위 | 미완료 |
|---|---|---|
| Animal Phase1 작업 1·2·3 | 화면·15종 아트·허브 연결·adapter·견적/거래 UI 방어·Unity/fixture 검증 | 실제 동물 성장 서비스와 저장·거래 수용기준 |
| Animal Phase2 작업 1·2·3 | 화면·실제 stage 문맥/테마·Fixed 검증·adapter·조회 경합/receipt 검사 | 고정 3종 데이터·계정 권한·승인 입장·로더 소비 |
| Animal Phase3 작업 1·2·3 | 화면·호스트 열람/반환·adapter·불변 요청/receipt/Unknown 방어·Unity/fixture 검증 | 실제 모드/intent·권한·방 입장·중앙 receipt 소비/복귀 |
| Missing UI 작업 1 | 상점 운영본·공용 모달 판면, 패키지 PNG 55개 및 독립 시안 도입 | 상품/구매/복원 연동 |
| Missing UI 작업 2 | 설정/조작/계정 운영본, 8개 로컬 preference와 초기화 | 오디오·계정/클라우드 서비스 |
| Missing UI 작업 3 | 경쟁 허브·커스텀·코드 입력·4슬롯 틀·기존 Phase3 재사용 | 실제 방 기능·4인 Core 이관·영속 요청 관리 |
| Missing UI 작업 4 | **미착수 상태로 분류**. 기존 화면/패키지 아트는 있음 | 이모티콘·기록·도움말·협동·이야기의 운영 보강. 작업 4 결과 문서 없음 |
| Missing UI 작업 5 | **미실행**. 이전 개별 Unity 증거는 있음 | 전 수용기준 최종 판정, Android/실서비스 검증. FINAL 결과 문서 없음 |

Missing UI `Generated`의 23개 독립 시안은 23개 운영 화면 구현 완료를 의미하지 않는다. 작업 1·2 당시 “다음 작업”으로 기록된 항목 중 경쟁 UI는 이번 작업 3 상태로 갱신했으며, 작업 4·5는 그대로 남아 있다.

## 4. 실제 코드와 운영 산출물 위치

프로젝트 내부 경로는 저장소 루트 기준이다. 링크는 이 문서에서 열 수 있도록 상대 경로로 제공한다.

| 소유 영역 | 파일/경로와 역할 |
|---|---|
| 화면 이동/모달 | [UiNavigationService.cs](../Assets/ANIMOL/Scripts/UI/UiNavigationService.cs), [UiModalStack.cs](../Assets/ANIMOL/Scripts/UI/UiModalStack.cs) |
| 로비 진입/검증 코인 | [PortraitEntryController.cs](../Assets/ANIMOL/UI/PortraitArtV1/PortraitEntryController.cs): `BindVerifiedLedger`, `Ledger`, 실제 메뉴 버튼 |
| 테마/목록/계정 성장/기존 로더 | [ProductionController.cs](../Assets/ANIMOL/UI/ProductionV1/ProductionController.cs): `BindAccountGateway`, `RefreshAccount`, `BeginLoading/LoadSelected` |
| 동물 공용 계약 | [AnimalUiContracts.cs](../Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiContracts.cs), [AnimalUiPresenter.cs](../Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiPresenter.cs), [AnimalUiRules.cs](../Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiRules.cs) |
| Phase1 | [AnimalUpgradeProjectAdapter.cs](../Assets/ANIMOL/UI/AnimalUpgradePhase1/AnimalUpgradeProjectAdapter.cs), [AnimalUpgradePhase1Host.cs](../Assets/ANIMOL/UI/AnimalUpgradePhase1/AnimalUpgradePhase1Host.cs), 운영 AnimalUpgradeIdMap |
| Phase2 | [AnimalStageProjectAdapter.cs](../Assets/ANIMOL/UI/AnimalStagePhase2/AnimalStageProjectAdapter.cs), [AnimalStageContext.cs](../Assets/ANIMOL/UI/AnimalStagePhase2/AnimalStageContext.cs), [AnimalStagePhase2Host.cs](../Assets/ANIMOL/UI/AnimalStagePhase2/AnimalStagePhase2Host.cs) |
| Phase3 | [AnimalMultiplayerProjectAdapter.cs](../Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalMultiplayerProjectAdapter.cs), [AnimalMultiplayerPhase3Host.cs](../Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalMultiplayerPhase3Host.cs) |
| 상점/모달 | [MissingUiProjectEntry.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/MissingUiProjectEntry.cs), [MissingStoreView.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/MissingStoreView.cs), `MissingCommonModalSkin.cs` |
| 설정/계정 | [MissingUtilityEntry.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/MissingUtilityEntry.cs), [MobileControlPreferences.cs](../Assets/ANIMOL/Scripts/UI/MobileControlPreferences.cs), [MobileControlLayoutApplier.cs](../Assets/ANIMOL/Scripts/UI/MobileControlLayoutApplier.cs) |
| 경쟁/커스텀 | [MissingMultiplayerEntry.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/MissingMultiplayerEntry.cs), [MissingMultiplayerView.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/MissingMultiplayerView.cs), [MissingRoomCodeInput.cs](../Assets/ANIMOL/UI/MissingUiV1/Project/Multiplayer/MissingRoomCodeInput.cs) |
| 계정/원장/광고/저장 계약 | [AccountProgression.cs](../Assets/ANIMOL/Scripts/Core/AccountProgression.cs), [GrowthEconomyContracts.cs](../Assets/ANIMOL/Scripts/Core/GrowthEconomyContracts.cs) |
| 멀티 기존 계약 | [MultiplayerMatchContracts.cs](../Assets/ANIMOL/Scripts/Core/MultiplayerMatchContracts.cs), [MultiplayerLoadoutService.cs](../Assets/ANIMOL/Scripts/Core/MultiplayerLoadoutService.cs), [MultiplayerUiPresenter.cs](../Assets/ANIMOL/Scripts/UI/MultiplayerUiPresenter.cs) |
| 설정 플래그 | [ExternalServiceConfiguration.asset](../Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset): 실제 SDK/권한/승인의 대체물이 아님 |

운영 프리팹은 다음 위치이며 모두 패키지 Generated 밖에서 관리한다.

- `Assets/ANIMOL/UI/AnimalUpgradePhase1/Resources/ANIMOLAnimalUpgradePhase1/CharacterUpgrade.prefab`
- `Assets/ANIMOL/UI/AnimalStagePhase2/Resources/ANIMOLAnimalStagePhase2/StageAnimalSelect.prefab`
- `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Resources/ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect.prefab`
- `Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/`: Store, Settings, Controls, Account, ControlReset 프리팹 및 Art/UtilityArt.
- 위 경로의 `Multiplayer/`: Hub, Custom, Create, Join, Room 프리팹 및 MultiplayerArt.

재생성 메뉴: `ANIMOL > Missing UI V1 > Build Project Task 1 Store and Modals`, `Build Project Task 2 Settings and Account`, `Build Project Task 3 Multiplayer`. 동물 패키지 메뉴는 `ANIMOL > Animal UI v2 > Build Upgrade Phase 1 / Build Stage Phase 2 / Build Multiplayer Phase 3`이다. **패키지 생성물을 운영 adapter·Catalog·IdMap·프리팹 위에 덮어쓰지 않는다.** 기존 M1~M9 전체 생성기의 재실행 안전성은 별도 검증이 필요하다.

## 5. 서버 이전에 확정할 콘텐츠/데이터

### 5.1 동물 ID와 구현 상태

이번 파일 조사에서 세 단계의 IdMap은 각각 15행이며 14행의 실제 종 참조가 비어 있음을 확인했다. 실제 종 asset은 `Data/Campaign/Animals/RABBIT.asset` 하나다.

매핑 소유자: [강화 IdMap](../Assets/ANIMOL/UI/AnimalUpgradePhase1/AnimalUpgradeIdMap.asset), [캠페인 IdMap](../Assets/ANIMOL/UI/AnimalStagePhase2/AnimalStageIdMap.asset), [멀티 IdMap](../Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalMultiplayerIdMap.asset). 위 종 asset의 전체 경로는 `Assets/ANIMOL/Data/Campaign/Animals/RABBIT.asset`이다.

| 역할 | 안정 아트 ID | 실제 프로젝트 종 ID | 성장 서비스 ID/운영 구현 |
|---|---|---|---|
| Ground | Rabbit | RABBIT | 성장 ID 미설정, 액티브/패시브 production 플래그 모두 false |
| Ground | Wolf | 미매핑 | 미확인 |
| Ground | WhiteFerret | 미매핑 | 미확인 |
| Ground | MountainGoat | 미매핑 | 미확인 |
| Ground | Otter | 미매핑 | 미확인 |
| Special | DreamFox | 미매핑 | 미확인 |
| Special | StarCat | 미매핑 | 미확인 |
| Special | MirrorDeer | 미매핑 | 미확인 |
| Special | DreamMole | 미매핑 | 미확인 |
| Special | ClockMoth | 미매핑 | 미확인 |
| Air | Swallow | 미매핑 | 미확인 |
| Air | Owl | 미매핑 | 미확인 |
| Air | FlyingSquirrel | 미매핑 | 미확인 |
| Air | Hummingbird | 미매핑 | 미확인 |
| Air | Bat | 미매핑 | 미확인 |

PNG가 존재해도 구현/보유/해금/사용 허용을 true로 만들지 않는다. 실제 ID를 표시 이름·배열 순서·대소문자 변환으로 추정하지 않는다. `DEV_GROUND/DEV_GLIDER/DEV_SPECIAL`은 실제 15종 ID로 매핑할 수 없다. 요청뿐 아니라 서비스 응답/승인 편성에도 명시적 역매핑이 필요하다.

### 5.2 스테이지·성장·상품의 미설정 데이터

| 데이터 | 현재 확인 | 필요한 결정/공급 |
|---|---|---|
| 캠페인 고정 편성 | 실제 100개 stage asset. T01-S01은 RABBIT 1종, 나머지 99개 지정 ID 목록 없음 | 실제 Ground/Special/Air 3종, 시작/대표 역할, 접근 정책, 콘텐츠/런 정책 버전 |
| 동물 성장 | CharacterUpgradeCatalog definitions 비어 있음, 성장 정책은 미설정 DEV 계약 | 동물별 독립 active/passive 현재·다음 효과, 레벨 상한, 실제 비용/무료 명시, 숙련도·해금 규칙 |
| 계정 공통 성장 | 기존 3트랙 카탈로그와 초안 차단 존재 | 기존 정책 소유자의 운영 승인 데이터. 새 BM 가격/스태미나 설계 금지 |
| 일반/랭크/커스텀 | 표시 카드 있음. 운영 ModeId/opaque EntryIntent 공급자 없음 | 일반/랭크의 실제 경기 규칙과 커스텀 생성/코드/초대 진입 방식 분리 |
| 방 옵션/코드 | 조회 공급자 없음, 코드 형식 미확정 | 허용 옵션과 코드 규칙/만료/참가 조건. 미정 이름/공개/암호/인원 편집 추가 금지 |
| 상점 | 성장 완료 팩/이모티콘 번들 설명만 있음 | 실제 상품 ID·플랫폼 SKU·구성·판매 가용성·현지화 가격·보유권 |
| 무료 이모티콘 | EMOTE_HELLO/EMOTE_THANKS/EMOTE_HELP, 기본 지급 플래그 true | 현재 visualKey `placeholder/...`를 신규 PNG에 연결. 무료 기본 지급과 유료 보유권 구분 |
| 이야기/도움말 | 패키지 도해/틀과 기존 설명 존재 | 실제 대본/장면/진행, 현재 조작·방울/출구 목표와 설명 대조 |

## 6. 서버·플랫폼 연동 작업 목록

아래 항목은 **필요한 계약과 현재 연결 위치**이며 새 REST URL/서버 클래스/서비스가 존재한다고 가정한 명세가 아니다. 실제 서버 구현이 별도 저장소에 있다면 그 소유자/API를 확인해 연결해야 한다.

| ID / 권장 담당 | 연동할 데이터·행동 | 현재 연결 경계 | 완료 조건 |
|---|---|---|---|
| AUTH / 계정·클라이언트 | Google/게스트 연계, 실제 계정 ID·세션·인증 실패·계정 변경 | AccountAccessSnapshot, AccountIdentityState, SC20 | 플랫폼 인증 결과와 서버 세션 검증, 계정 변경 시 옛 조회/요청 분리. 화면만 Linked로 바꾸지 않음 |
| SAVE / 저장·클라이언트 | 로컬/클라우드 진행·기록·revision·갱신 시각 조회 및 충돌 선택 commit | CampaignRecordSummary, SaveConflictSelectionService | 실제 저장 소유자, 최신 revision 재검증·원자적 반영·복구. 샘플 TrySelect는 저장 성공 아님 |
| LEDGER / 경제 | 검증된 보유 코인·지급 내역·광고 quota | IRewardLedgerAdapter → PortraitEntryController.BindVerifiedLedger | 개발 adapter 제외, 서버/권한 있는 실제 잔액 공급, 지급 중복 방지와 결과 확인 |
| ACCOUNT-GROWTH / 경제 | 계정 3트랙 조회·강화 | IAccountGateway.RequestUpgrade, ProductionController.BindAccountGateway/ApprovedGrowthSnapshot | 최신 레벨·catalog version·잔액 재검증, 원자적 차감/성장, 승인 뒤 최신 snapshot. 메모리 HashSet을 영속 멱등 처리로 간주하지 않음 |
| U-01 / 동물 성장 | 동물 보유/해금·숙련도·active/passive 진행도 | AnimalUpgradeProjectAdapter.ReadSnapshotAsync | Snapshot.CoinBalance와 각 AnimalProgress.MasteryBalance에 실제 값, 독립 레벨/설명, 실제 revision |
| U-02 / 동물 성장 | 다음 강화 견적 | QuoteUpgradeAsync | Ready는 완전한 현재/다음 레벨·효과·전체 비용·검증 가능한 QuoteToken. 누락은 Unconfigured, 부족/상한/잠김/미지원 구분 |
| U-03 / 동물 성장 | 구매 확정·원자적 저장 | TryUpgradeAsync | ActionId·AnimalId·Track·QuoteToken·ExpectedCurrentLevel 재검증, 승인 전 값 불변, Accepted 후 저장/UI 일치 |
| S-01~02 / 캠페인·권한 | 실제 stage Fixed 3종과 계정별 접근/체험 grant·능력 조회 | AnimalStageContext.Read, ReadStageSnapshotAsync | 현재 stage/정책과 snapshot 일치, 구현·권한·사용 가능 검증. 선행 stage 해금만으로 동물 사용권 발급 금지 |
| S-03 / 캠페인 | 승인 입장·receipt·결과 조회 | SubmitFixedCampaignAsync | ActionId/StageId/조회·정책 버전/정확한 고정 편성 최신 검증, 실제 receipt 발행 |
| S-04 / 클라이언트·캠페인 | 승인→기존 런/로더 연결 | CampaignAccepted, 기존 StageLaunchUseCase/CampaignLaunchContext/SceneManager | 승인 receipt 1회 소비 후 기존 로더 한 번 호출. 로컬 RunId를 승인 토큰으로 대체 금지 |
| M-01 / 모드·방 호스트 | 모드/원래 편성/허용 풀/대표 역할/정책/진입 의도 | AnimalMultiplayerPhase3Host.Open(request) | 실제 소유자에서 문맥 생성·깊은 복사. OpenUnconfigured를 실제 모드로 간주하지 않음 |
| M-02 / 권한·성장 | 모드와 intent별 Snapshot.Revision, 구현/보유/사용권·능력 | ReadMultiplayerSnapshotAsync | Implemented/HasContextPermission/CanUseInContext 각각 검증, 서버 허용 풀 재검증 |
| ROOM-LOOKUP / 방 | 코드 조회, 방장/인원/정책/참가 조건, 초대 capability | MissingRoomCodeInput/MissingMultiplayerView는 현재 입력·차단만 구현 | 실제 응답과 로딩/없음/오류/만료 표시. 입력 문자열을 확인된 방 코드로 승격 금지 |
| M-03~04 / 방 입장 | 정확한 3종 편성으로 단일 입장 요청과 실제 receipt | SubmitRoomEntryAsync | 요청·승인 필드 일치, 실제 승인만 Accepted. 호스트가 별도 create/join을 다시 실행하지 않음 |
| M-05 / 클라이언트·방 | 승인→기존 대기방 전달 | MultiplayerAccepted, 기존 SC06 | 중앙에서 승인/요청 1회 소비, 늦은 결과·중복 구독·재진입으로 두 번 이동하지 않음 |
| ROOM-STATE / 방 | 참가자 목록, 본인/방장, 연결/Ready, 시작/초대/나가기 | SC06 새 4슬롯 및 기존 MultiplayerUiPresenter | 권위 있는 방 snapshot/상태 변경 이벤트와 capability, 호스트 권한·끊김/재접속, 승인된 나가기. 가짜 참가자/자동 충원 금지 |
| STORE / 상거래·플랫폼 | 인증 상품/DisplayPrice/구성/보유권, 구매·복원 | AuthenticatedOfferSnapshot/GrowthCompletionPackContract, MissingStoreView | SDK 영수증 검증·멱등 지급·보유권 재조회, 취소=요청 0·확정=1, 복원 반복 시 중복 보상 없음 |
| AD / 광고·경제 | 광고 완료 증거·일일 제한·지급 승인 | RewardedAdGrantRequest/RewardedAdGrantCoordinator/IRewardLedgerAdapter | SDK와 서버 검증 후 실제 지급. 성공 지급 때 quota 차감, 캠페인 기본 코인 B만 추가 지급(2B+T+O), 경쟁 보상 2배 금지 |
| RECORD / 기록 | 실제 프로필·캠페인 진행/최고 시간·경쟁 기록 | SC17, CampaignProgressionService, 기존 MetaUiPresenter | 영속 기록/계정 조회 소유자와 실패/빈 상태. 미확정 랭킹·티어·승률 생성 금지 |
| EMOTE / 소유·저장 | 유료 보유권과 장착 상태 영속 저장 | EmoteCatalog/EmoteLoadoutService/MetaUiPresenter | 무료 기본 3종 보존, 실제 소유권 및 기존 장착 정책 사용. 현재 service의 배열은 메모리 상태이며 영속 저장 아님 |
| COOP / 협동 | 협동 모드/방 입장/Ready/시작/복귀 | 기존 SC08 및 별도 협동 정책 | 실제 협동 서비스에 연결. 현재 DEV 2~4 범위를 경쟁의 3역할/4인 정책으로 덮어쓰지 않음 |

`IMatchGateway.RequestReady(string[])`는 현재 disconnected이며 ModeId/EntryIntent/ActionId/입장 receipt 계약이 없다. 이것을 Phase3 입장 서비스로 직접 대체할 수 없다. `IAccountGateway`는 계정 공통 성장용이므로 동물 숙련도/트랙 거래로 전용하지 않는다.

### 6.1 세 동물 어댑터의 현재 반환

| 어댑터 | 현재 조회/견적 | 현재 실행 |
|---|---|---|
| AnimalUpgradeProjectAdapter | 검증 ledger만 코인 수용. 동물 진행도는 미조회, 현재 15종 견적 Unconfigured | TryUpgradeAsync=Unavailable. 다른 phase 제출도 Unavailable |
| AnimalStageProjectAdapter | 실제 카탈로그 문맥 비교 후 권한 서비스 부재 실패. 불완전 문맥은 base에서 먼저 차단 | SubmitFixedCampaignAsync=Unavailable, 런/맵 로딩 안 함. 강화/멀티 Unavailable |
| AnimalMultiplayerProjectAdapter | 모드·권한·성장 서비스 부재 실패, 성공 revision 합성 안 함 | SubmitRoomEntryAsync=Unavailable, 방 생성/참가/Ready 안 함. 강화/캠페인 Unavailable |

### 6.2 요청·승인 필드와 검증 규칙

| 흐름 | 확인 시 고정할 요청 | 서버/권한 있는 서비스의 결과 |
|---|---|---|
| 동물 강화 | ActionId, AnimalId, Track, QuoteToken, ExpectedCurrentLevel | 견적/현재 상태 재검증, 원자적 갱신, 실제 결과와 최신 snapshot |
| 캠페인 | ActionId, ContextId=실제 StageId, SnapshotRevision, PolicyRevision, Loadout | 유효 AcceptanceToken, AcceptedContextId, AcceptedPolicyRevision, AcceptedLoadout |
| 멀티 입장 | ActionId, ContextId=실제 ModeId, SnapshotRevision, PolicyRevision, EntryIntent, Loadout | 위 승인 필드와 AcceptedEntryIntent. 제출과 정확히 같은 문맥/정책/진입 의도/3종 |

- 멀티 `AllowedAnimalIds=null`은 추가 클라이언트 풀 제한 없음, 빈 배열은 허용 없음이다.
- 캠페인은 지정 Ground/Special/Air 각각 1종 고정. 열람 동물이 편성이나 확인 모달 대상을 바꾸면 안 된다.
- 멀티는 원래 편성의 복사본에 임시 선택한다. 실제 허용된 미보유 동물은 Unlocked=false만으로 막지 않으며, Unlocked=true만으로 사용권을 대신하지 않는다.
- 선택한 3종과 무관한 다른 동물의 미구현은 입장을 막지 않는다. 다른 플레이어와 같은 동물 사용을 금지하거나 선점 잠금을 만들지 않는다.
- 확인 취소는 요청 0회. 확정은 고정한 내용으로 1회. 처리 중 연타/편성 변경/뒤로/재진입은 추가 요청을 만들지 않는다.
- 확인 모달 동안 공개 Refresh/SetBackend로 문맥을 교체하지 않는다. 늦은 이전 조회는 새 화면의 데이터에 반영하지 않는다.
- 승인 전 재화·레벨·보유권 낙관적 변경, 별도 방 입장/대기방 이동을 하지 않는다. 빈 토큰/불일치 receipt는 정상 승인으로 소비하지 않는다.
- 최종 Rejected/Unavailable은 실제 사유를 표시한다. 입장은 수동 최신 조회와 필요 시 호스트의 새 문맥으로 다시 확인해야 한다. 거절된 요청의 내용을 바꿔 같은 ActionId를 재사용하지 않는다.
- 타임아웃/예외/응답 유실은 Unknown이다. 같은 ActionId와 **불변 요청 전체**로 결과를 조회한다. 결과 조회 계약이 없으면 새 구매/입장을 허용하지 않는다.

### 6.3 공통 최우선: G-04 중앙 요청·receipt·복귀

현재 UI/base의 보존은 주로 **같은 객체 수명**에 한정된다. account 구매의 HashSet, 광고 coordinator의 메모리 사전, fixture의 재응답은 영속 서버 멱등 처리나 앱 종료 복구를 대신하지 않는다. 운영 중앙 요청/receipt 관리자는 현재 조사 범위에서 확인되지 않았다.

필요한 계약은 계정에 귀속된 ActionId와 불변 payload 보존, 진행/최종 거절/Unknown 구분, 결과 조회, 서버 영속 멱등 처리, 승인 receipt의 단일 소비, 씬 파괴/앱 복귀 시 재연결이다. 미확인 원래 문맥·권한 snapshot은 후속 조회 cache와 독립적으로 유지해야 한다. 실제 취소 계약이 없으면 처리 중 거래가 취소되었다고 표시하지 않는다. 새로운 병렬 입장/저장 시스템을 UI 안에 임의로 만들지 않는다.

## 7. 서버 없이 진행 가능한 UI·클라이언트 작업

| 항목 | 할 일 | 경계 |
|---|---|---|
| Missing UI 작업 4 | 무료 3종 새 PNG/visualKey 연결, 이모티콘 카드/선택, 기록·도움말·협동·이야기 틀 보강 | 현재 실제 데이터만 표시. 영속 장착/해제 계약은 별도 확인 필요 |
| BGM/SFX/음소거 | 실제 오디오 적용/저장 소유자 마련 후 위젯 연결 | 서버 필수 항목이 아님. 현재 값/핸들 숨김과 비활성 유지 |
| 접근성·번역 | 전체 화면 번역 누락 점검, 큰 글씨·실제 HUD 터치 영역·진동 실기기 검사 | 언어 preference 저장과 번역 완료를 구분 |
| 전체 설정 초기화 | 초기화 대상/영향·복원 규칙 확정 | 현재 지원은 MobileControlPreferences의 8개 Reset뿐. 계정 저장/재화 초기화와 분리 |
| 버튼 효과 직렬화 | UiButtonFeedback 클래스/MonoScript 구조 정리 후 제한적으로 프리팹 재생성 | 기존 사용자 프리팹 전체 덮어쓰기 금지 |
| 경쟁 4인 이관 | Core/DEV 생성기/검사를 최신 경쟁 정책과 정합화 | 서버 정책 소유자와 함께 결정. 협동 규칙은 별개 |
| 기록/도움말 콘텐츠 | 실제 소유자와 현행 목표·조작 설명 대조 | 가짜 전적/능력/이야기 생성 금지 |
| Android 검증 | 빌드·notch·손가락·키보드·OS 뒤로·백그라운드/복귀·설정 저장 | Editor Safe Area 모사로 대신하지 않음 |

로컬 설정 8개는 버튼 크기(.75~1.35), 가로 위치(-.08~+.08), 불투명도(.3~1), 좌우손, 전용 하향 버튼, 큰 글씨, 진동, 언어(Korean/English)다. 실제 기존 setter/PlayerPrefs/Reset을 재사용하고 있다.

## 8. 알려진 결함·정책 불일치

| 항목 | 현 상태·근거 | 후속 조치 |
|---|---|---|
| 기존 버튼 효과 누락 참조 | 이번 파일 재확인: Store.prefab `m_Script: {fileID: 0}` 4개, Settings.prefab 7개. 작업 3에서 보조 MonoBehaviour 클래스 직렬화 문제 확인 | 공용 UiButtonFeedback을 Unity가 직렬화할 수 있게 정리하고 관련 프리팹 재생성/하위 전체 검사. 신규 멀티 5프리팹은 런타임 1회 부착으로 해결됨 |
| 경쟁 인원 불일치 | 새 운영 UI 최대4/4슬롯. `MultiplayerModeRules.CompetitiveParticipants`는 (4,8), 기존 presenter/generator/test에도 8인 기준 잔존 | MultiplayerMatchContracts, MultiplayerUiPresenter, UiBuildPipeline.M6Multiplayer, M6MultiplayerContractTests와 DEV 생성물 이관 |
| v7 원본 토끼 해시 | 이전 작업에서 두 시트의 기준 패키지/현 작업 트리 차이 확인, MenuThemePolicyTests 1개 실패 기록 | 사용자 의도 확인 후 별도 수정. 이번 작업에서 원본으로 되돌리지 않았고 해결로 보고하지 않음 |
| null AudioClip | 이전 Unity 실행에 UiFeedbackAudio.Play/PlayOneShot 경고 | 실제 clip 참조/무음 처리 검토. 오디오 설정 연결과 함께 확인 |
| 레거시 M6 회귀 3개 | Missing UI 작업1에서 M6GrowthEconomyUiFlowTests 1/4. 새 설치 비활성 대조에서도 동일 실패 | 비활성 레거시 버튼을 찾는 테스트를 실제 진입 경로 기준으로 정리. 이후 58개 통과 묶음에 포함되지 않음 |
| 캠페인 StableId 기대값 1개 | Missing UI 작업2 M9PortraitPolicyTests 8/9, 현재 맵과 과거 ID 기대값 불일치 | 맵 소유자/기대값 정합 검토. 작업3에서 해결·재검증한 항목 아님 |
| 전체 재생성 안전성 | Missing UI 전용 생성 메뉴 재실행은 검증. 기존 M1~M9 전체 재생성 미실행 | 사용자 변경 보존을 전제로 별도 검증 |

이 결함들은 서버를 연결해도 자동으로 해결되지 않는다. 반대로 서비스 없는 버튼을 비활성화하고 `--`로 표시한 것은 미지원 상태를 정직하게 표현한 것이므로 삭제해야 할 결함으로 분류하지 않는다.

## 9. 검증 증거와 한계

**아래는 이전 작업에서 실제 실행한 기록이며 이번 문서 작성에서 새로 실행한 검사 수가 아니다.** 전체 프로젝트/전체 운영 서비스 통과로 확대하지 않는다.

| 기록 | 확인된 범위 | 해석 |
|---|---|---|
| Animal Phase1 작업3 | 15종 원본/초상/두 해상도, 긴 효과·비용, 조회 경합/거래 UI fixture | 실제 성장 저장/서버 구매는 미검증 |
| Animal Phase2 작업3 | 실제 100stage 차단, 5테마, Fixed 편성 불변, receipt/지연응답 fixture | 운영 수용 10 통과·15 부분·3 미검증이라는 당시 판정 |
| Animal Phase3 작업3 | Game View 92장, Unity 104개 중 103 통과/기존 1실패 | 운영 수용 13 통과·14 부분·4 미검증. 실제 방 서비스 검증 아님 |
| Missing UI 작업1 | Unity 컴파일, 고유 42개 중 39통과/기존 3실패, 캡처 29장, 신규 PNG 55개 원본/import 검사 | 상점 열람·모달·차단 검증. 구매/복원 실서비스 없음 |
| Missing UI 작업2 | Unity 컴파일, 고유 51개 중 50통과/기존 1실패, 캡처 28장 | 8개 preference·초기화·입력·씬 재진입 및 계정 차단 |
| Missing UI 작업3 | Unity 컴파일 오류0, 선택 검사 **58/58**, 캡처 **40장**, 신규 5프리팹 누락 스크립트0 | 실제 로비 동선/화면 검사와 합성 거래 fixture. 과거 실패 전부 해결 의미 아님 |

작업3의 58개는 MissingMultiplayer 8, CommercialPolish 3, MissingUtility 7, MissingUiStore 5, PortraitEntry 3, Production 7, MenuTheme 3, UpgradePhase1 2, StagePhase2 9, MultiplayerPhase3 3, MultiplayerPhase3Transaction 8이다. [최신 결과](../ANIMOL_MISSING_UI_V1_TASK3_RESULT.md), [컴파일 JSON](MissingUiV1Task3/compile.json), [화면 캡처](MissingUiV1Task3/Captures), [fixture 요청 기록](MissingUiV1Task3/fixture-pending-unknown.json)을 참조한다. 작업 간 같은 테스트가 중복되므로 총합을 전체 고유 테스트 수로 더하지 않는다.

기존 캡처는 1080×1920/1080×2400 Game View 및 Editor에서 좌우48/상단120/하단96px inset을 모사한 범위다. 긴 데이터 일부는 합성 QA 값이다. 실제 최장 상품 가격/효과/모드명, Android/iOS 키보드·notch·OS 복귀, 다계정 4인 경기, 실제 승인 후 저장·맵·대기방 일치, 서버 중복 차감 방지는 여전히 후속 검증이다.

이번 문서 작성에서 실행한 것은 현재 경로/코드/asset 조사, ID map·stage 목록·서비스 플래그·기존 누락 참조 확인, 문서 링크/경로 및 Git 변경 경계 검사다. 런타임 변경이 없어 Unity 컴파일·CommercialPolishPolicyTests·PlayMode를 재실행하지 않았다.

## 10. 이어서 진행할 순서와 완료 기준

1. **데이터 소유자 확정:** 실제 15종 ID/능력/성장, stage 고정3종, 운영 ModeId/EntryIntent, 4인 정책, 상품/보상/계정 정책을 제공한다. 서버 코드가 외부에 있다면 저장소·계약 위치를 함께 전달한다.
2. **공통 기반:** 인증/계정 전환·권한 snapshot·원장/저장·G-04 멱등 요청/결과 조회/receipt 소비를 연결한다. Unknown에서 신규 행동을 풀지 않는다.
3. **기존 경계에 서비스 연결:** 계정/동물 성장, 캠페인 승인, 멀티 lookup/입장/방 상태, 구매/복원/광고를 각 기존 adapter/호스트에 연결한다. 새로운 병렬 로더·방 입장 경로를 만들지 않는다.
4. **독립 UI 보강 병행:** Missing UI 작업4, 오디오, 번역, 알려진 직렬화/정책/검사 결함을 해결한다.
5. **실제 서비스 통합 검증:** 취소0/확정1/연타추가0, 승인 전 변화0, 최신 권한/가격/버전 거절, 같은 ActionId 결과 확인, 승인1회 소비, 저장/UI 일치, 다계정/기기 복귀를 검증한다.
6. **Missing UI 작업5 최종 판정:** 각 수용기준을 pass/partial/blocked/미검증으로 증거와 함께 갱신한다. 필수 운영 연결이 남으면 전체 완료로 보고하지 않는다.

서버 담당자가 넘겨야 할 최소 인수 자료는 인증/계정 식별 방식, 실제 카탈로그와 revision, 권한·가격·잔액 조회, 실행/결과 조회 계약, 최종 거절 사유, 멱등 보존 범위, 실제 receipt 예시, 클라이언트 로더/대기방 소비 경계, 실험용 계정/환경 및 검증 방법이다. 문맥/가격/토큰을 UI 임의 값으로 채워 계약 부재를 숨기지 않는다.

## 11. 아트·입력·기존 동작 보존 기준

- 동물 15종은 v17 Rabbit 포함 원본 PNG 바이트, 128×160 전체 캔버스/여백/팔레트를 유지한다. Single/Full Rect, Point, 무압축, mipmap 없음, preserveAspect 및 추가 마스크 크롭 방지를 유지한다.
- v7 20배경+토끼 2시트와 메인 발판/연출은 별도 버전 단위다. 다른 패키지 배경으로 교체하지 않는다. 현재 시트의 기존 차이는 별도 이슈로 다룬다.
- 공용 CanvasScaler 1080×1920 width 기준, SafeAreaLayout, 기존 EventSystem/StandaloneInputModule, pixelroborobo/TMP/PixelTextBridge를 재사용한다.
- 데모 host/EventSystem/PREVIEW_ONLY/Generated 자동 시안을 운영에 넣지 않는다. Missing UI의 추가 프리팹에도 Canvas/EventSystem을 중복 생성하지 않는다.
- 클릭 피드백은 기존 Subtle 정책을 따른다. 피드백 자체 `BlocksInput=false`와 거래 중 중복 요청 차단은 서로 다른 책임이다.
- 계정 공통 스태미나/BM, 기존 캠페인 로더, 협동 정책, 기존 로비 버튼, 사용자 미커밋 변경을 UI 통합 과정에서 임의로 바꾸지 않는다.

## 12. 근거 문서

| 범위 | 문서 |
|---|---|
| 동물 전체 이전 통합 | [Phase1·2·3 미완료 목록](ANIMOL_ANIMAL_UI_PHASE1_2_3_REMAINING_WORK.md) — 인원 관련 4~8 표기는 이 문서의 최신 4인 UI/잔존 Core 구분으로 해석 |
| Phase1 | [작업1](ANIMOL_UPGRADE_PHASE1_TASK1_RESULT.md) · [작업2](ANIMOL_UPGRADE_PHASE1_TASK2_RESULT.md) · [작업3](ANIMOL_UPGRADE_PHASE1_TASK3_RESULT.md) |
| Phase2 | [작업1](ANIMOL_STAGE_PHASE2_TASK1_RESULT.md) · [작업2](ANIMOL_STAGE_PHASE2_TASK2_RESULT.md) · [작업3](ANIMOL_STAGE_PHASE2_TASK3_RESULT.md) |
| Phase3 | [작업1](ANIMOL_MULTIPLAYER_PHASE3_TASK1_RESULT.md) · [작업2](ANIMOL_MULTIPLAYER_PHASE3_TASK2_RESULT.md) · [작업3](ANIMOL_MULTIPLAYER_PHASE3_TASK3_RESULT.md) |
| Missing UI | [작업1 상점](../ANIMOL_MISSING_UI_V1_TASK1_RESULT.md) · [작업2 설정](../ANIMOL_MISSING_UI_V1_TASK2_RESULT.md) · [작업3 경쟁](../ANIMOL_MISSING_UI_V1_TASK3_RESULT.md) · [작업4·5 지시](../UNITY_APPLY_MISSING_UI_V1.md) |
| 기반 UI | [포트레이트 로비](ANIMOL_PORTRAIT_ART_V1_RESULT.md) · [캠페인/강화 ProductionV1](ANIMOL_UI_BACKGROUND_PRODUCTION_V1_RESULT.md) · [v7 배경](ANIMOL_MAIN_UI_FANTASY_BACKGROUND_V7_RESULT.md) · [픽셀 폰트](ANIMOL_PIXEL_TYPOGRAPHY_RESULT.md) |
| 외부 아트 제작 | [스프라이트 제작 브리프](ANIMOL_MISSING_UI_SPRITE_PRODUCTION_BRIEF.md) — 제작 당시 요구이며 이후 운영 적용/최대4인 변경은 최신 작업 결과 우선 |

이 문서는 현재 UI와 서비스 연결 상태의 인수인계다. 맵 오브젝트/물리/레벨 제작 전체의 완료 목록은 범위가 아니며, UI에 보이는 플레이 구현/콘텐츠 준비 상태에 영향을 주는 항목만 포함했다.
