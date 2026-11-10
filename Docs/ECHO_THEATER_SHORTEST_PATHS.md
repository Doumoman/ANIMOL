# 잔영 극장 ET-20까지의 최단 입력 경로

맵 원본 커밋 `e904b95d3`을 기준으로 존재하는 **20개 맵**을 계산했다. **최단 확정 20개, 도달 불가 0개, 미확정 0개**다.

ET-06 파일은 삭제된 상태를 유지했다. 화면 번호와 파일 ID는 다를 수 있으므로 아래의 실제 MapSet 연결을 함께 표시한다. 계산 결과는 MapData와 별도의 경로 카탈로그에 기록한다.

[맵·GUID·원본 해시](Validation/EchoThrough20/inventory.json) · [Unity 실제 경로 재생](Validation/EchoThrough20/native_replay.json)

## 현재 결과

| 파일 ID | 화면 스테이지 | 목표 방울 | 결과 | 최단 입력 | 발견 상태 |
|---|---|---:|---|---:|---:|
| ET-00 | ET-02 | 3 | 최단 확정 | 31 | 64 |
| ET-01 | ET-06 | 3 | 최단 확정 | 19 | 51 |
| ET-02 | ET-07 | 3 | 최단 확정 | 29 | 3,736 |
| ET-03 | ET-03 | 2 | 최단 확정 | 49 | 48,738 |
| ET-04 | ET-04 | 2 | 최단 확정 | 33 | 37,414 |
| ET-05 | ET-05 | 2 | 최단 확정 | 26 | 3,081 |
| ET-07 | ET-01 | 2 | 최단 확정 | 42 | 253,243 |
| ET-08 | ET-08 | 2 | 최단 확정 | 43 | 1,191 |
| ET-09 | ET-09 | 3 | 최단 확정 | 40 | 400,283 |
| ET-10 | ET-10 | 2 | 최단 확정 | 42 | 478,718 |
| ET-11 | ET-11 | 3 | 최단 확정 | 33 | 2,164 |
| ET-12 | ET-12 | 3 | 최단 확정 | 43 | 84,123 |
| ET-13 | ET-13 | 2 | 최단 확정 | 32 | 3,229,651 |
| ET-14 | ET-14 | 3 | 최단 확정 | 46 | 847,211 |
| ET-15 | ET-15 | 2 | 최단 확정 | 54 | 579,952 |
| ET-16 | ET-16 | 3 | 최단 확정 | 45 | 1,219 |
| ET-17 | ET-17 | 3 | 최단 확정 | 19 | 6,728 |
| ET-18 | ET-18 | 2 | 최단 확정 | 48 | 22,957 |
| ET-19 | ET-19 | 7 | 최단 확정 | 8 | 30 |
| ET-20 | ET-20 | 4 | 최단 확정 | 54 | 444,210 |

ET-19는 시작점에서 위로 8번 연속 이동하면 별 7개를 모으고 출구에 도달한다. 현재 맵의 가장 짧은 통과 경로이며, 별도 우회가 필요하지 않다.

ET-20은 현재 책장·발판 연결과 회전 설정, 목표 방울 4개를 기준으로 다시 계산했다. **4개를 모두 모으는 최단 경로는 54입력**이다. Play Mode 미리보기에서 실제 스와이프 수신부와 컨트롤러로 54개 입력을 실행하고, 각 애니메이션 종료 후 상태·플레이어 위치·방울 4개·승리를 확인했다. [실제 컨트롤러 재생](Validation/EchoThrough20/et20_controller_replay.json).

방향 입력 하나의 비용은 1이다. 제자리 밀기도 포함한다. `↑/N` 위, `→/E` 오른쪽, `↓/S` 아래, `←/W` 왼쪽이며 반복 입력을 생략하지 않는다.

## 계산과 검증

모든 간선은 현재 Unity와 같은 `PuzzleDeviceSession` 소스의 `SearchStep`으로 생성했다. 20개 맵의 8,000개 실제 Unity 상태와 32,000개 공개 입력 전이를 별도 실행기와 대조했다. 완성된 경로는 Unity의 `ReplayRuntimeInput`으로 다시 실행해 수집·출구·승리·비용을 확인했다.

A* 하한은 동적 장애물을 제거하고 카트가 줄 수 있는 밀림을 더 넓게 허용한 완화 보드다. 더 싼 경로를 찾으면 상태를 다시 열며, 최소 `g+h`의 승리 상태를 꺼낸 경우에만 최단으로 확정한다. 셔터의 phase는 키에서만 홀짝으로 묶는다. 석상이 돌진 중이 아닐 때의 과거 timer 역시 다음 감지 때 덮어쓰므로 키에서만 정규화하며, 잠긴 석상의 timer는 유지한다. 유리 상자의 지난 이동 방향은 다음 이동 입력이나 충돌 판정에 사용하지 않으므로 키에서만 묶는다. 실제 세션과 체크포인트의 원래 값은 보존한다.

ET-15 재탐색은 석상과 유리를 제거한 완화 보드에서 플레이어·주사위 위치·24가지 면 방향·별 수집·주사위 발판 개방을 함께 계산한다. 이 보드의 369,272개 상태에 대한 역방향 BFS 거리를 추가 하한으로 사용한다. 유리 밀기는 플레이어가 제자리에 있는 완화 간선으로 허용한다. 주사위 면 전환은 원래 `PuzzleDeviceSession.Roll`로 생성하며, 실제 탐색에서 확장한 모든 유효 간선의 투영이 완화 그래프에도 존재하는지 검사한다. Unity 표본의 유효 간선 1,128개도 같은 투영 검사를 통과했다. [주사위 하한 검증](Validation/EchoThrough18/transition_verification_dice.json).

석상·유리·주사위만 있는 맵에서는 바닥이 끝나거나 영구 정지한 석상에 막혀 더는 이동할 수 없는 석상을 찾는다. 이 영구 장애물만 남긴 완화 보드에서도 출구나 필요한 별에 도달할 수 없는 분기만 제외한다. 다른 장치가 있는 맵에는 이 조건을 적용하지 않는다. 작은 보드의 완전 BFS와 실제 소스 전이로 거짓 차단 여부를 검사했다.

탐색 상태 수와 상태·메모리 상한은 각 검색 JSON에 있다. 상한·취소·원본 변경은 미확정이며 도달 불가로 바꾸지 않는다. 원본 맵, `.meta` GUID와 작성된 `verificationRoute`를 계산 때문에 수정하지 않았다. 모든 맵의 PlayMode 전체 플레이나 모바일 렌더링을 검증한 결과와는 구분한다.

초기 검색 빌드와 정규화를 보강한 빌드는 각각의 소스 해시와 검증 보고서로 구분한다. 완료된 초기 빌드의 큐·진입점 소스는 `Validation/EchoThrough18/InitialBuildSources`에 보관했다. 현재 런타임 판정 소스는 모든 빌드에서 동일하다.

## 경로

## ET-00

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-00.json) · [검색 증거](Validation/EchoThrough18/Search/ET-00.json)

**31입력**

```text
01~10: → → ↑ → → ↓ → ↑ ↑ ↑
11~20: ↑ ↓ ↓ ↓ → → ↑ → → →
21~30: ↑ ↓ ↓ → → ↑ ↑ ↓ ↓ →
31~31: →
```

NESW: `EENEESENNNNSSSEENEEENSSEENNSSEE`

## ET-01

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-01.json) · [검색 증거](Validation/EchoThrough18/Search/ET-01.json)

**19입력**

```text
01~10: ← ← ↓ ← → → ↓ → → →
11~19: → ↑ ↓ ↓ ← ↑ → → →
```

NESW: `WWSWEESEEEENSSWNEEE`

## ET-02

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-02.json) · [검색 증거](Validation/EchoThrough18/Search/ET-02.json)

**29입력**

```text
01~10: → ↑ ↑ → → ↑ ← ← ↓ ←
11~20: ← ↑ ↓ ↓ ↑ → → ↑ → →
21~29: → → ↓ ↓ ↑ ↑ ← ↑ ↑
```

NESW: `ENNEENWWSWWNSSNEENEEEESSNNWNN`

## ET-03

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-03.json) · [검색 증거](Validation/EchoThrough18/Search/ET-03.json)

**49입력**

```text
01~10: → → ↓ ↑ → ↓ ↑ ← ↑ ↑
11~20: ← ↑ ↓ ← ↓ ↓ ↓ ← ↓ ↓
21~30: → → → → → ↓ → ↑ ↑ ↑
31~40: ↓ ← ↑ → ↑ ↑ ↑ ← ↑ →
41~49: ↑ → → ↓ → → ↑ ↑ ↑
```

NESW: `EESNESNWNNWNSWSSSWSSEEEEESENNNSWNENNNWNENEESEENNN`

## ET-04

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-04.json) · [검색 증거](Validation/EchoThrough18/Search/ET-04.json)

**33입력**

```text
01~10: ↑ → ↑ ↑ → → ↑ ↑ ↑ ↑
11~20: ← ↓ ↓ ← ← ↓ → ↑ → →
21~30: ↓ ↓ ↑ → → ↑ ↓ ← ↑ →
31~33: ↑ ↑ →
```

NESW: `NENNEENNNNWSSWWSENEESSNEENSWNENNE`

## ET-05

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-05.json) · [검색 증거](Validation/EchoThrough18/Search/ET-05.json)

**26입력**

```text
01~10: → → → ↑ ← ← ↓ ← ↑ ↑
11~20: → ↑ ↑ ↓ ↓ → → ↑ ↑ ↑
21~26: ↑ ↑ ↑ ↓ → →
```

NESW: `EEENWWSWNNENNSSEENNNNNNSEE`

## ET-07

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-07.json) · [검색 증거](Validation/EchoThrough18/Search/ET-07.json)

**42입력**

```text
01~10: → ↑ → → ↓ → ↓ ↓ ← ↑
11~20: ← ← ↓ ← ↓ ↓ → ← ← ←
21~30: ↑ ↑ ↑ → → → → ↑ ↓ ↓
31~40: → ↓ ↓ → → → → ← ↑ ↑
41~42: ↑ ↑
```

NESW: `ENEESESSWNWWSWSSEWWWNNNEEEENSSESSEEEEWNNNN`

## ET-08

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-08.json) · [검색 증거](Validation/EchoThrough18/Search/ET-08.json)

**43입력**

```text
01~10: ← ↑ ↑ → → ↓ → → ↑ ↑
11~20: ↑ ← ← ← → → ↑ ↑ → ↑
21~30: ← ← ↓ ← ↑ → ↓ ↓ ↓ ←
31~40: ← ↓ ↑ → → → → ↑ ↑ →
41~43: → → ↑
```

NESW: `WNNEESEENNNWWWEENNENWWSWNESSSWWSNEEEENNEEEN`

## ET-09

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-09.json) · [검색 증거](Validation/EchoThrough18/Search/ET-09.json)

**40입력**

```text
01~10: → ↓ ↓ → ← ← ↑ ← ↑ ←
11~20: → ↓ ↓ ← → ↑ ↑ ↑ ← ↑
21~30: → ↓ → → → → ↓ → ↑ ↑
31~40: ↑ ← ↑ ↑ → ↓ → ↑ → ↑
```

NESW: `ESSEWWNWNWESSWENNNWNESEEEESENNNWNNESENEN`

## ET-10

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-10.json) · [검색 증거](Validation/EchoThrough18/Search/ET-10.json)

**42입력**

```text
01~10: ↑ ↑ → → ↑ ↓ ↑ → ↑ ←
11~20: → → → ↓ ↑ → ↓ → ↑ ↓
21~30: → ↑ ↑ ↑ ← ↓ ← ← ← ↑
31~40: → → ↓ → ↑ ↑ → ↑ ← ←
41~42: ← ↑
```

NESW: `NNEENSNENWEEESNESENSENNNWSWWWNEESENNENWWWN`

## ET-11

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-11.json) · [검색 증거](Validation/EchoThrough18/Search/ET-11.json)

**33입력**

```text
01~10: ← ↑ ↑ ↑ → ↑ ↑ ↑ ← ↓
11~20: ← ↑ ← ↓ ↓ ← → ↑ ↑ ↑
21~30: ↑ ↑ → → ↓ ↓ → ↓ → →
31~33: ↑ → ↑
```

NESW: `WNNNENNNWSWNWSSWENNNNNEESSESEENEN`

## ET-12

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-12.json) · [검색 증거](Validation/EchoThrough18/Search/ET-12.json)

**43입력**

```text
01~10: ← ↑ ↑ ← ← ↑ ↑ → → →
11~20: → ↓ ↓ → → ↑ ↑ ↑ ↑ ↑
21~30: ← ↑ → → → → → ← ← ←
31~40: ↓ ← ↓ ← → → ↓ ↓ ↓ →
41~43: → ↓ ↓
```

NESW: `WNNWWNNEEEESSEENNNNNWNEEEEEWWWSWSWEESSSEESS`

## ET-13

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-13.json) · [검색 증거](Validation/EchoThrough18/Search/ET-13.json)

**32입력**

```text
01~10: ← ↑ → → ↓ → ↑ ↑ → ←
11~20: ← ↑ → → → ↑ ↑ ↓ → →
21~30: ↑ ↓ ↓ ↑ ← ↑ ← ← ↑ ←
31~32: ← ↑
```

NESW: `WNEESENNEWWNEEENNSEENSSNWNWWNWWN`

## ET-14

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-14.json) · [검색 증거](Validation/EchoThrough18/Search/ET-14.json)

**46입력**

```text
01~10: ← ↑ ← ↑ ↑ → → ↑ ↑ ↑
11~20: ↑ → → ↓ ↓ → ↓ → ← ←
21~30: ← → → → → → ↓ → → ↑
31~40: ← ← ↑ → → → → → → ↓
41~46: → ← ↑ ↑ ← ↑
```

NESW: `WNWNNEENNNNEESSESEWWWEEEEESEENWWNEEEEEESEWNNWN`

## ET-15

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-15.json) · [검색 증거](Validation/EchoThrough18/Search/ET-15.json)

**54입력**

```text
01~10: ↑ → ↓ ↓ ↑ ↓ ← ← ↑ →
11~20: ↑ ↑ ↓ → ↓ → ↓ → → ↑
21~30: ↑ ← ↑ → → ← → → → ↓
31~40: ↓ ↑ ↓ → ↑ ↑ ↑ ↑ ↑ ↑
41~50: ↑ ↑ ↑ ↑ ← ← ← ↑ ↓ →
51~54: → → ↑ ↑
```

NESW: `NESSNSWWNENNSESESEENNWNEEWEEESSNSENNNNNNNNNNWWWNSEEENN`

## ET-16

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-16.json) · [검색 증거](Validation/EchoThrough18/Search/ET-16.json)

**45입력**

```text
01~10: ↑ ← ← ↑ ↑ ↑ → → → ↑
11~20: ↓ ↑ ↑ ← ← → → ↓ ↓ ←
21~30: ↑ ↑ ↑ ← ← ↓ ↓ ↓ ↓ ←
31~40: → ↓ ↓ → → → ↑ ↑ ← ←
41~45: → → ↓ → ↓
```

NESW: `NWWNNNEEENSNNWWEESSWNNNWWSSSSWESSEEENNWWEESES`

## ET-17

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-17.json) · [검색 증거](Validation/EchoThrough18/Search/ET-17.json)

**19입력**

```text
01~10: ↑ ↑ ↑ → ↑ ← ↓ ← ↓ ↓
11~19: → ↑ ↑ ↑ ↑ → ↑ ↑ →
```

NESW: `NNNENWSWSSENNNNENNE`

## ET-18

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-18.json) · [검색 증거](Validation/EchoThrough18/Search/ET-18.json)

**48입력**

```text
01~10: ← ↑ ← ↑ → → → ↑ ↑ ↑
11~20: ↑ ↓ → → ← ← ← ← ↓ ↑
21~30: ↓ → → ↓ ↓ ↓ → → ← ←
31~40: ↑ ← ← ← ← ↑ ↑ ↓ ↓ →
41~48: ↑ ↑ ↑ → → ↑ → →
```

NESW: `WNWNEEENNNNSEEWWWWSNSEESSSEEWWNWWWWNNSSENNNEENEE`

## ET-19

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-19.json) · [검색 증거](Validation/EchoThrough20/Search/ET-19.json)

**8입력**

```text
01~08: ↑ ↑ ↑ ↑ ↑ ↑ ↑ ↑
```

NESW: `NNNNNNNN`

## ET-20

[입력별 Unity 재생](Validation/EchoThrough20/Replay/ET-20.json) · [검색 증거](Validation/EchoThrough20/Search/ET-20.json)

**54입력**

```text
01~10: ↓ → ↑ ↑ → ← ↑ ↑ ↓ ↓
11~20: ↓ ↓ ↓ → → → → → ↑ ←
21~30: ↑ ↑ ↑ ↓ ← → ↑ ↑ ↑ ←
31~40: ← ↓ ↑ → → ↓ → ↓ ← ↓
41~50: ↓ ↓ ↓ ← ← ↑ ↓ → → ↑
51~54: ↑ → → ↑
```

NESW: `SENNEWNNSSSSSEEEEENWNNNSWENNNWWSNEESESWSSSSWWNSEENNEEN`

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
