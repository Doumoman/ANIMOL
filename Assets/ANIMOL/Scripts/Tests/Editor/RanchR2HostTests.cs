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
