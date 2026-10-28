using System;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    // Keep authored rules inside the map's asset file, without changing PuzzleMap's runtime API.
    public static class PuzzleEmbeddedScenario
    {
        public static PuzzleScenario Ensure(PuzzleMap map, string legacyPath = null)
        {
            if (!map) throw new ArgumentNullException(nameof(map));
            var mapPath = AssetDatabase.GetAssetPath(map);
            if (string.IsNullOrEmpty(mapPath)) throw new InvalidOperationException("Save the map asset first.");
            if (map.scenario && AssetDatabase.GetAssetPath(map.scenario) == mapPath)
                return map.scenario;

            var source = map.scenario ? map.scenario :
                string.IsNullOrEmpty(legacyPath) ? null : AssetDatabase.LoadAssetAtPath<PuzzleScenario>(legacyPath);
            var scenario = source ? UnityEngine.Object.Instantiate(source) : ScriptableObject.CreateInstance<PuzzleScenario>();
            scenario.name = "Scenario";
            AssetDatabase.AddObjectToAsset(scenario, map);
            map.scenario = scenario;
            EditorUtility.SetDirty(scenario);
            EditorUtility.SetDirty(map);
            return scenario;
        }

        public static void RemoveLegacyAfterReadback(PuzzleMap map, string legacyPath)
        {
            if (string.IsNullOrEmpty(legacyPath)) return;
            var legacy = AssetDatabase.LoadMainAssetAtPath(legacyPath);
            if (!legacy) return;
            if (!(legacy is PuzzleScenario))
                throw new InvalidOperationException("Unrelated asset at old scenario path: " + legacyPath);
            var mapPath = AssetDatabase.GetAssetPath(map);
            var saved = AssetDatabase.LoadAssetAtPath<PuzzleMap>(mapPath);
            if (!saved || !saved.scenario || AssetDatabase.GetAssetPath(saved.scenario) != mapPath ||
                !AssetDatabase.IsSubAsset(saved.scenario))
                throw new InvalidOperationException("Embedded scenario readback failed: " + mapPath);
            if (!AssetDatabase.DeleteAsset(legacyPath))
                throw new InvalidOperationException("Could not remove redundant scenario: " + legacyPath);
        }
    }
}
