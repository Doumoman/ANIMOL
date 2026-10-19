# ANIMOL 별길 목장 V12 실제 프로젝트 적용 결과

2026-10-19, Unity 6000.3.8f1의 열린 Editor에서 적용했다. 활성 테마 ID는 `starway`이며 전달물의 `starway_meadow`를 저장 ID로 추가하지 않았다. 기존 장치 ID, 격자 좌표, 코스트, 점유 및 다른 세 테마의 공용 프리팹은 유지했다.

## 적용 파일과 실제 생성 경로

| 대상 | 실제 경로와 변경 |
|---|---|
| V12 원화·명세 | `Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12/`: 런타임 PNG 1,945개와 필요한 JSON/TXT. `sources/`, `examples/`, `inspection/`, `Tests/`는 Assets에 복사하지 않았다. |
| 카탈로그·클립 | 같은 폴더의 `_Generated/`: `StarwayRanchV12VisualCatalog.asset`, SpriteFrameSequence 46개, `.anim` 46개. 생성기의 `animation_build_report.json`은 성공, 129 프레임 Sprite와 부품 2개를 기록한다. |
| 임포트·빌드 | `Assets/ANIMOL/Scripts/Editor/`의 V12 원본 생성기·임포터, `StarwayRanchV12ProjectBuilder.cs`, `StarwayRanchV12PrefabMigrator.cs`. 열린 Editor에서 `StarwayRanchV12AnimationBuilder.BuildFromManifest`를 실행했다. 별도 배치 Editor는 실행하지 않았다. |
| 실제 아트 연결 | 기존 GUID의 `Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset`을 V12 지형·배경·소품·장치 및 카탈로그로 갱신했다. `StarwayRanchArt`, `StarwayRanchMapRenderer`와 `PuzzleMapView`가 최신 선택기와 배경을 사용한다. |
| 런타임 생성 | `PuzzleDeviceView`·`DeviceVisualView`가 V12 프레임 키와 권위 상태를 연결한다. `PuzzleDeviceSession`은 확정된 바람 영향 셀만 시각 이벤트로 전달한다. 캠페인의 기존 `DeviceCell` 경로는 `PuzzleMapView`·`PuzzleController`에서 목장 Conveyor/Wind/Brake에 V12 방향 원화를 사용한다. |
| 프리팹·맵 | `Assets/ANIMOL/Prefabs/Themes/starway/` 전용 프리팹 8개와 `Assets/ANIMOL/Data/Puzzle/Prototypes/`의 목장 시나리오 9개를 갱신했다. `StarGarden.asset`에는 몸체와 별도 로터를 쓰는 풍차 장식을 배치했다. |
| 씬 | `Assets/ANIMOL/Scenes/QA/`의 Starway Art/Island/Character 및 Device8 QA 씬 4개를 V12 표시로 갱신했다. 실제 런타임 퍼즐 씬은 기존 `ANIMOL_Puzzle.unity`, 프로토타입은 기존 `ANIMOL_starway_Prototype.unity`를 사용한다. |

장치 대응은 [PROJECT_BINDING_REPORT.md](PROJECT_BINDING_REPORT.md)와 [실제 바인딩 JSON](Validation/StarwayRanchV12/device_bindings_project.json)에 소스 위치, 기존 역할·ID·상태 이벤트, Renderer 및 프리팹을 기록했다. 공통 07/09/18/19, 목장 01의 돌·홈, 02의 누름판·다리, 20의 본체·별도 영향 범위, 24의 병아리를 대응시켰다. 장식 풍차와 장식 문은 장치로 바인딩하지 않았다. Animator와 V12 재생기가 동일 Renderer를 동시에 쓰지 않도록 목장 전용 시각 프리팹에는 Animator를 두지 않았다.

## GUID/fileID와 레거시

적용 전에 [V10 의존성 dry-run](Validation/StarwayRanchV12/legacy_dependency_dry_run.md)을 작성하고 old PNG 1,816개의 GUID/fileID·경로·V12 대응을 [CSV](Validation/StarwayRanchV12/legacy_guid_fileid_dry_run.csv)에 보관했다. 기존 목장 Art asset과 시나리오의 GUID를 유지하고, 새 V12 PNG는 새 GUID/fileID로 명시적으로 다시 연결했다. [프리팹](Validation/StarwayRanchV12/prefab_guid_fileid_mapping.csv), [프레임](Validation/StarwayRanchV12/catalog_frame_guid_fileid.csv), [클립](Validation/StarwayRanchV12/catalog_clip_guid_fileid.csv) 대응표도 남겼다.

전체 활성 Assets YAML의 이전 목장 Sprite GUID 참조는 0건이며, 프로토타입 Play Mode Renderer 272개와 캠페인 `StarGarden` Play Mode Renderer 392개에서 이전 목장 경로 참조는 각각 0건이었다. V10 PNG 1,816개, 더 이상 사용하지 않는 V10 선택기·배경 런타임 5개, V10 Builder/Importer, 구 JSON 6개를 목장 범위에서 제거했다. 다른 테마와 공유하는 일반 장치 프리팹은 보존했다. 삭제 전 파일·메타 백업은 Assets 밖 `C:\Users\user\Documents\GitHub\ANIMOL_StarwayV12_Before_20261019.zip` 등에 보관했다.

## 실행 검증

| 검사 | 실제 결과 |
|---|---|
| 오프라인 패키지·프로젝트 감사 | `python Tools/StarwayRanchV12/audit.py` 성공. [결과 JSON](Validation/StarwayRanchV12/final_file_audit.json): 런타임 PNG 1,945개 SHA 불일치 0, 카탈로그 SHA 일치, 46 클립·46 시퀀스, 잔존 V10 PNG 0, 활성 YAML의 이전 Sprite GUID 참조 0. |
| Unity 생성·컴파일 | 열린 Editor에서 생성 성공. `_Generated/animation_build_report.json`의 source hash는 `f9b0c4f03def0023afa13c201266e899d108c872126cb8801425a5c6c85b42ef`로 sidecar와 일치. 최종 [콘솔 상태](Validation/StarwayRanchV12/unity_console_status.json)는 compile failure=false, error=0, warning=0. |
| TestRunner EditMode | [V12 4/4](Validation/StarwayRanchV12/unity_editmode_tests.json), [장치 규칙 43/43](Validation/StarwayRanchV12/unity_device_logic_tests.json) 통과. V12 검사에는 키 누락 시 진단하고 구 스프라이트를 복원하지 않는 음성 검사가 포함된다. |
| TestRunner PlayMode | [V12 6/6](Validation/StarwayRanchV12/unity_playmode_tests.json), [프로토타입 3/3](Validation/StarwayRanchV12/unity_prototype_play_tests.json), [캠페인 퍼즐 6/6](Validation/StarwayRanchV12/unity_campaign_play_tests.json) 통과. 기존 ArtAssetTests 4/4, ArtQaPlayTests 1/1도 실행해 통과했다. |
| 실제 Play Mode 시각 점검 | [프로토타입](Evidence/StarwayRanchV12/prototype_play_1080x1920.png), [캠페인](Evidence/StarwayRanchV12/campaign_play_1080x1920.png), [풍차](Evidence/StarwayRanchV12/campaign_windmill_play_1080x1920.png), [4방향 무빙워크](Evidence/StarwayRanchV12/conveyor_four_headings_play_1080x1920.png), [4방향 병아리](Evidence/StarwayRanchV12/chick_four_headings_play_1080x1920.png), [중간 프레임](Evidence/StarwayRanchV12/v12_directional_midframes_play_1080x1920.png)을 1080×1920 Play Mode에서 캡처했다. 마지막 세 장은 Play Mode에서 수동 상태·프레임을 선택한 시각 미리보기이며 실제 퍼즐 행동 영상이 아니다. |
| QA 카메라 | `StarwayRanchValidation.Capture()`로 [1080×1920 및 카메라 상하좌우·배율 5/25 캡처](Validation/StarwayRanchV12/)를 생성했다. 이는 QA 장면 캡처이며 모바일 실행 결과가 아니다. |

## 남은 범위와 미확정 규칙

- 현재 활성 프로젝트에는 목장 전용 PvP 진입, Addressables/원격 카탈로그/AssetBundle 경로가 확인되지 않았다. 해당 경로의 실제 생성 인스턴스와 저장 로드·풀 재사용은 실행 검증하지 않았다.
- 전체 도감 조합의 행동별 Play Mode 영상과 실제 모바일 빌드·실기기 검사는 수행하지 않았다. 네 방향과 중간 프레임의 캡처는 위 범위까지만 증명한다.
- 공용 금속 상자는 전달된 V12 전용 원화가 없어 기존 공용 표현을 유지한다. 캠페인의 `Ice/Portal`도 대응 V12 장치 원화가 없어 기존 표시를 유지한다.
- 수축 중 다리 위 대상 보호, 병아리의 포털·도약·배·둥지 세부 규칙은 제공 문서만으로 확정하지 않았으며 이번 시각 작업에서 새 동작을 만들지 않았다.
- 카탈로그 생성기의 `projectBindingsVerified=false`와 `legacyReferenceAuditComplete=false`는 생성기 자체의 범위 표기다. 실제 바인딩·레거시 검사는 위 프로젝트 대응표와 별도 감사 결과에 기록했다.
