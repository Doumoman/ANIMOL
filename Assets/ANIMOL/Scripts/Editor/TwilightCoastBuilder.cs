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
    [Serializable] public class TwilightCoastManifest
    {
        public IslandMaterial[] materials;
        public TheaterManifestProp[] props;
        public TheaterManifestAnimation[] animations;
    }
    public sealed class TwilightCoastImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 23000;
        public static bool Matches(string path) => new[] { TwilightCoastBuilder.Tiles, TwilightCoastBuilder.Props, TwilightCoastBuilder.Backgrounds }.Any(root => path.StartsWith(root + "/", StringComparison.Ordinal));
        public static Vector2 Pivot(string path)
        {
            var manifest = TwilightCoastBuilder.Read();
            var prop = manifest.props.FirstOrDefault(p => path.EndsWith("/" + Path.GetFileName(p.file), StringComparison.Ordinal));
            if (prop != null) return new Vector2(prop.pivot[0], prop.pivot[1]);
            if (Path.GetFileName(path).StartsWith("horizon_")) return new Vector2(0,1);
            return path.Contains("/Animation/palm_") ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
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
    public static class TwilightCoastBuilder
    {
        public const string Source = "Docs/Inbox/TwilightCoast/ANIMOL_TWILIGHT_COAST_V02";
        public const string Tiles = "Assets/ANIMOL/Sprites/Tiles/Floor/twilight_sea/TwilightCoast";
        public const string Props = "Assets/ANIMOL/Sprites/Props/twilight_sea/TwilightCoast";
        public const string Backgrounds = "Assets/ANIMOL/Sprites/Background/twilight_sea/TwilightCoast";
        public const string Data = "Assets/ANIMOL/Data/Art/TwilightCoast";
        public const string Prefabs = "Assets/ANIMOL/Prefabs/Environment/TwilightCoast";
        public const string Catalog = Data + "/EnvironmentArt.asset";
        public const string InspectionScene = "Assets/ANIMOL/Scenes/ANIMOL_TwilightCoast_Environment_QA.unity";
        public const string PreviewScene = "Assets/ANIMOL/Scenes/ANIMOL_TwilightCoast_Puzzle_QA.unity";
        public static TwilightCoastManifest Read() => JsonUtility.FromJson<TwilightCoastManifest>(File.ReadAllText(Source + "/manifest.json"));
        public static string RuntimePath(string source)
        {
            if (source.StartsWith("tiles/")) return Tiles + source.Substring(5);
            if (source.StartsWith("props/")) return Props + source.Substring(5);
            if (source.StartsWith("animation/")) return Props + "/Animation/" + source.Substring(10);
            return Backgrounds + source.Substring(11);
        }
        [MenuItem("ANIMOL/Art QA/Apply twilight coast environment")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play and save current scene first.");
            var manifest = Read();
            foreach (var material in manifest.materials) foreach (var blob in material.blob47) Copy(blob.file);
            foreach (var part in manifest.props) Copy(part.file);
            foreach (var anim in manifest.animations) foreach (var frame in anim.frames) Copy(frame.file);
            foreach (var layer in new[] { "far", "mid", "near" }) Copy("backgrounds/" + layer + "_repeat_960.png");
            Directory.CreateDirectory(Data); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var art = Asset<TwilightCoastArt>(Catalog);
            art.blob47 = manifest.materials.First(m => m.key == "sand").blob47.Select(b => new CoastBlob { outer = b.outer, inner = b.inner, sand = Tile("sand", b), wetSandBody = Tile("wet_sand_body", b) }).ToArray();
            art.props = manifest.props.Select(p => new TheaterProp { id = p.name, sprite = Sprite(p.file) }).ToArray();
            art.far = Sprite("backgrounds/far_repeat_960.png"); art.mid = Sprite("backgrounds/mid_repeat_960.png"); art.near = Sprite("backgrounds/near_repeat_960.png");
            art.parallax = new Vector3(.12f, .30f, .50f);
            art.compositor = AssetDatabase.LoadAssetAtPath<Material>("Assets/ANIMOL/Materials/SkyIslandComposite.mat");
            foreach (var file in new[] { "horizon_core_960x320", "horizon_without_sun_960x320", "horizon_side_left_repeat_80x320", "horizon_side_right_repeat_80x320" }) Copy("backgrounds/"+file+".png");
            AssetDatabase.Refresh();
            art.horizonCore=Sprite("backgrounds/horizon_core_960x320.png"); art.horizonWithoutSun=Sprite("backgrounds/horizon_without_sun_960x320.png");
            art.horizonLeft=Sprite("backgrounds/horizon_side_left_repeat_80x320.png"); art.horizonRight=Sprite("backgrounds/horizon_side_right_repeat_80x320.png");
            foreach(var anim in manifest.animations)
            {
                var seq=Asset<SpriteFrameSequence>(Data+"/"+anim.name+".asset"); seq.frames=anim.frames.Select(f=>Sprite(f.file)).ToArray(); seq.durations=anim.frames.Select(f=>f.durationMs/1000f).ToArray(); seq.loop=anim.loop; EditorUtility.SetDirty(seq);
                if(anim.name=="palm_idle") art.palmIdle=seq; else if(anim.name=="palm_touch") art.palmTouch=seq; else art.waterRipples=seq;
            }
            var horizonRoot=new GameObject("Single sunset horizon"); var horizon=horizonRoot.AddComponent<TwilightCoastHorizon>(); horizon.art=art; horizon.Assemble(2,2);
            PrefabUtility.SaveAsPrefabAsset(horizonRoot,Prefabs+"/Horizon.prefab"); UnityEngine.Object.DestroyImmediate(horizonRoot); art.horizonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Horizon.prefab");
            foreach(var id in new[] {"palm_160x160","rocks_160x80","rope_fence_160x80"})
            {
                var image=new GameObject(id).AddComponent<SpriteRenderer>(); image.sprite=art.Prop(id); image.sortingOrder=8;
                if(id.StartsWith("palm")) { var palm=image.gameObject.AddComponent<TwilightCoastPalm>(); palm.visual=image; palm.idle=art.palmIdle; }
                var path=Prefabs+"/"+id+".prefab"; PrefabUtility.SaveAsPrefabAsset(image.gameObject,path); UnityEngine.Object.DestroyImmediate(image.gameObject);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(id.StartsWith("palm")) art.palmPrefab=prefab; else if(id.StartsWith("rocks")) art.rocksPrefab=prefab; else art.fencePrefab=prefab;
            }
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
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
            var lens = new GameObject("Coast environment QA camera").AddComponent<Camera>(); lens.orthographic = true; lens.orthographicSize = size;
            lens.transform.position = new Vector3(p.x, p.y, -10); lens.backgroundColor = Color.black; lens.tag = "MainCamera";
            new GameObject("Directional Light").AddComponent<Light>().type = LightType.Directional; return lens;
        }
        static void BuildInspection(TwilightCoastArt art)
        {
            if (File.Exists(InspectionScene)) return;
            var map = Asset<PuzzleMap>(Data + "/JoinInspection.asset");
            var rows = new[] { "00011111000", "01111111110", "01110111100", "11110111100", "11111111100", "00001001110", "10001001110", "00001000010" };
            map.Resize(11, 8); map.terrain.Clear();
            for (var y = 0; y < rows.Length; y++) for (var x = 0; x < rows[y].Length; x++) if (rows[y][x] == '1') map.SetTerrain(new Vector2Int(x, rows.Length - 1 - y), TerrainMaterial.Grass);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); var lens = Camera(new Vector2(5, 3.45f), 4.05f);
            var root = new GameObject("Native coast floor join inspection"); var renderer = root.AddComponent<TwilightCoastRenderer>(); renderer.environmentEnabled = false; renderer.Bind(map, art);
            var host = root.AddComponent<TwilightCoastInspection>(); host.lens = lens; host.map = map; host.art = art; EditorSceneManager.SaveScene(scene, InspectionScene);
        }
        static void BuildPreview(TwilightCoastArt art)
        {
            if (File.Exists(PreviewScene)) return;
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset"); var map = UnityEngine.Object.Instantiate(original); map.name = "TwilightCoastPreview";
            var theme = UnityEngine.Object.Instantiate(original.theme); theme.name = "TwilightCoast"; theme.skyIsland = null; theme.memorySkyTheater = null; theme.clockDome = null; theme.twilightCoast = art;
            AssetDatabase.CreateAsset(theme, Data + "/PuzzleTheme.asset"); map.theme = theme; AssetDatabase.CreateAsset(map, Data + "/PreviewMap.asset");
            var originalScene = EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/ANIMOL_Puzzle.unity"); EditorSceneManager.SaveScene(originalScene, PreviewScene, true);
            var scene = EditorSceneManager.OpenScene(PreviewScene); UnityEngine.Object.FindFirstObjectByType<PuzzleController>().stage = map; EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static void ApplyExistingQa(TwilightCoastArt art)
        {
            var scene = EditorSceneManager.OpenScene(TwilightSeaArtBuilder.ScenePath);
            var existing = UnityEngine.Object.FindFirstObjectByType<TwilightCoastInspection>();
            if (existing) { existing.GetComponent<TwilightCoastRenderer>().Bind(existing.map, art); EditorSceneManager.SaveScene(scene); return; }
            var qa = UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();
            foreach (var image in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (image.name == "Flat floor cell" || image.name == "Backdrop behind flat board") UnityEngine.Object.DestroyImmediate(image.gameObject);
            var map = Asset<PuzzleMap>(Data + "/ExistingArtQaGround.asset"); map.Resize(13, 21); map.terrain.Clear();
            for (var y = 0; y < 21; y++) for (var x = 0; x < 13; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            var root = new GameObject("Twilight coast beach environment"); root.transform.position = new Vector3(-6, -11, 0);
            root.AddComponent<TwilightCoastRenderer>().Bind(map, art);
            var host = root.AddComponent<TwilightCoastInspection>(); host.lens = qa.qaCamera; host.map = map; host.art = art; host.hideBackground = false; host.environmentEnabled = true;
            EditorSceneManager.SaveScene(scene);
        }
    }
}
