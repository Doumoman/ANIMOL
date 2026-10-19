# 잔영 극장 환경 아트 적용 결과

## 적용 위치와 연결

- 테마 저장 ID와 기존 레벨 경로: `memory_theater` 유지. 기존 극장 아트 에셋에 V02 참조를 추가해 같은 `PuzzleTheme`에서 사용한다.
- 표시명: 로비의 한글 `잔영 극장`을 유지하고, 극장 캐릭터 메타데이터의 영문 표시값을 `Echo Theater`로 갱신했다. 메타데이터의 제작용 `id`와 실제 저장 ID `memory_theater`는 변경하지 않았다.
- 런타임 원화: `Assets/ANIMOL/Sprites/Environment/memory_theater/`에 원본 PNG 1,782개를 바이트 변경 없이 설치했다. 지형 1,728개, 소품 14개, 장치 26개, 배경 14개다. `.meta`가 있던 기존 아트와 프리팹의 GUID는 유지했다.
- 명세와 카탈로그: `Assets/ANIMOL/Data/Art/MemorySkyTheater/Environment/`. `sprite_import_settings.json`의 1,782개 항목을 Unity 임포터와 대조한다.
- 실제 퍼즐 씬: `Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity`. 기존 맵·카메라·장치 컴포넌트를 사용하고, 런타임 극장 아트 렌더러만 새 선택기로 연결했다. 씬 에셋 자체와 저장 데이터는 변경하지 않았다.
- 기존 QA 씬의 `MemorySkyTheaterInspection`도 새 원화가 설치된 경우 V02 지형과 배경을 표시한다. 기존 V01 QA 렌더러와 테스트용 데이터는 보존했다.

## 전경과 배경

기존 맵의 `TryTerrain` 점유만 `BuildVisualMaterialRows`에 전달한다. 카펫은 기존 레이아웃의 시각 마스크이며 빈 셀을 새 바닥으로 만들지 않는다. 기존 `visualDeck`은 **이미 점유된 셀에서만** 캣워크 재질을 선택할 수 있다. 맵 전체로 8이웃 상면, 남쪽 하부 구간, 연속 연결띠와 빈 공간 외곽 난간을 계획한다. 기존 퍼즐 씬은 모든 점유 셀이 무대 바닥 재질이므로 캣워크 난간은 이 레벨에서 나오지 않는다. 상면·하부·난간은 모두 SpriteRenderer이며 충돌을 만들지 않는다.

기존 `fitBorderToMap`의 장식 영역에 V02 커튼 날개·반복 린텔·오르간·좌석·고정 발치 조명을 놓는다. 오르간을 실제 좁은 통로에서 무대 바깥 장식 영역으로 옮겨 재촬영했다. 나머지 소품 원화는 정확한 개별 PPU·피벗으로 카탈로그에 들어 있으며, 원래 레벨에 배치 지시가 없는 소품은 임의로 놓지 않았다.

배경은 새 하늘 1개와 독립 Renderer 130개(별 120, 구름 6, 극장 섬 2, 벨벳 장식 2)를 배치 명세에서 생성한다. 기존 하늘 반복 합성은 극장 테마에서 사용하지 않는다. 별은 모든 구름 뒤에 있고 drift/twinkle 0, parallax multiplier 0.03이다. 카메라의 직교 투영과 기존 추적을 유지하고, far clip은 최소 128로 설정한다. `Time.deltaTime` 정지와 기존 reduced-motion 설정 전달을 플레이에서 확인했다.

## 기존 장치 대응

패키지의 `artKey`는 저장 ID로 사용하지 않았다. 기존 `DeviceVisualView`의 정적 상태 중 그림과 방향이 확실한 12개만 같은 프리팹에서 교체했다. 기존 시퀀스, 이벤트명, 방향별 상태와 GUID는 유지했다.

| V02 계열 | 기존 장치 ID / 프리팹 | 연결한 기존 정지 상태 |
|---|---|---|
| conveyor | 07 `d07_conveyor` | `idle_e` |
| oneway_gate | 09 `d09_one_way_gate` | `closed_e` |
| bounce_corner | 18 `d18_bounce_corner` | `idle_ne` |
| brake_pad | 19 `d19_brake_pad` | `idle_horizontal`, `pressed_horizontal` |
| alternating_shutter | 04 `d04_stage_shutter` | `a_horizontal_closed` |
| moving_stage | 06 `d06_moving_stage` | `idle_e` |
| rotating_bookcase | 08 `d08_rotating_bookshelf` | `horizontal`, `horizontal_reversed` |
| domino_wall | 13 `d13_domino_wall` | `standing_e`, `folded_e` |
| charging_statue | 17 `d17_charging_statue` | `idle_e` |
| timid_puppet | 23 `d23_coward_doll` | 대응 정적 방향·상태 미확인, 기존 원화 유지 |
| dice_block | 22 `d22_dice_block` | 여섯 면만으로 기존 24방향/회전 상태를 대체할 수 없어 기존 원화 유지 |

`open_e` 등 기존 연속 시퀀스와 추가 방향은 V02 정적 그림만으로 완성되지 않는다. 나머지 V02 장치 원화는 카탈로그에 있으며 추가 방향·전환 원화가 제공되면 같은 ID와 상태에 연결할 수 있다. 현재 플레이 씬의 장치 뷰 7개 중 4개는 V02 정지 상태로 표시되고 나머지는 보존한 기존 상태로 표시된다.

## 검증

- 패키지 원본과 설치본의 PNG 바이트, 1,782개 명세의 PNG 크기 대조: 오류 0.
- PNG 알파 검사: 하늘은 불투명 RGB, 나머지 1,781장은 알파 채널과 투명 픽셀을 가진다.
- Unity `EchoTheaterV02Builder.Validate()`: 1,782개 Sprite의 개별 PPU·피벗·Bilinear·FullRect 등과 카탈로그, 배경 131개 검증 통과.
- Unity 컴파일 통과. `EchoTheaterV02TerrainTests` 8/8, `EchoTheaterV02PlayTests` 1/1, 기존 `MemorySkyTheaterTests` 10/10, `MemorySkyTheaterPlayTests` 3/3, `ArtAssetTests` 4/4, `ArtQaPlayTests` 1/1 통과.
- 실제 극장 Prototype 플레이: 점유 123칸과 상면 123개 일치, 중복 구형 극장 렌더러 0개, 새 지형 Collider 0개, 독립 배경 Renderer 131개, far clip 1000 확인. 1080×1920에서 5행·25행 배율과 세로 80칸 임시 맵의 카메라 y=60 화면을 촬영해 하늘 빈 띠를 확인했다. `timeScale=0`에서 구름 위치가 정지했고 reduced-motion 값 전달을 확인했다. 긴 맵 테스트의 빈 영역과 플레이어 이동은 배경 확인용 런타임 변경이며 레벨 데이터에 저장하지 않았다.
- `STYLE_CHECKLIST_KO.txt`의 1~5, 7~10항은 전체 예시·축소 구성 시트·배경 비교 시트와 실제 씬 캡처를 육안 대조했다. 큰 각진 외곽, 넓은 색면, 재질 분리, 새 극장 하늘과 독립 별·구름이 읽힌다. 6항의 8이웃·하부·연결은 선택기 테스트와 실제 씬에서 점검했으나, 현재 레벨에 캣워크 재질이 없어 모든 다리 방향과 내부 구멍을 플레이 화면으로 각각 검수하지는 못했다.
- 원본 ZIP의 `verify_device_semantics.cjs`는 외부 Node 패키지 `sharp`가 없어 실행되지 않았다. Android 빌드는 실행하지 않았다.

남은 시각 검수: V02 정적 장치에 없는 방향/전환 상태는 기존 픽셀 원화와 혼합 표시된다. 해당 아트를 임의 회전하거나 애니메이션으로 합성하지 않았다. 캣워크·내부 구멍·오목 모서리·모든 2×1 소품의 실전 레벨 배치는 현재 레벨 데이터에 없어 수동 플레이 검수가 남았다.
