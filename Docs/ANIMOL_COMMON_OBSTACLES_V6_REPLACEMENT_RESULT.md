# 공통 장애물 C01~C10 교체 및 구형 장애물 폐기

2026-10-06 · Unity 6000.3.8f1 · terrain artVersion=6 / obstacle visualVersion=1.

사용자는 이전 장애물 전체 폐기와 공통 10종의 **실제 기능 교체**를 승인했다. 이전 그래픽 전용 패치의 C01/C05 연결 제한을 해제하고 새 운영 타입 10개를 만들었다. V4 지형은 설치하지 않았으며 16×16 청크도 재도입하지 않았다.

## 사용 경로

- 기존 맵 에디터의 장애물 목록은 C01~C10만 표시한다. 모든 테마에서 같은 기능을 사용하며 V6 A/B/C/D 재료와 해당 테마의 기능 Sprite를 사용한다.
- 현재 Registry: `Assets/ANIMOL/Resources/ANIMOL_CommonObstaclesV6.asset` (catalog version=2). 기존 45종 Registry의 개수 검사를 재사용하지 않고 `CommonObstacleCatalog.Load`가 정확한 10 ID/종류/프리팹을 검증한다.
- 신규 프리팹/타입: `Assets/ANIMOL/ObstacleGraphicsV1/CommonDevices/C01~C10`. 프리팹에는 옛 그림이나 사각형 Sprite가 없다. 실행 상태/충돌은 `CommonObstacleObject`, 표시는 기존 `FreeShapeTerrainRenderer`/`ObstacleRuntimeGraphics`가 소유한다.
- 새 enum은 1001~1010으로 분리했다. 구형 enum 번호나 기존 배치를 새 기능으로 조용히 해석하지 않는다. Runtime은 새 Registry의 프리팹을 사용하므로 배치에 구형 프리팹 참조가 있더라도 불러오지 않는다.
- 4개 캠페인 마커 START/CHECK/BUBBLE/EXIT는 유지한다. 구형 45종 및 구형 위험 마커는 배치 목록, Scene 프리뷰, Runtime 생성, footprint/swept 점유에서 제외했다. 구형 그림으로 되돌리는 표시 경로도 없다.
- 기존 맵의 직렬화 기록과 구형 소스/에셋 자체는 호환·추적용으로 남아 있다. 특히 다른 작업의 미커밋 파일을 물리적으로 지우지 않았다. **사용 경로의 폐기이지, 저장소의 구형 파일 전부 삭제는 아니다.**
- 선택 맵에 구형 기록이 있으면 장애물 목록의 `폐기 기록 N개 정리`로 정리할 수 있다. 한 번의 revision/Undo로 저장하며 실패하면 메모리·디스크를 복원한다. 창 열기/리로드로 사용자 맵을 자동 저장하지 않는다. 구형 기록이 남은 운영 맵은 Ready 검증에서 교체·재검토를 요구한다.

## 구현한 기능

| ID | 실제 동작 | 기본 개발 조정값 |
|---|---|---|
| C01 | one-way 상판, 기존 아래 통과 입력, 키보드 ↓/S 지원 | 상판 물리 두께 0.12셀 |
| C02 | 지정 좌/우 면에 진입할 때 충돌 전 vx 부호만 반사, vy/스태미나/쿨다운 유지 | 고정 발사량 없음 |
| C03 | 윗면 착지 시 수평 속도를 유지하고 위로 발사 | vy=11 |
| C04 | 공용 Fixed 시간의 안전→예고→위험, 실제 Local Play 복귀 서비스 호출 | 1 / 0.45 / 1.5초 |
| C05 | 마지막 탑승자 이탈 후 예고·소멸, 점유 공간이 비었을 때 복구 | 예고 0.45 / 소멸 1.5초 |
| C06 | 윗면 착지 시 낮은 반동 | vy=5 |
| C07 | UP/LEFT/RIGHT 방향의 공중 바람, 고체 표면 없음 | 가속도 32 |
| C08 | 몸체가 고정된 좌/우 표면 운반 | 속도 2 |
| C09 | 접근 예고 후 같은 칸에 상판 생성, 이탈 예고 후 소멸, 생성 공간 점유 보호 | 거리 2셀 / 예고 0.45초 |
| C10 | 정지 입력에서 자연 감속만 완화, 자동 가속 없음 | 감속 0.8, Collider 마찰 0 |

위 값은 기존 자료에 수치가 지정되지 않은 부분의 개발 기본값이며 밸런스 확정은 아니다. 각 타입에 해당하는 설정만 에디터에 표시한다. CommonObstaclePlayerSample/Motion은 기존 플레이어 전후 FixedUpdate에서 필요한 외부 효과만 적용한다. 기존 플레이어 파일의 미커밋 구현을 덮어쓰지 않았다. 점프 입력 해제 감쇠가 스프링 반동/상향 바람을 즉시 소거하지 않도록 보완했다.

C05/C09는 플레이어 수와 무관하게 점유를 모으고, C09 Pending은 상판 비활성, 이탈 Warn은 상판 활성이다. 개별 복귀에서 공통 장치를 전부 초기화하지 않는다. C04는 각 장치의 별도 랜덤 타이머를 만들지 않는다. FULL/TOP/AIR의 렌더 연결, 전역 좌표·seed·47마스크·4위상 및 기존 V6 Sprite를 유지한다. 기능 PNG와 아틀라스는 다시 만들지 않았다.

## 검증 자료

실제 실행 기록과 캡처는 [Validation/CommonObstaclesV6](Validation/CommonObstaclesV6)에 보관한다. 설치 당시 과거 PASS를 이번 실행으로 재사용하지 않는다. 세부 최종 집계는 아래와 같다.

초기 기능 테스트에서 Unity의 missing-component 값에 C# `??`를 적용한 문제를 발견해 명시적 Unity null 검사로 고쳤다. 다음 실행에서 C10 기본 Collider 마찰의 추가 감속을 발견해 마찰 0을 지정했고, 테스트용 맵의 collision revision 초기화도 보완했다. 초기 실패 기록은 `play-initial.json`, `play-second.json`으로 남겼다.

## 보존과 미검증 범위

- 작업 시작 당시 기존 변경/미추적 1,700개 파일을 기록했다. NamedArt 자동 인덱스 갱신 외에는 기존 작업 파일의 바이트를 유지했고, 그 인덱스 변경도 이번 커밋에서 제외한다.
- 원본 메커니즘 아틀라스 SHA256 `7b09246c16d1987d32f6d14ed1d18942c2cfdcc253b3cfe4253aae4141e18f6b` 일치. V6 지형 아트·Registry는 변경하지 않았다.
- 구형 맵을 자동 새 장애물로 치환하지 않았다. 구형 경로의 설계 의도를 새 10종으로 옮기는 작업은 각 맵의 배치 재검토가 필요하다.
- 지역 후보 M02/R01/R03은 이번 공통 10종 목록에 포함하지 않는다.
- 기존 실제 Local Play 경로와 플레이어를 사용한다. 네트워크 4~8인 동기화, 모든 동물 폼 조합, 모바일 기기 빌드/CPU Profiler Timeline, 전체 운영 맵 완주·밸런스 검사는 미실행이다. 8명 점유 보호 테스트를 네트워크 검증으로 보고하지 않는다.
- 기존 프로젝트의 플레이어/복귀 서비스는 Local Play 중심이다. 이번 변경을 새 멀티플레이 서버나 운영 출시 인증으로 해석하지 않는다.

## 최종 실행 결과

고유 테스트 **79개 PASS (EditMode 65 / PlayMode 14)**.

| 실제 검사 | 결과 |
|---|---:|
| CommonObstaclePlayModeTests | 12/12 |
| ObstacleGraphicsPlayModeTests (실제 상승·착지·아래 통과, 재로드/삭제) | 2/2 |
| ObstacleGraphicsTests / PixelTests / IntegrationTests | 13/13 + 설정 hash 추가 검사 1/1 |
| TerrainEditorV4Tests | 14/14 |
| FreeShapeTerrainTests(6) | 20/21 최초 + 실패 사례 수정 후 1/1 재검사 |
| TerrainStructureIntegrationTests | 13/13 |
| CommercialPolishPolicyTests | 3/3 |

자유형 회귀에서 일반 authoring API의 고정 1셀 장애물/지형 겹침 검사가 빠진 경로를 발견해 `StageMapObjectAuthoringOperations.ValidatePlacement`에 검사를 추가했다. 해당 실패 사례를 다시 실행해 통과했다. 최초 결과와 재검사 결과를 별도 보관하며 전체 21개를 수정 후 다시 실행했다고 기록하지 않는다. 폐기한 이동 레일/연결 쌍/반블록의 authoring 기대값은 새 고정 1셀·폐기 거절 계약으로 갱신했다.

- 20스타일 × 공통10 그래픽의 독립 원본 조립 비교: 1×/8× **40개 이미지 / 119,808,000 pixels / 차이 0**. 실행 기록 `pixel-parity.json`. 기존 기준 PNG는 동일한 원본 아트를 비교하는 입력이며 과거 PASS를 복사한 것이 아니다.
- 실제 새 10종 배치의 Scene/Runtime 두 경로: 1×/8× **20비교 / 차이 0** (`scene-runtime-parity.json`). 기능 상태 조합 전체를 이 20비교로 인증하지 않는다.
- 신규 개발 맵: `Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-COMMON-10-V6.asset`. 모든 장치는 기존 플레이어가 접근 가능한 바닥 높이에 배치했고 기존 운영 스테이지에 자동 배정하지 않았다.
- 기존 `TerrainEditorSession.OpenForSceneTest`를 실제 실행했다. 플레이어 1, 새 장치 10, 각 장치 하위 구형 SpriteRenderer **0**, 아트 Collider **0**, 아트 Renderer **14**를 확인했다 (`local-play.json`).
- 실제 Game camera 캡처: [local-play.png](Validation/CommonObstaclesV6/local-play.png), 원배율 전체 배치 [local-play-overview-1x.png](Validation/CommonObstaclesV6/local-play-overview-1x.png). 파란 사각형은 기존 개발 플레이어이며 구형 장애물 잔재가 아니다. 두 캡처를 직접 확인했다.
- 검증 후 Play Mode를 종료하고 새 개발 맵의 장애물 카테고리를 열었다. 실제 목록은 공통 장애물 10개 + 캠페인 마커 4개다 (`final-editor.json`).
- 최종 실제 Unity 상태: compilationFailed=false, compiling=false, consoleErrors=0. 설치 코드와 기존 코드의 obsolete API 경고는 이전 컴파일에서 있었으나 오류는 없다 (`final-console.json`).

이 결과는 Local Play 기능·저장/Undo·픽셀 연결의 검증이며 모든 맵의 미관이나 운영 출시 승인으로 확대하지 않는다.

최종 검토에서 새 장애물의 warning/active/recover/phase/effect 설정 전체가 ContentHash에 포함되도록 보완했다. 기존 타입의 hash 표현은 유지하며 공통 10종에만 전체 설정 직렬화를 추가했다. 추가 hash 검사 1개가 통과했다 (`hash-settings-test.json`).
