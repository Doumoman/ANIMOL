# ANIMOL 자유형 Scene 맵 에디터 적용 결과

작성일: 2026-10-06. 프로젝트 Unity 6000.3.8f1.

기존 Scene 편집기에 자유형 모드를 구현하고 실제 Unity Editor 및 PlayMode에서 검증했다. 기존 사용자 맵은 자유형으로 변환하지 않았다. 아래 수치는 이 프로젝트·작업 PC에서 실행한 결과다.

## 사용 순서

1. Unity의 `ANIMOL > Map Editor > Open Scene Editor V4`를 연다.
2. 하단 `자유형 지형`을 선택한다. 테마와 네 스타일 카드에서 아트를 확인하고 스타일을 고른다.
3. `칠하기 B`, `지우기 E`, `영역 R`, `영역−`로 편집한다. `[ ]` 또는 크기 조절기로 브러시를 바꾼다.
4. 프리셋 목록에서 사각형·U자 구덩이·닫힌 구멍 등을 고르고 `프리셋 그리기`를 누른다. 배치 후에는 일반 자유형 셀로 편집한다.
5. `덩어리` 도구로 클릭·드래그하면 같은 스타일의 4방향 연결 셀을 함께 이동한다. `Del`은 선택 덩어리 전체 삭제다.
6. 드래그 중 `Esc`로 취소한다. `Ctrl+Z/Y`, 저장, 기존 시험 플레이 버튼을 그대로 사용한다.
7. 기존 완성 부품과 객체는 `v3 부품 / 객체`로 전환한다.

완성 예시는 `Assets/ANIMOL/Data/Development/FreeShape/DEV-FREESHAPE.asset`을 선택한 뒤 위 메뉴로 열 수 있다. 캠페인 스테이지에 배정하지 않은 별도 개발 검증 맵이다.

기존 맵의 셀 크기가 미설정이면 맵 이름 버튼의 설정에서 `1셀 = 1unit 설정`을 명시적으로 수행해야 한다. 사용자 맵의 설정을 설치 과정에서 자동 변경하지 않는다. 다른 테마 스타일은 현재 맵에 칠할 수 없으며 거절 이유를 표시한다.

## 적용 계약과 아트

- 적용 지시서 전체를 읽었다. 최신 자유형 계약은 구덩이·내부 구멍·돌출부를 포함하며 지형 크기를 16셀로 제한하지 않는다. 앞선 직사각형/규칙 계단 제안은 이번 구현의 제한으로 사용하지 않았다.
- ZIP 원본: 프로젝트 루트 `ANIMOL_FreeShape_Sprites_v1.zip`.
- 실제 패키지 경로: `Tools/ArtSources/ANIMOL_FreeShape_Sprites_v1`.
- `PACKAGE_MANIFEST.json`의 5,883개 파일 크기와 SHA256을 검사했다.
- 계약: `ANIMOL_FREE_SHAPE_BLOB47_V1`, 아트 버전 1.
- `Assets/ANIMOL/TerrainFreeShape/Art/Atlases`: 80장, Multiple Sprite 3,760개.
- `Assets/ANIMOL/TerrainFreeShape/Art/Motifs`: 문양 20장, 20 Sprite.
- 셀 PNG 3,760개, 원화 Raw, Samples, 갤러리와 Node 도구는 Runtime에 중복 설치하지 않았다.
- 셀 rect는 JSON top-left에서 Unity bottom-left로 변환했다. PPU 32, 중앙 pivot, Point, mipmap Off, 무압축, sRGB, FullRect다. 문양은 PPU 32, bottom-left pivot, 4×4셀이다.
- 공용 Sweetie16 색과 알파 0/255를 원본 PNG 픽셀에서 검사했다. 원화용 팔레트 변환 Shader는 사용하지 않는다. 새 아트는 `Sprites/Default` Material로 표시한다.
- 초기화 메뉴: `ANIMOL > Terrain Free Shape > Initialize Package`. 동일 파일은 바이트를 변경하지 않으며 이름이 안정적인 Sprite 참조 레지스트리를 갱신한다.
- 실제 초기 설치 118.2초. 같은 메뉴 재실행 89.7초, 셀·문양 Sprite 3,780개의 GlobalObjectId가 모두 유지됐다. 현재 초기화는 기존 아틀라스도 재임포트하므로 즉시 끝나는 작업은 아니다.
- 표시 이름은 패키지 style_catalog에서 가져온다. 스타일 카드의 네 모양은 logical_fixtures의 점유와 실제 셀 Sprite를 사용한다. 실루엣은 점유로 그리며 PNG 알파로 추정하지 않는다.

## 실제 연결 경로

`TerrainEditorSceneEditor`의 기존 Scene View 안에 자유형 패널·입력을 추가했다. 별도 제작 창으로 옮기는 작업은 없다.

`BeginFreeStroke / UpdateFreeStroke` → `AnimolTerrainPlacementEngine.TryEditFreeShape / TryMoveFreeShape` → 기존 `TerrainEditorAdapter.Validate / Commit` → `AnimolTerrainMapEditorBridge` → `StageTerrainStructureIntegration` → `StageMapDefinition.EditorApplyTerrainCandidate`.

Read DTO는 일반 셀·v3 placements·자유형 레이어를 함께 운반한다. 후보 검증은 기존 bounds, 객체 footprint, swept path, 소유권 겹침과 v3 overlay support/cascade를 거친다. 객체 검증은 동일한 resolved 고체를 읽도록 연결했다. 후보용 임시 맵에 고체를 중복 직렬화하던 검증용 셀 추가는 제거했다.

Runtime은 기존 `StageMapRuntimeLoader` → `StageTerrainStructureRuntime`에서 자유형 고체를 기존 operational TilemapCollider2D에 합친다. 아트용 `FreeShapeTerrainRenderer`의 청크 Tilemap에는 Collider가 없다. 셀 내부 구멍을 외접 사각형 Collider로 막지 않는다. `StageMapRuntimeFactory`의 객체 Configure/reset 흐름은 바꾸지 않았다.

## 저장과 이전 정책

`StageMapDefinition.freeShapeTerrain`을 추가했다. `FreeShapeLayer`는 다음을 저장한다.

| 필드 | 의미 |
|---|---|
| schemaVersion | 자유형 저장 버전 1 |
| artContract | ANIMOL_FREE_SHAPE_BLOB47_V1 |
| artVersion | 1 |
| seed | 지속 int32, 기본값 0 |
| cells | x, y, styleId 목록 |

기존 `cells`, tileId/variant, 9방향 enum, v3 `terrainPlacements` schemaVersion=3은 그대로다. 자유형 점유를 기존 일반 셀 목록에 중복 저장하지 않는다. Sprite 인덱스와 렌더 청크는 파생 데이터다.

기존 저장에 새 레이어가 없으면 빈 자유형 레이어로 읽는다. 기존 맵을 일괄 저장·이전하지 않는다. 자유형 데이터가 없는 기존 맵의 content hash 입력은 유지한다. 자유형 데이터/시드/계약이 있으면 별도 hash 입력에 포함한다. 알 수 없는 자유형 schema/contract/version, 중복 좌표, 잘못된 스타일은 저장 전에 실패하며 최신 계약으로 자동 변환하지 않는다.

한 브러시 드래그는 후보만 갱신하다가 mouse-up에서 한 commit·Undo group·authoring revision 증가로 처리한다. 기존 변경 전 백업, 디스크 저장, derived collision 무효화와 실패 rollback을 사용한다. Undo/Redo는 새 레이어 및 v3 overlay cascade를 함께 복원한다.

## 연결·문양·캐시

- 같은 아트 계약의 같은 스타일 자유형 셀만 연결한다. 기존 일반 셀/v3 스탬프와 그림을 자동 혼합하지 않는다.
- N/NE/E/SE/S/SW/W/NW 비트 순서와 직교 이웃을 요구하는 대각선 게이팅을 C#으로 옮겼다. Sprite 선택은 패키지 rawToIndex 및 canonicalMask 배열을 사용한다.
- 위상은 전역 좌표와 저장 시드의 floorMod 식을 사용한다. mask000은 고립된 고체 셀이다.
- 문양은 전역 mod-8 후보, FNV-1a uint32 hash, 동일 스타일 6×6 지지 조건으로 선택한다. 추가 점유/충돌은 없다.
- 변경 셀의 3×3과 `[cx-4,cx+1] × [cy-4,cy+1]` 문양 후보를 갱신한다. 지형만 바뀐 Scene 프리뷰는 기존 아트 캐시를 유지하며 부분 갱신한다.
- 청크는 floor division을 사용한다. 전체 점유로 이웃을 조회하고, 청크 표시 상태는 점유와 연결 판정을 바꾸지 않는다.
- 아트 sorting은 자유형 셀 21, 문양 22다. 기존 일반 terrain 20, v3 내부 장식 30의 상대 순서를 유지한다.

## 일방통행 물리에서 발견한 문제와 수정

새 실제 물리 테스트에서 기존 생성형 `LegacyOneWayCells` Tilemap의 아래쪽 접촉이 막히는 현상을 재현했다. 접촉 Collider 이름과 법선으로 해당 일방통행 소유자임을 확인했다. 같은 Tilemap/정적 Rigidbody2D/PlatformEffector2D 경로에서 CompositeCollider2D로 형상을 합치고 `usedByEffector`를 합성 Collider에 지정했다. 이후 아래에서 상승 통과하고 위에서 하강 착지하는 테스트가 통과했다. 별도의 자유형 일방통행 물리 root를 만들지 않았다.

설정 참고: [Unity TilemapCollider2D API](https://docs.unity3d.com/ja/6000.0/ScriptReference/Tilemaps.TilemapCollider2D.html). 수정의 동작 근거는 프로젝트 Unity의 실제 PlayMode 테스트다.

## 검증 기록

실제 실행 결과는 `Docs/Validation/FreeShape`에 기록한다. 테스트 실패를 숨기지 않으며 초기 실패와 수정 후 결과를 구분한다.

- 초기 EditMode: Grid 누락 처리 오류 및 문양 negative candidate를 positive로 지정한 테스트 오류를 발견했다. Unity 객체의 null 판정을 수정하고, 패키지 hash상 허용되는 문양 원점으로 테스트를 수정했다.
- 수정 후 1차 EditMode: 10/10 통과. 이후 객체 경로 및 시드 회귀 검증을 추가했다.
- 초기 PlayMode: 20스타일 실제 플레이어 착지는 통과, 일방통행 아래쪽 통과는 실패했다. 위 물리 수정 후 해당 테스트 통과.
- JavaScript가 생성한 기준값 256 raw 및 567 좌표/시드/hash 벡터와 실제 Unity C# 결과를 비교한다. JS 기준값 생성 자체를 Unity PASS로 표기하지 않는다.

| 실제 Unity 검사 | 결과 |
|---|---:|
| C# 재컴파일 / 기존 Runtime·Editor asmdef 분리 | 오류 0 |
| FreeShapeTerrainTests (EditMode) | 12/12 |
| TerrainEditorV4Tests | 14/14 |
| TerrainStructureIntegrationTests | 13/13 |
| CommercialPolishPolicyTests | 3/3 |
| CampaignCoreTests | 22/22 |
| M7MapEditorTests | 11/11 |
| M7CampaignMapRunTests | 5/5 |
| FreeShapePlayModeTests | 2/2 |

EditMode 합계 80개, PlayMode 2개가 통과했다. 최종 소스에서 자유형 검사를 재실행했으며, 최종 UI 수정은 접혀 있던 v3 팔레트에서 자유형으로 전환할 때 카드 영역을 펼치는 처리다.

자유형 검사는 3,760개 참조·rect·방향·PPU·pivot·텍스처 설정, 256개 정규화와 567개 JS 기준 벡터, 16개 fixture의 실제 TilemapCollider2D 고체/빈 셀, 음수 좌표·청크 경계·다른 스타일·문양 제거·v3 overlay cascade·객체 footprint/swept path·실패 rollback·시드 저장을 포함한다. 한 셀 수정의 파생 아트 갱신은 3×3 셀과 해당 문양 후보로 제한되는지 검사했다. PlayMode에서는 실제 DevPlayerController의 20스타일 착지와 일방통행 상승 통과/하강 착지를 검사했다.

v3 회귀 첫 실행은 Scene 편집창이 연결된 상태에서 구형 창이 Bridge 소유권을 가져온다고 가정한 1개 테스트가 실패했다. 기존 `CampaignMapEditorWindow.BindTerrainStructures`는 Scene 편집 중 구형 창의 Bind를 생략한다. Scene 창을 닫고 구형 창의 독립 저장/재열기 검사를 실행하여 13/13 통과했다. 이 실패 기록은 `TerrainStructureIntegrationTests-window-conflict.json`에 남겼으며, 테스트를 통과시키기 위해 운영 Bind 정책을 바꾸지 않았다.

`ANIMOL > Terrain Structure > Validate Saved Map Contract`도 실제 메뉴를 실행했다. 이 결과는 v3 순수 C# 저장 계약 검사이며 물리 검증으로 대신 계산하지 않는다.

### Scene 입력과 시험 플레이

- 실제 SceneView.SendEvent 마우스 down/drag/up: revision 11/11/12, 셀 329/329/341. 드래그 중 정본은 유지되고 mouse-up 한 번에 12셀이 저장됐다 (`SceneInput.json`).
- 같은 Scene의 Ctrl+Z/Y, 지우기 클릭으로 닫힌 구멍 만들기, Esc 후보 취소, 연결 덩어리 드래그, Delete, 이동/삭제 Undo와 이동 Redo를 실행했다 (`SceneActions.json`).
- 저장 후 창 닫기·AssetDatabase 재임포트·창 재열기: 340셀, revision 14, 같은 content hash (`Reopen.json`).
- Scene의 `시험 플레이` 버튼에 실제 클릭 이벤트를 보내 기존 Local Play를 시작했다. 동일 hash/340고체, 플레이어 접지, 고체 내부 충돌 있음, 내부 구멍 충돌 없음, 자유형 아트 Collider 0개를 확인했다 (`LocalPlay.json`). 종료 후 Scene Bind가 복구됐다. Play 진입/종료와 C# 재컴파일의 domain reload 후에도 맵 연결이 복구됐다.
- 자동화 입력을 여러 CLI 호출로 나눈 첫 시도는 포커스 상실로 후보가 취소됐다. 같은 입력 묶음으로 재검증했으며 포커스 상실 취소는 정본을 바꾸지 않았다. 좌표가 고정된 초기 검증 스크립트의 실패를 최종 PASS에 포함하지 않았다.

### 작동 화면

모두 실제 Unity 화면을 캡처하고 이미지를 열어 확인했다. 합성 UI 목업이 아니다.

- [Scene 팔레트와 자유형 지형](../Assets/ANIMOL/Screenshots/TerrainFreeShape/SceneEditor.png)
- [연결 Sprite가 적용된 후보](../Assets/ANIMOL/Screenshots/TerrainFreeShape/SceneGhost.png)
- [저장 후 재열기](../Assets/ANIMOL/Screenshots/TerrainFreeShape/SceneReopened.png)
- [기존 Local Play에서 착지](../Assets/ANIMOL/Screenshots/TerrainFreeShape/Runtime.png)

Scene 캡처는 GUIView의 실제 렌더 버퍼, Runtime 캡처는 Game View 화면을 사용했다. Runtime의 파란 직사각형은 기존 개발용 플레이어 표시다.

### 성능 측정

Unity Editor, DEV-FREESHAPE 340셀, 4개 스타일이 섞인 맵에서 측정했다. 빌드된 모바일 성능 수치가 아니다 (`Performance.json`).

| 항목 | 측정값 |
|---|---:|
| 64셀 사각 후보 갱신, 8회 | 14.06~21.76ms |
| commit + 백업 + 검증 + 디스크 저장 + preview + Save | 2,225ms (1회) |
| 기존 RuntimeLoader.Load, 5회 | 8.82~9.93ms |
| 실제 참조 텍스처 | 100개 (80 atlas + 20 motif) |
| Profiler.GetRuntimeMemorySizeLong 합 | 34,168,320 bytes (약 32.59MiB) |

후보 갱신은 입력 이벤트 때 수행하며 프레임마다 전체 맵/셀별 텍스처를 생성하지 않는다. 저장은 동기식이므로 큰 지형/많은 백업 환경에서 지연될 수 있다. 대규모 스트레스 테스트와 모바일 메모리 측정은 아직 하지 않았다.

### 사용자 맵 보존

시작 시 15개 기존 StageMapDefinition의 전체 Editor JSON, content hash, revision, 파일 SHA256을 별도 기록했다. 종료 시 **15/15의 기존 직렬화 필드 전체, content hash, revision이 일치**하고 자유형 셀은 모두 0개다 (`MapPreservation.json`). 사용자 맵/객체/스탬프를 샘플 값으로 덮어쓰지 않았다.

디스크 바이트는 14/15가 동일하다. 시작 때 열려 있던 T01-S02는 빈 `freeShapeTerrain` 기본 필드가 포함되어 재직렬화됐다. 기존 필드는 전체 JSON 대조상 변화가 없고 revision 180/content hash도 동일하다. 기존 회귀 테스트의 `SaveAssets`를 포함한 검증 과정에서 발생한 파일 직렬화 차이이며, 해당 사용자 파일은 이번 커밋에 포함하지 않는다. 15개 모두 파일 바이트까지 같다고 보고하지 않는다.

v3 Data/SourceArt/Generated는 Git 변경이 없으며, 원본 5장/60프레임/40스탬프/20overlay 검사가 통과했다. 기존 스테이지 배정, Ready/보상 설정을 이번 작업에서 수정하지 않았다.

## 변경 파일 범위

메타·생성 에셋까지 포함한 정확한 목록: [ChangedFiles.txt](Validation/FreeShape/ChangedFiles.txt).

- TerrainStructure Runtime: `AnimolFreeShape.cs`, placement model/engine의 자유형 후보·resolve 연결.
- Core: `StageMapDefinition.cs`, `StageMapDefinition.TerrainStructures.cs`.
- Gameplay: `FreeShapeArtRegistry.cs`, `FreeShapeTerrainRenderer.cs`, `StageTerrainStructureRuntime.cs`, `StageMapRuntimeLoader.TerrainOwner.cs`.
- Editor: `FreeShapeArtBuilder.cs`, SceneEditor 및 FreeShape partial, 객체 검증/adapter, 기존 terrain integration.
- Development: 기존 editor view state와 render-only map preview.
- 신규 아트, 데이터, Resources 레지스트리와 Unity meta.
- EditMode/PlayMode 자유형 테스트, 검증 JSON, 별도 DEV 검증 맵과 화면 캡처.

## 남은 검증 범위

모바일 실기기 빌드·성능 및 사람이 판단하는 최종 아트 승인은 별도다. 신규 기능 테스트와 Scene 시험 플레이는 캠페인 운영 Ready/보상 승인이나 스테이지 할당 완료를 의미하지 않는다.

스타일 사이와 v3 사이의 전용 전환 아트는 패키지에 없어 독립된 재료 경계로 표시한다. seed는 저장·Undo·재열기 계약과 테스트를 갖췄고 기본 0을 사용하지만, 이번 패널에는 별도 seed 변경 UI를 추가하지 않았다. 범위 밖 자유형 셀이 남은 청크 축소는 먼저 해당 덩어리를 이동/삭제하도록 거부한다.
