using System.Linq;
using ANIMOL.Presentation.ShutterOneTileV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class ShutterOneTileTests
    {
        const string Root = "Assets/ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01";

        public static PuzzleMap OppositePhaseRoom(int legacyOrientation = 1)
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device04_Shutter.asset");
            var map = Object.Instantiate(source); map.scenario = Object.Instantiate(source.scenario);
            map.scenario.costLimit = 0; map.scenario.goals.Clear();
            map.start = new Vector2Int(1, 1); map.bubbles.Clear(); map.targetBubbles = 0;
            map.SetTerrain(map.start, TerrainMaterial.Stone);
            map.SetTerrain(new Vector2Int(2, 1), TerrainMaterial.Stone);
            map.SetTerrain(new Vector2Int(0, 1), TerrainMaterial.Stone, true);
            var closed = map.scenario.devices.Single(d => d.role == PrototypeRole.Shutter);
            closed.active = false; closed.orientation = legacyOrientation;
            var open = JsonUtility.FromJson<PrototypeDevice>(JsonUtility.ToJson(closed));
            open.id = "opposite_shutter"; open.position = new Vector2Int(5, 2); open.active = true;
            map.SetTerrain(open.position, TerrainMaterial.Stone); map.scenario.devices.Add(open);
            map.InvalidateIndex(); return map;
        }
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void ShutterInitialActivationKeepsOppositePhasesAcrossEightTurns(int legacyOrientation)
        {
            var map = OppositePhaseRoom(legacyOrientation);
            try
            {
                var before = JsonUtility.ToJson(map.scenario);
                var session = new PuzzleDeviceSession(map);
                for (var turn = 0; turn <= 8; turn++)
                {
                    Assert.That(session.States["shutter"].active, Is.EqualTo(turn % 2 == 1));
                    Assert.That(session.States["opposite_shutter"].active, Is.EqualTo(turn % 2 == 0));
                    Assert.That((session.States["shutter"].phase - session.States["opposite_shutter"].phase) % 2,
                        Is.EqualTo(1));
                    if (turn < 8) Assert.That(session.Apply(turn % 2 == 0 ? Vector2Int.right : Vector2Int.left).accepted, Is.True);
                }
                Assert.That(session.Cost, Is.EqualTo(8));
                Assert.That(JsonUtility.ToJson(map.scenario), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
        }
        [Test] public void ShutterOppositePhaseRejectUndoResetAndSnapshotPreserveTheirClocks()
        {
            var map = OppositePhaseRoom();
            try
            {
                var session = new PuzzleDeviceSession(map); var initial = session.SearchStateKey();
                Assert.That(session.Apply(Vector2Int.left).accepted, Is.False);
                Assert.That(session.SearchStateKey(), Is.EqualTo(initial)); Assert.That(session.Cost, Is.Zero);
                Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
                var after = session.SearchStateKey();
                var restored = session.RestoreSearchSnapshot(session.CaptureSearchSnapshot());
                Assert.That(restored.SearchStateKey(), Is.EqualTo(after));
                Assert.That(restored.States["shutter"].active, Is.True);
                Assert.That(restored.States["opposite_shutter"].active, Is.False);
                Assert.That(session.Undo(), Is.True); Assert.That(session.SearchStateKey(), Is.EqualTo(initial));
                session.Apply(Vector2Int.right); session.Reset();
                Assert.That(session.SearchStateKey(), Is.EqualTo(initial)); Assert.That(session.Cost, Is.Zero);
            }
            finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
        }
        [TestCase(false), TestCase(true)]
        public void ShutterOccupancySafetyRetainsConfiguredClockPolicy(bool advanceWhenOccupied)
        {
            var map = OppositePhaseRoom();
            map.scenario.shutterPhaseAdvancesWhenOccupied = advanceWhenOccupied;
            map.start = new Vector2Int(4, 2); map.SetTerrain(map.start, TerrainMaterial.Stone);
            map.SetTerrain(new Vector2Int(5, 3), TerrainMaterial.Stone, true); map.InvalidateIndex();
            try
            {
                var session = new PuzzleDeviceSession(map);
                Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
                Assert.That(session.States["opposite_shutter"].active, Is.True, "Occupied shutter closed on the player");
                Assert.That(session.States["opposite_shutter"].phase, Is.EqualTo(advanceWhenOccupied ? 1 : 0));
                var key = session.SearchStateKey();
                Assert.That(session.Apply(Vector2Int.up).accepted, Is.False);
                Assert.That(session.SearchStateKey(), Is.EqualTo(key));
                Assert.That(session.Apply(Vector2Int.left).accepted, Is.True);
                if (advanceWhenOccupied)
                    Assert.That(session.States["shutter"].active, Is.Not.EqualTo(session.States["opposite_shutter"].active));
            }
            finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
        }

        [Test] public void InstalledArtAndThemeUseEightOneCellFrames()
        {
            var art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(
                Root + "/_Generated/ShutterOneTileArt.asset");
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            Assert.That(art, Is.Not.Null);
            Assert.That(theme.oneTileShutterArt, Is.SameAs(art));
            Assert.That(art.Validate(out var error), Is.True, error);
            foreach (var direction in new[] { "horizontal", "vertical" })
            {
                var frames = direction == "horizontal" ? art.horizontal : art.vertical;
                for (var i = 0; i < 6; i++)
                {
                    var name = i < 4 ? "frame_" + i.ToString("00") : i == 4 ? "closed" : "open";
                    var path = Root + "/Sprites/" + direction + "/" + name + ".png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(sprite.texture.width, Is.EqualTo(256), path);
                    Assert.That(sprite.texture.height, Is.EqualTo(256), path);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(256, 256)), path);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(256), path);
                    Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128, 128)), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                    foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                        Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.False,
                            path + " " + platform);
                    if (i < 4) Assert.That(frames[i], Is.SameAs(sprite), path);
                }
            }
        }

        [Test] public void RepeatedProgressUsesAbsoluteOneTileScale()
        {
            var art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(
                Root + "/_Generated/ShutterOneTileArt.asset");
            var anchor = new GameObject("Shutter tile anchor");
            var visual = new GameObject("Visual");
            try
            {
                anchor.transform.position = new Vector3(4, 5);
                visual.transform.SetParent(anchor.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                var view = anchor.AddComponent<ShutterOneTileViewV01>();
                Assert.That(view.Configure(art, renderer, anchor.transform, 1f, out var error), Is.True, error);
                for (var i = 0; i < 20; i++)
                {
                    Assert.That(view.ShowOpenProgress(i % 2 == 0, (i % 4) / 3f), Is.True);
                    Assert.That(renderer.bounds.size.x, Is.EqualTo(1f).Within(.001f));
                    Assert.That(renderer.bounds.size.y, Is.EqualTo(1f).Within(.001f));
                    Assert.That(renderer.bounds.center, Is.EqualTo(anchor.transform.position));
                }
                anchor.transform.localScale = new Vector3(2f, .5f, 1f);
                Assert.That(view.ShowState(true, true), Is.True);
                Assert.That(renderer.bounds.size.x, Is.EqualTo(1f).Within(.001f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(1f).Within(.001f));
            }
            finally { Object.DestroyImmediate(anchor); }
        }
    }
}
