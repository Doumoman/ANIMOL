# 편지 천문대 캐릭터 160px v2 적용 결과

## 패키지와 변경 범위

- 입력은 `MCP_INBOX/ANIMOL_LetterObservatory_160px_Characters_v2.zip`이며 SHA-256은 `DDE1ABF8E745F9E3349C19F2399E7231E3C38F0EAEDB3D286B329B94165AEFE5`다. ZIP 최상위 폴더를 중첩하지 않고 새 루트 `Assets/ANIMOL/Art/LetterObservatoryCharacters160/`에 배치했다. `letter_observatory_animations.json`은 `version=2`, `outfitRevision=3`, `artRevision=lumi-ferret-darkbag-soyul-navy-native160`이다.
- `README.md`, `CLI_APPLY.md`, manifest, `character_designs.json`, `Source/character_redesign_v2.json`, `Export/validation.json`과 패키지 Unity 임포터를 읽었다. `Source/`와 `Previews/` 및 원본 ZIP 전체는 런타임 루트에 복사하지 않았다. 제작 단계의 `Export/validation.json`은 통과했지만 아래 Unity 실행 결과와 구분한다.
- 게임용 RGBA 160×160 프레임 **360장**, RGBA 960×160 스트립 **60장**, RGBA 960×800 방향 시트 **12장**, 확정 RGB 일러스트 **4장**, manifest와 인물 JSON을 배치했다. 개별 프레임은 manifest의 `frames[0..5]`만 사용한다. 패키지 원본과 배치한 438개 파일의 SHA-256 불일치는 **0개**다. [validation.json](../Validation/LetterObservatoryCharacters160_v2/validation.json)에 크기·파일 수·GUID·테스트 수치를 기록했다.
- 시작 시 `git status --short`의 사용자 작업 **733개 경로**를 확인하고 보존했다. 기존 활성 편지 천문대 160px 캐릭터 루트와 카탈로그는 없어서 이 테마의 새 GUID를 생성했다. 기존 `letter_observatory_Theme.asset.meta`와 GUID는 그대로다. 다른 세 테마의 캐릭터 아트·카탈로그·테마 자산은 수정하지 않았다. 패키지는 세인·다온의 v1 아트가 같은 바이트라고 설명하지만 현 작업 폴더에는 비교할 v1 통합 ZIP이 없어 독립적인 v1 바이트 대조는 하지 못했다.

## 인물 ID, 플레이어, 일러스트

| 순서 | 캐릭터 ID | 표시 이름 | 연결 |
|---|---|---|---|
| 1 | `sein` | 세인 | 기본 인물, 15클립·90프레임·일러스트 1장 |
| 2 | `daon` | 다온 | 15클립·90프레임·일러스트 1장 |
| 3 | `lumi` | 루미 | 페럿 포함 15클립·90프레임·일러스트 1장 |
| 4 | `soyul` | 소율(가칭) | 15클립·90프레임·일러스트 1장 |

`Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset`에 정확히 네 ID와 60개 motion, 360개 개별 Sprite 참조, 네 일러스트를 저장했다. `Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset`에서 이 카탈로그와 기본 ID `sein`을 참조한다. 실제 `ANIMOL_letter_observatory_Prototype` 씬의 `PuzzlePrototypeController`가 `Player/Visual`에 `SpriteRenderer`와 기존 `StarwayCharacterVisual` 프레임 재생기를 생성한다. `SelectLetterObservatoryCharacter(id)`는 같은 인물 ID의 프레임과 `Illustration` 참조를 갱신한다. `PuzzleController`에도 같은 테마 선택 진입점을 연결했다. Animator나 페럿 렌더러를 중복 추가하지 않았다.

소율은 `soyul`로 신규 최소 등록했고 `doyun`을 소율로 치환하지 않았다. 활성 `Assets/ANIMOL`의 기존 인물 카탈로그/저장 정의에는 `doyun` ID가 없으며, 레거시 프로젝트의 코드·JSON·에셋 검색에서도 현재 참조를 찾지 못했다. 레거시 파일·저장 데이터는 수정하지 않았다. `lumi`의 기존 박쥐 설정이나 펫 관련 능력/충돌/AI를 새로 만들지 않았다. 현재 활성 프로젝트에는 편지 천문대 전용 캐릭터 선택·도감·일러스트 미리보기 UI가 없으므로 새 화면은 만들지 않았다. 일러스트는 기존 카탈로그와 Visual의 참조 경로에서 선택된다.

패키지 `AnimolLetterObservatory160Importer`를 `Assets/ANIMOL/Scripts/Editor/AnimolLetterObservatory160Importer.cs` 한 곳에 배치하고, Unity Editor **6000.3.8f1**의 `ImportBatch`로 60개 `.anim`을 생성했다. 메뉴 `ANIMOL > Art > Import Letter Observatory 160px Animations`는 같은 임포터를 실행한다. 커브는 `Player/Visual` 객체 자신의 `SpriteRenderer.m_Sprite`에 빈 상대 경로로 묶는다. 현재 런타임은 이 클립의 프레임/FPS/loop를 기존 재생기에서 사용한다. `front`=아래, `back`=위, `side`=오른쪽, 왼쪽=`side+flipX`이고 다른 방향으로 돌아갈 때 flipX를 해제한다. idle/move/sliding은 6/12/10fps 반복, jump/victory는 12/8fps 1회 후 기존 Visual 상태 제어로 idle로 복귀한다. 정지 시 마지막 방향과 기존 정렬 순서를 유지한다.

## 임포트 설정, 표시 크기, 외형 확인

- 게임용 프레임 360장만 Sprite/Single/Point/Compression None/Crunch 끔/Mipmap 끔/FullRect/PPU 160/pivot `(0.5,0.125)`=`(80,20)px`/NPOT None으로 설정했다. Android override는 RGBA32·비압축·최대 2048이다. 사용 중인 활성 SpriteAtlas는 없었다. 스트립·시트·일러스트에 게임 프레임 설정을 전역 적용하지 않았다. 일러스트 4장은 원본 RGB를 유지하며 Sprite/Single/Bilinear/비압축으로 임포트했다.
- 이전 플레이어는 별도 캐릭터 프리팹 대신 `PuzzleTheme.player`의 96×160 공용 토끼 정지 Sprite(PPU 80, pivot `(0.5,0.05)`)였다. 기존 생성 경로의 균일 표시 배율은 0.425로 월드 캔버스 **0.51×0.85**였다. 새 160×160/PPU 160 프레임은 기존 캐릭터 `Visual.localScale=0.85`에서 월드 캔버스 **0.85×0.85**다. 세로 표시 크기를 유지했고 가로는 새 아트의 고유 비율에 따라 넓다. 폭까지 옛 토끼와 일치시키는 비균일 배율은 아트를 왜곡하므로 사용하지 않았다. `Player` 루트는 `(1,1,1)`이며 콜라이더·이동·격자·카메라 크기를 바꾸지 않았다. 새 프레임 전체의 불투명 영역 최소 여백은 왼쪽 7/위 4/오른쪽 5/아래 19px이고 캔버스 경계에 닿는 그림은 0장이다. 발 pivot의 20px과 실제 하단 그림의 최대 차이는 약 0.0053 월드 단위다.
- 패키지 시트·일러스트와 Unity 1080×1920 Game View [12장 캡처](../Validation/LetterObservatoryCharacters160_v2/Portrait/lumi_front_idle.png)를 직접 확인했다. 세인의 흰 가지뿔/둥근 안경/검은 케이프, 다온의 납작한 시계·장미 장식과 나방 날개, 루미의 어두운 가방·흰 앞머리·머리 위 페럿, 소율의 남색/푸른색 의상과 제비 꼬리가 보인다. 후면의 앞얼굴·안경 렌즈·가슴 장식 복사나 장식/뿔/날개/가방/페럿/신발의 프레임 잘림은 관찰되지 않았다. 페럿은 루미와 같은 프레임과 방향 반전을 사용한다. 원본 단색 면·RGB·반투명 윤곽을 변경하는 팔레트 치환, 디더링, 추가 조명은 하지 않았다.

## Unity 실행 검증과 남은 항목

- 임포트·카탈로그 연결 배치는 컴파일 오류 없이 정상 종료했다. 로그 원본은 `Logs/animol-letterobservatory-160-v2-import.log`와 `Logs/animol-letterobservatory-160-v2-connect.log`이며 주요 줄은 [unity-operation-excerpts.txt](../Validation/LetterObservatoryCharacters160_v2/unity-operation-excerpts.txt)에 있다.
- 대상 EditMode **2/2**, 대상 PlayMode **2/2**, 전체 EditMode **136/136**, 전체 PlayMode **37/37** 통과했다. 대상 테스트는 60개 클립의 SpriteRenderer 바인딩·360개 프레임 순서·Android 임포트 설정과, 실제 프로토타입 씬에서 4인×3방향×5상태 및 왼쪽 반전의 6프레임 재생, 반복/1회 종료, 선택과 씬 재로드를 확인했다. 전체 테스트에 다른 활성 테마의 관련 검수가 포함된다. NUnit XML 네 개는 검증 폴더에 있다. 격리된 `Legacy`의 CommercialPolishPolicyTests는 현 활성 테스트가 아니다.
- 기존 세로 카메라/UI 정책의 1080×1920 Unity Game View에서 표시 크기와 선명도를 확인했다. 기존 편지 천문대 프로토타입·아트 QA 두 씬을 포함한 Android APK 빌드가 성공했다(`Logs/animol-letterobservatory-160-v2-android.log`). 실제 APK는 `%TEMP%/ANIMOL_letter_observatory_160_v2.apk`, **45,522,821바이트**, SHA-256 `8C88085B742CF10620A36CAF23A4C0BE7803BE10F90A85D5138C23AB9B35FF1E`다. Unity 로그의 612,185,458바이트는 BuildReport totalSize이며 APK 파일 크기는 아니다. `adb devices -l`에 연결 기기가 **0대**라 설치·실기기 조작·화면 검수는 미실행이다. Unity Licensing의 토큰 갱신 실패 메시지는 세 배치 로그에 있었으나 컴파일·테스트·빌드 결과를 막지 않았다.
