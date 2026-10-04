# NET02 TASK 03 — 실제 데이터와 모드 문맥 공급

기준일: 2026-10-05. TASK 03의 DEV 조회·변환·기존 UI 연결을 적용했다. 운영 데이터와 실제 입장 승인은 아직 미구성이다. TASK 04의 Phase3 snapshot/입장 어댑터 연결은 별도 작업이다.

후속 상태: [TASK 04](NET02_TASK04_RESULT.md)에서 DEV snapshot/입장 어댑터와 Unknown 복구를 연결했다. 아래는 TASK 03 완료 시점의 기록이며 실제 데이터 미구성 상태는 유지된다.

## 실제 공급자와 매핑

| 항목 | 공급자 | 적용 방식 / 현재 상태 |
|---|---|---|
| 아트 ID·역할 | `Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalCatalog.asset` | 기존 15종 사용. enum 정수 캐스팅 없이 Ground/Special/Air를 명시적으로 변환 |
| 실제 종 ID | 같은 폴더의 `AnimalMultiplayerIdMap.asset` → `CampaignAnimalDefinition.AnimalId` | ID→아트→실제 asset 왕복 검증. 실제 참조가 없는 항목은 제외. TEST_/DEV_를 운영 매핑에 추가하지 않음 |
| ModeId·DisplayName·PolicyRevision·대표 역할·허용 풀 | NET02 `/v1/catalog`의 `Modes` | 명시적으로 선택한 ID와 서버 정책 일치 확인. 배열 순서나 이름으로 추측하지 않음 |
| opaque EntryIntent | 서버 `Modes[].EntryIntents` + 로컬 DEV 모드 라우팅 설정 | public/create/join Kind와 정확히 일치해야 함. UI 버튼 이름을 intent로 전송하지 않음 |
| 일반·랭크 성장 정책 | 로컬 DEV `Modes[].GrowthPolicy` | 일반은 OwnedProgress, 랭크는 RankedMaximumPreset. 동일 ModeId 사용과 정책 혼동을 거절. 성장 수치 자체는 공급되지 않음 |
| 초기 3종·revision·종별 사용 권한 | `/v1/multiplayer/context`의 Context/Snapshot | 선택된 3종의 역할·구현·HasContextPermission·CanUseInContext 및 허용 풀 검증. Unlocked만으로 허용하지 않음 |
| 코드 참가의 모드·정책·인원·상태 | `/v1/room/lookup` | 실제 조회 응답의 ModeId를 사용. Waiting, 최대 4명, 빈 자리, 현 카탈로그 정책 일치를 확인한 다음 context 조회 |
| DEV 계정·endpoint·자격 증명 | `Application.persistentDataPath/ANIMOLNet02/connection.json` | 별도 로컬 설정. 게임 AccountIdentityState와 ExternalServiceConfiguration 변경 없음 |

실제 종 매핑은 다음과 같다. 파일에 없는 참조를 새로 만들지 않았다.

| 역할 | 아트 ID | 실제 종 ID |
|---|---|---|
| Ground | Rabbit | RABBIT |
| Ground | Wolf, WhiteFerret, MountainGoat, Otter | 미매핑 |
| Special | DreamFox, StarCat, MirrorDeer, DreamMole, ClockMoth | 미매핑 |
| Air | Swallow, Owl, FlyingSquirrel, Hummingbird, Bat | 미매핑 |

RABBIT의 identity 연결은 구현 완료나 멀티플레이 사용권의 증거가 아니다. 현재 Rabbit asset의 placeholder/능력 구현 상태도 기존 그대로다. 완전한 실제 Ground/Special/Air 편성이 없어 현 운영 UI에서는 입장을 열 수 없다. 서버 기본 `config.empty.json` 역시 종·모드·계정이 비어 있다.

## DEV 연결 설정

`Tools/ANIMOLNet02/Client/connection.empty.json`은 값이 없는 템플릿이다. 프로젝트에 실제 자격 증명이나 테스트 종 매핑을 내장하지 않았다. Unity의 NET02 DEV Enable 메뉴로 `ANIMOL_NET_UI_DEV`를 켜고, Editor 또는 Development 빌드에서만 별도 로컬 파일을 읽는다. 기본 Windows 경로는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/ANIMOL/ANIMOLNet02/connection.json`이다.

| 설정 | 의미 |
|---|---|
| ServerUrl | NET02 HTTP endpoint. 휴대폰에서는 PC의 LAN 주소 사용. URL 내 자격 증명·query·fragment 금지 |
| AccountId / DevAccessKey | 서버 DEV config의 명시적 테스트 계정. Google/운영 로그인과 별개 |
| NormalModeId / RankedModeId | 확인된 서로 다른 서버 ModeId |
| CustomModeId | 만들기 화면에서 선택할 확인된 ModeId. 커스텀을 새로운 경기 모드로 합성하지 않음 |
| Modes | 각 원소에 ModeId, GrowthPolicy, PublicIntent, CreateIntent, JoinIntent. intent 값은 서버가 소유한 opaque ID와 정확히 일치 |

Modes가 비었거나 필요한 intent·정책이 없으면 해당 경로를 차단한다. 성장 정책 문자열은 프로젝트 라우팅 계약이며, 서버에 없는 성장 수치·최대 레벨·능력 설명을 만들지 않는다. 사용자에게 일반/랭크 정책은 구분하여 표시하되 성장 필드는 `--`를 유지한다.

로컬 설정은 저장소 외부에 둔다. 저장소 안에서 준비하는 `Client/connection.local*.json`은 ignore 대상이다. PlayMode fixture 테스트는 실제 로컬 설정이 없을 때만 실행되어 사용자 DEV 계정에 연결하지 않는다.

## 기존 UI 연결

`NetUiProjectEntry`는 설정이 있을 때 Bootstrap의 전용 빈 서비스 GameObject에 기존 `NetUiDevCoordinator`를 한 번 설치한다. Lobby 직접 실행도 같은 singleton을 사용한다. UI는 기존 navigation에 조회 바인딩만 추가하며 Canvas, EventSystem, 그림, 방 프리팹을 만들거나 교체하지 않는다.

1. 일반/랭크/만들기 화면의 동물 버튼은 선택된 실제 모드와 intent의 context를 읽는다.
2. 완전한 context는 기존 `AnimalMultiplayerPhase3Host.Open(request)`로 전달한다. 미구성·실패이면 일반/랭크/만들기는 `OpenUnconfigured()`로 열람만 제공한다.
3. 코드 입력은 기존 단일 TMP 필드를 사용한다. 서버 catalog의 코드 형식 검증과 실제 lookup을 거쳐야 한다. 형식이 맞거나 텍스트를 붙여넣었다는 이유로 조회 성공을 표시하지 않는다.
4. 조회 성공 시 실제 코드·인원·모드·정책과 편성 사용 권한 확인 상태를 표시한다. API에 없는 방장 정보는 `--`다. 조회는 입장 승인이 아니다.
5. 조회가 완료되어야 ‘동물 선택’ 버튼을 활성화한다. 버튼을 누를 때 lookup/context를 다시 읽어 변경된 방 정책·정원·권한을 재확인한다.
6. 코드 변경은 기존 결과를 무효화한다. 화면 이동·코드 변경 후 늦게 끝난 조회는 화면을 열거나 참가 버튼을 활성화하지 않는다. 읽기 중에도 뒤로가기는 가능하다.
7. Pending/Unknown/미탈퇴 Accepted가 있으면 기존 coordinator/journal의 차단을 따른다. 계정·endpoint를 바꾸거나 journal을 지우지 않는다.

`MissingMultiplayerView`에는 NET02 타입 참조 없이 선택적 조회 callback만 추가했다. define OFF 또는 로컬 설정 없음에서는 기존 열람과 서비스 미연결 상태가 유지된다. 협동 DEV 경로는 그대로다.

TASK 03의 `INetUiContextSource`에는 입장·Ready 메서드가 없다. 이 작업은 세션/catalog/lookup/context만 호출한다. 기존 Phase3 운영 어댑터는 아직 snapshot 조회와 입장 제출을 Unavailable로 반환한다. 따라서 완전한 fixture context로 화면을 열었다는 검증은 입장 성공 검증이 아니다. TASK 04에서 이 어댑터와 중앙 제출을 연결해야 한다.

## 검증

- DEV ON EditMode: 71/71 통과. 신규 context 테스트 22, NET02 불변식 18, 기존 Phase3 adapter 25·asset 3, CommercialPolishPolicyTests 3.
- DEV ON PlayMode: 9/9 통과. 신규 실제 Lobby 바인딩 fixture 3개와 기존 Missing Multiplayer 비캡처 흐름 6개.
- 서버: 임시 localhost HTTP/DB fixture 테스트 25/25 통과. 패키지 정적 검사 통과.
- DEV OFF: EditMode 31/31, PlayMode 6/6 통과. define과 기존 ProjectSettings 원본 바이트 복원 완료.
- 패키지 정적 검사 통과. 작업 전 변경 파일 1,411개의 해시가 그대로임을 확인했다. 상세 개수와 XML/서버 로그 해시는 [validation.json](NET02Task03/validation.json)에 기록했다.

기존 Missing UI utility/store의 missing script 경고와 Python 3.14 HTTPError ResourceWarning은 남아 있으며 이번 테스트 실패는 없다. 캡처 테스트와 기기 검증은 실행하지 않았다.

신규 fixture는 메모리 안에서만 TEST_* ID와 명시적 매핑을 사용한다. 기존 IdMap/Catalog asset은 수정하지 않았다. 실제 IdMap으로 같은 fixture를 변환하면 실패하는 테스트를 별도로 통과했다. 실제 계정/모드/3종이 없는 상태의 운영 차단과 fixture 성공을 구분한다.

검증 범위 밖: 실제 운영 데이터로 UI↔HTTP 완전 연결, 실제 입장 승인·SC06 소비, Android 기기·APK 및 LAN 다중 기기. 후속 TASK 04~06과 데이터 공급이 필요하다. 새 연출/입력 차단 효과는 추가하지 않았다.
