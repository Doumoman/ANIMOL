별너울 해안 연결 장식 V01 — 다칸 6종과 한 칸 소품 8종

새 builtin imagegen 원화 8개를 사용한 런타임 PNG14개다. 처음 제작한 조수 정원과 수리선의 미채택 초안2개를 포함해 native source10개를 보존한다. 수리선은 배경 알파 정리를 요청한 새 native 편집본, 정원은 물의 넓은 면을 평면색으로 다듬은 새 편집본을 사용한다.
모든 프롬프트는 Authoring/prompts.json 및 edits.json에 있다. 원본 RGB를 재도색하거나 다른 테마를 재색칠하지 않았다. 네이티브 컴포넌트의 작은 분리 점/희미한 배경을 알파 몸체+2px AA 범위로 정리한 뒤, crop·균일 확대축소·등록만 수행했다. Atlas는 실제 연결된 알파 몸체를 찾아 네 개를 순서대로 추출해 가상 4등분 경계로 자르지 않는다.

그룹은 picnic_setting_2x1,folded_sail_store_2x2,dock_notice_board_2x1,tidepool_garden_2x2,beached_skiff_3x2,shore_steps_2x1이며 points8종은 manifest를 따른다. 전체 원화당 Renderer하나, PPU256, 지면칸 중심 pivot을 쓴다. 다칸을 한칸으로 축소하지 않는다.
수리선은 정적 수리 작업장이다. 실제 플레이어 나룻배를 대체하지 않는다. 정원의 물은 장식 내부의 평면색이며 실제 물칸을 만들지 않는다. 기존 배/통/폭탄/가시/야자수/해/배경은 변경하지 않는다.
수리선은 균일한 가로772px 등록을 사용해 논리 가로768px 양쪽으로2px의 시각 여백을 허용한다. 논리 점유는3×2 그대로이고, 지면 깊이85.156% 및 각 줄 알파밀도62.31%/94.23%다. 비균일 늘이기는 없다. 다른 전체 그룹/포인트도 원화별 logicalRect/pivot/크기를 따른다.

검수는 파일·RGBA·원화 출처·PPU·pivot·지면 점유·alpha128 기준 깊이행·오프라인 도감 확인이다. Unity Import/C#compile/NUnit/실제 Prefab·Scene바인딩/Play는 NOT_RUN이다. Inspection/GROUPS_B_14_CATALOG.png는 실제 게임 스크린샷이 아니다.
