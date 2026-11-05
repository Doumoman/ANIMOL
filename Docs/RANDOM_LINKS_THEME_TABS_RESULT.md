# 랜덤 진입 연결선·테마 이동 버튼 적용 결과

2026-11-06, Unity 6000.3.8f1에서 동봉 `William_Random_Links_Theme_Tabs_Patch.zip`의 두 변경만 현재 ANIMOL 경로에 병합했다. 전체 패키지 설치, Build Fresh Scene, Apply Neon Map, 지도 재생성은 실행하지 않았다.

## 실제 적용 위치

- 활성 빌드 진입 씬: `Assets/ANIMOL/Scenes/Main/NotebookUI.unity`
- 사용 프리팹: `Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab`
- NotebookRoot: `William Notebook UI/Design 1080 Safe Area`
- 캠페인 버튼: `Main/Main Footer/Campaign Direct To Constellation` → 기존 `OpenCampaign`
- 지도: `Constellation Neon Map`
- 선택 패널: `Selected Star Transparent Neon Sheet`
- ScrollRect와 Viewport: `Full View Constellation Scroll`, Content: `Four Regions One Map`
- 실제 카탈로그: `Assets/ANIMOL/Data/UI/Notebook/catalog.json`
- 실제 서비스: 기존 `NotebookProjectGateway`
- 전환: 기존 `campaignSkyTransition` → `NotebookCampaignSkyTransition.cs`, 직렬화된 `transitionShader` → `Assets/ANIMOL/Art/UI/ConstellationMap/Shaders/UI_WilliamCampaignSkyTransition.shader`. 기존 GUID를 유지했다.

프리팹 모드에서 실제 Root를 선택하고 Edit Mode 메뉴 `ANIMOL > New Notebook UI > Apply Theme Navigation To Selected Notebook`을 실행했다. ScrollRect에 `NotebookMapThemeNavigation`을 추가하고 지도 아래 `Roman Theme Navigation`과 버튼 4개를 연결했다. Undo/Redo와 트랜잭션 내부 실패 복원을 실제 Prefab Stage에서 확인한 뒤 저장했다. 기존 씬 파일은 작업 전 바이트 그대로다.

기존 프리팹 문서 7,995개는 삭제하지 않았다. 새 문서는 40개이며 기존 문서에서 바뀐 부분은 지도 자식 목록, ScrollRect 컴포넌트 목록, Root의 `mapThemeNavigation` 참조 추가 3곳이다. 프리팹 미리보기에서 자동 계산된 CanvasScaler·배경 크기는 저장 전 기존 값으로 보존했다.

## 변경 동작

기존 별 27개와 3개 깊이층에서 진입당 연결 8개를 한 번만 뽑는다. 전용 RNG를 사용하고 자기 연결·중복·긴 선·과밀·교차·직전 구성 반복을 제한한다. 1.10초 전환, 0.62 지도 교체, 원본 별 좌표·크기·깊이·색·그림은 유지했다. 실제 지도 별자리와 진행선은 무작위화하지 않는다.

하단은 Safe Area 중앙 하단 +24, 984×240이다. 각 칸 246, 버튼 234×192, 좌우 6·상하 24 여백과 흰 Galmuri14 로마 숫자를 적용했다. 동봉 정적 PNG 5종을 Border32 / 9-slice / White Tint / Bilinear / Full Rect / Mipmap Off / Uncompressed / PPU100으로 사용한다.

| 버튼 | 실제 테마 | catalog startY |
|---|---|---:|
| I | 별길 목장 (`starway`) | 0 |
| II | 잔영 극장 (`theater`) | 9310 |
| III | 낙일 성소 (`observatory`) | 18620 |
| IV | 별너울 해안 (`sea`) | 27930 |

테마 버튼은 잠금 여부와 무관하게 실제 ScrollRect만 약 0.42초 이동한다. 콘텐츠 범위를 제한하고 재입력 시 현재 위치에서 다시 출발하며 드래그·휠은 자동 이동을 취소한다. 직접 스크롤에도 현재 테마 강조를 갱신한다.

첫 별 선택은 버튼 입력 즉시 차단 → 0.07초/14 상승 → 총 0.28초에 버튼 완전 숨김 → 기존 패널 0.20초 상승 순서다. 패널이 이미 열렸으면 정보만 갱신한다. 닫기는 버튼을 0.18초에 복귀시킨다. 이동 감소·포커스·일시정지·이탈·비활성화에서는 코루틴과 지연 콜백을 정리한다. 기존 선택 별의 4fps와 번호·잠금·선택 표시 동작을 유지했다.

현재 프로젝트의 Root 전체를 동봉 원본으로 덮어쓰지 않았다. 기존 입장 승인·중복 차감 방지·광고/결제 연결 검사·다시보기·메인 그림 넘김·설정 연출을 보존하고 테마 이동 관련 필드와 호출만 병합했다. 얼굴 스크롤 설정을 복원하지 않았다.

실제 Unity 검증 중 동봉 셰이더의 예약어 `point`를 기존 `starPoint` 이름으로 유지해 D3D 컴파일 오류를 해결했다. 기존 전환의 Material 정리도 유지했다. 새 메뉴의 생성 객체 Undo 등록 순서를 수정하고, Prefab Stage 내부 참조를 지우는 JSON 복원 대신 Unity Undo로 실패를 복원하도록 했다.

## 실행한 검증

| 실제 Unity 검사 | 결과 |
|---|---:|
| NotebookThemePatchTests | EditMode 3/3 |
| NotebookIntegrationTests | EditMode 5/5 |
| ArtAssetTests | EditMode 4/4 |
| NotebookThemePatchPlayTests | PlayMode 4/4 |
| NotebookUiPlayTests | PlayMode 5/5 |
| ArtQaPlayTests | PlayMode 1/1 |

EditMode 12/12, PlayMode 10/10. 결과 JSON은 [Validation/RandomLinksThemeTabs](Validation/RandomLinksThemeTabs)에 있다.

- 6개 화면 비율 × 128개 seed = 768개 구성의 8쌍·무중복·길이·차수·교차·직전 반복 방지·전역 RNG 보존을 검사했다.
- 실제 캠페인 버튼으로 6회 진입해 새 구성과 재생 중 고정을 확인했다. 기존 0.62 교체·1.10초 종료·중단 처리는 회귀 테스트를 통과했다.
- 실제 카탈로그 4개 시작점, 잠긴 지역 탐색, 연속 입력, 드래그·휠 취소, 수동 스크롤 강조, 움직임 감소 즉시 정착을 검사했다.
- 버튼 퇴장 뒤 패널 등장, 퇴장 중 다른 별 선택, 열린 패널의 즉시 정보 갱신, 닫기·재진입·포커스·일시정지·Root/지도 비활성화를 검사했다. 숨긴 rail의 비활성화, CanvasGroup raycast 차단, 버튼 비활성화와 코루틴 종료를 확인했다.
- 실제 GameView 360×640 / 390×844 / 445×773 / 1440×900에서 버튼·패널의 화면 경계를 검사하고 PNG를 검토했다. 390×844에서 상단 노치·하단 홈 영역을 가정한 Safe Area 기하도 검사했다. 이는 실제 기기의 `Screen.safeArea` 검증과 구분한다.
- 실제 서비스·스토리 에셋을 사용하는 기존 입장·다시보기·재화 검사와 선택 별 프레임·번호/잠금 검사도 통과했다.
- 기존 PlayMode 테스트의 첫 패널 확인은 새 0.28초 퇴장 대기를 반영하도록 갱신했다. 기존 저장된 클리어 기록 때문에 초기 잠금을 가정하는 테스트가 한 번 실패했으며, 진행/지갑을 백업한 초기 상태에서 재실행해 5/5를 확인했다. 이후 진행 키 70개와 원래 지갑·설정 2개 키를 복원하고 동일성을 확인했다. 사용자 저장 값은 결과 문서에 포함하지 않았다.

실제 Prefab Stage 메뉴의 적용·Undo·Redo·내부 실패 복원을 확인했고, 별도의 임시 테스트 씬에서도 같은 회귀 검사를 통과했다. 생성기는 Play Mode에서 실행하지 않았다. Unity Import, C# 컴파일, Editor 그래픽 API의 셰이더 컴파일을 통과했다.

## 보존과 미실행 항목

작업 전 29,407개 기존 파일과 15,322개 기존 `.meta`를 SHA-256으로 비교했다. 기존 씬·카탈로그·아트·서비스 코드·게임플레이·SW00 원본과 GUID는 유지했다. 기존 사용자 미커밋 변경은 이번 커밋에 포함하지 않는다. TMP가 검증 중 저장한 동적 글리프 캐시는 Editor 종료 후 작업 전 폰트 바이트로 복원했다. 자세한 비교는 [preservation.json](Validation/RandomLinksThemeTabs/preservation.json)에 있다.

실제 Android/iOS 기기, 노치 기기의 OS Safe Area, GLES/Metal GPU 성능·발열·프레임 시간, 실제 구매·광고·서버 거래는 실행하지 않았다. 보관된 CommercialPolishPolicyTests는 현재 활성 테스트가 아니므로 실행하지 않았으며, 위 ArtAssetTests/ArtQaPlayTests 실행과 구분한다. 브라우저 프리뷰·동봉 구문 검사 결과를 실제 Unity 검증 수에 넣지 않았다.
