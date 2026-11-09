# 크레용 글씨체 적용 결과 — 2026-11-09

마지막 요청인 “글씨체만 변경”을 적용 범위로 삼았다. 기존 크레용 장식, 셰이더, 캐릭터 얼굴 스크롤링, 게임/BM 코드는 수정하지 않았다.

- 실제 빌드 진입 씬: `Assets/ANIMOL/Scenes/Main/NotebookUI.unity`
- 씬 객체: `William Notebook UI/Design 1080 Safe Area` (`NotebookRoot`)
- 지도 원본: `Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab`의 `NotebookCanvas/Design 1080 Safe Area`
- 실제 게임 HUD: 위 Root의 `NotebookProjectGateway.gameplayCanvasPrefab`이 참조하는 `Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab`의 `GameplayCanvas/Gameplay Safe Area 1080`

현재 Root 스크립트는 `Assets/ANIMOL/Scripts/Runtime/Notebook/NotebookRoot.cs`에 있다. `IsReducedMotion` getter가 이미 있으므로 코어 스크립트를 수정하거나 예전 경로를 복구하지 않았다.

`ANIMOL_Crayon_UI_Complete.zip`의 확장 TTF·WOFF2, TTF 메타, 문자 매니페스트, 숫자 설치기를 기존 경로에 병합했다. ZIP에는 중앙 디렉터리가 없었으나, 로컬 헤더의 1,209개 항목 전체를 압축 해제해 길이·CRC를 확인했다. 원본 ZIP은 변경하지 않았다. SourceArt/Preview/Tools/Docs를 Assets에 복사하지 않았다.

Unity를 Play Mode가 종료된 상태로 열고 실제 지도 프리팹을 Prefab Mode에서 선택해 `ANIMOL > Crayon Numerals > Apply To Selected Map Numbers`를 실행했다. 110개 노드의 number/label 220개, 망원경 지갑 1개, I–IV 4개, 선택 스테이지 ID 1개, 합계 226개 TMP 필드가 새 폰트로 연결됐다. 한글 및 설명 문장과 보호 화면의 폰트 참조는 유지했다.

새 정적 96자 SDF는 `CrayonNumerals/Generated/Fonts/ANIMOL_Crayon_Numerals_d8f41ee5bad5bde4ba5c8c0505cfe24e_Keyboard.asset`이다. 2048×2048 아틀라스와 기본 머티리얼은 이 asset의 subasset이고 별도 스타일 머티리얼은 Generated/Materials에 있다. 기존 숫자 전용 Generated 폰트·머티리얼·아틀라스는 그대로 보존했다. Undo/Redo가 이전 참조를 복구하고 생성 자원은 남는 것, 재적용 시 동일 자원을 재사용하는 것을 실제 Editor에서 확인했다.

HUD 숫자 메뉴도 실제 HUD의 Prefab Stage에서 확인했다. 기존 digits[0..9]와 표시 이미지는 ZIP과 동일한 220×280 크레용 PNG를 이미 사용하고 있어 최종 HUD 프리팹에는 변경이 없다. 설치 과정에서 SetNumber, 재시작, BM 초기화를 호출하지 않았다. 장식 적용 메뉴와 씬/지도 생성 메뉴는 실행하지 않았다.

최종 프리팹 직렬화 비교 결과 지도는 226개 텍스트의 `m_fontAsset`와 `m_sharedMaterial`만 바뀌었다. 다른 직렬화 필드, RectTransform, 버튼, 이벤트와 객체 수는 동일하다. 프리팹을 열며 기존 편집용 코드가 갱신한 레이아웃/TMP 기본값은 Unity API로 원래 값으로 복원한 후 저장했다. 기존 사용자 수정이 있던 NotebookUI 씬은 저장하거나 커밋하지 않았고, 그 씬에서 새 프리팹 폰트가 실제 상속되는 것을 확인했다.

## 실행한 검증

- Unity 6000.3.8f1 import·C# 컴파일 및 TMP 생성: 완료. 최종 Console의 컴파일 실패, 오류, 경고 0개.
- TTF와 WOFF2: printable ASCII 95자와 U+20A9를 합한 정확히 96자, 숫자 advance 650 확인. 원본 표시 PNG도 별도로 렌더링했다. WOFF2는 FontTools로 디코딩한 로컬 표시 검증이며 브라우저 검증은 아니다.
- `ArtAssetTests`: EditMode 4/4 통과.
- `ArtQaPlayTests`: 세로 GameView PlayMode 1/1 통과. 작은 세로 크기와 모의 Safe Area 검사 포함.
- `MapHudCrayonPlayTests`: PlayMode 3/3 통과. 실제 지도 씬과 실제 HUD 프리팹을 사용했다. 지도 잠금·미보유 지역·다시보기 조건, 독립적인 선택 별 회전, 남은 턴 0/9/10/17/99/100, 목표 별 1–4개와 수집 단계, 망원경 0–5칸·눌림·비활성, 일시정지·복귀·검증된 클리어 표시, 1080×1920/390×844/1440×900 UI 검사를 포함한다. 테스트용 gateway/스냅샷을 이용한 표시 검사이며 실제 구매/광고/입장 거래나 실제 맵 클리어를 수행한 것은 아니다.
- 기존 장식 폴더, 숫자 PNG 및 메타, 이전 Generated 자원, Galmuri 원본 TTF/스타일 머티리얼, Build Settings, Root 코드, 메인 씬, HUD 프리팹의 파일 해시 보존 확인.
- `CommercialPolishPolicyTests`는 레거시에 격리된 테스트여서 실행하지 않았다. I–IV 전체 이동, 실제 노치 기기, 모바일 GPU, 실제 구매·광고·전체 퍼즐 진행은 이번에 별도 실행하지 않았다.

## 보존 검사에서 발견한 예외

`Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset`는 작업 전부터 사용자 미커밋 변경이 있던 파일이다. Unity Editor/Play Mode 검증 중 TMP가 이 동적 한글 폰트의 캐시를 추가로 자동 저장했다. 원본 TTF·스타일 머티리얼·한글 폰트 참조와 전역 기본 폰트 설정은 유지했지만, 검증 전 캐시의 정확한 바이트 백업은 확보하지 못해 완전한 캐시 보존이나 복원을 주장하지 않는다. 사용자 변경을 HEAD로 덮어쓰지 않았으며 이 파일은 이번 커밋에서 제외했다. 전후 SHA-256은 `Validation/CrayonTypography/Preservation.json`에 기록했다.

실행 근거와 실제 변경 필드 경로는 `Validation/CrayonTypography/`의 MapApply/ActualScene/HudApply, 테스트 결과 JSON, Preservation.json에 있다. FontSourcePreview는 원본 폰트 표시 미리보기이며 Captures는 Unity 실행 캡처다.
