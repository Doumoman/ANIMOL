# 실제 데이터 연결 및 DEV 범위

## 기본 설정은 차단 상태

`Server/config.empty.json`에는 입장 가능한 모드/종/계정이 없다. 서버가 뜨는 것과 참가 승인은 별개다. UI를 동작시키기 위해 임의 ModeId, 종 ID, 해금·사용 가능 상태를 채우지 않는다. `Server/API.md`의 설정 구조에 따라 확인된 소유자 데이터를 제공한다.

## 필요한 소유자와 데이터

| 소유자 | 최소 공급 |
|---|---|
| 종/능력 카탈로그 | 실제 AnimalId, stableArtId, Ground/Special/Air 역할, 실제 구현 여부 |
| DEV 계정 권한 | 계정 ID, 표시 이름, 테스트 자격 증명, snapshot revision, 명시적 허용 동물·초기 3종 편성 |
| 모드/진입 의도 | 실제 ModeId, 이름, PolicyRevision, 허용 풀, 커스텀 Create/Join 또는 공개 배정에 해당하는 opaque EntryIntent |
| 방 정책 | 경쟁 최대4, 해당 모드 시작 최소 인원, 코드 규칙·허용 옵션·초대 정책 |
| 클라이언트 IdMap | 실제 종 ID→stableArtId 역매핑, 서버 승인 편성에 대한 역할·ID 왕복 |

NET02 서버는 최소 인원·진입 의도 분류를 DEV config로 받는다. 경쟁 최대 인원은 4로 제한한다. 커스텀 시작 최소2 같은 DEV 값은 제품 정책 소유자가 승인하기 전 운영 규칙으로 승격하지 않는다.

프로토타입 코드는 서버 생성 6자리 대문자/숫자다. 최신 인수인계에서 운영 코드 규칙은 미확정이다. 이 형식을 기존 코드 입력 화면의 운영 정책으로 강제하지 않고 DEV 룰 안내에서만 사용한다.

## 현재 실제 매핑

| 역할 | 안정 아트 ID | 실제 종 ID |
|---|---|---|
| Ground | Rabbit | RABBIT |
| Ground | Wolf, WhiteFerret, MountainGoat, Otter | 미매핑 |
| Special | DreamFox, StarCat, MirrorDeer, DreamMole, ClockMoth | 미매핑 |
| Air | Swallow, Owl, FlyingSquirrel, Hummingbird, Bat | 미매핑 |

PNG 또는 캠페인 해금 여부만으로 사용권을 발급하지 않는다. DEV_GROUND/DEV_GLIDER/DEV_SPECIAL도 운영 종 ID로 사용하지 않는다. 테스트 fixture의 TEST_* ID는 서버 검증 안에서만 유효하다.

## 단일 입장 흐름

1. 허브가 실제 mode/context 및 opaque intent를 조회한다. 코드 참가면 먼저 lookup하고 서버가 반환한 방 정책/문맥을 사용한다.
2. 기존 Phase3 `Open(request)`에 문맥과 편성 복사본을 전달한다. `OpenUnconfigured()`는 열람 경로로 유지한다.
3. 실제 권한 snapshot을 읽고 선택된 Ground/Special/Air 각1종의 구현·권한·허용 풀을 검증한다.
4. 확인 취소는 HTTP 입장 요청0회. 확인 확정 시 ActionId와 전체 불변 요청을 journal에 먼저 기록하고 제출1회.
5. 서버가 ActionId/계정/revision/intent/3종과 방 capacity를 재검증해 receipt를 발급한다. 클라이언트가 요청값을 복사해 Accepted로 만들지 않는다.
6. 중앙에서 정확한 receipt를 검증한 후 SC06에 실제 방 상태를 전달한다. 별도 create/join API를 다시 호출하지 않는다.
7. 타임아웃은 Unknown이다. 같은 ActionId·전체 요청으로 결과 조회한다. 서버 DB에 없는 결과라면 같은 불변 요청의 재전송 여부를 명시적으로 확인하며 새로운 ActionId를 발급하지 않는다.

## 계정·세션·연결 복구

DEV 세션은 테스트 계정 자격 증명으로 발급한 bearer다. Google/운영 AccountAccessSnapshot을 Linked로 바꾸지 않는다. 세션/미확정 요청/receipt는 동일 endpoint+DEV 계정에 귀속되어야 한다. 다른 서버나 계정으로 바꿀 때 기존 journal을 무시한 채 입장을 다시 실행하지 않는다.

클라이언트는 HTTP 응답을 받을 수 없을 때 보존된 승인과 현재 서버 방 snapshot으로 기존 SC06을 재바인딩한다. receipt 처리 표시를 저장한 뒤에도 앱 재시작의 방 화면 복원은 별도 상태 재바인딩이며 새 입장 승인/거래가 아니다. 네비게이션과 PlayerPrefs 저장은 하나의 원자적 트랜잭션이 아니므로 중간 종료는 실제 통합 테스트에서 검증한다.

NET02 journal은 중앙 멀티 입장용 DEV 구현이다. 동물 강화/구매/광고의 G-04 영속 거래 시스템까지 완료했다고 보고하지 않는다.
