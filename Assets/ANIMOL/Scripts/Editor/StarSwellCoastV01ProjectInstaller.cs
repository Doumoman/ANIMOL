#if UNITY_EDITOR
using System;
using System.IO;
using ANIMOL.Art.StarSwellCoastV01;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.EditorTools
{
    public static class StarSwellCoastV01ProjectInstaller
    {
        const string Root = "Assets/ANIMOL/Art/ANIMOL_STAR_SWELL_COAST_CARTOON_V01";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset";
        const string PrefabPath = Root + "/_Generated/StarSwellCoastV01Device.prefab";

        [MenuItem("Tools/ANIMOL/Star Swell Coast V01/Connect actual twilight_sea scenarios")]
        public static void Connect()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarSwellCoastV01VisualCatalog>(
                Root + "/_Generated/StarSwellCoastV01VisualCatalog.asset");
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/background_scene_layout.json");
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            if (!catalog || !layout || !theme) throw new InvalidOperationException("Build Coast V01 catalog before migration.");
            string error;
            if (!catalog.ValidateIntegrity(StarSwellCoastV01PuzzleBinding.ManifestSha256, out error))
                throw new InvalidDataException(error);
            var prefab = DevicePrefab(catalog);
            theme.starSwellCoastV01Catalog = catalog;
            theme.starSwellCoastV01BackgroundLayout = layout;
            theme.twilightCoast = null;
            // The Coast V01 map renderer owns these retired visual-only fallbacks.
            theme.terrain = Array.Empty<TerrainArt>();
            theme.far = null; theme.mid = null; theme.near = null;
            EditorUtility.SetDirty(theme);
            int scenarios = 0, devices = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:PuzzleScenario",
                new[] { "Assets/ANIMOL/Data/Puzzle/Prototypes" }))
            {
                var scenario = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(AssetDatabase.GUIDToAssetPath(guid));
                if (!scenario || scenario.themeId != "twilight_sea") continue;
                scenarios++;
                scenario.beamPrefab = null; // Coast has no optical beam device.
                foreach (var device in scenario.devices)
                {
                    if (!StarSwellCoastV01PuzzleBinding.Covers(device)) continue;
                    device.visualPrefab = prefab;
                    devices++;
                }
                EditorUtility.SetDirty(scenario);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Coast V01] Pinned catalog and connected " + devices +
                " actual devices in " + scenarios + " twilight_sea scenarios. No rules, IDs or cells changed.");
        }

        static GameObject DevicePrefab(StarSwellCoastV01VisualCatalog catalog)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing)
            {
                if (!existing.GetComponent<StarSwellCoastV01AnimatedVisual>())
                    throw new InvalidOperationException("Existing Coast V01 prefab has wrong type.");
                return existing;
            }
            var go = new GameObject("Star Swell Coast V01 device");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var visual = go.AddComponent<StarSwellCoastV01AnimatedVisual>();
                var serialized = new SerializedObject(visual);
                serialized.FindProperty("catalog").objectReferenceValue = catalog;
                serialized.FindProperty("target").objectReferenceValue = renderer;
                serialized.FindProperty("expectedManifestSha256").stringValue =
                    StarSwellCoastV01PuzzleBinding.ManifestSha256;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Could not create Coast V01 device prefab.");
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
