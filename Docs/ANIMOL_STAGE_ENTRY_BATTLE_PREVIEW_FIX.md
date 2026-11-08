# 첫 스테이지 진입 시 별비 미리보기 노출 수정

2026-11-09, Unity 6000.3.8f1에서 재현·수정했다.

## 원인

이전 검수 뒤 에디터에 `ANIMOL_StarRain_Battle`과 `NotebookUI` 두 씬이 함께 열린 상태였다. 활성 씬은 전투 미리보기였고, 전투 Canvas는 정렬 0, 메인 Canvas는 정렬 100으로 동시에 켜져 있었다. 스테이지 진입은 정상적으로 SW01→SW00 퍼즐/HUD를 생성했지만 메인 Canvas를 숨기면서 남아 있던 전투 미리보기가 드러났다. 맵 매핑이나 전투 진입 이벤트의 오류가 아니었다.

## 수정

- `BattleCrayonArtPreview`에 독립 미리보기 격리를 추가했다. 정확한 별비 미리보기 씬과 메인 씬이 동시에 로드되고 ArtPreview가 존재할 때만 전투 미리보기의 Canvas·카메라·EventSystem 루트를 비활성화한다.
- 활성 씬이 미리보기였으면 메인으로 전환해 새 퍼즐 세션이 미리보기 씬에 생성되지 않게 한다. 최초 Play와 이후 씬 로드에 모두 적용하며, Domain Reload 비활성 시에도 이벤트를 중복 등록하지 않는다.
- 별비 씬을 단독으로 여는 아트 검수는 유지한다. 실제 전투나 이야기 분기 진입은 추가하지 않았다.
- 현재 에디터에서는 저장되지 않은 변경이 없는 전투 미리보기 씬만 닫고 메인 씬 하나를 활성화했다. 씬 파일·Build Settings·Play 시작 씬 강제 설정은 변경하지 않았다. 생성기는 기존부터 임시 씬을 정리하고 있어 재생성하지 않았다.

## 실행 검증

| 검사 | 결과 |
| --- | --- |
| 전투 미리보기 → 메인 추가 로드 → SW01 입장 | 통과, SW00 퍼즐과 기존 HUD만 활성, 세션은 메인 씬에 생성 |
| 메인 → 전투 미리보기 추가 로드 → SW01 입장 | 통과, 전투 화면/카메라/입력 루트 모두 비활성 |
| 메인에서 나와 별비 미리보기 단독 로드 | 통과, 시작 카드·버튼 정상 |
| 실제 다시보기·맵 진입·복귀 경로 | 통과, 무차감 다시보기·기존 복귀 버튼·SW00 입장·턴 45→44 유지 |
| 두 씬이 열린 실제 에디터에서 첫 Play | 통과, 활성 씬 Main, 메인 Canvas 활성, 전투 Canvas 비활성 |
| 최종 에디터 | Play 종료, Main 하나만 로드 |
| 컴파일 | 오류·경고 0 |

PlayMode 회귀 검사 **4개 통과**와 실제 첫 Play 확인을 구분한다. [회귀 결과](Validation/StageEntry/IsolationPlayTests.json), [실제 입장 결과](Validation/StageEntry/RealEntryPlayTests.json), [첫 Play 상태](Validation/StageEntry/ColdBoot.json), [최종 에디터](Validation/StageEntry/FinalEditorState.json).

검증 중 PlayerPrefs 지갑/설정을 보존·복원했다. 동적 TMP 글리프 추가도 작업 전 Galmuri14 백업으로 복원했다. 맵·지도/HUD·숫자·스토리 스킨·Main/Battle 씬·Build Settings 등 **1,327개 보호 파일의 해시가 동일**하다. 사용자의 기존 미커밋 내용은 이번 커밋에서 제외한다. [보존 검사](Validation/StageEntry/Preservation.json)

새 피드백 연출이나 입력 잠금을 추가하지 않았다. 보관된 CommercialPolishPolicyTests는 실행하지 않았으며 이번 변경에는 실제 씬 로드/퍼즐 진입 PlayMode 검사를 실행했다. 모바일 빌드·실물 기기 검증은 하지 않았다.
