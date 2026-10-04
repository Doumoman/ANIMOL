# NET02 제작 검증 결과

기준: 2026-10-05 KST. 현재 프로젝트 소스/Unity가 없는 제작 환경에서 실행한 범위를 기록한다.

| 검사 | 결과 | 범위·증거 |
|---|---|---|
| Python 서버 실제 HTTP | **25/25 PASS** | `Evidence/server_http_tests.txt`. localhost 실제 세션/입장/상태/Ready/시작/나가기 요청 |
| 최대4/동시5번째·중복종 | PASS | 트랜잭션 하에서 동시 참가·capacity·동일종 허용 |
| ActionId·SQLite 복구 | PASS | 불변 payload 충돌, 서버 재개방 후 동일 승인, 계정귀속·Unknown |
| 정책/권한·방장/시작 | PASS | stale revision·권한 회수·Ready/연결/방장·최소인원·시작후 탈퇴 |
| 첫 CLI 실행·나가기 대상 | PASS | 새 DB 디렉터리 자동 생성, 늦은 이전 RoomId leave가 새 방에 영향 없음 |
| 클라우드 실행 래퍼 | **6/6 PASS** | 기본·앞자리0 포트·범위/문자/누락설정 오류. `Evidence/cloud_runner_checks.json` |
| Bash syntax | PASS | Tools/Cloud와 Server의 run-server.sh |
| 소스/패키지 정적 검사 | PASS | 7 C# 파일 구분자, 3 asmdef JSON/참조·define, link.xml, 빈config/fixture 분리. `Evidence/static_package_checks.json` |
| C#·서버 계약 검토 | 수동 검토 | DTO 필드/경로/RoomId 대상·receipt·Unknown·앱 재시작·계정 scope/PC 슬롯 경계를 대조 |
| Unity 컴파일 | **미실행** | Unity6000.3.8f1/실제 프로젝트가 이 환경에 없음 |
| Unity NUnit | **18개 작성 / 미실행** | NetUiInvariantTests. 실행 결과로 계산하지 않음 |
| 기존 Phase3/SC06 연결 | **프로젝트 적용 필요** | 실제 프로젝트 시그니처/버튼/프리팹을 읽어 TASK01~06 실행 필요 |
| Windows·Android APK 빌드 | **미실행** | DEV 빌드 메뉴 소스만 제공. 실제 실행파일/APK 없음 |
| Android/다기기·실계정 | **미검증** | 실제기기·서비스·완전한 3종 운영 매핑이 없음 |
| 클라우드 배포 | **미실행** | 실행/env/systemd 예시만 제공. 자원 생성/배포 안 함 |

## 해석

HTTP 서버 tests는 TEST_* fixture와 임시 SQLite에서 실행했다. 실제15종 구현·운영보유권·Google인증·입장서비스·경기 완료를 검증한 것이 아니다. 기본 빈 config의 차단도 검증했다. NUnit18개는 실제 Unity에서 실행해야 한다.

클라이언트 journal은 DEV PlayerPrefs 저장이다. 서버 SQLite 멱등 입장 결과와 연계하는 소스이며, SC06 화면 이동과 PlayerPrefs 저장의 단일 원자적 처리 보장은 없다. 기존 SC06 소비자는 RoomId에 대해 중복 전달을 처리하고 앱 복귀에 상태를 재바인딩해야 한다.

SDK·데이터·기기 부족으로 검사하지 못한 것은 partial/blocked/미검증으로 보고한다. 서비스 플래그 true·합성 Accepted·단독 서버 테스트로 운영 성공을 만들지 않는다. 운영 캠페인/동물 성장/구매/광고·실경기는 별도 범위다.
