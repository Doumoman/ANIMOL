# ANIMOL 자유형 지형 아트 theme_finish_v6

T01_A는 내부 구멍 마감을 수정한 v5의 실제 PNG를 유지한다. 다른 19스타일은 다섯 테마의 승인된 디자인을 기준으로 재료 면과 반복 이음, 외곽·내곽 연결을 다시 제작했다. 월궁의 옥판·토기·처마, 구름고래 목장의 구름 패드·목재·짐함, 별가루 도서관의 종이 묶음·접힘·책등, 시간유리 온실의 유리 챔버·모래관·덩굴, 오로라 수정광산의 큰 암판·수정맥·쐐기 면을 사용한다. 같은 벽돌 무늬를 전 테마에 적용하지 않는다.

넓은 면과 일정한 상단/하단 명암으로 디테일을 만들고 무작위 점, 노이즈, 불규칙한 작은 명암 조각을 넣지 않는다. 몸체와 경계 장식은 별도 native 영역으로 등록한다. 몸체의 같은 색이 구멍 받침이나 외곽 장식으로 잘못 추출되어 튀어나오지 않도록 한다. 발판은 수평인 2D 측면 상면을 유지한다.

artVersion=6 / schemaVersion=1 / contractId=ANIMOL_FREE_SHAPE_BLOB47_V1.
32×32px / PPU32 / 셀 점유 1×1. 20스타일 × 47마스크 × 4전역 위상 = 셀 Sprite 3,760개, 아틀라스 80장, 분면 1,600개.
기존 문양 20개는 시각 크기 4×4셀이며 추가 점유와 Collider는 없다.

- Unity 적용 명령: Docs/ANIMOL_THEME_FINISH_V6_APPLY.txt
- 현재 출력 검사: Docs/THEME_FINISH_V6_EXPORT_QA_KO.md / Docs/theme_finish_validation.json
- 20스타일 실제 출력: Previews/ANIMOL_theme_finish_v6_overview.png
- 지역별 v5/v6 실제 비교: Previews/T01_theme_finish_v6_comparison.png ~ T05_theme_finish_v6_comparison.png
- 실제 셀 편집 프리뷰: DESIGN_GALLERY.html
- 재료·외곽·내곽·기준 원화: APPROVED_DESIGNS.html
- Runtime 참조: Data/sprite_lookup.json
- 19스타일 native 규칙: Data/native_v6/T01.json ~ T05.json
- 실제 출처: SourceArt/THEME_FINISH_NATIVE_REGISTRATION_V6.json
- 유지한 T01_A 규칙: Data/hole_native_layout_v5.json
- 보존한 v5 해시 기준: Data/v5_preservation_manifest.json
- 전체 파일·크기·SHA256: PACKAGE_MANIFEST.json

비교판의 지형은 실제 아틀라스의 32px rect를 canonical mask와 전역 위상으로 선택한 결과다. 가운데 1×1 구멍, 2×2 구멍, 계단, 열린 구덩이와 재료 반복 면을 함께 보여준다. 전체 비교판에는 20스타일의 3×3 고체와 가운데 1×1 구멍을 정수 최근접 2×로 표시한다. 문양·격자·손으로 덧그린 수정은 넣지 않는다. 재료 swatch는 실제 material 패널의 x32/y32/w64/h64 영역이다.

현재 아트 재생성: npm run build:native, npm run build, npm run gallery, npm run build:approved-gallery. 이 경로는 패키지 안의 native 프로필·역할 원본·보존한 문양과 T01_A v5 소스를 사용하며 별도의 v5 폴더가 필요하지 않다.
현재 검사: npm run validate:theme, npm run validate:art, npm run validate:topology, npm run validate:gallery.
v6 검사는 별도 v5 폴더가 있으면 실제 원본과 비교하고 없으면 동봉한 v5 PNG/RGBA SHA256과 T01_A 전체 메타데이터를 사용한다. node Tools/validate_theme_finish_v6.mjs --packaged-baseline으로 동봉한 기준 경로를 명시적으로 검사할 수 있다.
현재 v5/v6 비교판 PNG 6개는 패키지에 완성본으로 포함한다. 과거 v5 그림까지 다시 조립하려는 경우에만 npm run build:comparison과 별도 v5 폴더가 필요하다. 다른 v5 경로는 node Tools/build_theme_finish_comparison_v6.mjs --baseline-v5 /absolute/path/to/v5로 지정한다.

SourceArt/DesignReferencesV6의 5개 이미지는 각 테마의 재료 디테일을 다듬은 원화 참고 자료다. Runtime Sprite나 정확한 16색/32px 출력의 검사 근거로 사용하지 않는다. 최종 19스타일 제작 원본은 SourceArt/NativeV6이며 실제 32px 셀과 아틀라스는 Art에 있다.

v5 이하 명령·검사·원화·비교판은 과거 제작 이력으로 보존한다. 과거 PASS를 v6 검사 결과로 사용하지 않는다. 기존 레지스트리와 사용자 맵을 보존하며 v6는 별도 설치한다. 서로 다른 styleId의 재료를 한 덩어리 안에서 자연스럽게 섞는 접합 아트는 이번 범위에 포함하지 않는다.

Unity 프로젝트가 이 공간에 없어 Unity 컴파일·Scene/Runtime·Undo·저장·물리 검사는 미실행이다. 적용 명령과 실제 아트를 제공하며 대상 프로젝트의 실행 결과는 별도 PATCH_RESULT에 기록해야 한다. 등록된 면과 경계의 검사 PASS를 모든 가능한 맵의 미관 자동 승인으로 확대하지 않는다.
