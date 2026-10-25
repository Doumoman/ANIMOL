using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story.Tests
{
    public sealed class RuntimeStoryPresentationTests
    {
        private GameObject root;
        private StoryView view;
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("RuntimeStoryPresentationTest");
            view = root.AddComponent<StoryView>();
            view.artworkRoot = Child("ArtworkRoot");
            view.artworkGroup = view.artworkRoot.gameObject.AddComponent<CanvasGroup>();
            view.dimImage = Child("Dim").gameObject.AddComponent<Image>();
            view.SetDim(0);
        }
        private RectTransform Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(root.transform, false); return child;
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
        [Test]
        public void ZeroPresentationDeltaFreezesAStartedCue()
        {
            var player = new StoryCuePlayer(view);
            player.Begin(new[] { new StoryCue { type = "dim", duration = 2, amount = 1 } }, false);
            player.Tick(0.5f); float frozen = view.DimAlpha;
            for (int i = 0; i < 20; i++) player.Tick(0);
            Assert.That(view.DimAlpha, Is.EqualTo(frozen).Within(0.0001f));
            Assert.That(player.IsPlaying, Is.True);
        }
        [Test]
        public void ErasureSwapRemainsPendingUntilItsAuthoredDelay()
        {
            var player = new StoryCuePlayer(view);
            player.Begin(new[] { new StoryCue { type = "swapArt", assetId = "AFTER", delay = 0.22f } }, false);
            player.Tick(0.15f);
            Assert.That(player.HasPendingSwapArt, Is.True);
            player.Tick(0.08f);
            Assert.That(player.HasPendingSwapArt, Is.False);
            Assert.That(player.IsPlaying, Is.False);
        }
        [Test]
        public void LegacyWholeFrameShakeIsRejectedAndNeverMovesDialogue()
        {
            var dialogue = Child("DialogueRoot"); dialogue.anchoredPosition = new Vector2(13, -400);
            view.artworkRoot.anchoredPosition = new Vector2(5, 8);
            var player = new StoryCuePlayer(view);
            player.Begin(new[] { new StoryCue { type = "shake", duration = 1, amount = 12 } }, false);
            Assert.That(player.IsPlaying, Is.False);
            player.Tick(0.2f); player.FinishAll();
            Assert.That(view.artworkRoot.anchoredPosition, Is.EqualTo(new Vector2(5, 8)));
            Assert.That(dialogue.anchoredPosition, Is.EqualTo(new Vector2(13, -400)));
            Assert.That(player.IsPlaying, Is.False);
        }
        [Test]
        public void ReducedMotionSettlesDelayedZoomWithoutWaitingOrAnimating()
        {
            var player = new StoryCuePlayer(view);
            player.Begin(new[] { new StoryCue { type = "zoom", duration = 2, delay = 5, amount = 1.04f } }, true);
            Assert.That(view.artworkRoot.localScale.x, Is.EqualTo(1.04f).Within(0.0001f));
            Assert.That(player.IsPlaying, Is.False);
        }
        [Test]
        public void RestoringAnInitialInsetCheckpointShowsItsAuthoredFinalStateWithoutReplaying()
        {
            view.insetRoot = Child("InsetRoot");
            view.insetGroup = view.insetRoot.gameObject.AddComponent<CanvasGroup>();
            // No Image is needed: this test checks presentation state, not Resources import.
            var cue = new StoryCue { type = "inset", target = "inset", assetId = "panel_01", delay = 1, duration = 1, scale = 1 };
            var player = new StoryCuePlayer(view);
            var earlySave = new StorySaveData { insetVisible = false, artScale = 1, insetScale = 1 };
            player.RestoreSettled(new[] { cue }, earlySave);
            Assert.That(view.insetGroup.alpha, Is.EqualTo(1));
            Assert.That(view.CurrentInsetId, Is.EqualTo("panel_01"));
            Assert.That(player.IsPlaying, Is.False);
        }
    }
}
