# 별길 목장 별도 최단 경로 계산

기본 경로는 Unity의 `PuzzleDeviceSession`, 상태 체크포인트, 장치 규칙, 하한, A* / 0-1 BFS 소스를 링크하는 .NET 10 콘솔 도구다. 제한된 목장 규칙에는 별도의 작은 상태 표현과 빠른 전이 구현도 제공한다. 최종 증거에는 모든 후속 상태를 원래 판정 코드로 생성하는 `authority` 모드를 사용할 수 있다. Unity Editor와 실제 게임 진행·저장은 별개로 유지한다.

`Adapters.cs`는 정수 좌표, Unity에서 검증·내보낸 정적 통행/장식 점유, Editor 업데이트 호출만 제공한다. 현재 지원 범위는 Starway의 Chick / MetalCrate / WoodCrate / StarSocket / Wind / Bounce / Conveyor이며, Echo 규칙과 레일은 거절한다. Unity 프로젝트 전체나 다른 테마의 대체 실행기가 아니다.

## 실행

작업 폴더는 저장소 루트다. Unity Edit Mode에서 `Export.cs.txt`의 본문을 Unity MCP `execute_code`로 실행한다. 원본을 저장하거나 수정하지 않으며, 현재 맵 검증·미저장 변경 검사 후 `Temp/exact_search_export.json`과 공개 API 전이 7,000개의 `Temp/exact_search_fixtures.json`을 만든다.

```powershell
dotnet build Tools/Puzzle/ExactSearch/ExactPuzzleSearch.csproj -c Release -o Temp/ExactSearchBuild
dotnet Temp/ExactSearchBuild/ExactPuzzleSearch.dll Temp/exact_search_export.json verify Temp/exact_search_fixtures.json Temp/exact_search_verified.json
dotnet Temp/ExactSearchBuild/ExactPuzzleSearch.dll Temp/exact_search_export.json SW02 Temp/exact_SW02.json 8000000 Temp/exact_search_verified.json
```

검증과 탐색은 같은 빌드·입력을 사용해야 한다. 검색 시작 전 검증 보고서의 입력·소스 해시를 대조한다. 비교 항목은 현재 상태 키와 비용, 네 방향 및 무료 바람의 수락·결과 키·비용·승리·실패다. 탐색 결과에는 컴파일 당시 포함한 16개 C# 소스의 SHA-256, 입력 및 검증 보고서 해시를 남긴다. 출력 경로는 ↑ → ↓ ←이며 ○는 무료 바람 대기다. 기본 검색의 방문 키에는 무손실 문자열 압축을 적용한다.

상태 상한은 검증 보고서 앞의 인수다. 추가 관리 메모리 예산은 기본 3,072 MiB이며, 마지막에 선택적으로 MiB 값을 붙여 조절할 수 있다. 실제 적용한 두 상한도 결과에 남긴다. `limit`과 `cancelled`는 미확정이다. `unreachable`은 대기열 소진일 때만 나오며 `solved`는 A* / 0-1 BFS 순서로 클리어를 꺼냈을 때만 나온다. 여러 프로세스를 실행하면 총 메모리 예산도 그만큼 늘어나므로 실제 사용량을 확인한다.

기록 규칙 5부터 이동 이벤트가 없는 풍차 단계 변경 뒤의 즉시 무료 바람을 방향 입력에 포함한다. 실제 컨트롤러는 이 경우 프레임 대기 없이 ContinueWind를 호출하므로 다음 입력으로 바람을 가로챌 수 없다. 이동 애니메이션 중 예약 입력과 그 뒤의 선택적 바람 대기는 유지한다. SW19의 기존 50입력 기록은 이 구분을 놓친 오류이며, 재계산한 최단값은 54다. [수정 경로와 실제 컨트롤러 재생](../../../Docs/STARWAY_SHORTEST_PATHS.md#sw19-바람-입력-타이밍-정정).

## 제한된 목장 상태

`compact` / `authority`는 63칸 이하, 병아리 하나(지연 1), 둥지 하나, 동일 금속 상자, 양 최대 4마리, 큰 풀 최대 10칸, 방울 최대 3개인 보드만 받는다. 바람·연못·다른 장치·별도 목표·비용 제한 등은 거절한다. 상자와 양 위치는 비트 집합이며, 플레이어·병아리·지연 경로·수집·소비 상태를 정수로 보관한다. 정렬 전용 ID와 표시 방향을 제외하는 기존 규칙 버전 4의 상태 동등성을 유지한다.

```powershell
dotnet Temp/ExactSearchBuild/ExactPuzzleSearch.dll Temp/exact_search_export.json SW17 Temp/exact_SW17.json 10000000 Temp/exact_search_verified.json 2048 authority
```

`verify`는 기존 전이 7,000개 외에 SW02/SW17의 200,000개 전이와 복원된 판정 세션을 대조한다. 작은 둥지·상자·양·풀 보드에서는 원래 판정의 완전 BFS, 빠른 A*, 판정 코드로 확장하는 A*가 같은 최단값을 내는지 검사한다. 상한은 미확정으로 남기고 원본 변경은 거절하는지도 확인한다.

`authority`는 꺼낸 각 상태를 판정 세션으로 복원한 뒤 네 방향을 모두 원래 `SearchStep`으로 확장한다. 빠른 전이 구현과도 모든 결과를 비교한다. 동등 상태에서 낮은 비용을 찾으면 새 노드를 만들고 이전 대기 노드를 무시하므로, 기존 하한이 일부 상태에서 불연속이어도 다시 열 수 있다. 최종 경로는 Unity 공개 API로 추가 재생한다.

## 필요한 양과 풀의 하한

모든 방울을 모아야 하는 보드에서 아직 남은 풀 위의 방울은 양이 그 풀을 먹은 뒤에만 수집할 수 있다. 양은 풀을 먹으면 사라지므로 서로 다른 필요한 풀에는 서로 다른 양이 필요하다. 정적 바닥과 밀 때 설 칸만 남긴 완화 그래프에서 각 양의 최소 밀기 수를 구하고, 서로 다른 양을 배정하는 최솟값을 사용한다. 배정 자체가 불가능한 분기만 완주 불가로 제외한다. 일부 방울만 필요하면 이 조건을 적용하지 않는다.

이 범위에서는 양·상자 밀기 때 플레이어가 제자리에 있고 병아리 경로도 진행하지 않는다. 기존 이동 하한은 이동 입력 수의 하한이고, 새 양 하한은 별도의 제자리 밀기 수의 하한이므로 두 값을 더해도 과대평가하지 않는다. `unreachable` 결과가 나오면 이 필요조건으로 제외한 분기를 포함한 탐색 소진임을 상세 문장에 표시한다.

## Unity로 결과 반영

콘솔은 `StarwayShortestPaths.json`을 자동 수정하지 않는다. 반영할 결과의 맵 SHA-256과 현재 판정·하한·탐색 소스를 확인하고, Unity의 `PuzzleShortestPathRecord.Save`에 `Result`를 전달한다. 이 메서드가 맵 미저장 변경·원본 해시를 검사하며, 성공 경로는 공개 `Apply` / `AdvanceWind`로 다시 재생하여 클리어와 비용을 확인한다. 기존 확정 결과를 미확정 결과로 교체하지 않는다. 계속 입력이나 실제 플레이어 저장 플래그와 연결하지 않는다.

원본 맵·`.meta`·작성된 `verificationRoute`는 보존한다. 계산 도중 맵·규칙이 바뀌면 새로 내보내고 검증·탐색을 다시 실행한다.
