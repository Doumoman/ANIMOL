# NET02 TASK 04 — Phase3 어댑터와 중앙 입장 관리 연결

기준일: 2026-10-05. 기존 Phase3의 실제 어댑터에 DEV 권한 조회·입장 제출·결과 재확인을 연결했다. 서버는 기존 NET02 HTTP/SQLite 구현을 사용한다. 별도 입장 관리자나 create/join/Ready 호출을 추가하지 않았다.

## 연결 경로

`NetUiProjectEntry` → 기존 `AnimalMultiplayerProjectAdapter`의 DEV 전용 포트 → `NetUiPhase3Authority` → 기존 `NetUiDevCoordinator` → HTTP 서버.

- 기존 어댑터 타입·프리팹을 유지한다. `ANIMOL_NET_UI_DEV`가 없으면 DEV 포트도 컴파일되지 않고 기존 Unavailable 동작으로 돌아간다. 네트워크 동작은 Editor/Development에 한정한다.
- TASK 03이 검증한 context를 복사해 바인딩한다. Phase3가 열리거나 사용자가 새로고침하면 서버 catalog/context를 다시 읽는다. 코드 참가면 lookup도 다시 확인한다. 모드·intent·정책·대표 역할·허용 풀·초기 편성이 기존 화면 문맥과 달라졌다면 허브에서 다시 열도록 차단한다.
- 실제 snapshot revision과 종별 Implemented/HasContextPermission/CanUseInContext를 전달한다. 성장 레벨·재화·능력 설명은 기존 미설정 값으로 유지한다. `Unlocked`만으로 참가를 허용하지 않는다.
- 확인 시 기존 Presenter의 ActionId, ContextId=ModeId, snapshot/policy revision, intent와 편성을 검증한다. 아트 ID를 명시적 실제 종 ID로 변환하고 coordinator가 보유한 context와 대조한 뒤 한 번 제출한다. 방 코드는 조회로 확정한 코드이며 RoomId와 ModeId를 혼동하지 않는다.
- coordinator는 계정·endpoint·ActionId·전체 payload를 journal에 저장한 다음 HTTP 요청을 보낸다. 어댑터가 새 ActionId를 만들거나 별도 HTTP client를 소유하지 않는다.
- 예외·응답 유실·receipt 불일치는 Unknown이다. Phase3의 기존 ‘결과 다시 확인’은 저장된 동일 요청의 `/v1/room/result`만 호출한다. 자동 재제출하지 않는다. `ACTION_NOT_FOUND`도 새 요청을 만들 수 있는 허가로 취급하지 않는다.
- Accepted는 서버의 ActionId/revision/token/context/policy/intent/3종을 검증한 다음 기존 `AnimalUiCommitResult`로 변환한다. 서버가 주지 않은 receipt 필드를 UI 요청에서 채우지 않는다.
- Rejected/Unavailable 사유를 표시하고 수동 최신 조회와 새 확인을 요구한다. 이전 ActionId를 바꾼 내용으로 재사용하지 않는다.

## 복구와 단일 소비

Bootstrap/Lobby 설치 시 미해결 journal이 있으면 로컬 설정이 없거나 변경되어도 저장된 계정·endpoint·세션을 복구한다. 허브/만들기 화면의 기존 동물 버튼은 ‘입장 결과 확인’으로 표시되고, 저장된 전체 요청으로만 결과를 확인한다. Pending/Unknown/미탈퇴 Accepted가 있는 동안 새 편성 진입과 계정·endpoint 변경은 차단된다.

`ConfigureConnection`은 새 scope 검증을 통과하기 전에 현재 계정·endpoint를 변경하지 않도록 수정했다. 복구 이후 `BindRecoveryProjectMap`은 저장된 3종의 실제 ID 왕복을 확인하고 ID 매핑만 재연결한다. journal·세션·원본 payload는 교체하지 않는다.

최종 SC06 소비자는 **coordinator의 `TryDeliverAccepted` 경로 한 곳**으로 정했다. 기존 `MultiplayerAccepted` 이벤트 구독자는 표시 횟수만 기록하고 이동하지 않는다. TASK 05에서 서버 방 상태 조회·SC06 바인딩이 성공한 뒤 소비를 확정해야 한다. 이번 작업에서는 승인 메시지와 중앙 receipt까지만 연결했고 실제 SC06 이동·Ready·시작·나가기는 연결하지 않았다.

coordinator의 프로세스 내 진단 카운터는 SubmissionCount, ResultQueryCount, ReceiptNotificationCount, AcceptedPresentationCount, ReceiptConsumeCount다. 테스트 증거에는 횟수와 상태만 기록하며 token·세션·자격 증명을 출력하지 않는다. 이 카운터는 영속 거래 원장을 대체하지 않는다.

## 검증과 재실행

DEV define ON 후 다음 명령으로 실제 HTTP 통합 검증을 재실행한다.

```powershell
python Tools/ANIMOLNet02/Tools/run_unity_admission_tests.py
```

러너는 임시 디렉터리의 fixture config/SQLite, 임의 localhost 포트, 전용 journal slot을 사용하며 종료 시 서버와 임시 DB를 정리한다. TEST_* 종·모드·계정만 사용한다. 기존 그림은 테스트 메모리에서 복제해 TEST 아트 ID와 대응시킨다. 운영 Catalog/IdMap 및 사용자 로컬 설정은 수정하지 않는다. 로컬 DEV 설정이 존재하면 fixture 테스트는 실패하여 사용자 계정 연결을 방지한다.

| 검증 | 내용 |
|---|---|
| 신규 실제 HTTP PlayMode 5개 | 확인 취소 0회/확정 1회/연타 추가 0회, 정확한 서버 receipt→3종, 중앙 소비 1회, 계정·endpoint 변경 거절 |
| Unknown 재확인 | 서버 SQLite에 반영된 후 응답을 빈 envelope로 바꾸는 고장 주입. 동일 payload로 결과 조회 1회, 재제출 0회 |
| 잘못된 receipt | 전송 응답의 intent만 변조. Unknown 유지 후 DB의 원래 receipt 조회로 복구 |
| 실제 정책 변경 | 임시 서버 config 정책 변경으로 실제 거절. 자동 재확인 없음, 수동 최신 조회 후 다른 ActionId 사용 |
| 복구 | coordinator 파괴와 Lobby 재설치, 로컬 설정 없이 persisted scope/요청 복원, 허브 결과 조회로 승인 복구, 새 제출 0회 |
| 기존 회귀 | NET02 불변식·context, Phase3 adapter·asset·비캡처 거래, Missing Multiplayer 흐름, CommercialPolishPolicyTests |

DEV ON EditMode 71/71, PlayMode 19/19, DEV OFF EditMode 31/31, PlayMode 6/6 및 서버 테스트 25/25가 통과했다. 패키지 정적 검사도 통과했다. 최종 상세 결과·XML 해시·토큰 없는 카운터는 [validation.json](NET02Task04/validation.json)에 기록했다. 초기 PlayMode 실행의 fixture 누적 카운터 초기화 누락과 TASK 04의 추가 snapshot 조회 횟수 기대치를 수정한 후 재검증했다.

기존 utility/store의 missing script 경고 및 Python 3.14 HTTPError ResourceWarning은 별도 기존 사항이다. 캡처 테스트는 배치 모드의 프레임 대기 때문에 선택하지 않았다. 실제 프로세스 강제 종료·기기 검증은 수행하지 않았으며, 이번 복구 검증은 동일 PlayerPrefs의 coordinator 파괴/Lobby 재설치다.

## 남은 범위

- 실제 종 매핑은 여전히 RABBIT 하나뿐이다. Ground/Special/Air 운영 3종, 실제 모드·계정·권한을 임의로 보충하지 않아 기본 운영 UI의 입장 차단은 유지된다.
- TASK 05: 중앙 receipt를 실제 SC06에 소비, 방 상태 렌더링·polling·Ready·시작·나가기·복귀 연결.
- TASK 06: Windows/Android DEV 빌드·기기/LAN·전체 흐름 검증. 실제 경기, Google/운영 인증, 재화·구매·광고 G-04 완료를 의미하지 않는다.
- 새 Canvas/EventSystem, 배경·토끼·프리팹 변경, 연출·입력 차단은 추가하지 않았다. 기존 미커밋 파일 1,411개의 해시를 보존했고 테스트용 define/ProjectSettings를 원본 바이트로 복원했다.
