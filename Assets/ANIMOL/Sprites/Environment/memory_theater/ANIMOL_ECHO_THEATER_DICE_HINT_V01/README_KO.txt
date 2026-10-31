ANIMOL 잔영 극장 — 전체 면 예고·목표 흡수 발판 V01 수정 R3

예고는 하나
지금 밀 수 있는 방향의 도착 칸에 다음 주사위 면 하나만 희미하게 깜빡인다.
아이콘 여백을 보정해 실제 면 그림이 칸 너비의 약98%를 채운다.
목표 칸도 예고 중에는 요구 면과 받침을 잠시 숨겨 예고 면 하나만 보인다.
예고가 사라지면 미완성 목표는 원래 요구 면/받침 표시로 돌아온다.
막힘·먼 위치·대각·굴림 중·입력 잠금에서는 예고를 숨긴다. 기본 알파.25~.55/1.2초 주기다.

목표에 맞는 주사위가 들어가면
1. 실제 한 칸 굴림이 확정된 뒤,목표 셀과 실제 윗면의 일치를 검사한다.
2. 일치하면 그 행동 안에서 주사위를 소비하고 목표를 통행 가능한 발판 상태로 전환한다.
3. 원래 주사위가 아래로 눌려 들어가고 받침 테두리가 낮아지는 연출을 표시한다.
4. 마지막은 높은 주사위가 없는 평평한 나무 바닥이다. 플레이어가 다음 입력으로 지나간다.
다른 면이면 전환하지 않고 주사위가 고체로 남아 다시 굴릴 수 있다.
완성은 한 판 동안 유지하며 Undo/Reset/로드는 실제 게임 스냅샷에 맞춰 복원한다.

자원
원래 실제 주사위40 PNG와 면 아이콘6개는 R2와 바이트가 같다.
새 받침4개: ready / half_pressed / nearly_flush / walkable.
받침 PNG320×320/PPU256/중앙Pivot,논리 셀256px와 바깥 여백32px.
완성 발판에는 주사위/점/숫자를 남기지 않아 이동 바닥으로 읽히게 했다.
기존 극장 금색·버건디·목재·남색 윤곽선에 맞춘 native imagegen 원화다.

faces_manifest.json revision3. Tools > ANIMOL > Dice face hints R3 > Build face and platform sets로
DiceFaceSet.asset과 DiceGoalPlatformArt.asset을 실제 생성한다.
기존 HintView/GoalFaceView/Feed/Source 이름과 설치 경로는 유지한다.
DiceGoalPlatformViewV03의 ShowReady/ShowPressing/ShowWalkable은 표시만 담당한다.
주사위 엔티티 소비와 발판의 충돌·지지·통행 판정은 실제 호스트가 원자적으로 갱신해야 한다.
연출은 Collider가 있는 부모 대신 전용 자식 Renderer에서 수행한다.
DiceGoalConversionIntentV03은 실제 굴림/목표 셀/윗면 일치의 순수 판단 보조다.
표시 코드 설치만으로 실제 점유/통행 로직을 구현했다고 보고하지 않는다.
흡수와 연출은 추가 코스트/장치 턴을 만들지 않는다. 기존3방울 후 출구 승리를 유지한다.

UNITY_APPLY_PREVIEW_CLI_KO.txt: 호스트 소비/발판 커밋,시각 연결,Undo/Reset/로드 적용 명령.
Examples~/HostBindingExample.cs.txt: 실제 API로 대응할 연결 예.
Preview는 큰 예고 하나→굴림→누름→발판→통과라는 오프라인 설명이며 Unity 캡처가 아니다.
원본 보존/면 대응/큰 예고 크기/누름 상태/PNG/ZIP을 오프라인 검수한다.
실제 프로젝트가 없어 Unity 컴파일/임포트/게임 로직 적용/EditMode/PlayMode는 NOT_RUN이다.
