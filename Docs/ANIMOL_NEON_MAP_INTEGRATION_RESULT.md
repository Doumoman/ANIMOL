# 별자리 지도 네온 UI·별 회전 통합 결과

## 실제 적용 경로

- 활성 빌드 첫 씬: `Assets/ANIMOL/Scenes/Main/NotebookUI.unity`
- 씬에서 참조하는 Canvas: `Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab`
- 실제 Root/Gateway: `NotebookRoot` / `NotebookProjectGateway`
- 기존 `Constellation Map`과 같은 부모에 `Constellation Neon Map`을 배치하고 `NotebookRoot.stageScreen`, `NotebookCanvasLayout.mapScreen`을 새 지도로 연결했다. 이전 지도는 비활성 상태로 보존했다. Main/Settings/Modal/Gateway, Camera, EventSystem, Build Settings는 교체하지 않았다.
- Edit Mode의 Prefab Stage에서 Root를 선택해 `ANIMOL > New Notebook UI > Apply Neon Map To Selected Notebook` 메뉴를 한 번 실행했다. Undo로 기존 지도와 Root 참조가 복원되는 것을 확인한 뒤 다시 적용하고 Prefab을 저장했다. 메뉴 자체에는 자동 저장 기능이 없다. 같은 Root에 재실행하면 중복 지도를 만들지 않도록 거절한다.

## 지도 자산과 동작

- `MCP_INBOX/William_Notebook_UI.zip`의 MapNeon PNG 18개와 Map/Stars PNG 48개를 `Assets/ANIMOL/Art/UI/ConstellationMap/Textures`에 원본 바이트 그대로 가져왔다. 기존 별 상태 PNG 4개는 동봉 V0/F0 호환본으로 갱신했고 GUID는 유지했다.
- Import 설정은 Sprite Single/Full Rect, Bilinear, Mipmap Off, Uncompressed, PPU 100, 중앙 피벗을 사용한다. PanelFrame Border 36, Utility/Wallet/Action Border 32, 아이콘과 별 Border 0이다. 버튼 상태는 SpriteSwap이고 Image Tint는 흰색이다.
- NightSky가 화면 바깥까지 채우고, 지도 스크롤 Viewport는 남는 상하 예약 공간 없이 지도 높이를 쓴다. 32개 원본 별자리 그림·정적 선, 110개 원래 스테이지 좌표, 106개 진행선과 catalog의 contain/정점 계약을 유지한다.
- 지도 진입 시 선택 패널은 닫혀 있다. 별을 탭하면 잠긴 별도 먼저 선택되며 하단 패널이 0.20초 ease-out으로 올라온다. 재생 가능한 실제 클리어 기록이 있는 별에만 다시보기 버튼이 보인다. 패널 밖의 지도 입력은 계속 가능하다.
- 3종×4상태×4프레임의 별 몸체 Sprite를 stageId UTF-16 코드 단위 합 `% 3`으로 고정해 쓴다. 선택한 별 몸체 Image 하나만 unscaled 0.25초 간격으로 회전 프레임을 바꾼다. 선택 변경·패널 닫힘·모달·화면 퇴장·앱 일시정지/포커스 이탈·움직임 감소 때 F0으로 돌아간다. 번호·잠금 표시·선택 표시·연결선·입력 영역에는 회전이 적용되지 않는다.

## 임시 별길 목장 연결

- `NotebookProjectGateway`에 활성 `SW01`–`SW20` MapData 20개, 기존 GameplayCanvas/수집 별 Prefab, 기존 `StoryPreviewCanvas`를 직렬화해 연결했다. SW 입장은 기존 망원경 원장 승인 후 같은 씬에서 `PuzzleController`와 MapData bridge를 시작한다. 지도 복귀는 현재 씬을 다시 로드한다. 성공 기록은 세션의 실제 Won 상태에서만 `ANIMOL.Starway.Cleared.SWxx`로 쓴다.
- 클리어된 SW 단계의 다시보기는 기존 Ranch StoryRunner를 지도 위에 열며 `restoreOnStart=false`, `persistProgress=false`로 실행한다. 캠페인 입장/재시작 API를 호출하지 않고 망원경과 클리어 기록을 바꾸지 않는다. 공급자 없음·초기화 실패·중복 클릭·선택 변경 후 늦은 응답은 성공으로 처리하지 않는다. 아직 단계별 스토리 매핑이 없으므로 클리어된 SW 단계는 같은 Ranch 스토리를 임시 재생한다.
- `SW21` 이후와 다른 세 지역에는 활성 플레이 맵/구매·광고 공급자가 연결되지 않았다. 이 경로는 승인 실패로 닫혀 있다. 기존 망원경 최대 10/180초 회복/검증 광고 +5, 구매 검증과 중복 차감 정책, 인게임 재시작 게이지 정책은 변경하지 않았다.

## 실행한 검증

- Unity 6000.3.8f1에서 새 PNG Import와 C# 컴파일 성공.
- EditMode `NotebookIntegrationTests`: 4/4 통과. 활성 씬→Prefab 참조, 32개 그림/정적 선, 110개 원래 정점, 48프레임 Import/연결, SpriteSwap 및 실패 시 메뉴 복원을 검사했다.
- PlayMode `NotebookUiPlayTests`: 2/2 통과. 실제 활성 씬에서 지도 진입·복귀, 패널 기본 숨김, 선택/회전/잠금, 다시보기 자리, `SW01` MapData 입장·복귀, StoryRunner 승인/실패/중복/늦은 응답, 재화·스토리 저장 불변을 검사했다.
- 현재 활성 아트 검증 `ArtAssetTests` EditMode 4/4, `ArtQaPlayTests` PlayMode 1/1 통과.
- 360×640, 390×844, 445×773, 1440×900 GameView에서 상단 조작·패널을 확인하고 `sea-30` 마지막 별이 스크롤 끝에서 화면 안에 있음을 검사했다. 캡처는 `Docs/NeonMap_QA/Map_*.png`에 있다.
- 동봉 새 PNG 66/66은 ZIP SHA-256과 일치한다. `Docs/ART_PRESERVATION.json`의 원본 얼굴 리소스 71/71 해시는 변경되지 않았다. 별자리 원본 그림·선 파일을 수정하지 않았다.

## 아직 실행하지 못한 검증

- 실제 노치 기기 Safe Area, 물리 터치 및 실제 광고/IAP 공급자 연동은 실행하지 못했다. 네 가지 고정 GameView 해상도와 Unity Play Mode만 확인했다.
- 다른 세 지역의 실제 구매·광고 승인과 플레이 입장, 별길 목장의 단계별 서로 다른 스토리 재생은 해당 공급자/매핑이 없어 실행하지 못했다.

## 인게임 살구색 배경 수정

- 지도에서 `SW01`로 입장할 때 `NotebookRoot`만 비활성화되어, 같은 `William Notebook UI` Canvas의 형제인 전체 화면 `Static Paper Base`가 퍼즐 카메라 위에 남아 있었다.
- 승인된 퍼즐 세션이 생성되면 Notebook Canvas 전체를 비활성화한다. 게임 HUD Canvas는 별도이므로 그대로 표시되고, 지도 복귀 시 기존 `NotebookUI` 씬을 다시 로드한다.
- 열린 Unity Editor의 실제 Play Mode에서 수정 전 살구색 화면을 재현하고, 수정 후 `SW01` 월드와 HUD가 함께 보이는 것을 확인했다. `NotebookUiPlayTests.ClearedStarwayReplayAndEntryUseRealAssetsWithoutReplayCharges`는 Canvas와 Paper Base가 비활성인지 검사하며 통과했다. 수정 후 화면: `Docs/NeonMap_QA/Gameplay_SW01_after.png`.
