"""Publish source-pinned theater searches only after native Unity replays pass."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRIOR_FOLDER = ROOT / "Docs/Validation/EchoThrough18"
CATALOG = ROOT / "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterShortestPaths.json"
DOC = ROOT / "Docs/ECHO_THEATER_SHORTEST_PATHS.md"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf8")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main(through=18):
    FOLDER = ROOT / f"Docs/Validation/EchoThrough{through}"
    relative_folder = FOLDER.relative_to(ROOT).as_posix()
    doc_folder = relative_folder.removeprefix("Docs/")
    inventory = read(FOLDER / "inventory.json")
    assert read(FOLDER / "replay_status.json")["status"] == "completed"
    replays = {r["id"]: r for r in read(FOLDER / "native_replay.json")}
    validation_paths = {digest(p): p for folder in {PRIOR_FOLDER, FOLDER} for p in folder.glob("transition_verification*.json")}
    validations = {key: read(path) for key, path in validation_paths.items()}
    used_builds = set()
    entries = []
    for item in inventory["maps"]:
        map_id, path = item["id"], ROOT / item["path"]
        assert digest(path) == item["sha256"] and digest(Path(str(path) + ".meta")) == item["metaSha256"]
        search_path = item.get("searchEvidence", f"{relative_folder}/Search/{map_id}.json")
        result = read(ROOT / search_path)
        used_builds.add(result["verificationSha256"])
        verified = validations[result["verificationSha256"]]
        assert verified["passed"] and result["sourceHashes"] == verified["sourceHashes"]
        assert result["inputSha256"] == verified["inputSha256"]
        replay = replays[map_id]
        assert result["sourceSha256"] == replay["sourceSha256"] == item["sha256"]
        assert result["status"] == replay["status"] and replay["sourceUnchanged"]
        status = result["status"]
        if status == "solved":
            assert replay["won"] and replay["exitReached"] and replay["collected"] >= replay["target"]
            assert replay["distance"] == result["distance"] == len(replay["routeNESW"])
            assert len(replay["steps"]) == result["distance"]
            assert replay["route"] == result["route"]
        else:
            assert status in {"limit", "cancelled", "stale", "unreachable"} and result["distance"] == -1
        entries.append({
            "mapId": map_id, "mapAsset": item["path"], "mapGuid": item["guid"],
            "mapSha256": item["sha256"], "solverVersion": 10 if "single-die pattern database" in result["algorithm"] else 9,
            "displayStageIds": replay["displayStageIds"], "status": status,
            "distance": result["distance"], "route": replay.get("route", ""),
            "routeNESW": replay.get("routeNESW", ""), "algorithm": result["algorithm"],
            "expanded": result["expanded"], "visited": result["visited"],
            "elapsedSeconds": result["elapsedSeconds"], "detail": result["detail"],
            "evidence": f"{relative_folder}/Replay/{map_id}.json",
            "searchEvidence": search_path,
            "verificationSha256": result["verificationSha256"],
        })
    prior = FOLDER / "prior_catalog.json"
    if not prior.exists():
        prior.write_bytes(CATALOG.read_bytes())
    outside_scope = [e for e in read(CATALOG)["maps"] if int(e["mapId"].split("-")[1]) > through]
    solved = [e for e in entries if e["status"] == "solved"]
    unreachable = [e for e in entries if e["status"] == "unreachable"]
    pending = [e for e in entries if e["status"] not in {"solved", "unreachable"}]
    write(CATALOG, {
        "formatVersion": 3, "canonicalCommit": inventory["mapCommit"],
        "scope": inventory["scope"],
        "costModel": "One accepted directional input costs 1, including stationary pushes; these maps have no free wind edges.",
        "rules": "Native two-state bookshelf return sweep; statues resolve before eligible carts.",
        "validation": f"{relative_folder}/native_replay.json",
        "maps": entries + outside_scope,
    })
    # One exact input/fixture dataset serves the source-pinned search builds.
    fixture_metadata = {}
    for name in ["input", "fixtures"]:
        source = ROOT / f"Temp/EchoThrough{through}Export/{name}.json"
        data = source.read_bytes()
        for path in FOLDER.glob("transition_verification*.json"):
            verified = read(path)
            assert hashlib.sha256(data).hexdigest() == verified["inputSha256" if name == "input" else "fixtureSha256"]
        (FOLDER / (name + ".json.gz")).write_bytes(gzip.compress(data, mtime=0))
        fixture_metadata[name] = {"sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)}
    datasets = {validations[key]["inputSha256"]: validations[key] for key in used_builds}
    parity_states = sum(v["states"] for v in datasets.values())
    parity_edges = sum(v["edges"] for v in datasets.values())
    lines = [
        f"# 잔영 극장 ET-{through}까지의 최단 입력 경로", "",
        f"맵 원본 커밋 `{inventory['mapCommit'][:9]}`을 기준으로 존재하는 **{len(entries)}개 맵**을 계산했다. "
        f"**최단 확정 {len(solved)}개, 도달 불가 {len(unreachable)}개, 미확정 {len(pending)}개**다.", "",
        "ET-06 파일은 삭제된 상태를 유지했다. 화면 번호와 파일 ID는 다를 수 있으므로 아래의 실제 MapSet 연결을 함께 표시한다. "
        "계산 결과는 MapData와 별도의 경로 카탈로그에 기록한다.", "",
        f"[맵·GUID·원본 해시]({doc_folder}/inventory.json) · "
        f"[Unity 실제 경로 재생]({doc_folder}/native_replay.json)", "",
        "## 현재 결과", "",
        "| 파일 ID | 화면 스테이지 | 결과 | 최단 입력 | 발견 상태 |",
        "|---|---|---|---:|---:|",
    ]
    for e in entries:
        label = "최단 확정" if e["status"] == "solved" else "도달 불가" if e["status"] == "unreachable" else "미확정"
        distance = str(e["distance"]) if e["status"] == "solved" else "—"
        lines.append(f"| {e['mapId']} | {', '.join(e['displayStageIds']) or 'MapSet 미연결'} | {label} | {distance} | {e['visited']:,} |")
    if any(e["mapId"] == "ET-19" and e["routeNESW"] == "N" * 8 for e in entries):
        lines += ["", "ET-19는 시작점에서 위로 8번 연속 이동하면 별 7개를 모으고 출구에 도달한다. 현재 맵의 가장 짧은 통과 경로이며, 별도 우회가 필요하지 않다."]
    lines += ["", "방향 입력 하나의 비용은 1이다. 제자리 밀기도 포함한다. `↑/N` 위, `→/E` 오른쪽, `↓/S` 아래, `←/W` 왼쪽이며 반복 입력을 생략하지 않는다.", "", "## 계산과 검증", "",
        "모든 간선은 현재 Unity와 같은 `PuzzleDeviceSession` 소스의 `SearchStep`으로 생성했다. "
        f"{len(entries)}개 맵의 {parity_states:,}개 실제 Unity 상태와 {parity_edges:,}개 공개 입력 전이를 별도 실행기와 대조했다. "
        "완성된 경로는 Unity의 `ReplayRuntimeInput`으로 다시 실행해 수집·출구·승리·비용을 확인했다.", "",
        "A* 하한은 동적 장애물을 제거하고 카트가 줄 수 있는 밀림을 더 넓게 허용한 완화 보드다. "
        "더 싼 경로를 찾으면 상태를 다시 열며, 최소 `g+h`의 승리 상태를 꺼낸 경우에만 최단으로 확정한다. "
        "셔터의 phase는 키에서만 홀짝으로 묶는다. 석상이 돌진 중이 아닐 때의 과거 timer 역시 다음 감지 때 덮어쓰므로 키에서만 정규화하며, 잠긴 석상의 timer는 유지한다. "
        "유리 상자의 지난 이동 방향은 다음 이동 입력이나 충돌 판정에 사용하지 않으므로 키에서만 묶는다. 실제 세션과 체크포인트의 원래 값은 보존한다.", "",
        "ET-15 재탐색은 석상과 유리를 제거한 완화 보드에서 플레이어·주사위 위치·24가지 면 방향·별 수집·주사위 발판 개방을 함께 계산한다. "
        "이 보드의 369,272개 상태에 대한 역방향 BFS 거리를 추가 하한으로 사용한다. 유리 밀기는 플레이어가 제자리에 있는 완화 간선으로 허용한다. "
        "주사위 면 전환은 원래 `PuzzleDeviceSession.Roll`로 생성하며, 실제 탐색에서 확장한 모든 유효 간선의 투영이 완화 그래프에도 존재하는지 검사한다. "
        "Unity 표본의 유효 간선 1,128개도 같은 투영 검사를 통과했다. "
        "[주사위 하한 검증](Validation/EchoThrough18/transition_verification_dice.json).", "",
        "석상·유리·주사위만 있는 맵에서는 바닥이 끝나거나 영구 정지한 석상에 막혀 더는 이동할 수 없는 석상을 찾는다. "
        "이 영구 장애물만 남긴 완화 보드에서도 출구나 필요한 별에 도달할 수 없는 분기만 제외한다. "
        "다른 장치가 있는 맵에는 이 조건을 적용하지 않는다. 작은 보드의 완전 BFS와 실제 소스 전이로 거짓 차단 여부를 검사했다.", "",
        "탐색 상태 수와 상태·메모리 상한은 각 검색 JSON에 있다. 상한·취소·원본 변경은 미확정이며 도달 불가로 바꾸지 않는다. "
        "원본 맵, `.meta` GUID와 작성된 `verificationRoute`를 계산 때문에 수정하지 않았다. "
        "모든 맵의 PlayMode 전체 플레이나 모바일 렌더링을 검증한 결과와는 구분한다.", "",
        "초기 검색 빌드와 정규화를 보강한 빌드는 각각의 소스 해시와 검증 보고서로 구분한다. "
        "완료된 초기 빌드의 큐·진입점 소스는 `Validation/EchoThrough18/InitialBuildSources`에 보관했다. "
        "현재 런타임 판정 소스는 모든 빌드에서 동일하다.", "", "## 경로", "",
    ]
    for e in entries:
        lines += [f"## {e['mapId']}", "",
            f"[입력별 Unity 재생]({e['evidence'].removeprefix('Docs/')}) · "
            f"[검색 증거]({e['searchEvidence'].removeprefix('Docs/')})", ""]
        if e["status"] == "solved":
            lines += [f"**{e['distance']}입력**", "", "```text"]
            route = e["route"]
            for i in range(0, len(route), 10):
                lines.append(f"{i + 1:02d}~{min(i + 10, len(route)):02d}: " + " ".join(route[i:i + 10]))
            lines += ["```", "", "NESW: `" + e["routeNESW"] + "`", ""]
        else:
            lines += [f"상태: `{e['status']}`. {e['detail']}", ""]
    old_doc = DOC.read_text(encoding="utf-8-sig")
    history = old_doc[old_doc.index("<details>"):] if "<details>" in old_doc else ""
    DOC.write_text("\n".join(lines) + "\n" + history, encoding="utf8")
    write(FOLDER / "summary.json", {
        "mapCommit": inventory["mapCommit"], "maps": len(entries), "solved": len(solved),
        "unreachable": len(unreachable), "unconfirmed": len(pending),
        "distances": {e["mapId"]: e["distance"] for e in solved}, "fixtures": fixture_metadata,
        "nativeReplay": "native_replay.json", "originalMapsAndMetadataPreserved": True,
        "validation": sorted(validation_paths[key].relative_to(ROOT).as_posix() for key in used_builds),
        "nativeParityStates": parity_states, "nativeParityEdges": parity_edges,
        "reusedSearchMaps": [item["id"] for item in inventory["maps"] if "searchEvidence" in item],
        "runtimeCodeChanged": False,
    })
    print(json.dumps({"maps": len(entries), "solved": len(solved), "unreachable": len(unreachable), "unconfirmed": len(pending)}, ensure_ascii=True))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--through", type=int, choices=[18, 20], default=18)
    main(parser.parse_args().through)
