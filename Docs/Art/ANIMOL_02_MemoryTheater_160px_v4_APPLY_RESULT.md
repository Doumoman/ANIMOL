# 기억 극장 캐릭터 아트 160px v4 적용 결과

## 입력과 변경 범위

- 입력: `MCP_INBOX/ANIMOL_MemoryTheater_160px_Characters_v4.zip` (SHA-256 `02E3530526D09BE9937E24C5ACA227324BEAF6DB410FEE337DDFA09429FB2D3A`). `README.md`, `CLI_APPLY.md`, `memory_theater_animations.json`, `character_designs.json`, `Source/character_redesign_v3.json`, `Source/piko_redesign_v4.json`, `Export/validation.json`을 확인했다. Manifest는 `version=4`, `outfitRevision=4`, `artRevision=piko-mint-swimdress-bookbag-twintails-headstars-native160`이다. 제작 패키지의 `Export/validation.json`은 제작 단계 검사이며 아래 Unity 실행 결과와 구분한다.
- 기존 적용 루트 `Assets/ANIMOL/Art/MemoryTheaterCharacters160/`를 재사용했다. 런타임 루트에는 피코 PNG 109장(게임 프레임 90, 스트립 15, 방향 시트 3, 일러스트 1)과 manifest·인물 JSON 2개만 갱신했다. `Source/`, `Previews/`, 원본 ZIP은 복사하지 않았다. 전체 연결 수는 게임 프레임 360, 클립 60, 스트립 60, 시트 12, 일러스트 4이다.
- 기존 v3 ZIP 및 적용 루트와 SHA-256을 대조했다. 세리·로안·미온 PNG **327장은 바이트 동일**, 달라진 PNG **109장은 모두 피코**다. 따라서 이번 디자인 수정 대상은 피코 한 명이다. 세리·로안·미온의 클립·일러스트·내부 ID도 유지했다.
- 작업 시작 `git status --short`는 사용자 선행 변경 733항목이었다. 그 파일들과 다른 테마의 자산을 이번 커밋에 포함하지 않았다.

## 캐릭터와 실제 연결

| 선택 순서 | 내부 ID | 이름 | 적용 |
|---|---|---|---|
| 1 | `seri` | 세리 | v3 승인 이미지 그대로 |
| 2 | `roan` | 로안 | v3 승인 이미지 그대로 |
| 3 | `mion` | 미온 | v3 승인 이미지 그대로, 빈손·두 갈래 꼬리 유지 |
| 4 | `piko` | 피코 | v4 민트색 연결형 수영복 원피스, 분리된 긴 양갈래, 책 모양 백, 머리 앞쪽 별 선글라스 |

기존 ID와 패키지 ID가 같아 저장 ID 매핑 변경이나 신규 인물 등록은 필요 없었다. `Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset`의 네 인물·60 motion·360 Sprite·4 Illustration 참조는 기존 GUID를 통해 새 피코 파일을 읽는다. 두 기억 극장 `PuzzleTheme`의 `memoryTheaterCharacters`와 기본 ID `seri`도 유지했다. 별길 목장·편지 천문대·황혼의 바다 테마 목록은 변경하지 않았다.

실제 경로는 `PuzzleController` 및 `PuzzlePrototypeController`의 `SelectMemoryTheaterCharacter(id)` → 테마 카탈로그 → `StarwayCharacterVisual.Create` → `Player/Visual`의 `SpriteRenderer`다. 같은 Visual의 자체 프레임 재생기를 재사용했고 Animator를 중복 추가하지 않았다. QA 씬의 기존 선택 버튼과 일러스트 미리보기는 같은 카탈로그를 읽는다. 별도 도감 화면은 현재 활성 프로젝트에 없다.

## 임포트와 크기

- 패키지의 `Unity/Editor/AnimolMemoryTheater160Importer.cs`를 현재 단일 동명 임포터와 대조했다. 현재 임포터에는 Android RGBA32·무압축 및 pivot 검사가 추가되어 있어 유지하고 버전 검사를 v4로 갱신했다. Unity 배치 진입점 `Animol.Art.MemoryTheater160.AnimolMemoryTheater160Importer.ImportBatch`로 60 `.anim`을 재검증·갱신했고 `MemoryTheaterCharacterBuilder.BuildBatch`로 카탈로그·두 테마 연결을 재확인했다. 클립 바인딩은 Visual 자신에 대한 `SpriteRenderer.m_Sprite`다.
- 기존과 적용 후 모두 프레임 160×160, PPU 160, pivot `(0.5,0.125)` = `(80,20)px`, 프레임 월드 크기 1×1이다. `Player` 루트 배율은 1, `Visual.localScale`은 `(0.85,0.85,0.85)`이므로 표시 크기는 0.85×0.85 월드 단위다. 발 기준과 배율 보정량은 0이다. 콜라이더·이동·격자·카메라는 변경하지 않았다.
- 게임 프레임은 Sprite/Single, Point, FullRect, 무압축, Crunch·Mipmap 꺼짐, NPOT None이다. Android override는 RGBA32·무압축이며 160px를 줄이지 않는다. 일러스트는 별도 Single/Bilinear·무압축 설정이다. 활성 캐릭터 프레임용 SpriteAtlas는 없다.
- 루트의 `.meta` 584개를 전후 SHA-256 트리 해시로 비교했고 모두 동일했다: `bef8403933aae8c7f49aaf2787d3176846cb49a851745433e66473f74b0a4e3e`. 피코 프레임·일러스트 및 기존 60 `.anim` GUID가 유지된다. `.anim`의 참조 GUID가 같아 YAML 내용 변경 없이 v4 PNG로 재생된다.
- 방향은 front=아래, back=위, side=오른쪽, 왼쪽은 side+`flipX=true`다. 다른 방향에서 flipX가 풀리고 정지 시 마지막 방향을 유지한다. 6장씩 idle 6fps, move 12fps, sliding 10fps 반복; jump 12fps, victory 8fps 1회 후 기존 Visual 상태 제어로 idle에 복귀한다.

## Unity 실행 검수

- Unity Editor `6000.3.8f1` 배치 임포트와 연결이 컴파일 오류 없이 종료했다. 근거: `Logs/animol-memorytheater-160-v4-import.log`, `Logs/animol-memorytheater-160-v4-connect.log` 및 보관한 [실행 로그 발췌](../Validation/MemoryTheaterCharacters160_v4/unity-operation-excerpts.txt).
- 전체 [EditMode 결과](../Validation/MemoryTheaterCharacters160_v4/EditMode.xml) **136/136 통과**. 60 클립의 바인딩·FPS·반복 설정, manifest `frames[00..05]` 순서, 360 Sprite 임포트·Android 설정, 카탈로그·테마 참조 및 다른 테마 격리 검사를 포함한다.
- 전체 [PlayMode 결과](../Validation/MemoryTheaterCharacters160_v4/PlayMode.xml) **37/37 통과**. 기억 극장 QA 씬에서 4인×3방향×5상태(왼쪽 flipX 추가) 전체 프레임 재생, 반복 및 jump/victory 복귀를 확인했다. 실제 퍼즐 씬에서 피코 선택·스폰·이동 후 새 Sprite 표시, QA 씬 재진입 후 일러스트 유지, 프로토타입의 피코 선택도 통과했다. 전체 테스트에 다른 세 테마 검사도 포함된다.
- Unity PlayMode의 1080×1920 세로 Game View [캡처 12장](../Validation/MemoryTheaterCharacters160_v4/Portrait/)을 육안 확인했다. [피코 정면](../Validation/MemoryTheaterCharacters160_v4/Portrait/piko_front_idle.png), [후면](../Validation/MemoryTheaterCharacters160_v4/Portrait/piko_back_idle.png), [측면](../Validation/MemoryTheaterCharacters160_v4/Portrait/piko_side_idle.png), [퍼즐 스폰](../Validation/MemoryTheaterCharacters160_v4/Portrait/puzzle_piko.png)에 새 디자인이 보인다. 후면에는 얼굴과 정면 별 렌즈가 보이지 않고 두 머리 갈래 사이가 분리된다. 90개 피코 프레임의 알파 영역 최소 여백은 좌·상·우·하 `(13,9,20,19)px`이며 경계에 닿는 프레임이 없다. 머리·가방·신발 잘림은 확인되지 않았다. 원본 RGB·반투명 외곽을 재가공하지 않았다.
- Android APK 빌드 성공. `Logs/animol-memorytheater-160-v4-android.log`와 실행 로그 발췌에 결과가 있다. `%TEMP%/ANIMOL_memory_theater_160_v4.apk`는 43,813,128바이트, SHA-256 `E027861D7123028CBAA8DCEF6A1459D859C50D822FB97F0AFBF21C7B9BEAC2EB`이다. `adb devices -l`에 연결된 기기가 없어 **실기기 설치·화면·조작 검수는 미검증**이다. APK와 임시 로그는 커밋하지 않는다.

구조화된 수치와 검사 결과는 [validation.json](../Validation/MemoryTheaterCharacters160_v4/validation.json)에 기록했다.
