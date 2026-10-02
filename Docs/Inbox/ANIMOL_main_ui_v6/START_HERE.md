# ANIMOL 메인 UI 적용 패키지 v6

귀의 왕관·뿔 모양 장식을 제거한 판타지 토끼와 기존 5테마 배경을 포함합니다.
초록 세잎클로버 모양 귀 핀, 크림색 튜닉, 초록 조끼와 짧은 청록 망토로 복장도 바꿨습니다. 꽃과 금속 바, 줄기는 없습니다.

## 실행

1. 프로젝트의 `Docs/Inbox` 안에 이 폴더를 압축 해제합니다.
2. 아래 `APPLY_PROMPT.txt` 내용을 현재 ANIMOL 프로젝트에 연결된 Unity 작업 에이전트에 입력합니다.
3. 에이전트는 `UNITY_APPLY_TASK.md` 전체를 읽고 기존 로비에 실제 연결한 뒤 검증 결과를 남깁니다.

압축을 풀었을 때 다음 경로가 되도록 합니다.

`C:/Users/user/Documents/GitHub/ANIMOL/Docs/Inbox/ANIMOL_main_ui_v6/UNITY_APPLY_TASK.md`

압축 해제 도구가 폴더를 한 번 더 만들었으면 실제 경로를 명령에 사용합니다.
에셋만 Assets 폴더에 넣는 것으로 로비 연결이 완료되지는 않습니다.

## 내용

| 경로 | 용도 |
|---|---|
| `UNITY_APPLY_TASK.md` | 기존 UI 조사부터 실제 적용·검증까지의 작업 지시서 |
| `APPLY_PROMPT.txt` | 작업 에이전트에 입력할 명령 |
| `runtime/assets` | 20개 배경 레이어 + 2개 달리기 시트 |
| `runtime/manifest.json` | 규격과 좌표 기준 |
| `runtime/sweetie-16.hex` | 제공한 16색 |
| `reference/preview.html` | 인터넷 연결 없이 열 수 있는 동작 비교 미리보기 |
| `reference/animation.js` | 움직임의 기준 구현; Unity용 C#가 아님 |
| `reference/rabbit-design-v6.png` | 왕관·뿔을 제거한 캐릭터 확대 검토본 |
| `reference/rabbit-run-right-v6-preview.png` | 8프레임 검토본 |
| `reference/*verification.json` | 실제 수행한 에셋·네이티브 Canvas 검증 |
| `ART_CHANGE.md` | 이번 캐릭터 수정 사항 |
| `SHA256SUMS.txt` | 동봉 파일의 체크섬 |

352×704 화면에서 건물과 발판은 고정되고, 배경과 근경은 서로 다른 속도로 대각선 아래쪽으로 이동합니다.
테마는 5초마다 바뀌며 토끼는 발판 위를 무작위 좌우 방향으로 달립니다.
토끼는 64×96px, 8프레임, 14fps이며 시트 크기는 512×96px입니다.

## 확인 범위

- 런타임 PNG 22개의 크기, Sweetie16 색, 0/255 alpha를 검사했습니다.
- 좌우 시트의 프레임별 미러와 발 기준선 정렬을 확인했습니다.
- 배경 20개는 승인된 v2 PNG와 바이트가 같습니다.
- 기준 JS를 네이티브 Canvas에서 실행해 5테마 렌더, 시차, 발판 고정, 5초 주기와 좌우 방향을 검사했습니다.
- Unity 프로젝트 수정, C# 컴파일, 실제 Unity Game View 검증은 이 패키지를 만드는 환경에서 실행하지 않았습니다.
- 브라우저 화면의 실측 검증은 하지 않았으며 네이티브 Canvas 검증 결과와 구분했습니다.

## 구현 시 공식 참고

제공 에셋과 프로젝트 소스를 우선 사용하며, API는 실제 설치된 Unity 및 UGUI 버전에 맞춰 확인합니다.

- [Unity 6.3 TextureImporter](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TextureImporter.html)
- [Unity 6.3 Texture.filterMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Texture-filterMode.html)
- [UGUI Graphic API](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Graphic.html)

공식 문서 확인일: 2026-10-02.
