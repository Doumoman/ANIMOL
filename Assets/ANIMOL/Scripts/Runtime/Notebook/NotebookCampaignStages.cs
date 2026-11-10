using System;
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
        public static string DisplayId(PuzzleMap map)
        {
#if UNITY_EDITOR
            if (map.scenario && map.scenario.themeId == "memory_theater")
            {
                var set = UnityEditor.AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
                    "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
                if (set) foreach (var entry in set.maps) if (entry.map == map) return entry.stageId;
            }
#endif
            string id = map.name.EndsWith("_MapData", StringComparison.Ordinal)
                ? map.name.Substring(0, map.name.Length - "_MapData".Length) : map.name;
            if (map.scenario && map.scenario.themeId == "starway" && id.Length == 4 && id.StartsWith("SW") &&
                int.TryParse(id.Substring(2), out int source) && source >= 0 && source < PerTheme)
                return "SW" + (source + 1).ToString("00");
            return id;
        }
    }
}
