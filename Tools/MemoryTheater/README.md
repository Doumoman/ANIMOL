# 기억 극장 검수 도구

프로젝트 루트에서 연결 Unity Editor를 사용한다. import_pack.py는 최초 복사용이며 적용된 프로젝트에서 다시 실행하지 않는다. 현재 산출물 재생성은 아래 Unity API 빌더를 사용한다. 현재 씬이 미저장 상태면 저장하고 실행한다.

```powershell
unity command run_script --file Tools/MemoryTheater/refresh.cs --timeout_ms 60000 --format json
unity command run_script --file Tools/MemoryTheater/operations.cs --entry TheaterOperations.Build --timeout_ms 60000 --format json
unity command run_script --file Tools/MemoryTheater/operations.cs --entry TheaterOperations.Validate --timeout_ms 60000 --format json
unity command eval_file --file Tools/DeviceArt/test_setup.cs --format json
unity command run_tests --mode editor --async_tests true --format json
unity command test_status --format json
unity command run_tests --mode playmode --async_tests true --format json
unity command test_status --format json
unity command run_script --file Tools/MemoryTheater/finalize.cs --timeout_ms 60000 --format json
```

각 테스트가 completed가 된 뒤 다음 명령을 실행한다. save_result.py로 최종 결과를 Docs/Validation/MemoryTheater에 저장할 수 있다. Capture는 씬을 일시 변경해 렌더하고 저장하지 않은 상태를 다시 열어 복원한다. atlas_qa.cs는 대표 자산을 임시 패킹하고 snapshot/PNG를 Docs에 보존한 뒤 Atlas를 활성 Assets에서 제거하고 Sprite 원본을 재임포트한다. Runtime 바인딩 복구는 실제 PlayMode texture 검사와 Game View로 확인해야 한다.

audit.py는 원본 해시·기존 바이트·팔레트/알파·타일 경계·GUID를 검사한다. 현재 검사 기준은 최초 적용 HEAD/사용자 설정 바이트다. 오래된 baseline을 새 작업의 기준으로 덮어쓰지 않는다. stage_changes.py는 최초 이 작업의 명시적인 산출물 스테이징 도구이며 일반 일괄 커밋 도구가 아니다.
