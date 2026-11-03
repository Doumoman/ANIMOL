ANIMOL 잔영 극장 연결 장식 V01 — 설치 연결 보완 R2

장식26종/PNG118개와 기존 아트 pin은 유지하고, 이전에 없던 실제 배치 호출부를 추가했다.
먼저 COMPLETION_R2_KO.txt와 UNITY_CLI_KO.txt를 읽는다. 새 Examples/의 두 typed 보드를
BoardTools로 가져오면 실제 Unity에서 BoardAsset/Prefab과 선택한 검사 씬을 생성할 수 있다.
Inspector에 Controller 카탈로그만 넣는 방식은 초기화 경로가 아니다. Binding과 Source를 사용한다.
실제 원 프로젝트는 제공되지 않아 프로덕션 바인딩·컴파일·실행 검증은 여전히 미실행이다.

--- 기존 아트/점유 계약 ---
ANIMOL 잔영 극장 — 연결 장식 확장 V01

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
- 벨벳 객석 통로 울타리 / velvet_aisle_barrier / 연결형 1×1 / 이웃에 따라 연속
- 조각 무대 난간 / carved_stage_balustrade / 연결형 1×1 / 이웃에 따라 연속
- 연결 소품 진열대 / prop_shelf_run / 연결형 1×1 / 이웃에 따라 연속
- 벨벳 장막 파티션 / velvet_curtain_screen / 연결형 1×1 / 이웃에 따라 연속
- 황동 무대 발치등 / brass_footlight_strip / 연결형 1×1 / 이웃에 따라 연속
- 덩굴 화분 띠 / ivy_planter_strip / 연결형 1×1 / 이웃에 따라 연속
- 그랜드피아노 무대 / grand_piano_3x2 / 3×2
- 객석 벤치 / audience_bench_3x1 / 3×1
- 매표소 / ticket_booth_2x2 / 2×2
- 무대 의상 행거 / costume_rack_3x1 / 3×1
- 분장대 / vanity_dressing_2x1 / 2×1
- 축음기 탁자 / phonograph_table_2x1 / 2×1
- 무대 배경 가벽 / scenery_flat_3x1 / 3×1
- 무대 소품 보관 묶음 / stacked_props_2x2 / 2×2
- 가면 진열대 / mask_display_2x1 / 2×1
- 꼭두각시 보관함 / puppet_storage_2x2 / 2×2
- 무대 뒤 조명 묶음 / backstage_spotlight_duo_2x1 / 2×1
- 무대 계단 / stage_steps_2x1 / 2×1
- 입장권 안내대 / ticket_stand / 1×1
- 접이식 의자 / folded_chair / 1×1
- 악보대 / music_stand / 1×1
- 장식 촛대 / tall_candelabra / 1×1
- 가면 받침대 / mask_pedestal / 1×1
- 극본 더미 / script_pile / 1×1
- 무대 모래주머니 / sandbag_stack / 1×1
- 극장 꽃 항아리 / flower_urn / 1×1
