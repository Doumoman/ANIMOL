using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class PuzzleMapTranslationTests
    {
        PuzzleMap map;
        PuzzleScenario rules;
        [SetUp] public void SetUp()
        {
            map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(12, 12);
            rules = ScriptableObject.CreateInstance<PuzzleScenario>(); map.scenario = rules;
            map.start = new Vector2Int(2, 2); map.exit = new Vector2Int(6, 6);
            Undo.IncrementCurrentGroup();
        }
        [TearDown] public void TearDown()
        {
            Undo.ClearUndo(map); Undo.ClearUndo(rules);
            Object.DestroyImmediate(map); Object.DestroyImmediate(rules);
        }
        static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        [Test] public void MovesEverySpatialRecordAndPreservesDirectionsIdsAndRelativeOffsets()
        {
            map.SetTerrain(P(3, 3), TerrainMaterial.Stone, true);
            map.devices.Add(new DeviceCell { kind = DeviceKind.Portal, position = P(3, 2), portalTarget = P(7, 2), direction = Vector2Int.up });
            map.decorations.Add(new DecorationCell { position = P(3, 4), placementId = "decor", connectionGroup = "network" });
            map.bubbles.Add(P(4, 4));
            var portal = new PrototypeDevice { id = "portal", role = PrototypeRole.Portal, position = P(4, 2), target = P(5, 2), direction = Vector2Int.left };
            var chick = new PrototypeDevice { id = "chick", role = PrototypeRole.Chick, position = P(2, 3), target = P(4, 5) };
            var socket = new PrototypeDevice { id = "socket", role = PrototypeRole.StarSocket, variant = "socket", position = P(5, 3), socketReward = StarSocketReward.RevealBubble, socketBubbleCell = P(6, 3), socketBridgeId = "bridge" };
            var bomb = new PrototypeDevice { id = "bomb", role = PrototypeRole.Bomb, position = P(4, 3), target = Vector2Int.zero, orientation = 3, active = true, blastOffsets = { Vector2Int.down, Vector2Int.left } };
            var bridge = new PrototypeDevice { id = "bridge", role = PrototypeRole.VineBridge, position = P(5, 4), cells = { P(6, 4) } };
            rules.devices.AddRange(new[] { portal, chick, socket, bomb, bridge });
            rules.railPaths.Add(new PrototypeRailPath { id = "rail", cells = { P(4, 6), P(5, 6) } });
            rules.water.Add(P(6, 4)); rules.nests.Add(P(4, 5));
            var pond = new RanchV13ArtPlacement { family = "sluice", position = P(5, 7), pondB = P(6, 7), hasPondB = true, instanceId = "pond" };
            var sheep = new RanchV13ArtPlacement { family = "sheep", position = P(5, 8), direction = Vector2Int.down };
            var sheepId = sheep.Id; rules.ranchV13Art.AddRange(new[] { pond, sheep });
            rules.goals.Add(new PrototypeGoal { kind = PrototypeGoalKind.Position, cell = P(6, 5), deviceId = "chick" });
            rules.goals.Add(new PrototypeGoal { kind = PrototypeGoalKind.Visited, cell = P(7, 5) });
            rules.goals.Add(new PrototypeGoal { kind = PrototypeGoalKind.DiceTop, cell = Vector2Int.zero, number = 4, deviceId = "die" });
            rules.verificationRoute = "URDL";

            Assert.That(PuzzleMapTranslation.Move(map, Vector2Int.one), Is.EqualTo(Vector2Int.one));
            Assert.That(map.start, Is.EqualTo(P(3, 3))); Assert.That(map.exit, Is.EqualTo(P(7, 7)));
            Assert.That(map.TryTerrain(P(3, 3), out _), Is.False);
            Assert.That(map.TryTerrain(P(4, 4), out var tile), Is.True); Assert.That(tile.blocked, Is.True);
            Assert.That(tile.material, Is.EqualTo(TerrainMaterial.Stone));
            Assert.That(map.devices[0].portalTarget, Is.EqualTo(P(8, 3)));
            Assert.That(map.devices[0].direction, Is.EqualTo(Vector2Int.up));
            Assert.That(map.decorations[0].position, Is.EqualTo(P(4, 5)));
            Assert.That(map.decorations[0].placementId, Is.EqualTo("decor"));
            Assert.That(map.decorations[0].connectionGroup, Is.EqualTo("network"));
            Assert.That(map.bubbles.Single(), Is.EqualTo(P(5, 5)));
            Assert.That(portal.target, Is.EqualTo(P(6, 3))); Assert.That(portal.direction, Is.EqualTo(Vector2Int.left));
            Assert.That(chick.target, Is.EqualTo(P(5, 6))); Assert.That(socket.socketBubbleCell, Is.EqualTo(P(7, 4)));
            Assert.That(socket.socketBridgeId, Is.EqualTo("bridge")); Assert.That(bridge.cells.Single(), Is.EqualTo(P(7, 5)));
            Assert.That(bomb.blastOffsets, Is.EqualTo(new[] { Vector2Int.down, Vector2Int.left }));
            Assert.That(bomb.target, Is.EqualTo(Vector2Int.zero)); Assert.That(bomb.orientation, Is.EqualTo(3)); Assert.That(bomb.active, Is.True);
            Assert.That(rules.railPaths[0].cells, Is.EqualTo(new[] { P(5, 7), P(6, 7) }));
            Assert.That(rules.water.Single(), Is.EqualTo(P(7, 5))); Assert.That(rules.nests.Single(), Is.EqualTo(P(5, 6)));
            Assert.That(pond.position, Is.EqualTo(P(6, 8))); Assert.That(pond.pondB, Is.EqualTo(P(7, 8)));
            Assert.That(sheep.Id, Is.EqualTo(sheepId)); Assert.That(sheep.direction, Is.EqualTo(Vector2Int.down));
            Assert.That(rules.goals[0].cell, Is.EqualTo(P(7, 6))); Assert.That(rules.goals[1].cell, Is.EqualTo(P(8, 6)));
            Assert.That(rules.goals[2].cell, Is.EqualTo(Vector2Int.zero)); Assert.That(rules.verificationRoute, Is.EqualTo("URDL"));
        }

        [Test] public void ClampsToEveryEdgeAndImmediatelyAllowsReverseOrFreeAxis()
        {
            map.Resize(8, 7); map.exit = P(4, 4);
            Assert.That(PuzzleMapTranslation.Move(map, P(99, 99)), Is.EqualTo(P(3, 2)));
            var before = JsonUtility.ToJson(map); var scenario = JsonUtility.ToJson(rules);
            Assert.That(PuzzleMapTranslation.Move(map, Vector2Int.one), Is.EqualTo(Vector2Int.zero));
            Assert.That(JsonUtility.ToJson(map), Is.EqualTo(before)); Assert.That(JsonUtility.ToJson(rules), Is.EqualTo(scenario));
            Assert.That(PuzzleMapTranslation.Move(map, P(-1, 1)), Is.EqualTo(Vector2Int.left));
            Assert.That(PuzzleMapTranslation.Move(map, P(-99, -99)), Is.EqualTo(P(-4, -4)));
            Assert.That(PuzzleMapTranslation.Move(map, P(-1, -1)), Is.EqualTo(Vector2Int.zero));
            Assert.That(PuzzleMapTranslation.Move(map, Vector2Int.one), Is.EqualTo(Vector2Int.one));
        }

        [TestCase(0), TestCase(1)] public void BoundsIncludeBothEndsOfTheInitialBookshelf(int orientation)
        {
            map.start = map.exit = P(2, 2);
            rules.devices.Add(new PrototypeDevice { id = "shelf", role = PrototypeRole.Bookshelf, position = P(2, 2), orientation = orientation });
            var delta = orientation == 0 ? Vector2Int.left : Vector2Int.down;
            Assert.That(PuzzleMapTranslation.Move(map, delta * 99), Is.EqualTo(delta));
            Assert.That(PuzzleMapTranslation.Move(map, delta), Is.EqualTo(Vector2Int.zero));
            Assert.That(PuzzleMapTranslation.Bounds(map).min.x, Is.GreaterThanOrEqualTo(0));
            Assert.That(PuzzleMapTranslation.Bounds(map).min.y, Is.GreaterThanOrEqualTo(0));
        }

        [Test] public void RemotePortalDestinationAndPondBAlsoStopAtTheBoundary()
        {
            map.devices.Add(new DeviceCell { kind = DeviceKind.Portal, position = P(3, 3), portalTarget = P(10, 3) });
            rules.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", position = P(4, 9), hasPondB = true, pondB = P(4, 11) });
            Assert.That(PuzzleMapTranslation.Move(map, P(9, 9)), Is.EqualTo(Vector2Int.right));
            Assert.That(map.devices[0].portalTarget.x, Is.EqualTo(11));
            Assert.That(rules.ranchV13Art[0].pondB.y, Is.EqualTo(11));
        }

        [Test] public void UndoRedoRestoresBothMapAndScenarioTogether()
        {
            rules.devices.Add(new PrototypeDevice { id = "die", role = PrototypeRole.Dice, position = P(3, 3) });
            var before = JsonUtility.ToJson(map); var beforeRules = JsonUtility.ToJson(rules);
            PuzzleMapTranslation.Move(map, P(2, 1));
            var after = JsonUtility.ToJson(map); var afterRules = JsonUtility.ToJson(rules);
            Undo.PerformUndo();
            Assert.That(JsonUtility.ToJson(map), Is.EqualTo(before)); Assert.That(JsonUtility.ToJson(rules), Is.EqualTo(beforeRules));
            Undo.PerformRedo();
            Assert.That(JsonUtility.ToJson(map), Is.EqualTo(after)); Assert.That(JsonUtility.ToJson(rules), Is.EqualTo(afterRules));
        }

        [Test] public void CroppedRecordsAndCertifiedCoordinatesAreNotSilentlyChanged()
        {
            map.bubbles.Add(P(15, 2)); var before = JsonUtility.ToJson(map);
            Assert.That(PuzzleMapTranslation.Available(map, out _), Is.False);
            Assert.That(PuzzleMapTranslation.Move(map, Vector2Int.left), Is.EqualTo(Vector2Int.zero));
            Assert.That(JsonUtility.ToJson(map), Is.EqualTo(before)); map.bubbles.Clear();
            var certificate = new TextAsset("fixed coordinates"); map.starwayV3Certificate = certificate;
            try { Assert.That(PuzzleMapTranslation.Move(map, Vector2Int.one), Is.EqualTo(Vector2Int.zero)); }
            finally { Object.DestroyImmediate(certificate); }
        }

        [Test] public void LegacyNestMovedToOriginRemainsAnExplicitNestAfterFurtherMovement()
        {
            map.start = P(2, 2); map.exit = P(3, 3);
            rules.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick, position = P(2, 1), target = P(1, 1) });
            PuzzleMapTranslation.Move(map, -Vector2Int.one);
            Assert.That(rules.NestCells, Does.Contain(Vector2Int.zero));
            PuzzleMapTranslation.Move(map, Vector2Int.one);
            Assert.That(rules.NestCells, Does.Contain(Vector2Int.one));
        }

        [Test] public void CurrentTheaterGameplayTransitionsAreEquivalentAfterWholeMapMovement()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-10_MapData.asset");
            var original = Object.Instantiate(source); var originalRules = Object.Instantiate(source.scenario); original.scenario = originalRules;
            var shifted = Object.Instantiate(source); var shiftedRules = Object.Instantiate(source.scenario); shifted.scenario = shiftedRules;
            var sourceJson = JsonUtility.ToJson(source); var rulesJson = JsonUtility.ToJson(source.scenario);
            var sourceDirty = EditorUtility.IsDirty(source); var rulesDirty = EditorUtility.IsDirty(source.scenario);
            try
            {
                original.Resize(original.Width + 2, original.Height + 2); shifted.Resize(shifted.Width + 2, shifted.Height + 2);
                var beforeRows = PuzzleMapEditor.TheaterTerrainRows(original);
                Assert.That(PuzzleMapTranslation.Move(shifted, Vector2Int.one), Is.EqualTo(Vector2Int.one));
                var afterRows = PuzzleMapEditor.TheaterTerrainRows(shifted);
                foreach (var tile in original.terrain)
                    Assert.That(afterRows[shifted.Height - tile.position.y - 2][tile.position.x + 1],
                        Is.EqualTo(beforeRows[original.Height - tile.position.y - 1][tile.position.x]), "Theater material appearance moved with its tile");
                Assert.That(shifted.ValidateMap(), Is.EqualTo(original.ValidateMap()));
                var a = new PuzzleDeviceSession(original); var b = new PuzzleDeviceSession(shifted);
                foreach (var c in "NNEENSNENWEEESNESENSENNNWSSWWWNEESENNENWWWN")
                {
                    var input = c == 'N' ? Vector2Int.up : c == 'E' ? Vector2Int.right : c == 'S' ? Vector2Int.down : Vector2Int.left;
                    Assert.That(b.Apply(input).accepted, Is.EqualTo(a.Apply(input).accepted));
                    Assert.That(b.Cell, Is.EqualTo(a.Cell + Vector2Int.one)); Assert.That(b.Cost, Is.EqualTo(a.Cost));
                    Assert.That(b.Won, Is.EqualTo(a.Won)); Assert.That(b.Failed, Is.EqualTo(a.Failed)); Assert.That(b.Collected, Is.EqualTo(a.Collected));
                    foreach (var id in a.States.Keys)
                    {
                        var first = a.States[id]; var second = b.States[id];
                        Assert.That(second.cell, Is.EqualTo(first.cell + Vector2Int.one), id);
                        Assert.That(second.dice, Is.EqualTo(first.dice), id); Assert.That(second.active, Is.EqualTo(first.active), id);
                        Assert.That(second.converted, Is.EqualTo(first.converted), id); Assert.That(second.phase, Is.EqualTo(first.phase), id);
                    }
                    if (a.Won || a.Failed) break;
                }
                Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceJson)); Assert.That(JsonUtility.ToJson(source.scenario), Is.EqualTo(rulesJson));
                Assert.That(EditorUtility.IsDirty(source), Is.EqualTo(sourceDirty)); Assert.That(EditorUtility.IsDirty(source.scenario), Is.EqualTo(rulesDirty));
            }
            finally
            {
                Undo.ClearUndo(shifted); Undo.ClearUndo(shiftedRules);
                Object.DestroyImmediate(original); Object.DestroyImmediate(originalRules); Object.DestroyImmediate(shifted); Object.DestroyImmediate(shiftedRules);
            }
        }

        [Test] public void SelectAllArrowKeysAndEscapeUseTheWholeMapMode()
        {
            var window = ScriptableObject.CreateInstance<PuzzleMapEditor>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(PuzzleMapEditor).GetMethod("Select", flags).Invoke(window, new object[] { map });
                var handle = typeof(PuzzleMapEditor).GetMethod("HandleMapMoveKeyboard", flags);
                handle.Invoke(window, new object[] { new Event { type = EventType.KeyDown, keyCode = KeyCode.A, modifiers = EventModifiers.Control } });
                Assert.That(typeof(PuzzleMapEditor).GetField("moveWholeMap", flags).GetValue(window), Is.True);
                handle.Invoke(window, new object[] { new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow } });
                Assert.That(map.start, Is.EqualTo(P(3, 2)));
                handle.Invoke(window, new object[] { new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape } });
                Assert.That(typeof(PuzzleMapEditor).GetField("moveWholeMap", flags).GetValue(window), Is.False);
            }
            finally { Object.DestroyImmediate(window); }
        }

        [UnityTest] public IEnumerator RealEditorDragClampsReversesAndUndoesWithoutPainting()
        {
            map.Resize(8, 7); map.exit = P(4, 4);
            map.SetTerrain(map.start, TerrainMaterial.Grass); map.SetTerrain(map.exit, TerrainMaterial.Stone);
            var window = ScriptableObject.CreateInstance<PuzzleMapEditor>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(PuzzleMapEditor).GetMethod("Select", flags).Invoke(window, new object[] { map });
                window.position = new Rect(100, 100, 1200, 850); window.ShowUtility(); window.Focus();
                var deadline = EditorApplication.timeSinceStartup + 3;
                while (((Rect)typeof(PuzzleMapEditor).GetField("mapMoveViewport", flags).GetValue(window)).height < 100 &&
                    EditorApplication.timeSinceStartup < deadline)
                { window.Repaint(); yield return null; }
                typeof(PuzzleMapEditor).GetMethod("SetMapMoveMode", flags).Invoke(window, new object[] { true });
                var viewport = (Rect)typeof(PuzzleMapEditor).GetField("mapMoveViewport", flags).GetValue(window);
                var origin = (Vector2)typeof(PuzzleMapEditor).GetField("mapMoveGridOrigin", flags).GetValue(window);
                var shownHeight = (int)typeof(PuzzleMapEditor).GetField("mapMoveShownHeight", flags).GetValue(window);
                var cell = (float)typeof(PuzzleMapEditor).GetField("cellSize", flags).GetValue(window);
                var scroll = (Vector2)typeof(PuzzleMapEditor).GetField("gridScroll", flags).GetValue(window);
                var point = viewport.position + origin - scroll + new Vector2(2.5f, shownHeight - 2.5f) * cell;
                Assert.That(viewport.Contains(point), Is.True, "viewport=" + viewport + " origin=" + origin + " height=" + shownHeight + " scroll=" + scroll + " point=" + point);
                var before = JsonUtility.ToJson(map);
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
                window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + Vector2.right * cell * 99 });
                Assert.That(map.exit.x, Is.EqualTo(7));
                window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + Vector2.right * cell * 100 });
                Assert.That(map.exit.x, Is.EqualTo(7));
                window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + Vector2.right * cell * 99 });
                Assert.That(map.exit.x, Is.EqualTo(6));
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point + Vector2.right * cell * 99 });
                Assert.That(map.terrain, Has.Count.EqualTo(2));
                var after = JsonUtility.ToJson(map);
                Undo.PerformUndo(); Assert.That(JsonUtility.ToJson(map), Is.EqualTo(before));
                Undo.PerformRedo(); Assert.That(JsonUtility.ToJson(map), Is.EqualTo(after));
                window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape });
                Assert.That(typeof(PuzzleMapEditor).GetField("moveWholeMap", flags).GetValue(window), Is.False);
            }
            finally { if (window) window.Close(); }
        }
    }
}
