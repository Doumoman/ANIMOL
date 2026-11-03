# William Notebook UI 적용 결과 — 2026-11-03

## 활성 경로

- 빌드 진입 씬 0: `Assets/ANIMOL/Scenes/Main/NotebookUI.unity`.
- 위 씬의 Canvas는 생성기를 다시 실행해 만든 `Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab`의 인스턴스다. 실제 Play Mode에서 `NotebookRoot`는 하나이고 기존 `MainLobbyController`는 없다.
- `ANIMOL > New Notebook UI > Build Fresh Scene` 생성기는 조직화된 활성 Art/Data 경로를 읽는다. 메인 캠페인 버튼은 이 Canvas 안의 지도 화면을 직접 연다.
- 지도는 `Map/NightSky.png`와 `catalog.artworks`의 별자리 그림 32개, 장식용 작은 별선 32개, 별 110개와 게임 진행 연결선 106개를 사용한다. 지역 풍경 PNG 4개와 사용하지 않는 이전 연결선 PNG는 `Archive/UnusedArt`로 GUID와 함께 옮겼다.
- ZIP의 `Assets/WilliamNotebookUI/Textures`는 저장소의 활성 분류에 맞게 `Assets/ANIMOL/Art/UI/{Main,Settings,ConstellationMap}/Textures`에 배치했다. 원본 얼굴 스크롤링 경로와 코드는 변경하지 않았다.

## 별 정점 정렬 갱신

- 2026-11-03에 받은 갱신 ZIP의 `catalog.artworks` 정점·경로와 `catalog.stages` 좌표를 활성 카탈로그에 반영했다. 기존 실제 스테이지 ID와 잔영 극장 씬 바인딩, BM 설정은 유지했다. 별자리 이름과 의미는 화면이 아닌 `Docs/CONSTELLATION_MEANINGS.md`에만 둔다.
- 32개 별자리 그림 PNG와 `Map/NightSky.png`는 기존 바이트를 보존했다. 패키지에서 바뀐 보조별선 32개와 진행 연결선 PNG 212개만 같은 GUID의 활성 경로에 갱신했다. 그림은 비율을 유지하고, 이미 contain 여백이 들어 있는 보조별선은 같은 680×850 Rect를 비율 보정 없이 채운다.
- 큰 별 110개는 각 그림의 `wirePath[stageRouteIndices]`가 가리키는 `wireVertices`와 일치한다. 진행선 106개는 그림 내부 경로 78개와 그림 간 전환 28개이며, 양 끝이 대응 스테이지 별 좌표와 일치한다. `Docs/CONSTELLATION_ALIGNMENT_VALIDATION.json`의 110개 정점 조건을 활성 카탈로그와 새 프리팹에서 다시 확인했다.
- 생성기를 다시 실행해 `NotebookCanvas.prefab`과 `NotebookUI.unity`를 갱신했다. 실제 빌드 씬 0의 프리팹 참조와 Play Mode의 단일 `NotebookRoot`를 재검증했다. 구형 Canvas의 PNG만 바꾼 결과가 아니다.

## 데이터와 서비스 경계

- 별 슬롯은 별길 목장 20, 잔영 극장 30, 낙일 성소 30, 별너울 해안 30이다. 실제 확인된 ID는 `SW01`–`SW20`, `ET-01`–`ET-30`으로 연결했다. 잔영 극장 30개만 기존 `ANIMOL_Echo_Theater` 씬을 지정했다. 나머지 테마의 실제 퍼즐 씬과 카탈로그 ID가 없어 슬롯은 플레이 불가다.
- 기존 `NotebookProjectGateway`는 잔영 극장의 클리어 기록을 읽는다. 결제 상품 ID, 현지화 가격, 필수 스테이지 목록, 구매·광고·입장 승인 SDK, 로그인·커스텀·대전 로더가 제공되지 않아 해당 기능은 성공 처리하지 않는다. 구매/복원/광고/입장 요청은 실패 또는 미연결로 표시하고, 대전은 잠긴 상태다. 구매 자체로 클리어 조건을 우회하지 않는다.
- 망원경 정책은 180초당 1개, 최대 10개, 검증된 광고 보상 최대 5개, 입장 승인당 1개 차감이다. 보상 토큰과 승인 ID는 중복 처리하지 않는다. 서비스가 없는 현재 실제 플레이에서는 광고 지급과 입장 차감이 일어나지 않는다.
- 설정 값은 현재 Notebook 저장 모델에 보존된다. 실제 BGM/SFX/진동 서비스와 계정 기능은 아직 연결되지 않았으므로 해당 서비스 동작까지 완료된 것으로 보지 않는다.

## 검증

- `Docs/ART_PRESERVATION.json` 원본 스크롤링 파일 71/71 SHA-256 일치. ZIP과 활성 지도 정적 PNG 283/283 SHA-256 일치하며 활성 지도 PNG 집합도 동일하다.
- 새 지도 PNG 277개의 Unity 임포트: Single, Full Rect, PPU 100, Bilinear, 무압축, Mipmap Off, Android/iPhone 오버라이드 Off. 새 버튼은 White Tint, 36픽셀 테두리의 9-slice, SpriteSwap 상태 PNG를 참조한다.
- Unity 6000.3.8f1에서 C# 재컴파일 성공. EditMode `NotebookIntegrationTests` 3/3, `ArtAssetTests` 4/4, PlayMode `NotebookUiPlayTests` 1/1 통과. PlayMode에서 360×640, 445×773, 390×844, 1280×720의 캐릭터 영역과 주요 버튼 경계, 메인→지도→구매 팝업→설정 저장/복귀를 확인했다. BM 테스트는 0/8/10개 보상, 취소·중복, 입장 거절·승인, 필수 클리어 전후 대전 잠금을 검증한다.
- 실제 Unity Play Mode 화면을 `ScreenCapture`로 캡처해 패키지 Preview의 메인과 지도를 육안 비교했다. 지도는 QA 전용 4망원경·3클리어 상태로 맞췄고, 검증 직후 원래 10망원경·0클리어 저장 상태로 복원했다. 그림, 밤하늘, 별·선 순서와 지도 레이아웃은 일치한다. Preview의 시작 버튼은 가상 플레이 가능 상태지만 실제 UI는 입장 서비스와 해당 퍼즐 씬이 없으므로 비활성이다. 얼굴 패턴의 스크롤 위상과 시간 표시에는 캡처 시점 차이가 있다.
- 이 UI는 `Screen Space Overlay` Canvas라 카메라 렌더 타깃 기반 CLI 스크린샷은 검정으로 나왔다. 검증 이미지는 실제 GameView의 `ScreenCapture`로 취득했다. 저장 씬에 테스트 카메라는 추가하지 않았다.
- 정점 갱신 후 Unity C# 컴파일, EditMode `NotebookIntegrationTests` 3/3 및 `ArtAssetTests` 4/4, PlayMode `NotebookUiPlayTests` 1/1 통과. 실제 GameView에서 목장 첫 그림과 정사각형 contain 그림, 잔영 극장 첫 그림의 큰 별·보조선·진행선을 대조했다. 최신 `Preview/NightMap_starway.png`와 동일한 QA 상태(망원경 4개, SW01–03 클리어, SW04 선택, 지도 스크롤 240)에서도 별 좌표가 일치했다. 실제 UI는 미연결 입장 승인으로 버튼이 비활성이고, Preview의 완료 체크 표시는 실제 폰트에서 `완료`로 표시된다. QA 저장 상태는 검증 후 원래 10개·0클리어로 복원했다.

## 남은 실제 연결

상품 ID와 플랫폼 가격, 검증된 영수증/광고/입장 서비스, 확정 필수 스테이지 목록, 나머지 80개 퍼즐의 실제 ID·씬, 로그인·커스텀·대전·오디오·진동 서비스를 제공받은 뒤 `NotebookProjectGateway`에 연결해야 한다. 그 전에는 해당 기능을 플레이 가능하거나 지급 완료로 보고하지 않는다.
