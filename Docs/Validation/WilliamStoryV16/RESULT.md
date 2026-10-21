# 별길 목장 v16 Unity 적용 결과 (2026-10-22)

## 적용 범위

- 여섯 ZIP을 `MCP_INBOX/v16` 한 루트로 합쳤다. `data`, `spec`, `art`, `unity`, `tools`를 포함한 172개 파일이며 원본 PNG는 수정하지 않았다.
- 기존 `Assets/WilliamParadoxStoryV14`에 v16 Runtime·Editor 코드와 EditMode 테스트를 적용하고 같은 `Resources/WilliamParadox/V14` 경로에서 미리보기를 재생성했다. `schemaVersion`은 14다.
- 교정 장면 R04, R07, R09, R11, R14, R15의 PNG와 새 레이어 자산을 임포트했다. 매핑 60건 모두 소스와 임포트 파일의 바이트가 일치한다. 기존 장면 PNG의 `.meta` GUID는 보존했다.
- `story_presentation.json`을 `Resources/WilliamParadox/V14/Story`에 임포트하고 `StoryRunner.presentationJson`과 새 레이어 스테이지를 씬·프리팹에 연결했다.
- Unity `JsonUtility`가 JSON에서 빠진 중첩 `rect`도 빈 객체로 만드는 동작을 보정하고 `foreground` 자산 종류를 허용했다. 세로 화면의 9:16 작품 뷰포트를 생성기에 복구했다. 해당 회귀 테스트를 추가했다.

## 검증

| 항목 | 결과 | 근거 |
|---|---|---|
| `validate_delivery.py --root .` | PASS | 194발화, 210노드, 70비트, 17장면, 오류 0 |
| `validate_presentation.py --root .` | FAIL | 105발화 강조와 80 PNG 구조 검사는 확인했으나 R09 PNG의 카탈로그 SHA/바이트 크기 불일치 1건 |
| Unity C# 컴파일 | PASS | Unity 6000.3.8f1, `recompile_status` 오류 0 |
| Editor 생성기 검증 전용·씬/프리팹 빌드 | PASS | 실제 한국어 TMP 글꼴 지정, 53개 고유 파일 임포트, 1080×1920 기준 생성 |
| Editor 경로·출력 안전 검사 | PASS | 17개 |
| EditMode | PASS | [원본 결과](editmode_result.json) 27/27 |
| PlayMode | PASS | [원본 결과](playmode_result.json) 2/2, R16 전용 입력과 R17 귀환·책 기록 경로 포함 |
| Game 화면 | PASS | 1080×1920 R01 구성과 여섯 교정 장면 화면 캡처 확인. 1080×2400 뷰포트 PlayMode 검사 통과 |
| 서로 다른 기기 Safe Area, 모바일 실기기 | NOT_RUN | 실행할 기기 연결 없음. Unity 화면 비율 검사는 실기기 검사와 구분함 |

R09 원본은 제공 ZIP에서 `1,777,820`바이트, SHA256 `9376c14b3df0bcd35ae080253708fbe86cabe17399ede92116d1984a76e72e0c`다. 패키지 `spec/ASSET_CATALOG.json`은 `1,745,296`바이트, SHA256 `f75ac69996a837a348ea21a0b5541bd2cc32ebf20463ed726a80d2ed9a0c12b6`을 기대한다. 사용자는 현재 PNG를 그대로 적용하고 불일치를 기록하도록 선택했다. Unity 생성기 통과는 이 패키지 카탈로그 불일치가 해결되었다는 뜻이 아니다.

## 화면·백업

- [R01 실제 Game 화면](R01_Game_1080x1920.png): 네 인물, 귀·손, 탁자의 네 잔과 한 주전자, 한국어 대사를 확인했다.
- [여섯 교정 장면 Game 화면](SixScenes_Game_contact.png), [R09 원본 크기 Game 화면](R09_Game_1080x1920.png). 캡처는 Unity Play 중 `ScreenCapture`로 얻었다.
- 변경 전 전체 스토리 패키지 백업: `C:\Users\user\Documents\ANIMOL_Backups\WilliamStory_pre_v16_20261022.zip`. 생성기의 중간 백업도 같은 폴더의 `WilliamStory_builder_backups_v16.zip`에 보관했다.
- Play 검수 후 `ANIMOL_Main.unity`의 깨끗한 편집 상태로 복귀했다. 메인 UI, 캠페인, 대전, 퍼즐, 보상, 입력 패키지와 프로젝트 설정은 이번 변경 범위에 포함하지 않았다.
