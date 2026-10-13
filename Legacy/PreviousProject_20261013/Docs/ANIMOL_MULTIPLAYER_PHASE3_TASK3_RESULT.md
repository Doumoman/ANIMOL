# ANIMOL Multiplayer Phase 3 — 작업 3 실제 프로젝트 검증

2026-10-04 / Unity 6000.3.8f1 / 현재 ANIMOL 저장소의 실행 중 Editor, Lobby 씬.

**판정: 단계3 운영 전체 미완료.** 실제 프로젝트 UI·차단 경계·비동기 fixture 검증을 실행했다. 운영 모드/진입 의도·계정 권한·방 입장·receipt·중앙 복귀/대기방 소비 계약이 없으므로 실제 방 입장 성공으로 판정하지 않는다. Phase 1·2도 운영 서비스가 미연결이다.

사용자가 요청한 **Phase 1·2·3의 작업 1·2·3 전체 수행 내역과 미구현/미설정/미연결 목록**은 [통합 현황](ANIMOL_ANIMAL_UI_PHASE1_2_3_REMAINING_WORK.md)에 모았다. 이전 작업의 미연결 표현과 이후 수정 완료 사항을 구분했다.

## 읽은 기준과 실제 연결

- 저장소 `AGENTS.md`, `ProjectSettings/ProjectVersion.txt`, 패키지 `README_PHASE3.md`, `Docs/acceptance-phase3.json`, 현재 패키지/프로젝트 Unity 소스, 이전 8개 작업 결과를 확인했다.
- [원본 수용기준](MultiplayerPhase3Task3/acceptance-phase3.source.json)은 수정하지 않고 별도 [이번 판정 JSON](MultiplayerPhase3Task3/acceptance-results.json)에 결과를 기록했다. 원본의 환경 플래그는 패키지 제작 당시 값이다.
- 실제 UI 구축은 기존 Lobby Canvas/CanvasScaler/SafeAreaLayout/EventSystem과 `PixelTextBridge`를 사용한다. 운영 프리팹의 자동 시안 열기는 false, Backend는 `AnimalMultiplayerProjectAdapter`, Catalog/IdMap은 운영 전용 asset이다. 별도 데모 Host/EventSystem을 추가하지 않았다.
- 실제 경로: `PortraitEntryController.ModeScreen.Competition → SC05_CompetitiveHub → CompetitiveMatchButton / RankedMatchButton / CompetitivePrivateButton → AnimalMultiplayerPhase3Entry/Host.OpenUnconfigured → AnimalUiPresenter.OpenMultiplayer`.
- 반환: `Presenter.Cancelled → Host.Return → navigation.Back → SC05_CompetitiveHub`. 원래 편성 복사본을 반환한다. 운영 원래 편성 데이터 소유자가 없어서 현재 실제 경로는 빈 편성이다. fixture 원래 3종 편성 반환도 별도로 실행했다.
- `Host.Open`은 문맥 복사, PREVIEW_ONLY 거절, 확인 모달/처리 중 재진입 차단을 유지한다. 현재 Host에는 `MultiplayerAccepted`를 운영 대기방에 전달하는 소비자가 없다.

운영 프리팹: `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Resources/ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect.prefab`. 이번 작업에서 재생성하지 않았다. 프로젝트 어댑터·운영 prefab·기존 단계 생성물을 덮어쓰지 않았다.

## 실제로 발견하고 수정한 것

1. **긴 모드 이름이 제목 영역을 넘쳤다.** 처음 PlayMode 실행에서 `PixelTextBridge.Overflow` 실패를 재현했다. 제목은 긴 이름을 문자 요소 단위로 요약하고 전체 원문은 읽기 전용 동물 정보와 전체 편성 확인 모달에 유지했다. 호스트 문맥과 요청의 이름/ID는 변경하지 않는다.
2. **결과 조회가 미지원일 때 구체적인 이유가 Unknown 모달에서 빠졌다.** 어댑터 메시지와 잘못된 승인 receipt 사유를 모달에 표시하고, 같은 인스턴스 비활성화/재활성화 후에도 보존하도록 수정했다. 미지원 상태에서 재확인을 눌러도 보호 입장 호출은 추가되지 않는다.

수정 전 [11개 중 2개 실패](MultiplayerPhase3Task3/initial-playmode-results.json), [이름 넘침](MultiplayerPhase3Task3/initial-long-mode-overflow.png), [미지원 사유 누락](MultiplayerPhase3Task3/initial-unknown-missing-reason.png)을 보관했다. 수정 후 멀티플레이 PlayMode **11/11 통과**. 실패 기록을 성공으로 덮어쓰지 않았다.

변경한 소스 4개:

| 파일 | 변경 |
|---|---|
| `Assets/ANIMOL/AnimalUiV2/Runtime/AnimalUiPresenter.cs` | 멀티플레이 긴 이름 요약/원문 상세, Unknown 이유 보존·표시 |
| `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Tests/PlayMode/MultiplayerPhase3FlowTests.cs` | 장문 모드/상세/확인 모달 검사와 두 해상도 캡처 |
| `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Tests/PlayMode/MultiplayerPhase3TransactionTests.cs` | 개별 권한/pool/잘못된 문맥/미지원 조회 검사 추가, 실제 UI fixture 요청·이벤트·SC06 이동 카운터/원문 요청 기록 |
| `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/Tests/PlayMode/MultiplayerTransactionFixture.cs` | 테스트에서 결과 조회 지원/미지원 선택 |

문서 2개와 `Docs/MultiplayerPhase3Task3/`의 검증 증거를 추가했다. 전체 파일 목록은 [changed-files.txt](MultiplayerPhase3Task3/changed-files.txt).

## 실제 데이터 감사

[실행 중 Unity에서 읽은 asset/연결 값](MultiplayerPhase3Task3/live-project-audit.json):

| 항목 | 현재 값 / 제한 |
|---|---|
| 실제 모드 카탈로그·진입 의도 | 운영 계약 없음. 두 `MultiplayerPreviewDefinition`은 DevelopmentPreviewOnly인 DEV 데이터 |
| 실제 버튼으로 전달된 문맥 | ModeId/EntryIntent/PolicyRevision=null, RepresentativeRole=-1, InitialLoadout 빈 편성. 가짜 모드/방 ID를 넣지 않음 |
| 종 ID | Rabbit ↔ RABBIT만 실제 asset으로 양방향 연결. Wolf/WhiteFerret/MountainGoat/Otter/DreamFox/StarCat/MirrorDeer/DreamMole/ClockMoth/Swallow/Owl/FlyingSquirrel/Hummingbird/Bat은 명시적 미매핑 |
| 실제 능력 | Rabbit도 ProductionActiveAbilityConfigured/ProductionPassiveAbilityConfigured=false. 다른 14종 실제 정의 없음 |
| 계정 성장 | CharacterUpgradeCatalog.Definitions=0, 15종 GrowthServiceId 미설정. DEV 성장 계약을 종 ID/가격/레벨로 사용하지 않음 |
| 캠페인 | 100개 스테이지 중 T01-S01만 RABBIT 1종, 나머지 99개 지정 편성 없음. 최신 3역할 고정 편성 운영 데이터 미완성 |
| 멀티 조회·입장 | ReadMultiplayerSnapshotAsync는 실제 서비스 미연결 오류, SubmitRoomEntryAsync는 Unavailable. 실제 snapshot/receipt를 발행하지 않음 |
| 기존 gateway | IMatchGateway.RequestReady(string[]) / DisconnectedMatchGateway는 모드/진입 의도/버전/ActionId/receipt 계약이 아님 |
| 대기방·복귀 | SC06/SC07 개발 미리보기는 존재하지만 운영 receipt consumer/영속 중앙 요청 관리자가 없음 |

보호 조회/입장은 공용 base의 공개 ReadSnapshotAsync/SubmitMultiplayerAsync 검증을 통과해야 한다. 프로젝트 어댑터의 강화/캠페인 메서드는 Unavailable이며 기존 1·2단계 adapter를 대체하지 않는다. 새 매칭 서버·방 목록·대기방 UX를 만들지 않았다.

## 아트와 Game View

**파일 검사:** [15종 원본 해시/크기/팔레트](MultiplayerPhase3Task3/portrait-byte-audit.json). 패키지 zip의 PNG와 프로젝트 PNG가 15/15 바이트 일치, 모두 128×160, 불투명 RGB 합집합은 공용 Sweetie16의 정확한 16색. PNG 리사이즈/재인코딩/여백 삭제 없음. Rabbit은 제공된 최신 v17 원본이다.

**실제 Unity:** 1080×1920·1080×2400 각각 전체 화면과 Safe Area inset(좌우 48px, 하단 96px, 상단 120px)을 사용했다. 실기기 notch를 측정한 결과는 아니다. 역할 탭마다 5개, 전체 15종의 카드/큰 초상 참조 일치, preserveAspect, 초상 전체 rect의 viewport 포함, 마스크·입력 전달, 카드 드래그→BodyScroll, 스크롤 중 하단 버튼 위치 불변을 검사했다. importer의 Single/Full Rect/Point/무압축/mipmap 없음은 실제 asset 검사와 메뉴 validator로 확인했다.

총 **Game View 원본 캡처 92장**: 실제 프로젝트 열람/fixture 화면 87장, 거래 fixture 5장. [전체 manifest](MultiplayerPhase3Task3/capture-manifest.json)에 크기·SHA-256을 기록했다. [15종 큰 초상 모음](MultiplayerPhase3Task3/hero-contact-sheet.png)은 실제 스크린샷의 초상 부분을 모은 검토용 이미지이며 게임 아트를 변경한 것이 아니다.

| 확인 내용 | 대표 증거 |
|---|---|
| 실제 기존 일반/랭크/비공개 진입 | [일반](MultiplayerPhase3Task3/Captures/CompetitiveMatchButton.png), [랭크](MultiplayerPhase3Task3/Captures/RankedMatchButton.png), [비공개](MultiplayerPhase3Task3/Captures/CompetitivePrivateButton.png) |
| Rabbit 귀·클로버 핀 / 1920 Safe Area | [Rabbit](MultiplayerPhase3Task3/Captures/1920_safe_Rabbit.png) |
| 2400 Safe Area / 날개 | [ClockMoth](MultiplayerPhase3Task3/Captures/2400_safe_ClockMoth.png), [Bat](MultiplayerPhase3Task3/Captures/2400_safe_Bat.png) |
| 역할별 카드 5개 / 긴 바늘다람쥐 이름 / 하단 버튼 | [지상](MultiplayerPhase3Task3/Captures/1920_safe_cards_Ground.png), [특수](MultiplayerPhase3Task3/Captures/1920_safe_cards_Special.png), [공중](MultiplayerPhase3Task3/Captures/1920_safe_cards_Air.png) |
| 긴 이름 요약과 원문 상세 | [2400 제목](MultiplayerPhase3Task3/Captures/fixture_long_mode_2400.png), [1920 상세 상단](MultiplayerPhase3Task3/Captures/fixture_1920_detail_top.png) |
| 장문 스크롤 마지막 패시브 설명 | [1920 하단](MultiplayerPhase3Task3/Captures/fixture_1920_detail_bottom.png), [2400 하단](MultiplayerPhase3Task3/Captures/fixture_2400_detail_bottom.png) |
| 현재 수달 열람과 별개인 늑대/몽환여우/제비 전체 편성 | [확인 모달](MultiplayerPhase3Task3/Captures/fixture_2400_confirmation.png) |

직접 스크린샷을 열어 15종 초상, 카드, 장문/모달/고정 버튼을 확인했다. 원본 자체의 하단 실루엣을 UI 크롭으로 오인하지 않았다. 스크롤 밖 콘텐츠가 viewport에 가려지는 것은 정상적인 스크롤이며, 초상을 열람 위치로 올리면 전체 캔버스가 포함된다.

긴 모드명·int.MaxValue 레벨·25줄 능력은 **TEST ONLY fixture**다. 실제 운영 최장 이름/최대 수치/장문 능력 데이터가 없으므로 그 운영 검증은 미완료다. 실제 경로의 미정 설명/레벨은 `설정 대기`/`--`를 유지한다. HTML/독립 미리보기를 실행해 운영 결과로 대신하지 않았다.

## 입장·승인·대기방 횟수

아래는 실제 Lobby의 운영 UI에 **테스트 어댑터를 주입한 Unity PlayMode 검사**다. 보호 메서드 호출과 Presenter 이벤트를 계수했으며 운영 서버 요청/실제 receipt/대기방 승인 전달이 아니다. [8개 시나리오의 문맥·요청 원문·최종 카운터](MultiplayerPhase3Task3/Transactions/).

| 시나리오 | 보호 입장 호출 | Presenter Accepted | SC06 대기방 이동 | 확인 |
|---|---:|---:|---:|---|
| 확인 취소 | 0 | 0 | 0 | 같은 테스트의 확정 전 지점에서 assert/캡처 |
| 확정·처리 중 연타/뒤로/탭/Refresh/재진입, 이후 정상 fixture receipt | 1 | 1 | 0 | 확인 당시 정확한 6개 요청 필드, 모달 SetBackend 차단, 중복 승인 이벤트 없음 |
| 개별 구현/권한/사용 조건 누락·ownership-only·empty/restricted pool | 0 | 0 | 0 | 열람 가능, 무효 슬롯 차단. 유효한 3종 외 미구현 동물은 차단 조건 아님 |
| 잘못된 문맥 12종·역할/편성/조회 버전/PREVIEW_ONLY | 0 | 0 | 0 | 직접 Presenter 경로도 확인/요청 불가 |
| Rejected/Unavailable 후 수동 조회와 새 확인 | 4 | 0 | 0 | 4개의 의도된 별도 확정. 거절 뒤 자동 재요청 없음, 실제 fixture 사유 유지, 새 ActionId |
| 타임아웃→잘못된 토큰/모드/정책/의도/편성→정확 receipt | 7 | 1 | 0 | 결과 조회 지원 fixture에서 7회 모두 동일 ActionId·전체 payload. 새 입장 7회가 아님 |
| 미지원 결과 조회 | 1 | 0 | 0 | 재확인해도 보호 호출 증가 없음, Unknown 유지, 미지원 메시지/변경·뒤로 차단 |
| 처리 중 비활성화→늦은 승인→재활성화 재확인 | 1 | 1 | 0 | 늦은 응답은 이전 화면에 이벤트 발행 안 함, 같은 객체의 캐시 재확인만 1회 이벤트 |
| 이전 조회 지연/새 모드/조회 실패 | 0 | 0 | 0 | 오래된 정보가 새 모드에 덮어쓰지 않음, 실패 시 권한 표시와 실행 상태 폐기 |

[취소](MultiplayerPhase3Task3/Transactions/fixture_cancel_zero.png), [처리 중 요청 1회](MultiplayerPhase3Task3/Transactions/fixture_pending_one_request.png), [fixture 승인 이벤트만 발생](MultiplayerPhase3Task3/Transactions/fixture_accepted_event_only.png), [타임아웃](MultiplayerPhase3Task3/Transactions/fixture_timeout_unknown.png), [미지원 결과 조회](MultiplayerPhase3Task3/Transactions/fixture_reconciliation_unsupported.png).

각 테스트에서 기존 MultiplayerUiPresenter 직렬화 상태·PreviewParticipantCount 불변과 대기방 이동 0회를 검증했다. 실제 저장 서버가 없으므로 DB 변경 0회를 원격 계측했다고 주장하지 않는다. 테스트 fixture는 명시된 미보유 허용/권한을 사용하며 운영 asset에 저장하지 않는다.

공용 base EditMode 검사는 문맥/요청/권한 snapshot의 깊은 복사, 후속 조회 cache와 미확인 attempt 분리, 동일 ActionId 내용 변경 거절, terminal result 복사/재생, 중복 보호 호출, 캐시 세대와 늦은 조회를 포함한다. 실제 두 계정이 같은 동물을 선택하는 서버 시나리오는 미실행이다. 기존 `M6MultiplayerContractTests` 8개 통과로 4~8인/경쟁전 중복 허용 계약을 확인했지만 운영 다계정 시험으로 세지 않는다.

## Unity 컴파일·테스트·보존

실제 Editor 컴파일 로그에 `Tundra build success`와 assembly reload가 있고, 수정된 테스트를 실행했다. 마지막 recompile 확인은 `up_to_date`, `failed=false`, `compilationFailed=false`. [컴파일 로그](MultiplayerPhase3Task3/unity-compile-log.txt), [컴파일 상태](MultiplayerPhase3Task3/unity-compile-status.json), [Console 상태](MultiplayerPhase3Task3/unity-console-status.json).

최종 **중복 없는 104개 테스트: 103 통과 / 1 기존 실패 / 0 건너뜀**. 초기 수정 전 실패 2건은 이 최종 수에 중복 합산하지 않았다. [전체 집계](MultiplayerPhase3Task3/test-summary.json).

| 실행 suite | 통과 / 전체 | 증거 |
|---|---:|---|
| Phase3 EditMode | 28/28 | [JSON](MultiplayerPhase3Task3/editmode-multiplayer.json) |
| Phase3 PlayMode | 11/11 | [JSON](MultiplayerPhase3Task3/playmode-multiplayer.json) |
| Phase2 EditMode / PlayMode | 11/11 + 9/9 | [Edit](MultiplayerPhase3Task3/editmode-stage.json), [Play](MultiplayerPhase3Task3/playmode-stage.json) |
| Phase1 EditMode / PlayMode | 7/7 + 11/11 | [Edit](MultiplayerPhase3Task3/editmode-upgrade.json), [Play](MultiplayerPhase3Task3/playmode-upgrade.json) |
| CommercialPolishPolicyTests | 3/3 | [JSON](MultiplayerPhase3Task3/editmode-feedback.json) |
| M6MultiplayerContractTests | 8/8 | [JSON](MultiplayerPhase3Task3/editmode-legacy-multiplayer.json) |
| ProductionFlowTests / PortraitEntryFlowTests | 7/7 + 3/3 | [Production](MultiplayerPhase3Task3/playmode-production.json), [Entry](MultiplayerPhase3Task3/playmode-entry.json) |
| MenuThemeFlowTests | 3/3 | [JSON](MultiplayerPhase3Task3/playmode-v7.json) |
| MenuThemePolicyTests | **2/3** | [실패 원문](MultiplayerPhase3Task3/editmode-v7-policy.json) |

기존 실패: `V7ImportsPreserveAllTwentyTwoOriginalPNGs`. 작업 시작 전부터 수정된 `rabbit-run-left.png`/`rabbit-run-right.png` 2개가 기준 v7 패키지와 다르다. [22개 비교](MultiplayerPhase3Task3/v7-existing-byte-audit.json). 사용자 변경을 임의 복원하거나 이번 변경에 포함하지 않았다. 로비 동작 회귀 검사는 통과했으나 원본 보존 정책까지 통과했다고 쓰지 않는다.

기존 null AudioClip 경고도 회귀 실행에서 재현되었다. [validator/경고 기록](MultiplayerPhase3Task3/unity-validation-and-warnings.json). Console의 누적 버퍼와 현재 Editor 카운터는 다르며 컴파일 오류로 오인하지 않는다. 메뉴 validator는 **Phase3 147 / Phase2 119 / Phase1 94개 통과**. 이는 계약/import/prefab 검사로, 실제 서버 검증이나 NUnit 104개 수에 합산하지 않는다. Build 메뉴 재실행은 이번 작업에서 하지 않았다.

시작 시 기존 Git 변경 492개를 분리했다. 테스트로 변경된 폰트 fallback atlas 4개와 NamedArtReferences cache는 시작 시 바이트로 복구하고 Unity에 재import했다. [복구 목록](MultiplayerPhase3Task3/restored-test-caches.json). 기존 MainUiV7·강화·캠페인·Generated·Data·Scenes·15종 Art의 **644개 파일 바이트 불변**을 확인했다. [보존 증거](MultiplayerPhase3Task3/preserved-assets.json).

## 수용기준 판정

`통과`는 아래에 명시한 실제 Unity 클라이언트 범위의 실행 결과다. 실제 서비스/계정/기기까지 요구하는 항목은 fixture가 통과해도 `부분` 또는 `미검증`이다. 구체적 상태·근거는 [acceptance-results.json](MultiplayerPhase3Task3/acceptance-results.json).

| ID | 판정 | 근거 / 남은 조건 |
|---|---|---|
| P3-HOST-01 | 부분 | 실제 3버튼 진입 확인. 운영 ModeId/의도/원래 편성/정책 소유자 없음 |
| P3-HOST-02 | 부분 | 실제 뒤로/원래 빈 편성, fixture 변경 후 원래 3종 반환 확인. 운영 저장 편성/DB 없음 |
| P3-HOST-03 | 통과 | 실제 씬 EventSystem 1개, 데모 없음, 운영 자동 시안 false, PREVIEW_ONLY 차단 |
| P3-ART-01 | 부분 | PNG 15/15 보존. 실제 양방향 ID는 Rabbit 1종, 14종 미매핑 |
| P3-ART-02 | 부분 | 두 해상도/Safe Area/15종/스크롤/모달 검사. 실제 최장 운영 이름·실기기 없음 |
| P3-DATA-01 | 통과 | 실제 미연결 상태 전체 15종 열람, --/설정 대기, 가짜 레벨/권한 없음 |
| P3-SELECT-01 | 부분 | fixture에서 해당 역할 슬롯만 변경, 실제 계정 권한/저장 없음 |
| P3-SELECT-02 | 통과 | Unity 각 권한 조건/ownership-only/불가 편성 호출 0회 |
| P3-SELECT-03 | 부분 | Unlocked=false 허용 fixture 통과. 실제 grant 서비스 없음 |
| P3-SELECT-04 | 통과 | 유효 3종 외 미구현 수달 열람에도 전체 확인 가능 |
| P3-SELECT-05 | 부분 | null pool 추가 제한 없음 확인. 서버 실제 풀 재검증 미지원 |
| P3-SELECT-06 | 통과 | 빈/제한 pool 열람 가능·무효 선택/요청 0회 |
| P3-SELECT-07 | 미검증 | 실제 다계정/4~8인 입장 없음. 기존 Core 계약 검사만 통과 |
| P3-CONTEXT-01 | 통과 | 누락/중복 역할/대표/버전/PREVIEW_ONLY 요청 0회 |
| P3-CONTEXT-02 | 통과 | 역할 오류·누락 슬롯·미지 ID 요청 0회 |
| P3-READ-01 | 통과 | 실제 Backend 미연결 차단, fixture 지연/오류 검사 |
| P3-READ-02 | 통과 | Unity 지연 Task의 이전 응답이 새 모드를 덮어쓰지 않음 |
| P3-ENTRY-01 | 부분 | 전체 3종 fixture 모달·취소 0회. 실제 운영 모드/권한 없음 |
| P3-ENTRY-02 | 부분 | fixture 요청 6필드/1회/잠금/Refresh/SetBackend 확인. 실제 서비스 없음 |
| P3-ENTRY-03 | 미검증 | 실제 서비스 재검증 계약 없음. fixture 승인 전 SC06 이동 0회만 확인 |
| P3-ENTRY-04 | 미검증 | 실제 receipt/기존 대기방 승인 전달 미연결 |
| P3-ENTRY-05 | 통과 | 잘못된 토큰/문맥/정책/의도/편성은 Unknown, 이동 0회 |
| P3-ENTRY-06 | 미검증 | 중앙 receipt consumer/운영 구독 없음. fixture 이벤트 중복 차단만 확인 |
| P3-ENTRY-07 | 부분 | fixture 최종 거절/사유/수동 조회/새 ID 통과. 서버 권한 변경 미실행 |
| P3-ENTRY-08 | 부분 | fixture 동일 ActionId/payload 재확인/독립 cache 통과. 운영 멱등 결과 조회 없음 |
| P3-ENTRY-09 | 통과 | Unity adapter mutation/deep-copy 검사, 원래 문맥/요청 불변 |
| P3-LIFE-01 | 부분 | 같은 객체 비활성화/재활성화만 통과. 중앙 보존/씬 파괴/OS 복귀 미지원 |
| P3-LIFE-02 | 통과 | 미지원 조회는 추가 보호 호출 0회, Unknown 유지·사유 표시·미완료 보고 |
| P3-REGRESSION-01 | 부분 | 기존 UI 회귀 통과. v7 기존 정책 1실패, 캠페인 실제 고정3/승인 미완료 |
| P3-GENERATOR-01 | 부분 | compile/validator/기존 생성물 보존 확인. Build 메뉴 이번 작업 재실행 안 함 |
| P3-REPORT-01 | 통과 | 실제 Unity/fixture/파일/운영 서비스 결과 분리, 미실행 항목 명시 |

## 운영 완료 전에 필요한 후속 작업

1. 실제 모드/의도/초기 편성/허용 풀/대표 역할/정책 데이터 소유자를 제공하고 Host.Open에 연결한다. 예시 값을 운영으로 복사하지 않는다.
2. 실제 15종 ID와 구현/권한/성장 Snapshot, 계정·풀·버전·ActionId를 재검증하는 기존 방 입장 서비스 및 정확한 receipt를 연결한다.
3. 기존 중앙 요청/receipt 관리자가 미확인 불변 요청을 보존하고, 승인만 기존 운영 대기방에 1회 전달하도록 한다. 미지원 상태에서 새 요청을 허용하지 않는다.
4. 실제 계정 2개 이상으로 동일 동물 선택, 4~8인 정책, stale 거절, 응답 유실/중복/앱 복귀·실기기 Safe Area를 검증한다.

세 단계 전체의 더 상세한 선행 조건·서비스 후보·완료 조건은 [통합 미완료 목록](ANIMOL_ANIMAL_UI_PHASE1_2_3_REMAINING_WORK.md)의 G/U/S/M/V 항목을 따른다. 이 문서의 UI/fixture 성공은 실제 강화 구매·캠페인 입장·멀티플레이 방 입장의 성공을 뜻하지 않는다.
