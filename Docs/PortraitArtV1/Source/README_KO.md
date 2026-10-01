# ANIMOL 세로 UI 스프라이트 팩 v1

참고 이미지의 따뜻한 목재 테두리, 크림색 판면, 잎·작은 꽃, 청록/분홍/보라 포인트를 기준으로 제작했습니다. 버튼과 카드에는 글자를 굽지 않았습니다. `Previews/`의 자주색 배경, 점선 영역, 수치·문구는 배치 예시이며 Unity에 불러올 UI 스프라이트가 아닙니다.

## 폴더

- `UnityImport/Sprites/`: 개별 RGBA PNG 10개. 표준 크기로 크롭·Point용 알파를 정리했습니다.
- `UnityImport/Fonts/pixelroborobo.otf`: 사용자가 제공한 원본 폰트. TMP 글자 레이어에 사용합니다.
- `Previews/`: 시작·모드 선택 화면의 1080×1920 및 1080×2400 예시 4장.
- `ui_manifest.json`: PNG 크기, import 설정, RectTransform 값, 이벤트 역할.
- `MCP_APPLY_PROMPT_KO.md`: Unity MCP에 전달할 명령문.

## 최종 화면 배치 (모든 수치는 SafeArea 내부, 1080 너비 기준)

화면의 배경과 캐릭터는 별도의 아트 슬롯입니다. UI가 그 위에 추가되는 구조이며, 가져올 수 있는 배경 PNG는 이 팩에 포함하지 않았습니다. 1080×2400에서는 상·하단에 붙은 요소의 거리를 유지하고 중앙 슬롯 높이만 확장합니다.

| 화면 | 요소 | 정렬/피벗 | anchoredPosition | sizeDelta | 텍스트/동작 |
|---|---|---|---|---|---|
| 시작 | PlayButton | 아래 중앙 / (0.5,0) | (0,180) | 640×228 | TMP `플레이` / 모드 선택 열기 |
| 모드 | BackButton | 위 왼쪽 / (0,1) | (32,-34) | 96×96 | 시작 화면으로 |
| 모드 | CurrencyCapsule | 위 왼쪽 / (0,1) | (144,-34) | 320×96 | 동전 아이콘과 현재 재화값 TMP |
| 모드 | AdButton | 위 오른쪽 / (1,1) | (-236,-34) | 96×96 | 광고 보상 연결 시만 활성화 |
| 모드 | ShopButton | 위 오른쪽 / (1,1) | (-134,-34) | 96×96 | 기존 상점 화면 |
| 모드 | SettingsButton | 위 오른쪽 / (1,1) | (-32,-34) | 96×96 | 기존 환경설정 화면 |
| 모드 | CampaignCard | 아래 중앙 / (0.5,0) | (0,690) | 832×272 | TMP `캠페인` / 캠페인 허브 |
| 모드 | CompetitionCard | 아래 중앙 / (0.5,0) | (0,402) | 832×272 | TMP `경쟁전` / 기존 경쟁 허브 |
| 모드 | CooperationCard | 아래 중앙 / (0.5,0) | (0,114) | 832×272 | TMP `협동` / 기존 협동 허브 |

`Start/CharacterBackdropSlot`: 화면 좌우 8%, 아래 500px~위 100px. `ModeSelect/CharacterBackdropSlot`: 화면 좌우 8%, 아래 1100px~위 200px. 둘 다 기본 상태에서 빈 RectTransform이고 raycastTarget은 꺼둡니다. 월궁 등 배경과 캐릭터는 나중에 별도로 끼웁니다. 모드 카드 텍스트는 아이콘을 피하도록 카드 왼쪽에서 약 265px 이후에 배치합니다.

## Unity 가져오기

1. ZIP의 `UnityImport` **안쪽 두 폴더**를 `Assets/ANIMOL/UI/PortraitArtV1/` 아래로 복사합니다. ZIP 전체와 Preview는 Assets에 넣지 않습니다.
2. PNG별 Texture Type=Sprite (2D and UI), Sprite Mode=Single, PPU=32, Filter Mode=Point, Compression=None, Generate Mip Maps=Off, Alpha Is Transparency=On, Mesh Type=Full Rect, Pivot=Center. UI Image Type=Simple, Preserve Aspect=On. 전체 그림을 가로 9-slice로 자르지 않습니다.
3. 기존 `UI_CommonRoot`의 `SafeArea/ScreenHost` 아래에 Start와 ModeSelect 레이어를 적용합니다. 실제 계층은 프로젝트의 현재 프리팹을 검사해서 찾습니다. 별도 Canvas를 중복 생성하지 않습니다.
4. TMP Font Asset은 제공된 OTF에서 만듭니다. 앱의 실제 한글/영문 문구를 수록하고 누락 글자에는 fallback을 둡니다. 밝은 카드에는 진한 갈색 글씨를 사용합니다.
5. Button의 Normal/Highlighted/Pressed/Disabled는 동일 PNG의 tint/scale 반응으로 구성합니다. 버튼 원본 PNG마다 별도 상태 프레임을 요구하지 않습니다.

기존 재화 갱신 출처와 게임 모드 내비게이션을 그대로 사용합니다. 현재 보상형 광고 연결이 없다면 광고 버튼은 회색/비활성으로 표시하고 보상을 지급하는 것처럼 보이는 클릭 반응은 연결하지 않습니다. 스테이지/기존 QA 씬/T01-S01 지도 데이터는 이 UI 변경 범위가 아닙니다.

**제작 방식:** OpenAI 이미지 생성으로 개별 목재 UI 픽셀아트 원형을 만들고, 투명 영역 크롭·최근접 축소·알파 이진화만 후처리했습니다. 크기와 SHA-256은 manifest에 있습니다.
