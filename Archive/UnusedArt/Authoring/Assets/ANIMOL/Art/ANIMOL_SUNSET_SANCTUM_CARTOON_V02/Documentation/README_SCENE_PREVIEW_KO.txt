낙일 성소 전체 원화 검토 씬

SANCTUM_FULL_SCENE_PREVIEW.png:1080x1920의 전체 장면.
SANCTUM_GRID_FOOTPRINT_REVIEW.png:같은 장면에 논리 칸과 장식의 ground/art-slot 경계를 겹쳐 확인하는 검사 이미지.
SANCTUM_FULL_SCENE_MOTION.gif:720x1280,6초,48프레임의 오프라인 원화 동작 검토.

모든 생산 원화는 최신 ANIMOL_SUNSET_SANCTUM_CARTOON_V02 패키지의 PNG를 그대로 읽고, 선언된 PPU256/pivot/logicalRect를 기준으로 균일 확대·축소·합성했다. 기존 CLOCK_DOME 픽셀 이미지나 새 임의 캐릭터는 사용하지 않았다. 광선과3칸 도약 점선은 경로를 읽기 위한 정확한 소프트웨어 참조 기하이며 별도 생산 스프라이트를 코드로 그린 것이 아니다.

판은9x12격자에88개 보행면을 놓은 원화 검토 fixture다. 기존 게임에 플레이 가능한 퍼즐을 추가한 결과가 아니다. 목재3칸·석재2칸 다리는 실제 playable tile 연결로 배치했고,10개 빈칸은4개의 내부 홀로 유지했다. 내부 남향 노출면은 shallow cap, 외부 남향 노출면은 matching1/3/4칸 결합 바디로 마감했다. 바디는 추가 점유를 만들지 않는다.

시각 상태
광원은 동쪽으로 빛을 내고, 온전한 유리를 통과한 뒤 slash 거울에서 북쪽으로 꺾여 켜진 감지기에 닿는다. 연결 문은 열린 상태로 유지한다. 이 전체 장면에서는 거울 회전/유리 파괴를 임의 재생하지 않는다. 잘못된 전환 중에도 계속 같은 광선을 그리는 모순을 피하기 위한 고정 광학 fixture다.
초승달 도약판은 같은 방향3칸 뒤 실제 보행면의 착지 문양을 가리킨다. 두 중간 칸은 빈칸이다. 이 점선은 실제 점프 입력/중간 장치 반응의 미확정 규칙을 확정하지 않는다.
포털 두 개는 같은 pair_01 문양이며 동쪽 이동의 출구 다음 한 칸을 표시한다. 자석은 서쪽 입구에서 금속 상자를 동쪽의 자석 쪽으로 끌도록 방향 효과를 놓았다. 목재 상자는 고정 비교 원화다. 실제 힘/포털/충돌/코스트를 이 GIF에서 시뮬레이션한 것은 아니다.
공통 문·무빙워크·얇은ㄴ자 모서리·제동판, 포털, 자석, 초승달판의 native프레임을 검토용 순서로 재생한다. 타이밍은 검토 편의용이며 실제 행동/턴을 진행시키지 않는다.

Unity 검토 씬 입력
review_scene_layout.json은 기존 Sprite pivot을 존중하는 world좌표(+Y위,1칸1world)와 native클립 키를 제공한다. static sprites와 motions는 이름이 겹치지 않는다. background의 z/sorting/parallax/drift를 분리하고, stars는 모든 cloud뒤에서 자동drift0으로 둔다. opacity는 선택 필드이며 미지원 소비자에서는1로 취급한다.
review_scene_effects.json의 gridOrigin/cellSize/lightRoute는 같은 광선을 실제 BeamRenderer에 연결하기 위한 데이터다. worldSegments도 함께 제공한다. mirror는east->north, receiverKey는review_sensor다. 이것은 실제 게임 ID가 아니다. review_map_layout.json은 원화 판의 재현/기하 검수 자료다.

검수
scene_preview_qa.json은 실제 사용한120개 runtime PNG의 SHA/규격/PPU/pivot,88칸 전체 raw neighbor mask,8개 하부 마감 chunk,3칸 착지,포털 출구,단일 렌더러 이름,광선 경로와6초 GIF의48개 고유 decoded프레임을 확인한다. 실제 Unity Import/C#/Shader Compile/Prefab Binding/Play Mode는 NOT_RUN이다. GIF는 공통256색 팔레트와Dither.NONE을 사용하므로 PNG와 달리 미세한 색 양자화가 보일 수 있다. 생산 PNG는 변경하지 않았다.

V02: 현재 최신 각진 원화로 재조립한 오프라인 검토본이다. 게임 논리·점유·방향·클립 키는 유지하며 실제 Unity 캡처가 아니다.
