using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public sealed class RanchR2HostTests
    {
        PuzzleMap map;
        PuzzleScenario scenario;

        [SetUp] public void Setup()
        {
            map = ScriptableObject.CreateInstance<PuzzleMap>();
            map.Resize(7, 5);
            for (var y = 0; y < 5; y++) for (var x = 0; x < 7; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = new Vector2Int(1, 2);
            map.exit = new Vector2Int(6, 4);
            map.targetBubbles = 0;
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            scenario = ScriptableObject.CreateInstance<PuzzleScenario>();
            scenario.themeId = "starway";
            map.scenario = scenario;
        }

        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(map);
            Object.DestroyImmediate(scenario);
        }

        [Test] public void PondCrossesBothDirectionsAndUndoRestoresOneSnapshot()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", position = map.start, instanceId = "pond" });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.RanchWet(new Vector2Int(2, 2)), Is.True);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.RanchWet(map.start), Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Apply(Vector2Int.left).accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.RanchWet(new Vector2Int(2, 2)), Is.True);
            Assert.That(session.Cost, Is.EqualTo(2));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.RanchWet(map.start), Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            session.Reset();
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.RanchWet(new Vector2Int(2, 2)), Is.True);
            Assert.That(session.Cost, Is.Zero);
        }

        [TestCase(1, 2, 2, 2, false)]
        [TestCase(2, 2, 1, 2, false)]
        [TestCase(2, 2, 2, 3, false)]
        [TestCase(2, 3, 2, 2, false)]
        [TestCase(1, 2, 2, 2, true)]
        [TestCase(2, 2, 1, 2, true)]
        [TestCase(2, 2, 2, 3, true)]
        [TestCase(2, 3, 2, 2, true)]
        public void PondHostKeepsWaterOppositePlayerForBothAxesAndEitherLabelOrder(
            int ax, int ay, int bx, int by, bool wetA)
        {
            var a = new Vector2Int(ax, ay); var b = new Vector2Int(bx, by);
            var dry = wetA ? b : a; var wet = wetA ? a : b;
            map.start = dry;
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice",
                position = a, direction = b - a, rightState = wetA, instanceId = "pond" });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(wet - dry);
            Assert.That(action.accepted, Is.True, action.reason);
            var cue = action.events.Single(e => e.eventName == "PondCrossed");
            Assert.That(cue.pondBefore.Wet, Is.EqualTo(new ANIMOL.Gameplay.RanchDevicesR2.RanchCellR2(wet.x, wet.y)));
            Assert.That(cue.pondAfter.Wet, Is.EqualTo(new ANIMOL.Gameplay.RanchDevicesR2.RanchCellR2(dry.x, dry.y)));
            Assert.That(cue.ranchResult.actualRoute.Select(p => new Vector2Int(p.x, p.y)), Is.EqualTo(new[] { dry, wet }));
            Assert.That(cue.ranchResult.waterRoute.Select(p => new Vector2Int(p.x, p.y)), Is.EqualTo(new[] { wet, dry }));
            Assert.That(session.Cell, Is.EqualTo(wet));
            Assert.That(session.RanchWet(dry), Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Cell, Is.EqualTo(dry));
            Assert.That(session.RanchWet(wet), Is.True);
            Assert.That(session.Cost, Is.Zero);
        }

        [Test] public void ExplicitBCellIsAuthoritativeOverLegacyDirection()
        {
            map.start = new Vector2Int(2, 2);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", instanceId = "pond",
                position = map.start, direction = Vector2Int.right,
                hasPondB = true, pondB = new Vector2Int(2, 3) });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.up).accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(session.RanchWet(map.start), Is.True);
        }

        [Test] public void ChickFollowsOntoDrySluiceCell()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice",
                position = new Vector2Int(2, 2), instanceId = "pond" });
            scenario.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                position = new Vector2Int(0, 2), target = new Vector2Int(6, 3) });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(session.Apply(Vector2Int.up).accepted, Is.True);
            var follow = session.Apply(Vector2Int.right);
            Assert.That(follow.accepted, Is.True);
            Assert.That(session.RanchWet(new Vector2Int(2, 2)), Is.False);
            Assert.That(session.States["chick"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(follow.events.Any(e => e.actor == "chick" && e.kind == PrototypeEventKind.Move &&
                e.to == new Vector2Int(2, 2)), Is.True);
        }

        [Test] public void IndependentNestAtOriginAcceptsChick()
        {
            scenario.nests.Add(Vector2Int.zero);
            scenario.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                position = new Vector2Int(0, 1) });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.States["chick"].cell, Is.EqualTo(Vector2Int.zero));
            Assert.That(session.States["chick"].active, Is.True);
            Assert.That(action.events.Single(e => e.eventName == "ChickReturned").nestId,
                Is.EqualTo("nest:0:0"));
        }

        [Test] public void SheepPushKeepsPlayerStillAndConsumesBothSheepAndGrass()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(2, 2), instanceId = "sheep" });
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "tall_grass", position = new Vector2Int(3, 2), instanceId = "grass" });
            var session = new PuzzleDeviceSession(map);
            var save = session.Copy();
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(action.events.Any(e => e.eventName == "SheepAteAndRemoved"), Is.True);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.RanchSheep, Is.Empty);
            Assert.That(session.RanchGrassPresent(new Vector2Int(3, 2)), Is.False);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.RanchGrassPresent(new Vector2Int(3, 2)), Is.True);
            Assert.That(session.Cost, Is.Zero);
            session.RestoreSnapshot(save);
            Assert.That(session.RanchSheep.ContainsKey("sheep"), Is.True);
        }

        [Test] public void SheepPushThenWindConsumesFirstGrassForOneCost()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(2, 2), instanceId = "sheep" });
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "tall_grass", position = new Vector2Int(4, 2), instanceId = "grass" });
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind,
                variant = "blower", position = new Vector2Int(0, 2), direction = Vector2Int.right,
                orientation = 2, active = true });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(action.events.Any(e => e.eventName == "SheepWindAteAndRemoved"), Is.True);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.RanchSheep, Is.Empty);
            Assert.That(session.RanchGrassPresent(new Vector2Int(4, 2)), Is.False);
            Assert.That(session.Cost, Is.EqualTo(1));
        }

        [Test] public void OrdinaryEntryIntoWetPondIsRejectedWithoutCharge()
        {
            map.start = new Vector2Int(3, 2);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", position = new Vector2Int(1, 2), instanceId = "pond" });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.left).accepted, Is.False);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.RanchWet(new Vector2Int(2, 2)), Is.True);
            Assert.That(session.Cost, Is.Zero);
        }

        [Test] public void WindCarriesSheepToLastActualCellAndMetalShadowsTheRest()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(2, 2), instanceId = "sheep" });
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind,
                variant = "blower", position = new Vector2Int(0, 2), direction = Vector2Int.right,
                orientation = 2, active = true });
            var open = new PuzzleDeviceSession(map);
            Assert.That(open.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(open.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(6, 2)));
            Assert.That(open.Cost, Is.EqualTo(1));

            scenario.devices.Add(new PrototypeDevice { id = "metal", role = PrototypeRole.MetalCrate,
                position = new Vector2Int(5, 2), direction = Vector2Int.right });
            var shadowed = new PuzzleDeviceSession(map);
            Assert.That(shadowed.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(shadowed.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(shadowed.Cost, Is.EqualTo(1));
        }
    }
}
