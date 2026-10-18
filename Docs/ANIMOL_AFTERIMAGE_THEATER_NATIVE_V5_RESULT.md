# 잔영 극장 Native Resolution v5 적용 결과

2026-10-19에 `MCP_INBOX/ANIMOL_Afterimage_Theater_Original_Resolution_v5.zip`을 실제 Unity 프로젝트의 두 번째 테마 캐릭터에 적용했다. 표시 이름은 **잔영 극장 / Afterimage Theater**로 갱신했다. 새 정식 리소스 ID `afterimage_theater`와 기존 `memory_theater`를 같은 로비 테마로 조회할 수 있으며, 기존 퍼즐·세이브 키 `memory_theater`와 로비 키 `theater`는 유지했다.

## 적용

- Unity `AssetDatabase.MoveAsset`으로 기존 Art 루트를 `Assets/ANIMOL/Art/AfterimageTheaterCharacters`로 옮겼다. `Export/legacy_frame_path_map.json`의 360개 경로가 모두 같음을 확인하고, 프레임 360개·기존 클립 60개·일러스트 4개와 Art 루트의 `.meta`/GUID를 보존했다.
- ZIP의 PNG 424개(프레임 360, 스트립 60, 일러스트 4)를 바이트 그대로 복사했다. 런타임은 개별 프레임 360개를 60개 클립에 연결한다. 프레임별 원본 크기·PPU·피벗은 `afterimage_theater_native_animations.json`에서 읽어 적용했고 160/320px 규격으로 변환하지 않았다.
- 프레임은 Sprite Single, Point, FullRect, 무압축, Mipmap 끔, NPOT None이며 Android/iPhone/Standalone/WebGL의 텍스처 오버라이드를 해제했다. UI 전신 일러스트 4개는 Bilinear, FullRect, 무압축으로 임포트했다.
- 세리·로안·미온·피코의 기존 ID와 선택 슬롯을 유지하고 새 일러스트를 캐릭터 카탈로그와 로비에 연결했다. 로비 테마 이름과 QA 화면 제목을 갱신했다. 기존 `Visual` 자식 SpriteRenderer, 왼쪽 `flipX`, 입력·이동·점프·콜라이더·카메라 로직은 그대로 사용한다. 미온과 피코의 소품에 별도 Renderer를 추가하지 않았다.
- 패키지 `Source`와 `Sheets`는 런타임으로 복사하지 않았다. 이동 전 Art 루트의 검수용 시트는 보존했다.

적용 전 자산과 카탈로그·로비 씬 백업은 Git 제외 경로 `Logs/afterimage_native_preapply_20261019.zip`에 있다. 테스트가 만든 기존 로비 캡처는 `Logs/afterimage_native_lobby_captures_20261019.zip`에 보관한 뒤 원래 파일로 복원했다.

## 검증

- Unity 6000.3.8f1 컴파일: 오류 없음.
- `MemoryTheaterCharacterArtTests`: 2/2 통과. 360개 Sprite의 원본 크기·PPU·피벗·플랫폼 설정, 60개 AnimationClip 바인딩, 캐릭터·테마·로비·별칭 연결을 확인했다.
- `MemoryTheaterCharacterPlayTests`: 3/3 통과. 네 캐릭터의 front/back/side/왼쪽 flipX와 idle/move/sliding/jump/victory, 실제 퍼즐 선택·재로드를 확인했다.
- `MainLobbyPlayTests`: 5/5 통과. 두 번째 테마의 새 일러스트 4개와 로비 전환을 확인했다.
- `MainLobbyArtTests`: 3/4 통과. 남은 실패는 이번 작업 밖의 `Assets/ANIMOL/Art/SunsetSanctumCharacters/Illustrations/animol_sunset_sanctum_sein_illustration.png`가 기존 Tight 메시인 데 비해 FullRect를 요구하는 검사다.
- `Docs/Validation/AfterimageTheaterNative/Portrait`와 `Lobby`에 새 검수 캡처 16개를 남겼다. 세리의 회청색 눈·고양이 특징·양갈래·무대복, 로안의 호박색 눈·분홍 머리·검은 복장, 미온의 두 갈래 꼬리와 끝 장식, 피코의 민트 원피스·가방·머리 위 별 선글라스와 후면 방향을 확인했다. 패키지 원본 검증도 소스 가장자리 잘림이 없다고 보고한다.
- Android 기기 실행은 수행하지 않았다.
