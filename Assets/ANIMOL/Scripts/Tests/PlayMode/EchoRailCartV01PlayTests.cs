using System.Collections;
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
                Assert.That(controller.UndoLast(), Is.True);
                AssertCart(controller);
                controller.ResetStage();
                AssertCart(controller);
                controller.Load(map);
                AssertCart(controller);
            }
            finally { Object.Destroy(host); }
        }

        static void AssertCart(PuzzlePrototypeController controller)
        {
            var stage = controller.Session.Devices.First(d => d.role == PrototypeRole.MovingStage);
            Assert.That(controller.Devices.MissingBindings, Is.Zero);
            var root = controller.Devices.Actor(stage.id);
            var visual = root.GetComponentInChildren<EchoRailCartVisualV01>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(root.GetComponent<ANIMOL.Presentation.DeviceVisualView>().enabled, Is.False);
            var direction = controller.Session.States[stage.id].direction;
            var heading = direction == Vector2Int.up ? "north" : direction == Vector2Int.right ? "east" :
                direction == Vector2Int.down ? "south" : "west";
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite,
                Is.SameAs(controller.Session.Map.theme.echoRailCartV01Catalog.sprites
                    .First(s => s.key == "cart/" + heading + "/idle").sprite));
            Assert.That(root.localPosition, Is.EqualTo((Vector3)(Vector2)controller.Session.States[stage.id].cell));
            Assert.That(root.localRotation, Is.EqualTo(Quaternion.identity));
            var rails = controller.Devices.GetComponentsInChildren<SpriteRenderer>()
                .Where(r => r.name.StartsWith(stage.id + " rail ")).ToArray();
            Assert.That(rails.Length, Is.EqualTo(stage.cells.Count));
            Assert.That(rails.All(r => r.transform.localRotation == Quaternion.identity), Is.True);
            var railSprites = controller.Session.Map.theme.echoRailCartV01Catalog.sprites
                .Where(s => s.key.StartsWith("rail/")).Select(s => s.sprite).ToArray();
            Assert.That(rails.All(r => railSprites.Contains(r.sprite)), Is.True);
        }
    }
}
