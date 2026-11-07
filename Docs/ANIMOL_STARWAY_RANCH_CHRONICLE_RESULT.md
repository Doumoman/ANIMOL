# 별길 목장 v0.28 두 시점 기록 적용 결과

2026-11-07, Unity 6000.3.8f1 / Android 프로젝트.

## 구현

- `Assets/ANIMOL/Scripts/Packages/WilliamParadoxStoryV14/Runtime`에 `StoryChronicleData`, `StoryChronicleView`를 추가하고 기존 `StoryDialogueView`, `StoryRunner`, `RanchStoryDialogueBridge`에 연결했다.
- 기존 Galmuri14 TMP Regular와 네온 UI를 유지했다. 일반 대화창은 안전 영역 하단 20px, 높이 422px로 고정했다. 장면별 상단 이동, 진행 카운터와 ‘다음 날’ 캡션을 제거했다.
- 일반 대사 178개와 `R17_008` 기록 진입 명령 1개를 사용한다. 기록 전문은 `Assets/ANIMOL/Data/StarwayRanch/Story/ranch_chronicle.json`에만 두고, 오프라인용 JS도 같은 폴더에서 읽는다.
- R17 → 기존 R17B 책을 0.6초 교차 전환한다. 전용 RectMask2D 안에서 제목과 여섯 문단을 모두 상승시킨 뒤 1초 정적, 동일 기둥의 4.2초 하향 정정, 0.65초 간격, 상위 기록 전체 상승을 재생한다. 두 버전 중 큰 실측 높이를 공통 슬롯으로 사용한다.
- 노이즈와 작은 겹침은 중앙을 통과하는 글자에만 적용한다. 배경 원화·책의 색상·질감과 게임의 `Time.timeScale`을 수정하지 않는다.
- 마우스·터치 EventSystem 홀드는 기록 시간만 3배로 가속한다. 해제·포인터 이탈·Cancel·포커스 상실·앱 일시정지·호스트 일시정지·뷰 종료 시 가속을 해제한다. 복귀 첫 틱을 버리고 이후 프레임을 최대 0.05초로 제한한다.
- 모션 축소는 아스테르인 6페이지 → 상위 기록 6페이지를 짧은 탭으로 읽는다. 홀드만으로 페이지를 넘기지 않는다. 모션 설정 변경은 진행 중 기록을 처음부터 다시 시작한다.
- 전체 열람과 스킵을 구분한다. 두 기록 또는 정적 12페이지를 모두 읽었을 때만 기존 세션에 `RANCH_RULE_EPILOGUE_SEEN`을 기록한다. 스킵은 별도 상태이며 기록이 끝나도 진행 노드는 유지한다. 새 ‘계속’ 입력만 `ActionConfirmed(play_chronicle)` → `SceneFinished(R17)` → 기존 호스트 종료로 이어진다.
- 진입·되감기 음향은 `RanchStoryDialogueBridge.ChronicleStarted`, `ChronicleRewindStarted`에 연결할 수 있다. 완성 음원과 환경음 믹서는 제공되지 않아 실제 오디오 연결은 남아 있다.

## 저장과 기존 자산

- 구형 최종 계속 노드는 새 기록 노드로 복원한다. 이전 기록 열람 표시는 새 두 기록의 열람 증거로 사용하지 않는다. 귀환, 이름 부르기, 등불 가리기 등 기존 플래그는 유지한다.
- 기록 중 저장을 복원하면 두 기록을 처음부터 재생한다. 중간 위치나 구형 여섯 줄의 페이지 번호로 새 기록의 열람을 추정하지 않는다.
- 이 진입점은 기존 스토리 미리보기 컨트롤러다. 실제 퍼즐·귀환 조건과 캠페인 저장은 게임 호스트의 책임이며, 미리보기에서 `CHAPTER_R_COMPLETE`나 보상을 새로 지급하지 않는다. 실제 호스트는 마지막 계속 뒤 기존 완료 검증/저장 경로를 사용한다.
- 기존 UI 프리팹, StoryPreviewCanvas 프리팹, RanchStoryPreview 씬은 Unity 직렬화 API로 갱신했으며 기존 `.meta` GUID를 유지했다. 메뉴의 새 프리팹 생성은 고유 이름을 사용한다.
- 원화·레이어 32개는 제공 ZIP과 기존 보존 해시가 모두 일치한다. 프롬프트 기록은 변경하지 않았다. 보호 대상 `SW00_MapData.asset` 및 `.meta`도 HEAD와 동일하다. [보존 증거](Validation/StarwayRanchChronicle/preservation.json).
- 작업 시작 전의 기존 미커밋 변경은 별도로 보존한다. 새 아트나 레거시 활성 복원은 없다.

## 검증

| 검증 | 결과 |
|---|---|
| Unity 스크립트 컴파일·최종 Console 오류 | 통과, 오류 0 |
| Story EditMode + 현재 `ArtAssetTests` | 69 / 69 통과 |
| 기록·대화·원화 통합·세로 미리보기 + `ArtQaPlayTests` PlayMode | 12 / 12 통과 |
| 오프라인 HTML Node VM 검사 | 16개 검사 통과, 179개 데이터 항목과 사용 이미지 경로 확인 |
| 원화·레이어 32개 / SW00 및 meta | 해시 동일 |

[Unity 결과](Validation/StarwayRanchChronicle/unity_tests.json), [HTML 결과](Validation/StarwayRanchChronicle/preview_runtime_checks.json).

PlayMode는 기본/3배속, 하향 이동, 두 번의 전체 읽기, 공통 문단 객체, 정적 12페이지, 홀드 해제와 계속 분리, 포커스·일시정지·Cancel, 재진입·저장 복원, 스킵 후 읽음 미지급, 최종 이벤트 순서, 178개 일반 대사의 250px 본문 높이를 검사했다.

1080×1920 실 캡처를 확인했다: [아스테르인](Validation/StarwayRanchChronicle/asteran.png), [상위 기록](Validation/StarwayRanchChronicle/higher.png), [종료 대기](Validation/StarwayRanchChronicle/await_continue.png), [일반 대화 하단](Validation/StarwayRanchChronicle/R01_bottom.png), [거리 장면 하단](Validation/StarwayRanchChronicle/R15_far_bottom.png).

초기 실행은 구형 최종 노드를 기대하는 검사 수정 후 재실행했고, 한 PlayMode 실행은 테스트 러너 내부 예외로 중단됐다. 중단된 작업을 정리하고 컴파일 완료 후 최종 실행에서 위 결과를 얻었다. Windows 창 활성화 도구는 `failed to activate captured window`로 실패하여 Unity API로 Game 뷰를 열었다. 에디터는 원래 NotebookUI 씬으로 복귀했다.

보관된 `CommercialPolishPolicyTests`는 현재 활성 테스트가 아니므로 실행하지 않았다. 기존 연출 정책 검사는 활성 Story EditMode 테스트로 확인했다. 모바일 실기기 터치·완성 음원 청취와 네이티브 브라우저의 실제 포인터/레이아웃 검수는 이번 자동화 범위에 포함하지 않았다.

## 검토와 재생성

- [오프라인 검토본](Reference/StarwayRanch/Preview/index.html): 저장소 폴더 구조를 유지하고 브라우저로 연다. 장면/일화 선택은 화면 바깥 검토 영역에만 있다.
- `node Tools/StarwayRanch/verify_preview.cjs`: 실제 HTML/JS/데이터의 실행 계약을 검사한다. 브라우저 렌더링 결과를 대신하지 않는다.
- `RanchStoryUiInstaller.ApplyChronicle()`: 아트 재가져오기 없이 기존 진입 프리팹·미리보기 씬 UI를 갱신한다. PlayMode를 중지한 상태에서 실행한다.
