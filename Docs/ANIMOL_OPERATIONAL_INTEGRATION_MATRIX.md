# ANIMOL 운영 연동 매트릭스

기준 커밋: `eb6b811`  
범위: 2차 작업의 직접 대상인 C6, M2, M6, R8을 제외한 41종. `연결됨`은 기존 운영 이벤트에 32px 아트와 phase 호출을 결합했다는 뜻이다.

| ID | 운영 오브젝트 | 운영 로직 | 연결할 이벤트 / 현재 연결 | 필요한 phase | 충돌체 소유자 |
|---|---|---|---|---|---|
| C1 | Side Spring | 있음·연결됨 | 접촉 발사→`active`, 이탈→`recover`, 리셋→`idle`; `warn`은 사전 예고 규칙 추가 시 | idle, warn, active, recover | 운영 루트 `BoxCollider2D` |
| C2 | Pounder | 있음·연결됨 | 감지→`warn`, 하강→`active`, 상승→`recover`, 리셋→`idle` | idle, warn, active, recover | 운영 루트 `Collider2D` + kinematic body |
| C3 | Rice Slow Zone | 있음·연결됨 | 진입→`active`, 마지막 이탈→`recover`, 리셋→`idle` | idle, active, recover | 운영 루트 trigger `Collider2D` |
| C4 | Half Block | 있음·정적 | 생성/리셋→`idle`; `active`는 변형 이벤트가 없어 미사용 | idle, active | 운영 루트 `BoxCollider2D` |
| C5 | Drop Platform | 있음·정적 | 생성/리셋→`idle`; 하향 통과 이벤트가 플레이어 측에 있어 별도 브리지 필요 | idle, active | 운영 루트 `BoxCollider2D` + `PlatformEffector2D` |
| M1 | Moon Lantern Step | 있음·연결됨 | 이탈→`warn`, 소멸→`vanish`, 복귀→`recover`, 고체화→`idle` | idle, warn, vanish, recover | 운영 루트 `BoxCollider2D` + `PlatformEffector2D` |
| M3 | Moon Rabbit Bowl | 있음·연결됨 | 착지 충전→`charge`, 이탈 보조→`active`, 보조 소비→`recover`, 리셋→`idle` | idle, charge, active, recover | 운영 루트 `BoxCollider2D` |
| M4 | Moon Jade Pendulum | 있음·연결됨 | 왕복 시작→`active`, 체크포인트 리셋→`idle` | idle, active | 운영 루트 `BoxCollider2D` + kinematic body |
| M5 | Moon Sliding Eave | 있음·연결됨 | 점유 확장→`active`, 지연 종료 복귀→`recover`, 리셋→`idle`; 별도 `warn` 규칙 없음 | idle, warn, active, recover | 운영 루트 `BoxCollider2D` + kinematic body |
| K1 | Cloud Whale Ferry | 있음·연결됨 | 탑승 이동→`travel`, 리셋→`idle`; 종점 연출은 전용 `vanish/respawn` 클립이 없어 이동·정차 규칙 유지 | idle, travel | 운영 루트 `BoxCollider2D` + kinematic body |
| K2 | Cloud Sheep Step | 없음 | 점유 해제 후 분산, 복귀 타이머 및 점유 중 보류 구현 필요 | idle, warn, disperse, recover | 구현 예정: 루트 발판 collider, 아트 collider 비활성 |
| K3 | Updraft Column | 없음 | 진입/체류 상승력, 이탈 종료 구현 필요 | idle, active | 구현 예정: 루트 trigger volume |
| K4 | Shepherd Bell | 없음 | 타격/공명 반경/쿨다운 구현 필요 | idle, ring, cooldown | 구현 예정: 루트 고체 collider + 감지 trigger |
| K5 | Cloud Balloon Tether | 있음·연결됨 | 점유 하강→`descend`, 비점유 상승→`ascend`, 리셋→`idle` | idle, ascend, descend | 운영 루트 `BoxCollider2D` + kinematic body |
| K6 | Cloud Windmill Blade | 없음 | 회전 구동, 탑승 운반, 끼임 안전 처리 구현 필요 | idle, spin | 구현 예정: 회전 루트/날개별 collider |
| K7 | Cloud Rainbow Slide | 없음 | 접촉 활주 가속 및 이탈 복원 구현 필요 | idle, active | 구현 예정: 루트 경사 collider |
| K8 | Cloud Rain Umbrella | 없음 | 강우 예고, 개방, 유지, 복귀 구현 필요 | idle, warn, open, recover | 구현 예정: 루트 발판 collider + 우산 trigger |
| K9 | Cloud Sail Step | 없음 | 탑승 이동, 종점 대기, 원점 복귀 구현 필요 | idle, warn, travel, return | 구현 예정: 루트 `BoxCollider2D` + kinematic body |
| L1 | Page Bridge | 없음 | 접근 예고, 펼침/개방, 폐쇄 구현 필요 | idle, warn, opening, open, closing | 구현 예정: 각 페이지 발판 collider |
| L2 | Bookmark Lift | 없음 | 탑승 상승, 상단 정차, 하강 구현 필요 | idle, ascend, top, descend | 구현 예정: 루트 `BoxCollider2D` + kinematic body |
| L3 | Ink Blot | 있음·연결됨 | 진입→`disturbed`, 마지막 이탈/리셋→`idle` | idle, disturbed | 운영 루트 trigger `Collider2D` |
| L4 | Index Drawer | 있음·연결됨 | 접근→`opening`, 이탈→`retract`, 리셋→`idle`; 종점 도달 시 `open/extend` 세분화 가능 | idle, opening, open, extend, retract | 운영 루트 `BoxCollider2D` + kinematic body |
| L5 | Letter Belt | 없음 | 접촉 중 컨베이어 속도 전달 구현 필요 | idle, run | 구현 예정: 루트 발판 collider |
| L6 | Popup Stair | 없음 | 예고 없이 펼침/개방/접힘 상태와 계단별 collider 동기화 구현 필요 | idle, unfold, open, fold | 구현 예정: 계단별 `BoxCollider2D` |
| L7 | Spine Brake | 없음 | 이동, 브레이크 진입, 정차, 재출발 구현 필요 | idle, travel, brake, stopped, resume | 구현 예정: 이동 루트 collider + kinematic body |
| L8 | Chapter Fork | 없음 | 좌우 선택, 선택 잠금, 리셋 구현 필요 | idle, choose_left, choose_right, left_locked, right_locked | 구현 예정: 좌우 선택 trigger + 루트 발판 collider |
| H1 | Dew Seed Step | 없음 | 씨앗→성장→만개→시듦 순환과 고체화 시점 구현 필요 | idle, seed, grow, full, wilt | 구현 예정: 성장 발판 collider |
| H2 | Glass Vine Lift | 없음 | 탑승 상승/하강과 승객 운반 구현 필요 | idle, ascending, descending | 구현 예정: 루트 `BoxCollider2D` + kinematic body |
| H3 | Sand Drip | 없음 | 낙하 예고, 낙하, 충돌 및 리셋 구현 필요 | idle, warn, fall, impact | 구현 예정: 낙하 body collider |
| H4 | Petal Cup | 없음 | 점유 압축, 점프 보조, 복귀 구현 필요 | idle, compress, jump_assist, recover | 구현 예정: 루트 발판 collider |
| H5 | Dew Glass | 없음 | 유동/용해/고체화 상태와 고체 collider 토글 구현 필요 | idle, flow, melt, solidify, solid | 구현 예정: 루트 collider(고체 상태만 활성) |
| H6 | Sundial Petal | 없음 | 예고, 좌우 기울기 선택, 복귀 및 점유 보류 구현 필요 | idle, warn, left_tilt, right_tilt, return | 구현 예정: 회전 발판 collider |
| H7 | Sand Retrace | 있음·연결됨 | 비점유 진행→`forward`, 점유 역행→`reverse`, 리셋→`idle` | idle, forward, reverse | 운영 루트 `BoxCollider2D` + kinematic body |
| H8 | Vine Knot | 없음 | 접촉 결속, 잠금, 해제 구현 필요 | idle, bind, locked, loose | 구현 예정: 루트 고체 collider + 결속 trigger |
| R1 | Resonance Tile | 없음 | 충전, 점등, 펄스, 감쇠/소등 상태 구현 필요 | idle, dormant, charge, lit, on, pulse, fade, off | 구현 예정: 루트 발판 collider + 공명 trigger |
| R2 | Prism Spring | 있음·연결됨 | 접촉 발사→`launch`, 이탈→`reload`, 리셋→`idle`; 압축 선행 상태는 접촉 프레임 분리 필요 | idle, compress, launch, reload | 운영 루트 `BoxCollider2D` |
| R3 | Crystal Stalactite | 없음 | 예고, 낙하, 충돌 및 재생성 구현 필요 | idle, warn, fall, impact | 구현 예정: 낙하 body collider |
| R4 | Magnet Pair | 있음·연결됨 | 점유 측→`selected_occupied`, 상대 이동→`partner_travel`, 원위치→`return`, 리셋→`idle` | idle, selected_occupied, partner_travel, return | 각 운영 루트 `BoxCollider2D` + kinematic body |
| R5 | Slick Facet | 없음 | 접촉 중 마찰/활주 변경 및 이탈 복원 구현 필요 | idle, glide | 구현 예정: 루트 경사 collider |
| R6 | Prism Beam | 없음 | 빔 예고/발사, 용해, 고체화 상태와 collider 토글 구현 필요 | idle, beam, dissolve, solidify, solid | 구현 예정: 빔 trigger + 고체 루트 collider |
| R7 | Crystal Lever | 없음 | 레버 시작/슬라이드/투척/래치 및 리셋 구현 필요 | idle, start, slide, throw, latched | 구현 예정: 레버 상호작용 trigger + 움직이는 파츠 collider |

## 적용 우선순위

1. 이번 2차에서 기존 운영 이벤트가 있던 16종의 아트·phase를 먼저 연결했다.
2. C4/C5처럼 물리는 있으나 동적 이벤트가 없는 항목은 `idle`만 사용하고, 이벤트 계약이 생긴 뒤 `active`를 연결한다.
3. 운영 로직이 없는 25종은 위 표의 상태 전이와 충돌체 소유권을 먼저 구현한 뒤 phase를 연결한다. 생성 아트 프리팹 내부 collider는 운영 중 비활성화하고, 실제 물리 파츠가 collider를 소유한다.
