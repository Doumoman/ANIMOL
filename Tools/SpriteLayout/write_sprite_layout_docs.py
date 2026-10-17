from pathlib import Path
Path('Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md').write_text('''# 현재 프로젝트 파일 구조

활성 파일에는 버전 폴더를 붙이지 않는다. 스프라이트와 AnimationClip의 정본은 `Assets/ANIMOL/Sprites`다.

```text
Assets/ANIMOL/
  Sprites/
    UI/InGame/
    Player/rabbit/Frames/
    Tiles/Floor/ranch/
    Tiles/Wall/ranch/
    Tiles/Decoration/ranch/
    Background/ranch/{Far,Mid}/
  Prefabs/
  Scenes/QA/ANIMOL_Starway_Art_QA.unity
  Scripts/{Runtime,Editor,Tests}/
  Fonts/
  Materials/
  Data/
    art_manifest.json
    art_import_manifest.json
    AnimationSequences/
```

`Assets/TextMesh Pro`는 Unity 텍스트 기반 의존성이다. 기존 로비·서비스·제작기·테스트·데이터·문서·캡처·패키지는 `Legacy/PreviousProject_20261013` 및 `Legacy/Platformer_20261013`에 있다. 이전 내용은 활성 Assets로 복구하지 않는다. Unity 엔진의 Packages/ProjectSettings/Library/Temp/Logs/UserSettings는 게임 콘텐츠와 구분한다.

## 정본 이름과 분류

- PNG 177개, AnimationClip 15개를 정본으로 이동했다. PNG는 `spr_`, 클립은 `anim_` 접두사의 소문자 snake_case다.
- UI는 `spr_ui_ingame_pause_idle.png`, 토끼 정면 그림은 `spr_player_rabbit_idle_s.png`다.
- 프레임은 `spr_tile_decoration_ranch_vine_tile_touch_f00.png`처럼 f00부터, 클립은 `anim_tile_decoration_ranch_vine_tile_touch.anim`이다.
- 장치 구현이 없으므로 장식/일반 오브젝트 그림을 임의의 M01~M24 장치로 판정하지 않는다. 해당 그림과 식물 애니메이션은 Tiles/Decoration/ranch에 두었다. 장치가 실제 구현될 때 ID별로 분류한다.
- 실제로 없는 메인 메뉴·설정·상점·스테이지 선택·새 장치 폴더는 빈 폴더로 생성하지 않는다.
- 원본 크기와 80px 셀/128px 월드 방울의 PPU·피벗·프레임 시간을 유지한다. 신규 미등록 32px 스프라이트는 기본 PPU 32다. 정리를 아트 승인이나 32px 축소 완료로 간주하지 않는다.
- 버전 이름은 원본 ZIP/출처와 과거 이력에만 남는다. C# MovedFrom의 이전 식별자는 직렬화 객체 연결을 위한 이력이며 활성 경로 이름이 아니다.

## 실행 및 검증

기본/빌드 씬은 위 QA 씬 한 개다. **플레이 가능한 탑뷰 퍼즐 구현은 아직 없다.** 이동·밀기·코스트·수집·승리를 구현했다고 표시하지 않는다. 물리 Root는 고정하고 시각 프레임만 재생하며 미연결 HUD는 COST -- / --/--를 표시한다.

현재 근거는 [이동 목록](Validation/SpriteLayout/move_manifest.json), [GUID·원본·명명 검사](Validation/SpriteLayout/audit.json), [EditMode](Validation/SpriteLayout/editmode.json), [PlayMode](Validation/SpriteLayout/playmode.json)다. 이전 전체 격리에서 17,539개 원본의 SHA-256을 확인한 기록은 Validation/ActiveSlice 및 레거시 README에 보존한다. 현재 경로는 이 문서와 SpriteLayout 기록을 따른다.

검수 도구는 Tools/SpriteLayout에 모았다. Live PlayMode 테스트 동안 시작 씬 강제를 해제하고 실행 후 QA SceneAsset으로 복원한다. 현재 테스트는 ArtAssetTests / ArtQaPlayTests다. 과거 로비/CommercialPolish 테스트는 레거시이며 활성 통과 수에 섞지 않는다.

단일 수풀 3×3 조립의 작은 원본 알파 틈은 남은 아트 검토 항목이며 이번 파일 정리에서 그림을 수정했다고 주장하지 않는다.
''',encoding='utf-8')
