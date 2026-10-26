잔영 극장 연결 장식 V01 — 큰 소품 B / 독립 소품

큰 소품 6종
scenery_flat_3x1: 3×1 무대 배경막 세트
stacked_props_2x2: 2×2 무대 소품과 트렁크 보관 구역
mask_display_2x1: 2×1 희극·비극 가면 전시대
puppet_storage_2x2: 2×2 마리오네트와 천 보관 구역
backstage_spotlight_duo_2x1: 2×1 무대 조명 두 개를 묶은 받침대
stage_steps_2x1: 2×1 무대 계단

독립 소품 8종
ticket_stand: 표 받침대 / folded_chair: 접어 보관한 의자
music_stand: 악보대 / tall_candelabra: 키 큰 촛대
mask_pedestal: 단일 가면 받침 / script_pile: 대본 더미
sandbag_stack: 무대 추 모래주머니 / flower_urn: 꽃 화분

모든 소품은 신규 생성한 실제 PNG이며, 색상만 바꾼 변형을 별도 종류로 세지 않는다.
큰 소품은 정해진 크기 전체를 한 이미지와 한 Renderer로 배치한다. 셀마다 반복 생성하지 않는다.
8개 독립 소품은 연결 부품이 아니다. 필요하면 연결형 장식과 큰 소품 사이에 함께 배치한다.
ppu=256, 논리 바닥 사각형과 바닥 중심 pivot은 manifest.json을 따른다.
소품의 PNG 바깥 여백이나 키 큰 부분으로 게임 점유·Collider 크기를 다시 계산하지 않는다.
접힌 의자는 원화 시트의 열린 초안을 사용하지 않고 별도 folded_chair_native.png를 사용한다.
조명은 정적 장식이며 빛 감지기·방향 장치·무대 전환 규칙을 새로 만들지 않는다.
보관된 인형은 장식이며 기존 겁쟁이 인형 장치를 교체하지 않는다.

원화와 프롬프트
sources/ 에 imagegen 원본 7개, Authoring/prompts.json 에 사용한 요청 7개를 보존했다.
spotlights_steps_draft_native.png는 생성 초안으로 runtime에서 참조하지 않는다.
원화 시트에서 지정한 영역과 주요 알파 연결 성분을 기계적으로 잘라서 균일 확대·축소하고 정렬했다.
흩어진 알파 미세 점과 완전 투명 픽셀의 RGB만 정리했다. 새 RGB를 그리거나 색을 치환하지 않았다.
Inspection/groups_b_contact_sheet.png는 원화의 합성 검토 이미지이며 실제 Unity 실행 캡처가 아니다.

stacked_props_2x2는 두 깊이 줄이 실제로 그려진 새 원화로 교정했다. 상단 한 줄이 비지 않으며,
논리 바닥 영역 안의 폭·높이 채움 모두 90% 이상, 각 타일 행의 불투명 점유율 35% 이상을 확인했다.
모서리의 바깥 윤곽선은 좌우 4px만 논리 바닥 밖으로 나가고, 캔버스·점유·pivot·PPU는 그대로다.
나머지 13개 PNG의 SHA-256은 교정 전과 완전히 같다.
오프라인 PNG 디코드, SHA-256, 투명 여백, 단일 비율 축소를 확인했다.
실제 Unity 임포트·컴파일·플레이·장면 연결은 이 환경에서 실행하지 않았다.
