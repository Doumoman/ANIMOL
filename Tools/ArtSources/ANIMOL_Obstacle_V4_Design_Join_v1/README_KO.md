# ANIMOL 장애물 v4 디자인·지형 접합 v1

2026-10-06. 타일 디자인 수정 채팅의 `ANIMOL_FreeShape_Sprites_joint_finish_v4.zip`을 기준으로 만든 그래픽 제작 패키지다.

## 바로 확인

1. `ANIMOL_Obstacle_V4_Preview.html`을 브라우저에서 연다. 모든 아트가 포함된 단일 파일이며 서버나 인터넷 연결이 필요 없다.
2. 스타일 20개, 공통 장치 10종, 바닥·벽·단차·분리·음수 경계와 표시 상태를 바꿔 본다.
3. 1배와 8배에서 줄눈·끝캡·기능 화살표를 확인한다. 클릭으로 자유 블록·장치·지우기를 시험한다.
4. 실제 Unity 적용은 `Docs/ANIMOL_OBSTACLE_V4_CODEX_APPLY.txt`를 코덱스에 전달한다.

## 들어 있는 결과

| 항목 | 내용 |
|---|---|
| 공통 장치 | 10종 × 5테마 = 기능 디자인 50개 |
| 기능 표시 PNG | 방향·사용하는 상태를 구분한 163개, 각 32×32px |
| 기능 표시 아틀라스 | `Art/MechanismAtlas.png`, 576×396px, rect 사이 여백 2px |
| 원래 지형 몸체 | v4 20스타일 × 4위상, 원본 아틀라스 80장 바이트 복사 |
| 지역 후보 | 월궁 달그릇, 광산 공명·대전 수정판의 그래픽 8프레임 포함 |
| 연결 선택기 | 실행 가능한 JavaScript와 Unity 이식용 순수 C# 소스 |
| 정확한 비교판 | `Art/Preview_50_Theme_Skins.png` |
| 이미지 생성 시안 | `Design/Concept_50_Theme_Skins.png`, 제작 방향 참고용 |

50개는 테마별 기능 스킨 수다. 몸체는 선택한 A/B/C/D 스타일과 47 연결 마스크·4위상에 따라 원본을 공유한다. 10종을 20개의 별도 물리 시스템으로 만들지 않는다.
기존 지역별 15종은 그대로 더하지 않는다. 위 3종은 최신 v2에서 지역 전용 후보로 남은 장치의 그래픽이며 신규 게임 규칙을 확정한 결과가 아니다.

## 실제 구현 범위

이 패키지의 미리보기에서 이웃에 따라 몸체와 윗면 선택이 자동으로 바뀐다. 같은 재질의 고체 장치는 지형 몸체와 이어지고, TOP 장치는 상판만 이어지며, AIR 장치는 연결 고체로 세지 않는다. 비활성 TOP이 사라지면 옆 지형 끝캡이 복구된다. 전역 좌표·시드로 벽돌 무늬 위상을 유지한다.

실제 ANIMOL Unity 저장소는 이 작업 공간에 없다. 제공한 C# 코어를 기존 Registry/Renderer와 연결하는 단계와 Unity 컴파일·Scene/Runtime·물리·Undo 검사는 적용 프로젝트에서 수행해야 한다. 기존 사용자 맵·프리팹을 이 패키지 제작 중 변경하지 않았다.

## 픽셀 검수

새 PNG의 팔레트·알파·32px 범위, v4 몸체 파일 보존, 고립 점과 검은 2×2 선 후보를 검사했다. 후보 0개는 모든 곡선이 미관상 완벽하다는 판정이 아니다. 기준 면과 의도한 2×2 모서리는 보존하고 자동 블러·디더링·일괄 점 삭제를 적용하지 않았다.
원본 1배의 50개 비교판과 월궁 바닥·벽 접합을 열어 확인했다. 확대 확인 범위와 실행 결과는 `Validation/RESULT_KO.md`에 기록한다.

이미지 생성 시안에는 미세 색·픽셀·표현 오차가 있을 수 있어 Runtime 아틀라스로 사용하지 않는다. Runtime은 검사를 통과한 원본 v4 몸체와 정수 좌표로 제작한 기능 표시 PNG를 사용한다. 시안의 점선·원근·떠 있는 장식을 그대로 구현하지 않는다.

## 재현

```bash
npm install
npm test
python -m pip install Pillow
python Tools/verify_pixel_art.py
node Tools/verify_native_preview.mjs
```

새 기능 표시를 재제작할 때는 전체 v4 원본 패키지의 경로를 지정한다.

```bash
node Tools/build_pack.mjs /absolute/path/ANIMOL_FreeShape_Sprites_joint_finish_v4
```

전체 v4 원본 ZIP은 중복 포함하지 않았다. 보존한 아틀라스의 출처와 SHA256은 `Validation/v4_source_preservation.json`에 있다. 다른 컴퓨터의 과거 v3 파일이나 현재 Unity 에셋 상태까지 검사했다는 의미는 아니다.
`Data/v4_sprite_lookup.json`은 원본의 `Art/Atlases` 경로를 유지한다. 이 패키지의 복사본 위치로 읽을 때는 `Data/body_atlas_paths.json`의 80개 경로 매핑을 적용한다. 실제 Unity Registry의 기존 GUID는 다시 만들지 않는다.
