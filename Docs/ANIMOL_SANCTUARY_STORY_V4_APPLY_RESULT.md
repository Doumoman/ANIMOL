# 낙일 성소 이야기 v4 적용 결과

- 날짜: 2026-10-26
- 환경: Windows, Unity 6000.3.8f1 Editor, `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity`
- 출처: `MCP_INBOX/ANIMOL_Sanctuary_Unity_Detailed_v4.zip`
- 판정 기준: 패키지 `Documentation/ACCEPTANCE_CHECKLIST_KO.md`의 `pass / partial / blocked / 미검증`

## 실제 연결

활성 프로젝트에는 이야기용 SC18 화면, 플레이 가능한 퍼즐, 캠페인 영속 저장 서비스가 없었다. `Legacy/PreviousProject_20261013`의 `SC18_CampaignMilestone.prefab`은 테마 완료 요약 화면이며 대사 Runner가 아니다. 기존 격리본을 활성 Assets로 되돌리지 않고, 현재 로비의 `Main UI Canvas/Safe Area/SC18_SanctuaryStory`에 이야기 화면을 만들었다. `Main UI Canvas`와 `EventSystem`은 각각 기존 1개를 재사용했다.

로비 `MainLobbyController`의 캠페인 계속 버튼을 `SanctuaryMainStoryBridge`에 연결했다. 이 버튼은 현재 `낙일 성소 · 이야기`로 표시된다. 화면 닫기 후에는 기존 캠페인 패널로 돌아간다. 이야기 화면은 `SanctuaryStoryRunner` 1개, `SanctuaryUGUIView` 1개, `SanctuarySpriteStage` 1개, 기존 Safe Area, 전용 선택 행 프리팹을 사용한다. Stage는 기존 대사와 선택 UI 뒤에 있다. 헤더와 결말 제목은 Stage 위에 표시된다.

`Assets/ANIMOL/SanctuaryStory` 전체를 가져와 101개 PNG(스토리 표정·자세 22, 배경 7, 인게임 프레임 72)와 이야기·연출 JSON을 보존했다. Unity 메뉴 `ANIMOL > Sanctuary Story > Install Sprite Stage Under Selection`을 실제 SC18 선택 상태에서 실행해 `Generated/SanctuaryVisualCatalog.asset`과 Stage를 만들었다. `Bind Active SC18 Stage`로 presentation JSON과 브리지를 연결했다. 선택지 노출 시 발 위치와 Safe Area 여백에 맞춰 `choiceFeetHeight`를 0.665로 설정했다.

## 검사 결과

| 항목 | 결과 | 실제 확인 |
|---|---|---|
| 패키지 데이터·PNG | pass | 패키지 검증기 3개 통과. 14장면, 170발화, 37큐, 14구도, 484인물 상태, 101 PNG, 원본 인게임 PPU·pivot 72개 확인. |
| Unity 가져오기·연결 | pass | 설치 메뉴 실행. Sprite 검사 메뉴: 인물·자세 94 PNG와 배경 7개, 5인물 바인딩, 필수 프레임 누락 0. 편집 씬에 Runner·View·Stage 각 1개, Canvas·EventSystem 각 1개, catalog·presentation 참조 확인. |
| Unity 컴파일 | pass | `recompile_status`: `completed`, `failed=false`, `compilationFailed=false`. 최종 Console ground truth 오류·경고 0. |
| 활성 프로젝트 Art QA 테스트 | partial | EditMode `ArtAssetTests` 3/4 통과. `ManifestImportsAndExactClipDurations`는 기존 `StarSwellCoastV01Device` 스프라이트 누락으로 실패했다(`ArtAssetBuilder.cs:215`). PlayMode `ArtQaPlayTests` 1/1 통과. 성소 패키지 경로 밖의 누락 자산은 이번 변경에 포함하지 않았다. |
| 이야기 진행·선택 | pass | 실제 Play Mode에서 버튼 `onClick`으로 2회 완주: 28장면, 340발화, 74큐. 첫 회 S01 망설임 후 2행 재표시, S05 4행, S07 7행, S10 7행. 망설임 3회는 직접 선택했으며 자동 확정 없음. 두 번째는 재감상으로 완료. 별도 로비 진입 경로 주행으로 S10까지 117발화·26큐를 지나 붉은 7행과 헤더 버튼을 캡처했다. |
| 배경·표정 | pass | 실제 Stage의 14장면 배경 및 170발화의 484인물 Sprite 상태를 연출 JSON과 대조. 오류 0. 듣는 인물 상태를 포함. |
| 되감기·귀환·S12 얼굴 | pass | S04 첫 되감기, S06/S09 반복 되감기, S12 `rewind_stop` 및 `return_forward` 큐 순서 확인. S12_L13 파수꾼 neutral 표정은 Stage 비활성·재활성 후에도 유지. |
| 화자 바운스 | pass | 새 발화 후 약 100ms에 화자 위치 변화 3.17 Canvas 단위 확인. 다른 인물 3명, 배경, 대사창 고정. 0.32초 후 화자 기본 위치 복귀. 발화 이벤트에서만 시작하는 코드 경로 확인. |
| 일시정지·복원·닫기 | pass | 실제 일시정지 버튼, 앱 pause/resume 시뮬레이션, 닫기 버튼을 실행. 앱 pause 중 호스트 일시정지를 해제해도 진행 정지 유지. 닫기 후 성소 화면·Safe Area 마스크 해제, 기존 캠페인 패널 유지. |
| 체크포인트·재감상 | partial | 첫 완주에서 체크포인트 15회. 재감상 시작 뒤 체크포인트 증가 없이 읽기 전용 재생 확인. 같은 실행 세션의 재개만 연결됨. |
| 1080×1920·Safe Area | partial | Unity Game View 1080×1920 화면을 캡처·육안 점검. Safe Area 상하 6% 여백을 Play Mode에서 모의 적용한 S07 화면에 얼굴·손·신발·7행 선택지 표시. 실제 모바일 노치·터치 기기 검사는 미검증. |
| 퍼즐 입력·제한시간 정지 | blocked | 활성 씬은 로비와 Art QA 구조이며 플레이 가능한 SC18 퍼즐 입력·타이머 소유자가 없다. 이야기 Runner의 호스트·앱 pause는 확인했으나 퍼즐 시스템과의 정지·복원 연결은 할 수 없다. |
| 영속 저장·앱 재실행 재개 | blocked | 활성 저장 소유자가 없고, 사용자 결정에 따라 성소 전용 로컬 저장 추가를 보류했다. 세션 종료 후 재개는 지원하지 않는다. |
| 효과음 | partial | 패키지에 음원 파일이 없고 Stage의 선택적 AudioSource/AudioClip이 연결되지 않았다. 시각 큐와 되감기·귀환 순서는 검사했으며 접촉·되감기 소리는 재생되지 않는다. |
| 실기기·PC 키보드·큰 글씨 | 미검증 | 모바일 빌드와 실제 터치, 키보드 포커스/확정, 큰 글씨 접근성 설정으로는 실행하지 않았다. |

### 화면 증거

- [S01 1080×1920](Validation/SanctuaryStoryV4/s01_1080x1920.png)
- [S07 Safe Area 상하 6% 모의 적용](Validation/SanctuaryStoryV4/s07_safe_area_1080x1920.png)
- [S10 붉은 7행 선택](Validation/SanctuaryStoryV4/s10_choices_1080x1920.png)

## 남은 연결 지점

현재 캠페인 계속 버튼은 네 테마가 있는 캠페인 패널에서 항상 성소 이야기로 들어간다. 정식 캠페인 스테이지 목록이 활성화될 때는 해당 목록의 SC18 진입 지점에 브리지를 연결해야 한다. 퍼즐 입력·타이머 정지와 영속 저장·재실행 복원 역시 그 시스템의 활성 소유자가 생길 때 연결해야 한다. 현재 체크포인트는 메모리 세션 안에서만 유지된다.

기존 `CommercialPolishPolicyTests`는 격리본에 있으므로 실행 대상이 아니다. 이번 변경에서 게임플레이 보상이나 공용 피드백 정책은 추가하지 않았다. 위 결과는 실제 Unity 컴파일, Editor 메뉴 검사, Play Mode 주행·Art QA 테스트 및 화면 캡처에 근거한다.
