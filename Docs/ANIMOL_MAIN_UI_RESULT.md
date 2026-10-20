# ANIMOL 메인 UI v8 적용 기록

## 적용 범위

- 전달 원본: `MCP_INBOX/ANIMOL_Main_UI_v8.zip`의 `ANIMOL_Main_UI_v8/`. 다운로드 폴더의 동명 ZIP은 다른 구성이라 사용하지 않았다.
- 활성 씬: `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity`. 기존 `MainLobbyController`와 `EventSystem`을 유지하고 `ANIMOL/UI/Apply Main Menu Skin` 도구로 표면과 아이콘을 연결했다.
- 공통 스타일: `Assets/ANIMOL/Data/UI/MainMenuSkin.asset`. 21개 독립 UI PNG와 글자가 있는 로고, 빈 타이틀 판을 `Assets/ANIMOL/Sprites/UI/Main/Chrome` 및 `Branding`에 추가했다. PNG 치수·SHA-256·9-slice border는 `MainMenuSpriteManifest.json`에 기록했다.
- 기존 배경 4장, 캐릭터 일러스트와 게임 캐릭터 카탈로그는 교체하지 않았다. `MainMenuCatalog.asset`의 4개 테마 ID, 도윤을 포함한 17명과 18외형, 이솔의 `uram` ID, 이든의 2외형을 유지했다.
- 로비 텍스트는 기존 컨트롤러의 직렬화된 `Text` 값을 유지하고 화면에는 공통 Galmuri14 TMP Font Asset을 쓰는 별도 `TMP_Text`를 표시한다. 로고 글자는 독립 Image다.
- 캠페인, 설정, 광고 제거와 대전/커스텀 안내는 하나의 기존 입력 컨트롤러를 사용한다. 배경 이동은 기존 단일 통합 배경에만 적용한다. 로비에서는 5초마다 기존 대각선 전환으로 테마와 인물이 바뀐다. 설정 또는 캠페인 복귀 자체는 재추첨을 일으키지 않으며, 팝업과 캠페인이 열린 동안 전환 타이머가 멈춘다.
- 메인 하단 살구색 메뉴 전체를 210에서 148 UI 단위 높이(약 70%)로 줄이고, 아트 영역 하단도 같은 위치로 내려 배경이 62 단위 더 보이게 했다. 캠페인/대전/커스텀과 광고 행은 각 44 단위다. 노란 시작 버튼은 축소 직전의 55 단위 높이로 되돌리고 좌우 끝을 위쪽 버튼 행의 바깥쪽에 맞췄다. 캠페인 계속 버튼의 높이는 60에서 52로 줄였다.

## 현재 연결 상태

- 필수 스테이지 정책 ID가 아직 없어 대전은 잠금이다. 테스트의 임시 진도 공급자로 unknown/실패/미완료/완료/재잠금을 확인했으며 실제 진도를 쓰지 않았다.
- 광고, 구매/복원, 매칭, 캠페인 스테이지 목록, 효과음 설정 서비스는 현재 활성 프로젝트에 연결되어 있지 않다. UI는 대기 안내를 표시하며 성공을 만들지 않는다.
- 스토리 완료에 따른 `낙일 성소` → `천구 날조국` 이름 변경을 확정할 실제 진도 공급자가 현재 없다. 기존 카탈로그 이름 `낙일 성소`를 유지하며 조기 해금하지 않는다.

## 복구

- 변경 전 씬·`.meta`·컨트롤러·생성기·카탈로그·빌드 씬 설정 원본은 Assets 밖의 `Docs/Backups/MainUIV7_20261021/`에 보관했다. `manifest.json`에 각 원본의 SHA-256을 기록했다.
- 복구할 때 이 백업의 동일 상대 경로를 프로젝트로 되돌리고 Unity에서 리임포트한다. 추가된 스킨 자산은 참조되지 않은 상태로 보존할 수 있다.
- 기존 씬 GUID `8136534c91872224c846f262a3b85134`, 카탈로그 GUID `db41f861f19c11b4e85ddade356fb6da`, 컨트롤러 GUID `4cd7e10b5a856824f8521488ea907a2d`는 적용 전후 동일하다.

## 검증 범위

- Unity 6000.3.8f1 에디터에서 컴파일 및 Play 화면의 1080×1920, 1080×2400, 캠페인과 설정 팝업을 확인했다. 하단 메뉴 축소 후 두 세로 크기에서 잘림과 배경 노출을 다시 확인했다. PlayMode는 18외형 원본, 배경 cover 네 끝, PC 창, 화면 전환/일시정지/모션 줄이기, 대전 잠금 정책, 누름/뗌 위치 복원을 검사한다.
- 노란 버튼 크기와 5초 자동 전환 복원 후 `MainMenuSkinTests` 2/2, `MainLobbyPlayTests` 6/6 통과했다. 이전 v8 적용 과정에서 `MainLobbyArtTests` 4/4, `ArtAssetTests` 4/4, `ArtQaPlayTests` 1/1 통과했다.
- 실제 Android 기기, 광고·결제 SDK, 서버 매칭과 구매 복원은 실행 검증하지 않았다.
