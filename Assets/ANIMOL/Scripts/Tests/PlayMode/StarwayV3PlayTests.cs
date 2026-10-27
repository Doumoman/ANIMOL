#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class StarwayV3PlayTests
    {
        [UnityTest] public IEnumerator AllTwentyMapsLoadWithVisualsHudAndWholeBoardCamera()
        {
            var set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>("Assets/ANIMOL/Data/Puzzle/Starway/Maps/V3/StarwayV3MapSet.asset");
            Assert.That(set, Is.Not.Null);
            var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera";
            var host = new GameObject("Starway V3 test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/hud_visual.prefab");
            controller.secondsPerCell = .001f;
            try
            {
                foreach (var entry in set.maps)
                {
                    controller.Load(entry.map); yield return null; yield return null;
                    Assert.That(controller.Session, Is.Not.Null, entry.stageName);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero, entry.stageName);
                    Assert.That(controller.Session.Collected, Is.Zero, entry.stageName);
                    Assert.That(controller.GetComponentInChildren<PuzzleHudView>().bubbles.text,
                        Is.EqualTo("0/3"), entry.stageName + " HUD");
                    Assert.That(controller.RuntimeMap.targetBubbles, Is.EqualTo(3), entry.stageName);
                    Assert.That(controller.Strict, Is.Not.Null, entry.stageName);
                    if (entry.sourceId == "SR19")
                        Assert.That(controller.GetComponentsInChildren<TextMesh>().Count(t => t.text == "4"),
                            Is.EqualTo(2), "V13 pattern 4 needs distinct stone and socket markers");
                    var lens = controller.FollowCamera.GetComponent<Camera>();
                    var halfH = lens.orthographicSize; var halfW = halfH * lens.aspect;
                    Assert.That(halfW * 2, Is.GreaterThanOrEqualTo(entry.map.Width), entry.stageName + " width");
                    Assert.That(halfH * 2, Is.GreaterThanOrEqualTo(entry.map.Height), entry.stageName + " height");
                    Assert.That(controller.Session.Cell, Is.EqualTo(entry.map.start), entry.stageName);
                }
            }
            finally { Object.Destroy(host); Object.Destroy(cameraObject); }
        }

        [UnityTest] public IEnumerator StrictWrongInputLocksThenResetRestoresBubbleHudAndRoute()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/Maps/1-2.asset");
            var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera";
            var host = new GameObject("Starway V3 strict test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/hud_visual.prefab");
            controller.secondsPerCell = .001f;
            try
            {
                controller.Load(map); yield return null;
                var shownCounts = new System.Collections.Generic.HashSet<int> { 0 };
                var exit = controller.GetComponentsInChildren<SpriteRenderer>().First(r => r.gameObject.name == "Exit");
                var unreadyTint = exit.color;
                Assert.That(unreadyTint, Is.Not.EqualTo(Color.white));
                var first = map.scenario.verificationRoute[0];
                var wrong = first == 'N' ? Vector2Int.left : Vector2Int.up;
                Assert.That(controller.RequestMove(wrong), Is.False);
                Assert.That(controller.Strict.Failed, Is.True);
                Assert.That(controller.Session.Cost, Is.Zero);
                Assert.That(controller.Session.Collected, Is.Zero);
                Assert.That(controller.RequestMove(Vector2Int.up), Is.False);
                Assert.That(controller.UndoLast(), Is.False);
                controller.ResetStage();
                Assert.That(controller.Strict.Failed, Is.False);
                Assert.That(controller.Session.Cost, Is.Zero);
                Assert.That(controller.Session.Collected, Is.Zero);
                foreach (var c in map.scenario.verificationRoute)
                {
                    var direction = c == 'N' ? Vector2Int.up : c == 'E' ? Vector2Int.right :
                        c == 'S' ? Vector2Int.down : Vector2Int.left;
                    Assert.That(controller.RequestMove(direction), Is.True, "input " + (controller.Session.Cost + 1));
                    var deadline = Time.realtimeSinceStartup + 15f;
                    while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(controller.IsMoving, Is.False);
                    yield return null;
                    var hudCount = controller.GetComponentInChildren<PuzzleHudView>().bubbles.text;
                    Assert.That(hudCount, Is.EqualTo(controller.Session.Collected + "/3"));
                    shownCounts.Add(controller.Session.Collected);
                }
                Assert.That(controller.Session.Won, Is.True);
                Assert.That(controller.Session.Collected, Is.EqualTo(3));
                Assert.That(controller.Session.Cost, Is.EqualTo(50));
                yield return null;
                Assert.That(controller.GetComponentInChildren<PuzzleHudView>().bubbles.text, Is.EqualTo("3/3"));
                Assert.That(exit.color, Is.EqualTo(Color.white));
                Assert.That(shownCounts, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
            }
            finally { Object.Destroy(host); Object.Destroy(cameraObject); }
        }
    }
}
#endif
