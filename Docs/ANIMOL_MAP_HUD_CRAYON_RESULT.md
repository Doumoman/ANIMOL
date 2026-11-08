# 별자리 지도·퍼즐 HUD 크레용 통합 결과

2026-11-09, Unity **6000.3.8f1**, Built-in Render Pipeline, Windows Editor / Direct3D 12에서 적용했다. 입력 패키지는 `MCP_INBOX/ANIMOL_Crayon_UI_Complete.zip`이다.

## 실제 적용 대상

| 실제 사용 경로 | 연결 결과 |
| --- | --- |
| `Assets/ANIMOL/Scenes/Main/NotebookUI.unity` → `Prefabs/UI/Main/NotebookCanvas.prefab` → `Design 1080 Safe Area`의 `NotebookRoot.stageScreen` | 지도 패널·버튼·별·망원경·I~IV 탭에 장식 바인딩 **343개** |
| 같은 `stageScreen`의 walletText / stageName / node.label / node.number / I~IV | 새 정적 TMP 숫자 폰트 **226곳**. 한글 테마 이름·본문은 기존 폰트 유지 |
| `NotebookProjectGateway.gameplayCanvasPrefab` → `Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab` | 실제 입장 시 생성되는 HUD. 장식 바인딩 **7개**, 남은 턴 숫자 스프라이트 **10개** 교체 |

두 실제 프리팹을 각각 Prefab Mode로 열고 자동 저장을 끈 뒤, 정확히 한 Root/HUD를 선택하여 **지도 장식 → 지도 숫자 → HUD 장식 → HUD 숫자** 순서로 설치기 진입점을 실행했다. 검토와 Undo/Redo 후 명시적으로 저장했다. Main 씬 파일은 저장하거나 수정하지 않았으며 기존 프리팹 연결을 통해 적용된다.

개별 오브젝트 경로와 연결된 A 이미지·재질은 [ObjectMapping.json](Validation/MapHudCrayon/ObjectMapping.json)에 기록했다. 별도 샘플 씬이나 새 UI 생성 메뉴는 사용하지 않았다.

## 병합과 수정

- 실제 코어 경로는 예전 WilliamNotebookUI 경로가 아닌 `Assets/ANIMOL/Scripts/Runtime/Notebook/NotebookRoot.cs`다. 새 자산을 임포트하기 전에 읽기 전용 `IsReducedMotion` getter 한 줄만 추가했다.
- 제공된 두 Assets 폴더와 `.meta`를 함께 병합했다. 스킨 PNG 333장과 숫자 PNG 10장, TTF·WOFF2의 바이트 및 제공 메타 **593개**의 GUID를 원본 패키지와 비교했다. 일치한다. SourceArt/Preview/Tools/Docs/SourcePatches는 Assets로 복사하지 않았다.
- 설치기의 예전 `Assets/WilliamNotebookUI/Textures/…` 참조는 현재 `Assets/ANIMOL/Art/UI/**/Textures/…`의 정확한 상대 경로로 해석하도록 보완했다. 기존 아트는 이동·복사·교체하지 않았다. 모호한 경로는 적용을 중단한다.
- 제공 설치기의 생성 Undo 등록 순서에서 장식 중복과 Redo 참조 손실을 발견해 수정했다. 생성 등록 후 교차 참조를 기록하며, 재적용 시 이 패치가 소유한 동일 이름·전용 스프라이트·비입력 장식만 중복 정리한다. 원본 UI는 삭제하지 않는다. 이미 올바른 PNG/재질은 재임포트·재저장하지 않는다.
- 새 TMP 폰트는 `CrayonNumerals/Generated/Fonts/`의 **Static SDF**이며 아틀라스·재질도 새 자원이다. 원래 폰트는 fallback으로만 참조한다. `costNumber.Value`, area, places, 최대 높이, gap과 표시 중 숫자의 인덱스를 유지했다. 설치기에서 `SetNumber(0)`이나 재시작을 호출하지 않는다.

## 보존 확인

작업 시작 상태 940개 항목과 원본 씬·프리팹·폰트·Build Settings를 `C:\Users\user\Documents\ANIMOL_Backups\MapHudCrayon_20261109_b8b2d5b1`에 보관했다. 원래 두 대상 프리팹은 Git에서 깨끗한 상태였고, 기존 사용자 변경은 커밋에서 제외했다.

원본 직렬화 레코드를 백업과 비교했다. **기존 RectTransform 2,001개의 수치, 모든 기존 Button 이벤트/targetGraphic/SpriteSwap, Root/HUD 코어 참조와 보호 화면은 동일하다.** 허용된 차이는 지도·HUD의 장식 컴포넌트/자식, 원본 그림의 시각 알파, 지정 숫자 폰트와 HUD 숫자 스프라이트뿐이다. Unity 저장에서 `-0`이 `0`으로 정규화된 것은 같은 수치로 비교했다.

Prefab Mode의 기존 ExecuteAlways 레이아웃이 자동 계산한 좌표·CanvasScaler 값은 Unity API로 원래 저장 값으로 복원했다. 테스트 중 TMP가 갈무리 폰트에 자동 추가한 글리프도 Editor에서 원본 백업으로 복원·재임포트했다. 최종 비교에서 Main 씬과 메타, 두 프리팹의 메타, Galmuri14 원본과 메타, Build Settings의 **7개 파일이 바이트 단위로 동일**하다. [보존 검사](Validation/MapHudCrayon/PrefabPreservation.json)

메인·설정·일시정지 팝업·클리어 팝업·공용 모달에는 새 바인딩이 없다. 캐릭터 얼굴 스크롤링, 별자리 원화·연결선·catalog 좌표, 월드 수집물·타일·충돌·카메라·EventSystem 및 경제/진행 코드를 수정하지 않았다. 기존 전투 미리보기에도 변경이 없다.

## 실제 Unity에서 실행한 검증

| 검증 | 결과 |
| --- | --- |
| C# 재컴파일 / 전용 셰이더 진단 | 오류 **0**, 컴파일 경고 **0**, 셰이더 메시지 **0** |
| PNG 333장, 재질 222개, 숫자 PNG 10장, 정적 SDF | Import Settings·크기·Border·A/B/C 참조·Motion·프레임 속도 검사 통과, SpriteAtlas 포함 없음 |
| 두 실제 프리팹 | 누락 스킨 참조 **0**, Missing Script **0**, 원본당 장식 자식 정확히 **1개** |
| Undo/Redo·재적용 | 실제 대상에서 장식·숫자 Undo/Redo 및 재적용 통과. 미적용 상태로 만든 임시 HUD 복제에서도 Undo 시 장식 제거, Redo/재적용 시 7개만 복원됨. 이 검증용 복제는 저장하지 않음 |
| `MapHudCrayonPlayTests` | **3/3 통과**. 실제 Main의 지도 숫자/움직임 감소/선택 별 독립 4프레임 회전, 잠금·미보유 지역·다시보기 표시, 실제 HUD 프리팹의 상태별 표현 검사 |
| `NotebookThemePatchPlayTests` | **4/4 통과**. I~IV 이동, 스크롤 취소/재지정, 탭 퇴장 후 패널 표시, 포커스/중지/감소 모션, 네 화면 크기·노치 모의 안전 영역 |
| `ClearedStarwayReplayAndEntryUseRealAssetsWithoutReplayCharges` | **1/1 통과**. 실제 스토리 다시보기 무차감, SW00 실제 퍼즐 입장, HUD 45→44 턴 반영, 지도 복귀와 입장 단일 차감 |
| `WalletApprovalsAndBattleGateRequireUniqueVerifiedEvents` EditMode | **1/1 통과**. 기존 검증 승인·중복 차감 방지 정책 |
| D3D12 GPU 프레임 선택 | 0/0.3초 동일 A, 0.61초 B, 1.21초 C, 1.81초 A 반복. `_Motion=0`은 A와 동일 픽셀. [GPU 결과](Validation/MapHudCrayon/GpuFrameSelection.json) |

HUD의 남은 턴 **0·9·10·17·99·100**, 목표 별 **1~4개 각각의 모든 수집 상태**, 망원경 **0~5** 잔량·눌림·비활성, 일시정지/복귀·클리어 표시는 실제 HUD 프리팹에 검증용 권위 상태 스냅샷을 공급해 확인했다. 이 화면 검증용 게이트웨이는 테스트 어셈블리에만 있으며 실제 BM/전투 구현이 아니다. 실제 퍼즐 연결은 위 별도 입장 테스트에서 확인했다.

기존 테마 스크롤 테스트는 코루틴 호출 즉시 진행되는 첫 프레임을 1px 이내 정지로 가정했다. 실제 프레임 시간에 따른 이동 상한과 목표 방향을 검사하도록 테스트만 보완했고, 스크롤 구현은 바꾸지 않았다.

기존 `ActivePortraitSceneOpensSingleMapPurchaseAndSettings`도 실행했으나 사용자에게 이미 클리어된 SW02를 잠긴 별로 가정하여 실패했다. 이 테스트는 수정하지 않았다. 새 상태 격리 테스트에서 저장 기록을 보존하며 잠금/미보유 지역/다시보기를 별도로 검증했고 통과했다. 통과한 9개 검사와 이 기존 테스트 실패를 구분한다. 테스트 전환 중 발생한 일시적 TestRunner 상태 오류는 재실행으로 해소했다.

과거 `CommercialPolishPolicyTests`는 Legacy 보관 범위이므로 실행하지 않았다. 이번 작업은 새 피드백 사건·게임 판정·연출 강도 정책을 추가하지 않는다.

## 캡처와 남은 검증

- [지도 기본·I~IV](Validation/MapHudCrayon/Map_Default_1080x1920.png), [선택 패널·숫자](Validation/MapHudCrayon/Map_Selected_Numerals.png)
- [HUD 목표 별 4개·턴 17](Validation/MapHudCrayon/HUD_Target_4.png), [턴 100](Validation/MapHudCrayon/HUD_Turns_100.png)
- [망원경 0](Validation/MapHudCrayon/HUD_Telescope_0.png), [망원경 5 눌림](Validation/MapHudCrayon/HUD_Telescope_5.png)
- [390×844](Validation/MapHudCrayon/HUD_Portrait_390x844.png), [1440×900](Validation/MapHudCrayon/HUD_PC_1440x900.png)
- [유지된 일시정지 팝업](Validation/MapHudCrayon/Protected_Pause.png), [유지된 클리어 팝업](Validation/MapHudCrayon/Protected_Clear.png)

캡처는 현재 UI 상태를 임시 카메라로 RenderTexture에 직접 렌더했다. HUD 캡처의 단색 배경은 UI 상태 검증용이며 게임 배경 교체가 아니다. 실제 카메라 자산은 변경하지 않았다.

패키지 제공자가 보고한 Chromium 13개/C# 구문 검사는 이번 Unity 실행 결과와 별개이며 여기서 재실행하지 않았다. **실물 모바일 GPU·노치 기기·네이티브 광고/구매 서비스는 실행하지 않았다.** Unity GameView와 노치 모의 검사는 실제 모바일 장치 검증을 대체하지 않는다. 패치 대상에 미연결된 필수 항목은 없다.
