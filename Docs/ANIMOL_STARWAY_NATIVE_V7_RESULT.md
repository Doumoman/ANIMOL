# 별길 목장 Native Resolution v7 적용 결과

2026-10-19에 `MCP_INBOX/ANIMOL_Starway_Original_Resolution_v7.zip`을 실제 Unity 프로젝트의 첫 번째 테마 캐릭터에 적용했다. 테마와 세이브의 `starway` 키, 네 캐릭터의 `choa`/`uram`/`yeoul`/`tori` ID, 이동·점프·판정·콜라이더·카메라 로직은 유지했다. `uram`의 표시 이름만 **이솔**로 바꿨다.

## 적용

- Unity `AssetDatabase.MoveAsset`으로 기존 Art 루트를 `Assets/ANIMOL/Art/StarwayCharacters`로 옮기고, `Export/legacy_frame_path_map.json`의 360개 경로를 확인했다. 네 일러스트와 360개 프레임, 60개 기존 클립, Art 루트의 `.meta`/GUID를 보존했다.
- 원본 ZIP의 PNG 424개(프레임 360, 스트립 60, 일러스트 4)를 바이트 그대로 복사했다. 런타임 애니메이션은 개별 프레임 360개와 60개 클립으로 연결했다. 각 프레임의 원본 크기·PPU·피벗을 `starway_native_animations.json`에서 읽어 적용했고, 고정 160/320px 변환은 하지 않았다.
- 프레임은 Sprite Single, Point, FullRect, 무압축, Mipmap 끔, NPOT None으로 임포트했다. Android/iPhone 등의 기존 텍스처 축소·압축 오버라이드는 해제했다. UI용 전신 일러스트 네 개는 별도로 Bilinear, FullRect, 무압축으로 임포트했다.
- 캐릭터 카탈로그와 로비 첫 번째 테마에 새 일러스트를 연결했다. 기존 `Visual` 자식의 SpriteRenderer 재생 구조, 상태 전이와 왼쪽 `flipX` 동작을 유지했다. `Source`와 패키지 `Sheets`는 런타임 자산으로 복사하지 않았다. 이동 전 Art 루트에 이미 있던 검수용 시트는 옮긴 루트에 보존했다.

적용 전 자산과 카탈로그 백업은 Git 제외 경로 `Logs/starway_native_preapply_20261019.zip`에 있다. 테스트가 갱신한 기존 검수 캡처는 `Logs/starway_native_test_captures_20261019.zip`에 보관한 뒤 기존 파일로 복원했다.

## 검증

- Unity 6000.3.8f1 컴파일: 오류 없음.
- `StarwayCharacterArtTests`: 2/2 통과. 360개 Sprite의 크기·PPU·피벗·플랫폼 설정, 60개 AnimationClip의 프레임 바인딩, 네 캐릭터·테마·로비 연결을 확인했다.
- `StarwayCharacterPlayTests`: 3/3 통과. 네 캐릭터의 front/back/side/왼쪽 flipX에서 idle/move/sliding/jump/victory, 실제 퍼즐 선택과 재로드를 확인했다.
- `MainLobbyPlayTests`: 5/5 통과. 새 첫 테마 캐릭터 프리뷰와 전체 로비 동작을 확인했다.
- `MainLobbyArtTests`: 3/4 통과. 남은 실패는 이번 작업 범위 밖의 극장 테마 `Assets/ANIMOL/Sprites/UI/Main/Illustrations/seri.png`가 기존 Point 필터인 데 비해 Bilinear를 요구하는 검사다.
- `Docs/Validation/StarwayNative/Portrait`와 `Lobby`에 새 캐릭터·로비 캡처 11개를 남겼다. 초아의 클로버·귀·양갈래, 이솔의 모자·뿔, 여울의 리본·민트 양말·흰 꼬리, 토리의 귀·꼬리와 프레임 잘림을 확인했다.
- Android 기기 실행은 수행하지 않았다.
