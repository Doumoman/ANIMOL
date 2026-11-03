using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story.Tests
{
    public sealed class RuntimeSpeakerPortraitTests
    {
        private GameObject root;
        private StoryView view;
        private StorySpeakerPortrait portrait;
        private StoryPresentationPlayer player;
        private StoryGraph graph;
        private StoryPresentationPackage presentation;
        private Texture2D texture;
        private Dictionary<string, Sprite> sprites;
        private Vector2 home;
        private Vector3 homeScale;
        private Quaternion homeRotation;

        [SetUp] public void SetUp()
        {
            root = new GameObject("PortraitTest", typeof(RectTransform));
            view = root.AddComponent<StoryView>();
            view.artworkRoot = Child("ArtworkRoot", root.transform);
            view.artworkRoot.sizeDelta = new Vector2(1080, 1920);
            view.artImage = Child("CG", view.artworkRoot).gameObject.AddComponent<Image>();
            view.artImage.rectTransform.sizeDelta = new Vector2(1080, 1920);
            var dialogue = Child("DialogueTray", root.transform);
            view.dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
            portrait = root.AddComponent<StorySpeakerPortrait>();
            portrait.actorRect = Child("IndependentFaceCard", dialogue);
            portrait.actorRect.sizeDelta = new Vector2(216, 216);
            portrait.actorRect.anchoredPosition = home = new Vector2(96, 120);
            portrait.actorRect.localScale = homeScale = new Vector3(0.8f, 0.9f, 1);
            portrait.actorRect.localRotation = homeRotation = Quaternion.Euler(0, 0, 5);
            portrait.actorImage = portrait.actorRect.gameObject.AddComponent<Image>();
            texture = new Texture2D(2, 2);
            sprites = new Dictionary<string, Sprite>();
            foreach (var id in new[] { "T01", "T02", "T03", "FACE_A", "FACE_B", "ACTOR", "BG" })
                sprites[id] = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            var story = new StoryPackage
            {
                packageId = "portrait-tests", contentVersion = "1", startNodeId = "L1",
                assets = sprites.Keys.Select(id => new StoryAsset { id = id, kind = id.StartsWith("FACE", StringComparison.Ordinal) ? "portrait" : id == "ACTOR" ? "actor" : id == "BG" ? "background" : "scene", resourcePath = "Tests/" + id }).ToArray(),
                nodes = new[] { Line("L1", "T01", "A", "L2"), Line("L2", "T01", "A", "L3"), Line("L3", "T01", "B", "L4"), Line("L4", "T02", "B", "L5"), Line("L5", "T03", "A", "L6"), Line("L6", "T01", "A", "END"), new StoryNode { id = "END", sceneId = "T01", beatId = "T01_END", kind = "end", artId = "T01" } }
            };
            Assert.That(StoryGraph.TryCreate(story, out graph, out var errors), Is.True, string.Join("\n", errors));
            presentation = new StoryPresentationPackage
            {
                scenes = Array.Empty<StoryLayeredScene>(),
                shots = new[] { new StoryPresentationShot { nodeId = "L6", bounce = false } },
                speakerBounceScenes = new[] { Bounce("T01", 0.35f), Bounce("T02", 0.2f), Bounce("T03", 0.25f) },
                speakerPortraitScenes = new[] { "T01", "T02" },
                speakerPortraits = new[] { Face("A", "FACE_A"), Face("B", "FACE_B") }
            };
            view.artImage.sprite = sprites["T01"];
        }

        [TearDown] public void TearDown()
        {
            player?.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var sprite in sprites.Values) UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            value.SetParent(parent, false); return value;
        }
        private static StoryNode Line(string id, string scene, string speaker, string next)
        {
            return new StoryNode { id = id, sceneId = scene, beatId = scene + "_B1", kind = "line", artId = scene, speaker = speaker, text = "text", nextId = next };
        }
        private static StorySpeakerPortraitAsset Face(string speaker, string asset)
        {
            return new StorySpeakerPortraitAsset { speaker = speaker, assetId = asset, independentLayer = true };
        }
        private static StorySpeakerBounceScene Bounce(string scene, float strength)
        {
            return new StorySpeakerBounceScene { sceneId = scene, strength = strength };
        }
        private void CreatePlayer() { player = new StoryPresentationPlayer(view, graph, presentation, id => sprites[id]); }
        private void AssertAtHome()
        {
            Assert.That(portrait.actorRect.anchoredPosition, Is.EqualTo(home));
            Assert.That(portrait.actorRect.localScale, Is.EqualTo(homeScale));
            Assert.That(portrait.actorRect.localRotation, Is.EqualTo(homeRotation));
        }

        [Test] public void AuthoredPolicyOverridesTheRanchDefaultWithoutChangingTheLegacyApi()
        {
            var index = new StoryPresentationIndex(presentation, graph);
            Assert.That(index.AllowsSpeakerBounce("T01"), Is.True);
            Assert.That(index.BounceStrength("T01"), Is.EqualTo(0.35f));
            Assert.That(index.AllowsSpeakerBounce("R01"), Is.False);
            Assert.That(StoryPresentationIndex.AllowsBounce("R01"), Is.True);
            presentation.speakerBounceScenes = null;
            index = new StoryPresentationIndex(presentation, graph);
            Assert.That(index.AllowsSpeakerBounce("R01"), Is.True);
            Assert.That(index.AllowsSpeakerBounce("T01"), Is.False);
        }

        [Test] public void ExplicitEmptyAllowlistDisablesBounceRatherThanUsingLegacyScenes()
        {
            presentation.speakerBounceScenes = Array.Empty<StorySpeakerBounceScene>();
            var index = new StoryPresentationIndex(presentation, graph);
            Assert.That(index.AllowsSpeakerBounce("R01"), Is.False);
            Assert.That(index.ShouldBounce(graph.Node("L1")), Is.False);
        }

        [Test] public void NonfiniteStrengthDuplicateScenesAndUnsafePortraitMetadataAreRejected()
        {
            presentation.speakerBounceScenes = new[] { Bounce("T01", float.NaN), Bounce("T01", 0.3f) };
            presentation.speakerPortraits[0].independentLayer = false;
            presentation.speakerPortraits[1].assetId = "T01";
            presentation.speakerPortraitScenes = new[] { "missing" };
            var errors = StoryPresentationIndex.Validate(presentation, graph);
            Assert.That(errors.Any(e => e.Contains("strength")), Is.True);
            Assert.That(errors.Any(e => e.Contains("duplicate speaker bounce")), Is.True);
            Assert.That(errors.Any(e => e.Contains("independent UI")), Is.True);
            Assert.That(errors.Any(e => e.Contains("actor or portrait")), Is.True);
            Assert.That(errors.Any(e => e.Contains("portrait scene")), Is.True);
        }

        [Test] public void OnlyTheIndependentCardMovesAndTheFullCgAndDialogueTrayStayStill()
        {
            CreatePlayer(); player.Present(graph.Node("L1"), false, false);
            var tray = view.dialogueGroup.transform.localPosition;
            var artPosition = view.artImage.rectTransform.anchoredPosition;
            player.Tick(0.16f);
            Assert.That(portrait.actorImage.sprite, Is.SameAs(sprites["FACE_A"]));
            Assert.That(portrait.actorRect.anchoredPosition.y - home.y, Is.EqualTo(3.5f).Within(0.001f));
            Assert.That(view.artworkRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(view.artImage.rectTransform.anchoredPosition, Is.EqualTo(artPosition));
            Assert.That(view.dialogueGroup.transform.localPosition, Is.EqualTo(tray));
            Assert.That(view.layeredRoot.gameObject.activeSelf, Is.False);
        }

        [Test] public void ConsecutivePagesOfTheSameSpeakerDoNotRestartTheImpulse()
        {
            CreatePlayer(); player.Present(graph.Node("L1"), false, false); player.Tick(0.08f);
            player.Present(graph.Node("L2"), false, false); player.Tick(0.16f);
            Assert.That(portrait.IsVisible, Is.True); Assert.That(player.IsPlaying, Is.False); AssertAtHome();
            player.Present(graph.Node("L3"), false, false); player.Tick(0.16f);
            Assert.That(portrait.actorImage.sprite, Is.SameAs(sprites["FACE_B"]));
            Assert.That(portrait.actorRect.anchoredPosition.y, Is.GreaterThan(home.y));
        }

        [Test] public void ASceneChangeCanAnimateTheSameSpeakerOnce()
        {
            CreatePlayer(); player.Present(graph.Node("L3"), false, false); player.FinishAll();
            player.Present(graph.Node("L4"), false, false); player.Tick(0.16f);
            Assert.That(portrait.actorRect.anchoredPosition.y - home.y, Is.EqualTo(2).Within(0.001f));
        }

        [Test] public void DeliveryLabelsSharingOnePortraitDoNotRestartTheSameSpeakerTurn()
        {
            graph.Node("L1").speaker = "A (혼잣말)";
            presentation.speakerPortraits = presentation.speakerPortraits.Concat(new[] { Face("A (혼잣말)", "FACE_A") }).ToArray();
            CreatePlayer(); player.Present(graph.Node("L1"), false, false); player.Tick(0.08f);
            Assert.That(portrait.actorRect.anchoredPosition.y, Is.GreaterThan(home.y));
            player.Present(graph.Node("L2"), false, false); player.Tick(0.16f);
            Assert.That(portrait.actorImage.sprite, Is.SameAs(sprites["FACE_A"]));
            Assert.That(player.IsPlaying, Is.False); AssertAtHome();
            player.Present(graph.Node("L3"), false, false); player.Tick(0.16f);
            Assert.That(portrait.actorImage.sprite, Is.SameAs(sprites["FACE_B"]));
            Assert.That(portrait.actorRect.anchoredPosition.y, Is.GreaterThan(home.y));
        }

        [Test] public void ZeroStrengthAndShotOptOutKeepThePortraitButNeverBounceIt()
        {
            presentation.speakerBounceScenes[0].strength = 0;
            CreatePlayer(); player.Present(graph.Node("L1"), false, false); player.Tick(0.16f);
            Assert.That(portrait.IsVisible, Is.True); Assert.That(player.IsPlaying, Is.False); AssertAtHome();
            player.Dispose(); player = null;
            presentation.speakerBounceScenes[0].strength = 0.35f;
            CreatePlayer(); player.Present(graph.Node("L6"), false, false); player.Tick(0.16f);
            Assert.That(portrait.IsVisible, Is.True); Assert.That(player.IsPlaying, Is.False); AssertAtHome();
        }

        [Test] public void RestoreAndReducedMotionShowTheCardAtItsExactHome()
        {
            CreatePlayer(); player.Present(graph.Node("L1"), false, true); player.Tick(0.16f);
            Assert.That(portrait.IsVisible, Is.True); Assert.That(player.IsPlaying, Is.False); AssertAtHome();
            player.Present(graph.Node("L3"), true, false); player.Tick(0.16f);
            Assert.That(portrait.actorImage.sprite, Is.SameAs(sprites["FACE_B"]));
            Assert.That(player.IsPlaying, Is.False); AssertAtHome();
        }

        [Test] public void ZeroDeltaFreezesPortraitMotionAndFinishNeverAccumulatesTransforms()
        {
            CreatePlayer();
            for (int i = 0; i < 10; i++)
            {
                player.Present(graph.Node(i % 2 == 0 ? "L1" : "L3"), false, false); player.Tick(0.08f);
                var position = portrait.actorRect.anchoredPosition; var scale = portrait.actorRect.localScale;
                player.Tick(0); Assert.That(portrait.actorRect.anchoredPosition, Is.EqualTo(position)); Assert.That(portrait.actorRect.localScale, Is.EqualTo(scale));
                player.FinishAll(); AssertAtHome();
            }
        }

        [Test] public void UnlistedScenesAndExplicitEmptyPortraitScenesHideTheCard()
        {
            CreatePlayer(); player.Present(graph.Node("L5"), false, false);
            Assert.That(portrait.IsVisible, Is.False); AssertAtHome();
            player.Dispose(); player = null;
            presentation.speakerPortraitScenes = Array.Empty<string>();
            CreatePlayer(); player.Present(graph.Node("L1"), false, false);
            Assert.That(portrait.IsVisible, Is.False);
        }

        [Test] public void MisbindingTheCgOrDialogueParentCannotAnimateOrDisableThem()
        {
            var artSprite = view.artImage.sprite;
            portrait.actorRect = view.artImage.rectTransform; portrait.actorImage = view.artImage;
            Assert.That(portrait.Show(view, sprites["FACE_A"], true), Is.False);
            portrait.BeginBounce(1); portrait.Tick(0.16f);
            Assert.That(view.artImage.enabled, Is.True); Assert.That(view.artImage.sprite, Is.SameAs(artSprite));
            Assert.That(view.artImage.rectTransform.localScale, Is.EqualTo(Vector3.one));
            portrait.actorRect = view.dialogueGroup.GetComponent<RectTransform>();
            Assert.That(portrait.Attach(view), Is.False);
        }

        [Test] public void ARealLayeredActorAndPortraitNeverBounceAtTheSameTime()
        {
            presentation.scenes = new[] { new StoryLayeredScene { sceneId = "T01", backgroundArtId = "BG", actors = new[] { new StoryActorPlacement { actorId = "actor", speaker = "A", assetId = "ACTOR", width = 200, height = 500 } } } };
            CreatePlayer(); player.Present(graph.Node("L1"), false, false); player.Tick(0.16f);
            Assert.That(player.ActorTransform("actor").localScale, Is.Not.EqualTo(Vector3.one));
            Assert.That(portrait.IsVisible, Is.False); Assert.That(portrait.IsPlaying, Is.False); AssertAtHome();
        }

        [Test] public void NoPortraitMappingKeepsTheFlattenedCgStatic()
        {
            presentation.speakerPortraits = Array.Empty<StorySpeakerPortraitAsset>();
            CreatePlayer(); player.Present(graph.Node("L1"), false, false); player.Tick(0.16f);
            Assert.That(portrait.IsVisible, Is.False);
            Assert.That(view.artworkRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(player.IsPlaying, Is.False);
        }

        [Test] public void AnAlternateAfterCgUsesTheFlatImageInsteadOfOldLayeredActors()
        {
            presentation.scenes = new[] { new StoryLayeredScene { sceneId = "T01", backgroundArtId = "BG", actors = new[] { new StoryActorPlacement { actorId = "actor", speaker = "A", assetId = "ACTOR", width = 200, height = 500 } } } };
            var after = graph.Node("L2"); after.kind = "cue"; after.artId = "T02";
            CreatePlayer(); player.Present(graph.Node("L1"), false, false);
            view.artImage.sprite = sprites["T02"];
            player.Present(after, false, false);
            Assert.That(view.layeredRoot.gameObject.activeSelf, Is.False);
            Assert.That(view.artImage.enabled, Is.True);
            Assert.That(view.artImage.sprite, Is.SameAs(sprites["T02"]));
            Assert.That(portrait.IsVisible, Is.False);
            Assert.That(player.IsPlaying, Is.False);
        }
    }
}
