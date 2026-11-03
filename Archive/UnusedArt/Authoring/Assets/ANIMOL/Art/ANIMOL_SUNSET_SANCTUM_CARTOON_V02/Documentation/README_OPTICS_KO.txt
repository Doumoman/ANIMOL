낙일 성소 V02 — 광학 장치 + 유리 벽

범위: 방향별 광원, 고정 핀 거울, 광 수신기, 연동문, 유리 벽.
V01 메타데이터/경로/클립 키를 유지하고 production bitmap은 새 native imagegen 원화로 교체합니다.
모든 장치는 1칸, 320×320, PPU256, pivot [0.5,0.5], logicalRect [32,32,256,256]입니다.
각진 외곽선: 긴 직선, 큰 모서리 절단, 사다리꼴, 보석형 핀; 원형 하우징/후면 원형 받침을 사용하지 않습니다.
단색 재질+한 단계 셀 그림자, 진한 네이비 외곽선, 아이보리/골드/구리와 제한된 청록 강조를 사용합니다.

production/build_optics.py는 새 원화의 크롭/알파 거터 정리/등록/레이어 조립만 수행합니다.
원화 RGB를 코드로 칠하거나 V01 원화를 필터링/다각형화하지 않습니다.
문짝은 새 native panel의 두 반쪽을 고정 native frame 아래에서 수평 이동합니다.
열린 문과 유리의 중앙은 진짜 투명 알파이며 실제 호스트의 바닥이 보입니다.
거울은 native로 그린 5개 yaw 포즈, 고정 중심핀/받침을 사용합니다. 스프라이트 전체 회전이 아닙니다.

게임 동작은 호스트의 기존 authoritative trace/state를 사용해야 합니다.
- 광원/거울/수신기/연동문은 같은 실제 ray trace를 공유합니다. 장치 원화에 긴 빔은 포함하지 않습니다.
- 광 수신기가 켜진 동안만 연동문이 열립니다. 애니메이션 종료가 판정을 바꾸지 않습니다.
- 거울 직접 조작 입력 의미는 미확정이며 기존 프로젝트의 입력 로직에 위임합니다.
- 유리는 온전할 때 빛을 통과시키고 몸체/바람을 막습니다. 깨진 칸에 들어온 actor는 빛을 막습니다.
- crack은 한 번의 기존 bomb/statue 파괴 이벤트 중간 포즈입니다. 2회 타격/새 내구도 규칙이 아닙니다.
- 실제 게임 ID/씬 바인딩은 확인되지 않았습니다.

inspection PNG/GIF는 오프라인 원화/프레임 확인용입니다. 실제 Unity import/compile/Play는 NOT_RUN입니다.

광원 시각 anchor 예외 (V02):
새 비대칭 native 몸체를 248px 최대 길이로 1칸 안에 크게 등록했습니다. 이에 opticalAnchorPixel은
E[123.449,160], N[160,205.939], W[198.814,160], S[160,116.134]로 진행축을 따라 이동했습니다.
이는 원화 iris 위치를 정직하게 기록한 시각 메타데이터 예외입니다. 점유/피벗/게임 판정은 바뀌지 않습니다.
20개 emitter 행 모두 logicalRayAnchorPixel=[160,160]이고, opticalAnchorRole 필드에 시각용임을 명시합니다.
출력 경계 anchor는 E[288,160]/N[160,32]/W[32,160]/S[160,288]로 V01과 같습니다.
기존 광선은 cell center160에서 같은 cardinal 직선축을 따릅니다. native iris를 gameplay 시작점으로 사용하지 마세요.
진행축의 직교 offset0, 전 phase irisanchor 고정, 논리 rayanchor와 경계출구 불변을 optics_qa.json에서 검사합니다.
production/native_geometry_proof.json에 동적 iris76×76 영역 및 mirror native sweep RLE와 고정 부품을 제공합니다.
