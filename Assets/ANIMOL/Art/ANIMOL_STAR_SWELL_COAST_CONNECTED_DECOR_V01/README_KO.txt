ANIMOL 별너울 해안 연결 장식 V01

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
manifest_sha256.txt의 현재 pin은 e97145e09c089d39307f1cad34190ac1350eaaab4f2cfc1266bebfa668897767이다. 정확한 manifest와 PNG allowlist만 사용한다.
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
