# ANIMOL 멀티플레이 동물 선택 3단계 · 작업 2 결과

작성: 2026-10-04 / Unity 6000.3.8f1 / 현재 ANIMOL 작업 트리

## 판정

**부분 적용. 운영 프리팹에 프로젝트 어댑터와 Catalog·ID map을 연결하고 요청 검증을 보강했다. 실제 문맥·권한 조회·방 입장 승인·기존 대기방 단일 전달은 미연결이다. 작업 2 또는 3단계 전체 완료로 판정하지 않는다.**

현재 저장소에는 실제 경쟁 모드 카탈로그, opaque EntryIntent 소유자, 동물별 계정 성장/문맥 권한 조회, 방 입장 receipt, 멱등 입장/결과 조회 및 중앙 요청 관리자가 없다. `IMatchGateway.RequestReady(string[])`는 개발용 Ready 계약이며 이를 입장 승인으로 바꿀 수 없다. 새 매칭 서버·방 목록·대기방·병렬 입장 경로는 만들지 않았다. 운영 화면은 15종 열람과 기존 허브 반환만 허용한다.

## 현재 코드와 데이터 확인

AGENTS.md, 작업 1 보고서, README_PHASE3.md, UNITY_APPLY_PHASE3.md와 현재 공용/운영 소스를 읽었다. Unity CLI로 실행 중인 Editor에 연결했다. 기존 미커밋 파일은 이번 커밋에서 제외한다.

| 확인한 소유자 | 실제 계약과 연결 여부 |
|---|---|
| `Scripts/UI/MultiplayerUiPresenter.cs` | 기존 일반/랭크/비공개 경쟁 허브와 개발용 방의 소유자. 운영 ModeId/PolicyRevision/EntryIntent 제공 메서드가 없다. 기존 미커밋 소스를 수정하지 않았다. |
| `Scripts/Core/ModeLoadoutRuleSet.cs` | GameModeKind, 슬롯 수, 중복 여부, allowed IDs만 보유한다. enum/버튼 이름을 운영 모드 ID로 사용하지 않았다. 이 클래스의 null→빈 배열 정규화는 새 UI 요청에 적용하지 않는다. |
| `MultiplayerPreviewDefinition`, `Data/Development/DEV-MULTI-*.asset` | 개발 전용 Competitive/Coop 미리보기. DEV ID와 예시 편성은 운영 값으로 전용하지 않았다. |
| `CampaignAnimalDefinition`, `Data/Campaign/Animals/RABBIT.asset` | 확인된 실제 종 ID는 RABBIT 하나다. 생산용 액티브/패시브 구현 플래그는 false다. 이 정의는 경쟁전 권한 응답이 아니다. |
| `CharacterUpgradeCatalog`, `GrowthEconomyPolicyCatalog`, `GrowthEconomyContracts` | 성장 설정/해금 조건 계약이며 계정의 실제 동물 레벨·숙련도·효과 조회 서비스가 아니다. 계정 값이나 설명을 합성하지 않았다. |
| `AccountProgression.IAccountGateway`, `UpgradePurchaseUseCase` | 계정 공통 성장 요청과 해당 구매 ID 중복 방지. 동물 사용 권한/방 입장/receipt 관리자 대용으로 연결하지 않았다. |
| `CompetitiveParticipantSelectionService` | 개발용 참가자별 편성, 참가자 간 같은 동물 허용. 캠페인 해금 스냅샷을 경쟁 사용 권한으로 승격하지 않았다. |
| `MultiplayerLoadoutService`, `IMatchGateway` | `RequestReady(string[])`와 Approved/Rejected/Unavailable·Message만 있다. ActionId, 모드/진입 의도, 정책/조회 버전, receipt, 결과 조회가 없다. 구현은 `DisconnectedMatchGateway`다. 새 어댑터는 이 Ready 메서드를 호출하지 않는다. |
| `SC06_MatchRoom`, `SC07_MultiplayerAnimalSelect` | 기존 개발용 방/동물 선택 경로다. 실제 입장 receipt를 받는 운영 메서드가 없어서 성공 이동 대상으로 사용하지 않았다. |

현재 서비스가 별도 브랜치/패키지에 있는지 경로를 요청했으나, 이 보고서 작성 시 추가 계약은 제공되지 않았다.

## 운영 배선과 요청 경계

운영 프리팹:
`Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Resources/ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect.prefab`

실제 Unity Editor에서 `AnimalMultiplayerPhase3Builder.ConnectProjectAdapter()`를 실행해 기존 프리팹에 `AnimalMultiplayerProjectAdapter`를 추가하고 Presenter.Backend, 독립 운영 Catalog, 기존 명시적 IdMap 참조만 연결했다. 기존 레이아웃을 재생성하지 않았다. 연결 메뉴는 다른 프로젝트 backend가 있으면 덮어쓰지 않고 거절한다. 기존 Create missing 메뉴도 이미 존재하는 운영 프리팹은 보존한다. Generated의 데모/프리팹은 변경하지 않았다.

호출 경로:

`기존 SC05 경쟁 허브 3개 버튼 → AnimalMultiplayerPhase3Host.OpenUnconfigured → Open(request 복사본) → Presenter.OpenMultiplayer → 공개 ReadSnapshotAsync → 문맥 검사 → protected ReadMultiplayerSnapshotAsync`

현재 실제 버튼의 요청에는 ModeId/EntryIntent/PolicyRevision/RepresentativeRole의 운영 값이 없으므로 공개 검사에서 차단된다. 테스트처럼 형식상 완전한 문맥을 전달해도 프로젝트 어댑터는 `DescribeContext`에서 실제 서비스 미연결을 반환하고 조회를 실패시킨다. 문자열 모양만으로 실제 모드나 계정 권한을 인정하지 않는다. 설명·레벨은 `설정 대기/--`이며 임의 Snapshot.Revision을 발급하지 않는다.

입장 호출 경계:

`확인 모달 확정 → 공개 SubmitMultiplayerAsync → 마지막 조회 문맥/권한/정확한 3종 및 버전 검사 → protected SubmitRoomEntryAsync`

프로젝트 구현의 protected 메서드는 `Unavailable`과 명확한 미연결 사유를 반환하며 외부 요청을 보내지 않는다. 실제 운영 경로에서는 유효한 조회를 만들 수 없어 이 protected 메서드까지 도달하지 않는다. 강화 Quote/TryUpgrade와 캠페인 Submit은 base의 sealed `Unavailable`을 유지한다. 1·2단계 운영 어댑터와 프리팹은 그대로다.

## 명시적 ID 매핑

`AnimalMultiplayerIdMap.asset`의 15행과 `ToProjectAnimal`/`ToArtId`를 사용한다. 배열 순서·표시 이름·DEV form을 종 ID로 추정하지 않는다. 양방향 중복, 빈 ID, null 매핑 배열은 실패한다.

| 역할 | 아트 안정 ID | 확인된 프로젝트 ID |
|---|---|---|
| Ground | Rabbit | RABBIT ↔ Rabbit |
| Ground | Wolf | 미매핑 |
| Ground | WhiteFerret | 미매핑 |
| Ground | MountainGoat | 미매핑 |
| Ground | Otter | 미매핑 |
| Special | DreamFox | 미매핑 |
| Special | StarCat | 미매핑 |
| Special | MirrorDeer | 미매핑 |
| Special | DreamMole | 미매핑 |
| Special | ClockMoth | 미매핑 |
| Air | Swallow | 미매핑 |
| Air | Owl | 미매핑 |
| Air | FlyingSquirrel | 미매핑 |
| Air | Hummingbird | 미매핑 |
| Air | Bat | 미매핑 |

RABBIT 매핑은 실제 종 정의의 존재만 뜻하며 보유/경쟁전 구현/권한을 뜻하지 않는다. 실제 서비스 응답 및 승인 편성의 역매핑은 대상 서비스와 나머지 실제 ID가 없어 **미구현**이다. 요청을 복사해 승인 편성을 만든 운영 코드는 없다.

## 공용 검증과 미확인 요청 보존

- `AnimalUiSnapshot.EntryIntent`를 추가하고 멀티플레이 조회가 실제로 반환한 ContextId·PolicyRevision·EntryIntent와 현재 문맥의 일치를 검증한다. 기존 Revision도 필수다. UI 요청에서 이 조회 값을 채워 넣지 않는다. 강화·캠페인에는 이 필드를 요구하지 않는다.
- DisplayName, 필수 3역할 각각 1회, 대표 역할, ID/역할, null/빈 pool 구별을 검증한다. `Implemented && HasContextPermission && CanUseInContext`를 사용하고 Unlocked는 경쟁전 사용 권한으로 대체하지 않는다. 현재 열람한 미구현 동물 때문에 유효한 선택 3종을 막지 않는다.
- 확인 모달 중 Refresh/SetBackend와 다른 문맥 열기를 막는다. 처리 중/Unknown은 뒤로·새 입장·편성 변경을 막는다. 조회 시작 시 이전 멀티플레이 표시 권한을 비워 실패 후 오래된 선택 가능 상태가 보이지 않게 했다.
- adapter는 요청·문맥·권한 스냅샷을 복사해 원래 attempt에 보존한다. 후속 조회 cache와 독립적이다. wrong-mode/잘못된 조회도 이전 cache를 무효화하고, 늦은 이전 응답은 최신 문맥을 대체하지 않는다.
- 같은 adapter의 진행 중 호출은 추가 protected 요청을 보내지 않는다. 완료 결과를 ActionId와 원래 요청/문맥에 묶어 보존하며, 같은 요청의 결과는 복사해서 반환한다. 이미 사용한 ActionId에 다른 버전/편성을 붙일 수 없다.
- Rejected/Unavailable은 실제 반환 사유를 표시하고 새 조회 전 확정을 막는다. 거절 전에 시작된 늦은 조회가 다시 권한을 살리지 못한다. 다음 확인은 새 ActionId다.
- 공백 토큰, 다른 모드/정책/진입 의도/편성, null 또는 잘못된 상태의 receipt는 Unknown이다. timeout/예외도 원래 ActionId와 정확한 내용을 보존한다. 실제 취소 계약 없이 취소 성공을 표시하지 않는다.
- 실제 결과 조회/멱등 계약이 확인되지 않은 adapter는 `SupportsRoomEntryReconciliation=false`가 기본이다. Unknown의 같은 요청 재확인도 추가 서비스 호출 없이 미지원 사유와 Unknown을 유지한다. 실제 계약을 구현한 adapter만 이를 명시적으로 활성화할 수 있다. 프로젝트 adapter는 활성화하지 않았다.

**이 보존은 살아 있는 adapter/presenter 인스턴스 범위다. 서버 멱등성이나 중앙 요청 관리자를 구현한 것이 아니다.** 화면 비활성화 후 늦게 도착한 승인 결과는 adapter에 보존되고, 동일 요청 재확인으로 읽으며 추가 서비스 호출을 만들지 않는 것을 테스트했다. 프로세스 종료/scene 파괴/도메인 재로드 뒤 복구, 계정 전환, 네트워크 결과 조회는 미지원이다. 운영 서비스 자체가 비활성이라 이 한계를 가진 상태로 실제 입장을 허용하지 않는다.

## receipt와 대기방 전달

현재 운영 `SubmitRoomEntryAsync`는 Accepted를 반환하지 않는다. AcceptanceToken·AcceptedContextId·AcceptedPolicyRevision·AcceptedEntryIntent·AcceptedLoadout을 제공하는 실제 receipt가 없으므로 이를 매핑하지 않았다.

`MultiplayerAccepted`의 운영 대기방 소비자도 연결하지 않았다. 기존 호스트는 Cancelled만 OnEnable/OnDisable에 구독/해제한다. 기존 방 생성/참가를 다시 호출하거나 SC06 개발용 방을 운영 성공으로 여는 경로는 없다. 테스트에서 관측한 Accepted 1회는 **Presenter 이벤트 횟수**이며 중앙 receipt 소비/실제 대기방 단일 전달의 증거가 아니다.

실제 후속 연결에는 기존 입장 서비스가 계정·현재 모드/진입 의도·구현/권한·허용 pool·3역할·정책/조회 버전·ActionId 멱등성을 재검증하는 계약이 필요하다. 원래 요청/문맥과 일치하는 실제 receipt를 중앙 요청/receipt 관리자가 한 번 소비하고 기존 대기방의 실제 메서드에 전달해야 한다. 현재 저장소에서 그 메서드와 관리자는 확인되지 않았다.

## 실제 실행 검증

검증 결과와 변경 파일 목록은 `MultiplayerPhase3Task2/`에 저장했다. 아래 결과는 실제 Unity 테스트이며, 입장 승인 응답은 테스트 assembly의 fixture에서만 제공했다. 독립 HTML/JavaScript 결과나 작업 1의 이전 통과 수를 이번 실행 수에 합산하지 않는다.

| 실행한 검증 | 최종 결과 / 증거 |
|---|---|
| 실제 Unity 컴파일 | [compilationFailed=false, compiling=false, Console Error 0](MultiplayerPhase3Task2/unity-compile.json) |
| 멀티플레이 adapter/asset EditMode | [28/28](MultiplayerPhase3Task2/editmode-multiplayer.json): 운영 미지원 경계, 15종/ID 매핑, 권한/문맥 차단, 깊은 복사, 늦은 조회, 중복/미확인 요청, 불완전 receipt |
| 멀티플레이 거래 PlayMode | [5/5](MultiplayerPhase3Task2/playmode-transactions.json): 취소 0회, 확정 1회, 처리 중 연타, 모달 Refresh/SetBackend/재진입 차단, 거절 사유/수동 조회, 동일 요청 재확인, 비활성화 중 늦은 승인 |
| 멀티플레이 실제 화면/진입 PlayMode | [3/3](MultiplayerPhase3Task2/playmode-multiplayer.json): 기존 허브 3개 진입·반환, 15종/양 해상도/모의 Safe Area, fixture의 선택/장문/확인 모달 |
| 캠페인 회귀 | [EditMode 11/11](MultiplayerPhase3Task2/editmode-stage.json), [PlayMode 9/9](MultiplayerPhase3Task2/playmode-stage.json) |
| 동물 강화 회귀 | [EditMode 7/7](MultiplayerPhase3Task2/editmode-upgrade.json), [PlayMode 11/11](MultiplayerPhase3Task2/playmode-upgrade.json) |
| 기존 Production/메뉴/v7 | [7/7](MultiplayerPhase3Task2/playmode-production.json), [진입 3/3](MultiplayerPhase3Task2/playmode-entry.json), [v7 메뉴 3/3](MultiplayerPhase3Task2/playmode-v7.json) |
| 기존 멀티플레이 계약/연출 정책 | [M6 8/8](MultiplayerPhase3Task2/editmode-legacy-multiplayer.json), [CommercialPolishPolicyTests 3/3](MultiplayerPhase3Task2/editmode-feedback.json) |
| 공용 Editor 검증 | [멀티플레이 147 / 캠페인 119 / 강화 94개 통과](MultiplayerPhase3Task2/unity-menu-validation.json). 별도의 synthetic 계약/import/prefab 검사이며 테스트 합계에 중복 합산하지 않음 |

**고유 테스트 98/98 통과, 실패/건너뜀 0.** 최종 증거만 표에 집계했다. 검증용 CLI eval 보조 명령의 잘못된 타입명/인용/using 구문은 수정 후 재실행했으며, 이는 프로젝트 소스 컴파일 오류와 구분한다.

[실제 운영 프리팹 배선·현재 카탈로그/매핑 조회](MultiplayerPhase3Task2/project-binding-audit.json), [프로젝트 adapter 연결 후 열람 전용 Game View](MultiplayerPhase3Task2/operational-unavailable.png).

테스트 fixture에서 timeout 및 잘못된 receipt를 차례로 반환한 7번의 확인은 동일 ActionId·ContextId·SnapshotRevision·PolicyRevision·EntryIntent·Loadout이었다. 실제 서버 호출이 아니라 test protected 메서드 호출이다. 화면 비활성화 중 보류된 승인 후 재확인은 protected 호출 1회, Presenter Accepted 이벤트 1회였다. 운영 대기방 전달은 0회이며, 실제 입장 성공을 검증한 것이 아니다.

## 변경 파일과 보존

[전체 변경 경로](MultiplayerPhase3Task2/changed-files.txt).

- `AnimalMultiplayerProjectAdapter.cs`: 현재 미지원 서비스 경계를 명시한 프로젝트 파생 adapter.
- 운영 `MultiplayerAnimalSelect.prefab`, `AnimalMultiplayerPhase3Builder.cs`: Catalog/IdMap/Backend 연결. 기존 생성물을 덮어쓰지 않는 별도 연결 메뉴.
- `AnimalMultiplayerIdMap.cs`, `AnimalMultiplayerPhase3Host.cs`: 잘못된 매핑 차단, 모달 중 문맥 교체 차단.
- 공용 `AnimalMultiplayerBackendAdapterBase.cs`, `AnimalUiContracts.cs`, `AnimalUiRules.cs`, `AnimalUiPresenter.cs`: 조회 문맥 바인딩, 복사/중복/미확인 요청 보호, 잘못된 조회 후 표시 권한 제거.
- 멀티플레이 Editor 검증 fixture와 EditMode/PlayMode 테스트, 보고서/증거.

작업 시작 시 해시를 기록한 v7, 강화/캠페인 운영 코드·asset, 기존 Generated, Data, Scenes, 원본 아트 총 **644개 파일의 바이트 변경 0개**를 확인했다. [보존 검사](MultiplayerPhase3Task2/preservation-result.json), [기준 해시](MultiplayerPhase3Task2/preservation-before.json). 테스트가 변경한 4개 폰트 atlas와 named art 캐시는 작업 시작 바이트로 복원했다. 기존 사용자 변경이나 미커밋 MultiplayerUiPresenter, ProductionV1, 씬을 이번 커밋에 포함하지 않는다.

## 남은 필수 사항

1. 실제 모드/진입 의도/대표 역할/정책·계정 편성 소유자와 14종의 실제 프로젝트 ID가 필요하다.
2. 계정별 실제 액티브/패시브 레벨·설명과 문맥 사용 권한을 반환하는 조회 서비스를 연결해야 한다. 해금 여부만으로 대체할 수 없다.
3. 실제 입장 재검증·멱등 처리·receipt 및 결과 조회가 필요하다. 기존 RequestReady는 필요한 보장을 제공하지 않는다.
4. 실제 중앙 요청/receipt 보존·소비 및 기존 운영 대기방 메서드 연결이 필요하다. 현재 화면 인스턴스를 파괴한 뒤 재입장/앱 복귀/네트워크 재연결 검증은 완료하지 않았다.
5. 실제 서버 승인/거절, 다른 플레이어 중복 허용, 결과 재조회, 실제 대기방 전달 횟수는 미검증이다. 새 서버나 가짜 모드/방 ID로 대체하지 않았다.
