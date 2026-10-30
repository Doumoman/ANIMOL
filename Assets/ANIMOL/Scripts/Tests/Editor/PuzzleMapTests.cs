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
            Undo.PerformUndo(); map.InvalidateIndex(); Assert.AreEqual(5, map.Width); Assert.IsFalse(map.Walkable(p));
            Undo.PerformRedo(); map.InvalidateIndex(); Assert.AreEqual(3, map.Width); Assert.AreEqual(count, map.OutsideCount(3, 3));
            map.Resize(5, 5); Assert.IsFalse(map.Walkable(p)); Assert.AreEqual(DeviceKind.Ice, map.DeviceAt(p).kind);
        }
        [Test] public void DecorationBlocksClassicAndScenarioMovementUntilRemoved()
        {
            Fill(); map.start = new Vector2Int(1, 1);
            var blocked = new Vector2Int(2, 1);
            map.decorations.Add(new DecorationCell { position = blocked });
            Assert.IsTrue(map.DecorationAt(blocked));
            Assert.IsFalse(map.Walkable(blocked));
            var classic = new PuzzleSession(map);
            Assert.IsFalse(classic.BeginMove(Vector2Int.right));
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                map.scenario = scenario;
                var device = new PuzzleDeviceSession(map);
                Assert.IsFalse(device.Apply(Vector2Int.right).accepted);
                map.decorations.Clear();
                Assert.IsTrue(map.Walkable(blocked));
                Assert.IsTrue(classic.BeginMove(Vector2Int.right));
                Assert.IsTrue(device.Apply(Vector2Int.right).accepted);
            }
            finally { map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void EditorResizeAddsGrassOnlyToNewEmptyCells()
        {
            Fill();
            var stored = new Vector2Int(6, 2);
            map.SetTerrain(stored, TerrainMaterial.Stone, true);
            var original = map.terrain.ToArray();
            PuzzleMapEditor.Resize(map, 7, 8, false);
            Assert.That(map.Width, Is.EqualTo(7));
            Assert.That(map.Height, Is.EqualTo(8));
            Assert.That(map.terrain.Count, Is.EqualTo(7 * 8));
            Assert.That(map.terrain.Take(original.Length), Is.EqualTo(original));
            Assert.That(map.TryTerrain(stored, out var restored), Is.True);
            Assert.That(restored.material, Is.EqualTo(TerrainMaterial.Stone));
            Assert.That(restored.blocked, Is.True);
            Assert.That(map.Walkable(new Vector2Int(5, 7)), Is.True);
            Undo.PerformUndo(); map.InvalidateIndex();
            Assert.That(map.Width, Is.EqualTo(5));
            Assert.That(map.terrain, Is.EqualTo(original));
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
        [Test] public void CameraPreviewBoundsUseInsetsAndRuntimeClamp()
        {
            map.cameraSettings.visibleRows = 11;
            map.cameraSettings.topInset = 90;
            map.cameraSettings.bottomInset = 30;
            var preview = PuzzleMapEditor.CameraBounds(map, new Vector2(50, -20));
            var aspect = 540f / (960 - 90 - 30);
            Assert.That(preview.width, Is.EqualTo(11 * aspect).Within(.001f));
            Assert.That(preview.height, Is.EqualTo(11).Within(.001f));
            Assert.That(preview.center, Is.EqualTo(new Vector2(2, 2)));
            map.Resize(40, 30);
            preview = PuzzleMapEditor.CameraBounds(map, Vector2.zero);
            Assert.That(preview.xMin, Is.EqualTo(-.5f).Within(.001f));
            Assert.That(preview.yMin, Is.EqualTo(-.5f).Within(.001f));
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
                var created = PuzzleMapEditor.CreateStarterMap(path, theme, 9, 7);
                Assert.That(created.scenario, Is.SameAs(AssetDatabase.LoadAllAssetsAtPath(path).OfType<PuzzleScenario>().Single()));
                Assert.That(AssetDatabase.LoadMainAssetAtPath(scenarioPath), Is.Null);
                Assert.That(created.theme, Is.SameAs(theme));
                Assert.That(created.scenario.themeId, Is.EqualTo("starway"));
                Assert.That(created.Width, Is.EqualTo(9));
                Assert.That(created.Height, Is.EqualTo(7));
                Assert.That(created.terrain.Count, Is.EqualTo(9 * 7));
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
        [Test] public void ExistingMapGetsPairedScenarioBeforePlacingADevice()
        {
            var path = "Assets/ANIMOL/Data/Puzzle/Maps/__ExistingMapTest_" + System.Guid.NewGuid().ToString("N") + ".asset";
            var scenarioPath = path.Substring(0, path.Length - 6) + "_Scenario.asset";
            var existing = ScriptableObject.CreateInstance<PuzzleMap>();
            existing.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            try
            {
                AssetDatabase.CreateAsset(existing, path);
                var scenario = PuzzleMapEditor.EnsureScenario(existing);
                Assert.That(scenario, Is.SameAs(AssetDatabase.LoadAllAssetsAtPath(path).OfType<PuzzleScenario>().Single()));
                Assert.That(AssetDatabase.LoadMainAssetAtPath(scenarioPath), Is.Null);
                Assert.That(existing.scenario, Is.SameAs(scenario));
                Assert.That(scenario.themeId, Is.EqualTo("starway"));
                Assert.That(scenario.title, Is.EqualTo(existing.name));
                Assert.That(PuzzleMapEditor.EnsureScenario(existing), Is.SameAs(scenario));
                Assert.That(scenario.devices, Is.Empty);
                var template = PuzzleMapEditor.LoadPracticeTemplates().First(entry => entry.device.artTheme == "starway").device;
                Assert.That(PuzzleMapEditor.StampTemplate(scenario, template, Vector2Int.one), Is.Not.Null);
                Assert.That(scenario.devices, Has.Count.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.DeleteAsset(scenarioPath);
                if (existing) Object.DestroyImmediate(existing);
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
                Assert.That(visible.Any(entry => entry.device.role == PrototypeRole.Wind && entry.device.variant == "field"), Is.False);
            }
            Assert.That(PuzzleMapEditor.TemplatesForTheme(templates, ""), Is.Empty);
            var starGarden = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            Assert.That(PuzzleMapEditor.ThemeId(starGarden), Is.EqualTo("starway"));
            Assert.That(PuzzleMapEditor.TemplatesForTheme(templates, "starway")
                .Any(entry => entry.device.role == PrototypeRole.VineBridge), Is.False);
        }
        [Test] public void StarwayMapEditorPreviewUsesV13SpritesForExistingDevices()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var devices = new[]
            {
                new PrototypeDevice { role = PrototypeRole.StarSocket, number = 2, direction = Vector2Int.right },
                new PrototypeDevice { role = PrototypeRole.Conveyor, direction = Vector2Int.left },
                new PrototypeDevice { role = PrototypeRole.Wind, variant = "blower", direction = Vector2Int.down, orientation = 2 }
            };
            var keys = new[] { "star/diamond/stone", "conveyor/west/idle", "windmill/south/6" };
            for (var i = 0; i < devices.Length; i++)
            {
                Assert.That(RanchDevicesV13EditorPreview.TryDevice(theme, devices[i], out var sprite, out var key), Is.True);
                Assert.That(key, Is.EqualTo(keys[i]));
                Assert.That(AssetDatabase.GetAssetPath(sprite).Replace('\\', '/'),
                    Does.StartWith("Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13/Sprites/"));
            }
            devices[0].variant = "socket";
            Assert.That(RanchDevicesV13EditorPreview.TryDevice(theme, devices[0], out _, out var socketKey), Is.True);
            Assert.That(socketKey, Is.EqualTo("star/diamond/empty"));
            Assert.That(RanchDevicesV13EditorPreview.Covers(theme,
                new PrototypeDevice { role = PrototypeRole.Chick, direction = Vector2Int.up }), Is.False);
            Assert.That(RanchDevicesV13EditorPreview.TryClip(theme, "nest/idle", out var nest), Is.True);
            Assert.That(nest, Is.Not.Null);
        }
        [Test] public void RanchWaterPreviewUsesEightNeighborMaskOrder()
        {
            var cell = new Vector2Int(5, 5);
            var connected = new System.Collections.Generic.HashSet<Vector2Int>
            { cell + Vector2Int.up, cell + Vector2Int.right, cell + new Vector2Int(-1, -1) };
            Assert.That(RanchDevicesV13EditorPreview.WaterNeighborMask(connected, cell), Is.EqualTo(1 | 4 | 32));
        }
        [Test] public void ThemeDeviceListIncludesEveryPlaceableRanchFamilyWithCatalogArt()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var entries = PuzzleMapEditor.RanchPaletteForTheme(theme);
            Assert.That(entries.Select(e => e.family), Is.EqualTo(new[]
                { "sheep", "sluice", "sluice", "tall_grass", "nest", "water" }));
            Assert.That(entries.Select(e => e.direction).Take(3), Is.EqualTo(new[]
                { Vector2Int.right, Vector2Int.right, Vector2Int.up }));
            Assert.That(entries.All(e => RanchDevicesV13EditorPreview.TryClip(theme, e.previewKey, out _)), Is.True);
            var theater = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            Assert.That(PuzzleMapEditor.RanchPaletteForTheme(theater), Is.Empty);
        }
        [Test] public void RanchPaletteNestAndWaterUseExistingScenarioRulesAndV13Visuals()
        {
            Fill();
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            var host = new GameObject("Ranch palette binding test");
            try
            {
                map.scenario = scenario; scenario.themeId = "starway";
                Assert.That(PuzzleMapEditor.TryPlaceNest(map, new Vector2Int(3, 2), out var error), Is.True, error);
                Assert.That(scenario.devices, Is.Empty);
                Assert.That(scenario.nests, Is.EqualTo(new[] { new Vector2Int(3, 2) }));
                scenario.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                    position = new Vector2Int(2, 2), direction = Vector2Int.right });
                Assert.That(scenario.devices[0].target, Is.EqualTo(Vector2Int.zero));
                Assert.That(PuzzleMapEditor.TryPlaceRanchWater(map, new Vector2Int(4, 2), out error), Is.True, error);
                Assert.That(scenario.water, Does.Contain(new Vector2Int(4, 2)));
                Assert.That(PuzzleMapEditor.TryPlaceRanchWater(map, new Vector2Int(3, 2), out _), Is.False);
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sheep", new Vector2Int(4, 2), out _), Is.False);
                var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
                Assert.That(host.GetComponentInChildren<ANIMOL.Art.RanchDevicesV13.RanchWaterCellVisualV13>(), Is.Not.Null);
                var nest = host.GetComponentsInChildren<ANIMOL.Art.RanchDevicesV13.RanchDeviceVisualV13>()
                    .Single(v => v.CurrentKey == "nest/idle");
                Assert.That(nest.transform.parent.localPosition, Is.EqualTo(new Vector3(3, 2, 0)));
                var session = new PuzzleDeviceSession(map);
                Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
                Assert.That(session.States["chick"].active, Is.True);
                Assert.That(session.States["chick"].removed, Is.True);
                Assert.That(session.NestPresent(new Vector2Int(3, 2)), Is.False);
                view.Present(session); view.RestoreRanchNests(session);
                Assert.That(nest.CurrentKey, Is.Null);
                Assert.That(nest.transform.parent.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(host); map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void NestCanBePlacedAndErasedWithoutAChick()
        {
            Fill();
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            var host = new GameObject("Independent nest preview test");
            try
            {
                map.scenario = scenario; scenario.themeId = "starway";
                var nest = new Vector2Int(3, 2);
                var outsideBefore = map.OutsideCount(3, 5);
                Assert.That(PuzzleMapEditor.TryPlaceNest(map, nest, out var error), Is.True, error);
                Assert.That(scenario.devices, Is.Empty);
                Assert.That(scenario.NestCells, Is.EqualTo(new[] { nest }));
                Assert.That(map.OutsideCount(3, 5), Is.EqualTo(outsideBefore + 1));
                var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
                Assert.That(host.GetComponentsInChildren<ANIMOL.Art.RanchDevicesV13.RanchDeviceVisualV13>()
                    .Any(visual => visual.CurrentKey == "nest/idle"), Is.True);
                Assert.That(PuzzleMapEditor.TryPlaceNest(map, nest, out _), Is.False);
                Assert.That(map.ValidateMap(), Is.Empty);
                Assert.That(PuzzleMapEditor.TryPlaceRanchWater(map, nest, out _), Is.False);
                Assert.That(PuzzleMapEditor.TryEraseNest(map, nest), Is.True);
                Assert.That(scenario.NestCells, Is.Empty);
                Assert.That(map.OutsideCount(3, 5), Is.EqualTo(outsideBefore));
                Assert.That(PuzzleMapEditor.TryEraseNest(map, nest), Is.False);
            }
            finally { Object.DestroyImmediate(host); map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void DeviceEraserRemovesNestArtWaterAndBothDeviceLists()
        {
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                map.scenario = scenario;
                var nest = new Vector2Int(3, 2);
                scenario.nests.Add(nest);
                var chick = new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                    position = new Vector2Int(1, 1), target = nest };
                scenario.devices.Add(chick);
                var bounce = new Vector2Int(2, 1);
                scenario.devices.Add(new PrototypeDevice { id = "bounce", role = PrototypeRole.Bounce, position = bounce });
                var pond = new RanchV13ArtPlacement { family = "sluice", position = new Vector2Int(1, 3),
                    direction = Vector2Int.right, hasPondB = true, pondB = new Vector2Int(2, 3) };
                scenario.ranchV13Art.Add(pond);
                var water = new Vector2Int(4, 2);
                scenario.water.Add(water);
                var legacy = new Vector2Int(4, 3);
                map.SetDevice(new DeviceCell { position = legacy, kind = DeviceKind.Ice });

                foreach (var cell in new[] { nest, bounce, pond.pondB, water, legacy })
                {
                    Assert.That(PuzzleMapEditor.TryEraseDeviceAt(map, cell), Is.True, cell.ToString());
                    Assert.That(PuzzleMapEditor.TryEraseDeviceAt(map, cell), Is.False, cell.ToString());
                }
                Assert.That(scenario.nests, Is.Empty);
                Assert.That(chick.target, Is.EqualTo(Vector2Int.zero));
                Assert.That(scenario.devices, Is.EqualTo(new[] { chick }));
                Assert.That(scenario.ranchV13Art, Is.Empty);
                Assert.That(scenario.water, Is.Empty);
                Assert.That(map.DeviceAt(legacy).kind, Is.EqualTo(DeviceKind.None));
            }
            finally { map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [TestCase(0, "nw"), TestCase(1, "ne"), TestCase(2, "se"), TestCase(3, "sw")]
        public void BounceFacingSelectionUpdatesTheCornerUsedByGameplay(int corner, string expected)
        {
            var bounce = new PrototypeDevice { role = PrototypeRole.Bounce, direction = Vector2Int.right };
            PuzzleMapEditor.SetBounceCorner(bounce, corner);
            Assert.That(PuzzleDeviceSession.Corner(bounce.orientation), Is.EqualTo(expected));
            Assert.That(bounce.direction, Is.EqualTo(Vector2Int.right));
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            Assert.That(RanchDevicesV13EditorPreview.TryDevice(map.theme, bounce, out _, out var key), Is.True);
            Assert.That(key, Is.EqualTo("bounce_corner/" + expected + "/idle"));
        }
        [TestCase("slash", 1), TestCase("backslash", -1)]
        public void MirrorBounceSlopeSelectionUpdatesReflectedTravel(string variant, int expectedX)
        {
            var bounce = new PrototypeDevice { role = PrototypeRole.Bounce, variant = "slash",
                direction = Vector2Int.down };
            PuzzleMapEditor.SetBounceSlope(bounce, variant);
            Assert.That(PuzzleDeviceSession.ReflectMirror(Vector2Int.up, bounce.variant == "slash"),
                Is.EqualTo(new Vector2Int(expectedX, 0)));
            Assert.That(bounce.direction, Is.EqualTo(Vector2Int.down));
        }
        [Test] public void SheepAndSluiceArtCanBePlacedWithoutInventingGameplayIds()
        {
            Fill();
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                scenario.themeId = "starway";
                map.scenario = scenario;
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sluice", new Vector2Int(1, 2), out var error), Is.True, error);
                Assert.That(scenario.ranchV13Art.Single().Footprint().ToArray(),
                    Is.EqualTo(new[] { new Vector2Int(1, 2), new Vector2Int(2, 2) }));
                Assert.That(scenario.ranchV13Art.Single().ClipKey, Is.EqualTo("pond_pair/horizontal/east_wet"));
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sheep", new Vector2Int(2, 2), out _), Is.False);
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sheep", new Vector2Int(4, 1), out error), Is.True, error);
                Assert.That(scenario.ranchV13Art[1].ClipKey, Is.EqualTo("sheep/east/idle"));
                Assert.That(scenario.devices, Is.Empty);
                Assert.That(PuzzleMapEditor.TryEraseRanchArt(map, new Vector2Int(2, 2)), Is.True);
                Assert.That(scenario.ranchV13Art.Single().family, Is.EqualTo("sheep"));
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sluice", new Vector2Int(4, 2), out _), Is.False);
            }
            finally { map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void PondEditorAcceptsVerticalAndPreservesStoredAOrder()
        {
            Fill();
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                scenario.themeId = "starway"; map.scenario = scenario;
                var a = new Vector2Int(3, 3);
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sluice", a, Vector2Int.down, out var error), Is.True, error);
                var pond = scenario.ranchV13Art.Single();
                Assert.That(pond.position, Is.EqualTo(a));
                Assert.That(pond.hasPondB, Is.True);
                Assert.That(pond.pondB, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(pond.direction, Is.EqualTo(Vector2Int.down));
                Assert.That(pond.PondPair(false).b.y, Is.EqualTo(2));
                Assert.That(pond.ClipKey, Is.EqualTo("pond_pair/vertical/south_wet"));
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sluice", a, new Vector2Int(1, 1), out _), Is.False);
            }
            finally { map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void OneWayGateKeepsTheExistingV12VisualBinding()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device09_OneWayGate_Scenario.asset");
            var gate = scenario.devices.Single(device => device.role == PrototypeRole.OneWayGate);
            Assert.That(RanchDevicesV13PuzzleBinding.Covers(gate), Is.False);
            Assert.That(RanchDevicesV13EditorPreview.Covers(theme, gate), Is.False);
            var root = Object.Instantiate(gate.visualPrefab);
            try
            {
                var view = root.GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.ActivateV12(theme.starwayRanch.v12Animations,
                    theme.starwayRanch.v12ManifestSha256), Is.True);
                Assert.That(view.IsV12Active, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void RanchArtPlacementsRenderAtTheirLogicalCenters()
        {
            Fill();
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            var host = new GameObject("V13 art layout test");
            try
            {
                scenario.themeId = "starway";
                map.scenario = scenario;
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sluice", new Vector2Int(1, 2), out var error), Is.True, error);
                Assert.That(PuzzleMapEditor.TryPlaceRanchArt(map, "sheep", new Vector2Int(4, 1), out error), Is.True, error);
                host.AddComponent<PuzzleMapView>().Bind(map);
                var art = host.GetComponentsInChildren<ANIMOL.Art.RanchDevicesV13.RanchDeviceVisualV13>();
                Assert.That(art.Length, Is.EqualTo(2));
                var pond = host.GetComponentInChildren<ANIMOL.Art.RanchDevicesV13.RanchPondSluiceVisualR3>();
                Assert.That(pond, Is.Not.Null);
                Assert.That(pond.transform.localPosition, Is.EqualTo(new Vector3(1.5f, 2, 0)));
                Assert.That(pond.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(pond.GetComponentsInChildren<ANIMOL.Art.RanchDevicesV13.RanchDeviceVisualV13>()
                    .Select(view => view.CurrentKey), Is.EquivalentTo(new[] { "pond_pair/horizontal/east_wet" }));
                Assert.That(pond.GetComponentsInChildren<SpriteRenderer>().All(renderer =>
                    renderer.sprite.pixelsPerUnit == 256 && renderer.sprite.rect.size == new Vector2(576, 320)), Is.True);
            }
            finally { Object.DestroyImmediate(host); map.scenario = null; Object.DestroyImmediate(scenario); }
        }
        [Test] public void DecorationPaletteShowsOnlyCuratedThemeProps()
        {
            foreach (var id in new[] { "starway", "memory_theater", "letter_observatory", "twilight_sea" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                    "Assets/ANIMOL/Data/Puzzle/Prototypes/" + id + "_Theme.asset");
                var choices = PuzzleMapEditor.DecorationChoices(theme);
                Assert.That(choices, Is.Not.Empty, id);
                Assert.That(choices.All(choice => choice.sprite && choice.label.Length > 0), Is.True, id);
                Assert.That(choices.All(choice => {
                    var path = AssetDatabase.GetAssetPath(choice.sprite).Replace('\\', '/');
                    return id == "starway" ?
                        path.Contains("/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V02/decor/") ||
                        path.EndsWith("/props/windmill_body.png") :
                        id == "memory_theater" ? path.Contains("/ANIMOL_ECHO_THEATER_CONNECTED_DECOR_V01/decor/") :
                        id == "letter_observatory" ? path.Contains("/ANIMOL_SUNSET_SANCTUM_CONNECTED_DECOR_V01/decor/") :
                        path.Contains("/ANIMOL_STAR_SWELL_COAST_CONNECTED_DECOR_V01/decor/");
                }), Is.True, id);
                if (id == "starway") Assert.That(choices.Count(choice => choice.sourceId.StartsWith("ranch:")), Is.EqualTo(26));
                if (id == "memory_theater") Assert.That(choices.Length, Is.EqualTo(26));
            }
            var ranch = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            var ranchChoices = PuzzleMapEditor.DecorationChoices(ranch);
            Assert.That(ranchChoices.Count(choice => choice.sourceId.StartsWith("ranch:")), Is.EqualTo(26));
            Assert.That(ranchChoices.Any(choice => choice.sourceId == "ranch:orchard_bench_3x1"), Is.True);
            Assert.That(ranchChoices.Any(choice => choice.sourceId == "props/windmill_body.png"), Is.True);
        }
        [Test] public void RanchMapRendersTheSelectedDecorationSprite()
        {
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            Fill();
            var bench = PuzzleMapEditor.DecorationChoices(map.theme)
                .Single(choice => choice.sourceId == "ranch:orchard_bench_3x1").sprite;
            Assert.IsTrue(PuzzleMapEditor.TryPlaceDecoration(map, bench, new Vector2Int(1, 2),
                Color.white, "", 0, 0, out var error), error);
            var host = new GameObject("Decoration test");
            try
            {
                var view = host.AddComponent<PuzzleMapView>(); view.Bind(map);
                var controller = view.ConnectedDecor;
                Assert.NotNull(controller);
                Assert.AreEqual(1, controller.OwnedVisualCount);
                var rendered = controller.GetComponentInChildren<SpriteRenderer>();
                Assert.That(rendered.sprite, Is.SameAs(bench));
                Assert.That(rendered.transform.position, Is.EqualTo(new Vector3(2, 2)));
            }
            finally { Object.DestroyImmediate(host); }
        }
        [TestCase("memory_theater")]
        [TestCase("letter_observatory")]
        [TestCase("twilight_sea")]
        public void EditorTerrainPreviewMatchesTheCurrentRuntimeTile(string themeId)
        {
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/" + themeId + "_Theme.asset");
            Fill();
            var cell = new Vector2Int(2, 2);
            map.SetTerrain(cell, TerrainMaterial.Stone);
            var preview = PuzzleMapEditor.TerrainPreviewSprite(map, cell);
            Assert.That(preview, Is.Not.Null, themeId);
            var host = new GameObject("Current theme tile test");
            try
            {
                host.AddComponent<PuzzleMapView>().Bind(map);
                var name = themeId == "memory_theater" ? "Floor 2,2" :
                    themeId == "letter_observatory" ? "V02 terrain " + cell : "Coast V01 land " + cell;
                var runtime = host.GetComponentsInChildren<SpriteRenderer>()
                    .First(renderer => renderer.name.StartsWith(name));
                Assert.That(preview, Is.SameAs(runtime.sprite), themeId);
            }
            finally { Object.DestroyImmediate(host); }
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
        [Test] public void StampingChickLeavesNestForMapAuthorToChoose()
        {
            var template = PuzzleMapEditor.LoadPracticeTemplates().First(entry => entry.device.role == PrototypeRole.Chick).device;
            var scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            try
            {
                Assert.That(template.target, Is.Not.EqualTo(Vector2Int.zero));
                Assert.That(PuzzleMapEditor.StampTemplate(scenario, template, new Vector2Int(3, 3)).target,
                    Is.EqualTo(Vector2Int.zero));
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
