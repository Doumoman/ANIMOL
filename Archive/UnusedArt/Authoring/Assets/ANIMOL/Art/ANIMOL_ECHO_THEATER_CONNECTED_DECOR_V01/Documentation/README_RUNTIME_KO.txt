ANIMOL 잔영 극장 연결 장식 V01 — 런타임 / Editor 소스 계약

범위
이 코드는 장식 시각물만 배치한다. 공통/극장 장치, 책장·도미노·셔터 애니메이션,
플레이어/충돌/턴/저장 ID를 바꾸지 않는다. 새 SpriteRenderer에는 Collider/Rigidbody를
만들지 않는다. 기존 목장 연결 장식과 namespace 및 카탈로그가 분리되어 함께 설치할 수 있다.
원 Unity 프로젝트가 없어 실제 임포트·C# 컴파일·TestRunner·Prefab 바인딩·Play는 미실행이다.
source_review.json의 PASS_SOURCE_REVIEW_ONLY는 컴파일 성공 표시가 아니다.

정확한 종류
연결형6종:
velvet_aisle_barrier / carved_stage_balustrade / prop_shelf_run /
velvet_curtain_screen / brass_footlight_strip / ivy_planter_strip
각16개 n00.png~n15.png: N1/E2/S4/W8, +Y가 북쪽이다. 대각 접촉만으로 연결하지 않는다.
연결 그룹에는 여러 칸에 공유하는 구역/띠 ID를 준다. 칸마다 다른 instance ID를 넣지 않는다.
같은 family+connectionGroup+logicalLayer+heightKey인 인접 칸만 연결한다.
처음2종에만 periodic_post.png를 선택적으로 선언할 수 있다. 이 이미지가 없으면 요청하지 않는다.
보조 기둥은 마스크5(NS)/10(EW)의 긴 직선에만3또는4칸 간격으로 선택한다.
양옆 이웃도 같은 직선일 때만 허용해 끝/모서리/T/십자 옆에 추가 기둥이 겹치지 않는다.

큰 장식12종(한 개체를 한 번만 생성):
grand_piano_3x2(3x2), audience_bench_3x1(3x1), ticket_booth_2x2(2x2),
costume_rack_3x1(3x1), vanity_dressing_2x1(2x1), phonograph_table_2x1(2x1),
scenery_flat_3x1(3x1), stacked_props_2x2(2x2), mask_display_2x1(2x1),
puppet_storage_2x2(2x2), backstage_spotlight_duo_2x1(2x1), stage_steps_2x1(2x1).
독립 소품8종(각1x1): ticket_stand / folded_chair / music_stand / tall_candelabra /
mask_pedestal / script_pile / sandbag_stack / flower_urn.
이20종 모두 manifest.groups와 TheaterDecorGroupPlacement를 사용한다.
26종은 실제 디자인 종류 수이며96개 연결 마스크나 보조 기둥을 다른 장식 종류로 세지 않는다.

소스 스키마
패키지 루트 이름: ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01
namespace: ANIMOL.Art.TheaterConnectedDecorV01
manifest.json: schemaVersion=1, packageId, assets[], groups[]
asset: file/sha256/width/height/ppu/pivot[]/logicalRect[]/footprintCells[]/family/kind.
PPU 필드의 실제 JSON 키는 ppu이며 값은256이다. pixelsPerUnit 키로 바꾸지 않는다.
logicalRect는 PNG 왼쪽 위 기준[x,y,width,height], pivot은 Unity 왼쪽 아래 기준 정규화 값이다.
연결형은320x384, logicalRect[32,96,256,256], pivot[0.5,160/384]이다.
큰 장식/독립 소품은 각 이미지의 명시된 캔버스·여백을 따른다. 모든 pivot은 논리 지면 중심이다.
group: key/file/footprintCells. 대응 asset.family는 group.key와 같고,
asset.kind는 큰 장식이면 group,1칸 독립 소품이면 point다.
연결 마스크 asset.kind=connected_tile, 보조 기둥 asset.kind=periodic_post.
필수116개(96연결+12큰 장식+8소품), 선택 보조 기둥0~2개만 카탈로그에 허용한다.
manifest_sha256.txt는 manifest.json 파일의 정확한 바이트 SHA-256이다.

Editor 생성
아래 호출의 Editor 경로/프로젝트 경로/Assets 경로는 실제 환경에 맞게 대입한다.
"<UNITY_EDITOR_PATH>" -batchmode -projectPath "<PROJECT_PATH>" -executeMethod ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01CatalogBuilder.BuildFromCommandLine -theaterDecorManifest "Assets/<실제경로>/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/manifest.json" -quit -logFile "<PROJECT_PATH>/Logs/theater-connected-decor-v01-build.log"
또는 manifest.json 선택 후 Tools/ANIMOL/Theater Connected Decor V01/Build selected manifest.json.
생성 카탈로그: <패키지 루트>/_Generated/TheaterConnectedDecorV01Catalog.asset
Builder가 실제 프로젝트의 Sprite GUID/fileID를 수집해 생성한다. 임의 GUID의 가짜.asset은 없다.
생성 보고서 decor_catalog_build_report.json은 카탈로그 생성만 기록하며 게임 바인딩 성공은 뜻하지 않는다.

Import 경계
Importer는 정확한 새 패키지 루트 및 현재 manifest allowlist/SHA 안의 PNG만 Sprite로 인정한다.
sources/Inspection/허용하지 않은 구 PNG는 Default Texture이며 Sprite fallback으로 쓰지 않는다.
Sprite Single/FullRect, custom pivot, PPU256, Bilinear/Clamp/uncompressed/noMip,
NPOT None/no fallback physics shape를 적용한다. 이 패키지 이미지에만 플랫폼 override를 비운다.
다른 테마나 구 전역 Importer를 일괄 변경하지 않는다. 가짜 GUID를 넣거나 .meta를 지우지 않는다.

호스트 연결 API
1. 실제 게임의 장식용 Sorting Layer 이름과 생성 카탈로그, manifest SHA를 준비한다.
   controller.Configure(catalog, pinnedManifestSha256, actualSortingLayerName)
2. 실제 맵 편집/생성/로드/되돌리기 후 전체 장식 보드 스냅샷을 전달한다.
   controller.SetLayout(networkCells, groupPlacements, worldOrigin, cellSize,
       hostAllowDecorCell, seed:0, postSpacing:4)
   hostAllowDecorCell은 필수 순수 조회 함수다. 승인 중 예약/삭제/저장/턴 진행 등 부수효과를 내지 않는다.
   카메라에 보이는 청크만 보내면 연결 끝이 잘못 선택되므로 항상 전체 장식 상태를 보낸다.
3. 빈 스냅샷 또는 ClearLayout()으로 이 컨트롤러가 소유한 시각물만 정리한다.
   일반 OnEnable은 이전 레이아웃을 자동 부활시키지 않는다. 풀 재사용 시 Configure+SetLayout을 다시 준다.

레코드
TheaterDecorNetworkCell: x,y,logicalLayer,heightKey,sortingOrder,worldZ,family,connectionGroup.
TheaterDecorGroupPlacement: placementId,groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
placementId는 한 큰 장식/소품당 고유하다. anchor는 논리 점유의 왼쪽 아래 칸이다.
WorldOrigin은 논리(0,0)칸의 왼쪽 아래이며 cellSize는 실제 월드 한 칸 크기다.
네트워크 중심=origin+(x+0.5,y+0.5)*cellSize.
그룹 중심=origin+(anchorX+width/2,anchorY+height/2)*cellSize.
worldZ는 origin.z에 더하는 장식 시각 깊이이며 논리 높이/연결 키를 대체하지 않는다.
이미지 전체 크기를 한 칸으로 나누거나 alpha bounds로 fit하지 않는다.
상위 계층은 회전이 없고 XY 배율이 양수여야 한다. 원본 PPU에 cellSize 배율만 한 번 적용한다.
그룹 점유 각 칸을 모두 조회하고 하나라도 거부하면 부분 생성하지 않는다. 전체 그룹당 Renderer1개다.
이 주장은 장식끼리의 논리 레이아웃 검증이며 게임 Collider/통행 점유에 새 블로커를 넣지 않는다.
서로 다른 layer/height의 시각 겹침도 호스트의 명시 승인 이후만 허용한다.

아래로 증가하는 행 좌표 변환
TheaterConnectedDecorV01Planner.FromRowDownCell(col,row) => (col,-row)
FromRowDownGroupBottomLeft(leftCol,topRow,heightCells) => (leftCol,-topRow-heightCells+1)
TheaterConnectedDecorV01Controller.RowDownTopLeftToWorldOrigin(boardTopLeft,cellSize)
=> 기존 보드 왼쪽 위 지점에서 Y를 한 칸 내린 worldOrigin.
이미 존재하는 실제 그리드 변환이 있다면 확인 후 한 번만 변환한다.

실패 정책
미정 키/누락 Sprite/SHA 불일치/틀린 점유/허용되지 않은 칸은 명확한 실패다.
이전 장식 PNG나 임의 기본 이미지로 대체하지 않는다.
SetLayout 실패 시 이 컨트롤러의 기존 시각물도 숨기고 정리한다. 이전 화면 보존 정책이 아니다.
게임 규칙상 무효 배치/입력은 호스트에서 먼저 거절하고 SetLayout을 호출하지 않는다.
본 코드는 이전 시각물 숨김→지연 Destroy 순서를 지켜 한 프레임 겹침을 피한다.

실제 프로젝트에서 필요한 검증(현재 모두 NOT_RUN)
임포트/컴파일/빌드 오류0, 실제 카탈로그 GUID/fileID, 현재 생성 경로/풀/로드/에디터 통합,
2/3/4칸·긴 선·모서리/T/십자·대각만 접촉·붙이기/삭제/재로드,
12큰 장식과8소품의 크기/발위치/한 번 생성/호스트 점유 승인/겹침 거부,
비활성 생성 후 활성/재사용/씬 재진입, 누락 키 음성 검사, 실제 모바일 화면 가독성,
기존 책장·도미노·셔터·장치 애니메이션/턴/충돌/다른 테마 회귀 없음.
Tests~의 NUnit 테스트16개는 제공만 한 상태이며 실제 TestRunner에서 옮겨 실행해야 한다.
