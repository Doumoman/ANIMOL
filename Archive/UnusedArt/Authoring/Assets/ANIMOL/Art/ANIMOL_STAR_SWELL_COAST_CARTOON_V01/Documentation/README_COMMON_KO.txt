별너울 해안 공통 장치 V01 — 각진 목재 부두 원화

무빙워크07 / 일방향 문09 / 튕김 모서리18 / 제동판19를 새 built-in imagegen 원화4장으로 제작했다. 밝은 유목, teal/coral 칠 목재, 꺾인 ivory 밧줄, 사각/마름모 황동 고정구, 두꺼운 navy 외곽선과 넓은 다각형 명암으로 통일했다. 성소·옛 해안 bitmap을 복사/필터/재색칠하지 않았다. 화살표와 X도 새 해안 원화다. 실제 생성 프롬프트는 production/generation_prompts.json에 있다.

runtime66개(58고유프레임+8최신endpointalias), clip40개,12fps. 성소V02의 generic 경로/키/프레임순서/루프/PPU256/pivot[.5,.5]/논리영역[32,32,256,256]/320²를 유지한다. 모든 runtime decodedRGBA는 성소V02와 다르다. 실제 gameID와 점유는 미확인 null이며 alpha를 collider나 register게임셀로 사용하지 않는다.

일방향 문: 한 칸 안의 고정 목재 rail 두 개와 분할 차단봉이다. 두 rail은 새 원화에서 각각 등록해 움직이는 동안 픽셀 단위로 고정된다. 완전히 열린 통로는164px=.640625cell이며 그 중앙은 실제 alpha0이다. 반대/양옆 접근에는 닫힌 봉을 유지하고 새 coral X판만 나타난다. X영역 밖은 closed와 같은 RGBA다. 허용 방향이어도 출구가 막히면 열리지 않아야 한다. 출구는 검사 칸이고 두 번째 점유 칸이 아니다. 0이동/0비용/0턴 거절과 출구 판정은 실제 host의 검증 로직에 연결한다.
무빙워크: E/N/W/S 각각 새 원화다. 가로 진행은 위아래 목재 rail+세로 slat, 세로 진행은 양옆 rail+가로 slat다. 실제 slat/printedarrow가 같은 방향으로 스크롤하고 belt창 밖 body는 고정된다. 자동 이동은 기존 accepted action 안에서만 진행하며 추가비용은 없다.
튕김 모서리: NE/NW/SE/SW의 얇은 열린 목재 L과45도 coral fender, 꺾인 밧줄 brace다. 긴 rail은 고정하고 안쪽 native face만0/4/1px 압축한다. 중앙160² 중 opaque비율8.2~8.8%로 열린 통행 공간을 남긴다. 실제90도 반사 방향은 기존 host의 함수를 사용하고 원화이름으로 새 규칙을 만들지 않는다.
제동판: 수평 진행에는 세로 stopbar2개, 수직 진행에는 가로 stopbar2개다. 운송 화살표는 없다. 외곽 프레임은 고정되고 안쪽 pad만0/2/4px 눌린다. 같은 칸에서 기존 강제 이동을 멈추는 피드백이며 반동/추가이동/새 타이머를 만들지 않는다.

제작 재현: production/build_common.py는 새 원화 crop/독립 rail 등록/원화strip 리샘플링/native 부품 이동/반복belt와glyph 합성/고립alpha speck 제거만 한다. 코드로 production색면·화살표·상자를 그리지 않는다. 부품은 임시PNG 후 atomic교체로 저장한다. production/native_assembly.json은 원화별 xyxy crop좌표다.
common_devices_qa.json: 실제 프레임 구별, 고정부,1칸경계, 방향, open alpha 검수.
common_devices_revision_qa.json: generic 경로·40클립 유지,66파일 전체 새RGBA, source4·alias·decode 검수.
inspection/oneway_gate_contact_sheet.png / common_devices_contact_sheet.png / common_devices_motion.gif: 오프라인 원화/동작 예시다.
공유glyph source는 sources/coast_gate_parts_native.png다. 다른 장치가 쓰는 경우 같은 basename/같은 bytes와 정확한 crop provenance를 사용한다.

원본Unity프로젝트가 없으므로 실제 import/C#·shader compile/Prefab바인딩/PlayMode/레거시참조0은 모두 NOT_RUN이다. 검토GIF가 게임 동작 검증을 대신하지 않는다. 기존실제host가 이동·차단·비용·턴·점유·반사 판정을 결정하며 애니메이션이 이를 구동하지 않는다.

축소 가독성 보강: 두 고정 rail마다 새 native arrow를 장축40px로 합성했다. 약80px게임칸에서12.5px장축으로 읽히며 모든 closed/open/transition/blocked상태에서 같은 좌표다. 중앙164px alpha0와 고정부는 그대로 유지한다. sourceRGB와 공유 parts bytes는 변경하지 않았다.
