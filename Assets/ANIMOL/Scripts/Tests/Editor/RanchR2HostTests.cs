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

        [Test] public void SheepCannotBePushedIntoAnEmptyNest()
        {
            scenario.nests.Add(new Vector2Int(3, 2));
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep",
                instanceId = "sheep", position = new Vector2Int(2, 2) });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.False);
            Assert.That(session.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.NestPresent(new Vector2Int(3, 2)), Is.True);
            Assert.That(session.Cost, Is.Zero);
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

        [Test] public void ManualPushThenWindCarriesSheepBeyondLastWindCell()
        {
            map.Resize(8, 5);
            map.SetTerrain(new Vector2Int(7, 2), TerrainMaterial.Grass);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(2, 2), instanceId = "sheep" });
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind,
                variant = "blower", position = new Vector2Int(0, 2), direction = Vector2Int.right,
                orientation = 2, active = true });
            var open = new PuzzleDeviceSession(map);
            Assert.That(open.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(open.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(7, 2)));
            Assert.That(open.Cost, Is.EqualTo(1));

            scenario.devices.Add(new PrototypeDevice { id = "metal", role = PrototypeRole.MetalCrate,
                position = new Vector2Int(5, 2), direction = Vector2Int.right });
            var shadowed = new PuzzleDeviceSession(map);
            Assert.That(shadowed.Apply(Vector2Int.right).accepted, Is.True);
            Assert.That(shadowed.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(shadowed.Cost, Is.EqualTo(1));
        }

        static readonly Vector2Int[] Headings = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        // Open faces in the authored art: NW = south/east, NE = south/west,
        // SE = north/west, SW = north/east. These are travel headings INTO those faces.
        static readonly int[][] OpenEntries = { new[] { 0, 3 }, new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 } };

        [Test] public void PlayerWalksIntoOnlyTheOpenFacesOfEveryReflector(
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction;
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            bool allowed = OpenEntries[corner].Contains(heading);
            var result = session.Apply(direction);
            Assert.That(result.accepted, Is.EqualTo(allowed));
            Assert.That(session.Cell, Is.EqualTo(allowed ? reflector : map.start));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            if (allowed) Assert.That(session.Undo(), Is.True);
            else Assert.That(result.events.Any(e => e.kind == PrototypeEventKind.Move), Is.False);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void FourSidedTheaterMirrorsStillAllowEveryPlayerEntry(
            [Values("slash", "backslash")] string variant, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction;
            scenario.devices.Add(new PrototypeDevice { id = "mirror", role = PrototypeRole.Bounce, position = reflector, variant = variant });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(direction).accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(reflector));
            Assert.That(session.Cost, Is.EqualTo(1));
        }

        [Test] public void ConveyorCannotCarryPlayerThroughClosedReflectorFaces(
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction * 2;
            scenario.devices.Add(new PrototypeDevice { id = "belt", role = PrototypeRole.Conveyor, position = reflector - direction, direction = direction });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            var result = session.Apply(direction);
            bool allowed = OpenEntries[corner].Contains(heading);
            Assert.That(result.accepted, Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(result.events.Any(e => e.kind == PrototypeEventKind.Move && e.actor == "player" && e.to == reflector), Is.EqualTo(allowed));
            Assert.That(session.Cell, Is.EqualTo(allowed ? reflector + PuzzleDeviceSession.ReflectCorner(direction, corner) : reflector - direction));
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void WindCannotCarryPlayerThroughClosedReflectorFaces(
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction;
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind, variant = "blower",
                position = reflector - direction * 2, direction = direction, orientation = 1, active = true });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            var result = session.AdvanceWind();
            bool allowed = OpenEntries[corner].Contains(heading);
            Assert.That(result.accepted, Is.EqualTo(allowed));
            Assert.That(session.Cost, Is.Zero);
            Assert.That(result.events.Any(e => e.kind == PrototypeEventKind.Move && e.actor == "player" && e.to == reflector), Is.EqualTo(allowed));
            if (!allowed) Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [TestCase(true)] [TestCase(false)]
        public void LeapLandingRespectsTheReflectorFace(bool allowed)
        {
            scenario.devices.Add(new PrototypeDevice { id = "leap", role = PrototypeRole.CrescentLeap, position = new Vector2Int(2, 2), direction = Vector2Int.right });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = new Vector2Int(5, 2), orientation = allowed ? 2 : 0 });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.EqualTo(allowed));
            Assert.That(session.Cell, Is.EqualTo(allowed ? new Vector2Int(5, 2) : map.start));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            if (allowed) Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [TestCase(true, true)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(false, false)]
        public void PortalArrivalAndItsExitStepRespectReflectorFaces(bool reflectorAtArrival, bool allowed)
        {
            var reflector = new Vector2Int(5, 2);
            scenario.devices.Add(new PrototypeDevice { id = "portal", role = PrototypeRole.Portal, position = new Vector2Int(2, 2), target = reflectorAtArrival ? reflector : reflector - Vector2Int.right });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = allowed ? 2 : 0 });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            var result = session.Apply(Vector2Int.right);
            Assert.That(result.accepted, Is.EqualTo(allowed));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            Assert.That(session.PortalCuts, Is.EqualTo(allowed ? 1 : 0));
            Assert.That(result.events.Any(e => e.kind == PrototypeEventKind.Move && e.actor == "player" && e.to == reflector), Is.EqualTo(allowed));
            if (allowed) Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [TestCase(true)] [TestCase(false)]
        public void GateRejectsAnExitThroughTheClosedReflectorFace(bool allowed)
        {
            map.start = new Vector2Int(2, 2);
            scenario.devices.Add(new PrototypeDevice { id = "gate", role = PrototypeRole.OneWayGate, position = new Vector2Int(3, 2), direction = Vector2Int.right });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = new Vector2Int(4, 2), orientation = allowed ? 2 : 0 });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.EqualTo(allowed));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            if (allowed) Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void SheepPushUsesOnlyTheOpenFacesOfEveryReflector(
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction * 2;
            var original = reflector - direction;
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = original, instanceId = "sheep" });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            var session = new PuzzleDeviceSession(map);
            var before = session.SearchStateKey();
            bool allowed = OpenEntries[corner].Contains(heading);
            var result = session.Apply(direction);
            Assert.That(result.accepted, Is.EqualTo(allowed), result.reason);
            Assert.That(session.RanchSheep["sheep"], Is.EqualTo(allowed ? reflector : original));
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            if (allowed) Assert.That(session.Undo(), Is.True);
            else Assert.That(result.events.Any(e => e.kind == PrototypeEventKind.Move), Is.False);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
            Assert.That(session.Cost, Is.Zero);
        }

        [Test] public void CratesUseTheSameOpenReflectorFaces(
            [Values(PrototypeRole.WoodCrate, PrototypeRole.MetalCrate)] PrototypeRole role,
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            var reflector = new Vector2Int(3, 2); var direction = Headings[heading];
            map.start = reflector - direction * 2;
            var original = reflector - direction;
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            scenario.devices.Add(new PrototypeDevice { id = "box", role = role, position = original });
            var session = new PuzzleDeviceSession(map); var before = session.SearchStateKey();
            bool allowed = OpenEntries[corner].Contains(heading);
            Assert.That(session.Apply(direction).accepted, Is.EqualTo(allowed));
            Assert.That(session.States["box"].cell, Is.EqualTo(allowed ? reflector : original));
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.Cost, Is.EqualTo(allowed ? 1 : 0));
            if (allowed) Assert.That(session.Undo(), Is.True);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void WindCarriesSheepAndWoodThroughOpenReflectorFacesAndTurns(
            [Values(true, false)] bool sheep,
            [Values(0, 1, 2, 3)] int corner, [Values(0, 1, 2, 3)] int heading)
        {
            map.Resize(9, 9);
            for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = Vector2Int.zero; map.exit = new Vector2Int(8, 8);
            var reflector = new Vector2Int(4, 4); var direction = Headings[heading];
            var original = reflector - direction * 2;
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind, variant = "blower",
                position = reflector - direction * 3, direction = direction, orientation = 2, active = true });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = reflector, orientation = corner });
            if (sheep) scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = original, instanceId = "body" });
            else scenario.devices.Add(new PrototypeDevice { id = "body", role = PrototypeRole.WoodCrate, position = original });
            var session = new PuzzleDeviceSession(map);
            var before = session.SearchStateKey();
            var snapshot = session.Copy();
            var result = session.AdvanceWind();
            Assert.That(result.accepted, Is.True);
            bool allowed = OpenEntries[corner].Contains(heading);
            var moves = result.events.Where(e => e.kind == PrototypeEventKind.Move && e.actor == "body").ToArray();
            Assert.That(moves.Any(e => e.to == reflector), Is.EqualTo(allowed));
            if (allowed)
                Assert.That(moves.Any(e => e.from == reflector && e.to != reflector + direction), Is.True,
                    "Wind must turn the body at the reflector rather than carry it straight through.");
            else
                Assert.That(sheep ? session.RanchSheep["body"] : session.States["body"].cell, Is.EqualTo(reflector - direction));
            Assert.That(session.Cost, Is.Zero);
            session.RestoreSnapshot(snapshot);
            Assert.That(session.SearchStateKey(), Is.EqualTo(before));
        }

        [Test] public void PlayerAndWindStillPassThroughAndTurnAtReflector()
        {
            map.start = new Vector2Int(2, 2);
            scenario.devices.Add(new PrototypeDevice { id = "wind", role = PrototypeRole.Wind, variant = "blower",
                position = new Vector2Int(0, 2), direction = Vector2Int.right, orientation = 2, active = true });
            scenario.devices.Add(new PrototypeDevice { id = "reflector", role = PrototypeRole.Bounce, position = new Vector2Int(4, 2), orientation = 2 });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.WindCells[new Vector2Int(4, 2)], Is.EqualTo(Vector2Int.up));
            Assert.That(session.AdvanceWind().accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(session.Cost, Is.Zero);
        }
    }
}
