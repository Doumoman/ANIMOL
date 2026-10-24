using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public sealed class PuzzleMapTests
    {
        PuzzleMap map;
        [SetUp] public void Setup() { map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(5, 5); map.start = Vector2Int.zero; map.exit = new Vector2Int(4, 4); map.targetBubbles = 0; }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(map); }
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5), TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10), TestCase(11), TestCase(12), TestCase(13), TestCase(14), TestCase(15)]
        public void EveryExposureMaskUsesNESWBits(int mask)
        {
            var p = new Vector2Int(2, 2); map.SetTerrain(p, TerrainMaterial.Grass);
            for (var i = 0; i < 4; i++) if ((mask & 1 << i) == 0) map.SetTerrain(p + PuzzleAutotile.Cardinal[i], TerrainMaterial.Grass);
            Assert.AreEqual(mask, PuzzleAutotile.Resolve(map, p).exposure);
        }
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void MissingDiagonalCreatesOnlyItsInsetCorner(int corner)
        {
            Fill(); var p = new Vector2Int(2, 2); map.RemoveTerrain(p + PuzzleAutotile.Diagonal[corner]);
            Assert.AreEqual(0, PuzzleAutotile.Resolve(map, p).exposure);
            Assert.AreEqual(1 << corner, PuzzleAutotile.Resolve(map, p).innerCorners);
            Assert.IsTrue(map.Walkable(p));
        }
        [Test] public void ResizeRetainsAllLayersAndUndoRestoresDimensions()
        {
            Fill(); var p = new Vector2Int(4, 4);
            map.bubbles.Add(p); map.SetDevice(new DeviceCell { position = p, kind = DeviceKind.Ice }); map.decorations.Add(new DecorationCell { position = p });
            var terrain = map.terrain.ToArray(); var count = map.OutsideCount(3, 3);
            Assert.Greater(count, 0); PuzzleMapEditor.Resize(map, 3, 3, false);
            CollectionAssert.AreEqual(terrain, map.terrain); Assert.AreEqual(1, map.devices.Count); Assert.AreEqual(1, map.decorations.Count); Assert.IsFalse(map.Walkable(p));
            Undo.PerformUndo(); map.InvalidateIndex(); Assert.AreEqual(5, map.Width); Assert.IsTrue(map.Walkable(p));
            Undo.PerformRedo(); map.InvalidateIndex(); Assert.AreEqual(3, map.Width); Assert.AreEqual(count, map.OutsideCount(3, 3));
            map.Resize(5, 5); Assert.IsTrue(map.Walkable(p)); Assert.AreEqual(DeviceKind.Ice, map.DeviceAt(p).kind);
        }
        [Test] public void MaterialBoundaryExposesEachSideWithoutChangingWalkability()
        {
            Fill(); var p = new Vector2Int(2, 2); var before = map.terrain.ToArray();
            PuzzleAutotile.Resolve(map, p); CollectionAssert.AreEqual(before, map.terrain);
            map.SetTerrain(p + Vector2Int.right, TerrainMaterial.Stone);
            Assert.AreEqual(2, PuzzleAutotile.Resolve(map, p).exposure); Assert.AreEqual(8, PuzzleAutotile.Resolve(map, p + Vector2Int.right).exposure & 8);
            Assert.IsTrue(map.Walkable(p + Vector2Int.right));
            map.SetTerrain(p + Vector2Int.up, TerrainMaterial.Grass, true);
            Assert.AreEqual(2, PuzzleAutotile.Resolve(map, p).exposure); Assert.IsFalse(map.Walkable(p + Vector2Int.up));
        }
        [Test] public void CameraCentersSmallAxesAndClampsLargeAxesAtDifferentAspects()
        {
            foreach (var aspect in new[] { 9f / 16, 9f / 20, 16f / 9 })
            {
                var small = PuzzleFollowCamera.Clamp(new Vector2(-100, 100), 4, 4, 5.5f, aspect); Assert.AreEqual(new Vector2(1.5f, 1.5f), small);
                var min = PuzzleFollowCamera.Clamp(new Vector2(-100, -100), 40, 40, 5.5f, aspect);
                var max = PuzzleFollowCamera.Clamp(new Vector2(100, 100), 40, 40, 5.5f, aspect);
                Assert.AreEqual(5.5f * aspect - .5f, min.x, .001f); Assert.AreEqual(5, min.y, .001f);
                Assert.AreEqual(39.5f - 5.5f * aspect, max.x, .001f); Assert.AreEqual(34, max.y, .001f);
            }
        }
        [Test] public void PortalKeepsIncomingDirectionAndSlidingMomentum()
        {
            Fill(); map.start = new Vector2Int(0, 2); map.SetDevice(new DeviceCell { position = new Vector2Int(1, 2), kind = DeviceKind.Ice });
            map.SetDevice(new DeviceCell { position = new Vector2Int(2, 2), kind = DeviceKind.Portal, portalTarget = new Vector2Int(3, 2), direction = Vector2Int.down });
            map.SetDevice(new DeviceCell { position = new Vector2Int(3, 2), kind = DeviceKind.Portal, portalTarget = new Vector2Int(2, 2) });
            var session = new PuzzleSession(map); Assert.IsTrue(session.BeginMove(Vector2Int.right)); session.Arrive(new Vector2Int(1, 2));
            Assert.IsTrue(session.NextAutomatic(false, out var next, out var teleport)); Assert.IsTrue(session.Sliding); session.Arrive(next);
            Assert.IsTrue(session.NextAutomatic(false, out next, out teleport)); Assert.IsTrue(teleport); session.Arrive(next);
            Assert.IsTrue(session.NextAutomatic(true, out next, out teleport)); Assert.IsFalse(teleport); Assert.AreEqual(new Vector2Int(4, 2), next); Assert.AreEqual(Vector2Int.right, session.Direction);
        }
        [Test] public void VictoryRequiresActualCollectionAndArrivalAtExit()
        {
            Fill(); map.start = Vector2Int.zero; map.exit = Vector2Int.right; map.targetBubbles = 1; map.bubbles.Add(new Vector2Int(2, 0));
            var session = new PuzzleSession(map); session.Arrive(map.exit); Assert.IsFalse(session.Won);
            session.Arrive(new Vector2Int(2, 0)); session.Arrive(new Vector2Int(2, 0)); Assert.AreEqual(1, session.Collected); Assert.IsFalse(session.Won);
            session.Arrive(map.exit); Assert.IsTrue(session.Won);
        }
        [Test] public void ShrinkFlagsPortalAndStartWithoutDeletingThem()
        {
            Fill(); map.start = new Vector2Int(4, 4); map.SetDevice(new DeviceCell { position = new Vector2Int(1, 1), kind = DeviceKind.Portal, portalTarget = new Vector2Int(4, 4) }); map.Resize(3, 3);
            Assert.AreEqual(2, map.ValidateMap().Count(e => e.StartsWith("Start") || e.StartsWith("Portal")));
            Assert.AreEqual(new Vector2Int(4, 4), map.start); Assert.AreEqual(new Vector2Int(4, 4), map.devices[0].portalTarget);
        }
        [Test] public void AllValidationMapsHaveValidPlacementsAndCompleteTheme()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PuzzleMap", new[] { PuzzleBuilder.Data }))
            {
                var stage = AssetDatabase.LoadAssetAtPath<PuzzleMap>(AssetDatabase.GUIDToAssetPath(guid)); Assert.IsEmpty(stage.ValidateMap(), stage.name);
                foreach (var art in stage.theme.terrain) { Assert.AreEqual(16, art.outer.Length); Assert.AreEqual(4, art.inner.Length); Assert.IsTrue(art.outer.All(s => s)); Assert.IsTrue(art.inner.All(s => s)); }
            }
        }
        [Test] public void StarterMapCreatesPairedScenarioAndCanRunCurrentDeviceLogic()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            Assert.That(theme, Is.Not.Null);
            var path = "Assets/ANIMOL/Data/Puzzle/Maps/__MapEditorTest_" + System.Guid.NewGuid().ToString("N") + ".asset";
            var scenarioPath = path.Substring(0, path.Length - 6) + "_Scenario.asset";
            try
            {
                var created = PuzzleMapEditor.CreateStarterMap(path, theme, 12, 18);
                Assert.That(created.scenario, Is.SameAs(AssetDatabase.LoadAssetAtPath<PuzzleScenario>(scenarioPath)));
                Assert.That(created.theme, Is.SameAs(theme));
                Assert.That(created.scenario.themeId, Is.EqualTo("starway"));
                Assert.That(created.terrain.Count, Is.EqualTo(12 * 18));
                Assert.That(created.ValidateMap(), Is.Empty);
                Assert.That(new PuzzleDeviceSession(created).Won, Is.False);
                Assert.That(AssetDatabase.LoadAssetAtPath<PuzzleMap>(path), Is.SameAs(created));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.DeleteAsset(scenarioPath);
            }
        }
        [Test] public void PracticeLibrarySuppliesAllTwentyFourDeviceRoles()
        {
            var roles = PuzzleMapEditor.LoadPracticeTemplates().Select(entry => entry.device.role).Distinct().ToArray();
            for (var role = 1; role <= 24; role++)
                Assert.That(roles, Does.Contain((PrototypeRole)role), "Missing device practice template " + role);
        }
        [Test] public void DeviceChooserOnlyOffersMatchingThemeArt()
        {
            var templates = PuzzleMapEditor.LoadPracticeTemplates();
            foreach (var id in new[] { "starway", "memory_theater", "letter_observatory", "twilight_sea" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Prototypes/" + id + "_Theme.asset");
                Assert.That(PuzzleMapEditor.ThemeId(theme), Is.EqualTo(id));
                var visible = PuzzleMapEditor.TemplatesForTheme(templates, id);
                Assert.That(visible, Is.Not.Empty, id);
                Assert.That(visible.All(entry => entry.source.themeId == id && entry.device.artTheme == id), Is.True, id);
            }
            Assert.That(PuzzleMapEditor.TemplatesForTheme(templates, ""), Is.Empty);
            var starGarden = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            Assert.That(PuzzleMapEditor.ThemeId(starGarden), Is.EqualTo("starway"));
        }
        [Test] public void StampingPortalCopiesArtAndOffsetsDestinationWithoutMutatingTemplate()
        {
            var template = PuzzleMapEditor.LoadPracticeTemplates().First(entry => entry.device.role == PrototypeRole.Portal).device;
            var originalPosition = template.position;
            var originalTarget = template.target;
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                var position = new Vector2Int(8, 9);
                var copy = PuzzleMapEditor.StampTemplate(scenario, template, position);
                Assert.That(copy.position, Is.EqualTo(position));
                Assert.That(copy.target, Is.EqualTo(originalTarget + position - originalPosition));
                Assert.That(copy.visualPrefab, Is.SameAs(template.visualPrefab));
                Assert.That(copy.group, Is.Empty);
                Assert.That(copy.cells, Is.Not.SameAs(template.cells));
                Assert.That(copy.blastOffsets, Is.Not.SameAs(template.blastOffsets));
                Assert.That(PuzzleMapEditor.StampTemplate(scenario, template, position + Vector2Int.right).id,
                    Is.Not.EqualTo(copy.id));
                Assert.That(template.position, Is.EqualTo(originalPosition));
                Assert.That(template.target, Is.EqualTo(originalTarget));
            }
            finally { Object.DestroyImmediate(scenario); }
        }
        [Test] public void EditorLinksOnlyChosenDevicesAndReusesTheirGroup()
        {
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                var button = new PrototypeDevice { id = "button", role = PrototypeRole.VineBridge };
                var bridge = new PrototypeDevice { id = "bridge", role = PrototypeRole.VineBridge };
                var other = new PrototypeDevice { id = "other", role = PrototypeRole.VineBridge };
                scenario.devices.AddRange(new[] { button, bridge, other });
                var group = PuzzleMapEditor.ConnectDevices(scenario, button, bridge);
                Assert.That(group, Is.Not.Empty);
                Assert.That(button.group, Is.EqualTo(bridge.group));
                Assert.That(string.IsNullOrEmpty(other.group), Is.True);
                Assert.That(PuzzleMapEditor.ConnectDevices(scenario, bridge, other), Is.EqualTo(group));
                Assert.That(other.group, Is.EqualTo(group));
                var neighbor = new PrototypeDevice { id = "neighbor", group = "existing_group" };
                var receiver = new PrototypeDevice { id = "receiver", group = "existing_group" };
                scenario.devices.AddRange(new[] { neighbor, receiver });
                Assert.That(PuzzleMapEditor.ConnectDevices(scenario, button, receiver), Is.EqualTo(group));
                Assert.That(neighbor.group, Is.EqualTo(group));
            }
            finally { Object.DestroyImmediate(scenario); }
        }
        void Fill() { for (var y = 0; y < 5; y++) for (var x = 0; x < 5; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass); }
        [TestCase(150, 10, 1, 0), TestCase(-150, 10, -1, 0), TestCase(10, 150, 0, 1), TestCase(10, -150, 0, -1), TestCase(20, 20, 0, 0), TestCase(100, 150, 0, 1)]
        public void SwipesUseDominantAxisAndIgnoreShortGestures(int dx, int dy, int x, int y)
            => Assert.AreEqual(new Vector2Int(x, y), PuzzleSwipeInput.Resolve(new Vector2(dx, dy), 1080));
        [Test] public void BackgroundFitUsesOneUniformScaleAtMultipleResolutions()
        {
            foreach (var size in new[] { new Vector2(1080, 1920), new Vector2(1080, 2400), new Vector2(720, 1280) })
            {
                var source = new Vector2(948, 1659); var scale = PuzzleBackdrop.UniformCover(size, source);
                var rendered = source * scale; Assert.GreaterOrEqual(rendered.x + .001f, size.x); Assert.GreaterOrEqual(rendered.y + .001f, size.y);
                Assert.AreEqual(source.x / source.y, rendered.x / rendered.y, .0001f);
            }
        }
    }
}
