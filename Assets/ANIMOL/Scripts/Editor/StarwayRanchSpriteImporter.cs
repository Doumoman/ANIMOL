using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art
{
    // The V10 JSON is the import authority. Keep this scoped to the new ranch sprites.
    public sealed class StarwayRanchSpriteImporter : AssetPostprocessor
    {
        const string Prefix = "Assets/ANIMOL/Sprites/StarwayRanch/";
        const string Settings = "Assets/ANIMOL/Data/Art/StarwayRanch/sprite_import_settings.json";

        [Serializable] sealed class Entry { public string file; public float ppu; public float[] pivot; }
        [Serializable] sealed class Document { public Entry[] spriteSettings; }
        static Dictionary<string, Entry> entries;

        public override int GetPostprocessOrder() => 20000;

        static Dictionary<string, Entry> Specs()
        {
            if (entries != null) return entries;
            var document = JsonUtility.FromJson<Document>(File.ReadAllText(Settings));
            entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in document.spriteSettings) entries.Add(entry.file, entry);
            return entries;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Prefix, StringComparison.Ordinal)) return;
            var relative = assetPath.Substring(Prefix.Length);
            if (!Specs().TryGetValue(relative, out var entry))
                throw new InvalidDataException("Missing V10 sprite import setting: " + relative);
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = entry.ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 8192;
            foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" })
                importer.ClearPlatformTextureSettings(platform);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
