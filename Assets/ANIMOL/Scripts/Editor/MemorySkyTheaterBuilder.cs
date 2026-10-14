using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Editor
{
    [Serializable] public class TheaterManifest
    {
        public IslandMaterial[] materials;
        public TheaterManifestProp[] props;
        public TheaterManifestAnimation[] animations;
    }
    [Serializable] public class TheaterManifestProp { public string name, file; public float[] pivot; public int[] size; }
    [Serializable] public class TheaterManifestAnimation { public string name; public TheaterManifestFrame[] frames; public bool loop; }
    [Serializable] public class TheaterManifestFrame { public string file; public int durationMs; }
    [Serializable] public class TheaterExample { public string[] structuralGround, carpet, walkableExample; public TheaterImagePlacement[] propPlacements; }
    [Serializable] public class TheaterImagePlacement { public string asset; public int[] topLeftImagePixels, spriteSize; }

    public sealed class MemorySkyTheaterImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 21000;
        public static bool Matches(string path) => new[] { MemorySkyTheaterBuilder.Tiles, MemorySkyTheaterBuilder.Props, MemorySkyTheaterBuilder.Backgrounds }.Any(root => path.StartsWith(root + "/", StringComparison.Ordinal));
        public static Vector2 Pivot(string path)
        {
            var manifest = MemorySkyTheaterBuilder.Read();
            var prop = manifest.props.FirstOrDefault(p => path.EndsWith("/" + Path.GetFileName(p.file), StringComparison.Ordinal));
            if (prop != null) return new Vector2(prop.pivot[0], prop.pivot[1]);
            return path.Contains("/Animation/") ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
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
    public static class MemorySkyTheaterBuilder
    {
        public const string Source = "Docs/Inbox/MemorySkyTheater/ANIMOL_MEMORY_SKYTHEATER_V02";
        public const string Tiles = "Assets/ANIMOL/Sprites/Tiles/Floor/memory_theater/SkyTheater";
        public const string Props = "Assets/ANIMOL/Sprites/Props/memory_theater/SkyTheater";
        public const string Backgrounds = "Assets/ANIMOL/Sprites/Background/memory_theater/SkyTheater";
        public const string Data = "Assets/ANIMOL/Data/Art/MemorySkyTheater";
        public const string Prefabs = "Assets/ANIMOL/Prefabs/Environment/MemoryTheater";
        public const string InspectionScene = "Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Environment_QA.unity";
        public const string PreviewScene = "Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Puzzle_QA.unity";
        public const string Catalog = Data + "/EnvironmentArt.asset";
        public static TheaterManifest Read() => JsonUtility.FromJson<TheaterManifest>(File.ReadAllText(Source + "/manifest.json"));
        public static string RuntimePath(string source)
        {
            if (source.StartsWith("tiles/")) return Tiles + source.Substring(5);
            if (source.StartsWith("props/")) return Props + source.Substring(5);
            if (source.StartsWith("animation/")) return Props + "/Animation/" + source.Substring(10);
            return Backgrounds + source.Substring(11);
        }
        [MenuItem("ANIMOL/Art QA/Apply memory theater environment")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play and save the current scene first.");
            var manifest = Read();
            foreach (var material in manifest.materials) foreach (var blob in material.blob47) Copy(blob.file);
            foreach (var prop in manifest.props) Copy(prop.file);
            foreach (var animation in manifest.animations) foreach (var frame in animation.frames) Copy(frame.file);
            foreach (var layer in new[] { "far", "mid", "near" }) Copy("backgrounds/" + layer + "_repeat_960.png");
            Directory.CreateDirectory(Data); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var art = Asset<MemorySkyTheaterArt>(Catalog);
            art.blob47 = manifest.materials.First(m => m.key == "wood").blob47.Select(blob => new TheaterBlob {
                outer = blob.outer, inner = blob.inner, wood = Tile("wood", blob), underside = Tile("underside", blob), velvet = Tile("velvet", blob)
            }).ToArray();
            art.props = manifest.props.Select(p => new TheaterProp { id = p.name, sprite = Sprite(p.file) }).ToArray();
            art.far = Sprite("backgrounds/far_repeat_960.png"); art.mid = Sprite("backgrounds/mid_repeat_960.png"); art.near = Sprite("backgrounds/near_repeat_960.png");
            art.compositor = AssetDatabase.LoadAssetAtPath<Material>("Assets/ANIMOL/Materials/SkyIslandComposite.mat");
            foreach (var anim in manifest.animations)
            {
                var seq = Asset<SpriteFrameSequence>(Data + "/" + anim.name + ".asset"); seq.frames = anim.frames.Select(f => Sprite(f.file)).ToArray();
                seq.durations = anim.frames.Select(f => f.durationMs / 1000f).ToArray(); seq.loop = anim.loop; EditorUtility.SetDirty(seq);
                if (anim.name == "curtain_left_idle") art.curtainLeft = seq; else if (anim.name == "curtain_right_idle") art.curtainRight = seq; else art.footlights = seq;
            }
            BuildPrefabs(art);
            var layout = Asset<MemorySkyTheaterLayout>(Data + "/PreviewLayout.asset"); layout.fitBorderToMap = true; layout.visualDeck.Clear(); layout.velvet.Clear(); layout.props.Clear();
            for (var y = -3; y <= 6; y++) for (var x = 0; x < 4; x++) if (y < 0 || y >= 4) layout.visualDeck.Add(new Vector2Int(x, y));
            for (var y = 0; y < 4; y++) for (var x = 1; x < 3; x++) layout.velvet.Add(new Vector2Int(x, y));
            Place(layout, "Frame_4", new Vector2(-.5f, 6.5f)); Place(layout, "organ_160x160", new Vector2(1.5f, 4.5f));
            Place(layout, "seat_pair_160x80", new Vector2(.5f, -2.45f)); Place(layout, "seat_pair_160x80", new Vector2(2.5f, -2.45f));
            for (var x = 0; x < 4; x++) Place(layout, "footlights_80x80", new Vector2(x, -1.25f));
            art.defaultLayout = layout; EditorUtility.SetDirty(layout); EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try { BuildInspection(art); BuildPreview(art); ApplyExistingQa(art); }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void Copy(string source)
        {
            var target = RuntimePath(source); Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) { if (!File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(Source + "/" + source))) throw new InvalidOperationException("Edited source target: " + target); return; }
            File.Copy(Source + "/" + source, target);
        }
        static T Asset<T>(string path) where T : ScriptableObject
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value) return value; value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value; }
        static Sprite Sprite(string source) => AssetDatabase.LoadAssetAtPath<Sprite>(RuntimePath(source));
        static Tile Tile(string material, IslandBlob blob)
        {
            var name = Path.GetFileNameWithoutExtension(blob.file); var tile = Asset<Tile>(Data + "/" + material + "_" + name + ".asset");
            tile.sprite = Sprite("tiles/" + material + "/blob47/" + name + ".png"); tile.color = Color.white; tile.transform = Matrix4x4.identity;
            tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None; EditorUtility.SetDirty(tile); return tile;
        }
        static SpriteRenderer Image(Transform parent, string name, Sprite sprite, Vector2 position, int order)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = Color.white; renderer.sortingOrder = order; return renderer;
        }
        static void Animate(SpriteRenderer renderer, SpriteFrameSequence sequence)
        { var player = renderer.gameObject.AddComponent<MemorySkyTheaterAnimation>(); player.visual = renderer; player.sequence = sequence; }
        static void SavePrefab(GameObject root, string name)
        { PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab"); UnityEngine.Object.DestroyImmediate(root); }
        static void BuildPrefabs(MemorySkyTheaterArt art)
        {
            foreach (var cells in new[] { 3, 4, 5, 9, 13 })
            {
                var root = new GameObject("Frame_" + cells); var n = cells - 2;
                var frame = root.AddComponent<MemorySkyTheaterFrame>();
                Animate(Image(root.transform, "Left fixed pillar and cloth", art.Prop("curtain_wing_left_80x160"), new Vector2(.5f, -2), 9), art.curtainLeft);
                for (var k = 0; k < n; k++) frame.lintels.Add(Image(root.transform, "Lintel " + k, art.Prop("lintel_repeat_80"), new Vector2(1.5f + k, 0), 9));
                frame.right = Image(root.transform, "Right fixed pillar and cloth", art.Prop("curtain_wing_right_80x160"), new Vector2(n + 1.5f, -2), 9); Animate(frame.right, art.curtainRight);
                SavePrefab(root, root.name);
            }
            foreach (var id in new[] { "organ_160x160", "seat_pair_160x80", "footlights_80x80" })
            {
                var root = new GameObject(id); var renderer = Image(root.transform, id, art.Prop(id), Vector2.zero, 8);
                if (id == "footlights_80x80") Animate(renderer, art.footlights);
                SavePrefab(root, id);
            }
        }
        static void Place(MemorySkyTheaterLayout layout, string name, Vector2 position)
        { layout.props.Add(new TheaterPlacement { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab"), position = position }); }
        static Camera Camera(Vector2 center, float halfHeight)
        {
            var lens = new GameObject("Environment inspection camera", typeof(Camera)).GetComponent<Camera>(); lens.tag = "MainCamera";
            lens.orthographic = true; lens.orthographicSize = halfHeight; lens.transform.position = new Vector3(center.x, center.y, -10);
            new GameObject("Main Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional; return lens;
        }
        static void BuildInspection(MemorySkyTheaterArt art)
        {
            if (File.Exists(InspectionScene)) return;
            var map = Asset<PuzzleMap>(Data + "/JoinInspection.asset"); var rows = JsonUtility.FromJson<IslandLayout>(File.ReadAllText("Docs/Validation/MemorySkyTheater/join_layout.json")).rows;
            map.Resize(rows[0].Length, rows.Length); map.terrain.Clear();
            for (var y = 0; y < rows.Length; y++) for (var x = 0; x < rows[y].Length; x++) if (rows[y][x] == '1') map.SetTerrain(new Vector2Int(x, rows.Length - 1 - y), TerrainMaterial.Grass);
            var layout = Asset<MemorySkyTheaterLayout>(Data + "/JoinLayout.asset"); layout.velvet.Clear(); layout.velvet.AddRange(map.terrain.Select(t => t.position)); EditorUtility.SetDirty(layout); EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); var lens = Camera(new Vector2(5, 3.4125f), 4.0875f);
            var root = new GameObject("Native theater join inspection"); var renderer = root.AddComponent<MemorySkyTheaterRenderer>(); renderer.Bind(map, art, layout);
            renderer.Velvet.GetComponent<TilemapRenderer>().enabled = false;
            var host = root.AddComponent<MemorySkyTheaterInspection>(); host.lens = lens; host.map = map; host.art = art; host.layout = layout;
            EditorSceneManager.SaveScene(scene, InspectionScene);
        }
        static void BuildPreview(MemorySkyTheaterArt art)
        {
            if (File.Exists(PreviewScene)) return;
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset");
            var map = UnityEngine.Object.Instantiate(original); map.name = "MemoryTheaterPreview";
            var theme = UnityEngine.Object.Instantiate(original.theme); theme.name = "MemoryTheater"; theme.skyIsland = null; theme.memorySkyTheater = art;
            AssetDatabase.CreateAsset(theme, Data + "/PuzzleTheme.asset"); map.theme = theme; AssetDatabase.CreateAsset(map, Data + "/PreviewMap.asset");
            var originalScene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/ANIMOL_Puzzle.unity"); EditorSceneManager.SaveScene(originalScene, PreviewScene, true);
            var scene = EditorSceneManager.OpenScene(PreviewScene); var controller = UnityEngine.Object.FindFirstObjectByType<PuzzleController>(); controller.stage = map;
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static void ApplyExistingQa(MemorySkyTheaterArt art)
        {
            var scene = EditorSceneManager.OpenScene(MemoryTheaterArtBuilder.ScenePath);
            var replaced = UnityEngine.Object.FindObjectsByType<DeviceVisualView>(FindObjectsSortMode.None)
                .Where(v => Mathf.Abs(v.transform.position.x) < 12 && (v.name == "foundation_curtain_idle" || v.name == "foundation_lamp_idle"))
                .Select(v => v.visual).ToArray();
            var existing = UnityEngine.Object.FindFirstObjectByType<MemorySkyTheaterInspection>();
            if (existing) { existing.replacedEnvironment = replaced; EditorUtility.SetDirty(existing); EditorSceneManager.SaveScene(scene); return; }
            foreach (var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (r.name == "Flat floor cell" || r.name == "Backdrop - behind flat board") UnityEngine.Object.DestroyImmediate(r.gameObject);
                else if (r.transform.parent && (r.transform.parent.name.StartsWith("foundation_curtain_idle") || r.transform.parent.name.StartsWith("foundation_lamp_idle"))) r.forceRenderingOff = true;
            }
            var map = Asset<PuzzleMap>(Data + "/ExistingArtQaGround.asset"); map.Resize(13, 20); map.terrain.Clear();
            for (var y = 0; y < 20; y++) for (var x = 0; x < 13; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            var layout = Asset<MemorySkyTheaterLayout>(Data + "/ExistingArtQaLayout.asset"); layout.visualDeck.Clear(); layout.velvet.Clear(); layout.props.Clear();
            for (var y = 0; y < 20; y++) for (var x = 2; x <= 10; x++) layout.velvet.Add(new Vector2Int(x, y));
            for (var x = 0; x < 13; x++) for (var y = -2; y <= 21; y++) if (y < 0 || y >= 20) layout.visualDeck.Add(new Vector2Int(x, y));
            Place(layout, "Frame_13", new Vector2(-.5f, 21.5f)); Place(layout, "organ_160x160", new Vector2(6, 19.5f));
            Place(layout, "seat_pair_160x80", new Vector2(3, -1.45f)); Place(layout, "seat_pair_160x80", new Vector2(9, -1.45f));
            for (var x = 2; x <= 10; x++) Place(layout, "footlights_80x80", new Vector2(x, -.65f));
            EditorUtility.SetDirty(layout); EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var root = new GameObject("Memory theater modular environment"); root.transform.position = new Vector3(-6, -10, 0);
            var renderer = root.AddComponent<MemorySkyTheaterRenderer>(); renderer.Bind(map, art, layout);
            var host = root.AddComponent<MemorySkyTheaterInspection>(); host.lens = UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>().qaCamera; host.map = map; host.art = art; host.layout = layout; host.hideBackground = false; host.replacedEnvironment = replaced;
            EditorSceneManager.SaveScene(scene);
        }
    }
}
