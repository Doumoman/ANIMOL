별길 목장 연결 장식 V02 — 다칸 장식 B·한 칸 소품
6개 전체 다칸 장식과8개 한 칸 소품이다. 원화는 built-in imagegen으로 새로 제작했고, 기존 테마의 재색칠이나 절차적 대체 그림을 쓰지 않았다.
색은 크림색 목재·에메랄드 마감·민트/세이지 잎·노란 작물/꽃·보라빛 슬레이트이며, 굵은 남색 윤곽선과 평면 다각형 셀 음영을 따른다.
전체 그림은 그룹당1개 Renderer에 사용한다. 게임 점유/Collider/비용/장치 상태를 추가하지 않는다. 빈 닭장은 정적 장식이며 병아리 생성기나 둥지 장치가 아니다.

다칸6종
harvest_cart_2x2 — 뒤 수확 수레/앞 상자·하역대가 이어진2×2.
chicken_coop_2x2 — 빈 닭장과 앞마당이 이어진2×2.
apiary_corner_2x2 — 양봉 상자와 앞 꽃밭/도구가 이어진2×2. 벌 등 살아 있는 동물은 그리지 않았다.
picnic_arbor_3x2 — 뒤 정자 프레임/앞 소풍 탁자와 갑판이 이어진3×2.
produce_stall_3x2 — 뒤 가판대/앞 농산물 진열이 이어진3×2.
garden_workbench_2x1 — 모종/작업판/도구가 놓인2×1.

한 칸8종
milk_can_pair,tool_basket,ranch_notice_sign,watering_can,feed_sack_stack,flower_bucket,harvest_crate,honey_jar_stand.

제작과 등록
투명 native PNG9개 중8개가 현재 런타임 원화다. 초기 정자 원화1개는 미채택 초안으로만 보존한다.
정자의 둥근 작은 꽃과 포도는 native 편집으로 뾰족한 별꽃과 각진 과일 조각으로 바꿨다.
Authoring/prompts.json 및 edits.json에 전체 원문 프롬프트, generated_paths.json에 실제 생성 원본 경로가 있다.
기술 처리 범위는 native crop, 균일한 비율의 크기 등록, 투명 픽셀 정리와 원화 알파 주변2px AA 보존이다.
RGB를 다시 그리거나 색으로 배경을 키잉하거나 가로/세로를 따로 늘리지 않았다.
PPU256, logicalRect의 폭/높이는 점유 셀×256이고 Unity pivot은 그 지면 중심이다.
위쪽 장식 높이는 필요한 canvas padding이며 점유/Collider가 아니다.
manifest.json의 개별 크기·좌표·피벗·source crop·scale·PNG SHA가 최종 기준이다.

재등록 코드는 별도 제작 export 폴더에서 실행한다.
python Authoring/build_groups_b.py --export-root <native sources 하위 폴더가 있는 별도 제작 폴더> --catalog <26종 catalog_design.json>
통합 설치 패키지의 manifest를14종으로 덮지 않도록, 기존 manifest에15개 이상 자산이 있으면 실행을 거절한다.
이 코드는 C#/Unity 빌드나 프로젝트 장식 어댑터를 대신하지 않는다.

검수
전체14개 PNG의 RGBA/투명도/크기/PPU/피벗/해시와 폭을 확인했다.
알파128 기준 다칸 가로 채움99.6%이상; 두 깊이 행 장식의 높이91.21%이상, 각256px행의 불투명 점유58.55%이상.
Inspection/GROUPS_B_14_CATALOG.png는 원화와 메타데이터를 합성한 오프라인 도감이며 실제 Unity 화면이 아니다.
실제 Unity Import/C# 컴파일/Prefab 연결/Play는 NOT_RUN이다.

