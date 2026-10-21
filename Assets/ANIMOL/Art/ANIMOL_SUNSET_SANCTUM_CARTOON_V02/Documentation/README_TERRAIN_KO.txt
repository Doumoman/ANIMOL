낙일 성소 지형 V02 — 전체 각진 재제작

모든 runtime 원화는 V02에서 새로 생성한 native3장으로 다시 제작했다.
V01 PNG를 필터링·재색칠·양자화·polygonize하지 않았다. 같은 파일 경로와 게임 격자 계약만 유지했다.

상면3재질
성소 석재 바닥 / 목재 다리 / 석재 다리 각각8이웃 raw256종이며 총768개다.
새 원화는 긴 직선, 명확한45도 chamfer, 사각·다이아 부품, 삼각형·사다리꼴 그림자로 구성된다.
모서리의 Gaussian 곡면과 sine 외곽을 사용하지 않는다. 새로운 점유 자료에서 piecewise-linear 외곽과 24px diamond chamfer alpha를 계산하고 새 native RGB를 기계적으로 UV 등록했다.

연결과 경계
terrain_manifest.json의 materials[material].neighborMaskToFile[rawMask]가 권위 있는 경로다.
material키는 sanctum_floor / wood_bridge / stone_bridge이며 G/W/S는 예시 맵 코드다.
방향 비트는 N1 NE2 E4 SE8 S16 SW32 W64 NW128이며 Unity N은 그리드+Y다.
이웃 연결은 같은 재질만이 아닌 실제 모든 연결된 바닥을 기준으로 한다. 재질 경계에 가짜 외곽 테두리를 넣지 않는다.
논리256px / 캔버스320px / PPU256 / pivot(.5,.5) / 논리 사각형[32,32,256,256]을 유지한다.
Sprite Single/FullRect/Bilinear/Clamp/noMip/noCompression이며 alpha가 점유나 Collider를 정의하지 않는다.

부유 하부
masonry / wood의1·2·3·4칸 fused body8개와 같은 길이의 얕은 구멍 cap8개다.
각각 새 native 원화에 맞는 길이별 몸체를 등록했으며 V01 몸체를 변형한 것이 아니다.
하부의 캔버스(n*256+64)×512 / 논리 사각형[32,32,n*256,256] / PPU256 / pivot(.5,.6875)는 동일하다.
body는 PNG 위에서y276부터, cap은y308까지만 그려져 있다. 모든 하부를 바닥 상면 뒤에 그린다.
내부 구멍에는 shallowCapFiles, 외부 하늘섬 윤곽에는 spanFiles를 선택한다.
2·3·4칸은 단일 fused 몸체를 쓰고 긴 run은 같은 seed·좌표에서 안정적인2~4칸 묶음으로 선택한다.

검수와 재현
terrain_qa.json: 파일/SHA/치수/PPU/논리 사각형/전체256매핑/1~49칸 run 검사.
terrain_seam_qa.json: 모든3재질×2축×주변1024배치의 동일 월드64px 겹침6,144건 비교.
v02_angular_rebuild_qa.json: V01과 경로·PPU·pivot·논리 사각형·점유 동일, 전784 decodedRGBA 변경 여부.
inspection/terrain_join_map.png, _grid.png, 각 재질 full256 contact를 함께 확인한다.

재현 순서:
python production/build_terrain.py
python production/add_shallow_caps.py
python production/render_and_verify_terrain.py
python production/verify_join_seams.py
python production/verify_v02_redraw.py
Pillow/numpy/scipy가 필요하다. native 생성은 빌트인 imagegen을 사용했고 정확한 프롬프트는 production/generation_prompts.json에 있다.

실제 Unity import/compile/씬 바인딩/Play Mode는 NOT_RUN이다.
