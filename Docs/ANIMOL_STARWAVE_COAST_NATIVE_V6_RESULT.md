# ANIMOL 별너울 해안 Native Resolution v6 적용 결과

## 적용

- `MCP_INBOX/ANIMOL_Starwave_Coast_Original_Resolution_v6.zip`의 프레임 PNG 450장, 클립 75개, UI 일러스트 5장을 `Assets/ANIMOL/Art/StarwaveCoastCharacters`에 적용했다. 기존 `TwilightSeaCharacters160` 폴더는 Unity `AssetDatabase.MoveAsset`로 이동하고, 기존 프레임·클립·일러스트 파일의 `.meta`를 유지했다.
- 이전 아트 루트, 레지스트리, 로비 데이터, 씬의 적용 전 백업은 `Logs/starwave_coast_native_preapply_20261019.zip`에 보관했다. 패키지의 `legacy_frame_path_map.json` 450개 경로는 모두 이전 경로와 새 상대 경로가 같았다.
- 백업과 비교해 프레임 450개, 클립 75개, 일러스트 5개 및 아트 루트의 GUID가 모두 유지됐다. 새 PNG 530개의 파일 바이트는 패키지와 일치한다.
- Native Importer는 프레임별 원본 크기·PPU·피벗을 적용하고, Single Sprite, Point, FullRect, 압축 없음, Mipmap 끔, NPOT None으로 임포트한다. Android/iPhone/Standalone/WebGL의 축소·압축 오버라이드를 해제한다. Player, Visual, Collider, 카메라 크기는 변경하지 않았다.
- 기존 네 캐릭터 ID와 `eden_*`/`edenpuppy_*` 클립 이름을 유지했다. `eden_costumes.json`을 사용해 이든 한 명의 기본 토끼탈과 대체 강아지탈을 연결했다. 다섯 일러스트는 캐릭터 카탈로그와 로비 선택 화면에 연결했다.
- 로비 표시 이름을 `별너울 해안`으로 바꾸고 리소스 ID `starwave_coast`와 기존 `twilight_sea` 별칭을 연결했다. 기존 테마 슬롯 ID `sea`, 캐릭터 ID, 세이브 키 및 퍼즐 로직은 유지했다.

## 검증

- Unity 6000.3.8f1 Editor에서 임포터 실행·컴파일 완료. `TwilightSeaCharacterArtTests` EditMode **2/2 통과**: 450 Sprite의 원본 크기·PPU·피벗·플랫폼 설정, 75 클립의 프레임 연결, 네 캐릭터와 이든 두 의상, 로비 별칭을 확인했다.
- `TwilightSeaCharacterPlayTests` PlayMode **2/2 통과**: 다섯 외형의 정면·후면·오른쪽·왼쪽 반전에서 idle/move/sliding/jump/victory를 실제 Visual로 재생하고 기존 퍼즐 선택 연결을 확인했다.
- `MainLobbyPlayTests` PlayMode **5/5 통과**: 다섯 일러스트를 포함한 로비 외형과 전환을 확인했다. `Docs/Validation/StarwaveCoastNative/Portrait`에 17장, `Docs/Validation/StarwaveCoastNative/Lobby`에 5장의 캡처를 보관했다. 검사로 다시 생성된 기존 공용 로비 캡처 22장은 적용 전 상태로 되돌리고 별도 백업 `Logs/starwave_coast_native_lobby_captures_20261019.zip`에 보관했다.
- `MainLobbyArtTests` EditMode는 **3/4 통과**했다. 실패한 한 건은 이번 해안 테마와 무관한 낙일 성소 세인 일러스트의 기존 Tight mesh 설정에 대한 FullRect 기대치다. 해당 아트는 변경하지 않았다.
- 토끼탈 후면의 얼굴 비표시, 강아지탈의 노란 몸·크림 배·파란 스카프, 해린의 상어 모자, 단우의 안경, 이방인의 꽃다발을 캡처로 확인했다. Android 기기 실행 검증은 수행하지 않았다.
