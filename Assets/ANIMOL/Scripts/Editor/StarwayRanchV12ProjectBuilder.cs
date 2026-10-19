using System;
using System.Collections.Generic;
using System.IO;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Art.V12
{
    // Rebind the existing art asset so existing theme/scene references retain their GUIDs.
    public static class StarwayRanchV12ProjectBuilder
    {
        public const string Root = "Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12";
        const string ArtPath = "Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset";
        [Serializable] sealed class Manifest { public AssetRow[] assets; }
        [Serializable] sealed class AssetRow
        { public string file, sha256; public int width, height; public float ppu; public float[] pivot; }

        [MenuItem("ANIMOL/Art QA/Apply Starway Ranch V12 environment")]
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebinding ranch art.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/manifest.json"));
            if (manifest?.assets == null || manifest.assets.Length != 1945)
                throw new InvalidDataException("The complete V12 1945-sprite manifest is required.");
            var art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(ArtPath);
            if (!art) throw new InvalidDataException("Existing Starway Ranch art asset is missing.");
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayRanchV12VisualCatalog>(
                Root + "/_Generated/StarwayRanchV12VisualCatalog.asset");
            if (!catalog) throw new InvalidDataException("Build V12 animated catalog first.");
            string error;
            if (!catalog.ValidateIntegrity(catalog.SourceManifestSha256, out error))
                throw new InvalidDataException(error);
            var entries = new List<RanchSpriteEntry>(manifest.assets.Length);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in manifest.assets)
            {
                if (row == null || !paths.Add(row.file) || row.pivot == null || row.pivot.Length != 2)
                    throw new InvalidDataException("Duplicate or malformed V12 sprite entry.");
                var path = Root + "/" + row.file;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!sprite || importer == null || sprite.rect.width != row.width || sprite.rect.height != row.height ||
                    Mathf.Abs(sprite.pixelsPerUnit - row.ppu) > .001f ||
                    Vector2.Distance(sprite.pivot, new Vector2(row.pivot[0] * row.width, row.pivot[1] * row.height)) > .03f ||
                    importer.filterMode != FilterMode.Bilinear || importer.mipmapEnabled ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.npotScale != TextureImporterNPOTScale.None ||
                    StarwayRanchV12VisualCatalog.Sha256Of(File.ReadAllBytes(path)) != row.sha256)
                    throw new InvalidDataException("V12 bytes or import settings differ: " + path);
                entries.Add(new RanchSpriteEntry { file = row.file, sprite = sprite });
            }
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/background_scene_layout.json");
            if (!layout) throw new InvalidDataException("V12 independent background layout is missing.");
            art.sprites = entries.ToArray();
            art.backgroundLayout = layout;
            art.v12Animations = catalog;
            art.v12ManifestSha256 = catalog.SourceManifestSha256;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return "Rebound 1945 verified V12 sprites and 46 animated clips to existing ranch art.";
        }

        [MenuItem("ANIMOL/Art QA/Refresh Starway Ranch V12 QA scenes")]
        public static string RefreshQaScenes()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit Play mode and save the current scene first.");
            var art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(ArtPath);
            if (!art || !art.v12Animations) throw new InvalidDataException("Apply V12 art first.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                RefreshTerrainScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity", art);
                RefreshTerrainScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Island_QA.unity", art);
                RefreshCharacterScene(art);
                RefreshDeviceScene(art);
            }
            finally { if (Array.Exists(setup, s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
            AssetDatabase.SaveAssets();
            return "Updated four Starway V12 QA scenes with latest terrain, background and device previews.";
        }

        static void RefreshTerrainScene(string path, StarwayRanchArt art)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(ArtPath);
            var prior = GameObject.Find("Starway Ranch V10 QA");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            while ((prior = GameObject.Find("Starway Ranch V12 QA"))) UnityEngine.Object.DestroyImmediate(prior);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Art/StarwayRanch/ArtQaMap.asset");
            if (!camera || !map) throw new InvalidDataException("Missing ranch QA camera or map: " + path);
            var root = new GameObject("Starway Ranch V12 QA");
            var foreground = new GameObject("V12 foreground").AddComponent<StarwayRanchMapRenderer>();
            foreground.transform.SetParent(root.transform, false);
            foreground.transform.localPosition = new Vector3(-5, -8, 0);
            foreground.Bind(map, art);
            foreground.Show(new RectInt(0, 0, map.Width, map.Height));
            StarwayRanchBackground.Create(root.transform, camera, art);
            var previews = new[] { "devices/conveyor/east/frame_00.png", "devices/oneway_gate/east/frame_00.png",
                "devices/bounce_corner/ne/frame_00.png", "devices/brake_patch/horizontal/frame_00.png",
                "devices/star_stone/idle.png", "devices/star_socket/empty.png",
                "devices/pressure_plate/released.png", "animations/linked/vine_bridge/horizontal/open/frame_00.png",
                "devices/wind_corridor/E/idle.png" };
            for (int i = 0; i < previews.Length; ++i)
            {
                var go = new GameObject("V12 device preview " + previews[i]);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(-3 + i % 3 * 2.7f, -5 + i / 3 * 3.8f, 0);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = art.Find(previews[i]);
                if (!renderer.sprite) throw new InvalidDataException("Missing V12 QA preview " + previews[i]);
                renderer.sortingOrder = 30;
            }
            EditorSceneManager.SaveScene(scene);
        }

        static void RefreshCharacterScene(StarwayRanchArt art)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity", OpenSceneMode.Single);
            art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(ArtPath);
            var prior = GameObject.Find("Starway Ranch background V10");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            while ((prior = GameObject.Find("Starway Ranch background V12"))) UnityEngine.Object.DestroyImmediate(prior);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidDataException("Missing Starway character QA camera.");
            StarwayRanchBackground.Create(null, camera, art);
            EditorSceneManager.SaveScene(scene);
        }

        static void RefreshDeviceScene(StarwayRanchArt art)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Device8_Art_QA.unity", OpenSceneMode.Single);
            art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(ArtPath);
            var prior = GameObject.Find("Starway Ranch V10 device states");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            while ((prior = GameObject.Find("Starway Ranch V12 device states"))) UnityEngine.Object.DestroyImmediate(prior);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.name == "grass") renderer.sprite = art.Find("tiles/grass/organic256/n255.png");
            var root = new GameObject("Starway Ranch V12 device states");
            var names = new[] { "d01_star_socket", "d02_pressure_vine_bridge", "d07_conveyor",
                "d09_one_way_gate", "d18_bounce_corner", "d19_brake_pad", "d20_wind_corridor", "d24_follower_chick" };
            for (int i = 0; i < names.Length; ++i)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Themes/starway/" + names[i] + ".prefab");
                if (!source) throw new InvalidDataException("Missing V12 ranch device prefab " + names[i]);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(-6 + (i % 4) * 4, 10 - (i / 4) * 7, 0);
                var view = go.GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                if (!view || !view.ActivateV12(art.v12Animations, art.v12ManifestSha256))
                    throw new InvalidDataException("Cannot activate V12 QA prefab " + names[i] + ": visual=" +
                        (view && view.visual) + ", catalog=" + (art.v12Animations != null) + ", initial=" +
                        (view ? view.initialState : "missing view") + ", current=" + (view ? view.CurrentState : "") +
                        ", diagnostic=" + (view ? view.GetComponent<StarwayRanchV12AnimatedVisual>()?.LastDiagnostic : "missing view"));
            }
            EditorSceneManager.SaveScene(scene);
        }
    }
}
