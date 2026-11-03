별너울 해안 지형 + 수면 / 신규 각진 카툰 V01

범위: 3 land materials × raw 8-neighbor256 =768 PNG, fused1/2/3/4 sandy rock/wood bodies8 + inner-hole shallow caps8, water carrier1 + native ambient wave4 + cardinal shore4 =793 runtime PNG.

제작: built-in imagegen 신규 원화4종을 사용했다. 상면은 flat-plane 지시를 강화하여 다시 생성한 최종 source만 사용하며 initial_unused 원화는 runtime에서 제외한다. 성소 또는 기존 픽셀 PNG는 재사용/리컬러/필터링하지 않는다. production/generation_prompts.json 및 top_material_regeneration_prompt.txt에 실제 프롬프트가 있다. RGB를 칠하거나 색 양자화하지 않았으며 native crop/scale/mirror/UV registration과 topology alpha만 적용한다. Sand의 넓은 바탕은 native quiet1pixel 확대를 사용해 단색으로 유지하며, 외곽은 원화의 terracotta/navy strip을 보존한다. Water의 teal 면은 alpha만 분리하여 native foam/shore RGB를 보존한다.

land: tiles/{coast_sand,wood_bridge,stone_bridge}/full256/n000..255.png. PPU256, canvas320, logicalRect[32,32,256,256], pivot[0.5,0.5]. N=+Unity gridY / 이미지 위. N1 NE2 E4 SE8 S16 SW32 W64 NW128. A/W/S 모든 실제 land 이웃을 연결하며 water는 mask에서 제외한다. 47 canonical reduction을 사용하지 않는다. 서로 다른 land 재질은 접속 silhouette을 공유하되 재질 자체의 경계는 보존한다.

undersides: south-exposed same-support contiguous run만 추출한다. A/S=sandy_rock, W=wood. 길이1/2/3/4는 그 길이의 단일 native fused body. 더 길면 map seed로2–4길이 chunk를 안정 선택하고 1칸 꼬리를 금지한다. 내부 hole은 cap_01..04만 선택해서 구멍을 깊은 벽으로 채우지 않는다. support는 논리면을 추가 점유하지 않으며 terrain top보다 먼저 그린다. canvas512높이, pivot[0.5,0.6875], ground centerY160, bodyTopY276를 유지한다.

water: tiles/water/carrier.png는 canvas256, logicalRect[0,0,256,256], PPU256,pivot[0.5,0.5]이며 패딩 없는 native quiet1pixel을 균일 확대하여 만든 완전 단색 periodic field다. 반대쪽 edge RGBA가 정확히 같다. 실제 watercell에 배치하거나 카메라 아래를 덮는 시각용 pool로만 사용한다. 이 파일에서 물 점유나 보트 이동가능 여부를 만들지 않는다.

water ambient는 fx/water/wave/frame_00..03.png에 독립적으로 그려진 native4poses/12fps. 흘러가는 화살표가 아닌 제자리 부서지는 파도다. shore north/east/south/west는 water에서 인접 land로 향하는 법선이며 movement/current heading이 아니다. 각 방향은 별도 native 원화에서 추출했고 upright 전체 회전으로 만들지 않았다. shore/foam은 실제 host watercell 영역으로 clip하고 boat/land보다 뒤에 그린다. boat wake/boarding/landing은 boat_exports 담당이다.

모든 footprintCells는 art registration 참고값이다. actualGameplayFootprintCells/existingGameplayId는 null, registerGameplayCellFromThisRecord=false, deriveColliderFromAlpha=false다. 실제 host map/ID/물점유/Unity bindings를 이 산출물에서 추정하지 않는다.

오프라인: terrain_qa.json, terrain_seam_qa.json, water_qa.json, native_source_qa.json 참고. 육안검수: inspection/terrain_join_map.png 및 grid, full256 contacts, water_contact.png, water_ambient_motion.gif. 원본 Unity 프로젝트가 없으므로 import/C#/shader/NUnit/Prefab/Play Mode/legacy references0는 모두 NOT_RUN이다.

수락된 native source로 재빌드할 때는 python production/rebuild_all.py를 실행한다. Build→caps→water→contacts→seam/source QA 순서를 보장한다. 이 스크립트는 imagegen을 다시 호출하거나 host Unity 파일을 수정하지 않는다. 실행 중 부분 파일을 조립/복사하지 말고 마지막 완료 뒤의 master manifest SHA를 확인한다.
