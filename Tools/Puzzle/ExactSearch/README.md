# 별길 목장 별도 최단 경로 계산

Unity의 `PuzzleDeviceSession`, 상태 체크포인트, 장치 규칙, 하한, A* / 0-1 BFS 소스를 그대로 링크하는 .NET 10 콘솔 도구다. Unity Editor는 실행을 유지하며 실제 게임 진행·저장·보상은 건드리지 않는다. 소스 복사본으로 판정을 다시 구현하지 않는다.

`Adapters.cs`는 정수 좌표, Unity에서 검증·내보낸 정적 통행/장식 점유, Editor 업데이트 호출만 제공한다. 현재 지원 범위는 Starway의 Chick / MetalCrate / WoodCrate / StarSocket / Wind / Bounce / Conveyor이며, Echo 규칙과 레일은 거절한다. Unity 프로젝트 전체나 다른 테마의 대체 실행기가 아니다.

## 실행

작업 폴더는 저장소 루트다. Unity Edit Mode에서 `Export.cs.txt`의 본문을 Unity MCP `execute_code`로 실행한다. 원본을 저장하거나 수정하지 않으며, 현재 맵 검증·미저장 변경 검사 후 `Temp/exact_search_export.json`과 공개 API 전이 7,000개의 `Temp/exact_search_fixtures.json`을 만든다.

```powershell
dotnet build Tools/Puzzle/ExactSearch/ExactPuzzleSearch.csproj -c Release -o Temp/ExactSearchBuild
dotnet Temp/ExactSearchBuild/ExactPuzzleSearch.dll Temp/exact_search_export.json verify Temp/exact_search_fixtures.json Temp/exact_search_verified.json
dotnet Temp/ExactSearchBuild/ExactPuzzleSearch.dll Temp/exact_search_export.json SW02 Temp/exact_SW02.json 8000000 Temp/exact_search_verified.json
```

검증과 탐색은 같은 빌드·입력을 사용해야 한다. 검색 시작 전 검증 보고서의 입력·소스 해시를 대조한다. 비교 항목은 현재 상태 키와 비용, 네 방향 및 무료 바람의 수락·결과 키·비용·승리·실패다. 탐색 결과에는 컴파일 당시 포함한 13개 C# 소스의 SHA-256, 입력 및 검증 보고서 해시를 남긴다. 방문 상태 키에는 반복 문자열을 치환하는 무손실 압축을 적용하며, 원래 키의 동등성은 유지한다.

상태 상한은 검증 보고서 앞의 인수다. 추가 관리 메모리 예산은 기본 3,072 MiB이며, 마지막에 선택적으로 MiB 값을 붙여 조절할 수 있다. 실제 적용한 두 상한도 결과에 남긴다. `limit`과 `cancelled`는 미확정이다. `unreachable`은 대기열 소진일 때만 나오며 `solved`는 A* / 0-1 BFS 순서로 클리어를 꺼냈을 때만 나온다. 여러 프로세스를 실행하면 총 메모리 예산도 그만큼 늘어나므로 실제 사용량을 확인한다.

## Unity로 결과 반영

콘솔은 `StarwayShortestPaths.json`을 자동 수정하지 않는다. 반영할 결과의 맵 SHA-256과 현재 판정·하한·탐색 소스를 확인하고, Unity의 `PuzzleShortestPathRecord.Save`에 `Result`를 전달한다. 이 메서드가 맵 미저장 변경·원본 해시를 검사하며, 성공 경로는 공개 `Apply` / `AdvanceWind`로 다시 재생하여 클리어와 비용을 확인한다. 기존 확정 결과를 미확정 결과로 교체하지 않는다. 계속 입력이나 실제 플레이어 저장 플래그와 연결하지 않는다.

원본 맵·`.meta`·작성된 `verificationRoute`는 보존한다. 계산 도중 맵·규칙이 바뀌면 새로 내보내고 검증·탐색을 다시 실행한다.
