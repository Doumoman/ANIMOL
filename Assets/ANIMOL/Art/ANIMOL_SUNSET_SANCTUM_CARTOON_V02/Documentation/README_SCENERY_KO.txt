낙일 성소 V02 — 각진 배경 / 장식 교체

구성: runtime PNG 29개(하늘1, 건축4, 구름4, 별/성좌8, 장식12), 신규 imagegen 원화4개.
V01과 동일한 상대 경로, 캔버스 크기, PPU256, pivot, logicalRect, footprintCells를 유지한다.
모든 runtime PNG와 보이는 디코딩 픽셀이 V01에서 바뀌었다. V01 파일은 수정하지 않았다.

원화: sources/sanctum_{sky|architecture|sky_parts|props}_native.png.
제작 프롬프트와 선택/폐기 원화 기록: generation_prompts.json.
전부 built-in image_gen으로 새로 생성했다. V01 필터, posterize, RGB 재도색, 코드 다각화가 아니다.
허용된 후처리는 crop, target alpha component 분리, 약한 alpha 잡티 정리, 균일 확대/축소와 투명 캔버스 등록뿐이다.
하늘은 추가 native 생성 후 가장 단순한 직선4색면 결과를 선택했다. 생성 모델의 미세 색차가 남을 수 있다.

형태: 큰 사다리꼴 패널과 뾰족 cap의 돔, 꺾인 문/아치, 사각 기둥, 다각 랜턴/관측기구,
접힌 두루마리, 독립된 입체 잎, 긴 쐐기 구름, 새로 그린 다각별/성좌.
하늘에 달/토성/행성은 없다. 게임판은 배경에서 분리한다.

레이어: sky z9/order-1000 → stars z8/-900 → far islands z7/-850 →
far clouds z6/-800 → side arches z4/-700 → near clouds z2/-600.
별은 drift0, 카메라 반응 .01. 원경 섬 .08, 먼 구름 .2, 아치 .3, 가까운 구름 .45.
구름만 독립 visual drift를 가진다. 장치 논리, 힘, 이동/행동 비용은 배경 시간으로 구동하지 않는다.
건축물은 opacity1로 배치한다. 투명한 바깥 여백의 실제 alpha는0이다.
background_scene_layout.json은 기존40개 배경 배치 템플릿을 그대로 유지한다.

장식의 지상 기준은 groundFootprintRect 중심이다. 긴 기둥의 전체 캔버스 중심을 쓰지 않는다.
장식은1x1,2x1,2x2 시각 슬롯이며 실제 게임 충돌 점유는 미검증이다.
actualGameplayFootprintCells는 null이며 alpha로 Collider를 만들지 않는다.

검수: scenery_qa.json, v01_to_v02_scenery_comparison.json.
inspection/scenery_contact_sheet.png: 장식12 + 건축4.
inspection/scenery_sky_parts_contact_sheet.png: 하늘 + 구름4 + 별/성좌8.
inspection/scenery_dome_assembled_preview.png: 배경 조립 예시. 전체 게임 실행 화면이 아니다.

재생성 순서: production/build_scenery.py → production/build_scene_layout.py → production/finalize_scenery.py.
실제 Unity import / compile / PlayMode / 실제 프로젝트 theme ID 및 프리팹 연결은 NOT_RUN / 미검증이다.
