using System.Collections;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public class TheaterRetouchPlayTests
    {
        [UnityTest] public IEnumerator TheaterAndStarwaySelectOnlyTheirOwnPushPresenter()
        {
#if UNITY_EDITOR
            var theater = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset");
            var starway = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/StarwayCharacters.asset");
            var host = new GameObject("Theater PUSH selection QA");
            var contact = new GameObject("PUSH contact").transform;
            try
            {
                var visual = StarwayCharacterVisual.Create(host.transform, Vector2.zero, 40, theater, "seri");
                foreach (var entry in theater.characters)
                {
                    Assert.That(visual.Initialize(theater, entry.id), Is.True, entry.id);
                    Assert.That(visual.TheaterPushPresenter.isActiveAndEnabled, Is.True, entry.id);
                    Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.False, entry.id);
                    Assert.That(visual.transform.localScale.x, Is.EqualTo(.85f).Within(.0001f), entry.id);
                    Assert.That(visual.visual.sprite.rect.height / visual.visual.sprite.pixelsPerUnit *
                        visual.transform.lossyScale.y, Is.EqualTo(entry.calibratedStandingHeight).Within(.001f), entry.id);
                    foreach (var direction in new[] { "front", "back", "side", "left" })
                        foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory", "push" })
                        {
                            Assert.That(visual.PlayPose(direction, state), Is.True, entry.id + "/" + direction + "/" + state);
                            Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                            if (state == "push")
                                Assert.That(entry.theaterPushContacts.TryGet(visual.visual.sprite, out _), Is.True);
                        }
                    visual.SetMotion(Vector2Int.right, "push");
                    Assert.That(entry.theaterPushContacts.TryGet(visual.visual.sprite, out var anchor), Is.True);
                    contact.position = visual.visual.transform.TransformPoint(anchor.handLocal) + Vector3.right * .2f;
                    visual.BeginPushPresentation(null, Vector2.right, contact);
                    Assert.That(visual.PushPoseActive, Is.True, entry.id);
                    Assert.That(visual.Initialize(starway, "choa"), Is.True);
                    Assert.That(visual.TheaterPushPresenter.isActiveAndEnabled, Is.False);
                    Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.True);
                    Assert.That(visual.transform.localPosition,
                        Is.EqualTo(starway.Find("choa").restingLocalPosition));
                    Assert.That(visual.PlayPose("side", "push"), Is.True);
                    Assert.That(visual.Initialize(theater, entry.id), Is.True);
                    Assert.That(visual.TheaterPushPresenter.isActiveAndEnabled, Is.True);
                    Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.False);
                    Assert.That(visual.transform.localPosition, Is.EqualTo(entry.restingLocalPosition));
                    yield return null;
                }
            }
            finally { Object.Destroy(host); Object.Destroy(contact.gameObject); }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ExistingTheaterDominoAndDiceUseCommittedPushWithoutChangingTurns()
        {
#if UNITY_EDITOR
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device13_Domino.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device22_Dice.asset" })
            {
                var map = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
                var expected = new PuzzleDeviceSession(map);
                var action = expected.Apply(Vector2Int.right);
                Assert.That(action.accepted, Is.True, path);
                var host = new GameObject("Theater committed PUSH QA");
                var controller = host.AddComponent<PuzzlePrototypeController>();
                controller.Load(map);
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True, path);
                Assert.That(controller.CharacterVisual.State, Is.EqualTo("push"), path);
                Assert.That(controller.CharacterVisual.TheaterPushPresenter.PoseActive, Is.True, path);
                Assert.That(controller.CharacterVisual.PushPresenter.isActiveAndEnabled, Is.False, path);
                var deadline = Time.realtimeSinceStartup + 5f;
                while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(controller.IsMoving, Is.False, path);
                yield return new WaitForSeconds(.13f);
                Assert.That(controller.Session.Cell, Is.EqualTo(expected.Cell), path);
                Assert.That(controller.Session.Cost, Is.EqualTo(expected.Cost), path);
                Assert.That(controller.CharacterVisual.transform.localPosition,
                    Is.EqualTo(controller.CharacterVisual.catalog.Find("seri").restingLocalPosition), path);
                Object.Destroy(host);
                yield return null;
            }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

    }
}
