using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class EchoTheaterPlayTests
    {
        private const string ScenePath = "Assets/WilliamParadoxStoryV14/Generated/EchoTheaterStory.unity";

        [UnityTest]
        public IEnumerator CaptureFirstFrameAtMobileAndDesktopSizes()
        {
#if UNITY_EDITOR
            string output = System.IO.Path.GetFullPath("Logs/EchoTheater");
            System.IO.Directory.CreateDirectory(output);
            string key = StorySaveStore.Key("echo-theater-22");
            bool hadSave = PlayerPrefs.HasKey(key);
            string oldSave = PlayerPrefs.GetString(key);
            try
            {
                SetGameView(1080, 1920);
                EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
                runner.StartStory(false);
                Focus(runner, true);
                Assert.That(runner.CurrentNodeId, Is.EqualTo("T01_01"));
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, "first-frame-mobile.png"));
                yield return null;
                SetGameView(1600, 900);
                yield return null;
                yield return null;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, "first-frame-desktop.png"));
                yield return null;
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetString(key, oldSave); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                SetGameView(1080, 1920);
            }
#else
            Assert.Ignore("Editor Game View screenshot capture is unavailable in a player build.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GeneratedScenePlaysEveryLineAndRestoresBothErasureStates()
        {
#if UNITY_EDITOR
            string key = StorySaveStore.Key("echo-theater-22");
            bool hadSave = PlayerPrefs.HasKey(key);
            string oldSave = PlayerPrefs.GetString(key);
            try
            {
                SetGameView(1080, 1920);
                EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
                Assert.That(runner, Is.Not.Null);
                runner.StartStory(false);
                Focus(runner, true);
                Assert.That(runner.CurrentNodeId, Is.EqualTo("T01_01"));
                Assert.That(runner.view.artImage.sprite, Is.Not.Null);
                Assert.That(runner.view.artImage.preserveAspect, Is.True);
                Assert.That(runner.ValidationErrors, Is.Empty);
                Assert.That(runner.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(runner.GetComponentsInChildren<StorySafeArea>(true).Length, Is.EqualTo(1));
                Assert.That(runner.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true).Length, Is.EqualTo(1));
                Assert.That(runner.GetComponentsInChildren<AudioListener>(true).Length, Is.EqualTo(1));
                Assert.That(runner.GetComponent<StoryGameplayBridge>().HasAdapters, Is.False);
                runner.TogglePause();
                Assert.That(runner.IsPaused, Is.True);
                runner.view.advanceButton.onClick.Invoke();
                Assert.That(runner.CurrentNodeId, Is.EqualTo("T01_01"));
                runner.TogglePause();
                Focus(runner, false);
                Assert.That(runner.IsPaused, Is.True);
                Focus(runner, true);
                Assert.That(runner.IsPaused, Is.False);
                typeof(StoryRunner).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(runner, new object[] { true });
                Assert.That(runner.IsPaused, Is.True);
                typeof(StoryRunner).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(runner, new object[] { false });
                Assert.That(runner.IsPaused, Is.False);
                runner.enabled = false;
                yield return new WaitForSecondsRealtime(0.2f);
                runner.view.advanceButton.onClick.Invoke();
                Assert.That(runner.CurrentNodeId, Is.EqualTo("T01_01"));
                runner.enabled = true;
                runner.view.advanceButton.onClick.Invoke();
                Assert.That(runner.CurrentNodeId, Is.EqualTo("T01_01"), "Reenable has an input grace period.");

                SetGameView(1600, 900);
                yield return null;
                yield return null;
                var portrait = runner.GetComponentInChildren<AspectRatioFitter>(true).transform as RectTransform;
                Assert.That(portrait.rect.width / portrait.rect.height, Is.EqualTo(9f / 16f).Within(0.01f));
                SetGameView(1080, 1920);
                yield return null;
                yield return null;

                int completed = 0;
                runner.onCompleted.AddListener(() => completed++);
                var visited = new HashSet<string>(StringComparer.Ordinal);
                bool piko = false, seri = false, sawPaging = false;
                for (int step = 0; step < 420 && runner.CurrentNode.kind != "end"; step++)
                {
                    yield return new WaitForSecondsRealtime(0.17f);
                    var node = runner.CurrentNode;
                    visited.Add(node.id);
                    if (node.kind == "line")
                    {
                        Assert.That(runner.view.bodyText.text, Is.EqualTo(node.text), node.id);
                        Assert.That(runner.view.bodyText.fontSize, Is.EqualTo(40), node.id);
                        runner.view.bodyText.ForceMeshUpdate();
                        if (!sawPaging && node.text.Length >= 50)
                        {
                            // The authored 1080x1920 tray fits every supplied turn.
                            // Constrain it to exercise the real paging/save path.
                            var textRect = runner.view.bodyText.rectTransform;
                            var oldMin = textRect.offsetMin;
                            var oldMax = textRect.offsetMax;
                            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 240);
                            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90);
                            runner.view.bodyText.ForceMeshUpdate();
                            Assert.That(runner.view.bodyText.textInfo.pageCount, Is.GreaterThan(1));
                            runner.view.advanceButton.onClick.Invoke();
                            Assert.That(runner.CurrentNodeId, Is.EqualTo(node.id), "A page turn stays in its story node.");
                            Assert.That(runner.view.bodyText.pageToDisplay, Is.EqualTo(2));
                            runner.StartStory(true);
                            Focus(runner, true);
                            Assert.That(runner.CurrentNodeId, Is.EqualTo(node.id));
                            Assert.That(runner.view.bodyText.pageToDisplay, Is.EqualTo(2));
                            textRect.offsetMin = oldMin;
                            textRect.offsetMax = oldMax;
                            sawPaging = true;
                            continue;
                        }
                    }
                    if (node.id == "T16_16" && !piko)
                    {
                        Assert.That(runner.HasFlag("PICO_ERASED"), Is.True);
                        runner.view.advanceButton.onClick.Invoke();
                        Assert.That(runner.CurrentNodeId, Is.EqualTo(node.id), "Erasure cannot be skipped before AFTER.");
                        yield return new WaitForSecondsRealtime(0.1f);
                        Assert.That(runner.view.artImage.sprite.name, Is.EqualTo("T16_AFTER"));
                        Assert.That(StorySaveStore.Load(runner.Package.packageId).nodeId, Is.EqualTo(node.id));
                        runner.StartStory(true);
                        Focus(runner, true);
                        Assert.That(runner.view.artImage.sprite.name, Is.EqualTo("T16_AFTER"));
                        piko = true;
                        continue;
                    }
                    if (node.id == "T17_18" && !seri)
                    {
                        Assert.That(runner.HasFlag("SERI_ERASED"), Is.True);
                        runner.view.advanceButton.onClick.Invoke();
                        Assert.That(runner.CurrentNodeId, Is.EqualTo(node.id));
                        yield return new WaitForSecondsRealtime(0.1f);
                        Assert.That(runner.view.artImage.sprite.name, Is.EqualTo("T17_AFTER"));
                        runner.StartStory(true);
                        Focus(runner, true);
                        Assert.That(runner.view.artImage.sprite.name, Is.EqualTo("T17_AFTER"));
                        seri = true;
                        continue;
                    }
                    if (node.id == "T22_REST")
                    {
                        Assert.That(runner.view.dialogueGroup.alpha, Is.Zero);
                        Assert.That(runner.HasFlag("PICO_ERASED") && runner.HasFlag("SERI_ERASED"), Is.True);
                    }
                    runner.view.advanceButton.onClick.Invoke();
                }
                Assert.That(runner.CurrentNode.kind, Is.EqualTo("end"), runner.CurrentNodeId);
                Assert.That(visited.Count, Is.GreaterThanOrEqualTo(245));
                Assert.That(piko && seri, Is.True);
                Assert.That(sawPaging, Is.True);
                Assert.That(completed, Is.EqualTo(1));
                runner.StartStory(true);
                Assert.That(completed, Is.EqualTo(1), "Restoring the end must not issue completion again.");
                Assert.That(runner.ValidationErrors, Is.Empty);
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetString(key, oldSave); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                SetGameView(1080, 1920);
            }
#else
            Assert.Ignore("Generated scene PlayMode verification runs in the Unity Editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private static void SetGameView(int width, int height)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView");
                if (type == null) continue;
                type.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, new object[] { width, height, "Echo Theater QA" });
                return;
            }
            Assert.Fail("PortraitGameView resolution helper is missing.");
        }
        private static void Focus(StoryRunner runner, bool value)
        {
            typeof(StoryRunner).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runner, new object[] { value });
        }
#endif
    }
}
