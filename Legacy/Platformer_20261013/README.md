# Platformer_20261013 복원 가능한 보관본

기준일: 2026-10-13. 이 폴더는 저장소 루트의 Legacy이며 Unity Assets 바깥에 있다.

## 범위와 기준 커밋

- 플랫포머 전용 코드·실험/게임플레이 씬·맵·프리팹·아트·테스트 620개를 원래 상대 경로와 `.meta`로 보관했다.
- 활성 프로젝트에 유지한 UiBuildPipeline Editor 생성 파일 17개의 조건부 컴파일 이전 원본과 `.meta`도 같은 상대 경로에 보존했다.
- 공용 CampaignUiPresenter의 과거 씬 호출부를 풀어 보존한 사본 1개와 `.meta`도 동일 경로에 있다. 활성 파일에서는 공용 UI와 세 호출부의 조건부 컴파일을 분리한다.
- 기존 사용자 빌드 씬 설정과 NamedArt 인덱스는 `ProjectSettings/`의 동일 상대 경로에 있다.
- 플랫포머 정리 전 추적 파일 기준: `be907f08` (`Resize player body and visuals to a 0.8 tile square`).
- 최초 컷오버: `87725aaa` (`Archive platformer assets and canonicalize UI sprite paths`). 이번 추가 검토의 기준 커밋이기도 하다.
- 현재 작업 트리의 수정 내용과 기존 미추적 파일을 보존한다. 최초 이동 파일 중 242개는 기존 추적 파일, 378개는 기존 미추적 파일이다. 커밋만으로 미추적 사용자 파일과 모든 공용 파일의 미커밋 변경이 복원되지는 않는다.

## 원래 경로와 목록

`Legacy/Platformer_20261013/Assets/ANIMOL/...`의 접두사 `Legacy/Platformer_20261013/`를 제거하면 원래 저장소 상대 경로다. 파일과 `.meta`를 한 쌍으로 취급한다. 폴더 `.meta`도 함께 보존한다.

- [최초 620개 이동 경로·GUID 목록](../../Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json)의 `moves` 중 destination이 이 폴더인 행.
- [현재 분류·원본 사본 목록](../../Docs/ANIMOL_CUTOVER_CLASSIFICATION.json)의 `archived`와 `copied-original` 행.
- [전체 컷오버 명세](../../Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md)와 [결과 보고](../../MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md).

## 복원 절차

1. 현재 작업 트리와 미추적 파일을 별도 보존하고, 복원은 별도의 작업 트리에서 수행한다. 예: `git worktree add ../ANIMOL-platformer-restore be907f08`.
2. 목록에서 필요한 Legacy 파일을 골라 그 작업 트리의 원래 상대 경로에 파일·인접 `.meta`·폴더 `.meta`를 함께 복사한다. 이미 있는 파일은 차이를 먼저 검토한다. 현재 활성 프로젝트로 같은 GUID의 두 사본을 넣지 않는다.
3. 미추적 378개와 공용 코어·UI·서비스에 남은 사용자 작업본이 해당 복원 코드의 선행 의존성일 수 있다. 현재 저장소의 실제 파일과 분류 참조자를 대조해 복원 작업 트리에서 함께 구성한다.
4. 원본 생성기 사본에는 이번 조건부 컴파일 래퍼가 없다. 활성 파일의 define을 켜는 것만으로 Assets 밖의 씬·자료가 복원되지는 않는다. 선택한 소스/씬/참조를 먼저 복원하고 해당 브랜치에서 제작기 설정을 명시적으로 구성한다.
5. Unity 6000.3.8f1로 재임포트·컴파일·중복 GUID·누락 참조를 검사한다. 보관된 EditorBuildSettings는 복원 참고 자료로 사용하고 현재 탑뷰 빌드 설정과 구분해 검토한다.
6. 맵 제작기·실험 씬·관련 EditMode/PlayMode 테스트를 실행한 뒤 복원 브랜치에 별도로 커밋한다. GUID 재생성, `.cs` 확장자 변경, `git reset/clean`은 복원 방법으로 사용하지 않는다.

보관본은 과거 구현의 회복 가능성을 위한 기록이다. 신규 탑뷰 장치나 승인된 32px 최종 아트로 간주하지 않는다.
