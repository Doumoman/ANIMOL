from pathlib import Path
import json
W=Path(__file__).resolve().parent
ID='ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01'
O=W/'handoff_exports';O.mkdir(exist_ok=True)
pinpath=W/'package_staging'/ID/'manifest_sha256.txt'
PIN=pinpath.read_text().strip() if pinpath.exists() else '__COAST_MANIFEST_SHA256__'
d=json.loads((W/'catalog_design.json').read_text())
(O/'decoration_catalog.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
readme=f'''ANIMOL 별너울 해안 연결 장식 V01

이번 묶음은 별너울 해안의 장식이 여러 칸에 걸쳐 이어지거나 전체 묶음으로 배치되는 추가 패키지다.
연결 마스크를 종류 수로 부풀리지 않은 실제26종: 연결6종, 다칸12종, 한칸8종.
새 built-in imagegen 원화에서 투명 PNG를 제작했고 전체 프롬프트/원본/등록 정보를 포함한다.
굵은 남색 외곽선, 긴 직선과 쐐기, 모래색·유목·청록·산호·슬레이트의 평면2–3단계 셀 음영을 따른다.
픽셀 아트 규칙이나 다른 테마 그림의 재색칠을 적용하지 않았다.

파일 구성
- decor/connected:6계열 각각 N1/E2/S4/W8의16마스크=96PNG.
- 첫2계열 선택 보조 기둥2PNG. 끝/모서리/갈림길 옆에 매칸 반복하지 않는다.
- decor/groups:12개 다칸 전체 그림. decor/points:8개 한칸 소품. 총118런타임PNG.
- Runtime/Editor:카탈로그/엄격한 임포터/순수 배치 계획/시각 컨트롤러와 실제 호출 Binding,
  BoardSnapshot/BoardAsset/PublishedSource/AuthoredSource, 검사 보드 Prefab/선택 씬 생성기.
- Examples:전체26종 typed 보드2개. 둘 다 isExampleOnly=true인 검사 자료다.
- Inspection:합성 참고도. 실제 Unity 스크린샷이 아니다.
  context 참고도는 기존 해안 V01의 하늘/바다/지형 위에 새 장식을 합성한 자료이며, 기존 배경/타일을 새 런타임 아트로 복사한 것이 아니다.
- sources/Authoring:원화·프롬프트·제작 등록. 런타임 선택 목록에서 제외한다.
- Tests~:선택 NUnit 소스. 실제 실행 전까지 NOT_RUN이다.

아트와 배치
manifest_sha256.txt의 현재 pin은 {PIN}이다. 정확한 manifest와 PNG allowlist만 사용한다.
한 칸=256px/PPU256=world1. 연결 그림은320×384, 지면 사각형은 이미지 위쪽 기준
[32,96,256,256], Unity 아래쪽 기준 pivot=[0.5,160/384]이다. 여백/상부 높이를 한칸으로 축소하지 않는다.
다칸은 manifest의 footprintCells/지면 사각형/pivot을 따르고 placementId당 Renderer하나를 만든다.
두 줄 깊이를 쓰는 장식은 실제 바닥의 두 줄을 차지하도록 제작했으며, 전체 점유가 허용돼야 배치한다.
같은 family/connectionGroup/logicalLayer/heightKey의4이웃만 이어진다. 연결된 한띠의 group값을 공유한다.
월드+Y가 북쪽이고 그룹 anchor는 왼쪽 아래다. 행이 아래로 증가하는 맵은 제공 좌표 변환 API를 쓴다.

실제 유니티 적용
UNITY_CLI_KO.txt 전체를 실제 프로젝트에서 작업할 CLI에게 전달한다. PNG 복사로 끝내지 않는다.
완성된 예시 생성기는 카탈로그→BoardAsset→Source→Binding→Controller를 연결하고 명시적으로 Refresh한다.
실제 게임에서는 확정된 장식 데이터와 호스트가 승인한 전체 허용 셀을 PublishSnapshot에 전달하는
프로젝트별 어댑터가 필요하다. 이 패키지가 보이지 않는 게임 모델이나 이벤트를 자동 추측하지 않는다.
실패 시 자기 소유 그림만 정리하고 진단한다. 옛 스프라이트 fallback이 없다.
풀 비활성화 시 PublishedSource의 옛 데이터는 무효화된다. 재사용 후 최신 보드를 다시 Publish한다.

기존 테마와의 관계
기존 해안 V01의 기능 장치/애니메이션/나룻배 경로/물칸/지형/하부/다리/독립 배경을 다시 설치하거나 덮지 않는다.
A=coast_sand,W=wood_bridge,S=stone_bridge이다. 기존 다리 경계 난간과 공유 기둥/하부를 중복 생성하지 않는다.
새 수리선 작업장/등불/어망/계류 말뚝은 정적 장식이다. 실제 나룻배·구르는 통·폭탄·이탈 가시의 대체물이 아니다.
표시 이름은 별너울 해안이다. 실제 기존 테마/저장/맵 ID를 바꾸지 않는다. 움직이는 기존 야자수의 리그도 보존한다.
배경에는 달·토성·행성을 더하지 않는다. 석양 해는 정확히1개, 해와 수평선은 BackgroundRig에서 제외해 월드 고정한다.
별은 모든 구름 뒤에서 거의 고정, 구름과 가까운 섬의 깊이 반응은 보존한다. 물칸/배 경로와 원경 OceanField는 구분한다.

검수 상태
원본 Unity 프로젝트와 Editor가 이 작업 환경에 없다. 파일/알파/접합/점유/typed데이터/소스검사는
실제 Import/C#/Shader 컴파일/NUnit/Prefab생성/씬 바인딩/Play/프로젝트 레거시 참조0의 증거가 아니다.
실제 실행은 모두 NOT_RUN이며 원 프로젝트에서 적용 지시대로 검증해야 한다.
QA/independent/audit_package.py로 배포 파일 자체를 검사할 수 있다(Python/Pillow 필요).
압축은 한 번에 제공하는 통합ZIP이며, 분할ZIP이나 조각 합치기 과정이 없다.
'''
cli=f'''ANIMOL 별너울 해안 연결 장식 V01 — Unity CLI 적용 명령

목표
제공된26종/118PNG를 실제 해안 장식 모델에 연결하고, 오래된 장식 시각 참조를 사용하지 않게 하라.
packageId={ID}
namespace=ANIMOL.Art.CoastConnectedDecorV01
currentManifestSha256={PIN}
실제 테마/맵/세이브 ID·이동·점유·비용·입력·충돌·기능 장치·물칸/나룻배 경로의 규칙은 유지한다.
동봉한 합성 PNG와 정적 소스검사를 실제 Unity 실행 결과로 보고하지 말라.

1. 프로젝트 조사와 설치
README/manifest/각원화pivot/Examples/Runtime/Editor를 읽는다. Assets 아래 패키지 이름을 유지하고
동일 클래스가 두곳에서 컴파일되지 않게 한다. 갱신 시 해당 폴더의 .meta와 실제 생성GUID를 보존한다.
기존 해안 V01 전체를 다시 설치하지 않는다. 현재 적용된 최신 장치/움직임/물/지형 수정을 유지한다.
실제 해안 테마ID/장식 모델/허용 셀/물칸/배 경로/확정 편집/로드/Undo/Redo/풀/씬 이벤트/SortingLayer를 조사하라.
추측한 프로젝트 타입이나 star_swell_coast 같은 임의의 저장ID를 새로 만들지 말라.

2. 실제 카탈로그와 검사 Prefab/선택 씬 생성
아래 Bash 명령의 경로와 SortingLayer를 실제 값으로 채운다. PowerShell은 실행 파일 앞에 &를 붙인다.

"<UNITY_EDITOR_PATH>" -batchmode -projectPath "<PROJECT_PATH>" -executeMethod ANIMOL.Art.CoastConnectedDecorV01.CoastConnectedDecorV01BoardTools.BuildExamplesFromCommandLine -coastDecorManifest "Assets/<설치경로>/{ID}/manifest.json" -coastDecorStageSnapshot "Assets/<설치경로>/{ID}/Examples/stage_snapshot.json" -coastDecorAllSnapshot "Assets/<설치경로>/{ID}/Examples/all26_snapshot.json" -coastDecorSortingLayer "<실제 기존 Sorting Layer>" -coastDecorCreateScenes true -quit -logFile "<PROJECT_PATH>/Logs/coast-connected-decor-v01-build.log"

메뉴 Tools/ANIMOL/Coast Connected Decor V01/Build example boards…도 같은 입력을 받는다.
생성 전 열린 PrefabStage와 생성대상 검사 씬을 저장하고 닫는다. 출력에 미저장 변경이 있으면 먼저 정리한다.
다른 생산 씬/카메라/조명/BuildSettings를 변경하지 말고 생성기 소유가 아닌 출력은 덮지 않는다.
출력은 _Generated의 실제 Catalog와 _Generated/InspectionBoards/Stage 및 All26의 BoardAsset/Prefab,
명시적으로 true일 때만 .unity이다. UnityAPI가 생성한 진짜GUID/fileID만 사용한다.
생성기는 Binding.Bind/RefreshNow와 CurrentPlan/실제 활성Renderer/Sprite를 검사한다.
코드 존재와 생성 실행 성공은 구분한다. 컴파일/생성 로그와 실제 오류를 제출하라.
Stage는55연결칸+20묶음=104논리점유/26종, All26은78연결칸+20묶음=127점유/26종이다.
두 보드 모두 검사전용이고 실제 게임 저장 데이터로 가져오지 않는다.

3. 권위 모델→Snapshot→Binding 연결
같은 장식 전용 GameObject의 Controller와 Binding만 연결한다. 실제 프로젝트 데이터를 평평한 DTO로
변환하는 얇은 어댑터를 작성한다. PublishedSnapshotSource.PublishSnapshot(snapshot,out error) 후
Binding.Bind(controller,catalog,currentPin,actualSortingLayer,source,out error)를 호출한다.
프로덕션에서는 선택 인수 allowExampleSource를 생략하거나 false로 둔다.
Publish와 Binding은 데이터 복사를 사용하므로 원본 배열 수정만으로 갱신되지 않는다. 새 스냅샷을 발행하라.

BoardSnapshot의 정확한16필드:
schemaVersion=1,packageId,sourceManifestSha256,boardId,revision,isWholeBoard=true,isExampleOnly=false,
originX,originY,originZ,cellSize,seed,postSpacing(3또는4),networkCells,groupPlacements,allowedDecorCells.
networkCells의8필드:x,y,logicalLayer,heightKey,sortingOrder,worldZ,family,connectionGroup.
groupPlacements의8필드:placementId,groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
allowedDecorCells의4필드:x,y,logicalLayer,heightKey. null/누락은 거절, 의도적 빈보드는 빈배열을 준다.
origin은 셀(0,0)의 왼쪽 아래 월드원점이다. 전체 보드를 전달하고 카메라/청크 일부로 잘라 전달하지 않는다.
검사 예시의 허용칸을 프로덕션에 복사하거나 요청칸을 스스로 승인하는 return true를 쓰지 말라.
호스트의 장식 허용 공간을 순수조회한다. 조회 중 점유예약/게임데이터 변경을 하지 않는다.
다칸의 모든칸이 허용되고 중복없어야 그룹당 Renderer하나를 생성한다.

4. 실제 갱신과 풀
로드/생성완료/확정편집/Undo/Redo/재시작/씬재진입/풀재사용 뒤 최신 전체 Snapshot을 Publish한다.
Binding의 Source.Changed 구독 경로를 쓴다. Start/Update 생성기나 매프레임 랜덤 장식은 추가하지 않는다.
취소된 편집/무효입력에서 게임 모델이 바뀌지 않으면 새 비용/턴을 만들지 않는다.
PublishedSource가 비활성화되면 옛 권위 데이터가 무효화되므로 재활성 뒤 freshPublish가 필수다.
AuthoredSource는 지정한 정적 BoardAsset 경로다. 실제 게임 이벤트 어댑터의 대체물이 아니다.
실패 시 자기소유 시각물만 지우고 진단한다. 잘못된pin/누락PNG를 옛 그림으로 대체하지 않는다.
옛Animator/풀초기화/코루틴이 새Renderer를 다시 덮지 않도록 쓰기책임을 단일화한다.

5. 크기·방향·연결·테마 마감
월드+Y=N,이웃N1E2S4W8. family+connectionGroup+logicalLayer+heightKey가 모두 같을 때만 연결한다.
한띠는 같은 connectionGroup을 공유하며 각 whole-group placementId는 고유하다.
FromRowDownCell(col,row)=(col,-row), FromRowDownGroupBottomLeft(left,top,h)=(left,-top-h+1),
RowDownTopLeftToWorldOrigin(boardTopLeft,cellSize)를 실제 행좌표 모델에 맞게 사용한다.
그룹 anchor왼쪽아래+지면중심(anchorX+w/2,anchorY+h/2), 개별PPU256/pivot/logicalRect를 유지한다.
전체PNG/alphaBounds를 한칸에 fit하지 않는다. 부모변환의 양수배율을 보정해 균일월드셀을 유지하고
회전/반전/비정상변환은 진단한다. 부모변경 후 같은Snapshot이어도 Refresh가 검증하게 한다.
첫2계열 보조기둥은 끝/모서리/갈림길에서 떨어진 긴직선에만 월드고정간격으로 둔다.
A=coast_sand,W=wood_bridge,S=stone_bridge. 기존 공유다리난간/꼭짓점기둥/연결하부를 중복시키지 않는다.
새 수리선/등불/어망/계류 말뚝을 나룻배/구르는 통/폭탄/이탈 가시 같은 기능 장치로 자동매핑하지 않는다.
beached_skiff_3x2는 육지의 정적 수리선과 받침/공구 묶음이며 실제 플레이어의1칸 나룻배가 아니다.
실제 물칸은 육지가 아니며 장식 허용 공간이 자동으로 아니다. tidepool_garden은 정적 화단/수반 그림이다.
각진 종단·모서리·T·십자·다칸 지면의두줄깊이/발치마감이 실제 셀경계와 맞는지 확인한다.
표시 이름은 별너울 해안이다. 저장ID를 바꾸지 않고 기존 움직이는 야자수 리그를 정적 소품으로 덮지 않는다.
별의거의고정/구름뒤순서/깊이반응/달·행성없음과 기존 최신배경을 보존한다.
석양해+수평선은 정확히1개씩 월드고정이며 BackgroundRig에서 제외한다. cameraMultiplier0만으로 월드고정이라고 해석하지 않는다.
원경 OceanField와 실제 물칸/배 경로 렌더러를 구분하며 수평선/해를 반복하지 않는다.

6. 레거시 이관과 검증
실제 기존 장식의 GUID/fileID/논리점유/사용처를 새26키에 검토 매핑한다. 크기가다른 장식은
새 전체점유가 허용되는 배치로 조정하고 강제축소/길막/무검토자동치환을 하지 않는다.
Scene/Prefab/SO/Resources/Addressables/문자열로딩/원격/Atlas/Animator/캐시/풀의 도달가능한
해안 장식 옛참조를 조사하라. 참조0가 확인된 해당미사용시각물만 dry-run목록후 정리한다.
다른테마/공유자산/타일/배경/장치/.meta를 통째삭제하지 않는다.
실제Import/C#컴파일 오류0,MissingScript/Sprite0,PPU/pivot/GUID/fileID/manifestSHA일치,
두검사씬 Play와26종/6계열접합/20묶음1Renderer, 실제보드갱신/부분거절/중복거절/틀린pin거절,
PublishedSource비활성후옛상태미복구/freshPublish복구,Undo/Redo/재시작/풀/씬재진입을 확인한다.
원배열Publish후수정이캡처에새지않음,부모이동/양수배율변경/회전거절/소스교체/구독해제를 확인한다.
Tests~ 선택NUnit을 프로젝트TestRunner에 연결해 실행한다. 미실행을 PASS로 보고하지 말라.
기존해안/공통장치의방향·애니메이션·충돌·비용/턴,나룻배탑승/하선/막힘,구르는통의밀기거리,이탈가시,
폭탄첫유효밀기3표시→자기유효행동2→1→실제host폭발셀목록(임의반경/피해추가금지),다른테마의회귀가없는지 확인한다.

{ID}_PROJECT_APPLY_RESULT.md에 실제 변경파일/어댑터타입/바인딩이벤트표,
26종매핑과예외/GUID·SHA표/컴파일·생성·TestRunner·Play로그/실제스크린샷·변경영상/미확인항목을 제출하라.
이 배포본의 파일·알파·정적소스검수는 실제 프로젝트 실행을 대신하지 않는다.
'''
(O/'README_KO.txt').write_text(readme,encoding='utf-8')
(O/'UNITY_CLI_KO.txt').write_text(cli,encoding='utf-8')
print('Wrote README, Unity CLI, catalog; pin',PIN)
