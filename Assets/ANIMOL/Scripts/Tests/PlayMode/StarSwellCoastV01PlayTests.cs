using System.Collections;
using ANIMOL.Art.StarSwellCoastV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarSwellCoastV01PlayTests
    {
        [UnityTest]
        public IEnumerator ActualCoastGateStaysOpenUnderActorAndClosesAfterDeparture()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_twilight_sea_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.NotNull(controller);
            controller.secondsPerCell = .005f;
            var gateCell = controller.Session.States["2_gate"].cell;
            var gateDirection = controller.Session.States["2_gate"].direction;
            var entryCell = gateCell - gateDirection;
            var exitCell = gateCell + gateDirection;
            var route = controller.RuntimeMap.scenario.verificationRoute;
            int step = 0;
            var passed = false;
            for (; step < route.Length && !passed; step++)
            {
                var before = controller.Session.Cell;
                Assert.IsTrue(controller.RequestMove(Direction(route[step])), "route " + step);
                while (controller.IsMoving) yield return null;
                if (before == entryCell)
                {
                    Assert.AreEqual(exitCell, controller.Session.Cell,
                        "A one-way gate traversal must finish beyond its occupied cell.");
                    passed = true;
                }
            }
            Assert.IsTrue(passed, "The verification route never reached the gate entry cell.");
            yield return new WaitForSeconds(.2f);
            var gate = controller.Devices.Actor("2_gate").GetComponent<StarSwellCoastV01AnimatedVisual>();
            Assert.AreEqual("oneway_gate.east.closed", gate.CurrentKey);
            Assert.AreEqual(0, controller.Devices.MissingBindings);
        }

        static Vector2Int Direction(char value) => value == 'N' ? Vector2Int.up :
            value == 'E' ? Vector2Int.right : value == 'S' ? Vector2Int.down : Vector2Int.left;

        [UnityTest]
        public IEnumerator ActualTwilightSeaUsesLatestCatalogAndCommitsSpikeOnlyAfterLeaving()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_twilight_sea_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.NotNull(controller);
            Assert.NotNull(controller.Devices);
            Assert.AreEqual(0, controller.Devices.MissingBindings);
            Assert.AreEqual("twilight_sea", controller.Session.Rules.themeId);
            Assert.AreEqual(1, Object.FindObjectsByType<StarSwellCoastV01MapRenderer>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<StarSwellCoastV01OceanField>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0, Object.FindObjectsByType<TwilightCoastRenderer>(FindObjectsSortMode.None).Length);
            var waterMasks = Object.FindFirstObjectByType<StarSwellCoastV01MapRenderer>()
                .GetComponentsInChildren<SpriteMask>();
            Assert.AreEqual(controller.Session.Rules.water.Count, waterMasks.Length);
            foreach (var wave in Object.FindFirstObjectByType<StarSwellCoastV01MapRenderer>()
                .GetComponentsInChildren<StarSwellCoastV01AnimatedVisual>())
            {
                Assert.AreEqual("water_surface.none.ambient_wave", wave.CurrentKey);
                Assert.AreEqual(SpriteMaskInteraction.VisibleInsideMask,
                    wave.GetComponent<SpriteRenderer>().maskInteraction);
            }
            foreach (var device in controller.Session.Devices)
            {
                if (!StarSwellCoastV01PuzzleBinding.Covers(device)) continue;
                var root = controller.Devices.Actor(device.id);
                Assert.NotNull(root, device.id);
                var visual = root.GetComponent<StarSwellCoastV01AnimatedVisual>();
                Assert.NotNull(visual, device.id);
                Assert.AreEqual(StarSwellCoastV01PuzzleBinding.IdleClip(device,
                    controller.Session.States[device.id]), visual.CurrentKey, device.id);
                Assert.IsNull(root.GetComponent<DeviceVisualView>(), device.id);
            }
            Assert.AreEqual(0, controller.Session.States["0_spikes"].phase);
            Assert.IsTrue(controller.RequestMove(Vector2Int.right));
            while (controller.IsMoving) yield return null;
            Assert.AreEqual(1, controller.Session.States["0_spikes"].phase);
            Assert.AreEqual(1, controller.Session.Cost);
            Assert.IsTrue(controller.RequestMove(Vector2Int.left));
            while (controller.IsMoving) yield return null;
            Assert.AreEqual(2, controller.Session.States["0_spikes"].phase);
            Assert.AreEqual(2, controller.Session.Cost);
            Assert.IsFalse(controller.RequestMove(Vector2Int.right));
            Assert.AreEqual(2, controller.Session.Cost);
            Assert.AreEqual(2, controller.Session.States["0_spikes"].phase);
        }
    }
}
