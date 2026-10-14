using System.Linq;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Tests
{
    public sealed class MemorySkyTheaterTests
    {
        static MemorySkyTheaterArt Art => AssetDatabase.LoadAssetAtPath<MemorySkyTheaterArt>(MemorySkyTheaterBuilder.Catalog);
        [Test] public void All256ExposedAndDiagonalPatternsResolveUnrotatedWoodUnderAndCarpet()
        {
            var offsets = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left, new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
            var selected = new HashSet<string>();
            for (var bits = 0; bits < 256; bits++)
            {
                var occupied = new HashSet<Vector2Int> { Vector2Int.zero };
                for (var i = 0; i < 8; i++) if ((bits & (1 << i)) != 0) occupied.Add(offsets[i]);
                var shape = TheaterTopology.Resolve(occupied.Contains, Vector2Int.zero);
                Assert.AreEqual((~bits) & 15, shape.exposure);
                int inner = 0; var corners = new[] { 9, 3, 6, 12 };
                for (var i = 0; i < 4; i++) if ((bits & corners[i]) == corners[i] && (bits & (1 << (i + 4))) == 0) inner |= 1 << i;
                Assert.AreEqual(inner, shape.innerCorners); var pair = Art.Find(shape); Assert.NotNull(pair);
                var expected = "o" + shape.exposure.ToString("00") + "_i" + inner.ToString("00"); selected.Add(expected);
                foreach (var tile in new[] { pair.wood, pair.underside, pair.velvet })
                { Assert.AreEqual(expected, tile.sprite.name); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); Assert.AreEqual(Matrix4x4.identity, tile.transform); Assert.AreEqual(Color.white, tile.color); }
            }
            Assert.AreEqual(47, selected.Count);
        }
        [Test] public void NativeImportsRetainCanvasPixelsAndManifestPivots()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { MemorySkyTheaterBuilder.Tiles, MemorySkyTheaterBuilder.Props, MemorySkyTheaterBuilder.Backgrounds }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.AreEqual(165, paths.Length);
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path); Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
                Assert.AreEqual(80, importer.spritePixelsPerUnit, path); Assert.AreEqual(MemorySkyTheaterImportPolicy.Pivot(path), settings.spritePivot, path);
                Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, path); Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path); Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, path); Assert.IsFalse(importer.alphaIsTransparency, path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); Assert.AreEqual(4, sprite.vertices.Length);
                Assert.AreEqual(sprite.texture.width, sprite.rect.width); Assert.AreEqual(sprite.texture.height, sprite.rect.height);
                foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) Assert.IsFalse(importer.GetPlatformTextureSettings(platform).overridden, path);
            }
        }
        [TestCase(3), TestCase(5), TestCase(9)] public void CurtainFramesHaveFixedPillarsAndHorizontalRepeats(int cells)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(MemorySkyTheaterBuilder.Prefabs + "/Frame_" + cells + ".prefab"); var images = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(cells, images.Length); Assert.AreEqual(2, root.GetComponentsInChildren<MemorySkyTheaterAnimation>().Length);
            Assert.AreEqual(new Vector3(.5f, -2, 0), images[0].transform.localPosition);
            Assert.AreEqual(new Vector3(cells - .5f, -2, 0), images.Last().transform.localPosition);
            for (var k = 0; k < cells - 2; k++) Assert.AreEqual(new Vector3(1.5f + k, 0, 0), images[k + 1].transform.localPosition);
            foreach (var r in images) Assert.AreEqual(Vector3.one, r.transform.localScale);
            Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Animator>());
        }
        [Test] public void AnimationTimesAndImageCoordinateConversionFollowManifest()
        {
            CollectionAssert.AreEqual(new[] { .9f, .35f, .7f, .35f, .9f }, Art.curtainLeft.durations);
            CollectionAssert.AreEqual(Art.curtainLeft.durations, Art.curtainRight.durations);
            Assert.AreEqual(3.2f, Art.curtainLeft.Duration, .0001f); Assert.IsTrue(Art.curtainLeft.loop);
            CollectionAssert.AreEqual(new[] { .06f, .06f, .12f }, Art.footlights.durations); Assert.IsFalse(Art.footlights.loop);
            Assert.AreSame(Art.footlights.frames.Last(), Art.footlights.At(20));
            Assert.AreEqual(new Vector2(1.5f, -2), TheaterTopology.PropPosition(new Vector2(80, 0), new Vector2(80, 160), new Vector2(.5f, 0)));
            Assert.AreEqual(new Vector2(2.5f, 0), TheaterTopology.PropPosition(new Vector2(160, 0), new Vector2(80, 80), new Vector2(.5f, 1)));
        }
        [Test] public void EnvironmentPreviewCopiesExistingPuzzleDataWithoutImportingExampleWalkability()
        {
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset");
            var preview = AssetDatabase.LoadAssetAtPath<PuzzleMap>(MemorySkyTheaterBuilder.Data + "/PreviewMap.asset");
            Assert.AreEqual(original.Width, preview.Width); Assert.AreEqual(original.Height, preview.Height);
            CollectionAssert.AreEqual(original.terrain, preview.terrain); CollectionAssert.AreEqual(original.devices, preview.devices);
            CollectionAssert.AreEqual(original.bubbles, preview.bubbles); Assert.AreEqual(original.start, preview.start); Assert.AreEqual(original.exit, preview.exit); Assert.AreEqual(original.targetBubbles, preview.targetBubbles);
            Assert.AreEqual(JsonUtility.ToJson(original.cameraSettings), JsonUtility.ToJson(preview.cameraSettings)); Assert.AreEqual(original.theme.player, preview.theme.player);
            foreach (var prop in Art.defaultLayout.props)
            {
                var instance = Object.Instantiate(prop.prefab); instance.transform.position = prop.position;
                try
                {
                    foreach (var image in instance.GetComponentsInChildren<SpriteRenderer>()) foreach (var cell in preview.terrain)
                        Assert.IsFalse(image.bounds.Contains(new Vector3(cell.position.x, cell.position.y, 0)), prop.prefab.name + " covers a playable cell center");
                }
                finally { Object.DestroyImmediate(instance); }
            }
        }
        [TestCase(1, 1), TestCase(2, 5), TestCase(9, 4)] public void BordersAttachToActualSparseGroundWithoutCoveringPlayableCenters(int width, int height)
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(16, 20);
            for (var y = 4; y < 4 + height; y++) for (var x = 3; x < 3 + width; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            var before = map.terrain.ToArray(); var host = new GameObject("Variable theater border test");
            try
            {
                var view = host.AddComponent<MemorySkyTheaterRenderer>(); view.Bind(map, Art);
                var frame = host.GetComponentInChildren<MemorySkyTheaterFrame>(); Assert.NotNull(frame);
                var span = width < 3 ? width + 2 : width; Assert.AreEqual(span, frame.GetComponentsInChildren<SpriteRenderer>().Length);
                foreach (var image in frame.GetComponentsInChildren<SpriteRenderer>()) Assert.AreEqual(Vector3.one, image.transform.localScale);
                foreach (var image in host.GetComponentsInChildren<SpriteRenderer>()) foreach (var cell in map.terrain)
                    Assert.IsFalse(image.bounds.Contains(new Vector3(cell.position.x, cell.position.y, 0)), image.name);
                foreach (var placement in host.GetComponentsInChildren<MemorySkyTheaterAnimation>())
                {
                    var feet = placement.visual.bounds.min; var middle = placement.visual.bounds.center;
                    Assert.IsTrue(view.Structural(new Vector2Int(Mathf.FloorToInt(middle.x + .5f), Mathf.FloorToInt(feet.y + .6f))), placement.name + " is not on its decor deck");
                }
                CollectionAssert.AreEqual(before, map.terrain); Assert.IsEmpty(host.GetComponentsInChildren<Collider2D>());
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(map); }
        }
    }
}
