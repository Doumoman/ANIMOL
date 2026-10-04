# Android/LAN — 기존 UI 통합 후 테스트

이 절차는 프로젝트 TASK01~06의 UI 바인딩과 확인된 데이터 공급 뒤에 수행한다. 현재 ZIP은 APK가 아니다. 기본 빈config에서 입장이 차단되면 정상이며, 데이터가 없는 운영UI 입장 테스트를 pass로 기록하지 않는다.

## 1. 준비

- Unity Hub의 Android Build Support/SDK/NDK/OpenJDK가 설치된 Unity6000.3.8f1.
- PC에서 Python3.10+ 실행 가능. Windows는 `py -3 --version`으로 확인.
- 폰과 서버PC가 같은Wi-Fi/LAN. 게스트Wi-Fi/AP isolation이면 기기간 접속이 막힐 수 있다.
- 각 클라이언트는 서로 다른 허가된 DEV 계정/세션. 같은 계정으로4명을 연출하지 않는다.
- 기존 화면은1080×1920 Portrait 유지. 인수인계의1080×2400 및실제SafeArea도 확인.

## 2. 서버

패키지 루트에서 다음을 실행한다.

```powershell
py -3 Server/server.py --config Server/config.empty.json --database Runtime/net02.sqlite3 --bind 0.0.0.0 --port 8080
```

빈 설정은 상태 확인/차단 검증용이다. 실제 UI 연결 테스트에는 확인된 실제종/모드/DEV계정 데이터를 공급한별도config 경로를 지정한다. `ServerTests/fixture_config.json`은 서버 프로토콜 검증에만 쓴다.

PC 사설IPv4는 `ipconfig`로 확인한다. 폰에 `127.0.0.1`을 입력하면 폰자신에게 연결되므로 `http://PC사설IP:8080`을 사용한다. `0.0.0.0`은 서버listen용이며 클라이언트주소가 아니다.

Windows 방화벽이 막으면 해당Python/TCP8080의Private프로필·LocalSubnet만 허용한다. 프로그램/OS/공유기전체 방화벽을 끄지 않는다. 이 서버는 HTTP/TCP이며 UDP7777과 다른프로토콜이다.

## 3. 빌드·연결

1. `ANIMOL_NET_UI_DEV`를 활성화하고 재컴파일이 끝난 뒤 DEV빌드메뉴를 사용한다. 정확한메뉴는 제공Editor소스와 `CLIENT_BINDING.md`를 확인한다.
2. 기존Bootstrap/Lobby 씬과 기존경쟁UI로 진입한다. 독립NetDev씬을빌드하지 않는다.
3. Android 테스트 앱ID는`com.animol.net02dev`. Development/debug서명/APK/Portrait/INTERNET를 사용한다.
4. LAN HTTP 허용은`PlayerSettings.insecureHttpOption=InsecureHttpOption.DevelopmentOnly`로 제한한다. APK연결실패 시 generated Android manifest/network-security설정도 실제로검사한다. 출시빌드에서HTTP허용을확대하지 않는다.
5. 프로젝트의별도DEV연결설정에서 서버URL·DEV계정을 지정하고 세션연결한다. 이설정은운영Google로그인상태를 바꾸지 않는다.

Windows 클라이언트 3개는 다음과 같이 별도 저장 슬롯으로 실행한다. 각 창에서 서로 다른 DEV 계정을 사용한다.

```powershell
.\Tools\StartWindowsClients.ps1 -Executable "C:\Users\user\Documents\GitHub\ANIMOL\Builds\ANIMOLNet02UiDev\Windows\ANIMOLNet02Dev.exe" -Count 3
```

공식참고: [Unity6000.3 insecureHttpOption](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-insecureHttpOption.html), [DevelopmentOnly](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/InsecureHttpOption.DevelopmentOnly.html), [INTERNET](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings.Android-forceInternetPermission.html).

## 4. 필수 시나리오

| 테스트 | 기대결과 |
|---|---|
| 빈config/미매핑3종 | 열람가능, 실제입장차단, 참가자/Ready성공없음 |
| 허브·코드조회 | 실제모드/정책확인. 잘못된/없는코드는오류·복귀 |
| 확인취소/연타 | 요청0회/확정1회/추가0회 |
| phone1+PC3 | 서버와UI의참가자4명·각편성·방장일치 |
| 동시5번째 | 서버거절, 클라이언트4행일치 |
| 같은동물 | 다른플레이어동일종이권한을충족하면허용 |
| Ready | 서버응답확정뒤상태표시, 응답유실시상태재조회 |
| 비방장시작 | 서버거절 및유효한오류 |
| 전원Ready/방장시작 | DEV최소인원검증뒤Started, 게임씬으로이동안함 |
| 방장퇴장 | 대기상태에서남은참가자로권위이전 |
| 입장응답유실 | Unknown, 새ActionId입장차단, 같은요청결과조회 |
| Unknown앱종료 | 재시작시저장된요청복구, 중복입장/방생성없음 |
| receipt중복/재진입 | 정확한서버승인만1회소비, SC06복원은상태재바인딩 |
| 서버재시작 | 같은SQLite/세션으로입장결과·방복구 |
| Wi-Fi꺼짐/복귀 | 조회실패/끊김표시, 현재방재조회, 자동신규입장없음 |
| 승인된나가기 | 서버반영뒤허브복귀;유실시탈퇴상태확인 |
| 계정/endpoint전환 | pending/Unknown/현재방동안차단 |
| phone4 | 각기다른기기/계정4인통합검증 |

## 5. 실제기기UI

notch/제스처SafeArea, AndroidOSBack, TMP키보드·붙여넣기·닫기, 코드복사, 긴방장/모드/오류문구,4슬롯잘림, 모달Raycast,배경복귀·프로세스재시작을 확인한다. 실제승인/거절/조회/소비카운터와기기모델/OS/서버configrevision/빌드버전을 결과에 남긴다. bearer/암호/AcceptanceToken 원문은 로그·캡처에서 가린다.

게임플레이동기화·방울3/출구·경기결과·보상·랭킹 테스트는 NET02 완료기준에 포함되지 않는다.
