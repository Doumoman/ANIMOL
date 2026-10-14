# 현재 프로젝트 파일 구조

**2026-10-14 퍼즐 구현 업데이트:** 새 플레이 씬은 `Assets/ANIMOL/Scenes/ANIMOL_Puzzle.unity`다. `Scripts/Runtime/Puzzle*`, `Scripts/Editor/Puzzle*`, `Data/Puzzle/{Maps,Themes}`, `Sprites/Tiles/Puzzle`에 가변 크기 탑뷰 퍼즐과 독립 제작기를 추가했다. 실제 격자 이동·자동 연쇄·포털·수집·출구 판정은 이 새 씬에서 실행된다. 아래 아트 적용 이력의 "게임 판정 미구현"은 기존 QA 씬의 범위를 설명한다. QA 씬은 계속 표시 검수용이며 새 퍼즐로 보고하지 않는다. 구현·검수 결과는 [가변 크기 퍼즐 결과](ANIMOL_VARIABLE_PUZZLE_RESULT.md), 근거는 `Validation/Puzzle`에 있다.

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
    Obstacles/common/{memory_theater,letter_observatory,twilight_sea}/M##_EnglishName/{Frames,Animations}/
    Obstacles/{memory_theater,letter_observatory,twilight_sea}/M##_EnglishName/{Frames,Animations}/
    Tiles/{Floor,Wall}/{memory_theater,letter_observatory,twilight_sea}/
    Props/{memory_theater,twilight_sea}/
    Background/{memory_theater,letter_observatory,twilight_sea}/{Far,Mid,Near}/
  Prefabs/
  Scenes/ANIMOL_Starway_Art_QA.unity
  Scenes/ANIMOL_Device8_Art_QA.unity
  Scenes/ANIMOL_MemoryTheater_Art_QA.unity
  Scenes/ANIMOL_LetterObservatory_Art_QA.unity
  Scenes/ANIMOL_TwilightSea_Art_QA.unity
  Scripts/{Runtime,Editor,Tests}/
  Fonts/
  Materials/
  Data/
    art_manifest.json
    art_import_manifest.json
    device_art_import_manifest.json
    memory_theater_import_manifest.json
    letter_observatory_import_manifest.json
    twilight_sea_import_manifest.json
    Themes/
    AnimationSequences/
```

`Assets/TextMesh Pro`는 Unity 텍스트 기반 의존성이다. 기존 로비·서비스·제작기·테스트·데이터·문서·캡처·패키지는 `Legacy/PreviousProject_20261013` 및 `Legacy/Platformer_20261013`에 있다. 이전 내용은 활성 Assets로 복구하지 않는다. Unity 엔진의 Packages/ProjectSettings/Library/Temp/Logs/UserSettings는 게임 콘텐츠와 구분한다.

## 정본 이름과 분류

- 기존 PNG177·클립15, 장치8 PNG454·클립59, 기억 극장 PNG1385·클립211, 편지 천문대 PNG727·클립120, 황혼의 바다 PNG712·클립137로 현재 정본은 총 PNG3455·클립542다. PNG는 `spr_`, 클립은 `anim_` 접두사의 소문자 snake_case다.
- 기억 극장 공통 변형은 Sprites/Obstacles/common/memory_theater/M##, 전용 7종은 Sprites/Obstacles/memory_theater/M##, 나머지 바닥/벽/상자/배경은 Tiles/Props/Background의 memory_theater에 둔다. 이전 테마 경로를 옮기지 않는다. 공통 프리팹 Variant와 테마/주사위 카탈로그는 Prefabs/Themes/memory_theater 및 Data/Themes에 있다.
- UI는 `spr_ui_ingame_pause_idle.png`, 토끼 정면 그림은 `spr_player_rabbit_idle_s.png`다.
- 프레임은 `spr_tile_decoration_ranch_vine_tile_touch_f00.png`처럼 f00부터, 클립은 `anim_tile_decoration_ranch_vine_tile_touch.anim`이다.
- 기존 장식/일반 오브젝트와 식물은 Tiles/Decoration/ranch에 유지한다. 새 8종 장치 팩의 명시된 ID만 Obstacles/M## 폴더로 분류했다. 장치 표현은 연결됐지만 게임 판정 구현은 없다.
- 실제로 없는 메인 메뉴·설정·상점·스테이지 선택·새 장치 폴더는 빈 폴더로 생성하지 않는다.
- 원본 크기와 80px 셀/128px 월드 방울의 PPU·피벗·프레임 시간을 유지한다. 신규 미등록 32px 스프라이트는 기본 PPU 32다. 정리를 아트 승인이나 32px 축소 완료로 간주하지 않는다.
- 버전 이름은 원본 ZIP/출처와 과거 이력에만 남는다. C# MovedFrom의 이전 식별자는 직렬화 객체 연결을 위한 이력이며 활성 경로 이름이 아니다.

## 실행 및 검증

기존 빌드 씬은 ANIMOL_Starway_Art_QA를 유지한다. 현재 에디터 검수/Play 시작 씬은 ANIMOL_TwilightSea_Art_QA다. **플레이 가능한 탑뷰 퍼즐 구현은 아직 없다.** 이동·밀기·코스트·수집·승리를 구현했다고 표시하지 않는다. 물리 Root는 고정하고 시각 프레임만 재생하며 미연결 HUD는 COST -- / --/--를 표시한다.

현재 근거는 [이동 목록](Validation/SpriteLayout/move_manifest.json), [GUID·원본·명명 검사](Validation/SpriteLayout/audit.json), [EditMode](Validation/SpriteLayout/editmode.json), [PlayMode](Validation/SpriteLayout/playmode.json)다. 이전 전체 격리에서 17,539개 원본의 SHA-256을 확인한 기록은 Validation/ActiveSlice 및 레거시 README에 보존한다. 현재 경로는 이 문서와 SpriteLayout 기록을 따른다.

검수 도구는 Tools/SpriteLayout 및 Tools/DeviceArt에 모았다. Live PlayMode 테스트 동안 시작 씬 강제를 해제하고 실행 후 QA SceneAsset으로 복원한다. 현재 테스트는 ArtAssetTests / ArtQaPlayTests / DeviceArtTests / DeviceArtPlayTests다. 과거 로비/CommercialPolish 테스트는 레거시이며 활성 통과 수에 섞지 않는다. 장치 적용 근거는 [적용 결과](../ANIMOL_DEVICE8_V01_APPLY_RESULT.md)와 Validation/DeviceArt에 있다.

기억 극장 도구는 Tools/MemoryTheater, 테스트는 MemoryTheaterArtTests / MemoryTheaterArtPlayTests다. [기억 극장 적용 결과](../ANIMOL_MEMORY_THEATER_V01_APPLY_RESULT.md)와 Validation/MemoryTheater에 최종 전체 EditMode 13/13·PlayMode 4/4, 24상태/96전이 렌더 검증, 임시 Atlas 검사/제거 및 실제 표시 복구 근거가 있다. 실제 게임 판정 연결은 미완료다.

단일 수풀 3×3 조립의 작은 원본 알파 틈은 남은 아트 검토 항목이며 이번 파일 정리에서 그림을 수정했다고 주장하지 않는다.

편지 천문대 적용으로 PNG727·클립120이 추가돼 현재 정본 총량은 PNG2743·클립405다. 공통 변형은 Obstacles/common/letter_observatory/M##, 전용5종은 Obstacles/letter_observatory/M##, 바닥/벽/배경은 각 letter_observatory 테마 폴더다. 프리팹247개와 Data/Themes의 테마/광학 표시 자료를 실제 QA에 연결했다. Tools/LetterObservatory, Validation/LetterObservatory/Applied, LetterObservatoryArtTests/LetterObservatoryArtPlayTests 및 [결과](../ANIMOL_LETTER_OBSERVATORY_V01_APPLY_RESULT.md)에 전체 EditMode18/18·PlayMode6/6의 근거가 있다. 실제 게임 판정 호출자는 없고 광선 경로는 제공받은 표시 목록만 재생한다.

황혼의 바다 `twilight_sea`를 추가했다. PNG712·클립137로 현재 정본 총량은 PNG3455·클립542다. 공통4종/전용4종 경로는 Obstacles/common/twilight_sea/M## 및 Obstacles/twilight_sea/M##이며 Tiles/Props/Background도 해당 테마에 있다. Prefabs/Themes 270개 + Prefabs/Presentation 2개, 테마8종, QA307 표시 객체다. 나룻배20쌍은 하나의 재생 시각/승객 발 피벗을 공유하며 범위·해안은 공급 셀만 표시한다. 당시 검수 씬은 ANIMOL_TwilightSea_Art_QA이고 실제 게임 엔진은 없었다. [결과](../ANIMOL_TWILIGHT_SEA_V01_APPLY_RESULT.md), Tools/TwilightSea 및 Validation/TwilightSea에 EditMode23/23·PlayMode8/8·74캡처 근거가 있다.

## 2026-10-14 퍼즐과 별길 목장 하늘섬 활성 경로

이전 절의 게임 미구현 설명은 아트 격리 당시의 기록이다. 현재 `Scenes/ANIMOL_Puzzle.unity`에는 기존 탑뷰 스와이프 퍼즐이 있고 `Data/Puzzle/{Maps,Themes}`를 사용한다. 이동/연쇄/수집/출구 구현과 코스트 미연결 상태는 구분한다. 기존 Build Settings의 기본 Art QA 씬은 이번 외곽 적용에서 변경하지 않았다.

별길 목장 `StarGarden` 퍼즐 테마에는 PNG149개를 원본 그대로 추가했다. 경로는 `Sprites/Tiles/Floor/ranch/SkyIsland/{Grass,Underside}`, `Sprites/Background/ranch/SkyIsland`, `Sprites/Tiles/Decoration/ranch/SkyIsland`이다. `Data/Art/StarwayIsland`에 47쌍의 Tile/아트 카탈로그/검사 맵을 두고 `SkyIslandBuilder`가 가져온다. 두 Tilemap과 반복 배경은 표시 전용이며 기존 판정을 소유하지 않는다. 버전 이름은 출처 패키지에만 유지한다.

`Scenes/ANIMOL_Starway_Island_QA.unity`는 접합 그림 검사 씬이다. 플레이 동작 검수는 기존 `ANIMOL_Puzzle`에서 했다. [적용 결과](ANIMOL_STARWAY_SKYISLAND_RESULT.md), `Validation/StarwayIsland`, `Tools/StarwayIsland`에 실제 Game View의 1배/4배 접합 및 세 배경 좌표 픽셀 일치, 세로 플레이, EditMode63/63·PlayMode15/15 결과를 기록했다. 다른 테마/공통 장치, HUD, 기존 맵/씬을 교체하지 않았다.

## 2026-10-14 ?? ?? ?? ?? ??

?? ?? ?? PNG165?? `Sprites/Tiles/Floor/memory_theater/SkyTheater`, `Sprites/Props/memory_theater/SkyTheater`(Animation ??), `Sprites/Background/memory_theater/SkyTheater`? ????. Tile/??/?? ??/3? ???/?? ?? `Data/Art/MemorySkyTheater`, ?? ?? ?? ???? `Prefabs/Environment/MemoryTheater`?. ??? ?? ????? ???.

?? `ANIMOL_MemoryTheater_Art_QA`? ??? ???? 11? ???/369? ?? ????? ?????????? UI? ????. `ANIMOL_MemoryTheater_Environment_QA`? ?? ?? ??, `ANIMOL_MemoryTheater_Puzzle_QA`? ?? Small ?? ??? ??? ??? ??? ?? ???. ?? ?? ?/?? ???/HUD/?? ??? Build Settings? ???? ???.

[?? ??](ANIMOL_MEMORY_SKYTHEATER_RESULT.md), `Validation/MemorySkyTheater`, `Tools/MemorySkyTheater`? ?? SHA292/???165, ?? Game View 10? ?? ?? ??, ?? ????/??/??/??/??, EditMode73/73?PlayMode18/18 ??? ??. 11? ??? ?? ?? ??? ???? ?? ??? ??? ???? ? ???? ???? ???.


# CLOCK DOME 환경 적용

시계탑 돔 환경은 `Sprites/Tiles/Floor/letter_observatory/ClockDome`, `Sprites/Props/letter_observatory/ClockDome`, `Sprites/Background/letter_observatory/ClockDome`에 원본 PNG 116개를 보관한다. Data/Art/ClockDome과 Prefabs/Environment/ClockDome, ClockDome 런타임/Editor 코드가 이를 연결한다. 기존 편지 천문대 아트 QA 씬의 환경을 교체했고 별도 `ANIMOL_ClockDome_Environment_QA`와 `ANIMOL_ClockDome_Puzzle_QA`를 추가했다. 기존 장치·UI·원래 레벨·카메라 추적은 보존한다. 전체 EditMode 80/80, PlayMode 21/21, 실제 Game View 픽셀 비교 18개와 이동 검증은 [적용 결과](ANIMOL_CLOCK_DOME_RESULT.md)에 있다. 코스트와 편지 천문대 장치의 권위 있는 퍼즐 판정은 여전히 미연결이다.
