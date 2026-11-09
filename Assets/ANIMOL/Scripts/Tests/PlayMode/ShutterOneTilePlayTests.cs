using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class ShutterOneTilePlayTests
    {
        [UnityTest] public IEnumerator OppositeInitialShuttersKeepDifferentTimingInGameplay()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device04_Shutter.asset");
            foreach (var vertical in new[] { false, true })
            {
                var map = Object.Instantiate(source); map.scenario = Object.Instantiate(source.scenario);
                map.scenario.costLimit = 0; map.scenario.goals.Clear(); map.bubbles.Clear(); map.targetBubbles = 0;
                map.start = new Vector2Int(1, 1); map.SetTerrain(map.start, TerrainMaterial.Stone);
                map.SetTerrain(new Vector2Int(2, 1), TerrainMaterial.Stone);
                var closed = map.scenario.devices.Single(d => d.role == PrototypeRole.Shutter);
                closed.active = false; closed.orientation = 1;
                closed.direction = vertical ? Vector2Int.up : Vector2Int.right;
                var open = JsonUtility.FromJson<PrototypeDevice>(JsonUtility.ToJson(closed));
                open.id = "opposite_shutter"; open.active = true; open.position = new Vector2Int(5, 2);
                map.SetTerrain(open.position, TerrainMaterial.Stone); map.scenario.devices.Add(open); map.InvalidateIndex();
                var host = new GameObject("Opposite initial shutters gameplay test");
                var controller = host.AddComponent<PuzzlePrototypeController>();
                try
                {
                    controller.Load(map);
                    var closedRenderer = controller.Devices.Actor(closed.id).GetComponent<DeviceVisualView>().visual;
                    var openRenderer = controller.Devices.Actor(open.id).GetComponent<DeviceVisualView>().visual;
                    var frames = vertical ? map.theme.oneTileShutterArt.vertical : map.theme.oneTileShutterArt.horizontal;
                    for (var turn = 0; turn <= 6; turn++)
                    {
                        Assert.That(controller.Session.States[closed.id].active, Is.EqualTo(turn % 2 == 1));
                        Assert.That(controller.Session.States[open.id].active, Is.EqualTo(turn % 2 == 0));
                        Assert.That(closedRenderer.sprite, Is.SameAs(turn % 2 == 1 ? frames[3] : frames[0]));
                        Assert.That(openRenderer.sprite, Is.SameAs(turn % 2 == 0 ? frames[3] : frames[0]));
                        if (turn == 6) break;
                        Assert.That(controller.RequestMove(turn % 2 == 0 ? Vector2Int.right : Vector2Int.left), Is.True);
                        var deadline = Time.realtimeSinceStartup + 5f;
                        while (controller.IsMoving && Time.realtimeSinceStartup < deadline)
                        {
                            AssertOneCell(closedRenderer, closed.position); AssertOneCell(openRenderer, open.position);
                            yield return null;
                        }
                        Assert.That(controller.IsMoving, Is.False);
                    }
                    Assert.That(controller.Session.Cost, Is.EqualTo(6));
                    Assert.That(controller.UndoLast(), Is.True);
                    Assert.That(controller.Session.States[closed.id].active, Is.True);
                    Assert.That(controller.Session.States[open.id].active, Is.False);
                    controller.ResetStage();
                    Assert.That(closedRenderer.sprite, Is.SameAs(frames[0])); Assert.That(openRenderer.sprite, Is.SameAs(frames[3]));
                    controller.Load(map);
                    Assert.That(controller.Session.States[closed.id].active, Is.False);
                    Assert.That(controller.Session.States[open.id].active, Is.True);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero);
                    Assert.That(controller.BlocksInput, Is.False);
                }
                finally { Object.Destroy(host); Object.Destroy(map.scenario); Object.Destroy(map); }
            }
        }
        static void AssertOneCell(SpriteRenderer renderer, Vector2 cell)
        {
            Assert.That(renderer.bounds.center.x, Is.EqualTo(cell.x).Within(.001f));
            Assert.That(renderer.bounds.center.y, Is.EqualTo(cell.y).Within(.001f));
            Assert.That(renderer.bounds.size.x, Is.LessThanOrEqualTo(1.001f));
            Assert.That(renderer.bounds.size.y, Is.LessThanOrEqualTo(1.001f));
        }

        [UnityTest] public IEnumerator AuthoredTheaterStageUsesTheOneTileShutterRenderer()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-16_MapData.asset");
            var shutter = map.scenario.devices.Single(d => d.role == PrototypeRole.Shutter);
            var sourceRenderer = shutter.visualPrefab.GetComponent<DeviceVisualView>().visual;
            var host = new GameObject("ET-16 shutter binding QA");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                yield return null;
                var root = controller.Devices.Actor(shutter.id);
                var oldWriter = root.GetComponent<DeviceVisualView>();
                var renderer = oldWriter.visual;
                var frames = map.theme.oneTileShutterArt.horizontal;
                Assert.That(oldWriter.enabled, Is.False);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(1));
                Assert.That(renderer.sprite, Is.SameAs(controller.Session.States[shutter.id].active ? frames[3] : frames[0]));
                Assert.That(renderer.sortingLayerID, Is.EqualTo(sourceRenderer.sortingLayerID));
                Assert.That(renderer.sortingOrder, Is.EqualTo(30));
                Assert.That(renderer.sharedMaterial, Is.SameAs(sourceRenderer.sharedMaterial));
                Assert.That(renderer.gameObject.layer, Is.EqualTo(sourceRenderer.gameObject.layer));
                AssertOneCell(renderer, shutter.position);
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator HorizontalAndVerticalShuttersStayInOneTileThroughTurnsAndRestore()
        {
            Assert.That(Screen.height, Is.GreaterThan(Screen.width), "Run shutter Game View QA in portrait.");
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device04_Shutter.asset");
            Assert.That(source, Is.Not.Null);
            var art = source.theme.oneTileShutterArt;
            Assert.That(art, Is.Not.Null);
            var captureDirectory = Path.Combine(Application.temporaryCachePath, "animol_shutter_one_tile_playmode");
            Directory.CreateDirectory(captureDirectory);
            var host = new GameObject("One tile shutter Play Mode QA");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                foreach (var vertical in new[] { false, true })
                {
                    var map = Object.Instantiate(source);
                    map.scenario = Object.Instantiate(source.scenario);
                    map.scenario.devices.Single(d => d.role == PrototypeRole.Shutter).direction =
                        vertical ? Vector2Int.up : Vector2Int.right;
                    map.InvalidateIndex();
                    controller.Load(map);
                    var label = vertical ? "vertical" : "horizontal";
                    var frames = vertical ? art.vertical : art.horizontal;
                    var root = controller.Devices.Actor("shutter");
                    var oldWriter = root.GetComponent<DeviceVisualView>();
                    var renderer = oldWriter.visual;
                    Assert.That(oldWriter.enabled, Is.False);
                    Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(1));
                    Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                    AssertOneCell(renderer, new Vector2(3, 2));
                    ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, label + "_closed.png"));

                    Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                    var sawOpeningMiddle = false;
                    while (controller.IsMoving)
                    {
                        if (renderer.sprite == frames[1] || renderer.sprite == frames[2])
                            sawOpeningMiddle = true;
                        AssertOneCell(renderer, new Vector2(3, 2));
                        yield return null;
                    }
                    Assert.That(sawOpeningMiddle, Is.True, label + " opening never displayed middle art");
                    Assert.That(controller.Session.States["shutter"].active, Is.True);
                    Assert.That(renderer.sprite, Is.SameAs(frames[3]));
                    ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, label + "_open.png"));

                    Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                    while (controller.IsMoving) yield return null;
                    Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(3, 2)));
                    Assert.That(controller.Session.States["shutter"].active, Is.True);
                    Assert.That(controller.Session.States["shutter"].phase, Is.EqualTo(3));
                    Assert.That(renderer.sprite, Is.SameAs(frames[3]));
                    Assert.That(controller.UndoLast(), Is.True);
                    Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(2, 2)));
                    Assert.That(renderer.sprite, Is.SameAs(frames[3]));

                    Assert.That(controller.RequestMove(Vector2Int.left), Is.True);
                    var sawClosingMiddle = false;
                    while (controller.IsMoving)
                    {
                        if (renderer.sprite == frames[1] || renderer.sprite == frames[2])
                        {
                            sawClosingMiddle = true;
                            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, label + "_middle.png"));
                        }
                        AssertOneCell(renderer, new Vector2(3, 2));
                        yield return null;
                    }
                    Assert.That(sawClosingMiddle, Is.True, label + " closing never displayed middle art");
                    Assert.That(controller.Session.States["shutter"].active, Is.False);
                    Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                    ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, label + "_closed_again.png"));

                    controller.ResetStage();
                    Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                    AssertOneCell(renderer, new Vector2(3, 2));
                    controller.Load(map);
                    renderer = controller.Devices.Actor("shutter").GetComponent<DeviceVisualView>().visual;
                    Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                    AssertOneCell(renderer, new Vector2(3, 2));
                    Object.Destroy(map.scenario);
                    Object.Destroy(map);
                }
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); }
        }
    }
}
