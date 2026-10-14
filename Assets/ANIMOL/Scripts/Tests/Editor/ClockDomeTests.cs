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
    public sealed class ClockDomeTests
    {
        static ClockDomeArt Art => AssetDatabase.LoadAssetAtPath<ClockDomeArt>(ClockDomeBuilder.Catalog);
        [Test] public void NativeImportsRetainCanvasPixelsAndManifestPivots()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { ClockDomeBuilder.Tiles, ClockDomeBuilder.Props, ClockDomeBuilder.Backgrounds }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.AreEqual(116, paths.Length);
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path); Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
                Assert.AreEqual(80, importer.spritePixelsPerUnit, path); Assert.AreEqual(ClockDomeImportPolicy.Pivot(path), settings.spritePivot, path);
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
                Assert.AreEqual(inner, shape.innerCorners); var pair = Art.Find(shape); Assert.NotNull(pair); selected.Add(pair.floor.sprite.name);
                foreach (var tile in new[] { pair.floor, pair.underside }) { Assert.AreEqual("o" + shape.exposure.ToString("00") + "_i" + inner.ToString("00"), tile.sprite.name); Assert.AreEqual(Matrix4x4.identity, tile.transform); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); Assert.AreEqual(Color.white, tile.color); }
            }
            Assert.AreEqual(47, selected.Count);
        }
        [TestCase(0), TestCase(4), TestCase(12)] public void RoomUsesSingleFixedCapAndUnscaledColumnRows(int rows)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(ClockDomeBuilder.Prefabs + "/Room_" + rows + ".prefab"); var images = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(6 + rows * 2, images.Length); Assert.AreEqual(1, images.Count(r => r.name == "dome_cap_960x400")); Assert.AreEqual(0, images.Count(r => r.name.Contains("segment")));
            Assert.AreEqual(2 * rows, images.Count(r => r.name.Contains("repeat")));
            foreach (var r in images) { Assert.AreEqual(Vector3.one, r.transform.localScale); Assert.AreEqual(Quaternion.identity, r.transform.localRotation); Assert.IsFalse(r.flipX || r.flipY); Assert.Less(r.sortingOrder, -20); }
            foreach (var side in new[] { "left", "right" })
            {
                var foot = images.Single(r => r.name == "column_" + side + "_foot_160x80"); Assert.AreEqual(new Vector3(side == "left" ? 0 : 10, -5 - rows, 0), foot.transform.localPosition);
                foreach (var r in images.Where(r => r.name == "column_" + side + "_repeat_160x80")) Assert.AreEqual(side == "left" ? 0 : 10, r.transform.localPosition.x);
            }
            Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Collider>()); Assert.IsEmpty(root.GetComponentsInChildren<Animator>());
            Assert.AreEqual(2, root.GetComponentsInChildren<ClockDomeAnimation>().Length);
        }
        [Test] public void ClockIsStaticAndPendulumIsIndependentAmbientLoop()
        {
            Assert.AreEqual(new Vector3(.15f, .30f, .45f), Art.parallax); Assert.AreEqual(8, Art.pendulum.frames.Length); Assert.IsTrue(Art.pendulum.loop);
            Assert.AreEqual(3.2f, Art.pendulum.Duration, .0001f); foreach (var duration in Art.pendulum.durations) Assert.AreEqual(.4f, duration);
            var room = Art.roomPrefab; var clock = room.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "clock_240x240");
            Assert.AreEqual(new Vector3(6, -2.475f, 0), clock.transform.localPosition); Assert.IsNull(clock.GetComponent<ClockDomeAnimation>()); Assert.IsNull(clock.GetComponent<DeviceVisualView>());
            Assert.AreEqual(new Vector2(120, 120), clock.sprite.pivot); Assert.AreEqual(new Vector2(80, 0), Art.Part("telescope_160x160").pivot);
        }
        [Test] public void PreviewCopiesOnlyAppearanceAndKeepsSourcePuzzleAndHud()
        {
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/Small.asset"); var preview = AssetDatabase.LoadAssetAtPath<PuzzleMap>(ClockDomeBuilder.Data + "/PreviewMap.asset");
            Assert.AreEqual(original.Width, preview.Width); Assert.AreEqual(original.Height, preview.Height); Assert.AreEqual(original.start, preview.start); Assert.AreEqual(original.exit, preview.exit); Assert.AreEqual(original.targetBubbles, preview.targetBubbles);
            CollectionAssert.AreEqual(original.bubbles, preview.bubbles); Assert.AreEqual(JsonUtility.ToJson(original.cameraSettings), JsonUtility.ToJson(preview.cameraSettings));
            Assert.AreEqual(original.terrain.Count, preview.terrain.Count); Assert.AreEqual(original.devices.Count, preview.devices.Count);
            Assert.AreSame(original.theme.player, preview.theme.player); Assert.AreSame(original.theme.bubble, preview.theme.bubble); Assert.AreSame(original.theme.exit, preview.theme.exit);
            Assert.IsNull(preview.theme.skyIsland); Assert.IsNull(preview.theme.memorySkyTheater); Assert.AreSame(Art, preview.theme.clockDome);
        }
    }
}
