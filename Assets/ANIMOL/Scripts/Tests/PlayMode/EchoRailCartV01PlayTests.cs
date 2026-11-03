using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Art.EchoRailCartV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class EchoRailCartV01PlayTests
    {
        [UnityTest] public IEnumerator ClosedRailShowsTurnedCartWithoutRotatingActor()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device06_MovingStage.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.start = new Vector2Int(1, 4);
            map.scenario.costLimit = 0;
            map.scenario.devices.First(d => d.id == "stage").cells = new List<Vector2Int>
            {
                new Vector2Int(2, 2), new Vector2Int(3, 2),
                new Vector2Int(3, 3), new Vector2Int(2, 3)
            };
            map.InvalidateIndex();
            var host = new GameObject("M06 closed rail turn test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                var track = controller.Devices.GetComponentsInChildren<EchoRailTrackViewV02>()
                    .Single(t => t.name == "stage rail track R2");
                Assert.That(track.Path.Closed, Is.True);
                for (var turn = 0; turn < 2; turn++)
                {
                    Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                    while (controller.IsMoving) yield return null;
                }
                Assert.That(controller.Session.States["stage"].direction, Is.EqualTo(Vector2Int.up));
                AssertCart(controller);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(map.scenario);
                Object.Destroy(map);
            }
        }

        [UnityTest] public IEnumerator ContactTweenMovesPlayerAheadOfCart()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device06_MovingStage.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.start = new Vector2Int(3, 3);
            map.scenario.costLimit = 0;
            map.InvalidateIndex();
            var host = new GameObject("M06 contact tween test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                Assert.That(controller.RequestMove(Vector2Int.down), Is.True);
                var stage = controller.Devices.Actor("stage");
                var checkedOverlap = false;
                while (controller.IsMoving)
                {
                    if (stage.position.x > 2.4f && stage.position.x < 2.9f)
                    {
                        Assert.That(controller.Player.position.x, Is.GreaterThan(3f));
                        checkedOverlap = true;
                    }
                    yield return null;
                }
                Assert.That(checkedOverlap, Is.True);
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(4, 2)));
                Assert.That(controller.Player.position, Is.EqualTo(new Vector3(4, 2, 0)));
                AssertCart(controller);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(map.scenario);
                Object.Destroy(map);
            }
        }

        [UnityTest] public IEnumerator M06UsesCartAndFloorRailsAfterMoveAndReload()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-06_MapData.asset");
            var host = new GameObject("M06 rail cart visual test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                AssertCart(controller);
                Assert.That(controller.RequestMove(Vector2Int.left), Is.True);
                while (controller.IsMoving) yield return null;
                AssertCart(controller);
                var moved = controller.Session.Copy();
                Assert.That(controller.UndoLast(), Is.True);
                AssertCart(controller);
                controller.ResetStage();
                AssertCart(controller);
                controller.RestoreSnapshot(moved);
                AssertCart(controller);
                controller.Load(map);
                AssertCart(controller);
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator IndependentRailDrawsOnceAndCarriesCartAfterReload()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device06_MovingStage.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.start = new Vector2Int(1, 4);
            map.scenario.costLimit = 0;
            var cart = map.scenario.devices.Single(d => d.role == PrototypeRole.MovingStage);
            cart.cells.Clear();
            map.scenario.railPaths.Add(new PrototypeRailPath { id = "rail_1", cells = new List<Vector2Int>
            {
                new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(2, 3)
            } });
            map.InvalidateIndex();
            var host = new GameObject("M06 independent rail test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
                Assert.That(controller.Devices.GetComponentsInChildren<EchoRailTrackViewV02>().Length, Is.EqualTo(1));
                var track = controller.Devices.GetComponentInChildren<EchoRailTrackViewV02>();
                Assert.That(track.name, Is.EqualTo("rail_1 rail track R2"));
                Assert.That(track.Path.CellCount, Is.EqualTo(4));
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                while (controller.IsMoving) yield return null;
                Assert.That(controller.Session.States[cart.id].cell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(controller.UndoLast(), Is.True);
                Assert.That(controller.Session.States[cart.id].cell, Is.EqualTo(new Vector2Int(2, 2)));
                controller.Load(map);
                Assert.That(controller.Devices.GetComponentsInChildren<EchoRailTrackViewV02>().Length, Is.EqualTo(1));
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); Object.Destroy(map.scenario); Object.Destroy(map); }
        }

        [UnityTest] public IEnumerator FloorRailRemainsVisibleWithoutAnyCart()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device06_MovingStage.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.scenario.devices.Clear();
            map.scenario.railPaths.Add(new PrototypeRailPath { id = "rail_1", cells = new List<Vector2Int>
            {
                new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)
            } });
            map.InvalidateIndex();
            var host = new GameObject("M06 floor-only rail test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                Assert.That(controller.Devices.GetComponentsInChildren<EchoRailTrackViewV02>().Length, Is.EqualTo(1));
                Assert.That(controller.Devices.GetComponentInChildren<EchoRailTrackViewV02>().Path.CellCount, Is.EqualTo(3));
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
                yield return null;
            }
            finally { Object.Destroy(host); Object.Destroy(map.scenario); Object.Destroy(map); }
        }

        static void AssertCart(PuzzlePrototypeController controller)
        {
            var stage = controller.Session.Devices.First(d => d.role == PrototypeRole.MovingStage);
            Assert.That(controller.Devices.MissingBindings, Is.Zero);
            var root = controller.Devices.Actor(stage.id);
            var visual = root.GetComponentInChildren<EchoRailCartVisualV01>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponent<ANIMOL.Presentation.DeviceVisualView>().enabled, Is.False);
            var direction = controller.Session.States[stage.id].direction;
            var heading = direction == Vector2Int.up ? "north" : direction == Vector2Int.right ? "east" :
                direction == Vector2Int.down ? "south" : "west";
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite,
                Is.SameAs(controller.Session.Map.theme.echoRailCartV01Catalog.sprites
                    .First(s => s.key == "cart/" + heading + "/idle").sprite));
            Assert.That(visual.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(30));
            Assert.That(root.localPosition, Is.EqualTo((Vector3)(Vector2)controller.Session.States[stage.id].cell));
            Assert.That(root.localRotation, Is.EqualTo(Quaternion.identity));
            var track = controller.Devices.GetComponentsInChildren<EchoRailTrackViewV02>()
                .Single(t => t.name == stage.id + " rail track R2");
            Assert.That(track.Path, Is.Not.Null);
            Assert.That(track.Path.CellCount, Is.EqualTo(stage.cells.Count));
            Assert.That(track.CellSize, Is.EqualTo(1f));
            Assert.That(track.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(track.transform.localScale, Is.EqualTo(Vector3.one));
            var rails = track.GetComponentsInChildren<MeshRenderer>();
            var sleepers = track.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(rails.Length, Is.EqualTo(1));
            Assert.That(rails[0].sortingOrder, Is.EqualTo(16));
            Assert.That(sleepers.Length, Is.EqualTo(track.Path.SleeperCount));
            Assert.That(sleepers.All(s => s.sortingOrder == 15), Is.True);
        }
    }
}
