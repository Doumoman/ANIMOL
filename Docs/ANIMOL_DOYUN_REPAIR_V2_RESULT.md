# 도윤 수정본 v2 적용 결과

- 입력: `MCP_INBOX/ANIMOL_Doyun_Repair_v2.zip`. 압축은 Assets 밖에서 풀었고 변경 전 도윤 파일·클립·메타·테마 JSON은 `C:/Users/user/Documents/GitHub/ANIMOL_Doyun_Before_20261019_163355/`에 백업했다.
- 적용 위치: `Assets/ANIMOL/Art/SunsetSanctumCharacters/`. 도윤 개별 PNG 90장, 스트립 15장, 시트 3장, 제작 원본 3장과 문서 1개, 일러스트 1장을 같은 상대 경로에 복사했다. `doyun_native_animations.json`과 Editor 전용 `AnimolDoyunRepairImporter.cs`를 추가했다.
- 전체 `sunset_sanctum_native_animations.json`에서 도윤 `frames.file` 90개, `clips.name` 15개, `sheets`의 도윤 3개만 교체했다. 전체 수는 450프레임·75클립·15시트이며 다른 네 캐릭터의 항목과 테마 설정은 그대로다.
- 열린 Unity 6000.3.8f1 Editor에서 `Animol.Art.DoyunRepairNative.AnimolDoyunRepairImporter.ImportFromFolder(themeRoot)`를 실행했다. 90프레임에 개별 PPU·커스텀 피벗, Bilinear, FullRect, mipmap 없음, Uncompressed, NPOT None을 적용했고 기존 15개 `.anim`을 같은 경로에서 갱신했다. 전체 성소 임포터를 다시 실행해도 도윤만 Bilinear로 설정되도록 했다.
- 활성 캐릭터는 `StarwayCharacterVisual`의 SpriteRenderer와 카탈로그 프레임 배열로 재생한다. Animator/OverrideController는 사용하지 않는다. 기존 클립 바인딩 `SpriteRenderer.m_Sprite`와 카탈로그 참조를 유지했다. 왼쪽은 side 원본의 `flipX`를 사용한다. 루트·카메라·콜라이더·UI 레이아웃은 수정하지 않았다.
- 기존 도윤 PNG·클립·일러스트의 `.meta` GUID 변경 0개. 로비와 캐릭터 카탈로그의 도윤 일러스트 참조는 같은 GUID `136b1de4f6b3c564ebe7c5eb1f4756e5`를 유지한다. 도윤 외 PNG·클립 496개의 SHA-256은 변경 전과 동일하다.

## 검증

- ZIP `Export/file_inventory.json`의 126개 파일 크기와 SHA-256 일치. 프로젝트에는 이전 공통 `Export/file_inventory.json`이 없어 다른 캐릭터의 인벤토리 기록을 수정하지 않았다.
- 새 일러스트 1199×1312, 투명 알파와 네 모서리 투명 확인. 90프레임 모두 불투명 픽셀이 캔버스 경계에 닿지 않으며 최소 여백은 3픽셀이다.
- 제공된 front/side/back 상태 시트를 육안 확인했다. 열린 눈 광채, 감은 눈과 뒷모습의 무광채, 발·손·제비 꼬리의 형태를 확인했다. idle 6프레임의 불투명 영역 중심 이동은 방향별 최대 0.0101유닛, 발 기준선 이동은 최대 0.0108유닛이다.
- Unity EditMode `SunsetSanctumCharacterArtTests` 2/2, `MainLobbyArtTests` 4/4 통과. 프레임 크기·PPU·피벗, 75클립 프레임 순서·FPS·loop·바인딩, 다섯 캐릭터와 로비 참조를 확인했다.
- Unity PlayMode 도윤 전용 네 방향×다섯 상태×6프레임 및 루트 크기 검사 1/1, 소율/도윤 선택과 씬 재로드 검사 1/1 통과.
- Editor Game View 캡처 도구가 정상 화면을 반환하지 않아 새 배치의 실제 화면 픽셀 육안 검수와 모바일 실기기 검증은 남아 있다. PlayMode의 SpriteRenderer 참조·상태 재생 검사는 실제 Unity에서 수행했다.
