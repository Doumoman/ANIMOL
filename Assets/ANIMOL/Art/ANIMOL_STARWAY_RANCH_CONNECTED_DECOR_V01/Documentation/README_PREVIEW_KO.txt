별길 목장 연결 장식 — 전체 배치와 타일 변경 예시

RANCH_CONNECTED_DECOR_PREVIEW.png는 실제 새 장식 PNG를 PPU256과 선언된 pivot 그대로 조합한1080×1920 오프라인 예시다. GRID는1칸80px의 검수 격자와 여러 칸 장식의 전체 지면을 표시한다. 실제 Unity 캡처나 완성된 퍼즐 스테이지가 아니다.

예시 전체 스냅샷은 example_layout.json이다. 바닥114칸, 연결 장식45칸, 큰 장식4개이며 큰 장식 하나당 renderer는 하나다. 큰 장식은 왼쪽 아래 anchor 기준2×1/3×1/2×2/3×2 전체 지면을 예약한 뒤 중심에 배치한다. 높이와 투명 여백을 포함한 PNG 전체를 한 칸에 맞추지 않는다. logicalLayer0의 울타리/돌담,1의 화단,2의 큰 장식은 시각 레이어이며 실제 게임의 Collider/통행 규칙을 만들지 않는다.

좌표는+Y North이고 worldOrigin은 셀(0,0)의 왼쪽 아래다. example_layout.json의 networkCells/groupPlacements 필드는 Runtime Planner 입력과 같다. groupPlacements의 widthCells/heightCells는 검수 설명이며 실제 크기는 최신 catalog가 선언한 groupKey로 조회해야 한다. JSON의 크기를 신뢰해 새 점유나 Collider를 만들지 않는다. hostAllowedDecorCells는 이 예시에서만 쓰는 허용 바닥 집합이다.

예시의 기둥/꽃 선택은 C# StableHash의 uint 연산을 그대로 옮겼다. seed와 postSpacing은 JSON에 명시한다. 같은 family/connectionGroup/layer/height만 연결하고, 직선 기둥은 전역 phase 후보의 양옆도 동일 직선인 경우에만 배치한다. 꽃은 hash%5==0인 경우 정렬한 최신 accent 목록의(hash/5)%개수로 고른다. 이는 프레임별 무작위 배치가 아니다.

CONNECTIONS는 고립1칸/2·3·4칸 직선/ㄴ자/T·교차/닫힌 구멍/타일 제거 후 열린 마감을 동일 PNG로 보여준다. EDIT_REPLAY.gif는12개의 전체 셀 스냅샷을500ms씩 재선택한6초 예시다. 정적 장식 선택을 설명하는 GIF이며 새로운 애니메이션/실시간 게임 입력/턴/비용/피해를 실행하지 않는다. 각 스냅샷과 선택 파일은 edit_replay_snapshots.json에 있다.

BEFORE_AFTER의 위쪽은 기존 V12의2×1 울타리 prop을 반복한 참고 배치이고, 아래쪽은 새 연결 부품으로 만든 연속 배치다. 기존 PNG도 이미2×1로 선언되어 있음을 명시했다. 실제 게임에서 강제1칸 배치가 발생했다는 재현 캡처로 해석하지 않는다.

기존 V12 잔디 바닥·바닥 하부·하늘·별·구름은 배경 맥락으로만 읽었다. 새 인게임 PNG나 새로운 원화로 주장하지 않는다. 원본 파일은 바꾸지 않았고 이 패치가 기존 바닥/배경을 다시 설치할 이유가 없다. 하부8칸은 기존의 깨끗한3+3+2 연속 부품으로 구성했다. 배우·장치·기능성 바람개비·병아리는 예시에 넣지 않았다.

preview_qa.json은 사용한 새 PNG77개와 기존 맥락 PNG30개의 SHA/PPU/pivot, 생산 manifest3개의 SHA, 최종PNG/GIF의 decode/hash, 전체 지면 배치와12 GIF 프레임을 기록한다. 모든 검수는 OFFLINE이며 실제 Unity Import/C# Compile/Prefab 연결/Play Mode/레거시 참조0은 NOT_RUN이다.
