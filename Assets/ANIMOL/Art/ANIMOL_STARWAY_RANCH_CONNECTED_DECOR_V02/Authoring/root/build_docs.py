from pathlib import Path
import json
W=Path(__file__).resolve().parent
ID='ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02'
O=W/'handoff_exports';O.mkdir(exist_ok=True)
pinpath=W/'package_staging'/ID/'manifest_sha256.txt'
PIN=pinpath.read_text().strip()if pinpath.exists()else'__RANCH_V02_MANIFEST_SHA256__'
d=json.loads((W/'catalog_design.json').read_text())
(O/'decoration_catalog.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
readme=f'''ANIMOL 별길 목장 연결 장식 V02 — 7종에서26종 확장·새 원화 교체

실제 장식 종류는 연결6종+다칸12종+한칸8종=26종이다. 마스크·꽃 변형·보조 기둥을 종류 수로 세지 않는다.
기존 연결 장식 V01의7종도 새 native imagegen 원화로 다시 제작했고19종을 더했다.
기존298런타임 상대 경로와 기존4묶음의 논리 점유를 유지하며64PNG를 추가한 총362런타임PNG다.
예전 PNG 인코딩만 바꾼 것이 아니라 대응298개 decoded RGBA가 전부 달라야 통과한다.
크림색 목재·에메랄드 마감·민트/세이지 잎·노란 건초/꽃·보라색 돌과 굵은 남색 윤곽선을 사용한다.
긴 직선·쐐기·각진 실루엣과 넓은 기본색/그림자 셀 음영을 따른다. 옛 픽셀/16색 규칙은 폐기했다.
새 원화는 built-in imagegen으로 제작했으며 전체 프롬프트·원화·자르기·등록 출처를 포함한다.

구성
- cream_fence와 meadow_stone_wall:각16마스크+선택 보조기둥1개씩.
- orchard_trellis,herb_planter_strip,ranch_lantern_strip:각16마스크.
- raised_flower_meadow:8이웃 원본256마스크+선택 꽃 겹그림4개. 잔디를16/47상태로 축소하지 않는다.
- 다칸12묶음과 한칸8소품:각1개 전체그림. 그룹/점유마다 쪼개 반복하지 않는다.
- Runtime/Editor:최신 카탈로그/엄격 임포터/순수 배치 계획/Controller와 실제 호출 Binding,
  typed BoardSnapshot/BoardAsset/PublishedSource/AuthoredSource/검사 Prefab·선택 씬 생성기.
- Examples:전체26종을 각각 포함하는 Stage65연결칸+20묶음=120점유,All2696연결칸+20묶음=151점유.
- Inspection:도감·격자 배치도와 기존 목장 하늘/지형 위 합성 참고도. 실제 Unity 스크린샷이 아니다.
- sources/Authoring:원화와 제작 출처. 원화/초안/검수 이미지를 인게임 선택 목록에 넣지 않는다.
- Tests~:선택 NUnit 소스. 실제 TestRunner 실행 전까지 NOT_RUN이다.

크기와 연결
기본 한칸은256px/PPU256=world1이다. V02의 모든 연결그림/꽃/보조기둥은320×320,
이미지 위쪽 기준 logicalRect=[32,32,256,256],Unity 아래쪽 기준 pivot=[0.5,0.5]다.
다른 테마의320×384 등록을 이 팩에 복사하지 않는다. 다칸은 각 manifest의 전체 점유/지면/pivot을 따른다.
PPU와 지면 중심을 유지하고 전체 PNG나 alpha bounds를 한칸으로 강제 축소하지 않는다.
같은 family/connectionGroup/logicalLayer/heightKey의 실제 전체 장식 맵 이웃만 연결한다.
보통5계열은 N1E2S4W8,잔디만 N1NE2E4SE8S16SW32W64NW128의 원본8비트 전부를 사용한다.
대각만 바뀌어도 잔디의 해당 모서리/안쪽 구멍을 다시 계산해야 한다.
꽃은 이미 있는 잔디 칸의 StableHash가5로 나누어떨어질 때만 (hash/5)%4의0..3변형을 고른다.
꽃/기둥은 추가 점유를 예약하지 않으며 매프레임 랜덤으로 바뀌지 않는다.

적용
새 namespace ANIMOL.Art.RanchConnectedDecorV02,packageId={ID}.
manifest_sha256.txt 현재 핀={PIN}
UNITY_CLI_KO.txt를 실제 Unity 프로젝트에서 작업할 CLI에게 전달한다. PNG 복사만으로 끝내지 않는다.
기존 V01과 새 V02의 namespace/폴더를 섞거나 .meta/GUID를 통째 복제·치환하지 않는다.
새 카탈로그→BoardAsset→Source→Binding→Controller로 실제 호출되는 검사 Prefab 생성기가 들어 있다.
실제 게임은 현재 장식 데이터와 순수 조회한 허용 셀에서 전체 스냅샷을 만들어 Publish하는 얇은 어댑터가 필요하다.
Binding은 Source.Changed를 구독해 갱신하고 실패 시 자기 소유 그림만 지운다. 옛 그림 fallback이 없다.
PublishedSource가 비활성화되면 옛 데이터가 무효화되므로 풀 재사용 후 최신 보드를 다시 Publish한다.

범위 보존
이 묶음 때문에 CartoonV12 전체를 다시 설치하지 않는다. 실제 설치된 이후의 문/바람 패치도 보존한다.
일방향 문1칸과 닫힘 거절/X,바람 영역/굴절,고정 풍차 몸체+회전 로터,병아리4방향 idle/walk와 눈광,
별 모양 돌/홈·누름판/덩굴다리·무빙워크/제동판/열린 ㄴ자와 기존 장치 시퀀스를 이 장식으로 덮지 않는다.
병아리 .62배는 visual 자식에 한번만 적용한다. 캐릭터·논리 루트·이동·코스트·점유는 바꾸지 않는다.
G=grass,W=wood_bridge,S=stone_bridge. S는 자기 석재 지지대를 쓰며 G로 정규화하지 않는다.
기존 W/S다리의 공유 난간/꼭짓점 기둥/이어진 하부를 새 장식으로 중복 생성하지 않는다.
닭장/여물통/건초/수레는 정적 장식이며 병아리 생성기·둥지·기능 장치가 아니다.
별은 모든 구름 뒤에 거의 고정,구름과 가까운 섬은 더 민감하게 반응하는 기존 깊이를 유지한다.
새 달·행성·태양을 추가하지 않는다. 기본 바닥·배경·UI·다른 테마는 이번 교체 대상이 아니다.

검수
QA/prior_v01_decoded_comparison.json은 기존298경로의 실제 RGBA 교체와 점유 보존을 기록한다.
python3 QA/independent/audit_package.py --package <패키지루트>로 파일·접합·점유를 검사할 수 있다(Python/Pillow).
원 Unity 프로젝트와 Editor/컴파일러가 없어 실제 Import/C#/Shader/NUnit/Prefab생성/게임 연결/
Play/접근 가능한 레거시 참조0은 NOT_RUN이다. 동봉 소스·PNG·오프라인 검수는 이 실행의 증거가 아니다.
다운로드는 분할 없는 통합ZIP 하나다. 제작 원본·코드·적용 지시·참고도를 모두 포함한다.
'''
cli=f'''ANIMOL 별길 목장 연결 장식 V02 — Unity CLI 적용 명령

목표
기존7종 연결 장식을 새26종/362PNG로 교체·확장하고 실제 게임의 배치 호출을 최신 V02에 연결하라.
packageId={ID}
namespace=ANIMOL.Art.RanchConnectedDecorV02
currentManifestSha256={PIN}
기존298상대경로/옛4묶음 크기는 유지하고 대응decodedRGBA는 모두 새 원화다. 추가 경로64개다.
게임 입력/이동/점유/코스트/턴/장치 ID/캐릭터/배경/기본 바닥/다른 테마의 규칙은 바꾸지 않는다.
이 배포본의 PNG와 정적 소스 검사 결과를 실제 Unity 실행 완료라고 보고하지 말라.

1. 실제 프로젝트 조사와 버전 이관
README/manifest/pin/decoration_catalog/Examples/Runtime/Editor/QA를 읽고 실제 목장 테마ID,
장식 모델/허용 셀/배치·삭제·생성·로드·Undo·Redo·재시작·풀 이벤트/SortingLayer를 조사하라.
추측한 theme key,프로젝트 타입/필드/이벤트/GUID를 새로 지어 넣지 않는다.
Assets 아래 V02 폴더 이름을 유지한다. V01의 .meta를 V02로 복제하거나 외부 GUID를 일괄 치환하지 않는다.
같은 V02를 갱신할 때는 기존 V02 .meta와 Unity가 생성한 자산 GUID를 보존한다.
V01 namespace/Controller/카탈로그를 V02와 뒤섞거나 같은 Renderer에 두 시각 공급자를 남기지 않는다.
최신 V02를 검증해 연결한 뒤 원래 호스트의 V01 호출/참조를 이관한다. 실제 미사용이 입증된 V01 범위만 정리한다.
CartoonV12 전체를 다시 설치하지 말고 현재 적용된 이후 문/바람·장치 수정본까지 보존한다.

2. 실제 카탈로그·검사 보드 Prefab/선택 씬 생성
아래 Bash 명령의 경로/SortingLayer를 실제 값으로 채운다. PowerShell은 실행 파일 앞에 &를 붙인다.

"<UNITY_EDITOR_PATH>" -batchmode -projectPath "<PROJECT_PATH>" -executeMethod ANIMOL.Art.RanchConnectedDecorV02.RanchConnectedDecorV02BoardTools.BuildExamplesFromCommandLine -ranchDecorManifest "Assets/<설치경로>/{ID}/manifest.json" -ranchDecorStageSnapshot "Assets/<설치경로>/{ID}/Examples/stage_snapshot.json" -ranchDecorAllSnapshot "Assets/<설치경로>/{ID}/Examples/all26_snapshot.json" -ranchDecorSortingLayer "<실제 기존 Sorting Layer>" -ranchDecorCreateScenes true -quit -logFile "<PROJECT_PATH>/Logs/ranch-connected-decor-v02-build.log"

메뉴 Tools/ANIMOL/Ranch Connected Decor V02/Build example boards…도 같은 입력을 받는다.
열린 Prefab Stage/생성대상 검사 씬과 미저장 출력 수정을 먼저 저장·닫기 상태로 정리한다.
기존 생산 씬/카메라/조명/BuildSettings를 변경하지 않고 생성기 소유가 아닌 출력은 덮지 않는다.
_Generated에 Catalog,_Generated/InspectionBoards/Stage 및 All26에 BoardAsset/Prefab을 만든다.
true일 때만 검사 .unity를 만든다. 실제 UnityAPI가 생성한 GUID/fileID만 사용한다.
BoardAsset→AuthoredSource→Binding→Controller를 연결하고 Bind/RefreshNow를 명시적으로 실행한다.
CurrentPlan과 실제 활성 Renderer/Sprite 수를 확인한다. 꽃/선택 기둥이 별도 시각이므로 점유 수와 같다고 가정하지 않는다.
Stage65연결칸+20묶음=120점유,All2696연결칸+20묶음=151점유이며 둘 다26종 검사 전용이다.
실제 로그/생성된 GUID/fileID/스프라이트/PPU/pivot을 제출하고 생성기 코드 존재와 실행 성공을 구분하라.

3. 실제 권위 모델에서 전체 스냅샷 발행
장식 전용 같은 GameObject의 Controller/Binding만 연결한다. 실제 모델과 허용 공간을 DTO로 변환하는
프로젝트별 어댑터를 작성하고 PublishedSnapshotSource.PublishSnapshot(snapshot,out error) 후
Binding.Bind(controller,catalog,currentPin,actualSortingLayer,source,out error)를 호출한다.
프로덕션 allowExampleSource는 생략하거나 false다. 검사 데이터/Source를 true로 우회해 넣지 않는다.

BoardSnapshot 정확한16필드:
schemaVersion=1,packageId,sourceManifestSha256,boardId,revision,isWholeBoard=true,isExampleOnly=false,
originX,originY,originZ,cellSize,seed,postSpacing(3또는4),networkCells,groupPlacements,allowedDecorCells.
networkCells 8필드:x,y,logicalLayer,heightKey,sortingOrder,worldZ,family,connectionGroup.
groupPlacements 8필드:placementId,groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
allowedDecorCells 4필드:x,y,logicalLayer,heightKey. null/누락은 거절, 빈 보드는 명시적 빈 배열을 준다.
origin은 셀(0,0)의 왼쪽 아래 월드 원점이다. 전체 보드를 전달하고 화면/청크 일부만 보내지 않는다.
예시의 허용칸 목록을 실제 게임에 복사하거나 요청칸을 스스로 승인하는 return true를 쓰지 말라.
호스트가 실제 허용하는 장식 공간을 순수 조회한다. 조회 중 게임 점유를 예약/변경하지 않는다.
다칸의 모든 셀이 허용되고 중복 없어야 그룹당 Renderer 하나를 생성한다.
꽃/보조 기둥은 해당 기존 연결칸 위 겹그림이며 추가 점유/Collider를 만들지 않는다.

4. 실제 변경·풀 재사용 연결
로드/생성완료/확정편집/Undo/Redo/재시작/씬재진입/풀 재사용 뒤 최신 전체 Snapshot을 Publish한다.
Binding은 Source.Changed를 구독해 갱신한다. 별도의 Start/Update 생성기·매프레임 랜덤을 추가하지 않는다.
원배열을 Publish 후 수정해도 캡처가 바뀌지 않는다. 모델 변경 후 새 스냅샷을 다시 발행해야 한다.
취소된 편집/무효입력에서 게임 모델이 안 바뀌면 새 비용/턴/행동을 만들지 않는다.
PublishedSource 비활성화 시 옛 권위 데이터가 무효화되므로 재활성/풀 재사용 후 freshPublish가 필수다.
AuthoredSource는 지정한 정적 BoardAsset 경로이며 실제 모델 이벤트 어댑터를 자동 대체하지 않는다.
실패 시 자기 소유 그림만 지우고 진단한다. 틀린 pin/누락 PNG를 옛 V01/V12 그림으로 대체하지 않는다.
기존 Animator/풀 초기화/코루틴이 같은 Renderer를 덮어쓰지 않도록 시각 쓰기 책임을 단일화한다.

5. Ranch 전용 이웃·꽃·등록 규칙
월드+Y=N이며 이웃 비트와 행좌표 변환을 혼동하지 않는다.
5계열 cream_fence,meadow_stone_wall,orchard_trellis,herb_planter_strip,ranch_lantern_strip은
4비트 N1E2S4W8,n00..n15를 쓴다. raised_flower_meadow만 원본8비트
N1NE2E4SE8S16SW32W64NW128,n000..n255를 쓴다. 16/47상태로 canonicalize하지 않는다.
같은 family/connectionGroup/logicalLayer/heightKey만 이웃이다. 대각만 달라져도 잔디를 갱신한다.
이어진 띠/화단은 같은 connectionGroup을 공유한다. 각 셀의 유일ID를 group에 넣지 말라.
StableHash(seed,x,y,layer,height,connectionGroup)를 사용한다. hash%5==0일 때만 꽃을 추가하고
(hash/5)%4로 정렬된 flowers_01..04를 고른다. 월드 정수 셀 좌표/seed를 보존해 로드/Undo 때 흔들리지 않게 한다.
첫2계열의 보조 기둥만 긴 직선 양옆 이웃까지 직선일 때 고정3/4셀 간격으로 선택한다.
모든 연결/꽃/기둥 캔버스320×320,logicalRect[32,32,256,256],PPU256,pivot[0.5,0.5]다.
다른 테마의320×384/pivot을 복사하거나 alpha bounds/PNG 전체를 한칸에 fit하지 않는다.
그룹 anchor왼쪽 아래,지면 중심=(anchorX+w/2,anchorY+h/2),placementId당 전체그림1개다.
FromRowDownCell(col,row)=(col,-row),FromRowDownGroupBottomLeft(left,top,h)=(left,-top-h+1),
RowDownTopLeftToWorldOrigin(boardTopLeft,cellSize)를 실제 행좌표 모델에 맞춰 사용한다.
부모 양수XY배율을 보정해 균일 월드 셀 크기를 유지하고 회전/반전/비정상변환을 진단한다.
부모 이동/배율 변경 후 같은 Snapshot이어도 Refresh가 다시 검증하게 한다.

6. 실제 기존 기능과 레거시 범위 보존
G=grass,W=wood_bridge,S=stone_bridge. S는 독립 석재 하부를 쓰며 G로 정규화하지 않는다.
기존 W/S다리 경계 난간·공유 꼭짓점 기둥과 연결 하부를 새 장식으로 중복 생성하지 않는다.
최신 병아리4방향 idle/walk/눈광,visual자식 .62배 한번,풍차 고정몸체+분리로터,
한칸 일방향 문/힌지/거절봉·X,바람 영역·굴절·셰이더와 최신 장치 프레임을 보존한다.
새 닭장/건초/여물통/수레를 둥지·병아리 생성기·압력판·운반 장치로 자동매핑하지 않는다.
거의 고정된 별/모든 구름 뒤 순서/구름·가까운 섬 깊이 반응,달·행성·태양 없는 기존 배경을 유지한다.
씬/Prefab/SO/Resources/Addressables/문자열 로딩/Atlas/Animator/캐시/풀의 실제 옛 장식 참조를 추적한다.
V01 장식→V02의 기존7키는 유지되지만 catalog/namespace는 새 V02로 전환한다. V12소품의 새26키 대응은
실제 사용처/점유를 검토해 매핑한다. 크기가 달라지는 소품은 전체 새 점유가 허용되는 배치로 조정한다.
프로젝트 참조0를 확인한 해당 미사용 V01시각물만 dry-run목록 후 정리한다. 공유 .meta/장치/바닥/배경/
다른 테마/캐릭터/UI를 통째로 지우지 않는다.

7. 실제 결과 검증과 제출
Import/C#컴파일 오류0,MissingScript/Sprite0,정확362allowlist/카탈로그/PPU/pivot/GUID/fileID/SHA를 확인한다.
두 검사 보드 Play,5cardinal+256meadow/대각만변경/안쪽구멍/큰화단/꽃변형/끝·T·십자/기둥 간격을 검증한다.
각 whole그룹은 Renderer1개이고 꽃/기둥 시각만 추가되며 점유가 늘지 않아야 한다.
실제 게임의 생성/로드/편집/Undo/Redo/재시작/풀/씬재진입을 검증하고 최신 모델→Publish→Binding→Controller
호출 증거를 남긴다. 틀린핀/한칸거절/중복/누락은 전체 거절,프로덕션 exampleOnly는 거절돼야 한다.
원배열 변경 누출 없음/부모이동·양수배율·회전거절/소스교체·구독해제/비활성뒤옛상태미복구/
freshPublish복구/같은 seed의 꽃·기둥 고정과 기존 장치·다른 테마 회귀 없음을 확인한다.
Tests~ 선택 NUnit 소스를 실제 TestRunner에 연결해 실행하고 미실행을 PASS로 표시하지 않는다.

{ID}_PROJECT_APPLY_RESULT.md에 변경파일/실제 어댑터타입·이벤트 바인딩표/26종 매핑·예외/
GUID·SHA표/생성·컴파일·TestRunner·Play로그/실제 화면과 변경영상/미확인 항목을 제출하라.
합성 PNG와 이 배포본의 오프라인 파일·소스검수는 실제 Unity 실행 증거가 아니다.
'''
(O/'README_KO.txt').write_text(readme,encoding='utf-8')
(O/'UNITY_CLI_KO.txt').write_text(cli,encoding='utf-8')
print('Wrote Ranch V02 README/CLI/catalog; pin',PIN)
