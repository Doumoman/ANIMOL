using System;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Editor
{
    public sealed class SkyIslandImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 20000;
        public static bool Matches(string path) => path.StartsWith(SkyIslandBuilder.Tiles + "/", StringComparison.Ordinal) || path.StartsWith(SkyIslandBuilder.Backgrounds + "/", StringComparison.Ordinal) || path.StartsWith(SkyIslandBuilder.Props + "/", StringComparison.Ordinal);
        void OnPreprocessTexture()
        {
            if (!Matches(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 80; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 8192;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false; importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, .5f); settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);
            foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
        }
    }
    [Serializable] public class IslandManifest { public IslandMaterial[] materials; }
    [Serializable] public class IslandMaterial { public string key; public IslandBlob[] blob47; }
    [Serializable] public class IslandBlob { public int outer, inner; public string file; }
    [Serializable] public class IslandLayout { public string[] rows; }
    public static class SkyIslandBuilder
    {
        public const string Source = "Docs/Inbox/StarwaySkyIsland/ANIMOL_STARWAY_SKYISLAND_V02";
        public const string Tiles = "Assets/ANIMOL/Sprites/Tiles/Floor/ranch/SkyIsland";
        public const string Backgrounds = "Assets/ANIMOL/Sprites/Background/ranch/SkyIsland";
        public const string Props = "Assets/ANIMOL/Sprites/Tiles/Decoration/ranch/SkyIsland";
        public const string Data = "Assets/ANIMOL/Data/Art/StarwayIsland";
        public const string Catalog = Data + "/IslandArt.asset";
        public const string Scene = "Assets/ANIMOL/Scenes/ANIMOL_Starway_Island_QA.unity";
        [MenuItem("ANIMOL/Art QA/Apply Starway island perimeter")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before applying source art.");
            foreach (var layer in new[] { "grass", "underside" })
                foreach (var file in Directory.GetFiles(Source + "/tiles/" + layer, "*.png", SearchOption.AllDirectories))
                    Copy(file, Tiles + "/" + (layer == "grass" ? "Grass" : "Underside") + file.Substring((Source + "/tiles/" + layer).Length).Replace('\\', '/'));
            foreach (var layer in new[] { "far", "mid", "near" }) Copy(Source + "/backgrounds/" + layer + "_repeat_960.png", Backgrounds + "/" + layer + "_repeat_960.png");
            foreach (var file in Directory.GetFiles(Source + "/context_props", "*.png")) Copy(file, Props + "/" + Path.GetFileName(file));
            Directory.CreateDirectory(Data); AssetDatabase.Refresh();
            var manifest = JsonUtility.FromJson<IslandManifest>(File.ReadAllText(Source + "/manifest.json"));
            var art = Asset<SkyIslandArt>(Catalog);
            var materialPath = "Assets/ANIMOL/Materials/SkyIslandComposite.mat";
            var composite = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!composite) { composite = new Material(Shader.Find("ANIMOL/SkyIslandComposite")); AssetDatabase.CreateAsset(composite, materialPath); }
            art.compositorMaterial = composite;
            art.blob47 = manifest.materials.First(m => m.key == "grass").blob47.Select(blob =>
            {
                var name = Path.GetFileNameWithoutExtension(blob.file); var grass = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + "/Grass/blob47/" + name + ".png");
                var underside = AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + "/Underside/blob47/" + name + ".png");
                return new SkyIslandTilePair { outer = blob.outer, inner = blob.inner, grass = grass, underside = underside,
                    grassTile = TileAsset("grass_" + name, grass), undersideTile = TileAsset("underside_" + name, underside) };
            }).ToArray();
            art.far = AssetDatabase.LoadAssetAtPath<Sprite>(Backgrounds + "/far_repeat_960.png"); art.mid = AssetDatabase.LoadAssetAtPath<Sprite>(Backgrounds + "/mid_repeat_960.png"); art.near = AssetDatabase.LoadAssetAtPath<Sprite>(Backgrounds + "/near_repeat_960.png"); EditorUtility.SetDirty(art);
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(PuzzleBuilder.Data + "/Themes/StarGarden.asset"); theme.skyIsland = art; EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets(); BuildInspection(art);
        }
        static void Copy(string source, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) { if (!File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(source))) throw new InvalidOperationException("Existing edited art differs: " + target); return; }
            File.Copy(source, target);
        }
        static T Asset<T>(string path) where T : ScriptableObject
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value) return value; if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Occupied asset path " + path); value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value; }
        static Tile TileAsset(string name, Sprite sprite)
        {
            var tile = Asset<Tile>(Data + "/" + name + ".asset"); tile.sprite = sprite; tile.color = Color.white; tile.transform = Matrix4x4.identity; tile.colliderType = Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }
        static void BuildInspection(SkyIslandArt art)
        {
            if (File.Exists(Scene)) return;
            var map = Asset<PuzzleMap>(Data + "/Inspection.asset"); var rows = JsonUtility.FromJson<IslandLayout>(File.ReadAllText("Docs/Validation/StarwayIsland/join_layout.json")).rows;
            map.Resize(rows[0].Length, rows.Length); map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(PuzzleBuilder.Data + "/Themes/StarGarden.asset"); map.terrain.Clear();
            for (var y = 0; y < rows.Length; y++) for (var x = 0; x < rows[y].Length; x++) if (rows[y][x] == '1') map.SetTerrain(new Vector2Int(x, rows.Length - 1 - y), TerrainMaterial.Grass);
            map.start = map.exit = map.terrain[0].position; map.targetBubbles = 0; EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var setup = EditorSceneManager.GetSceneManagerSetup(); if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before creating inspection.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Inspection Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.transform.position = new Vector3(5, 3.4125f, -10); camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 4.0875f;
            new GameObject("Main Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            var renderer = new GameObject("Island inspection").AddComponent<SkyIslandMapRenderer>(); renderer.Bind(map, art); renderer.PopulateAll();
            var host = renderer.gameObject.AddComponent<SkyIslandInspection>(); host.map = map; host.art = art;
            EditorSceneManager.SaveScene(scene, Scene); EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
