using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.EchoRailCartV01
{
    // The host's ordered cell list owns the route. These keys only select floor art.
    public static class EchoRailCartRailTopologyV01
    {
        public static bool TryKey(IReadOnlyList<Vector2Int> cells, int index, Vector2Int fallbackDirection,
            out string key) => TryKey(cells, index, false, fallbackDirection, out key);
        public static bool TryKey(IReadOnlyList<Vector2Int> cells, int index, bool closed, Vector2Int fallbackDirection,
            out string key)
        {
            key = null;
            if (cells == null || index < 0 || index >= cells.Count) return false;
            var here = cells[index];
            var incoming = index > 0 ? cells[index - 1] - here :
                closed && cells.Count > 2 ? cells[cells.Count - 1] - here :
                cells.Count > 1 ? here - cells[1] : -fallbackDirection;
            var outgoing = index + 1 < cells.Count ? cells[index + 1] - here :
                closed && cells.Count > 2 ? cells[0] - here :
                cells.Count > 1 ? here - cells[index - 1] : fallbackDirection;
            if (!Cardinal(incoming) || !Cardinal(outgoing) || incoming == outgoing) return false;
            var north = incoming == Vector2Int.up || outgoing == Vector2Int.up;
            var east = incoming == Vector2Int.right || outgoing == Vector2Int.right;
            var south = incoming == Vector2Int.down || outgoing == Vector2Int.down;
            var west = incoming == Vector2Int.left || outgoing == Vector2Int.left;
            key = north ? east ? "ne" : south ? "ns" : "wn" :
                east ? south ? "es" : "ew" : "sw";
            return true;
        }

        static bool Cardinal(Vector2Int direction) =>
            direction == Vector2Int.up || direction == Vector2Int.right ||
            direction == Vector2Int.down || direction == Vector2Int.left;
    }
}
