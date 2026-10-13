# ANIMOL 동물 강화 1단계 · 작업 3 검증 결과

2026-10-04 · Unity 6000.3.8f1 · 현재 ANIMOL 작업 트리 · 연결된 실제 Editor(port 7800)

## 판정

**현재 실행 가능한 프로젝트 검증은 수행했으나, 동물 강화 1단계의 운영 수용 판정은 미완료다.** 패키지 `acceptance-phase1.json`의 28개 항목은 모두 운영 수용에 필요하다. 실제 동물 성장 저장소·견적·원자적 구매·결과 조회 서비스가 없어 P1-DATA-02, P1-TRADE-04/05 등 필수 항목을 통과시킬 수 없다. UI 테스트 통과나 계정 성장 테스트를 동물 강화의 실거래 성공으로 환산하지 않는다.

이번에 실제 발견한 장문 트랙 표시 넘침을 수정했다. 운영 화면, 아트 보존, 읽기 경계, 요청 차단과 UI 비동기 처리, 기존 화면 회귀 검사는 통과했다. 서버 가격·해금·숙련도·레벨을 임의로 만들거나 개발 승인 서비스를 운영 어댑터로 연결하지 않았다.

## 선행 확인과 범위

- 저장소 `AGENTS.md`, 패키지 `README_PHASE1.md`, [acceptance-phase1.json 원본 사본](UpgradePhase1Task3/acceptance-phase1.source.json), 작업 1·2 결과를 읽었다. 패키지 원본은 저장소 루트 `ANIMOL_Animal_Upgrade_Phase1.zip`이다.
- 작업 1 화면 및 작업 2 운영 어댑터가 이미 존재했다. 현재 프리팹의 backend와 모든 참조를 Editor에서 다시 읽은 결과는 [integration-audit.json](UpgradePhase1Task3/integration-audit.json)에 있다.
- 기존 uGUI/Input Manager, 운영 Canvas/SafeAreaLayout/PixelTextBridge를 유지했다. 기존 운영 프리팹·씬·Generated 에셋을 재생성하거나 덮어쓰지 않았다.
- `CharacterUpgradeCatalog.Definitions.Count=0`, 성장 정책에는 DEV_GROUND/DEV_GLIDER/DEV_SPECIAL만 존재한다. 동물별 성장 저장/구매 gateway는 여전히 없다. 기존 `IAccountGateway`는 계정 공통 성장 계약이며 이를 동물 구매로 사용하지 않는다.
- `PortraitEntryController.Ledger`가 기존 검증된 코인 경계다. 운영 기본 상태는 disconnected여서 `--`가 맞다. 실제 동물 숙련도·레벨은 확인되지 않아 null/-1이다.

## 수정한 문제

`NumericTypeBoundsAndLongTextRemainReadableInBothSafeGameViews` 최초 실행에서 긴 효과가 카드 영역을 넘쳤다. 최초 결과(5 pass / 1 fail)는 [initial-long-text-failure.json](UpgradePhase1Task3/initial-long-text-failure.json)에 보존했다.

`AnimalUiPresenter`를 다음과 같이 수정한 뒤 다시 검증했다.

- 카드 효과는 40개 텍스트 요소까지 미리보기하고 초과 시 `… (상세)`로 안내한다. 긴 rich text는 태그를 중간에서 자르지 않고 상세 안내를 표시한다.
- 긴 비용 목록은 카드에 비용 종류 수와 상세 안내를 표시한다. **실제 quote의 효과·비용 문자열은 변경하지 않으며 확인/상세 모달에는 전체를 표시한다.** 비용 누락은 `-- · 설정 대기`이며 무료로 바꾸지 않는다.
- 새 모달은 스크롤 관성을 멈추고 맨 위에서 연다. 상세를 끝까지 읽은 뒤 확인창을 열어도 선택 동물/트랙/레벨 변화부터 볼 수 있다.
- 견적의 현재 레벨이 미정이어도 snapshot에 이미 알려진 레벨이 있으면 그 값은 표시한다. 불완전한 견적의 구매 차단은 유지한다. 최종 코드 검토에서 보완하고 누락 데이터 테스트를 다시 실행했다.

## 15종 아트·매핑·Game View

[portrait-byte-audit.json](UpgradePhase1Task3/portrait-byte-audit.json): ZIP 내 Unity 원본 15개와 현재 PNG가 바이트 단위로 같고 SHA-256/128×160 크기가 일치했다. 최신 Rabbit의 해시는 `8d51fbdd019aa964980ac30ed49f3f97a80d190840055de52e8044d16dd32257`이다. 원본 여백과 전체 캔버스를 보존했다.

| 탭 | 카드/큰 초상 확인한 안정 ID | 현재 실제 종 ID |
|---|---|---|
| 지상 5종 | Rabbit, Wolf, WhiteFerret, MountainGoat, Otter | Rabbit만 RABBIT. 나머지 미매핑 |
| 특수 5종 | DreamFox, StarCat, MirrorDeer, DreamMole, ClockMoth | 5종 미매핑 |
| 공중 5종 | Swallow, Owl, FlyingSquirrel, Hummingbird, Bat | 5종 미매핑 |

각 catalog ID의 PNG 파일명·sprite 참조가 일치함을 검사했다. 성장 서비스 ID는 15종 모두 비어 있다. 따라서 **안정 아트 ID와 그림의 1:1 연결은 확인했지만, 미존재하는 14종의 실제 프로젝트 ID 연결까지 완료했다고 판정하지 않는다.** Rabbit도 고유 액티브/패시브 및 성장 데이터가 미설정이다.

Importer의 Single/Full Rect/Point/Uncompressed/mipmap off와 128×160 sprite rect, 카드/큰 초상의 preserveAspect를 검사했다. 실제 Lobby 경로로 열어 15종 전체를 두 해상도와 두 안전 영역에서 순회했다. 카드/큰 초상의 viewport 포함, 카드 위 실제 EventSystem drag 전달, 본문 이동, 긴 이름의 글꼴 overflow 없음, 고정 하단 버튼, 패시브 트랙 노출, 상세 모달을 검사했다.

운영 상태 Game View 캡처 **64장**: [Captures](UpgradePhase1Task3/Captures). 15종 × 1080×1920/1080×2400 × 기본/모의 Safe Area + 하단 4장이다. Safe Area는 좌/우 48px, 상단 120px, 하단 96px inset이다. 이번 작업에서 `1920_safe_*` 15종을 모두 직접 열어 카드·큰 초상의 귀/클로버 핀/날개를 확인했다. 추가로 2400 Safe Area의 긴 이름과 1920 스크롤 하단을 확인했다. UI가 추가로 자르는 현상은 발견하지 못했다. 원본 그림 자체의 경계는 변경하지 않았다.

장문·숫자·확인 모달 스트레스 캡처 **10장**: [FixtureCaptures](UpgradePhase1Task3/FixtureCaptures). 운영 프리팹을 실제 Lobby Game View에 열고 **테스트 backend만** 주입했다. 두 해상도 Safe Area에서 24줄 현재 효과+24줄 다음 효과, 비용 8항목, int.MaxValue 코인과 long.MaxValue 숙련도/비용을 사용했다. 코인의 int는 기존 ledger 타입 경계이며 long은 공용 UI 계약 타입 경계다. **실제 계정 잔액이나 게임의 허용 최대치가 아니다.** 실제 최대 숫자·최장 효과는 카탈로그와 서비스가 없어 검증하지 못했다. 모달 전체 문자열, 끝까지 스크롤, 버튼의 안전 영역 포함, 재개방 시 맨 위 복귀가 통과했다.

대표 캡처: [Rabbit](UpgradePhase1Task3/Captures/1920_safe_Rabbit.png), [긴 이름](UpgradePhase1Task3/Captures/2400_safe_FlyingSquirrel.png), [고정 하단/스크롤](UpgradePhase1Task3/Captures/1920_safe_bottom.png), [테스트 데이터 확인창](UpgradePhase1Task3/FixtureCaptures/2400_fixture_confirm_top.png).

## 데이터·거래 검증의 구분

| 실행 환경 | 실제 수행 및 결과 | 의미/한계 |
|---|---|---|
| 운영 어댑터 + 현재 데이터 | 15종 미정 값/강화 비활성화, 정확한 ID 조회, 미매핑 fallback 없음. 강제 TryUpgrade도 Unavailable | 실제 거래를 전송하지 않는 현재 계약 검증 |
| 운영 어댑터 + 기존 로비 ledger 경계에 읽기 fixture 주입 | 코인 123 및 int.MaxValue 반영, 15종 열람 전후 snapshot 동일, 보상 grant 0회, 계정/종 상태 생성 없음 | 실제 경계의 UI 연결 검사이며 실제 계정 서버 잔액 검증 아님 |
| 현재 프로젝트 상태 비교 | 캠페인 진행 revision/완료 ID, pending launch, 멀티플레이 loadout/selection/room/current preview 참조 유지, 뒤로 허브 복귀 | 현재 로컬 상태에 대한 검사. 존재하지 않는 동물 영속 저장은 검사 불가 |
| 운영 프리팹 + 비동기 fixture | 늦은 이전 snapshot/quote 무시, Rabbit/Wolf 각각 다른 숙련도와 active/passive 값 표시, 열람 후 fixture 데이터와 draft 유지 | 실제 서비스 데이터의 정확성은 확인하지 못함 |
| 운영 프리팹 + 구매 불가 fixture | Unconfigured/Locked/Unavailable/InsufficientFunds/Maximum, 미구현/미보유, 현재/다음 효과·비용·토큰·레벨 누락/불일치·음수 비용·통화 ID 누락에서 요청 0회 | UI guard 검증. 실제 MAX/가격/해금 규칙의 근거는 아직 없음 |
| 운영 프리팹 + 명시적 무료/0 비용 fixture | 그 fixture가 명시한 경우만 확인창 열림, 취소 후 요청 0회. 누락 비용은 무료로 표시하지 않음 | 운영 무료 상품을 추가하거나 확인한 것이 아님 |
| 지연 요청 fixture | 취소=0회, 확정 및 연타=1회, 처리 중 동물/트랙/뒤로/취소 차단, 승인 전 UI 잔액·숙련도·레벨 변화 없음 | 실제 저장소의 원자성 검증 아님 |
| 확정 거절/승인 fixture | 거절 이유 표시 후 변경된 snapshot/quote 재조회, 재확인 후 새 요청. Accepted 이벤트는 fixture Accepted 1회에만 발생하고 그 snapshot 표시 | 서버의 최신 가격·예상 레벨 검증 및 영속 저장 일치 검증 아님 |
| Unknown/타임아웃/비활성화 fixture | 같은 ActionId/AnimalId/Track/QuoteToken/ExpectedCurrentLevel로 재확인. 불확실 중 새 구매·취소 불가. 비활성 중 늦은 Accepted가 이벤트·강제 이동을 만들지 않음 | 같은 presenter 인스턴스에 한정. 실제 중복 차감·앱 종료 복구·중앙 결과 조회는 미검증 |
| 단계1 선택 제출 | SubmitCampaignAsync/SubmitMultiplayerAsync 직접 호출은 Unavailable. sealed 기반 구현에 후속 서비스 호출 없음 | 단계1에서 stage/room 요청 또는 운영 선택창 연결 없음 |

`UpgradeTransactionTestBackend`는 테스트 assembly의 컴포넌트이며 운영 프리팹·씬에 직렬화하지 않았다. 테스트는 실제 서비스에 성공을 주입하거나 저장하지 않았다. Unknown과 확정 Rejected/Unavailable는 구분되며, 실제 운영 어댑터는 무전송이 확정된 Unavailable만 반환한다.

## 실제 Unity 실행 결과

최종 [compile-final.json](UpgradePhase1Task3/compile-final.json): `compilationFailed=false`, `compiling=false`, `consoleErrors=0`. 편집기는 종료하지 않았으며 원래 Lobby 씬, Play 중 아님, dirty=false 상태로 돌아왔다.

| 실행 검사 | 결과/증빙 |
|---|---|
| EditMode UpgradePhase1 | [7/7 통과](UpgradePhase1Task3/editmode-upgrade.json) |
| PlayMode UpgradePhase1 | [10/10 통과](UpgradePhase1Task3/playmode-upgrade.json), 이후 추가한 [운영 ledger/상태 보존 1/1 통과](UpgradePhase1Task3/playmode-project-ledger.json). 최종 알려진 레벨 표시 보완 후 [누락/차단 검사 1/1 재통과](UpgradePhase1Task3/playmode-incomplete-final.json) |
| PlayMode ProductionFlowTests | [7/7 통과](UpgradePhase1Task3/playmode-production.json): 기존 계정 성장/강화 허브/캠페인 상세 반환 포함 |
| PlayMode MenuThemeFlowTests | [3/3 통과](UpgradePhase1Task3/playmode-v7.json): v7 출력·전환·리소스·복귀 |
| PlayMode PortraitEntryFlowTests | [3/3 통과](UpgradePhase1Task3/playmode-entry.json): 실제 pointer raycast 캠페인 진입/반환, 미연결 경쟁·협동·광고 비활성화 유지 |
| EditMode CommercialPolishPolicyTests | [3/3 통과](UpgradePhase1Task3/editmode-polish.json) |
| EditMode FixedGroundAndIndependentReproducibleDirections | [1/1 통과](UpgradePhase1Task3/editmode-v7-ground.json) |
| 패키지 Validate Upgrade Phase 1 메뉴 | [94개 정적 계약/import/Generated prefab 검사 통과](UpgradePhase1Task3/unity-console.json). 운영 실거래 검증과 별개 |

최종 서로 다른 테스트 **35개 통과**. 최초 장문 검사의 실패와 수정 후 성공을 구분했다. HTML/독립 데모 씬 실행 검사는 이번 작업에서 하지 않았다. 실거래 서버 테스트도 실행하지 않았다.

기존 UI 회귀 중 `PlayOneShot was called with a null AudioClip` 경고가 관찰되어 console 증빙에 남겼다. 새로운 오디오 변경은 없다. v7 토끼 시트 두 개의 원본 패키지 불일치는 작업 1 때 기록한 해시와 현재 해시가 같으며 작업 시작 전부터 있던 변경이다. [v7 보존 audit](UpgradePhase1Task3/v7-preservation-audit.json)에 기록하고 그대로 보존했다. 이 동물 강화 단계의 수용 판정에 다른 미완료 프로젝트 기능의 기준을 덧붙이지 않았다.

## 28개 수용기준별 판정

`통과(UI 통제)`는 실제 Unity 프리팹에서 통제된 응답으로 확인한 항목이다. `부분`/`미검증`은 운영 전체 통과가 아니다. 기계 판독 결과는 [acceptance-results.json](UpgradePhase1Task3/acceptance-results.json).

| ID | 이번 판정 | 근거/남은 항목 |
|---|---|---|
| P1-SCOPE-01 | 통과 | 동물 강화 경로만 연결, 선택 운영 연결 추가 없음 |
| P1-SCOPE-02 | 통과 | 두 submit Unavailable 및 무전송 코드/호출 검사 |
| P1-IMPORT-01 | 통과 | 지침·실제 코드/asset·작업 1/2 확인 |
| P1-IMPORT-02 | 통과 | 컴파일 및 현재 운영 진입/비활성 정책 회귀 통과 |
| P1-ART-01 | 부분 | 원본 15/15 동일, 실명 ID는 Rabbit→RABBIT만 확인 |
| P1-ART-02 | 통과 | 15종 importer/카드/큰 초상/캔버스/비율 검사 |
| P1-ART-03 | 부분 | 양 해상도/모의 Safe Area/장문 fixture 통과. 실제 최대 데이터와 모바일 기기 미검증 |
| P1-ROSTER-01 | 부분 | 5/5/5와 선택 갱신 통과. 실제 트랙 데이터 없음 |
| P1-ROSTER-02 | 부분 | 현재 프로젝트 상태 보존/허브 반환 통과. 동물 영속 저장·서버 상태 비교 불가 |
| P1-ROSTER-03 | 통과 | 미매핑/미구현 열람과 구매 차단 |
| P1-DATA-01 | 부분 | ledger 경계 및 동물별 UI fixture 통과. 실제 숙련도 없음 |
| P1-DATA-02 | 미검증 | 실제 동물 active/passive catalog·저장소 미제공 |
| P1-DATA-03 | 통과 | 운영 unknown과 데이터 누락 UI guard 검사 |
| P1-DATA-04 | 통과(UI 통제) | 의도적으로 순서를 뒤집은 snapshot/quote 응답 무시 |
| P1-DATA-05 | 부분 | 누락 비용 차단, 명시적 free/0 fixture만 허용. 실제 무료 데이터 없음 |
| P1-DATA-06 | 부분 | 전체 장문/8종 비용 모달 fixture 통과. 실제 최장 비용/효과 없음 |
| P1-STATE-01 | 부분 | 현재 Unconfigured/Unavailable 및 locked/미구현 UI fixture 통과. 실제 해금 조회 없음 |
| P1-STATE-02 | 부분 | 부족/MAX UI 요청 0회. 실제 잔액/상한 카탈로그 없음 |
| P1-STATE-03 | 통과(UI 통제) | 미연결/읽기 대기/실패에서 차단하고 새로고침 회복 |
| P1-TRADE-01 | 부분 | 확인 모달/취소 0회 fixture 통과. 실제 Ready 견적 없음 |
| P1-TRADE-02 | 부분 | 5개 요청 필드/연타 1회 fixture 통과. 실제 서비스 요청 없음 |
| P1-TRADE-03 | 부분 | 승인 전 UI 불변 통과. 실제 동물 저장소 없음 |
| P1-TRADE-04 | 미검증 | 거절 후 최신 표시 fixture만 통과. 서비스 가격/레벨/잔액 재검증 없음 |
| P1-TRADE-05 | 미검증 | Accepted 화면/event fixture만 통과. 원자적 거래·실제 저장 일치 미검증 |
| P1-TRADE-06 | 부분 | 현재 Unavailable 및 거절 이유/재조회 fixture 통과. 실거래 거절 없음 |
| P1-TRADE-07 | 부분 | Unknown 동일 요청 유지 통과. 실제 멱등 조회/중복 차감 검증 불가 |
| P1-TRADE-08 | 부분 | 같은 객체 비활성/재진입 통과. 중앙 요청 관리·기기 앱 복귀 미검증 |
| P1-REPORT-01 | 통과 | 패키지/운영 UI/fixture/실제 서비스 증빙 분리, 전체 완료 주장 없음 |

## 필요한 후속 조치

1. 실제 동물 성장 서비스/저장소가 있는 경로 또는 브랜치를 제공하고 데이터 소유자를 확정해야 한다. 이 저장소에서는 새로 연결할 구현을 찾지 못했다.
2. 실제 종 ID 14개와 15종의 성장 ID, 소유/해금·구현 상태, 동물별 숙련도/독립 트랙, 실제 레벨별 효과·비용·상한을 제공해야 한다. 누락 값은 계속 구매 불가로 유지한다.
3. 권한 있는 견적 토큰과 최신 가격 버전·예상 레벨·잔액 검증, 원자적 차감/증가, ActionId 멱등 결과 조회 및 중앙 요청 영속화 계약을 연결해야 한다. 이후 실제 승인/거절/응답 유실/재진입 테스트와 저장 비교를 수행해야 한다.
4. 실제 최대 숫자/최장 효과 데이터 및 모바일 장치의 Safe Area·터치·백그라운드 복귀를 재검증해야 한다. 현재 숫자 타입 경계 fixture와 Editor inset 검사를 기기 검사로 대체 표기하지 않는다.

## 변경 파일과 보존

- `Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiPresenter.cs`: 장문 compact 표시와 전체 모달 보존, 새 모달 스크롤 초기화.
- `Assets/ANIMOL/UI/AnimalUpgradePhase1/Tests/EditMode/UpgradePhase1AssetTests.cs`: 안정 ID/PNG 파일명 1:1 검사.
- `Assets/ANIMOL/UI/AnimalUpgradePhase1/Tests/PlayMode/UpgradePhase1AcceptanceTests.cs` 및 `.meta`: 7개 추가 수용 검사.
- 같은 PlayMode 디렉터리의 `UpgradeTransactionTestBackend.cs`: 통제된 지연 조회 응답 주입 지점.
- 같은 디렉터리의 `ANIMOL.AnimalUpgradePhase1.Tests.PlayMode.asmdef`: 기존 로비 ledger 경계 검사 참조.
- 이 보고서, `Docs/UpgradePhase1Task3/`의 기준 사본·검증 JSON·캡처 74장·변경 파일 목록.

운영 어댑터/프리팹/ID 매핑/PNG/계정 성장/캠페인/멀티플레이/로비/씬은 수정하지 않았다. 테스트로 생성된 TMP fallback atlas와 자동 아트 참조 캐시는 검사 전 바이트로 복원했다. 이번 작업 이전의 미커밋 변경은 이번 커밋에 포함하지 않는다.
