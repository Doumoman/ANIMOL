별너울 해안 — 각진 신규 가시/통/폭탄/금 간 벽 V01

80개 투명 runtime PNG / 31개 clip / clip12fps / PPU256.
320x320 canvas, logicalRect[32,32,256,256], pivot[0.5,0.5]. 모든1x1은 원화 표시 슬롯이며 실제 게임 점유/충돌을 새로 등록하지 않습니다.

05 이탈 가시: leave_spike.none.flat / occupied_armed / departure_rise / raised.
밟고 있는 동안 작은 팁만 표시하고 실제 플레이어 이탈 이벤트에서만 상승합니다. 올라온 칸 재진입은 불가합니다. 낮아지는 조건은 원문 미정이므로 자동 타이머/턴 주기/역재생 reset clip을 제공하지 않습니다. 실제 host reset/undo가 authoritative합니다.

12 구르는 통: rolling_barrel.{east,north,west,south}.idle / roll / settle.
독립 native4방향×4roll pose입니다. 수평 긴 축 통은 N/S, 수직 긴 축 통은 E/W로 정확히 재매핑했습니다. billboard 전체 회전은0도입니다. 고정 raster bottom anchor[160,272]. 실제 host가 확정한 cell path를 따라 world transform으로 이동하며 애니메이션 callback으로 논리 이동하지 않습니다. 플레이어는 통의 원래 칸까지만 갑니다. 첫 통 목적지가 막혔으면 밀기 전체 무효/비용0입니다. fx/barrel_stop_prediction.png와 barrel_direction_fx는 host가 공급한 실제 경로/정지 칸에만 사용합니다.

15 3턴 폭탄: three_turn_bomb.none.inert / ignite / count_3 / count_2 / count_1 / detonate / spent.
첫 성공한 실제 밀기 이동 직후3. 그 밀기를 동시에 차감하여2로 표시하면 안 됩니다. 이후 자기 플레이어의 유효 행동이 하나씩 승인될 때2,1,세 번째 후속 행동 후 폭발입니다. 잘못된 입력/이동 없는 밀기/다른 플레이어/강제 이동 substep/시간/도화선 재생은 카운트를 바꾸지 않습니다. 숫자3/2/1은 신규 native 큰 block glyph이며 본체 고정, 도화선 spark만 native phase로 변합니다.
폭발 반경/모양/피해/연쇄 반응은 SOURCE_UNRESOLVED. 기본 cross/radius/damage 규칙은 없습니다. fx/host_blast_cell.png는 host가 제공한 authoritative blast-cell list에만 배치하는 시각 표시입니다. 폭발 원화의 픽셀 크기는 실제 피해 범위가 아닙니다.

금 간 벽: cracked_wall.none.intact / crack_feedback / break / open.
crack_feedback은 파괴 연출이며 두 번 공격해야 깨지는 HP 규칙을 만들지 않습니다. open endpoint 중앙[115,115,105,105] alpha0 검증, 모서리 낮은 rubble만 남습니다. 통행/점유 해제는 기존 host가 결정합니다.

제작과 검수
새로운 해안 native 아틀라스5개(spike/socket,spike/blades,barrel,bomb,wall)와 common의 새4방향 glyph source를 사용했습니다. Sanctuary bitmap은 사용하지 않았습니다. sources/*_draft_native.png는 최초 생성 원화 기록이며 최종 runtime은 flat-cel native edit 결과를 사용합니다. 원화 RGB를 코드로 칠하거나 필터링하지 않았습니다. 원화 crop, 저알파 배경/작은 고립 잔재의 alpha분리, 동일비율 resize, 고정점 register, layer composite만 수행했습니다.
통/폭탄은 큰6~8각 장변, 사각 bolt, 긴 쐐기 color plane, 직선 rope. 가시는 큰 chamfer목재/모래 받침과 날카로운 coral 삼각 blade. 벽은 sand/coral/slate의 넓고 평평한 면, 열리면 중앙이 투명합니다.

검증 상태: PASS_OFFLINE — 파일 decode/hash, native source closure,4방향 roll phase, ground/body 고정, alias exact bytes, wall open alpha0, glyph24px 방향 step, atlas이웃행잔재 제거, PNG/GIF full decode.
실제 Unity project가 제공되지 않았으므로 실제 import/C#compile/shader/NUnit/Prefab/registry/PlayMode/legacy0 scan은 모두 NOT_RUN입니다.

검수 이미지: inspection/hazards_contact_sheet.png
동작 검수: inspection/hazards_motion_review.gif (5760ms editor replay; 게임 timer가 아님)
조립 재현: python production/build_hazards.py
