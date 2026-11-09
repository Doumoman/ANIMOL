"""Build repository map placement documentation from ExportMapLayouts.Run output.
Requires Pillow and PyYAML. Never writes Unity assets.
"""
import collections
import hashlib
import json
import math
import pathlib
import re
import struct
from datetime import datetime, timedelta

import yaml
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Validation/MapLayoutReport'
DATA = json.loads((OUT / 'unity_snapshot.json').read_text(encoding='utf8'))
ROLES = {1: '별돌', 2: '덩굴 다리', 3: '도약대', 4: '셔터', 5: '이탈 가시',
         6: '이동 발판', 7: '무빙워크', 8: '책장', 9: '일방통행 문', 10: '광학 장치',
         11: '배', 12: '통', 13: '도미노', 14: '자석', 15: '폭탄', 16: '포털',
         17: '돌진 석상', 18: '반사판', 19: '브레이크', 20: '바람 발생기', 21: '유리벽',
         22: '주사위', 23: '겁쟁이 인형', 24: '병아리', 25: '얼음', 100: '벽',
         101: '금 간 벽', 102: '나무 상자', 103: '금속 상자'}
FAMILIES = {'sheep': '양', 'sluice': '수문', 'tall_grass': '키 큰 풀'}
MATERIALS = ['풀', '돌', '모래', '극장 무대 바닥', '극장 카펫', '극장 목재 통로']
COLORS = ['#dceecb', '#dce1e8', '#f7e1b6', '#eee0c4', '#efd0d7', '#e5c8a8']
ARROWS = {(1, 0): '→', (-1, 0): '←', (0, 1): '↑', (0, -1): '↓'}


def xy(p):
    return p['x'], p['y']


def coord(p):
    return f"({p['x']},{p['y']})"


def coords(points):
    return ', '.join(map(coord, points)) or '없음'


def esc(value):
    return str(value).replace('|', '\\|').replace('\n', ' ')


def table(headers, rows):
    return ['| ' + ' | '.join(headers) + ' |', '|' + '|'.join(['---'] * len(headers)) + '|'] + [
        '| ' + ' | '.join(map(esc, row)) + ' |' for row in rows] + ['']


def role(d):
    if d['role'] == 1 and d['variant'] == 'socket': return '별 홈'
    if d['role'] == 22 and d['variant'] in ('target', 'socket'): return '주사위 목표'
    if d['variant'] == 'plate': return ROLES[d['role']] + ' 압력판'
    return ROLES.get(d['role'], str(d['role']))


def inside(m, p):
    return 0 <= p['x'] < m['width'] and 0 <= p['y'] < m['height']


def components(points):
    remaining = set(points); count = 0
    while remaining:
        count += 1; queue = [remaining.pop()]
        for x, y in queue:
            for p in [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]:
                if p in remaining: remaining.remove(p); queue.append(p)
    return count


def disk_docs(path):
    text = path.read_text(encoding='utf-8-sig')
    text = re.sub(r'(?m)^(\s*initialDiceFaces: )([0-9a-fA-F]+)\s*$', r"\1'\2'", text)
    parts = re.split(r'^--- !u!\d+ &(-?\d+)\s*$', text, flags=re.M)
    return {parts[i]: yaml.safe_load(parts[i + 1])['MonoBehaviour'] for i in range(1, len(parts), 2)}


def equal(a, b):
    if a is None: a = ''
    if b is None: b = ''
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return math.isclose(a, b, abs_tol=1e-6)
    if isinstance(a, dict) and isinstance(b, dict):
        return a.keys() == b.keys() and all(equal(a[k], b[k]) for k in a)
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(equal(x, y) for x, y in zip(a, b))
    return a == b


def saved_comparison(e):
    docs = disk_docs(ROOT / e['assetPath'])
    disk_map = next(d for d in docs.values() if d.get('m_EditorClassIdentifier', '').endswith('.PuzzleMap'))
    reference = disk_map['scenario']
    if str(reference['fileID']) not in docs:
        docs = disk_docs(ROOT / e['map']['scenario'])
    disk_rules = docs[str(reference['fileID'])]
    differences = []
    for key in ['width', 'height', 'start', 'exit', 'targetBubbles', 'terrain', 'devices', 'bubbles', 'cameraSettings']:
        if not equal(disk_map.get(key), e['map'][key]): differences.append('map.' + key)
    for key in ['title', 'themeId', 'echoTheaterV3Rules', 'railPaths', 'nests', 'ranchV13Art', 'water', 'goals', 'costLimit']:
        default = [] if isinstance(e['scenario'][key], list) else None
        if not equal(disk_rules.get(key, default), e['scenario'][key]): differences.append('scenario.' + key)
    saved, live = [], []
    for original in disk_rules['devices']:
        d = dict(original); d.pop('visualPrefab', None); d.setdefault('bookshelfRotation', 0)
        raw = d.get('initialDiceFaces', '')
        if isinstance(raw, str): d['initialDiceFaces'] = list(struct.unpack('<' + 'i' * (len(raw) // 8), bytes.fromhex(raw)))
        saved.append(d)
    for original in e['scenario']['devices']:
        d = dict(original); d.pop('visualPrefab', None); d.pop('initialFootprint', None); d.pop('initialRotationArea', None); live.append(d)
    if not equal(saved, live): differences.append('scenario.devices')
    for key in ['decorations']:
        saved = [{k: v for k, v in d.items() if k != 'sprite'} for d in disk_map[key]]
        live = [{k: v for k, v in d.items() if k not in ('sprite', 'occupiedCells')} for d in e['map'][key]]
        if not equal(saved, live): differences.append('map.' + key)
    return differences


def rails(s):
    result = [(p['id'], p['cells']) for p in s['railPaths']]
    for d in s['devices']:
        if d['role'] == 6 and d['cells'] and not any(d['position'] in cells for _, cells in result):
            result.append((d['id'] + ' (cells)', d['cells']))
    return result


def nests(s):
    return list({xy(p): p for p in s['nests'] + [d['target'] for d in s['devices']
                  if d['role'] == 24 and xy(d['target']) != (0, 0)]}.values())


def settings(d, s):
    result = []
    if d['role'] in {3, 6, 7, 9, 10, 13, 14, 16, 17, 20, 23, 24}:
        result.append('방향 ' + ARROWS.get(xy(d['direction']), coord(d['direction'])))
    if d['variant']: result.append('변형 ' + d['variant'])
    if d['role'] == 1:
        result.append('번호 ' + str(d['number']))
        if d['variant'] == 'socket':
            reward = d.get('socketReward', 0)
            if reward == 1: result.append('다리 공개 ' + d['socketBridgeId'])
            if reward == 2: result.append('별 공개 ' + coord(d['socketBubbleCell']))
    if d['role'] == 22:
        if d['variant'] in ('target', 'socket'):
            result.append('목표 윗면 ' + str(d['number']))
            result.append('진입 방향 ' + ARROWS.get(xy(d['direction']), coord(d['direction'])))
        else: result.append('면 [위,아래,북,남,동,서]=' + ','.join(map(str, d['initialDiceFaces'])))
    if d['role'] == 4: result.append('초기 열림' if d['active'] else '초기 닫힘')
    if d['role'] == 17: result.append('기본 돌진 범위 ' + str(d['number']))
    if d['role'] == 18: result.append('회전값 ' + str(d['orientation']))
    if d['role'] == 24:
        result.append('추적 지연 ' + str(d['number']))
        if xy(d['target']) != (0, 0): result.append('둥지 ' + coord(d['target']))
    if d['role'] == 16: result.append('목적지 ' + coord(d['target']))
    if d['role'] == 15: result.append('폭발 상대 칸 ' + coords(d['blastOffsets']))
    if d['cells']: result.append('cells ' + coords(d['cells']))
    if d['group']: result.append('연결 그룹 ' + d['group'])
    if d['active']: result.append('초기 active=true')
    return '; '.join(result) or '기본값'


def diagram(e):
    m, s = e['map'], e['scenario']; cell = 34; left, top = 46, 54
    width, height = m['width'], m['height']
    im = Image.new('RGB', (left + width * cell + 22, top + height * cell + 50), '#ffffff')
    dr = ImageDraw.Draw(im)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 10)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 18)
    small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 9)
    dr.text((left, 12), f"{e['id']}   {width} x {height}   S start / E exit / B star", font=title, fill='#172033')
    def box(p):
        x, y = xy(p); a = left + x * cell; b = top + (height - 1 - y) * cell
        return a, b, a + cell, b + cell
    def center(p):
        a, b, _, _ = box(p); return a + cell / 2, b + cell / 2
    terrain = {xy(t['position']): t for t in m['terrain']}
    for y in range(height):
        dr.text((14, top + (height - 1 - y) * cell + 10), str(y), font=font, fill='#475569')
        for x in range(width):
            p = {'x': x, 'y': y}; t = terrain.get((x, y))
            fill = '#f5f7fa' if t is None else '#596579' if t['blocked'] else COLORS[t['material']]
            dr.rectangle(box(p), fill=fill, outline='#d7dde4')
            if t and t['blocked']: dr.text((box(p)[0] + 13, box(p)[1] + 9), '#', font=font, fill='white')
    for x in range(width): dr.text((left + x * cell + 11, top + height * cell + 4), str(x), font=font, fill='#475569')
    for p in e['decorationOccupiedCells']:
        a, b, c, d = box(p); dr.rectangle((a + 3, b + 3, c - 3, d - 3), fill='#b9c2cb')
    for p in s['water']:
        if inside(m, p): dr.rectangle(box(p), fill='#bfe7f3', outline='#60a5bb')
    for _, cells in rails(s):
        positions = [center(p) for p in cells if inside(m, p)]
        if len(positions) > 1: dr.line(positions, fill='#137e88', width=3)
    labels = collections.defaultdict(list)
    def mark(p, label, color, fill=None):
        if not inside(m, p): return
        if fill:
            a, b, c, d = box(p); dr.rectangle((a + 2, b + 2, c - 2, d - 2), fill=fill)
        labels[xy(p)].append((label, color))
    for i, d in enumerate(s['devices'], 1):
        for p in d['initialFootprint']: mark(p, f'D{i}', '#1143a3')
    for i, a in enumerate(s['ranchV13Art'], 1):
        if a['family'] == 'sluice':
            b = a['pondB'] if a['hasPondB'] else dict(zip(('x', 'y'), (a['position']['x'] + a['direction']['x'], a['position']['y'] + a['direction']['y'])))
            mark(a['position'], f'R{i}A', '#693c00', '#a9dcf2' if a['rightState'] else '#f1d7a2')
            mark(b, f'R{i}B', '#693c00', '#f1d7a2' if a['rightState'] else '#a9dcf2')
        else: mark(a['position'], f'R{i}', '#9a2862')
    for i, a in enumerate(m['decorations'], 1): mark(a['position'], f'X{i}', '#313b45')
    for i, p in enumerate(m['bubbles'], 1): mark(p, f'B{i}', '#a75b00')
    for p in nests(s): mark(p, 'N', '#167547')
    mark(m['start'], 'S', '#026535'); mark(m['exit'], 'E', '#af270a')
    for (x, y), marks in labels.items():
        a, b, c, d = box({'x': x, 'y': y}); count = len(marks)
        shown = marks if count <= 3 else marks[:2] + [(f'+{count - 2}', '#475569')]
        for index, (label, color) in enumerate(shown):
            ft = font if len(label) < 4 else small
            bounds = dr.textbbox((0, 0), label, font=ft); tw = bounds[2] - bounds[0]
            xx = a + (cell - tw) / 2; yy = b + (cell - len(shown) * 11) / 2 + index * 11 - 1
            dr.rectangle((xx - 1, yy + 1, xx + tw + 1, yy + 12), fill='#ffffff')
            dr.text((xx, yy), label, font=ft, fill=color)
    dr.text((left, top + height * cell + 25), 'D device | R ranch | X decor | N nest | # blocked | blue pond = wet', font=font, fill='#475569')
    dest = OUT / 'Diagrams' / (e['id'] + '.png'); dest.parent.mkdir(exist_ok=True); im.save(dest)


def main():
    existing = [e for e in DATA['maps'] if e['exists']]
    audit = {'sourceAssetsSaved': False, 'requested': 26, 'existing': len(existing), 'missing': [], 'maps': []}
    for e in DATA['maps']:
        if not e['exists']:
            audit['missing'].append(e['id']); continue
        for suffix, key in [('', 'sourceSha256'), ('.meta', 'metaSha256')]:
            assert hashlib.sha256((ROOT / (e['assetPath'] + suffix)).read_bytes()).hexdigest() == e[key], e['id']
        e['savedDifferences'] = saved_comparison(e)
        m, s = e['map'], e['scenario']
        e['floor'] = sum(not t['blocked'] and inside(m, t['position']) for t in m['terrain'])
        e['blocked'] = sum(t['blocked'] and inside(m, t['position']) for t in m['terrain'])
        e['components'] = components(map(xy, e['staticWalkableCells']))
        audit['maps'].append({'id': e['id'], 'sourceSha256': e['sourceSha256'], 'metaSha256': e['metaSha256'],
                             'sourceUnchanged': True, 'mapDirty': e['mapDirty'], 'scenarioDirty': e['scenarioDirty'],
                             'savedLayoutDifferences': e['savedDifferences'], 'validationErrors': e['validationErrors']})
        diagram(e)
    lines = ['# 별길 목장과 잔영 극장 맵 배치 보고서', '',
             '2026-11-10 정본 커밋 `a2f825e09`의 별길 목장 SW00~SW19·SW21 21개와 잔영 극장 ET-11까지 현존 파일 11개, 총 32개가 대상이다. ET-06은 삭제 상태를 유지한다. SW20은 없고 SW21은 현재 노트북 20개 진입 목록 밖이다. 현재 파일 ID와 화면 연결을 구분한다.', '',
             '좌표는 왼쪽 아래가 `(0,0)`, 오른쪽이 +x, 위가 +y다. 배치도는 현재 Unity 데이터로 만든 초기 배치도이며 플레이 화면 캡처가 아니다. `D`는 시나리오 장치, `R`은 양·수문·풀, `X`는 장식 인스턴스, `B`는 별, `S/E`는 시작/출구, `N`은 둥지다. 같은 번호가 여러 칸에 표시되면 하나의 다중 칸 객체다.', '',
             '32개 모두 미저장 변경이 없으며 정본 GUID·SHA-256과 일치한다. 원본 저장은 실행하지 않았다. 별길 목장 SW00~SW19는 화면 SW01~SW20에 대응한다. SW03은 이전 SW05의 GUID, 새 배치 SW05는 이전 SW03의 GUID를 이어받았다. [현재 최단 경로와 변경 대조](STARWAY_SHORTEST_PATHS.md), [잔영 극장 최단 경로](ECHO_THEATER_SHORTEST_PATHS.md)를 함께 참조한다.', '',
             '바닥 수에는 장치가 차지한 바닥도 포함한다. `#`는 기록된 통행 불가 지형, 연한 회색 빈칸은 지형 없음, 회색 내부 칸은 장식 점유다. 정적 연결 영역은 `PuzzleMap.Walkable` 기준의 상하좌우 연결 수이며, 다리·수문·물체 이동을 반영한 도달 가능 판정은 아니다.', '',
             '## 전체 배치 요약', '']
    for label, prefix in [('별길 목장', 'SW'), ('잔영 극장', 'ET')]:
        lines += ['### ' + label, '']
        rows = []
        for e in DATA['maps']:
            if not e['id'].startswith(prefix): continue
            if not e['exists']:
                rows.append([e['id'], '원본 없음', '—', '—', '—', '—', '—']); continue
            m, s = e['map'], e['scenario']
            rows.append([f"[{e['id']}](#{e['id'].lower()}) → {', '.join(e['displayStages']) or '미연결'}",
                         f"{m['width']}×{m['height']}", coord(m['start']) + ' → ' + coord(m['exit']),
                         f"{m['targetBubbles']} / {len(m['bubbles'])}", f"{e['floor']} / {e['blocked']}",
                         f"{len(s['devices'])} / {len(s['ranchV13Art'])} / {len(m['decorations'])}", e['components']])
        lines += table(['원본 → 화면 ID', '격자', '시작 → 출구', '목표 / 초기 별', '바닥 / 막힘', 'D / R / X', '정적 영역'], rows)
    lines += ['## 맵별 배치', '']
    for e in DATA['maps']:
        lines += ['### ' + e['id'], '']
        if not e['exists']:
            lines += ['ET-06 파일과 메타의 삭제 상태를 유지했다. 현재 실제 화면 연결은 ET-06→원본 ET-01이며, 삭제된 파일을 복원하지 않았다.', '']; continue
        m, s = e['map'], e['scenario']
        lines += [f"시나리오 제목: **{s['title']}**. 화면 연결: **{', '.join(e['displayStages']) or '없음'}**. [원본 에셋](../{e['assetPath']}).", '']
        material = collections.Counter(MATERIALS[t['material']] for t in m['terrain'] if not t['blocked'])
        lines += [f"격자 {m['width']}×{m['height']}, 바닥 {e['floor']}칸({', '.join(f'{k} {v}' for k, v in material.items())}), 막힌 지형 {e['blocked']}칸, 지형 없는 칸 {m['width'] * m['height'] - len(m['terrain'])}칸이다. 정적 바닥은 {e['components']}개 영역으로 나뉜다.", '',
                  f"시작 {coord(m['start'])}, 출구 {coord(m['exit'])}. 별은 {coords(m['bubbles'])}에 있으며 수집 목표는 {m['targetBubbles']}개다.", '',
                  f"![{e['id']} 좌표별 배치도](Validation/MapLayoutReport/Diagrams/{e['id']}.png)", '']
        if s['devices']:
            lines += table(['표시', '장치 ID', '종류', '기준 좌표', '초기 점유 칸', '방향과 연결 설정'], [
                [f'D{i}', d['id'], role(d), coord(d['position']), coords(d['initialFootprint']), settings(d, s)]
                for i, d in enumerate(s['devices'], 1)])
        else: lines += ['시나리오 장치 D는 없다.', '']
        shelves = [d for d in s['devices'] if d.get('initialRotationArea')]
        if shelves:
            lines += table(['책장 ID', '회전 방향', '초기 회전 영역'], [[d['id'], '시계' if d['bookshelfRotation'] == 0 else '반시계', coords(d['initialRotationArea'])] for d in shelves])
        if s['ranchV13Art']:
            rows = []
            for i, a in enumerate(s['ranchV13Art'], 1):
                if a['family'] == 'sluice':
                    b = a['pondB'] if a['hasPondB'] else {'x': a['position']['x'] + a['direction']['x'], 'y': a['position']['y'] + a['direction']['y']}
                    detail = 'A ' + coord(a['position']) + ' ↔ B ' + coord(b) + '; 초기 물 ' + coord(a['position'] if a['rightState'] else b)
                elif a['family'] == 'sheep': detail = '초기 방향 ' + ARROWS.get(xy(a['direction']), coord(a['direction']))
                else: detail = '양과의 상호작용 대상인 풀 칸'
                rows.append([f'R{i}', FAMILIES[a['family']], coord(a['position']), detail])
            lines += table(['표시', '목장 기믹', '기준 좌표', '세부 배치'], rows)
        if nests(s): lines += ['둥지: ' + coords(nests(s)) + '.', '']
        if rails(s): lines += table(['레일 ID', '저장된 경로 순서'], [[name, coords(cells)] for name, cells in rails(s)])
        if s['water']: lines += ['물 칸: ' + coords(s['water']) + '.', '']
        if m['decorations']:
            lines += table(['표시', '장식 종류 또는 스프라이트', '기준 좌표', '실제 점유 칸'], [
                [f'X{i}', a['visualFamily'] or pathlib.PurePosixPath(a['sprite']).name, coord(a['position']), coords(a['occupiedCells'])]
                for i, a in enumerate(m['decorations'], 1)])
        if s['goals']:
            names = ['활성', '제거', '위치', '주사위 윗면', '방문']
            lines += table(['추가 목표', '대상 ID', '좌표', '숫자'], [[names[g['kind']], g['deviceId'], coord(g['cell']), g['number']] for g in s['goals']])
        else: lines += ['추가 장치 목표(goals)는 없으며, 별 수집 목표와 출구가 승리 조건이다.', '']
        lines += [f"원본 시나리오 costLimit: {s['costLimit']}" + (' (제한 없음). 실제 HUD 예산에 적용되는 별도 기록과 구분한다.' if s['costLimit'] == 0 else '.'), '']
        if e['validationErrors']: lines += ['배치 유의 사항: ' + '; '.join(e['validationErrors']), '']
    lines += ['## 현재 규칙과 연결 해석', '',
              '잔영 극장의 돌진 석상은 유리를 부수되 유리가 있던 칸에 들어가지 않고 바로 앞에서 이번 돌진을 끝낸다. 이후 재조준·재돌진할 수 있다. 장치의 방향·번호·변형은 위 표의 저장값을 따른다. 자세한 판정은 [현재 장치 규칙](ANIMOL_DEVICE_RULES_CURRENT.md)을 참조한다.', '',
              '초기 별 배치 수와 필요한 수집 수는 각 맵 표의 현재 값을 따른다. 표의 초기 점유 칸과 바닥 통계는 이동 후 상태나 해답 경로를 뜻하지 않는다.', '',
              '현재 [별길 목장 최단 경로](STARWAY_SHORTEST_PATHS.md)와 [잔영 극장 최단 경로](ECHO_THEATER_SHORTEST_PATHS.md)는 별도 카탈로그에 기록한다. 경로·최단성 근거·공개 API 재생 검증은 별도 보고서에 기록했다.', '',
              '## 자료와 검증', '',
              f"Unity {DATA['unityVersion']}에서 실제 로드된 MapData와 Scenario를 읽었다. 32개 맵의 ValidateMap 결과는 모두 오류 0개이며, 이는 배치 데이터 검사 결과다. 원본 에셋과 메타의 SHA-256, 미저장 표시, 저장본과의 비교는 [검증 기록](Validation/MapLayoutReport/audit.json), 전체 좌표와 원본 필드는 [데이터 스냅샷](Validation/MapLayoutReport/unity_snapshot.json)에 있다.", '',
              '원본 맵·메타·작성된 verificationRoute를 저장하거나 수정하지 않았다. 게임 완주나 모바일 표시 검증을 배치 검사 결과에 포함하지 않는다.', '']
    assert len(existing) == 32 and audit['missing'] == ['ET-06']
    assert all(not e['savedDifferences'] for e in existing), 'Saved/layout differences need explicit reporting'
    assert all(not e['validationErrors'] for e in existing), 'Validation notes need editorial review'
    (OUT / 'audit.json').write_text(json.dumps(audit, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    (ROOT / 'Docs/ANIMOL_MAP_LAYOUT_REPORT.md').write_text('\n'.join(lines), encoding='utf8')
    print(json.dumps({'maps': len(existing), 'diagrams': len(list((OUT / 'Diagrams').glob('*.png'))), 'reportLines': len(lines), 'nativeAssetsChanged': 0}))


if __name__ == '__main__':
    main()
