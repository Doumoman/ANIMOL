# ANIMOL 탑뷰 전환용 Legacy·스프라이트 정리 결과

2026-10-13. Unity 6000.3.8f1로 실제 이동·재임포트·컴파일·씬 검사를 수행했다.

## 적용

- 플랫포머 전용 파일 620개를 `Legacy/Platformer_20261013/`로 이동했다. 원래 상대 경로·내용·파일 `.meta`와 폴더 `.meta`를 보존했다.
- 공용 UI PNG 75개를 `Assets/ANIMOL/Sprites/UI/`로 이동하고 소문자 snake_case로 이름을 정리했다. StageSelect/Store는 실제 기존 화면을 기준으로 분류했다.
- 새 Player/Tiles/Obstacles/Background 분류 폴더를 준비했다. 탑뷰 전용 그림·애니메이션은 실제 존재하지 않아 생성하지 않았다.
- MapAuthoringWorkspace, 전용 제작기·실험/런 프로토타입 씬과 테스트를 보관했다. 활성 빌드는 Bootstrap/Lobby/Results만 유지한다.
- 활성 유지가 필요한 구형 UiBuildPipeline 부분 클래스 17개는 보관 위치 주석과 `#if ANIMOL_LEGACY_MAP_EDITOR`로 차단했다. 기본 설정에 define을 추가하지 않았다.
- 기존 씬을 호출하는 캠페인 UI 함수도 차단하고 기본 경로에서 준비 중 메시지를 표시한다.
- GUID 기반 씬·프리팹·애니메이션 참조를 유지했다. Editor 빌더 계산 경로·asmdef 참조, 스프라이트 정본 manifest, NamedArt 경로·이름·슬롯 캐시를 갱신했다. Resources 카탈로그/프리팹 키는 기존 위치에서 유지한다.
- 기존 빌드 씬 설정과 NamedArt 인덱스의 사용자 작업본은 Legacy의 동일 ProjectSettings 경로에 보존했다.

## 분류와 기존 작업

Assets 파일 4,989개, GUID 2,632개, asmdef 52개를 조사했다. 초기 중복 GUID는 0개였다. 최초 사용자 변경은 미추적 파일을 펼친 기준 1,736개다.

초기 참조 조사에서 활성 UI·계약·런타임 등에 연결된 후보 792개는 보수적으로 유지 대상으로 분류했다. 일부 참조 증거는 이후 보관한 테스트를 포함하는 초기 조사 기록이다. 이번 작업에서는 공용 계약과 게임플레이 타입의 결합을 다시 작성하지 않았다. 파일별 이동·GUID·분류 증거는 인벤토리에 있다.

미커밋 파일의 내용은 이동 위치에서 보존한다. 이전부터 미추적이던 파일은 이동 후에도 미추적이다. 커밋은 기존 인덱스 blob의 경로 이동과 이번 작업의 변환만 포함하므로, 이전 사용자 수정 내용은 새 경로의 미커밋 변경으로 남는다. 따라서 커밋만으로 기존 미추적 사용자 파일까지 새로 추적되지는 않는다.

보관 파일 가운데 기존 추적 파일은 242개, 기존 미추적 파일은 378개다. 기존 추적 파일의 보관 대상 인덱스 blob을 원래 HEAD와 대조했으며 불일치는 0건이었다. UI PNG 75개는 모두 기존 추적 자산이다. 사용자가 이미 추가했던 캠페인 실행 함수는 사용자 변경으로 남기고, 커밋에는 원래 HEAD에 존재하던 실행 함수의 차단만 적용한다.

Phase1 동물 업그레이드 UI는 실제 존재하는 공용 UI로 유지했다. 탑뷰/스와이프 시제품과 P01 QA 씬은 발견되지 않았다. 해당 씬 검사를 수행한 것처럼 처리하지 않았다. 메인 UI V7 배경 패키지·기준·빌더는 유지했다.

## 검증

| 검사 | 결과 |
|---|---|
| 작업 전 및 이동 후 Unity 재임포트·컴파일 | 성공 |
| 이동 파일·`.meta` SHA-256 비교 | 불일치 0 |
| 비대상 Assets 파일 비교 | 변경 0 |
| define 내부 원래 코드 내용 비교 | 불일치 0 |
| 활성 자산의 보관 GUID 참조 | 0 |
| Bootstrap/Lobby/Results 누락 컴포넌트·오브젝트 참조 | 0 |
| 컴파일된 구형 맵 제작기/런 프로토타입 메뉴 | 0 |
| NamedArt의 이번 이동 대상에 대한 옛 경로 | 0 |
| EditMode: CommercialPolishPolicyTests + CanonicalSpritePathTests | 5/5 통과 |
| PlayMode: MenuThemeFlowTests | 3/3 통과 |
| P01 QA | 실제 씬 없음 — 검사 제외 |

전체 구형 플랫포머 테스트 스위트 대신 보존 UI와 이번 경로 변경에 관련된 테스트를 실행했다. 이 정리는 탑뷰 퍼즐 구현 완료를 의미하지 않는다.

## 산출물

- [Legacy 컷오버 명세](../Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md)
- [스프라이트 경로·이름 규칙](../Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md)
- [전체 이동·GUID·초기 참조 인벤토리](../Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json)
- [정본 스프라이트 매핑](../Assets/ANIMOL/Sprites/sprite_path_manifest.json)
- [Unity 씬 검사](../Docs/Validation/TopdownCutover/unity-validation.json)
- [NamedArt 검사](../Docs/Validation/TopdownCutover/namedart-validation.json)
- [EditMode 결과](../Docs/Validation/TopdownCutover/editmode.xml)
- [PlayMode 결과](../Docs/Validation/TopdownCutover/playmode.xml)
