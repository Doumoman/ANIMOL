# Unity 클라이언트 API와 기존 UI 바인딩

이 문서는 제공 소스의 연결 지점이다. 기존 프로젝트의 `AnimalUi*` 타입을 확인한 뒤 변환 코드를 작성해야 한다. 패키지 자체는 기존 어댑터를 상속/수정하거나 버튼을 자동 연결하지 않는다.

## 컴파일·서비스 소유권

- 런타임 namespace는 `Animol.NetUiDev`, assembly는 `ANIMOL.NetUiDev`다. 런타임 asmdef는 `ANIMOL_NET_UI_DEV`를 요구한다.
- 프로젝트 쪽의 모든 NET02 타입 참조도 같은 `#if ANIMOL_NET_UI_DEV`로 격리한다. define 없는 출시 빌드에서는 기존 disconnected 경계를 유지하고 컴파일을 확인한다.
- 중앙 `NetUiDevCoordinator`는 기존 Bootstrap 담당자가 전용 서비스 GameObject에 한 번만 부착한다. Canvas/화면/기존 Bootstrap 루트에 부착하여 전체 UI를 DontDestroyOnLoad 처리하지 않는다. 이 오브젝트는 UI·EventSystem을 만들지 않는다.
- Coordinator는 Unity 메인 스레드에서 호출한다. 기본 URL과 계정은 비어 있어 자동 접속하지 않는다.
- 기존 설정 화면에 별도 DEV 연결 섹션을 바인딩하거나 빌드 전 인스펙터 설정을 제공한다. Android에서 수정 가능한 URL·DEV 계정·자격 증명 입력은 기존 Canvas/SafeArea/TMP를 재사용한다. 출시 설정 메뉴에는 노출하지 않는다. 비밀키를 Resources에 넣지 않는다.
- 기존 UI/base에도 pending 관리가 있다면 중앙과 책임을 연결한다. 두 요청 주체가 동시에 제출하거나 Accepted를 각각 소비하지 않는다.
- 앱 시작에서 journal의 잠긴 active scope를 먼저 확인하고 같은 URL/계정을 복원한다. 다른 계정으로 초기화해 Unknown을 우회하지 않는다. 제공 journal의 active scope 조회 API를 정본으로 확인한다.

## 공개 API

| API | 기존 프로젝트 연결 |
|---|---|
| ConfigureConnection(url, accountId, projectMap) | 별도 DEV 설정. unresolved 요청/현재 방 동안 전환 금지 |
| ConnectAsync(devAccessKey) | DEV 세션 연결 및 카탈로그 읽기. 운영 Google 로그인과 별개 |
| GetCatalogAsync() | 현재 모드/정책/진입 의도/종 매핑 조회 |
| LookupRoomAsync(input) | 기존 코드 입력 확인. 조회 응답은 참가 승인 아님 |
| ReadContextAsync(modeId, entryIntent, roomCode) | Phase3 Open 문맥과 권한 snapshot 공급 |
| ToActualLoadout / ToArtLoadout | 명시적 IdMap의 요청·승인 역매핑 |
| PrepareEntry(actionId, loadout, options) | 기존 확인 모달의 불변 요청 생성 |
| SubmitEntryAsync(request) | 확정1회. 서버입장 요청 이전 journal 저장 |
| ResolvePendingAsync() | Pending/Unknown의 같은 전체 요청으로 서버 결과 조회 |
| ResubmitUnknownAsync() | ACTION_NOT_FOUND 결과 확인 뒤 같은 journal payload만 재전송 |
| PollRoomAsync() | 실제 방 상태를 조회·검증하고 CurrentRoom에 바인딩 |
| TryDeliverAccepted(consumer) | 기존 SC06 전달이 성공하면 receipt 처리 확인 저장 |
| SetReadyAsync(bool), StartRoomAsync(), LeaveRoomAsync() | 서버 capability에 따른 실제 상태 변경 |
| BeginRoomPolling / StopRoomPolling | 기존 방 화면 수명에 단일 상태 조회 루프 연결 |
| ReplayCurrentState() | 새 UI 구독자에게 보존한 상태를 다시 제공 |

각 API의 실제 매개변수와 예외는 제공 C# 소스를 정본으로 확인한다. 네트워크 실패는 실제 오류 상태를 표시하며 토큰 원문은 UI/로그에 노출하지 않는다.

## 이벤트·receipt 소비

`OperationStateChanged`는 처리/Unknown/최종 거절과 연결 실패 안내에, `RoomStateChanged`는 실제 SC06 표시 갱신에 사용한다. `AcceptedReceiptReady`는 미소비 승인 존재 알림이다. 기존 UI 담당자는 다음 순서를 수행한다.

1. `PollRoomAsync()`로 현재 room과 승인 RoomId/ModeId/PolicyRevision/본인 편성을 검증한다.
2. `TryDeliverAccepted`에 기존 SC06 바인딩·네비게이션 소비자를 제공한다.
3. 소비자는 실제 RoomId에 대해 중복 바인딩에 안전하도록 구현하고 성공 뒤에만 true를 반환한다. 요청만으로 참가자/Ready/대기방을 만들지 않는다.
4. 기존 `MultiplayerAccepted`를 같은 최종 소비자에 연결하고 별도 create/join을 추가하지 않는다.
5. 승인 소비와 PlayerPrefs 저장·네비게이션은 하나의 원자적 트랜잭션이 아니다. 중간 종료에서 현재 서버 방 상태로 재바인딩하는 경로를 검증한다.

`ReceiptConsumed=true`인 재시작은 accepted 이벤트를 다시 발생시키는 입장 처리가 아니다. `HasActiveRoomReceipt=true`이면 PollRoomAsync 후 현재 SC06 상태를 복원한다. `RequiresReceiptDelivery=true`일 때만 TryDeliverAccepted로 최초 전달을 완료한다. 나가기 승인까지 신규 입장·계정/endpoint 전환을 막는다. 승인된 나가기 이후 RequestState는 Left이며 방 복원 조회를 실행하지 않는다.

## 응답 유실 복구

원래 POST가 서버까지 도달했으면 ResolvePendingAsync가 저장된 최종 결과를 반환한다. 도달하지 않았으면 Unknown/ACTION_NOT_FOUND다. 이때 사용자에게 같은 요청을 다시 전송하는 DEV 복구 동작을 제공하고 ResubmitUnknownAsync를 호출한다. ActionId/정책/revision/편성/options는 journal 그대로이며 새 확인 내용으로 바꾸지 않는다. 해당 권한/revision이 바뀌었으면 서버의 최종 거절 후 새로운 조회와 확인으로 진행한다.

나가기 응답이 유실되면 저장한 승인 RoomId로 동일 `LeaveRoomAsync()`를 재호출한다. 앱 재시작 후 방 조회가 NOT_IN_ROOM이어도 이 복구 동작은 가능하며 서버의 Accepted/ALREADY_LEFT 응답으로만 나가기를 확정한다. 방 조회404나 로컬 표시 초기화만으로 계정·새 입장을 풀지 않는다. 서버 Started에서 나가면 남은 방은 Aborted이며 새 경기 재사용 기능은 없다.

## 제공 데이터의 한계

HTTP DTO는 입장 권한·3역할 편성과 대기방 상태를 제공한다. 액티브/패시브 설명·성장 레벨·실제 능력 실행·랭크 최대 성장 적용을 공급하지 않는다. 기존 Phase3가 이 공급을 필수로 요구하면 실제 제공자가 연결될 때까지 입장을 차단하고 해당 필드를 합성하지 않는다.

Server mode의 AllowedAnimalIds는 명시적 배열을 요구한다. 기존 UI 계약의 null=추가 클라이언트 제한 없음 / 빈 배열=허용 없음 의미는 변환 과정에서 유지한다. 다른 플레이어와 동일 종을 사용하는 것을 금지하지 않는다.

## DEV 빌드 메뉴

`ANIMOL > NET02 Existing UI DEV > 0 Enable DEV Define` 후 재컴파일을 기다린다. `1 Build Windows Development`, `2 Build Android Development APK`가 기존 enabled 씬의 Bootstrap/Lobby를 사용한다. `9 Disable DEV Define`은 DEV 사용 해제다. 새 NetDev 씬·새 독립 UI를 만들지 않는다.

산출 경로: `Builds/ANIMOLNet02UiDev/Windows/ANIMOLNet02Dev.exe`, `Builds/ANIMOLNet02UiDev/Android/ANIMOLNet02Dev.apk`. 설정 복원 결과를 NET02_RESULT.md에 기록한다. SDK/NDK/OpenJDK·ARM64 IL2CPP 설치가 필요하다.

Windows 다중 실행 시 `-animolNet02Slot PC01`처럼 서로 다른 슬롯을 지정한다. 기본 슬롯은 같은 PC의 같은 앱 저장을 공유하므로 3인 테스트에서 그대로 사용하지 않는다. `Tools/StartWindowsClients.ps1`이 PC01~PC03을 지정한다. 각 창은 서로 다른 DEV 계정으로 연결한다. Android 각 기기는 자체 저장 슬롯을 사용한다.
