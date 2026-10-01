# ANIMOL 다섯 테마 메뉴 배경 적용 결과

2026년 10월 1일, Unity 6000.3.8f1에서 Bootstrap 시작 화면과 Lobby 모드 선택 화면에 제공 팩을 적용했다. 메뉴 장식만 추가했으며 플레이 스테이지, 물리, 충돌체, 수동 맵은 변경하지 않았다. 시작 씬은 `Assets/ANIMOL/Scenes/Bootstrap.unity`다. Play 모드에서 기존 UI와 새 배경이 설치된다.

## 적용 기준과 기존 작업 보존

시작 커밋은 `f9c919a0fec8e8a9ea4d1609e42f09c0f4081a28`이다. 작업 전 미커밋 파일 1,040개의 경로와 SHA256을 [baseline.json](FiveThemeMenu/baseline.json)에 기록했다. 기존 미커밋 파일은 이번 커밋에 포함하지 않는다. 별도의 보호 대상인 `Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity`는 기준 커밋의 Git blob과 비교한다. T01-S01 수동 맵은 기존 파일 해시 비교에 포함된다.

ZIP의 [README](FiveThemeMenu/Source/README_KO.md), [manifest](FiveThemeMenu/Source/manifest.json), [적용 지침](FiveThemeMenu/Source/CLI_APPLY_PROMPT_KO.md), 원본 프리뷰를 확인했다. 팩 지침은 이전 v3 화면을 전제로 하지만 실제 저장소에는 PortraitArtV1 세로 카드 화면이 있었다. 따라서 원본 프리팹을 덮어쓰지 않고 기존 실행 중 Button 객체의 그림과 배치만 이번 규격으로 바꿨다. 버튼 이벤트, 화면 전환, 재화 상태, SafeArea는 기존 구현을 재사용한다.

## 배경과 토끼 규칙

배경 순서는 아래와 같으며, 5초마다 다음 테마로 진행한다. 각 구간 4.5초부터 5초까지 배경과 광선만 크로스페이드하고, 25초에는 다시 월궁으로 이어진다. 시작과 모드 선택은 같은 세션의 실시간 기준점을 공유하므로 Bootstrap에서 Lobby로 넘어가도 처음부터 재생하지 않는다. 상점 등 다른 화면에서는 배경을 숨기지만 시간축은 이어진다.

| 테마 | crop 시작 Y | 원본 발 Y | 기본 화면 발 Y |
|---|---:|---:|---:|
| 월궁 | 52 | 424 | 1488px |
| 구름고래 목장 | 52 | 424 | 1488px |
| 별가루 도서관 | 48 | 420 | 1488px |
| 시간유리 온실 | 39 | 411 | 1488px |
| 오로라 수정광산 | 33 | 405 | 1488px |

좌표 Y는 원본 이미지 및 화면의 위쪽에서 아래쪽으로 센 값이다. 기본 crop X는 41이다. 각 352×704 배경은 1408×2816으로 표시한다. 1080×1920에서는 270×480도트, 1080×2400에서는 270×600도트가 보인다. 배경 전체에만 정수 도트 x±6, y±4 팬을 적용하며 건축물이나 회랑을 분리하지 않는다. 다음 배경은 자기 구간 시작 0.5초 전부터 독립적인 팬 위치로 그리므로 구간 경계에서 같은 장면이 반대쪽으로 되감기지 않는다.

토끼는 제공된 개별 24×32 이미지 8개를 96×128 크기로 표시하며 10fps로 갱신한다. 프레임 상단 기준 발 (12,30), 중심에서 발까지 14도트 규칙과 배경 pan Y를 적용한다. 기본 발 위치는 1488px이고 팬에 따라 ±16px 움직인다. 세션별 난수와 구간 번호로 좌우 방향을 선택하며 Unity 게임플레이 난수 상태를 소비하지 않는다. 왼쪽 이동은 X 반전한다. 구간 4.5초까지 화면 밖으로 빠져나가고 다음 구간에는 화면 밖에서 다시 진입한다. 전환 중에도 토끼 Image는 하나뿐이다.

배경과 토끼에는 Collider나 게임플레이 입력이 없고 모든 장식 Image의 raycastTarget은 false다. 게임 시간 배율, 입력 차단, 보상, 소리, 햅틱, 카메라 흔들림은 추가하거나 변경하지 않았다.

## UI와 임포트 설정

기존 Canvas와 SafeArea를 그대로 사용한다. 전체 화면 배경은 Canvas 내부에서 SafeArea 뒤에 배치하고, 버튼은 SafeArea 안에 남긴다. 전체 화면에는 불필요한 RectMask2D를 사용하지 않아 화면 끝의 1px 보간을 제거했다.

- 시작 화면은 기존 640×228 플레이 버튼 하나를 유지한다. 위아래 96px SafeArea 검사에서 토끼 발을 가리지 않도록 하단 여백만 기존 180에서 72로 조정했다. 버튼 객체·입력 영역 크기·핸들러는 같다.
- 모드 선택은 기존 세 Button을 x24/376/728, 하단 72, 크기 328×176의 가로 행으로 표시한다. 새로운 버튼이나 중복 이벤트는 만들지 않는다.
- 상단 버튼은 이번 팩의 48×48 원본을 96×96으로, 코인은 24×24를 48×48으로 표시한다. 재화 캡슐과 플레이 그림은 기존 2배 크기를 유지한다.
- 카드 이름과 재화 수치는 기존 TMP로 표시한다. 카드 제목은 32pt, 플레이 제목은 기존 80pt다. 폰트는 기존 Point 래스터 정적 에셋과 동적 폴백을 재사용한다. 따라서 이미지 2배 규칙과 모든 글자의 래스터 배율이 동일하다는 의미는 아니다.
- 실제 서버가 없는 경쟁전·협동, 광고 제공자가 없는 광고는 보이는 비활성 상태를 유지한다. 코인은 조회 불가일 때 `--`이며 가짜 잔액이나 보상 동작을 추가하지 않았다. 안내 문구 뒤에는 작은 비입력 어두운 판을 추가해 다섯 배경 위에서 읽을 수 있게 했다.
- 캠페인, 상점, 설정 및 뒤로 가기는 기존 화면과 연결되고, 기존 UI 피드백도 유지한다.

`Assets/ANIMOL/UI/FiveThemeMenuMaps/`에 PNG 29개와 제공 OTF 1개를 임포트했다. PNG는 Sprite Single, FullRect, Point, PPU32, 압축 없음, Mipmap Off, NPOT None이다. 실제 토끼 애니메이션은 개별 8프레임만 사용한다. 제공 아틀라스는 사용하지 않으므로 슬라이스하거나 중복 표시하지 않는다. 제공 폰트 및 기존과 동일한 플레이·재화 그림의 복사본은 팩 보존용이며 실행 중에는 기존 자산을 재사용한다.

## 검증 결과와 캡처

컴파일 오류는 없으며 아래 관련 검사 15건은 모두 통과했다.

관련 EditMode 9건과 PlayMode 6건을 최신 코드로 실행했다. 상세 결과는 [메뉴 정책](FiveThemeMenu/editmode-menu-run.json), [연출 정책](FiveThemeMenu/editmode-feedback-run.json), [기존 UI 자산](FiveThemeMenu/editmode-existing-ui-run.json), [메뉴 Play](FiveThemeMenu/playmode-menu.json), [기존 UI 이동](FiveThemeMenu/playmode-existing-ui.json)에 있다. 최종 컴파일 결과는 [compile.json](FiveThemeMenu/compile.json)이다. 전체 프로젝트 회귀 테스트는 이번 작업에서 재실행하지 않았으므로 과거 전체 검사 실패 6건을 이번에 재검증했다고 주장하지 않는다.

실제 Unity Game View Play 모드에서 해상도별 23장씩 총 46장을 기록했다. 프리뷰를 실제 실행 캡처로 대체하지 않았다. 정해진 경계값을 확인하는 캡처에서는 메뉴 컨트롤러의 검사 전용 시간값을 지정했으며 게임 시간은 변경하지 않았다. 별도로 시간값 지정 없이 26초 동안 실제 순환을 [CSV](FiveThemeMenu/Captures/LiveClock_26s.csv)에 기록했다.

| 테마 | 1080×1920 모드 화면 | 1080×2400 모드 화면 |
|---|---|---|
| 월궁 | [캡처](FiveThemeMenu/Captures/Mode_moon_1080x1920.png) | [캡처](FiveThemeMenu/Captures/Mode_moon_1080x2400.png) |
| 구름고래 목장 | [캡처](FiveThemeMenu/Captures/Mode_cloud_whale_ranch_1080x1920.png) | [캡처](FiveThemeMenu/Captures/Mode_cloud_whale_ranch_1080x2400.png) |
| 별가루 도서관 | [캡처](FiveThemeMenu/Captures/Mode_stardust_library_1080x1920.png) | [캡처](FiveThemeMenu/Captures/Mode_stardust_library_1080x2400.png) |
| 시간유리 온실 | [캡처](FiveThemeMenu/Captures/Mode_timeglass_greenhouse_1080x1920.png) | [캡처](FiveThemeMenu/Captures/Mode_timeglass_greenhouse_1080x2400.png) |
| 오로라 수정광산 | [캡처](FiveThemeMenu/Captures/Mode_aurora_crystal_mine_1080x1920.png) | [캡처](FiveThemeMenu/Captures/Mode_aurora_crystal_mine_1080x2400.png) |

같은 폴더의 `Start_*`는 테마별 시작 화면, `Boundary_*`는 0/4.5/4.75/4.999/5/24.5/24.75/24.999/25초, `Rabbit_Left/Right_*`는 좌우 반전, `SafeArea96_*`는 위아래 96px 인셋 모의 검사다. 각 PNG의 JSON에는 테마·시간·블렌드·발 위치·버튼 영역이 기록돼 있다. `1080x1920_checks.txt`와 `1080x2400_checks.txt`에는 한글 잘림과 누락 글자, 버튼 영역, 실제 포인터 이벤트 이동, 광고·모드 비활성, 단일 Canvas 검사 결과가 있다.

[최종 검증 JSON](FiveThemeMenu/final-verification.json)은 기존 파일 보존, 테스트 결과, 캡처 크기와 해시, 배경 4×4 및 뒤로 버튼 2×2 픽셀 블록의 직접 검사 결과를 기록한다. 재검증은 `Docs/FiveThemeMenu/verify.ps1`로 실행할 수 있다.

## 변경 파일과 남은 범위

이번 변경은 새 `Assets/ANIMOL/UI/FiveThemeMenuMaps/` 폴더와 메타 파일, `Docs/FiveThemeMenu/` 검증 자료, 이 보고서로 한정했다. 기존 씬·프리팹·스크립트·프로젝트 설정은 수정하지 않았다. 새 폴더에는 `MenuThemeCycle`, `MenuThemeCatalog`, `MenuThemeSkin`, 임포트 빌더, 명시적으로 실행하는 Editor 전용 캡처 도구, EditMode·PlayMode 테스트가 들어 있다. Unity가 생성한 카탈로그는 제공 이미지 참조만 보관한다.

테스트 도중 발견한 QA 경로 오기와 SafeArea 복원 직후 검사 타이밍은 수정 후 재실행했다. 초기 캡처에서 확인한 상태 문구 대비와 마스크 가장자리 보간도 보정했다. 캡처는 기존 UI 화면 전환 효과가 끝난 뒤 기록한다. 실시간 CSV는 검사 전용 시간값을 해제한 후 프레임 동기화를 기다려 기록한다. 테스트가 채운 기존 동적 폰트 캐시는 Unity API로 원래 빈 상태로 정리하고 Git 차이가 없음을 확인했다. 결과물은 기존 워크트리에 의존하며 다른 작업의 미커밋 파일을 함께 커밋하지 않는다.

실기기 성능, 노치가 있는 실제 기기, 다양한 가로 해상도는 미검증이다. 정확한 4px/2px 값은 요청한 가로 1080 해상도 기준이며 다른 가로 해상도에서는 기존 CanvasScaler 배율을 따른다. 광고·온라인 서버 구현, 실제 플레이 스테이지 변환은 범위 밖이다. 메뉴 배경 다섯 장과 토끼는 제공된 최종 자산을 사용하며 임시 생성 그림은 없다.
