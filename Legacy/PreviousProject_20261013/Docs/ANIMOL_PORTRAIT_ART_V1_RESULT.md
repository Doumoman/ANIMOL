# ANIMOL 세로 UI 아트 v1 적용 결과

루트의 `ANIMOL_Portrait_UI_v1.zip`을 기존 Bootstrap·Lobby 진입 흐름에 적용했다. 시작 화면에는 플레이 버튼 하나만 표시하고, 누르면 기존 Lobby의 모드 선택 화면으로 이동한다. 중앙 배경·캐릭터 슬롯은 비어 있다. 광고와 온라인 모드는 현재 실제 서비스 상태에 맞춰 비활성으로 표시하며, 예시 잔액이나 보상은 만들지 않는다.

## 원본 확인과 가져오기

[README](PortraitArtV1/Source/README_KO.md), [배치 명세](PortraitArtV1/Source/ui_manifest.json), [미리보기 4장](PortraitArtV1/Source/Previews/)을 먼저 확인했다. ZIP의 자주색 배경·예시 문구·12,500 잔액은 레이아웃 참고용이므로 게임 자산으로 가져오지 않았다. 현재 화면의 어두운 기본 배경을 유지했다.

`UnityImport/Sprites`의 PNG 10개와 `UnityImport/Fonts/pixelroborobo.otf` 1개만 `Assets/ANIMOL/UI/PortraitArtV1/`로 가져왔다. 스프라이트 SHA-256은 manifest와 일치한다. ZIP·Preview·README·manifest는 Assets에 넣지 않았다. 새 스크립트·TMP 에셋·프리팹은 Unity 통합을 위해 같은 기능 폴더에 생성했다.

모든 스프라이트 설정은 **Single, Point, PPU 32, Uncompressed, Mipmap Off, Alpha Is Transparency, Full Rect, 중앙 Pivot**이다. UI Image는 Simple·Preserve Aspect이며 9-slice를 사용하지 않는다.

## 기존 구성과 설치 방식

실제 공통 루트는 `UI_CommonRoot` 자체가 Canvas이고 그 아래 `SafeArea/ScreenHost`가 있다. 별도 Canvas를 만들지 않는다. 기존 `UiNavigationService`, `BootstrapPresenter`, `CampaignUiPresenter`, `MetaUiPresenter`, `GrowthEconomyUiPresenter`, `MultiplayerUiPresenter`와 Safe Area 정책을 조사했다. [조사 기록](PortraitArtV1/inventory.txt)에 서비스 설정도 남겼다.

기존 공통 루트·UI 프리팹에는 이미 미커밋 편집이 있어 파일을 재작성하지 않았다. 새 `Start.prefab`, `ModeSelect.prefab`과 Resources 설치 프리팹이 **Bootstrap·Lobby에서만 실행 시 설치**된다. 편집 모드의 기존 씬 미리보기는 바뀌지 않으며 Play에서 확인할 수 있다. 기존 Lobby 인스턴스는 삭제하지 않고 실행 중 `PA1_LegacyLobby`로 이름을 바꿔 숨긴다. 새 모드 선택이 기존 `SC01_Lobby` 화면 ID를 사용하므로 기존 돌아가기 경로와 Presenter 참조를 유지한다.

시작 화면의 Play는 Bootstrap에서 기존 `BootstrapPresenter.Continue()`를 통해 Lobby를 연다. Lobby에서 시작 화면으로 돌아갔다가 Play를 누르면 같은 Canvas 안에서 모드 선택을 다시 연다. 상점·설정·캠페인 진입과 각 기존 Back 버튼의 복귀를 실제 Pointer Event와 GraphicRaycaster로 확인했다.

## 버튼과 서비스 연결

| 요소 | 실제 연결 | 현재 상태 |
|---|---|---|
| 플레이 | Bootstrap → Lobby 또는 Lobby 내부 모드 선택 | 활성 |
| 뒤로 | `PA1_Start` | 활성 |
| 캠페인 | `SC02_ThemeSelect` | 활성, 기존 콘텐츠 검사와 해금 규칙 유지 |
| 상점 | `SC11_Store` | 화면 진입 활성, 구매 미연결 상태 유지 |
| 환경설정 | `SC13_Settings` | 활성 |
| 경쟁전 | `SC05_CompetitiveHub`, 기존 `OperationalReadyEnabled` 조건 | 매치 서버 미연결로 비활성 |
| 협동 | `SC08_CoopHub`, 같은 운영 가능 조건 | 매치 서버 미연결로 비활성 |
| 광고 | 운영 광고 공급자가 없으므로 클릭 핸들러 없음 | 보이는 비활성, `미연결` 표시 |
| 코인 | 기존 `IRewardLedgerAdapter` 계약, 현재 `DisconnectedRewardLedgerAdapter` | `--`, `코인 조회 불가` |

`ExternalServiceConfiguration`의 계정·매치·구매·광고 연결 값은 모두 false였다. 운영 지갑 구현은 없고 별도의 DEV 승인 장부만 있었으므로 `DevVerifiedBalance`나 로컬 진행을 실제 코인으로 표시하지 않는다. `BindVerifiedLedger`는 DEV 장부를 거절하고 비개발 장부의 검증 잔액만 읽는다. 잔액 변화 갱신은 독립 테스트 장부로 확인했으며 이것을 실서버 연동으로 주장하지 않는다. UI는 광고 완료·코인 지급·차감을 호출하지 않는다.

버튼의 tint는 manifest 값을 사용하고 기존 일반 등급 `UiButtonFeedback`의 작은 반응만 재사용한다. 새 진동·카메라 효과·시간 변경·입력 차단을 추가하지 않았다. 비활성 카드에는 밝은 글자와 `서버 미연결` 문구를 표시한다.

## TMP와 레이아웃

카드 이름·안내·재화는 모두 **TextMeshProUGUI**이며 PNG에 문구를 굽지 않았다. ZIP에 포함된 OTF에서 `PortraitUI_Raster`와 `PortraitUI_Dynamic`을 만들었다. 원본 OTF는 기존 사용자 제공 파일과 바이트가 같다.

- 샘플 크기 16, Padding 2, `RASTER_HINTED`, Bitmap 셰이더, Point, 밉맵 없음.
- 현재 문구 609자 중 원본 지원 문자 **587자**를 1024×1024 정적 아틀라스에 수록했다.
- 미수록 한글은 같은 OTF의 512×512 Dynamic·Multi Atlas 폴백을 사용한다. 기호는 기존 Typography의 기호 폴백을 재사용한다. 최종 누락은 0자다.
- 일반 표시 크기: 플레이 80px, 카드 이름 64px, 섹션 제목 48px, 보조 문구·재화 32px. 10자를 넘는 큰 재화 수치는 영역을 늘리지 않고 16px 단계로 표시한다.

모든 버튼 anchor·pivot·position·size는 manifest에서 읽어 생성했고 EditMode 검사에서도 대조한다. 플레이 640×228, 카드 832×272, 상단 버튼 96×96의 RectTransform을 유지했다. 비활성 버튼은 기존 피드백의 0.985배 표시를 따르며 원래 Rect 크기는 유지된다. 카드 보조 문구는 목재 테두리와 겹치지 않도록 내부에서 33px 올렸다. 외부 버튼 배치와 터치 영역은 바꾸지 않았다.

두 `CharacterBackdropSlot`은 자식·Graphic이 없는 빈 RectTransform이다. 좌우 8%와 명세의 상·하단 여백을 사용하므로 긴 화면에서는 중앙 슬롯만 더 높아진다.

## 실제 Canvas 캡처

`ScreenCapture.CaptureScreenshotAsTexture`를 프레임 종료에 실행해 Overlay Canvas를 포함한 실제 Game View를 저장했다. PNG 옆 JSON에는 TMP 누락·넘침, 글자 크기, 버튼 좌표·화면 경계·활성 상태가 있다. Preview를 결과 캡처로 대체하지 않았다.

| 화면 | 1080×1920 | 1080×2400 |
|---|---|---|
| 시작 | [캡처](PortraitArtV1/Captures/Start_1080x1920.png) | [캡처](PortraitArtV1/Captures/Start_1080x2400.png) |
| 모드 선택 | [캡처](PortraitArtV1/Captures/ModeSelect_1080x1920.png) | [캡처](PortraitArtV1/Captures/ModeSelect_1080x2400.png) |
| Safe Area 모의 검사 | [캡처](PortraitArtV1/Captures/ModeSelect_SimulatedSafeArea96_1080x1920.png) | [캡처](PortraitArtV1/Captures/ModeSelect_SimulatedSafeArea96_1080x2400.png) |

[Captures](PortraitArtV1/Captures/)에는 기존 캠페인·상점·설정 진입 증거도 포함해 총 12장이 있다. `SimulatedSafeArea96`은 상·하단 96px을 제외한 **에디터 모의 검사**이며 실제 노치 기기 측정은 아니다. `*_navigation.txt`는 최상위 레이캐스트 대상, 활성 버튼의 이동·복귀, 비활성 클릭의 무동작을 기록한다.

## 검증과 보존

시작 커밋은 `bffbeb42c85bb02b63ab8434b3471682955c409d`이며, [기준 기록](PortraitArtV1/baseline.json)에 기존 미커밋 파일 1,039개와 보호 대상 해시를 남겼다. T01-S01 수동 맵, MoonGraphicsQA 씬, 기존 공통 프리팹·스크립트는 저장하거나 수정하지 않았다. 이번 모듈은 월궁 씬에 설치되지 않는다.

최신 컴파일은 성공했다. [검증 요약](PortraitArtV1/final-verification.json), [EditMode XML](PortraitArtV1/final-editmode.xml), [PlayMode XML](PortraitArtV1/final-playmode.xml)을 함께 보관한다.

- EditMode **171건 중 165건 통과**. 실패 6건은 이전 폰트 작업 결과와 테스트 이름·실패 메시지가 일치한다. 수동 맵과 과거 조합 수·타일 수·객체 ID·백업 해시 기대값의 차이이며 새 실패는 없다.
- PlayMode **85/85 통과**. 새 UI 검사 **6/6**, `CommercialPolishPolicyTests` **3/3** 통과.
- 실제 Canvas 캡처 **12장**, 모두 지정 해상도이며 글자 누락·영역 넘침 **0건**. 새 모드 선택의 활성 상단 버튼 화면 영역은 두 해상도에서 **96×96px**이다. Play Rect는 640×228이며 에디터 hover 캡처에서는 기존 1.025배 피드백이 반영될 수 있다.
- 기존 미커밋 파일 **1,039개 바이트 보존**, T01-S01·MoonGraphicsQA 보호 해시 일치.

배치 PlayMode의 입력 검사는 기본 640×480 호스트 대신 1080×1920 카메라 렌더 타깃을 사용하며, 실제 세로 Overlay 확인은 위 캡처로 별도 수행했다. 캡처용 백그라운드 실행 설정은 시작 당시 값으로 복원했고, 전체 테스트가 다시 쓴 주행 기록은 이번 실행의 차이만 원복했다. 새로 생성된 테스트 자동 백업 6개는 `Logs/PortraitArtV1TestBackups/`로 옮겨 복구 가능하게 보관하고 커밋에서 제외했다. EditMode 실패 시 CLI가 라이선스 토큰 경고도 출력했지만 NUnit 검사 결과는 정상 생성되었다.

```powershell
unity test . --mode EditMode --output Docs/PortraitArtV1/final-editmode.xml --timeout 600 -- -logFile Logs/portrait-art-edit.log
unity test . --mode PlayMode --output Docs/PortraitArtV1/final-playmode.xml --timeout 600 -- -logFile Logs/portrait-art-play.log
powershell -NoProfile -ExecutionPolicy Bypass -File Docs/PortraitArtV1/verify.ps1
```

## 변경 파일과 남은 작업

변경은 `Assets/ANIMOL/UI/PortraitArtV1/`의 제공 아트·폰트·새 프리팹·설치 코드·생성/캡처 도구·검사, 해당 폴더 메타 파일, `Docs/PortraitArtV1/` 증거와 이 보고서에 한정한다. 원본 ZIP은 수정하거나 커밋에 포함하지 않는다. 기존 미커밋 UI·맵 작업도 이번 커밋에서 제외한다.

확인은 `Bootstrap.unity`를 열고 Play하면 된다. `Lobby.unity`를 직접 Play하면 모드 선택부터 보인다. 별도 배경·캐릭터 아트, 실제 광고·온라인 매치·지갑 공급자, 실기기 노치 및 접근성 큰 글씨의 추가 조합은 미완료 범위다. 빈 슬롯을 임시 캐릭터나 가짜 서비스 화면으로 채우지 않았다. 이번 TMP 레이블은 고정 픽셀 크기를 사용하며 기존 legacy Text용 큰 글씨 설정을 자동 승계하지 않는다.

이번 기능은 최신 워크트리의 기존 코드·프리팹에 의존한다. 아직 커밋되지 않은 이전 작업을 이번 UI 커밋에 섞지는 않는다.
