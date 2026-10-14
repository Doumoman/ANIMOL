using System.Collections;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class PuzzlePlayTests
    {
        PuzzleController controller;
        PuzzleMap map;
        GameObject cameraObject;
        [UnitySetUp] public IEnumerator Setup()
        {
            map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(40, 12); map.start = new Vector2Int(1, 5); map.exit = new Vector2Int(38, 8); map.targetBubbles = 1;
            map.theme = ScriptableObject.CreateInstance<PuzzleTheme>(); map.theme.terrain = new TerrainArt[0];
            map.bubbles.Add(new Vector2Int(39, 5));
            for (var y = 0; y < 12; y++) for (var x = 0; x < 40; x++) map.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass));
            cameraObject = new GameObject("Test Camera", typeof(Camera)); cameraObject.tag = "MainCamera"; cameraObject.GetComponent<Camera>().aspect = 9f / 16;
            controller = new GameObject("Test Puzzle").AddComponent<PuzzleController>(); controller.secondsPerCell = .12f;
            controller.Load(map); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(controller.gameObject); Object.Destroy(cameraObject); Object.Destroy(map.theme); Object.Destroy(map); yield return null; }
        [UnityTest] public IEnumerator CameraFollowsActualInterpolationAndAutomaticChain()
        {
            map.start = new Vector2Int(10, 5); map.SetDevice(new DeviceCell { position = new Vector2Int(11, 5), kind = DeviceKind.Conveyor, direction = Vector2Int.right });
            map.SetDevice(new DeviceCell { position = new Vector2Int(12, 5), kind = DeviceKind.Wind, direction = Vector2Int.right }); controller.Load(map);
            var original = controller.FollowCamera.transform.position.x; controller.RequestMove(Vector2Int.right); yield return null;
            Assert.Less(controller.Player.position.x, 11.01f); Assert.Less(controller.FollowCamera.transform.position.x, 11.01f);
            yield return WaitIdle(); Assert.AreEqual(new Vector2Int(13, 5), controller.Session.Cell); Assert.AreEqual(1, controller.Session.Moves);
            yield return new WaitForSeconds(.3f); Assert.Greater(controller.FollowCamera.transform.position.x, original + 2.5f); Assert.AreEqual(5.5f, cameraObject.GetComponent<Camera>().orthographicSize);
        }
        [UnityTest] public IEnumerator PortalCutsAtExitAndContinuesSlideWithoutTraversingMiddle()
        {
            map.SetDevice(new DeviceCell { position = new Vector2Int(2, 5), kind = DeviceKind.Ice });
            map.SetDevice(new DeviceCell { position = new Vector2Int(3, 5), kind = DeviceKind.Portal, portalTarget = new Vector2Int(38, 5) });
            map.SetDevice(new DeviceCell { position = new Vector2Int(38, 5), kind = DeviceKind.Portal, portalTarget = new Vector2Int(3, 5) });
            controller.Load(map);
            var cutCell = Vector2Int.zero; var cutCamera = 0f; controller.PortalCut += () => { cutCell = controller.Session.Cell; cutCamera = controller.FollowCamera.transform.position.x; };
            controller.RequestMove(Vector2Int.right);
            var timeout = Time.realtimeSinceStartup + 3;
            while (controller.IsMoving && Time.realtimeSinceStartup < timeout)
            { Assert.IsFalse(controller.FollowCamera.transform.position.x > 8 && controller.FollowCamera.transform.position.x < 32); yield return null; }
            Assert.IsFalse(controller.IsMoving); Assert.AreEqual(1, controller.PortalCuts); Assert.AreEqual(new Vector2Int(38, 5), cutCell);
            Assert.AreEqual(39.5f - 5.5f * 9f / 16, cutCamera, .01f); Assert.AreEqual(new Vector2Int(39, 5), controller.Session.Cell); Assert.AreEqual(1, controller.Session.Collected);
            Assert.AreEqual(Vector2Int.right, controller.Session.Direction); Assert.IsFalse(controller.BlocksInput);
        }
        [UnityTest] public IEnumerator RuntimeTerrainRefreshesNineCellsAndNeverChangesLogicalFlags()
        {
            var p = new Vector2Int(3, 5); var before = controller.View.RefreshedCells;
            controller.RuntimeMap.SetTerrain(p, TerrainMaterial.Stone, true);
            Assert.AreEqual(9, controller.View.RefreshedCells - before); Assert.IsFalse(controller.RuntimeMap.Walkable(p));
            Assert.IsTrue(map.Walkable(p)); Assert.AreEqual(TerrainMaterial.Grass, map.terrain.Find(t => t.position == p).material);
            controller.View.RefreshNeighborhood(p); Assert.IsFalse(controller.RuntimeMap.Walkable(p));
            controller.RuntimeMap.Resize(4, 4); Assert.AreEqual(480, controller.RuntimeMap.terrain.Count); yield return null;
            Assert.AreEqual(new Vector2(1.5f, 1.5f), (Vector2)controller.FollowCamera.transform.position);
        }
        [UnityTest] public IEnumerator AutomaticCycleStopsAndBufferedInputRemainsUsable()
        {
            map.SetDevice(new DeviceCell { position = new Vector2Int(2, 5), kind = DeviceKind.Conveyor, direction = Vector2Int.right });
            map.SetDevice(new DeviceCell { position = new Vector2Int(3, 5), kind = DeviceKind.Conveyor, direction = Vector2Int.left }); controller.Load(map);
            controller.RequestMove(Vector2Int.right); yield return WaitIdle(); Assert.AreEqual("Automatic loop stopped", controller.Status);
            Assert.IsTrue(controller.RequestMove(Vector2Int.up)); Assert.IsTrue(controller.RequestMove(Vector2Int.up)); yield return WaitIdle();
            Assert.AreEqual(7, controller.Session.Cell.y); Assert.AreEqual(3, controller.Session.Moves);
        }
        [UnityTest] public IEnumerator MapEdgePlayerRemainsInsideGameplayViewportBelowOriginalHud()
        {
            map.start = new Vector2Int(1, 11); controller.Load(map);
            var lens = cameraObject.GetComponent<Camera>(); lens.ResetAspect(); controller.FollowCamera.Cut(); yield return null;
            var point = lens.WorldToScreenPoint(controller.Player.position);
            Assert.GreaterOrEqual(point.y, lens.pixelRect.y); Assert.LessOrEqual(point.y, lens.pixelRect.yMax);
            Assert.Less(point.y, Screen.safeArea.yMax - 160 * Mathf.Min(Screen.width / 1080f, Screen.height / 1920f));
            Assert.AreEqual(5.5f, lens.orthographicSize);
        }
        [UnityTest] public IEnumerator SwipeMovesOneCellAndIgnoresUiAndShortGestures()
        {
            var input = controller.GetComponent<PuzzleSwipeInput>(); var start = new Vector2(300, 400);
            input.BeginGesture(start, true); Assert.IsFalse(input.EndGesture(start + Vector2.right * 200));
            input.BeginGesture(start, false); Assert.IsFalse(input.EndGesture(start + Vector2.right * 5)); Assert.AreEqual(0, controller.Session.Moves);
            input.BeginGesture(start, false); Assert.IsTrue(input.EndGesture(start + Vector2.right * 200)); Assert.IsFalse(input.EndGesture(start + Vector2.right * 400));
            yield return WaitIdle(); Assert.AreEqual(new Vector2Int(2, 5), controller.Session.Cell); Assert.AreEqual(1, controller.Session.Moves);
        }
        IEnumerator WaitIdle() { var end = Time.realtimeSinceStartup + 4; while (controller.IsMoving && Time.realtimeSinceStartup < end) yield return null; Assert.IsFalse(controller.IsMoving, "Movement failed to terminate"); }
    }
}
