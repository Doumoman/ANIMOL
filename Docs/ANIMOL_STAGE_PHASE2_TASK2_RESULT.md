# ANIMOL 스테이지 동물 확인 2단계 · 작업 2 결과

작성: 2026-10-04 / 현재 ANIMOL 작업 트리 / Unity 6000.3.8f1

## 판정

**작업 2의 실제 권한·입장 승인·맵 로딩 연결은 미완료다.** 확인된 프로젝트 카탈로그에 연결하는 어댑터, 문맥 재조회와 권한 검증 경계를 적용했다. 현재 저장소에는 요구된 승인 토큰·버전 재검증·멱등 입장·결과 조회 서비스가 없고, 실제 지상/특수/공중 고정 3종 스테이지도 없다. 이를 대체하는 가짜 권한/스냅샷 버전/승인 결과/로더를 만들지 않았다.

별도 구현 브랜치나 경로가 있는지 질문했다. 이 보고서 작성 시 추가 위치는 제공되지 않았다. 이 결과는 UI/어댑터 검증 결과이며 운영 입장 성공 판정이 아니다.

## 먼저 확인한 현재 소스

작업 1은 `9ebfd2d feat(ui): integrate read-only stage animal confirmation phase 2`로 적용되어 있었다. 저장소 AGENTS.md, 작업 1 보고서, 패키지 README_PHASE2.md와 UNITY_APPLY_PHASE2.md의 작업 2, 현재 공용/운영 소스를 확인했다.

| 실제 소스/데이터 (`Assets/ANIMOL/` 기준) | 소유 계약과 확인 결과 |
|---|---|
| `Data/Campaign/CampaignCatalog.asset`, `Scripts/Core/CampaignCatalog.cs` | 실제 StageId 조회 소유자. 현재 100개 스테이지를 테스트에서 직접 열거했다. |
| `CampaignStageDefinition`, `CampaignStageRuntimePolicy` | 고정/시작 동물, 선행 스테이지, 콘텐츠/런 정책 버전 소유자. T01-S01만 RABBIT 1종, 나머지는 빈 지정 목록. |
| `CampaignUiPresenter.progression`, `CampaignProgressionService` | 현재 세션의 스테이지 완료/해금. `IsUnlocked`는 선행 스테이지 완료 조건이다. 동물별 체험 grant, 원격 권한 스냅샷, 승인 토큰 발행 계약이 아니다. |
| `ContentAvailabilityResolver` | 기존 맵/런 규칙/동물 정의 정적 준비 상태와 실제 차단 이유를 제공한다. 계정 권한을 대신하지 않는다. |
| `Data/Campaign/Animals/RABBIT.asset`, `CampaignAnimalDefinition` | 유일한 확인된 실명 종 ID는 RABBIT. placeholderVisual=true, productionActiveAbilityConfigured=false, productionPassiveAbilityConfigured=false. |
| `GrowthEconomyPolicyCatalog`, `CharacterUpgradeCatalog` | 실제 15종 액티브/패시브 진행도·효과 조회가 없다. 계정 공통 `IAccountGateway.RequestUpgrade`는 동물/스테이지 서비스로 전용하지 않았다. |
| `StageLaunchUseCase.Launch`, `StageLaunchSnapshot` | 로컬 런 구성과 RunId 생성. ActionId·SnapshotRevision·PolicyRevision 검증, 입장 receipt, 결과 조회 기능이 없다. |
| `ProductionController.BeginLoading/LoadSelected` | 기존 단일 로딩 경로. StageLaunchUseCase → CampaignLaunchContext.Set → SceneManager.LoadSceneAsync. 승인 토큰을 받는 메서드가 없다. |

프로젝트 소스에서 스테이지 입장 서비스/게이트웨이, 승인 토큰, ActionId, 결과 조회 구현을 탐색했다. 요구된 서비스가 확인되지 않아 새로운 원격 DTO나 대체 권한 서비스를 설계하지 않았다.

## 적용한 연결

새 `UI/AnimalStagePhase2/AnimalStageProjectAdapter.cs`는 `AnimalStageBackendAdapterBase`를 상속한다. 운영 프리팹 `UI/AnimalStagePhase2/Resources/ANIMOLAnimalStagePhase2/StageAnimalSelect.prefab`의 Backend에 실제 Editor 메뉴로 연결했다. 아트 Catalog, 기존 CampaignCatalog, 기존 AnimalStageIdMap을 참조한다.

`AnimalStagePhase2Builder.ConnectProjectBackend`는 기존 사용자 정의 backend를 덮어쓰지 않는다. 기존 프리팹을 재생성하지 않고 어댑터 참조만 추가했다. 이후 누락 asset 생성에도 같은 연결이 적용된다. Generated 결과와 1단계 어댑터/프리팹은 그대로 유지한다.

공개 `ReadSnapshotAsync/SubmitCampaignAsync`는 sealed base의 Fixed/필수 3역할/지정 편성/문맥/버전 검사를 통과해야 protected 메서드로 전달된다.

- `ReadStageSnapshotAsync`: 실제 StageCatalog.FindStage와 AnimalStageContext.Read로 받은 문맥을 대조한다. 실제 ID 불일치, 고정 편성/시작 역할/정책 변경, 콘텐츠 미설정 이유를 구분한다. 현재 권한·성장 조회 서비스가 없어 성공 스냅샷을 반환하지 않는다. 현재 100개 스테이지는 필수 문맥이 불완전해 대부분 base에서 먼저 차단된다.
- `SubmitFixedCampaignAsync`: 현재 입장 서비스 미지원이므로 **Unavailable**만 반환한다. 외부 입장 요청을 전송하지 않았으므로 Unknown/Accepted로 표시하지 않는다. 런 생성·맵 로딩·편성 저장을 호출하지 않는다.
- 강화 견적/거래와 멀티플레이 제출은 base의 sealed 메서드로 **Unavailable**을 유지한다. 1단계 성장 어댑터를 재사용하거나 수정하지 않았다.

호스트는 선택된 실제 ProductionCatalog.Campaign을 어댑터의 조회 소스로 연결한다. 새로고침 버튼은 `CampaignRefreshRequested → AnimalStagePhase2Host.OpenSelectedStage → AnimalStageContext.Read → OpenCampaign`으로 최신 문맥을 다시 만든다. 정책이 바뀌면 기존 요청을 억지로 재사용하지 않고 새 확인을 요구한다. 처리 중/결과 미확인 중에는 이 경로도 차단된다.

## ID와 스테이지 문맥

Generated 밖의 `UI/AnimalStagePhase2/AnimalStageIdMap.asset` 15행을 유지한다. `ToArtId/ToProjectAnimal`의 명시적 양방향 조회를 사용하며 중복/모호한 매핑, DEV 폼 ID, 대소문자 추정을 허용하지 않는다.

| 역할 | 안정 ID | 실제 프로젝트 ID |
|---|---|---|
| 지상 | Rabbit | RABBIT |
| 지상 | Wolf, WhiteFerret, MountainGoat, Otter | 각각 미매핑 |
| 특수 | DreamFox, StarCat, MirrorDeer, DreamMole, ClockMoth | 각각 미매핑 |
| 공중 | Swallow, Owl, FlyingSquirrel, Hummingbird, Bat | 각각 미매핑 |

호스트의 StageSelectionRequest는 기존 작업 1 방식대로 실제 StageId·이름·FixedLoadout·대표 역할을 읽고 RequiredRoles=Ground/Special/Air를 각 1회 전달한다. Policy=Fixed. 실제 로컬 버전 `content:{ContentVersion};runtime:{RuntimePolicy.Version}`을 보존하며, 누락 버전은 null이다. 이것을 권한 조회 Revision으로 복사하지 않는다.

보유·구현·스테이지 사용 허용·레벨·효과는 아트로 채우지 않았다. 실제 설명/레벨 데이터가 없으므로 `-- · 설정 대기`, 시작은 비활성이다. 고정 편성은 토끼/빈칸/빈칸 또는 모두 빈칸이며 예시 3종을 운영 asset에 넣지 않았다.

## 조회·요청 경계 보완

1. UI 내부 AnimalUiSnapshot에 `ContextId`, `PolicyRevision`을 추가했다. 스테이지 조회 서비스는 실제 응답의 문맥/정책을 매핑해야 한다. 요청값을 무조건 응답에 복사해 검사를 통과시키면 안 된다. 강화 스냅샷에는 이 필드를 요구하지 않는다.
2. base와 시작/확인 검사가 Snapshot.ContextId/PolicyRevision을 현재 스테이지 문맥과 대조한다. 버전 문자열만 있는 다른 스테이지 응답은 권한으로 사용하지 않는다.
3. 새로운 조회가 시작되면 검증 실패/취소 여부와 관계없이 이전 어댑터 권한 캐시를 먼저 폐기한다. 이전 정상 조회 뒤 잘못된 문맥 재조회가 실패해도 옛 권한으로 제출할 수 없다.
4. 늦게 끝난 이전 조회는 새로운 어댑터 문맥을 덮어쓰지 않는다. Presenter의 기존 조회 generation/epoch 검사도 유지한다.
5. 요청은 기존 ActionId/ContextId/SnapshotRevision/PolicyRevision/Loadout 복사 계약을 유지한다. 어댑터에 전달한 복사본을 변경해도 Presenter 소유 요청은 변하지 않는다.
6. Unavailable/Rejected 뒤에는 실제 메시지를 보존하고 수동 새로고침 전 재제출을 막는다. Unknown/예외/타임아웃은 같은 요청을 유지한다. 취소 계약 없는 진행 중 요청을 취소된 것으로 표시하지 않는다.

## 승인 토큰 전달과 로더 상태

**실제 AcceptanceToken 전달, CampaignAccepted → 기존 로더 연결, 승인당 단일 로딩은 아직 구현/검증하지 못했다.** 현재 backend는 Accepted를 만들지 않으며 호스트에 무조건 BeginLoading을 호출하는 이벤트 구독을 추가하지 않았다. 로컬 RunId를 승인 토큰으로 바꾸거나, UI에서 요청 문맥을 복사한 receipt를 합성하지 않았다.

다음 연결은 실제 서비스가 발행한 AcceptanceToken·AcceptedContextId·AcceptedPolicyRevision·정확한 AcceptedLoadout과 기존 런 구성/로더 계약이 제공된 뒤 해야 한다. 현재 공용 Presenter의 receipt 검사 자체는 유지하며 잘못된 승인 결과로 이동하지 않는다. 새 맵 로더는 만들지 않았다.

중앙 입장 요청 관리자·멱등 요청·결과 조회도 현재 프로젝트에서 확인되지 않았다. 같은 Presenter 인스턴스가 비활성화/재활성화되는 동안 요청을 보존하는 UI 경계는 테스트했지만, 앱 종료/씬 파괴 뒤 복구나 실제 서버의 중복 런 방지는 보장하지 않는다. 이 지원이 없으므로 운영 입장을 활성화하지 않았다.

## 실제 실행 검증

| 구분 | 결과 / 증빙 |
|---|---|
| 실제 Unity 컴파일 | [최종 compiler 상태](StagePhase2Task2/unity-compile.json): compilationFailed=false, compiling=false, Console Error 0 |
| Stage EditMode | [11/11](StagePhase2Task2/editmode-stage.json): 운영 참조/재생성 보호 3개, 실제 100개 스테이지 차단 및 어댑터 경계 8개 |
| Stage PlayMode | [5/5](StagePhase2Task2/playmode-stage.json): 5테마·15종·두 해상도/Safe Area, 모달, 최신 문맥 새로고침, Unknown 재확인 |
| 기존 강화 EditMode | [asset 3/3](StagePhase2Task2/editmode-upgrade-assets.json), [backend 4/4](StagePhase2Task2/editmode-upgrade-backend.json) |
| 기존 강화 PlayMode | [11/11](StagePhase2Task2/playmode-upgrade.json) |
| 기존 Production UI | [7/7](StagePhase2Task2/playmode-production.json) |
| v7 로비 / 기존 메뉴 진입 | [3/3](StagePhase2Task2/playmode-v7.json), [3/3](StagePhase2Task2/playmode-entry.json) |
| 연출 정책 | [CommercialPolishPolicyTests 3/3](StagePhase2Task2/editmode-feedback.json) |
| 패키지 검증 메뉴 | [Stage 119개 / Upgrade 94개](StagePhase2Task2/unity-menu-validation.json). 정적 계약/import/prefab 검사이며 서버 승인 검증 아님 |

고유 Unity 테스트 **50/50 통과**. 기존 오디오 null AudioClip 경고는 계속 관찰되며 이번 작업의 오디오 변경은 없다. 테스트로 변경된 폰트 atlas/아트 참조 캐시는 작업 전 바이트로 복원했다.

운영 검사: 실제 100개 스테이지의 조회가 권한을 생성하지 않고, 공개 제출은 Unavailable이며 CampaignLaunchContext.Pending 및 stage asset 데이터가 변하지 않는다. 실제 Lobby에서 강제로 Primary를 Invoke해도 로더 요청 0회이고, 15종 열람으로 Draft와 고정 편성이 변하지 않는다. [운영 Game View](StagePhase2Task2/1920_full_Rabbit.png), [모의 Safe Area](StagePhase2Task2/2400_safe_cards_Air.png).

통제 fixture 검사: 지정 3종만 구현된 상태에서 나머지 12종 미구현은 시작을 막지 않는다. Unlocked=false여도 실제 권한 형태의 응답을 받은 지정 동물은 허용하며 지정 동물 미구현은 차단한다. 잘못된 ContextId/PolicyRevision/Revision, 늦은 응답, 실패한 재조회 후 옛 캐시, 요청 복사 방어를 검사했다. fixture는 운영 서비스에 저장하지 않는다.

UI 요청 fixture: 취소=0회, 최초 확정 및 처리 중 연타=총 1회, 처리 중 뒤로/새로고침/다른 스테이지 Open 차단, 승인 전 로더 0회. 타임아웃 후 실제 host가 활성화된 상태에서 화면을 비활성화/재활성화하고 같은 ActionId와 4개 문맥/편성 필드로 결과 재확인 1회를 수행했다. 최종 Unavailable과 Unknown을 구분했다. **실제 서버 요청/원자성/저장/멱등성/앱 종료 복구 검사로 계산하지 않는다.**

## 변경 파일과 필요한 후속 연결

정확한 파일은 [변경 목록](StagePhase2Task2/changed-files.txt)에 기록했다. 핵심 변경은 신규 AnimalStageProjectAdapter, 운영 prefab backend 연결, builder 연결 메뉴, host 새로고침, 공용 조회 문맥 검증/캐시 무효화, 관련 테스트와 이 보고서다. 원본 15종 PNG·ID map·Generated 프리팹·씬·1단계 운영 소스·기존 ProductionController/성장/로더/v7 에셋은 변경하지 않았다.

실제 작업 2를 완료하려면 다음 기존 계약/데이터의 위치 또는 구현이 필요하다.

- 승인된 지상/특수/공중 고정 3종 스테이지 및 14종 미매핑 ID, 실제 동물 구현·진행도·액티브/패시브 조회 데이터.
- 현재 스테이지 접근·체험 grant·실제 Snapshot.Revision/문맥/정책을 제공하는 권한 조회 서비스.
- ActionId와 불변 요청을 최신 권한/편성/버전으로 검증하고 receipt를 발행하는 입장 서비스, 같은 ActionId 결과 조회와 중앙 요청 보존 계약.
- 그 receipt와 승인된 런 구성을 기존 단일 로더에 넘기는 공식 경계. 승인 전 런을 생성하지 않고 중복 receipt를 한 번만 소비해야 한다.

현재는 위 필수 연결이 남아 있으므로 **작업 2 전체 및 2단계 운영 완료로 판정하지 않는다.**
