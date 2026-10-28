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
    [Serializable] public class ClockDomeManifest
    {
        public IslandMaterial[] materials;
        public TheaterManifestProp[] parts;
        public TheaterManifestAnimation[] animations;
    }
    public sealed class ClockDomeImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 22000;
        public static bool Matches(string path) => new[] { ClockDomeBuilder.Tiles, ClockDomeBuilder.Props, ClockDomeBuilder.Backgrounds }.Any(root => path.StartsWith(root + "/", StringComparison.Ordinal));
        public static Vector2 Pivot(string path)
        {
            var manifest = ClockDomeBuilder.Read();
            var prop = manifest.parts.FirstOrDefault(p => path.EndsWith("/" + Path.GetFileName(p.file), StringComparison.Ordinal));
            if (prop != null) return new Vector2(prop.pivot[0], prop.pivot[1]);
            return path.Contains("/Animation/") ? new Vector2(.5f, 1) : new Vector2(.5f, .5f);
        }
        void OnPreprocessTexture()
        {
            if (!Matches(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 80;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 8192;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false; importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = Pivot(assetPath); settings.spriteExtrude = 1; importer.SetTextureSettings(settings);
            foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
        }
    }
    public static class ClockDomeBuilder
    {
        public const string Source = "Docs/Inbox/ClockDome/ANIMOL_CLOCK_DOME_V01";
        public const string Tiles = "Assets/ANIMOL/Sprites/Tiles/Floor/letter_observatory/ClockDome";
        public const string Props = "Assets/ANIMOL/Sprites/Props/letter_observatory/ClockDome";
        public const string Backgrounds = "Assets/ANIMOL/Sprites/Background/letter_observatory/ClockDome";
        public const string Data = "Assets/ANIMOL/Data/Art/ClockDome";
        public const string Prefabs = "Assets/ANIMOL/Prefabs/Environment/ClockDome";
        public const string Catalog = Data + "/EnvironmentArt.asset";
        public const string InspectionScene = "Assets/ANIMOL/Scenes/QA/ANIMOL_ClockDome_Environment_QA.unity";
        public const string PreviewScene = "Assets/ANIMOL/Scenes/QA/ANIMOL_ClockDome_Puzzle_QA.unity";
        public static ClockDomeManifest Read() => JsonUtility.FromJson<ClockDomeManifest>(File.ReadAllText(Source + "/manifest.json"));
        public static string RuntimePath(string source)
        {
            if (source.StartsWith("tiles/")) return Tiles + source.Substring(5);
            if (source.StartsWith("parts/")) return Props + source.Substring(5);
            if (source.StartsWith("animation/")) return Props + "/Animation/" + source.Substring(10);
            return Backgrounds + source.Substring(11);
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play and save current scene first.");
            var manifest = Read();
            foreach (var material in manifest.materials) foreach (var blob in material.blob47) Copy(blob.file);
            foreach (var part in manifest.parts) Copy(part.file);
            foreach (var anim in manifest.animations) foreach (var frame in anim.frames) Copy(frame.file);
            foreach (var layer in new[] { "far", "mid", "near" }) Copy("backgrounds/" + layer + "_repeat_960.png");
            Directory.CreateDirectory(Data); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var art = Asset<ClockDomeArt>(Catalog);
            art.blob47 = manifest.materials.First(m => m.key == "floor").blob47.Select(b => new ClockDomeBlob { outer = b.outer, inner = b.inner, floor = Tile("floor", b), underside = Tile("underside", b) }).ToArray();
            art.parts = manifest.parts.Select(p => new TheaterProp { id = p.name, sprite = Sprite(p.file) }).ToArray();
            art.far = Sprite("backgrounds/far_repeat_960.png"); art.mid = Sprite("backgrounds/mid_repeat_960.png"); art.near = Sprite("backgrounds/near_repeat_960.png");
            art.parallax = new Vector3(.15f, .30f, .45f);
            art.compositor = AssetDatabase.LoadAssetAtPath<Material>("Assets/ANIMOL/Materials/SkyIslandComposite.mat");
            var animation = manifest.animations.Single(); var seq = Asset<SpriteFrameSequence>(Data + "/pendulum_idle.asset");
            seq.frames = animation.frames.Select(f => Sprite(f.file)).ToArray(); seq.durations = animation.frames.Select(f => f.durationMs / 1000f).ToArray(); seq.loop = animation.loop; art.pendulum = seq; EditorUtility.SetDirty(seq);
            foreach (var rows in new[] { 0, 4, 12 })
            {
                var root = new GameObject("Clock Dome " + rows + " rows"); var room = root.AddComponent<ClockDomeRoom>(); room.art = art; room.Assemble(rows);
                var path = Prefabs + "/Room_" + rows + ".prefab"; PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
                if (rows == 4) art.roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            var telescope = new GameObject("Telescope on nonwalkable deck").AddComponent<SpriteRenderer>(); telescope.sprite = art.Part("telescope_160x160"); telescope.sortingOrder = 8;
            PrefabUtility.SaveAsPrefabAsset(telescope.gameObject, Prefabs + "/Telescope.prefab"); UnityEngine.Object.DestroyImmediate(telescope.gameObject);
            art.telescopePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Telescope.prefab"); EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try { BuildInspection(art); BuildPreview(art); ApplyExistingQa(art); }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void Copy(string source)
        {
            var target = RuntimePath(source); Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) { if (!File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(Source + "/" + source))) throw new InvalidOperationException("Edited target: " + target); return; }
            File.Copy(Source + "/" + source, target);
        }
        static T Asset<T>(string path) where T : ScriptableObject
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value) return value; value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value; }
        static Sprite Sprite(string source) => AssetDatabase.LoadAssetAtPath<Sprite>(RuntimePath(source));
        static Tile Tile(string material, IslandBlob blob)
        {
            var name = Path.GetFileNameWithoutExtension(blob.file); var tile = Asset<Tile>(Data + "/" + material + "_" + name + ".asset");
            tile.sprite = Sprite("tiles/" + material + "/blob47/" + name + ".png"); tile.color = Color.white; tile.transform = Matrix4x4.identity; tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }
        static Camera Camera(Vector2 p, float size)
        {
            var lens = new GameObject("Clock environment QA camera").AddComponent<Camera>(); lens.orthographic = true; lens.orthographicSize = size;
            lens.transform.position = new Vector3(p.x, p.y, -10); lens.backgroundColor = Color.black; lens.tag = "MainCamera";
            new GameObject("Directional Light").AddComponent<Light>().type = LightType.Directional; return lens;
        }
        static void BuildInspection(ClockDomeArt art)
        {
            if (File.Exists(InspectionScene)) return;
            var map = Asset<PuzzleMap>(Data + "/JoinInspection.asset");
            var rows = new[] { "00011111000", "01111111110", "01110111110", "11110111100", "11111111100", "00001001110", "10001001110", "00001000010" };
            map.Resize(11, 8); map.terrain.Clear();
            for (var y = 0; y < rows.Length; y++) for (var x = 0; x < rows[y].Length; x++) if (rows[y][x] == '1') map.SetTerrain(new Vector2Int(x, rows.Length - 1 - y), TerrainMaterial.Grass);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); var lens = Camera(new Vector2(5, 3.425f), 4.075f);
            var root = new GameObject("Native clock floor join inspection"); var renderer = root.AddComponent<ClockDomeRenderer>(); renderer.architectureEnabled = false; renderer.Bind(map, art);
            var host = root.AddComponent<ClockDomeInspection>(); host.lens = lens; host.map = map; host.art = art; EditorSceneManager.SaveScene(scene, InspectionScene);
        }
        static void BuildPreview(ClockDomeArt art)
        {
            if (File.Exists(PreviewScene)) return;
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset"); var map = UnityEngine.Object.Instantiate(original); map.name = "ClockDomePreview";
            var theme = UnityEngine.Object.Instantiate(original.theme); theme.name = "ClockDome"; theme.skyIsland = null; theme.memorySkyTheater = null; theme.clockDome = art;
            AssetDatabase.CreateAsset(theme, Data + "/PuzzleTheme.asset"); map.theme = theme; AssetDatabase.CreateAsset(map, Data + "/PreviewMap.asset");
            var originalScene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity"); EditorSceneManager.SaveScene(originalScene, PreviewScene, true);
            var scene = EditorSceneManager.OpenScene(PreviewScene); UnityEngine.Object.FindFirstObjectByType<PuzzleController>().stage = map; EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static void ApplyExistingQa(ClockDomeArt art)
        {
            var scene = EditorSceneManager.OpenScene(LetterObservatoryArtBuilder.ScenePath);
            if (UnityEngine.Object.FindFirstObjectByType<ClockDomeInspection>()) return;
            var qa = UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();
            // Keep every device/state/prefab reference intact. Hide only the old page-zero telescope decoration at runtime.
            var replaced = qa.views.Where(v => Mathf.Abs(v.transform.position.x) < 12 && v.name == "foundation_telescope_static").Select(v => v.visual).ToArray();
            foreach (var image in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (image.name == "Flat floor cell" || image.name == "Backdrop behind flat board") UnityEngine.Object.DestroyImmediate(image.gameObject);
            var map = Asset<PuzzleMap>(Data + "/ExistingArtQaGround.asset"); map.Resize(13, 21); map.terrain.Clear();
            for (var y = 0; y < 21; y++) for (var x = 0; x < 13; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var root = new GameObject("Clock dome observatory environment"); root.transform.position = new Vector3(-6, -11, 0);
            root.AddComponent<ClockDomeRenderer>().Bind(map, art);
            var host = root.AddComponent<ClockDomeInspection>(); host.lens = qa.qaCamera; host.map = map; host.art = art; host.hideBackground = false; host.architectureEnabled = true; host.replacedEnvironment = replaced;
            EditorSceneManager.SaveScene(scene);
        }
    }
}
