별너울 해안 V01 — 조종 나룻배 (도감11)

116 runtime PNG /32 clips /20 최신 endpoint 별칭 /3 새 native 원화
- devices/steerable_ferry/{east,north,west,south}/rowing/frame_00..02.png
- 같은 방향 아래 arrive/blocked/boarding/disembarking/frame_00..03.png
- idle.png, arrived.png, blocked.png, boarded.png, disembarked.png는 현재 endpoint와 바이트 동일한 별칭
- fx/boat/wake/{heading}/frame_00..03.png: 독립된 4방향 물결
- fx/boat/landing/{heading}.png: host의 실제 육지 상호작용 방향을 표시하는 수면 표식

클립 키
steerable_ferry.{heading}.{idle|rowing|arrive|blocked|boarding|disembarking}
ferry_wake.{heading}.flow
ferry_landing.{heading}.ready
모든 clip12fps. rowing/wake만 loop; 나머지는 visual feedback 단발입니다.

원화와 등록
NEW built-in imagegen으로 만든 4방향 hull, 별도 노3개 native 포즈, 방향별 물결/표식입니다.
각진 뱃머리, 긴 직선과 큰 모서리 절단, 평평한 plank/strap/사각 brass hardware를 사용합니다.
기존 픽셀 원화/성소 원화를 복사·재색칠·필터링하지 않았습니다.
원화 RGB 변경 없이 crop, alpha거터 분리, uniformresize, native부품 조립만 했습니다.
원본의 alpha0 배경에는 보이지 않는 RGB haze가 남을 수 있으나 runtime에는 투명하게 표시됩니다.

320×320, PPU256, pivot[0.5,0.5], logicalRect[32,32,256,256], 시각 슬롯1×1.
이 시각 슬롯을 실제 gameplay 점유나 collider로 등록하지 마세요. 실제 footprint/ID/Prefab은 미확인(null)입니다.
배·노·물결의 실제 alpha도 현재 시각 슬롯 내부에 들어갑니다.
seatAnchorPixel=[160,160], seatWorldOffset=[0,0,0]은 모든 방향/상태에서 고정입니다.
native hull의 실제 rowlock에 노를 등록했으며, 해당 방향의 rowlock은 모든 포즈와 상태에서 고정입니다.
production/native_geometry_proof.json 및 components의 native 등록 원화에서 확인할 수 있습니다.

기존 actor와 움직임 연결 (새 캐릭터 원화는 포함하지 않음)
- 기존 host actor visual을 고정 seat anchor에 붙이거나, 배와 동일한 host tween을 적용하세요.
- 승선한 뒤 허용된 물 방향 swipe일 때만 배와 같은 actor가 같은 방향으로 정확히 물1칸을 움직입니다.
- 물 목표 칸이 막히면 배/actor 모두 이동하지 않습니다. blocked clip은 고정된 배 위에 native X 피드백만 표시합니다.
- 유효한 host 육지 칸으로만 하선합니다. actor만 육지로 나가고 배는 마지막 물 칸에 남습니다.
- 막힌 육지 하선 입력은 무효이며 배/actor를 이동시키지 않습니다.
- 별도 비용/턴/자동 이동을 추가하지 않습니다. 원문에 없는 새로운 timing을 만들지 않습니다.
- Sprite 프레임/animation event/clip 종료/타이머로 실제 Transform 또는 논리 칸 이동을 적용하지 마세요.
- Boarding/disembarking는 좌석의 native 기하 표식이며 actor 부착/분리는 기존 host가 승인한 상태를 따릅니다.
- 도감 설명의 '같은 실루엣으로 이동'은 기존 actor visual+배를 한 authoritative tween으로 움직인다는 뜻입니다.

수면/해안 layer 계약
water carrier < ambient/shore/boat wake/landing marker < boat hull 및 land top < existing actor visual.
모든 foam/wake/landing marker는 실제 host water-cell mask로 잘라 육지·다리를 덮지 않도록 하세요.
수면 표식이 새로운 walkable 타일/점유/collider/current를 만들지 않습니다.
Landing marker의 방향은 현재 뱃머리와 독립이며 실제 host의 육지 입력 방향을 사용합니다.
인접 육지의 shore normal을 boat 진행 방향으로 해석하지 마세요.

검수
boat_qa.json은 파일/픽셀/native부품 고정/알파/방향/메타데이터의 PASS_OFFLINE입니다.
inspection/boat_four_headings.png, boat_all_states.png, boat_water_feedback.png, boat_motion.gif는 오프라인 원화 확인용입니다.
실제 Unity import, C# compile, shader/NUnit, Prefab binding, Play Mode는 NOT_RUN입니다.
