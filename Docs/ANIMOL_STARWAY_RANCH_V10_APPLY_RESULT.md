# 별길 목장 V10 적용 결과

## 적용 범위

- 패키지의 런타임 PNG 1,816개를 `Assets/ANIMOL/Sprites/StarwayRanch`에 원본 바이트로 추가했다. 기존 별길 전경 PNG와 병아리 원화는 덮어쓰지 않았다. `sprite_import_settings.json`의 파일별 PPU와 피벗을 전용 임포터로 적용했다.
- 기존 별길 테마와 별길 프로토타입 테마가 새 카탈로그를 참조한다. 실제 퍼즐 렌더러는 V10 organic256 상면, 전역 맵 기반 하부 span/collar, 외곽 다리 난간/기둥을 한 경로에서 그린다. `TerrainMaterial.Grass/Stone/Sand`는 각각 G/S/W 원화에 대응한다. 게임의 terrain enum, 점유, 이동, 저장 정보는 바꾸지 않았다.
- 퍼즐 런타임은 V10 하늘 1개와 별·구름·섬 독립 Renderer 128개를 만든다. 두 별길 아트 QA 씬에는 같은 Renderer가 실제 씬 오브젝트로 저장됐다. `Background` Sorting Layer를 `Default` 뒤에 두고 하늘 -100, 별 -90/-85/-80, 구름 -60/-40/-20, 섬 -30을 사용한다. 구름·별 PNG를 포함한 옛 합성 하늘, 달, 행성 원화는 이 경로에서 사용하지 않는다.
- 병아리의 퍼즐 장치 루트와 논리 footprint는 유지하고 visual 자식만 0.62배로 줄였다. 제공된 V10 front 원화는 남쪽 idle에만 쓰고 접지 anchor 기준 pivot을 +0.30515625 unit 높인다. 다른 방향 및 걷기 상태는 기존 클립을 같은 visual 배율로 표시한다.
- 기존 `PrototypeRole`과 `PuzzleDeviceSession` 이벤트를 기준으로 정적 장치 원화를 연결했다. 실제 역할·상태 대응은 [검증 대응표](Validation/StarwayRanchV10/device_bindings_verified.json)에 기록했다. 원화 18장은 클립이나 새 상태 머신으로 만들지 않았다.
- `ANIMOL_Starway_Art_QA`, `ANIMOL_Starway_Island_QA`, `ANIMOL_Starway_Character_QA`, `ANIMOL_Device8_Art_QA`를 갱신했다. 기존 QA 조작·검사 컴포넌트는 유지하고 이전 그림은 화면에서 숨겼다. 장치 QA에는 18개 상태 원화를 각각 배치했다. 실제 별길 프로토타입은 Play 시 V10 배경·전경·연결 가능한 장치 원화를 생성한다.
- 로비의 기존 reduced-motion 토글은 씬을 건너 현재 세션의 V10 Rig `SetReducedMotion(bool)`에 연결했다. 별도 UI나 저장 스키마는 없다.

## 직접 실행한 검증

| 검사 | 결과 |
|---|---|
| Unity 6000.3.8f1 배치 컴파일 및 V10 적용 | 성공. 네 QA 씬과 카탈로그 저장 |
| ZIP 원본과 런타임 PNG SHA-256 대조 | 1,816/1,816 일치 |
| 배경 설정 검사 | 독립 레이어 128개, 별 drift/twinkle 0, 별 multiplier 0.03, wrap 레이어 guard 부족 0개 |
| EditMode 회귀 | [51/51 통과](Validation/StarwayRanchV10/editmode_regression.xml) |
| PlayMode 퍼즐·아트 회귀 | [8/8 통과](Validation/StarwayRanchV10/playmode_regression.xml) |
| PlayMode 네 QA·V10 검사 | [6/6 통과](Validation/StarwayRanchV10/playmode_qa_regression.xml) |
| 로비 PlayMode | [5/5 통과](Validation/StarwayRanchV10/playmode_lobby.xml) |
| 로비 EditMode | [6/7 통과](Validation/StarwayRanchV10/editmode_lobby.xml). `SunsetSanctumCharacters/...sein_illustration.png`의 기존 임포트가 `Tight`여서 `FullRect` 기대값과 불일치. 이 작업에서 해당 파일과 메타를 변경하지 않음 |
| Unity 카메라 렌더 | [1080×1920 아트 QA](Validation/StarwayRanchV10/qa_1080x1920.png), [18상태 장치 QA](Validation/StarwayRanchV10/device_qa_1080x1920.png), 카메라 X±30/Y±60 및 직교 크기 [5](Validation/StarwayRanchV10/qa_ortho_5.png)/[25](Validation/StarwayRanchV10/qa_ortho_25.png) 캡처 |

PlayMode 검사에서 별의 카메라 상대 X 이동 비율 약 0.00375, 원경 구름 약 0.357을 확인했다. `timeScale=0` 이후 드리프트 정지와 reduced-motion 카메라 고정도 확인했다. 카메라 원거리 캡처는 별·구름과 하늘 Cover 유지 여부를 확인하기 위한 실제 Unity 렌더이며, 제공 패키지의 합성 이미지를 실행 증거로 사용하지 않았다.

## 연결 범위와 남은 제한

- 18장에는 모든 방향별 원화가 없다. 검증한 동쪽·수평·NE 상태에만 새 정적 원화를 표시하고 다른 방향과 부족한 상태는 기존 스프라이트·클립을 유지한다. `props/ranch_gate.png`와 `props/windmill.png`는 장식으로만 취급한다.
- 현재 별길 프로토타입 덩굴다리의 `cells`는 1칸이다. V10 다리 원화는 2칸 폭이므로 그 프로토타입에 억지로 덮지 않았다. 이미 2칸 수평 footprint를 가진 기존 덩굴다리 인스턴스에만 연결된다. 맵의 footprint나 전개 규칙은 바꾸지 않았다.
- 현재 `PuzzleDeviceSession.Solid`는 기존 병아리 역할을 길막 대상으로 분류한다. 이번 시각 축소는 그 판정을 수정하지 않았다. 제공된 `nonblocking` 아트 메타와 기존 게임 규칙의 차이는 별도 게임 규칙 결정이 필요하다.
- 지역 이름 네 개는 기존 `MainMenuCatalog`에 이미 요청된 표기로 저장되어 있었다. 활성 프로젝트에는 낙일 성소의 **스토리 완료 조건·이벤트**가 아직 없으므로 천구 날조국으로 자동 전환하는 조건을 임의로 만들지 않았다. 기존 `requiredStageIds`는 대전 잠금용이며 스토리 완료 판정으로 사용하지 않았다.
- 일시정지 시 `Time.timeScale=0` 경로는 확인했다. 현재 퍼즐 HUD의 `PauseRequested`는 게임 일시정지 동작에 연결되지 않아 별도 pause 어댑터 실행은 검증 대상이 아니었다. 모바일 기기 실행과 실제 출시 빌드 검사는 수행하지 않았다.
