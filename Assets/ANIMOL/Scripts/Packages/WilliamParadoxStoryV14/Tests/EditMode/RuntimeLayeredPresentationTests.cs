using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story.Tests
{
    public sealed class RuntimeLayeredPresentationTests
    {
        private GameObject root;
        private StoryView view;
        private Texture2D texture;
        private readonly List<Sprite> sprites = new List<Sprite>();
        private StoryPresentationPlayer player;
        private StoryGraph graph;
        private StoryPresentationPackage presentation;
        private Dictionary<string, Sprite> byId;
        [SetUp] public void SetUp()
        {
            root = new GameObject("LayeredStoryTest", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            view = root.AddComponent<StoryView>();
            view.artworkRoot = Child("ArtworkRoot", root.transform);
            view.artworkRoot.sizeDelta = new Vector2(1080, 1920);
            view.artImage = Child("ArtImage", view.artworkRoot).gameObject.AddComponent<Image>();
            view.artImage.rectTransform.sizeDelta = new Vector2(1080, 1920);
            texture = new Texture2D(2, 2);
            byId = new Dictionary<string, Sprite>();
            foreach (string id in new[] { "R01", "R02", "R17", "BG", "BOOK", "TABLE", "A", "A2", "B" })
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                sprite.name = id; sprites.Add(sprite); byId.Add(id, sprite);
            }
            var story = new StoryPackage
            {
                packageId = "test-layered", contentVersion = "14.0.0", startNodeId = "L1",
                assets = byId.Keys.Select(id => new StoryAsset { id = id, kind = id == "A" || id == "A2" || id == "B" ? "actor" : id == "BG" || id == "BOOK" ? "background" : id == "TABLE" ? "prop" : "scene", resourcePath = "WilliamParadox/V14/Tests/" + id }).ToArray(),
                // Deliberately not route order: reconstruction must follow nextId.
                nodes = new[]
                {
                    Node("END", "R02", "end", "R02", "", null),
                    Node("L2", "R01", "line", "R01", "이솔", "L3"),
                    Node("L1", "R01", "line", "R01", "초아", "L2"),
                    Node("L3", "R02", "line", "R02", "초아", "END")
                }
            };
            Assert.That(StoryGraph.TryCreate(story, out graph, out var errors), Is.True, string.Join("\n", errors));
            presentation = new StoryPresentationPackage
            {
                scenes = new[] { LayeredScene("R01") },
                shots = new[]
                {
                    new StoryPresentationShot { nodeId = "L1", actorStates = new[] { new StoryActorState { actorId = "a", assetId = "A2" } } },
                    new StoryPresentationShot { nodeId = "L2", camera = new StoryCameraShot { focusX = 0.35f, focusY = 0.65f, scale = 1.6f, duration = 0.5f } }
                }
            };
        }
        [TearDown] public void TearDown()
        {
            if (player != null) player.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
            sprites.Clear(); UnityEngine.Object.DestroyImmediate(texture);
        }
        private void CreatePlayer() { player = new StoryPresentationPlayer(view, graph, presentation, id => byId[id]); }
        private static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        private static StoryNode Node(string id, string sceneId, string kind, string art, string speaker, string next)
        {
            return new StoryNode { id = id, sceneId = sceneId, beatId = sceneId + "_B1", kind = kind, artId = art, speaker = speaker, text = kind == "line" ? "test" : "", nextId = next };
        }
        private static StoryLayeredScene LayeredScene(string scene)
        {
            return new StoryLayeredScene
            {
                sceneId = scene, backgroundArtId = "BG", foregroundArtId = "TABLE",
                foreground = new StoryStagePlacement { x = 0.5f, y = 0.35f, width = 800, height = 300 },
                actors = new[]
                {
                    new StoryActorPlacement { actorId = "a", speaker = "초아", assetId = "A", x = 0.25f, y = 0.4f, width = 250, height = 650, pivotX = 0.5f, pivotY = 0 },
                    new StoryActorPlacement { actorId = "b", speaker = "이솔", assetId = "B", x = 0.75f, y = 0.4f, width = 250, height = 650, pivotX = 0.5f, pivotY = 0 }
                }
            };
        }
        [Test] public void ExactlyTheSevenRequestedScenesAllowBounce()
        {
            var expected = new HashSet<int> { 1, 5, 6, 8, 10, 13, 17 };
            for (int i = 1; i <= 17; i++) Assert.That(StoryPresentationIndex.AllowsBounce("R" + i.ToString("00")), Is.EqualTo(expected.Contains(i)));
        }
        [Test] public void WholeFrameBounceAndOldZoomCannotBypassSidecarPolicy()
        {
            var cues = new[] { new StoryCue { type = "shake" }, new StoryCue { type = "popup" }, new StoryCue { type = "zoom" }, new StoryCue { type = "dim" }, new StoryCue { type = "inset" } };
            CollectionAssert.AreEqual(new[] { "dim", "inset" }, StoryPresentationIndex.FilterLegacyCues(cues, true).Select(c => c.type).ToArray());
            CollectionAssert.AreEqual(new[] { "zoom", "dim", "inset" }, StoryPresentationIndex.FilterLegacyCues(cues, false).Select(c => c.type).ToArray());
        }
        [Test] public void UnknownActorStateAndNonfiniteCameraAreRejectedBeforeRendering()
        {
            presentation.shots[0].actorStates[0].actorId = "missing";
            presentation.shots[1].camera.scale = float.NaN;
            var errors = StoryPresentationIndex.Validate(presentation, graph);
            Assert.That(errors.Any(x => x.Contains("actor state")), Is.True);
            Assert.That(errors.Any(x => x.Contains("camera")), Is.True);
        }
        [Test] public void LayeredActorsAreRejectedOutsideTheAllowlist()
        {
            presentation.scenes[0].sceneId = "R02";
            Assert.That(StoryPresentationIndex.Validate(presentation, graph).Any(x => x.Contains("seven requested")), Is.True);
        }
        [Test] public void OnlyTheSpeakerMovesWhileBackgroundTableAndDialogueStayStill()
        {
            CreatePlayer(); player.Present(graph.Node("L1"), false, false);
            var speaker = player.ActorTransform("a"); var listener = player.ActorTransform("b");
            Vector2 speakerHome = speaker.anchoredPosition, listenerHome = listener.anchoredPosition;
            Vector2 table = view.layeredForeground.rectTransform.anchoredPosition;
            Vector2 background = view.layeredBackground.rectTransform.anchoredPosition;
            var dialogue = Child("DialogueOutsideArtwork", root.transform); dialogue.anchoredPosition = new Vector2(8, 300);
            player.Tick(0.16f);
            Assert.That(speaker.anchoredPosition.y, Is.GreaterThan(speakerHome.y));
            Assert.That(speaker.localScale, Is.Not.EqualTo(Vector3.one));
            Assert.That(listener.anchoredPosition, Is.EqualTo(listenerHome));
            Assert.That(listener.localScale, Is.EqualTo(Vector3.one));
            Assert.That(view.layeredForeground.rectTransform.anchoredPosition, Is.EqualTo(table));
            Assert.That(view.layeredBackground.rectTransform.anchoredPosition, Is.EqualTo(background));
            Assert.That(dialogue.anchoredPosition, Is.EqualTo(new Vector2(8, 300)));
        }
        [Test] public void ZeroDeltaFreezesBothBounceAndCamera()
        {
            CreatePlayer(); player.Present(graph.Node("L2"), false, false); player.Tick(0.1f);
            var actor = player.ActorTransform("b"); var position = actor.anchoredPosition; var scale = actor.localScale;
            var camera = view.artworkRoot.anchoredPosition; var zoom = view.artworkRoot.localScale;
            for (int i = 0; i < 30; i++) player.Tick(0);
            Assert.That(actor.anchoredPosition, Is.EqualTo(position)); Assert.That(actor.localScale, Is.EqualTo(scale));
            Assert.That(view.artworkRoot.anchoredPosition, Is.EqualTo(camera)); Assert.That(view.artworkRoot.localScale, Is.EqualTo(zoom));
        }
        [Test] public void PerSceneStrengthCanKeepTheReturnSceneSubtle()
        {
            presentation.scenes[0].bounceStrength = 0.35f;
            CreatePlayer(); player.Present(graph.Node("L1"), false, false);
            var actor = player.ActorTransform("a"); float home = actor.anchoredPosition.y;
            player.Tick(StoryPresentationMath.BounceDuration * 0.5f);
            Assert.That(actor.anchoredPosition.y - home, Is.EqualTo(3.5f).Within(0.001f));
            Assert.That(Mathf.Abs(actor.localScale.y - 1), Is.LessThan(0.01f));
            player.FinishAll(); Assert.That(actor.localScale, Is.EqualTo(Vector3.one));
        }
        [Test] public void OutOfRangeSceneStrengthIsRejected()
        {
            presentation.scenes[0].bounceStrength = 1.01f;
            Assert.That(StoryPresentationIndex.Validate(presentation, graph).Any(x => x.Contains("bounceStrength")), Is.True);
        }
        [Test] public void FastAdvanceAndRepeatedBouncesNeverAccumulateScaleOrPosition()
        {
            CreatePlayer();
            for (int i = 0; i < 25; i++)
            {
                player.Present(graph.Node("L1"), false, false); player.Tick(0.08f);
                player.Present(graph.Node("L2"), false, false); player.Tick(0.12f); player.FinishAll();
                Assert.That(player.ActorTransform("a").anchoredPosition, Is.EqualTo(new Vector2(270, 768)));
                Assert.That(player.ActorTransform("b").anchoredPosition, Is.EqualTo(new Vector2(810, 768)));
                Assert.That(player.ActorTransform("a").localScale, Is.EqualTo(Vector3.one));
                Assert.That(player.ActorTransform("b").localScale, Is.EqualTo(Vector3.one));
                Assert.That(player.ActorTransform("a").localRotation, Is.EqualTo(Quaternion.identity));
            }
        }
        [Test] public void RestoringRebuildsEarlierPoseChangesAndSettlesTheAuthoredCamera()
        {
            CreatePlayer(); player.Present(graph.Node("L2"), false, true);
            Assert.That(player.ActorTransform("a").GetComponent<Image>().sprite, Is.SameAs(byId["A2"]));
            Assert.That(view.artworkRoot.localScale.x, Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.ActorTransform("b").localScale, Is.EqualTo(Vector3.one));
        }
        [Test] public void ReducedMotionKeepsNewPosesButRunsNoBounceOrCameraTween()
        {
            CreatePlayer(); player.Present(graph.Node("L2"), true, false);
            Assert.That(player.ActorTransform("a").GetComponent<Image>().sprite, Is.SameAs(byId["A2"]));
            Assert.That(player.IsPlaying, Is.False); Assert.That(view.artworkRoot.localScale.x, Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(player.ActorTransform("b").localScale, Is.EqualTo(Vector3.one));
        }
        [Test] public void R17BookHoldHidesLayersAndClearsTheCloseupBeforeCredits()
        {
            var p = graph.Package;
            p.startNodeId = "RETURN";
            p.nodes = new[] { Node("RETURN", "R17", "line", "R17", "이솔", "BOOK_HOLD"), Node("BOOK_HOLD", "R17", "cue", "BOOK", "", "END"), Node("END", "R17", "end", "BOOK", "", null) };
            Assert.That(StoryGraph.TryCreate(p, out graph, out var errors), Is.True, string.Join("\n", errors));
            presentation.scenes = new[] { LayeredScene("R17") };
            presentation.shots = new[] { new StoryPresentationShot { nodeId = "RETURN", camera = new StoryCameraShot { scale = 1.8f, focusY = 0.6f } } };
            CreatePlayer(); player.Present(graph.Node("RETURN"), false, false); player.FinishAll();
            player.Present(graph.Node("BOOK_HOLD"), false, false);
            Assert.That(view.layeredRoot.gameObject.activeSelf, Is.False);
            Assert.That(view.artImage.enabled, Is.True);
            Assert.That(view.artworkRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(player.IsPlaying, Is.False);
        }
        [Test] public void CameraClampNeverRevealsAnEdgeAtExtremeFocusPoints()
        {
            foreach (var viewport in new[] { new Vector2(1080, 1920), new Vector2(1080, 2160), new Vector2(1920, 1080) })
            {
                float cover = Mathf.Max(viewport.x / 1080, viewport.y / 1920);
                var content = new Vector2(1080, 1920) * cover;
                foreach (var focus in new[] { Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector2(1, 0) })
                {
                    const float zoom = 1.8f;
                    var position = StoryPresentationMath.CameraPosition(viewport, content, zoom, focus);
                    var minimum = position - content * zoom * 0.5f; var maximum = position + content * zoom * 0.5f;
                    Assert.That(minimum.x, Is.LessThanOrEqualTo(-viewport.x * 0.5f + 0.01f));
                    Assert.That(minimum.y, Is.LessThanOrEqualTo(-viewport.y * 0.5f + 0.01f));
                    Assert.That(maximum.x, Is.GreaterThanOrEqualTo(viewport.x * 0.5f - 0.01f));
                    Assert.That(maximum.y, Is.GreaterThanOrEqualTo(viewport.y * 0.5f - 0.01f));
                }
            }
        }
        [Test] public void BounceEndsExactlyAtHomeAndStaysWithinTheAuthoredSmallAmplitude()
        {
            foreach (float t in new[] { 0f, 1f, 2f })
            {
                var sample = StoryPresentationMath.Bounce(t);
                Assert.That(sample.scale, Is.EqualTo(Vector3.one)); Assert.That(sample.y, Is.Zero); Assert.That(sample.rotation, Is.Zero);
            }
            for (int i = 0; i <= 100; i++)
            {
                var sample = StoryPresentationMath.Bounce(i / 100f);
                Assert.That(sample.y, Is.InRange(0, 10.001f));
                Assert.That(sample.scale.x, Is.InRange(0.9749f, 1.0251f));
                Assert.That(sample.scale.y, Is.InRange(0.9749f, 1.0251f));
            }
        }
    }
}
