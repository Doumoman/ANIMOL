ANIMOL 별길 목장 장치 V13 R3 / installationRevision 3

물웅덩이와 구덩이 사이에 낮은 연결 수로와 작은 개폐 수문을 추가했다.
가로 2×1 / 세로 1×2, 네 방향 물 흐름을 별도 스프라이트로 제공한다.
물이 흐르는 방향은 행동 전 물칸→마른 칸, 플레이어 방향은 그 반대다.
열림→이동하는 물결→수위 이전→닫힘의 12프레임, 12fps(1초)이며 게임 판정은 애니메이션과 분리된다.

Sprite 325개 / 애니메이션 정의 158개. 기존 277 PNG 경로를 유지하고 6개 가로 수문 그림만 교체했다.
양 밀기/풀과 양 제거/바람의 R2 규칙과 아트는 유지한다.
1×1 320×320, 2×1 576×320, 1×2 320×576, PPU256, Pivot(.5,.5).

최신 규칙: DEVICE_RULES_KO.txt. 적용 명령: UNITY_APPLY_CLI_KO.txt.
게임 참조 판정: Runtime/Rules. 새 표현 연결: Runtime/RanchPondSluiceVisualR3.cs.
R2의 분리 셀 렌더러는 호환용이다. 이번 수로는 R3 합성 렌더러 하나로 표시한다.
실제 프로젝트의 맵/엔티티/입력/Undo 연결과 Unity 컴파일·NUnit·플레이 검증은 NOT_RUN이다.
원화/최종 프롬프트: Authoring~/native 및 Authoring~/generation_prompts_r3.json.
