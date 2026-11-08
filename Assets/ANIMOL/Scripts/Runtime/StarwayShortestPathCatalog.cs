using System;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Read the same evidence asset used by the map editor. Source maps stay intact;
    // controllers apply a verified budget only to their owned runtime scenario.
    public static class StarwayShortestPathCatalog
    {
        public const int SolverVersion = 7;
        public const string AssetPath = "Assets/ANIMOL/Data/Puzzle/MapData/starway/StarwayShortestPaths.json";
        [Serializable] sealed class Index { public int formatVersion; public Entry[] maps; }
        [Serializable] sealed class Entry
        {
            public string mapId, mapAsset, mapSha256, status, route;
            public int solverVersion, distance;
        }
        public static bool TryGetBudget(PuzzleMap source, TextAsset catalog, out int budget)
        {
            budget = 0;
            if (!source || !source.scenario || source.scenario.themeId != "starway" || source.starwayV3Certificate || !catalog) return false;
            Index data;
            try { data = JsonUtility.FromJson<Index>(catalog.text); }
            catch (ArgumentException) { return false; }
            if (data == null || data.formatVersion != SolverVersion || data.maps == null) return false;
            string id = source.name.EndsWith("_MapData", StringComparison.Ordinal)
                ? source.name.Substring(0, source.name.Length - "_MapData".Length) : source.name;
            foreach (var entry in data.maps)
            {
                if (entry == null || entry.mapId != id || entry.status != "solved" || entry.solverVersion != SolverVersion || entry.distance <= 0) continue;
                try { if (PuzzleRouteNotation.InputCount(entry.route) != entry.distance) return false; }
                catch (ArgumentException) { return false; }
                catch (FormatException) { return false; }
#if UNITY_EDITOR
                // Live asset edits invalidate evidence before a preview can use it.
                string path = UnityEditor.AssetDatabase.GetAssetPath(source);
                if (path != entry.mapAsset || !System.IO.File.Exists(path) || UnityEditor.EditorUtility.IsDirty(source) || UnityEditor.EditorUtility.IsDirty(source.scenario)) return false;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                    if (hash != entry.mapSha256) return false;
                }
#endif
                budget = entry.distance; return true;
            }
            return false;
        }
    }
}
