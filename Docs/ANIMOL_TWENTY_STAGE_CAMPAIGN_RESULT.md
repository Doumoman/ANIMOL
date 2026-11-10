# 20스테이지 캠페인과 클리어 후 이동

일반 스테이지를 클리어하면 `다음 스테이지로`와 `별자리 지도` 버튼을 함께 표시한다. 각 테마의 마지막 20번에서는 다음 버튼을 숨기고 `별자리 지도`만 표시한다. 다음 이동은 실제 클리어와 이동 종료를 확인한 뒤 같은 테마의 인접 맵을 새 세션으로 불러온다. ET-20에서 ET-21로 이동하는 요청도 거절한다.

## 적용 범위

- 별길 목장, 메아리 극장, 천문대, 바다를 각각 20스테이지로 통일했다. 지도는 총 80개 별과 76개 연결선으로 구성한다.
- 기존 별자리 그림 32개와 배경을 유지하고, 테마마다 8개 그림에 별을 3·3·3·3·2·2·2·2개로 배치했다. 새 별 위치에 필요한 연결선 24쌍을 추가했다.
- 캠페인과 맵 제작기 미리보기의 다음 맵 이동을 연결했다. 별길 목장의 화면 번호 SW01~SW20은 원본 SW00~SW19에 대응하며, 원본 ID로 맵과 클리어 기록을 찾는다.
- ET-19의 목표 방울 7개가 HUD의 기존 상한 4개에 잘리던 문제도 수정했다. 실제 목표 수로 클리어를 확인하고, 방울 표시 간격과 병 채움 이미지는 그 수에 맞춰 계산한다.
- 기존 20번 이후 맵 자산은 보존하고, 캠페인의 선택·진행 범위를 20개로 제한했다. 퍼즐 원본, GUID, 작성된 검증 경로는 수정하지 않았다.

## 검증

Unity 6000.3.8f1의 연결된 Editor에서 실행했다. 상세 결과는 [검증 기록](Validation/TwentyStageCampaign/summary.json)에 보관한다.

| 검사 | 통과 |
|---|---:|
| EditMode `NotebookCampaignPolicyTests` | 4 |
| EditMode `NotebookIntegrationTests` | 5 |
| EditMode 테마 이동 메뉴 구성 | 1 |
| PlayMode `NotebookCampaignPlayTests` | 4 |
| PlayMode `ActivePortraitSceneOpensSingleMapPurchaseAndSettings` | 1 |

캠페인 테스트는 클리어 상태 fixture로 ET-19→ET-20의 실제 로딩, ET-20의 다음 이동 거절, 버튼 노출과 별길 목장 SW19→SW20의 원본 연결을 검사한다. 미리보기 테스트는 ET-19를 위 방향 입력 8회로 실제 클리어하여 목표 7개와 다음 맵 로딩을 검사한다. 클리어 화면 캡처는 UI 경계 검사에 사용한 fixture이며 ET-20의 새 풀이 증거는 아니다.

지도 테스트는 4개 테마의 별·연결선 개수와 배치를 검사한다. 화면 크기별 테스트는 360×640, 390×844, 445×773, 1440×900에서 조작부와 마지막 별의 화면 내 표시를 검사한다. 사용자 진행 기록을 보존한 상태로 테스트하고 종료 시 복원한다.

- [ET-19: 버튼 두 개](Validation/TwentyStageCampaign/ET19_Clear.png)
- [ET-20: 별자리 지도만 표시](Validation/TwentyStageCampaign/ET20_Clear.png)
- [별길 목장](Validation/TwentyStageCampaign/Map_starway.png), [메아리 극장](Validation/TwentyStageCampaign/Map_theater.png), [천문대](Validation/TwentyStageCampaign/Map_observatory.png), [바다](Validation/TwentyStageCampaign/Map_sea.png)

기존 `CommercialPolishPolicyTests`는 활성 Assets 밖의 Legacy 보관본이므로 실행하지 않았다. 이번 작업은 피드백 강도, 입력 차단, 게임 시간을 변경하지 않는다. 기존 미커밋 변경은 이번 작업과 분리해 보존했다.

## 재생성

`Tools/Reports/rebuild_twenty_stage_map.py`로 카탈로그와 필요한 연결선 PNG를 계산한 뒤, 연결된 Unity Editor에서 `Tools/UI/ApplyTwentyStageCampaign.cs`의 `ApplyTwentyStageCampaign.Run`을 실행한다. 프리팹의 지도 외 레이아웃과 CanvasScaler 설정은 저장 전 보존한다. 원본 퍼즐 맵은 생성 대상에 포함하지 않는다.
