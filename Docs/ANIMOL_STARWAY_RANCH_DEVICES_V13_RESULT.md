# 별길 목장 장치 V13 적용 결과 (2026-10-27)

## 적용 위치

- 원본 ZIP `MCP_INBOX/ANIMOL_STARWAY_RANCH_DEVICES_V13.zip`의 전체 폴더를 `Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13/`에 배치했다. `Authoring~`은 보관했고 런타임 스프라이트 경로로 사용하지 않았다.
- `.gitattributes`에 이 패키지의 바이트 보존 규칙을 추가했다. 기본 Git 무시 규칙에 걸리는 체크섬 대상 `.png.tmp` 7개도 포함했으며, 스테이징된 311개 원본 파일의 SHA256을 다시 대조해 불일치 0건을 확인했다.
- Unity 6000.3.8f1 에디터에서 `ANIMOL.Editor.RanchDevicesV13.RanchDevicesV13Builder.Build`를 실행했다. 생성 경로는 같은 폴더의 `_GeneratedV13/`이며, `RanchDevicesV13Catalog.asset`, 137개 `.anim`, `InspectionOnly_All137Clips.prefab`을 포함한다.
- `Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset`의 `v13Devices`에 생성 카탈로그를 연결했다. 기존 V12 배경, 지형, 잔디, 장식 V02와 V12 카탈로그 참조는 유지했다.
- 실제 프로토타입 바인딩: `Assets/ANIMOL/Scripts/Runtime/PuzzleDeviceView.cs`, `RanchDevicesV13PuzzleBinding.cs`, `PuzzlePrototypeController.cs`. 기존 `Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity`의 장치 정의와 프리팹 루트를 그대로 사용한다. V13은 StarSocket, Conveyor, Bounce, Brake, Wind 블로어, Chick, WoodCrate, MetalCrate의 기존 Visual 자식에 연결된다. 사용자 요청에 따라 OneWayGate는 V12로 복귀했으며 VineBridge도 V12를 유지한다.
- 기존 루트의 ID, 논리 점유, 콜라이더, 저장 참조는 바꾸지 않았다. 이동 장치의 루트는 확정 위치로 옮기고 `RanchCommittedMotionV13`은 Visual 자식에 실제 연속 타일 경로를 재생한다. 순간이동 구간은 임의의 보간 경로를 만들지 않는다. 수동 밀기와 바람 운반이 이어지면 구간별 클립을 전달한다.
- `RanchResolvedWindVisualV13`은 세션이 해석한 바람 경로, 0/3/6/9 범위, 셀 크기, 정렬 값을 받는다. 경로 갱신은 이전 표시를 지운다. 원본 패키지 소스의 체크섬을 보존하기 위해, 호스트 바인딩은 기존 `RanchCommittedMotionV13`의 직렬화 필드를 생성 시 한 번 연결한다.

## 실제 실행한 검사

| 검사 | 결과 |
|---|---|
| ZIP 무결성 및 설치 후 `SHA256SUMS.txt` | 311개 파일 대조, 누락·불일치 0 |
| Unity 빌더 및 에디터 컴파일 | 빌더 성공, 최종 C# 컴파일 오류 0 |
| Unity 에셋 API 검사 | 실사용 PNG 267개: PPU 256, Single, FullRect, 중앙 Pivot, Bilinear, 무압축, Mipmap Off, Clamp. 카탈로그 267 Sprite, 137개 `.anim`의 SpriteRenderer 곡선, 검사 프리팹 137 자식, 테마 참조 확인 |
| EditMode `RanchDevicesV13BindingTests` | 9/9 통과: 문양 키, 병아리 착석 키, 바람 off/3/6/9 키 |
| EditMode `PuzzleDeviceLogicTests` | 63/63 통과: 문양 불일치 거절과 코스트/턴 0, 빈 홈 통행 거절, 일치 착석, 플레이어·상자·병아리 바람 이동, 금속 차단, 둥지, 비차단·누름판 제외, 자동 이동 추가 코스트 0 등 |
| PlayMode `RanchDevicesV13ClipPlayTests` | 1/1 통과: 검사 렌더러에서 네 방향 양·병아리 걷기, 양 먹기, 병아리 앉기, 둥지, 수문 좌·우 및 왕복 전환, 바람개비 off/3/6/9 클립 재생. 수문 스프라이트 576×320, scale 1 확인 |
| PlayMode 목장 프로토타입 V13 검사 | 1/1 통과. 실제 `ANIMOL_starway_Prototype.unity`에서도 V13 대상 14개가 기존 루트에 연결됐고 누락 바인딩 0. 오른쪽 입력 1회 후 코스트 1, 플레이어 (2,2), 카메라 추적 확인 |
| 바람 경로 시각화 | 임시 복제한 장치 20 맵의 확정 상태를 적용해 3/6/9칸에서 각각 terminal 1개, off에서 경로·terminal 0개 확인 |
| 독립 수로 렌더러 | `RanchWaterCellVisualV13.Apply`를 별도 검사 오브젝트에서 마스크 0(수면·둑 9개)과 255(수면 1개)로 실행. 실제 A/B 수로 게임 연결 검사는 아님 |
| 다른 범위 회귀 | `PuzzlePrototypePlayTests` 3/3, `RanchConnectedDecorV02PlayTests` 2/2, `ArtQaPlayTests` 1/1 통과 |
| `ArtAssetTests` | 3/4 통과. `ManifestImportsAndExactClipDurations`는 작업 범위 밖의 변경 없는 `StarSwellCoastV01Device.prefab` 루트 SpriteRenderer에 Sprite가 없어 실패했다. V13 에셋 누락은 아니다. |

`StarwayRanchV12PlayTests` 전체 실행은 Unity 테스트 러너가 장시간 `running` 상태에서 결과를 반환하지 않아 완료 결과가 없다. 해당 클래스의 V13 프로토타입 검사만 따로 다시 실행해 1/1 통과했다. 이전 V12 배경·장식은 위의 목장 및 장식 PlayMode 검사로 확인했다.
격리된 이전 프로젝트의 `CommercialPolishPolicyTests`는 현재 활성 테스트 대상이 아니므로 실행하지 않았다.

## 연결되지 않은 규칙과 미실행 검사

현재 `PrototypeRole`, `PuzzleDeviceSession`에는 별양, 2×1 수문, 별도 둥지 본체, 풀 소비, 수로 A/B의 가변 젖음 상태가 없다. `PuzzleScenario`에는 아래의 별양·수문 **그림 배치 데이터만** 추가했다. 실제 게임 판정과 현재 씬의 플레이 장치로 연결됐다고 보지 않는다. 수문 전환 후 `RanchWaterCellVisualV13.Apply`와 변경 칸 주변 8칸 마스크 갱신도 아직 호출할 논리 상태가 없다. 수문은 별도 1×1 루트 둘로 만들어 대체하지 않았다.

따라서 실제 맵에서 별양의 한 칸 밀기·앞 풀 한 칸 먹기·바람 운반, 풀의 Undo/Reset 복구, 수문 2×1 단일 루트와 A/B 수로 전환, 병아리 수문 통과 후 `waiting_at_sluice` 및 재추종은 **NOT_RUN / 미구현**이다. 검사 프리팹의 해당 클립 재생 성공은 게임 연결 검증으로 간주하지 않는다. 현재 기본 런타임 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity`는 별길 장치 시나리오가 없는 별도 맵이다.

기존 확정 로직을 조사한 결과, 일반 셔터와 다리의 점유 유지, 병아리의 기록 경로 추종, 기존 움직임 연쇄·게이트·바람 순서는 있다. 다만 **점유된 A/B 수로 전환, 수문 정지 뒤 병아리 재추종, 바람 속 양의 풀 먹기, 여러 이동 장치 경합의 새 우선순위**에 해당하는 확정 로직은 없다. 이 조합들은 새 규칙으로 채우거나 기획 완료로 표시하지 않았다. 별돌의 기존 숫자 1/2/3을 triangle/diamond/cross 그림 키로 대응한 것은 시각 바인딩이며 새 공식 장치 ID가 아니다.

풀 재사용·되돌리기·비활성화 후 재활성화, 화면 밖 복귀, 1이 아닌 셀 크기, 바람 terminal의 모든 장애물 배치, 수문 및 별양이 포함된 완성 맵의 코스트 계측은 **NOT_RUN**이다. 현재 프로토타입의 플레이어/나무상자/병아리 바람과 금속 차단은 EditMode 세션 테스트로 검증했으며, 모든 조합을 PlayMode의 실제 맵에서 재현한 결과는 아니다.

## 맵 제작기 보정 및 일방향 문 V12 복귀 (2026-10-27)

- 원인: 맵 제작기는 프리팹의 기존 그림을 사용하고 V13 카탈로그를 조회하지 않았다. `PuzzleMapEditor.cs`와 `RanchDevicesV13EditorPreview.cs`를 연결해 기존 목장 장치의 팔레트 및 격자 그림을 V13 상태 키로 표시한다. 다른 테마와 일방향 문은 기존 프리팹 그림을 유지하고 조회 결과를 캐시한다.
- 양·수문은 공식 장치 ID가 없으므로 `PrototypeRole`을 새로 만들지 않았다. 맵 제작기 장치 탭에 별양 1칸과 수문 가로 2칸의 **그림 배치**를 추가했다. 클릭 배치, 두 번째 수문 칸에서 지우기, 양 방향 및 수문 좌우 그림 선택, 시나리오 저장, 격자 및 `PuzzleMapView` 표시를 연결했다. 수문 그림은 576×320, PPU 256, scale 1로 두 칸의 가운데에 놓는다. 기존 맵 데이터는 자동 수정하지 않았다.
- 이 배치 데이터는 `PuzzleDeviceSession`에 들어가지 않는다. 별양 밀기·풀 먹기와 수문 A/B 전환은 여전히 **미구현**이고, 제작기에서도 이 점을 표시한다. V13 갤러리의 둥지·풀·수로도 상태 그림 미리보기이며 게임 판정 연결은 아니다.
- 사용자 요청대로 `RanchDevicesV13PuzzleBinding.Covers`에서 `OneWayGate`를 제외했다. 기존 `PuzzleDeviceView`의 V12 바인딩과 프리팹 그림을 다시 사용한다.
- 열려 있는 Unity 에디터에서 기존 사용자 맵 `1-1.asset`을 맵 제작기 장치 탭으로 열고 V13 카탈로그 참조가 있음을 확인했다. 맵 자산의 배치 내용은 수정하지 않았다. 창 재도색 뒤 `Editor.log` 최근 출력에는 맵 제작기 예외가 없었다.
- Unity 6000.3.8f1 컴파일 오류 0. EditMode `PuzzleMapTests` 53/54 통과, 새 양·수문 배치/렌더링과 V12 일방향 문 검사는 통과. 나머지 1개는 이번 작업 전부터 미커밋 상태인 `PuzzleMapf`의 목표 장치가 맵 밖에 있어서 실패한다. `RanchDevicesV13BindingTests` 9/9, `StarwayRanchV12Tests` 4/4 통과. 양·수문의 게임 판정 PlayMode 검사는 **NOT_RUN / 미구현**이다.
