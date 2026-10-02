# ANIMOL 이름 기반 스프라이트 교체 점검·적용

2026-10-03 · Unity 6000.3.8f1

## 점검 결과

기존 구조 전체가 이름 참조 방식은 아니었다. `ProductionCatalog.Find`는 이름으로 검색하지만 배열 자체, 씬의 Image/SpriteRenderer, 프리팹, 애니메이션 프레임, Tile, 로비 배경 카탈로그는 Unity GUID/fileID 직접 참조였다. 기존 PNG를 덮어쓰고 `.meta`를 유지하면 정상 갱신되지만, 삭제 후 새 GUID로 가져오면 직접 참조가 끊어질 수 있었다.

이제 **파일과 스프라이트 이름을 기준으로 기존 참조를 자동 재연결**한다. Unity의 Inspector와 저장된 씬은 정상적인 Sprite 참조를 그대로 표시한다. 이름 목록이 복구 기준이고, Unity 직접 참조는 렌더링과 빌드에 사용하는 캐시다. 모든 필드를 런타임 문자열 검색으로 바꾸는 방식은 사용하지 않았다. 이미지·스프라이트·하이어라키를 Play Mode에 새로 생성하지 않는다.

## 교체 방법

1. `Assets/ANIMOL/` 아래 **실제 사용하는 원본 이미지의 파일명과 확장자를 유지**하여 교체한다. 가능하면 기존 `.meta`도 유지한다.
2. `.meta`를 포함해 삭제하고 같은 이름으로 다시 가져와 GUID가 달라져도, 등록된 이름을 찾아 씬·프리팹·데이터 연결을 복구한다.
3. 시트는 기존 셀 구성과 스프라이트 이름을 유지한다. 새 GUID의 importer에는 기존 Point/압축/mipmap/PPU/pivot/9-slice/분할 영역·outline/physics shape 설정을 복원한다. 그림의 캔버스와 시트 규격까지 바꾸면 별도 배치·분할 검수가 필요하다.
4. 저장하지 않은 씬이나 열려 있는 Prefab Stage는 자동 저장하거나 닫지 않는다. 씬은 편집 저장 후, 프리팹은 편집 저장 후 Prefab Stage를 닫으면 자동 재연결을 재개한다. 필요하면 아래 Repair 메뉴로 다시 실행한다.

같은 이름의 이미지가 여러 폴더에 있으면 원래 폴더를 우선한다. 현재 `button_play`, `capsule_currency`가 두 아트 패키지에 각각 있으므로 폴더 구분을 함께 저장했다. 원래 경로가 없고 이름 후보가 여러 개라면 임의 선택하지 않고 검사 문제로 기록한다. 이름 자체를 변경하는 경우에는 새 이름으로 참조를 지정하고 Index 메뉴를 실행한다. 이미 존재하는 올바른 수동 참조는 덮어쓰지 않는다.

메뉴:

- `ANIMOL > Art References > Index current named sprites`: 현재 이름과 참조 목록 재수집.
- `ANIMOL > Art References > Repair references by name`: 이름 기준 재연결 및 검사.
- 자동 처리: 이미지 import/reimport/move, 씬·에셋 저장, 새 이미지 등록.
- 빌드 전: 연결을 복구하고 해결되지 않은 이름/편집 보류 문제가 있으면 빌드를 중단하고 이유를 표시한다.
- 최근 검사 결과: `Library/ANIMOLNamedArtLastReport.json`.

## 실제 등록 범위

| 항목 | 수 |
| --- | ---: |
| 게임 스프라이트 | **831**: 이미지 importer의 823개 + 로딩 Sprite subasset 8개 |
| 텍스처 | **241**: importer 이미지 234개 + 에셋 내부 텍스처 7개 |
| 참조를 가진 에셋/씬 | **413** |
| 이름과 연결된 참조 위치 | **2,150** |
| 애니메이션 클립 | 172 |
| 프리팹 | 97 |
| ScriptableObject 등 `.asset` | 135 |
| 씬 | 9 |

범위는 게임 소스인 `Assets/ANIMOL/` 전체다. Production UI 60개, 게임 배경 15레이어, 승인 v7 배경/토끼 Texture2D, 기존 로비 아이콘, 아틀라스·애니메이션·Tile·카탈로그·로딩 프레임이 포함된다. 프로젝트 밖 패키지 에셋과 `Assets/Temp/portrait-audit-wide.png` 검수 스크린샷은 게임 에셋 등록에서 제외했다. 코드로만 만든 흰 픽셀·DEV 표시용 도형처럼 교체할 원본 파일이 없는 절차적 리소스는 파일명 교체 대상이 아니다.

현재 전체 연결 감사 결과는 **복구 필요 0곳, 문제 0건**이다. 정상적인 기존 참조를 다시 저장하지 않고 이름 목록만 추가했으므로 실제 게임 아트와 화면 배치는 바뀌지 않았다. 기존 UI 생성기가 씬/프리팹/애니메이션을 저장하면 새 참조 목록도 갱신된다.

## 구현 파일

- `Assets/ANIMOL/NamedArt/Editor/NamedArtIndex.cs`: 이름·폴더·subasset·importer 설정 기록 및 이름 해석.
- `Assets/ANIMOL/NamedArt/Editor/NamedArtReferences.cs`: Unity SerializedObject/Prefab/Scene API로 참조 수집·복구. 씬 YAML 직접 수정 없음.
- `Assets/ANIMOL/NamedArt/Editor/NamedArtAutomation.cs`: import/save 자동 연결과 빌드 검사.
- `Assets/ANIMOL/NamedArt/Tests/Editor/NamedArtReferenceTests.cs`: 실제 파일 교체 테스트 11개.
- `ProjectSettings/ANIMOLNamedArtReferences.json`: 현재 프로젝트 전체 이름/참조 목록과 importer 설정. 이 파일도 Git에 보관해야 새 GUID 교체 후 복구할 수 있다.
- `Docs/NamedArtReferenceQA/`: 실제 테스트 응답, 참조 감사, Game View 자료.

런타임 플레이어·입력·물리·경제 코드, 기존 게임 데이터, 수동 맵과 기존 UI/게임 씬은 수정하지 않았다. 작업 시작 시 이미 수정되어 있던 추적 파일 97개는 SHA256 대조 결과 모두 작업 시작 상태와 일치한다(`preexisting-work-verification.json`). 기존 미커밋 파일은 이번 커밋에 포함하지 않았다.

## 실제 검증

Unity 스크립트 컴파일 완료. 최종 **EditMode 24/24, PlayMode 13/13**, 고유 테스트 총 **37개 통과**.

새 검사 11개는 다음을 실제 임시 PNG·프리팹·씬·클립·Tile로 실행했다. 운영 이미지는 바꾸지 않았으며 테스트 임시 파일은 정리했다.

1. 원본과 다른 GUID로 같은 이름 PNG 교체 → 정적 Image/비활성 SpriteRenderer 연결 복구, 오브젝트 수 유지.
2. 이름이 있는 시트의 셀·pivot·Point 설정 및 애니메이션/Tile 프레임 복구.
3. 씬 연결 복구와 저장하지 않은 씬 편집 보류.
4. 사용자가 지정한 다른 이미지와 명시적으로 비운 참조 보존.
5. 이름 중복 후보가 있을 때 임의 연결 거부.
6. 별도 재생성 버튼 없이 import 후 자동 복구.
7. Sprite subasset으로 저장된 로딩 프레임의 원본 Texture2D 재연결.
8. 같은 경로 삭제/재import에서 Unity가 이전 GUID를 재사용하는 경우도 시트 설정 복원.
9. 새 이미지 자동 등록 및 이후 교체를 위한 설정 보관.
10. 사용자 편집 중 미룬 복구를 씬 저장 후 재개하고 위치 편집 보존.
11. 열린 Prefab Stage의 연결 복구를 보류하고 창을 닫으면 자동 복구 재개.

기존 회귀 검사는 ProductionAsset 4개, v7 hierarchy/policy 6개, CommercialPolishPolicy 3개, ProductionFlow 7개, v7/Portrait PlayMode 6개다. 초기 구현 검사에서는 importer 전체 JSON 덮어쓰기를 Unity가 지원하지 않는 점과 테스트의 삭제 GUID 재사용 문제가 드러났다. 지원되는 importer 설정 API와 검증된 serialized sheet 필드 복원으로 수정했고, 실제로 다른 GUID를 만드는 테스트로 보완했다. 초기 실패 자료도 보관했으며 최종 결과에 섞어 PASS로 처리하지 않았다.

Prefab Stage 테스트의 첫 실행은 Unity 내부 지연 프레이밍 완료 전에 창을 닫아 실패했다. 창 준비를 기다리도록 테스트를 수정한 후 최종 24개를 다시 실행해 통과했다. 임시 씬 정리 과정의 마지막 씬 unload 경고는 테스트 응답에 남아 있으며 테스트 실패나 운영 씬 저장으로 이어지지 않았다.

최종 테스트 응답: `Docs/NamedArtReferenceQA/editmode-complete.json`, `playmode-final.json`. `editmode-final.json`은 이전 23개 통과 시점이며, `editmode-prefab-initial.json`은 Prefab Stage 초기 실패 기록이다. 이름/참조 전수 검사: `coverage.json`, `reference-audit.json`.

Game View 1080×1920의 [로비](NamedArtReferenceQA/Captures/lobby.png)와 [동물 강화](NamedArtReferenceQA/Captures/animal.png)를 추가로 확인했다. 정적 하이어라키를 유지했고 실행 중 누락된 UI sprite는 0개였다. 이번 변경은 Editor 전용 참조 관리이므로 전체 화면·모든 해상도 재검수나 플랫폼 플레이어 빌드는 실행하지 않았다. 빌드 전 검사 코드의 추가를 플랫폼 빌드 성공으로 보고하지 않는다.
