using System;
using System.Collections.Generic;
using System.IO;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03
{
    /// <summary>Rebinds the existing theater environment catalog without changing its GUID or theme ID.</summary>
    public static class EchoTheaterV03ProjectBuilder
    {
        public const string Root = "Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_CARTOON_V03";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/MemorySkyTheater/Environment/EnvironmentArt.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Art/MemorySkyTheater/EnvironmentArt.asset";

        [Serializable] sealed class Manifest { public AssetRow[] assets; }
        [Serializable] sealed class AssetRow
        {
            public string file, sha256;
            public int width, height;
            public float ppu;
            public float[] pivot;
        }

        public static string ApplyEnvironment()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebinding theater art.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/manifest.json"));
            if (manifest?.assets == null || manifest.assets.Length != 1977)
                throw new InvalidDataException("The complete P01 V03 1977-sprite manifest is required.");
            var art = AssetDatabase.LoadAssetAtPath<EchoTheaterV02Art>(CatalogPath);
            var theme = AssetDatabase.LoadAssetAtPath<MemorySkyTheaterArt>(ThemePath);
            if (!art || !theme || theme.v02 != art)
                throw new InvalidDataException("Existing theater environment catalog binding is missing.");
            var entries = new List<EchoTheaterV02SpriteEntry>(manifest.assets.Length);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in manifest.assets)
            {
                if (row == null || !paths.Add(row.file) || row.pivot == null || row.pivot.Length != 2)
                    throw new InvalidDataException("Duplicate or malformed V03 sprite entry.");
                var path = Root + "/" + row.file;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!sprite || importer == null || sprite.rect.width != row.width || sprite.rect.height != row.height ||
                    Mathf.Abs(sprite.pixelsPerUnit - row.ppu) > .001f ||
                    Vector2.Distance(sprite.pivot, new Vector2(row.pivot[0] * row.width, row.pivot[1] * row.height)) > .03f ||
                    importer.filterMode != FilterMode.Bilinear || importer.mipmapEnabled ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.npotScale != TextureImporterNPOTScale.None ||
                    EchoTheaterV03VisualCatalog.Sha256Of(File.ReadAllBytes(path)) != row.sha256)
                    throw new InvalidDataException("V03 bytes or import settings differ: " + path);
                entries.Add(new EchoTheaterV02SpriteEntry { path = row.file, sprite = sprite });
            }
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/background_scene_layout.json");
            if (!layout) throw new InvalidDataException("V03 independent background layout is missing.");
            var animations = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(
                Root + "/_Generated/EchoTheaterV03VisualCatalog.asset");
            if (!animations) throw new InvalidDataException("Build the V03 animation catalog before binding the project.");
            EchoTheaterV03AnimationBuilder.ValidateBuiltCatalog(animations);
            art.sprites = entries.ToArray();
            art.backgroundLayout = layout;
            art.v03Animations = animations;
            art.v03ManifestSha256 = animations.SourceManifestSha256;
            art.v03DiceOrientationMap = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/dice_orientation_map.json");
            if (!art.v03DiceOrientationMap) throw new InvalidDataException("V03 dice orientation map is missing.");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("[Echo Theater V03] Existing theater catalog rebound to 1977 verified P01 V03 sprites and the independent background layout.");
            return "1977 P01 V03 environment sprites rebound";
        }
    }
}
