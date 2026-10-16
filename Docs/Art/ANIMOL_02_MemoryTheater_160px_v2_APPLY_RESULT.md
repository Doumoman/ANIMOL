# 기억 극장 캐릭터 160px v2 적용 결과

## 입력과 적용 범위

- 입력: `MCP_INBOX/ANIMOL_MemoryTheater_160px_Characters_v2.zip` (SHA-256 `7D25E58E3BD7653D98924B27E8DD341947F0823BDBFABDEA61C532A0F2FE6B97`). `README.md`, `CLI_APPLY.md`, `memory_theater_animations.json`, `character_designs.json`, `Export/validation.json`을 확인했다. Manifest `version=2`, `outfitRevision=2`, `artRevision=reference-colors-piko-dress-native160`이다.
- 실제 적용 루트: `Assets/ANIMOL/Art/MemoryTheaterCharacters160/`. ZIP 최상위 폴더는 중첩하지 않았다. `Sprites/`의 개별 프레임 360장과 스트립 60장, `Sheets/`의 방향 시트 12장, `Illustrations/` 4장, manifest와 디자인 JSON만 복사했다. 이 438개 파일은 ZIP에서 추출한 원본과 SHA-256이 모두 일치한다. `Source/`, `Previews/`, ZIP 전체는 런타임 Art에 넣지 않았다.
- 패키지의 제작 검수 `Export/validation.json`은 360 RGBA 프레임, 60 스트립, 12 시트, 4 일러스트가 유효하다고 기록한다. 별도로 프로젝트의 360개 manifest 프레임을 열어 모두 RGBA·160×160이며 서로 다른 파일임을 확인했고 투명 여백 최소값은 4px이었다. Unity 실행 결과는 아래에서 별도로 기록한다.

## 캐릭터와 연결

| 디자인 JSON 순서 | 내부 ID | 이름 | Unity 카탈로그/일러스트 |
|---|---|---|---|
| 1 | `seri` | 세리 | `MemoryTheaterCharacters.asset` / `animol_memory_theater_seri_illustration.png` |
| 2 | `roan` | 로안 | 동일 카탈로그 / `animol_memory_theater_roan_illustration.png` |
| 3 | `mion` | 미온 | 동일 카탈로그 / `animol_memory_theater_mion_illustration.png` |
| 4 | `piko` | 피코 | 동일 카탈로그 / `animol_memory_theater_piko_illustration.png` |

Manifest의 clip 목록은 ID 사전순(`mion`, `piko`, `roan`, `seri`)이다. 기존 활성 프로젝트에는 기억 극장 4인의 저장용 캐릭터 ID/선택 목록이 없었다. 디자인 JSON의 ID를 그대로 등록했으며 별도 영문 ID나 능력·수치·스토리를 만들지 않았다. 기본 미리보기/스폰 ID는 `seri`다.

패키지 임포터 `Assets/ANIMOL/Scripts/Editor/AnimolMemoryTheater160Importer.cs`의 메뉴 `ANIMOL > Art > Import Memory Theater 160px Animations`를 배치 진입점으로 실행해 60개 `.anim`을 생성했다. 기존 같은 경로의 160px PNG/클립은 없었으므로 이 파일들의 GUID는 신규다. 기존 기억 극장 `PuzzleTheme.asset` 두 개의 `.meta`/GUID(`22f9e23bf98b8294e836f99f3eb5b589`, `40256f2ee19876b4b96d397ae7a49592`)는 유지했다. 다른 테마의 테마 자산·카탈로그·프리팹은 교체하지 않았다.

각 `.anim`은 manifest의 `frames[0..5]`만 사용하며 `Visual` 객체 자체의 `SpriteRenderer.m_Sprite`에 바인딩된다. 모든 360개 게임 프레임은 Single Sprite, Point, 무압축·Crunch 끔·Mipmap 끔·FullRect, PPU 160, pivot (0.5, 0.125), NPOT Scale None이다. Android override는 RGBA32·무압축·최대 크기 2048이다. 일러스트 4장도 원본 RGB를 압축하지 않도록 개별적으로 Single Sprite·Bilinear·무압축·Android RGBA32로 임포트했다. 활성 `SpriteAtlas`는 없다. 스트립·시트에는 프레임 전용 임포트 설정을 일괄 적용하지 않았다.

현재 플레이어 프리팹 대신 런타임 생성 경로가 사용된다. `PuzzleController`와 `PuzzlePrototypeController`가 기억 극장 테마의 `memoryTheaterCharacters` 카탈로그를 읽어 기존 단일 `StarwayCharacterVisual` 프레임 재생기를 `Player/Visual`의 `SpriteRenderer`에 생성한다. `StarwayCharacterVisual`은 두 테마가 공유하는 컴포넌트이며 별도의 중복 재생기를 붙이지 않았다. 기억 극장 프로토타입 테마 `Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset`와 퍼즐 미리보기 테마 `Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset`에 카탈로그와 기본 ID를 연결했다. `SelectMemoryTheaterCharacter(id)`로 같은 Visual에서 4인을 전환한다. 정적 토끼 `player` 필드는 기존 다른 미리보기/호환 경로를 위해 유지했다.

캐릭터 선택/도감 전용 활성 UI는 현재 없다. `Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Character_QA.unity`의 기존 아트 QA 방식에 맞춘 미리보기 UI가 네 인물의 일러스트, 방향, 동작을 카탈로그에서 직접 표시한다. 게임 규칙·해금·대전 데이터는 변경하지 않았다.

## 표시 크기와 재생

- 변경 전 기억 극장 실제 플레이어 표현은 토끼 정적 PNG 96×160, PPU 80, pivot (0.5, 0.05), Visual 배율 0.85였다. 토끼 캔버스의 표시 크기는 1.02×1.70 월드 단위였다. 이는 기억 극장 캐릭터 80px 자산이 아니라 호환용 토끼였다.
- 제공된 기억 극장 80px 규격(80×80/PPU 80)과 이번 160px 규격(160×160/PPU 160)은 둘 다 배율 0.85에서 0.85×0.85 월드 단위 캔버스를 만든다. Visual `localScale`은 0.85→0.85, Player 루트는 (1,1,1)로 유지했다. 새 발 피벗은 (80,20)px이며 월드의 뿌리 위치를 바꾸지 않았다. 기존 토끼와 신체 실루엣 크기는 다르지만 캐릭터 프레임 규격에 맞춘 배율 보정은 0이다. 콜라이더·이동·격자·카메라는 확대하지 않았다.
- `front`=아래, `back`=위, `side`=오른쪽이고 왼쪽은 `side` + `flipX=true`다. 다른 방향 선택 시 `flipX=false`로 돌아오며 정지 시 마지막 방향이 유지된다. idle/move/sliding은 6/12/10fps 반복, jump/victory는 12/8fps 1회 후 기존 표현 상태의 idle로 복귀한다. 퍼즐 상태 확정은 기존 컨트롤러가 맡는다.

## Unity 검수

- Unity Editor 6000.3.8f1. 임포트 로그: `Logs/animol-memorytheater-160-v2-import.log`; 최종 연결 로그: `Logs/animol-memorytheater-160-v2-connect-final.log`. 양쪽 모두 컴파일 오류 없이 배치 종료했다.
- 새 EditMode `MemoryTheaterCharacterArtTests` 2/2: manifest→360개 Sprite→60개 clip 커브의 참조/순서, FPS/loop, 모든 프레임·일러스트 임포트 설정과 Android override, 카탈로그·테마·일러스트 연결을 검사했다. NUnit XML은 `Docs/Validation/MemoryTheaterCharacters160/editmode-targeted.xml`에 보관한다.
- 새 PlayMode `MemoryTheaterCharacterPlayTests` 3/3: 검수 씬에서 4인×3방향×5상태 60개와 왼쪽 반전 20개를 각각 6프레임 재생, loop/1회 종료, 실제 기억 극장 퍼즐·프로토타입 스폰과 선택, 씬 전환 후 참조를 검사했다. NUnit XML은 `Docs/Validation/MemoryTheaterCharacters160/playmode-targeted.xml`에 보관한다.
- 전체 EditMode 132/132, 전체 PlayMode 33/33 (`Docs/Validation/MemoryTheaterCharacters160/{editmode-all,playmode-all}.xml`). 첫 전체 PlayMode는 새 QA 테스트가 씬 카메라를 남겨 기존 퍼즐 카메라 테스트 3개를 실패시켰다. 테스트 종료 시 씬 정리를 추가했고 재실행에서 모두 통과했다. 별길 목장 PlayMode 3개도 전체 재실행에서 통과했다.
- 1080×1920 세로 Game View 실촬영: `Docs/Validation/MemoryTheaterCharacters160/Portrait/`의 네 인물 정면·후면, 기억 극장 퍼즐의 세리·미온 캡처. 일러스트와 게임 스프라이트가 함께 표시되며 피코의 연결된 흰색/하늘색 A라인 원피스, 미온 옆 포니테일·귀·꼬리·신발의 여백, 네 인물 후면의 앞얼굴 부재를 눈으로 확인했다. 패키지 `Previews/`는 참고로만 보고 Unity 실행 근거로 사용하지 않았다.
- Android: 기억 극장 퍼즐·프로토타입·캐릭터 QA 3씬 검수 APK 빌드 성공 (`Logs/animol-memorytheater-160-v2-android-final.log`, 배치 정상 종료). APK는 `%TEMP%/ANIMOL_memory_theater_160_v2_final.apk`, 43,005,784 bytes, SHA-256 `EB234841D8A62B8D54FDB73CF5AC16FE3F8F2B9E6E1CAA5DC8A33575C7339A61`다. Unity SDK의 `adb devices -l`에는 연결 기기가 없어서 실기기 설치/실행은 미검증이다.

## 최종 검수 상태

- Android 검수 APK: 빌드 성공. Editor 실행과 Android 빌드는 각각 통과했다.
- 남은 항목: Android 실기기 화면과 조작은 연결 기기가 없어 검증하지 못했다.
