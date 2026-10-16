# 기억 극장 캐릭터 160px v3 적용 결과

## 입력과 변경 범위

- 입력은 `MCP_INBOX/ANIMOL_MemoryTheater_160px_Characters_v3.zip`이다. SHA-256은 `2322454CED7221C7B839C8C700173A9DEA6CFCDC3F9C5D4BC99DEB4CD5E95E68`이다. 패키지의 `README.md`, `CLI_APPLY.md`, `memory_theater_animations.json`, `character_designs.json`, `Source/character_redesign_v3.json`, `Export/validation.json`을 확인했다. Manifest는 `version=3`, `outfitRevision=3`, `artRevision=mion-fox-piko-ashwaves-redesign-native160`이다. 미온의 무기 제거 지시를 원본 참고 사진보다 우선했다.
- 기존 루트 `Assets/ANIMOL/Art/MemoryTheaterCharacters160/`를 재사용했다. 런타임에는 개별 프레임 360장, 스트립 60장, 시트 12장, 일러스트 4장, manifest와 디자인 JSON만 둔다. `Source/`, `Previews/`, ZIP 전체는 복사하지 않았다. 게임 재생은 manifest의 개별 PNG `frames[0..5]`만 사용한다.
- v2와 파일별 SHA-256을 비교했다. 세리와 로안의 게임 프레임 180장, 스트립 30장, 시트 6장, 일러스트 2장은 기존 파일과 v3 패키지가 모두 같은 바이트다. 이번에 교체한 파일은 미온·피코의 게임 프레임 180장, 스트립 30장, 시트 6장, 일러스트 2장과 두 JSON이다. 최종적으로 테마 PNG 전부가 v3 패키지와 바이트 단위로 일치한다.
- Unity 재임포트 과정에서 반복 클립 36개의 `.anim` YAML에 `SpriteRenderer.m_Sprite` 바인딩 상수가 기록됐다. 이 중 세리·로안의 18개 클립은 참조하는 PNG와 재생 프레임이 기존과 같고, 일회성 클립을 포함한 60개 클립의 경로·GUID는 유지됐다.
- 기존 아트 루트의 `.meta` 584개는 적용 전후 바이트가 모두 같아서 PNG·클립 GUID가 유지됐다. 기존 카탈로그와 두 테마의 GUID 및 참조도 유지됐다. 다른 테마의 아트·카탈로그·프리팹은 수정하지 않았다.

## 인물, 재생 및 화면 연결

| 순서 | 저장/아트 ID | 인물 | v3 적용 |
|---|---|---|---|
| 1 | `seri` | 세리 | 승인 v2 PNG·일러스트 동일 바이트 유지 |
| 2 | `roan` | 로안 | 승인 v2 PNG·일러스트 동일 바이트 유지 |
| 3 | `mion` | 미온 | 은백 단발·붉은 눈·흰 여우 귀, 검정/짙은 붉은 기모노풍 옷, 두 흰 꼬리의 붉은 끝 장식, 빈손 |
| 4 | `piko` | 피코 | 애쉬베이지 웨이브·청록 머리핀, 흰색/하늘색 A라인 원피스, 책 모양 가방; 보이는 날개·동물 귀·꼬리 없음 |

`character_designs.json`의 인물 순서와 기존 `MemoryTheaterCharacters.asset`의 ID가 일치해서 별도 ID 변환이나 신규 인물 등록은 필요 없었다. 패키지 manifest의 클립 정렬은 `mion,piko,roan,seri`이나, 카탈로그의 사용자 선택 순서는 기존 `seri,roan,mion,piko`를 유지한다. 기본 ID는 `seri`다. 인물 능력·수치·스토리·해금·게임 판정은 변경하지 않았다.

`Assets/ANIMOL/Scripts/Editor/AnimolMemoryTheater160Importer.cs`의 기존 단일 임포터를 v3 검증으로 갱신하고 Unity의 `ANIMOL > Art > Import Memory Theater 160px Animations` 진입점으로 60개 `.anim`을 재생성했다. 각 커브는 `Visual` 객체 자신의 `SpriteRenderer.m_Sprite`에 빈 상대 경로로 바인딩된다. `MemoryTheaterCharacterBuilder.BuildBatch`를 이어 실행해 `Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset`의 60개 motion, 360개 frame 참조 및 4개 일러스트를 다시 확인·저장했다. 기존 GUID가 같아서 카탈로그·테마 YAML에는 실질적인 참조 변경이 없었다.

런타임은 `Assets/ANIMOL/Scripts/Runtime/StarwayCharacterVisual.cs`의 단일 프레임 재생기를 공유한다. `PuzzleController`와 `PuzzlePrototypeController`는 기억 극장 `PuzzleTheme.memoryTheaterCharacters`를 통해 이를 `Player/Visual`에 생성하고, `SelectMemoryTheaterCharacter(id)`로 교체한다. 기억 극장 QA 씬의 `MemoryTheaterCharacterQaController`가 동일 카탈로그의 일러스트와 포즈를 표시한다. 활성 프로젝트에 별도 선택/도감 화면은 없다. 고정 `PuzzleTheme.player` 및 다른 테마 목록은 유지했다.

`front`는 아래, `back`은 위, `side`는 오른쪽이며 왼쪽은 `side`와 `flipX=true`다. 다른 방향으로 전환하면 `flipX`를 해제하고, 멈추면 마지막 방향을 유지한다. idle/move/sliding은 각각 6/12/10fps 반복, jump/victory는 12/8fps 1회 뒤 기존 Visual 상태 제어로 idle에 복귀한다. Animator를 중복 부착하지 않았다.

## 크기와 임포트 설정

- 적용 전과 후 모두 개별 프레임 160×160, PPU 160, 발 기준 pivot `(0.5, 0.125)` = `(80,20)px`, `Visual.localScale=(0.85,0.85,0.85)`이다. 프레임 월드 크기는 1×1, Visual 표시 크기는 0.85×0.85 월드 단위로 동일하며 배율 보정량은 0이다. `Player` 루트 스케일은 `(1,1,1)`이다. 루트·콜라이더·이동·격자·카메라 크기를 확대하지 않았다.
- 게임 프레임 360장의 Unity 설정은 Sprite/Single, Point, Compression None, Crunch 끔, Mipmap 끔, FullRect, PPU 160, NPOT None, Android override RGBA32·비압축이다. v3 일러스트 4장은 기존 별도 설정인 Single·Bilinear·비압축·Android RGBA32를 사용한다. 활성 캐릭터 아트에 적용되는 SpriteAtlas는 없다.
- 패키지와 프로젝트의 프레임 360장 모두 원본 RGBA 160×160이고, 60개 스트립은 RGBA 960×160, 12개 시트는 RGBA 960×800이다. 전체 개별 프레임의 알파 경계 최소 여백은 좌/상/우/하 `5/6/4/19px`이다. Unity 재생 캡처에서 미온·피코의 귀·머리·꼬리·가방·신발 잘림이 없고, 후면에 앞얼굴이 없다. 미온은 손 소품이 보이지 않고 꼬리 둘만 붉은 끝 장식을 유지한다. 피코의 후면에는 앞 가슴 리본이나 가방 앞 금색 판이 복사되지 않았다. 원본 RGB와 반투명 윤곽을 변환하지 않았다.

## Unity 실행 검증

- 에디터 `6000.3.8f1`의 독립 배치 실행으로 임포트와 재연결을 완료했다. 로그 `Logs/animol-memorytheater-160-v3-import.log`, `Logs/animol-memorytheater-160-v3-connect.log`는 모두 컴파일 오류 없이 `Exiting batchmode successfully`로 끝났다. 결정적 로그 발췌는 `Docs/Validation/MemoryTheaterCharacters160_v3/unity-operation-excerpts.txt`에 보관했다.
- 대상 EditMode `MemoryTheaterCharacterArtTests` **2/2 통과**: 60개 클립, 360개 프레임 순서와 실참조, 커브 바인딩, FPS/loop, 임포트 및 Android 설정, 카탈로그·테마 참조. 대상 PlayMode `MemoryTheaterCharacterPlayTests` **3/3 통과**: 4인×3방향×5상태 60포즈와 좌향 반전 20포즈의 6프레임 재생, 반복과 1회 종료, 퍼즐 씬의 선택/스폰, 프로토타입 선택, 씬 재로드 후 참조. 전체 EditMode **132/132**, 전체 PlayMode **33/33 통과**. NUnit XML은 `Docs/Validation/MemoryTheaterCharacters160_v3/`에 있다. 전체 테스트에는 다른 테마 검사가 포함된다.
- 같은 폴더의 `Portrait/`에는 Unity PlayMode에서 캡처한 1080×1920 세로 Game View 12장이 있다. 4인 정면·후면, 변경된 두 인물의 측면, 퍼즐 씬의 세리·미온을 확인했다. 캐릭터 QA는 미리보기 씬이며 실제 퍼즐 기능으로 보고하지 않는다. 패키지 `Export/validation.json`은 제작 단계 검사이고, 이 Unity 결과와 별개다.
- 기억 극장 퍼즐·프로토타입·캐릭터 QA 3개 씬을 포함한 Android APK 빌드가 성공했다. 실제 APK는 `%TEMP%/ANIMOL_memory_theater_160_v3.apk`, 43,911,984바이트, SHA-256 `25AB9A8284EA0188FD668C24464CF552098E8AC7DFDE554EA9F71232B6A66CEC`이다. 로그는 `Logs/animol-memorytheater-160-v3-android.log`다. `adb devices -l`에 연결 기기가 없어서 실기기 설치·세로 화면·조작 검수는 **미검증**이다. 에디터 세로 Game View와 APK 빌드 검증만 완료했다.

수치와 파일별 비교 결과는 `Docs/Validation/MemoryTheaterCharacters160_v3/validation.json`에 기록했다. 남은 문제는 연결된 Android 기기에서의 실기기 검수뿐이다.
