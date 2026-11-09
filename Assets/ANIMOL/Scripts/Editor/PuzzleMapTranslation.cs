using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    /// <summary>Moves authored coordinates together, without resizing or discarding records.</summary>
    public static class PuzzleMapTranslation
    {
        public const string UndoName = "맵 전체 이동";

        static bool HasTarget(PrototypeDevice d) => d.role == PrototypeRole.Portal ||
            d.role == PrototypeRole.Chick && d.target != Vector2Int.zero;
        static bool HasBubbleReward(PrototypeDevice d) => d.role == PrototypeRole.StarSocket &&
            d.variant == "socket" && d.socketReward == StarSocketReward.RevealBubble;
        static bool HasGoalCell(PrototypeGoal g) =>
            g.kind == PrototypeGoalKind.Position || g.kind == PrototypeGoalKind.Visited;

        static IEnumerable<Vector2Int> Cells(PuzzleMap map)
        {
            yield return map.start; yield return map.exit;
            foreach (var t in map.terrain) yield return t.position;
            foreach (var d in map.devices)
            {
                yield return d.position;
                if (d.kind == DeviceKind.Portal) yield return d.portalTarget;
            }
            foreach (var p in map.bubbles) yield return p;
            foreach (var d in map.decorations)
            {
                var ranch = RanchConnectedDecorV02Host.SourceMapping(map, d);
                var theater = TheaterConnectedDecorHost.TryMap(map, d, out var t) ? t : null;
                var sanctum = SanctumConnectedDecorHost.TryMap(map, d, out var s) ? s : null;
                var coast = CoastConnectedDecorHost.TryMap(map, d, out var c) ? c : null;
                var width = theater?.width ?? sanctum?.width ?? coast?.width ?? ranch?.occupiedWidth ?? 1;
                var height = theater?.height ?? sanctum?.height ?? coast?.height ?? ranch?.occupiedHeight ?? 1;
                yield return d.position;
                yield return d.position + new Vector2Int(width - 1, height - 1);
            }
            if (!map.scenario) yield break;
            var rules = map.scenario;
            foreach (var d in rules.devices)
            {
                yield return d.position;
                if (HasTarget(d)) yield return d.target;
                if (HasBubbleReward(d)) yield return d.socketBubbleCell;
                foreach (var p in d.cells) yield return p;
                if (d.role == PrototypeRole.Bookshelf && d.variant != "plate")
                {
                    var axis = d.orientation % 2 == 0 ? Vector2Int.right : Vector2Int.up;
                    yield return d.position - axis; yield return d.position + axis;
                }
            }
            foreach (var path in rules.railPaths ?? Enumerable.Empty<PrototypeRailPath>())
                if (path?.cells != null) foreach (var p in path.cells) yield return p;
            foreach (var p in rules.water) yield return p;
            foreach (var p in rules.nests) yield return p;
            foreach (var art in rules.ranchV13Art)
                if (art != null) foreach (var p in art.Footprint()) yield return p;
            foreach (var goal in rules.goals)
                if (HasGoalCell(goal)) yield return goal.cell;
        }

        public static RectInt Bounds(PuzzleMap map)
        {
            if (!map) return default;
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var p in Cells(map)) { min = Vector2Int.Min(min, p); max = Vector2Int.Max(max, p); }
            return new RectInt(min, max - min + Vector2Int.one);
        }

        public static bool Available(PuzzleMap map, out string reason)
        {
            reason = null;
            if (!map) { reason = "맵을 선택하세요."; return false; }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { reason = "Play Mode를 종료한 뒤 이동하세요."; return false; }
            if (map.starwayV3Certificate)
            { reason = "인증 좌표가 고정된 맵은 전체 이동할 수 없습니다."; return false; }
            var bounds = Bounds(map);
            if (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax > map.Width || bounds.yMax > map.Height)
            { reason = "맵 밖에 보관된 배치가 있습니다. 맵 크기를 넓혀 모든 배치를 포함한 뒤 이동하세요."; return false; }
            return true;
        }

        public static Vector2Int ClampDelta(PuzzleMap map, Vector2Int requested)
        {
            if (!Available(map, out _)) return Vector2Int.zero;
            var b = Bounds(map);
            return new Vector2Int(Mathf.Clamp(requested.x, -b.xMin, map.Width - b.xMax),
                Mathf.Clamp(requested.y, -b.yMin, map.Height - b.yMax));
        }

        static void Shift(List<Vector2Int> points, Vector2Int delta)
        { if (points != null) for (var i = 0; i < points.Count; i++) points[i] += delta; }

        public static Vector2Int Move(PuzzleMap map, Vector2Int requested)
        {
            var delta = ClampDelta(map, requested);
            if (delta == Vector2Int.zero) return delta;
            var objects = map.scenario ? new Object[] { map, map.scenario } : new Object[] { map };
            Undo.RegisterCompleteObjectUndo(objects, UndoName);

            // Older theater tiles get their material from shared, absolute theme masks.
            // Store their existing appearance on the moved tiles, leaving the theme untouched.
            var theater = map.theme && map.theme.memorySkyTheater;
            var layout = theater ? map.theme.memorySkyTheater.defaultLayout : null;
            var deck = new HashSet<Vector2Int>(layout ? layout.visualDeck : Enumerable.Empty<Vector2Int>());
            var velvet = new HashSet<Vector2Int>(layout ? layout.velvet : Enumerable.Empty<Vector2Int>());
            for (var i = 0; i < map.terrain.Count; i++)
            {
                var t = map.terrain[i];
                if (theater && !EchoTheaterTerrain.IsAuthored(t.material))
                    t.material = deck.Contains(t.position) ? TerrainMaterial.TheaterWoodCatwalk :
                        velvet.Contains(t.position) ? TerrainMaterial.TheaterVelvetCarpet : TerrainMaterial.TheaterStageFloor;
                t.position += delta; map.terrain[i] = t;
            }
            for (var i = 0; i < map.devices.Count; i++)
            {
                var d = map.devices[i]; d.position += delta;
                if (d.kind == DeviceKind.Portal) d.portalTarget += delta;
                map.devices[i] = d;
            }
            for (var i = 0; i < map.decorations.Count; i++)
            { var d = map.decorations[i]; d.position += delta; map.decorations[i] = d; }
            Shift(map.bubbles, delta); map.start += delta; map.exit += delta;
            if (map.scenario)
            {
                var rules = map.scenario;
                Shift(rules.nests, delta);
                foreach (var d in rules.devices)
                {
                    d.position += delta;
                    if (HasTarget(d))
                    {
                        d.target += delta;
                        // (0,0) is the legacy "no nest" sentinel; preserve a nest moved there explicitly.
                        if (d.role == PrototypeRole.Chick && d.target == Vector2Int.zero && !rules.nests.Contains(d.target))
                            rules.nests.Add(d.target);
                    }
                    if (HasBubbleReward(d)) d.socketBubbleCell += delta;
                    Shift(d.cells, delta);
                }
                foreach (var path in rules.railPaths ?? Enumerable.Empty<PrototypeRailPath>())
                    if (path != null) Shift(path.cells, delta);
                Shift(rules.water, delta);
                foreach (var art in rules.ranchV13Art)
                {
                    if (art == null) continue;
                    if (string.IsNullOrEmpty(art.instanceId)) art.instanceId = art.Id;
                    art.position += delta;
                    if (art.hasPondB) art.pondB += delta;
                }
                foreach (var goal in rules.goals) if (HasGoalCell(goal)) goal.cell += delta;
                EditorUtility.SetDirty(rules);
            }
            map.InvalidateIndex(); map.NotifyDecorationsChanged(); EditorUtility.SetDirty(map);
            return delta;
        }
    }
}
