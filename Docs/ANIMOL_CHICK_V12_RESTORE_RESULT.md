# 별길 목장 병아리 V12 복귀 결과

2026-10-27, Unity 6000.3.8f1의 실제 프로젝트에서 확인했다.

- 병아리의 기존 기록 경로 지연 추종과 둥지 도착 후 정지는 유지했다. V12의 1칸 고체 점유를 복구해 플레이어 진입과 상자 밀기를 막고, 일반 누름판을 누르게 했다. 막힌 입력은 이동·코스트가 0이다.
- V13에서 추가된 병아리 바람 운반을 해제했다. 병아리는 바람 경로의 장애물이 되며, 다른 장치의 V13 바람 이동 규칙은 유지한다.
- 실제 별길 프로토타입의 병아리는 기존 `d24_follower_chick` 프리팹의 V12 4방향 idle/walk 클립으로 표시한다. `RanchDeviceVisualV13`과 V13 경로 트윈을 병아리에게 생성하지 않는다. 다른 장치의 V13 카탈로그와 클립, 병아리 ID·논리 루트·저장 참조는 변경하지 않았다.
- 적용 파일: [퍼즐 판정](../Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceSession.cs), [시각 바인딩](../Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceView.cs), [현재 규칙](ANIMOL_DEVICE_RULES_CURRENT.md). V12의 해당 판정은 Git `1225f5909`에서, V13의 비차단 변경은 Git `e66c992ab`에서 확인했다.

## 실행한 검증

| 검사 | 결과 |
|---|---|
| Unity Editor C# 재컴파일 | 오류 0 |
| `PuzzleDeviceLogicTests` EditMode | 64/64 통과. 병아리의 추종·플레이어/상자 길막·누름판·바람 차단·둥지·무변화 코스트 0 포함 |
| `PrototypeUsesV13DeviceVisualsWithoutChangingPuzzleAuthority` PlayMode | 1/1 통과. 실제 별길 프로토타입에서 병아리 `DeviceVisualView.IsV12Active`, V13 컴포넌트/경로 모션 없음, 기존 루트·배율 확인 |

V13 패키지에 남은 병아리 스프라이트와 검사 클립은 카탈로그 검사용이며, 게임의 병아리 시각 바인딩에는 사용하지 않는다. 수문 통과 후 재추종 등 기존에 미확정으로 남겨 둔 조합은 이번 V12 복귀에서 새 규칙으로 확정하지 않았다.
