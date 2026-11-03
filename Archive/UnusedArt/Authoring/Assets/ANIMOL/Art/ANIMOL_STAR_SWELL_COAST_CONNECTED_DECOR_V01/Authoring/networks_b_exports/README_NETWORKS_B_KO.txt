ANIMOL 별너울 해안 연결 장식 V01 — 입체 사구 풀띠 / 산호 암석 경계 / 항구 발치등 띠

서로 다른 새 장식3계열이며 각 계열의16방향 마스크는 새 종류 수로 세지 않는다.
총48runtimePNG,3장의 새로운 built-in imagegen native 원화와 전체 생성 프롬프트를 포함한다.
기존 성소·극장·목장 그림을 재색칠한 패키지가 아니다. 제작/원본/검토 PNG는 런타임 선택 목록이 아니다.

지면 한칸은256px/PPU256이다.320×384캔버스의 지면 사각형은 이미지위기준[32,96,256,256],
Unity아래기준 pivot=[0.5,160/384]이다. N1/E2/S4/W8,+Y북쪽의16마스크를 정확히 선택한다.
같은 family,connectionGroup,logicalLayer,heightKey의4이웃만 연결한다. 높이·레이어·다른계열은 연결하지 않는다.
연결된 띠의 실제 전체world셀을 기준으로 고르고,카메라/청크경계에서 잘못 마감하지 않는다.
첫두울타리계열과 달리 이3계열에는 periodic_post가 없다. 긴직선에 반복끝기둥을 붙이지 않는다.

새 원화에서 native H/V/carrier/junction을 자르고 균일크기로 등록한 뒤 native cross-section을
각포트의12px범위에 재사용한다. 원화 RGB의 재그리기·재색칠·양자화를 하지 않는다.
생성 alpha254의 거의불투명 core는255로 안정화하고,최대2nativepx AA fringe를 남긴다.
서로만나는 padding은 동일RGB의 완전불투명4px core만 허용한다. 부분alpha 이중합성은 하지 않는다.
고정 캔버스/지면/pivot을 유지하고 alphaBounds나 전체그림을 한칸으로 다시fit하지 않는다.

허용 포트384쌍의 정확한RGBA 프로필/겹침과48PNG load검사를 통과했다.
CPU축소합성210사례는 Bilinear불투명core alpha손실0, Lanczos최대3/255를 측정했다.
안티앨리어싱 범위를 포함한 전체RGBA가 모두 같다는 뜻이 아니며 실제GPU/Atlas성공 증거도 아니다.
Unity Import/C#/NUnit/실제게임바인딩/Play는 NOT_RUN이다.

각진 raisedgrass는 지면에 납작하게 그린 잔디가 아니라 위로튀어나온 쐐기 잎이다.
산호는 굵은 직선갈래,암석은 큰facet,등불은 작은 마름모glass와각진목재받침으로 제작했다.
새 장식은 시각전용이며 물칸/나룻배/통/폭탄/이탈가시/턴·코스트·충돌을 바꾸지 않는다.
