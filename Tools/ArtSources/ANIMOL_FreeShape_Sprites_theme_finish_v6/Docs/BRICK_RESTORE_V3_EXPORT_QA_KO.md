# ANIMOL brick_restore_v3 — 출력 및 검수 기록

이번 변경은 첨부 화면에서 식별한 **T01_A / 월궁 / 포개 옥판층** 한 스타일이다. 서로 다른 크기의 옥 벽돌, 어긋난 줄눈, 예전의 작은 끝캡과 하부 받침 비례를 복원했다. material/frame/ring/motif 네 패널을 새 제작 원화에서 추출했다. 나머지 19스타일은 clean_v2의 픽셀을 그대로 유지했다.

## 아트와 출처

- 제작 수정: 내장 imagegen의 원형 보존 편집. 정확한 프롬프트와 참고 자료는 `SourceArt/BRICK_RESTORE_GENERATION.json` 및 `SourceArt/RestorationReferences/`.
- 수정 원화: `SourceArt/Revised/T01_A_BrickEnd_Restore.png` (2172×724).
- 수정 원화 SHA256: `6b7a6a251c4ab7b40fb05efd8adc7ba5782b480055a7680005c323ae9a279a45`.
- 원화는 생성 참고 자료다. Runtime에는 `Data/sprite_lookup.json`에 연결된 최종 아틀라스와 문양을 사용한다.
- 기존 공용 Sweetie16 중 T01_A에는 `#1a1c2c #257179 #f4f4f4 #ef7d57 #5d275d`의 5색만 사용했다. 산발적 밝은 청록/노랑 명암과 희미한 나뭇결을 넓은 면색으로 변환했다.
- 크림/노랑의 작은 명암은 흰 면으로 정돈됐다. v1 픽셀을 그대로 복사한 결과가 아니다.

## 끝 형태와 추출 비례

T01_A만 frame/ring을 128px에서 추출한다. v2의 64px frame 코너를 전체 16×16으로 쓰면서 커졌던 보라색 기둥 비례를 되돌렸다. OUTER에는 이전 방식의 8px 외곽 합집합을 사용해 각 quarter 안쪽 8×8은 옥판 몸체로 남겼다. INNER는 완성된 원화의 16×16 모서리 조각을 유지한다.

최종 frame 등록은 `x4/y4/w120/h118`이다. 중복된 바깥 윤곽 행·열을 제외하고, 추출한 맨 바깥 1px에는 N/S/W/E별 ink 역할 팔레트를 적용한다. 이 설정은 `Data/source_rects.json`과 lookup의 `registeredContourPalettes`에 기록했다. 원화와 128px 참고 패널의 RGB는 보존하고 source crop과 팔레트 변환으로 실제 셀을 내보낸다. 다른 19스타일에는 이 설정을 적용하지 않는다.

1×1 샘플의 네 가장자리 각각 32px, 16×16 사각형의 네 가장자리 각각 512px가 ink색으로 연결된다. 확대 검수에서 지적한 캡 위 보라색 점과 짧은 조각은 제거됐다. 단일 셀 중앙의 옥판은 v1의 단일 둥근 돌을 그대로 복제한 모양이 아니라 새 벽돌 줄눈이 지나가는 형태다.

## 유지한 규격과 실제 검사

| 검사 | 이번 최종 결과 |
|---|---|
| 연결 계약 / 저장 schema / artVersion | ANIMOL_FREE_SHAPE_BLOB47_V1 / 1 / 3 |
| 스타일 / 셀 규격 | 20 / 32×32px, PPU32, 1×1 점유 |
| 셀 Sprite / 아틀라스 / 문양 | 3,760 / 80 / 20 |
| 연결·팔레트·알파·Sprite·atlas·gallery | PASS |
| source crop 재현 | material/frame/ring/motif 4패널 정확 일치 |
| 나머지 19스타일 보존 | 5,605 PNG 파일 목록·바이트 정확 일치 |
| 몸체 줄눈 생존 | native64 core의 긴 가로줄 9개 / 세로줄 13개 |
| Sampling / body crop | 60 PNG, 536,768px / 80개 정확 일치 |
| T01_A 노출 외곽 | 188 Sprite의 208면 × 32px = 6,656px 모두 ink |
| 단일 셀 몸체 보존 | mask000 네 위상의 중앙 16×16이 mask255 몸체와 1,024px 정확 일치 |

몸체의 다섯 색은 모두 면적 1% 이상이며 청록색 최대 비율은 67.63%다. 단색 면으로 벽돌 무늬가 사라지는 회귀를 검사했다. 크기·줄눈 수 검사는 미관이나 모든 지형의 완성도를 자동 승인하는 검사가 아니다.

최종 3,760 셀 PNG digest: `cf7d94f22348a17fbe817f28deb3fd433befb34f5eae37258f8b312f761125db`.

전체 고립 1px 후보는 27,466개, 면적 3px 이하 후보는 42,798개다. v1 대비 각각 33.36%와 34.42% 감소했다. T01_A의 188개 셀에서는 고립 후보 371개, 작은 군집 후보 579개이며 v1 대비 각각 71.68%와 75.51% 감소했다. 후보에는 벽돌의 작은 끝점과 구조 장식도 포함되므로 전부 잡음으로 판단하지 않는다.

T01_A 외곽 중앙의 안쪽 연속 ink 깊이 검사에서는 3,328개 중 2,884개가 정확히 1px, 가장자리 ink 누락은 0개다. 444개는 더 깊은 ink 후보로 분류된다. 구조 안쪽의 ink와 외곽 첫 행의 ink를 구분해야 한다. **모든 곡선·줄눈·장식의 jaggies/doubles가 0개라는 판정이나 전체 윤곽 두께의 완전 통일을 주장하지 않는다.**

기존 v1 manifest의 5,883개, clean_v2 manifest의 5,975개 파일도 크기와 SHA256이 원본과 같음을 별도로 확인했다.

## 실제 시각 검수 범위

T01_A의 16개 지형 예시를 원배율로 확인했다. 단일 셀은 nearest 8배, 사각형·최대 지형·양방향/비대칭 계단·얇은 가로/세로 지형·U자 구덩이·구멍·overhang·T분기·대각 분리·다중 덩어리·음수 청크 예시는 nearest 확대에서 검토했다. 128px 소재/외곽/내곽/문양 패널과 47종×4위상의 실제 셀 contact sheet도 확인했다.

벽돌 줄눈, 작은 끝캡, 하부 받침이 유지된다. 빈 공간을 가리는 아트 연결, 분리된 덩어리의 가짜 연결, 분기 경계 잘림, 청크 경계의 가짜 벽은 이 예시에서 발견하지 못했다. 모든 가능한 맵 조합을 시각 검수했다고 확대하지 않는다.

`Previews/T01_A_v1_v2_v3_comparison.png`는 v1/v2/v3 실제 PNG로 조합한 비교판이다. 그림을 최근접 정수 배율로 배치했으며 Unity 화면 캡처가 아니다.

## 적용 상태

실제 ANIMOL Unity 프로젝트는 이 작업 공간에 없으므로 Unity 임포트·Scene/Runtime 렌더·버전 전환·Undo·저장·착지·모바일 검사를 실행하지 않았다. `Docs/ANIMOL_BRICK_RESTORE_V3_APPLY.txt`에 v1/v2/v3 공존, 선택 맵의 명시적 전환과 실제 Unity 검증 명령을 제공한다.

검사 세부 파일: `topology_validation.json`, `sprite_file_validation.json`, `gallery_validation.json`, `clean_pixel_validation.json`, `brick_restore_validation.json`.
