#if UNITY_EDITOR
using System;
using System.IO;
using ANIMOL.Art.SunsetSanctumV02;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.EditorTools
{
    public static class SunsetSanctumV02ProjectInstaller
    {
        const string Root = "Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CARTOON_V02";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset";
        const string AnimatedPrefabPath = Root + "/_Generated/SunsetSanctumV02AnimatedDevice.prefab";
        const string StaticPrefabPath = Root + "/_Generated/SunsetSanctumV02StaticDevice.prefab";

        [MenuItem("Tools/ANIMOL/Sunset Sanctum V02/Connect actual letter_observatory scenarios")]
        public static void Connect()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SunsetSanctumV02VisualCatalog>(
                Root + "/_Generated/SunsetSanctumV02VisualCatalog.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/LightBeam.mat");
            var backgroundLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/background_scene_layout.json");
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            if (!catalog || !material || !backgroundLayout || !theme || !Directory.Exists(Root + "/_Generated"))
                throw new InvalidOperationException("Build and verify the V02 catalog and beam material before migration.");
            string error;
            if (!catalog.ValidateIntegrity(SunsetSanctumV02PuzzleBinding.ManifestSha256, out error))
                throw new InvalidDataException(error);

            var animated = DevicePrefab(AnimatedPrefabPath, catalog, true);
            var still = DevicePrefab(StaticPrefabPath, catalog, false);
            theme.sunsetSanctumV02Catalog = catalog;
            theme.sunsetSanctumV02BeamMaterial = material;
            theme.sunsetSanctumV02BackgroundLayout = backgroundLayout;
            theme.clockDome = null;
            EditorUtility.SetDirty(theme);

            int scenarios = 0, devices = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:PuzzleScenario",
                new[] { "Assets/ANIMOL/Data/Puzzle/Prototypes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scenario = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(path);
                if (!scenario || scenario.themeId != "letter_observatory") continue;
                scenarios++;
                // The V02 route renderer uses the authoritative trace; the old beam
                // prefab must not remain as a serialized fallback for this theme.
                scenario.beamPrefab = null;
                foreach (var device in scenario.devices)
                {
                    if (!SunsetSanctumV02PuzzleBinding.Covers(device)) continue;
                    device.visualPrefab = device.role == PrototypeRole.MetalCrate ? still : animated;
                    devices++;
                }
                EditorUtility.SetDirty(scenario);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Sunset Sanctum V02] Catalog pinned; connected " + devices +
                " actual device entries in " + scenarios + " letter_observatory scenarios. IDs, directions, cells and rules were untouched.");
        }

        static GameObject DevicePrefab(string path, SunsetSanctumV02VisualCatalog catalog, bool isAnimated)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing)
            {
                if (isAnimated ? !existing.GetComponent<SunsetSanctumV02AnimatedVisual>()
                    : !existing.GetComponent<SunsetSanctumV02StaticVisual>())
                    throw new InvalidOperationException("Existing V02 prefab has a different type: " + path);
                return existing;
            }
            var go = new GameObject(isAnimated ? "Sunset Sanctum V02 animated device" : "Sunset Sanctum V02 static device");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var visual = isAnimated ? (MonoBehaviour)go.AddComponent<SunsetSanctumV02AnimatedVisual>()
                    : go.AddComponent<SunsetSanctumV02StaticVisual>();
                var serialized = new SerializedObject(visual);
                serialized.FindProperty("catalog").objectReferenceValue = catalog;
                serialized.FindProperty("target").objectReferenceValue = renderer;
                serialized.FindProperty("expectedManifestSha256").stringValue = SunsetSanctumV02PuzzleBinding.ManifestSha256;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                if (!prefab) throw new InvalidOperationException("Could not create " + path);
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
