# 별길 목장 연결 장식 V02 원근 보정 R2 적용 결과

2026-10-27, Unity 6000.3.8f1의 실제 ANIMOL 프로젝트에서 확인했다. 제공 ZIP의 `installationRevision=2`와 실사용 PNG 362개를 기존 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02`에 반영했다. 별도의 V02 폴더는 만들지 않았다. 기존 20개 변경 PNG의 `.meta` GUID와 생성 카탈로그 GUID를 유지했다.

## 적용과 바인딩

- 원본 ZIP의 파일 441개 SHA-256과 ZIP 무결성을 먼저 확인했다. 적용 후 호스트 호환을 위해 수정한 C# 3개 파일의 해시를 패키지 체크섬 목록에 반영했으며, 최종 폴더의 체크섬 441/441개가 일치한다. `manifest.json` 자체의 SHA-256은 `manifest_sha256.txt` 및 `RanchConnectedDecorV02BoardSnapshot.ArtManifestSha256`의 `2ac1cfca3c42a1a332cc8e987272621ad44893ef76e7213e11bcc2d7b9b34727`와 일치한다.
- 기존 패키지의 [카탈로그](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/RanchConnectedDecorV02Catalog.asset)를 선택된 manifest로 다시 빌드했다. `Sprite` 362개, 연결형 6종, 다칸 장식 12종, 한 칸 소품 8종, 카탈로그 그룹 20개다. 연결형 PNG는 변경하지 않았다.
- 실제 Unity 임포트의 362개 `Sprite.rect`, PPU 256, pivot을 manifest와 대조했다. 12개 다칸 장식과 8개 한 칸 소품의 `logicalRect = footprintCells × 256` 및 비중앙 지면 pivot을 확인했다. 큰 캔버스를 칸 수나 Collider로 환산하지 않았다.
- [StarGarden 테마](../Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset)와 `starway_Theme.asset`은 동일한 GUID의 카탈로그와 핀 에셋을 이미 참조했다. 새 카탈로그를 같은 경로와 GUID로 재생성해 기존 테마 및 배치를 그대로 재바인딩했다. `_Generated`, Data, Scenes, Prefabs에서 이전 manifest 핀 참조는 발견되지 않았다.
- 실제 [맵 제작기](../Assets/ANIMOL/Scripts/Editor/PuzzleMapEditor.cs)는 R2 카탈로그에서 목장 장식 선택지 26개를 중복 없이 반환한다. 기존 별도 바람개비 선택지 1개도 유지한다. [QA 맵](../Assets/ANIMOL/Data/Puzzle/QA/StarwayConnectedDecorQA.asset)의 저장 장식 59개(연결 셀 93개, 그룹 4개), 사용 중인 `1-1.asset`의 저장 장식 17개(연결 셀 10개, 그룹 6개)를 읽기 전용 임시 씬에서 `PuzzleMapView.Bind`로 실제 렌더링했다. 각각 그룹 렌더러가 4개, 6개였으며 옛 V01 그룹 그림은 남지 않았다. `1-1.asset`은 기존 사용자 미커밋 파일이므로 수정하거나 커밋하지 않았다.
- [All26 검사 프리팹](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/InspectionBoards/All26/RanchDecorAll26.prefab)의 그룹 20개가 각각 렌더러 하나, 올바른 카탈로그 스프라이트 하나, `localScale=(1,1,1)`로 배치됐으며 Collider는 0개다. 헛간 3×2, 닭장·수레 2×2, 정자·가판대 3×2, 작업대 2×1의 *시각 논리 격자*는 manifest와 일치한다. 기존 헛간의 게임 점유 3×1은 [호스트 매핑](../Assets/ANIMOL/Scripts/Runtime/RanchConnectedDecorV02Host.cs)대로 유지한다.
- 예시 JSON은 검사 보드에만 사용했다. 기존 캠페인 스테이지를 예시 JSON으로 덮지 않았다. [Stage](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/InspectionBoards/Stage/RanchDecorStage.unity)와 [All26](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/_Generated/InspectionBoards/All26/RanchDecorAll26.unity) 씬·프리팹·BoardAsset을 Unity에서 다시 생성했다. Stage는 연결 셀 65개/그룹 20개, All26은 연결 셀 96개/그룹 20개다.

## 프로젝트 호환 수정

- 제공 R2의 `RanchConnectedDecorV02Binding`과 `RanchConnectedDecorV02SnapshotSource`에서 빠진 `[ExecuteAlways]`를 복구했다. 빠진 상태에서는 편집·비활성화·풀 재사용 관련 EditMode 테스트 4개가 실패했다. 복구 후 60/60 통과했다.
- [보드 빌더](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/Editor/RanchConnectedDecorV02BoardTools.cs)는 Unity 6000.3.8f1에서 새 additive 씬을 활성 씬으로 전환하지 못했다. 생성할 루트·카메라를 해당 씬으로 직접 이동하게 고쳐 빌드를 완료했고, 빌드 보고서의 `installationRevision`도 2로 수정했다.
- 맵 로드·편집·Undo·풀 재사용은 기존 [바인딩](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/Runtime/RanchConnectedDecorV02Binding.cs)과 [컨트롤러](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/Runtime/RanchConnectedDecorV02Controller.cs)를 유지한다. 장치 Visual, 판정 Collider, 코스트 코드는 수정하지 않았다.

## 실제 검증

| 항목 | 결과 |
|---|---|
| Unity Editor 임포트 및 C# 컴파일 | 실행, 컴파일 오류 0 |
| 원본 ZIP 무결성 / 설치본 체크섬 | 각각 441/441 일치 |
| 카탈로그 생성·무결성 검사 | 실행, 362 Sprite / 20 그룹 / 새 핀 통과 |
| 실제 Sprite 캔버스·PPU·pivot | Unity에서 362/362 일치 |
| 검사 보드 생성 및 프리팹 배율·Collider | 실행, 2개 보드와 20개 그룹 렌더러 확인 |
| 실제 테마·맵·제작기 바인딩 | `StarGarden`, `starway_Theme`, QA 맵, `1-1`, 제작기 선택지 확인 |
| `RanchConnectedDecorV02` EditMode | 60/60 통과: 계획·호스트·보드·편집 수명주기 포함 |
| `RanchConnectedDecorV02PlayTests` PlayMode | 2/2 통과: 생성 씬 렌더링 및 실제 프로토타입 갱신·재사용·코스트 |
| `PuzzleMapTests` EditMode | 53/54 통과. 사용자 미커밋 `PuzzleMapf`의 필수 목표 장치가 맵 밖에 있어 배치 검증 1개 실패. 해당 맵은 수정하지 않음 |
| `ArtAssetTests` EditMode | 3/4 통과. 별도 `StarSwellCoastV01Device` 스프라이트 누락으로 1개 실패 |
| `ArtQaPlayTests` 추가 실행 | 테스트 러너의 씬 저장 단계 `InvalidOperationException`으로 중단·취소. 완료 결과로 집계하지 않음 |

## 가림과 남은 확인

[실제 QA 맵의 헛간과 캐릭터 캡처](QA/RanchConnectedDecorR2/actual_barn_overlap.png)에서 플레이 배율의 접지점과 크기 관계를 확인했다. 현재 [호스트](../Assets/ANIMOL/Scripts/Runtime/RanchConnectedDecorV02Host.cs)는 헛간 전체를 정렬 순서 41, 다른 장식 전체를 15로 보낸다. 캐릭터 렌더러는 순서 40이다. 따라서 헛간은 앞에 고정되고 다른 장식은 뒤에 고정된다. 단일 스프라이트만으로 헛간의 전면 울타리와 후면 지붕 사이에 있는 캐릭터를 위치별로 올바르게 가릴 수 없다. 특히 헛간·닭장·양봉장·정자·가판대·나무/꽃 구역·수레의 전면 울타리/기둥/짐과 후면 지붕/수관/벽을 별도 전경·후경 그림으로 나누는 범위가 필요하다. R2 제공본에는 그 분리 스프라이트가 없으므로 자동 전후 가림이 완료됐다고 표시하지 않는다.

연결형의 마스크 생성과 PlayMode 경로는 통과했지만, 직선·코너·T·십자·끝·잔디 안쪽 구멍의 모든 조합을 사람이 화면에서 하나씩 확인한 검사는 실행하지 않았다. 모든 장식에 대한 앞·뒤·좌·우 캐릭터 가림, 길 침범·부유 여부와 상자·기둥·문·벤치의 전 조합 실측도 완료되지 않았다. [오프라인 검사 이미지](../Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/Inspection/CATALOG_26_DESIGNS.png)는 이 Unity 실측을 대체하지 않는다. 패키지 내부의 `actualUnityImport` 등 `NOT_RUN` 표시는 제공 환경의 기록이며, 이번 프로젝트의 실행 결과는 위 표에 따로 기록했다.
