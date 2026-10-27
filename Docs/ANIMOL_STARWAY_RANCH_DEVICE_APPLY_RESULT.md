# 별길 목장 장치 적용 결과

2026-10-27, Unity 6000.3.8f1 연결 에디터에서 실행.

## 적용 경로

- 패키지: `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13`에 R2 ZIP 전체 331개 파일을 반영했다. 기존 267개 PNG 경로를 유지했고, 기존 Sprite `.meta`의 GUID 변경은 0건이다. ZIP의 `SHA256SUMS.txt` 330개 항목을 모두 대조했다.
- 빌더: 설치된 `animation_catalog.json`으로 `RanchDevicesV13Builder.Build`를 실행했다. 기존 카탈로그 asset GUID를 유지하고 `_GeneratedV13/InspectionOnly_AllClips.prefab` 및 클립 150개를 생성했다. 이전 `InspectionOnly_All137Clips.prefab`은 삭제하지 않았고, 프로젝트 내 다른 참조는 발견되지 않았다.
- 실제 바인딩: `PuzzleScenario.ranchV13Art`의 `sluice`, `sheep`, `tall_grass` 배치를 `PuzzleDeviceSession`의 단일 논리 상태에 연결했다. 공식 `PrototypeRole` ID는 추가하지 않았다. 신규 배치에는 인스턴스 ID를 저장하며, 기존 ID 없는 배치는 가족명과 원래 좌표로 안정적인 런타임 ID를 만든다. 기존 `rightState`는 R2의 `true = A 물`, `false = B 물` 위상으로 읽는다.
- 판정: 수문은 짝의 마른 칸에서 물칸으로 들어가는 입력에만 `TryCrossPond`를 적용한다. 양 직접 밀기는 `TryPushSheep`, 바람은 실제 호스트 경로를 직선 구간별로 `TryWindSheep`에 전달한다. 호스트 `Cost`와 `EndTurn`만 한 번 반영하고 커널의 회계 차이를 더하지 않는다. 거절된 입력은 복사 상태를 버린다.
- 복구: 세션 복사에 플레이어, 장치, 양 위치/존재, 풀 소비, 수문 위상과 코스트를 함께 담았다. `Undo`, `Reset`, `RestoreSnapshot`과 컨트롤러의 대응 메서드를 추가했다. 표시 재구성 시 진행 중인 소비 효과를 취소한다.
- 표현: `PuzzleMapView`는 수문 한 논리 배치에 `RanchPondPairVisualR2`의 1칸 렌더러 두 개만 만든다. 양 이동 뒤 `RanchSheepConsumptionVisualR2`를 임시 효과로 재생하며, 논리 양과 풀은 이미 제거된 상태다. 양의 정렬은 플레이어와의 지면 y 좌표로 갱신한다. 기존 병아리의 V12 차단·따라오기와 일방향 문의 V12 시각 바인딩은 유지했다.
- 검수 맵: `Assets/ANIMOL/Data/Puzzle/Prototypes/StarwayRanchDeviceQA.asset` 및 `_Scenario.asset`에 수문·양·키 큰 풀을 배치했다. 사용자의 미커밋 `Assets/ANIMOL/Data/Puzzle/Maps/1-1.asset`은 수정하지 않았고, 기존 수문 1개가 있는 맵의 검증 오류는 0건이었다.

## 실행 결과

| 검사 | 결과 |
|---|---|
| ZIP 체크섬 / 이전 PNG 경로 / GUID | 330/330 일치, 기존 267경로 전부 존재, 기존 Sprite GUID 차이 0 |
| Unity C# 컴파일 | 오류 0 |
| Unity Sprite / 클립 / 카탈로그 / 검사 프리팹 | 277 / 150 / 유효 / 시각 150개 |
| 277 Sprite 임포터 설정 | Single, FullRect, PPU 256, 중앙 Pivot, Bilinear, 무압축, Mipmap Off, Clamp 및 캔버스 크기 모두 일치 |
| 제공 순수 규칙 EditMode | 21/21 통과 |
| 호스트 R2 EditMode | 5/5 통과: 양 밀기·바람 끝/금속 차단·첫 풀 소멸·수문 왕복·일반 물칸 거절·Undo/Reset |
| R2 PlayMode | 4/4 통과: 셀별 수문 표시와 fill/drain 6프레임 완료·양/풀 소멸 효과·Undo·검수 맵 3입력 경로·전후 가림 |
| 방향별 양/병아리 및 수문/풀/바람 클립 PlayMode | 1/1 통과 |
| 기존 퍼즐 논리 EditMode | `PuzzleDeviceLogicTests` 64/64 통과. V12 병아리 차단·누름판·따라오기와 일방향 문 검사가 포함됨 |
| 실제 게임 카메라 캡처 | `Docs/ANIMOL_STARWAY_RANCH_DEVICE_QA.png`에서 수문 두 칸, 플레이어 접지, 양과 풀의 크기·위치를 확인 |

`PuzzleMapTests`는 53/54 통과했다. 유일한 실패는 이번 작업에서 건드리지 않은 사용자 미커밋 `Assets/ANIMOL/Data/Puzzle/Maps/PuzzleMapf.asset`의 필수 목표 장치가 맵 밖에 있다는 기존 데이터 검증 오류다. `DevicePatchP01HostTests`는 1/5 통과했다. 실패 4개는 R2 변경 대상이 아닌 일방향 문 이동 종료 위치와 기존 바람 코너 경로 기대값이다. 해당 경로를 이번 장치 변경 때문에 수정하지 않았다.

## 남은 정책과 범위

- 병아리 등 다른 객체가 수문 교환으로 새 물칸에 남는 경우는 `FloodPolicyRequired`로 전체 동작을 보류하고 진단한다. 세부 동행 정책은 제공 규칙에서 미확정이다.
- 여러 바람·컨베이어·제동판의 동시 경합은 기존 호스트 순서에 맡겼다. 새 우선순위를 확정하지 않았다.
- 호스트에는 영속 저장 파일 시스템이 없다. `RestoreSnapshot`은 같은 맵의 메모리 스냅샷 복원이며 디스크 저장/재로드는 실행하지 않았다.
- 사용자 캠페인 전체를 플레이하며 모든 장치 조합을 수동 검수하지는 않았다. 위 PlayMode는 별도 저장된 검수 맵과 테스트 생성 맵에서 실행했다.
