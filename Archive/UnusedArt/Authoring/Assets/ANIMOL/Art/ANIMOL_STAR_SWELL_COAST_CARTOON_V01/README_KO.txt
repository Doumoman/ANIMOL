ANIMOL 별너울 해안 — 각진 카툰 V01

별너울 해안의 모래·젖은 모래 마감·목재/석재 다리·바다·노을·해안 장식과 공통4종/해안4종 장치를 새 native 원화로 교체하는 패키지다. 굵은 남색 윤곽선, 긴 직선과 큰 모서리 컷, 넓은 기본색+그림자 평면을 사용한다. 모래·테라코타·청록 바다·유목·슬레이트·산호빛 노을·상아색 포말이 같은 그림체로 이어져야 한다. 구80PPU/16색/1px/둥근 모서리 규칙은 폐기했다. 낙일 성소의 원화를 재색칠한 팩도 아니다.

설치와 기존 프로젝트 보존
패키지 폴더 이름 ANIMOL_STAR_SWELL_COAST_CARTOON_V01을 유지해 Assets 아래에 설치한다. 분할 다운로드는 01_CORE, 02_FLOOR, 03_BRIDGES 3개 ZIP을 같은 위치에 모두 풀어, 동일 상위 폴더 ANIMOL_STAR_SWELL_COAST_CARTOON_V01 하나로 합친다. 이3개가 인게임 설치 필수이며 04_SOURCES는 원화/편집/전체 provenance 검수용 선택 파일이다. 서로 다른 위치에 각각 설치하지 않는다. 원화 제작 소스와 검수 화면을 인게임 Sprite 후보에 넣지 않는다. 최종 교체본은 runtime1097 PNG/105 clips/49 endpoint aliases이며27 source PNG는 인게임 선택 대상이 아니다. manifest.json과 animation_catalog.json이 기준이다.
CLI_HANDOFF_KO.txt를 실제 Unity 프로젝트 CLI에게 전달한다. 해안의 실제 theme/device/map/save ID, 방향 enum, 점유 목록, Prefab/SO/Scene/Animator/풀/Addressables 연결을 조사한 뒤 새 catalog/Sprite 참조로 통제하여 전환한다. 다른 테마·플레이어·UI·공용 자산을 삭제하지 않는다. 기존 외부 GUID/.meta를 일괄 치환하지 않고 같은 새 패키지 갱신 시 .meta를 보존한다.
새 namespace는 ANIMOL.Art.StarSwellCoastV01이다. 검증된 animation_catalog.json의 SHA와 expectedManifestSha256를 함께 연결하고 최신 endpoint만 선택한다. 실제 호스트 행동과 시각 재생은 단일 권한 경로로 연결한다.

지형·물·하늘
논리1칸은256px/PPU256=world1이다. 보통320×320의[32,32,256,256]이 지면 슬롯이며 여백·높이·회전 sweep·alpha는 점유/Collider가 아니다. 육지는 coast_sand(A),wood_bridge(W),stone_bridge(S)의8이웃 full256으로 고르고 재질 경계도 연결한다. 물은 육지 점유와 이웃 계산에서 제외한다.
물 carrier/물결/포말은 시각 요소다. 반복 주기·alpha·정렬과 host water-cell 배치를 따르며 새로운 걷는 타일·수류·비용·충돌을 만들지 않는다. OceanField는 world 수평선 아래의 원경 바다 반복용이다. Sprite.bounds 전체 크기로 반복하므로 전체 캔버스가 연속 패턴인 전용 carrier를 사용하고 기본 장치의 투명 padding 있는 이미지를 넣지 않는다. 실제 물칸/배 경로 렌더러를 대신하지 않는다.
하부는2/3/4칸이 이어진 불규칙한 하나의 몸체이며 긴 구간은 안정된2~4칸 덩어리로 나눈다. 안쪽 구멍은 얕은 cap으로 열어두고 물/배/출구를 가리는 긴 벽을 만들지 않는다.
독립된 각진 석양 해는 정확히1개다. 해가 박힌 하늘과 분리 해를 중복 렌더링하거나 수평선/해를 세로 반복하지 않는다. 별은 모든 구름 뒤에서 거의 고정되고 구름·가까운 섬은 카메라에 더 민감하게 반응한다. 명시적 sorting과 worldZ를 함께 사용한다. background_integration_policy.json의 fixedWorldAnchors/excludeFromBackgroundRig를 따라 unique_world_horizon과 one_setting_sun은 BackgroundRig에서 제외하고 world 고정 Transform으로 둔다. cameraMultiplier0도 일반 BackgroundRig에서는 카메라를 따라가므로 world 고정 의미로 해석하지 않는다.

장치의 고정 규칙
이탈 가시는 플레이어가 밟은 뒤 실제로 떠날 때만 솟는다. 다시 내려가는 조건은 미확정이므로 실제 host reset/undo 상태를 따른다.
나룻배는 기존의 같은 플레이어와 물 한 칸을 함께 움직인다. 육지로 내리면 배는 마지막 물칸에 남는다. 막힌 물칸/육지로는 움직이지 않으며 새 캐릭터를 만들지 않는다.
구르는 통은 장애물 직전까지 굴러가지만 밀던 플레이어는 통의 원래 칸까지만 간다. 첫 이동 칸이 막혔으면 밀기 전체가 무효다.
폭탄의 첫 실제 이동 밀기는 숫자3을 표시한다. 그 뒤 자기 플레이어의 유효 행동3회가2→1→폭발이다. 첫 밀기에서 바로2로 줄지 않고 무효 입력·타 플레이어·자동 연쇄 중간 단계·실시간은 카운트를 진행하지 않는다. 폭발 범위 수치/모양·플레이어/일반 물체 피해는 미확정이다. 실제 host의 명시적 blast cell 목록만 표시하며 기본 십자/반경/피해/연쇄 폭발을 만들지 않는다.
공통 장치는1칸 일방향 문과 닫힌 거절봉/X, 진행 축과 같은 무빙워크, 얇은 열린 ㄴ자 모서리, 진행에 수직인 제동선으로 표시한다. 자동 연쇄는 원래 한 행동의 비용을 유지한다.

검수 상태
파일/원화/방향·상태 프레임/접합/오프라인 씬 검수와 실제 Unity 검증은 별개다. 원본 Unity 프로젝트가 없어 Import/C#/Shader/NUnit/실제 Prefab 바인딩/Play Mode/실제 레거시 참조0은 NOT_RUN이다. source_behavior_contract.json은 도감 근거와 미확정 규칙, actual_gameplay_checks_NOT_RUN.json은 실제 프로젝트에서 실행할 시나리오를 담는다. 소스 코드 읽기나 GIF 재생을 실제 컴파일/플레이 성공이라고 보고하지 않는다.

오프라인 검수 명령
필수01~03 합본: python QA/verify_coast_package.py --package "<상위경로>/ANIMOL_STAR_SWELL_COAST_CARTOON_V01" --runtime-only --report "coast_runtime_check.json"
04_SOURCES까지 합친 전체본: python QA/verify_coast_package.py --package "<상위경로>/ANIMOL_STAR_SWELL_COAST_CARTOON_V01" --report "coast_full_check.json"
검수 스크립트 옆 QA/rejected_prior_art_hashes.json을 보존한다. 오프라인 전수 파일/원화/프레임/6144접합/씬 검수 PASS_OFFLINE이며 실제 Unity 시나리오76개는 모두 NOT_RUN이다.
