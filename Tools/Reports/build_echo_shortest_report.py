"""Publish already verified Unity search results; never edits native map assets.

Run after EchoTheaterShortestPaths.Start, VerifyEchoSearch.Transitions and
VerifyEchoSearch.ReplayAll through Unity Pipeline. Import the new JSON catalog
through AssetDatabase.ImportAsset to obtain its Unity-generated .meta.
"""

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / 'Docs/Validation/EchoTheaterShortestPaths'
CATALOG = ROOT / 'Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json'
IDS = ['ET-00', 'ET-01', 'ET-03', 'ET-04', 'ET-05']
ARROWS = dict(zip('NESW', '↑→↓←'))


def read(path):
    return json.loads(path.read_text(encoding='utf8'))


def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf8')


def main():
    layouts = read(ROOT / 'Docs/Validation/MapLayoutReport/unity_snapshot.json')
    by_id = {m['id']: m for m in layouts['maps']}
    transitions = read(EVIDENCE / 'transition_validation.json')
    replays = {r['id']: r for r in read(EVIDENCE / 'final_replay.json')}
    independent = read(EVIDENCE / 'independent_ET-03_BFS.json')
    assert transitions['passed'] and transitions['publicApplyEdges'] == 10000
    assert independent['status'] == 'solved' and independent['distance'] == 49
    assert independent['sourceSha256'] == by_id['ET-03']['sourceSha256']
    assert len(independent['route']) == independent['distance'] and set(independent['route']) <= set(ARROWS)
    maps = []
    results = []
    for map_id in IDS:
        result = read(EVIDENCE / (map_id + '.json'))
        layout = by_id[map_id]
        assert result['status'] == 'solved' and result['publicApplyReplayWon'] and result['sourceUnchanged']
        route = result['routeNESW']
        assert set(route) <= set(ARROWS) and len(route) == result['distance']
        assert ''.join(ARROWS[c] for c in route) == result['route']
        assert hashlib.sha256((ROOT / result['sourceAsset']).read_bytes()).hexdigest() == result['sourceSha256']
        assert result['sourceSha256'] == layout['sourceSha256']
        for path, expected in result['engineSourceHashes'].items():
            assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected, path
        replay = replays[map_id]
        assert all(replay[k] for k in ['won', 'exitReached', 'snapshotMatchesCurrentEditor', 'sourceDirtyPreserved'])
        assert replay['inputs'] == result['distance'] and replay['collected'] >= replay['target']
        assert len(result['replay']) == result['distance'] and result['replay'][-1]['won']
        results.append(result)
        maps.append({
            'mapId': map_id, 'displayStageIds': layout['displayStages'],
            'mapAsset': result['sourceAsset'], 'mapSha256': result['sourceSha256'],
            'snapshotSha256': result['snapshotSha256'], 'sourceWasUnsaved': result['sourceWasUnsaved'],
            'status': 'solved', 'distance': result['distance'],
            'route': result['route'], 'routeNESW': route,
            'algorithm': result['algorithm'], 'expanded': result['expanded'],
            'visited': result['visited'], 'elapsedSeconds': result['elapsedSeconds'],
            'evidence': 'Docs/Validation/EchoTheaterShortestPaths/' + map_id + '.json',
        })
    write(CATALOG, {
        'formatVersion': 1,
        'scope': 'Native ET-00, ET-01, ET-03, ET-04 and ET-05; display ET-02 uses native ET-00',
        'costModel': 'One accepted directional input costs 1, including stationary actions; no wind on these maps',
        'engineSourceHashes': results[0]['engineSourceHashes'],
        'validation': 'Docs/Validation/EchoTheaterShortestPaths/final_replay.json',
        'maps': maps,
    })
    lines = [
        '# 잔영 극장 초반 맵 최단 정답 경로', '',
        '원본 ET-00~ET-05 범위에서 현재 존재하는 다섯 맵의 최단 정답을 확정했다. 현재 Unity Editor에 로드된 맵과 실제 `PuzzleDeviceSession` 이동 판정으로 계산했으며, 모든 경로를 공개 `Apply` API로 다시 재생해 별 수집·출구 도달·승리를 확인했다.', '',
        '화면 ET-02는 원본 ET-00을 사용한다. 원본 `ET-02_MapData.asset`와 메타는 현재 없으므로 옛 JSON을 대신 풀거나 새 맵을 만들지 않았다. 화면 ET-01은 원본 ET-01, 화면 ET-03~05는 같은 번호의 원본을 사용한다.', '',
        '## 확정 결과', '',
        '| 원본 맵 | 화면 스테이지 | 최단 입력 수 | 수집 / 목표 | 방문 상태 수 | 탐색 시간 |',
        '|---|---|---:|---:|---:|---:|',
    ]
    for m in maps:
        replay = replays[m['mapId']]
        lines.append(f"| [{m['mapId']}](#{m['mapId'].lower()}) | {', '.join(m['displayStageIds'])} | **{m['distance']}** | {replay['collected']} / {replay['target']} | {m['visited']:,} | {m['elapsedSeconds']:.2f}초 |")
    lines += [
        '', '시간은 이번 실행의 측정값이다. 최단성은 시간이나 방문 수가 아니라 아래의 탐색 조건에 근거한다.', '',
        '## 경로 읽는 법', '',
        '`↑/N`은 위, `→/E`는 오른쪽, `↓/S`는 아래, `←/W`는 왼쪽이다. 화살표 하나가 비용 1인 유효 방향 입력 하나다. 물체를 밀거나 장치를 작동시켜 플레이어가 같은 칸에 남는 입력도 포함하므로, 반복 방향이나 제자리 입력을 생략하면 안 된다. 아래는 10입력씩 끊어 적었으며 마지막 줄만 더 짧을 수 있다.', '',
    ]
    for m in maps:
        lines += [f"## {m['mapId']}", '',
                  f"화면 {', '.join(m['displayStageIds'])}, **{m['distance']}입력**. [배치도와 장치 목록](ANIMOL_MAP_LAYOUT_REPORT.md#{m['mapId'].lower()}) · [매 입력 좌표·수집 기록](Validation/EchoTheaterShortestPaths/{m['mapId']}.json).", '', '```text']
        route = m['route']
        for i in range(0, len(route), 10):
            lines.append(f'{i + 1:02d}~{min(i + 10, len(route)):02d}: ' + ' '.join(route[i:i + 10]))
        lines += ['```', '', 'NESW 표기:', '', '```text', m['routeNESW'], '```', '']
    lines += [
        '## 최단성을 확인한 방법', '',
        '각 간선은 현재 런타임의 `PuzzleDeviceSession.SearchStep`으로 생성했다. 이 다섯 맵에는 무료 바람 이동이 없고, 받아들여진 입력은 모두 비용 1인지 검사했다. 셔터, 이동 발판, 도미노, 유리, 주사위, 겁쟁이 인형, 석상의 실제 상태와 추가 승리 조건을 유지했다. 석상은 유리를 부순 뒤 그 칸에 들어가지 않고 바로 앞에서 이번 돌진을 끝내는 현재 규칙을 사용한다.', '',
        '탐색은 정확 A*다. 하한은 움직이는 장애물을 제거하고, 제자리 행동과 이동 발판의 가능한 밀림을 더 넓게 허용한 격자에서 필요한 별을 모아 출구까지 가는 최단 입력 수다. 각 발판을 순서대로 반영하고 중간에 얻는 별도 보존하며, 발판이 원래 입력 위치로 돌려보내는 경우까지 허용한다. 실제로 가능한 이동을 이 완화된 그래프가 포함하므로 실제 남은 비용을 초과하지 않는다. 지원 밖 장치가 있으면 하한 0으로 폴백한다.', '',
        '큐는 `누적 비용 + 하한`이 가장 작은 상태부터 꺼내며, 더 싼 경로가 발견된 상태는 다시 연다. 승리 상태가 큐에서 꺼내졌을 때만 최단으로 확정했다. 검색한 유효 간선마다 하한의 일관성과 승리 시 하한 0도 검사했다.', '',
        '셔터의 무한히 커지는 phase는 탐색 키에서만 홀짝으로 정규화했다. 현재 셔터 판정은 phase의 홀짝만 사용하며 active 값과 나머지 장치 상태는 그대로 보존한다. 실제 세션의 phase나 맵 규칙은 바꾸지 않았다. 상태 스냅샷은 실제 세션 상태를 보존하고 키 압축은 기존 무손실 코덱을 사용한다.', '',
        '각 맵의 상한은 방문 상태 3,000,000개, 추가 관리 메모리 3,072 MiB였다. 다섯 결과 모두 상한 중단 없이 승리 상태를 확정했다. 도달 불가·취소·상한 결과를 최단값으로 바꾸지 않았다.', '',
        '## 실행한 검증과 범위', '',
        f"Unity {layouts['unityVersion']}의 Editor에서 다음을 실행했다. Play Mode 전체 플레이 테스트와는 구분한다.", '',
        '- 다섯 맵의 정확 탐색과 공개 `Apply` 정답 재생: 모두 목표 별을 모으고 출구에서 승리했다. [최종 재생 기록](Validation/EchoTheaterShortestPaths/final_replay.json).',
        '- 2,500개 표본 상태를 스냅샷으로 왕복 복원하고 네 방향 입력 10,000개를 공개 API로 대조했다. 셔터 phase+2 쌍의 후속 상태와 하한 일관성 검사도 통과했다. 셔터가 있는 두 맵에서의 비자명한 비교는 이 중 4,000쌍이며, 나머지 맵의 비교는 셔터가 없는 동일 상태 비교다. [전이 검증 기록](Validation/EchoTheaterShortestPaths/transition_validation.json).',
        f"- 이동 발판 둘이 있는 ET-03은 기존 `PuzzleShortestPathSolver`의 0-1 BFS로 별도 계산해 **49입력**을 재확인했다. 이 실행은 {independent['visited']:,}개 상태를 방문했고 {independent['elapsedSeconds']:.2f}초가 걸렸다. [별도 BFS 기록](Validation/EchoTheaterShortestPaths/independent_ET-03_BFS.json).", '',
        'ET-05의 Map/Scenario에는 기존 Editor 미저장 표시가 있다. 계산은 그 로드된 상태를 복제해 수행했고, 마지막에는 현재 원본으로 재생했다. 배치 보고서에서 비교한 배치·카메라·주요 규칙 값은 디스크 저장본과 같지만, 미저장 표시 자체는 해제하지 않았다. 각 결과에 디스크 원본 SHA-256과 계산 입력 스냅샷 SHA-256을 따로 남겼다.', '',
        '전체 플레이 입력 예약, HUD 예산·표시, 애니메이션, 모바일 GPU는 이번 경로 계산으로 검증하지 않았다. Unity Test Runner의 EditMode/PlayMode 테스트 모음도 이번 문서·탐색 작업에서는 실행하지 않았다. 런타임 이동·연출 코드를 수정하지 않았으며, 원본 맵·메타·작성된 `verificationRoute`, 저장 진행도, costLimit, HUD 예산은 변경하지 않았다.', '',
        '## 저장 위치와 재검증', '',
        '[기계 판독용 경로 기록](../Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json)은 별길 목장처럼 MapData 폴더에 별도 JSON으로 남겼다. 이 파일을 런타임 예산에 연결하는 변경은 포함하지 않았다. [원본 배치 보고서](ANIMOL_MAP_LAYOUT_REPORT.md)와 함께 확인할 수 있다.', '',
        '각 맵의 `_input.json`에는 계산 당시의 판정 입력이 있고, 결과 JSON에는 엔진·탐색기 소스 해시와 매 입력 재생 좌표가 있다. 맵·장치 규칙이 바뀌면 기록을 다시 검증해야 한다. 이전 숫자를 새 배치의 최단값으로 재사용하지 않는다.', '',
        '재현 도구는 [Unity 탐색기](../Tools/Puzzle/EchoTheaterShortestPaths.cs), [공개 API 검증기](../Tools/Puzzle/VerifyEchoSearch.cs), [보고서·기록 생성기](../Tools/Reports/build_echo_shortest_report.py)다. 준비된 Editor의 Play Mode 밖에서 `unity command run_script --file Tools/Puzzle/EchoTheaterShortestPaths.cs --entry EchoTheaterShortestPaths.Start --format json`을 한 번 실행하고 완료를 기다린다. 이어서 같은 run_script 명령으로 `VerifyEchoSearch.Transitions`, `VerifyEchoSearch.ReplayAll`을 실행한다. 검증기는 탐색기 타입이 로드된 동일 Editor 세션에서 실행한다.', '',
        '별도 BFS 증거는 `VerifyEchoSearch.IndependentET03Bfs`로 재현할 수 있다. 내부에서 ET-03에 `PuzzleShortestPathSolver.Start(map, callback, 3000000, false, 3072)`를 실행한다. `false`는 휴리스틱을 끄는 옵션이며, 콜백은 증거 JSON만 기록한다. 증거가 준비되면 `python Tools/Reports/build_echo_shortest_report.py`로 이 문서와 JSON을 다시 생성한다.', '',
    ]
    (ROOT / 'Docs/ECHO_THEATER_SHORTEST_PATHS.md').write_text('\n'.join(lines), encoding='utf8')
    print(json.dumps({'maps': len(maps), 'distances': {m['mapId']: m['distance'] for m in maps}, 'sourceAssetsChanged': 0}))


if __name__ == '__main__':
    main()
