# 현재 프로젝트 파일 구조

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
    Obstacles/{M01_StarSocket,M02_VineBridge,M07_Conveyor,M09_OneWayGate,M18_BounceCorner,M19_BrakePlate,M20_WindLane,M24_FollowerChick}/{Frames,Animations}/
  Prefabs/
  Scenes/ANIMOL_Starway_Art_QA.unity
  Scenes/ANIMOL_Device8_Art_QA.unity
  Scenes/ANIMOL_MemoryTheater_Art_QA.unity
  Scenes/ANIMOL_LetterObservatory_Art_QA.unity
  Scripts/{Runtime,Editor,Tests}/
  Fonts/
  Materials/
  Data/
    art_manifest.json
    art_import_manifest.json
    device_art_import_manifest.json
    memory_theater_import_manifest.json
    Themes/
    AnimationSequences/
```

`Assets/TextMesh Pro`는 Unity 텍스트 기반 의존성이다. 기존 로비·서비스·제작기·테스트·데이터·문서·캡처·패키지는 `Legacy/PreviousProject_20261013` 및 `Legacy/Platformer_20261013`에 있다. 이전 내용은 활성 Assets로 복구하지 않는다. Unity 엔진의 Packages/ProjectSettings/Library/Temp/Logs/UserSettings는 게임 콘텐츠와 구분한다.

## 정본 이름과 분류

- 기존 PNG 177개·클립 15개, 장치8 PNG 454개·클립 59개, 기억 극장 PNG 1385개·클립 211개가 정본에 있다(총 PNG 2016개·클립 285개). PNG는 `spr_`, 클립은 `anim_` 접두사의 소문자 snake_case다.
- 기억 극장 공통 변형은 Sprites/Obstacles/common/memory_theater/M##, 전용 7종은 Sprites/Obstacles/memory_theater/M##, 나머지 바닥/벽/상자/배경은 Tiles/Props/Background의 memory_theater에 둔다. 이전 테마 경로를 옮기지 않는다. 공통 프리팹 Variant와 테마/주사위 카탈로그는 Prefabs/Themes/memory_theater 및 Data/Themes에 있다.
- UI는 `spr_ui_ingame_pause_idle.png`, 토끼 정면 그림은 `spr_player_rabbit_idle_s.png`다.
- 프레임은 `spr_tile_decoration_ranch_vine_tile_touch_f00.png`처럼 f00부터, 클립은 `anim_tile_decoration_ranch_vine_tile_touch.anim`이다.
- 기존 장식/일반 오브젝트와 식물은 Tiles/Decoration/ranch에 유지한다. 새 8종 장치 팩의 명시된 ID만 Obstacles/M## 폴더로 분류했다. 장치 표현은 연결됐지만 게임 판정 구현은 없다.
- 실제로 없는 메인 메뉴·설정·상점·스테이지 선택·새 장치 폴더는 빈 폴더로 생성하지 않는다.
- 원본 크기와 80px 셀/128px 월드 방울의 PPU·피벗·프레임 시간을 유지한다. 신규 미등록 32px 스프라이트는 기본 PPU 32다. 정리를 아트 승인이나 32px 축소 완료로 간주하지 않는다.
- 버전 이름은 원본 ZIP/출처와 과거 이력에만 남는다. C# MovedFrom의 이전 식별자는 직렬화 객체 연결을 위한 이력이며 활성 경로 이름이 아니다.

## 실행 및 검증

기존 빌드 씬은 ANIMOL_Starway_Art_QA를 유지한다. 현재 에디터 검수/Play 시작 씬은 ANIMOL_LetterObservatory_Art_QA다. **플레이 가능한 탑뷰 퍼즐 구현은 아직 없다.** 이동·밀기·코스트·수집·승리를 구현했다고 표시하지 않는다. 물리 Root는 고정하고 시각 프레임만 재생하며 미연결 HUD는 COST -- / --/--를 표시한다.

현재 근거는 [이동 목록](Validation/SpriteLayout/move_manifest.json), [GUID·원본·명명 검사](Validation/SpriteLayout/audit.json), [EditMode](Validation/SpriteLayout/editmode.json), [PlayMode](Validation/SpriteLayout/playmode.json)다. 이전 전체 격리에서 17,539개 원본의 SHA-256을 확인한 기록은 Validation/ActiveSlice 및 레거시 README에 보존한다. 현재 경로는 이 문서와 SpriteLayout 기록을 따른다.

검수 도구는 Tools/SpriteLayout 및 Tools/DeviceArt에 모았다. Live PlayMode 테스트 동안 시작 씬 강제를 해제하고 실행 후 QA SceneAsset으로 복원한다. 현재 테스트는 ArtAssetTests / ArtQaPlayTests / DeviceArtTests / DeviceArtPlayTests다. 과거 로비/CommercialPolish 테스트는 레거시이며 활성 통과 수에 섞지 않는다. 장치 적용 근거는 [적용 결과](../ANIMOL_DEVICE8_V01_APPLY_RESULT.md)와 Validation/DeviceArt에 있다.

기억 극장 도구는 Tools/MemoryTheater, 테스트는 MemoryTheaterArtTests / MemoryTheaterArtPlayTests다. [기억 극장 적용 결과](../ANIMOL_MEMORY_THEATER_V01_APPLY_RESULT.md)와 Validation/MemoryTheater에 최종 전체 EditMode 13/13·PlayMode 4/4, 24상태/96전이 렌더 검증, 임시 Atlas 검사/제거 및 실제 표시 복구 근거가 있다. 실제 게임 판정 연결은 미완료다.

단일 수풀 3×3 조립의 작은 원본 알파 틈은 남은 아트 검토 항목이며 이번 파일 정리에서 그림을 수정했다고 주장하지 않는다.

편지 천문대 적용으로 PNG727·클립120이 추가돼 현재 정본 총량은 PNG2743·클립405다. 공통 변형은 Obstacles/common/letter_observatory/M##, 전용5종은 Obstacles/letter_observatory/M##, 바닥/벽/배경은 각 letter_observatory 테마 폴더다. 프리팹247개와 Data/Themes의 테마/광학 표시 자료를 실제 QA에 연결했다. Tools/LetterObservatory, Validation/LetterObservatory/Applied, LetterObservatoryArtTests/LetterObservatoryArtPlayTests 및 [결과](../ANIMOL_LETTER_OBSERVATORY_V01_APPLY_RESULT.md)에 전체 EditMode18/18·PlayMode6/6의 근거가 있다. 실제 게임 판정 호출자는 없고 광선 경로는 제공받은 표시 목록만 재생한다.
