# NET02 TASK 02 — 경쟁 최대 4인 정책 이관

2026-10-05. 범위는 TASK 02이며 실제 HTTP와 기존 UI의 연결은 TASK 03~05에 남아 있다.

## 변경

- `MultiplayerModeRules.CompetitiveParticipants`를 `(4,8)`에서 `(4,4)`로 변경했다. 기존 공개 경쟁의 최소 4명을 유지한다. 커스텀 최소 인원은 서버 모드 정책이 소유하며 이 Core 값을 커스텀 입장에 적용하거나 DEV 최소 2명을 운영 규칙으로 확정하지 않는다.
- `MultiplayerUiPresenter`는 경쟁의 인원 토글을 비활성화하고 직접 이벤트를 호출해도 인원을 변경하지 않는다. 일반/랭크 모두 4명, 각 참가자의 기존 3역할 편성과 성장 표시 구분을 유지한다. 참가자 label을 실제 인원에 맞게 표시하고 구형 프리팹에 남아 있는 초과 행도 숨긴다.
- 협동의 `(2,4)`, 2↔4명 토글, 별도 2슬롯·동일 동물 중복 금지 정책을 유지했다. 경쟁 참가자 사이의 같은 동물 선택 허용도 유지했다.
- `UiBuildPipeline.M6Multiplayer`의 생성/검사/결과 문자열을 4인으로 맞췄다. `OpenM6CompetitiveRoomForCapture(bool ranked, bool disconnected)`에서 폐기된 8인 인수를 제거하고 내부 호출자도 변경했다. 협동 캡처 인수는 그대로다.
- 전용 메뉴 `ANIMOL/NET02 Existing UI DEV/Apply TASK 02 Room Capacity` 및 `UiBuildPipeline.ApplyNet02RoomCapacity()`를 추가했다. Unity의 PrefabUtility로 SC06만 수정하며 전체 UI 빌더를 실행하지 않는다. 기존 포트레이트 배치·폰트·버튼·다른 화면을 재생성하지 않는다.
- 실제 `SC06_MatchRoom.prefab`에서 `RoomParticipant_05`~`08`의 4개 GameObject와 관련 컴포넌트를 제거했다. 나머지 4행의 레이아웃은 보존했다. 경쟁 인원 버튼은 `경쟁 4명 고정`으로 표시하며 런타임에서 협동일 때만 활성화한다.
- 새 Missing UI의 Room은 이미 4슬롯이므로 변경하지 않았다. 서버도 이미 최대 4명을 검증하므로 서버 규칙/config/fixture를 수정하지 않았다. 운영 서비스 플래그, DEV define, 실제 종 매핑도 변경하지 않았다.
- `MissingMultiplayerEntry`가 SC06 진입 시 기존 참가자 행을 모두 다시 켜던 문제를 수정했다. 실제 참가자 수로 행을 복원해 협동 2명일 때 4행이 표시되는 문제를 방지한다.

## 테스트 보강

`M6MultiplayerContractTests`는 공개 경쟁 4명 허용, 3/5/8명 거절을 검사한다. 잘못된 참가자 수 요청이 기존 4명 배열을 훼손하지 않는지, 선택 서비스에서도 범위 밖 인원을 거절하는지 확인한다. 일반/랭크 성장 구분, 경쟁 4명 유지, 협동 2↔4명, 프리팹의 정확한 4개 행도 검사한다.

`M6MultiplayerUiFlowTests`는 레거시 DEV 대기방의 일반/랭크 4명 표시와 경쟁 토글 차단, 협동 토글 활성화 및 기존 HUD 경로를 검증한다. 운영 대기방 입장 성공을 합성하는 테스트가 아니다.

## 검증 결과

Unity 6000.3.8f1에서 컴파일 완료, 서버와 아래 선택 검사가 통과했다. [검증 요약](NET02Task02/validation.json)에 XML 해시와 결과를 보관하며 원본 로그는 저장소의 `Logs/NET02Task02/`에 있다.

| 검사 | 결과 |
|---|---|
| EditMode | 16/16: M6 멀티 계약 13 + CommercialPolishPolicyTests 3 |
| PlayMode 최종 | 5/5: M6 일반/랭크/협동 DEV 흐름 3 + Missing UI 4슬롯 차단/협동 분리 2 |
| Python HTTP/SQLite | 25/25. 동시 참가 검사에서 방장 외 5개 동시 요청 중 3개만 승인, 2개 ROOM_FULL, 실제 참가자 배열 4명. 같은 동물 편성 허용 유지 |
| 소스·산출물 | 적용 코드의 8인 생성/표시/토글 기준 제거, SC06 참가자 행 정확히 4개, 이번 변경의 staged diff 검사 |

첫 PlayMode 실행에서는 현재 로비에서 사라진 옛 CompetitiveButton/CoopButton을 찾던 M6 검사 3개가 실패했다. DEV 검사 진입을 현재 허브 navigation에 맞추고 설치 완료를 기다리도록 수정했다. 다음 실행에서는 실제 화면에 켜진 행을 검사하면서 협동 2명인데 4행이 켜지는 문제 1개를 발견했다. MissingMultiplayerEntry를 수정한 최종 실행에서 5개 모두 통과했다. 초기 실패를 통과 수에 합산하지 않는다.

신규 연출은 추가하지 않았다. 기존 utility/store의 missing script 경고와 Python HTTPError 정리 ResourceWarning은 별개로 남아 있다. 실기기/화면 캡처/실제 UI HTTP 입장 검사는 이번 범위에 포함하지 않는다.

## 기존 작업 보존과 커밋 경계

작업 시작 시 Git 상태와 기존 변경·미추적 1,411개 파일의 해시를 기록했고, 수정 대상의 작업 전 바이트와 HEAD를 따로 보관했다. 기존 변경이 있는 Core/UI/생성기/테스트/SC06은 파일 전체를 stage하지 않고 이번 4인 변경만 index에 반영한다. 특히 SC06의 기존 포트레이트 조정은 남은 객체에서 유지하며, 이번 범위인 초과 행만 제거한다.

수정 대상: `MultiplayerMatchContracts.cs`, `MultiplayerUiPresenter.cs`, `UiBuildPipeline.M6Multiplayer.cs`, `M6MultiplayerContractTests.cs`, `M6MultiplayerUiFlowTests.cs`, `MissingMultiplayerEntry.cs`, `SC06_MatchRoom.prefab`, 적용 문서와 이 결과서/검증 요약.

실제 일반/랭크/커스텀 모드 ID·정책·권한·3종 매핑 공급은 TASK 03의 데이터 차단 항목이다. 다음 작업에서 미설정 화면을 억지로 성공 처리하지 않고 명시적인 공급·매핑 경계를 연결한다.
