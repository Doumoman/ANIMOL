낙일 성소 공통 장치 V02 — 각진 형태 전면 교체

무빙워크07 / 일방향 문09 / 튕김 모서리18 / 제동판19의 신규 native 원화 4장을 built-in imagegen으로 제작했다. V01 및 옛 픽셀 PNG의 필터·다각형화·색 교체는 하지 않았다. 사각/마름모 볼트, 직선 쐐기 레일 끝, 각진 금속 고정구, 두꺼운 navy 윤곽, 넓은 ivory/copper/gold 단색면과 다각형 그림자로 교체했다. 화살표와 빨간 X도 새 원화다.

모든 66 runtime PNG는 V01과 동일한 상대 경로, 320×320 / PPU256 / pivot[.5,.5] / logicalRect[32,32,256,256] / 1칸 기준을 유지한다. 40 clip의 키, 프레임 순서,12fps,루프 여부도 동일하다. 8개 endpoint alias는 해당 최신 프레임과 PNG 바이트가 같다. 모든 66파일의 decodedRGBA가 V01과 다르다. V01 파일은 수정하지 않았다.

일방향 문: 네 방향 모두 고정 rail/핀은 그대로이고 native 분할 차단봉만 수납된다. 통과 폭164px=.640625cell이다. 반대/옆 입력의 blocked는 닫힌 차단봉을 유지하며 새 빨간 X만 더한다. X영역 밖 RGBA가 closed와 같다. 출구는 검사 칸이며 두 번째 점유 칸이 아니다. 실제 거절0이동/0비용/0턴은 기존 host 검증에 연결해야 한다.
무빙워크: 가로 진행은 상하 rail+세로 slat, 세로 진행은 좌우 rail+가로 slat이다. 새 native 화살표와 벨트가 같은 방향으로 움직이며, 벨트 영역 밖 body는 픽셀 단위로 고정된다.
튕김 모서리: 새 native thin L 레일과45도 face다. 긴 rail은 고정되고 안쪽 face만0/4/1px 압축된다. 중앙160²의 opaque 비율은2.0~3.8%라 열린 통행 공간이 보인다. NE/NW/SE/SW는 원화 참조이며 실제 방향 반사는 host의 기존 함수를 사용한다.
제동판: 수평 진행에는 세로 stopbar 두 개, 수직 진행에는 가로 stopbar 두 개다. 화살표는 없다. 외곽 고정 body 안의 native pad가0/2/4px 눌리며, 반동이나 새 자동 이동을 만들지 않는다.

production/build_common.py: 오직 새 native bitmap의 crop, native strip 등록/리샘플링, 부품 이동, 벨트 반복, 새 glyph 합성, 고립 alpha speck 제거만 수행한다. RGB색면/심볼을 코드로 그리지 않는다. Component PNG는 임시 파일 후 원자 교체로 저장한다.
production/generation_prompts.json: 실제 built-in imagegen 프롬프트4개.
production/native_assembly.json: 새 source의 정확한 crop 좌표 xyxy.
common_devices_qa.json: 고정부, 실제 상이 프레임,1칸 alpha 경계 및 방향축 검수.
common_devices_revision_qa.json: V01 대비 경로/clip/metadata 유지와66파일 전체 교체, source/PNG decode 검수.
inspection: 새 contact2장과 모든4종의 motion GIF. 표시용 오프라인 예시이며 게임 실행 화면이 아니다.

66runtimePNG(고유58+endpointalias8),40clips,새native source4장. 실제 Unity Import/C#Compile/Prefab binding/Play Mode는 NOT_RUN이다. 원본 Unity 프로젝트의 실제ID, 충돌체, 이동권위 및 binding은 아직 검증하지 않았다. 실제 gameplay는 accepted action과 기존 host를 따르며 애니메이션이 비용/턴/이동을 결정하지 않는다.
