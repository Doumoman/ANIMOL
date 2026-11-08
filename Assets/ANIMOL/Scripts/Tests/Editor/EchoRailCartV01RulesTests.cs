using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using ANIMOL.Art.EchoRailCartV01;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class EchoRailCartV01RulesTests
    {
        readonly List<Object> temporary = new List<Object>();

        PuzzleMap Room(Vector2Int start)
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device06_MovingStage.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.scenario.costLimit = 0;
            map.start = start;
            map.InvalidateIndex();
            temporary.Add(map.scenario);
            temporary.Add(map);
            Assert.That(map.ValidateMap(), Is.Empty);
            return map;
        }

        [TearDown] public void Cleanup()
        {
            foreach (var item in temporary) Object.DestroyImmediate(item);
            temporary.Clear();
        }

        [Test] public void PlayerMovesFirstThenCartMovesOneRailCell()
        {
            var session = new PuzzleDeviceSession(Room(new Vector2Int(1, 2)));
            var action = session.Apply(Vector2Int.up);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(1, 3)));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.States["stage"].direction, Is.EqualTo(Vector2Int.right));
            var moves = action.events.Where(e => e.kind == PrototypeEventKind.Move).ToArray();
            Assert.That(moves.Select(e => e.actor), Is.EqualTo(new[] { "player", "stage" }));
            Assert.That(moves[1].to - moves[1].from, Is.EqualTo(Vector2Int.right));
        }

        [TestCase(PrototypeRole.WoodCrate, false)]
        [TestCase(PrototypeRole.MetalCrate, false)]
        [TestCase(PrototypeRole.Dice, false)]
        [TestCase(PrototypeRole.Dice, true)]
        [TestCase(PrototypeRole.Domino, false)]
        public void StationaryPushFinishesBeforeCartPushesPlayerAndUndoesTogether(PrototypeRole role, bool goal)
        {
            var map = Room(new Vector2Int(3, 2));
            map.scenario.echoTheaterV3Rules = true;
            map.scenario.devices.Add(new PrototypeDevice { id = "body", role = role,
                position = new Vector2Int(3, 3), direction = Vector2Int.up });
            if (goal)
                map.scenario.devices.Add(new PrototypeDevice { id = "goal", role = PrototypeRole.Dice,
                    variant = "socket", position = new Vector2Int(3, 4), number = 2 });
            var session = new PuzzleDeviceSession(map);
            var before = session.SearchStateKey();
            var action = session.Apply(Vector2Int.up);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.States["body"].cell,
                Is.EqualTo(new Vector2Int(3, role == PrototypeRole.Domino ? 3 : 4)));
            if (role == PrototypeRole.Domino) Assert.That(session.States["body"].active, Is.True);
            if (goal)
            {
                Assert.That(session.States["body"].removed, Is.True);
                Assert.That(session.States["goal"].converted, Is.True);
            }
            var cartIndex = action.events.FindIndex(e => e.kind == PrototypeEventKind.Move && e.actor == "stage");
            Assert.That(cartIndex, Is.GreaterThan(0));
            Assert.That(action.events.Take(cartIndex).Any(e => e.actor == "player"), Is.False);
            Assert.That(action.events.Take(cartIndex).Any(e => e.actor == "body"), Is.True);
            Assert.That(action.events.Count(e => e.kind == PrototypeEventKind.Move && e.actor == "stage"), Is.EqualTo(1));
            Assert.That(action.events.Last(e => e.kind == PrototypeEventKind.Move).actor, Is.EqualTo("player"));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
            Assert.That(session.Cost, Is.Zero);
        }

        [TestCase(false)] [TestCase(true)]
        public void StationaryPushAndCartRespectBlockedDestinations(bool rejectPush)
        {
            var map = Room(new Vector2Int(3, 2));
            map.scenario.echoTheaterV3Rules = true;
            map.scenario.devices.Add(new PrototypeDevice { id = "body", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(3, 3), direction = Vector2Int.up });
            map.scenario.devices.Add(new PrototypeDevice { id = "wall", role = PrototypeRole.Wall,
                position = rejectPush ? new Vector2Int(3, 4) : new Vector2Int(4, 2) });
            var session = new PuzzleDeviceSession(map);
            var before = session.SearchStateKey();
            var action = session.Apply(Vector2Int.up);
            Assert.That(action.accepted, Is.EqualTo(!rejectPush));
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.Cost, Is.EqualTo(rejectPush ? 0 : 1));
            Assert.That(session.States["body"].cell, Is.EqualTo(new Vector2Int(3, rejectPush ? 3 : 4)));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(action.events.Any(e => e.kind == PrototypeEventKind.Move && e.actor == "stage"), Is.False);
            if (rejectPush) Assert.That(session.SearchStateKey(), Is.EqualTo(before));
            else
            {
                Assert.That(session.Undo(), Is.True);
                Assert.That(session.SearchStateKey(), Is.EqualTo(before));
            }
        }

        [Test] public void Et06UsesItsRegisteredCartAsAOneCellBlocker()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-06_MapData.asset");
            var session = new PuzzleDeviceSession(map);
            var cart = session.Devices.Single(d => d.role == PrototypeRole.MovingStage);
            Assert.That(cart.id, Is.EqualTo("platform_1"));
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(13, 8)));
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(12, 8)));
            var blocked = session.Apply(Vector2Int.right);
            Assert.That(blocked.accepted, Is.False);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(11, 8)));
            Assert.That(session.Cost, Is.EqualTo(2));
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(12, 8)));
        }

        [Test] public void CartBlocksPlayerAndCrateWithoutCostOrCartTurn()
        {
            var map = Room(new Vector2Int(1, 2));
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.False);
            Assert.That(session.Cost, Is.Zero);
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));

            map = Room(new Vector2Int(4, 2));
            map.scenario.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(3, 2), direction = Vector2Int.left });
            map.InvalidateIndex();
            session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.left).accepted, Is.False);
            Assert.That(session.States["crate"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.Cost, Is.Zero);
        }

        [Test] public void CartPushesPlayerInItsCommittedDirectionOnce()
        {
            var session = new PuzzleDeviceSession(Room(new Vector2Int(3, 3)));
            var action = session.Apply(Vector2Int.down);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(session.Direction, Is.EqualTo(Vector2Int.right));
            var moves = action.events.Where(e => e.kind == PrototypeEventKind.Move).ToArray();
            Assert.That(moves.Select(e => e.actor), Is.EqualTo(new[] { "player", "stage", "player" }));
            Assert.That(moves[2].from, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(moves[2].to, Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(action.events.Any(e => e.eventName == "rail_cart_player_pushed"), Is.True);
            Assert.That(action.events.Count(e => e.kind == PrototypeEventKind.Move && e.actor == "stage"), Is.EqualTo(1));
        }

        [TestCase(true)] [TestCase(false)]
        public void CartCannotPushPlayerIntoAClosedReflectorFace(bool allowed)
        {
            var map = Room(new Vector2Int(3, 3));
            var reflector = new Vector2Int(4, 2);
            map.scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce,
                position = reflector, orientation = allowed ? 2 : 0 });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            var action = session.Apply(Vector2Int.down);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Cell, Is.EqualTo(allowed ? reflector + Vector2Int.up : map.start));
            Assert.That(action.events.Any(e => e.eventName == "rail_cart_player_pushed"), Is.EqualTo(allowed));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void BlockedPushReturnsPlayerToInputStart()
        {
            var map = Room(new Vector2Int(3, 3));
            map.scenario.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(4, 2), direction = Vector2Int.left });
            map.InvalidateIndex();
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.down);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(action.events.Any(e => e.eventName == "rail_cart_player_returned_p0"), Is.True);
            Assert.That(action.events.Count(e => e.kind == PrototypeEventKind.Move && e.actor == "stage"), Is.EqualTo(1));
        }

        [Test] public void ObstacleStopsCartButValidPlayerMoveStillCostsOne()
        {
            var map = Room(new Vector2Int(1, 3));
            map.scenario.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(3, 2), direction = Vector2Int.left });
            map.InvalidateIndex();
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.down);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(action.events.Any(e => e.kind == PrototypeEventKind.Move && e.actor == "stage"), Is.False);
        }

        [Test] public void ClosedRailLoopTurnsAtCornersAndUndoRestoresHeading()
        {
            var map = Room(new Vector2Int(1, 4));
            map.scenario.devices.Single(d => d.id == "stage").cells = new List<Vector2Int>
            {
                new Vector2Int(2, 2), new Vector2Int(3, 2),
                new Vector2Int(3, 3), new Vector2Int(2, 3)
            };
            var session = new PuzzleDeviceSession(map);
            foreach (var heading in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down })
            {
                Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
                Assert.That(session.States["stage"].direction, Is.EqualTo(heading));
            }
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(session.States["stage"].direction, Is.EqualTo(Vector2Int.left));
            session.Reset();
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.States["stage"].direction, Is.EqualTo(Vector2Int.right));
        }
        [Test] public void EditorAuthoredFloorRailOwnsCartRouteAndCanBeEdited()
        {
            var map = Room(new Vector2Int(1, 4));
            var template = map.scenario.devices.Single(d => d.id == "stage");
            map.scenario.devices.Clear();
            string id = null, error;
            Assert.That(PuzzleMapEditor.TryPlaceRailCell(map, new Vector2Int(2, 2), id, out id, out error), Is.True, error);
            Assert.That(PuzzleMapEditor.CanStampPrototype(map, template, new Vector2Int(2, 2)), Is.False);
            foreach (var cell in new[] { new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(2, 3) })
                Assert.That(PuzzleMapEditor.TryPlaceRailCell(map, cell, id, out id, out error), Is.True, error);
            Assert.That(map.scenario.railPaths.Single().cells.Count, Is.EqualTo(4));
            Assert.That(PuzzleMapEditor.CanStampPrototype(map, template, new Vector2Int(2, 2)), Is.True);
            var cart = PuzzleMapEditor.StampTemplate(map.scenario, template, new Vector2Int(2, 2));
            Assert.That(cart.cells, Is.Empty);
            Assert.That(cart.direction, Is.EqualTo(Vector2Int.right));
            Assert.That(map.scenario.RailCellsFor(cart), Is.SameAs(map.scenario.railPaths.Single().cells));
            Assert.That(map.ValidateMap(), Is.Empty);
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(3, 3)));
            Assert.That(session.States[cart.id].direction, Is.EqualTo(Vector2Int.up));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.States[cart.id].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(PuzzleMapEditor.TryPlaceRailCell(map, new Vector2Int(4, 2), id, out _, out error), Is.False);
            Assert.That(error, Does.Contain("끝 칸"));
            Assert.That(PuzzleMapEditor.TryEraseRailCell(map, new Vector2Int(3, 2), out error), Is.False);
            Assert.That(error, Does.Contain("끝 칸"));
            Assert.That(PuzzleMapEditor.TryEraseRailCell(map, new Vector2Int(2, 2), out error), Is.False);
            map.scenario.devices.Clear();
            Assert.That(PuzzleMapEditor.TryEraseRailCell(map, new Vector2Int(2, 3), out error), Is.True, error);
            Assert.That(map.scenario.railPaths.Single().cells.Count, Is.EqualTo(3));
            Assert.That(EchoRailCartRailTopologyV01.TryKey(
                new[] { new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(2, 3) },
                0, true, Vector2Int.right, out var key), Is.True);
            Assert.That(key, Is.EqualTo("ne"));
        }

        [Test] public void PaintingPastLegacyTwoCellCartAutomaticallyExpandsItsRoute()
        {
            var map = Room(new Vector2Int(1, 4));
            var cart = map.scenario.devices.Single(d => d.id == "stage");
            Assert.That(cart.cells.Count, Is.EqualTo(2));
            Assert.That(PuzzleMapEditor.TryPlaceRailCell(map, new Vector2Int(3, 3), null,
                out var pathId, out var error), Is.True, error);
            Assert.That(pathId, Is.EqualTo("rail_1"));
            Assert.That(cart.cells, Is.Empty);
            Assert.That(map.scenario.RailCellsFor(cart).Count, Is.EqualTo(3));
            Assert.That(map.scenario.RailCellsFor(cart).Last(), Is.EqualTo(new Vector2Int(3, 3)));
            Assert.That(map.ValidateMap(), Is.Empty);
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.States["stage"].cell, Is.EqualTo(new Vector2Int(3, 3)));
            Assert.That(session.States["stage"].direction, Is.EqualTo(Vector2Int.up));
        }
    }
}
