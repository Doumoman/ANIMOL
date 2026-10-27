#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ANIMOL.Art.RanchDevicesV13;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class RanchR2PlayTests
    {
        GameObject camera, host;
        PuzzleMap map;
        PuzzleScenario scenario;
        PuzzlePrototypeController controller;

        [UnitySetUp] public IEnumerator Setup()
        {
            camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
            host = new GameObject("R2 test controller");
            controller = host.AddComponent<PuzzlePrototypeController>();
            controller.secondsPerCell = .01f;
            map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(7, 5);
            for (var y = 0; y < 5; y++) for (var x = 0; x < 7; x++)
                map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.start = new Vector2Int(1, 2); map.exit = new Vector2Int(6, 4); map.targetBubbles = 0;
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            scenario = ScriptableObject.CreateInstance<PuzzleScenario>(); scenario.themeId = "starway";
            map.scenario = scenario;
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(host); Object.Destroy(camera);
            Object.Destroy(map); Object.Destroy(scenario);
            yield return null;
        }

        IEnumerator WaitIdle()
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.IsMoving, Is.False);
        }

        [UnityTest] public IEnumerator PondUsesOnePairVisualAndCrossingRestoresOnUndo()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", position = map.start, instanceId = "pond" });
            controller.Load(map); yield return null;
            var pair = controller.GetComponentInChildren<RanchPondSluiceVisualR3>();
            Assert.That(pair, Is.Not.Null);
            Assert.That(pair.GetComponentsInChildren<RanchDeviceVisualV13>().Length, Is.EqualTo(1));
            Assert.That(pair.GetComponentsInChildren<RanchDeviceVisualV13>().Select(v => v.CurrentKey),
                Is.EquivalentTo(new[] { "pond_pair/horizontal/east_wet" }));
            Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
            yield return new WaitForSeconds(.1f);
            Assert.That(controller.IsMoving, Is.True);
            Assert.That(pair.GetComponentsInChildren<RanchDeviceVisualV13>().Select(v => v.CurrentKey),
                Is.EquivalentTo(new[] { "pond_pair/horizontal/water_west" }));
            yield return WaitIdle();
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(controller.Session.Cost, Is.EqualTo(1));
            Assert.That(pair.GetComponentsInChildren<RanchDeviceVisualV13>().Select(v => v.CurrentKey),
                Is.EquivalentTo(new[] { "pond_pair/horizontal/west_wet" }));
            Assert.That(controller.UndoLast(), Is.True);
            Assert.That(controller.Session.Cell, Is.EqualTo(map.start));
            Assert.That(pair.GetComponentsInChildren<RanchDeviceVisualV13>().Select(v => v.CurrentKey),
                Is.EquivalentTo(new[] { "pond_pair/horizontal/east_wet" }));
        }

        [UnityTest] public IEnumerator VerticalPondTransfersWaterSouthWhilePlayerTravelsNorth()
        {
            map.start = new Vector2Int(2, 2);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sluice", position = map.start,
                direction = Vector2Int.up, instanceId = "vertical_pond" });
            controller.Load(map); yield return null;
            var pair = controller.GetComponentInChildren<RanchPondSluiceVisualR3>();
            var visual = pair.GetComponentInChildren<RanchDeviceVisualV13>();
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
            Assert.That(pair.transform.localPosition, Is.EqualTo(new Vector3(2, 2.5f, 0)));
            Assert.That(controller.RequestMove(Vector2Int.up), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/water_south"));
            Assert.That(controller.Player.position.y, Is.GreaterThan(2f).And.LessThan(3f));
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(controller.Session.RanchWet(new Vector2Int(2, 2)), Is.True);
            yield return WaitIdle();
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/south_wet"));
            Assert.That(controller.RequestMove(Vector2Int.down), Is.True);
            yield return WaitIdle();
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(controller.Session.Cost, Is.EqualTo(2));
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
            Assert.That(controller.UndoLast(), Is.True);
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/south_wet"));
            controller.ResetStage();
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
        }

        [UnityTest] public IEnumerator SheepAndGrassDisappearThenUndoRestoresVisuals()
        {
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(2, 2), instanceId = "sheep" });
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "tall_grass", position = new Vector2Int(3, 2), instanceId = "grass" });
            controller.Load(map); yield return null;
            Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
            yield return WaitIdle();
            Assert.That(controller.Session.Cell, Is.EqualTo(map.start));
            Assert.That(controller.Session.RanchSheep, Is.Empty);
            Assert.That(controller.Session.RanchGrassPresent(new Vector2Int(3, 2)), Is.False);
            Assert.That(controller.Session.Cost, Is.EqualTo(1));
            Assert.That(controller.GetComponentsInChildren<RanchSheepConsumptionVisualR2>()
                .Count(effect => effect.IsPlaying), Is.EqualTo(2));
            Assert.That(controller.UndoLast(), Is.True);
            Assert.That(controller.Session.RanchSheep["sheep"], Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(controller.Session.RanchGrassPresent(new Vector2Int(3, 2)), Is.True);
            Assert.That(controller.GetComponentsInChildren<RanchDeviceVisualV13>()
                .Any(v => v.CurrentKey == "sheep/east/idle"), Is.True);
            Assert.That(controller.GetComponentsInChildren<RanchDeviceVisualV13>()
                .Any(v => v.CurrentKey == "tall_grass/idle"), Is.True);
        }

        [UnityTest] public IEnumerator PaletteWaterRendersFromExistingWaterCellsAndRestoresAfterReload()
        {
            scenario.water.Add(new Vector2Int(5, 1));
            controller.Load(map); yield return null;
            var water = controller.GetComponentInChildren<RanchWaterCellVisualV13>();
            Assert.That(water, Is.Not.Null);
            Assert.That(water.GetComponentsInChildren<RanchDeviceVisualV13>()
                .Single().CurrentKey, Is.EqualTo("water/wet"));
            controller.Load(map); yield return null;
            Assert.That(controller.GetComponentsInChildren<RanchWaterCellVisualV13>().Length, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PaletteNestUsesV13ArtAndKeepsTheChickOnV12()
        {
            var template = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device24_Chick_Scenario.asset")
                .devices.Single(d => d.role == PrototypeRole.Chick);
            scenario.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                position = new Vector2Int(3, 3), target = new Vector2Int(4, 3),
                direction = Vector2Int.right, number = 1, visualPrefab = template.visualPrefab,
                initialVisual = template.initialVisual });
            controller.Load(map); yield return null;
            var nest = controller.GetComponentsInChildren<RanchDeviceVisualV13>()
                .Single(v => v.CurrentKey == "nest/idle");
            Assert.That(controller.Devices.Actor("chick").GetComponentInChildren<RanchDeviceVisualV13>(), Is.Null);
            Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
            yield return WaitIdle();
            Assert.That(controller.Session.States["chick"].active, Is.True);
            Assert.That(nest.CurrentKey, Is.EqualTo("nest/settled"));
        }

        [UnityTest] public IEnumerator SavedQaMapCrossesPondThenConsumesSheepForThreeInputs()
        {
            var qa = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/StarwayRanchDeviceQA.asset");
            Assert.That(qa, Is.Not.Null);
            controller.Load(qa); yield return null;
            for (var i = 0; i < 3; i++)
            {
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                yield return WaitIdle();
            }
            Assert.That(controller.Session.Cost, Is.EqualTo(3));
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(controller.Session.RanchPonds["qa_pond"], Is.True);
            Assert.That(controller.Session.RanchSheep, Is.Empty);
            Assert.That(controller.Session.RanchGrassPresent(new Vector2Int(5, 2)), Is.False);
            controller.ResetStage();
            Assert.That(controller.Session.Cost, Is.Zero);
            Assert.That(controller.Session.RanchPonds["qa_pond"], Is.False);
            Assert.That(controller.Session.RanchSheep.ContainsKey("qa_sheep"), Is.True);
            Assert.That(controller.Session.RanchGrassPresent(new Vector2Int(5, 2)), Is.True);
        }

        [UnityTest] public IEnumerator SavedVerticalQaMapCrossesBothWaysAndResetsWaterPhase()
        {
            var qa = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/StarwayRanchVerticalQA.asset");
            Assert.That(qa, Is.Not.Null);
            Assert.That(qa.ValidateMap(), Is.Empty);
            controller.Load(qa); yield return null;
            var pair = controller.GetComponentInChildren<RanchPondSluiceVisualR3>();
            var visual = pair.GetComponentInChildren<RanchDeviceVisualV13>();
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
            Assert.That(controller.RequestMove(Vector2Int.up), Is.True);
            yield return WaitIdle();
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(controller.Session.RanchWet(new Vector2Int(2, 2)), Is.True);
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/south_wet"));
            Assert.That(controller.RequestMove(Vector2Int.down), Is.True);
            yield return WaitIdle();
            Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
            Assert.That(controller.Session.Cost, Is.EqualTo(2));
            controller.ResetStage();
            Assert.That(controller.Session.Cost, Is.Zero);
            Assert.That(visual.CurrentKey, Is.EqualTo("pond_pair/vertical/north_wet"));
        }

        [UnityTest] public IEnumerator VerticalPondReenableRestoreAndReloadUseTheCommittedPhase()
        {
            var qa = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/StarwayRanchVerticalQA.asset");
            controller.Load(qa); yield return null;
            Assert.That(controller.RequestMove(Vector2Int.up), Is.True);
            yield return WaitIdle();
            var saved = controller.Session.Copy();
            var view = controller.GetComponentInChildren<PuzzleMapView>();
            view.gameObject.SetActive(false);
            view.gameObject.SetActive(true);
            yield return null;
            Assert.That(controller.GetComponentInChildren<RanchPondSluiceVisualR3>()
                .GetComponentInChildren<RanchDeviceVisualV13>().CurrentKey,
                Is.EqualTo("pond_pair/vertical/south_wet"));
            controller.ResetStage();
            Assert.That(controller.GetComponentInChildren<RanchPondSluiceVisualR3>()
                .GetComponentInChildren<RanchDeviceVisualV13>().CurrentKey,
                Is.EqualTo("pond_pair/vertical/north_wet"));
            controller.RestoreSnapshot(saved);
            Assert.That(controller.GetComponentInChildren<RanchPondSluiceVisualR3>()
                .GetComponentInChildren<RanchDeviceVisualV13>().CurrentKey,
                Is.EqualTo("pond_pair/vertical/south_wet"));
            controller.Load(qa); yield return null;
            Assert.That(controller.GetComponentInChildren<RanchPondSluiceVisualR3>()
                .GetComponentInChildren<RanchDeviceVisualV13>().CurrentKey,
                Is.EqualTo("pond_pair/vertical/north_wet"));
        }

        [UnityTest] public IEnumerator SheepSortingUsesGroundContactAboveAndBelowThePlayer()
        {
            map.start = new Vector2Int(1, 3);
            scenario.ranchV13Art.Add(new RanchV13ArtPlacement { family = "sheep", position = new Vector2Int(3, 2), instanceId = "sheep" });
            controller.Load(map); yield return null;
            var sheep = controller.GetComponentsInChildren<RanchDeviceVisualV13>()
                .Single(v => v.CurrentKey == "sheep/east/idle");
            Assert.That(sheep.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(41));
            Assert.That(sheep.transform.parent.localScale, Is.EqualTo(Vector3.one));
            Assert.That(Camera.main.orthographic, Is.True);
            for (var i = 0; i < 2; i++)
            { Assert.That(controller.RequestMove(Vector2Int.down), Is.True); yield return WaitIdle(); }
            Assert.That(sheep.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(39));
        }
    }
}
#endif
