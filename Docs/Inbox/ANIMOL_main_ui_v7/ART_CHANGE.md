# ANIMOL 픽셀 마감 개선 v7

사용자의 image(4).png를 색면·픽셀 경계·장식 밀도 기준으로 삼았습니다.
테마 분위기와 Sweetie16 팔레트는 유지하고, 5개 테마의 원경·중경·고정 건물·근경을 새로 제작했습니다.
넓은 색면, 소수의 밝은 포인트와 읽기 쉬운 구조를 중심으로 마감했습니다.
근경 장식은 위쪽 모서리에 배치해 토끼가 달리는 높이를 비웠습니다.
구름고래 목장의 하늘과 온실의 중경은 최종 합성 후 별도로 정리했습니다.

## 캐릭터 잘림 수정

기존에는 4×2 그림을 균등 분할해 실제 그림의 귀·발·망토를 자르는 문제가 있었습니다.
새 그림은 8개 토끼 실루엣을 각각 완전히 분리한 다음 공통 비율로 64×96 셀 안에 배치합니다.
최종 셀의 좌우·상단에는 4px 이상 여백이 있으며, 원본 캔버스 가장자리에 닿는 실루엣은 없습니다.
세잎클로버 핀, 크림 튜닉·초록 조끼·청록 망토, 얼굴과 털의 정체성은 유지합니다.
꽃·왕관·뿔·귀 보석·드러난 금속 바·줄기는 추가하지 않았습니다.
이제 원본 그림을 임의의 4×2 격자로 다시 잘라서는 안 됩니다.

## 실행 규격

네이티브 화면352×704, 좌우 시트512×96, 셀64×96,8프레임14fps입니다.
일반 발행90, 공중3·7번발행86, 발판Y526, 셀Y436을 유지했습니다.
시트의 사방 여백은 자산의 일부이므로 Unity에서 tight trim하지 않습니다.
화면 밖에서 진입·퇴장하는 자연스러운 화면 클리핑과 원본 시트 잘림은 구분합니다.

## 제작 지시 요약

Built-in imagegen으로 캐릭터와 배경을 제작·편집했습니다.
Use the user's clean landscape pixel art only as finishing reference. Preserve the five theme identities and the latest fantasy rabbit. Use coherent flat color areas, crisp stepped edges and sparse accents. Keep complete ears, paws, cape and tail. Separate complete silhouettes before sprite export. Keep foreground ornaments above the runner. Use only Sweetie16, no extra colors, noisy stipple, gradients or blurred pixel edges.

## 검증 범위

asset-verification.json은 크기·색·alpha·완전한 캐릭터·사방 여백 검사입니다.
render-verification.json은 실제 기준JS를 네이티브 Canvas에서 실행한 검사입니다.
occlusion-verification.json은 양방향·모든 프레임·여러 시점의 근경 가림 검사입니다.
이들은 Unity 프로젝트 컴파일·실기기·실제 Game View 검증이 아닙니다.
