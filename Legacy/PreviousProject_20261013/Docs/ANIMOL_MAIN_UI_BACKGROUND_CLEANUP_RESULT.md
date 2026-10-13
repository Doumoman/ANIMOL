# 메인 로비 배경 단일 기준 정리

2026-10-02. 메인 로비 배경은 v7만 사용한다. 기준 원본은 `Docs/Inbox/ANIMOL_main_ui_v7`, 런타임 에셋은 `Assets/ANIMOL/UI/MainUiV7`, 생성기는 `MainUiV7Builder`다. 저장소 `AGENTS.md`에도 이 기준을 반영했다.

## 정리 내용

- 사용하지 않는 기존 배경 5장, 빛 레이어 5장, 토끼 프레임·아틀라스 9장과 대응 meta를 삭제했다. 삭제 전 Unity AssetDatabase로 모든 씬·프리팹·asset의 의존성을 조사해 참조가 없음을 확인했다.
- UI 컨트롤 카탈로그에서 배경·토끼 필드를 제거하고 asset을 재직렬화했다. 컨트롤 생성기는 현재 버튼·아이콘만 생성하며 폐기한 manifest를 읽지 않는다.
- 기존 배경 전용 캡처 코드를 삭제했다. 추적 중인 이전 원본 패키지·보고서·영상·스크린샷·검증 자료 322개, 남아 있던 비추적 ZIP·영상·검증 마커 10개도 삭제했다.
- 현재 22개 v7 PNG와 meta의 GUID를 보존한 채 경로를 v7으로 옮겼다. Resources 키, 생성기 클래스·파일, 합성 shader 이름, Bootstrap·Lobby의 배경 이름과 테스트·검증 스크립트 참조도 통일했다.
- 현재 UI가 사용하는 컨트롤 스프라이트·폰트·프리팹·이벤트와 v7 검증 자료는 유지했다. 게임플레이 배경·맵은 이 정리 대상이 아니다.

폴더 전체 재귀 삭제는 자동 승인 검토에서 차단되어 실행되지 않았다. 이후 삭제 범위를 사전 확인한 Git 추적 파일 목록과 명시적인 개별 비추적 파일 10개로 제한했다. Git 추적 파일은 `git rm`을 force 없이 실행하여 미커밋 변경이 있으면 거부되도록 했다.

Git의 과거 커밋 이력은 재작성하지 않았다. 이번 정리는 현재 작업 트리와 이후 커밋의 파일·문서 기준에 적용된다.

## 검증

Unity 6000.3.8f1 실제 Editor에서 수행했다.

| 항목 | 결과 |
|---|---|
| C# 컴파일 및 v7 shader | 오류 0 |
| EditMode | **12/12 통과**, 실패·skip 0. CommercialPolishPolicyTests 3개와 현재 컨트롤 에셋·폰트 검사 포함 |
| PlayMode | **6/6 통과**, 실패·skip 0. 화면 이동·재생성·재진입·단일 배경·palette 확인 |
| v7 원본 비교 | 현재 런타임 22개 PNG 전부 원본과 바이트 동일 |
| 독립 near 검사 | 800개 조합, 토끼·상위 silhouette 가림 0 |
| 실제 Game View | 1080×1920, 1080×2400에서 실제 Lobby 재확인, v7 배경 1개 |
| 보호 맵·데이터 | 678개 파일 SHA-256 동일 |

이번 정리 후 고유 Test Runner 테스트 수는 **18개**다. 이전 적용 시 실행한 26초 재생·1,600개 GPU 검사의 증거는 그대로 유지했다. 이번 정리에서 26초 재생을 새로 수행한 것으로 합산하지 않았다.

[EditMode 결과](MainUiV7/cleanup-editmode-results.json), [PlayMode 결과](MainUiV7/cleanup-playmode-results.json), [보호 파일 확인](MainUiV7/cleanup-protection.json), [1920 화면](MainUiV7/Captures/Cleanup_1080x1920.png), [2400 화면](MainUiV7/Captures/Cleanup_1080x2400.png).

1920 캡처는 적용 당시 같은 시각(12.25초)의 Game View와 전체 픽셀이 동일하다. 2400 캡처의 차이는 캠페인 버튼 영역에만 있으며 배경 영역은 동일하다. 기존 버튼의 hover/transition 상태는 고정하지 않았다. [이미지 비교](MainUiV7/cleanup-screen-comparison.json).

## 기존 변경 및 제한

작업 전 미커밋 파일 95개 중 92개는 SHA-256이 동일하다. 검증 과정에서 Unity가 아래 동적 TMP 폰트의 글리프/atlas 캐시를 갱신했다. 이 세 파일은 기존 변경이 있는 파일이므로 임의 초기화·덮어쓰기하지 않았으며 정리 커밋에도 포함하지 않는다.

- `Assets/ANIMOL/Typography/NotoSansSymbols2-Regular_Fallback.asset`
- `Assets/ANIMOL/Typography/NotoSansSymbols_Fallback.asset`
- `Assets/ANIMOL/Typography/Symbols_Fallback.asset`

게임플레이·입력·물리·재화·보상·수동 편집 맵은 수정하지 않았다. 운영 스테이지의 기존 보상 승인·검수 제한도 유지했다. 모바일 기기 빌드와 전체 프로젝트 테스트는 이번 정리에서 실행하지 않았다.

현재 적용·연출·화면 기준: [v7 적용 보고서](ANIMOL_MAIN_UI_FANTASY_BACKGROUND_V7_RESULT.md).
