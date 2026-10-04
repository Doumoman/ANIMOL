# NET02 TASK 05 — SC06 서버 방 상태·복구·복귀

2026-10-05. 기존 SC06 Missing Multiplayer 뷰에 실제 NET02 RoomState를 바인딩했다. 소비자는 `NetUiRoomController` 하나이며 기존 coordinator의 `TryDeliverAccepted`가 UI 바인딩/이동 성공 뒤 receipt 소비를 저장한다. `MultiplayerAccepted`는 표시 카운터만 기록한다.

- 서버 catalog와 저장된 실제 종 매핑을 검증하고 방을 조회한 뒤 입장한다. 소비된 receipt로 다시 Lobby를 열면 거래를 재실행하지 않고 서버 방 상태와 화면만 복원한다.
- 기존 4슬롯에 실제 계정·본인/방장·연결·Ready·3종 편성을 표시한다. 서버 참가자가 없는 슬롯은 빈 자리다. 코드 복사는 조회된 실제 RoomCode만 사용한다. 초대는 미제공이므로 비활성이다.
- coordinator의 기존 polling 루프만 사용한다. 숨김/파괴 시 중지·구독 해제하며 이전 세대 응답을 버린다. 조회 실패 시 Ready/시작/복사를 차단하고, 실제 조회 성공 후 복원한다.
- Ready는 원하는 bool을 한 번 보내고 응답으로 렌더링한다. 시작은 서버 CanStart 권한을 따라 한 번 요청한다. Started는 ‘시작 승인 · 경기 연결 대기’를 표시하며 Gameplay로 이동하지 않는다. Aborted도 별도 표시한다.
- 나가기는 저장된 승인 RoomId로 확인한다. 응답 유실이나 상태 조회 404만으로 journal을 지우지 않는다. 재시도에서 Accepted/ALREADY_LEFT를 확인한 뒤 원래 진입 화면으로 복귀한다. 재실행 시 기본 복귀 화면은 경쟁 허브다.
- UI 전달 전 서버 승인 상태에서도 명시적 나가기를 허용한다. 이는 receipt 소비 성공으로 기록하지 않으며, 이미 탈퇴한 receipt가 다시 전달되지 않게 했다.
- 기존 navigation의 선택적 BackOverride를 이용해 Room의 버튼/OS Back을 같은 탈퇴 경로로 연결한다. Phase3 Pending/Unknown 중 OS Back도 동일 요청 보존을 따른다. 사용자에게 새 Canvas/EventSystem이나 프리팹을 추가하지 않았다.
- 기존 협동 DEV 미리보기와 실제 서버 방 렌더링을 구분했다. 협동 정책·서비스 플래그·계정 Linked 상태·배경·게임 판정은 변경하지 않았다.

## 검증

격리된 localhost HTTP/SQLite와 TEST_* 메모리 매핑으로 기존 Lobby/Phase3/SC06을 구동했다. 운영 IdMap/Catalog를 바꾸지 않았다.

- 실제 UI: 확인→중앙 소비 1회→SC06 1회, 4명과 동일 3종 허용/5번째 거절, 코드 복사, Ready, 방장 시작, 나가기/복귀.
- 복구: 소비된 승인 재표시 시 제출·소비 증가 0, 나가기 응답 유실→404→동일 RoomId 탈퇴 재확인, polling 실패/복귀, 숨김 시 polling 중지.
- 불확실한 입장 중 Back 차단 후 동일 ActionId 결과 조회로 SC06 진입.
- 기존 Task 04 HTTP·거래 회귀, Missing Multiplayer 비캡처 흐름, CommercialPolishPolicyTests 포함.

최종 횟수와 로그 해시: [validation.json](NET02Task05/validation.json). TASK 06에서 DEV OFF/빌드/그래픽 검증을 이어서 수행한다. 실제 운영 종 3종·모드·계정 자료와 휴대폰은 아직 제공되지 않아 운영 데이터·기기 실검증과 fixture 통과를 구분한다.

`UiNavigationService.cs`의 기존 미커밋 변경을 보존하고 이번 BackOverride 추가분만 커밋한다. 나머지 기존 미커밋 파일도 보존한다.
