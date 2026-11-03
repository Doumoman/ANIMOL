ANIMOL 낙일 성소 연결 장식 V01 — 다칸 장식 A

새 built-in image_gen 원화로 만든 다칸 장식 6종이다. 다른 테마의 색상 치환이나 픽셀 아트 필터를 사용하지 않았다.
성소 제단 3×2, 기록관 작업대 3×1, 무너진 주랑 3×1, 기도석 긴 의자 3×1, 천체 도표 탁자 2×2, 성유물 진열함 2×1.

모든 다칸 장식은 배치 하나당 SpriteRenderer 하나로 전체 원화를 표시한다. PPU256이며 logicalRect는 이미지 좌상단 기준이다. Unity pivot은 logicalRect 중심을 이미지 좌하단 정규화 좌표로 변환했다. 일부 위쪽 여백은 기둥·깃펜 등 높이 표현용이며 점유 셀이나 Collider가 아니다. PNG 전체를 한 칸에 맞춰 축소하지 않는다.

2칸 깊이 제단과 도표 탁자는 두 깊이 행을 사용하는 새 원화이며, 논리 지면 높이의 85% 이상과 행별 불투명 밀도 35% 이상을 검사했다. 나머지 장식도 가로 논리 폭 90% 이상을 채운다. Inspection의 셀 경계선과 배경은 오프라인 검수도에만 있다.

최종 원화 6개와 선택하지 않은 제단 초안 1개를 sources에 보존했다. 초안은 runtimeSelectable=false이며 어느 런타임 PNG에도 사용하지 않는다. 실제 사용한 생성 프롬프트는 Authoring/generation_prompts.json, 제단의 최종 프롬프트는 Authoring/altar_revision_prompt.txt에도 있다.
Authoring/build_groups_a.py는 원화의 RGB를 다시 그리지 않고 alpha 외곽의 미약한 halo와 speck만 정리한 뒤 균일 확대/축소, 투명 캔버스 등록을 한다. 정리 기준과 crop/scale/paste 좌표는 asset.registration에 기록했다. 비균일 스트레치나 외곽 RGB 재칠은 없다.

굵은 남색 윤곽, 큰 직선·절단면·쐐기, 상아색 석재·황동·구릿빛·짙은 청색 면으로 기존 낙일 성소 V02와 맞췄다. 기능성 광학 장치나 입력·충돌·턴·스토리 플래그를 추가하지 않는다. 표시 이름 천구 날조국은 실제 게임의 기존 스토리 완료 플래그가 결정한다.

파일·alpha·pivot·등록·원화 출처 검수 PASS_OFFLINE. Unity 원본 프로젝트가 없으므로 Import, C# 컴파일, TestRunner, 실제 Prefab 연결과 Play Mode는 모두 NOT_RUN이다.
