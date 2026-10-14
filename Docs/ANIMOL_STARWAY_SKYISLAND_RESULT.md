# 별길 목장 하늘섬 외곽 V02 적용 결과

2026-10-14 / Unity 6000.3.8f1 / Built-in Render Pipeline / Linear color space.

## 적용 범위와 사전 확인

작업 시작 시 실제 활성 씬은 `Assets/ANIMOL/Scenes/ANIMOL_Puzzle.unity`, 맵은 `Data/Puzzle/Maps/StarGarden.asset`이었다. 맵에는 지형 418개가 있으며 `PuzzleMap.terrain`이 지형 점유의 기준이다. 장치·장식·방울 목록은 별도다. 기존 지형은 셀마다 네 개 SpriteRenderer로 표시하고 있었고 Tilemap / RuleTile / Sprite Atlas는 사용하지 않았다. 기존 아트 QA 씬은 플레이 가능한 퍼즐과 구분했다.

별길 목장의 `StarGarden` 퍼즐 테마에만 새 아트 카탈로그를 연결했다. 기존 맵·씬·HUD 프리팹·스와이프·이동·충돌 판정·수집·출구 코드는 수정하지 않았다. 다른 테마/공통 장치도 복원하거나 수정하지 않았다. [범위 감사](Validation/StarwayIsland/pixel_and_scope_audit.json)는 보호 파일 2,168개의 해시를 비교했고 기존 런타임 파일 중 변경된 것은 시각 연결용 `PuzzleTheme`, `PuzzleMapView`, `PuzzleBackdrop` 세 파일뿐이다. 별도 변경한 기존 편집기 파일은 `PuzzleMapEditor.cs`다. `StarGarden.asset`의 diff는 `skyIsland` 참조 한 줄이다.

## 원본과 임포트

원본 ZIP은 사용자 제공 `MCP_INBOX/ANIMOL_STARWAY_SKYISLAND_V02.zip`이다. 압축 해제본을 [Docs/Inbox/StarwaySkyIsland](Inbox/StarwaySkyIsland/ANIMOL_STARWAY_SKYISLAND_V02/README_KO.txt)에 보관했다. README, manifest, qa_report를 읽고 SHA256SUMS의 190개 파일을 확인했다. 해당 폴더의 `.gitattributes`로 텍스트/팔레트의 자동 개행 변환도 막아 체크아웃 이후의 원본 바이트를 보존한다. 패키지 `qa_report.json`의 PASS는 원본 PNG 검사이며 아래 Unity 실행 결과와 구분한다.

149개 PNG를 원본 바이트 그대로 가져왔다. 타일 142개(Grass/Underside 각각 outer16 16, inner4 4, inner4_samples 4, blob47 47), 배경 3개, 선택 장식 4개다.

- `Sprites/Tiles/Floor/ranch/SkyIsland/{Grass,Underside}`
- `Sprites/Background/ranch/SkyIsland`
- `Sprites/Tiles/Decoration/ranch/SkyIsland`
- `Data/Art/StarwayIsland`: 47쌍의 Tile 자산, `IslandArt.asset`, 검사 맵

모두 Sprite / Single / PPU80 / Pivot(.5,.5) / Full Rect / Point / Uncompressed / Mipmap off / NPOT None이다. Android/iPhone/Standalone/WebGL 압축 override가 없다. 투명 RGB를 임포터가 확장하지 않도록 `alphaIsTransparency=false`를 사용했다. 원본 투명 여백·80×80 캔버스·RGBA를 유지하고 trim, 회전, 반전, 재보간, 재압축을 하지 않았다. Atlas는 사용하지 않는다. 타일 본체 6색과 기타 자료의 SWEETIE16 팔레트, 이진 알파, 투명 RGB 0을 [바이트/픽셀 감사 도구](../Tools/StarwayIsland/audit.py)로 확인했다.

## 지형 표시

`SkyIslandAutotile`은 `TryTerrain`에서 실제 인바운드 지형 존재 여부를 읽는다. 레벨 바깥은 빈칸이다. o는 노출 N1/E2/S4/W8, i는 두 직교 이웃이 있고 대각선만 없을 때 NW1/NE2/SE4/SW8이다. 숫자가 같은 Grass/Underside blob47 쌍을 직접 선택하며 회전이나 connected-mask 추측을 하지 않는다. 기존 RuleTile이 없으므로 변환 경로도 없다. 기존의 다른 코너 비트 순서와 혼용하지 않는다.

한 Grid 아래 두 Tilemap을 만든다. 셀 간격은 X/Y 1unit, Grass 중심은 논리 셀 좌표다. Underside의 local Y는 정확히 -14/80=-0.175unit, sortingOrder -20이며 Grass 전체의 order 0보다 먼저 렌더링한다. 두 레이어와 Tile 자산 모두 Collider가 없다. 지형 점유나 이동 가능 여부를 기록하지 않는다. 셀별로 Grass 뒤에 단면을 붙이지 않는다.

새 그림은 비차단 Grass에만 적용한다. Stone/Sand 및 차단 셀은 기존 표현을 유지한다. 새 섬 외곽 마스크의 이웃은 다른 재질/차단 지형도 실제 지반 점유로 취급한다. 재질 구분·차단 여부 자체는 기존 판정과 그대로다. 재질 경계 검수 화면에는 기존 Stone/Sand 표현과 경계의 기존 어두운 띠가 남아 있다.

편집기 미리보기에서도 배경 셀 → 단면 전체 → 잔디 순서로 그린다. 편집/Undo/Redo/리사이즈는 최신 점유에서 그림을 다시 선택한다. 런타임 변경은 기존 3×3 알림으로 변경 셀과 주변 8칸을 갱신한다. 리사이즈 시 범위 밖 데이터를 유지하는 기존 정책을 보존했다. 화면 주변 Tilemap 셀만 스트리밍하며 마스크는 전체 점유에서 계산한다.

기본 경로는 blob47이며 inner4 패치를 알파 합성하지 않는다. 선택 격자는 끈 상태다. context_props는 임포트만 했으며 임의 배치하지 않았다.

## 배경과 픽셀 표시

Far → Mid → Near → Underside → Grass → 기존 장치/장식/캐릭터 → 기존 Overlay HUD 순서다. 960×960 원본을 X/Y 양축으로 반복하며 화면 밖에도 복사본을 둔다. 배경 Sprite transform scale은 1이다. 구름을 맵 셀마다 장식으로 생성하지 않는다.

원본 카메라의 이동·클램프·11행 시야·HUD 예약 viewport는 유지한다. 시각 전용 캡처 카메라가 80px/unit으로 그림을 만들고, Point 샘플링으로 정수 확대한다. 실제 플레이어/원본 카메라 transform은 픽셀 표시기가 수정하지 않는다. 배경은 별도 네이티브 버퍼에서 반복하며 같은 정수배로 표시한다. 연출용 상하 흔들림, 독립 진동, 점광원, 발광, 파티클, UI를 추가하지 않았다.

최종 배경 샘플은 카메라의 오른쪽/아래쪽 원본 픽셀 좌표 `(worldX*80,-worldY*80)`에 .15/.35/.65를 곱하고 정수 반올림한 뒤 floor-mod 960을 적용한다. 카메라가 정지하면 배경도 정지한다. 고정된 배경 판 뒤에서 카메라 이동에 따른 서로 다른 깊이의 시차만 발생한다. 이번 명시된 반복/시차 기준을 별길 목장에 적용했고 다른 테마의 기존 배경 경로는 유지했다.

| 카메라 px (오른쪽/아래) | Far phase | Mid phase | Near phase |
|---|---|---|---|
| (0,0) | (0,0) | (0,0) | (0,0) |
| (640,480) | (96,72) | (224,168) | (416,312) |
| (-400,960) | (900,144) | (820,336) | (700,624) |

정수 확대가 기존 viewport에 딱 맞지 않으면 배경 여백을 사용한다. 양축 초과가 각각 5% 이하일 때만 다음 정수배의 가장자리 crop을 허용한다. 1080×1920에서는 native 546×880을 2배로 표시하며 1092×1760 중 좌우 6px/상하 10px가 기존 gameplay viewport 밖으로 잘린다. 전체 맵 fit-to-screen 확대/축소는 하지 않는다. 720×1280은 1배, 1080×2400은 2배와 배경 여백이다. [세로 크기 기록](Validation/StarwayIsland/portrait_sizes.json)을 참조한다.

## 실제 실행 검증

최종 출력은 특정 카메라의 `Camera.Render` 캡처가 아닌 Unity Play mode의 **Game View / ScreenCapture**로 얻었다. 배경과 지형의 네이티브 버퍼는 런타임 표시 구현이며, 증거 PNG는 그 버퍼를 다시 저장한 파일이 아니라 최종 화면이다.

| 검사 | 실제 결과/근거 |
|---|---|
| 한 칸 섬, 1칸 통로, 구멍, U자/오목/볼록 접합 | [join_1x](Validation/StarwayIsland/join_1x.png) 880×654와 [join_4x](Validation/StarwayIsland/join_4x.png) 3520×2616. 제공 inspection 이미지와 RGB 차이 0픽셀. 배경 투명 부위는 검은 clear로 비교 |
| 네 방향 끝점, 네 안쪽 코너 | [four_endpoints](Validation/StarwayIsland/four_endpoints.png). runtime 복제 검사 맵의 십자와 1칸 팔. 끝점 o11/o07/o14/o13, 중앙 o00_i15. 그림을 회전/반전하지 않음 |
| 세 배경 위치와 음수 floor-mod | [A](Validation/StarwayIsland/background_A.png), [B](Validation/StarwayIsland/background_B.png), [C](Validation/StarwayIsland/background_C.png). 모두 제공 composite와 RGB 차이 0픽셀. 반복 경계를 통과해도 빈틈 없음 |
| 작은 맵 중앙, 긴 가로/세로 맵 경계 | [실행 기록](Validation/StarwayIsland/Portrait/gameview.json), Small/Horizontal/Vertical. 11행 시야 유지, 연쇄 종료 x31/y31, 맵 끝 클램프 |
| 스와이프, 수집, 출구 | Small에서 기존 `PuzzleSwipeInput.BeginGesture/EndGesture`를 통해 한 칸 스와이프 수행, 방울 1개 수집 후 출구 도착 승리. 이동 판정/입력 임계값 변경 없음 |
| 1칸 통로/섬/U자/재질 경계/런타임 변화 | Portrait 폴더의 corridor/u_inner/material_boundary/runtime_terrain 캡처. 제거 시 9칸 갱신, 원본 맵 유지, 기존 이동 가능/차단 플래그 유지 |
| 맵 끝 포털 | [출구 전환 프레임](Validation/StarwayIsland/Portrait/portal_exit_cut.png), [미끄럼 끝](Validation/StarwayIsland/Portrait/portal_slide_end.png). cut 1회, 보존된 오른쪽 방향, (39,5) 도착/방울 수집. 먼 중간 영역을 카메라가 가로지르지 않음 |
| 세로 출력 | [720×1280](Validation/StarwayIsland/portrait_720x1280.png), [1080×1920](Validation/StarwayIsland/Portrait/starway_applied.png), [1080×2400](Validation/StarwayIsland/portrait_1080x2400.png). 알파 테두리/타일 틈/겹친 단면 없음. 화면 밖 맵과 정수 버퍼 경계 crop은 의도된 viewport 처리 |
| 전체 EditMode | [63/63 Passed](Validation/StarwayIsland/editmode.json). ArtAssetTests, 장치/다른 테마, PuzzleMapTests, 새 SkyIslandArtTests 포함 |
| 전체 PlayMode | [15/15 Passed](Validation/StarwayIsland/playmode.json). ArtQaPlayTests, 장치/다른 테마, 기존 PuzzlePlayTests, 새 SkyIslandPlayTests 포함 |

새 EditMode 테스트는 256개 모든 이웃 조합이 47개 정확한 쌍으로 귀결되는지, 오브젝트 점유 분리, 149개 실제 임포터 설정, 음수 phase를 확인했다. 새 PlayMode 테스트는 지형 편집/리사이즈/복구/테마 해제 시 그림과 Collider 부재를 확인했다. 세로 플레이 검사는 19개 캡처, 7,310개 이동 프레임, 27개 체크를 기록했으며 매 프레임 클램프와 원본 카메라에서 계산한 시차를 확인했다. 입력 차단은 false, timeScale은 1이었다.

첫 전체 EditMode 요청은 MCP 시작 콜백 timeout으로 결과를 얻지 못했다. 시작 제한을 120초로 재실행하여 위 63/63 결과를 얻었다. 기존의 중복 `Rebuild memory theater QA` MenuItem 경고는 별도 기존 문제이며 수정하지 않았다. 보관된 CommercialPolishPolicyTests는 활성 대상이 아니므로 실행하지 않았다. 이를 새 테스트의 통과 수에 포함하지 않는다.

## 남은 범위와 표현 누락

- 물리 모바일 기기/Android·iOS 빌드 실행은 하지 않았다. 세로 화면 검증은 Unity Editor의 실제 Game View 해상도이다. 모바일 GPU 성능과 실제 Safe Area는 별도 기기 검사가 필요하다.
- 현재 퍼즐의 코스트 시스템은 미연결이고 HUD의 `COST --`가 그대로다. 코스트 동작이 구현/통과되었다고 주장하지 않는다.
- 현재 퍼즐 장치는 `PuzzleMapView.DrawDevice`의 글리프로 표시한다. 보관된/다른 테마의 장치 아트 프레젠터를 이번 지형 교체 때문에 새로 연결하지 않았다. 입력→준비→행동→변화→결과→정착의 완전한 장치 연출은 현재 플레이 씬에 연결되어 있지 않으며 별도 표현 누락이다. 기존 장치 코드/순서는 변경하지 않았다.
- 1080×2400에서는 기존 방울 HUD 패널 오른쪽이 일부 잘린다. [기존 배경 경로로 캡처한 동일 HUD](Validation/StarwayIsland/baseline_tall_hud.png)에서도 같은 잘림을 확인했다. Overlay Canvas/프리팹/배치를 유지했으며 이번 작업에서 UI를 변경하지 않았다.
- 새 표시기는 Built-in 파이프라인을 대상으로 한다. URP 및 외부 카메라 스택과의 통합은 검증하지 않았다. 정수 확대를 위해 배경/전경 RenderTexture를 별도로 유지한다.

## 재현과 수정 파일

`ANIMOL/Art QA/Apply Starway island perimeter`는 현재 패키지에서 아트 카탈로그와 검사 씬을 만든다. 기존 대상 PNG가 원본과 다르면 덮어쓰지 않고 중단한다. 활성 폴더/파일에는 버전 접미사를 붙이지 않았다.

`ANIMOL_Starway_Island_QA.unity`를 열고 Play한 뒤 Game View를 880×654 또는 3520×2616으로 설정하면 접합 검사를 재현한다. 이는 그림 검사 씬이며 퍼즐 동작 검사는 `ANIMOL_Puzzle.unity`에서 수행했다. 세로 검사 재실행은 Play 중 `SkyIslandGameViewValidation.Start()`를 호출한다(1080×1920). 바이트/팔레트/캡처/보호 파일 재검사는 저장소 루트에서 `python -X utf8 Tools/StarwayIsland/audit.py`를 실행한다.

기존 수정: Runtime의 `PuzzleTheme.cs`, `PuzzleMapView.cs`, `PuzzleBackdrop.cs`; Editor의 `PuzzleMapEditor.cs`; `Data/Puzzle/Themes/StarGarden.asset`; 새 활성 아트 경로를 덧붙인 `Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md`.

신규 코드: Runtime `SkyIslandArt.cs`, `SkyIslandMapRenderer.cs`, `SkyIslandBackdrop.cs`, `SkyIslandInspection.cs`; Editor `SkyIslandBuilder.cs`, `SkyIslandGameViewValidation.cs`; Editor/PlayMode `SkyIslandArtTests.cs`, `SkyIslandPlayTests.cs`; `Materials/SkyIslandComposite.shader/.mat`.

신규 데이터/그림/씬: 위 SkyIsland 세 PNG 경로, `Data/Art/StarwayIsland`, `Scenes/ANIMOL_Starway_Island_QA.unity`. 신규 출처/검증 자료는 `Docs/Inbox/StarwaySkyIsland`, `Docs/Validation/StarwayIsland`, `Tools/StarwayIsland`이다. 검사 뒤 원래 퍼즐 씬과 1080×1920 Game View로 복귀했고 원래 씬은 dirty 상태가 아니다.
