# ANIMOL 캠페인·강화 UI / 배경 제작물 v1 적용 결과

작성: 2026-10-02 · Unity 6000.3.8f1 · 현재 ANIMOL 작업 트리 기준

## 적용 결과와 범위

신규 UI 7개와 로비의 아이콘·‘강화’ 진입 버튼을 실제 메뉴 씬에 적용했다. 이미지, 프레임, 버튼, 초상, 로딩 이미지와 게임 배경 SpriteRenderer는 **Edit Mode에서 생성하여 씬/프리팹 하이어라키에 저장**했다. 이 신규 모듈은 Play Mode에 이미지 오브젝트나 Sprite를 생성하지 않으며, 저장된 오브젝트의 활성 상태·텍스트·스프라이트 참조·로딩 프레임만 갱신한다. 승인 v7의 기존 합성 렌더링 구현은 유지했다. 전체 목업 PNG를 화면으로 사용하지 않았다.

작업 지시 경로의 패키지는 없었으며 실제 동봉 위치인 `Docs/Inbox/ANIMOL_UI_Background_Production_v1/ANIMOL_ui_production_v1/`의 `docs/UNITY_APPLY_TASK.md`, `docs/PRODUCTION_BRIEF.md`, `runtime/manifest.json`, `reference/layouts.json`, `reference/preview.html`을 기준으로 작업했다. 두 지시 문서는 끝까지 읽었다.

| 항목 | 상태 | 실제 적용 / 제한 |
| --- | --- | --- |
| SC02 테마 선택 | 적용 완료 | 실제 5테마, 완료 수, 잠금 상태, 스크롤 |
| SC03 스테이지 선택 | 적용 완료 | 선택 테마의 20개 데이터, 잠금·제작 중·입장 가능·완료 구분 |
| SC04 스테이지 상세 | 적용 완료 | 목표·시간·체크포인트·기록, 고정 사용/시작 동물 읽기 전용, 강화 왕복 시 선택·스크롤 보존 |
| SC09 진입 로딩 | 적용 완료 / 정상 운영 진입 미실행 | 실제 `StageLaunchUseCase`와 `LoadSceneAsync` 연결, 준비 중·실제 씬 진행률·중복 시작 차단·실패·재시도·복귀 |
| SC10 강화 허브 | 적용 완료 / 서비스 미연결 | 로비 또는 상세에서 진입한 내비게이션 경로로 복귀 |
| SC10A 계정 강화 | 적용 완료 / 서비스 미연결 | 실제 3트랙 카탈로그, 현재/다음 효과·비용·보유량, 운영 초안 구매 차단, 확정 응답 후 조회값 반영 |
| SC10B 동물 강화 | 적용 완료 / 데이터 미설정·서비스 미연결 | 실제 RABBIT 식별자, 내부 상세 패널, 액티브·패시브 2영역, 미설정·미조회·구매 비활성 |
| 공용 상태 9종 | 적용·검증 완료 | 확인, 처리 중, 성공, 재화 부족, MAX, 연결 실패, 미설정, 결과 미확정, 진입 실패. 정적 모달 하나 재사용 |
| 메뉴 아트 | 적용 완료 | 60개 원본 PNG: 메뉴 배경 7, 썸네일 5, 공통 프레임·아이콘·토끼 초상·로딩 시트 등 |
| 게임 배경 | 5테마 적용 / T01 이동 검수 완료 | 별도 제작 소스 15레이어. T02~T05 개별 맵 플레이 검수는 미실행 |
| 승인 v7 로비 | 보존 | 기존 20레이어·토끼 시트 2개·5초 주기·고정 건물/발판·통로 구현 유지, 관련 회귀 검사 실행 |

## 하이어라키와 변경 파일

`Assets/ANIMOL/Scenes/Lobby.unity`에서 Play를 누르기 전에도 다음 오브젝트를 확인할 수 있다.

```text
UI_CommonRoot
  ProductionV1Background
  SafeArea
    ScreenHost
      SC01_Lobby/PV1_LobbyUpgrade
      SC02_ThemeSelect/ProductionV1
      SC03_StageSelect/ProductionV1
      SC04_StageDetail/ProductionV1
      SC09_MapLoading/ProductionV1
      SC10_UpgradeHub/ProductionV1
      SC10A_AccountUpgrade/ProductionV1
      SC10B_CharacterUpgrade/ProductionV1
    ModalHost/ProductionV1Modal
ThemeRuntime_T01 ~ T05
  ProductionV1_GameBackdrop
    Far
    Mid
    Decor
```

- `Assets/ANIMOL/UI/ProductionV1/`: 독립 모듈, Art 60개·GameArt 15개와 importer 메타, 카탈로그, 런타임 바인딩, 배경 배치 컴포넌트, 정적 배경 프리팹 5개, Editor 저작/캡처 도구, EditMode/PlayMode 테스트.
- `Assets/ANIMOL/Scenes/Bootstrap.unity`, `Assets/ANIMOL/Scenes/Lobby.unity`: 정적 UI 하이어라키 저장. 기존 씬 전환과 화면 컨테이너 재사용.
- `Assets/ANIMOL/Scenes/Campaign/ThemeRuntime_T01.unity` ~ `T05.unity`: 작업 트리의 기존 씬에 배경 루트 추가. 이 씬들은 작업 시작 전부터 사용자의 **미추적 파일**이었다. 파일 전체를 이번 커밋에 포함하면 기존 사용자 작업까지 섞이므로 커밋에서는 제외하고, 적용된 작업 파일은 그대로 보존했다. 커밋에는 신규 배경 프리팹 5개와 재적용 코드가 포함된다.
- `Assets/ANIMOL/UI/PortraitArtV1/Tests/PlayMode/PortraitEntryFlowTests.cs`: 기존 테마 뒤로 버튼 탐색 이름을 새 정적 버튼 이름으로 1줄 갱신.
- `Docs/UiProductionV1/`: 실제 테스트 응답, 캡처, 원본/보존 해시 검사 자료.

기존 UI 프리팹과 작업 중인 런타임 코드의 미커밋 변경을 덮어쓰지 않았다. 대체 화면의 기존 자식은 씬에서 비활성화하여 기존 프리팹·사용자 변경을 보존했다. 새 UI 생성은 `ProductionController.Author`의 Editor 경로에서만 실행한다. `ProductionBuilder`의 sceneSaving 연결로 기존 메뉴 생성기가 Bootstrap/Lobby를 저장할 때 정적 UI가 포함된다. 실제 기존 메뉴 생성 경로를 실행한 뒤 하이어라키 검사와 회귀 검사를 다시 실행했다.

재저작 메뉴: `ANIMOL > UI Production V1 > Import art and catalog`, 이어서 `Apply authored hierarchy to menus`. 게임 씬 재적용 API는 `ProductionBuilder.AuthorGameTheme(1..5)`이며 이미 배경 루트가 있으면 중복 생성하지 않는다. 씬 재저작 API는 Play Mode와 저장하지 않은 씬 편집이 있으면 중단한다.

## 이미지와 레이아웃

- 신규 PNG **75/75개가 동봉 원본과 SHA-256 및 바이트 일치**한다. `source-asset-verification.json` 참조.
- Point, 무압축, mipmap 없음, FullRect로 가져왔다. manifest pivot과 9-slice를 적용하고 로딩 8프레임은 Editor에서 카탈로그 subasset으로 저장했다. 토끼 초상은 원본 128×160과 투명 여백을 보존했다.
- 비율 유지 이미지와 9-slice 프레임을 분리했다. 기존 승인 코인·Pixelroborobo 폰트를 사용하고 신규 프레임·아이콘과 조합했다.
- 기존 CanvasScaler 기준 1080×1920과 SafeArea를 유지했다. 1080×2400에서는 스크롤/앵커가 추가 공간을 처리하고 배경만 균일 cover한다.
- 장식 이미지의 raycastTarget은 껐다. 모달 딤은 하위 버튼 입력을 차단하며 뒤로 가기는 모달부터 닫는다. 피드백 강도 정책은 변경하지 않았다.

## 데이터와 서비스 연결

캠페인은 기존 `CampaignCatalog`, `CampaignUiPresenter`의 세션 진행 정보, `ContentAvailabilityResolver`, `StageLaunchUseCase`, `CampaignLaunchContext`를 사용한다. 데이터와 수동 맵은 수정하지 않았다. 새 화면이 활성화된 동안 기존 캠페인 presenter의 중복 뒤로 처리를 막고, 그 외 화면에서는 기존 동작을 유지한다.

현재 실제 스테이지는 운영 진입 승인을 만족하지 않는다. T01-S01에는 빠른 클리어/재플레이 보상 승인과 현재 맵 해시에 대한 수동 완료 검수가 필요하다. 이 제한을 해제하거나 테스트를 위해 운영 데이터를 바꾸지 않았다. 따라서 **실제 운영 스테이지의 정상 로딩 완료→게임 진입은 미실행**이다. 실제 미승인 데이터에서 로딩 실패·중복 시작·복귀는 실행했고, T01 게임 배경은 기존 MapDevTest에서 실제 플레이어로 확인했다.

계정 강화는 기존 `AccountUpgradeCatalog`의 3트랙과 `UpgradePurchaseUseCase`/`IAccountGateway`를 사용한다. 기본 gateway는 미연결 상태다. 연동 지점 `BindAccountGateway(gateway, readApprovedSnapshot)`에 서버 확정 스냅샷 조회를 연결해야 한다. 승인 응답과 갱신된 스냅샷이 모두 확인된 경우에만 새 레벨·잔액을 표시한다. 응답 유실/중복 상태에서는 결과 미확정으로 재요청을 막는다. 자동 검사의 승인 응답은 **격리된 테스트 gateway**이며 실제 운영 서버 구매가 아니다.

계정 카탈로그의 `PrototypeValues`는 운영 승인으로 바꾸지 않았다. 상한/비용 초안을 운영값으로 고정하지 않았고 미조회 수치는 `--`로 표시한다. 동물 카탈로그의 DEV 계약을 실제 토끼 능력으로 전용하지 않았다. 실제 RABBIT 성장 계약·효과·가격·숙련도 정책과 서비스가 없으므로 동물 2트랙의 구매 및 운영 수치 연결은 **미설정/서비스 미연결**이다. 임의 동물·능력·가격을 추가하지 않았다.

## 게임 배경 확인

T01의 실제 맵 월드 bounds는 `(-16, -16, 64, 32)`, 타일 단위는 1이다. 런타임 카메라 orthographicSize는 약 10.6667이며 PixelPerfect 컴포넌트는 없다. 실제 플레이어 스프라이트는 PPU 16이고 기존 맵 오브젝트는 PPU 32도 사용한다.

704×352 원본을 PPU 16의 **44×22 유한 평면**으로 가져오고 카메라 범위에 맞춰 균일 확대 및 12% 여유를 적용했다. 전체 월드 규격으로 강제하지 않았고 Repeat/seamless로 처리하지 않았다. 카메라 추종과 제한된 상대 이동만 사용한다. Default sorting order는 Far -30 / Mid -20 / Decor -10이며 기존 Terrain 1 / Decoration 2 / Object 3 / Player 12보다 뒤에 있다. 배경에는 Collider가 없다. 중경·장식 대비/알파를 낮춰 충돌 가능한 실제 발판과 구별했다.

기존 MapDevTest에 T01 정적 프리팹을 임시 배치하여 실제 `DevPlayerController.ApplySwipe` 이동을 실행했다. 플레이어 X -10.72 → -0.41, 카메라 X -9.40 → 0.89의 5개 샘플 모두 grounded=true, 모든 레이어 균일 배율을 확인했다. 시작 및 이동 캡처 6개에서 발판·플레이어 앞 가림을 확인했다. 검수용 MapDevTest 씬 변경은 저장하지 않았다. 이후 같은 배치 규칙을 T02~T05 실제 ThemeRuntime 씬에 적용했다. **T02~T05 개별 맵 이동/완주, 기기 실측 SafeArea, 실제 서버 왕복 검증은 미실행**이다.

## 실제 실행한 검사

Unity Editor에서 실제 스크립트 컴파일 후 관련 테스트를 실행했다. 최신 런타임 코드 기준 EditMode 13개, PlayMode 13개가 통과했으며, 최종 UI 배치 수정 뒤 Production PlayMode 7개를 추가 재실행했다. 마지막 Editor 저작/캡처 도구 수정 후 EditMode 13개를 다시 실행했다.

| 검사 묶음 | 고유 테스트 수 | 최종 결과 / 근거 |
| --- | ---: | --- |
| ProductionAssetTests | 4 | 통과: 60 import, 정적 7화면·진입점·중복 없음, 구매 게이트, T01 배경 sorting/충돌 없음 |
| MenuHierarchyTests + MenuThemePolicyTests | 6 | 통과: v7 자산·5초 슬롯·프레임·고정 요소·저장된 하이어라키 |
| CommercialPolishPolicyTests | 3 | 통과: 기존 연출 강도 정책 |
| ProductionFlowTests | 7 | 통과: 상세/강화 왕복·스크롤, 모달 raycast/뒤로, 로딩 중복/실패/복귀, 미조회/미설정, 9상태 재사용, 확정 응답/중복 구매, 레벨/MAX/재화 부족/큰 잔액 |
| 기존 v7 PlayMode + PortraitEntryFlowTests | 6 | 통과: 배경·로비 재진입·기존 메뉴 왕복 |
| 합계 | **26개** | 고유 테스트 수. 반복 실행을 새 테스트로 세지 않음 |

작업 중 실제 8회 실행은 **96회 테스트 실행: 성공 95, 초기 실패 1**이다. 초기 실패는 기존 흐름 테스트가 폐기된 `ThemeBackButton` 이름을 찾던 문제였다. 실제 새 뒤로 버튼으로 검사 대상을 갱신한 뒤 해당 흐름을 포함한 재실행은 통과했다. 실패 기록도 `playmode-initial-failure.json`에 보존했다.

최종 증거: `editmode-authoring-final.json` (13/13 상세 결과), `playmode-final.json` (13개 완료·succeeded·실패 0), `playmode-focused-final.json` (7개 완료·succeeded·실패 0). 뒤의 두 MCP 응답은 개별 result 본문이 null이므로 응답에 실제 존재하는 completed/status/failures를 근거로 기록했다. 이전 상세 PlayMode 12/12 결과는 `playmode-results.json`에 있다. 실행하지 않은 검증이나 0개 테스트를 PASS로 기록하지 않았다.

## Game View 캡처와 검수 범위

실제 Game View 1080×1920·1080×2400에서 각 19장, 총 **UI 38장**을 캡처했다. 각 해상도에서 로비 진입점, 7화면, 스테이지 아래 스크롤, SafeArea 상하 90px 모의 조건, 공용 상태 9종을 확인했다. 상태 캡처에는 긴 한글과 2,147,483,647 / 1,234,567,890 예시를 넣고 검수용임을 명시했다. 예시는 운영 데이터가 아니다.

- [1080×1920 전체 화면 모음](UiProductionV1/ui-contact-1920.png)
- [1080×2400 전체 화면 모음](UiProductionV1/ui-contact-2400.png)
- [1080×1920 공용 상태 모음](UiProductionV1/modal-contact-1920.png)
- [1080×2400 공용 상태 모음](UiProductionV1/modal-contact-2400.png)
- 원본 PNG: `Docs/UiProductionV1/Captures/1080x1920/`, `Docs/UiProductionV1/Captures/1080x2400/`
- T01 실제 이동: `1080x1920/T01_start.png`, `T01_move_0.png` ~ `T01_move_4.png`; `t01-movement-evidence.json`
- 실제 화면 ID·해상도·프레임·missing script 기록: `gameview-evidence.json`

검수 중 포커스를 잃은 Unity가 프레임을 멈춰 오래된 로비 이미지를 반환하는 캡처 문제를 발견했다. 해당 이미지는 최종 자료로 사용하지 않고 다시 캡처했다. 검수 도구는 캡처 동안만 background 실행을 켜고 프레임 정지를 실패로 처리하도록 보완했다. UI 프레임과 텍스트/초상 영역을 분리하여 긴 문장과 장식이 겹치지 않게 조정했다. 실제 기기 입력/노치 검수와 운영 구매 검수를 수행한 것으로 해석하면 안 된다.

## 기존 작업 보존과 남은 작업

작업 시작 시 변경되어 있던 tracked 파일 96개의 백업과 SHA-256을 비교했다. 원래 사용자 변경을 되돌리거나 이번 커밋에 포함하지 않았다. 보호 대상 849개 중 844개는 동일하며 변경된 5개는 위에 명시한 ThemeRuntime 씬의 배경 추가다. 수동 맵·Data·MapBackups·승인 v7 파일은 바이트 단위로 보존했다. 자세한 근거는 `working-tree-before.txt`, `protected-before.json`, `protected-result.json`, `preexisting-work-verification.json`이다.

남은 운영 연결은 서버 gateway/확정 스냅샷, RABBIT의 승인된 2트랙 성장 계약, 스테이지 보상 승인/현재 맵 수동 검수다. 이 데이터가 준비되면 실제 서버 구매, 정상 캠페인 비동기 진입, T02~T05 전체 맵 플레이와 기기 SafeArea 검수를 이어서 수행해야 한다. 기존 DEV 맵의 미지원 오브젝트 경고와 테스트 정리 씬의 AudioListener 경고는 이번 배경/UI 변경으로 수정하지 않았다.
