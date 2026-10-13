# ANIMOL legacy cutover — 2026-10-13

플랫포머 파일을 참조 관계에 따라 분리하는 작업이다. 탑뷰 퍼즐 구현이나 가상의 P01 씬을 만들지 않는다.

## 실제 조사

- 최초 인벤토리: Assets 파일 4,989개, GUID 2,632개, asmdef 52개. 중복 GUID 0개.
- `git status --porcelain=v1 -z --untracked-files=all` 기준 기존 변경 1,736개(이번 작업의 조사 도구 제외). 미추적 폴더를 펼친 파일 수이므로 `git status --short`의 행 수와 다르다.
- 탑뷰/스와이프 시제품과 P01 QA 씬은 실제 파일 검색에서 발견되지 않았다.
- Phase1은 동물 업그레이드 UI로 존재한다. `UI/AnimalUpgradePhase1` 및 `AnimalUiV2`의 실제 공용 UI로 분류하고 유지한다.
- Unity 6000.3.8f1 배치 모드의 작업 전 재임포트·컴파일 성공.

## 이동과 유지

정확한 원본→대상 경로, GUID, 유지 이유, 차단 파일은 [기계 판독 인벤토리](ANIMOL_LEGACY_CUTOVER_INVENTORY.json)를 정본으로 한다.

- 보관 정본: `Legacy/Platformer_20261013/<원래 상대 경로>`.
- 원래 파일명은 보관본에서 유지한다. 파일·인접 `.meta`를 함께 이동하고 폴더 `.meta`도 같은 계층에 보존한다.
- 플랫폼 전용 후보 가운데 활성 파일의 GUID, C# 타입 또는 명시 경로 참조가 남는 항목은 이동하지 않는다. UI·서비스의 계약 타입, 데이터와 연결된 런타임은 유지 이유를 인벤토리에 기록한다.
- 런 프로토타입, 참조가 분리된 전용 코드·맵·아트·테스트, Gameplay/MapDevTest/MapAuthoringWorkspace 및 전용 실험 씬을 보관한다.
- 공용 UI, 서비스, Bootstrap/Lobby/Results, Typography, 메인 UI V7 배경 패키지는 유지한다. `ThemeRuntime_T01`은 외부 경로 참조 때문에 파일 자체는 유지하지만 빌드 목록에서 제외한다.
- `MainUiV7`의 배경 20개 레이어와 토끼 시트 2개는 기존 기준 패키지에 남는다. 폐기 배경을 가져오지 않는다.

## Unity 이동 절차

`AnimolTopdownCutoverBatch.Execute`는 사용 중인 자산을 `AssetDatabase.MoveAsset`으로 이동하고 GUID 일치를 확인한다. 저장소 루트는 Unity 자산 경로가 아니므로 보관 자산은 Unity 내부 임시 경로로 이동한 뒤 파일과 `.meta`를 루트의 Legacy로 내보낸다. 완료 후 임시 자산 폴더를 제거하고 재임포트한다.

공용 UI 스프라이트는 `AssetDatabase.MoveAsset`으로 `Assets/ANIMOL/Sprites`로 직접 이동한다. 기존 프리팹·씬은 GUID 참조를 유지한다. 소스 패키지의 manifest와 sprite 키는 호환 입력이며, 실제 정본 경로는 `sprite_path_manifest.json`에 기록한다. 빌더의 계산 경로는 `CanonicalSpritePaths.Resolve`로 변환한다. Resources의 기존 프리팹·카탈로그 키는 바꾸지 않는다.

`ProjectSettings/ANIMOLNamedArtReferences.json`도 갱신한다. 보관 자산의 Sources/Importers/Containers와 해당 캐시 슬롯을 제거하고, 이관한 UI의 Path/FileName/Key/Name 및 연결 슬롯 키를 실제 임포트 자산 이름에 맞춘다. 변경 전 사용자 인덱스는 Legacy의 같은 ProjectSettings 경로에 보존한다. 실제 씬·프리팹 슬롯은 수정하지 않는다.

## 구형 진입점

- 맵 제작기 클래스와 메뉴/자동 복구/실험 씬 진입점은 활성 Assets에서 보관 위치로 이동한다.
- 남는 `UiBuildPipeline` 부분 클래스는 보관 위치 주석과 `#if ANIMOL_LEGACY_MAP_EDITOR`로 차단한다. 해당 define은 기본 설정에 추가하지 않는다.
- 로비에서 기존 Gameplay/MapDevTest/캠페인 씬으로 넘어가는 코드는 동일 define 안에 둔다. 기본 빌드에서는 준비 중 메시지로 종료한다.
- 활성 빌드 목록에서는 보관 씬과 ThemeRuntime 씬을 제외한다. 기존 사용자 씬 목록은 `Legacy/Platformer_20261013/ProjectSettings/EditorBuildSettings.asset`에 보존한다.
- define만 켠다고 보관 자산이 복원되지는 않는다. 구형 제작 환경 복원은 별도 브랜치에서 필요한 Legacy 파일을 원래 경로로 복원하고 참조·빌드를 검증하는 작업이다.

## 기존 작업과 커밋 경계

파일 이동은 현재 작업 트리의 내용을 그대로 보존한다. 기존 수정 파일의 이동을 커밋할 때에는 원래 Git 인덱스의 blob을 대상 경로로 옮겨 기존 수정 내용 자체가 이번 커밋에 섞이지 않도록 한다. 수정 내용은 새 경로의 미커밋 변경으로 남는다. 이전부터 미추적이던 파일은 이동 후에도 미추적 상태를 유지한다. 원래 상대 경로는 이동 인벤토리에서 추적한다.

새 차단/경로 변경은 원래 HEAD에도 동일 변환을 적용해 스테이징한다. 외부 작업 저널에는 변경 전 파일이 보존되며, 최종 인벤토리는 이동한 원본 내용과 `.meta`, 비대상 파일, 차단 내부 내용의 일치를 검사한다.

최종 실행 검증과 제한은 [작업 결과](../MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md)에 기록한다.
