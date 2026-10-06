# ANIMOL FreeShape Sprites clean_v2

ANIMOL의 기존 자유형 지형 편집기에 적용할 아트 버전 2 패키지다. 사용자가 승인한 아래 다섯 원화만 시각 기준으로 사용한다. 승인된 형태·색 배합·세로띠·클램프·상하 마감·모티프를 보존하면서 픽셀 마감, 연결 부품 추출과 조합을 수행한다. 점유·충돌을 결정하는 1×1 셀과 기존 47종 연결 규격을 유지한다.

| 테마 | 사용자가 승인한 원화 |
|---|---|
| T01 월궁 | `image(20261005-190222).png` |
| T02 구름고래 목장 | `image(20261005-190237-2).png` |
| T03 별가루 도서관 | `image(20261005-190303).png` |
| T04 시간유리 온실 | `image(20261005-190219).png` |
| T05 오로라 수정광산 | `image(20261005-190312).png` |

승인 원본은 `SourceArt/Approved/T01.png`부터 `T05.png`까지 바이트 그대로 보존하고 SHA256으로 식별한다. 업로드 이름과 파일 경로의 대응은 `Data/approved_design_sources.json`에 있다. 장식을 없앤 평면 프레임으로 바꾸거나 별도의 형태·색 변경 시안을 생성하지 않는다. 앞서 제시된 두 번째 참고 사진은 픽셀 마감 품질 참고이며, 실제 디자인은 위 승인 원화가 기준이다.

압축을 풀고 아래 두 화면을 연다. 승인 원본 이미지와 HTML의 상대 경로를 유지한다.

| 시작 화면 | 확인할 내용 |
|---|---|
| `APPROVED_DESIGNS.html` | 승인 원화 5장과 20스타일의 소재·외곽·내곽·문양 128×128px 패널 80개. 전체 디자인과 장식 보존 확인. |
| `DESIGN_GALLERY.html` | 실제 32px 연결 셀을 칠하거나 지워 지형을 조합하는 오프라인 미리보기. 구덩이·구멍·천장·위상·셀 이음 확인. |

승인 디자인 화면의 전체 패널과 자유형 화면의 셀 조합은 서로 다른 검토 대상이다. 완성 지형 예시는 `Samples`, 편집기와 Runtime 적용용 아트는 `Art`, 연결 정보는 `Data`에 있다.

| 항목 | 계약 |
|---|---|
| 아트 버전 | 2 (`Data/sprite_lookup.json.artVersion`) |
| 연결 계약 | `ANIMOL_FREE_SHAPE_BLOB47_V1` |
| 테마·스타일 | 5테마 × 4스타일 = 20 |
| 셀 | 32×32px, PPU 32, 1×1 논리 셀 |
| 연결 형태 | 주변 256상태를 정규화한 47종 |
| 질감 위상 | 스타일당 4종, 전역 좌표·지속 seed 기준 |
| 셀 Sprite | 3,760개 = 20 × 47 × 4 |
| 연결 부품 | 1,600개, 각 16×16px |
| 셀 아틀라스 | 80장 |
| 큰 문양 | 20장, 각 128×128px, 추가 점유 0 |
| 조합 예시 | 320개 = 20스타일 × 16모양 |

구덩이·둘러싸인 구멍·천장·얇은 지형·여러 청크의 지형은 점유 셀을 조합하여 만든다. 전체 맵에서 가능한 모든 모양을 각각 한 장씩 그린 패키지는 아니다. 다른 스타일 사이 또는 기존 v3 부품과 이어지는 전용 전환 아트는 포함하지 않는다.

적용용 `Art`와 `Samples`는 공용 Sweetie16의 불투명 색과 알파 0/255를 사용한다. 승인 생성 원본은 16색 제한이 없는 파일일 수 있다. 최종 출력의 팔레트 매핑은 승인된 색 배합을 유지하는 범위에서 수행하고 원본 파일은 변경하지 않는다. 색 제한과 픽셀 마감은 서로 다른 검사다.

[기존 에디터 아트 패치 적용 명령](Docs/ANIMOL_CLEAN_ART_V2_APPLY.txt)을 사용한다. [마감 기준](Docs/PIXEL_ART_FINISHING_RULES_KO.md), [한국어 안내](Docs/README_KO.md), [연결 규격](Docs/TOPOLOGY_SCHEMA.md)와 카탈로그를 함께 읽는다. 현재 에디터를 새로 구현하라는 초기 v1 적용 명령은 이번 패치의 실행 진입점이 아니다.

실제 ANIMOL Unity 프로젝트의 자유형 에디터는 이전 적용 보고서에 구현되어 있다. 이 clean_v2 제작 공간에서는 그 Unity 프로젝트에 새 아트를 설치하거나 맵을 버전 2로 전환하지 않았다. 기존 버전 1 맵은 그대로 유지하고 선택한 맵의 아트 버전만 명시적으로 전환해야 한다.

## 빌드·검증

실제 재빌드 스크립트와 명령은 `package.json`을 기준으로 실행한다. 패키지 폴더에서 다음을 실행한다.

```bash
npm install
npm run build
npm run gallery
npm run build:approved-gallery
npm run validate:topology
npm run validate:art
npm run validate:gallery
npm run validate:clean -- --baseline ../ANIMOL_FreeShape_Sprites_v1 --check-core --contact-sheets
```

마감 비교 검사는 원본 v1 폴더가 필요하다. 같은 상위 폴더에 v1 패키지를 풀거나 `--baseline`에 실제 v1 경로를 지정한다. 검사 결과는 `Docs/clean_pixel_validation.json`에 기록한다. `--check-core`와 `--contact-sheets`는 기존 검사 실행과 검토 시트 생성을 추가한다.

`Docs/art_build_stats.json`, `sprite_file_validation.json`, `gallery_validation.json`, `gallery_build.json`, `topology_validation.json`은 이번 clean_v2 빌드·검사에서 다시 생성한 기록이다. 최종 출력에서 `--check-core --contact-sheets` 검사를 실행했고 exit code 0, topology·sprite files·gallery 검사가 PASS다. 기존 v1 기록을 이번 결과로 복사하지 않았다.

`Docs/old_v3_preserved_sha256.json`은 과거 v3 원본의 보존 비교용 출처 기록이다. clean_v2의 픽셀 마감이나 Unity 검증을 뜻하지 않는다. 이전 최초 에디터 통합 명령의 파일명은 안내용으로만 남겼으며 새 실행 진입점은 `Docs/ANIMOL_CLEAN_ART_V2_APPLY.txt`다.

## 이번 출력의 검사 기록

| 검사 | 실제 실행 범위·결과 |
|---|---|
| 최종 PNG 규격·팔레트·알파·아틀라스 일치 | 셀 3,760개, 부품 1,600개, 아틀라스 80장, 패널 80개, 예시 320개. 28,446,720픽셀 검사 PASS. |
| Sampling 자료 | body/frame/ring PNG 60개, 513,280픽셀 검사 PASS. body-only mask255 출력 80개와 원본 body crop 일치 PASS. |
| 실제 셀 이음 후보 조사 | 163,840개 논리 문맥, 중복 제거한 실제 그림 쌍 27,040개, 공유 경계 865,280픽셀 조사. 후보는 의미 판정과 구분. |
| 확대 검토 자료 | 20스타일의 47종 × 4위상 contact sheet 20장 최신 출력. 최근접 4배이며 픽셀 수정 없음. |
| 승인 원본 보존 | Approved 5장과 Raw 5장의 SHA256 일치. 기존 v1 패키지 5,883파일 보존 확인. |

최종 셀 PNG 집합의 digest는 `e7cc6852e054845d1241a2d3ccbd4e49b82c9f5eb1520af213a5dea69a5f0d3e`이다. 기록은 `Docs/clean_pixel_validation.json`, 원본·디자인 보존과 실제 시각 검수 범위는 [승인 디자인 출력 QA](Docs/APPROVED_DESIGN_EXPORT_QA_KO.md)를 확인한다.

같은 검출 규칙의 고립 픽셀 후보는 v1의 41,215개에서 28,219개로 31.53% 감소했고, 면적 3픽셀 이하의 작은 덩어리 후보는 65,265개에서 44,155개로 32.35% 감소했다. body field는 20스타일 중 17스타일에서 고립 후보 0개이며 나머지에 총 24개가 있다. body-only 셀 출력 80개에는 총 4개 후보가 있다. T02_B/D는 승인 원화의 가로 판재·명암띠를 유지한 행별 팔레트 정리 후 body field와 각 mask255 위상에서 고립·작은 덩어리 후보가 0개다. 후보에는 승인된 장식·작은 기호의 끝점이 포함될 수 있어 모두 잡음으로 단정하지 않는다.

PNG·카탈로그·논리 연결 검사, Jaggies/Doubles 후보 검사, 확대 미관 검수, Unity 적용 검사와 모바일 실기기 검사는 각각 구분한다. 이 결과를 Jaggies/Doubles가 전부 없다는 결론으로 사용하지 않는다. 실제 Unity 적용·Scene/Runtime 렌더 비교와 모바일 실기기 검사는 이번 제작 공간에서 실행하지 않았다.

시각 검수 결과에는 실제로 확인한 원화·패널·셀 아틀라스·조합 지형의 파일과 배율을 적는다. 5장 원화나 일부 예시 확인을 셀 3,760개 전체의 미관 검수로 확대하지 않는다. `Docs/PIXEL_ART_FINISHING_RULES_KO.md`의 기준에 따라 셀·조합 지형·문양 끝의 픽셀과 승인 원화의 디자인 보존을 확인한다.
