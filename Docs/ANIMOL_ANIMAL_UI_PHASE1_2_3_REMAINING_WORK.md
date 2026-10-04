# ANIMOL 동물 UI Phase 1·2·3 통합 작업 현황과 미완료 목록

기준: 2026-10-04, 현재 저장소 및 각 단계 작업 1·2·3 보고서. **세 단계 모두 운영 전체 완료가 아니다.** 화면·연결 경계·차단 검증과 실제 성장/입장 성공을 구분한다. 과거 작업 1의 `Backend=null`은 이후 작업 2에서 프로젝트 어댑터로 교체되었으므로 현재 미완료 목록에 중복 등록하지 않는다.

## 9개 작업에서 한 일과 남은 일

| 단계·작업 | 적용/검증한 것 | 현재 남은 것 | 원본 결과 |
|---|---|---|---|
| Phase 1 강화 / 작업 1 | 패키지·15종 원본·생성 메뉴, Generated 밖 운영 CharacterUpgrade, 두 기존 강화 허브 진입/반환, 기존 폰트/Canvas/입력/Safe Area 사용. 재진입·스타일러 충돌·좌우 inset·스크롤 전달 수정 | 실제 성장 데이터/구매 연결은 당시 제외. 다음 작업에서도 실서비스 부재로 미완료 | [작업 1](ANIMOL_UPGRADE_PHASE1_TASK1_RESULT.md), `3e45e3e` |
| Phase 1 / 작업 2 | AnimalUpgradeProjectAdapter, 운영 Catalog/명시적 ID map, 검증된 코인 ledger 경계, Unconfigured/Unavailable 및 누락 비용 차단, 거절 메시지/재조회 | 아래 U-01~U-04: 성장 저장소·견적·원자적 구매·결과 조회 | [작업 2](ANIMOL_UPGRADE_PHASE1_TASK2_RESULT.md), `4a5bb87` |
| Phase 1 / 작업 3 | 수용기준별 판정, 15종 Game View/해시 검사, 장문 효과/비용 compact 표시와 전체 모달 보존 수정, 거래 UI 비동기 fixture/회귀 검사 | 실제 데이터·거래·저장 일치와 기기/앱 복귀 검증. P1-DATA-02, P1-TRADE-04/05 등 필수 미충족 | [작업 3](ANIMOL_UPGRADE_PHASE1_TASK3_RESULT.md), `da650b0` |
| Phase 2 캠페인 / 작업 1 | 별도 StageAnimalSelect 운영본, 실제 스테이지 목록/상세 진입·반환, 현재 카탈로그 기반 Fixed 문맥과 기존 테마별 배경, 열람으로 지정 편성 불변 | 실제 지상/특수/공중 고정 3종 데이터 없음. 권한/입장 서비스는 당시 제외 | [작업 1](ANIMOL_STAGE_PHASE2_TASK1_RESULT.md), `9ebfd2d` |
| Phase 2 / 작업 2 | AnimalStageProjectAdapter 및 실제 CampaignCatalog 연결, 문맥/정책 비교, 호스트 수동 문맥 재조회, 오래된 권한 캐시 폐기 | 아래 S-01~S-04: 고정 3종·권한·receipt·기존 로더 단일 전달 | [작업 2](ANIMOL_STAGE_PHASE2_TASK2_RESULT.md), `934849a` |
| Phase 2 / 작업 3 | 실제 100스테이지 차단, 5테마 진입/반환, 15종/두 해상도, TMP 장문 모달 끝부분 잘림 수정, 지연 응답/잘못된 receipt/동일 인스턴스 복귀 fixture | 운영 수용 28개 중 10 통과·15 부분·3 미검증이라는 당시 판정. 실제 고정 편성/입장/로더 성공 미검증 | [작업 3](ANIMOL_STAGE_PHASE2_TASK3_RESULT.md), `9fc573a` |
| Phase 3 멀티플레이 / 작업 1 | 기존 공용 수정 보존 병합, 독립 MultiplayerAnimalSelect, 경쟁 허브 일반/랭크/비공개 버튼의 열람 진입·원래 편성 반환, 15종/두 해상도 | 실제 운영 모드/진입 의도/정책 소유자가 없어 OpenUnconfigured 사용. 서버/대기방 성공 미연결 | [작업 1](ANIMOL_MULTIPLAYER_PHASE3_TASK1_RESULT.md), `fb473ea` |
| Phase 3 / 작업 2 | AnimalMultiplayerProjectAdapter와 Catalog/ID map, 문맥/의도/정책 스냅샷 검사, immutable attempt와 완료 결과 복사, 중복 호출 차단, 미지원 결과 조회는 추가 호출 금지. 실제 Unity 98개 검사 통과(당시) | 아래 M-01~M-05: 실제 모드·권한·입장·receipt·대기방 단일 전달. 테스트 Accepted는 운영 성공 아님 | [작업 2](ANIMOL_MULTIPLAYER_PHASE3_TASK2_RESULT.md), `de003f6` |
| Phase 3 / 작업 3 | 긴 모드 이름 넘침·Unknown 사유 누락 수정, Game View 92장, 최종 Unity 104개 검사 중 103 통과/1 기존 실패, 644개 기존 파일 보존, 모든 단계 미완료 통합 | 실제 서비스가 필요한 항목은 계속 미완료. 운영 수용기준은 13 통과·14 부분·4 미검증 | [이번 작업 3](ANIMOL_MULTIPLAYER_PHASE3_TASK3_RESULT.md) |

## 공통 선행 과제 — 세 단계에 중복되는 차단 요인

| ID | 미구현/미설정/미연결 내용 | 현재 근거·소유 경계 | 완료 조건 |
|---|---|---|---|
| G-01 | **14종 실제 종 ID/asset 미확인**, 15종 성장 서비스 ID 미설정 | 각 단계 Generated 밖 IdMap. Rabbit만 실제 `CampaignAnimalDefinition RABBIT` 참조. `DEV_GROUND/DEV_GLIDER/DEV_SPECIAL`은 실제 종이 아님 | 소유자가 제공한 실제 ID로 정확한 양방향 매핑, 응답/승인 편성 역매핑, 잘못된 ID 차단 |
| G-02 | **15종 운영 고유 능력 구현·레벨별 설명 데이터 부족** | Rabbit도 productionActiveAbilityConfigured/productionPassiveAbilityConfigured=false. 다른 14종 정의 미확인. 아트는 완성되었으나 플레이 구현 증거가 아님 | 실제 액티브/패시브 구현 상태와 독립 설명/레벨 조회를 연결; 미구현은 계속 차단 |
| G-03 | 계정별 동물 보유/해금·동물 숙련도·트랙 진행도 저장소/조회 서비스 없음 | CharacterUpgradeCatalog 정의 비어 있음. GrowthEconomyPolicyCatalog는 미설정 DEV 계약. CampaignProgressionService는 세션 스테이지 진행도뿐 | 권한 있는 실제 저장 소유자·조회 버전·동물별 값·계정 전환/로드 계약 제공 |
| G-04 | 영속적인 ActionId 멱등 처리·결과 조회·중앙 미확인 요청/receipt 관리자 없음 | 기존 UpgradePurchaseUseCase HashSet은 계정 성장 중복 거절이며 거래 결과 저장소가 아님. UI/base의 보존은 같은 객체 수명 범위 | 같은 불변 요청 결과를 조회하고 씬 파괴/앱 종료/재접속 후 복원, receipt 한 번 소비. 계약 없이는 실행 활성화 금지 |

명시적 ID 현황(세 단계 공통):

| 역할 | 안정 ID | 실제 프로젝트 종 ID |
|---|---|---|
| Ground | Rabbit | RABBIT — 성장 ID/멀티 권한은 별도 미연결 |
| Ground | Wolf / WhiteFerret / MountainGoat / Otter | 각각 미매핑 |
| Special | DreamFox / StarCat / MirrorDeer / DreamMole / ClockMoth | 각각 미매핑 |
| Air | Swallow / Owl / FlyingSquirrel / Hummingbird / Bat | 각각 미매핑 |

## Phase 1 — 강화의 미완료

| ID | 남은 구현/연결 | 실제 후보와 주의점 | 필요한 운영 검증 |
|---|---|---|---|
| U-01 | CoinBalance와 동물별 MasteryBalance·ActiveLevel·PassiveLevel·소유/해금의 실제 조회 | 코인은 `PortraitEntryController.Ledger → IRewardLedgerAdapter` 경계만 있음. 기본 disconnected/DEV ledger는 운영 값으로 채택하지 않음. 동물 진행도 소유자 없음 | 실제 저장값과 화면 일치, 동물 전환/지연 응답, 편성/해금/재화 불변 |
| U-02 | 실제 효과·현재/다음 레벨·비용·상한·해금 조건·QuoteToken 견적 | `CharacterUpgradeCatalog` / `GrowthEconomyPolicyCatalog` / `AnimalUpgradeProjectAdapter.QuoteUpgradeAsync`. Ready/InsufficientFunds/Maximum/Locked의 실제 응답 변환 미연결 | 실제 비용/명시적 무료/0 구별, 비용·효과 누락 차단, 최신 가격/레벨 견적 거절, 긴 실제 데이터 |
| U-03 | 원자적 재화 차감과 해당 트랙 레벨 증가, 승인 결과 저장 | `AnimalUpgradeProjectAdapter.TryUpgradeAsync`는 Unavailable. 계정 공통 스태미나 `IAccountGateway.RequestUpgrade`를 동물 거래로 전용하지 않음 | 실제 승인 전 값 불변, 최신 레벨·가격 버전·잔액 재검증, Accepted 뒤 UI/저장 일치, 오류 시 부분 차감 없음 |
| U-04 | 동물 거래의 멱등 처리·결과 조회·중앙 복귀 | G-04 필요. 현재 Unknown 재확인은 fixture 및 동일 presenter 수명만 검사 | 타임아웃/응답 유실 후 같은 ActionId/동물/트랙/견적/예상 레벨, 중복 차감 없음, 재시작 복원 |

## Phase 2 — 캠페인의 미완료

| ID | 남은 구현/연결 | 실제 후보와 주의점 | 필요한 운영 검증 |
|---|---|---|---|
| S-01 | **실제 스테이지의 Ground/Special/Air 고정 3종** 및 필수 역할·시작/대표 역할·실제 정책 | CampaignCatalog 100개 중 T01-S01만 RABBIT 1종, 나머지 99개 지정 목록 없음. host는 현재 데이터를 정직하게 전달. 예시 3종을 운영에 복사하지 않음 | 실제 카탈로그 3슬롯/정책/StageId·이름, 열람 불변, 모달 지정 편성, 실제 테마 연결 |
| S-02 | 스테이지 접근과 각 지정 동물의 계정/체험 grant·구현/사용 권한·성장 조회 | `AnimalStageProjectAdapter.ReadStageSnapshotAsync`. `IsUnlocked`·ContentAvailabilityResolver는 입장 권한 스냅샷/체험 승인 아님 | 실제 미보유 허용 동물, 미구현/권한 부재 차단, 문맥/조회/정책 버전, 실제 능력 값 |
| S-03 | 입장 재검증·승인 receipt·멱등/결과 조회 | `SubmitFixedCampaignAsync`는 Unavailable. `StageLaunchUseCase.Launch`의 로컬 RunId는 입장 토큰 아님 | 실제 최신 FixedLoadout/접근/권한/버전 재검증, stale 거절, 실제 receipt/결과 확인 |
| S-04 | 검증된 CampaignAccepted를 기존 로더에 한 번 전달, 중앙 요청 보존 | 기존 `ProductionController.BeginLoading/LoadSelected → StageLaunchUseCase → CampaignLaunchContext → SceneManager`는 있음. 승인 receipt를 받는 공식 메서드/consumer 없음 | 승인 전 로드 0회, 정상 승인 1회, 중복 receipt/이벤트 0회 추가, 실제 런/맵과 승인 편성 일치, 앱 복귀 |

## Phase 3 — 멀티플레이의 미완료

| ID | 남은 구현/연결 | 실제 후보와 주의점 | 필요한 운영 검증 |
|---|---|---|---|
| M-01 | **운영 ModeId/DisplayName/EntryIntent/InitialLoadout/AllowedAnimalIds/대표 역할/PolicyRevision 소유자** | `MultiplayerUiPresenter`에는 운영 문맥 계약 없음. ModeLoadoutRuleSet enum/DEV PreviewId는 운영 ID 아님. 현재 `OpenUnconfigured` | 기존 일반/랭크/비공개 및 실제 생성/찾기/초대 경로 문맥 전달, 원래 편성/진입 의도 복사, 정책 변경 후 새 확인 |
| M-02 | 모드·진입 의도별 실제 계정 사용 권한·성장 Snapshot | `AnimalMultiplayerProjectAdapter.ReadMultiplayerSnapshotAsync`는 실패. 구현/권한/사용 가능을 해금 하나로 대체하지 않음 | 실제 허용된 미보유, 권한 없는 보유, null/빈/제한 pool, 실제 설명/레벨, 지연 조회 |
| M-03 | 실제 방 입장·현재 권한/pool/3역할/버전/ActionId 재검증·멱등 결과 조회 | `IMatchGateway.RequestReady(string[])`는 DisconnectedMatchGateway뿐이며 입장 계약 아님. `SubmitRoomEntryAsync`는 Unavailable | 실제 서비스 요청 1회, 승인 전 별도 create/join/대기방/저장 0회, stale/timeout/서버 재확인 |
| M-04 | 실제 receipt 5개 필드와 실제 ID 편성의 양방향 매핑 | AcceptanceToken, AcceptedContextId, AcceptedPolicyRevision, AcceptedEntryIntent, AcceptedLoadout을 발행하는 운영 receipt 없음 | 실제 응답만 Accepted, 공백/불일치 receipt Unknown, 요청값 복사로 가짜 승인 생성 금지 |
| M-05 | 중앙 receipt 1회 소비 → 기존 운영 대기방 연결 및 복귀 | 기존 SC06/SC07은 개발 미리보기. 운영 MultiplayerAccepted 소비 메서드/중앙 관리자 없음 | 중복 이벤트/구독/늦은 응답/재진입에도 실제 대기방 전달 1회. 서버 4~8인과 다계정 동일 동물 허용 |

## 미검증과 별도 기존 이슈

| ID | 항목 | 현재 증거의 한계 / 후속 조치 |
|---|---|---|
| V-01 | 실제 최대 수치·최장 효과/가격·긴 운영 모드명 | 타입 최대값/장문 fixture와 Editor Game View는 검사했지만 실제 운영 데이터가 없음. 데이터 확정 후 실제 값으로 재검사 |
| V-02 | 실기기 Safe Area/터치/OS 앱 복귀 | 1080×1920/2400 및 Editor inset·EventSystem 주입 검사다. 실제 notch/터치/백그라운드/프로세스 종료 검증 필요 |
| V-03 | 실제 다계정 경쟁전, 4~8인 방과 동물 중복 | 기존 Core 개발 계약과 클라이언트 검증은 확인 가능. 실제 두 계정/서버 방 입장은 M-01~05 없어서 미검증 |
| V-04 | 실제 승인/거절/저장/런/방 카운터 | fixture 요청·Presenter 이벤트와 실제 서버/저장/로더/대기방 성공은 다름. 현재 운영은 요청을 보내지 않는 차단 경계만 확인 |
| V-05 | 기존 v7 토끼 시트 원본 해시 불일치 | Phase 1 작업 1부터 있던 좌/우 rabbit-run PNG 2개와 기준 패키지의 불일치를 이번에도 확인. `MenuThemePolicyTests.V7ImportsPreserveAllTwentyTwoOriginalPNGs` 1개 실패. 사용자 변경을 임의 복원하지 않았음. 원본/현행 중 의도한 버전을 확인하고 별도 수정 필요 |
| V-06 | 기존 null AudioClip 경고 | 이번 실제 회귀 실행에서도 `UiFeedbackAudio.Play → PlayOneShot` 경고 재현. 새 UI 작업에서 오디오 시스템을 변경하지 않았고 해결 완료로 표시하지 않음. 기존 버튼 피드백 clip 연결 확인 필요 |

## 권장 처리 순서와 완료 조건

1. 실제 콘텐츠/계정/서비스 소유자를 확정하고 G-01~03, U-02, S-01, M-01의 데이터를 제공한다. 이미 별도 브랜치/패키지에 있으면 그 구현을 연결한다.
2. 기존 권한 있는 성장·캠페인·멀티플레이 서비스에 U-03, S-02~03, M-02~04를 연결한다. 현재 미연결 adapter의 실행 차단은 보장이 갖춰질 때까지 유지한다.
3. G-04와 기존 로더/대기방 소비 경계(S-04/M-05)를 연결한다. 계정 공통 성장/BM 가격이나 새 매칭/대기방 시스템 설계는 이번 범위에 포함하지 않는다.
4. 실제 계정·서버·저장·기기에서 승인/거절/응답 유실/중복/복귀 시나리오와 각 phase acceptance를 다시 실행한다. 이후 각 단계의 필수 운영 항목이 충족될 때만 전체 완료를 판정한다.

`Unavailable`을 유지해야 하는 다른 단계의 Submit 메서드, 미설정 값의 `--/설정 대기`, 데모를 운영에 넣지 않는 것, Generated 밖 운영본 보존은 **미구현 결함이 아니라 의도된 경계**다. 이전 작업에서 수정 완료한 스크롤/장문/재진입 문제도 현재 미완료 항목으로 다시 세지 않았다.
