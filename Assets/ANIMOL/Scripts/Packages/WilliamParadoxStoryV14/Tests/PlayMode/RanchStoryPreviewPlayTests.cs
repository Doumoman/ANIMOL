using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace WilliamParadox.Story.Tests.PlayMode
{
    public sealed class RanchStoryPreviewPlayTests
    {
        private const string ScenePath = "Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity";

        [UnityTest]
        public IEnumerator PreviewButtonsPreserveRescueOrderAndBookResume()
        {
#if UNITY_EDITOR
            SetGameView(1080, 1920);
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
            Assert.That(runner, Is.Not.Null);
            runner.SetReducedMotion(false);
            runner.StartStory(false);
            Focus(runner, true);
            Assert.That(runner.IsPaused, Is.False);
            Assert.That(runner.CurrentNodeId, Is.EqualTo("R01_B1_L001"));
            Assert.That(runner.ValidationErrors, Is.Empty);
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            Assert.That(runner.GetComponentInChildren<CanvasScaler>().referenceResolution,
                Is.EqualTo(new Vector2(1080, 1920)));
            Assert.That(runner.GetComponentsInChildren<StorySafeArea>(true).Length, Is.EqualTo(1));
            Assert.That(runner.view.artImage.sprite.rect.size, Is.EqualTo(new Vector2(941, 1672)));
            Assert.That(runner.view.artImage.GetComponent<StoryCoverImage>(), Is.Not.Null);

            // One click settles a running cue; it cannot also consume the line.
            runner.view.advanceButton.onClick.Invoke();
            Assert.That(runner.CurrentNodeId, Is.EqualTo("R01_B1_L001"));
            yield return new WaitForSecondsRealtime(0.18f);
            runner.view.advanceButton.onClick.Invoke();
            Assert.That(runner.CurrentNodeId, Is.EqualTo("R01_B1_L002"));

            yield return new WaitForSecondsRealtime(0.18f);
            runner.view.pauseButton.onClick.Invoke();
            Assert.That(runner.IsPaused, Is.True);
            runner.view.advanceButton.onClick.Invoke();
            Assert.That(runner.CurrentNodeId, Is.EqualTo("R01_B1_L002"));
            runner.view.pauseButton.onClick.Invoke();
            Focus(runner, false);
            Assert.That(runner.IsPaused, Is.True);
            runner.view.advanceButton.onClick.Invoke();
            Assert.That(runner.CurrentNodeId, Is.EqualTo("R01_B1_L002"));
            Focus(runner, true);
            Assert.That(runner.IsPaused, Is.False);
            runner.SetReducedMotion(true);

            var sawWorkyard = false;
            var sawPudding = false;
            var sawEmptyCup = false;
            var sawNoodle = false;
            var blockedLamp = false;
            for (var steps = 0; steps < 300 && runner.CurrentNode.kind != "credits"; steps++)
            {
                yield return new WaitForSecondsRealtime(0.18f);
                Focus(runner, true);
                Assert.That(runner.IsPaused, Is.False, runner.CurrentNodeId);
                var node = runner.CurrentNode;
                if (node.id == "R04_B1_L001") sawWorkyard = node.artId == "BG14_workyard_day";
                if (node.id == "R04_B2_L001") sawPudding = node.artId == "BG14_home_pudding";
                if (node.id == "R04_B3_L001") sawEmptyCup = node.artId == "R04";
                if (node.id == "R06_B2_L007") sawNoodle = runner.view.CurrentInsetId == "noodle_01";
                if (node.id == "R16_B3_WISH_CONFIRMED")
                    Assert.That(runner.HasFlag("ISOL_WANTS_RETURN"), Is.True);
                if (node.id == "R16_B3_BLOCK_LAMP")
                {
                    Assert.That(node.artId, Is.EqualTo("R15"));
                    Assert.That(runner.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
                    runner.view.advanceButton.onClick.Invoke();
                    Assert.That(runner.CurrentNodeId, Is.EqualTo(node.id));
                    yield return new WaitForSecondsRealtime(0.18f);
                    runner.view.gateButton.onClick.Invoke();
                    blockedLamp = runner.HasFlag("PLAYER_BLOCKS_LAMP");
                }
                else if (node.kind == "gate") runner.view.gateButton.onClick.Invoke();
                else runner.view.advanceButton.onClick.Invoke();
                if (runner.CurrentNodeId == "R16_B4_L001")
                    Assert.That(runner.CurrentNode.artId, Is.EqualTo("R16"));
            }
            Assert.That(runner.CurrentNode.kind, Is.EqualTo("credits"), runner.CurrentNodeId);
            Assert.That(sawWorkyard && sawPudding && sawEmptyCup && sawNoodle && blockedLamp, Is.True);
            Assert.That(runner.HasFlag("ISOL_PRESENT"), Is.True);
            Assert.That(runner.HasFlag("RETURN_PATH_USED"), Is.True);
            var saved = StorySaveStore.Load(runner.Package.packageId);
            Assert.That(saved.nodeId, Is.EqualTo(runner.CurrentNodeId));

            // Reenter the book from its saved checkpoint, then read all six records manually.
            runner.StartStory(true);
            Focus(runner, true);
            Assert.That(runner.CurrentNode.kind, Is.EqualTo("credits"));
            Assert.That(runner.CurrentNode.artId, Is.EqualTo("BG14_epilogue_book"));
            Assert.That(runner.reducedMotion, Is.True);
            Assert.That(runner.view.creditsContinueButton.interactable, Is.False);
            for (var paragraph = 1; paragraph < 6; paragraph++)
            {
                yield return new WaitForSecondsRealtime(0.18f);
                runner.view.advanceButton.onClick.Invoke();
                Assert.That(runner.view.creditsText.text,
                    Does.Contain(runner.CurrentNode.credits[paragraph]));
            }
            Assert.That(runner.view.creditsContinueButton.interactable, Is.False);
            yield return new WaitForSecondsRealtime(3.1f);
            Assert.That(runner.view.creditsContinueButton.interactable, Is.True);
            runner.view.creditsContinueButton.onClick.Invoke();
            Assert.That(runner.CurrentNode.gateId, Is.EqualTo("PREVIEW_FINAL_CONTINUE"));
            yield return new WaitForSecondsRealtime(0.18f);
            runner.view.gateButton.onClick.Invoke();
            Assert.That(runner.CurrentNode.kind, Is.EqualTo("end"));
            Assert.That(runner.HasFlag("PREVIEW_SESSION_FINISHED"), Is.True);
            Assert.That(runner.ValidationErrors, Is.Empty);
            runner.RestartStory();
#else
            Assert.Ignore("The generated preview scene is inspected in the Unity Editor.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator TallScreenKeepsTheCoveredArtworkWidthVisible()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            SetGameView(1080, 2400);
            try
            {
                yield return null;
                yield return null;
                Assert.That(Screen.width, Is.EqualTo(1080));
                Assert.That(Screen.height, Is.EqualTo(2400));
                var runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
                Assert.That(runner, Is.Not.Null);
                Assert.That(runner.GetComponentsInChildren<StorySafeArea>(true).Length, Is.EqualTo(1));
                var cover = runner.view.artImage.GetComponent<StoryCoverImage>();
                Assert.That(cover.viewport.GetComponent<StoryArtViewport>(), Is.Not.Null);
                Assert.That(cover.viewport.rect.width / cover.viewport.rect.height,
                    Is.EqualTo(9f / 16f).Within(0.001f));
                Assert.That(runner.view.artImage.rectTransform.rect.width / cover.viewport.rect.width,
                    Is.LessThan(1.01f));
                Assert.That(runner.ValidationErrors, Is.Empty);
            }
            finally { SetGameView(1080, 1920); }
#else
            Assert.Ignore("The generated preview scene is inspected in the Unity Editor.");
            yield break;
#endif
        }

        private static void SetGameView(int width, int height)
        {
            Type setup = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                setup = assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView");
                if (setup != null) break;
            }
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { width, height, "Story preview QA" });
        }

        private static void Focus(StoryRunner runner, bool value)
        {
            typeof(StoryRunner).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runner, new object[] { value });
        }
    }
}
