# 활성 프로젝트와 레거시 경계

2026-10-13: 사용자의 “이전 프로젝트 내용은 안 보이도록 전부 레거시” 요청에 따라 공유 UI/서비스도 보존 예외에서 제외했다.

```text
Assets/
  ANIMOL/
    Art/StarwayPasture/V04/   현재 자산·폰트·프리팹·QA 씬
    StarwayV04/              현재 표현 코드·임포터·테스트
  TextMesh Pro/             Unity 텍스트 기반 의존성
Docs/
  ANIMOL_ACTIVE_PROJECT_LAYOUT.md
  ANIMOL_SPRITE_PATHS_AND_NAMES.md
  Inbox/ANIMOL_REFERENCE_SPRITES_V04/
  Validation/{StarwayV04,ActiveSlice}/
Legacy/
  Platformer_20261013/       이전 1차 플랫포머 보관본
  PreviousProject_20261013/  이번에 격리한 이전 프로젝트 전체 잔여분
```

옛 로비, 계정·상점·서버·방울 서비스, 제작기, 데이터, 프리팹, 테스트, 생성 파이프라인과 과거 문서·도구·입력 패키지·캡처는 두 Legacy 폴더 밖에서 활성 콘텐츠로 사용하지 않는다. 원래 상대 경로와 .meta를 보존하며 삭제하지 않았다. ProjectSettings의 옛 ANIMOL 아트 캐시도 보관했다. Packages/ProjectSettings/Library/Temp/Logs/UserSettings 등 Unity 엔진 설정·패키지·자동 캐시는 게임 콘텐츠 보관 대상과 구분한다.

유일한 활성 씬/빌드 씬은 `Assets/ANIMOL/Art/StarwayPasture/V04/Scenes/ANIMOL_Starway_Art_QA.unity`다. 에디터의 시작 씬도 이 씬이다. **QA 씬이며 플레이 가능한 탑뷰 퍼즐은 아직 없다.** 이동·밀기·코스트·수집·승리를 구현한 것으로 표시하지 않는다.

## 파일 규칙의 실제 적용 상태

이전 `Assets/ANIMOL/Sprites/UI`의 75개 PNG에는 정본 UI 경로와 `spr_ui_...` 명명이 적용되어 있었다. 현재는 이전 UI 자체가 레거시이므로 `Legacy/PreviousProject_20261013/Assets/ANIMOL/Sprites/UI`에 있다.

V04는 직전 사용자 요청의 **명시적 예외 경로** `Assets/ANIMOL/Art/StarwayPasture/V04`와 입력 패키지의 파일명을 유지한다. ASCII snake_case지만 모든 파일이 미래 정본 `Assets/ANIMOL/Sprites`의 분류 경로·`spr_`/`anim_` 접두사를 준수한다고 주장하지 않는다. 새로 승인되는 일반 스프라이트는 `ANIMOL_SPRITE_PATHS_AND_NAMES.md`를 따른다. 이번 요청은 이전 프로젝트 격리이며 V04의 지정 적용 루트를 임의로 바꾸지 않았다.

현재 표현에서 실제로 필요한 폰트 8개만 GUID 유지 이동으로 V04/Fonts에 분리했다. 원래 바이트 사본은 `Legacy/PreviousProject_20261013/SharedDependenciesSnapshot/Assets/...`에 있다. 나머지 타이포그래피 코드·프리팹·테스트는 전부 레거시다. Game View 해상도 도구는 이전 전체 Editor 어셈블리에 의존하지 않는 V04 전용 도구로 분리했다.

복원 절차와 이동 목록은 [레거시 README](../Legacy/PreviousProject_20261013/README.md), [보존 해시/원래 경로 목록](Validation/ActiveSlice/archive_inventory.json), [현재 검증](Validation/ActiveSlice/audit.json)을 참조한다. 과거 V04 적용 결과는 격리 전 상태의 기록이고, 현재 경계는 이 문서를 따른다.

## 격리 후 실제 검증

- 기존 파일 17,539개(이동 및 필요한 폰트의 원본 사본)의 SHA-256 일치, 보존 오류 0.
- 활성 씬은 QA 1개, 활성 C#은 V04 전용 9개. 옛 C# 0. Unity 에디터 컴파일 실패 false, Console 오류 0.
- 현재 Assets/패키지 GUID 참조 1,128건 검사: 누락 0, Assets 중복 GUID 0. 실제 QA 씬의 누락 스크립트/Sprite 0.
- 격리 후 V04 [EditMode 4/4](Validation/ActiveSlice/editmode.json), [PlayMode 1/1](Validation/ActiveSlice/playmode.json) 통과. 기존 로비/CommercialPolish 테스트는 의도적으로 레거시로 이동했으므로 이번 활성 프로젝트에서는 실행하지 않았다.
- 첫 Live PlayMode 시도는 기본 시작 씬 강제가 테스트 러너 씬을 대신 열어 진행되지 않았다. 취소 후 시작 씬 강제를 테스트 동안만 해제했고, 실제 통과 후 QA로 복원했다. 라이선스 문서 2개를 검사 도중 추가하여 파일 생성 경고가 기록됐으며 필요한 라이선스 파일이므로 유지한다. Input Manager 사용 중단 예정 경고도 기존 엔진 설정에 남아 있다. 두 경고를 컴파일 오류로 취급하거나 테스트 통과로 숨기지 않는다.
- 옛 URP 글로벌/Quality 설정과 입력 액션 연결, 기존 3개 빌드 씬을 제거했다. 새 렌더 파이프라인은 현재 V04에 쓰던 Built-in으로 유지한다.
- [격리 후 1080×1920 캡처](../Screenshots/ActiveSlice_20261013/qa_clean_project.png)를 렌더하고 확인했다. PNG 원본의 작은 수풀 알파 틈은 이전 V04 보고서의 미확정 아트 항목이며 이번 파일 격리로 해결했다고 주장하지 않는다. 모바일 실기기/빌드는 이번 격리 검수에서 미실행이다.

새 플레이 구현 전에는 QA 화면을 기본으로 열어 확인한다. Live CLI로 PlayMode 테스트를 실행할 때 `EditorSceneManager.playModeStartScene`을 일시적으로 null로 두어 테스트 러너가 자신의 씬을 열 수 있게 하고, 실행 후 QA SceneAsset으로 복원한다. 복원/검수 도구는 Tools 아래에 있으며 마이그레이션은 완료 후 재실행을 차단한다.
