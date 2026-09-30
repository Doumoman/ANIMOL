using System;
using System.Collections.Generic;
using System.IO;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Animol.Editor
{
    public static class AnimolTerrainTileIntegration
    {
        public const string CatalogPath = "Assets/ANIMOL/Resources/ANIMOL_TerrainTileCatalog.asset";
        private const string TileRoot = "Assets/ANIMOL/Generated/TerrainTiles";

        [MenuItem("ANIMOL/Integrate Theme Terrain Tiles")]
        public static void Integrate()
        {
            EnsureFolder("Assets/ANIMOL/Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<StageTerrainTileCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<StageTerrainTileCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var themes = new[] { ("T01", "moon"), ("T02", "cloud"), ("T03", "library"), ("T04", "greenhouse"), ("T05", "mine") };
            var entries = new List<StageTerrainTileEntry>(80);
            foreach (var theme in themes)
            foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { $"{TileRoot}/{theme.Item2}" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                var entry = new StageTerrainTileEntry(); entry.EditorConfigure(Path.GetFileNameWithoutExtension(path), theme.Item1, tile); entries.Add(entry);
            }
            entries.Sort((a, b) => string.Compare(a.TileId, b.TileId, StringComparison.Ordinal));
            if (entries.Count != 80) throw new InvalidOperationException($"Expected 80 terrain tiles, found {entries.Count}.");
            catalog.EditorConfigure(entries); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL theme terrain integration complete: 5 themes / 80 tiles.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", Path.GetFileName(path));
        }
    }
}
