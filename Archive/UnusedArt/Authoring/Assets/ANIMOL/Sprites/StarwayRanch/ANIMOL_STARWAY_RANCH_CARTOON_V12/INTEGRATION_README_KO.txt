별길 목장 V12 표현 계층 — 구현 범위와 연결 계약

이 소스는 namespace ANIMOL.Art.V12의 시각 자료 카탈로그·프레임 재생기·선택적 풍차 날개
회전기와 Editor 자산 빌더다. 실제 게임 프로젝트와 장치 이벤트 어댑터·Scene·Prefab·Controller는
제공되지 않았다. 이 환경에서 C# 컴파일·Unity 실행·NUnit 실행은 수행하지 않았다.
프로젝트 전체의 구버전 참조 제거·실제 장치 바인딩이 완료된 것으로 보고하지 않는다.

필수 입력
V12 폴더를 정확히 ANIMOL_STARWAY_RANCH_CARTOON_V12로 두고 루트에 animation_catalog.json을 둔다.
필수 JSON:
schemaVersion:1
packageId:"ANIMOL_STARWAY_RANCH_CARTOON_V12"
sprites:[{file,width,height,ppu,pivot:[x,y],sha256}]
clips:[{key,family,heading,state,fps,loop,frames:[relativePngPath]}]
parts:[{key,file}]는 선택 사항이다. 추가 parts 필드는 원화 메타 자료로 보관할 수 있다.
Sprite 메타는 sprites[]가 기준이며 프레임/part 파일은 이 표에 등록되어 있어야 한다.
fps는 float다. walk/move는 10fps 이상·실제 프레임2장 이상이어야 한다.
정지 프레임 공유·끝점 반복·역순 재사용으로 같은 경로가 여러 clip에 나오는 것은 허용한다.
clip key 및 family/heading/state 조합은 중복할 수 없다. heading은 정확한 문자열로 조회한다.
없는 방향을 None이나 다른 방향으로 자동 대체하지 않는다.

Editor 빌드
V12 정확한 PNG 임포터를 먼저 적용한다. 이 빌더는 임포트 값을 임의 수정하지 않는다.
animation_catalog.json을 선택해 Tools/ANIMOL/Starway Ranch V12/Build selected animation_catalog.json을 실행한다.
배치 실행의 예시는 아래와 같다. Unity 실행 파일·프로젝트 경로·Assets 하위 경로는 실제 프로젝트에 맞춘다.
Unity -batchmode -quit -projectPath <PROJECT_PATH> -executeMethod ANIMOL.Art.V12.StarwayRanchV12AnimationBuilder.BuildFromCommandLine -ranchV12Manifest Assets/ANIMOL_STARWAY_RANCH_CARTOON_V12/animation_catalog.json -logFile <LOG_PATH>
BuildFromCommandLine은 인자 누락·중복·실패를 예외로 보고하며 실패를 성공 종료로 바꾸지 않는다.
빌더는 JSON 필드 존재/타입/중복·패키지·경로·PNG SHA256·Sprite Single·크기·PPU·피벗을 확인한다.
성공하면 _Generated/StarwayRanchV12VisualCatalog.asset과 SpriteRenderer.m_Sprite 곡선만 있는
프레임 .anim을 생성한다. .anim 바인딩 경로는 ""이므로 해당 Clip은 visual SpriteRenderer와
같은 GameObject를 기준으로 한다. Animation Event·Transform 곡선·Controller는 만들지 않는다.
이미 생성된 동일 자산은 내용을 갱신해 GUID를 유지한다. 옛 파일을 자동 삭제하지 않는다.
_Generated/animation_build_report.json은 자산 빌드 결과이며 projectBindingsVerified와
legacyReferenceAuditComplete는 false다. 생성 실패/부분 생성은 예외와 보고서로 알린다.

런타임 연결
1. 실제 시뮬레이션의 방향·확정 상태·이동 경로·유효 행동 이벤트를 먼저 찾는다.
2. visual 자식의 SpriteRenderer와 카탈로그를 StarwayRanchV12AnimatedVisual.Configure에 전달한다.
   세 번째 인자는 실제 빌더 보고서의 manifestSha256이다. 빈 문자열·다른 SHA256·다른 루트/
   패키지·카탈로그 fingerprint는 오류다. 자동으로 카탈로그 내부 해시를 복사해 검증을 생략하지 않는다.
3. 유효 행동에서만 PlayForCommittedAction(key,true,actualDuration,...)을 호출한다.
   false는 clip·현재 프레임·위상을 전혀 바꾸지 않는다. 막힌 입력에서 Play를 직접 호출하지 않는다.
4. 상태 루프/전환은 SetClip(key,restart,playReverse) 또는 Play(key,durationSeconds,...)를 사용한다.
   restart=false이면 같은 key와 방향의 현재 위상을 보존한다. 유효 행동의 반복 바람은 필요할 때
   restart=true로 다시 시작할 수 있다. -1은 fps 시간, 양의 duration은 실제 행동/트윈 시간이다.
5. 실제 트윈이 진행률을 공급하면 ApplyNormalizedProgress(key,progress,playReverse)를 사용한다.
   진행률은0~1로 제한하고 자동 시간을 더하지 않는다. 재생 중인 key에 progress만 줄 수도 있다.
   SnapToState는 undo/reset/로드 등 권위 상태 변경에 즉시 맞추고 대기 동작을 취소한다.
   Cancel은 마지막 프레임을 유지하며 자동 진행을 중단한다. 비루프는 마지막 프레임을 유지한다.
6. SetPaused로 정지한다. 자동 시간은 Time.deltaTime만 사용하므로 timeScale0에서도 멈춘다.
   pause 중 Play/ApplyNormalizedProgress는 동작하지 않는다. 명시적 SnapToState만 상태 재바인딩을 허용한다.
   clip 완료 callback/event는 제공하지 않는다. 시각 완료를 기다려 이동·턴·코스트를 진행시키지 않는다.
7. 동일 SpriteRenderer에 쓰는 구 Animator/Animation/코드의 Sprite 곡선은 명시적으로 비활성화하거나
   바인딩을 제거한다. LateUpdate/Animator가 프레임 플레이어의 결과를 덮지 않아야 한다.
   다른 몸체·캐릭터 로직까지 통째로 끄지 말고 실제 writer 범위를 확인한다. 생성 .anim을 기존
   Animator에 쓸 경우 프레임 플레이어와 동시에 같은 Renderer를 구동하지 않는다.

최신 자료가 없거나 해시가 틀리면 검은색/자홍색 진단 Sprite와 오류를 표시한다. 옛 원화나
기본 방향으로 fallback하지 않는다. 필수 카탈로그·프레임이 손상되면 build guard가 실패한다.
프로젝트의 권위 V12 카탈로그는 정확히1개여야 하며 원본 V12 폴더의 _Generated 위치와 일치해야 한다.
에디터에서는 원본 PNG bytes와 Sprite GUID/fileID까지 검사한다. 런타임은 unreadable PNG의
원본 bytes나 Editor GUID를 다시 읽지 못하므로 pin된 manifest 해시·bake된 데이터 fingerprint·
Sprite 메타와 참조를 검사한다. 실제 원본 파일 검사는 Editor/build 단계가 담당한다.

풍차
StarwayRanchV12RotorVisual은 visual owner의 엄격한 하위 자식이며 SpriteRenderer가 있는
날개 Transform만 회전한다. 날개 또는 그 자식에 Collider/Rigidbody가 있으면 오류다.
날개의 원점은 원화에서 정한 허브 피벗에 맞춰 놓는다. 몸체·gameplay root·Collider는 돌리지 않는다.
기본 회전 속도는 초당18도이며 시각 연출용 값이다. 실제 바람 장치의 이동 방향이나 코스트를 결정하지 않는다.
SetPaused/SetReducedMotion을 기존 설정에 연결한다. Rebase는 의도한 날개 재배치 뒤 호출한다.
풍차 장식과 실제 wind_corridor 장치는 이름으로 자동 연결하지 않는다.

이관/검수
integration_design.md의 GUID/fileID 보존·Scene/Prefab/SO/Addressables/Resources/SpriteAtlas/
AnimatorOverride/Pool/EditorSpawner/런타임 fallback 추적 순서를 따른다. 최신 키가 빠지면
보고하고 가시적으로 실패하며 구버전으로 후퇴하지 않는다. 역참조0 확인 전 기존/공유 파일을 삭제하지 않는다.
공통 장치가 다른 테마와 공유되어 있으면 목장 시각 바인딩만 분리한다. 잔영 극장을 변경하지 않는다.
Tests~의 NUnit 소스는 실제 프로젝트의 Editor 테스트 어셈블리에 넣어 실행한다.
그 테스트 어셈블리는 새 Runtime 소스와 Editor 빌더가 속한 어셈블리를 참조해야 하므로 실제 asmdef 구조를 확인한다.
타이밍 경계·반전·진행률·정확한 방향 조회·잘못된 루트/해시·중복 키·막힌 입력·일시정지·
최신 키 누락·JSON 필드 누락/중복을 검사하며 실행 전에는 통과했다고 보고하지 않는다.
