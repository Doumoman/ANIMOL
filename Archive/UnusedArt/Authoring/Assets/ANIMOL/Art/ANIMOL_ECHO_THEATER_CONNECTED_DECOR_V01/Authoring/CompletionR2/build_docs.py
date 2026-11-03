from pathlib import Path

W = Path(__file__).resolve().parent
P = W / 'baseline_package'
O = W / 'docs_exports'
O.mkdir(parents=True,exist_ok=True)

guide = '''잔영 극장 연결 장식 — 배치 연결 보완본 R2

이번 보완의 범위
장식은26종/PNG118개 그대로다. 기존 PNG와 art manifest/pin을 변경하지 않았다.
이전 V01에는 Controller.Configure/SetLayout의 실제 호출부가 없었고 example_layout.json도
실행용 데이터 형식이 아니었다. R2는 데이터 → Source → Binding → Controller의 실제 호출 경로,
직렬화 가능한 BoardAsset, 변경 이벤트 갱신, 유니티 예시 보드/Prefab/검사 씬 생성기를 추가한다.

원 Unity 프로젝트는 이 작업 환경에 없다. 따라서 실제 캠페인/대전/맵 에디터/로드/undo의
프로젝트별 어댑터 연결 및 Unity 컴파일/플레이는 미실행이다. 아래 생성기 소스가 포함되었다는
뜻이며, 여기서 .asset/.prefab/.unity를 실행 생성·검증했다는 뜻이 아니다.

파일 설치
통합 ZIP의 루트 ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01을 Assets 아래 설치한다.
이전 묶음이 있으면 같은 루트에 병합하고 실제 프로젝트의 기존 .meta/_Generated 자산을 유지한다.
같은 클래스를 가진 두 벌의 패키지 폴더를 남기지 않는다. 원 극장 V03 전체를 다시 덮어쓰지 않는다.
기존 책장/도미노/교대 셔터 등 장치 애니메이션은 이번 정적 장식과 구분해 보존한다.

유니티에서 확인할 순서
1. manifest.json을 선택하고 Tools/ANIMOL/Theater Connected Decor V01/Build example boards…
   창을 연다. Examples/stage_snapshot.json과 Examples/all26_snapshot.json을 고른다.
2. 실제 프로젝트에 존재하는 Sorting Layer를 명시적으로 선택한다. 검사 씬 생성 옵션을 켜면
   패키지 _Generated/InspectionBoards 아래에 Stage와 All26의 별도 검사 씬까지 생성한다.
   기본 작업은 BoardAsset/Prefab 생성이며 기존 게임 씬을 고치지 않는다. 생성 전 열려 있는
   Prefab Stage와 생성 대상 검사 씬을 저장·닫고, 기존 출력 자산의 미저장 수정도 정리한다.
3. All26 검사 씬을 열어 Play한다. 생성기는 AuthoredSnapshotSource와 Binding을 실제 연결하고
   명시적으로 RefreshNow를 호출해 Controller의 배치/렌더러가 만들어지는지 확인하도록 작성되어 있다.
4. Stage는 이전 배치의21종/48연결칸/16묶음이고, All26은26종/78연결칸/20묶음 전체다.
   두 예시는 모두 isExampleOnly=true이며 실제 캠페인 저장 데이터가 아니다.
5. 선택한 Binding에는 Refresh selected binding, Clear selected binding visuals 메뉴가 있다.
   BoardAsset에는 Validate selected board asset 메뉴를 사용한다. Clear는 장식 시각물만 지운다.
   Undo/Redo 편집 갱신은 해당 Inspector의 명시적 옵션 범위에서 사용한다.

새 런타임 연결
- BoardSnapshot: 전체 보드의 평평한 배열 데이터. 허용 셀은 {x,y,logicalLayer,heightKey} 객체다.
  예전 [[x,y],...] 배열을 JsonUtility에 그대로 넣지 않는다. sourceManifestSha256은 현재 art pin이다.
- BoardAsset: 검증된 스냅샷을 직렬화하며 읽기/쓰기 때 복사해 외부 수정이 새는 것을 막는다.
- AuthoredSnapshotSource: 의도적으로 지정한 BoardAsset을 공급한다. 검사 데이터는 검사로 표시한다.
- PublishedSnapshotSource: 실제 게임의 확정된 상태를 PublishSnapshot으로 받는다. 검사 데이터는 거절한다.
  비활성화/풀 반납 뒤에는 이전 데이터가 무효화되므로 실제 호스트가 다시 Publish해야 한다.
- Binding: Source.Changed 구독, 최신 캡처, Controller.Configure와 SetLayout 호출을 담당한다.
  Start/Update 폴링·맵 자동 생성·턴 변경·Collider 추가는 없다. 비활성화/소스 변경/오류에는
  자기 Controller가 소유한 장식만 지운다. 같은 상태의 반복 갱신과 부모 변환도 확인한다.

실제 게임에 연결할 때
실제 원본 맵/장식 모델에서 전체 스냅샷을 만든 뒤 PublishedSnapshotSource.PublishSnapshot을 호출한다.
이 source를 Binding.Bind(controller,catalog,pin,sortingLayer,source,out error)에 연결한다.
원래 프로젝트의 타입/필드/이벤트명은 이 묶음에 지어 넣지 않았다. 실제 프로젝트 CLI가 확인해야 한다.
맵 로드, 확정된 편집, undo/redo, 재시작, 풀 재사용 후 최신 상태를 Publish한다.
처음부터 막힌 무효 입력은 상태가 바뀌지 않았으므로 Publish나 게임 코스트를 발생시키지 않는다.
허용 셀은 기존 호스트의 순수한 장식 허용 판정 결과로 만들고, 요청한 칸을 스스로 승인하는 예시
코드를 실제 게임에 사용하지 않는다. 큰 장식은 모든 칸이 허용돼야 한 번에 배치한다.
isWholeBoard=true, isExampleOnly=false를 실제 상태에 사용한다. 검사 허용 플래그를 프로덕션에서 켜지 않는다.

확인 범위
원화/PPU/pivot/연결 마스크의 기존 검수는 동일 PNG에 대해 유지한다. 이번 보완은 JSON 구조,
전체 보드 점유, 실제 호출부, 소스 이벤트/수명주기, 생성기 소스와 출력 소유 범위를 검수한다.
ALL26_BOARD PNG는 같은 typed JSON으로 합성한 오프라인 참고도이며 Unity 캡처가 아니다.
실제 Unity import/compile/TestRunner/Prefab 생성/검사 씬 Play/프로덕션 바인딩/legacy reference0는
실제 프로젝트에서 별도 실행해야 한다. NOT_RUN을 PASS로 보고하지 않는다.
'''
(O/'COMPLETION_R2_KO.txt').write_text(guide,encoding='utf-8')
readme=(P/'README_KO.txt').read_text(encoding='utf-8')
readme='''ANIMOL 잔영 극장 연결 장식 V01 — 설치 연결 보완 R2

장식26종/PNG118개와 기존 아트 pin은 유지하고, 이전에 없던 실제 배치 호출부를 추가했다.
먼저 COMPLETION_R2_KO.txt와 UNITY_CLI_KO.txt를 읽는다. 새 Examples/의 두 typed 보드를
BoardTools로 가져오면 실제 Unity에서 BoardAsset/Prefab과 선택한 검사 씬을 생성할 수 있다.
Inspector에 Controller 카탈로그만 넣는 방식은 초기화 경로가 아니다. Binding과 Source를 사용한다.
실제 원 프로젝트는 제공되지 않아 프로덕션 바인딩·컴파일·실행 검증은 여전히 미실행이다.

--- 기존 아트/점유 계약 ---
'''+readme
(O/'README_KO.txt').write_text(readme,encoding='utf-8')

cli='''ANIMOL 잔영 극장 연결 장식 V01 — 배치 연결 보완 R2 Unity 적용 명령

목표
이번에는 제공된 소스가 실제로 장식을 표시하는 경로까지 연결하라. 이 패키지의 기존26종/118PNG,
manifest.json과 art pin8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069를 보존한다.
낙일 성소나 다른 테마로 넘어가지 않는다. 장식 모델·입력·턴·코스트·Collider·게임 규칙을 변경하지 않는다.
현재 원 프로젝트에서 실제 실행을 확인해야 하며, 동봉된 오프라인 검수와 PNG를 실행 증거로 쓰지 말라.

1. 설치와 실제 프로젝트 조사
Assets 아래 패키지 루트 ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01 이름을 유지한다.
이전 묶음과 같은 폴더에 병합하고 기존 .meta 및 실제 Unity가 만든 _Generated 자산의 GUID를 유지한다.
같은 C# 클래스를 포함한 두 폴더가 함께 컴파일되지 않게 한다. 원 극장 V03 전체를 다시 설치하지 않는다.
README_KO.txt,COMPLETION_R2_KO.txt,manifest/pin,sprite_import_settings,Runtime/Editor의 신규 소스,
Examples/stage_snapshot.json,Examples/all26_snapshot.json,QA/CompletionR2 자료를 읽는다.
현재 극장 테마 ID,장식 데이터 모델,맵 생성/편집/로드/undo/redo/재시작/풀 재사용 이벤트와 Sorting Layer를
실제 소스로 확인한다. memory_theater 같은 추측 ID나 임의의 프로젝트 필드/이벤트를 만들지 말라.

2. 정확한 카탈로그와 실제 작동 예시를 생성
기존 CatalogBuilder는 카탈로그를 만들고, 새 BoardTools는 typed JSON을 엄격하게 검사해
BoardAsset → AuthoredSnapshotSource → Binding → Controller를 연결한 실제 Prefab을 만든다.
검사 씬 생성은 명시적 옵션이다. 아래는 Bash 형식이며 PowerShell에서는 실행 파일 앞에 &를 붙인다.

"<UNITY_EDITOR_PATH>" -batchmode -projectPath "<PROJECT_PATH>" -executeMethod ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01BoardTools.BuildExamplesFromCommandLine -theaterDecorManifest "Assets/<설치경로>/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/manifest.json" -theaterDecorStageSnapshot "Assets/<설치경로>/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/Examples/stage_snapshot.json" -theaterDecorAllSnapshot "Assets/<설치경로>/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/Examples/all26_snapshot.json" -theaterDecorSortingLayer "<실제 기존 Sorting Layer>" -theaterDecorCreateScenes true -quit -logFile "<PROJECT_PATH>/Logs/theater-connected-decor-r2-build.log"

Editor 메뉴 Tools/ANIMOL/Theater Connected Decor V01/Build example boards…도 같은 입력을 받는다.
소스의 실제 출력 경로를 확인하고 _Generated/InspectionBoards/Stage 및 All26의 BoardAsset/Prefab,
옵션으로 생성한 .unity와 보고서를 제출한다. Scene 생성 옵션이 false이면 씬은 만들지 않는다.
생성 전 열려 있는 Prefab Stage를 저장하고 닫는다. 생성 대상 검사 씬이 이미 열려 있거나
소유한 출력 자산에 저장하지 않은 수정이 있으면 먼저 저장·닫기 상태를 정리한 뒤 재실행한다.
기존 생산 씬이나 사용자의 저장하지 않은 씬을 변경하지 말라. 카메라/선택 조명은 새 검사 씬 안에만 둔다.
출력 소유 표식이 다른 기존 파일을 덮어쓰지 말라. 실제 Unity API가 만든 GUID/fileID만 사용한다.
생성기는 명시적 RefreshNow와 CurrentPlan/렌더러 수를 검사한다. 그 코드가 있다는 사실과 실제 생성 성공을
구분하고, enabled/active/실제 Sprite 준비 여부까지 확인한 임포트/컴파일/생성 로그를 남긴다. 실패 로그를 숨기고 완료로 보고하지 말라.

3. 두 검사 보드에서 실제 표시 확인
Stage: 기존 예시의48연결칸/16묶음/88점유/21종. All26:78연결칸/20묶음/123점유/26종.
All26에는 기존 Stage에 없던 folded_chair,mask_pedestal,sandbag_stack,script_pile,
stacked_props_2x2가 포함된다. Catalog의 마스크/보조 기둥은 새로운 장식 종류로 세지 않는다.
두 JSON은 schemaVersion1의16개 필드를 가지며 허용 셀은 평평한 객체 배열이다.
오래된 example_layout.json은 합성 예시 자료이고 새 strict importer의 입력이 아니다.
두 typed 보드는 isExampleOnly=true이다. 실제 게임 저장 상태로 가져오지 말라.
검사 씬을 열어 Play하여 Source 캡처 → Binding.RefreshNow → Controller.Configure/SetLayout의
실제 호출과 Renderer/Sprite/GUID/PPU/pivot을 확인한다. 실제 스크린샷과 로그를 남긴다.

4. 실제 게임의 확정 데이터에 PublishedSnapshotSource 연결
같은 장식 전용 GameObject의 Controller와 Binding을 사용한다. Binding이 다른 오브젝트의
Controller나 Renderer를 정리하게 하지 말라. 실제 모델 공급자는 PublishedSnapshotSource를 사용한다.
호스트가 실제 장식 데이터와 허용 셀에서 스냅샷을 만들어 PublishSnapshot(snapshot,out error)로
먼저 전달한 뒤 Binding.Bind(controller,catalog,pin,actualSortingLayer,source,out error)를 호출한다.
프로덕션에서는 마지막 allowExampleSource 인수를 생략하거나 false로 한다. true로 검사 데이터를 통과시키지 않는다.

BoardSnapshot 필드
schemaVersion=1, packageId=ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01, sourceManifestSha256=현재pin,
boardId=실제 보드의 식별값, revision=호스트의 데이터 리비전, isWholeBoard=true,isExampleOnly=false,
originX/Y/Z=셀(0,0)의 왼쪽 아래 월드 원점, cellSize=실제 셀 월드 크기, seed,postSpacing=3또는4,
networkCells,groupPlacements,allowedDecorCells의 명시적 배열.
networkCells는 x,y,logicalLayer,heightKey,sortingOrder,worldZ,family,connectionGroup.
groupPlacements는 placementId,groupKey,anchorX,anchorY,logicalLayer,heightKey,sortingOrder,worldZ.
allowedDecorCells는 x,y,logicalLayer,heightKey. 배열 null과 누락은 거절한다. 의도적인 빈 보드는 빈 배열로 전달한다.

실제 타입/필드에서 위 형식으로 변환하는 얇은 어댑터를 작성하고 소스 위치를 제출한다. 이 어댑터에
표시용 예시의1920허용 셀을 복사하거나 요청한 칸을 스스로 승인하는 코드, return true를 넣지 말라.
호스트의 현재 장식 허용 공간을 순수 조회한 결과를 스냅샷으로 만든다. 게임 점유를 이 조회 안에서
예약/변경하지 않는다. 모든 multi-cell 칸이 허용돼야 그룹당1개만 생성한다.
스냅샷은 카메라/청크 일부가 아닌 전체 장식 보드다. Source와 Binding은 외부 배열 변경이 새지 않도록
복사하므로 실제 모델이 바뀐 후에는 새 스냅샷을 다시 Publish해야 한다.

5. 변경·로드·Undo·풀의 실제 이벤트 연결
맵 로드/생성 완료, 확정 편집,undo/redo,재시작,씬 재진입,풀 재사용 후 최신 스냅샷을 Publish한다.
Binding은 Source.Changed를 구독하고 자동으로 최신 상태를 캡처해 실제 Controller를 갱신한다.
별도의 Start/Update 폴링, 매 프레임 랜덤, 애니메이션 종료에 의한 턴 진행을 추가하지 말라.
무효 입력이나 취소된 편집은 권위 모델이 바뀌지 않았으므로 새 Publish/비용을 발생시키지 않는다.
PublishedSource가 비활성화되면 옛 권위 데이터는 무효화된다. 풀에서 꺼낸 뒤 반드시 현재 보드 상태를
새로 Publish한다. 예전 그림이 자동 복구된다는 가정으로 두지 말라. AuthoredSource는 명시적으로
지정한 정적 BoardAsset을 사용하는 경로이며 권위 모델을 자동으로 읽는 범용 어댑터가 아니다.
실패 시 Binding은 자기 소유 그림만 지우고 진단을 남긴다. 누락/틀린pin을 옛 PNG로 대체하지 않는다.
기존 Animator/코루틴이 같은 Renderer를 다시 쓰지 않도록 그림 쓰기 책임을 한 곳으로 이관한다.

6. 좌표·크기·연결 규칙 보존
월드+Y=N,4이웃 N1 E2 S4 W8이다. 같은 family/connectionGroup/logicalLayer/heightKey만 연결한다.
연결된 띠 전체가 connectionGroup을 공유하고, 큰 그룹/한 칸 소품은 각 placementId가 고유하다.
행이 아래로 늘어나면 FromRowDownCell(col,row)=(col,-row),
FromRowDownGroupBottomLeft(left,top,h)=(left,-top-h+1),
RowDownTopLeftToWorldOrigin(boardTopLeft,cellSize)를 사용한다.
그룹 anchor는 왼쪽 아래, 지면 중심은 anchor+(w/2,h/2)이다. PNG pivot은 logicalRect 중심이다.
PPU256/개별 캔버스/pivot/전체 점유를 유지하고 alpha bounds나 PNG 전체를 한 칸에 fit하지 말라.
부모 양수 X/Y 배율은 축별 상쇄해 균일한 월드 셀 크기를 유지하고, 회전/반전/비정상 변환은 진단한다.
부모가 이동/스케일 변경된 뒤에도 같은 스냅샷이라고 잘못 건너뛰지 않고 Refresh가 검증하게 한다.
보조 기둥은 선언된 첫2계열의 긴 직선에서만 월드 고정 간격으로 선택된다. 끝/모서리/갈림길 옆에 덧붙이지 않는다.
기존 W다리 난간/공유 꼭짓점 기둥 owner와 새 객석 장식 owner를 중복시키지 않는다.
G=stage_floor,W=wood_catwalk,S=velvet_carpet이며 S의 지지대는 G로 정규화한다.

7. 기존 장치와 최신 아트/레거시 이관 범위
장식 curtain screen/prop shelf/puppet storage는 작동 교대 셔터/회전 책장/겁쟁이 인형이 아니다.
책장 H2×1↔V1×2 중간 회전, 도미노의 순차 넘어짐, 기존 공통 문/바람/무빙워크/제동판 등
최신 동작/애니메이션/점유를 이번 정적 장식으로 덮지 않는다. 실제 게임의 새 기능이나 충돌을 만들지 않는다.
기존14소품 중6개가 이미 multi-cell이었다. 실제 old GUID/fileID/논리 점유/사용처와 새26키를 대응한다.
organ2×2→piano3×2,seat_pair2×1→bench3×1,scenery2×2→new3×1처럼 크기가 달라지면
새 점유 전체의 허용 여부를 확인하고 실제 배치를 수정한다. 임의 축소나 경로 차단을 하지 않는다.
기존 footlight on/dim/off가 기능 상태이면 정적 strip으로 덮지 않는다. 미확인 자동 매핑을 하지 않는다.
씬/Prefab/SO/문자열 로딩/Resources/Addressables/원격/SpriteAtlas/Animator/캐시/풀을 조사하고
극장 장식의 접근 가능한 옛 참조만 최신 카탈로그로 이관한다. 실제 참조0를 증명한 해당 옛 시각물만
dry-run 목록 후 정리하고 다른 테마/장치/타일/배경의 공유 자산과 .meta는 일괄 삭제하지 않는다.

8. 실제 결과 검증과 제출
- import/compile 오류0,Missing Script/Sprite0,실제 Catalog/BoardAsset/Prefab/Scene GUID/fileID 일치.
- Stage/All26 생성 성공과 두 씬의 실제 Play,6계열 접합·20소품 점유·그룹당 Renderer1개 확인.
- 실제 캠페인·대전·에디터·로드·undo/redo·재시작·풀에서 최신 모델→Publish→Binding→Controller 호출 증거.
- 원본 배열을 Publish 후 수정해도 기존 캡처가 바뀌지 않음; 한 칸 거절/중복/누락/잘못된pin이 전체 거절됨.
- PublishedSource 비활성/풀반납후 옛 상태가 복구되지 않으며 freshPublish로만 다시 표시됨.
- 반복동일갱신/부모이동·스케일/회전거절/소스교체/구독해제/빠른변경/씬재진입 확인.
- 프로덕션 Source의 exampleOnly거절, 실제 호스트 미연결을 검사 씬 생성 성공으로 숨기지 않음.
- 기존 책장·도미노·공통/극장 장치·게임 경로·코스트·다른 테마 회귀 없음.
선택 NUnit 소스는 Tests~에 있다. 실제 TestRunner로 실행하고 미실행을 PASS로 표시하지 말라.

ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01_PROJECT_APPLY_RESULT.md에 실제 변경 파일/씬/Prefab,
호스트 타입/이벤트 바인딩표,26종 매핑/예외,GUID/fileID/SHA표,생성/컴파일/TestRunner/Play로그,
실제 화면/갱신 영상,미확인 항목을 제출한다. 이 환경의 합성 PNG와 소스 검수는 Unity 실행 증거가 아니다.
'''
(O/'UNITY_CLI_KO.txt').write_text(cli,encoding='utf-8')
print([(p.name,p.stat().st_size) for p in O.iterdir()])
