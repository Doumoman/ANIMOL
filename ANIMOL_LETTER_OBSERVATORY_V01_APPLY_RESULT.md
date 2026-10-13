# 편지 천문대 적용 결과 — 입력 파일 손상으로 미적용

2026-10-14. 기준 커밋 `ea7336164e7ca8bde7eab557fe13684968810aa5`. **임포트·프리팹·새 QA 씬·현재 플레이 씬 연결은 완료하지 못했다.** 대상 ZIP의 런타임 PNG가 모두 누락돼 가짜 자산이나 placeholder 씬을 만들지 않았다. 정상 ZIP을 같은 경로에 다시 제공해야 재개할 수 있다.

## 실제 입력 검사

`MCP_INBOX/ANIMOL_LETTER_OBSERVATORY_V01.zip`: 9,139,036 bytes. SHA-256 `d79804c2cb77e78fd3ec8fb2e8eb85d0e8a140b384eed946ce3f8f707410c772`.
Python zipfile은 BadZipFile을 반환했다. ZIP local header는 있으나 EOCD/중앙 디렉터리가 없다. 로컬 헤더를 순차 해석하고 크기/CRC32를 대조해 13개 문서·미리보기만 외부 Temp에 복구했다. `reference/ART_REFERENCE.png`의 DEFLATE 스트림이 미완결이라 해당 항목은 복구하지 않았다. 원본 ZIP을 변경하지 않았다. Downloads/Desktop/Documents에서 다른 ZIP 사본을 찾았으나 동일 저장소 입력만 있었다.

복구된 manifest가 선언한 **정적 PNG 118 + 프레임 PNG 609 = 727개 중 727개 전부 없음**, 클립 120개 모두 생성 불가. 필수 문서 unity_import_manifest.json과 source/ADDED_DECISIONS_KO.txt도 누락됐다. [입력 감사/전수 누락 경로](Docs/Validation/LetterObservatory/input_audit.json), [CRC 복구 기록](Docs/Validation/LetterObservatory/zip_recovery.json).

README, manifest, 전체 DEVICE_EVENT_BINDINGS 120행, OPTICS_RULES/MOTION_BINDINGS, 제작 QA 및 POLISH_REVIEW를 읽고 [복구 문서](Docs/Inbox/LetterObservatory)에 보존했다. POLISH_REVIEW의 한국어에는 원본 UTF-8 U+FFFD 손실 문자가 들어 있다. 미리보기 PNG/GIF는 Temp에만 있고 활성 Sprite로 사용하지 않는다. 제작 QA의 passed=true는 과거 제작 검사이며 손상된 현재 ZIP이나 실제 Unity 검수를 통과시켰다는 의미가 아니다.

## 현재 프로젝트 조사

AGENTS.md 전체·Unity 6000.3.8f1·패키지·현재 코드/테마/씬을 확인했다. 활성 프로젝트는 세 아트 QA와 표현 코드이며 실제 스와이프·이동·비용·자동 연쇄·장치 턴 판정 호출자는 없다. 테마 카탈로그는 memory_theater이며 Grid는 없다. QA 셀은 1 world unit/80 PPU를 사용한다. 실제 연결 Editor 상태/빌드 씬/테마 목록은 [현재 상태](Docs/Validation/LetterObservatory/editor_state.json)에 기록한다.

기존 표시 재사용점은 DeviceVisualView/SpriteFrameSequence, DeviceThemeCatalog의 아트 재바인딩, 행동 ID 중복 방지다. UI는 PuzzleHudView의 미연결 COST --이며 실제 비용 모델이 아니다. 원본 자산이 확보돼도 현재 코드에는 도약/광선 추적/자석/포털/유리 충돌·파괴 판정이 없으므로 게임 통합은 별도로 필요하다. 레거시 러너나 맵 제작기를 복원하지 않았다.

## 규칙 확인과 미확정 항목

OPTICS_RULES의 진행 방향 8반사는 slash E→N/N→E/W→S/S→W, backslash E→S/S→E/W→N/N→W다. 유리는 빛 통과, 불투명 벽·목재/금속·플레이어는 차단이라는 선언을 읽었으나 실제 추적을 실행하지 않았다. MOTION_BINDINGS는 3셀 도약/320ms/18px의 시각 권장값, 포털 방향·이동 상태 보존, 금속만 견인, valid1/unchanged0/chain0을 선언한다. 선언을 새 게임 판정으로 구현하거나 통과라고 표시하지 않았다.

| 장치 | 활성 논리 구현 | 현재 적용/검수 | 난이도 / 재개 우선순위 |
|---|---|---|---|
| 07/09/18/19 공통4종 | 표시 코드만 존재 | 테마 PNG 없음, Variant 추가 미실행 | 낮음~중간 / 입력 복구 후 P1 |
| 03 초승달 도약판 | 없음 | 3셀 궤적/비대칭 피벗 포함 전부 미실행 | 중간 / P0 |
| 10 광학군 | 없음 | 8반사 선언만 확인, Sprite/광선/레이어·센서 미실행 | 높음 / P0 |
| 14 자석 | 없음 | 금속/목재·경로·정지 및 시각 적용 미실행 | 높음 / P0 |
| 16 짝 포털 | 없음 | 방향/걷기·미끄러짐/불발 비용·재진입 검수 미실행 | 높음 / P0 |
| 21 유리벽 | 없음 | alpha/파편/빛·몸체 차단/파괴 검수 미실행 | 중간~높음 / P0 |

도약 중간 칸/착지 막힘/직접 입력, 거울 조작, 여러 상자 처리 등의 미정 분기는 코드에 없어 미정으로 남겼다. 추가 비용·압착·임의 입력·스태미나를 넣지 않았다.

## 보존 및 실제 검수 여부

기존 Assets/ProjectSettings/Packages의 SHA-256은 시작과 모두 동일하다. 기존 별길 목장·기억 극장·장치8 및 사용자 설정 변경을 보존했다. 이번 변경은 진단/복구 스크립트와 문서/보고서뿐이다. 입력 ZIP과 기존 사용자 변경은 커밋에서 제외한다.

- 수행: ZIP 로컬 레코드 크기/CRC 복구, 원본 SHA, 매니페스트 전수 누락 조사, 현재 프로젝트 읽기, 기존 파일 바이트 비교, 연결 Editor 상태/Console 확인.
- 미실행: 편지 천문대 Unity 임포트·Atlas·프리팹·QA/플레이 씬 연결·EditMode/PlayMode·모바일. 자산이 없어 테스트 대상을 만들 수 없다. 기존 테마의 과거 13/13·4/4 결과를 이번 팩 결과로 재사용하지 않는다.
- 기존 CommercialPolishPolicyTests/로비는 비활성 레거시이며 이번에도 실행하지 않았다.

## 남은 작업

1. 즉시 효과 큰 조치: 정상 전체 ZIP을 MCP_INBOX에 다시 제공한다. 727 런타임 PNG, unity_import_manifest.json 및 ADDED_DECISIONS_KO.txt가 포함돼야 한다.
2. 적은 비용 개선: CRC/필수 경로 확인 후 letter_observatory 테마로 공통 Variant/전용 Sprite/프레임/정확한 피벗을 임포트하고 QA 및 기존 표시 카탈로그에 연결한다.
3. 이후: 실제 게임 호출자를 확인/구현해 비용·턴·충돌 검수를 수행하고 실제 Atlas/모바일 렌더 검수로 확장한다.

변경 파일 목록은 [changed_files.txt](Docs/Validation/LetterObservatory/changed_files.txt)에 기록한다.
