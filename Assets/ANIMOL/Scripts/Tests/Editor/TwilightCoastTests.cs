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
    public sealed class TwilightCoastTests
    {
        static TwilightCoastArt Art => AssetDatabase.LoadAssetAtPath<TwilightCoastArt>(TwilightCoastBuilder.Catalog);
        [Test] public void NativeImportsRetainCanvasPixelsAndManifestPivots()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { TwilightCoastBuilder.Tiles, TwilightCoastBuilder.Props, TwilightCoastBuilder.Backgrounds }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.AreEqual(124, paths.Length);
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path); Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
                Assert.AreEqual(80, importer.spritePixelsPerUnit, path); Assert.AreEqual(TwilightCoastImportPolicy.Pivot(path), settings.spritePivot, path);
                Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, path); Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path); Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, path); Assert.IsFalse(importer.alphaIsTransparency, path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); Assert.AreEqual(4, sprite.vertices.Length);
                Assert.AreEqual(sprite.texture.width, sprite.rect.width); Assert.AreEqual(sprite.texture.height, sprite.rect.height);
                foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) Assert.IsFalse(importer.GetPlatformTextureSettings(platform).overridden, path);
            }
        }
        [Test] public void ExposedMasksCoverAll256NeighbourhoodsWithoutRotation()
        {
            var offsets = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left, new Vector2Int(-1,1), new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1) };
            var selected = new HashSet<string>();
            for (var bits = 0; bits < 256; bits++)
            {
                var occupied = new HashSet<Vector2Int> { Vector2Int.zero }; for (var k = 0; k < 8; k++) if ((bits & (1 << k)) != 0) occupied.Add(offsets[k]);
                var shape = TheaterTopology.Resolve(occupied.Contains, Vector2Int.zero); Assert.AreEqual((~bits) & 15, shape.exposure);
                int inner = 0; var corners = new[] { 9, 3, 6, 12 }; for (var k = 0; k < 4; k++) if ((bits & corners[k]) == corners[k] && (bits & (1 << (k + 4))) == 0) inner |= 1 << k;
                Assert.AreEqual(inner, shape.innerCorners); var pair = Art.Find(shape); Assert.NotNull(pair); selected.Add(pair.sand.sprite.name);
                foreach (var tile in new[] { pair.sand, pair.wetSandBody }) { Assert.AreEqual("o" + shape.exposure.ToString("00") + "_i" + inner.ToString("00"), tile.sprite.name); Assert.AreEqual(Matrix4x4.identity, tile.transform); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); Assert.AreEqual(Color.white, tile.color); }
            }
            Assert.AreEqual(47, selected.Count);
        }
        [TestCase(false), TestCase(true)] public void HorizonHasOneSunAndDistinctUnscaledExtensions(bool separated)
        {
            var root=Object.Instantiate(Art.horizonPrefab);
            try {
                var horizon=root.GetComponent<TwilightCoastHorizon>(); horizon.Assemble(2,2,separated); var images=root.GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(separated?7:5,images.Length); Assert.AreEqual(separated?0:1,images.Count(r=>r.name=="Single horizon core"));
                Assert.AreEqual(separated?1:0,images.Count(r=>r.name=="Single setting sun")); Assert.AreEqual(separated?1:0,images.Count(r=>r.name=="Single sun reflection"));
                foreach(var r in images) { Assert.AreEqual(Vector3.one,r.transform.localScale); Assert.IsFalse(r.flipX||r.flipY); Assert.Less(r.sortingOrder,-20); }
                foreach(var r in images.Where(r=>r.name=="Left repeat")) { Assert.AreSame(Art.horizonLeft,r.sprite); Assert.Less(r.transform.localPosition.x,0); }
                foreach(var r in images.Where(r=>r.name=="Right repeat")) { Assert.AreSame(Art.horizonRight,r.sprite); Assert.GreaterOrEqual(r.transform.localPosition.x,12); }
                Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Collider>()); Assert.IsEmpty(root.GetComponentsInChildren<PuzzleParallax>());
            } finally { Object.DestroyImmediate(root); }
        }
        [Test] public void SuppliedAnimationTimingAndIdleOnlyPalmArePreserved()
        {
            Assert.AreEqual(new Vector3(.12f,.30f,.50f),Art.parallax);
            CollectionAssert.AreEqual(new[] {.8f,.4f,.8f,.4f,.8f},Art.palmIdle.durations); Assert.AreEqual(3.2f,Art.palmIdle.Duration,.0001f); Assert.IsTrue(Art.palmIdle.loop);
            CollectionAssert.AreEqual(new[] {.04f,.06f,.07f,.08f,.08f,.1f},Art.palmTouch.durations); Assert.AreEqual(.43f,Art.palmTouch.Duration,.0001f); Assert.IsFalse(Art.palmTouch.loop);
            Assert.AreEqual(4,Art.waterRipples.frames.Length); CollectionAssert.AreEqual(new[] {.6f,.6f,.6f,.6f},Art.waterRipples.durations); Assert.AreEqual(2.4f,Art.waterRipples.Duration,.0001f); Assert.IsTrue(Art.waterRipples.loop);
            Assert.AreSame(Art.palmIdle,Art.palmPrefab.GetComponent<TwilightCoastPalm>().idle); Assert.IsEmpty(Art.palmPrefab.GetComponentsInChildren<DeviceVisualView>()); Assert.IsEmpty(Art.palmPrefab.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(Art.palmPrefab.GetComponentsInChildren<Animator>());
            Assert.AreEqual(new Vector2(80,0),Art.Prop("palm_160x160").pivot); Assert.AreEqual(Vector2.zero,Art.Prop("setting_sun_240x160").pivot-new Vector2(0,160));
        }
        [Test] public void PreviewCopiesOnlyAppearanceAndKeepsSourcePuzzleAndHud()
        {
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset"); var preview = AssetDatabase.LoadAssetAtPath<PuzzleMap>(TwilightCoastBuilder.Data + "/PreviewMap.asset");
            Assert.AreEqual(original.Width, preview.Width); Assert.AreEqual(original.Height, preview.Height); Assert.AreEqual(original.start, preview.start); Assert.AreEqual(original.exit, preview.exit); Assert.AreEqual(original.targetBubbles, preview.targetBubbles);
            CollectionAssert.AreEqual(original.bubbles, preview.bubbles); Assert.AreEqual(JsonUtility.ToJson(original.cameraSettings), JsonUtility.ToJson(preview.cameraSettings));
            Assert.AreEqual(original.terrain.Count, preview.terrain.Count); Assert.AreEqual(original.devices.Count, preview.devices.Count);
            Assert.AreSame(original.theme.player, preview.theme.player); Assert.AreSame(original.theme.bubble, preview.theme.bubble); Assert.AreSame(original.theme.exit, preview.theme.exit);
            Assert.IsNull(preview.theme.skyIsland); Assert.IsNull(preview.theme.memorySkyTheater); Assert.IsNull(preview.theme.clockDome); Assert.AreSame(Art, preview.theme.twilightCoast);
        }
    }
}
