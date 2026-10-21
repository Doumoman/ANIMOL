# 윌리엄의 역설 · 별길 목장 v17 적용 결과

## 적용 범위

- 제공된 7개 ZIP을 `MCP_INBOX/v17` 한 루트로 합쳤다. `v17/v17` 중첩과 파일 충돌은 없었다.
- 변경 전 `Assets/WilliamParadoxStoryV14` 200개 파일을 `C:/Users/user/Documents/ANIMOL_Backups/WilliamStory_pre_v17_20261022.zip`에 보관했다. 생성기가 만든 이전 씬·프리팹 백업도 `C:/Users/user/Documents/ANIMOL_Backups/WilliamStory_builder_backups_v17.zip`으로 Assets 밖에 보관했다. 두 ZIP의 무결성을 검사했다.
- v17의 `unity/Runtime`, `unity/Editor`는 현재 설치된 코드와 대조했다. 제공 소스에는 이미 설치된 `StorySpriteRect.IsSpecified`, `foreground` 처리, 9:16 `StoryArtViewport` 및 회귀 테스트가 없고 새로운 동작 코드는 없었다. v16과 동일한 C# 동작 계약을 유지하기 위해 설치 코드를 보존했다. `schemaVersion: 14`와 `Resources/WilliamParadox/V14` 경로도 유지했다.
- Unity 6000.3.8f1에서 한국어 TMP Font Asset `Assets/WilliamParadoxStoryV14/Fonts/RanchStoryKorean SDF.asset`을 지정해 검증 전용 빌드 후 실제 생성 빌드를 수행했다. `Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity`, `Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab` 및 스토리·연출 JSON을 갱신했다. 기존 Sprite `.meta` GUID는 보존했다.
- v17 원화 80장을 v16과 디코딩한 RGBA SHA-256으로 비교했고 **80/80 변경**을 확인했다. 임포트 manifest의 60개 매핑은 51개 고유 PNG를 가리키며, 설치된 51개 PNG 모두 v17 원본과 바이트가 같다. 나머지 29장은 스타일 앵커·미사용 포즈·소품 등 manifest 비대상 원화로 합본에 보존했다. 원본 PNG를 재인코딩하거나 잘라 저장하지 않았다. 세부 결과는 [art_install_check.json](art_install_check.json)에 있다.

## 계약 및 검증

| 항목 | 상태 | 근거 |
|---|---|---|
| 로컬 delivery 구조 | PASS | `python tools/validate_delivery.py --root .`: 210노드, 194발화, 70비트, 17장면, 오류 0. [원본 결과](delivery_result.json) |
| 로컬 presentation·원화 구조 | PASS | `python tools/validate_presentation.py --root .`: 80 PNG, 7 레이어 장면, 79샷, 66카메라, 105화자 강조, R01 12포즈, 오류 0. [원본 결과](presentation_result.json) |
| Unity Editor 검증 빌드·생성 빌드 | PASS | v17 루트와 위 TMP Font Asset으로 `StoryPreviewBuilder.Build(..., true)` 후 `Build(..., false)` 성공. 빌드 지문 `6565b9562b92a88be61b6f329199cf390f96b68d27aa01865b6af92b3d8c4233_input0_newFalse`. `StoryEditorSafetyChecks.Run()` 17항목 통과. |
| Unity C# 컴파일 | PASS | Editor `recompile_status`: `up_to_date`, `compilationFailed=false`, 컴파일 오류 0. |
| EditMode | PASS | `WilliamParadox.Story.Tests` 27/27. 화자만 바운스, 누적 방지, 정지·복원·연출 줄이기, R17 HOLD 해제 및 R16 플래그 제한 등을 검사. [원본 테스트 결과](editmode_result.json) |
| PlayMode | PASS | `WilliamParadox.Story.Tests.PlayMode` 2/2. 실제 미리보기 대화 버튼으로 R16 전용 입력·귀환·R17 책 복구 순서와 1080×2400 세로 화면 cover를 검사. [원본 테스트 결과](playmode_result.json) |
| 실제 Editor Play 화면 | PASS | 1080×1920 R01의 네 인물 귀·손, 탁자 네 잔·한 주전자와 한국어 대사를 육안 확인. R05/R06/R08/R10/R13 장면도 전환 완료 후 확인. R13부터 실제 버튼 50회로 R16 전용 입력을 지나 R17 도달; `ISOL_WANTS_RETURN`, `PLAYER_BLOCKS_LAMP`, `RETURN_PATH_USED` 모두 참. [R01](R01_Game_1080x1920.png), [레이어 장면](LayeredScenes_Game_contact.png), [R17](R17_Game_1080x1920.png) |
| 실제 모바일 기기·서로 다른 실제 Safe Area | NOT_RUN | 연결된 실기기에서 실행하지 않았다. Unity 테스트의 1080×2400 검사는 실기기 확인이 아니다. |

`data/story_presentation.json`은 실제 `Resources/WilliamParadox/V14/Story/story_presentation.json`에 소스와 같은 바이트로 임포트됐다. R01은 투명 배경·인물 4명·전경 탁자 레이어를 사용하며 Sprite Rect 메타데이터와 `preserveAspect`로 표시한다. 7개 지정 장면의 105발화에만 독립 화자 강조가 있고, 다른 10장면에는 이 강조를 추가하지 않았다. 미리보기는 캠페인 보상을 호출하지 않는다.

Play 검수 후 스토리 QA 저장을 초기화하고 Play를 종료했으며, 원래 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity`를 다시 열어 `dirty=False`를 확인했다. 메인 UI·캠페인·대전·퍼즐·프로젝트 설정은 이 작업에서 변경하지 않았다.
