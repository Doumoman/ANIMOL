별너울 해안 — 새 각진 카툰 배경 / 장식 V01

runtime PNG42개: 배경19(하늘1/수평선1/독립태양1/원경·frame4/구름4/별·성좌8),
장식12, 야자수 rig부품6, 완성된 야자수 wind frame5. clip1개, 새 imagegen native6개.
모두 새 native에서 crop/alpha분리/균일resize/등록/합성했다. 이전 픽셀 해안이나 성소 원화를 재색칠하지 않았다.
generation_prompts.json에 선택6개와 폐기한 하늘 시안의 실제 프롬프트를 기록했다.
생성 모델의 미세 색차가 남을 수 있지만 code paint/posterize/quantize로 생산 아트를 고치지 않았다.

배경 수평선 처리
- backgrounds/coast/sky_base.png: 태양/수평선 없는 warmcoral판, 카메라 cover 가능.
- backgrounds/coast/horizon_plate.png: 고유한 실제 수평선 한 개. 반드시 월드에 고정한다.
  PPU256,1080x1920,pivot[.5,.7305053025577043],runtime horizonY517.4298190892077.
  기본 worldanchor[0,6.25,8.9],worldwidth22. 카메라 cover나 Yrepeat 금지.
- backgrounds/coast/angular_setting_sun.png: 독립된 setting_sun family, 한 씬에 정확히 한 개만.
  512x256,pivot[.5,.0625],바닥이 수평선에 붙는다. 기본[3.6,6.25,8.6],scale2.1.
  태양은 다른 판에 그려져 있지 않으며 반사 스프라이트도 없다. 달/행성/토성은 없다.

가변 맵 / 물
실제 water carrier는 terrain_exports 소유의 tiles/water/carrier.png를 사용한다.
root/host는 worldY6.25 아래의 카메라 빈 공간만 같은carrier로채운다. 태양/수평선 판은 반복하지 않는다.
전체 oceanCameraFill은 order-880(별-900앞/섬-850뒤), 실제 watercell carrier는-500,
wave-490,shore-480,land0,boat20+가 권장이다. 물은 새로운 걸을 수 있는 점유 타입이 아니다.
실제 watercell clip/범위/장치 출력/충돌은 host가 결정한다. 장식/foam alpha로 점유를 만들지 않는다.

깊이 / 동작
sky9/-1000 → horizon8.9/-990 → sun8.6/-960 → stars8/-900 →
globaloceanfield1/-880(별도terraincarrier) → islands7/-850 → farcloud6/-800 →
coastalframe4/-700 → nearcloud2/-600 → actualwatercellcarrier/-500 → land0.
z는 카메라가 negativeZ에서+Z로 바라보는 기준이며 sorting을 명시한다.
별은 drift0/spin0/twinkle0,parallax.01. 섬.08/먼구름.2/frame.3/근구름.45.
건축물·frame은 opacity1. 구름은 독립된 visual drift만 가진다. 실제 행동/비용/물리 논리를 구동하지 않는다.
왼쪽 frame palm은 원화에서 윗부분이 화면 밖으로 잘린 framing조각이므로 authored topcut을 카메라 밖에 둔다.
background_scene_layout.json에 38개 배경 배치와12개 선택적 장식 배치가 있다.
게임 진행로/배 출력칸과 실제 충돌을 검사한 레벨 파일이 아니므로 root가 실제 보드에 맞춰 위치를 조정한다.

점유 / 야자수
모든 파일 PPU256. 실제 지면 기준은 logicalRect/groundFootprintRect 중심이다.
높이/투명padding/잎 overhang은 타일 점유가 아니다. 실제GameplayFootprintCells와ID는 null/미검증.
decor12개는1x1 또는2x1 시각 슬롯. 야자수768x896의 지상LR[256,608,256,256],pivot[.5,.1785714285714286].
야자수는 고정trunk1개+새canopy5개를 동일한 연결점에 등록했다. 아래608px부터는 전frame 고정몸체가 byte동일하다.
coast.palm.wind clip: 새native5pose를 [0,1,2,1,0,3,4,3] 역방향순서로12fps재생한다.
연속동일프레임은 없고반복된phase경로는 되돌림 동작이다. 별도의 touch/충돌/행동 로직을 발명하지 않았다.
props/coast/coast_palm.png는 frame00의 byte동일 idle별칭이다.
완성프레임을 쓰면 components/coast_palm/*를 동시에 월드에 추가해 중복 렌더하지 않는다.

검수 / 재현
scenery_qa.json은42PNG전체decode/SHA/PPU/투명padding,태양1개,세계수평선1개,5nativephase,
고정줄기·지상anchor·별칭을 오프라인검사한다. inspection에는 contact3개/배경조립예시/야자수2초GIF가 있다.
실행순서: production/build_scenery.py → production/build_layout_and_qa.py.
실제 Unity import/compile/PlayMode/Prefab/기존ID 연결은 NOT_RUN이다.
