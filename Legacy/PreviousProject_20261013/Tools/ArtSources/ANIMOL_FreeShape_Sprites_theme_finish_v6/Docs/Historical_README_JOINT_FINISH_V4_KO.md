# ANIMOL 자유형 지형 아트 joint_finish_v4

T01_A의 벽돌 접합부를 수정했다. 기존 흰 윗면 하이라이트와 산호/보라 아랫면 명암을 유지하고, 돌 모서리는 동일한 1px 곡선 계단으로 둥글고 뭉툭하게 정리했다. 직선 줄눈은 1px이며 의도한 곡선 코너는 별도 제작 규칙으로 검사한다.

material의 64px 반복 원본을 모든 분면과 셀에서 전역 좌표로 이어 쓴다. 프레임에서는 장식과 윤곽만 덮어써 몸체 줄눈이 다른 무늬로 바뀌지 않게 했다. 흰 옥판·보라 끝기둥·산호 받침의 구조 역할을 유지하면서 native 곡선/접합 좌표를 정돈했다. 다른19스타일 PNG는 v3와 바이트가 같다.

artVersion=4, schemaVersion=1, contractId=ANIMOL_FREE_SHAPE_BLOB47_V1.
32px/PPU32,47마스크×4위상×20스타일,Sprite3760개,아틀라스80장,문양20개.
문양은4×4셀 시각 크기이며 추가 점유·Collider0.

- 적용 명령: Docs/ANIMOL_JOINT_FINISH_V4_APPLY.txt
- 실제 검사: Docs/JOINT_FINISH_V4_EXPORT_QA_KO.md
- native 제작 규칙: Data/joint_native_layout_v4.json
- native 원본: SourceArt/Revised/T01_A_BrickRepeat64.png, T01_A_Frame_Joint128.png, T01_A_Ring_Joint128.png
- 실제 PNG 비교: Previews/T01_A_joint_finish_v4_comparison.png
- 실제 셀/패널 미리보기: DESIGN_GALLERY.html / APPROVED_DESIGNS.html
- Runtime 참조: Data/sprite_lookup.json
- 무결성: PACKAGE_MANIFEST.json

생성 이미지 T01_A_Joint_Rectangle_Reference.png는 작업 중 사각 구조 참고 이미지다. 최종 아트는 최신 명암·둥근 코너 요청을 반영한 native PNG와 아틀라스다. v2/v3 문서·비교판·원화는 과거 이력이며 v4 Runtime을 대체하지 않는다. 생성 프롬프트와 native 등록 기록은 SourceArt에 보존했다.

검사: npm run validate:joint, validate:topology, validate:art, validate:gallery.
재생성: npm run build:native 후 npm run build; npm run gallery 및 build:approved-gallery.
Unity 프로젝트가 이 공간에 없어 컴파일·Scene·Runtime·물리 검사는 미실행이다.

