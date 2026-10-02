# ANIMOL 메인 로비 배경 v7 적용 결과

작업 ID: `ANIMOL_MAIN_UI_FANTASY_BACKGROUND_V7_INTEGRATION`  
검증일: 2026-10-02  
환경: 실제 연결된 Unity **6000.3.8f1**, Built-in Render Pipeline, Linear color space, Windows Editor.

## 적용 결과

기존 Bootstrap 시작 화면과 Lobby의 실제 `SC01_Lobby`에 v7을 적용했다. 이전 v6 렌더러·카탈로그·씬 참조를 재사용하고 **배경 20개 + 좌우 토끼 시트 2개 전부** v7 원본으로 교체했다. 이전 배경 이미지가 섞이지 않았으며 토끼와 합성기는 각각 하나다. 이전에 폐기한 `FiveThemeBackdrop`와 `PA1_LegacyLobby`는 복원하지 않았다.

요청 경로가 처음에는 없어서 발견된 `Docs/Inbox/ANIMOL_Unity_Main_UI_v7/ANIMOL_main_ui_v7` 패키지를 요청 경로 `Docs/Inbox/ANIMOL_main_ui_v7`에 그대로 복사했다. 발견된 원래 패키지는 삭제·변경하지 않았다. `UNITY_APPLY_TASK.md` 전체, manifest, animation.js와 preview HTML의 렌더 규칙을 읽었으며 HTML에 포함된 22개 이미지도 원본과 바이트 대조했다.

에셋 GUID와 연결을 보존하기 위해 런타임 경로 `Assets/ANIMOL/UI/MainUiV6`, Resources 키 `ANIMOLMainUiV6`, 기존 GameObject·shader 이름을 유지했다. **이 이름은 참조 식별자이며 실제 이미지 버전은 전부 v7**이다. 카탈로그의 `sourceVersion`은 `7.0`이다. 패키지 합성 이미지·GIF·HTML을 런타임 배경으로 사용하지 않는다.

## 실제 연결 및 생성 경로

- 씬: `Assets/ANIMOL/Scenes/Bootstrap.unity`, `Assets/ANIMOL/Scenes/Lobby.unity`.
- 실제 흐름: `PortraitEntryController`의 `PA1_Start` Play → 기존 Bootstrap 진입 처리 → Lobby `SC01_Lobby`. 캠페인 `SC02_ThemeSelect`, 상점 `SC11_Store`, 설정 `SC13_Settings`, 뒤로 가기, 종료 확인 모달의 기존 이벤트를 유지했다.
- `UI_CommonRoot`의 `MainUiV6Cycle`이 `MainUiV6Backdrop`을 구동한다. 배경과 불투명 채움은 SafeArea보다 먼저 그려지는 형제이며 ScreenHost·ModalHost·OverlayHost보다 아래다. 두 Graphic 모두 raycastTarget=false다.
- 기존 UI_CommonRoot 프리팹, CanvasScaler 1080×1920, PortraitDisplayController, SafeAreaLayout, 버튼·텍스트·모달 배치를 수정하지 않았다.
- `MainUiV6Builder.Build()`의 메뉴 표시를 **ANIMOL/Main UI V7/Import and apply to existing menus**로 갱신했다. 실행 시 v7 원본 22개를 복사·가져온 뒤 기존 두 씬에 적용한다. 실제 두 차례 연속 실행으로 재적용도 확인했다.
- `MenuHierarchyBuilder`의 기존 `MainUiV6Builder.Apply(nav)` 연결을 유지했다. 직렬화 배경이 없는 재생성 상황은 PlayMode 테스트에서 실제 제거·재설치해 카탈로그 v7, 배경·채움 각각 하나를 확인했다. 전체 씬/맵 생성 메뉴는 실행하지 않았다.
- 반복 적용 시 채움 순서가 합성 화면보다 뒤로 가던 경우를 수정하고, AuthorLayout 3회 반복 시 동일 배경·단일 채움·올바른 순서를 확인하는 EditMode 회귀 테스트를 추가했다.

## 렌더 규칙과 화면 대응

352×704 Point RenderTexture에 `#1a1c2c → far → mid → platform → rabbit → near` 순서로 합성한다. 플랫폼·건물 레이어는 (0,0), 352×704 고정이다. far/mid/near 확대는 1.16/1.36/1.60, 속도는 .30/.62/1.60이며 X=direction×88×(p−.5)×speed, Y=72×(p−.5)×speed를 네이티브 정수로 반올림한다. 제작 기록의 sourceWalkY/platformShiftY/exportFrames 위치값은 런타임에 재적용하지 않는다.

테마는 0·5·10·15·20·25초에 T01→T02→T03→T04→T05→T01 순환한다. 각 5초에는 .36초의 8×8 대각선 binary wipe가 포함된다. 이전 슬롯은 p=.999, 새 슬롯은 현재 p로 렌더하며 alpha crossfade를 사용하지 않는다. 토끼는 512×96 시트의 64×96 셀, `floor(t×14)%8`, Y=436, 발 기준 Y=526이다. X는 오른쪽 -64+p×416, 왼쪽 352−p×416이다. trim, 비균일 확대, 추가 bob 또는 공중 프레임 보정은 없다. 세잎클로버·크림 튜닉·초록 조끼·청록 망토는 원본 그대로다.

배경 방향과 토끼 방향은 같은 슬롯에서 서로 다른 salt(91/314)를 사용하는 독립 선택이다. seed=0을 기본 재현값으로 유지했으며 네 방향 조합을 QA override로 모두 검증했다. 일반 실행에서는 QA override를 설정하지 않는다.

기본 cover를 그대로 쓰면 1080×2400에서 좌우 약 17.6 네이티브 픽셀이 잘린다. 지시서의 전체 셀 가시성 조건을 만족시키기 위해 **배경만 가로폭 기준 등비 확대**했다. 1080×1920에서는 1080×2160 화면의 위아래 120px을 자르고, 1080×2400에서는 같은 배경 위아래에 각 120px의 지정색 채움을 둔다. 토끼 통로의 좌우는 모두 유지한다. Canvas의 Linear vertex color 양자화가 어두운 채움 색을 바꾸지 않도록 흰 tint와 1×1 sRGB 색상 텍스처를 사용했다. 실제 긴 Game View 여백 픽셀도 (26,28,44)다.

22개 원본은 Point, Uncompressed, mipmap=false, NPOT=None, alpha FromInput, sRGB로 가져왔다. 주요 플랫폼 압축 override를 지웠다. 현재·이전·최종 352×704 RT 3개(약 2.84MiB)와 채움 1×1 텍스처, material을 재사용하며 OnDisable/OnDestroy에서 해제한다. 프레임마다 PNG 로드·GameObject 생성·삭제를 하지 않는다.

로컬 unscaled clock은 다른 메뉴·focus loss·pause에서 멈춘다. 같은 씬 메뉴 복귀는 이어 재생하고, 씬 재진입은 T01부터 다시 시작한다. 복귀 시 숨겨진 시간을 더하지 않는다. 게임 timeScale·입력·게임 플레이어·물리·재화·보상 처리를 변경하지 않았다.

## 실제 실행한 검증

| 검증 | 실제 결과 | 증거 |
|---|---|---|
| Unity 컴파일 | 오류 0 | 실제 Editor refresh/compile 및 Console 확인 |
| EditMode | **9/9 통과**, 실패·skip 0 | [editmode-results.json](MainUiV7/editmode-results.json) |
| PlayMode | **6/6 통과**, 실패·skip 0 | [playmode-results.json](MainUiV7/playmode-results.json) |
| 1080×1920 실제 Game View | 26초 실시간 재생, 전체 5테마 | [실재생 CSV](MainUiV7/Captures/1080x1920_Live_26s.csv), [검사 기록](MainUiV7/Captures/1080x1920_checks.txt) |
| 1080×2400 실제 Game View | 26초 실시간 재생, 전체 5테마 | [실재생 CSV](MainUiV7/Captures/1080x2400_Live_26s.csv), [검사 기록](MainUiV7/Captures/1080x2400_checks.txt) |
| 실제 GPU 토끼·근경·viewport | 해상도별 800개, 총 **1,600개** 조합, 원본 불투명 픽셀 손실 0 | [1920 CSV](MainUiV7/Captures/1080x1920_rabbit-audit.csv), [2400 CSV](MainUiV7/Captures/1080x2400_rabbit-audit.csv) |
| 독립 CPU 기준과 GPU 전체 화면 비교 | 5테마+4전환=9장, **2,230,272픽셀 차이 0** | [native-reference-comparison.json](MainUiV7/native-reference-comparison.json) |
| 생성 경로·재진입 | 단일 합성기·단일 배경 유지, UI 경로 유지 | EditMode/PlayMode 결과 및 해상도별 검사 기록 |
| 보호 파일 | 678개 경로·SHA-256 동일 | [전](MainUiV7/protected-before.json), [후](MainUiV7/protected-after.json) |
| 작업 전 기존 미커밋 파일 | 95개 SHA-256 동일 | [전](MainUiV7/preexisting-dirty-before.json), [후](MainUiV7/preexisting-dirty-after.json) |

최종 Unity Test Runner의 **고유 테스트 합계는 15개**다. 재실행 횟수나 이미지 조합·assert 개수를 테스트 수에 합산하지 않았다. EditMode에는 CommercialPolishPolicyTests 3개가 포함된다. PlayMode에는 실제 화면 이동, 비활성 clock, 리소스 재사용, fallback 생성, Sweetie16 binary wipe, 보호 Moon 씬 비설치와 비연결 서비스 비활성 유지가 포함된다.

800개 조합은 T01~T05 × p={.2,.35,.5,.65,.8} × 카메라 방향 2 × 토끼 방향 2 × 8프레임이다. GPU에서 실제 토끼 영역을 읽어 원본 셀의 모든 불투명 RGB와 비교했고, 상위 60% bbox 손실도 별도 집계했다. 부모 Mask/RectMask2D 부재와 실제 화면 좌표를 확인했으며 전체 셀 구간 양 끝 p=64/416·352/416에서도 양방향 셀이 모두 보였다. **원본 시트 잘림, near 가림, viewport 잘림을 구분**했다.

원본 자산 검증은 Unity 검증과 별도로 수행했다. 22개 크기·alpha 0/255·Sweetie16·원본 바이트 동일, 16개 셀 안전 여백·가장자리 불투명 0·좌우 정확한 반전, 발 픽셀 Y=90(3/7번은86), 공통 export scale을 확인했다. preview 내장 이미지 22개와 패키지 체크섬 42개가 일치한다. 독립 near alpha 계산 800개도 전체/귀·얼굴 가림 모두 0이다. [원본 검증](MainUiV7/source-validation.json), [독립 near 검증](MainUiV7/independent-near-validation.json).

실제 raycast로 Start Play, 캠페인, 상점, 설정, 각 Back, 로비 Back→Start→Play, 기존 종료 확인 모달 Cancel을 실행했다. 배경·근경이 UI를 덮거나 클릭을 받지 않았다. 이후 기존 DEV 시작으로 gameplay에 진입해 배경이 사라지는 것과 Lobby 씬 재진입 시 단일 합성기를 확인했다. focus/pause는 Editor에서 해당 Unity lifecycle callback을 호출해 재현했다.

## 제한 및 미실행 항목

운영 T01-S01의 정상 시작 완료는 **미실행/차단**이다. 기존 AvailabilityText에 `Fast-clear reward policy is not approved`, `Replay reward policy is not approved`, `Human completion review for this exact version/hash is missing`가 표시된다. 이 정책과 진행 상태는 변경하지 않았다. 비활성 시작 버튼 클릭이 전환되지 않는 것, 기존 MapDevTestButton 경로와 로비 복귀는 실제 검증했다. 운영 스테이지 시작 성공으로 기록하지 않는다. [차단 원문](MainUiV7/Captures/operational-start-blocked.txt).

PlayMode 격리 씬 전환 중 기존 AudioListener 부재 안내가 한 번 포함되지만 실패는 없었다. DEV 맵 진입 시 기존 StageMapRuntimeLoader의 지원되지 않는 오브젝트 경고가 발생할 수 있으며 이번 범위에서 맵·로더를 고치지 않았다. 모바일 기기 빌드·실기기 Safe Area·OS 백그라운드 전환·전체 프로젝트 테스트는 실행하지 않았다. 이번 결과는 요청한 두 실제 Editor Game View와 관련 테스트에 한정한다.

## 검토할 화면과 재현

- [1920 실제 로비](MainUiV7/Captures/Lobby_T03_1080x1920.png), [2400 실제 로비](MainUiV7/Captures/Lobby_T05_1080x2400.png)
- [도서관 좌우 8프레임](MainUiV7/Captures/rabbit-frames-1080x1920.png), [광산 좌우 8프레임](MainUiV7/Captures/rabbit-frames-1080x2400.png)
- [1920 모달](MainUiV7/Captures/Modal_1080x1920.png), [2400 모달](MainUiV7/Captures/Modal_1080x2400.png)
- [1920 Unity 실재생 영상](MainUiV7/Unity-live-1080x1920.mp4), [2400 Unity 실재생 영상](MainUiV7/Unity-live-1080x2400.mp4)
- [검증 요약](MainUiV7/verification-summary.json), 원본 전체 스크린샷: `Docs/MainUiV7/Captures/`.

영상은 실제 Unity Game View를 약 0.5초마다 캡처한 PNG와 실측 CSV 시간 간격을 이어 붙인 26초 증거 영상이다. 출력 30fps는 동일 캡처의 반복이며 30fps 녹화나 성능 측정 결과를 의미하지 않는다. 실재생에서는 InspectionTime/프레임/방향 override를 모두 해제했다. 프레임 정지 검사는 별도 스크린샷 64장(32장×2해상도)과 GPU CSV로 남겼다.

재현: Bootstrap을 연 뒤 원하는 고정 Game View 해상도에서 Play → `ANIMOL.FiveThemeMenu.MainUiV7Capture.Begin()` 실행. 이 QA 컴포넌트는 UNITY_EDITOR 전용이며 일반 실행에 자동 설치되지 않는다. 독립 검증은 `python Docs/MainUiV7/validate_assets.py`, `python Docs/MainUiV7/validate_native.py`, `python Docs/MainUiV7/summarize_evidence.py`로 실행한다(Pillow, 마지막 명령의 영상 생성에는 imageio_ffmpeg 필요).

변경 범위는 기존 UI 모듈·두 진입 씬의 배경 채움·v7 이미지·관련 검사·원본 패키지 사본·이 보고서와 증거다. 사용자 기존 dirty 변경은 커밋에 포함하지 않는다. 이 문서를 포함한 작업 커밋은 `git log -1 -- Docs/ANIMOL_MAIN_UI_FANTASY_BACKGROUND_V7_RESULT.md`로 확인할 수 있다.
