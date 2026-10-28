using System;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public static class PuzzleBuilder
    {
        public const string Data = "Assets/ANIMOL/Data/Puzzle";
        public const string Art = "Assets/ANIMOL/Sprites/Tiles/Puzzle";
        public const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity";
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building maps.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene before building puzzle assets.");
            Directory.CreateDirectory(Data + "/Maps"); Directory.CreateDirectory(Data + "/Themes"); Directory.CreateDirectory(Art); AssetDatabase.Refresh();
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(Data + "/Themes/StarGarden.asset");
            if (!theme)
            {
            theme = Asset<PuzzleTheme>(Data + "/Themes/StarGarden.asset");
            theme.terrain = new TerrainArt[3];
            for (var m = 0; m < 3; m++)
            {
                var set = new TerrainArt { material = (TerrainMaterial)m };
                for (var mask = 0; mask < 16; mask++) set.outer[mask] = Tile(m, mask, -1);
                for (var corner = 0; corner < 4; corner++) set.inner[corner] = Tile(m, 0, corner);
                theme.terrain[m] = set;
            }
            theme.player = Sprite("Assets/ANIMOL/Sprites/Player/rabbit/Frames/spr_player_rabbit_idle_s.png");
            theme.bubble = FindSprite("soap_bubble_pink");
            theme.exit = FindSprite("exit_arch"); if (!theme.exit) theme.exit = Token("exit", new Color(.96f, .78f, .35f), false);
            theme.decoration = FindSprite("clover_idle_f00");
            theme.far = Sprite("Assets/ANIMOL/Sprites/Background/ranch/Far/spr_bg_ranch_far_night_sky_idle.png");
            theme.mid = Sprite("Assets/ANIMOL/Sprites/Background/ranch/Mid/spr_bg_ranch_mid_cloud_small_idle.png");
            theme.near = Sprite("Assets/ANIMOL/Sprites/Background/ranch/Far/spr_bg_ranch_far_star_large_idle.png");
            if (!theme.bubble) theme.bubble = Token("bubble", new Color(.55f, .88f, 1), true);
            EditorUtility.SetDirty(theme);
            }
            CreateMap("Small", 4, 4, theme, map => { map.start = new Vector2Int(0, 0); map.exit = new Vector2Int(3, 3); map.bubbles.Add(new Vector2Int(2, 1)); });
            CreateMap("Horizontal", 40, 7, theme, map =>
            { map.start = new Vector2Int(1, 3); map.exit = new Vector2Int(38, 3); map.bubbles.Add(new Vector2Int(30, 3)); for (var x = 2; x <= 30; x++) Device(map, new Vector2Int(x, 3), x % 2 == 0 ? DeviceKind.Conveyor : DeviceKind.Wind, Vector2Int.right); });
            CreateMap("Vertical", 7, 40, theme, map =>
            { map.start = new Vector2Int(3, 1); map.exit = new Vector2Int(3, 38); map.bubbles.Add(new Vector2Int(3, 30)); for (var y = 2; y <= 30; y++) Device(map, new Vector2Int(3, y), y % 2 == 0 ? DeviceKind.Conveyor : DeviceKind.Wind, Vector2Int.up); });
            CreateMap("CorridorIslands", 14, 18, theme, map =>
            {
                map.terrain.Clear(); for (var y = 1; y < 17; y++) map.SetTerrain(new Vector2Int(3, y), TerrainMaterial.Grass);
                for (var x = 3; x <= 11; x++) map.SetTerrain(new Vector2Int(x, 8), TerrainMaterial.Grass);
                map.SetTerrain(new Vector2Int(10, 12), TerrainMaterial.Stone); map.start = new Vector2Int(3, 1); map.exit = new Vector2Int(11, 8); map.bubbles.Add(new Vector2Int(3, 8));
            });
            CreateMap("UHole", 14, 18, theme, map =>
            {
                map.terrain.Clear(); for (var y = 3; y <= 13; y++) for (var x = 2; x <= 10; x++) if (x < 5 || x > 7 || y < 7) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
                map.start = new Vector2Int(3, 4); map.exit = new Vector2Int(9, 12); map.bubbles.Add(new Vector2Int(6, 5));
            });
            CreateMap("MaterialBoundary", 14, 18, theme, map =>
            {
                for (var y = 0; y < map.Height; y++) for (var x = 7; x < map.Width; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Stone);
                map.start = new Vector2Int(6, 8); map.exit = new Vector2Int(12, 15); map.bubbles.Add(new Vector2Int(7, 8));
                map.SetTerrain(new Vector2Int(9, 9), TerrainMaterial.Sand, true);
            });
            CreateMap("EdgePortal", 40, 12, theme, map =>
            {
                map.start = new Vector2Int(1, 5); map.exit = new Vector2Int(38, 8); map.bubbles.Add(new Vector2Int(39, 5));
                Device(map, new Vector2Int(2, 5), DeviceKind.Ice, Vector2Int.right);
                Device(map, new Vector2Int(3, 5), DeviceKind.Portal, Vector2Int.right, new Vector2Int(38, 5));
                Device(map, new Vector2Int(38, 5), DeviceKind.Portal, Vector2Int.right, new Vector2Int(3, 5));
                Device(map, new Vector2Int(39, 5), DeviceKind.Ice, Vector2Int.right);
            });
            var demo = CreateMap("StarGarden", 18, 26, theme, map =>
            {
                map.start = new Vector2Int(2, 2); map.exit = new Vector2Int(15, 23); map.targetBubbles = 3;
                map.bubbles.AddRange(new[] { new Vector2Int(9, 2), new Vector2Int(15, 13), new Vector2Int(15, 23) });
                for (var x = 3; x <= 8; x++) Device(map, new Vector2Int(x, 2), DeviceKind.Conveyor, Vector2Int.right);
                for (var y = 3; y <= 7; y++) Device(map, new Vector2Int(9, y), DeviceKind.Wind, Vector2Int.up);
                Device(map, new Vector2Int(10, 8), DeviceKind.Ice, Vector2Int.right);
                Device(map, new Vector2Int(11, 8), DeviceKind.Portal, Vector2Int.right, new Vector2Int(14, 13));
                Device(map, new Vector2Int(14, 13), DeviceKind.Portal, Vector2Int.right, new Vector2Int(11, 8));
                for (var y = 17; y <= 22; y++) Device(map, new Vector2Int(15, y), DeviceKind.Conveyor, Vector2Int.up);
                for (var y = 10; y < 20; y++) for (var x = 3; x < 8; x++) map.RemoveTerrain(new Vector2Int(x, y));
                map.decorations.Add(new DecorationCell { position = new Vector2Int(4, 5), tint = Color.white });
            });
            AssetDatabase.SaveAssets();
            if (!File.Exists(ScenePath))
            {
                var setup = EditorSceneManager.GetSceneManagerSetup();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); PuzzlePreview.CreateRoot(demo);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            Debug.Log("[ANIMOL Puzzle] Puzzle assets ready; existing authored maps, theme and scene preserved.");
        }
        static T Asset<T>(string path) where T : ScriptableObject
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset) return asset; if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Asset path is already used: " + path); asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        public static PuzzleMap CreateMap(string name, int w, int h, PuzzleTheme theme, Action<PuzzleMap> configure)
        {
            var path = Data + "/Maps/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<PuzzleMap>(path)) return AssetDatabase.LoadAssetAtPath<PuzzleMap>(path); // Do not overwrite authored maps.
            var map = Asset<PuzzleMap>(path); map.Resize(w, h); map.theme = theme; map.targetBubbles = 1;
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass));
            configure(map); map.InvalidateIndex(); EditorUtility.SetDirty(map); return map;
        }
        static void Device(PuzzleMap map, Vector2Int p, DeviceKind kind, Vector2Int direction, Vector2Int target = default)
            => map.SetDevice(new DeviceCell { position = p, kind = kind, direction = direction, portalTarget = target });
        static Sprite FindSprite(string name)
        {
            var row = ArtAssetBuilder.Read().assets.FirstOrDefault(a => a.id == name);
            if (row != null) return Sprite("Assets/ANIMOL/" + row.path);
            var path = AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/ANIMOL/Sprites" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith(".png", StringComparison.Ordinal)); return path == null ? null : Sprite(path);
        }
        static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static Sprite Tile(int material, int mask, int inner)
        {
            var name = "spr_tile_puzzle_" + ((TerrainMaterial)material).ToString().ToLowerInvariant() + (inner < 0 ? "_outer_" + mask.ToString("D2") : "_inner_" + inner);
            var path = Art + "/" + name + ".png";
            if (File.Exists(path)) return Sprite(path);
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var baseColor = material == 0 ? new Color(.24f, .53f, .39f) : material == 1 ? new Color(.4f, .45f, .6f) : new Color(.74f, .61f, .4f);
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
            {
                var color = baseColor * (1 + ((x * 7 + y * 13) % 17 == 0 ? .08f : 0)); color.a = 1;
                var edge = (mask & 1) != 0 && y >= 29 || (mask & 2) != 0 && x >= 29 || (mask & 4) != 0 && y < 3 || (mask & 8) != 0 && x < 3;
                if (edge) { color = baseColor * .55f; color.a = 1; }
                if (inner >= 0)
                {
                    var dx = inner == 0 || inner == 1 ? 31 - x : x; var dy = inner == 0 || inner == 3 ? 31 - y : y;
                    if (dx + dy < 6) color = Color.clear;
                    else if (dx + dy < 9) { color = baseColor * .55f; color.a = 1; }
                }
                texture.SetPixel(x, y, color);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); return Sprite(path);
        }
        static Sprite Token(string name, Color color, bool circle)
        {
            var path = Art + "/spr_puzzle_" + name + ".png"; if (File.Exists(path)) return Sprite(path);
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
            { var r = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)); texture.SetPixel(x, y, circle ? (r > 13 ? Color.clear : r > 10 ? color : new Color(color.r, color.g, color.b, .35f)) : (x < 5 || x > 26 || y < 3 || y > 29 ? Color.clear : x < 8 || x > 23 || y > 26 ? color : new Color(.13f, .15f, .23f))); }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path); return Sprite(path);
        }
    }
}
