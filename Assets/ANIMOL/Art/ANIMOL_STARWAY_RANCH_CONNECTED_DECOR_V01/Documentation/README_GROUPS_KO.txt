별길 목장 — 여러 칸을 함께 쓰는 새 장식 4종

hay_store_2x1: 연결된 건초 보관대·상자, 지면 슬롯2×1.
feeding_corner_3x1: 긴 단일 물통·건초 급이대·우유통, 지면 슬롯3×1.
tree_flower_corner_2x2: 큰 나무와 이어진 화단·낮은 목재 테두리, 지면 슬롯2×2.
barnyard_3x2: 헛간과 붙은 울타리 마당·건초 보관·벤치, 지면 슬롯3×2.

각 장식은 새 imagegen 원화 한 장에서 제작한 하나의 전체 그림이다. 여러 1칸 장식을 늘여 붙인 그림이 아니다. 장식 폭은 각 선언 지면 폭의 약96.7~96.9%를 채우며, 낮은 보관대/물통의 위쪽 여백은 투명한 공간이다. 나무의 위쪽 돌출은 지면2×2와 별개인128px 여백에 수용한다.

PPU256,transform scale1을 유지한다. 기본 지면1칸은world1이다. 논리 영역과 pivot은 groups_manifest.json의 각 asset값을 사용한다. PNG 전체를1칸에 맞추거나, alpha bounding box에 맞춰 매번 재축소하지 않는다.
호스트 장식 맵의 bottom-left anchor(x,y)에서 전체 w×h decorative cells를 원자적으로 예약하고 renderer는 한 개만 만든다. 격자 셀의 world 경계 원점이 gridOrigin이면 renderer 중심은 gridOrigin+(x+w/2,y+h/2)다. row-down 맵은 기존 어댑터로 명시 변환한다. 각 asset의 Unity pivot은 논리 영역 중심에 맞춰져 있으므로 sprite 크기로 추가 중심 보정을 하지 않는다.

이 예약은 시각 장식 배치 계약이다. 실제 게임 Collider/통행/장치 점유는 기존 호스트 규칙을 따르며 alpha로 새 장애물을 만들지 않는다. 실제 ID/GameplayFootprint는 미확인(null)이다. 큰 그룹 자체의 붙어 있는 울타리는 전체 원화의 일부다. 자동 연결 울타리 네트워크에 대한 새 포트/충돌을 추정하지 않는다.

sources의 새 native 원화4개와 초안2개, 실제 generation_prompts.json/edit_prompts.json, production/build_groups.py, inspection 지면 격자와 contact sheet를 함께 보관했다. RGB 채색/리컬러/코드로 도형 그리기는 사용하지 않았다. alpha의 독립 미세 조각 정리→crop→비율 유지 resize→투명 캔버스 등록만 수행했다. 격자 선은 검사 이미지에만 있고 production PNG에는 없다.

PNG/메타데이터/원화/지면 사각형/pivot/폭 검수는 PASS_OFFLINE이다. 실제 Unity import/C# compile/Prefab 바인딩/Play Mode는 NOT_RUN이다.
