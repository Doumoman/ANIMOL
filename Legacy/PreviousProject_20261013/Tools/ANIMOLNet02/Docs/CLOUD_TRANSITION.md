# 이후 클라우드로 연결하는 경계

NET02에서 실제로 만든 클라우드 자원이나 배포는 없다. 서버는 PC/VM에서 동일한 Python/SQLite 소스를 실행하며 클라이언트 endpoint를 바꿀 수 있게 구성했다. 기존 UI 버튼/Phase3/receipt/SC06 바인딩은 그대로 사용하는 것이 목표다. 실제 운영 인증/권한/맵 게임서버까지 자동으로 준비된 것은 아니다.

## DEV VM 전환

1. Python3.10+가 있는 LinuxVM에 이패키지 전체를 `/opt/animol-net02`로 복사한다. Python서버이므로 UnityLinuxDedicatedServer 실행파일은 필요 없다.
2. 전용 사용자 `animol`과 DB 디렉터리 `/var/lib/animol-net02`를 준비하고 해당 사용자의쓰기권한을 부여한다. 실제검증된config는`/etc/animol-net02/config.json`, 환경파일은`/etc/animol-net02/service.env`에 둔다. 테스트암호/DB는공개파일에저장하지 않는다.
3. `Tools/Cloud/animol-net02.service`와 env예시를 설치한다. 기본bind는127.0.0.1:8080이며reverseproxy가앞에서연결한다. systemd배포·방화벽설정은사용자환경에서실행해야 하며이패키지작성시실행하지 않았다.
4. VM의SQLite·세션/입장결과를동일volume에유지한다. 다중프로세스/다중VM의동시방관리·분산DB·오토스케일은미구현이다.
5. 승인된DEV테스터의VPN/접근제어와HTTPSreverseproxy를구성한다. VM공개주소에무인증stdlibHTTP를노출한운영서비스로취급하지 않는다.
6. 클라이언트endpoint를`https://확인된테스트호스트`로설정하고기존UI에서4기기검증한다. 인증서검증을우회하는CertificateHandler를추가하지 않는다.

NET02 lobby API는HTTP/TCP8080, HTTPSproxy는TCP443을사용한다. 이전NET01 UnityTransport의UDP7777을NET02용으로열지 않는다. 후속실시간Unity게임서버의UDP포트는별도계약이다.

## 운영 연결 전 필요한 교체

| DEV 구현 | 운영 공급자가 맡을 책임 |
|---|---|
| config의테스트계정/발급bearer | Google/게스트플랫폼토큰검증, 실제계정세션·만료·회수·계정전환 |
| 명시적DEV동물권한/revision | 실제종구현·계정별보유/사용권·성장snapshot 및정책재검증 |
| PythonstdlibHTTP서버 | 인증/TLS/요청한도/관측/오류/운영배포관리의확인된서비스 |
| SQLite단일서버방/멱등결과 | 실제영속저장·백업·마이그레이션·분산동시성/만료정책 |
| Started대기방상태 | 실제Unity게임서버배정·접속ticket·동기화·결과검증 |
| DEV입장journal | 실제운영G-04의계정귀속보존·서버최종결과/receipt수명·앱복구 |

향후서버가같은API/receipt계약을유지하면클라이언트endpoint/provider변경을중심으로연결할수있다. 실제서버의계약이다르면기존adapter경계의변환코드도수정한다. UI에새입장경로를병렬로추가하지 않는다.

클라우드사업자·인스턴스크기·가격은이패키지에서확정하지 않는다. 실제사용량과게임서버단계를기준으로추후선택한다.
