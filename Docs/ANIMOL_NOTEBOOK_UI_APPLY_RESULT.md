# William Notebook UI 적용 결과 (2026-11-04)

## 실제 진입 경로와 화면

- Build Settings의 첫 씬은 `Assets/ANIMOL/Scenes/Main/NotebookUI.unity`다. `NotebookInstaller.BuildFreshSceneForAutomation`을 Unity Editor에서 실행해 `Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab`과 씬을 새로 생성했다. 활성 씬에는 `NotebookRoot`가 하나이며 구형 `MainLobbyController`가 함께 실행되지 않는다.
- 메인의 캠페인 버튼은 동일 Canvas의 별자리 지도로 바로 이동한다. 지도는 `Map/NightSky.png`, 별자리 그림 32개, 보조 별선 32개, 스테이지 별 110개, 진행 연결선 106개를 가진다. `catalog.json`의 20/30/30/30개와 실제 활성 카탈로그의 ID를 보존했다. Unity Play Mode에서 110개 큰 별의 위치와 제작 `wireVertices` 위치를 대조해 오차 0건을 확인했다.
- `Assets/ANIMOL/Art/UI/Common/Textures/Modal`에 전용 Modal PNG 14개를 설치했다. 설정·구매·충전 모달은 회색 `#28282C`/0.58 전체 화면 막 위에 `PanelBase → 24px 원본 얼굴 패턴 클립 → PanelFrame → 내용` 순서로 표시한다. 패널과 버튼의 정적 스프라이트는 흰색 Tint, 버튼은 SpriteSwap이다. 설정 중 메인은 보이지만 입력과 포커스가 차단되고 전역 얼굴 패턴은 숨겨졌다가 복원된다.
- 설정의 얼굴 스크롤 행과 `NotebookPreferences.scroll` 저장 필드를 제거했다. 오래된 `scroll:false` JSON 값은 더 이상 해석되지 않는다. 기존 얼굴 패턴 자료와 움직임 감소 처리는 유지한다.

## 인게임 적용

- `Assets/ANIMOL/Art/UI/InGame/Textures/Gameplay`에 새 PNG 54개를 설치하고 `NotebookGameplayInstaller.BuildGameplayOverlayPrefab`으로 `GameplayCanvas.prefab`과 `CollectibleStarVisual.prefab`을 생성했다.
- `NotebookGameplaySceneInstaller.BindEchoTheaterScene`으로 실제 `ANIMOL_Echo_Theater.unity`의 캠페인 컨트롤러에 HUD, 게이트웨이, 별 비주얼을 연결했다. 기존 퍼즐 세션·카메라·판정·클리어 기록은 그대로 사용한다. 실제 스테이지의 목표 별 개수(ET-01은 2개)와 코스트가 HUD에 표시된다. 별 획득 표시는 실제 세션 이벤트에서만 갱신되고, 클리어 패널은 기존 세션의 승리와 저장된 클리어 기록이 모두 확인된 뒤에만 열린다.
- 일시정지 승인을 실제 `PuzzleSwipeInput`과 `PuzzlePrototypeController.RequestMove` 입력 경계에 연결했다. Play Mode에서 정지 중 직접 이동 요청이 거절되고 위치가 유지되며, 재개 시 입력이 복원됨을 확인했다. 지도 버튼은 실제 `NotebookUI` 씬으로 이동해 새 UI 하나만 남겼다.
- 재시작의 5회 원장·승인 서비스가 활성 프로젝트에 없어 재시작 버튼은 승인 전까지 비활성이다. HUD는 자체 재화 원장을 생성하거나 차감하지 않는다. 다음 스테이지 입장도 승인 서비스가 없어 성공으로 표시하지 않는다.

## 임포트와 보존

- 새 Gameplay PNG 54개와 Modal PNG 14개는 Unity에서 Single, Full Rect, Bilinear, PPU 100, 무압축, Mipmap Off, Android/iPhone 오버라이드 없음으로 확인했다. Modal 프레임과 상태별 버튼 스프라이트는 36px 9-slice다.
- `Docs/ART_PRESERVATION.json`의 원본 얼굴 스크롤 관련 71개 파일 SHA-256을 재검사해 누락·불일치 0건이었다. 원본 PNG·그림·셰이더·스크롤 코드와 지도 정적 PNG를 다시 그리거나 수정하지 않았다.
- ZIP에 포함된 임시 `catalog.json`은 기존 실제 ID와 빈 필수 목록을 되돌리므로 복사하지 않았다. 기존 `Assets/ANIMOL/Data/UI/Notebook/catalog.json`을 유지했다.

## Unity 검증

- Unity 6000.3.8f1: 생성 프리팹/씬 Import 및 C# 재컴파일 완료, 오류 0건.
- Unity Editor에서 `NotebookIntegrationTests`의 3개 EditMode 검사를 직접 호출해 모두 통과했다. 씬·프리팹/32그림·110별 정점·106연결, 277개 지도 PNG 임포트/SpriteSwap, 망원경 0/8/10 및 보상·입장 중복/대전 조건을 포함한다. 공식 `run_tests` 명령은 테스트 3개 목록을 읽은 뒤 125초 응답 제한에 걸려 완료 결과를 받지 못했다. 따라서 공식 Test Runner 통과로 기록하지 않는다.
- 실제 Play Mode GameView에서 메인·설정·지도·잠긴 지역 구매 모달, 잔영 극장 HUD·일시정지·재개·지도 복귀를 확인했다. 360×640, 390×844, 445×773 세로 화면과 1280×720 PC 화면에서 메인 캐릭터·버튼 및 퍼즐 HUD가 잘리지 않았고, 퍼즐 화면은 상단 미렌더 영역과 레터박스 없이 채워졌다. 445×773 `starway/choa` 동일 시각 상태의 메인을 제공 Preview PNG와 대조해 주요 요소 배치가 일치했다. 프리뷰의 임의 클리어·유료 구매 상태를 실제 검증 결과로 취급하지 않았다.

## 현재 미연결 항목

- 활성 프로젝트에서 구매 Product ID, 플랫폼 현지화 가격, 검증 영수증/복원 SDK, 광고 보상 승인, 입장 승인, 재시작 원장, 로그인·커스텀·대전 로더, 실제 대전 필수 스테이지 확정 목록을 찾지 못했다. 관련 버튼·보상·입장은 성공으로 꾸미지 않고 잠금 또는 미연결로 표시한다. `battleRequiredStageIds`는 비어 있고 `stageBindingsPending`은 `true`다.
- 카탈로그 110 슬롯 중 잔영 극장 30개만 현재 퍼즐 씬 이름이 있다. 별길 목장 20개와 나머지 두 지역 60개는 플레이 가능한 씬이 아직 연결되지 않아 지도에서 진입 불가다. 잔영 극장 역시 입장 승인 게이트웨이가 없어 지도에서 바로 플레이할 수 없다.
- 실제 승리 동작으로 클리어 패널을 열고 다음 입장을 승인하는 시나리오, 실제 구매/복원/광고 SDK와 재시작 원장 시나리오는 실행하지 않았다. 프리뷰 PNG는 Unity Play Mode 검증으로 세지 않았다.
