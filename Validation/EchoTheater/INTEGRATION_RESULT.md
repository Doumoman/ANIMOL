# 잔영 극장 22씬 Unity 통합 결과

검증일: 2026-10-25. 프로젝트: Unity 6000.3.8f1 / Windows. 독립 대화 씬은 `Assets/WilliamParadoxStoryV14/Generated/EchoTheaterStory.unity`다. 기존 목장 씬, 캠페인 씬, Packages, ProjectSettings와 게임플레이 코드는 변경하지 않았다.

## 적용과 병합

- 세 ZIP을 `MCP_INBOX/EchoTheater_unpacked_v1`에 합쳐 풀고 README, 경험 검토, `Docs/VALIDATION.json`을 읽었다. 공급 검증 JSON은 554개 로컬 검사 통과, Unity 미실행으로 기록돼 있었다.
- `apply_to_project.py`의 최초 계획은 v19 대비 7개 파일 차이로 중단됐다. 현재 프로젝트의 `StoryArtViewport`, JsonUtility의 기본 rect 판정, foreground 허용, importer 버전과 기존 테스트를 보존해 설치 입력을 병합했다. 재계획은 create 41, replace 9, unchanged 18이고 `--apply`가 완료됐다. 기존 `.meta` GUID는 유지했다. 설치 전 교체본은 `StoryIntegrationBackups/20261025T134127755626Z`에 남아 있다.
- 같은 `WilliamParadox.Story` 소스가 다른 Assets 경로에 없는 것을 설치 가드로 확인했다. 레거시 폴더는 삭제하거나 활성 폴더로 복구하지 않았다.
- 런타임은 기존 단일 StoryRunner/View/Graph/Save를 확장했다. 별도 진행·보상·턴 시스템은 추가하지 않았다. 목장 전용 viewport 동작은 유지하고, 극장 씬 생성 시 해당 컴포넌트를 제거해 고정 원화 창과 충돌하지 않게 했다.

## Unity 실행 결과

| 검사 | 결과 | 근거 |
|---|---|---|
| UGUI / TMP / 입력 | UGUI 2.0.0, `Assets/TextMesh Pro`, `activeInputHandler: 0` 확인. 생성 씬은 StandaloneInputModule 1개 | `Packages/manifest.json`, ProjectSettings, 생성 씬 검사 |
| 전체 C# 컴파일 | 오류 없이 컴파일. 기존 `TMP_Text.enableWordWrapping` obsolete 경고와 기존 중복 메뉴 경고가 남음 | `Logs/EchoTheater/font-build.log`, `theater-build-final.log` |
| 한국어 TMP 글꼴 | 동봉 NotoSansKR-SDF 생성, 한글 글리프 로드 검증 통과 | `Generated/NotoSansKR-SDF.asset`, `theater-validate.log` |
| 원화 Importer | Theater Art 24 PNG 강제 Reimport 후 NPOT None / 2048 / Single Sprite / mipmap·readable off / sRGB / alpha / Bilinear / Clamp / CompressedHQ 확인. 다른 텍스처 설정은 변경하지 않음 | `theater-art-reimport.log`, 새 `.png.meta` 24개 |
| 씬 데이터·생성 | 22씬, 발화 239개, 총 노드 246개. Resources 원화와 글꼴 사전 검사, 씬 생성, 생성본 구조 검사 통과 | `theater-validate.log`, `theater-build-final.log`, `theater-scene-check-final.log` |
| EditMode | WilliamParadox.Story.Tests 51/51 통과 | `Logs/EchoTheater/editmode-results.xml` |
| PlayMode | 생성 씬 전용 2/2 통과. 245개 종료 전 노드 순회, 발화/지문 분리, T16/T17, 저장 복원, 페이지 저장, Pause/Focus/비활성화 복귀, 종료 1회 호출 검사 | `Logs/EchoTheater/playmode-results.xml` |
| 화면 | 배치 PlayMode에서 1080×1920 모바일과 1600×900 PC 캡처. 첫 진입 원화가 alpha 0으로 빈 화면이 되는 문제를 고쳐 두 캡처에서 첫 원화·대사·버튼 확인. PC 9:16 필러박스와 단일 Safe Area 구조 확인 | `Logs/EchoTheater/first-frame-mobile.png`, `first-frame-desktop.png` |

첫 Unity 씬 생성 시 배치 모드의 저장되지 않은 기본 씬 때문에 Additive NewScene 예외가 났다. 배치 시작 씬에 한해 Single 모드를 사용하도록 고친 뒤 생성과 생성본 재검사가 통과했다. 처음 PlayMode에서 모든 대사가 기본 1080×1920 대사창 한 페이지에 들어가 자연 페이지가 없음을 확인했다. 텍스트 영역을 좁힌 조건에서 같은 노드의 2페이지와 저장 복원을 검증했다.

## 사건·연출 확인 범위

- T16/T17은 선택·드래그 없이 고정 cue다. 두 사건의 flag, 임시 지우개음 매핑, 220ms 대기 중 연타 차단, AFTER 그림, 저장 후 즉시 정착, 종료 재진입 시 완료 이벤트 중복 방지를 검사했다. T16_AFTER에는 피코 없이 가방이 남고 T17_AFTER에는 세리 없이 가방과 빈자리가 남는 원화도 확인했다.
- T21/T22 원화에는 피코·세리가 돌아오지 않는다. 마지막 대사 뒤 `T22_REST`는 대사창을 숨기고 입력을 기다리며, end 노드는 350ms dim만 사용한다. 원화의 별빛과 빈 객석은 정지 그림으로 유지한다.
- 버튼 시각 반응, 고정 hitbox, Disabled, 감소 모션, 초상 정착, Bridge 요청 균형과 복귀 중 소리 억제는 EditMode에서 확인했다. PC 키보드 실제 조작, 터치 실기기, 노치별 Safe Area, 청감 품질, 239발화의 사람 읽기 속도는 수동 기기 검수 전까지 미확인이다.
- Unity 자동 테스트는 극장 단독 씬을 실행했다. 실제 캠페인 씬에 이 프리뷰 Canvas/EventSystem/AudioListener를 중복 삽입하지 않았다. 전역 `Time.timeScale`은 변경하지 않았다.

## 실제 게임 연결이 남은 이유

활성 `PuzzleController`, `PuzzlePrototypeController`, `PuzzleSwipeInput`, `PuzzleSession`, `PuzzleDeviceSession`을 읽었다. 현 코드에서 스와이프 수락과 코스트는 확인했으나, 요청한 20초 능력창, 퍼즐 타이머, 캠페인 저장·정식 체크포인트 이벤트, 출구 클리어를 스토리 진행으로 전달하는 호스트 계약은 찾지 못했다. `StoryGameplayBridge.sinks`는 0개이며 대화 중 실제 게임 시간이 멈춘다는 판정은 할 수 없다. 새 타이머·턴·보상 시스템이나 임의의 완료 이벤트를 만들지 않았고 `onCompleted`를 보상으로 연결하지 않았다. 이 항목과 대화 종료 후 자동 1턴 소모 여부는 호스트 계약이 준비된 뒤 `IStoryPauseSink` 어댑터와 정식 출구/체크포인트 이벤트로 연결하여 PlayMode에서 검증해야 한다.

## 다음 검토 순서

1. 호스트 계약을 확보해 입력·20초 창·타이머·턴 정지를 연결하고, 실제 기기에서 원인/결과·읽기·첫 프레임·복귀를 확인한다.
2. 버튼 상태와 5개 임시 효과음의 실제 눌림/정착/볼륨을 모바일·PC에서 확인한다.
3. 필요한 인서트·정식 음향·프로파일링은 독서 시간 측정 뒤 진행한다.

`CommercialPolishPolicyTests`는 2026-10-13 격리본의 이전 테스트라 이번 활성 런타임에서 실행하지 않았다. 이번 수정에 대한 실제 Unity 검증은 위 EditMode/PlayMode 결과로 구분한다.
