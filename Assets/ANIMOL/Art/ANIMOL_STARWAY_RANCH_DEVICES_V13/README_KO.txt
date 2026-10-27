ANIMOL 별길 목장 장치 V13 R4 / installationRevision 4

병아리가 둥지에 귀환할 때 짧게 움츠림→폴짝 뛰기→착지→날개를 접고 앉기→귀환 완료 표시를 추가했다.
남/동/북/서 네 방향 원화를 별도로 제작했다. 8타임프레임/10fps, 0.8초다.
귀환 후에는 병아리가 둥지 안에 앉아 있는 정지 상태로 남는다. 살아 있는 병아리를 다시 추종 상태로 바꾸지 않는다.

373 runtime PNG / 170 animation definitions. 기존325 PNG는 모두 동일 바이트로 보존했다.
새 독립 병아리24프레임 + 둥지 합성24프레임 =48 PNG, 새12클립이다.
nest/arrive 및 nest/settled는 정면 귀환 연출의 호환 별칭으로 갱신했다.
신규 연결에는 nest_return/<direction>/arrive 및 occupied와 RanchChickNestReturnVisualR4를 사용한다.

R3의 가로·세로 수문/물 방향과 R2 양 규칙을 포함한다. 기존 R3 연출은 Preview/R3_FLOW_PREVIEW.gif로 보존했다.
최신 명령: UNITY_APPLY_CLI_KO.txt. 귀환 세부 규칙: UPDATE_R4_CHICK_RETURN_KO.txt.
원화·최종 생성 프롬프트: Authoring~/native 및 generation_prompts_r4.json (built-in imagegen).
픽셀 등록/앞 테두리 가림: Authoring~/R4_CHICK_REGISTRATION.json.

실제 귀환 판정/도착 경로/추종 해제/저장/Undo는 원 프로젝트에서 기존 성공 이벤트에 연결해야 한다.
이 패키지는 표현과 연결용 소스를 제공한다. Unity 컴파일/NUnit/프로젝트 연결/플레이 검증은 NOT_RUN이다.
