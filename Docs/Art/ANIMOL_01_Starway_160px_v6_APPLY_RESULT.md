# ANIMOL 별길 목장 160×160 v6 캐릭터 아트 적용 결과

## 입력과 범위

- 입력: `C:\Users\user\Downloads\ANIMOL_Starway_160px_FlatColor_v6.zip` (같은 이름의 `(1)` ZIP과 SHA-256 동일). ZIP SHA-256은 [검증 요약](../Validation/StarwayCharacters160/validation.json)에 기록했다.
- `README.md`, `CLI_APPLY.md`, `starway_animations.json`, `Source/CHARACTER_DESIGN_v6.md`, `Export/validation.json`을 확인했다. `starway_animations.json`은 `version=6`, `outfitRevision=9`, `theme=starway_pasture`다. `Export/validation.json`은 제작 단계 검수 `passed=true`이며, 거기 적힌 `unityExecutionTested=false`와 `mobileExecutionTested=false`를 이번 Unity 검수 결과로 대체해 읽지 않았다.
- 선택 사항인 `character_designs.json`은 ZIP에 없다. 다른 테마나 구버전 아트로 채우지 않았다.
- 적용 루트: `Assets/ANIMOL/Art/StarwayCharacters160/`. ZIP 최상위 폴더 없이 manifest 상대 경로 그대로 복사했다. `Source/`, `Previews/`, 원본 ZIP은 Assets에 복사하지 않았다.
- 원본과 SHA-256이 일치하는 파일 437개: 게임 프레임 PNG 360개(각 160×160 RGBA), 동작 스트립 60개(960×160), 방향 시트 12개(960×800), 일러스트 4개, manifest 1개. 스트립·시트·일러스트는 게임 프레임 배열에 들어가지 않는다. 360프레임의 최소 투명 여백은 4px이며 원본 RGB·알파를 바꾸지 않았다.

## 인물과 실제 참조

| 순서 | 이름 | 아트/내부 ID | 클립 | 일러스트 |
|---:|---|---|---:|---|
| 1 | 초아 | `choa` | 15 | `animol_starway_choa_illustration.png` |
| 2 | 우람 | `uram` | 15 | `animol_starway_uram_illustration.png` |
| 3 | 여울 | `yeoul` | 15 | `animol_starway_yeoul_illustration.png` |
| 4 | 토리 | `tori` | 15 | `animol_starway_tori_illustration.png` |

활성 프로젝트에는 기존 4인 캐릭터 ID/카탈로그, 선택 화면, 도감 화면이 없었다. 새 ID를 저장 데이터에 강제 이식하지 않았으며 manifest ID를 그대로 사용한다. [StarwayCharacters.asset](../../Assets/ANIMOL/Data/Art/StarwayCharacters.asset)은 4명 각각의 일러스트, 15개 `.anim`, manifest 순서의 프레임 6장·FPS·반복 값을 참조한다. 기본 캐릭터는 초아다. 두 별길 테마인 [StarGarden.asset](../../Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset)과 [starway_Theme.asset](../../Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset)에만 카탈로그와 기본 ID를 연결했다. 다른 세 테마 카탈로그·프리팹은 변경하지 않았다.

Manifest에는 별도의 캐릭터 목록 순서가 없고 `clips`의 첫 등장 순서는 `choa, tori, uram, yeoul`이다. 카탈로그 표시 순서는 사용자 요청과 패키지 README의 `초아, 우람, 여울, 토리`를 사용했으며, 각 모션의 프레임 순서는 manifest 그대로 유지했다.

실제 플레이어는 기존 퍼즐의 동적 `Player` 루트 아래 `Visual/SpriteRenderer`를 만든다. [PuzzleController.cs](../../Assets/ANIMOL/Scripts/Runtime/PuzzleController.cs)와 [PuzzlePrototypeController.cs](../../Assets/ANIMOL/Scripts/Runtime/PuzzlePrototypeController.cs)는 별길 카탈로그가 있을 때 [StarwayCharacterVisual.cs](../../Assets/ANIMOL/Scripts/Runtime/StarwayCharacterVisual.cs)를 사용한다. 기존 격자 이동·수집·승리 권한과 카메라 추적 대상은 `Player` 루트에 남는다. 두 컨트롤러의 `SelectStarwayCharacter(id)`로 현재 세션의 표시 인물을 바꿀 수 있다. 현재 게임에는 이 메서드를 호출하는 사용자용 선택 화면이나 저장형 선택 시스템이 없다.

프로젝트의 식물용 `SpriteFramePlayer`는 `idle/touch` 두 상태 전용이므로 플레이어에 중복 부착하지 않았다. 캐릭터 Visual 하나가 카탈로그의 6장·FPS·loop를 재생한다. 생성한 60개 `.anim`도 같은 카탈로그의 각 모션에 연결되어 있으며 일반 Unity 클립으로 보관한다. 런타임의 프레임 전환은 이 카탈로그의 `frames` 배열을 사용한다. `front=아래`, `back=위`, `side=오른쪽`, 왼쪽은 `side + flipX`이며 나머지 방향에서는 flipX를 해제한다. 정지는 마지막 방향을 유지한다. `idle/move/sliding`은 6/12/10 fps 반복, `jump/victory`는 12/8 fps 1회 후 idle로 돌아간다. 점프 판정은 새로 만들지 않았고 승리 판정도 기존 세션만 결정한다.

[ANIMOL_Starway_Character_QA.unity](../../Assets/ANIMOL/Scenes/ANIMOL_Starway_Character_QA.unity)는 기존 아트 QA 용도의 미리보기 씬으로 4인, 방향, 상태와 현재 일러스트를 확인한다. 게임의 선택/도감 화면으로 취급하지 않는다. 여울은 v6 은백색 묶음머리, 큰 분홍 리본, 분홍/흰 원피스, 흰 귀·꼬리의 90프레임과 새 일러스트를 사용한다. [정면](../Validation/StarwayCharacters160/Portrait/yeoul_front_idle.png) 및 [후면](../Validation/StarwayCharacters160/Portrait/yeoul_back_idle.png) 캡처에서 후면 앞얼굴이 없고 리본·귀·꼬리·신발이 잘리지 않는다.

## 임포트와 표시 크기

- 패키지 [AnimolStarway160Importer.cs](../../Assets/ANIMOL/Scripts/Editor/AnimolStarway160Importer.cs)를 단일 Editor 클래스/메뉴로 두고 `ImportBatch`를 Unity 6000.3.8f1에서 실행했다. 60개 `.anim`을 생성·갱신했고 바인딩은 같은 `Visual` 객체의 `SpriteRenderer.m_Sprite` (`path=""`)다. 클립 커브는 프레임 `00`~`05`와 마지막 프레임 유지 키를 사용한다.
- 게임 프레임 360개를 `Sprite(2D and UI)`, `Single`, `Point`, `Compression=None`, Crunch 끔, Mipmap 끔, `FullRect`, PPU 160, pivot `(0.5,0.125)` = `(80,20)` px, NPOT None으로 검사했다. Android override는 RGBA32·무압축·최대 2048px이다. 활성 SpriteAtlas는 없다. 다른 PNG 종류에는 이 설정을 일괄 적용하지 않았다.
- 첫 임포트 로그의 GUID와 최종 `.meta`를 대조해 PNG 436개와 `.anim` 60개, 총 496개의 GUID가 재임포트 후 유지됨을 확인했다. 기존 동일 경로 자산은 없었고, 기존 다른 테마 `.meta`는 스테이징하지 않았다.
- 기존 실제 퍼즐 토끼는 96×160/PPU 80/pivot `(0.5,0.05)`이고 `Player` SpriteRenderer 루트 배율은 0.425여서 월드 표시 크기는 약 0.51×0.85다. 신규 캐릭터는 160×160/PPU 160/pivot `(0.5,0.125)`에 `Player` 루트 배율 1, `Visual` 자식 배율 0.85로 월드 높이 0.85를 유지한다. 새 정사각형 아트의 폭은 0.85로 넓어졌다. 비율을 강제로 찌그러뜨리지 않았으며 발 피벗은 패키지 기준 20px로 맞췄다. 플레이어 루트 좌표, 격자, 이동 거리, 카메라 크기, 콜라이더는 확대하지 않았다. 공용 `rabbit_front` QA 프리팹의 `Visual` 배율 1과 기존 정적 토끼는 유지했다.
- 다른 테마의 환경 미리보기는 별길 테마의 과거 정적 `player` 스프라이트를 복사해 참조한다. 이 정적 폴백을 그대로 유지해 해당 미리보기와 테스트를 보존했고, 실제 별길 플레이어는 위 카탈로그 Visual이 표시한다.

## Unity 검증

| 검증 | 결과 | 근거 |
|---|---|---|
| Unity 컴파일·임포트·카탈로그 연결 | 성공, 신규 C# 오류 없음 | [검증 요약](../Validation/StarwayCharacters160/validation.json)에 배치 로그 경로 기록 |
| 전체 EditMode | 130/130 통과 | [NUnit XML](../Validation/StarwayCharacters160/editmode-all.xml) |
| 전체 PlayMode | 29/29 통과 | [NUnit XML](../Validation/StarwayCharacters160/playmode-all.xml) |
| 최종 캐릭터 EditMode | 2/2 통과; 360개 프레임 설정·클립 커브·카탈로그 매핑 검사 | [NUnit XML](../Validation/StarwayCharacters160/editmode-targeted.xml) |
| 최종 캐릭터 PlayMode | 3/3 통과; 4인×3방향×5상태 60클립의 00~05 전 프레임과 왼쪽 flip 20포즈의 00~05 전 프레임, 반복·1회 종료, 퍼즐/프로토타입 스폰·전환·씬 재로드 | [NUnit XML](../Validation/StarwayCharacters160/playmode-targeted.xml) |
| 1080×1920 Unity Game View | 초아·우람·여울·토리 정면과 여울 후면, [퍼즐 초아](../Validation/StarwayCharacters160/Portrait/puzzle_choa.png)·[퍼즐 여울](../Validation/StarwayCharacters160/Portrait/puzzle_yeoul.png) 캡처 확인; 기존 HUD·카메라 정책 유지 | [Portrait 캡처](../Validation/StarwayCharacters160/Portrait) |
| Android APK 빌드 | 성공, 39,087,583 byte; 퍼즐·별길 프로토타입·캐릭터 QA 씬 포함 | [검증 요약](../Validation/StarwayCharacters160/validation.json), `Logs/animol-starway-v6-android-build.log` |
| Android 실기기 실행 | 미검증 | `adb devices`에 연결된 기기 없음 |

APK는 `C:\Users\user\AppData\Local\Temp\animol_starway_v6_validation.apk`에 생성했고 SHA-256을 검증 요약에 기록했다. 이 검수 빌드는 기존 Build Settings의 기본 씬 목록을 바꾸지 않았다. 원본 제작 패키지의 `unityExecutionTested=false`와 달리 위 Unity 결과는 프로젝트에서 실제로 실행한 결과다. 남은 범위는 실기기 확인과, 향후 사용자용 캐릭터 선택/도감 화면이 만들어질 때 이 카탈로그를 연결하는 작업이다.
