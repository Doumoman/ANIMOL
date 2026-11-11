using System;
using System.Collections.Generic;
using ANIMOL.Puzzle;

namespace William.NotebookUI
{
    public static class NotebookCampaignStages
    {
        public const int PerTheme = 20;
        static readonly string[] Prefixes = { "SW", "ET-", "observatory-", "sea-" };
        public static bool TryOrder(string id, out string prefix, out int order)
        {
            prefix = null; order = 0;
            if (string.IsNullOrEmpty(id)) return false;
            foreach (string candidate in Prefixes)
                if (id.StartsWith(candidate, StringComparison.Ordinal) && id.Length == candidate.Length + 2 &&
                    int.TryParse(id.Substring(candidate.Length), out order) && order >= 1 && order <= PerTheme)
                { prefix = candidate; return true; }
            return false;
        }
        public static bool TryNext(string id, out string next)
        {
            next = null;
            if (!TryOrder(id, out string prefix, out int order) || order >= PerTheme) return false;
            next = prefix + (order + 1).ToString("00"); return true;
        }
        public static bool IsLast(string id) => TryOrder(id, out _, out int order) && order == PerTheme;
        public static PuzzleMap FindStarwayMap(IEnumerable<PuzzleMap> maps, string stageId)
        {
            if (maps == null || !NotebookProjectGateway.TryStarwayMapId(stageId, out _)) return null;
            PuzzleMap found = null;
            foreach (var map in maps)
            {
                if (!TryStarwayStageId(map, out string assigned) || assigned != stageId) continue;
                // Duplicate names must never select a different puzzle by array order.
                if (found && found != map) return null;
                found = map;
            }
            return found;
        }
        public static bool TryStarwayStageId(PuzzleMap map, out string stageId)
        {
            stageId = null;
            if (!map || !map.scenario || map.scenario.themeId != "starway") return false;
            string id = StarwayStageCatalog.AssetName(map);
            if (id.EndsWith("_MapData", StringComparison.Ordinal)) id = id.Substring(0, id.Length - "_MapData".Length);
            if (id.Length != 4 || !id.StartsWith("SW", StringComparison.Ordinal) ||
                id[2] < '0' || id[2] > '9' || id[3] < '0' || id[3] > '9') return false;
            int source = (id[2] - '0') * 10 + id[3] - '0';
            if (source >= PerTheme) return false;
            stageId = "SW" + (source + 1).ToString("00");
            return true;
        }
        public static string DisplayId(PuzzleMap map)
        {
            if (!map) return null;
#if UNITY_EDITOR
            if (map.scenario && map.scenario.themeId == "memory_theater")
            {
                var set = UnityEditor.AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
                    "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
                if (set) foreach (var entry in set.maps) if (entry.map == map) return entry.stageId;
            }
#endif
            string name = map.scenario && map.scenario.themeId == "starway" ? StarwayStageCatalog.AssetName(map) : map.name;
            string id = name.EndsWith("_MapData", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "_MapData".Length) : name;
            if (TryStarwayStageId(map, out string stageId)) return stageId;
            return id;
        }
    }
}
