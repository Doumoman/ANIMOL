using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Tests
{
    public sealed class SkyIslandArtTests
    {
        static SkyIslandArt Art => AssetDatabase.LoadAssetAtPath<SkyIslandArt>(SkyIslandBuilder.Catalog);
        [Test] public void All256NeighborPatternsSelectThe47UnrotatedSourcePairs()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3, 3);
            try
            {
                var center = Vector2Int.one;
                var offsets = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left, new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
                var selected = new HashSet<string>();
                for (var neighbors = 0; neighbors < 256; neighbors++)
                {
                    map.terrain.Clear(); map.InvalidateIndex(); map.SetTerrain(center, TerrainMaterial.Grass);
                    for (var bit = 0; bit < 8; bit++) if ((neighbors & (1 << bit)) != 0) map.SetTerrain(center + offsets[bit], TerrainMaterial.Grass);
                    var before = map.terrain.ToArray(); var shape = SkyIslandAutotile.Resolve(map, center);
                    Assert.AreEqual((~neighbors) & 15, shape.exposure);
                    var inner = 0; var adjacent = new[] { 9, 3, 6, 12 };
                    for (var bit = 0; bit < 4; bit++) if ((neighbors & adjacent[bit]) == adjacent[bit] && (neighbors & (1 << (bit + 4))) == 0) inner |= 1 << bit;
                    Assert.AreEqual(inner, shape.innerCorners);
                    var pair = Art.Find(shape.exposure, shape.innerCorners); Assert.NotNull(pair);
                    var name = "o" + shape.exposure.ToString("00") + "_i" + inner.ToString("00");
                    Assert.AreEqual(name, pair.grass.name); Assert.AreEqual(name, pair.underside.name); selected.Add(name);
                    Assert.AreEqual(Tile.ColliderType.None, pair.undersideTile.colliderType); Assert.AreEqual(Tile.ColliderType.None, pair.grassTile.colliderType);
                    Assert.AreEqual(Matrix4x4.identity, pair.grassTile.transform); Assert.AreEqual(Matrix4x4.identity, pair.undersideTile.transform);
                    CollectionAssert.AreEqual(before, map.terrain);
                }
                Assert.AreEqual(47, selected.Count); Assert.AreEqual(47, Art.blob47.Length);
            }
            finally { Object.DestroyImmediate(map); }
        }
        [Test] public void OccupancyIgnoresObjectsAndIncludesBlockedAndOtherGroundMaterials()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3, 3);
            try
            {
                map.SetTerrain(Vector2Int.zero, TerrainMaterial.Grass); map.SetTerrain(Vector2Int.up, TerrainMaterial.Stone, true);
                map.SetDevice(new DeviceCell { position = Vector2Int.right, kind = DeviceKind.Ice }); map.bubbles.Add(Vector2Int.right);
                Assert.AreEqual(14, SkyIslandAutotile.Resolve(map, Vector2Int.zero).exposure);
                Assert.IsFalse(map.Walkable(Vector2Int.up)); Assert.IsFalse(map.Walkable(Vector2Int.right));
                map.Resize(1, 1); Assert.AreEqual(15, SkyIslandAutotile.Resolve(map, Vector2Int.zero).exposure);
                Assert.AreEqual(2, map.terrain.Count); map.Resize(3, 3); Assert.AreEqual(14, SkyIslandAutotile.Resolve(map, Vector2Int.zero).exposure);
            }
            finally { Object.DestroyImmediate(map); }
        }
        [Test] public void NativeImportSettingsPreserveFullCanvasAndPixels()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { SkyIslandBuilder.Tiles, SkyIslandBuilder.Backgrounds, SkyIslandBuilder.Props }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.AreEqual(149, paths.Length);
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path); Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
                Assert.AreEqual(80, importer.spritePixelsPerUnit, path); Assert.AreEqual(new Vector2(.5f, .5f), settings.spritePivot, path);
                Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, path); Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path); Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, path); Assert.IsFalse(importer.alphaIsTransparency, path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); Assert.AreEqual(4, sprite.vertices.Length, path);
                Assert.AreEqual(sprite.texture.width, sprite.rect.width, path); Assert.AreEqual(sprite.texture.height, sprite.rect.height, path);
                foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) Assert.IsFalse(importer.GetPlatformTextureSettings(platform).overridden, path);
            }
        }
        [TestCase(0, 0, 0, 0, 0, 0, 0, 0)]
        [TestCase(640, 480, 96, 72, 224, 168, 416, 312)]
        [TestCase(-400, 960, 900, 144, 820, 336, 700, 624)]
        public void PixelPhasesMatchSourceExamples(int x, int y, int fx, int fy, int mx, int my, int nx, int ny)
        {
            var point = new Vector2(x, y);
            Assert.AreEqual(new Vector2Int(fx, fy), SkyIslandBackdrop.Phase(point, .15f));
            Assert.AreEqual(new Vector2Int(mx, my), SkyIslandBackdrop.Phase(point, .35f));
            Assert.AreEqual(new Vector2Int(nx, ny), SkyIslandBackdrop.Phase(point, .65f));
            Assert.AreEqual(959, SkyIslandBackdrop.FloorMod(-1, 960)); Assert.AreEqual(0, SkyIslandBackdrop.FloorMod(-1920, 960));
        }
    }
}
