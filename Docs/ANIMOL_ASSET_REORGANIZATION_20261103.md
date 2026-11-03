# 활성 자산 정리 결과 (2026-11-03)

## 범위와 기준

- Unity Build Settings에서 켜진 세 씬을 시작점으로 GUID 의존성을 추적했다: `Scenes/Main/NotebookUI`, `Scenes/QA/ANIMOL_Starway_Art_QA`, `Scenes/Gameplay/ANIMOL_Echo_Theater`.
- 이 세 씬에서 참조되지 않는 아트는 삭제하지 않고 저장소의 `Archive/UnusedArt`로 이동했다. QA·프로토타입·제작기에서만 참조하는 아트는 활성 기준에 포함하지 않았다.
- 각 파일과 `.meta`를 함께 옮겨 GUID를 보존했다. 사용자에게서 이미 수정된 맵 데이터, 폰트, TwilightSea 임포터 `.meta`의 작업 트리 내용도 보존했다.

## 새 활성 구조

| 역할 | 위치 |
|---|---|
| 캐릭터 애니메이션·일러스트 | `Assets/ANIMOL/Art/Characters/<theme>/<character>/Animations/<state>` 및 `Illustrations` |
| 스토리 장면 아트 | `Assets/ANIMOL/Art/Story/<theme>/<scene>`; 현재 빌드에서 쓰는 스토리 원화는 없음 |
| UI 아트 | `Assets/ANIMOL/Art/UI/{InGame,Main,ConstellationMap,Settings}` |
| 맵 아트 | `Assets/ANIMOL/Art/Maps/<theme>/{Tiles,Devices,Decorations}`; 사용되는 표식이 생기면 `Markers` |
| 셰이더·머티리얼 | `Assets/ANIMOL/Art/Shaders` |
| 지도 데이터 | `Assets/ANIMOL/Data/Puzzle/MapData/<theme>` |
| 프리팹 | `Assets/ANIMOL/Prefabs/{Gameplay,Maps,UI}` |
| 메인 씬 | `Assets/ANIMOL/Scenes/Main/NotebookUI.unity` |
| 스크립트 | `Assets/ANIMOL/Scripts`; 기존 어셈블리 경계와 Editor 구분은 유지 |

보관한 아트 파일은 원래의 상대 경로를 `Archive/UnusedArt/Assets/...` 아래에 유지한다. 제작 자료는 `Archive/UnusedArt/Authoring`, 사용하지 않는 폴더 메타는 `Archive/UnusedArt/FolderMetas`에 보관했다. 활성 아트 6,243개는 모두 빌드 씬 GUID 그래프에 속한다. 사용하지 않는 아트 11,855개를 보관했다.

## Unity 연결과 검증

- `NotebookInstaller`는 새 PNG 경로와 `Scenes/Main/NotebookUI.unity`, `Prefabs/UI/Main/NotebookCanvas.prefab`에 생성한다. 실제 생성기를 다시 실행했다.
- 활성 Notebook 씬은 별 노드 110개, 지역 4개, 카탈로그와 `NotebookProjectGateway`, 새 프리팹, 원본 스크롤 패턴 텍스처를 참조한다. 누락 스크립트는 0개였다.
- 원본 스크롤 자료 71개의 SHA-256이 `Docs/ART_PRESERVATION.json`과 일치한다. 파일 내용은 바꾸지 않고 경로만 갱신했다.
- Unity 6000.3.8f1 재임포트와 C# 컴파일 통과. Notebook 씬 Play Mode 진입·종료 후 콘솔 오류·경고 0개. `ArtAssetTests` EditMode 4/4, `ArtQaPlayTests` PlayMode 1/1 통과.
- GameView 캡처 명령은 30초 시간 제한을 반환했지만, 실제 Play Mode 화면 PNG를 기록했다. 390×844 세로 화면에서 캐릭터 손발과 메인 버튼이 화면 안에 표시되는 것을 확인했다. 다른 해상도의 시각 확인 결과로 확대 해석하지 않는다.

## 보관 자산의 제한

기존 QA·프로토타입 씬과 제작기에서만 사용하던 아트는 현재 Unity `Assets` 밖에 있다. 해당 씬 또는 제작기를 다시 쓰려면 필요한 원화와 `.meta`를 보관 위치에서 복원하고 참조를 다시 검증해야 한다. 현재 빌드 씬의 참조만 보존한다는 사용자 선택에 따른 범위다.


## 2026-11-03 맵 데이터 경로 복원

사용자 요청으로 지도 데이터 58개와 `StarwayMapPack` 전체 자료·스크립트 120개를 원래 `Assets/ANIMOL/Data/Puzzle/MapData` 및 `Assets/ANIMOL/StarwayMapPack` 경로로 되돌렸다. 파일·`.meta`·원래 폴더 `.meta`를 함께 복원했고, 이동 전후 371개 파일의 SHA-256이 같았다. 맵 파일과 기존 미커밋 수정은 새 분류 대상에서 제외한다.
