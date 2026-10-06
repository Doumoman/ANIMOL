# ANIMOL 장애물 v4 · 그래픽 개정 v2

2026-10-07 (한국 시간). 사용자 수정 지시를 반영했다. 지형의 artVersion=4는 유지하고 장애물 visualVersion만 2로 올렸다.

## 결과

- 공통 9종 × 5테마 = 45개 디자인. C09 접근 생성판은 삭제했다. C04는 상시 위험칸이다.
- 32×32 기능 PNG 393개와 576×900 아틀라스. 좌우·상향 스프링 7프레임, 떡 눌림·복원 7프레임, 양방향 컨베이어 8프레임을 포함한다.
- 하향 발판은 팻말·화살표 없는 v4 얇은 상판이다. 소멸판은 4조각의 갈라진 지형, 반동 발판은 내부 단색 떡, 저마찰은 얼음이다.
- C05용 조각 Sprite 20개와 Unity 기본 컴포넌트로 낙하·페이드하는 `Runtime/UnityCrumbleVfx.cs`를 제공한다. PNG에 페이드 애니메이션을 굽지 않았다.
- 원본 v4 몸체 20스타일 × 4위상의 아틀라스 80장을 보존했다. 47 연결 마스크와 전역 무늬 위상을 유지한다.
- 지역 후보 M02/R01/R03의 그래픽 8개는 유지했다. 공통 장치 삭제를 새 지역 장치로 채우지 않았다.

## 바로 확인

1. `ANIMOL_Obstacle_V4_Preview.html`을 브라우저에서 연다. 모든 아트가 포함된 단일 파일이다.
2. 표시 상태를 작동으로 바꾸거나 ‘밟기·충돌 연출’을 눌러 스프링·떡을 확인한다. 컨베이어는 띠와 롤러가 계속 움직인다.
3. C05를 골라 ‘이탈·붕괴 연출’을 누르면 상판이 사라지고 조각이 아래로 떨어져 페이드아웃된다. 이 HTML은 Unity 구현 결과가 아니라 연출 방향을 보여 주는 미리보기다.
4. 정지 비교판은 `Art/Preview_45_Theme_Skins.png`, 움직임 비교판은 `Art/Preview_45_Animated.gif`다.
5. MCP/코덱스에는 `Docs/ANIMOL_OBSTACLE_V4_CODEX_APPLY.txt`를 전달한다.

## 적용 범위

미리보기와 JavaScript 접합·프레임 선택은 실행했다. 실제 ANIMOL Unity 저장소는 이 환경에 없어 C# 컴파일·Scene/Runtime·물리·맵 마이그레이션·Undo 검증은 실행하지 않았다. Unity 적용 명령에 이 검사를 포함했다.
C05는 기존 이탈 소멸 의미를 유지한다. 밟을 때 균열이 더 드러나고 마지막 점유자가 이탈해 실제 상판이 비활성화되면 조각 낙하를 시작한다. 서 있는 플레이어를 낙하시킬 새 타이머는 추가하지 않았다.

## 파일

- `Docs/ANIMOL_Common_9_Tile_Design_v3.md`: 이번 기능 정본과 삭제·변경 사항.
- `Docs/OBSTACLE_THEME_DESIGN_KO.md`: 45개 소재·형태 및 픽셀 규칙.
- `Docs/GRAPHIC_JOIN_CONTRACT_KO.md`: FULL/TOP/AIR 연결과 cap 대체.
- `Docs/UNITY_ANIMATION_AND_CRUMBLE_KO.md`: Unity 연출 연결과 이벤트.
- `Data/mechanism_catalog.json`: 프레임별 atlas rect; key는 `theme/kind/facing/pose/fN`.
- `Data/c05_fragment_catalog.json`, `c05_fragment_effect.json`: 조각 이미지·낙하값.
- `Validation/RESULT_KO.md`: 검사 결과와 미실행 범위.

## 재현

```bash
npm install
npm test
python -m pip install Pillow
python Tools/verify_pixel_art.py
node Tools/verify_native_preview.mjs
```

기능 아트를 다시 만들 때 전체 v4 원본 ZIP을 풀어 경로를 지정한다.

```bash
node Tools/build_pack.mjs /absolute/path/ANIMOL_FreeShape_Sprites_joint_finish_v4
```

GIF 재생성에는 FFmpeg가 필요하다. Native 검수 후 `node Tools/build_animation_gif.mjs`로 만든다. 원본 v4 전체 ZIP은 중복 포함하지 않았다. `Data/v4_sprite_lookup.json`의 원본 `Art/Atlases` 경로는 `Data/body_atlas_paths.json`으로 이 패키지의 복사본 위치에 대응한다. 실제 Unity에서는 기존 Registry와 GUID를 재사용한다.

픽셀 PNG는 Sweetie16·정수 좌표·alpha 0/255를 사용한다. 낙하 페이드의 런타임 투명도는 요청한 연출이며 자산 명암·디더링과 별개다. 자동 후보 검사는 모든 Jaggies/Doubles의 미관 판정을 보장하지 않는다.
