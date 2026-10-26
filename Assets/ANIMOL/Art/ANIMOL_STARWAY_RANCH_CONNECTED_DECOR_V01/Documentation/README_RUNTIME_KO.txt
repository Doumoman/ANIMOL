별길 목장 연결 장식 V01 — 코드 접합

이 코드는 새 장식 팩 전용이다. 기존 목장/다리 난간/장치/병아리/플레이어/충돌/턴/세이브 ID를 바꾸지 않는다. 실제 프로젝트가 없으므로 C# 컴파일, Unity 임포트, NUnit, Prefab 바인딩과 Play Mode는 모두 NOT_RUN이다.

새 폴더 ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01을 Assets 아래에 설치한다. manifest.json의 assets[]는 file,width,height,ppu,pivot,logicalRect,footprintCells,sha256,family,kind를 사용하고 groups[]는 key,file,footprintCells를 사용한다. 모든 수치와 실제 PNG 바이트가 일치해야 한다. manifest_sha256.txt는 manifest.json의 실제 바이트 SHA256이다. 기존 V12의 custom padding/pivot를 새 320/32 규격으로 덮어쓰지 않는다.

무인자 Editor 진입점:
ANIMOL.Art.RanchConnectedDecorV01.RanchConnectedDecorV01CatalogBuilder.BuildFromCommandLine
필수 인자:
-ranchDecorManifest Assets/<실제부모>/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01/manifest.json
결과:
_Generated/RanchConnectedDecorV01Catalog.asset 및 decor_catalog_build_report.json

Builder는 새 팩의 정확한 PNG만 재임포트하고 실제 SHA/캔버스/PPU/pivot/GUID/fileID/ground footprint를 대조한다. Sprite Single/FullRect/Bilinear/무압축/noMip/Clamp/NPOT None, physics fallback false를 사용한다. 새 팩의 Android/iPhone/Standalone/WebGL 개별 texture override를 비운다. 원화·Inspection·미선언 PNG는 Sprite 후보에 넣지 않는다. 전역 SaveAssets/Refresh/씬 저장/Build Settings 변경을 하지 않는다. 현재 catalog 검사는 빌드 직전에도 실행된다.

런타임:
1. Configure(latestCatalog, manifestSha256, actualSortingLayerName)를 호출한다.
2. 실제 호스트의 전체 장식 보드로 SetLayout(networkCells, groupPlacements, worldOrigin, cellSize, hostAllowDecorCell, seed=0, postSpacing=4)를 호출한다.
3. 지도 편집/배치/삭제/undo/load 후에 전체 스냅샷을 다시 제공한다. 화면에 보이는 청크만 제공하면 연결 끝이 잘못 선택된다. 매 프레임 호출할 필요가 없다.
4. ClearLayout()은 이 컴포넌트가 소유한 새 장식 renderer만 지운다. Configure는 catalog 설정을 새로 잡고 기존 소유 renderer를 먼저 숨겨 재설정 실패 시 이전 그림이 남지 않게 한다.

RanchDecorNetworkCell: x,y,family,connectionGroup,logicalLayer(int),heightKey(int),sortingOrder,worldZ.
RanchDecorGroupPlacement: placementId(유일),groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
logicalLayer는 연결·장식 claim용 논리 층이며 Unity Sorting Layer 이름과 별개다. heightKey는 정수 연결 평면이며 float Z의 오차로 연결하지 않는다. worldZ와 sortingOrder는 호스트가 주는 시각 배치 값이다.

연결은 같은 family+connectionGroup+logicalLayer+heightKey에서만 일어난다. 울타리/낮은 돌담은 N1,E2,S4,W8의16종, 화단은 N1,NE2,E4,SE8,S16,SW32,W64,NW128의256종이다. 직선 n05/n10에만 추가 기둥을 둘 수 있고, 고정 seed/세계 좌표의3또는4칸 간격 후보를 고른 뒤 양옆 즉시 이웃도 같은 직선 mask인지 확인한다. 끝/모서리/T/교차의 기본 기둥 옆에 추가 기둥을 붙이지 않는다. 화단 꽃 accent는 kind=meadow_flower_accent인 현재 원화만 정렬하여 같은 입력에 항상 같은 약1/5밀도로 고른다.

큰 장식은 bottom-left anchor 하나와 선언된 전체 footprint를 사용한다. 예를 들어 3×2 장식은6개 cell을 검증/claim하지만 SpriteRenderer는1개다. renderer 중심은 anchor+(w/2,h/2)이며 전체 PNG를 한 칸에 맞추거나 alpha bounds로 축소하지 않는다. PPU256과 logicalRect 중심 pivot를 유지하고 셀 크기에만 균일한 world scale을 적용한다. 꽃/기둥 accent는 이미 claim된 네트워크 cell의 시각 overlay이며 추가 gameplay cell이 아니다.

hostAllowDecorCell은 모든 claim에 대한 순수 승인 조회다. 이 callback에서 실제 예약/점유/충돌/코스트를 변경하지 않는다. 하나의 group cell이라도 거절되면 전체 새 plan이 거절되며 부분 배치가 반환되지 않는다. 같은 논리층/평면의 중복은 거절한다. 다른 층의 같은 좌표도 callback이 명시적으로 허용해야 한다. 실제 호스트는 성공한 전체 CurrentPlan을 받은 뒤 자신의 장식 예약을 한 번에 확정한다. 이 코드는 플레이어 통행 가능 여부를 직접 바꾸지 않는다.

좌표:
Unity grid +Y=North. worldOrigin은 grid cell(0,0)의 bottom-left다.
FromRowDownCell(column,row)는(column,-row), FromRowDownGroupBottomLeft(left,top,height)는(left,-top-height+1)이다.
호스트 원점이 보드 top-left라면 RowDownTopLeftToWorldOrigin(boardTopLeft,cellSize)로 먼저 Y를 한 셀 내려 worldOrigin을 구한다. 이렇게 row0 renderer 중심은 top-left에서(+.5,-.5)셀이 된다.

Pool:
disable 시 소유 시각을 즉시 숨기고 구성/명시 배치 상태를 지운다. 다시 사용할 때 최신 host 상태로 Configure와 SetLayout을 호출한다. 비활성 객체에 Configure+SetLayout을 미리 호출했다면 첫 Awake/enable이 그 새 명시 상태를 지우지 않는다. 단순 re-enable로 과거 layout을 자동 복구하지 않는다. 실패/빈 스냅샷/undo에서 이전 그림을 fallback으로 남기지 않는다. Destroy가 프레임 끝으로 지연되더라도 이전 renderer는 먼저 비활성화한다.

Tests~의10개 NUnit 계약 검사는 실제 프로젝트의 EditMode 테스트 어셈블리에 옮겨 실행한다. 현재 제공 상태는 테스트 소스뿐이며 실제 실행 성공으로 보고하지 않는다.
