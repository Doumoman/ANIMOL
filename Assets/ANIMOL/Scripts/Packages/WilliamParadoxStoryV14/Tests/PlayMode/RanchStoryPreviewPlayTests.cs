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
        private string priorSave;
        private bool hadSave;
        private const string PackageId = "william-paradox.ranch.dialogue-preview";
        [SetUp] public void PreservePlayerCheckpoint()
        {
            hadSave = PlayerPrefs.HasKey(StorySaveStore.Key(PackageId));
            priorSave = PlayerPrefs.GetString(StorySaveStore.Key(PackageId));
        }
        [TearDown] public void RestorePlayerCheckpoint()
        {
            foreach (var runner in UnityEngine.Object.FindObjectsByType<StoryRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None)) runner.persistProgress = false;
            if (hadSave) PlayerPrefs.SetString(StorySaveStore.Key(PackageId), priorSave); else PlayerPrefs.DeleteKey(StorySaveStore.Key(PackageId));
            PlayerPrefs.Save();
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
            // A real focus callback occurs on a change, not before every test click.
            if ((bool)typeof(StoryRunner).GetField("hasFocus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(runner) == value) return;
            typeof(StoryRunner).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runner, new object[] { value });
        }
    }
}
