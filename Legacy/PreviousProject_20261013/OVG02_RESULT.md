# OVG02_COMMON9_NATIVE_ANIMATION — V6 적용 결과

2026-10-07 · 대상 `C:/Users/user/Documents/GitHub/ANIMOL` · Unity **6000.3.8f1**.

사용자의 최종 V6 지시를 우선했다. 입력 패키지 이름과 제작 출처의 terrainArtVersion=4는 출처 정보다. 운영 지형은 **artVersion=6**, 장애물 아트는 **visualVersion=2**, 공통 타입 카탈로그는 **version=3**이다. 별도 에디터, 실험 CommonDeviceTile, V4 Registry 또는 16×16 청크를 추가하지 않았다.

## 정본과 적용 경로

정본은 `Docs/ANIMOL_Common_9_Tile_Design_v3.md`다. AGENTS.md와 기존 OVG01, 공통 10종 교체, V6 전용, 셀 경계, 로딩 개선 완료 기록 및 현재 owner를 확인했다. StageMapRuntimeFactory는 별도 파일이 아니라 `Scripts/Gameplay/StageMapRuntimeObject.cs`에 있다.

| 책임 | 실제 적용 경로 |
|---|---|
| 실제 충돌·착지·상태 | `Scripts/Gameplay/CommonObstacleObject.cs`의 기존 owner 확장 |
| 저장 정의·검증·로드·생성 | `Scripts/Core/StageMapDefinition*.cs`, `CommonObstacleCatalog.cs`, `Gameplay/StageMapRuntimeLoader.cs`, `StageMapRuntimeObject.cs` |
| 상태 스냅샷·프레임·접합 | `Gameplay/ObstacleGraphicsV1/{ObstacleSnapshotAdapter,ObstacleVisualResolver,ObstacleRuntimeGraphics}.cs` |
| V6 몸체·cap·모티프 | 기존 `Gameplay/FreeShapeTerrainRenderer.cs`, `FreeShapeArtRegistry`, `StageTerrainStructureRuntime`, `AnimolTerrainPlacementEngine` 경로 |
| 임포트·이름 기반 참조 | 기존 `Editor/ObstacleArtV1Builder.cs`, `CommonObstacleV6Builder.cs`, `ObstacleNamedArtBindings.cs`, `ANIMOL.Editor.asmdef` |
| 기존 Scene 장애물 카테고리 | `Editor/TerrainEditorV4/TerrainEditorSceneEditor.Obstacles.cs` 및 `TerrainEditorObjectAdapter.cs` |
| native 붕괴 | `Gameplay/ObstacleGraphicsV1/{UnityCrumbleVfx,StageCrumbleVfxPool}.cs` |
| 명시적 삭제 마이그레이션 | `Editor/ObstacleV2Migration.cs` |

패키지 전체는 `Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v2`에 보관했다. manifest **605개**의 전체 경로·크기·SHA256이 일치했다. manifest 자체를 포함해 606개 파일이며 일부 샘플만 설치한 것이 아니다. 제작 문서·JS/C# 코어·카탈로그·효과 데이터를 읽었다.

Runtime 아트는 기존 `Assets/ANIMOL/ObstacleGraphicsV1/Art/MechanismAtlas.png` 한 장과 `Art/C05Fragments` 20장이다. 576×900 atlas에서 **393개의 32×32 Sprite**를 `theme/kind/facing/pose/fN`으로 등록했다. 이 중 385개는 공통 기능, 8개는 비운영 지역 후보다. Registry 경로는 기존 `Assets/ANIMOL/Resources/ANIMOL_ObstacleGraphicsV1.asset`을 유지했다. 폴더·리소스 이름의 V1은 기존 GUID를 유지하는 저장 경로이며 아트 버전 fallback을 뜻하지 않는다.

JSON top-left rect를 Unity bottom-left로 변환했다. Multiple/FullRect/중앙 pivot/PPU32, Point/None/mipmap Off/sRGB 정상/NPOT None이며 플랫폼 압축 override를 제거했다. 조각은 catalog의 크기·중앙 pivot에 맞는 Single Sprite다. 전 Sprite의 physics shape 생성은 끄고, palette/원본 픽셀을 다시 가공하지 않았다. HTML/GIF/Concept/Node 도구와 원본 V4 몸체 80장은 Tools 검수 소스에만 있다. Runtime SpriteAtlas를 새로 사용하지 않는다.

V6 지형 Registry `Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV6.asset` 및 `TerrainFreeShape/V6`의 PNG/meta/에셋 211개를 보존 검사했다. 변경 0개다. 20스타일·47마스크·4전역 위상·3,760개 셀 참조, schemaVersion=1, `ANIMOL_FREE_SHAPE_BLOB47_V1` 계약은 그대로다. 지형 artVersion이나 맵 seed를 설치 과정에서 바꾸지 않았다.

## 공통 9종과 실제 동작

활성 ID는 C01,C02,C03,C04,C05,C06,C07,C08,C10이다. C09는 배치 목록·타입 Registry·생성 후보·생성 경로에서 제외하고 타입/프리팹/meta를 삭제했다. **enum 1009는 삭제 진단용으로 예약**, C10은 **1010/C10** 그대로다. 삭제 전 에셋은 Git 이력에 있다. 지역 후보 M02/R01/R03은 새 운영 기능으로 만들지 않았다.

C04는 항상 active인 AIR 위험칸이다. 기존 점멸 타이머·warn/inactive·안전 구간 경로를 제거했고 실제 위험 trigger의 활성값을 스냅샷과 일치시킨다. active/f0 Sprite 하나를 흰색 alpha1로 그린다. 기존 복귀 서비스와 접촉 처리 owner는 유지했다.

C02/C03는 실제 충돌 시각으로 30ms×7, C06는 실제 하강 착지 시각으로 40ms×7을 선택하고 마지막 프레임에 머문다. 시간은 기존 Fixed 게임 시각의 double 정밀도와 scaled game clock을 사용한다. C08은 등록된 LEFT/RIGHT 8프레임이며 기존 속도×32px 이동거리로 phase를 계산한다. 같은 속도의 벨트는 하나의 stage epoch를 공유하고 pause/reset에도 일관된다. 렌더 어댑터는 Rigidbody 속도·Collider·루트 scale·점유를 쓰지 않는다. C02 반사, C03 vy11, C06 vy5, C08 속도2, C10 저마찰/감속0.8은 기존 owner 값이다.

FULL=C02/C03/C08/C10, TOP=C01/C05/C06, AIR=C04/C07이다. 같은 theme/style/artVersion/joinGroup에서만 양쪽 그래픽 이웃으로 연결한다. 논리 지형 점유와 물리 소유자는 교체하지 않았다. C01은 V6 상판 8px만 그린다. C05/C06/C08/C10의 ReplaceCap에서는 온전한 원본 cap을 덧그리지 않고 FULL 하단24px는 유지한다. TOP 아래 빈 공간, 장치가 모티프 지지가 되지 않는 규칙, 기존 다른 재료 판정은 유지한다. 위치·표면·상태 변경은 이전/새 위치 주변3×3 및 모티프 anchor36개를 다시 평가한다. Sprite index/frame/렌더 mask는 저장 정본에 넣지 않는다.

C05는 점유 중 active 균열, 마지막 이탈 뒤 기존 grace와 점유 보호를 거쳐 **SurfaceEnabled true→false 이벤트**에만 네 조각을 재생한다. 기존 재사용 가능한 Stage VFX pool이 없어 같은 그래픽 owner의 stage 수명에 독립 풀을 추가했다. 조각은 플랫폼 자식이 아니다. effect JSON의 원점/폭/속도/중력5를 사용해 400ms 낙하, 처음80ms 불투명, 나머지320ms alpha fade, 1/32 좌표 스냅, rotation0/scale1이다. Collider/Rigidbody/피해/아이템/외부 트윈은 없다. owner-generation-sequence key로 중복 방지하고 동시 붕괴를 각각 처리한다. reset/unload는 효과·이벤트 캐시를 비운다. 이미 inactive인 owner를 새 어댑터로 읽어도 붕괴를 합성하지 않는다. EditMode Undo/프리뷰에는 실효과가 없고 입력 차단·카메라·햅틱·시간 변경이 없는 Subtle 연출이다.

프레임만 바뀌는 경우 `UpdateAnimationFrames`가 해당 Sprite만 바꾼다. 지형 전체 인덱싱/Tilemap/모티프 재생성은 하지 않는다. 기존 편집기 Validate/Commit/Undo와 local backup 경로를 유지했다. Ready·보상·스테이지 배정 검증은 우회하지 않았다.

## C09 저장 데이터 삭제와 작업 경계

실제 저장소 전체 검색 및 StageMapDefinition 스캔에서 C09는 한 파일 한 건이었다.

- `Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-COMMON-10-V6.asset`: `COMMON-9`, kind1009, (26,0)만 삭제. 다른 장치나 바닥으로 치환하지 않았다.
- 현재 작업 중 맵 revision **35→36**, hash와 삭제 목록은 `Docs/Validation/OVG02/c09-migration.json`에 기록했다. 삭제 전 JSON은 같은 기록에 명시한 `UserSettings/ANIMOL/MapBackups/DEV-COMMON-10-V6/...OVG02-before-C09-deletion...json`에 보존했다.
- 삭제는 기존 owner의 batch/Undo/검증/저장 방식으로 한 번의 revision이다. 실패 시 메모리·디스크·프리뷰를 복원한다. 창 열기나 설치가 자동 마이그레이션을 호출하지 않는다. Legacy C09 읽기/운영 검증/Factory는 명확한 삭제 마이그레이션 진단을 낸다. 다시 필요한 경우 명시적으로 `ObstacleV2Migration.RemoveC09(selectedMap)`을 호출한다.
- 시작 당시 미커밋 1,727개를 기록했다. 그중 변경한 기존 작업 파일은 이 맵과 이름 참조 인덱스 두 개뿐이며, 각각 승인된 C09 삭제와 새 아트 등록 범위다. 기존 맵의 사용자 추가 배치를 커밋에 섞지 않기 위해 **HEAD 복사본을 Unity owner로 마이그레이션해 만든 15→16 투영본**만 스테이징했다. 현재 35→36 사용자 맵은 작업 트리에 유지한다. 이름 참조 인덱스도 장애물 아트/Registry 범위만 HEAD에 반영하고 다른 미커밋 참조는 유지한다. `staging-boundary.json` 참조.

새 개발 맵은 `Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-COMMON-9-V6.asset`이다. 운영 캠페인 배정 없이 기존 Local Play/플레이어 프리팹으로 실행했다. C10 인스턴스 이름도 COMMON-10이며 C09를 재번호화하지 않았다.

## 실제 실행한 검사

**Unity 고유 테스트 101개 PASS: EditMode81 / PlayMode20.** 아래 값은 이번 대상 Editor의 실제 최종 실행이며 패키지 JS PASS나 과거 v1/v5 검사로 대체하지 않았다. 0-test 검색 결과는 PASS로 세지 않았다.

| 실제 어셈블리/검사 | 최종 결과 |
|---|---:|
| ObstacleGraphicsTests/V2Tests/PixelTests/IntegrationTests | 21 PASS + 신규 fragment import 검사1 PASS + 변경 개발 맵 Integration 재검사1 PASS(중복 집계 제외) |
| ObstacleConnectionPixelTests | 3 PASS |
| ObstacleV2LifecycleTests | 3 PASS |
| TerrainEditorV4Tests | 16 PASS |
| FreeShapeTerrainTests(6) | 21 PASS |
| TerrainStructureIntegrationTests | 13 PASS |
| CommercialPolishPolicyTests | 3 PASS |
| CommonObstaclePlayModeTests | 16 PASS |
| FreeShapePlayModeTests(6) | 2 PASS |
| ObstacleGraphicsPlayModeTests | 2 PASS |

실제 C02 좌우 반사/충돌 전 속도/수직 속도·스태미나·쿨다운, C03 상승, C06 실제 하강 착지, C08 양방향 운반, C10 저마찰, TOP 아래 상승/하강 착지/통과를 검사했다. 첫·중간·마지막·loop 프레임과 physics 값 불변, 8명 점유 보호, 마지막 이탈, 동시 붕괴/중복 이벤트/400ms fade/복구/pause/reload/unload, inactive 스냅샷 미재생, C04 긴 게임 시각 벡터/pause/reset도 포함했다. C06 실제 충돌에는 기존 solver 마찰이 있으므로 그 vx를 임의로 2로 강제하지 않고 애니메이션 전후 불변 및 기존 직접 발사 보존을 확인했다.

20스타일×raw256×4전역 위상/3,760 참조를 검사했다. native1×/정수 nearest8×에서 **20스타일×4프레임 구성=160 비교**, **벽/단차/분리20스타일=120 비교**, 실제 Scene/Runtime9장치=18 비교가 원본 PNG 조립/서로 일치한다. 연결 fixture는 음수좌표와 이전 -16 경계, FULL/TOP/AIR, cap 교체와 빈 TOP 하부를 포함한다. 원본 기대 이미지는 독립 Python이 전달 PNG와 lookup/catalog만 읽어 조립한다.

기존 `TerrainEditorSession.OpenForSceneTest → TerrainEditorScreen.EnterTest → StageMapRuntimeLoader`에서 실제 플레이어와9 owner를 확인했다. 9:16 world viewport의 288×512/2304×4096 캡처18장 및 실제 C05 이벤트를 0/80/240/401ms로 sample한 1×/8× 캡처8장을 보관했다. 결정적 C05 검수는 일시정지 상태에서 실제 owner Tick/VFX Sample을 명시적으로 전진시킨 것이며 자연시간 영상이라고 보고하지 않는다. 아홉 장애물 셀의 nearest 확대 차이는0이다. C01 전체 viewport에는 기존 플레이어의 소수점 y 위치로 2,574픽셀 차이가 있으며, 차이 경계는 플레이어 영역이고 C01 타일 영역 차이는0이다. 나머지8개 전체 viewport 차이는0이다. 플레이어 렌더 경로는 수정하지 않았다.

Editor 성능 fixture: 2만 지형 셀, 프레임 갱신1000회에서 평균 **0.005477ms**, 변경셀1/모티프0, Renderer **2→2**, atlas 메모리 **4,148,096bytes**(Unity Profiling API). 초기 전체 구성은264.8365ms다. 이는 한 장치의 프레임 바인딩 측정이며 전체 모바일 프레임 시간/paint commit/네트워크 성능으로 확대하지 않는다.

Node/제작 소스 검사는 별도로 `npm.cmd test` **18 PASS**, `verify:pixels` **393프레임/45공통테마스킨/80제작용 V4 body/20조각/오류0**, source atlas의 isolated/dark2×2 후보0을 확인했다. 실행 로그 및 현재 audit를 보관한 뒤 패키지 내부 audit는 ZIP 원본으로 복원해 605개 manifest 검사를 다시 통과시켰다. 이 결과는 대상 V6 전체 미관이나 Unity 물리 PASS를 의미하지 않는다.

자료는 `Docs/Validation/OVG02`의 unity-*.json, manifest/preservation/migration/parity/performance 기록, Expected 및 Renders에 있다. 변경 파일 전체는 같은 폴더 `changed-paths.txt`에 기록한다. 초기 기대값/fixture 실패는 현재 9종 카탈로그·실제 player/FixedUpdate·EditMode lifecycle에 맞춰 검증 경로를 고친 뒤 최종 재검사했으며, 실패나 미실행을 PASS로 숨기지 않았다.

## 미실행·확인 한계

모바일 실제 기기 빌드/성능, 네트워크4~8인 동기화, 모든 동물 폼/운영 맵 전체 완주·밸런스, CPU Profiler Timeline 장시간 기록, HTML/GIF preview browser 검사는 미실행이다. 모바일 캡처는 Editor의 실제 Local Play world camera이며 전체 터치 UI/실기기 인증이 아니다. 동일 스타일 접합과 선언된 source 픽셀 검증을 재료 혼합 미관 또는 모든 가능한 맵의 자동 미관 승인으로 확대하지 않는다. 기존 퇴역45종/과거 소스·보고서는 이력으로 보존하며 이번 활성9종에 다시 연결하지 않았다.
