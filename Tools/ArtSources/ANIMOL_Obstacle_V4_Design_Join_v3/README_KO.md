# ANIMOL 장애물 v4 · 노출 스프링과 임시 플레이 개정 v3

지형 artVersion=4를 유지하고 장애물 visualVersion=3으로 개정했다. 옆스프링의 천장과 상향 스프링의 땅속 몸체를 제거했으며, 시작점에서 방울 세 개를 모아 목적지로 가는 임시 플레이를 추가했다.

## 바로 확인

`ANIMOL_Obstacle_V4_Preview.html`을 브라우저에서 연다. 아트와 플레이 로직이 모두 들어 있는 단일 파일이다. 위쪽 화면에서 ←→/A D 이동, Space/↑/W 점프, ↓/S 하향 통과, R 재시작을 사용한다. 화면 버튼도 지원한다. ‘경로 재생’은 실제 입력으로 검증한 클리어 경로를 재생한다. 방울 세 개가 모이면 목적지가 열린다.

옆스프링은 투명한 위·아래 공간 사이에 접촉판·흰 코일·뒤 고정부만 보인다. 상향 스프링은 바닥 위의 높은 코일과 넓은 발사판이 보인다. 두 장치의 빈 코일 공간에는 전체 셀 충돌체를 만들지 않았다.
아래쪽 접합 확인 화면과 공통 45개 비교판도 사용할 수 있다. C05 이탈·낙하·페이드, 단색 떡, 컨베이어 애니메이션, 얼음 바닥, 표식 없는 하향 발판을 유지했다. C09는 삭제 상태이며 C04는 상시 위험칸이다.

## 포함 내용

- 공통 9종×5테마 45개 디자인, 32×32 기능 프레임 393개, 기능 아틀라스 576×900.
- 원본 v4 몸체 80개 아틀라스의 바이트 보존, 47 연결 마스크·20스타일·4위상 유지.
- C05 조각 20개와 Unity 기본 SpriteRenderer·Transform·alpha 낙하 연출.
- 임시 플레이어·시작 표시·방울·잠금/열림 목적지의 PNG 6개, 고정 QA 스테이지 JSON.
- 실제 실행한 JS 플레이어, Unity 임시 플레이어 코드 7개, Editor 씬 생성기.
- 지역 후보 M02/R01/R03의 이전 그래픽 8개 유지.

## Unity에 적용

`Docs/ANIMOL_OBSTACLE_V4_CODEX_APPLY.txt`를 프로젝트 MCP/코덱스에 전달한다. 실제 프로젝트를 읽고 스프링의 렌더 분류와 접촉판을 연결한 뒤, 제공된 생성기로 별도 QA 씬을 만들도록 작성했다. `Docs/TEMP_PLAYER_QA_KO.md`에 배치·조작·수치·코드 설치·씬 생성 호출이 있다.

여기에서는 ANIMOL Unity 저장소에 접근할 수 없어 C# 컴파일·실제 씬·Unity 물리 검증은 실행하지 않았다. HTML과 Unity 코드는 서로 다른 물리 솔버를 사용한다. 미리보기 성공을 실제 프로젝트 적용 완료로 보지 않는다.

## 주요 자료

| 자료 | 내용 |
|---|---|
| `Docs/ANIMOL_Common_9_Tile_Design_v3.md` | 공통 장치 정본, C02 SIDE·C03 TOP |
| `Docs/OBSTACLE_THEME_DESIGN_KO.md` | 테마별 디자인·픽셀 규칙 |
| `Docs/GRAPHIC_JOIN_CONTRACT_KO.md` | FULL/TOP/SIDE/AIR 연결·상판 대체 |
| `Docs/UNITY_ANIMATION_AND_CRUMBLE_KO.md` | 이벤트와 Unity 기본 붕괴 연출 |
| `Docs/TEMP_PLAYER_QA_KO.md` | 임시 플레이어 전체 흐름 |
| `Data/temporary_qa_stage.json` | 시작·방울 세 개·목적지·장치 배치 |
| `Data/temporary_qa_graphics.json` | 고정 QA 씬용 그림·프레임 경로 |
| `Art/Preview_45_Theme_Skins.png`, `Preview_45_Animated.gif` | 정지·움직임 비교판 |
| `Validation/player_level_map.png` | 실제 테스트 맵 그림 |
| `Validation/RESULT_KO.md` | 실행 결과와 검증 범위 |

## 재현

```bash
npm install
npm test
python -m pip install Pillow
python Tools/verify_pixel_art.py
node Tools/verify_native_preview.mjs
node Tools/build_animation_gif.mjs
```

GIF 재생성에는 FFmpeg가 필요하다. 아트·HTML·QA 그래픽 데이터를 다시 만들려면 원본 v4 패키지 경로를 지정한다.

```bash
node Tools/build_pack.mjs /absolute/path/ANIMOL_FreeShape_Sprites_joint_finish_v4
```

원본 v4 전체 ZIP은 중복 포함하지 않았다. PNG는 Sweetie16·정수 좌표·alpha 0/255를 사용한다. 낙하 페이드의 런타임 투명도는 요청한 연출이다. 자동 후보 검사는 모든 곡선의 Jaggies/Doubles를 완벽하게 판정하지 않으므로 원배율과 최근접 확대에서도 확인했다.
