using System.Linq;
using System.Collections;
using System.Reflection;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class BookshelfRotationTests
    {
        PuzzleMap map;
        PuzzleScenario rules;
        PrototypeDevice shelf;
        static Vector2Int P(int x, int y) => new Vector2Int(x, y);
        [SetUp] public void SetUp()
        {
            map = ScriptableObject.CreateInstance<PuzzleMap>(); rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.Resize(7, 7); map.scenario = rules; map.start = P(1, 0); map.exit = P(0, 6); map.targetBubbles = 0;
            foreach (var p in new[] { map.start, map.exit, P(1, 1), P(4, 3) }) map.SetTerrain(p, TerrainMaterial.TheaterStageFloor);
            rules.themeId = "memory_theater"; rules.echoTheaterV3Rules = true;
            rules.devices.Add(new PrototypeDevice { id = "plate", role = PrototypeRole.Bookshelf, variant = "plate", group = "books", position = P(1, 1) });
            shelf = new PrototypeDevice { id = "shelf", role = PrototypeRole.Bookshelf, group = "books", position = P(4, 3) };
            rules.devices.Add(shelf);
        }
        [TearDown] public void TearDown()
        { Undo.ClearUndo(rules); Undo.ClearUndo(map); Object.DestroyImmediate(map); Object.DestroyImmediate(rules); }
        PrototypeDevice Obstacle(PrototypeRole role, Vector2Int cell)
        {
            var device = new PrototypeDevice { id = "obstacle", role = role, position = cell, active = true,
                variant = "" };
            if (role == PrototypeRole.Shutter) device.active = false;
            rules.devices.Add(device); map.SetTerrain(cell, TerrainMaterial.TheaterStageFloor); return device;
        }

        [TestCase(0, false), TestCase(1, false), TestCase(2, false), TestCase(3, false)]
        [TestCase(0, true), TestCase(1, true), TestCase(2, true), TestCase(3, true)]
        public void QuarterTurnHasSevenCellsAndDirectionSelectsTheTwoSweptCorners(int phase, bool reverse)
        {
            var direction = reverse ? BookshelfRotationDirection.CounterClockwise : BookshelfRotationDirection.Clockwise;
            var cells = BookshelfRotationRules.SweptCells(Vector2Int.zero, phase, direction).ToArray();
            Assert.That(cells.Distinct().Count(), Is.EqualTo(7));
            foreach (var p in new[] { Vector2Int.zero, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                Assert.That(cells, Does.Contain(p));
            var y = ((phase & 1) == 0) != reverse ? -1 : 1;
            Assert.That(cells, Does.Contain(P(1, y))); Assert.That(cells, Does.Contain(P(-1, -y)));
            Assert.That(cells, Has.No.Member(P(1, -y))); Assert.That(cells, Has.No.Member(P(-1, y)));
        }

        [Test] public void EmptyAirNeedsNoFloorAndCounterclockwiseUsesTheAuthoredSweep()
        {
            shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
            var session = new PuzzleDeviceSession(map); var action = session.Apply(Vector2Int.up);
            Assert.That(map.TryTerrain(P(4, 4), out _), Is.False);
            Assert.That(action.accepted, Is.True); Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
            var cue = action.events.Single(e => e.eventName == "bookshelf_rotation_committed");
            Assert.That(cue.reverseAnimation, Is.False); Assert.That(cue.mirrorAnimation, Is.False);
            Assert.That(cue.state, Is.EqualTo("rotate_0_cw"));
            Assert.That(session.Undo(), Is.True); Assert.That(session.States[shelf.id].phase, Is.Zero);
        }

        [TestCase(PrototypeRole.Wall), TestCase(PrototypeRole.Dice), TestCase(PrototypeRole.Glass)]
        [TestCase(PrototypeRole.Bookshelf)]
        public void DeviceOrBlockInSweptCornerPreventsRotationEvenWhenTheFinalEndsAreClear(PrototypeRole role)
        {
            Obstacle(role, P(3, 4));
            var session = new PuzzleDeviceSession(map); var action = session.Apply(Vector2Int.up);
            Assert.That(action.accepted, Is.True); Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.States[shelf.id].phase, Is.Zero);
            Assert.That(action.events.Any(e => e.eventName == "bookshelf_rotation_committed"), Is.False);
            Assert.That(session.States["plate"].active, Is.True);
        }

        [Test] public void OppositeDirectionAvoidsTheBlockedCornerAndEditorShowsTheSameDecision()
        {
            Obstacle(PrototypeRole.Wall, P(3, 4));
            var clockwise = new PuzzleDeviceSession(map);
            Assert.That(clockwise.BookshelfRotationBlocked(shelf, out var cell), Is.True); Assert.That(cell, Is.EqualTo(P(3, 4)));
            Assert.That(PuzzleMapEditor.BookshelfPreviewBlocked(map, shelf, cell), Is.True);
            shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
            var counterclockwise = new PuzzleDeviceSession(map);
            Assert.That(counterclockwise.BookshelfRotationBlocked(shelf, out _), Is.False);
            Assert.That(BookshelfRotationRules.SweptCells(shelf.position, shelf.orientation, shelf.bookshelfRotation)
                .Any(p => PuzzleMapEditor.BookshelfPreviewBlocked(map, shelf, p)), Is.False);
            counterclockwise.Apply(Vector2Int.up); Assert.That(counterclockwise.States[shelf.id].phase, Is.EqualTo(1));
        }

        [Test] public void ClearedObstacleAllowsTheNextPress()
        {
            Obstacle(PrototypeRole.Glass, P(3, 4));
            var session = new PuzzleDeviceSession(map); session.Apply(Vector2Int.up);
            session.States["obstacle"].removed = true;
            session.Apply(Vector2Int.down); var press = session.Apply(Vector2Int.up);
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
            Assert.That(press.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
        }

        [Test] public void PlayerBlockedTerrainAndAnotherBookshelfWingArePhysicalObstacles()
        {
            map.start = P(3, 4); map.SetTerrain(map.start, TerrainMaterial.TheaterStageFloor);
            Assert.That(new PuzzleDeviceSession(map).BookshelfRotationBlocked(shelf, out _), Is.True);
            map.start = P(1, 0); map.SetTerrain(P(3, 4), TerrainMaterial.TheaterStageFloor, true);
            Assert.That(new PuzzleDeviceSession(map).BookshelfRotationBlocked(shelf, out _), Is.True);
            map.RemoveTerrain(P(3, 4));
            rules.devices.Add(new PrototypeDevice { id = "second", role = PrototypeRole.Bookshelf, group = "other", position = P(2, 4) });
            Assert.That(new PuzzleDeviceSession(map).BookshelfRotationBlocked(shelf, out var wing), Is.True);
            Assert.That(wing, Is.EqualTo(P(3, 4))); Assert.That(PuzzleMapEditor.BookshelfPreviewBlocked(map, shelf, wing), Is.True);
        }

        [Test] public void BoardBoundaryBlocksButFloorEffectsAndOrdinaryFloorDoNot()
        {
            var free = new PuzzleDeviceSession(map); Assert.That(free.BookshelfRotationBlocked(shelf, out _), Is.False);
            map.SetTerrain(P(3, 4), TerrainMaterial.TheaterStageFloor);
            map.devices.Add(new DeviceCell { position = P(3, 4), kind = DeviceKind.Ice }); map.InvalidateIndex();
            Assert.That(new PuzzleDeviceSession(map).BookshelfRotationBlocked(shelf, out _), Is.False);
            map.devices.Clear(); map.InvalidateIndex(); shelf.position = P(4, 0);
            Assert.That(new PuzzleDeviceSession(map).BookshelfRotationBlocked(shelf, out _), Is.True);
        }

        [Test] public void ClockwiseDefaultAndSerializedDirectionSurviveUndo()
        {
            var old = JsonUtility.FromJson<PrototypeDevice>("{\"id\":\"old\",\"role\":8}");
            Assert.That(old.bookshelfRotation, Is.EqualTo(BookshelfRotationDirection.Clockwise));
            Undo.IncrementCurrentGroup(); Undo.RegisterCompleteObjectUndo(rules, "Bookshelf direction");
            shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
            Assert.That(JsonUtility.FromJson<PrototypeDevice>(JsonUtility.ToJson(shelf)).bookshelfRotation,
                Is.EqualTo(BookshelfRotationDirection.CounterClockwise));
            Undo.PerformUndo(); Assert.That(rules.devices.Single(d => d.id == "shelf").bookshelfRotation,
                Is.EqualTo(BookshelfRotationDirection.Clockwise));
        }

        [Test] public void LegacyFourPoseBookcaseMovesCounterclockwiseAndKeepsItsCenter()
        {
            rules.echoTheaterV3Rules = false; shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
            var session = new PuzzleDeviceSession(map); session.Apply(Vector2Int.up);
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(3));
            Assert.That(session.States[shelf.id].cell, Is.EqualTo(shelf.position));
        }

        [Test] public void CounterclockwiseCommitReachesTheActualV03PresenterWithoutMissingBindings()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/Prototypes/Device08_Bookshelf.asset");
            map.theme = source.theme;
            foreach (var d in rules.devices)
                d.visualPrefab = source.scenario.devices.First(s => s.role == d.role && (s.variant == "plate") == (d.variant == "plate")).visualPrefab;
            shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var host = (GameObject)PrefabUtility.InstantiatePrefab(rules.devices[0].visualPrefab, preview);
                var dummyPlayer = host.GetComponent<SpriteRenderer>(); if (!dummyPlayer) dummyPlayer = host.AddComponent<SpriteRenderer>();
                var presenter = host.AddComponent<PuzzleDeviceView>(); var session = new PuzzleDeviceSession(map);
                presenter.Bind(session, dummyPlayer);
                var cue = session.Apply(Vector2Int.up).events.Single(e => e.eventName == "bookshelf_rotation_committed");
                presenter.Notify(cue, "counterclockwise-test");
                var visual = host.GetComponentsInChildren<DeviceVisualView>().Single(v => v.gameObject.name == shelf.id);
                var binding = visual.states.Single(s => s.name == cue.state);
                var art = map.theme.memorySkyTheater.v02;
                Assert.That(art.v03Animations.TryGetClip(binding.v03ClipKey, art.v03ManifestSha256, out var clip, out var error), Is.True, error);
                Assert.That(visual.visual.sprite, Is.SameAs(clip.GetFrame(0).Sprite));
                Assert.That(presenter.MissingBindings, Is.Zero); Assert.That(visual.BlocksInput, Is.False);
                var time = visual.Elapsed; visual.paused = true; visual.Advance(.25f);
                Assert.That(visual.Elapsed, Is.EqualTo(time));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        [TestCase(PrototypeRole.Bookshelf, "plate"), TestCase(PrototypeRole.Dice, "target")]
        [TestCase(PrototypeRole.Shutter, ""), TestCase(PrototypeRole.Domino, "")]
        [TestCase(PrototypeRole.VineBridge, "")]
        public void PassableDevicesDoNotBlockTheSweepOrEditorPreview(PrototypeRole role, string variant)
        {
            var device = Obstacle(role, P(3, 4)); device.variant = variant; device.active = true;
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.BookshelfRotationBlocked(shelf, out _), Is.False);
            Assert.That(PuzzleMapEditor.BookshelfPreviewBlocked(map, shelf, device.position), Is.False);
            var action = session.Apply(Vector2Int.up);
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
            Assert.That(action.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
        }

        [TestCase(0, false), TestCase(1, false), TestCase(2, false), TestCase(3, false)]
        [TestCase(0, true), TestCase(1, true), TestCase(2, true), TestCase(3, true)]
        public void RuntimeSweepAndPreviewAgreeForEveryStartAndDirection(int phase, bool counterclockwise)
        {
            rules.echoTheaterV3Rules = false; shelf.orientation = phase;
            shelf.bookshelfRotation = counterclockwise ? BookshelfRotationDirection.CounterClockwise : BookshelfRotationDirection.Clockwise;
            var sweptY = ((phase & 1) == 0) != counterclockwise ? -1 : 1;
            var obstacle = Obstacle(PrototypeRole.Wall, shelf.position + P(1, -sweptY));
            var free = new PuzzleDeviceSession(map);
            Assert.That(free.BookshelfRotationBlocked(shelf, out _), Is.False);
            var cue = free.Apply(Vector2Int.up).events.Single(e => e.eventName == "bookshelf_rotation_committed");
            Assert.That(cue.state, Is.EqualTo("rotate_" + phase * 90 + "_cw"));
            obstacle.position = shelf.position + P(1, sweptY);
            var blocked = new PuzzleDeviceSession(map);
            Assert.That(blocked.BookshelfRotationBlocked(shelf, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(obstacle.position));
            Assert.That(PuzzleMapEditor.BookshelfPreviewBlocked(map, shelf, cell), Is.True);
            Assert.That(blocked.Apply(Vector2Int.up).events.Any(e => e.eventName == "bookshelf_rotation_committed"), Is.False);
        }

        [Test] public void ShutterOpeningAndDominoFoldingImmediatelyClearTheSweep()
        {
            foreach (var role in new[] { PrototypeRole.Shutter, PrototypeRole.Domino })
            {
                rules.devices.RemoveAll(d => d.id == "obstacle");
                var device = Obstacle(role, P(3, 4)); device.active = false;
                var session = new PuzzleDeviceSession(map);
                Assert.That(session.BookshelfRotationBlocked(shelf, out _), Is.True);
                session.States[device.id].active = true;
                Assert.That(session.BookshelfRotationBlocked(shelf, out _), Is.False);
                Assert.That(session.Apply(Vector2Int.up).events.Any(e => e.eventName == "bookshelf_rotation_committed"), Is.True);
            }
        }

        [Test] public void Et12PressRetriesAfterTheCartClearsTheSweepInTheSameTurn()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-12_MapData.asset");
            var session = new PuzzleDeviceSession(source);
            const string route = "RUUUUUUDLLDURRDL";
            PrototypeAction action = null;
            foreach (var input in route)
            {
                action = session.Apply(input == 'R' ? Vector2Int.right : input == 'L' ? Vector2Int.left :
                    input == 'U' ? Vector2Int.up : Vector2Int.down);
                Assert.That(action.accepted, Is.True, "ET-12 input " + input);
            }
            Assert.That(session.States["statue_1"].cell, Is.EqualTo(P(12, 9)));
            Assert.That(session.States["movingstage_1"].cell, Is.EqualTo(P(10, 8)));
            Assert.That(session.States["bookshelf_3"].active, Is.True);
            Assert.That(session.States["bookshelf_1"].phase, Is.EqualTo(1));
            Assert.That(action.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
            Assert.That(session.Cost, Is.EqualTo(route.Length));
        }

        [Test] public void StatueClearingTheSweepAfterThePressStillAllowsOneRotation()
        {
            shelf.orientation = 1;
            var statue = Obstacle(PrototypeRole.Statue, P(5, 4));
            statue.direction = Vector2Int.down; statue.number = 2;
            map.SetTerrain(P(5, 3), TerrainMaterial.TheaterStageFloor);
            map.SetTerrain(P(5, 2), TerrainMaterial.TheaterStageFloor);
            var session = new PuzzleDeviceSession(map);
            session.States[statue.id].locked = true; session.States[statue.id].timer = 2;
            Assert.That(session.BookshelfRotationBlocked(shelf, out var blocked), Is.True);
            Assert.That(blocked, Is.EqualTo(P(5, 4)));
            var action = session.Apply(Vector2Int.up);
            Assert.That(session.States[statue.id].cell, Is.EqualTo(P(5, 2)));
            Assert.That(session.States[shelf.id].phase, Is.Zero);
            Assert.That(action.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
            Assert.That(action.events.FindIndex(e => e.eventName == "bookshelf_rotation_committed"),
                Is.GreaterThan(action.events.FindLastIndex(e => e.actor == statue.id)));
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.States[statue.id].cell, Is.EqualTo(P(5, 4)));
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
        }

        [Test] public void ShutterOpeningLaterInTheSameTurnRechecksTheBlockedPress()
        {
            Obstacle(PrototypeRole.Shutter, P(3, 4));
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.up);
            Assert.That(session.States["obstacle"].active, Is.True);
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
            Assert.That(action.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
        }

        [TestCase(0, false), TestCase(1, false), TestCase(2, false), TestCase(3, false)]
        [TestCase(0, true), TestCase(1, true), TestCase(2, true), TestCase(3, true)]
        public void StatueOutsideTheSelectedSweepDoesNotBlockRotation(int phase, bool counterclockwise)
        {
            rules.echoTheaterV3Rules = false; shelf.orientation = phase;
            shelf.bookshelfRotation = counterclockwise ? BookshelfRotationDirection.CounterClockwise : BookshelfRotationDirection.Clockwise;
            var sweptY = ((phase & 1) == 0) != counterclockwise ? -1 : 1;
            Obstacle(PrototypeRole.Statue, shelf.position + P(1, -sweptY));
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.BookshelfRotationBlocked(shelf, out _), Is.False);
            var action = session.Apply(Vector2Int.up);
            Assert.That(action.events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
        }

        [Test] public void BlockedPressExpiresAtTurnEndAndDoesNotFireOnALaterHeldPlateInput()
        {
            shelf.orientation = 1;
            var statue = Obstacle(PrototypeRole.Statue, P(5, 4));
            var crate = new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate, position = P(2, 1) };
            rules.devices.Add(crate);
            foreach (var cell in new[] { P(2, 1), P(3, 1) }) map.SetTerrain(cell, TerrainMaterial.TheaterStageFloor);
            var session = new PuzzleDeviceSession(map);
            var blocked = session.Apply(Vector2Int.up);
            Assert.That(blocked.events.Any(e => e.eventName == "bookshelf_rotation_committed"), Is.False);
            Assert.That(session.States[shelf.id].phase, Is.EqualTo(1));
            session.States[statue.id].cell = P(5, 1);
            var held = session.Apply(Vector2Int.right);
            Assert.That(held.accepted, Is.True); Assert.That(session.Cell, Is.EqualTo(P(1, 1)));
            Assert.That(session.States["plate"].active, Is.True);
            Assert.That(held.events.Any(e => e.eventName == "bookshelf_rotation_committed"), Is.False);
            session.Apply(Vector2Int.down);
            Assert.That(session.Apply(Vector2Int.up).events.Count(e => e.eventName == "bookshelf_rotation_committed"), Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator EditorRendersBookshelfSweepAndDirectionControlsWithoutChangingAssets()
        {
            var window = ScriptableObject.CreateInstance<PuzzleMapEditor>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(PuzzleMapEditor).GetMethod("Select", flags).Invoke(window, new object[] { map });
                typeof(PuzzleMapEditor).GetField("selectedDeviceId", flags).SetValue(window, shelf.id);
                var layer = typeof(PuzzleMapEditor).GetField("layer", flags);
                layer.SetValue(window, System.Enum.Parse(layer.FieldType, "Device"));
                shelf.bookshelfRotation = BookshelfRotationDirection.CounterClockwise;
                window.position = new Rect(100, 100, 1200, 850); window.ShowUtility(); window.Focus();
                var deadline = EditorApplication.timeSinceStartup + 3;
                while (((Rect)typeof(PuzzleMapEditor).GetField("mapMoveViewport", flags).GetValue(window)).height < 100 &&
                    EditorApplication.timeSinceStartup < deadline)
                { window.Repaint(); yield return null; }
                window.Repaint(); yield return null; yield return null;
                Assert.That(((Rect)typeof(PuzzleMapEditor).GetField("mapMoveViewport", flags).GetValue(window)).height, Is.GreaterThan(100));
                Assert.That(typeof(PuzzleMapEditor).GetField("showBookshelfRotationArea", flags).GetValue(window), Is.True);
                Assert.That(shelf.bookshelfRotation, Is.EqualTo(BookshelfRotationDirection.CounterClockwise));
            }
            finally { if (window) window.Close(); }
        }
    }
}
