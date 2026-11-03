낙일 성소 연결 장식 B — 신규 원화 / 3종 × 16방향 마스크

별 문양 휘장 파티션(star_banner_partition), 청색 결정 화단(blue_crystal_planter), 봉헌등 장식 띠(votive_light_channel) 세 종류다. 16개 이웃 마스크는 각각 다른 장식 종류로 세지 않는다. 봉헌등은 정적인 장식이며 기존 광원·감지기·광선 장치의 논리나 애니메이션을 대체하지 않는다.

각 파일은320×384, PPU256, 지면 슬롯은 좌상단 기준[32,96,256,256], Unity 아래왼쪽 기준 pivot=[0.5,160/384]다. N1 E2 S4 W8, 세계+Y가 북쪽이다. 모든 연결점은 같은 family·connectionGroup·logicalLayer·heightKey 이웃에만 이어진다. 단일 시각 셀을 줄여 맞추거나 바운드로 자동 스케일하지 않는다.

새 native RGBA 원화는 built-in imagegen으로 가족별 한 장씩 제작했으며 sources/와 generation_prompts.json에 실제 원화와 전체 프롬프트를 보존했다. 기존 잔영 극장·별길 목장 원화의 재색칠은 사용하지 않았다. 세 원화에서 가로 carrier·세로 carrier·접합부를 잘라 균일 배율로 등록하고, 정해진 논리 셀에 배치하여 모든 마스크를 구성한다. 원화의 RGB를 덧칠하거나 색상 양자화하지 않는다. 분리된 원화의 alpha128 이상 최대 연결 성분과2px 외곽 AA를 보존하고, near-opaque alpha245이상은255로 정리하며8미만 alpha는 제거한다. 접속부는 원화의 공통 단면을12px 반복 등록하고, 논리 셀 밖4px에는 동일하고 완전 불투명한 단면만 둔다. 반투명 가장자리를 두 번 겹치지 않는다. 이 기술적 alpha 정리와 단면 등록은 새 장식을 절차 도형으로 대체하는 작업이 아니다.

직선 마스크05/10에는 매 칸 중앙 접합부를 놓지 않는다. 끝·모서리·T·교차 마스크는 동일한 native 접합부를 공유한다. 별 문양 휘장은 남색의 큰 평면과 상아 별, 화단은 날카로운 청색 쐐기, 봉헌등은 작은 각진 불꽃과 황동·상아 받침으로 구분된다. 둥근 초·렌즈·기계적 발광 효과를 추가하지 않았다.

production/build_networks_b.py는48개 런타임 PNG와 원화 단면/접합/마스크 검사 및 오프라인 예시를 재현한다. production/check_scaled_networks_b.py는2~4칸 직선/L/T/교차/고리7종을48/64/80/96/128px 칸 크기와 Bilinear/Lanczos 두 필터에서 비교한다. 전체210개 CPU 경우를 측정했으며 Bilinear 불투명 내부 alpha 손실은0이다. Lanczos의 리샘플링 차이는 보고서에 실제 수치로 남겼으며 전역 RGBA 일치나 Unity GPU 결과로 간주하지 않는다.

검수:48개 PNG 디코드/치수/해시, 세 가족 총384개의 호환 접속 쌍에서 경계 RGBA 완전 일치와 불안전 겹침0 확인. inspection/의 예시는 원화 합성 자료다. 실제 Unity Import/C# Compile/Play/GPU 검증은 원 프로젝트가 없어 NOT_RUN이다.
