# ANIMOL 월궁 대표 화면 작업 결과

기준일은 2026년 10월 1일이다. 최신 워크트리를 바탕으로 월궁의 그래픽 규칙을 검토할 수 있는 독립 QA 씬을 구성했다. 기존 자산 표현과 개선 표현은 같은 배치와 물리를 사용하며, 운영 캠페인과 전체 테마에는 적용하지 않았다. 이 결과는 대표 화면의 검토본이며 최종 캐릭터 애니메이션이나 실기기 품질 승인을 의미하지 않는다.

## 범위와 원본 보존

- 시작 커밋: `7f72aa6f3b4ebf68aefc3032bef529dabd48fd5a` — `fix(map-editor): persist device placement and deletion`.
- 시작 상태: 수정된 추적 파일 91개, 미추적 파일 911개. 파일 단위 상태와 SHA256은 [시작 기록](GraphicsQA/baseline-worktree.json)에 보관했다.
- 보호한 T01-S01 맵 SHA256: `D2BB4452923912030EA106719C1FC83CC3DBD8FDC37812BB24FA7F5757DA3FB5`.
- T01-S01 재생성, 지도 수정, 공용 UI/플레이어/장치 프리팹 수정, 운영 Ready/ReleaseCandidate 변경, Build Settings 등록은 수행하지 않았다.
- QA 씬은 `Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity`이다. 운영 빌드 씬 목록에 포함되지 않는다.
- 이번 커밋에는 신규 QA 파일만 포함한다. 현재 워크트리의 기존 미커밋 스크립트·프리팹·아트 자산을 참조하므로, 이 커밋만 별도 깨끗한 체크아웃에 적용하는 것으로 전체 프로젝트 상태가 재현되지는 않는다. 기존 작업의 별도 커밋 범위는 건드리지 않았다.

기준 문서는 [세로 화면 결과](ANIMOL_M9_PORTRAIT_RESULT.md), [폴리시 검토 결과](ANIMOL_COMMERCIAL_POLISH_REVIEW_RESULT.md), [운영 연동 매트릭스](ANIMOL_OPERATIONAL_INTEGRATION_MATRIX.md)다. 문서의 과거 테스트 수치를 최신 통과 수치로 재사용하지 않고 이번 작업에서 다시 검사했다.

## 대표 구간 구성

가로 가시 범위는 기존 `PortraitWorldCameraPolicy`의 12셀을 유지한다. 1080×1920에서는 세로 21.33셀, 1080×2400에서는 26.67셀이 보인다. 플레이어, 9방향 지형, 단방향 복귀 발판, M1 달빛 발판, M2 옥 저울 한 쌍, M6 월상 계단과 문양판, 기둥·등불·지붕, HUD를 한 화면에 구성했다.

장치 8개는 기존 운영 프리팹과 `StageMapRuntimeFactory`를 사용한다. M1·M2·M6 각 상태 머신, 충돌체 크기/오프셋, 탑승자 처리, 점유 중 전환 보류는 그대로 사용한다. QA 장치의 위치·링크·경로만 독립 구성했다. 단방향 발판을 통해 낮은 층에서 다시 올라갈 수 있으며 바닥 아래 이탈 시 QA 시작점으로 복귀한다.

공통 UI 프리팹의 Safe Area, 터치 소유권, 스와이프 이동, 점프, 하향 통과, 좌우 미러·크기·투명도 설정을 재사용한다. HUD는 실제 연습 경과 시간과 스태미나, 장치 상태를 표시한다. 캠페인 방울·코인·보상 완료를 모사하지 않는다. 일반 피드백의 입력 차단이나 시간 변경을 추가하지 않았다.

## QA 전용 아트 규칙

기존 월궁 9방향 타일, M1·M2·M6의 운영 아트 바인딩과 애니메이션, `DevPlayer.prefab`, `UI_CommonRoot.prefab`, `SC15_GameplayHud.prefab`을 기준으로 조사했다. 타일의 반복 무늬와 장치별 색 편차, 장치 스프라이트의 투명 여백·조립도 때문에 보이는 면과 충돌면이 어긋나는 점을 대표 화면의 정리 대상으로 삼았다. 원본 자산을 교체하지 않고 QA 메시·전용 팔레트·렌더 변환으로 적용 범위를 제한했다.

| 요소 | 적용 규칙 |
|---|---|
| 공통 색 | 밤 배경 `#101E32`, 윤곽 `#172A3D`, 지형 `#294259`, 옥색 `#78BBAE`, 밝은 면 `#C8E5CB`, 금색 `#DCB16D` |
| 윤곽과 격자 | 1셀을 32분할한 좌표만 사용. 지형 외곽은 2px, 밟는 윗면은 연속된 밝은 띠로 구성 |
| 지형 이음새 | NW/N/NE/W/C/E/SW/S/SE 방향별 노출 가장자리에만 테두리를 두고, 내부 타일 사이에는 장식 점을 넣지 않음 |
| 반복 무늬 | 매 타일의 얼룩과 점 장식 대신 구조물 단위의 긴 석재 줄눈 사용. 원본 아틀라스는 보존 |
| 배경 | 명도와 대비를 낮추고 충돌체를 두지 않음. 기둥 밑동은 바닥에 닿고 등불은 기둥에서 뻗은 브래킷과 줄에 연결 |
| 캐릭터 | 상아색 몸, 남색 외곽, 옥색 띠, 작은 금색 강조. 기존 플레이어의 충돌체·입력·점프 피드백을 재사용 |
| 장치 | QA 전용 팔레트 셰이더로 원본 애니메이션을 남색·옥색·상아색·금색 계열로 정리 |
| HUD | 동일한 남색 바탕과 상아색 글자, 옥색 상태 수치, 금색 상태 안내. 기존 터치 영역 위치는 유지 |

지형·배경·토끼는 Unity의 코드로 생성하는 픽셀 격자 메시다. 기존 PNG를 덮어쓰거나 아틀라스를 다시 슬라이스하지 않았다. 프로젝트가 선형 색 공간을 사용하므로 메시 정점 색을 변환해 배경이 의도보다 밝게 뜨는 문제를 수정했다.

원본 장치 스프라이트에는 조립도 전체 크기의 투명 여백이 있다. QA에서는 스프라이트의 불투명 영역을 읽어 보이는 발판을 실제 충돌면에 정렬한다. 저울의 가는 줄은 발판 윗면 계산에서 제외하고, 이동하는 개별 판에 붙어 움직이던 조립도 받침은 QA 개선 표현에서 숨긴다. 이 조정은 렌더링 변환에만 적용한다.

## 상태 판독성


- 밟을 수 있는 면: 밝은 옥색 선으로 실제 충돌면을 표시한다.
- M1 소멸 예고: 금색 면과 금색 윤곽, HUD의 소멸 예고 문구를 함께 표시한다.
- M1 숨김: 충돌과 실선 윤곽이 사라지고 희미한 잔상만 남는다.
- M1 복귀: 보랏빛 윤곽과 복귀 문구를 사용하며 기존 복귀 타이머가 끝나기 전에는 충돌이 활성화되지 않는다.
- M2 작동/복귀: 실제 판의 하강·상승과 HUD 상태를 보여 주고, 비점유 복귀 중에는 윤곽색을 바꾼다.
- M6 점유 보류: 금색 윤곽과 회전 후 형상의 반투명 예고를 표시한다. 비어 있으면 실제 90도 전환을 수행한다.

처음 배치에서는 상단 지형이 M6의 회전 공간 검사를 막았다. 장치의 점유/회전 검사를 약화하지 않고 QA 지형 위치를 바꿔 해결했다. 최초 검사에서 공통 UI에 이미 존재하던 입력 라우터를 중복 추가한 것도 발견해 재사용하도록 수정했다.

## 캡처와 녹화

두 해상도 모두 Unity Game View를 고정 해상도로 설정하고, Play Mode의 `WaitForEndOfFrame`에서 `ScreenCapture.CaptureScreenshotAsTexture`로 캡처했다. 카메라 전용 RenderTexture 캡처를 Game View 증거로 대신하지 않았다. PNG 원본에는 ScreenSpaceOverlay HUD가 포함된다.

| 산출물 | 경로 |
|---|---|
| 1080×1920 전후 비교 | [비교 이미지](GraphicsQA/Captures/comparison_1080x1920.png) |
| 1080×2400 전후 비교 | [비교 이미지](GraphicsQA/Captures/comparison_1080x2400.png) |
| 상태별 확대 | [상태 비교 이미지](GraphicsQA/Captures/state_comparison.png) |
| 입력 기반 플레이 녹화 | [플레이 영상](GraphicsQA/Captures/moon_input_replay.mp4) |
| 원본 화면 | `Docs/GraphicsQA/Captures/before_*.png`, `after_*.png` |
| 상태 원본과 개별 확대 | `state_M*.png`, `detail_M*.png` |

전후 비교의 왼쪽은 **동일한 새 QA 배치에 기존 자산과 HUD를 적용한 화면**이다. 과거 T01-S01의 화면을 현재 화면인 것처럼 사용하지 않았다. 오른쪽은 QA 전용 개선 표현이다.

플레이 영상은 기존 이동·점프 입력 API를 구동하고 실제 물리 프레임을 기록한 자동 입력 재생이다. 사람 플레이 기록은 아니다. 상태별 확대는 별도의 제어된 점유 시나리오이며 기존 장치 상태 API를 사용한다. 영상과 상태 시나리오를 서로 다른 검증으로 구분한다. 비교 조립과 동영상 인코딩은 [증거 패키징 스크립트](GraphicsQA/package_evidence.py)로 재현할 수 있다.

## 검증 결과

수정 전 전체 EditMode는 160개 중 154개 통과, 6개 실패였다. 최종 EditMode는 162개 중 156개 통과, 기존 6개 실패다. 기존 실패의 이름과 실패 메시지를 작업 전 XML과 비교하며, 검증 원문은 [최종 대조 기록](GraphicsQA/final-verification.json), [EditMode XML](GraphicsQA/final-editmode.xml), [PlayMode XML](GraphicsQA/final-playmode.xml)에 보관한다.

- Unity 6000.3.8f1에서 최신 스크립트 컴파일 및 QA 씬 생성 성공. C# 컴파일 오류 없음.
- 전체 PlayMode: **70/70 통과**.
- 신규 QA 검사: EditMode 2개 + PlayMode 5개, **7/7 통과**.
- `CommercialPolishPolicyTests`: **3/3 통과**.
- 기존 미커밋 파일 **1,002개 모두 SHA256 일치**. T01-S01 맵도 시작 해시와 일치한다.
- 기존 EditMode 실패 6개는 이름과 실패 메시지가 모두 동일하다. 새 회귀는 없다. 테스트 통과를 위한 맵 복원이나 과거 기대값 변경은 하지 않았다.

재실행 명령은 `unity test . --mode EditMode --output Docs/GraphicsQA/final-editmode.xml --timeout 600`, `unity test . --mode PlayMode --output Docs/GraphicsQA/final-playmode.xml --timeout 600`이다. 보존 및 실패 비교는 `powershell -NoProfile -ExecutionPolicy Bypass -File Docs/GraphicsQA/verify.ps1`로 수행한다. 실패가 포함된 EditMode 실행에서 CLI는 라이선스 토큰 갱신 메시지도 출력했으나, 테스트 XML은 162개 검사를 완료했고 위 6개 assertion 실패를 기록했다.

중간 검증에서 QA 생성기의 입력 라우터 중복을 발견해 기존 HUD 내부 라우터를 재사용하도록 수정했다. 또한 장치 점유를 직접 주입하는 QA 검사에서는 시작 지점의 플레이어가 M1 복귀 영역에 겹치는 간섭을 발견했다. 해당 상태 검사만 플레이어를 떨어진 안전 지형에 두도록 격리했다. 실제 점프 경로 검사는 별도로 유지했으며, 이를 위해 장치 동작이나 플레이어 물리를 변경하지 않았다.

기존 실패 6개는 다음과 같다.

1. `M8MoonPalaceStageTests.AllTenBubbleTriplesFitConservativeAutomatedRouteBudgetWithoutCreatingHumanReview`
2. `M9PortraitPolicyTests.CampaignIdentityAndReadinessRemainAtM8Boundary`
3. `M9T01ContentTests.AllTenTriplesHaveARepeatableSeedAndFitTwoDimensionalBudgets`
4. `M9T01ContentTests.AuthoredMapExpandsToFourByTwoChunksWithoutChangingStableIdentity`
5. `M9T01ContentTests.LearningRoutePreservesEveryM8ObjectIdAndKeepsRabbitOnly`
6. `T01S01ManualAuthoringTests.InitialBackup_MatchesCurrentLayoutObjectsAndContentHash`

QA 전용 검사는 운영 씬 목록 제외, 맵 로드 전후 바이트 보존, 9방향과 32px 좌표, 두 해상도의 12셀 시야, 장치 화면 포함, 표현 전환 시 물리/입력 불변, 장치 아트 충돌체 비활성, M1/M6 점유 보류와 복귀, 실제 점프 착지와 하향 통과를 확인한다. 전체 EditMode에는 `CommercialPolishPolicyTests`도 포함된다.

## 변경 파일과 사용 방법

- `Assets/ANIMOL/GraphicsQA/`: 독립 씬, QA 팔레트 셰이더/머티리얼, 생성 메시와 Unity 메타 파일.
- `Assets/ANIMOL/Scripts/Editor/MoonGraphicsQaBuilder.cs`: QA 씬 구성과 32px 메시 생성, 원본 장치 불투명 영역 분석.
- `Assets/ANIMOL/Scripts/Editor/MoonGraphicsQaCapture.cs`: Game View 캡처 메뉴.
- `Assets/ANIMOL/Scripts/Gameplay/MoonGraphicsQaScene.cs`: QA 장치 초기화, 국소 표현 전환, HUD, 입력 재생과 캡처.
- `Assets/ANIMOL/Tests/EditMode/MoonGraphicsQaTests.cs`와 `Tests/PlayMode/MoonGraphicsQaPlayModeTests.cs`: QA 회귀 검사.
- `Docs/GraphicsQA/`와 이 문서: 시작 기록, 검증 결과, 비교 이미지, 영상, 패키징 스크립트.

Unity에서 `MoonGraphicsQA.unity`를 열고 Play를 누르면 실행된다. 좌우 방향키/A·D와 Space 또는 기존 터치 컨트롤을 사용한다. `B`는 같은 구간의 전후 표현을 전환하고, `R`은 QA 시작 상태로 복귀한다. `ANIMOL/Graphics QA` 메뉴로 씬 재생성과 캡처를 실행할 수 있다. 씬 생성기는 열려 있는 씬에 저장하지 않은 변경이 있으면 중단한다.

## 남은 임시 자산과 검토 항목

- 토끼는 QA용 정적 픽셀 메시이며 완성된 걷기·점프·표정 스프라이트 세트가 아니다. 기존 Squash/Stretch만 재사용한다.
- 배경은 대표 구간용 구조물이며 테마 전체의 배경 세트나 시차 이동 시스템이 아니다.
- HUD 폰트와 미연결 능력/특수 버튼은 기존 프로젝트 자산과 상태를 유지한다. 해당 능력이 완성됐다고 표시하지 않는다.
- SFX는 기존 임시 톤이다. 이번 작업에 최종 사운드 제작은 포함하지 않았다.
- 원본 장치별 투명 여백과 조립 구조는 QA의 국소 보정으로 처리했다. 전체 프리팹에 적용할 경우 장치별 기준점과 파츠 소유권을 따로 검토해야 한다.
- Editor 캡처에서 상태 차이와 화면 포함을 확인했지만 실기기 Safe Area, 엄지 가림, 저사양 성능, 색각 차이, 장시간 피로도와 사람 완주는 미검수다.
- 전체 테마와 운영 화면으로의 적용은 이 대표 화면 검토 후 별도 작업으로 진행한다.
