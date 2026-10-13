# ANIMOL 스테이지 동물 확인 2단계 · 작업 3 검증 결과

작성: 2026-10-04 / Unity 6000.3.8f1 / 현재 ANIMOL 작업 트리

## 판정

**실행 가능한 프로젝트 검증과 장문 모달 수정은 완료했다. 2단계 전체 운영 수용은 미완료다.** 최종 Unity EditMode/PlayMode 테스트는 **60/60 통과**, 컴파일 오류는 0이다. 수용기준 28개는 통과 10개, 부분 검증 15개, 미검증 3개다. 테스트 통과 수와 운영 수용기준 충족 수는 다르다. [개별 판정 JSON](StagePhase2Task3/acceptance-results.json).

실제 고정 3종 편성, 14종 실제 ID/구현/능력 데이터, 계정별 입장 권한 조회, 승인 receipt·멱등 처리·결과 조회·중앙 요청 관리자가 없다. 실제 승인 후 기존 로더 연결은 여전히 미완료이며 운영 시작 버튼은 비활성이다. 테스트용 Accepted를 운영 성공으로 사용하지 않았다.

## 확인한 기준과 운영 경로

저장소 AGENTS.md, 패키지 README_PHASE2.md·acceptance-phase2.json·UNITY_APPLY_PHASE2.md 작업 3, 작업 1/2 결과와 현재 Presenter·adapter·host·ID map·카탈로그·로더 소스를 확인했다. 실행 중인 실제 프로젝트 Editor에 Unity CLI로 연결했다. 패키지 HTML 독립 미리보기는 이번 작업에서 실행하지 않았다.

기존 uGUI, Input Manager/단일 EventSystem, 1080×1920 width-match CanvasScaler, SafeAreaLayout, PixelTypographyProfile/PixelTextBridge를 유지한다. 운영 프리팹은 `Assets/ANIMOL/UI/AnimalStagePhase2/Resources/ANIMOLAnimalStagePhase2/StageAnimalSelect.prefab`이다. Generated 프리팹·데모·운영 프리팹을 재생성하지 않았다.

진입은 `ProductionController.SelectStage → SC04_StageDetail → AnimalStagePhase2Host.OpenSelectedStage → AnimalUiPresenter.OpenCampaign`이며, 뒤로는 `Cancelled → UiNavigationService.Back → SC03_StageSelect`다. 실제 T01~T05 첫 스테이지 각각의 진입·반환·배경 참조·시작 차단을 실행했다. 데모 host/DemoSwitcher가 없고 활성 EventSystem은 하나였다.

기존 로더 후보는 `ProductionController.BeginLoading/LoadSelected → StageLaunchUseCase.Launch → CampaignLaunchContext.Set → SceneManager.LoadSceneAsync`다. 현재 Launch는 로컬 RunId 생성이며 입장 승인 계약이 아니다. 호스트에 CampaignAccepted → 로더 소비 연결은 없다. 이 경로를 호출해 임의 승인을 만들지 않았다.

## 실제 데이터 감사

[Editor에서 다시 읽은 카탈로그와 매핑](StagePhase2Task3/project-catalog-audit.json): 실제 스테이지 100개 중 T01-S01에만 RABBIT 1종이 있고 나머지 99개는 빈 목록이다. T01-S01의 정책은 실제 `content:2;runtime:2`, 편성은 `Rabbit||`다. 다른 스테이지의 누락된 정책·대표 역할·편성은 설정 대기로 남는다. 예시의 토끼/몽환여우/제비를 운영 데이터에 복사하지 않았다.

| 역할 | 아트 안정 ID → 실제 ID |
|---|---|
| 지상 | Rabbit → RABBIT; Wolf/WhiteFerret/MountainGoat/Otter → 각각 미매핑 |
| 특수 | DreamFox/StarCat/MirrorDeer/DreamMole/ClockMoth → 각각 미매핑 |
| 공중 | Swallow/Owl/FlyingSquirrel/Hummingbird/Bat → 각각 미매핑 |

15개 명시적 매핑 행과 아트 파일명을 대조했다. DEV 폼을 실제 종으로 대체하지 않았다. 실제 능력·성장 조회가 없으므로 액티브/패시브는 `-- · 설정 대기`다. 아트 존재로 구현·보유·사용 권한을 부여하지 않는다. 기존 1단계 성장 서비스의 미연결 상태도 그대로다.

## 발견한 문제와 수정

장문 모달의 끝부분이 스크롤 범위를 벗어나는 문제를 추가 검사로 재현했다. 기존 ContentSizeFitter는 uGUI Text의 높이를 사용하지만 화면은 PixelTextBridge의 TMP 글꼴과 문단 재배치로 표시되어, 실제 텍스트가 더 길어졌다. [수정 전 실패](StagePhase2Task3/initial-long-modal-failure.json).

PixelTextBridge에 스크롤 본문 전용 높이 계산 옵션을 추가했다. 실제 TMP 문단 배치 이후 높이를 LayoutElement로 전달하고 줄 간격을 확보한다. AnimalStagePhase2Host.Awake에서 **스테이지 모달 본문에만** 활성화한다. 다른 Text의 기본 동작은 유지한다. 마지막 패시브 설명까지 스크롤 가능하고 짧은 확인 모달은 viewport 안에 들어오는지 검사했다.

새 PlayMode 검사 4개는 늦은 A 조회/B 현재 상태, 잘못된 권한·문맥의 강제 버튼 호출, 잘못된 receipt와 중복 승인 이벤트, 비활성화 중 늦은 승인 및 동일 요청 재확인을 다룬다. 기존 장문 검사에 두 해상도·Safe Area·실제 텍스트 overflow·스크롤 끝·고정 모달 버튼 검증을 추가했다. 짧아 스크롤이 필요 없는 본문은 normalizedPosition 대신 viewport 포함으로 검사한다.

## 실제 Unity 실행 결과

| 실행 | 최종 결과 / 증빙 |
|---|---|
| 실제 Editor 컴파일 | [compilationFailed=false, compiling=false, Console Error 0](StagePhase2Task3/unity-compile.json) |
| Stage EditMode | [11/11](StagePhase2Task3/editmode-stage.json): 실제 100개 스테이지 차단·매핑/import·문맥·지연 조회·운영 생성 보호 |
| Stage PlayMode | [9/9](StagePhase2Task3/playmode-stage.json): 실제 운영 화면 및 분리된 테스트 backend 시나리오 |
| 기존 강화 EditMode | [asset 3/3](StagePhase2Task3/editmode-upgrade-assets.json), [backend 4/4](StagePhase2Task3/editmode-upgrade-backend.json) |
| 기존 강화 PlayMode | [11/11](StagePhase2Task3/playmode-upgrade.json) |
| 기존 Production 흐름 | [7/7](StagePhase2Task3/playmode-production.json): 계정 성장·허브 반환·기존 로더 실패 경로 등 |
| v7 / 기존 캠페인·멀티 메뉴 진입 | [3/3](StagePhase2Task3/playmode-v7.json), [3/3](StagePhase2Task3/playmode-entry.json) |
| 연출 정책 | [CommercialPolishPolicyTests 3/3](StagePhase2Task3/editmode-feedback.json) |
| 폰트/타이포그래피 회귀 | [EditMode 3/3](StagePhase2Task3/editmode-fonts.json), [PlayMode 3/3](StagePhase2Task3/playmode-typography.json) |
| 패키지 Editor 계약 검사 | [Stage 119개 / Upgrade 94개](StagePhase2Task3/unity-menu-validation.json), 운영 승인 성공 검사는 아님 |

60은 최종 고유 테스트 합계다. 수정 전 실패와 반복 실행을 중복 합산하지 않았다. Console의 누적 로그 카운터에는 과거 세션 이력이 포함되므로 컴파일 판정은 groundTruth를 사용했다. 기존 null AudioClip 경고는 별도 오디오 수정 없이 유지했다.

## Game View와 원본 아트

15종 카드와 큰 초상의 ID/Sprite 참조를 전부 검사했다. 각 역할은 5종이다. [원본 PNG 감사](StagePhase2Task3/portrait-byte-audit.json): 패키지와 15개 SHA-256 일치, 모두 128×160, 불투명 색상 합집합이 Sweetie16의 16색과 일치한다. 원본 바이트·캔버스·여백을 변경하지 않았다. Rabbit 해시는 `8d51fbdd019aa964980ac30ed49f3f97a80d190840055de52e8044d16dd32257`이다.

Unity importer의 Single/Full Rect, Point, 무압축, mipmap 없음과 카드/큰 초상 preserveAspect를 검사했다. 귀·클로버 핀·뿔·날개의 추가 프레임 크롭은 보이지 않았다. 스크롤 viewport 경계 밖에서 일시적으로 가려진 내용은 스크롤하면 전체가 보인다. 원본 자체의 경계 형태와 UI 크롭을 구분했다.

1080×1920/1080×2400 각각 정상 영역과 좌우 48px·하단 96px·상단 120px의 모의 Safe Area에서 실행했다. 실제 기기 notch 검사는 아니다. 카드 위 EventSystem 드래그가 BodyScroll로 전달되고 하단 시작 버튼이 움직이지 않으며 안전 영역 안에 있는지 검사했다.

- [15종 큰 초상 모음](StagePhase2Task3/all-15-hero-contact.png), [역할별 15종 카드 모음](StagePhase2Task3/all-15-card-contact.png).
- [1920 토끼](StagePhase2Task3/1920_full_Rabbit.png), [1920 Safe Area 시계나방](StagePhase2Task3/1920_safe_ClockMoth.png), [2400 하늘다람쥐](StagePhase2Task3/2400_full_FlyingSquirrel.png), [2400 Safe Area 공중 카드](StagePhase2Task3/2400_safe_cards_Air.png).
- 실제 문맥과 다른 테마: [T01-S01](StagePhase2Task3/actual_T01-S01.png), [T02-S01](StagePhase2Task3/actual_T02-S01.png). T03~T05도 증빙 폴더에 있다.
- 표시 fixture: [1920 Safe Area 장문 처음·최대 정수](StagePhase2Task3/fixture_only_1920_safe_detail_top.png), [장문 마지막 패시브](StagePhase2Task3/fixture_only_1920_safe_detail_bottom.png), [2400 Safe Area 장문 끝](StagePhase2Task3/fixture_only_2400_safe_detail_bottom.png), [지정 3종 확인](StagePhase2Task3/fixture_only_1920_safe_confirmation.png).

총 95개 실제 Game View 캡처를 생성했다. [전체 파일명·크기·해시](StagePhase2Task3/capture-manifest.json); 전체 원본은 `%TEMP%/ANIMOL-StagePhase2-Captures`, 대표 원본과 검토용 합성 19개는 보고서 폴더에 보관한다. 합성은 캡처 검토용이며 운영 아트를 수정한 것이 아니다. 장문·int.MaxValue는 표시 fixture 값이다. 실제 최대 레벨·최장 효과가 존재한다고 주장하지 않는다.

## 요청·receipt·로더 관찰 결과

| 시나리오 | 실제 관찰 범위와 결과 |
|---|---|
| 운영 100개 카탈로그 | 실제 adapter 조회 실패/제출 Unavailable. 권한·런·편성 저장 생성 경로 없음. CampaignLaunchContext.Pending 유지 |
| 운영 5테마 UI 및 15종 열람 | 강제 시작 버튼 Invoke에도 확인 모달 없음, ProductionController.LoadRequests=0. Draft/FixedLoadout/stage/map 직렬화값 유지, 뒤로 목록 복귀 |
| 지정 외 12종 미구현 | fixture 지정 3종만 Implemented/권한 true, Unlocked=false여도 확인 가능. 실제 체험 grant 검증은 아님 |
| 구현/권한/사용 불가·보유만 true·버전/역할 오류 | fixture 9가지 상태에서 강제 버튼 호출에도 제출 0, LoadRequests=0 |
| 모달 취소 / 처리 중 연타 | fixture 취소 0회, 확정 연타는 최초 제출 1회. 승인 전 LoadRequests=0, 뒤로/다른 문맥 열기/새로고침 차단 |
| 잘못된 Accepted | 빈 token·다른 context·다른 policy·다른 편성 각각 accepted 이벤트 0, LoadRequests=0, Unknown 유지 |
| 잘못된 receipt 이후 결과 확인 | 4회 잘못된 응답과 최종 유효 응답까지 총 5회 조회/제출 호출의 직렬화 요청이 동일. 최종 presenter 이벤트 1회. 재클릭/동일 인스턴스 재활성화 후 추가 이벤트·제출 0 |
| 타임아웃 → 최종 Unavailable | fixture 최초 1회+결과 확인 1회, ActionId/ContextId/SnapshotRevision/PolicyRevision/Loadout 모두 동일. 실제 거절 메시지 표시, 시작 비활성, LoadRequests=0 |
| 비활성화 중 늦은 Accepted | fixture 비활성 상태 accepted 이벤트/LoadRequests=0. 재활성화 후 같은 요청으로 확인하여 이벤트 1회. 운영 로딩은 0 |
| A 조회보다 B 조회가 먼저 완료 | base 권한 캐시와 presenter의 현재 설명이 B로 유지. 실제 외부 서비스 지연 검사는 아님 |
| 수동 새로고침 | 실제 호스트가 변경된 메모리 카탈로그 문맥을 다시 읽음. source asset은 변경하지 않음 |

런 생성의 별도 운영 카운터는 없다. 따라서 위 표의 런 관련 근거는 adapter/host에 Launch 호출이 없다는 소스 검사와 Pending 유지이며, 서버 런 카운터 측정으로 표현하지 않는다. 유효 fixture receipt의 presenter 이벤트 1회는 **실제 로더 1회 성공**을 의미하지 않는다. 실제 서버의 재검증·중복 차감/런 방지·저장 일치 결과는 검증하지 못했다.

## 수용기준별 판정

| 기준 | 판정 | 남은 부분 또는 증거 범위 |
|---|---|---|
| P2-SCOPE-01 | 통과 | 강화·로비·기존 진입 회귀, 새 멀티 연결 없음 |
| P2-SCOPE-02 | 통과 | 운영 adapter의 강화/멀티 Unavailable |
| P2-IMPORT-01 | 통과 | 현재 소스·운영 자산 분리 |
| P2-IMPORT-02 | 통과 | 컴파일·기존 UI/로더 실패 회귀·데모 미설치 |
| P2-ART-01 | 부분 | 원본 보존 통과, 14종 실제 ID 없음 |
| P2-ART-02 | 통과 | importer·15종 카드/큰 초상·현재/fixture 슬롯 표시 |
| P2-ART-03 | 부분 | Editor 화면/장문 수정 통과, 실제 최대 데이터·기기 미검증 |
| P2-CONTEXT-01 | 부분 | 실제 5테마 대조, 완성된 실제 고정 3종 없음 |
| P2-CONTEXT-02 | 부분 | Fixed/역할 차단 통과, 실제 완성 편성 없음 |
| P2-CONTEXT-03 | 통과 | 누락/다른 문맥·버전 요청 차단 |
| P2-CONTEXT-04 | 통과 | 제어된 비동기 응답 순서·pending 재열기 차단 |
| P2-ROSTER-01 | 부분 | 5×3 열람 통과, 실제 능력 조회 미연결 |
| P2-ROSTER-02 | 부분 | 열람/반환 불변, 전체 3종 확인은 fixture |
| P2-DATA-01 | 부분 | unknown 정직 표시, 실제 성장 값 없음 |
| P2-PERMISSION-01 | 부분 | 지정 3종만 검사하는 fixture, 운영 권한 없음 |
| P2-PERMISSION-02 | 통과 | 구현/권한 불가·보유만 true 제출 0 |
| P2-PERMISSION-03 | 부분 | Unlocked=false 체험 fixture, 실제 grant 없음 |
| P2-PERMISSION-04 | 부분 | 실제 미설정/미연결 차단, 계정별 권한 서비스 없음 |
| P2-ENTRY-01 | 부분 | fixture 전체 편성 확인·취소 0 |
| P2-ENTRY-02 | 부분 | fixture 한 요청·불변 필드·중복 차단 |
| P2-ENTRY-03 | 미검증 | 실제 권한/버전 재검증 서비스 없음 |
| P2-ENTRY-04 | 미검증 | 실제 승인 receipt와 로더 소비 연결 없음 |
| P2-ENTRY-05 | 통과 | 잘못된 receipt fixture는 Unknown/로딩 0 |
| P2-ENTRY-06 | 미검증 | 운영 host의 receipt 중복 소비 방지 계약 없음 |
| P2-ENTRY-07 | 부분 | 거절 표시·호스트 재조회 통과, 실제 stale 거절 없음 |
| P2-ENTRY-08 | 부분 | 동일 요청 재확인 통과, 서버 멱등 결과 미검증 |
| P2-ENTRY-09 | 부분 | 동일 인스턴스 생명주기 통과, 중앙 복구 계약 없음 |
| P2-REPORT-01 | 통과 | 패키지/Unity/fixture/실제 서비스 판정 구분 |

## 변경 파일과 후속 조치

직접 수정한 소스는 `Assets/ANIMOL/Typography/PixelTextBridge.cs`, `UI/AnimalStagePhase2/AnimalStagePhase2Host.cs`, 같은 모듈의 `Tests/PlayMode/StagePhase2FlowTests.cs`, `StagePhase2PresentationFixture.cs` 네 개다. 보고서와 증빙을 추가했다. [정확한 변경 파일 목록](StagePhase2Task3/changed-files.txt).

운영/Generated 프리팹·카탈로그·실제 ID map·PNG는 이번 작업에서 변경하지 않았다. v7·캠페인 데이터·1단계 UI·씬·동물 아트 457개 파일을 작업 전후 SHA-256으로 비교해 변경 0개였다. [보존 결과](StagePhase2Task3/preservation-result.json), [비교 기준](StagePhase2Task3/preservation-before.json). 테스트의 폰트 atlas/아트 참조 캐시는 작업 시작 바이트로 복원했다. 기존 사용자의 변경은 별도 유지한다.

운영 수용을 마치려면 다음이 필요하다.

1. 승인된 실제 3역할 고정 편성, 14종 종 ID 및 구현/능력 데이터와 실제 정책 버전을 카탈로그 소유자가 제공해야 한다.
2. 기존 입장 서비스의 권한 스냅샷·최신 정책 재검증·receipt·ActionId 멱등/결과 조회 계약을 제공한 뒤 adapter에 연결해야 한다. 클라이언트가 합성한 버전/RunId로 대신하지 않는다.
3. 검증된 receipt를 기존 로더에 한 번만 전달하는 host 연결과 중앙 요청 보존을 구현한 뒤 실제 승인·stale 거절·타임아웃·중복 receipt·화면 재생성/앱 복귀를 서버/런 카운터와 함께 재검증해야 한다. 현재 동일 인스턴스의 보존은 프로세스 종료나 씬 재생성을 보장하지 않는다.
4. 실제 최대 데이터와 실제 기기 Safe Area/터치 검사를 추가해야 한다. 그 전까지 운영 시작 비활성을 유지한다.

이번 판정에는 멀티플레이 방 진입, 새 성장/BM 설계, 독립 HTML 시안의 외관 통과를 운영 완료 근거로 포함하지 않았다.
