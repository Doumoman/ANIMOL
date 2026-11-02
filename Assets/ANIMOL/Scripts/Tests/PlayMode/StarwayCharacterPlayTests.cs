using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.StarwayRetouch;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Presentation.Tests
{
    public class StarwayCharacterPlayTests
    {
        const string Portrait = "Docs/Validation/StarwayNative/Portrait";

        [UnityTest] public IEnumerator RetouchPushKeepsSizeAndReturnsGraphicsToRest()
        {
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/StarwayCharacters.asset");
            var host = new GameObject("Retouch play validation");
            var target = new GameObject("Visual contact target").transform;
            try
            {
                var visual = StarwayCharacterVisual.Create(host.transform, Vector2.zero, 40, catalog, "choa");
                var logicRoot = visual.transform.parent;
                var logicStart = logicRoot.position;
                foreach (var entry in catalog.characters)
                {
                    Assert.That(visual.Initialize(catalog, entry.id), Is.True, entry.id);
                    Assert.That(visual.PushPresenter.enabled, Is.True, entry.id);
                    Assert.That(visual.visual.sprite.rect.height / visual.visual.sprite.pixelsPerUnit *
                        visual.transform.lossyScale.y, Is.EqualTo(entry.calibratedStandingHeight).Within(.001f), entry.id);
                    foreach (var direction in new[] { "front", "back", "side", "left" })
                        foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory", "push" })
                        {
                            Assert.That(visual.PlayPose(direction, state), Is.True, entry.id + "/" + direction + "/" + state);
                            Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                            Assert.That(visual.CurrentClip, Is.Not.Null);
                            if (state == "push")
                                Assert.That(entry.pushContacts.TryGet(visual.visual.sprite, out _), Is.True);
                        }
                    visual.SetMotion(Vector2Int.right, "push");
                    Assert.That(entry.pushContacts.TryGet(visual.visual.sprite, out var contact), Is.True);
                    var hand = visual.visual.transform.TransformPoint(contact.handLocal);
                    target.position = hand + Vector3.right * .20f;
                    visual.PushPresenter.BeginPush(null, Vector2.right, target);
                    yield return new WaitForSeconds(.15f);
                    Assert.That(visual.PushPresenter.PoseActive, Is.True);
                    visual.PushPresenter.EndPush();
                    yield return new WaitForSeconds(.13f);
                    Assert.That(visual.transform.localPosition, Is.EqualTo(entry.restingLocalPosition));
                    Assert.That(logicRoot.position, Is.EqualTo(logicStart));
                }
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(target.gameObject);
            }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ExistingStarstoneAndCratePushKeepSessionResults()
        {
#if UNITY_EDITOR
            foreach (var sample in new[] {
                ("Assets/ANIMOL/Data/Puzzle/Prototypes/Device01_StarSocket.asset", Vector2Int.right),
                ("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW05_MapData.asset", Vector2Int.up) })
            {
                var map = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(sample.Item1);
                var expected = new PuzzleDeviceSession(map);
                var action = expected.Apply(sample.Item2);
                Assert.That(action.accepted, Is.True, sample.Item1);
                var pushed = action.events.Find(e => e.kind == PrototypeEventKind.Move && e.actor != "player" &&
                    e.from == map.start + sample.Item2);
                Assert.That(pushed, Is.Not.Null, sample.Item1);
                var host = new GameObject("Retouch committed push QA");
                var controller = host.AddComponent<PuzzlePrototypeController>();
                controller.Load(map);
                Assert.That(controller.RequestMove(sample.Item2), Is.True, sample.Item1);
                Assert.That(controller.CharacterVisual.State, Is.EqualTo("push"), sample.Item1);
                Assert.That(controller.CharacterVisual.PushPresenter.PoseActive, Is.True, sample.Item1);
                if (sample.Item1.Contains("Device01_StarSocket"))
                {
                    var contactPoint = controller.Devices.Actor(pushed.actor).Find("PushVisualContact");
                    Assert.That(contactPoint, Is.Not.Null);
                    var targetRenderer = controller.Devices.Actor(pushed.actor)
                        .GetComponentInChildren<SpriteRenderer>();
                    var points = new System.Collections.Generic.List<Vector2>();
                    var visibleFace = float.PositiveInfinity;
                    for (var shape = 0; shape < targetRenderer.sprite.GetPhysicsShapeCount(); shape++)
                    {
                        points.Clear();
                        targetRenderer.sprite.GetPhysicsShape(shape, points);
                        foreach (var point in points)
                            visibleFace = Mathf.Min(visibleFace,
                                targetRenderer.transform.TransformPoint(point).x);
                    }
                    Assert.That(contactPoint.position.x, Is.EqualTo(visibleFace).Within(.001f),
                        "Contact point follows the alpha outline, not the transparent rectangle");
                }
                for (var frame = 0; frame < 300 && controller.IsMoving; frame++) yield return null;
                Assert.That(controller.IsMoving, Is.False, sample.Item1);
                yield return new WaitForSeconds(.13f);
                Assert.That(controller.Session.Cell, Is.EqualTo(expected.Cell), sample.Item1);
                Assert.That(controller.Session.Cost, Is.EqualTo(expected.Cost), sample.Item1);
                Assert.That(controller.Session.States[pushed.actor].cell,
                    Is.EqualTo(expected.States[pushed.actor].cell), sample.Item1);
                Assert.That(controller.CharacterVisual.transform.localPosition,
                    Is.EqualTo(controller.CharacterVisual.catalog.Find(controller.CharacterVisual.characterId).restingLocalPosition));
                Object.Destroy(host);
                yield return null;
            }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ExistingSheepVisualPushesFromFourDirections()
        {
#if UNITY_EDITOR
            foreach (var sample in new[] {
                ("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW02_MapData.asset", new Vector2Int(6, 12), Vector2Int.up),
                ("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW02_MapData.asset", new Vector2Int(6, 14), Vector2Int.down),
                ("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW03_MapData.asset", new Vector2Int(6, 16), Vector2Int.right),
                ("Assets/ANIMOL/Data/Puzzle/MapData/starway/SW03_MapData.asset", new Vector2Int(8, 16), Vector2Int.left) })
            {
                var source = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(sample.Item1);
                var map = Object.Instantiate(source);
                map.start = sample.Item2;
                var expected = new PuzzleDeviceSession(map);
                var action = expected.Apply(sample.Item3);
                Assert.That(action.accepted, Is.True, sample.Item1 + sample.Item3);
                Assert.That(action.events.Exists(e => e.eventName == "SheepPushed"), Is.True);
                var host = new GameObject("Retouch sheep contact QA");
                var controller = host.AddComponent<PuzzlePrototypeController>();
                controller.Load(map);
                Assert.That(controller.RequestMove(sample.Item3), Is.True);
                Assert.That(controller.CharacterVisual.State, Is.EqualTo("push"));
                Assert.That(controller.CharacterVisual.PushPresenter.PoseActive, Is.True);
                for (var frame = 0; frame < 300 && controller.IsMoving; frame++) yield return null;
                Assert.That(controller.IsMoving, Is.False, sample.Item1 + sample.Item3);
                yield return new WaitForSeconds(.13f);
                Assert.That(controller.Session.Cost, Is.EqualTo(expected.Cost));
                Assert.That(controller.Session.Cell, Is.EqualTo(expected.Cell));
                Assert.That(controller.CharacterVisual.transform.localPosition,
                    Is.EqualTo(controller.CharacterVisual.catalog.Find("choa").restingLocalPosition));
                Object.Destroy(host);
                Object.Destroy(map);
                yield return null;
            }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        static void UsePortraitGameView()
        {
            var setup = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .FirstOrDefault(type => type != null);
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { 1080, 1920, "Starway character art QA" });
        }

        [UnityTest] public IEnumerator FourCharactersAllDirectionsAndStatesPlayOnVisual()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            StarwayCharacterQaController qa = null;
            for (var attempt = 0; attempt < 60 && !qa; attempt++)
            {
                yield return null;
                qa = Object.FindFirstObjectByType<StarwayCharacterQaController>();
            }
            Assert.That(qa, Is.Not.Null);
            var visual = qa.character;
            Assert.That(visual.transform.name, Is.EqualTo("Visual"));
            Assert.That(visual.visual, Is.SameAs(visual.GetComponent<SpriteRenderer>()));
            foreach (var entry in qa.catalog.characters)
                foreach (var direction in new[] { "front", "back", "side", "left" })
                    foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory", "push" })
                    {
                        Assert.That(qa.Preview(entry.id, direction, state), Is.True, entry.id + "/" + direction + "/" + state);
                        Assert.That(visual.CurrentClip, Is.Not.Null);
                        Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                        Assert.That(visual.visual.sprite, Is.Not.Null);
                        Assert.That(visual.Illustration, Is.SameAs(entry.illustration));
                        var motion = entry.Motion(direction == "left" ? "side" : direction, state);
                        Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]),
                            "renderer=" + visual.visual.sprite.GetInstanceID() + " frame=" + motion.frames[0].GetInstanceID() +
                            " sameCatalog=" + ReferenceEquals(qa.catalog, visual.catalog));
                        for (var frame = 1; frame < 6; frame++)
                        {
                            visual.Advance(1f / motion.fps + .0001f);
                            Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[frame]),
                                entry.id + "/" + direction + "/" + state + "/" + frame);
                        }
                        visual.Advance(1f / motion.fps + .0001f);
                        if (motion.loop) Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]));
                        else Assert.That(visual.State, Is.EqualTo("idle"));
                        yield return null;
                    }
            Assert.That(qa.Preview("yeoul", "back", "idle"), Is.True);
            Assert.That(visual.visual.flipX, Is.False);
            Assert.That(qa.Preview("choa", "front", "idle"), Is.True);
            var first = visual.visual.sprite;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(visual.visual.sprite, Is.Not.SameAs(first), "Animation changes the SpriteRenderer frame");
            yield return new WaitForSecondsRealtime(1.15f);
            Assert.That(visual.State, Is.EqualTo("idle"), "idle repeats");
            Assert.That(qa.Preview("yeoul", "side", "jump"), Is.True);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(visual.State, Is.EqualTo("idle"), "jump returns to idle");
            Assert.That(qa.Preview("yeoul", "front", "victory"), Is.True);
            yield return new WaitForSecondsRealtime(.95f);
            Assert.That(visual.State, Is.EqualTo("idle"), "victory returns to idle");
            UsePortraitGameView();
            yield return null;
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            Directory.CreateDirectory(Portrait);
            foreach (var entry in qa.catalog.characters)
            {
                Assert.That(qa.Preview(entry.id, "front", "idle"), Is.True);
                yield return null;
                ScreenCapture.CaptureScreenshot(Portrait + "/" + entry.id + "_front_idle.png");
                yield return new WaitForSecondsRealtime(.2f);
            }
            Assert.That(qa.Preview("yeoul", "back", "idle"), Is.True);
            yield return null;
            ScreenCapture.CaptureScreenshot(Portrait + "/yeoul_back_idle.png");
            yield return new WaitForSecondsRealtime(.2f);
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ActualPuzzleSpawnSelectionAndSceneReloadKeepNativeArt()
        {
#if UNITY_EDITOR
            UsePortraitGameView();
            yield return null;
            yield return null;
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzleController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("choa"));
            Assert.That(puzzle.Player.localScale, Is.EqualTo(Vector3.one));
            Assert.That(puzzle.CharacterVisual.transform.localScale.x, Is.EqualTo(.85f).Within(.0001f));
            Directory.CreateDirectory(Portrait);
            ScreenCapture.CaptureScreenshot(Portrait + "/puzzle_choa.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(puzzle.SelectStarwayCharacter("yeoul"), Is.True);
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("yeoul"));
            Assert.That(puzzle.CharacterVisual.visual.sprite, Is.Not.Null);
            yield return null;
            ScreenCapture.CaptureScreenshot(Portrait + "/puzzle_yeoul.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(puzzle.RequestMove(Vector2Int.right), Is.True);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(puzzle.CharacterVisual.Direction, Is.EqualTo("side"));
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("yeoul"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var qa = Object.FindFirstObjectByType<StarwayCharacterQaController>();
            Assert.That(qa.Preview("yeoul", "front", "idle"), Is.True);
            Assert.That(qa.character.Illustration, Is.SameAs(qa.catalog.Find("yeoul").illustration));
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator StarwayPrototypeUsesTheSameCharacterVisual()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype, Is.Not.Null);
            Assert.That(prototype.CharacterVisual, Is.Not.Null);
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("choa"));
            Assert.That(prototype.SelectStarwayCharacter("tori"), Is.True);
            Assert.That(prototype.CharacterVisual.visual.sprite,
                Is.SameAs(prototype.RuntimeMap.theme.starwayCharacters.Find("tori").FirstFrame("front", "idle")),
                "renderer=" + prototype.CharacterVisual.visual.sprite.GetInstanceID() +
                " frame=" + prototype.RuntimeMap.theme.starwayCharacters.Find("tori").FirstFrame("front", "idle").GetInstanceID());
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }
    }
}
