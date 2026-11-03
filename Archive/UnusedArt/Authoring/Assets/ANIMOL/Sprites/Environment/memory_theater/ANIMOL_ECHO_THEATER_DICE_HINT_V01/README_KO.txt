ANIMOL 잔영 극장 — 주사위 흡수 애니메이션 R5 — 주사위75%

R5 변경: 실제 주사위의 가로·세로를 R4의75%로 균일하게 줄였다.
흡수 복사본은 DiceVisualSizeV05.CanvasWidthInCells(.9375칸 캔버스)를 쓴다.
보드 위/굴림 Sprite도 호스트의 미축소 시각 자식 스케일에 FromOriginalScale을 적용한다.
부모 Collider/점유/목표 틀/발판/큰 희미한 예고 면은 변경하지 않는다.
.75를 매 프레임 현재 스케일에 곱하지 않는다. 원본 기준값에서 항상 다시 계산한다.
크기는 연출 시작 전에 이미75%이며,흡수 중에 점점 작아지는 효과가 아니다.

주사위가 목표 칸에 들어가는 연출을 다시 만들었다.
주사위의 가로·세로 비율과 불투명도를 유지하며 실제 원본 Sprite를 아래로 이동시킨다.
앞쪽 금색 테두리의 뒤로 가려지고,완전히 들어간 뒤 두 나무 덮개가 안쪽으로 닫힌다.
목표 틀의 외곽은 고정되어 흔들리지 않는다. 끝은 평평한 통행 발판이다.

타임라인 약.80초
0~12%: 짧은 접촉/자리 잡기.
12~67%: 주사위를 원래 형태로 아래로 내리고 앞 테두리가 점차 가린다.
67~94%: 주사위가 완전히 가려진 뒤 좌우 덮개를 닫는다.
94~100%: 닫힌 나무 발판에 정착한다.
주사위 자체에 Y축 스쿼시나 소멸 페이드를 적용하지 않는다.

파일
기존 실제 주사위40 PNG와 예고 면6개는 이전 버전과 바이트가 같다.
새 목표 틀 원화와 front_rim/closing_rim/lid_left/lid_right 레이어를 사용한다.
Assets/ANIMOL/Scripts/Runtime/DiceAbsorbMotionV04.cs: 연속 타임라인.
Assets/ANIMOL/Scripts/Runtime/DiceVisualSizeV05.cs: 원본 시각 스케일을 보관하고75%를 재적용한다.
Runtime/DiceReceiverClipV04.shader: 각 목표의 앞 테두리 뒤에서만 주사위를 가리는 클리핑.
DiceGoalPlatformViewV03 클래스/API 이름은 유지하지만 내부를 R5 크기/기존 레이어 재생으로 교체했다.
각 주사위의 실제 확정 Sprite를 받아 사용하므로 원래24방향의 점 배열을 모두 유지한다.
ShowPressing의 normalizedProgress를 약.80초 동안0→1로 전달한다.

원본 전달 패키지의 Examples~/Absorb_o01_24Frames: 윗면4 예시의 실제24개 PNG 연속 프레임.
실제 런타임은 해당 예시를 모든 주사위에 복사하지 않고,확정된 원래 Sprite로 연속 렌더링한다.
원본 전달 패키지의 Preview 자료는 오프라인 검토용이다.

이전 사양 유지
예고는 칸을 거의 채우는 희미한 면 하나만 보인다.
일치한 주사위는 실제 호스트가 소비하고 통행 가능한 발판으로 바꾼다. 불일치는 그대로 남는다.
코스트·턴·판정·Undo·Reset은 실제 호스트가 소유한다. 연출 콜백이 게임 판정을 확정하지 않는다.
R3의 축소/페이드 누름 코드와 병행 재생하지 않는다.

faces_manifest.json revision5의6개 면/4개 목표 상태/4개 레이어를 새 빌더로 생성한다.
clip shader도 가져오고 새 DiceGoalPlatformArt.asset을 다시 생성해 연결해야 한다.
현재 저장소에서는 Editor의 Build 메서드로 두 생성 asset을 갱신한다.
실제 Unity 컴파일·셰이더 임포트·EditMode/PlayMode 결과는 작업 보고를 따른다.
