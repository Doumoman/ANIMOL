# 플레이어 0.8 × 0.8 타일 몸체 조정

현재 설정된 맵의 1타일 = 1 Unity unit 기준으로 DevPlayer의 BoxCollider2D를 0.8 × 0.8, offset (0, 0)으로 변경했다. 루트 Scale은 (1, 1, 1)이며, 기존 Visual 쿼드의 기본 크기도 0.8 × 0.8이다.

## 실제 적용 경로

- `Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab`: 연결된 Unity 6000.3.8f1에서 PrefabUtility로 수정. 기존 이동/폼/스태미나 설정은 보존했다.
- `CampaignMapMarkerGeometry.PlayerBodySize`: 재생성 및 저작 검사가 공유하는 크기. `PlayerBodyRectAtFeetInCells`는 기존 맵 단위 변환을 유지한다.
- `UiBuildPipeline.M2.BuildWorldPrefabs`: 재생성해도 같은 콜라이더와 Visual 크기를 사용한다. 전체 씬/프리팹 빌더는 실행하지 않았다.
- `StageMapMarkerValidation` 및 `TerrainEditorSceneEditor`: 안전 리스폰 영역 검사와 Scene View 몸체 표시를 새 크기로 통일했다.
- `CampaignMapMarkerGeometry.BodyAtFeet`: 실제 콜라이더 아래쪽으로 시작점·체크포인트 위치를 계산하는 기존 경로를 유지했다. 발바닥 위치는 그대로이며, 중심은 발바닥에서 0.4 unit 위다.
- `GameplayFeedbackDirector.cs`의 기존 `PlayerMotionFeedback`: 점프 준비/발사/착지/폼 변경 Scale 연출을 Visual 자식으로 옮겼다. 물리 루트 및 충돌체 크기를 연출이 바꾸지 않는다. 그림이 없는 테스트 몸체에는 Scale 연출을 적용하지 않는다.

## 검증

연결된 실제 Unity Editor에서 컴파일 및 다음 검사를 실행했다.

| 검사 | 결과 |
|---|---:|
| PlayerBodySizeTests: 실제 프리팹 크기/그림/발바닥, 시작점·체크포인트 한 타일 천장 여유 및 고체 겹침 거절 | 3/3 |
| Ovg04EditorJoinTests: 이동·방향·복제·삭제·Undo/Redo·저장/재열기 및 저작 검증 | 7/7 |
| CommercialPolishPolicyTests | 3/3 |
| PlayerBodySizePlayModeTests: 실제 플레이어의 한 타일 통로 이동/지지 바닥, 네 종류 연출 중 충돌체 크기 유지 | 2/2 |
| Ovg04CheckpointJoinPlayModeTests: Save + Local Play, 체크포인트 방울 복원/출구 재잠금/실제 접촉, 스프링 판·코일·reset | 4/4 |
| CommonObstaclePlayModeTests | 16/16 |
| M7PlayerFormsPhysicsTests | 2/2 |
| M9PortraitInputPlayModeTests | 4/4 |

EditMode 13/13, Physics2D를 사용하는 PlayMode 28/28 통과. JSON 결과와 런타임 캡처는 `Docs/Validation/PlayerBodySize/`에 있다.

기존 충돌 검사는 새 0.8 높이 몸체가 실제로 겹치는 셀을 사용하도록 수정했다. 옆 스프링 코일 통과 검사는 x=21.5에서 폭 0.8 몸체가 앞 판(x=21.875)에 걸리는 0.025 겹침을 피하도록 x=21.375로 조정했다. 앞 판 충돌 자체는 별도 기존 장치 테스트로 검증했다.

첫 EditMode 실행은 기존 1.2 높이를 전제로 한 충돌 시험 위치 때문에 1건 실패했고, 위치 수정 후 13/13 통과했다. 첫 PlayMode 실행 역시 앞 판에 걸린 시험 위치 때문에 1건 실패했고 수정했다. 이후 MCP의 전체 묶음 실행은 24/28 진행에서 Unity TestRunner 내부 `PlayModeRunTask`/`ExitPlayModeTask` 오류와 도메인 재로드로 종료 결과를 반환하지 못했다. 이 실행은 통과 집계에 포함하지 않았다. Unity Pipeline으로 각 시험군을 나누어 재실행해 최종 28건 완료 결과를 확인했다.

새 수동 GUI 조작·모바일 실기기 검사는 수행하지 않았다. 기존 Save + Local Play 및 세로 입력 검사는 자동 PlayMode 시험으로 실행했다. V6 지형/장치 아트, 저작 맵, 이동 속도·점프·스태미나·타이머 규칙은 변경하지 않았다.

## 기존 변경 보존 및 커밋 범위

작업 시작 시 기존 미커밋 항목 537개를 확인했다. DevPlayer 프리팹 및 UiBuildPipeline.M2의 기존 변경은 그대로 두고 이번 크기 수정만 부분 스테이징했다. 기존부터 미추적 상태인 GameplayFeedbackDirector.cs 전체는 새 파일로 커밋하지 않았다. 실제 작업 파일에는 수정이 적용되어 있으며, 이번 수정만 `preexisting-feedback-owner.patch`와 전후 SHA256 기록으로 보존했다. 패치는 현재 파일에 대한 reverse check로 확인했다.

T01-S01 맵·스테이지와 Gameplay 씬의 내용 보존을 SHA256으로 확인했다. 테스트가 재생성한 기존 OVG04 캡처는 별도 PlayerBodySize 근거로 복사한 뒤 작업 전 상태로 복구했다. 임시 시험 씬은 제거하고 원래 Bootstrap 편집 씬으로 복귀했다.
