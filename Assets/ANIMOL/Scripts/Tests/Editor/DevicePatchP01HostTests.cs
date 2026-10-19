using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Art.DevicePatch01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class DevicePatchP01HostTests
    {
        readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
        PuzzleMap Clone(string name)
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device" + name + ".asset");
            Assert.That(source, Is.Not.Null);
            var map = UnityEngine.Object.Instantiate(source);
            map.scenario = UnityEngine.Object.Instantiate(source.scenario);
            map.scenario.costLimit = 0;
            map.targetBubbles = 0;
            map.bubbles.Clear();
            map.exit = new Vector2Int(7, 5);
            map.InvalidateIndex();
            temporary.Add(map.scenario); temporary.Add(map);
            return map;
        }
        [TearDown] public void Dispose()
        { foreach (var obj in temporary) UnityEngine.Object.DestroyImmediate(obj); temporary.Clear(); }

        [TestCase("starway")]
        [TestCase("memory_theater")]
        public void HostGateHasOneOccupiedCellAndFourStrictHeadings(string theme)
        {
            var headings = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            foreach (var allowed in headings)
            {
                var map = Clone("09_OneWayGate");
                map.scenario.themeId = theme;
                var gate = map.scenario.devices.Single(d => d.role == PrototypeRole.OneWayGate);
                gate.position = new Vector2Int(4, 3); gate.direction = allowed;
                foreach (var attempt in headings)
                {
                    map.start = gate.position - attempt;
                    var session = new PuzzleDeviceSession(map);
                    Assert.That(session.Footprint(gate), Is.EquivalentTo(new[] { gate.position }));
                    Assert.That(session.Footprint(gate).Contains(gate.position + allowed), Is.False);
                    var result = session.Apply(attempt);
                    Assert.That(result.accepted, Is.EqualTo(attempt == allowed), theme + " " + allowed + "/" + attempt);
                    Assert.That(session.Cost, Is.EqualTo(attempt == allowed ? 1 : 0));
                    Assert.That(session.Cell, Is.EqualTo(attempt == allowed ? gate.position : map.start));
                    if (attempt != allowed)
                        Assert.That(result.events.Any(e => e.eventName == "existing_rejected_gate_contact" &&
                            e.state == "blocked_" + PuzzleDeviceSession.Dir(allowed)), Is.True);
                }
                map.start = gate.position - allowed;
                map.SetTerrain(gate.position + allowed, TerrainMaterial.Grass, true);
                var blocked = new PuzzleDeviceSession(map);
                var denied = blocked.Apply(allowed);
                Assert.That(denied.accepted, Is.False);
                Assert.That(blocked.Cost, Is.Zero);
                Assert.That(denied.events.Any(e => e.state == "blocked_" + PuzzleDeviceSession.Dir(allowed)), Is.True);
                map.SetTerrain(gate.position + allowed, TerrainMaterial.Grass);
                map.start = gate.position;
                foreach (var side in headings.Where(h => h != allowed))
                {
                    var inside = new PuzzleDeviceSession(map);
                    Assert.That(inside.Apply(side).accepted, Is.False, "inside side exit " + side);
                    Assert.That(inside.Cost, Is.Zero);
                }
            }
        }

        [Test]
        public void HostWindTurnsAtInstalledCornerAndForceMatchesDisplayedRoute()
        {
            var map = Clone("20_Wind");
            var blower = map.scenario.devices.Single(d => d.variant == "blower");
            var metal = map.scenario.devices.Single(d => d.role == PrototypeRole.MetalCrate);
            map.scenario.devices.Add(new PrototypeDevice {
                id = "corner", role = PrototypeRole.Bounce, position = new Vector2Int(3, 4),
                direction = Vector2Int.right, orientation = 2
            });
            var session = new PuzzleDeviceSession(map);
            var route = session.WindRouteFor(blower.id);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.cells.Select(x => new Vector2Int(x.cell.x, x.cell.y)),
                Does.Contain(new Vector2Int(3, 4)));
            Assert.That(session.WindCells[new Vector2Int(3, 4)], Is.EqualTo(Vector2Int.up));
            foreach (var cell in route.cells)
            {
                Assert.That(route.TryGetForce(cell.cell, out var direction), Is.True);
                Assert.That(session.WindCells[new Vector2Int(cell.cell.x, cell.cell.y)],
                    Is.EqualTo(direction == WindHeading.North ? Vector2Int.up :
                        direction == WindHeading.East ? Vector2Int.right :
                        direction == WindHeading.South ? Vector2Int.down : Vector2Int.left));
            }
            Assert.That(route.cells.Any(x => x.cell.x == metal.position.x && x.cell.y == metal.position.y), Is.False);
            Assert.That(route.maxRangeCells, Is.EqualTo(map.Width * map.Height));
        }

        [Test]
        public void TheaterDominoAndBookcaseKeepOriginalCellContracts()
        {
            var domino = new PuzzleDeviceSession(Clone("13_Domino"));
            foreach (var d in domino.Devices.Where(d => d.role == PrototypeRole.Domino))
                Assert.That(domino.Footprint(d).Count(), Is.EqualTo(1));
            var action = domino.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(domino.Cost, Is.EqualTo(1));
            Assert.That(action.events.Count(e => e.eventName == "this_domino_piece_fold_committed"), Is.EqualTo(3));
            var bookcase = new PuzzleDeviceSession(Clone("08_Bookshelf"));
            var shelf = bookcase.Devices.Single(d => d.role == PrototypeRole.Bookshelf && d.variant != "plate");
            var center = bookcase.States[shelf.id].cell;
            Assert.That(bookcase.Footprint(shelf), Is.EquivalentTo(new[] {
                center + Vector2Int.left, center, center + Vector2Int.right }));
            bookcase.Apply(Vector2Int.right);
            Assert.That(bookcase.States[shelf.id].cell, Is.EqualTo(center));
            Assert.That(bookcase.Footprint(shelf), Is.EquivalentTo(new[] {
                center + Vector2Int.down, center, center + Vector2Int.up }));
        }
    }
}
