# ANIMOL 스프라이트 경로·명명 규칙

기준일: 2026-10-13. `Assets/ANIMOL/Sprites/`를 새 스프라이트의 단일 정본으로 사용한다. `Legacy/Platformer_20261013/`는 저장소 루트, Unity `Assets` 바깥에 둔다.

기준 게임은 세로 화면의 2D 탑뷰 스와이프 퍼즐이다. 유효한 이동 1회는 코스트 1이고 방울 3개를 모아 출구로 간다. 이 문서는 자산 정리 규칙이며 새 퍼즐 24종 구현과 기존 화면 전면 재제작은 작업 범위에 포함하지 않는다.

## 정본 경로

```text
Assets/ANIMOL/Sprites/
├── UI/
│   ├── MainMenu/
│   ├── InGame/
│   ├── Settings/
│   ├── StageSelect/
│   ├── Store/
│   └── Common/
├── Player/<character_id>/
│   ├── Frames/
│   └── Animations/
├── Tiles/
│   ├── Floor/<theme_id>/
│   ├── Wall/<theme_id>/
│   └── Decoration/<theme_id>/
├── Obstacles/M##_EnglishName/
│   ├── Frames/
│   └── Animations/
└── Background/<theme_id>/
    ├── Far/
    ├── Mid/
    └── Near/

Legacy/Platformer_20261013/
├── README.md
└── Assets/ANIMOL/...
```

UI/StageSelect와 UI/Store는 실제 화면이 있는 경우 사용한다. 실제 자산이 없는 빈 장치 폴더는 만들 필요가 없다. 캐릭터 ID는 `rabbit`처럼 ASCII 소문자 snake_case로 쓴다.

화면 ID는 `main`, `ingame`, `settings`, `stage_select`, `store`, `common`을 사용한다. 기본 테마 ID는 `common`, `ranch`, `theater`, `observatory`, `sea`다. 2026-10-13 기억 극장 적용 요청에 따라 `memory_theater`를 추가 등록했다. 기존 `theater` 이름이나 다른 테마는 일괄 변경하지 않는다. 한 장치의 모든 프레임과 클립은 같은 `M##` 폴더에 둔다. 에디터가 생성한 `.anim`과 원본 PNG의 이름에는 같은 기계 ID·상태·방향을 쓴다.

## 파일 이름

파일은 ASCII 소문자 snake_case를 기본으로 한다. 지정된 폴더 분류명과 장치 등록표의 대소문자는 그대로 유지한다.

- PNG: `spr_<scope>_<theme-or-screen>_<object>_<state>[_<direction>][_fNN].png`
- SpriteSheet: `sheet_<scope>_<theme-or-screen>_<object>_<state>[_<direction>].png`
- AnimationClip: `anim_<scope>_<theme-or-screen>_<object>_<state>[_<direction>].anim`
- Tile asset: `tile_<theme>_<family>_<edge>[_vNN].asset`
- 기계 ID는 `m01`~`m24`, 방향은 `n/e/s/w`, 프레임은 `f00`부터 두 자리로 통일한다.

| 분류 | 예시 |
|---|---|
| UI | `UI/InGame/spr_ui_ingame_pause_idle.png`, `UI/MainMenu/spr_ui_main_play_pressed.png` |
| Player | `Player/rabbit/Frames/spr_player_rabbit_walk_s_f00.png`, `Player/rabbit/Animations/anim_player_rabbit_walk_s.anim` |
| Floor / Wall / Decoration | `Tiles/Floor/ranch/spr_tile_floor_ranch_grass_ne_v01.png`, `Tiles/Wall/ranch/spr_tile_wall_ranch_stone_n.png`, `Tiles/Decoration/ranch/spr_tile_decoration_ranch_clover_v01.png` |
| Obstacle frames | `Obstacles/M16_PairedPortal/Frames/spr_obstacle_observatory_m16_portal_enter_e_f00.png` |
| Obstacle clips | `Obstacles/M16_PairedPortal/Animations/anim_obstacle_observatory_m16_portal_enter_e.anim` |
| Background | `Background/ranch/Far/spr_bg_ranch_far_stars_v01.png` |

타일 경계는 `center,n,s,e,w,ne,nw,se,sw`를 사용한다. 상태는 실제 화면 변화에 맞춰 `idle, preview, active, blocked, reset` 등 의미 있는 단어로 적는다. 방향이나 프레임이 없는 단일 그림에서는 해당 접미사를 생략한다. `final`, `new`, `fix`, 날짜, 반복된 `v2_final`은 파일 이름에 넣지 않는다. 바뀐 결과는 Git 이력에서 관리한다. `vNN`은 명세에 있는 타일·장식 변형 번호다.

서로 다른 기존 UI 도안에는 object의 도안 식별자로 `portrait`, `theme_menu`를 사용한다. 다른 GUID를 합치거나 PNG 내용을 덮어쓰지 않는다. 예: `spr_ui_main_portrait_button_play_idle.png`.

## 24개 장치 폴더 등록표

| ID | 폴더 | 대표 테마 |
|---|---|---|
| 01 | `M01_StarSocket` | ranch |
| 02 | `M02_VineBridge` | ranch |
| 03 | `M03_CrescentJump` | observatory |
| 04 | `M04_AlternatingShutters` | theater |
| 05 | `M05_ExitSpikes` | sea |
| 06 | `M06_ActionStage` | theater |
| 07 | `M07_Conveyor` | common |
| 08 | `M08_RotatingBookshelf` | theater |
| 09 | `M09_OneWayGate` | common |
| 10 | `M10_LightSensor` | observatory |
| 11 | `M11_Ferry` | sea |
| 12 | `M12_RollingBarrel` | sea |
| 13 | `M13_DominoWall` | theater |
| 14 | `M14_Magnet` | observatory |
| 15 | `M15_TimedBomb` | sea, 일부 맵 |
| 16 | `M16_PairedPortal` | observatory |
| 17 | `M17_ChargeStatue` | theater |
| 18 | `M18_BounceCorner` | common |
| 19 | `M19_BrakePlate` | common |
| 20 | `M20_WindLane` | ranch |
| 21 | `M21_GlassWall` | observatory |
| 22 | `M22_DiceBlock` | theater |
| 23 | `M23_CowardDoll` | theater |
| 24 | `M24_FollowerChick` | ranch |

## 크기·임포트·애니메이션

- 새 타일 원본은 최소 32×32 px이며 여러 칸 그림은 정수 타일 크기로 관리한다.
- 새 최종 픽셀 스프라이트는 Point, 압축 없음, mipmap 없음, 32px 타일의 PPU 32를 기본으로 한다.
- P01 고해상도 원본은 32px 최종 스프라이트로 간주하거나 강제로 축소하지 않는다. 표시 크기를 유지할 별도 PPU와 원본/시제품 상태를 기록한다.
- 기존 P01 10fps 클립은 이동 후 sampleRate·실제 프레임 수·스프라이트 곡선을 유지한다. 원본 `.anim`과 `.meta`를 이동한다.
- 현재 공용 UI는 기존 픽셀 크기·PPU·슬라이스·피벗을 보존한다. 실측 75개 모두 Point/Uncompressed/mipmap 없음/PPU 32다. 32px보다 작은 UI 아이콘 11개는 타일이 아니며 크기를 바꾸지 않았다. [실측 임포트 검사](Validation/TopdownCutover/supplement-import-audit.json)에 각각 기록했다.

## 레거시와 정본의 경계

- Legacy는 플랫포머 시절의 회복 가능한 보관본이며 활성 Assets에 임포트하지 않는다. 보관본의 원래 이름은 유지한다.
- `Assets/ANIMOL/Phase1`이 실제로 있으면 탑뷰 시제품 후보로 유지한다. `UI/AnimalUpgradePhase1`과 구분한다.
- P01 적용 문서만으로 아트를 승인하지 않는다. 정확한 16색이 아닌 PNG·고해상도 시제품은 그 상태를 기록한다.
- 공용 계정·로비·경제·서버·방울 기능은 실제 참조를 검사한 뒤 분류한다.
- 구형 맵 제작기는 원본을 Legacy에 보존한다. 남는 Editor 코드에는 `// LEGACY: platformer map editor, 2026-10-13`과 보관 경로를 적고 기본 비활성 `ANIMOL_LEGACY_MAP_EDITOR` define으로 차단한다.
- `MainUiV7`의 기존 20개 배경 레이어·토끼 시트 2개는 저장소의 별도 배경 기준 패키지로 유지한다.

## 이번 실측 적용

ANIMOL 공용 UI PNG 75개의 내용·GUID·`.meta`를 유지하며 화면 ID·상태 규칙으로 이름과 경로를 정리했다. [정본 manifest](../Assets/ANIMOL/Sprites/sprite_path_manifest.json)는 원래 패키지 키, 이전 정본 경로, 최종 경로와 GUID를 연결한다. Editor의 호환 입력도 최종 경로로 변환한다. 런타임 프리팹·씬은 동일 GUID를 사용한다.

실측 저장소에는 `Assets/ANIMOL/Phase1`과 지정 P01 QA 씬이 없다. 10fps 보존·P01 표시 검사·P01 Validate는 대상 없음으로 구분한다. 동물 업그레이드 Phase1 UI는 공용 기능으로 유지한다. 새 퍼즐 장치 24종이나 없는 테마 아트를 생성하지 않는다.
# 현재 정본 적용 상태

기존 정리에서 PNG177개와 클립15개를 Assets/ANIMOL/Sprites에 이동했다. 장치8·기억 극장·편지 천문대·황혼의 바다 적용을 포함한 현재 총량은 PNG3455개·클립542개다. 이전 버전 적용 루트 예외는 폐지했다. PNG는 spr_, 클립은 anim_ 접두사의 소문자 snake_case이며 프레임은 f00부터다. 장치 구현이 없는 식물/일반 그림은 M## ID를 임의 지정하지 않고 Tiles/Decoration/ranch에 분류한다. 고해상도 원본과 80/128 PPU는 유지하며 경로 정리 때문에32px로 축소하지 않는다. 실제 폴더는 [활성 프로젝트 구조](ANIMOL_ACTIVE_PROJECT_LAYOUT.md)를 따른다.

기억 극장 공통 변형은 `Sprites/Obstacles/common/memory_theater/M##_EnglishName/{Frames,Animations}`, 전용 7종은 `Sprites/Obstacles/memory_theater/M##_EnglishName/{Frames,Animations}`를 사용한다. 바닥/벽은 `Tiles/{Floor,Wall}/memory_theater`, 상자는 `Props/memory_theater`, 배경·기둥·막·조명은 `Background/memory_theater/{Far,Mid,Near}`다. 이전 M## 정본을 덮어쓰지 않는다. 이 테마 경로/ID 확장은 사용자의 기억 극장 적용 지시를 따른다.

사용자의 편지 천문대 적용 요청에 따라 `letter_observatory`를 추가 등록했다. 공통4종은 `Sprites/Obstacles/common/letter_observatory/M##_EnglishName/{Frames,Animations}`, 전용5종은 `Sprites/Obstacles/letter_observatory/M##_EnglishName/{Frames,Animations}`다. 바닥/벽은 `Tiles/{Floor,Wall}/letter_observatory`, 배경/망원경/편지는 `Background/letter_observatory/{Far,Mid,Near}`에 둔다. 기존 테마는 유지한다. 현재 정본 총량은 PNG2743개·클립405개다. 256px 도약 궤적의 시작점 피벗과 128px 유리 파편 캔버스를 유지하며32px로 축소하지 않는다.

황혼의 바다 지시에 따라 `twilight_sea`를 등록했다. 공통4종은 `Sprites/Obstacles/common/twilight_sea/M##_EnglishName/{Frames,Animations}`, 전용4종은 `Sprites/Obstacles/twilight_sea/M##_EnglishName/{Frames,Animations}`다. 바닥/물/해안·벽은 `Tiles/{Floor,Wall}/twilight_sea`, 상자는 `Props/twilight_sea`, 배경/난간/등/해초는 `Background/twilight_sea/{Far,Mid,Near}`다. 이전 테마 경로와 GUID는 유지한다. PNG712·클립137을 더해 현재 정본은 PNG3455·클립542다. 셀80px/PPU80과96/128px 캔버스·좌상단 JSON 피벗·변동 프레임 시간을 유지하며 원화/시트/preview는 활성 Assets에서 제외한다.
