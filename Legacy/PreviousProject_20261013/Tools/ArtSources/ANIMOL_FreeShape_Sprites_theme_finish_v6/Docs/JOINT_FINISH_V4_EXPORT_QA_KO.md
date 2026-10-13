# ANIMOL joint_finish_v4 — 실제 출력·검수 결과

T01_A의 접합부 수정본이다. 명암과 둥글고 뭉툭한 돌 모서리를 유지한다. 청록 돌 윗면의 흰 1px 하이라이트, 흰 면 아래 산호색 명암, 산호색 면 아래 보라색 명암을 원래 색 역할로 살렸다. 모서리는 대칭적인 1px 코너 계단이고 직선 줄눈은 1px이다. 기존 흰 옥판·보라 끝기둥·산호 받침의 형태 역할을 native 좌표로 정리했다. 다른19스타일의 아트는 수정하지 않았다.

artVersion4 / schemaVersion1 / ANIMOL_FREE_SHAPE_BLOB47_V1.
32px, PPU32, 47mask×4위상×20스타일, Sprite3760개, 아틀라스80장, 문양20개, 예시320개.

| 실제 검사 | 최종 결과 |
|---|---|
| 접합 전용 검사 | Tools/validate_joint_finish_v4.mjs PASS, 실패0 |
| native64 등록 규칙 | 4,096px 불일치0; 직선4줄, course별 vertical port 정상 |
| 명암 유지 | 몸체 하이라이트168px·하단 명암31px, 불일치0 |
| 외곽·내곽 돌 면 | 4,112px 비교, 불일치0; 하단 명암496px·상단 하이라이트20px·둥근 코너136px |
| 64px 반복 경계·128px 반복장 | 반복장16,384px 비교, 불일치0 |
| 분면/셀 전역 몸체 | 80분면,188셀의 장식 외 몸체137,220px 비교, 불일치0 |
| 2×2 잉크 검사 | 의도한 둥근 코너48개; 미등록0, 누락0 |
| 실제 변경 타일 | T01_A 47mask×4위상=188개 |
| 다른19스타일 보존 | PNG5,605개/2,717,143bytes를 v3와 비교, 전체 동일 |
| Sprite/아틀라스/예시 | validate_sprite_files.mjs PASS; 28,446,720px 검사 |
| 연결 규격 | validate_topology.mjs PASS; 65,536개4×4 점유, raw256상태, 셀524,288맥락·공유경계393,216 |
| 오프라인 갤러리 | validate_gallery.mjs PASS; 셀15,140검사·문양60검사·위상216검사 |
| v3 원본 보존 | Docs/v3_baseline_preservation.json: 원본 manifest 전체 크기·SHA256 검사 PASS |
| 현재 패키지 무결성 | PACKAGE_MANIFEST.json에 실제 파일별 크기·SHA256, ZIP CRC 검사 PASS |

색은 기존 T01_A의5색(#1a1c2c,#257179,#f4f4f4,#ef7d57,#5d275d)이고 alpha0/255다.
registered rounded2×2는 둥근 모서리 제작 규칙에서 생기는 형태다. 수평/수직 직선 줄눈의 겹침과 구분했다. 전체 그림의2×2가0이라고 보고하지 않는다.

| 실제 시각 확인 | 증거 |
|---|---|
| 소재 및 1×1,5×3,계단·내부 구멍 | Previews/T01_A_joint_finish_v4_comparison.png: 실제 PNG를 정수 최근접4×/2× 확대 |
| 네 위상·47mask 전체 | QA/ContactSheets/T01_A_all47_all4phases_4x.png: 최종 아틀라스로 재생성 |
| 얇은 가로/세로,비대칭 계단,T분기,내부 구멍 | 실제 Samples/T01_A 원본1×/최근접8× 검토; 이전 프레임 배경에 의한 줄눈 어긋남이 검토 범위에서 보이지 않음 |
| 최종 하이라이트·둥근 코너 | native64를6× 확대하고 최종 비교판2×/4×에서 확인 |

일반 clean_pixel_validation.json의 자동 고립/작은 클러스터 후보는0이 아니다. T01_A는 고립 후보252/작은 클러스터 후보395이며 명암·작은 장식도 후보에 포함된다. 이 후보 수를 Jaggies/Doubles 전체 제거 판정으로 사용하지 않았다. 접합 규칙 검사는 등록된 native geometry와 실제 전역 몸체 일치 검사다. 모든 모티프·장식 곡선·가능한 맵의 미관을 자동 승인한 검사가 아니다.

최종 Runtime 소스는 Data/sprite_lookup.json의 아틀라스/문양이다. 실제 제작 규칙은 Data/joint_native_layout_v4.json, native 재생성은 Tools/build_joint_native_sources_v4.mjs다. SourceArt의 사각형 생성 이미지는 작업 중 참고 기록이며 최종 둥근 명암 출력이 아니다. 새 원본·v2/v3 원화와 문서는 과거 버전 이력으로 보존했다. v3 baseline이 없는 외부 환경은 보존 비교를 미확인으로 기록한다.

**Unity 프로젝트가 이 공간에 없어 Unity 컴파일·Scene/Runtime·Undo·저장·물리 검사는 미실행이다.** 실제 적용은 Docs/ANIMOL_JOINT_FINISH_V4_APPLY.txt를 따른다. v1/v2/v3 레지스트리·GUID·사용자 맵을 보존하고 별도 V4 레지스트리를 설치한다. 선택 개발 맵의 아트 버전만 기존 트랜잭션으로 바꾸고 점유·seed·객체·물리·문양 추가 점유/Collider0을 보존한다. 실제 Unity1×/8×·저장·Undo·Runtime 결과는 적용 프로젝트의 PATCH_RESULT에 별도로 기록한다.

