ANIMOL_STARWAY_RANCH_DEVICES_V13
별길 목장 장치 스프라이트·애니메이션·Unity 표현 연결 패키지

실사용 RGBA PNG 267개 / 애니메이션 정의 137개 / 이미지 생성 원화 16장.
양·병아리 네 방향, 별돌 3문양, 2×1 수문, 바람개비 4방향×4단계,
밀 수 있는 나무상자, 금속 상자, 둥지, 키 큰 풀, 젖은/마른 연결 수로,
공통 무빙워크·단방향 문·튕김 모서리·제동판을 포함한다.

1. 먼저 UNITY_APPLY_CLI_KO.txt를 원 프로젝트의 Unity 에이전트에게 전달한다.
2. DEVICE_RULES_KO.txt의 확정/미확정 구분을 확인한다.
3. DECOR_PERSPECTIVE_OCCUPANCY_PROMPT_KO.txt는 장식 제작 채팅에서 그대로 사용할 공통 프롬프트다.
4. Preview/OVERVIEW.png와 ANIMATION_PREVIEW.gif는 이 패키지 프레임을 조립한 참고도다. Unity 실행 화면이 아니다.
5. animation_catalog.json은 모든 프레임·fps·방향·반복·점유와 체크섬의 기준 데이터다.
6. Editor 빌더는 Unity 안에서 실제 카탈로그 asset/.anim/검사 프리팹을 만든다. 게임플레이 로직 자체는 포함하지 않는다.

최신 목장 V12 / 연결 장식 V02와 동일한 고해상도 플랫 카툰 규격을 따른다.
PPU 256 / 가운데 논리 1칸 256px / 사방 32px 여백 / Pivot 중앙.
1×1 PNG 320×320, 수문 2×1 PNG 576×320. 투명 여백까지 충돌 크기로 취급하지 않는다.
원 그림의 알파와 평면 색을 보존했으며 엄격한 16색/32px 픽셀아트 변환본은 아니다.

실사용 폴더: Sprites/, Runtime/, Editor/, animation_catalog.json.
Authoring~/native에는 내장 이미지 생성 도구의 최종 원화를 그대로 보관한다.
Authoring~/generation_prompts.json에는 제작 프롬프트를 포함한다.
기존 배경/잔디/장식 묶음을 교체하는 전체 테마 패키지가 아니다. 장치 부분에 덧붙여 적용한다.

Runtime은 확정된 게임 상태를 표시한다. 입력 판정·점유·물리·코스트·수집·장치 턴을 생성하지 않는다.
새 장치 공식 ID를 임의 배정하지 않았다. 수로 점유 예외·병아리 재추종 등은 미확정으로 보존한다.

검수 범위: PNG 디코딩·크기·알파·프레임 참조·체크섬·클립 순서·방향/상태 커버리지·ZIP 압축 해제 대조.
실제 Unity 임포트 / C# 컴파일 / 원 프로젝트 바인딩 / PlayMode: 모두 미실행.
QUALITY_REPORT.json에 검사 결과와 미실행 항목을 별도로 남겼다.
