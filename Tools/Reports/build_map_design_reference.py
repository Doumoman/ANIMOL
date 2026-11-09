"""Build LLM authoring references from read-only Unity exports and proven replays.

Standard library only. Does not write Assets, scenes, prefabs or runtime budgets.
The weighting model is an editorial proposal, not a measured human difficulty model.
"""

from collections import Counter
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re
import statistics
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Validation/MapDesign'
WEIGHTS = {'walk_or_other': 1.0, 'body_push': 1.5, 'sheep_push': 1.5, 'pond_cross': 1.75,
           'domino_push': 1.75, 'wind_setting': 2.0, 'dice_push': 2.5, 'mirror_rotate': 2.0, 'free_wind': 0.0}
LABELS = {'walk_or_other': '이동·기타', 'body_push': '물체 밀기', 'sheep_push': '양 밀기', 'pond_cross': '수문 교차',
          'domino_push': '도미노 밀기', 'wind_setting': '풍차 설정', 'dice_push': '주사위 밀기', 'mirror_rotate': '거울 회전', 'free_wind': '무료 바람'}
ROLES = dict(enumerate(['StarSocket', 'VineBridge', 'CrescentLeap', 'Shutter', 'DepartureSpikes', 'MovingStage',
    'Conveyor', 'Bookshelf', 'OneWayGate', 'Optics', 'Boat', 'Barrel', 'Domino', 'Magnet', 'Bomb', 'Portal',
    'Statue', 'Bounce', 'Brake', 'Wind', 'Glass', 'Dice', 'CowardDoll', 'Chick', 'Ice'], 1))
ROLES.update({100: 'Wall', 101: 'CrackedWall', 102: 'WoodCrate', 103: 'MetalCrate'})
# Pattern annotations describe reference uses; only the replay establishes action counts.
PATTERNS = {
 'SW00': ['P02', 'P03', 'P04', 'P06'], 'SW01': ['P02', 'P03', 'P05'],
 'SW02': ['P02', 'P04', 'P05'], 'SW03': ['P01', 'P04', 'P06'],
 'SW04': ['P02', 'P03', 'P04', 'P06'], 'SW05': ['P01', 'P02', 'P03', 'P06'],
 'SW06': ['P02', 'P04', 'P05', 'P07'], 'SW07': ['P03', 'P04', 'P05', 'P07'],
 'SW08': ['P01', 'P05', 'P06'], 'SW09': ['P03', 'P04', 'P06', 'P07'],
 'SW10': ['P02', 'P03', 'P04', 'P07'], 'SW11': ['P01', 'P05'],
 'SW12': ['P02', 'P03', 'P04', 'P05'], 'SW13': ['P02', 'P03', 'P04', 'P05'],
 'SW14': ['P01', 'P02', 'P05'], 'SW15': ['P02', 'P03', 'P05'],
 'SW16': ['P01', 'P04'], 'SW17': ['P02', 'P04', 'P05'],
 'SW18': ['P01', 'P04', 'P05'], 'SW19': ['P04', 'P05', 'P06', 'P07'],
 'ET-00': ['P08', 'P09', 'P11'], 'ET-01': ['P08', 'P09', 'P10', 'P11'],
 'ET-03': ['P09', 'P10'], 'ET-04': ['P09', 'P10', 'P13'], 'ET-05': ['P11', 'P12'],
}
NOTES = {
 'SW00': '별돌/소켓과 금속 상자, 일방통행 문에 수문과 양 소모를 결합한 예다. 서로 다른 규칙을 한 경로에 배치하는 기준으로 본다.',
 'SW01': '별돌을 소켓에 넣는 접근과 병아리 점유를 함께 본다. 짧은 통로에서도 제자리 밀기는 별도 입력으로 계산한다.',
 'SW02': '선택 경로에서 양 3마리가 소모되고 직접 양 밀기 14회, 물체 밀기 6회가 필요하다. 자원 배정과 접근로 정리의 긴 예다.',
 'SW03': '별 4개 중 3개가 목표다. 어느 별을 수집할지 선택하는 분기와 양·수문의 상태 변화를 참고한다.',
 'SW04': '넓은 바닥 안에 별돌·상자·양·수문을 둔다. 면적보다 물체를 밀러 돌아가는 접근 순서를 본다.',
 'SW05': '별 3개 중 2개가 목표다. 선택 경로에는 수문 교차 3회와 물체 밀기 6회가 들어간다.',
 'SW06': '풍차 단계 변경과 무료 바람 한 번이 있는 예다. 직접 양 밀기는 1회지만 바람 반응까지 포함해 양 2마리가 소모된다.',
 'SW07': '별돌과 양 조작 사이에 풍차 설정과 무료 바람을 섞는다. 기다림 기호를 유료 방향 입력과 구분한다.',
 'SW08': '병아리 3마리와 수문 4쌍을 배치했다. 선택 경로에는 수문 교차 2회와 병아리 귀환 1회가 있으며 풍차 단계 변경은 없다.',
 'SW09': '풍차 3대의 단계 변경이 선택 경로에서 8회다. 여러 풍차의 설정값을 기억하는 부담과 단순 이동량을 구분한다.',
 'SW10': '풍차·무빙워크·양·별돌이 함께 있다. 제자리 조작과 자동 이동이 섞여 정답 입력 수와 실제 이동량이 달라진다.',
 'SW11': '병아리 2마리가 모두 귀환하는 짧은 예다. 긴 복도 없이 추종 순서와 일방통행을 조합하는 참고로 본다.',
 'SW12': '별 1개 목표에 소켓과 상자, 양 소모, 병아리 귀환을 배치했다. 적은 수집 목표에도 상태 의존을 만들 수 있다.',
 'SW13': '28개의 정적 바닥 칸에 상자·소켓·병아리·양을 배치한 압축형 예다. 작은 면적과 짧은 입력 수가 상태 단순함을 뜻하지 않는다.',
 'SW14': '병아리 3마리와 금속 상자 3개가 있고 정답은 22입력이다. 짧은 맵에서 통로 점유 순서를 비교하는 예다.',
 'SW15': '별돌/소켓 4개 항목과 병아리 2마리를 함께 놓았다. 선택 경로의 물체 밀기는 10회이며 두 병아리가 귀환한다.',
 'SW16': '양 5마리와 풀 12곳 중 선택 경로는 양 3마리를 소모한다. 별 4개 중 3개 목표여서 선택 자원과 선택 수집을 함께 본다.',
 'SW17': '양 2마리와 풀 6곳, 병아리와 상자를 둔다. 두 양을 소비하는 지점의 배정과 병아리 귀환을 맞추는 참고다.',
 'SW18': '병아리 4마리와 양 4마리, 풀 5곳이 있다. 선택 경로에서는 병아리 3마리가 귀환하고 양 1마리가 소모된다.',
 'SW19': '풍차 1대·반사판 3개와 양·수문·병아리를 결합했다. 반사판의 열린 면과 무료/동기 바람 순서를 유지해야 한다.',
 'ET-00': '셔터와 주사위, 방향성 도미노를 결합한다. 주사위 밀기 2회와 도미노 직접 밀기 1회로 목표 흡수와 조각 2개 접힘이 일어난다.',
 'ET-01': '카트 1대와 셔터, 주사위·도미노를 둔다. 선택 경로의 주사위 밀기는 5회이며 주사위 목표 흡수는 1회다.',
 'ET-03': '카트 2대의 선로와 주사위 접근을 결합한다. 선택 경로에서 카트가 플레이어를 미는 사건이 1회 있다.',
 'ET-04': '겁쟁이 인형 3개와 카트, 주사위를 둔다. 선택 경로는 주사위를 한 번 밀지만 목표 숫자 흡수 없이 승리한다.',
 'ET-05': '석상 4개·유리 2개·도미노 2개를 배치했다. 선택 경로는 한 번의 석상 돌진을 포함하지만 유리는 제거하지 않는다.',
}


def read(path):
    return json.loads(path.read_text(encoding='utf8'))


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')


def coord(p):
    return f"({p['x']},{p['y']})"


def geometry(layout):
    cells = {(p['x'], p['y']) for p in layout['staticWalkableCells']}
    degrees = Counter(sum((x + dx, y + dy) in cells for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]) for x, y in cells)
    remaining = set(cells); components = 0
    while remaining:
        components += 1; queue = [remaining.pop()]
        for x, y in queue:
            for p in [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]:
                if p in remaining: remaining.remove(p); queue.append(p)
    edges = sum(degree * n for degree, n in degrees.items()) // 2
    terrain = [p['position'] for p in layout['map']['terrain'] if not p['blocked']]
    return {'staticWalkableCells': len(cells), 'staticComponents': components,
            'staticBranchCells': degrees[3] + degrees[4], 'staticDeadEndCells': degrees[1],
            'staticCycleRank': edges - len(cells) + components,
            'floorBounds': {'minX': min(p['x'] for p in terrain), 'maxX': max(p['x'] for p in terrain),
                            'minY': min(p['y'] for p in terrain), 'maxY': max(p['y'] for p in terrain)}}


def main():
    snapshot = read(OUT / 'unity_snapshot.json'); trace = read(OUT / 'route_analysis.json')
    layouts = {m['id']: m for m in snapshot['maps'] if m['exists']}
    assert len(layouts) == 25 and set(layouts) == set(PATTERNS) == set(NOTES)
    assert all(not m['validationErrors'] for m in layouts.values())
    assert trace['sourceAssetsUnchanged']
    for path, digest in trace['engineSourceHashes'].items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    maps = []
    for replay in trace['maps']:
        layout = layouts[replay['id']]
        for suffix, key in [('', 'sourceSha256'), ('.meta', 'metaSha256')]:
            assert hashlib.sha256((ROOT / (layout['assetPath'] + suffix)).read_bytes()).hexdigest() == layout[key]
            assert layout[key] == replay[key]
        inputs = [s for s in replay['steps'] if s['inputCost'] == 1]
        categories = Counter(s['category'] for s in inputs)
        assert sum(categories.values()) == replay['distance']
        assert replay['won'] and replay['exitReached'] and replay['collected'] >= replay['target']
        events = Counter(e['event'] for s in replay['steps'] for e in s['committedEvents'])
        g = geometry(layout)
        summary = {'categories': dict(categories), 'baseWeightedScoreW0': sum(WEIGHTS[k] * n for k, n in categories.items()),
                   'noPlayerMoveInputs': sum(s['playerMoveSegments'] == 0 for s in inputs),
                   'multiplePlayerMoveSegmentInputs': sum(s['playerMoveSegments'] > 1 for s in inputs),
                   'playerManhattanTravel': sum(s['playerManhattanTravel'] for s in replay['steps']),
                   'explicitFreeWindSteps': sum(s['category'] == 'free_wind' and s['input'] != 'initial_wind' for s in replay['steps']),
                   'sheepConsumed': sum(s['sheepRemoved'] for s in replay['steps']),
                   'chicksReturned': events['ChickReturned'], 'diceGoalsAbsorbed': events['dice_goal_absorbed'],
                   'dominoPiecesFolded': events['this_domino_piece_fold_committed'],
                   'railCartPlayerPushes': events['rail_cart_player_pushed'],
                   'statueChargeBegins': events['statue_next_valid_action_charge_begin'],
                   'glassRemoved': sum(change['role'] == 'Glass' and not change['removedBefore'] and change['removedAfter']
                                       for s in replay['steps'] for change in s['stateChanges']),
                   'committedEventCounts': dict(events)}
        rules = deepcopy(layout['scenario'])
        rules.pop('beamPrefab', None)
        for d in rules['devices']: d['roleName'] = ROLES[d['role']]
        maps.append({'mapId': replay['id'], 'displayStageIds': layout['displayStages'], 'title': rules['title'],
            'assetPath': layout['assetPath'], 'sourceSha256': layout['sourceSha256'], 'metaSha256': layout['metaSha256'],
            'sourceWasUnsaved': replay['sourceWasUnsaved'], 'patternAnnotations': PATTERNS[replay['id']],
            'interpretation': NOTES[replay['id']], 'map': layout['map'], 'scenario': rules,
            'staticWalkableCells': layout['staticWalkableCells'], 'geometry': g,
            'provenRoute': {'status': 'optimal_confirmed', 'distance': replay['distance'], 'route': replay['route'],
                            'replaySummary': summary, 'steps': replay['steps']}})
    reference = {'format': 'ANIMOL_LLM_MapReference_v1', 'unityVersion': snapshot['unityVersion'],
        'coordinateSystem': 'Bottom-left (0,0); right +x, up +y; positions are integer objects {x,y}',
        'scope': 'SW00-SW19 and native ET-00/01/03/04/05; native ET-02 is absent, display ET-02 uses ET-00',
        'costModel': {'acceptedDirectionalInput': 1, 'windAutomaticStep': 0, 'rejectedInput': 0,
                      'stationaryPushCostsOne': True, 'rejectedInputAdvancesTurn': False},
        'weightModel': {'status': 'proposed_not_playtest_calibrated', 'baseWeights': WEIGHTS,
                        'modifiers': {'T': 0.5, 'H': 0.75, 'R': 0.75, 'C': 0.5}, 'perInputCap': 3.5,
                        'note': 'W0 computed; T/H/R/C require justified designer annotations and are not measured here'},
        'roleEnum': {str(k): v for k, v in ROLES.items()},
        'provenance': {'layout': 'Validation/MapDesign/unity_snapshot.json', 'replay': 'Validation/MapDesign/route_analysis.json',
                       'sourceAssetsUnchanged': True, 'engineSourceHashes': trace['engineSourceHashes'],
                       'routeStatisticsScope': 'One selected optimal route per map; another equally short route may have different action counts'},
        'maps': maps}
    published = deepcopy(reference)
    for m in published['maps']:
        m['provenRoute'].pop('steps')
        m['provenRoute']['replayEvidence'] = 'Validation/MapDesign/route_analysis.json'
    write(ROOT / 'Docs/ANIMOL_LLM_MAP_REFERENCE.json', published)
    stats = ['| 테마 | 실제 맵 | 격자 | 최단 입력 최소 / 중앙값 / 최대 |', '|---|---:|---|---:|']
    for prefix, label in [('SW', '별길 목장'), ('ET', '잔영 극장')]:
        theme_maps = [m for m in maps if m['mapId'].startswith(prefix)]
        vals = [m['provenRoute']['distance'] for m in theme_maps]
        sizes = Counter((m['map']['width'], m['map']['height']) for m in theme_maps)
        stats.append(f"| {label} | {len(theme_maps)} | " + ', '.join(f'{w}×{h} {n}개' for (w, h), n in sizes.items()) + f' | {min(vals)} / {statistics.median(vals):g} / {max(vals)} |')
    total = sum(m['provenRoute']['distance'] for m in maps)
    no_move = sum(m['provenRoute']['replaySummary']['noPlayerMoveInputs'] for m in maps)
    counts = Counter(s['category'] for m in maps for s in m['provenRoute']['steps'])
    stats += ['', f"선택한 최단 경로 25개에는 유효 방향 입력 {total:,}회와 명시적인 무료 바람 전이 {counts['free_wind']}회가 있다. 플레이어 이동 이벤트가 없는 유효 입력은 {no_move}회다. 최초 로드 시 바람 전이는 이번 배치에서는 모두 추가 이동 없이 끝났다.", '',
              '| 직접 조작 분류 | 유효 입력 횟수 |', '|---|---:|']
    stats += [f'| {LABELS[k]} | {counts[k]:,} |' for k in WEIGHTS if k != 'free_wind' and counts[k]]
    stats += ['', '이 행동 분포는 각 맵에서 선택한 정답 하나의 통계다. 같은 길이의 다른 정답이나 잘못된 분기의 분포까지 대표하지 않는다. 특히 ‘이동·기타’는 장치와 무관한 안전 이동만을 뜻하지 않는다.']
    guide_path = ROOT / 'Docs/ANIMOL_LLM_MAP_DESIGN_GUIDE.md'
    guide = guide_path.read_text(encoding='utf8')
    guide, n = re.subn(r'<!-- DESIGN_STATS_START -->.*?<!-- DESIGN_STATS_END -->',
                      '<!-- DESIGN_STATS_START -->\n' + '\n'.join(stats) + '\n<!-- DESIGN_STATS_END -->', guide, flags=re.S)
    assert n == 1
    guide_path.write_text(guide, encoding='utf8')
    lines = ['# ANIMOL 맵별 제작 참고표', '',
        '현재 배치와 선택한 최단 경로의 행동을 함께 비교한다. 패턴 ID와 제안 가중치의 뜻은 [제작 가이드](ANIMOL_LLM_MAP_DESIGN_GUIDE.md)를 따른다. 전체 좌표·장치 설정·정답은 [참고 JSON](ANIMOL_LLM_MAP_REFERENCE.json), 입력별 상태 변화는 [재생 JSON](Validation/MapDesign/route_analysis.json)에 있다.', '',
        'L은 게임 입력 비용, W0는 제안 계수로 계산한 기본 조작량 점수다. W0에는 사고 부담 수정치를 자동으로 붙이지 않았다. 정적 바닥 수는 장식 점유를 제외하며, 원래 배치 보고서의 지형 바닥 수는 장식 아래의 지형도 포함한다. 정적 영역과 막다른 길은 동적 통행 판정과 다르다.', '',
        '| 원본 → 화면 | 최단 L | W0 | 이동 없는 유효 입력 | 패턴 |', '|---|---:|---:|---:|---|']
    for m in maps:
        p = m['provenRoute']; s = p['replaySummary']; mid = m['mapId']
        lines.append(f"| [{mid}](#{mid.lower()}) → {', '.join(m['displayStageIds'])} | {p['distance']} | {s['baseWeightedScoreW0']:g} | {s['noPlayerMoveInputs']} | {', '.join(m['patternAnnotations'])} |")
    lines += ['']
    for m in maps:
        mid = m['mapId']; data = m['map']; g = m['geometry']; p = m['provenRoute']; s = p['replaySummary']
        devices = Counter(d['roleName'] for d in m['scenario']['devices'])
        families = Counter(d['family'] for d in m['scenario']['ranchV13Art'])
        mechanism = ', '.join(f'{k} {v}' for k, v in (devices + families).items())
        lines += [f'## {mid}', '', f"화면 **{', '.join(m['displayStageIds'])}**. 현재 제목: {m['title']}. {m['interpretation']}", '',
            f"{data['width']}×{data['height']} 격자, 시작 {coord(data['start'])}, 출구 {coord(data['exit'])}, 별 {len(data['bubbles'])}개 / 목표 {data['targetBubbles']}개. 정적 바닥 {g['staticWalkableCells']}칸, 연결 영역 {g['staticComponents']}개, 분기 칸 {g['staticBranchCells']}개, 막다른 칸 {g['staticDeadEndCells']}개다.", '',
            f'장치: {mechanism}.', '',
            f"최단 **{p['distance']}입력**, 기본 조작량 **W0={s['baseWeightedScoreW0']:g}**. " + ', '.join(f'{LABELS[k]} {v}회' for k, v in s['categories'].items()) + '.', '',
            f"양 소모 {s['sheepConsumed']}마리, 병아리 귀환 {s['chicksReturned']}회, 주사위 목표 흡수 {s['diceGoalsAbsorbed']}회, 도미노 접힘 {s['dominoPiecesFolded']}조각, 유리 제거 {s['glassRemoved']}개. 이동 없는 유효 입력 {s['noPlayerMoveInputs']}회, 명시 무료 바람 {s['explicitFreeWindSteps']}회다.", '',
            f"[전체 장치 좌표](ANIMOL_MAP_LAYOUT_REPORT.md#{mid.lower()}) · [입력별 판정 기록](Validation/MapDesign/route_analysis.json)", '',
            f'![{mid} 현재 배치도](Validation/MapLayoutReport/Diagrams/{mid}.png)', '', '```text', p['route'], '```', '']
    lines += ['## 해석 범위', '',
        'SW00~19는 화면 SW01~20이다. 원본 ET-02는 없고 화면 ET-02는 위 ET-00이다. 원본 ET-05의 미저장 표시는 유지했으며 이 표는 현재 로드된 상태의 판정 재생에 근거한다.', '',
        '이 표의 소비·귀환·목표 흡수 횟수는 선택한 정답에서의 실제 사건이다. 설치된 모든 장치가 필수라는 증거로 확대하지 않는다. 주사위 면·카트 위상·병아리 이력 등 자세한 연결 조건은 JSON과 현재 장치 판정을 확인한다.', '']
    (ROOT / 'Docs/ANIMOL_MAP_DESIGN_ATLAS.md').write_text('\n'.join(lines), encoding='utf8')
    assert total == trace['acceptedDirectionalInputs'] == 1069 and counts['free_wind'] == 3 and no_move == 177
    assert all(not m['scenario']['goals'] for m in maps)
    assert next(m for m in maps if m['mapId'] == 'ET-05')['provenRoute']['replaySummary']['glassRemoved'] == 0
    audit = {'maps': len(maps), 'sourceAndMetaHashesChecked': 50, 'sourceAssetsChanged': 0,
             'acceptedDirectionalInputs': total, 'explicitFreeWindSteps': counts['free_wind'],
             'noPlayerMoveInputs': no_move, 'weightModel': 'proposed_not_playtest_calibrated',
             'fullShortestSearchesRepeated': False, 'allRecordedRoutesReplayedInUnity': True}
    write(OUT / 'audit.json', audit)
    build_zip()
    print(json.dumps(audit))


def build_zip():
    names = ['ANIMOL_LLM_MAP_DESIGN_GUIDE.md', 'ANIMOL_LLM_MAP_DESIGN_PROMPT.md', 'ANIMOL_MAP_DESIGN_ATLAS.md',
             'ANIMOL_LLM_MAP_REFERENCE.json', 'ANIMOL_MAP_LAYOUT_REPORT.md', 'ANIMOL_DEVICE_RULES_CURRENT.md',
             'STARWAY_SHORTEST_PATHS.md', 'ECHO_THEATER_SHORTEST_PATHS.md']
    members = {f'Docs/{name}': (ROOT / 'Docs' / name).read_bytes() for name in names}
    for path in sorted((ROOT / 'Docs/Validation/MapLayoutReport/Diagrams').glob('*.png')):
        members[path.relative_to(ROOT).as_posix()] = path.read_bytes()
    for name in ['audit.json', 'route_analysis.json']:
        members['Docs/Validation/MapDesign/' + name] = (OUT / name).read_bytes()
    # The portable bundle keeps available relative links. Repository-only source and
    # historical evidence links become readable paths; full current coordinates are in JSON.
    for name in list(members):
        if not name.endswith('.md'): continue
        def portable(match):
            label, target = match.groups()
            if target.startswith(('http:', 'https:', '#')): return match.group(0)
            path = (ROOT / name).parent.joinpath(target.split('#')[0]).resolve()
            try: key = path.relative_to(ROOT).as_posix()
            except ValueError: return label
            return match.group(0) if key in members else f'{label}（저장소 경로 `{target}`）'
        members[name] = re.sub(r'\[([^\]]*)\]\(([^)]+)\)', portable, members[name].decode('utf8')).encode('utf8')
    members['README.md'] = ('# ANIMOL 맵 제작 참고 자료\n\n'
        'Docs/ANIMOL_LLM_MAP_DESIGN_PROMPT.md의 요청문으로 시작하고 가이드와 참고 JSON을 함께 읽으세요.\n\n'
        '25개 실제 맵의 좌표·장치·선택한 최단 정답은 참고 JSON에, 입력별 판정은 Docs/Validation/MapDesign/route_analysis.json에 포함했습니다. '
        '행동 가중치는 제안값이며 게임 코스트는 방향 입력 1, 무료 바람 0입니다.\n\n'
        '이 묶음은 Unity 프로젝트나 자동 가져오기 패키지가 아닙니다. 원본 소스 및 과거 검증 링크는 '
        '저장소 경로로 표시했습니다. PNG는 현재 좌표로 만든 배치도입니다.\n').encode('utf8')
    manifest = {name: hashlib.sha256(data).hexdigest() for name, data in sorted(members.items())}
    members['MANIFEST.json'] = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf8')
    path = ROOT / 'Docs/Exports/ANIMOL_LLM_Map_Design.zip'; path.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, data in sorted(members.items()):
            info = zipfile.ZipInfo(name, date_time=(2026, 11, 9, 0, 0, 0)); info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        for name, digest in manifest.items(): assert hashlib.sha256(archive.read(name)).hexdigest() == digest


if __name__ == '__main__':
    main()
