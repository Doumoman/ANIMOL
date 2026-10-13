# ANIMOL 스프라이트 정본과 명명

정본 루트는 `Assets/ANIMOL/Sprites/`다. 사용 중인 자산의 `.meta` GUID와 스프라이트 subasset fileID를 유지한다. PNG를 복제하거나 재슬라이스해서 같은 그림의 새 GUID를 만들지 않는다.

| 종류 | 경로 |
|---|---|
| UI | `UI/{MainMenu,InGame,Settings,Common}` |
| 실제 선택·상점 UI | `UI/{StageSelect,Store}` |
| 플레이어 | `Player/<character_id>` |
| 타일 | `Tiles/{Floor,Wall,Decoration}/<theme_id>` |
| 장애물 프레임 | `Obstacles/M##_EnglishName/Frames` |
| 장애물 애니메이션 | `Obstacles/M##_EnglishName/Animations` |
| 배경 | `Background/<theme_id>/{Far,Mid,Near}` |

폴더의 지정 분류명은 위 표의 대소문자를 따른다. 실제 파일명은 ASCII 소문자 snake_case다. `character_id`와 `theme_id`도 소문자 snake_case를 사용한다. 아직 없는 캐릭터·테마·장애물 번호를 임의로 발급하지 않는다.

## 규칙

- 스프라이트: `spr_<분류>_<대상>_<동작/상태>[_방향][_프레임].png`.
- 애니메이션: `anim_<분류>_<대상>_<동작>[_방향].anim`.
- 방향은 `n/e/s/w`, 프레임은 `f00`부터 시작하는 두 자리 순번이다. 방향/프레임이 없는 정적 UI에는 억지로 접미사를 붙이지 않는다.
- 예: `spr_ui_ingame_pause_idle.png`, `spr_player_rabbit_walk_s_f00.png`.
- 예: `spr_obstacle_observatory_m16_portal_enter_e_f00.png`, `anim_obstacle_observatory_m16_portal_enter_e.anim`.
- 보관본의 기존 이름은 재작성하지 않는다. 새 규칙은 활성 정본과 이후 제작 자산에 적용한다.

## 이번 이관

기존 공용 UI 패키지에서 실제 PNG 75개를 옮긴다. 이름 충돌과 출처 혼동을 피하려고 이름에 원래 UI 패키지 식별자를 포함한다. 원본→정본 대응은 [`sprite_path_manifest.json`](../Assets/ANIMOL/Sprites/sprite_path_manifest.json)에 있다.

`CanonicalSpritePaths.Resolve`는 패키지 manifest의 원래 입력 키와 계산 경로를 정본 자산 경로로 변환한다. 빌더 asmdef는 독립 Editor 전용 경로 해석 어셈블리를 명시적으로 참조한다. 런타임 프리팹/씬은 기존 GUID로 같은 스프라이트를 참조한다.

Phase1 업그레이드 UI는 실제 공용 화면으로 유지한다. 새 탑뷰 플레이어·타일·장애물·배경 파일은 발견되지 않았으므로 해당 분류의 자산을 만들어 낸 것처럼 기록하지 않는다. 카테고리 기본 폴더만 준비한다. 메인 로비 배경은 기존 `MainUiV7` 패키지 기준과 생성 경로를 유지한다.

향후 추가 시 AssetDatabase 이동, 계산 경로/Resources 키/manifest 확인, GUID 및 subasset 참조 검증, 재임포트·컴파일·해당 화면 검사를 함께 수행한다.
