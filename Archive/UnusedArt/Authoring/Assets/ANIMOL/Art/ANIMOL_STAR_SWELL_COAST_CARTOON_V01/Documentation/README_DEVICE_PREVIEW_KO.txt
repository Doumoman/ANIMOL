별너울 해안 V01 — 공통4종 + 해안4종 전체 장치 검수 예시

ANIMOL_STAR_SWELL_COAST_DEVICE_REVIEW.png: 1800×2360,2열×4행의8카드.
ANIMOL_STAR_SWELL_COAST_DEVICE_MOTION.gif: 1200×1573,8초,96개12fps 샘플. GIF10ms 단위 제약은80/80/90ms 반복으로3샘플250ms를 맞춘다. 원본clip fps를 그대로 사용하며 프레임을 느리게 늘려 초안처럼 보이게 만들지 않는다. 하나의256색 팔레트와 Dither.NONE을 사용한다. runtime PNG의 색/alpha/metadata는 수정하지 않는다.
build_device_previews.py는 common/boat/hazards의 실제 manifest와 SHA를 읽고 각 PNG의 PPU/pivot으로 배치한다. 전체 upright sprite를 회전해 방향을 만들지 않으며, 제공된4방향 native clip을 직접 사용한다. preview_qa.json에 참조한 실제 PNG의 SHA, manifest SHA, GIF 검수 결과를 기록한다.

07 무빙워크: 네 방향 원화에서 구조축·화살표·슬랫 이동이 일치한다. 한 칸씩 점유하는 기존 자동 이동 장치이며 추가비용을 만들지 않는다.
09 일방향 문: 1칸 고정 rail 안에서 분할봉이 움직인다. 열린 중앙은164px alpha0이다. 반대/양옆/막힌 출구에는 닫힌 봉과 X를 유지하며0이동/0비용/0턴이다. 출구는 검사 칸이다.
18 튕김 모서리: NE/NW/SE/SW thin L과45도 fender 원화다. 실제90도 반사 함수는 host의 기존 규칙이 권위다.
19 제동판: 가로/세로 두 축, 진입 방향과 직교하는 stopbar다. 진행 화살표/반동/추가이동이 없다.
05 출발 가시: flat→occupied armed→P가 완전히 떠난 뒤 rise→raised를 보여준다. 가시는 다시 내려오지 않는다. raised 칸으로 돌아오는 입력은 무효다. lowering/reset 조건은 미확정이며 기존 host reset/undo만 따른다.
11 배: 윗줄은4방향 bow/oar/feedback 원화다. 아래의 별도 host 예시는 LAND[0]→배가 있는 WATER[1]에 탑승→배와 같은P가 정확히 WATER[2]까지1칸 이동→LAND[3]으로 하선한다. 배는 마지막 WATER[2]에 남는다. blocked 물/육지는 무효이고 별도비용을 추가하지 않는다. P는 기존 actor를 설명하는 사각 문자 annotation일 뿐 새 캐릭터 sprite가 아니다. Native seatAnchor[160,160]를 따른다. Wake는 예시의 명시 water 마스크 안에서만 표시한다.
12 구르는 통: P[0],통[1],장애물[4]에서 시작해 통은 장애물 바로 전[3]에 멈춘다. P는 통의 원래칸[1]까지만 이동하고 더 따라가지 않는다. 최초 통 목표가 막히면 전체 push가 무효이며 이동/비용이 없다. 실제 경로는 기존 host가 공급해야 한다.
15 세 턴 폭탄: 처음 실제로 이동한 성공 push에서3이 된다. 이후 자신의 accepted action#1→2,#2→1,#3이 끝난 뒤 explosion이다. 첫 push가 즉시3→2가 되지 않는다. 무효입력/실제변위없는 입력/자동chain/timer/애니메이션은 점화·카운트·폭발을 결정하지 않는다. 예시의 벽은 명시 debug host list {(2,0)}에만 포함되어 break/open을 보여준다. 이는 기본 blast모양·범위·반경·데미지·연쇄반응을 정하는 값이 아니다. 벽 crack/break는 기존host이벤트의 피드백이며2hit HP를 만들지 않는다. 열린 벽 중앙은 실제alpha0이다.

격자, 물/육지색, 얇은 경로/화살표, 장애물 사각형, P badge 및 host list 테두리는 검수용 diagram annotation이다. production 원화가 아니며 runtime에 쓰이지 않는다. 화면의 예시 시간은 독립 host accepted-action 스냅샷을 설명하기 위한 편집 타이밍이다. 실제 게임의 새 타이머/턴/자동 행동은 아니다. GIF 루프는 같은 예시를 다시 재생할 뿐 가시 자동reset이나 보트/통의 자동복귀를 구현하지 않는다.

전체 예시는 오프라인 PNG 합성이다. 실제 Unity 화면 캡처/실제 인게임 동작 증거가 아니다. 실제 import/C#·shader compile/Prefab binding/PlayMode/장치ID/기존 레거시참조0은 모두 NOT_RUN이다.

하선 마커 방향은 이 예시의 명시 host 하선 입력 east를 따른다. 배의 bow로 하선 방향을 추론하거나 고정하는 규칙이 아니다. 원화 body의 모든 footprintCells[1,1]과 alpha→collider 금지를 읽고 검증한다. 렌더 종료 시에도 입력 manifest3개와 사용PNG의 SHA가 시작 시점과 동일한지 다시 검사해 제작 중간 변경을 완성 예시에 잘못 귀속하지 않는다.
