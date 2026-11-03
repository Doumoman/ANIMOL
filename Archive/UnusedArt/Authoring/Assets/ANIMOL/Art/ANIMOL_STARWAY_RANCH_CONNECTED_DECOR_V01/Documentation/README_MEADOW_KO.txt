별길 목장 — 연결형 입체 화단/풀밭

새 native 원화 2개에서 만든 연결 carrier256개와 희소 꽃 장식4개다. 타일을 붙여도 내부에 사각 테두리를 반복하지 않고, 외곽에만 낮은 풀 잎과 짧은 앞면 그림자를 둔다. 2/3/4칸·L/T·가운데 구멍 등은 하나의 연결된 영역이 된다. 기존 지형 위의 장식이며 새 보행 지형/Collider/통행 규칙을 만들지 않는다.

규격: PPU256, canvas320×320, logicalRect[32,32,256,256], pivot[0.5,0.5], 지면1world. 마스크는 Unity +Y North 기준 N1 NE2 E4 SE8 S16 SW32 W64 NW128이다. 이미지 제작 코드의 y는 아래로 증가하므로 N은 row-1이다. 같은 family/connectionGroup/layer/height만 연결한다.

선택 경로: decor/connected/raised_flower_meadow/n000.png ~n255.png. 옛 풀 이미지를 이 경로로 fallback하지 않는다. 중심 영역을 타일마다 테두리로 둘러싸지 않는다. 꽃은 decor/accents/meadow/flowers_01.png~04.png이고 kind=meadow_flower_accent/family=raised_flower_meadow다. 기존 화단 셀 위의 선택적 시각 요소이며 별도 점유/Collider를 등록하지 않는다. 호스트가 준 seed와 세계 좌표로 희소하게 선택하고 프레임마다 Random을 호출하지 않는다.

알파/연결 방식: 완전히 같은 RGB와 alpha255인 연결 여백은 양쪽에 유지한다. 동일한 불투명 픽셀은 중복 합성되어도 색이 변하지 않으며, 이 support가 있어야 Bilinear 축소에서 경계에 밝은 격자가 생기지 않는다. 반투명 잎 끝은 가장 가까운 점유 셀(동률은 world x,y순)의 한 주인만 둔다. 따라서 '전체 알파 중복0'을 주장하지 않는다. 부분 알파 중복이나 서로 다른 색의 중복은0이며, 합성 결과를 한 번 그린 전체 native field와 대조했다.

오프라인 검수: 가로1024+세로1024=2048개 유효 인접 조합, 불안전 알파 중복0, 전체 field 대비 RGBA 채널 차이0, 중앙 연결 포트 불투명, 구멍 중앙 alpha0. 64/80/128/256px 논리 칸 크기로 PNG를 개별 LANCZOS 축소해 조립했을 때 중앙 셀 내부 RGB 범위0. 실제 Unity GPU/Bilinear/Import/Prefab/Play 검증은 NOT_RUN이다.

원화 RGB를 코드로 다시 칠하거나 필터/리컬러하지 않았다. 새 생성 원화의 조용한 canopy 1픽셀 crop 확대, native rim crop과 균일 리샘플, side profile 반복, native 풀 cutout 배치, 알파만의 모따기/부분잎 소유권으로 구성했다. 반투명 바깥 잔재 제거 threshold12, 거의 불투명 alpha>=245 정규화는 기술적 알파 정리다. 두 번째 원화의 코너 초안도 출처로 보존하며 실제 꽃 장식은 아래쪽 네 꽃 crop을 사용한다.

재현: production/build_meadow.py → production/qa_meadow.py → production/qa_resampling.py 순서다. qa_meadow는 현재 native 입력으로 다시 내보낸 뒤 모든 조합을 검사한다. 신규 PNG/메타데이터가 생성되면 전체 패키지 catalog/import SHA도 함께 갱신해야 한다. sources와 production/components는 게임 Sprite 후보가 아니다.
