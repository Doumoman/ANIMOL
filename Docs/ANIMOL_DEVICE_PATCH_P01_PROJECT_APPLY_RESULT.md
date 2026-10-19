# ANIMOL_DEVICE_PATCH_P01 프로젝트 적용 결과

2026-10-19. 실제 프로젝트: Unity 6000.3.8f1, Built-in Render Pipeline. 열린 Editor의 Unity MCP로 임포트, 카탈로그·머티리얼 생성, TestRunner와 Play Mode를 실행했다. 별도 배치 Editor는 실행하지 않았다.

## 적용 범위와 원본 보존

| 항목 | 실제 경로·결과 |
| --- | --- |
| 목장 패키지 | `Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12` |
| 극장 패키지 | `Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03` |
| P01 공통 코드 | `Assets/ANIMOL/Scripts/Runtime`, `Assets/ANIMOL/Scripts/Editor`, 기존 Editor 테스트 어셈블리 |
| P01 셰이더·머티리얼 | `Assets/ANIMOL/Shaders/AnimolVividWindRoute.shader`, `Assets/ANIMOL/Materials/VividWindRoute.mat` |
| 변경 전 백업 | `C:\Users\user\Documents\GitHub\ANIMOL_DevicePatchP01_Before_20261019.zip` (Assets 밖) |

패치 기준의 목장·극장 manifest/catalog SHA와 기존 파일 107개의 `expectedBaselineSha256`가 모두 일치했다. 패치의 런타임·Editor 변경 140개를 해당 기존 경로에 병합했다. 제작용 `sources/`, `production/`은 Assets에 넣지 않았다. [overlay_audit.json](Validation/DevicePatchP01/overlay_audit.json)에 복사 전·후 해시와 대상 경로가 있다. 기존 설치 파일을 무조건 대체한 불일치는 없었다.

목장 런타임 스프라이트 1,937개, 클립 54개; 극장 런타임 스프라이트 1,977개, 클립 202개다. 패치 대상 PNG는 각각 48개와 55개이며 다른 family 자산은 유지했다. 생성된 `.anim` 256개 모두 `SpriteRenderer.m_Sprite` 참조 곡선 1개를 가지며, 빈 프레임·게임 이벤트·Transform 곡선이 0개였다. 새 SHA는 다음과 같다.

| 패키지 | `animation_catalog.json` SHA256 = txt = 생성 카탈로그 SourceManifestSha256 |
| --- | --- |
| V12 | `493330ea695d90fa3fad55059488aed2063a4def6015778674719007d5bf4378` |
| V03 | `942291a89c38c545a8c0cecb8c29a86b0ad934a715073f5dd509faf7dc6556bb` |

기존 PNG 87개의 GUID와 Sprite subasset fileID는 Unity 재임포트 전후 모두 일치했다. 전체 대응은 [guid_fileid_before.csv](Validation/DevicePatchP01/guid_fileid_before.csv)와 [guid_fileid_after.csv](Validation/DevicePatchP01/guid_fileid_after.csv)에 있다. 예: 목장 문 `db2341a048998e64284ae4781bd1a69c:21300000`, 목장 바람 `0ac70e49b27cee14f8bdaa78064810c3:21300000`, 극장 문 `865f79168b2efa746b0ff5b1e4344648:21300000`, 도미노 `0a15f30491aca49448e39b5bfa1a5b75:21300000`, 책장 `e0310c6ea0a1a434f88d2c6d0cefeca3:21300000`.

## 실제 호스트 바인딩

테마 저장 ID는 `starway`, `memory_theater` 그대로다. 도감 번호와 실제 `PrototypeRole` 값은 이 프로젝트에서 우연히 일치하며 새 ID를 만들지 않았다. 권위 있는 상태·점유·코스트는 `PuzzleDeviceSession`, 입력 커밋과 tween은 `PuzzlePrototypeController`, 시각 인스턴스화는 `PuzzleDeviceView`가 담당한다. 프리팹의 `DeviceVisualView`만 P01 카탈로그 상태와 클립을 참조한다.

| 테마 / 실제 역할 | 실제 ID / 상태 출처 | 방향·클립 연결 | 점유·동작 |
| --- | --- | --- | --- |
| 목장·극장 일방향 문 | `PrototypeRole.OneWayGate=9`, scenario의 `gate`, `direction` | 실제 `Vector2Int` N/E/S/W → `oneway_gate_{heading}_opening/open/closing/blocked`; 시각 이벤트는 `allowed_passage_begin/clear`, `existing_rejected_gate_contact` | `OneCellGateRules`를 `GateAllows`에 연결. gate 한 칸만 점유; 출구는 통과 검사. 거절은 이동·코스트·턴 0. |
| 목장 바람 통로 | `PrototypeRole.Wind=20`, scenario의 `blower` (`wind1/2`는 기존 한 칸 변형) | 실제 `direction` → `wind_corridor_{N/E/S/W}_active/idle`; 범위는 `AnimolWindRouteRenderer` | 기존 `ReflectCorner`가 방향을 꺾는다. `WindRoute.TryGetForce`와 같은 route를 힘·MeshRenderer가 사용. 실제 `secondsPerCell`과 tween 진행도에 맞춰 흐름을 재생. |
| 극장 도미노 | `PrototypeRole.Domino=13`, scenario의 `domino2/3/4` | 각 확정 붕괴 신호 `this_domino_piece_fold_committed` → `domino_wall.fall.{heading}` | 각 셀의 1×1 프리팹. 한 행동의 세 확정 신호를 짧게 엇갈려 재생하고 마지막 낮은 바닥 상태를 유지. 턴·비용은 한 번만 확정. |
| 극장 책장 | `PrototypeRole.Bookshelf=8`, scenario의 `shelf`, phase 짝수 H / 홀수 V | H→V `id08_bookcase_H_to_V`, V→H `id08_bookcase_V_to_H` | 중심 핀 고정, H와 V가 각각 3개 셀. P01의 세 칸 원화 계약과 실제 호스트가 일치해 사전 조건 통과. 회전·점유 판정은 기존 Session. |

`DeviceKind.Wind`를 쓰는 별도 클래식 캠페인 맵은 자기 셀에서만 작용하는 기존 규칙이다. 삭제된 `flow_effect` 스프라이트 재생을 제거하고 V12 본체 active/idle만 유지했다. 이를 ranged ID20 바람으로 바꾸지 않았다. 프로토타입 ID20 안에서도 `blower`의 힘은 대상당 한 유효 행동에 한 칸이며, 기존 `wind1/2` 필드 변형의 연결 이동은 승인된 맵 경로를 위해 유지했다. 장식용 `props/windmill`과 배경 풍차에도 힘을 추가하지 않았다. 현재 프로토타입 뷰는 로드마다 Instantiate/Destroy하며 재사용 풀을 사용하지 않는다. 파괴 시 바람 route를 숨기고 action sequence를 초기화한다.

실제 원형 점유 출력(기준 전후 동일): 문 `gate=(3,2)` 한 칸; 바람 `blower=(1,4)`, `wind1=(2,2)`, `wind2=(3,2)` 각 한 칸; 도미노 `(2,2),(3,2),(4,2)` 각 한 칸; 책장 H `(3,3),(4,3),(5,3)`이고 중앙 `(4,3)` 고정, V는 `(4,2),(4,3),(4,4)`다. 원래 `Footprint` 소스와 적용 후 실제 `PuzzleDeviceSession` 출력으로 확인했다. 맵 데이터·Collider·저장 ID는 수정하지 않았다.

## 레거시·참조 검사

변경된 두 패키지의 373개 프리팹 검사에서 Missing Script/Sprite/Clip과 구 `effects/wind_corridor`, V11, V02 경로 사용은 모두 0건이었다. 두 프로토타입 씬과 장치 QA 씬의 Missing Script/Sprite도 0건이었다. 실제 Play Mode의 목장 장치 18개, 극장 장치 21개는 `MissingBindings=0`이었다. 활성 C#·데이터·프리팹·씬에 구 `flow_effect`/`gust_` 로딩 문자열과 이전 V12 SHA 참조가 0건이었다.

`p01_retired_assets.json`은 후보 목록으로만 사용했다. 이전 바람 PNG 16개를 참조하던 오래된 생성 `.anim`/시퀀스 8개의 외부 PPtr 참조가 모두 0임을 확인한 뒤, 그 8개와 PNG 16개 및 구 `wind_effect_manifest.json`을 각각 `.meta`와 함께 제거했다. 전체 폴더 삭제는 없었다. [asset_audit.json](Validation/DevicePatchP01/asset_audit.json)은 삭제 전 후보 참조, [retirement_audit.json](Validation/DevicePatchP01/retirement_audit.json)은 25개 파일의 외부 참조 0건과 실제 삭제를 기록한다. 원격/배포된 AssetBundle 카탈로그는 이 로컬 프로젝트에 확인할 연결이 없어 검증하지 않았다.

## 실행한 검증

| 상태 | 검사 | 근거 |
| --- | --- | --- |
| PASS | Unity 임포트·컴파일, P01 셰이더 | 마지막 Editor ground truth `compilationFailed=false`, 오류 0. `ANIMOL/DevicePatch01/VividWindRoute` supported, `ShaderHasError=false`. |
| PASS | 전체 TestRunner | 최종 EditMode **203/203**, PlayMode **53/53**. [unity_test_results.json](Validation/DevicePatchP01/unity_test_results.json). |
| PASS | 문 4방향·두 테마, 거절/출구 막힘/내부 옆 이탈/1칸 점유 | `DevicePatchP01HostTests` 2개 케이스, `OneCellGateRulesTests` 4개, 기존 장치 로직 테스트. |
| PASS | `blower` 한 칸 힘, 기존 필드 연결 이동, 단일 코너 힘·표시 route 일치, 순수 경로의 차단·거리·순환 | 호스트 바람 테스트, `AnimolWindRouteTests` 9개, P01 PlayMode 검사 및 기존 Device20 검증 맵 회귀. |
| PASS | 도미노 3개 실제 체인·순차 시각·한 코스트, 책장 H/V 고정 중심 | 호스트 테스트, `DevicePatchP01PlayTests`, 기존 `PuzzleDeviceLogicTests`. |
| PASS | 카탈로그·PPtr·프리팹 연결 | 생성 클립 256개 바인딩 검사, PNG 87개 GUID/fileID 대조, 테마 프리팹 373개 검사. |
| PASS | 프로토타입 Play Mode 시작·바인딩 | 목장 18개·극장 21개 `MissingBindings=0`; [목장 1080×1920 시작 화면](Validation/DevicePatchP01/starway_prototype_play.png), [극장 1080×1920 시작 화면](Validation/DevicePatchP01/theater_prototype_play.png). |
| PASS | Play Mode 장치 장면 확인 | [확정된 문 닫힘](Validation/DevicePatchP01/ranch_gate_closing_play.png), [목장 바람 모서리 경로](Validation/DevicePatchP01/ranch_wind_corner_play.png), [도미노 첫 패널 확정 직후](Validation/DevicePatchP01/theater_domino_chain_play.png), [책장 V 최종 상태](Validation/DevicePatchP01/theater_bookcase_rotated_play.png). 바람 모서리는 테스트용 맵 복제에 모서리·목재 상자를 추가한 Play Mode 장면이며 프로젝트 레벨을 변경하지 않았다. 단일 캡처는 전체 재생 영상이 아니다. |
| NOT_RUN | 네 방향 문의 실제 통과 중 충돌체 안전성, 바람 다중 방출기 우선순위, 금속 뒤 실제 플레이 경로, 책장 회전 목적지 점유의 현장 시연 | 순수 규칙과 제한된 호스트 테스트는 실행했으나 이 모든 조합의 독립 Play Mode 시연은 만들지 않았다. |
| NOT_RUN | 네 장치의 전체 전환 영상, 실제 모바일 렌더 및 원격 콘텐츠 카탈로그 | Play Mode 단일 프레임을 촬영했으나 연속 영상이나 모바일 검증은 수행하지 않았다. |

패치 적용 대상 파일 목록은 [changed_paths.txt](Validation/DevicePatchP01/changed_paths.txt)에 있다. 이 문서는 기존 보관 프로젝트나 다른 세 테마의 변경을 포함하지 않는다.
