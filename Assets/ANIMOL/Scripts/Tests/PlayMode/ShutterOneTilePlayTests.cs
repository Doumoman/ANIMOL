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
