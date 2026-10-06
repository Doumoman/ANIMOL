# 자유 블록과 장애물의 그래픽 접합 계약

## 1. 그래픽 자료와 게임 정본

지형 점유와 장치의 실제 충돌·효과는 기존 시스템이 소유한다. 새 선택기는 상태 갱신이 끝난 스냅샷을 읽어 몸체·상판·기능 표시의 렌더 계획만 만든다. 그래픽 연결을 위해 장애물 주변에 고체 셀·추가 Collider·추가 발판을 만들지 않는다.
동일 셀을 기존 지형과 장치 두 개가 동시에 소유하게 만들지 않는다. 지형 칸에 FULL 장치를 넣을 때는 기존 Validate/Commit에서 그 셀의 소유자를 장치로 교체하고 다른 값은 보존한다. AIR 장치는 고체 점유가 없는 칸에만 배치한다. TOP은 하부 전체가 고체가 아니다.

## 2. 재질 키

`themeId + styleId + terrainArtVersion(4) + joinGroup`이 같을 때만 이어 붙인다.
사용자가 고른 맵의 v4 A/B/C/D 스타일을 장치에 명시적으로 전달한다. 이웃 스타일이 둘 이상 다르면 임의로 한쪽을 고르거나 원본 지형을 재색칠하지 않는다. 스타일 경계를 유지한다. 자동 상속은 현재 맵 스타일이 유일한 경우에만 가능하다.
`visualVersion=1`은 이번 장애물 장식 버전이며 지형 artVersion=4와 다른 개념이다.

## 3. 연결 분류

| 분류 | 타입 | 몸체 마스크 참여 | 상판 연결 | 빈 공간 |
|---|---|---|---|---|
| FULL | C02/C03/C08/C10, 후보 R03 | 동일 재질의 FULL·자유 블록과 8방향 | 같은 높이 TOP과 윗면만 연결 | 기존 전체 셀 점유 |
| TOP | C01/C05/C06/C09, 후보 M02/R01 | 참여하지 않음 | 활성 윗면이 있는 좌우 이웃과 연결 | 상판 8px 아래 공간을 유지 |
| AIR | C04/C07 | 참여하지 않음 | 없음 | 셀 배경은 투명 |

동일 높이는 논리 셀의 윗면 `y+1`이다. 한 칸 위·아래의 상판을 옆판처럼 이어 붙이지 않는다. 대각선만 닿은 셀은 연결하지 않는다. C02는 지정한 면, 다른 표면형은 노출 윗면이 있어야 한다. 막힌 기능 면은 배치 검증 오류로 보고한다.

## 4. v4의 실제 선택 규칙

- N1/NE2/E4/SE8/S16/SW32/W64/NW128을 유지한다.
- 대각선은 양쪽 직교 이웃이 모두 있을 때만 켠다. 256 raw → 47 canonical → 기존 atlas index다.
- FULL의 `bodyMask`는 자유 블록과 같은 전역 좌표에서 계산한다. 장치가 옆에 있으면 자유 블록 쪽도 그 장치를 그래픽 이웃으로 읽어 자기 끝캡을 바꿔야 한다.
- TOP은 몸체 마스크에 들어가지 않는다. FULL 옆에 TOP이 있으면 FULL의 기본 몸체는 유지하고 윗쪽 8px만 `capMask` 그림으로 바꾼다. 아래 벽·빈 공간을 채우지 않는다.
- `variant = floorMod(x+(seed&1),2)+2*floorMod(-y+((seed>>1)&1),2)`는 원본 v4 식이다. 상태·프레임·청크·시간으로 무늬 시드를 바꾸지 않는다.
- 연결 Sprite 선택은 47종과 기존 규칙을 재사용한다. 새로운 임의 9방향 표로 축소하지 않는다.

## 5. 렌더 순서와 좌표

1. FULL 몸체: 원본 v4 atlas의 32×32 rect, 중앙 pivot, world `(x+0.5,y+0.5)`.
2. TOP 상판 또는 FULL의 상판 패치: 선택 rect의 이미지 위쪽 32×8px, PPU32. 중앙 pivot이면 world `(x+0.5,y+0.875)`이며 맨 위가 정확히 `y+1`이다.
3. 기능 표시: 새 32×32 투명 PNG, 중앙 pivot, world `(x+0.5,y+0.5)`.

그림만 선택·합성한다. 몸체를 덮어 흐릿하게 만들거나 선형 확대하지 않는다. TOP 상판에서 잘라 내는 높이는 표시용 높이이며 충돌체 두께를 바꾸지 않는다. inactive TOP은 상판 Sprite를 표시하지 않는다.
JSON rect는 top-left다. 32×32 Unity rect는 `atlasHeight-jsonY-32`, 상판 32×8 rect는 `atlasHeight-jsonY-8`. `GraphicPlan.CapOnly/CapPatch`와 `DrawBody`를 함께 사용한다.
정상 v4 Registry가 이미 설치돼 있으면 몸체 텍스처를 이중 로드하지 말고 기존 참조를 재사용한다. 제공한 원본 복사본은 설치·보존 검증용이다.

## 6. 상태·프레임·캐시

기능 표시 key는 `theme/kind/facing/pose`다. 방향은 C02·C08 LEFT/RIGHT, C07 UP/LEFT/RIGHT이며 나머지는 UP이다. C01의 실제 기능 표시는 DOWN 화살표다.
Pending은 생성 예고 프레임 warn으로 변환한다. 사용 상태 목록은 `Runtime/pixel_mechanisms.mjs`의 `POSES_BY_KIND`와 아틀라스 카탈로그에 있다. 등록되지 않은 key를 임의로 idle에 매핑하지 않는다.
TOP의 `SurfaceEnabled`는 기존 런타임의 실제 활성 윗면 값을 전달한다. `pose==warn`이라는 이유만으로 모든 TOP을 끄지 않는다. C09의 생성 Pending과 퇴장 Warn은 같은 예고 그림을 쓸 수 있지만 상판 활성 값은 서로 다르다.
단독 사망·리스폰이나 4~8인 상태를 이 선택기가 재정의하지 않는다. 점유 보호·위상은 기존 공용 게임 틱의 스냅샷을 사용한다. 개별 플레이어가 접촉했다고 몸체 위상나 연결 셀을 무작위로 바꾸지 않는다.
캐시에는 지형 버전·스타일·joinGroup·kind·bodyMask·capMask·variant·facing·pose·SurfaceEnabled가 포함된다. 위치가 달라도 해당 파생 값이 같으면 같은 그래픽을 공유할 수 있다.

## 7. 편집·삭제·이동·동적 복구

지형 점유·장치 종류·재질·TOP 활성 상태가 바뀌면 변경 셀과 8방향 이웃을 dirty 처리한다. 이동은 이전 위치와 새 위치를 둘 다 갱신한다. 삭제와 리스폰 복구도 같은 경로다. Undo/Redo는 정본 복원 후 같은 캐시를 재선택한다.
v4 문양은 4×4 본체와 주변 1셀 지지가 필요하므로 변경 셀 q에 대해 `(q.x-4..q.x+1,q.y-4..q.y+1)`의 36개 문양 anchor도 재검사한다. 새 장치 칸 위에는 큰 문양을 겹쳐 기능을 덮지 않는다. 문양은 기존 free terrain 지지 규칙을 유지하고 장치를 문양 지지로 추가하지 않는다.
청크 경계의 이웃은 전역 정본에서 읽는다. 음수 좌표에 C# 정수 truncation을 사용해 청크나 위상을 정하지 않는다. paint stroke 중에는 프리뷰 후보를, commit 이후에는 정본 스냅샷을 읽는다. 렌더용 파생 마스크를 게임 저장 데이터에 중복 저장하지 않는다.

## 8. 구현 파일의 역할

- `Runtime/obstacle_visual_resolver.mjs`: 이번 미리보기에서 실제 사용하는 선택기.
- `Runtime/ObstacleVisualResolver.cs`: 같은 계산의 순수 C# 이식 코어. 이 환경에서 C# 컴파일은 미실행이다.
- `Runtime/v4_terrain_*.mjs`: v4 원본 계산을 그대로 복사한 출처 자료.
- `Data/v4_sprite_lookup.json`: 기존 body rect·47마스크·4위상.
- `Data/mechanism_catalog.json`: 새 기능 표시 rect·163프레임·2px padding.

## 9. 검증 대상

FULL 접합, TOP 상판만 접합, AIR 미참여, 비활성 TOP의 끝캡 복원, 서로 다른 스타일, 대각선 접촉, 4위상과 음수 좌표, 기능 면 차단, 하나의 셀 중복 소유, 이동·삭제·Undo·재열기·Scene/Runtime 일치, 기존 body/motif/GUID·Collider 보존을 확인한다.
Unity 공식 참고: [RefreshTile](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/tilemaps/itilemap/refreshtile), [Sprite Atlas reference](https://docs.unity.cn/6000.0/Documentation/Manual/sprite/atlas/sprite-atlas-reference.html). 아틀라스 Filter Mode는 개별 Sprite 설정을 덮을 수 있으므로 아틀라스도 Point·압축 None·mipmap Off로 확인한다. 런타임 접합 규칙 자체는 이 패키지의 추가 구현 계약이다.
