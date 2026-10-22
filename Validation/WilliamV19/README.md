# 윌리엄의 역설 · 별길 목장 v19 적용 검증

2026-10-22. 작업 전 기존 `Assets/WilliamParadoxStoryV14`에는 미커밋 변경이 없었다. 설치본 전체는 `MCP_INBOX/william_pre_v19_backup.zip`에, 빌더가 만든 이전 씬·프리팹 백업은 `MCP_INBOX/william_v19_builder_backups`에 보관했다. 이 두 보관본과 7개 원본 ZIP은 작업용 로컬 전달물이며 이번 Git 커밋에 포함하지 않는다.

## 적용

- `MCP_INBOX/william_v19_*.zip` 7개를 동일한 `MCP_INBOX/v19` 루트로 합쳤다. 4개 layer ZIP을 모두 포함했고 `v19/v19` 중첩은 없다.
- v19의 `data/ranch_story.json`, `data/story_presentation.json`과 60개 논리 자산 매핑을 기존 `Resources/WilliamParadox/V14` 경로로 임포트했다. 실제 PNG 파일 51개의 원본 바이트를 유지했다.
- 한국어 글꼴은 기존 `Assets/WilliamParadoxStoryV14/Fonts/RanchStoryKorean SDF.asset`을 사용했다. Unity 6 (`6000.3.8f1`)에서 검증 전용 실행 후 씬과 프리팹을 생성하고 씬을 열었다.
- 동봉된 일부 C#은 저장소의 `14.1.1-layered-v16`보다 오래된 `14.1.0` 코드였다. 이를 그대로 복사하면 누락된 Rect를 잘못 판정하고 `foreground`를 거부해 Unity 검증이 실패한다. 기존의 Rect 처리, `foreground` 지원, 뷰포트 마스킹 및 관련 테스트를 보존했다. `schemaVersion=14`, `Resources/WilliamParadox/V14`와 namespace도 유지했다.

## 실행 결과

- 7개 ZIP 모두 33 MiB 미만, CRC 통과. `spec/DELIVERY_LAYOUT.json`의 203개 멤버 크기·SHA-256과 압축 해제 파일의 바이트가 일치했다. 공급자가 문서에서 지정한 `qa/package_validation.json`은 7개 ZIP에 포함되지 않았다. 독립 검사 결과와 ZIP별 SHA-256은 [local_validation.json](local_validation.json)에 있다.
- `python tools/validate_delivery.py --root .`: 통과. 210노드, 194발화, 70비트, 17장면, 60논리 자산.
- `python tools/validate_presentation.py --root .`: 통과. 79샷, 66카메라, 17장면 커버리지, 강조 발화 105개, R01 포즈 12개, 원본 PNG 80개. 승인 R03/R04는 각 참조 파일과 바이트가 같다.
- Unity C# 재컴파일: 통과. 빌더 검증 전용 실행: 통과. 생성 후 60개 자산 매핑과 두 스토리 JSON을 소스 원본과 바이트 비교: 일치.
- `WilliamParadox.Story.Tests` EditMode: 27/27 통과. 빠른 진행 시 Transform 비누적, 중단·복구, R16 게이트, R17 귀환 저장과 책 전환에 관한 단위 테스트를 포함한다.
- Unity Play 모드에서 생성 씬을 시작하고 Game View 1080×1920을 확인했다. 첫 장면의 한국어 글리프, 네 인물, 탁자 가림, 대화 UI가 표시됐다. 실제 Unity 캡처는 [Unity_Play_R01_1080x1920.png](Unity_Play_R01_1080x1920.png)다.

기기 Safe Area, 앱 이탈·복귀, 전체 17장면의 실제 플레이 진행과 프레이밍은 이번 검증에서 실행하지 않았다. PlayMode 자동 테스트도 없다. PDF와 HTML은 Canvas2D 검토 자료이며 Unity 캡처로 취급하지 않는다. 이 미리보기는 캠페인 UI·퍼즐 클리어·재화·보상·별자리 저장을 변경하지 않는다.
