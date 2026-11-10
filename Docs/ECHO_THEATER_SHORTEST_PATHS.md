# 잔영 극장 현재 정본의 최단 입력 경로

?? ? ?? ?? ?? ?? ? ET-10? ?? 43?? ??? 35?? ???? ???. ? ??? ???? 1,000,000? ?? ??? ???? ????? ????. ?? ??? ?? ??? ??? ??? ???. [??? ??](Validation/StatueBeforeCart/ET10_revalidation.json)

2026-11-10 기준 **11개 맵 중 최단 확정 11개, 경로 없음 0개, 미확정 0개**다. 현재 사용자가 작성한 맵을 정본으로 먼저 커밋한 `a2f825e09` 이후에 계산했다. 계산 때문에 MapData·작성된 `verificationRoute`·GUID·진행 기록을 수정하지 않았다.

[정본 파일·GUID·이전 이름 대조](Validation/CanonicalMaps/inventory.json) · [실제 화면/MapSet 연결](Validation/CanonicalMapPaths/stage_aliases.json) · [Unity 전체 경로 재생](Validation/CanonicalMapPaths/native_replay.json)

## 파일 이름과 화면 번호

ET-11까지 현존하는 ET-00~05, ET-07~11의 11개 파일만 계산했다. ET-06 삭제를 그대로 유지했고, ET-12~35의 기존 제작용 파일은 이번 작성 범위에 넣지 않았다. 화면 번호는 EchoTheaterMapSet의 실제 GUID 참조를 따른다.

| 현재 파일 | 동일 GUID의 이전 파일 | 현재 화면 연결 |
|---|---|---|
| ET-00 | ET-02 | ET-02 |
| ET-01 | ET-06 | ET-06 |
| ET-02 | ET-07 | ET-07 |
| ET-07 | ET-01 | ET-01 |

위 이름 이동과 현재 배치를 정본으로 삼았다. 과거 ET-02 파일이 없던 시점의 보고서를 현재 배치의 증거로 사용하지 않는다. 현재 책장의 회전 영역·방향·충돌, 돌진 석상의 유리벽 직전 정지, 셔터의 초기 반대 위상, 카트의 논리 처리 순서를 모두 실제 판정으로 반영했다.

## 현재 결과

| 파일 ID | 화면 스테이지 | 결과 | 최단 입력 | 발견 상태 |
|---|---|---|---:|---:|
| ET-00 | ET-02 | 최단 확정 | 31 | 64 |
| ET-01 | ET-06 | 최단 확정 | 19 | 52 |
| ET-02 | ET-07 | 최단 확정 | 29 | 3,167 |
| ET-03 | ET-03 | 최단 확정 | 49 | 48,761 |
| ET-04 | ET-04 | 최단 확정 | 33 | 37,414 |
| ET-05 | ET-05 | 최단 확정 | 26 | 3,081 |
| ET-07 | ET-01 | 최단 확정 | 42 | 252,714 |
| ET-08 | ET-08 | 최단 확정 | 43 | 1,191 |
| ET-09 | ET-09 | 최단 확정 | 40 | 400,205 |
| ET-10 | ET-10 | 최단 확정 | 43 | 604,257 |
| ET-11 | ET-11 | 최단 확정 | 33 | 2,191 |

유효한 방향 입력 하나의 비용은 1이다. 제자리에서 장치를 작동하거나 물체를 미는 입력도 1이며, 자동 바람 대기 `○`는 비용 0이다. N/↑=위, E/→=오른쪽, S/↓=아래, W/←=왼쪽이다. 경로를 줄이기 위해 제자리 입력이나 반복 방향을 생략하지 않는다.

## 배치 변경 대조

아래 칸 수는 직렬화된 terrain 항목 수이며 실제 통행 가능 칸 수와 다르다. 이전 파일은 이전 HEAD에서 같은 GUID를 가진 파일이다. 이름 이동과 배치 변경을 분리해서 비교한다.

| 현재 파일 | 이전 이름 | terrain 항목 이전→현재 | 목표 별 이전→현재 | 현재 장치 수 |
|---|---|---:|---:|---:|
| ET-00 | ET-02 | 131→32 | 3→3 | 6 |
| ET-01 | ET-06 | 133→24 | 3→3 | 3 |
| ET-02 | ET-07 | 140→29 | 3→3 | 6 |
| ET-03 | ET-03 | 127→38 | 3→2 | 7 |
| ET-04 | ET-04 | 136→33 | 3→2 | 7 |
| ET-05 | ET-05 | 112→42 | 3→2 | 8 |
| ET-07 | ET-01 | 109→40 | 3→2 | 10 |
| ET-08 | ET-08 | 130→33 | 3→2 | 10 |
| ET-09 | ET-09 | 139→38 | 3→3 | 11 |
| ET-10 | ET-10 | 118→39 | 3→2 | 10 |
| ET-11 | ET-11 | 137→44 | 3→3 | 9 |

## 경로

### ET-00

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-00.json) · 원본 SHA-256 `b6ea624e638223cb17b7ab6501a338281c5afd185e8d865d049fbeaf9a4b9264`

**31입력**

```text
→→↑→→↓→↑↑↑↑↓↓↓→→↑→→→↑↓↓→→↑↑↓↓→→
```

### ET-01

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-01.json) · 원본 SHA-256 `339d720cfd7d60cd37ba518e434cad61482bbc8f8f84afdaa2b020403bc92a6c`

**19입력**

```text
←←↓←→→↓→→→→↑↓↓←↑→→→
```

### ET-02

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-02.json) · 원본 SHA-256 `8de33d72ef5b43f7bfcbf0333b41022fb4af882504cef594130e6c00a6e19c42`

**29입력**

```text
→→→↑↑↑←←↓←←↑↓↓↑→→↑→→→→↓↓↑↑←↑↑
```

### ET-03

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-03.json) · 원본 SHA-256 `4be33c69e120a690733dba2f47ec5d765e9e3e063926bc25a94accdf5edff590`

**49입력**

```text
→→↓↑→↓←↑↑→↑←↑↓←↓↓↓←↓↓→→→→→↓→↑↑↑↓←↑→↑↑↑←↑→↑→↓→→↑↑↑
```

### ET-04

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-04.json) · 원본 SHA-256 `744ddcb7f4bed20df6fa3b54f29e20178ec79197cdfb033e1e9aa8a1c5b868e0`

**33입력**

```text
↑→↑→→↑↑↑↑↑←↓↓←←↓↑→→→↓↓↑→→↑←←→→↑↑→
```

### ET-05

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-05.json) · 원본 SHA-256 `60f35d7ba497f31b0d081c6da01e3bdc9f22158d1553126d7e603fca8e216c63`

**26입력**

```text
→→→↑←←↓←↑↑→↑↑↓↓→→↑↑↑↑↑↑↓→→
```

### ET-07

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-07.json) · 원본 SHA-256 `ffaca9eca2ce08572d1695cc3c9d50e457146379548633e1781f3d962d3228a4`

**42입력**

```text
→↑→→↓→↓↓←↑←←↓←↓↓→←←←↑↑↑→→→→↑↓↓↓→↓→→→→←↑↑↑↑
```

### ET-08

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-08.json) · 원본 SHA-256 `499eb71bae8acd26eb866156047052e82ae4a1f09966d015a1a1c105007fe2dd`

**43입력**

```text
←↑↑→→↓→→↑↑↑←←←→→↑↑↓→←←←↓↑→↓↓↓←←↓↑→→→→↑↑→→→↑
```

### ET-09

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-09.json) · 원본 SHA-256 `de694d5d59dfda9101c656b1553830fc6044dedc1cacc3d0e43c83ddc212a7c1`

**40입력**

```text
↑→→→↓↓←↓←↑↑←←↑↑↓↓→↓↓←→→→→↑↑→↑↑↑←↑↑→↓→→↑↑
```

### ET-10

[?? ?? ???](Validation/StatueBeforeCart/ET10_revalidation.json) ? ?? SHA-256 `15d26f9e35896a54bbc9582d0727814d1f9da6ab9d5c830f4c5b5de98ff8c63d`

**?? ?? ???**

?? ??? ?? ??? ? ??? ???? ???? ?????. ?? 43?? ??? 35?? ???? ????. 0-1 BFS? ?? ?? 1,000,000? ??? ???? ? ??? ???? ???? ???. ?? ??? ??? ????? `previousResult`? ????? ?? ??? ???? ???.

### ET-11

[입력별 재생 증거](Validation/CanonicalMapPaths/Echo/ET-11.json) · 원본 SHA-256 `b5d73b6263108ad0e04261d66df509c641179bf72c82251e5834f4c1ff5fde98`

**33입력**

```text
←↑↑↑→↑↑←←↑←←→↓↓←→↑↑↑↑↑→→↓↓→↓→→↑→↑
```

## 검증 범위

별길 목장은 실제 Unity에서 내보낸 4,200개 상태·21,000개 공개 API 전이를 소스 링크 탐색기와 대조했다. SW02/SW17의 작은 상태 표현은 원래 판정과 추가 200,000개 전이를 비교했고, 두 작은 보드의 완전 BFS와 A* 거리·원본 변경 거부·상한 미확정 처리를 검사했다. 최종 SW02/SW17 탐색은 모든 후속 상태를 원래 SearchStep으로 생성하고 빠른 전이와도 비교하는 authority 모드다.

잔영 극장은 Unity Editor의 실제 PuzzleDeviceSession에서 A*를 실행했다. 책장 등의 통행 제한을 제거하고 카트의 가능한 자동 밀기를 넓게 허용한 하한을 사용하며, 카트 순서는 원래 장치 ID 정렬과 같다. 모든 조사된 실제 전이에 하한의 일관성을 검사했고 셔터는 판정에 필요한 홀짝만 상태 키에 기록한다. 가장 작은 g+h의 클리어 상태를 꺼낸 경우에만 최단으로 확정한다. 상한이나 취소는 경로 없음으로 기록하지 않는다.

두 테마 모두 Unity에서 확정 경로 전체를 공개 판정 API로 다시 실행하여 목표 별·출구·승리·입력 비용을 확인했다. 원본과 .meta GUID, 미저장 상태, 계산에 사용한 엔진 소스 해시를 다시 검사한 뒤 두 경로 카탈로그만 갱신했다. Play Mode에서 모든 맵을 사람이 플레이하거나 모바일 GPU를 검증한 결과는 아니다. 구체적인 EditMode 결과와 실행하지 않은 항목은 [이번 검증 기록](Validation/CanonicalMapPaths/validation_summary.json)을 따른다.

이전 카탈로그는 [별길 목장](Validation/CanonicalMapPaths/PriorCatalogs/StarwayShortestPaths.json)과 [잔영 극장](Validation/CanonicalMapPaths/PriorCatalogs/EchoTheaterShortestPaths.json)에 보관했다. 아래는 당시 배치·규칙의 이력이며 현재 정답은 위 결과를 따른다.

<details>
<summary>이전 배치와 규칙의 검증 이력</summary>

# 잔영 극장 초반 맵 최단 정답 경로

원본 ET-00~ET-05 범위에서 현재 존재하는 다섯 맵의 최단 정답을 확정했다. 현재 Unity Editor에 로드된 맵과 실제 `PuzzleDeviceSession` 이동 판정으로 계산했으며, 모든 경로를 공개 `Apply` API로 다시 재생해 별 수집·출구 도달·승리를 확인했다.

화면 ET-02는 원본 ET-00을 사용한다. 원본 `ET-02_MapData.asset`와 메타는 현재 없으므로 옛 JSON을 대신 풀거나 새 맵을 만들지 않았다. 화면 ET-01은 원본 ET-01, 화면 ET-03~05는 같은 번호의 원본을 사용한다.

## 확정 결과

| 원본 맵 | 화면 스테이지 | 최단 입력 수 | 수집 / 목표 | 방문 상태 수 | 탐색 시간 |
|---|---|---:|---:|---:|---:|
| [ET-00](#et-00) | ET-02 | **31** | 3 / 3 | 64 | 0.13초 |
| [ET-01](#et-01) | ET-01 | **42** | 2 / 2 | 252,714 | 89.61초 |
| [ET-03](#et-03) | ET-03 | **49** | 2 / 2 | 48,761 | 12.19초 |
| [ET-04](#et-04) | ET-04 | **33** | 2 / 2 | 37,414 | 8.43초 |
| [ET-05](#et-05) | ET-05 | **26** | 2 / 2 | 3,081 | 0.91초 |

시간은 이번 실행의 측정값이다. 최단성은 시간이나 방문 수가 아니라 아래의 탐색 조건에 근거한다.

## 경로 읽는 법

`↑/N`은 위, `→/E`는 오른쪽, `↓/S`는 아래, `←/W`는 왼쪽이다. 화살표 하나가 비용 1인 유효 방향 입력 하나다. 물체를 밀거나 장치를 작동시켜 플레이어가 같은 칸에 남는 입력도 포함하므로, 반복 방향이나 제자리 입력을 생략하면 안 된다. 아래는 10입력씩 끊어 적었으며 마지막 줄만 더 짧을 수 있다.

## ET-00

화면 ET-02, **31입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#et-00) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/ET-00.json).

```text
01~10: → → ↑ → → ↓ → ↑ ↑ ↑
11~20: ↑ ↓ ↓ ↓ → → ↑ → → →
21~30: ↑ ↓ ↓ → → ↑ ↑ ↓ ↓ →
31~31: →
```

NESW 표기:

```text
EENEESENNNNSSSEENEEENSSEENNSSEE
```

## ET-01

화면 ET-01, **42입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#et-01) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/ET-01.json).

```text
01~10: → ↑ → → ↓ → ↓ ↓ ← ↑
11~20: ← ← ↓ ← ↓ ↓ → ← ← ←
21~30: ↑ ↑ ↑ → → → → ↑ ↓ ↓
31~40: ↓ → ↓ → → → → ← ↑ ↑
41~42: ↑ ↑
```

NESW 표기:

```text
ENEESESSWNWWSWSSEWWWNNNEEEENSSSESEEEEWNNNN
```

## ET-03

화면 ET-03, **49입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#et-03) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/ET-03.json).

```text
01~10: → → ↓ ↑ → ↓ ← ↑ ↑ →
11~20: ↑ ← ↑ ↓ ← ↓ ↓ ↓ ← ↓
21~30: ↓ → → → → → ↓ → ↑ ↑
31~40: ↑ ↓ ← ↑ → ↑ ↑ ↑ ← ↑
41~49: → ↑ → ↓ → → ↑ ↑ ↑
```

NESW 표기:

```text
EESNESWNNENWNSWSSSWSSEEEEESENNNSWNENNNWNENESEENNN
```

## ET-04

화면 ET-04, **33입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#et-04) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/ET-04.json).

```text
01~10: ↑ → ↑ → → ↑ ↑ ↑ ↑ ↑
11~20: ← ↓ ↓ ← ← ↓ ↑ → → →
21~30: ↓ ↓ ↑ → → ↑ ← ← → →
31~33: ↑ ↑ →
```

NESW 표기:

```text
NENEENNNNNWSSWWSNEEESSNEENWWEENNE
```

## ET-05

화면 ET-05, **26입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#et-05) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/ET-05.json).

```text
01~10: → → → ↑ ← ← ↓ ← ↑ ↑
11~20: → ↑ ↑ ↓ ↓ → → ↑ ↑ ↑
21~26: ↑ ↑ ↑ ↓ → →
```

NESW 표기:

```text
EEENWWSWNNENNSSEENNNNNNSEE
```

## 최단성을 확인한 방법

각 간선은 현재 런타임의 `PuzzleDeviceSession.SearchStep`으로 생성했다. 이 다섯 맵에는 무료 바람 이동이 없고, 받아들여진 입력은 모두 비용 1인지 검사했다. 셔터, 이동 발판, 도미노, 유리, 주사위, 겁쟁이 인형, 석상의 실제 상태와 추가 승리 조건을 유지했다. 석상은 유리를 부순 뒤 그 칸에 들어가지 않고 바로 앞에서 이번 돌진을 끝내는 현재 규칙을 사용한다.

탐색은 정확 A*다. 하한은 움직이는 장애물을 제거하고, 제자리 행동과 이동 발판의 가능한 밀림을 더 넓게 허용한 격자에서 필요한 별을 모아 출구까지 가는 최단 입력 수다. 각 발판을 순서대로 반영하고 중간에 얻는 별도 보존하며, 발판이 원래 입력 위치로 돌려보내는 경우까지 허용한다. 실제로 가능한 이동을 이 완화된 그래프가 포함하므로 실제 남은 비용을 초과하지 않는다. 지원 밖 장치가 있으면 하한 0으로 폴백한다.

큐는 `누적 비용 + 하한`이 가장 작은 상태부터 꺼내며, 더 싼 경로가 발견된 상태는 다시 연다. 승리 상태가 큐에서 꺼내졌을 때만 최단으로 확정했다. 검색한 유효 간선마다 하한의 일관성과 승리 시 하한 0도 검사했다.

셔터의 무한히 커지는 phase는 탐색 키에서만 홀짝으로 정규화했다. 현재 셔터 판정은 phase의 홀짝만 사용하며 active 값과 나머지 장치 상태는 그대로 보존한다. 실제 세션의 phase나 맵 규칙은 바꾸지 않았다. 상태 스냅샷은 실제 세션 상태를 보존하고 키 압축은 기존 무손실 코덱을 사용한다.

각 맵의 상한은 방문 상태 3,000,000개, 추가 관리 메모리 3,072 MiB였다. 다섯 결과 모두 상한 중단 없이 승리 상태를 확정했다. 도달 불가·취소·상한 결과를 최단값으로 바꾸지 않았다.

## 실행한 검증과 범위

Unity 6000.3.8f1의 Editor에서 다음을 실행했다. Play Mode 전체 플레이 테스트와는 구분한다.

- 다섯 맵의 정확 탐색과 공개 `Apply` 정답 재생: 모두 목표 별을 모으고 출구에서 승리했다. [최종 재생 기록](Validation/EchoTheaterShortestPaths/final_replay.json).
- 2,500개 표본 상태를 스냅샷으로 왕복 복원하고 네 방향 입력 10,000개를 공개 API로 대조했다. 셔터 phase+2 쌍의 후속 상태와 하한 일관성 검사도 통과했다. 셔터가 있는 두 맵에서의 비자명한 비교는 이 중 4,000쌍이며, 나머지 맵의 비교는 셔터가 없는 동일 상태 비교다. [전이 검증 기록](Validation/EchoTheaterShortestPaths/transition_validation.json).
- 이동 발판 둘이 있는 ET-03은 기존 `PuzzleShortestPathSolver`의 0-1 BFS로 별도 계산해 **49입력**을 재확인했다. 이 실행은 97,483개 상태를 방문했고 26.17초가 걸렸다. [별도 BFS 기록](Validation/EchoTheaterShortestPaths/independent_ET-03_BFS.json).

ET-05의 Map/Scenario에는 기존 Editor 미저장 표시가 있다. 계산은 그 로드된 상태를 복제해 수행했고, 마지막에는 현재 원본으로 재생했다. 배치 보고서에서 비교한 배치·카메라·주요 규칙 값은 디스크 저장본과 같지만, 미저장 표시 자체는 해제하지 않았다. 각 결과에 디스크 원본 SHA-256과 계산 입력 스냅샷 SHA-256을 따로 남겼다.

전체 플레이 입력 예약, HUD 예산·표시, 애니메이션, 모바일 GPU는 이번 경로 계산으로 검증하지 않았다. Unity Test Runner의 EditMode/PlayMode 테스트 모음도 이번 문서·탐색 작업에서는 실행하지 않았다. 런타임 이동·연출 코드를 수정하지 않았으며, 원본 맵·메타·작성된 `verificationRoute`, 저장 진행도, costLimit, HUD 예산은 변경하지 않았다.

## 저장 위치와 재검증

[기계 판독용 경로 기록](../Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json)은 별길 목장처럼 MapData 폴더에 별도 JSON으로 남겼다. 이 파일을 런타임 예산에 연결하는 변경은 포함하지 않았다. [원본 배치 보고서](ANIMOL_MAP_LAYOUT_REPORT.md)와 함께 확인할 수 있다.

각 맵의 `_input.json`에는 계산 당시의 판정 입력이 있고, 결과 JSON에는 엔진·탐색기 소스 해시와 매 입력 재생 좌표가 있다. 맵·장치 규칙이 바뀌면 기록을 다시 검증해야 한다. 이전 숫자를 새 배치의 최단값으로 재사용하지 않는다.

재현 도구는 [Unity 탐색기](../Tools/Puzzle/EchoTheaterShortestPaths.cs), [공개 API 검증기](../Tools/Puzzle/VerifyEchoSearch.cs), [보고서·기록 생성기](../Tools/Reports/build_echo_shortest_report.py)다. 준비된 Editor의 Play Mode 밖에서 `unity command run_script --file Tools/Puzzle/EchoTheaterShortestPaths.cs --entry EchoTheaterShortestPaths.Start --format json`을 한 번 실행하고 완료를 기다린다. 이어서 같은 run_script 명령으로 `VerifyEchoSearch.Transitions`, `VerifyEchoSearch.ReplayAll`을 실행한다. 검증기는 탐색기 타입이 로드된 동일 Editor 세션에서 실행한다.

별도 BFS 증거는 `VerifyEchoSearch.IndependentET03Bfs`로 재현할 수 있다. 내부에서 ET-03에 `PuzzleShortestPathSolver.Start(map, callback, 3000000, false, 3072)`를 실행한다. `false`는 휴리스틱을 끄는 옵션이며, 콜백은 증거 JSON만 기록한다. 증거가 준비되면 `python Tools/Reports/build_echo_shortest_report.py`로 이 문서와 JSON을 다시 생성한다.


</details>
