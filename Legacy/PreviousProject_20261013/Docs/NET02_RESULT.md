# NET02 프로젝트 적용 결과 — TASK 01~06

2026-10-05 KST · Unity 6000.3.8f1 · TASK 05 기준 HEAD `dd4c011`. TASK 06의 소스·서버·Unity 검증과 Windows/Android 개발 빌드를 완료했다. 실기기와 운영 데이터가 필요한 항목은 아래처럼 구분한다. 서버 소스 및 산출물 SHA-256, 테스트별 결과와 카운터는 [validation.json](NET02Task06/validation.json)에 있다.

## 결과

| 항목 | 결과 | 증거·사유 |
|---|---|---|
| 기존 허브/커스텀/코드/Phase3 UI 바인딩 | pass, fixture | 기존 Lobby·Phase3·SC06을 사용한 PlayMode 회귀. 별도 사용자 Canvas/EventSystem/방 프리팹 없음 |
| 실제 ModeId/EntryIntent/3종 역매핑 | blocked | 실제 종 연결은 RABBIT뿐이며 Special/Air·모드·계정 공급 부재. TEST_*는 메모리 fixture에만 존재. [매핑 현황](NET02_DATA_BINDING.md) |
| 경쟁 Core 4인·협동 보존 | pass | [TASK 02](NET02_TASK02_RESULT.md) 및 기존 협동 PlayMode 회귀 |
| 빈 config 차단·fixture 분리 | pass | 기본 미설정 차단, 실제 IdMap으로 fixture 변환 거절, DEV OFF 검사. 운영 서비스 플래그/Linked 상태 변경 없음 |
| 서버 실행/HTTP 자동 테스트 | pass, fixture | 임시 localhost HTTP/SQLite 서버 테스트 25/25 |
| Unity 컴파일/의미 있는 회귀 | pass | DEV ON EditMode 73/73·PlayMode 25/25, DEV OFF EditMode 31/31·PlayMode 6/6. CommercialPolishPolicyTests 포함 |
| 취소 0/확정 1/연타 추가 0 | pass, fixture | 실제 HTTP 제출 횟수와 기존 Phase3 확인 UI 검사 |
| ActionId 전체 요청 보존/Unknown 앱 복구 | partial | 응답 유실·중앙 객체 재생성·journal 복구·동일 요청 결과 조회 통과. 실제 OS 프로세스 강제 종료 후 복구는 미검증 |
| 서버 receipt 검증/승인 전 이동 0 | pass, fixture | 서버 발급 receipt·정책·intent·3종 검증, 변조 receipt 보류 후 결과 조회 |
| receipt 1회 처리/SC06 상태 복원 | pass, fixture | 승인→소비 1회→SC06 1회. 화면 재생성 시 추가 제출/소비 없이 복원. [TASK 05](NET02_TASK05_RESULT.md) |
| 4명/동시 5번째 거절/동일종 허용 | pass, fixture | 서버 동시성 테스트 및 Unity UI 1개+실제 HTTP 원격 클라이언트 3개. 휴대폰 4대 결과가 아님 |
| Ready/방장 시작/나가기/방장 이전 | pass, fixture | SC06 버튼→서버→UI, 서버 방장 이전, leave 응답 유실→조회 404→ALREADY_LEFT 확인 |
| 서버 재시작/연결 복귀/계정 전환 차단 | pass, fixture | 서버 재시작 영속 receipt, polling 503 복귀, pending/Unknown/미탈퇴 방 계정·endpoint 전환 차단 |
| Windows DEV 빌드 | pass | 기존 enabled 13씬, Development. D3D11 시작 검사 약 51초, 예외/오류 0. 실제 운영 입장 검증은 아님 |
| Android DEV APK/설정 복원 | pass | ARM64 IL2CPP, 별도 `com.animol.net02dev`, Android Debug 서명, Portrait, INTERNET, debuggable. 빌드별 설정 원본 바이트 복원 확인 |
| phone1+PC3 / phone4 | blocked | `adb devices -l` 연결 기기 0대, 완전한 실제 3종/모드/계정 자료 없음 |
| 실제 SafeArea/키보드/OS Back/복귀 | partial | 1080×1920 한국어·1080×2400 영어/큰 글씨/합성 Safe Area 캡처. 기존 TMP 입력·붙여넣기와 Back 경로 자동 검사. 실제 notch/모바일 키보드/동시 터치/백그라운드/프로세스 재시작 미검증 |
| 기존 아트/폰트/캠페인/BM/미커밋 보존 | pass | 시작 시 기존 변경 1,411개 보존. navigation 기존 변경 위에 TASK 05의 BackOverride 2줄만 추가. 테스트가 생성한 폰트 직렬화와 fixture 기록은 원복 |

## 카운터와 로그

성공한 HTTP 테스트는 모두 TEST_* 계정·종·모드의 격리된 fixture 결과다. 운영 IdMap/Catalog asset이나 사용자 로컬 자격 증명을 바꾸지 않았다. [validation.json](NET02Task06/validation.json)에 시나리오별 제출/결과 조회/승인 표시/receipt 소비/SC06 이동 횟수와 ActionId의 SHA-256 앞 12자리만 기록한다. 비밀번호·토큰 원문은 첨부하지 않는다.

- 정상 SC06 흐름: 제출 1, 승인 표시 1, 소비 1, 이동 1. 소비 후 화면 재생성: 제출 1/소비 1 유지, 화면 이동 누계 2.
- Unknown 복구: 결과 조회로 원래 승인을 확인하며 새 제출 없음. coordinator 재생성 테스트의 카운터는 재생성 이후부터 집계되므로 제출 0이다.
- 승인만 검증하는 어댑터 테스트는 SC06 소비자를 설치하지 않아 소비/이동 0이다. 이 수치를 UI 연결 실패로 해석하지 않는다.
- Unity 원본: `Logs/NET02-task06-{edit,play,off-edit,off-play}.{xml,log}`.
- 서버: `Logs/NET02-task06-server-tests.log`, HTTP 합계: `Logs/NET02-task06-http-counts.json`.
- 빌드: `Logs/NET02-task06-build-{windows,android}.log`, `Logs/NET02-build-{StandaloneWindows64,Android}.json`.
- APK 검사: `Logs/NET02-task06-apk-{badging,manifest,signature}.txt`. Windows 실행: `Logs/NET02-task06-windows-graphics-smoke.log`.

검토한 캡처는 서버 참가자 4명 상태의 기존 SC06이며 테스트 카메라의 RenderTexture로 저장했다. 카메라와 언어/큰 글씨/Safe Area/Game View 선택은 테스트 종료 시 복원한다. 2400 캡처의 검은 외곽은 인위적으로 지정한 inset이고 실제 휴대폰 notch 결과가 아니다.

| 해상도/조건 | 상단 | 스크롤 하단 |
|---|---|---|
| 1080×1920, 한국어 기본 글씨 | [상단](NET02Task06/Captures/SC06_1920_top.png) | [하단](NET02Task06/Captures/SC06_1920_bottom.png) |
| 1080×2400, 영어 큰 글씨·합성 Safe Area | [상단](NET02Task06/Captures/SC06_2400_top.png) | [하단](NET02Task06/Captures/SC06_2400_bottom.png) |

## 수정·산출물

- TASK 05: `NetUiRoomController`, coordinator/journal, 기존 SC06 view/entry/navigation의 선택적 callback, 실제 HTTP 복구 테스트. 커밋 `dd4c011` — `Connect NET02 SC06 room state and confirmed recovery flows`.
- TASK 06: 빌드 결과 JSON 저장 및 PlayerSettings 원본 복원, standalone의 모바일 `Handheld` API 호환 shim, SC06 승인된 방 제목 수정, 그래픽 포트레이트 테스트·runner 옵션·가린 ActionId 증거 추가. 기존 프리팹/씬 직렬화 필드는 추가하지 않았다.
- Windows: `C:/Users/user/Documents/GitHub/ANIMOL/Builds/ANIMOLNet02UiDev/Windows/ANIMOLNet02Dev.exe`. 실행하려면 같은 폴더의 Data/Mono 등도 함께 필요하다.
- APK: `C:/Users/user/Documents/GitHub/ANIMOL/Builds/ANIMOLNet02UiDev/Android/ANIMOLNet02Dev.apk`. 실제 APK 크기는 validation의 artifact bytes를 참고한다. Unity BuildSummary의 Bytes는 부가 산출물을 포함하여 APK 파일 크기와 다르다.
- 기존 enabled Bootstrap/Lobby 등 13씬을 그대로 사용했다. HTTP DevelopmentOnly를 빌드 시에만 적용했고, 원래 PlayerSettings 바이트·DEV OFF·Win64 타깃으로 복원했다. Android 앱 ID가 원래 없는 경우까지 보존한다. 빌드 파일은 Git에 넣지 않았다.
- 기본 연결 설정은 여전히 미설정이다. Windows DEV 제품의 로컬 파일 위치는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/ANIMOLNet02Dev/ANIMOLNet02/connection.json`, Android는 해당 앱의 persistentDataPath 아래 `ANIMOLNet02/connection.json`이다. 실제 매핑/계정/모드 공급 전에는 입장 차단이 정상이다.

## 발견 사항과 남은 검증

이번에 해결한 문제: Windows에서 기존 모바일 Handheld 참조가 컴파일되지 않아 데스크톱에서만 no-op인 별도 shim을 추가했다. 원래 미커밋 GameplayFeedbackDirector는 수정하지 않았다. 승인 후에도 미연결 제목이 남던 SC06 제목을 고쳤다. Unity batch 빌드가 임시 PlayerSettings를 디스크에 남기는 문제는 저장된 원본 복원과 재import로 해결했고 양쪽 빌드 후 바이트 일치를 확인했다.

기존 경고: 최종 각 빌드의 경고 2개는 Pipeline RuntimePipelineConfig 부재 및 플랫폼별 진단 심볼/Cloud 업로드 설정이다. Windows 시작 검사에는 기존 Missing Utility Store의 missing-script 경고 6개가 있다. 무그래픽 Windows 시작 시도는 v7 compositor의 Null graphics 오류가 반복되어 중단했고, GPU를 사용하는 최종 D3D11 시작 검사로 대체했다. 이를 시각 앱의 무그래픽 실행 지원으로 보고하지 않는다.

남은 작업은 실제 Ground/Special/Air 편성·mode/intent·DEV 계정의 공급과 Android 기기 연결이다. 그 뒤 같은 LAN의 PC 사설 IP:8080으로 phone1+PC3→phone4, 실제 키보드/붙여넣기/notch/OS Back/동시 터치/백그라운드·복귀/강제 종료·재시작을 수행해야 한다. NET01 UDP7777 절차는 사용하지 않는다.

Started는 ‘시작 승인 · 경기 연결 대기’까지만 표시한다. 실제 경기·운영 Google 인증·동물 성장 거래·캠페인 승인·랭킹·코인·결제·광고·협동 서비스 완료와는 별개다.
