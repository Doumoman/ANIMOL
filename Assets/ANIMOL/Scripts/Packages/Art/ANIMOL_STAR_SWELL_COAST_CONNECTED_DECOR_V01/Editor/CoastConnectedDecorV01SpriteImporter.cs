#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.CoastConnectedDecorV01
{
    // Exact current manifest allowlist, restricted to the NEW addon root. No reimport
    // or SaveAssets calls occur inside this callback, so it cannot start import loops.
    public sealed class CoastConnectedDecorV01SpriteImporter : AssetPostprocessor
    {
        private sealed class Cache
        { public long manifestTicks, manifestBytes, pinTicks, pinBytes; public Dictionary<string, CoastConnectedDecorV01AssetInput> rows; }
        private static readonly Dictionary<string, Cache> ByRoot = new Dictionary<string, Cache>(StringComparer.Ordinal);
        // Explicit builder refresh also handles copies that retain file mtime/length.
        public static void InvalidateRoot(string root) { ByRoot.Remove(root); }
        private static Dictionary<string, CoastConnectedDecorV01AssetInput> ExactRows(string root)
        {
            var manifest = new FileInfo(CoastConnectedDecorV01CatalogBuilder.FullPath(root + "/" + CoastConnectedDecorV01Catalog.ManifestName));
            var pin = new FileInfo(CoastConnectedDecorV01CatalogBuilder.FullPath(root + "/" + CoastConnectedDecorV01Catalog.PinName));
            Cache cached;
            if (manifest.Exists && pin.Exists && ByRoot.TryGetValue(root, out cached) && cached.manifestTicks == manifest.LastWriteTimeUtc.Ticks
                && cached.manifestBytes == manifest.Length && cached.pinTicks == pin.LastWriteTimeUtc.Ticks && cached.pinBytes == pin.Length) return cached.rows;
            string hash; var input = CoastConnectedDecorV01CatalogBuilder.LoadManifest(root, out hash);
            var rows = new Dictionary<string, CoastConnectedDecorV01AssetInput>(StringComparer.Ordinal);
            foreach (var row in input.assets) rows.Add(row.file, row);
            ByRoot[root] = new Cache { manifestTicks = manifest.LastWriteTimeUtc.Ticks, manifestBytes = manifest.Length,
                pinTicks = pin.LastWriteTimeUtc.Ticks, pinBytes = pin.Length, rows = rows };
            return rows;
        }
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            string marker = "/" + CoastConnectedDecorV01Catalog.PackageId + "/";
            int at = path.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0 || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            string root = path.Substring(0, at + marker.Length - 1), relative = path.Substring(at + marker.Length);
            var importer = (TextureImporter)assetImporter;
            // Native originals / Inspection / undeclared stale PNGs are never Sprite candidates.
            importer.textureType = TextureImporterType.Default;
            try
            {
                CoastConnectedDecorV01AssetInput row;
                if (!ExactRows(root).TryGetValue(relative, out row)) return;
                CoastConnectedDecorV01CatalogBuilder.ValidateSourcePng(root, row);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = row.ppu; importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.crunchedCompression = false;
                importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 8192;
                foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(row.pivot[0], row.pivot[1]);
                settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
            }
            catch (Exception e)
            {
                importer.textureType = TextureImporterType.Default; ByRoot.Remove(root);
                Debug.LogError("[Coast Connected Decor V01] Current art import rejected; no stale Sprite fallback: " + relative + " / " + e.Message);
            }
        }
    }
}
#endif
