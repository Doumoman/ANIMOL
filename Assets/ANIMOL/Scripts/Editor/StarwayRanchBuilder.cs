using System;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Presentation.Editor
{
    public static class StarwayRanchBuilder
    {
        const string Data = "Assets/ANIMOL/Data/Art/StarwayRanch";
        const string Sprites = "Assets/ANIMOL/Sprites/StarwayRanch";
        const string Art = Data + "/StarwayRanchArt.asset";
        [Serializable] sealed class SpriteSetting { public string file; }
        [Serializable] sealed class SpriteSettings { public SpriteSetting[] spriteSettings; }

        static void EnsureSortingLayer()
        {
            if (SortingLayer.layers.Any(layer => layer.name == "Background")) return;
            var tag = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").First();
            var serialized = new SerializedObject(tag);
            var layers = serialized.FindProperty("m_SortingLayers");
            layers.InsertArrayElementAtIndex(0);
            var background = layers.GetArrayElementAtIndex(0);
            background.FindPropertyRelative("name").stringValue = "Background";
            background.FindPropertyRelative("uniqueID").intValue = 1357522431;
            background.FindPropertyRelative("locked").intValue = 0;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        static StarwayRanchArt Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(Art);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<StarwayRanchArt>(); AssetDatabase.CreateAsset(catalog, Art); }
            var settings = JsonUtility.FromJson<SpriteSettings>(File.ReadAllText(Data + "/sprite_import_settings.json"));
            catalog.sprites = settings.spriteSettings.Select(row => new RanchSpriteEntry {
                file = row.file, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "/" + row.file)
            }).ToArray();
            if (catalog.sprites.Any(entry => !entry.sprite)) throw new InvalidDataException("V10 sprites did not import");
            catalog.backgroundLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/background_scene_layout.json");
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static PuzzleMap QaMap(StarwayRanchArt catalog)
        {
            var path = Data + "/ArtQaMap.asset";
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
            if (!map) { map = ScriptableObject.CreateInstance<PuzzleMap>(); AssetDatabase.CreateAsset(map, path); }
            map.Resize(11, 17); map.terrain.Clear(); map.InvalidateIndex();
            for (var y = 3; y <= 13; y++) for (var x = 2; x <= 8; x++)
            {
                if ((x == 4 || x == 5) && (y == 8 || y == 9)) continue;
                if (y == 7 && x >= 3 && x <= 6) continue;
                var material = y == 10 && x >= 3 && x <= 6 ? TerrainMaterial.Stone :
                    y == 6 && x >= 3 && x <= 6 ? TerrainMaterial.Sand : TerrainMaterial.Grass;
                map.SetTerrain(new Vector2Int(x, y), material);
            }
            map.start = new Vector2Int(2, 3); map.exit = new Vector2Int(8, 13);
            map.targetBubbles = 0;
            EditorUtility.SetDirty(map);
            return map;
        }

        static void ReplaceScene(string path, StarwayRanchArt catalog, PuzzleMap map, bool hideOldIsland)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.CompareTag("MainCamera")) ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidOperationException("QA camera missing: " + path);
            camera.orthographic = true;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 121f);
            camera.orthographicSize = 9.6f;
            camera.transform.position = new Vector3(0, 0, -10);
            var prior = GameObject.Find("Starway Ranch V10 QA");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                renderer.enabled = false;
            if (hideOldIsland)
            {
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None))
                    renderer.enabled = false;
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    renderer.enabled = false;
            }
            else
            {
                // Preserve legacy QA test components while replacing their visible artwork.
                foreach (var player in UnityEngine.Object.FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None))
                    if (player.visual) { var color = player.visual.color; color.a = 0; player.visual.color = color; }
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    if (!renderer.GetComponentInParent<SpriteFramePlayer>()) renderer.enabled = false;
            }
            var root = new GameObject("Starway Ranch V10 QA");
            var foreground = new GameObject("V10 foreground").AddComponent<StarwayRanchMapRenderer>();
            foreground.transform.SetParent(root.transform, false);
            foreground.transform.localPosition = new Vector3(-5, -8, 0);
            foreground.Bind(map, catalog);
            foreground.Show(new RectInt(0, 0, map.Width, map.Height));
            StarwayRanchBackground.Create(root.transform, camera, catalog);
            var groups = new[] { "conveyor/idle", "oneway_gate/closed", "bounce_corner/idle",
                "brake_patch/idle", "star_stone/idle", "star_socket/empty", "pressure_plate/released",
                "vine_bridge/extended", "wind_corridor/idle" };
            for (var i = 0; i < groups.Length; i++)
            {
                var go = new GameObject("V10 device preview " + groups[i]);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(-3 + i % 3 * 2.7f, -5 + i / 3 * 3.8f, 0);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = catalog.Find("devices/" + groups[i] + ".png");
                renderer.sortingOrder = 30;
            }
            EditorSceneManager.SaveScene(scene);
        }

        static void ReplaceCharacterQa(StarwayRanchArt catalog)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity", OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidOperationException("Character QA camera missing");
            var prior = GameObject.Find("Starway Ranch background V10");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            StarwayRanchBackground.Create(null, camera, catalog);
            EditorSceneManager.SaveScene(scene);
        }

        static void ReplaceDeviceQa(StarwayRanchArt catalog)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Device8_Art_QA.unity", OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidOperationException("Device QA camera missing");
            var prior = GameObject.Find("Starway Ranch V10 device states");
            if (prior) UnityEngine.Object.DestroyImmediate(prior);
            foreach (var view in UnityEngine.Object.FindObjectsByType<DeviceVisualView>(FindObjectsSortMode.None))
                if (view.visual) { var color = view.visual.color; color.a = 0; view.visual.color = color; }
            var grass = catalog.Find("tiles/grass/organic256/n255.png");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.name == "grass") renderer.sprite = grass;
            var root = new GameObject("Starway Ranch V10 device states");
            var families = new[] { "conveyor", "oneway_gate", "bounce_corner", "brake_patch",
                "star_stone", "star_socket", "pressure_plate", "vine_bridge", "wind_corridor" };
            var states = new[] { "idle,active", "closed,open", "idle,pressed", "idle,pressed",
                "idle,pushed", "empty,filled", "released,pressed", "extended,retracted", "idle,active" };
            for (var i = 0; i < families.Length; i++)
            {
                var pair = states[i].Split(',');
                for (var j = 0; j < 2; j++)
                {
                    var go = new GameObject(families[i] + " " + pair[j]);
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(-7 + (i % 3) * 7 + j * 2.1f, 12 - (i / 3) * 9, 0);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = catalog.Find("devices/" + families[i] + "/" + pair[j] + ".png");
                    renderer.sortingOrder = 40;
                }
            }
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("ANIMOL/Starway Ranch/Apply V10")]
        public static string Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit Play mode and save the current scene before V10 authoring.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.Refresh();
            EnsureSortingLayer();
            var catalog = Catalog();
            foreach (var path in new[] { "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (theme) { theme.starwayRanch = catalog; EditorUtility.SetDirty(theme); }
            }
            var map = QaMap(catalog);
            AssetDatabase.SaveAssets();
            ReplaceScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity", catalog, map, false);
            ReplaceScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Island_QA.unity", catalog, map, true);
            ReplaceCharacterQa(catalog);
            ReplaceDeviceQa(catalog);
            if (setup.Any(entry => entry.isLoaded && entry.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            AssetDatabase.SaveAssets();
            return "V10 sprites imported, starway theme and two QA scenes connected";
        }
    }
}
