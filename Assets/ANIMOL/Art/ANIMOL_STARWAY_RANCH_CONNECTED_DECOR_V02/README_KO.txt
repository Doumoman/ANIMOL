ANIMOL 별길 목장 연결 장식 V02 — 7종에서26종 확장·새 원화 교체

실제 장식 종류는 연결6종+다칸12종+한칸8종=26종이다. 마스크·꽃 변형·보조 기둥을 종류 수로 세지 않는다.
기존 연결 장식 V01의7종도 새 native imagegen 원화로 다시 제작했고19종을 더했다.
기존298런타임 상대 경로와 기존4묶음의 논리 점유를 유지하며64PNG를 추가한 총362런타임PNG다.
예전 PNG 인코딩만 바꾼 것이 아니라 대응298개 decoded RGBA가 전부 달라야 통과한다.
크림색 목재·에메랄드 마감·민트/세이지 잎·노란 건초/꽃·보라색 돌과 굵은 남색 윤곽선을 사용한다.
긴 직선·쐐기·각진 실루엣과 넓은 기본색/그림자 셀 음영을 따른다. 옛 픽셀/16색 규칙은 폐기했다.
새 원화는 built-in imagegen으로 제작했으며 전체 프롬프트·원화·자르기·등록 출처를 포함한다.

구성
- cream_fence와 meadow_stone_wall:각16마스크+선택 보조기둥1개씩.
- orchard_trellis,herb_planter_strip,ranch_lantern_strip:각16마스크.
- raised_flower_meadow:8이웃 원본256마스크+선택 꽃 겹그림4개. 잔디를16/47상태로 축소하지 않는다.
- 다칸12묶음과 한칸8소품:각1개 전체그림. 그룹/점유마다 쪼개 반복하지 않는다.
- Runtime/Editor:최신 카탈로그/엄격 임포터/순수 배치 계획/Controller와 실제 호출 Binding,
  typed BoardSnapshot/BoardAsset/PublishedSource/AuthoredSource/검사 Prefab·선택 씬 생성기.
- Examples:전체26종을 각각 포함하는 Stage65연결칸+20묶음=120점유,All2696연결칸+20묶음=151점유.
- Inspection:도감·격자 배치도와 기존 목장 하늘/지형 위 합성 참고도. 실제 Unity 스크린샷이 아니다.
- sources/Authoring:원화와 제작 출처. 원화/초안/검수 이미지를 인게임 선택 목록에 넣지 않는다.
- Tests~:선택 NUnit 소스. 실제 TestRunner 실행 전까지 NOT_RUN이다.

크기와 연결
기본 한칸은256px/PPU256=world1이다. V02의 모든 연결그림/꽃/보조기둥은320×320,
이미지 위쪽 기준 logicalRect=[32,32,256,256],Unity 아래쪽 기준 pivot=[0.5,0.5]다.
다른 테마의320×384 등록을 이 팩에 복사하지 않는다. 다칸은 각 manifest의 전체 점유/지면/pivot을 따른다.
PPU와 지면 중심을 유지하고 전체 PNG나 alpha bounds를 한칸으로 강제 축소하지 않는다.
같은 family/connectionGroup/logicalLayer/heightKey의 실제 전체 장식 맵 이웃만 연결한다.
보통5계열은 N1E2S4W8,잔디만 N1NE2E4SE8S16SW32W64NW128의 원본8비트 전부를 사용한다.
대각만 바뀌어도 잔디의 해당 모서리/안쪽 구멍을 다시 계산해야 한다.
꽃은 이미 있는 잔디 칸의 StableHash가5로 나누어떨어질 때만 (hash/5)%4의0..3변형을 고른다.
꽃/기둥은 추가 점유를 예약하지 않으며 매프레임 랜덤으로 바뀌지 않는다.

적용
새 namespace ANIMOL.Art.RanchConnectedDecorV02,packageId=ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02.
manifest_sha256.txt 현재 핀=770e8085e6cd8c6804c062742f44888ab0fd395282bee413366888ea2d32e38c
UNITY_CLI_KO.txt를 실제 Unity 프로젝트에서 작업할 CLI에게 전달한다. PNG 복사만으로 끝내지 않는다.
기존 V01과 새 V02의 namespace/폴더를 섞거나 .meta/GUID를 통째 복제·치환하지 않는다.
새 카탈로그→BoardAsset→Source→Binding→Controller로 실제 호출되는 검사 Prefab 생성기가 들어 있다.
실제 게임은 현재 장식 데이터와 순수 조회한 허용 셀에서 전체 스냅샷을 만들어 Publish하는 얇은 어댑터가 필요하다.
Binding은 Source.Changed를 구독해 갱신하고 실패 시 자기 소유 그림만 지운다. 옛 그림 fallback이 없다.
PublishedSource가 비활성화되면 옛 데이터가 무효화되므로 풀 재사용 후 최신 보드를 다시 Publish한다.

범위 보존
이 묶음 때문에 CartoonV12 전체를 다시 설치하지 않는다. 실제 설치된 이후의 문/바람 패치도 보존한다.
일방향 문1칸과 닫힘 거절/X,바람 영역/굴절,고정 풍차 몸체+회전 로터,병아리4방향 idle/walk와 눈광,
별 모양 돌/홈·누름판/덩굴다리·무빙워크/제동판/열린 ㄴ자와 기존 장치 시퀀스를 이 장식으로 덮지 않는다.
병아리 .62배는 visual 자식에 한번만 적용한다. 캐릭터·논리 루트·이동·코스트·점유는 바꾸지 않는다.
G=grass,W=wood_bridge,S=stone_bridge. S는 자기 석재 지지대를 쓰며 G로 정규화하지 않는다.
기존 W/S다리의 공유 난간/꼭짓점 기둥/이어진 하부를 새 장식으로 중복 생성하지 않는다.
닭장/여물통/건초/수레는 정적 장식이며 병아리 생성기·둥지·기능 장치가 아니다.
별은 모든 구름 뒤에 거의 고정,구름과 가까운 섬은 더 민감하게 반응하는 기존 깊이를 유지한다.
새 달·행성·태양을 추가하지 않는다. 기본 바닥·배경·UI·다른 테마는 이번 교체 대상이 아니다.

검수
QA/prior_v01_decoded_comparison.json은 기존298경로의 실제 RGBA 교체와 점유 보존을 기록한다.
python3 QA/independent/audit_package.py --package <패키지루트>로 파일·접합·점유를 검사할 수 있다(Python/Pillow).
원 Unity 프로젝트와 Editor/컴파일러가 없어 실제 Import/C#/Shader/NUnit/Prefab생성/게임 연결/
Play/접근 가능한 레거시 참조0은 NOT_RUN이다. 동봉 소스·PNG·오프라인 검수는 이 실행의 증거가 아니다.
다운로드는 분할 없는 통합ZIP 하나다. 제작 원본·코드·적용 지시·참고도를 모두 포함한다.
