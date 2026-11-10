# 별길 목장 별도 최단 경로 계산

기본 경로는 Unity의 `PuzzleDeviceSession`, 상태 체크포인트, 장치 규칙, 하한, A* / 0-1 BFS 소스를 링크하는 .NET 10 콘솔 도구다. 제한된 목장 규칙에는 별도의 작은 상태 표현과 빠른 전이 구현도 제공한다. 최종 증거에는 모든 후속 상태를 원래 판정 코드로 생성하는 `authority` 모드를 사용할 수 있다. Unity Editor와 실제 게임 진행·저장은 별개로 유지한다.

`Adapters.cs`는 정수 좌표, Unity에서 검증·내보낸 정적 통행/장식 점유, Editor 업데이트 호출만 제공한다. Starway의 Chick / MetalCrate / WoodCrate / StarSocket / Wind / Bounce / Conveyor / OneWayGate / Brake를 지원한다. 잔영 극장은 아래의 검증된 내보내기와 별도 A* 진입점을 사용하며, 실제 레일 이동도 원래 `PuzzleScenario.RailCellsFor`와 `PuzzleDeviceSession` 소스를 실행한다. 레일 모양 검증은 Unity에서 끝내며 별도의 레일 물리를 구현하지 않는다.

## ET-20까지의 현재 잔영 극장

현재 ET-20 작성본의 기준 커밋은 `8f62aec34`다. `Docs/Validation/EchoThrough20/inventory.json`은 현존 20개 맵의 원본·GUID·메타 해시와 이전 18개 맵의 검색 증거 경로를 고정한다. ET-06은 복원하지 않는다.

```powershell
unity command run_script --file Tools/Puzzle/ExportEchoSearch.cs --entry ExportEchoSearch.StartThrough20 --format json
# Temp/EchoThrough20Export/status.json이 completed가 된 뒤 실행한다.
dotnet build Tools/Puzzle/ExactSearch/ExactPuzzleSearch.csproj -c Release -o Temp/EchoThrough20Build
dotnet Temp/EchoThrough20Build/ExactPuzzleSearch.dll Temp/EchoThrough20Export/input.json verify Temp/EchoThrough20Export/fixtures.json Docs/Validation/EchoThrough20/transition_verification.json
dotnet Temp/EchoThrough20Build/ExactPuzzleSearch.dll Temp/EchoThrough20Export/input.json ET-19 Docs/Validation/EchoThrough20/Search/ET-19.json 20000000 Docs/Validation/EchoThrough20/transition_verification.json 4096
dotnet Temp/EchoThrough20Build/ExactPuzzleSearch.dll Temp/EchoThrough20Export/input.json ET-20 Docs/Validation/EchoThrough20/Search/ET-20.json 20000000 Docs/Validation/EchoThrough20/transition_verification.json 4096
unity command run_script --file Tools/Puzzle/VerifyEchoThrough18.cs --entry VerifyEchoThrough18.StartThrough20 --format json
# Docs/Validation/EchoThrough20/replay_status.json이 completed가 된 뒤 실행한다.
python Tools/Reports/build_echo_through18_report.py --through 20
```

내보내기는 새 두 맵만 처리하며 800개 Unity 상태·3,200개 공개 입력 전이를 대조한다. 검증기는 이전 소스 해시도 현재 판정 코드와 대조하고, 20개 맵 전체의 확정 경로를 Unity에서 다시 재생한다. ET-19는 최단 8입력(`NNNNNNNN`)이다. ET-20은 현재 책장·발판 연결과 회전 설정, 목표 방울 4개를 기준으로 **최단 54입력**이다. ET-19는 위로만 이동해 별 7개와 출구에 도달할 수 있으므로 맵 검토 시 이 통로를 확인한다. 계산 때문에 원본이나 `verificationRoute`를 바꾸지 않는다.

ET-20은 실제 `PuzzlePreview`의 Play Mode에서도 검증했다. `VerifyEcho20Controller.Start`는 초기 상태의 ET-20 미리보기에서 `PuzzleSwipeInput` 수신부로 입력하고, 실제 애니메이션 종료를 기다린 뒤 매 입력의 판정 상태와 플레이어 표시 위치를 비교한다. `Docs/Validation/EchoThrough20/et20_controller_replay.json`에 54입력·방울 4개·승리 증거를 보존한다. 미리보기 종료 시 원래 열린 씬을 복원한다.

## ET-18까지의 계산과 ET-15 재탐색

사용자가 작성한 ET-12~18은 `6c46f4dbb`로 먼저 커밋했다. 이번 범위는 ET-00~18의 현존 18개 파일이며 ET-06은 만들지 않는다. 입력 원본·GUID·메타 해시는 `Docs/Validation/EchoThrough18/inventory.json`에 고정한다.

```powershell
unity command run_script --file Tools/Puzzle/ExportEchoSearch.cs --entry ExportEchoSearch.Start --format json
# Temp/EchoThrough18Export/status.json이 completed가 된 뒤 실행한다.
dotnet build Tools/Puzzle/ExactSearch/ExactPuzzleSearch.csproj -c Release -o Temp/EchoThrough18FinalBuild
dotnet Temp/EchoThrough18FinalBuild/ExactPuzzleSearch.dll Temp/EchoThrough18Export/input.json verify Temp/EchoThrough18Export/fixtures.json Docs/Validation/EchoThrough18/transition_verification_final.json
dotnet Temp/EchoThrough18FinalBuild/ExactPuzzleSearch.dll Temp/EchoThrough18Export/input.json ET-18 Docs/Validation/EchoThrough18/Search/ET-18.json 10000000 Docs/Validation/EchoThrough18/transition_verification_final.json 3072
```

현재 Echo 지원 장치는 셔터·카트·도미노·유리·주사위·겁쟁이 인형·돌진 석상·책장이다. 무료 바람이나 다른 장치가 있으면 이 진입점으로 계산하지 않는다. 각 간선은 현재 런타임의 `SearchStep`이고, 별도의 빠른 이동 규칙으로 대체하지 않는다. 카트 완화 하한과 셔터 홀짝 키는 기존 Unity 탐색기와 같은 정의를 사용한다.

일부 완료된 검색은 초기 빌드다. 해당 결과가 기록한 `sourceHashes`와 일치하는 진입점·큐 소스는 `Docs/Validation/EchoThrough18/InitialBuildSources`에 보관한다. 현재 빌드는 돌진 중이 아닌 석상의 과거 timer와 유리 상자의 지난 이동 방향을 키에서만 정규화한다. 이 값들은 다음 실제 판정에서 읽히지 않거나 새 입력/감지로 덮어써진다. 실제 세션과 체크포인트는 유지한다. 내보내기는 Unity의 네 방향 후속 상태를 비교하고, `verify`는 같은 비교를 실제 링크 소스로 반복한다.

석상·유리·주사위만 있는 보드에는 영구 정지 석상의 연결 영역 검사도 적용한다. 지원 바닥이 끝나거나 다른 영구 정지 석상에 막혀 앞으로 갈 수 없는 석상만 고정 장애물로 남긴다. 나머지 물체를 모두 제거한 완화 보드에서도 출구나 목표 별을 모을 수 없는 경우만 제외한다. 작은 보드의 독립 완전 BFS로 거짓 차단을 검사한다.

ET-15의 재탐색은 `EchoDiceLowerBound`를 추가로 사용한다. 이동 주사위 하나와 발판 하나가 있는 석상·유리·주사위 보드에서 석상과 유리만 지운 완화 그래프를 만든다. 플레이어 위치·주사위 위치·24가지 면 방향·별 수집·발판 개방을 유지하고, 유리 밀기에 대응하는 제자리 간선을 허용한다. 모든 실제 간선의 투영을 검사하며, 이 그래프의 정확한 역방향 BFS 거리를 하한으로 사용한다. ET-15는 기존 거리 하한 16에서 주사위 하한 36으로 강화했고 **최단 54입력**을 확정했다. 물리 전이는 계속 원래 `SearchStep`이다.

모든 결과가 준비된 뒤 다음을 실행한다. 검증기는 사용된 각 검색 빌드의 소스 해시와 원본을 확인하고, 확정 경로를 실제 Unity 공개 API로 다시 재생한다. 보고서 생성기는 이 검증이 끝나야 카탈로그를 갱신한다.

```powershell
unity command run_script --file Tools/Puzzle/VerifyEchoThrough18.cs --entry VerifyEchoThrough18.Start --format json
# Docs/Validation/EchoThrough18/replay_status.json이 completed가 된 뒤 실행한다.
python Tools/Reports/build_echo_through18_report.py
```

내보낸 입력과 전이 표본은 같은 증거 폴더의 `input.json.gz` / `fixtures.json.gz`로 보존한다. 재검증하려면 `Temp/EchoThrough18Export`에 JSON으로 풀고 같은 빌드로 `verify`부터 실행한다. 상한이나 취소 결과를 최단·도달 불가로 승격하지 않는다. MapData·작성된 verificationRoute·GUID·진행 기록은 검색 도구가 저장하지 않는다.

## 이전 계산 이력 (2026-11-08)

규칙 9에서는 양·상자의 반사각 안쪽 진입을 허용하고, 플레이어는 안쪽 진입 뒤 반사 방향으로 자동 이동한다. 플레이어의 바깥면 진입·이탈은 차단한다. 반사 이동과 바람은 추가 입력 비용을 쓰지 않는다.

현재 사용자 수정 배치의 SW06(화면상 7번)은 **최단 31입력**, SW19(화면상 20번)는 **최단 60입력**이다. SW06은 (3,5) 바닥 추가와 (2,6) 바닥 제거를 반영한 소스 `5dd2f88f…`로 다시 내보내 검증했다. 두 맵 모두 실제 컨트롤러의 예약 입력과 애니메이션으로 잔여 0 클리어를 확인했다. [7번 최신 경로와 증거](../../../Docs/STARWAY_SHORTEST_PATHS.md#7번-맵-바닥-보완-후-재탐색)를 따른다. 이전 SW06 도달 불가와 아래 규칙 5·6의 결과는 수정 전 배치·판정의 이력이다. 사용자 맵 수정분은 계산 결과 커밋에 포함하지 않고 별도로 보존한다.

## 2026-11-10 정본과 실행

맵 원본 커밋 `a2f825e09`를 먼저 만든 뒤 다시 계산한다. 이번 입력은 `Docs/Validation/CanonicalMaps/inventory.json`의 별길 목장 21개 파일이며, 파일 ID·GUID·SHA-256이 모두 일치해야 내보낸다. SW20은 없고 SW21이 별도로 존재한다. 현재 SW03/SW05의 GUID 교환과 새 SW05 배치를 원본 그대로 따른다. 실제 노트북 화면은 이름으로 찾는 SW00~SW19의 20개 맵을 사용한다.

Unity Edit Mode에서 본문을 임시 `.cs`에 복사한 후 실행한다. MapData와 `verificationRoute`를 저장하거나 수정하지 않는다. 각 맵 200개 표본 상태와 네 방향/무료 바람의 공개 API 전이 21,000개를 내보낸다.

```powershell
Copy-Item Tools/Puzzle/ExactSearch/Export.cs.txt Temp/CanonicalExport.cs
unity command eval_file --file Temp/CanonicalExport.cs --format json

dotnet build Tools/Puzzle/ExactSearch/ExactPuzzleSearch.csproj -c Release -o Temp/CanonicalSearchBuild

dotnet Temp/CanonicalSearchBuild/ExactPuzzleSearch.dll Docs/Validation/CanonicalMapPaths/Starway/input.json verify Docs/Validation/CanonicalMapPaths/Starway/fixtures.json Docs/Validation/CanonicalMapPaths/Starway/verification.json

dotnet Temp/CanonicalSearchBuild/ExactPuzzleSearch.dll Docs/Validation/CanonicalMapPaths/Starway/input.json SW05 Temp/SW05.json 5000000 Docs/Validation/CanonicalMapPaths/Starway/verification.json 2048
```

문은 진입 후 추가 입력 없이 한 칸 더 이동하므로 완화 그래프에서도 문 원점의 모든 방향을 무료로 허용한다. 브레이크는 강제 이동을 멈추므로 완화 그래프에서 통행 제한을 제거한다. 두 장치를 포함해도 실제 이동은 원래 판정 코드만 사용한다.

잔영 극장은 `Tools/Puzzle/EchoTheaterShortestPaths.cs`를 `run_script`의 `EchoTheaterShortestPaths.Start`로 실행한다. ET-11까지 현존하는 11개 파일만 선택하며 삭제된 ET-06이나 ET-12 이후 맵을 생성·변경하지 않는다. 책장은 통행을 제한하는 장치이므로 하한 계산에서 제거할 수 있다. 카트 완화 전이는 실제 장치의 ID 정렬 순서를 따른다.

모든 결과가 준비되면 `Tools/Puzzle/ImportCanonicalMapPaths.cs`의 `ImportCanonicalMapPaths.Import`를 실행한다. 두 카탈로그를 쓰기 전에 모든 정본 GUID·원본 해시·컴파일 소스 해시를 검사하고, 확정 경로 전체를 Unity의 실제 `ReplayRuntimeInput`/`Apply`/`AdvanceWind`로 재생한다. 이전 카탈로그는 `Docs/Validation/CanonicalMapPaths/PriorCatalogs/`에 보관한다. 미확정 결과는 미확정 상태로만 기록한다. 이 도구는 SW21도 별도 증거로 수록하며 화면 스테이지나 클리어 키를 추가하지 않는다.

검증과 탐색은 같은 빌드·입력을 사용해야 한다. 검색 시작 전 검증 보고서의 입력·소스 해시를 대조한다. 비교 항목은 현재 상태 키와 비용, 네 방향 및 무료 바람의 수락·결과 키·비용·승리·실패다. 탐색 결과에는 컴파일 당시 포함한 17개 C# 소스의 SHA-256, 입력 및 검증 보고서 해시를 남긴다. 출력 경로는 ↑ → ↓ ←이며 ○는 무료 바람 대기다. 기본 검색의 방문 키에는 무손실 문자열 압축을 적용한다.

커밋한 전이 증거는 `Starway/fixtures.json.gz`로 압축 보관한다. 기존 증거를 재검증하려면 같은 폴더의 `fixtures.json`으로 풀어 위 `verify`에 전달한다. 풀어서 얻은 바이트의 SHA-256은 검증 보고서의 `fixtureSha256`과 같아야 한다.

상태 상한은 검증 보고서 앞의 인수다. 추가 관리 메모리 예산은 기본 3,072 MiB이며, 마지막에 선택적으로 MiB 값을 붙여 조절할 수 있다. 실제 적용한 두 상한도 결과에 남긴다. `limit`과 `cancelled`는 미확정이다. `unreachable`은 대기열 소진일 때만 나오며 `solved`는 A* / 0-1 BFS 순서로 클리어를 꺼냈을 때만 나온다. 여러 프로세스를 실행하면 총 메모리 예산도 그만큼 늘어나므로 실제 사용량을 확인한다.

기록 규칙 5부터 이동 이벤트가 없는 풍차 단계 변경 뒤의 즉시 무료 바람을 방향 입력에 포함한다. 실제 컨트롤러는 이 경우 프레임 대기 없이 ContinueWind를 호출하므로 다음 입력으로 바람을 가로챌 수 없다. 이동 애니메이션 중 예약 입력과 그 뒤의 선택적 바람 대기는 유지한다. SW19의 기존 50입력 기록은 이 구분을 놓친 오류이며, 재계산한 최단값은 54다. [수정 경로와 실제 컨트롤러 재생](../../../Docs/STARWAY_SHORTEST_PATHS.md#sw19-바람-입력-타이밍-정정).

규칙 6은 반사판 칸으로 양·나무 박스·금속 박스가 진입하는 것을 막는다. 직접 밀기뿐 아니라 바람으로 운반될 때도 반사판 바로 앞에서 멈춘다. 캐릭터와 바람의 반사는 유지한다. SW19의 규칙 5 당시 54입력은 현재 규칙에 사용할 수 없다. 점유 범위를 줄인 현재 SW19도 .NET A*와 Unity 0-1 BFS가 같은 500,454개 상태를 소진하여 도달 불가를 확인했다. [최신 재검증](../../../Docs/STARWAY_SHORTEST_PATHS.md#반사판-진입-금지와-sw19-재검증).

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

콘솔은 `StarwayShortestPaths.json`을 자동 수정하지 않는다. 반영할 결과의 맵 SHA-256과 현재 판정·하한·탐색 소스를 확인하고, SW00~SW19의 개별 에디터 기록은 Unity의 `PuzzleShortestPathRecord.Save`에 `Result`를 전달한다. 이번 21개 정본 전체 갱신에는 위의 검증된 Import 도구를 사용한다. 이 메서드가 맵 미저장 변경·원본 해시를 검사하며, 성공 경로는 공개 `Apply` / `AdvanceWind`로 다시 재생하여 클리어와 비용을 확인한다. 기존 확정 결과를 미확정 결과로 교체하지 않는다. 계속 입력이나 실제 플레이어 저장 플래그와 연결하지 않는다.

원본 맵·`.meta`·작성된 `verificationRoute`는 보존한다. 계산 도중 맵·규칙이 바뀌면 새로 내보내고 검증·탐색을 다시 실행한다.
