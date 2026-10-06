# ANIMOL theme_finish V6 적용 결과

## 설치 범위

- 대상: Unity 6000.3.8f1, ANIMOL 실제 프로젝트. 검증 렌더러는 Direct3D11.
- 원본: `Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6` 전체 패키지. 원래 ZIP은 사용자 파일로 보존하고 커밋에 혼입하지 않음.
- Runtime PNG: `Assets/ANIMOL/TerrainFreeShape/V6/Art/Atlases` 80개, `Art/Motifs` 20개만 설치. 개별 셀·원화·NativeV6 분면·Samples·HTML은 Runtime으로 설치하지 않음.
- Registry: `Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV6.asset`. 설치는 기존 `FreeShapeArtV2Builder.InitializePackage`를 재사용하는 `FreeShapeArtV6Builder.Initialize`로 수행.
- contract/artContract `ANIMOL_FREE_SHAPE_BLOB47_V1`, 자유형 schema 1, 아트 버전 6. 별도 terrainPlacements schema 3과 구분.
- 5테마, 20 styleId/이름, 47 canonical mask, 256 raw mask, 4 전역 위상, 3,760 Sprite를 유지. V0~V3는 위상이며 아트 버전이 아님.
- T01_A는 동봉된 V5 원본 PNG를 그대로 설치. 나머지 19스타일은 V6 실제 아틀라스만 참조. 이 설치는 이전 버전 에셋을 공유하지 않고 V6 경로에 완전한 100개 PNG를 복사함.

## 기존 구조와 변경 경로

| 경로 | 역할 / 변경 |
|---|---|
| `TerrainStructure/Runtime/AnimolFreeShape.cs` | FreeShapeLayer 저장 계약 및 버전 지원. 전역 점유, mask, 위상, floor division, motif hash/support 계산은 유지 |
| `Scripts/Gameplay/FreeShapeArtRegistry.cs` | 버전별 Resources Registry 로드. schema 오류·잘못된 계약·알 수 없거나 미설치된 버전은 예외. fallback 없음 |
| `Scripts/Editor/FreeShapeArtV2Builder.cs` | 기존 버전별 설치 경로 확장, V6 전체 셀 digest 추가 검사 |
| `Scripts/Editor/FreeShapeArtV6Builder.cs` | 명시적 V6 설치 진입점. 자동 실행·맵 전환 없음 |
| `Scripts/Editor/TerrainEditorV4/TerrainEditorSceneEditor.FreeShape.cs` | 스타일 실루엣/재료 썸네일/브러시 프리뷰가 선택 맵 Registry를 사용. 브러시의 V4 강제 덮어쓰기 제거. 현재 맵 아트 버전 선택 UI |
| `Scripts/Editor/TerrainEditorV4/TerrainEditorSceneEditor.cs` | 세션 종료 시 fixture 캐시 해제. 기존 Scene 편집기 재사용 |
| `Scripts/Editor/TerrainEditorV4/TerrainEditorAdapter.cs` | 기존 ChangeFreeShapeArtVersion → Validate → Commit 경로 재사용 |
| `TerrainStructure/Editor/AnimolTerrainMapEditorBridge.cs` | 기존 Undo/Redo, 정본/디스크/프리뷰 실패 롤백 재사용 |
| `Scripts/Core/StageMapDefinition.TerrainStructures.cs` | 기존 점유/객체 footprint/swept path/버전 검증 및 revision·hash 처리 유지 |
| `Scripts/Gameplay/FreeShapeTerrainRenderer.cs` | 기존 Registry 변경 감지에 따른 전역 셀/motif 재해석 재사용. 아트 Collider 추가 없음 |
| `Scripts/Gameplay/StageMapRuntimeLoader.cs`, `StageTerrainStructureRuntime.cs` | 기존 operational terrain 물리 소유자 유지 |

현재 맵 테마의 4스타일만 팔레트에 표시한다. 서로 다른 테마의 스타일을 허용하도록 기존 배치 규칙을 바꾸지 않는다. 기존 캠페인 브라우저·마커·Ready/보상 검증을 유지하며 별도 에디터나 추가 ANIMOL 메뉴를 만들지 않는다.

## 파일·아트 검증

- `Manifest.json`: PACKAGE_MANIFEST의 6,568개 상대 경로/크기/SHA256 전수 검사 PASS. manifest 자체와 전달 ZIP의 SHA도 기록.
- 3,760셀 digest: `a6bd64ffcf58e8b5eb82760084c6721369de1ca4e96f47ac14c47ef2737b48ff` 일치.
- `Data/native_v6/T01~T05.json`, native registration, source_rects, lookup, topology/style catalog, 보존 manifest 및 제작 도구의 등록 경로를 확인했다. 색으로 body/decoration을 추정하는 Runtime 경로는 추가하지 않았다.
- 완성 PNG 재양자화·색 제거·추가 외곽선·후처리 Shader 없음. 원래 Sprites/Default material 사용.
- 아틀라스 Multiple, 32×32 rect, 중앙 pivot, PPU32, FullRect. y는 `atlasHeight-y-height`로 변환.
- 문양 Single, bottom-left pivot, PPU32, 4×4셀 시각 크기. Point/압축 None/mipmap off/sRGB/NPOT None. 문양 추가 점유·Collider 0.

## 패키지 Node 실행 (Unity 결과와 별개)

원본 패키지의 과거 검증 보고서를 보존하기 위해 동일 바이트의 Temp 복사본에서 실행하고 새 결과를 `Docs/Validation/ThemeFinishV6/Node`에 수집했다. 최종 실행 환경은 Node **24.19.0**, npm 11.13.0, sharp 0.35.4다.

| 실제 실행 | 결과 |
|---|---|
| `npm run validate:theme -- --packaged-baseline` | PASS: 20스타일, 3,760셀, 1,600분면, 640 small-hole cases. 픽셀/atlas/role-port mismatch 0 |
| `npm run validate:art` | PASS: 80 atlas, 3,760 PNG, palette/binary alpha. 별도 과거 원본 대조 항목은 SKIPPED |
| `npm run validate:topology` | PASS: 256 raw, 65,536개의 4×4 점유, 524,288 cell contexts, 393,216 shared edges |
| `npm run validate:gallery` | PASS: 논리/atlas render plan/JS 문법. 브라우저 이벤트 실행은 미실행 |

별도 V5 패키지는 없다. T01_A PNG와 메타데이터, 유지 문양은 `Data/v5_preservation_manifest.json`의 정확한 PNG/RGBA SHA256 및 전체 T01_A 메타데이터로 대조했으며 T01_A byte mismatch는 0이다. V5 이하 과거 보고서를 V6 PASS 근거로 재사용하지 않았다.

완성 PNG를 설치했으므로 `build:native → build → gallery → build:approved-gallery` 재생성은 이번 작업에서 실행하지 않았다. `build:comparison`도 미실행(별도 V5 소스 필요). 제작 소스/문양/재생성 도구는 패키지 그대로 보존한다.

## Unity 실행 및 캡처

- 실제 컴파일 완료. 설치는 6,568 파일 검증 → 80 atlas/3,760 Sprite/20 motif 생성 완료. 첫 CLI 요청은 서버의 5초 응답 제한으로 timeout을 반환했으나 Unity 설치 작업은 계속 진행되어 605.2초 후 정상 완료했다. 완료 파일 및 실제 Registry를 별도로 확인했다.
- EditMode 최종 28개 PASS: V6 참조/임포트/버전 선택/전수 렌더 5개, 기존 FreeShapeTerrainTests를 V6로 실행한 20개, CommercialPolishPolicyTests 3개.
- 반복 구멍 편집은 첫 실행에서 NUnit 기본 180초 한도를 초과했다. 전용 테스트에 600초 제한을 명시한 후 동일한 전체 20스타일·각 2회 생성→확대→채우기를 재실행하여 PASS. 초기 실패 기록도 삭제하지 않았다.
- `ArtParity.json`: 20스타일 × 23형상 × Scene/Runtime = 920개의 1× 캡처 + 전 스타일 1×1 구멍의 Scene/Runtime 8× 40개, 총 **960개 / 64,348,160픽셀**. RGB/alpha 차이 모두 **0**.
- 23형상에는 패키지 16종(큰 구멍, 1셀 벽/천장/바닥, 계단, U자, T분기, -16/0/16 청크 경계 등)과 3×3 가운데1×1 구멍의 네 seed0 위상, 음수 시작/고정 seed, 4×4 가운데2×2 구멍을 포함했다.
- 비교 기준은 `BuildThemeFinishV6Expectations.mjs`가 패키지의 실제 atlas/motif 및 topology/composition 모듈로 별도 조립했다. Unity의 Renderer/Registry를 기대 이미지 생성에 사용하지 않았다. Runtime 설치에 기대 이미지나 Samples를 사용하지 않았다.
- Unity 전수 참조 검사는 3,760개의 독립 GUID/local file ID, 80 atlas, 20 motif, 실제 패키지 PNG SHA, rect/pivot/PPU/FullRect/Point/None/sRGB를 검사했다.
- 위상/hash C# 대조는 V6 패키지 JS에서 새로 생성한 256 raw 및 1,029개의 음수·정수 경계 좌표/seed 벡터를 사용했다.
- 모든 테스트 렌더와 기준 이미지는 `Validation/ThemeFinishV6/Renders`, `Expected`에 보관. 모든 스타일의 원점 seed0 작은 구멍 8× 이미지를 직접 확인했다. 확인 범위는 `VisualReview.json`에 기록했다.
- PlayMode **3/3 PASS**: 실제 DevPlayer가 20스타일에 착지, 기존 one-way 상승 통과/낙하 착지, 20스타일의 음수 좌표 빈 구멍 내부에서 물리 body가 내려가 안쪽 바닥에 착지. 아트 소유 Collider는 0.
- 최종 고유 Unity 테스트 **31/31 PASS (EditMode 28 + PlayMode 3)**. `UnityFinal.json`에 각 테스트의 최신 실행 결과와 원본 실행 보고서를 연결했다.
- `SelectedMap.json`: 새 `Assets/ANIMOL/Data/Development/ThemeFinishV6/DEV-THEME-V6-COPY.asset`를 기존 DEV-FREESHAPE에서 복사하고 개발 복사본의 고유 stageId만 부여했다. 이후 기존 트랜잭션으로 V1→V6 전환. **revision 22→23**, hash 변경, 나머지 terrain DTO 동일, Undo/Redo 디스크 바이트 동일, 원본 파일 불변. 330셀과 객체 1개 유지. 구조 검증 PASS.
- 실제 편집기의 시험 플레이 경로로 이 저장/재열기 복사본을 실행했다. `SelectedMapPlay.json`: PlayMode/testing=true, DEV-THEME-V6-COPY/artVersion6, 330셀, 아트 Tilemap 8개, 플레이어 1개, 아트 Collider 0. 종료 후 Scene 편집기로 복귀했다.

- 최종 Unity 콘솔: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=1. 시험 플레이의 기존 `START/START` 객체에 대해 런타임 로더가 unsupported 경고를 출력했다. 객체는 그대로 보존했고 해당 객체 유형의 런타임 지원은 이번 아트 패치에서 변경하지 않았다. 공용 설치기의 기존 obsolete `TextureImporter.spritesheet` API 경고도 남아 있으며 실제 Sprite 임포트 검증은 통과했다.

## 선택 맵 및 보존

전환 대상은 `DEV-FREESHAPE`의 새 복사본 `DEV-THEME-V6-COPY`로 한정한다. 기존 캠페인/개발 맵 원본의 좌표·객체·스탬프·seed·점유·셀 크기·Ready 상태를 자동 변경하지 않는다. 기존 설치 V1/V2/V4 Registry·PNG·meta/GUID와 사용자 미커밋 변경을 보존한다. 기존 에셋/맵/설정 1,110개 파일의 SHA256은 변경 0이며, 따라서 기존 sprite GUID/local file ID 및 GlobalObjectId를 재할당하는 임포트도 수행하지 않았다 (`Preservation.json`). V3/V5 Registry는 대상에 미설치되어 선택 대상에서 제외하며 로드 요청 시 거절한다.

## 검수 한계

- V5 ↔ V6 선택·저장·재열기·플레이 비교는 V5 Registry/전체 패키지가 없어 미실행. 기존 설치 버전 ↔ V6 전환을 별도 검사한다.
- 전체 가능한 맵 조합의 미관, 다른 styleId 사이의 자연스러운 혼합 접합, 모든 jaggy/double의 자동 판정은 승인하지 않는다.
- Node의 등록 제작 영역/실제 셀/공유 경계 PASS는 Unity 물리 PASS나 모든 맵의 미관 자동승인이 아니다.
- 의도된 명암 띠·둥근 모서리 픽셀 절개는 원본대로 유지한다. 단순 색상 성분 통계를 근거로 삭제하지 않는다.

검증 산출물은 `Docs/Validation/ThemeFinishV6`에 모았다. Git의 텍스트 줄바꿈 변환으로 패키지 SHA가 바뀌지 않도록 V6 소스 전체에 `-text` 속성을 적용하고, 스테이징된 6,568개 파일도 manifest와 다시 대조했다. 직접 작성한 코드/문서는 diff whitespace 검사 PASS이며, Unity 생성 meta와 전달 패키지의 원래 공백은 유지했다.
