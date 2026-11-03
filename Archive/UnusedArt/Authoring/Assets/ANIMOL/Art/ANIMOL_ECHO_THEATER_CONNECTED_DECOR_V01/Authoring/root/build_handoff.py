from pathlib import Path
import json

W = Path(__file__).resolve().parent
O = W / 'handoff_exports'
O.mkdir(parents=True, exist_ok=True)
design = json.loads((W / 'catalog_design.json').read_text())
rows = []
for category in ['networks', 'groups', 'points']:
    for a in design[category]:
        size = '연결형 1×1 / 이웃에 따라 연속' if category == 'networks' else '×'.join(map(str, a['footprintCells']))
        rows.append(f"- {a['nameKo']} / {a['key']} / {size}")
readme = '''ANIMOL 잔영 극장 — 연결 장식 확장 V01

장식 26종: 연결형6 + 여러 칸짜리12 + 한 칸 소품8.
색상·마스크·보조 기둥 변형을 별개의 장식 종류로 세지 않는다.
실제 PNG 수와 파일별 크기/PPU/pivot/SHA는 manifest.json에 기록한다.

이웃을 공유하는 장식은 끝/직선/모서리/T/십자 상태를 선택한다. 큰 장식은 선언된
2×1·3×1·2×2·3×2 공간을 유지하고 placementId마다 한 번만 생성한다.
작은 소품은 억지로 이어 붙이지 않고 큰 소품과 색/선/마감을 공유한다.
표현은 굵고 각진 남색 윤곽, 크림·황동·버건디·자주색의 플랫 셀 셰이딩이다.
모든 신규 원화는 built-in imagegen으로 제작했고 원본/프롬프트를 동봉한다.

설치
1. 통합 ZIP을 한 상위 폴더에 푼다. Assets/Art/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01
   형태처럼 패키지 루트 이름을 보존한다. 폴더를 이중 중첩하지 않는다.
2. UNITY_CLI_KO.txt 전체를 원 Unity 프로젝트에 연결된 CLI에 전달한다.
3. 실제 프로젝트에서 생성기를 실행해 _Generated/TheaterConnectedDecorV01Catalog.asset을
   만들고 실제 맵 생성/편집/로드 경로의 장식 어댑터를 연결한다. ZIP 해제만으로 바인딩이 되지 않는다.
4. sources/Authoring/Inspection 이미지는 런타임 Sprite로 등록하지 않는다. 새 manifest의
   allowlist에 있는 decor/ PNG만 런타임 카탈로그에 등록한다.

수정 범위
이번 ZIP은 잔영 극장의 장식 추가·교체 전용이다. 기존 극장 전체 V03를 다시 덮어쓰지 않는다.
현재 설치된 책장·도미노·장막 장치 애니메이션 및 공통 장치 수정본을 유지한다. 이 ZIP의
장막 파티션/조명 소품/인형 보관함은 장식이고 작동 장치의 게임플레이 ID와 같지 않다.
기존 배경·타일·연결 하부는 예시의 배경 맥락이며 새 런타임 PNG로 복제하지 않는다.
다리 경계 난간은 기존 terrain/bridge owner가 이미 생성한다. 새 객석 울타리를 중복 생성하지 않는다.

배치 계약
- +Y=북쪽, cardinal mask N1 E2 S4 W8. 다른 family/connectionGroup/layer/height와 연결하지 않는다.
- 연결된 한 구역은 공유 connectionGroup 하나를 쓴다. 각 칸의 인스턴스ID를 group으로 쓰지 않는다.
- 큰 장식과 한 칸 소품 모두 고유 placementId + groupKey + 왼쪽 아래 anchor를 갖는다.
- PPU256, logicalRect/pivot은 지면 기준이다. PNG 전체·alpha bounds를 한 칸에 맞춰 축소하지 않는다.
- 장식 점유는 호스트의 장식 허용 영역 검사용이다. 기존 게임 Collider·이동·턴·저장 규칙을 바꾸지 않는다.
- hostAllowDecorCell은 순수 조회이고 전체 점유 칸을 확인한다. 점유를 그 콜백 안에서 예약하지 않는다.
- SetLayout에는 전체 장식 보드 스냅샷을 전달한다. 화면/청크 일부만 전달하면 이웃이 끊어진다.
- 2/3/4칸 및 긴 연결은 하나의 경로로 이어진다. 선택 보조 기둥은 첫2계열의 긴 직선에만
  월드 좌표에 고정된 간격으로 나타나며 끝·모서리·교차로 옆에는 추가되지 않는다.
- 셀 추가/삭제, 맵 로드, undo/redo가 확정될 때 재구성한다. 매 프레임 랜덤화나 자동 맵 생성은 없다.

기존 자산 조사 결과
V03 props_manifest는14파일이며 그중6파일은 이미 여러 칸짜리다. 기존 소품이 전부1칸이었던
것은 아니다. 한 칸 강제 fit, 칸마다 큰 소품을 반복 생성, pivot/anchor 오류, 이웃 선택 부재가
실제 프로젝트에 있는지 먼저 조사해야 한다. 이번 환경에서는 원 프로젝트에 접근하지 않았다.
기존 footlight on/dim/off는3가지 장식이 아니라 한 장치의 상태 변형이다.

검증 한계
PNG/SHA/치수/pivot/마스크/접합/파일 참조 및 C# 소스 계약을 오프라인 검수한다.
Inspection 예시는 실제 PNG를 메타데이터에 따라 합성한 배치 예시이며 Unity 스크린샷이 아니다.
연결 편집 GIF는 이웃 칸 추가/삭제 데모이며 장식에 새 실시간 애니메이션이 있다는 뜻이 아니다.
실제 Unity import·C# compile·TestRunner·Prefab 연결·Play·캠페인/대전·모바일 검증은 NOT_RUN이다.
검수 JSON에서 미실행을 PASS로 바꾸지 않는다. 실제 호스트 검증은 UNITY_CLI_KO.txt를 따른다.

26종 목록
''' + '\n'.join(rows) + '\n'
(O / 'README_KO.txt').write_text(readme, encoding='utf-8')

cli = '''ANIMOL 잔영 극장 — 연결 장식 V01 Unity 적용 명령

첨부 ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01.zip을 원 Unity 프로젝트에 적용하라.
연결형6계열 + 여러 칸 장식12 + 한 칸 소품8의 실제 PNG/PPU/pivot/카탈로그/배치 코드를 사용한다.
원래 게임 규칙, 테마ID·저장 형식, 캐릭터·공통/극장 장치 동작 및 최신 애니메이션을 보존한다.
이 작업을 장식의 실제 생성/로드/편집 경로까지 끝내라. JSON 복사나 예시 씬 배치만으로 완료하지 말라.

0. 패키지와 기준을 확인하라.
루트 이름 ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01을 보존하여 Assets 아래 설치한다.
README_KO.txt, manifest.json, manifest_sha256.txt, decoration_catalog.json,
sprite_import_settings.json, example_layout.json, QA/검수 자료와 Runtime/Editor 소스를 읽는다.
원화는 sources/, 제작 자료는 Authoring/, 합성 검토 자료는 Inspection/에 있다.
새 manifest.assets[]의 decor/ PNG만 최신 런타임 자산이다. 폴더명만 보고 Sprite를 일괄 등록하지 말라.
기존 극장 V03 전체 ZIP을 재설치하지 말라. 이후 고친 한 칸 문·바람 영역·책장·도미노 등 수정본을
이 패키지의 과거 기준 파일로 덮어쓰지 말라. Ranch 및 나머지 테마는 수정하지 말라.

1. 실제 프로젝트의 장식 생성 경로를 조사하라.
실제 극장 테마 enum/ID, 캠페인·대전·맵 에디터·재시작·저장 로드·런타임 생성 경로를 찾는다.
Prefab/Nested/Variant, SO/카탈로그, 풀 프리웜·재사용·반납, Resources/Addressables/원격카탈로그,
문자열 키/switch/default, 비활성 오브젝트, SpriteAtlas, Animator·Timeline·LateUpdate sprite 쓰기를
조사한다. 실제 클래스·필드·GUID/fileID·경로를 기록하고 새 프로젝트 ID를 추측해서 만들지 말라.
V03 소품14파일 중6개는 이미 multi-cell이다. 기존 이미지를 전부1칸으로 간주하지 말라.
fitToCell/alpha bounds 기반 축소, occupied-cell마다 그룹 중복 spawn, 중심/row 부호 오류,
카메라 청크만 조회하는 이웃 마스크가 있는지 조사하고 실제 소스 위치를 보고하라.

2. 장식과 작동 장치를 구분하라.
새 velvet_curtain_screen은 정적 장막 파티션이다.04교대 셔터의 상태/애니메이션을 대체하지 않는다.
prop_shelf_run은 장식 진열대이다.08회전 책장의 H2×1↔V1×2와 중간 프레임을 대체하지 않는다.
puppet_storage_2x2는 장식 보관함이다.23겁쟁이 인형의 시선/후퇴/막힘 동작을 대체하지 않는다.
backstage_spotlight_duo_2x1과 brass_footlight_strip은 장식이다. 실제 빛 감지/턴/코스트 로직을 만들지 않는다.
공통 장치·도미노·무대·석상·주사위·책장·인형의 최신 바인딩을 유지한다. 장식 스프라이트를
그 장치의 idle/fallback으로 넣지 말라. 공유 prefab은 극장용 시각 자식/Variant로 범위를 좁힌다.

3. 정확한 최신 import/카탈로그를 생성하라.
manifest_sha256.txt가 manifest.json의 실제 파일 SHA256와 같은지 먼저 확인한다. 현재 pin을
설정/어댑터에 명시적으로 넣고 빈 문자열이나 과거 pin을 허용하지 말라.
정확한 namespace는 ANIMOL.Art.TheaterConnectedDecorV01이다. 실행은 실제 경로를 대입한다.

"<UNITY_EDITOR_PATH>" -batchmode -projectPath "<PROJECT_PATH>" -executeMethod ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01CatalogBuilder.BuildFromCommandLine -theaterDecorManifest "Assets/<실제설치경로>/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/manifest.json" -quit -logFile "<PROJECT_PATH>/Logs/theater-connected-decor-v01-build.log"

생성 결과 _Generated/TheaterConnectedDecorV01Catalog.asset과 실제 Sprite GUID+subasset fileID를
제출한다. 동봉하지 않은 .asset/.meta의 GUID를 지어내지 말라. 생성기 로그 오류를 숨기지 말라.
Importer는 이 패키지 루트/현재 manifest allowlist/각 PNG SHA만 적용한다. 다른 테마를 일괄 변경하지 않는다.
PPU256, SpriteSingle, FullRect, custom pivot, Bilinear, Clamp, uncompressed, mipmap off,
NPOT none, fallback physics shape off를 사용한다. 회전/tight Atlas packing을 허용하지 않는다.
sources/Inspection/미선언 PNG는 런타임 Sprite fallback이 될 수 없다.
오프라인 검사 도구가 있으면 package-root에서 QA/verify_addon.py를 실행하되 이것을 Unity 컴파일로
보고하지 말라. 실제 컴파일/생성/임포트 로그를 별도로 남겨라.

4. 검증된 호스트에 새 어댑터를 연결하라.
독립 시각 자식에 TheaterConnectedDecorV01Controller를 붙이고 다음 공개 API를 연결한다.
Configure(catalog, pinnedManifestSha256, actualSortingLayerName)
SetLayout(networkCells, groupPlacements, worldOrigin, cellSize, hostAllowDecorCell, seed, postSpacing)
실제 함수 시그니처/타입은 동봉 소스로 확인한다. Sorting Layer 이름을 추측하지 말라.
호스트 어댑터가 실제 게임/에디터 장식 목록을 아래 레코드로 변환한다.

TheaterDecorNetworkCell: x,y,logicalLayer,heightKey,sortingOrder,worldZ,family,connectionGroup.
TheaterDecorGroupPlacement: placementId,groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
heightKey는 정확한 정수 높이 구역 키이며 worldZ와 구분한다. connectionGroup은 연결될 장식
띠 전체가 공유하는 값이다. 타일별 고유 ID를 connectionGroup에 넣으면 연결되지 않는다.
placementId는 반대로 큰 장식 한 묶음마다 고유하다. 한 타일마다 같은 그룹을 spawn하지 말라.
groupKey는 manifest.groups의 정확한20키 중 하나이며 art.family도 그 키와 같아야 한다.

호스트가 확인한 장식 허용 셀의 순수 조회 함수를 hostAllowDecorCell로 전달한다. 이 콜백은 여러 번
호출돼도 상태를 바꾸지 않아야 한다. 콜백 안에서 점유를 예약/삭제하거나 게임 턴을 진행하지 말라.
모든 footprint 칸이 허용되어야 그룹 전체를 배치한다. 일부만 가능하면 전체 거절하고 잘라 배치하지 않는다.
이 시각 점유 검사로 새 Collider/Rigidbody/게임 장애물을 만들지 말라. 플레이어 이동 가능 여부는
기존 권위 게임플레이가 정한다. 논리 layer/height가 달라도 겹침은 호스트가 승인한 장식에만 허용한다.

5. 방향·연결·좌표를 정확히 변환하라.
장식은 N1 E2 S4 W8의4이웃이다. 각 family의 n00..n15를 실제 이웃으로 선택한다.
같은 family + connectionGroup + logicalLayer + heightKey인 셀끼리만 연결한다. 다른 구역/높이/
테마/재료 사이를 연결하지 않는다. N/S로 이어지는 별도 원화 부품을 사용한다.
월드+Y가 North이다. grid row가 아래로 증가하면 공개 변환기를 사용한다.
FromRowDownCell(col,row)=(col,-row).
FromRowDownGroupBottomLeft(left,top,h)=(left,-top-h+1).
RowDownTopLeftToWorldOrigin(topLeft,cellSize)는 셀 바닥 기준에 맞게 Y를-cellSize 보정한다.
그룹 anchor는 왼쪽 아래 셀, sprite 중심은 anchor+(width/2,height/2)이다.
PNG pivot은 logicalRect 중심이며 PNG 상단 여백/지면 위 돌출을 포함한다. 지면 중심을 이미지 중심으로
간주하지 말라. 모든 방향에 같은 ppu/원근/셀 간격을 적용하고 bounds로 매번 scale을 바꾸지 말라.
네트워크320×384의 logicalRect는[32,96,256,256],pivot[0.5,160/384]이다.
큰 소품은 manifest의 개별 캔버스/지면rect를 사용한다.3×2를1×1에 넣거나2×2로 임의 변경하지 않는다.
상위 보드의 공통 cellSize는 유지하고 부모의 X/Y 배율을 축별로 상쇄해 월드에서 균일한 셀 크기를
유지한다. 양수인 비균일 X/Y 부모 배율도 지원한다. game root/Collider는 회전/확대하지 않는다.
각 조상 localRotation은 identity이고 X/Y localScale은 양수여야 하며 위반 시 진단한다.

긴 직선의 선택 보조 기둥은 첫2계열에서만3 또는4셀의 고정 월드 간격으로 선택된다. 즉시 양옆도
직선일 때만 배치하며 끝/모서리/T/십자 바로 옆에는 덧붙이지 않는다. 해당 periodic_post PNG가
manifest에 선언된 경우만 보조 기둥을 선택한다. 선택 보조 PNG가 없으면 생략한다. 매 프레임 Random을 쓰지 않는다.
기존 W목재 통로의 bridge_decor 난간/공유 꼭짓점 기둥은 기존 owner가 생성한다. 같은 셀/경계에
새 객석 장식 난간을 자동 복제하지 말라. 실제 G=stage_floor,W=wood_catwalk,S=velvet_carpet을 유지한다.
S는 지지대 기준에서 G로 정규화한다. 목장 S=stone_bridge 규칙을 극장에 복사하지 말라.

6. 전체 스냅샷/수명주기를 연결하라.
SetLayout에는 해당 보드 전체 장식 스냅샷을 전달한다. 화면 밖/카메라 청크의 이웃도 포함한다.
확정된 셀 추가/삭제·로드·undo/redo·재시작·풀 재사용 시 현재 스냅샷을 재전달한다.
자동 Start/Update 생성기나 타이머로 게임 상태를 바꾸지 않는다. 효과 애니메이션 종료가 턴을 진행하지 않는다.
플래너는 순수 계획이다. Controller는 입력/카탈로그 검증 실패 시 자기 소유 시각을 지우는 fail-closed
동작이다. 실패 입력에도 이전 그림이 유지된다고 가정하지 말라. 게임의 무효 입력은 권위 호스트가
먼저 거절하고 변경 없는 SetLayout 호출을 생략하라.
풀 반납/OnDisable에는 소유 노드를 숨기고 상태를 지운다. 재사용에는 Configure+최신 SetLayout을
다시 호출한다. 매개변수 없는 ClearLayout() 후 구 Sprite를 자동 복구하는 코드를 만들지 말라.
Destroy가 지연될 때 이전 노드를 즉시 비활성화해 한 프레임 이중 표시가 없게 한다.
기존 Animator/다른 스크립트가 새 장식 Renderer를 다시 쓰지 않도록 책임을 한 곳으로 정한다.

7. 기존 장식 참조를 실제 의도로 이관하라.
현재 각 소품의 실제 old GUID/fileID,논리 점유,사용처를 표로 기록한 뒤 새26키와 매핑한다.
기존 organ2×2→grand_piano3×2,seat_pair2×1→audience_bench3×1,
scenery_flat2×2→scenery_flat3×1처럼 크기가 다르면 현재 배치에 새 footprint 전체가
허용되는지 먼저 확인한다. 빈 셀을 임의로 장악하거나 새 그림을 한 칸에 축소하지 말라.
맞지 않는 곳은 실제 장식 authoring/배치 규칙을 조정하고 해당 맵을 다시 검증한다. 게임 경로를 막지 않는다.
기존 footlights_on/dim/off가 실제 기능/상태에 쓰이면 새 정적 strip으로 덮지 않는다.
rope_pulley가 장치 보조그림인지 장식인지 조사하고 검증되지 않은 자동 매핑을 하지 않는다.
소품의 장식 의도가 새 종류와 다르면 조용히 치환하지 말고 명시 매핑/예외 사유를 보고한다.

선택된 장식의 구 fallback/기본 이미지/문자열 로딩/캐시 복귀를 최신 catalog 경로로 이관한다.
누락 키를 옛 장식 또는 임의 첫 Sprite로 덮지 말라. 실제 GUID+fileID+SHA를 검증한다.
기존 최신 장치/타일/배경과 다른 테마의 공유 자산까지 삭제하지 말라. 장식 이관이 확인된 뒤 해당
소품의 reachable reference0를 증명한 것만 dry-run 목록을 남겨 제거한다. .meta 일괄 삭제 금지.

8. 실제 검증과 결과를 제출하라.
- import/compile 오류0,Missing Script/Sprite0,현재 manifest pin/GUID/fileID/SHA 일치.
-6계열의 단독/2/3/4/긴 가로·세로,끝/ㄴ/T/십자/고리/구멍 가장자리에서 자연스러운 마감.
-셀 추가·제거·undo/redo·로드/저장·재시작·풀 재사용·씬 재진입 후 올바른 마스크 재선택.
-다른 family/group/layer/height는 분리. 경계/구멍/다리 난간 이중 생성 없음.
-큰12종의 실제 점유2×1/3×1/2×2/3×2 전체 허용 검사와 그룹당 renderer1개.
-한 칸 소품8종도 고유 placementId로1개. 겹침/부분 실패 시 부분 생성 없음.
-원근/PPU/pivot/scale 일치,1080×1920 및 모바일 축소에서 접합선/투명 테두리/가독성 확인.
-negative-case: 누락 PNG/틀린SHA/낡은pin/잘못된groupKey/중복placementId/한 칸 거절이
  명확히 실패하고 구 이미지 fallback이 없음.
-비활성 Configure+SetLayout→활성,OnDisable→풀 재사용,Pause/재개,단위/정수좌표 경계 확인.
-캠페인·대전·에디터·런타임 생성에서 실제 renderer가 새 catalog를 사용한다는 증거.
-기존 문/바람/책장/도미노/극장 장치 애니메이션·게임 경로·턴/코스트·다른 테마 회귀 없음.

제출: ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01_PROJECT_APPLY_RESULT.md,
실제 변경 파일/씬/Prefab,26종 매핑/예외 표,GUID/fileID/SHA 표,카탈로그 생성 로그,
컴파일/TestRunner/PlayMode 로그,실제 배치 스크린샷/연결 편집 영상,검증되지 않은 항목.
동봉된 합성 PNG/GIF는 실제 Unity 실행 증거가 아니다. 미실행을 PASS로 보고하지 말라.
'''
(O / 'UNITY_CLI_KO.txt').write_text(cli, encoding='utf-8')
(O / 'decoration_catalog.json').write_text(json.dumps(design, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('handoff draft:', [(p.name, p.stat().st_size) for p in O.iterdir()])
