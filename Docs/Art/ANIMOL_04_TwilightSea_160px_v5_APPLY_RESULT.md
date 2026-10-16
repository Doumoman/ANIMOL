# 황혼의 바다 캐릭터 160px v5 적용 결과

## 입력과 적용 범위

- 입력은 `MCP_INBOX/ANIMOL_TwilightSea_160px_Characters_v5.zip`이다. SHA-256은 `0AEC18F6504884BC041D101F57DA363276B048273E232464A659B1094B76FC42`이다. ZIP 안의 `twilight_sea_character_pack_160_v5/`를 제거한 뒤 `Assets/ANIMOL/Art/TwilightSeaCharacters160/`에 배치했다. `twilight_sea_animations.json`의 `version=5`, `outfitRevision=5`, `artRevision=haerin-yellow-white-aqua-native160`을 확인했다.
- 패키지의 `README.md`, `CLI_APPLY.md`, manifest, `character_designs.json`, `eden_costumes.json`, `Export/validation.json`, `Source/stranger_redesign_v4.json`, `Source/haerin_recolor_v5.json`과 Unity 임포터를 확인했다. `Source/HistoricalV3/`, `Source/HistoricalV4/`는 현행 해린의 색상 기준으로 사용하지 않았다. 패키지의 `Export/validation.json`은 제작 단계 결과이며 아래 Unity 실행 결과와 구분한다.
- 런타임 루트에는 manifest가 참조하는 개별 RGBA 160×160 프레임 **450장**, RGBA 960×160 스트립 **75장**, RGBA 960×800 방향 시트 **15장**, 확정 일러스트 **5장**, manifest와 두 인물/의상 JSON만 넣었다. `Source/`, `Previews/`, ZIP 전체는 복사하지 않았다. 원본과 배치한 이 548개 파일의 SHA-256 불일치는 **0개**다. 일러스트 원본은 RGB 4장과 RGBA 1장이며 색/알파를 재가공하지 않았다. 별도 검증 수치는 [validation.json](../Validation/TwilightSeaCharacters160_v5/validation.json)에 있다.
- 적용 전에 `git status --short`로 확인한 사용자 작업 **733개 경로**는 보존했다. 기존 활성 황혼의 바다 160px 캐릭터 루트와 카탈로그는 없었으므로 새 경로의 GUID를 생성했다. 이미 존재하던 두 황혼의 바다 `PuzzleTheme.asset` 및 `.meta`의 GUID는 유지했다. 다른 테마의 아트·카탈로그·씬을 교체하지 않았다. 패키지 설명은 이방인·단우·이든 아트가 v4와 같다고 명시하지만, 이 작업 폴더에 v4 ZIP이 없어 그 버전과의 바이트 동일성은 독립 검증하지 못했다.

## 인물과 실제 참조

| 카탈로그 ID | 표시 | assetSet / 의상 | 기본 여부 | 클립 / 프레임 / 일러스트 |
|---|---|---|---|---|
| `haerin` | 해린 | `haerin` / `default` | 테마 기본 인물 | 15 / 90 / 1 |
| `stranger` | 이방인 | `stranger` / `default` |  | 15 / 90 / 1 |
| `danu` | 단우 | `danu` / `default` |  | 15 / 90 / 1 |
| `eden` | 이든 | `eden` / `rabbit` | 이든 기본 의상 | 15 / 90 / 1 |
| `eden` | 이든 | `edenpuppy` / `puppy` | 이든 대체 의상 | 15 / 90 / 1 |

저장/인물 ID는 4개이고 `edenpuppy`는 다섯 번째 인물이 아니다. 이방인의 고유 이름·동물종·설정은 추가하지 않았다. 기본 해린과 이든의 토끼탈을 연결했고, 이든 강아지탈은 기존 카탈로그의 의상 데이터와 `SelectTwilightSeaCostume("puppy")` 또는 `PuzzleTheme` Inspector의 `twilightSeaCharacterId=eden`, `twilightSeaCostumeId=puppy`로 선택할 수 있다. 별도 의상 상점이나 능력·수치는 만들지 않았다.

`Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset`에 4개 인물의 기본 15개 motion과 이든 puppy 15개 motion 및 의상별 일러스트를 참조시켰다. `Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset`과 `Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset`만 이 카탈로그를 참조한다. 실제 플레이어는 `PuzzleController`/`PuzzlePrototypeController`가 스폰하는 `Player/Visual`의 `SpriteRenderer`와 기존 `StarwayCharacterVisual` 프레임 재생기를 사용한다. `SelectTwilightSeaCharacter`와 `SelectTwilightSeaCostume`은 같은 재생기와 일러스트 참조를 갱신한다. Animator를 중복으로 붙이지 않았다. 현재 활성 프로젝트에는 황혼의 바다 전용 캐릭터 선택·도감·일러스트 미리보기 UI가 없어서 새 화면은 만들지 않았다. 일러스트는 기존 카탈로그/Visual의 `Illustration` 접근 경로에서 의상별로 선택된다.

패키지 `AnimolTwilightSea160Importer`를 `Assets/ANIMOL/Scripts/Editor/AnimolTwilightSea160Importer.cs` 한 곳에 두고 `ImportBatch`를 Unity Editor **6000.3.8f1**에서 실행했다. 메뉴 `ANIMOL > Art > Import Twilight Sea 160px Animations`도 같은 진입점이다. 생성된 **75개 `.anim`**은 `Player/Visual` 객체 자신의 `SpriteRenderer.m_Sprite` 빈 상대 경로에 00~05 프레임을 묶는다. 실제 플레이는 이 클립이 가진 순서/FPS/반복 데이터를 기존 프레임 재생기에 연결한다. `front`=아래, `back`=위, `side`=오른쪽이고 왼쪽은 `side+flipX`이다. 다른 방향으로 바꾸면 flipX를 해제하며, 멈출 때 마지막 방향을 유지한다. idle/move/sliding은 6/12/10fps 반복, jump/victory는 12/8fps 1회 후 기존 Visual 상태 제어로 idle에 돌아간다.

## 임포트 크기와 디자인 검수

- 게임 프레임 450장만 Sprite/Single/Point/Compression None/Crunch 끔/Mipmap 끔/FullRect/PPU 160/pivot `(0.5,0.125)`=`(80,20)px`/NPOT None으로 설정했다. Android override는 RGBA32·비압축·최대 2048이다. 해당 활성 캐릭터에 적용되는 SpriteAtlas는 없었다. 스트립·시트·일러스트에 게임 프레임 설정을 일괄 적용하지 않았다.
- 이전 황혼의 바다 플레이어는 별도 캐릭터 프리팹이 아니라 `PuzzleTheme.player`의 공용 토끼 정지 그림이었다. 기존 PNG는 96×160, PPU 80, pivot `(0.5,0.05)`이고 기존 생성 경로의 표시 배율은 0.425로 월드 캔버스 **0.51×0.85**였다. 새 160×160/PPU 160 프레임은 기존 캐릭터 Visual 배율 **0.85**로 월드 캔버스 **0.85×0.85**다. 따라서 세로 표시 크기는 유지하고 가로 실루엣은 원본 비율대로 넓어졌다. 두 축을 모두 옛 토끼 폭에 맞추면 새 아트가 찌그러지므로 비균일 배율은 사용하지 않았다. 새 전체 프레임의 불투명 영역 최소 여백은 왼쪽 5/위 4/오른쪽 9/아래 19px이고 경계에 닿는 프레임은 0장이다. 발 pivot은 아래 20px이므로 실제 하단 그림과 차이는 최대 약 0.0053 월드 단위다. `Player` 루트는 `(1,1,1)`이며 콜라이더·이동·격자·카메라 크기는 바꾸지 않았다.
- Unity PlayMode에서 5외형×3방향×5상태의 모든 6프레임, 추가 왼쪽 flipX, 반복/1회 종료를 재생·확인했다. 1080×1920 Game View 17장 중 15장은 각 외형의 정면/후면/측면이고 2장은 실제 해안 퍼즐에서 이방인과 강아지탈 이든을 선택한 화면이다. [Portrait 캡처](../Validation/TwilightSeaCharacters160_v5/Portrait/haerin_front_idle.png)와 같은 폴더의 이방인·단우·이든 캡처에서 해린의 노란 상의/흰 후드/아쿠아 모자, 이방인의 장미 모자·코트·꽃다발, 단우의 안경·배낭·앞 무릎 표현, 이든의 토끼/강아지 별도 외형을 확인했다. 후면에 앞얼굴이나 앞 라펠/배 패널을 복사한 모습, 장미·모자·머리·리본·꼬리·신발의 화면 또는 프레임 잘림은 관찰되지 않았다. 원본 RGB와 반투명 윤곽을 보존했고 제한 팔레트·디더링·발광 처리는 추가하지 않았다.

## Unity 실행 증거와 남은 검수

- 임포트 및 연결 배치가 컴파일 오류 없이 종료했다. 실행 로그 발췌는 [unity-operation-excerpts.txt](../Validation/TwilightSeaCharacters160_v5/unity-operation-excerpts.txt)에 있으며 원본은 `Logs/animol-twilightsea-160-v5-import.log`, `Logs/animol-twilightsea-160-v5-connect.log`다. 연결 배치 종료 직전에 Unity Pipeline 서버의 `ObjectDisposedException`이 한 번 기록됐지만, 자산 저장 후 정상 종료했고 뒤의 전체 테스트·Android 빌드가 성공했다. Unity Licensing의 토큰 갱신 실패 문구도 있었으나 컴파일/실행을 막지 않았다.
- 대상 EditMode **2/2**, 대상 PlayMode **2/2**, 전체 EditMode **134/134**, 전체 PlayMode **35/35** 통과했다. 대상 테스트는 `.anim` SpriteRenderer 바인딩과 450개 순서·임포트 설정, 4개 ID/5개 외형과 테마 연결, 실제 씬 스폰·선택·의상·씬 재로드·반복/1회 종료를 검증한다. 전체 테스트에는 활성 다른 테마의 ArtAsset/ArtQa 검수도 포함된다. NUnit XML 네 개는 [검증 폴더](../Validation/TwilightSeaCharacters160_v5/validation.json)에 함께 보관했다. 격리된 `Legacy`의 옛 CommercialPolishPolicyTests는 현 활성 테스트가 아니다.
- 기존 세로 카메라/UI 정책의 Unity Game View **1080×1920**에서 표시 크기와 선명도를 확인했다. 세 개 기존 황혼의 바다 씬을 포함한 Android APK 빌드가 성공했다(`Logs/animol-twilightsea-160-v5-android.log`). 실제 APK는 `%TEMP%/ANIMOL_twilight_sea_160_v5.apk`, **47,228,979바이트**, SHA-256 `6E2F2AFF0B00D68FE0DA86E0DF95B8A6F44EECA2FF11A2EBA1D52E9174A62596`이다. Unity 로그의 640,605,472바이트는 BuildReport totalSize이며 APK 파일 크기가 아니다. `adb devices -l`에 연결 기기가 **0대**라 설치·실기기 조작·화면 검수는 미실행이다.
