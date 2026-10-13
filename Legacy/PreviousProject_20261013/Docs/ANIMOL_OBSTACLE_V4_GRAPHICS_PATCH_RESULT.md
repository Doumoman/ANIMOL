# OVG01_V4_OBSTACLE_GRAPHICS — V6 adaptation

작업일: 2026-10-06. 실제 Editor/ProjectVersion: **6000.3.8f1**.

사용자의 후속 지시인 **V6 유지**를 적용했다. 파일명과 소스 패키지명에 V4가 남아 있지만, 설치한 지형은 V6 하나뿐이다. V4 Registry를 복원하거나 운영 맵의 버전을 바꾸지 않았다. 장애물 장식은 visualVersion=1이며 지형 artVersion=6과 별개다. 16×16 청크는 재도입하지 않았다.

## 구현 범위와 운영 연결의 제한

기존 Scene 맵 에디터에 **장애물** 카테고리, 현재 테마/공통 장치 필터, 미구현 표시 토글, V6 A/B/C/D 재료 선택을 추가했다. 별도 에디터는 만들지 않았다. 45종 운영 Registry의 실제 프리팹 컴포넌트를 조사한 결과는 [operational-types.tsv](Validation/ObstacleGraphicsV1/operational-types.tsv)에 있다.

패키지의 공통 10종을 기존 45종과 동일 기능이라고 가정하지 않았다. 현재 새 아트로 운영 연결한 장치는 **C01 하향 발판 / DropPlatformObject**, **C05 달등불 발판 / MoonLanternStepObject** 두 종류다. 나머지는 기존 동작과 그림으로 에디터에서 접근할 수 있다. 공통 10종의 5테마 아트와 접합 선택기는 모두 설치·검증했지만, 이를 10종 게임 기능 구현 완료로 보고하지 않는다.

| 패키지 | 실제 프로젝트 확인 | 이번 처리 |
|---|---|---|
| C01 | DropPlatformObject, 1셀, 기존 PlatformEffector2D | V6 상판/기능 아트 연결 |
| C05 | MoonLanternStepObject, 실제 Solid/Warning/Hidden/Recovering | 실제 collider.enabled를 읽어 연결 |
| C02 | SideSpringObject는 고정 horizontal/vertical impulse 발사 | 속도 반사 장치로 바꾸거나 반사 스킨으로 오표시하지 않음 |
| C03 | PrismSpring도 SideSpringObject 사용 | 상향 전용 공통 장치로 취급하지 않음 |
| C04 | 기존 위험 마커/왕복 절구는 공유 위상 점멸 공중칸과 다름 | 새 물리/타이머 없음 |
| C06 | CloudSheepStep은 소멸/복귀, GreenPetalCup은 prototype | 착지 반동 기능으로 재해석하지 않음 |
| C07/C08/C10 | UpdraftColumn / LibLetterBelt / MineSlickFacet는 PrototypeMapObject | 미구현 보기에서 기존 프리뷰로 구분 |
| C09 | LibPopupStair 3×2, DewSeedStep 3×1 및 개별 물리 부품 | 1셀 접근 생성판으로 축소·대체하지 않음 |
| M02/R01/R03 | MoonRabbitBowl은 2×1; Resonance는 prototype; R03 대응 운영 타입 없음 | 아트 후보만 등록, 운영 활성화 없음 |

## 설치와 보존

- 소스 전체: `Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v1` (manifest 284개 + manifest 자체).
- Runtime 신규 PNG: `Assets/ANIMOL/ObstacleGraphicsV1/Art/MechanismAtlas.png` **한 장**.
- Runtime catalog: 같은 루트의 `Data/mechanism_catalog.json`. 원본 terrainArtVersion=4는 출처 정보로 보존하며, 설치 Registry가 V6 adaptation을 명시한다.
- 신규 Registry: `Assets/ANIMOL/Resources/ANIMOL_ObstacleGraphicsV1.asset`, 163 Sprite, visualVersion=1 / terrainArtVersion=6.
- 몸체: 기존 `Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV6.asset`의 참조 재사용. 추가 몸체 텍스처·셀 PNG·문양 복제 없음.
- 패키지 경로/크기/SHA256 284개 일치. 포함된 V4 소스 80장은 검수 원본으로만 보관하며 Assets에 설치하지 않았다.
- 실제 V6 아틀라스 80장도 V6 원본과 SHA256 일치. V6 Registry/기존 아트에 Git 변경 없음. T01_A 포함 기존 V6 픽셀 유지.
- 기존 사용자 맵·프리팹은 저장·자동 전환하지 않았다. 시작 시 미커밋 데이터 에셋 75개의 바이트가 유지됐다.
- 기존 사용자 미커밋 변경과 untracked 파일은 커밋에서 제외했다. 기존 NamedArt 자동화가 임포트 중 갱신한 `ProjectSettings/ANIMOLNamedArtReferences.json`도 기존 사용자 작업 영역으로 남겼다.

임포트: Multiple, top-left JSON y→`396-y-height`, 32×32 rect, 중앙 pivot, FullRect, PPU32, Point, 압축 없음, mipmap Off, sRGB, NPOT resize 없음. PhysicsShape 자동 생성 Off를 실제 Sprite 163개에서 확인했다. 새 Collider는 없다.

## 선택기·상태·저장 경로

- 기존 `ANIMOL.Runtime` / `ANIMOL.Editor` asmdef 아래 `Scripts/Gameplay/ObstacleGraphicsV1`과 `Scripts/Editor`로 연결했다.
- `ObstacleVisualResolver`: 패키지 C# 코어를 V6 전용으로 이식. 기존 `FreeShapeTopology.Canonical/Variant` 호출로 47 mask/256 raw/4전역위상을 재사용한다. theme/style/artVersion/joinGroup이 같은 재료만 연결한다. 막힌 기능 면은 다른 재료여도 오류다.
- FULL은 렌더용 점유에서 지형과 양방향 연결. TOP은 몸체에 참여하지 않고 같은 높이 상판만 연결. AIR는 둘 다 비참여. 저장된 물리 점유에 렌더 마스크를 기록하지 않는다.
- FULL 상단 패치는 기존 32×32 Sprite의 하단 24px와 선택한 상단 8px를 원본 rect로 나눠 표시한다. 투명한 패치 뒤에 원래 끝선이 남지 않는다. 픽셀 수정·재색칠·새 Shader 없음.
- `FreeShapeTerrainRenderer`를 확장했다. 변경 전후 셀의 3×3을 갱신하고 기존 문양 후보 규칙으로 지원 범위를 다시 검사한다. 36개 가능 anchor 중 기존 8셀 간격 조건에 맞는 문양 좌표만 실제 캐시 대상이다. 장치는 문양 지지로 추가하지 않는다.
- `ObstacleRuntimeGraphics.LateUpdate`는 기존 물리 갱신 이후 상태만 읽는다. 상태/위치/맵 revision이 같으면 맵 렌더를 재생성하지 않는다. 기존 Renderer는 표시만 억제하며 장치 스크립트, Collider, effector, 효과 서비스는 유지한다.
- 기존 맵의 일부 장치만 연결할 수 있다. 재료가 모호한 다른 장치는 기존 모습과 명시적 오류를 유지하며, 정상 연결된 장치까지 되돌리거나 강제로 함께 저장하지 않는다.
- 장치 재료는 `StageMapObjectSettings.obstacleStyleId`에 저장한다. 비어 있으면 맵에 스타일이 유일할 때만 상속한다. 모호한 재료는 명시적으로 선택해야 한다. 비어 있는 기존 설정은 기존 hash 표현을 유지하고, 명시적 스타일은 hash에 포함한다.
- 기존 `ValidateObject → CommitObject → native Undo → 저장 → Notify`를 사용한다. 신규 배치, 이동, 삭제, 설정 변경, Undo/Redo는 파생 그림을 다시 읽는다. 기존 Ready/보상 검증을 변경하지 않았다.
- 에디터 썸네일·배치 후보·Scene·Runtime 모두 설치된 V6 Registry/기능 Registry를 읽는다. 배치 후보도 실제 전역 좌표와 seed, 주변 접합을 사용한다.

## 실행 결과

패키지 검사와 Unity 검사는 별개다.

- 패키지 Node 선택기 테스트: **13/13 PASS**. 원본 V4 패키지 자체의 검사이며 V6 Unity PASS로 전용하지 않는다.
- 패키지 픽셀 검사: 163프레임, 80개 보관 원본, Sweetie-16/alpha0·255/rect 일치. 고립 픽셀·검은 2×2 후보 0. 전체 미관 승인 아님.
- Unity OVG EditMode: **12/12 PASS**. 장치만 있는 레이어의 seed/스케일 변경도 포함.
- Unity OVG PlayMode: **3/3 PASS**. 같은 프레임의 연속 Load와 삭제 대기 중인 이전 소유자 배제/장치 삭제 후 캐시 제거를 포함. 기존 하향 발판 물리의 상승 통과·하강 착지, 달등불의 실제 Warning/비활성/Reset 상태 관찰.
- 20스타일 × raw256 × 4위상의 V6 선택/참조 확인. 20스타일 × 공통10종의 등록 key 확인.
- 독립 Python 원본 조립본과 실제 Unity 렌더: **40개 1×/8× 이미지, 픽셀 차이 0**. 이는 그래픽 fixture이고 200종 플레이 가능 장치 검사가 아니다.
- 개발 맵의 실제 Scene/Runtime 두 경로: **16비교, 픽셀 차이 0**. 두 운영 장치 × 네 재료 × 1×/8×.
- 객체 배치 revision +1, hash, 스타일 저장/재열기, 이동/삭제, Undo/Redo, 잘못된 배치 거절, 저장 이후 Notify 오류 주입의 정본/디스크 rollback 확인.
- 검사 결과·원본 조립본·캡처: [Validation/ObstacleGraphicsV1](Validation/ObstacleGraphicsV1).

개발 맵: `Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OBSTACLE-V6-JOIN.asset`. 사용자 승인에 따라 V4 대신 V6 이름으로 생성했다. 4재료 × 두 운영 장치, 음수 좌표와 기존 -16 경계, 바닥/상판 접합을 배치했다. 운영 스테이지에 연결하거나 Ready로 승격하지 않았다.

## 미검증·제약

- 아직 운영 소유자가 없는 C02/C03/C04/C06/C07/C08/C09/C10에 대한 실제 20스타일 플레이·네트워크 4~8인 검사는 미실행. 이 작업에서 새 장치 기능을 만들지 않았다.
- 모든 동물의 이동·스태미나·가속/대시 수치 전후 측정, 실제 C02 속도 반사 보존 검사는 미실행. 해당 서비스 파일은 변경하지 않았고 현재 SideSpring은 C02 반사 계약과 다르다.
- 모든 가능한 맵/층/혼합 재료의 미관, 모든 벽/단차/막힌 기능 면 조합의 실플레이 승인은 하지 않았다.
- 후보 M02/R01/R03 운영 연결은 미실행. 기존 복수 셀 장치를 한 셀 아트로 축소하지 않았다.
- 외부 파일 시스템의 실제 저장 불능/디스크 장애는 발생시키지 않았다. 저장 후 예외를 주입한 트랜잭션 rollback을 검사했다.
- 패키지 HTML 브라우저/다운로드 동작 재검사는 미실행. HTML·미리보기·원화는 Runtime Sprite로 사용하지 않는다.

## 최종 회귀 검사와 Local Play

이번 실행의 고유 테스트 **66개 PASS (EditMode 63, PlayMode 3)**:

| 실제 테스트 클래스 | 결과 |
|---|---:|
| ObstacleGraphicsTests / PixelTests / IntegrationTests | 12/12 |
| ObstacleGraphicsPlayModeTests | 3/3 |
| FreeShapeTerrainTests(6) | 21/21 |
| TerrainEditorV4Tests | 14/14 |
| TerrainStructureIntegrationTests | 13/13 |
| CommercialPolishPolicyTests | 3/3 |

처음 발생한 1개 실패는 문양 필터링 후 갱신 수를 36이라고 기대한 테스트였다. 기존 `AffectedMotifs`는 36개 범위 중 등록 가능한 위상 anchor만 반환하므로 테스트를 수정했다. 최초 결과도 `edit-initial.json`에 보관한다. 추가된 기존 맵 개별 연결 테스트는 목록 순서가 아닌 stableId로 객체를 확인하도록 보정했다(`legacy-test-initial.json`). 이후 12개 최종 OVG EditMode 검사와 모든 회귀 검사는 통과했다.

Local Play는 `TerrainEditorSession.OpenForSceneTest`로 실제 실행했다. 기존 플레이어 1, 운영 장치 8, 그래픽 observer 1, FreeShape renderer 1을 확인했다. 새로운 아트 하위 Collider는 0이다. 캡처는 [local-play.png](Validation/ObstacleGraphicsV1/local-play.png), 실행 상태는 [local-play.json](Validation/ObstacleGraphicsV1/local-play.json)이다. 이 캡처는 현재 Game camera 화면이며 모든 동물의 수동 완주나 HUD 검수 기록은 아니다. 검증 후 Play Mode를 종료했다.

### 측정한 성능과 범위

- Unity `Profiler.GetRuntimeMemorySizeLong`의 메커니즘 아틀라스 메모리: **1,825,664 bytes**. 576×396, `isReadable=false`. GPU 전용 메모리 추정값이 아니라 Unity API의 실제 보고값이다.
- 개발 맵의 그래픽 연결 초기 갱신: 72개 영향 셀 / 문양 후보 3 / Renderer 25 / **0.9939ms** (기존 Stopwatch 계측).
- 한 장치의 TOP 활성값 변경: 3×3 **9셀** 갱신. 동일 스냅샷 재입력은 0셀 갱신. 단위 테스트에서 확인했다.
- 20스타일의 10종 그래픽 fixture는 각 Renderer 19개, 초기 갱신 약 1~8ms 범위였다. 세부 값은 최종 `pixel-parity.json`을 따른다.
- CPU Profiler Timeline 저장·모바일 빌드·4~8인 부하 측정은 **미실행**. 이 소규모 수치를 전체 맵/기기 성능 보증으로 확장하지 않는다.
- 1×/8× 비교 총 **119,808,000 pixels**, 차이 0. 전체 가능한 맵의 미관 승인이 아니다.

최종 Unity 컴파일 오류 0. 기존 obsolete API 경고가 남아 있으며 새 installer도 프로젝트 기존 방식인 `TextureImporter.spritesheet`를 사용하므로 같은 deprecated 경고 1개를 추가한다. 실제 163 Sprite의 rect/pivot/PPU/physics shape와 임포트 설정은 검증했다. 원본 source package의 manifest는 검증 후에도 다시 일치했다.

테스트가 남긴 `TEST-OVG01` 백업은 제거했고, 테스트 teardown도 고유 stageId의 백업을 정리하도록 수정했다. 변경 파일 목록은 `Validation/ObstacleGraphicsV1/changed-paths.txt`에 기록한다.
