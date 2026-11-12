using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using William.NotebookUI;
using WilliamParadox.Story;
using ANIMOL.Puzzle;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookUiPlayTests
    {
        string[] portraitPreferenceKeys;
        bool[] portraitPreferencePresent;
        string[] portraitPreferenceValues;
        NotebookCampaignPreview portraitPreview;
        bool portraitAllStagesCleared;
        [SetUp] public void IsolatePortraitProgress()
        {
            if (TestContext.CurrentContext.Test.MethodName != nameof(ActivePortraitSceneOpensSingleMapPurchaseAndSettings)) return;
            // This case verifies uncleared/locked star artwork in normal progress mode.
            portraitPreview = NotebookCampaignPreview.Current;
            if (portraitPreview)
            {
                portraitAllStagesCleared = portraitPreview.allStagesCleared;
                portraitPreview.allStagesCleared = false;
            }
            portraitPreferenceKeys = new[] { "WilliamNotebookUI.Wallet", "WilliamNotebookUI.Preferences",
                NotebookProjectGateway.MapClearKey("SW00"), NotebookProjectGateway.MapClearKey("SW01"),
                "ANIMOL.Starway.Cleared.SW01" };
            portraitPreferencePresent = portraitPreferenceKeys.Select(PlayerPrefs.HasKey).ToArray();
            portraitPreferenceValues = portraitPreferenceKeys.Select((key, i) => i < 2
                ? PlayerPrefs.GetString(key) : PlayerPrefs.GetInt(key).ToString()).ToArray();
            foreach (string key in portraitPreferenceKeys) PlayerPrefs.DeleteKey(key);
        }
        [UnityTearDown] public IEnumerator RestorePortraitProgress()
        {
            if (portraitPreferenceKeys == null) yield break;
            try
            {
                var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
                var canvas = root ? root.GetComponentInParent<Canvas>(true) : null;
                if (canvas) UnityEngine.Object.Destroy(canvas.rootCanvas.gameObject);
                yield return null;
            }
            finally
            {
                if (portraitPreview) portraitPreview.allStagesCleared = portraitAllStagesCleared;
                for (int i = 0; i < portraitPreferenceKeys.Length; i++)
                    if (!portraitPreferencePresent[i]) PlayerPrefs.DeleteKey(portraitPreferenceKeys[i]);
                    else if (i < 2) PlayerPrefs.SetString(portraitPreferenceKeys[i], portraitPreferenceValues[i]);
                    else PlayerPrefs.SetInt(portraitPreferenceKeys[i], int.Parse(portraitPreferenceValues[i]));
                PlayerPrefs.Save(); portraitPreferenceKeys = null;
            }
        }
        [UnityTest] public IEnumerator MainArtworkSwipesThroughCharacterIllustrationsEveryFiveVisibleSeconds()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.worlds.Length, Is.EqualTo(4));
            foreach (var art in root.worlds)
                foreach (var character in art.characters)
                    StringAssert.Contains("Assets/ANIMOL/Art/Characters/", AssetDatabase.GetAssetPath(character));
            var timer = typeof(NotebookRoot).GetField("mainArtworkElapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            var incoming = typeof(NotebookRoot).GetField("incomingWorldImage", BindingFlags.Instance | BindingFlags.NonPublic);
            typeof(NotebookRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { true });
            timer.SetValue(root, 0f);
            int first = Array.FindIndex(root.worlds, art => art.background == root.worldImage.sprite);
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            Assert.That(root.worlds[first].characters, Does.Contain(root.characterImage.sprite));
            Sprite firstCharacter = root.characterImage.sprite;
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                yield return new WaitForSecondsRealtime(4.75f);
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[first].background));
                Assert.That(root.characterImage.sprite, Is.EqualTo(firstCharacter));
                yield return new WaitForSecondsRealtime(.45f);
                int second = (first + 1) % root.worlds.Length;
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[first].background));
                Assert.That((UnityEngine.UI.Image)incoming.GetValue(root), Is.Not.Null);
                Assert.That(((UnityEngine.UI.Image)incoming.GetValue(root)).sprite, Is.EqualTo(root.worlds[second].background));
                Assert.That(root.worldImage.rectTransform.anchoredPosition.x, Is.LessThan(0f));
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(incoming.GetValue(root), Is.Null);
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[second].background));
                Assert.That(root.worlds[second].characters, Does.Contain(root.characterImage.sprite));
                Assert.That(root.characterImage.sprite, Is.Not.EqualTo(firstCharacter));

                timer.SetValue(root, 4.9f);
                root.OpenStages();
                yield return new WaitForSecondsRealtime(.35f);
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[second].background));
                root.OpenMain();
                yield return new WaitForSecondsRealtime(.75f);
                int third = (second + 1) % root.worlds.Length;
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[third].background));
                Assert.That(root.worlds[third].characters, Does.Contain(root.characterImage.sprite));

                timer.SetValue(root, 4.9f);
                root.OpenSettings();
                yield return new WaitForSecondsRealtime(.35f);
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[third].background));
                root.OpenMain();
                yield return new WaitForSecondsRealtime(.8f);
                int fourth = (third + 1) % root.worlds.Length;
                Assert.That(root.worldImage.sprite, Is.EqualTo(root.worlds[fourth].background));
                Assert.That(root.worlds[fourth].characters, Does.Contain(root.characterImage.sprite));
            }
            finally { Time.timeScale = previousTimeScale; }
#else
            Assert.Ignore("Unity Editor Play Mode is required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ActivePortraitSceneOpensSingleMapPurchaseAndSettings()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .FirstOrDefault(t => t != null);
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 445, 773, "Notebook UI portrait test" });
            yield return null; yield return null;
            Assert.That(Screen.width, Is.EqualTo(445)); Assert.That(Screen.height, Is.EqualTo(773));
            var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(UnityEngine.Object.FindObjectsByType<NotebookRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(UnityEngine.Object.FindObjectsByType<ANIMOL.Menu.MainLobbyController>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(root.nodes.Length, Is.EqualTo(80));
            foreach (var size in new[] { new Vector2Int(360, 640), new Vector2Int(445, 773),
                new Vector2Int(390, 844), new Vector2Int(1440, 900) })
            {
                if (size.x < size.y)
                    setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { size.x, size.y, "Notebook UI layout test" });
                else SelectWidePcResolution(size.x, size.y);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width, Is.EqualTo(size.x)); Assert.That(Screen.height, Is.EqualTo(size.y));
                foreach (var button in new[] { root.profileButton, root.muteButton, root.settingsButton,
                    root.campaignButton, root.battleButton, root.customButton })
                    AssertOnScreen(button.GetComponent<RectTransform>(), size, button.name);
                Assert.That(root.characterImage.preserveAspect, Is.True);
                AssertOnScreen(root.characterImage.rectTransform, size, "full character area");
            }
            root.OpenStages();
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.mainScreen.activeSelf, Is.False);
            Assert.That(root.MapSelectionVisible, Is.False);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            Assert.That(root.mapScroll.content.anchoredPosition.y, Is.GreaterThanOrEqualTo(0));
            AssertOnScreen(root.mapBack.GetComponent<RectTransform>(), new Vector2Int(Screen.width, Screen.height), "map back");
            AssertOnScreen(root.telescopeButton.GetComponent<RectTransform>(), new Vector2Int(Screen.width, Screen.height), "telescope");
            root.nodes.First(v => v.stageId == "SW01").button.onClick.Invoke();
            Assert.That(root.MapSelectionVisible, Is.True);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(.30f);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.True);
            Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.False);
            var selectedNode = root.nodes.First(v => v.stageId == "SW01");
            var secondNode = root.nodes.First(v => v.stageId == "SW02");
            Assert.That(selectedNode.star.sprite, Is.EqualTo(root.mapStarFrames[(1 * 3 + NotebookRoot.MapStarVariant("SW01")) * 4]));
            yield return new WaitForSecondsRealtime(.22f);
            AssertOnScreen(root.stageSheet, new Vector2Int(Screen.width, Screen.height), "stage sheet");
            typeof(NotebookRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { true });
            yield return new WaitForSecondsRealtime(.28f);
            Assert.That(selectedNode.star.sprite, Is.EqualTo(root.mapStarFrames[(1 * 3 + NotebookRoot.MapStarVariant("SW01")) * 4 + 1]));
            Assert.That(secondNode.star.sprite, Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("SW02")) * 4]));
            secondNode.button.onClick.Invoke();
            Assert.That(root.modal.activeSelf, Is.False);
            Assert.That(selectedNode.star.sprite, Is.EqualTo(root.mapStarFrames[(0 * 3 + NotebookRoot.MapStarVariant("SW01")) * 4]));
            Assert.That(secondNode.star.sprite, Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("SW02")) * 4]));
            root.SetReducedMotion(true);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(secondNode.star.sprite, Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("SW02")) * 4]));
            foreach (var size in new[] { new Vector2Int(360, 640), new Vector2Int(390, 844),
                new Vector2Int(445, 773), new Vector2Int(1440, 900) })
            {
                if (size.x < size.y)
                    setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { size.x, size.y, "Neon map QA" });
                else SelectWidePcResolution(size.x, size.y);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                AssertOnScreen(root.mapBack.GetComponent<RectTransform>(), size, "map back");
                AssertOnScreen(root.telescopeButton.GetComponent<RectTransform>(), size, "telescope");
                AssertOnScreen(root.stageSheet, size, "neon selection sheet");
                Assert.That(root.mapScroll.viewport.rect.height,
                    Is.EqualTo(root.stageScreen.GetComponent<RectTransform>().rect.height).Within(1f));
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "NeonMap_QA");
                Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "Map_" + size.x + "x" + size.y + ".png"));
                yield return null;
            }
            root.mapScroll.verticalNormalizedPosition = 0f;
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            AssertOnScreen(root.nodes.First(v => v.stageId == "sea-20").button.GetComponent<RectTransform>(),
                new Vector2Int(Screen.width, Screen.height), "last constellation star");
            root.mapScroll.verticalNormalizedPosition = 1f;
            root.SetReducedMotion(false);
            typeof(NotebookRoot).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { true });
            Assert.That(secondNode.star.sprite, Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("SW02")) * 4]));
            typeof(NotebookRoot).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { false });
            typeof(NotebookRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { false });
            Assert.That(secondNode.star.sprite, Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("SW02")) * 4]));
            typeof(NotebookRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { true });
            var model = (NotebookBmModel)typeof(NotebookRoot)
                .GetField("model", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            var savedClears = model.State.clearedStageIds.ToArray();
            try
            {
                model.State.clearedStageIds.Clear();
                model.State.clearedStageIds.Add("SW01");
                typeof(NotebookRoot).GetMethod("RefreshMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(root, null);
                var clearedNode = root.nodes.First(v => v.stageId == "SW01");
                Assert.That(clearedNode.star.sprite, Is.EqualTo(root.mapStarFrames[(2 * 3 + NotebookRoot.MapStarVariant("SW01")) * 4]));
                Assert.That(clearedNode.locked.gameObject.activeSelf, Is.False);
                Assert.That(clearedNode.number.gameObject.activeSelf, Is.True);
                Assert.That(clearedNode.label.text, Is.EqualTo("1-01"));
                selectedNode.button.onClick.Invoke();
                Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.True);
                var firstEdge = root.edges.First(e => e.from == "SW01" && e.to == "SW02");
                Assert.That(firstEdge.image.sprite, Is.EqualTo(firstEdge.future));
                model.State.clearedStageIds.Add("SW02");
                typeof(NotebookRoot).GetMethod("RefreshMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(root, null);
                Assert.That(firstEdge.image.sprite, Is.EqualTo(firstEdge.cleared));
                var secondEdge = root.edges.First(e => e.from == "SW02" && e.to == "SW03");
                Assert.That(secondEdge.image.sprite, Is.EqualTo(secondEdge.future));
            }
            finally
            {
                model.State.clearedStageIds.Clear();
                model.State.clearedStageIds.AddRange(savedClears);
                typeof(NotebookRoot).GetMethod("RefreshMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(root, null);
            }
            root.nodes.First(v => v.stageId == "observatory-01").button.onClick.Invoke();
            Assert.That(root.modal.activeSelf, Is.False);
            Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.False);
            root.launchButton.onClick.Invoke();
            Assert.That(root.modal.activeSelf, Is.True);
            Assert.That(root.nodes.First(v => v.stageId == "observatory-01").star.sprite,
                Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("observatory-01")) * 4]));
            Assert.That(root.modalPrimary.interactable, Is.False);
            Assert.That(root.gateway.CanRestorePurchases, Is.False);
            Assert.That(root.gateway.CanShowRewardedAd, Is.False);
            Assert.That(root.gateway.CanApproveStageEntry, Is.False);
            root.modalClose.onClick.Invoke();
            root.mapBack.onClick.Invoke();
            Vector2 settingsRest = root.settingsPanel.anchoredPosition;
            root.settingsButton.onClick.Invoke();
            Assert.That(root.settingsScreen.activeSelf, Is.True);
            Assert.That(root.profileButton.gameObject.activeSelf, Is.False);
            Assert.That(root.settingsScreen.transform.Find("Settings Title Static Plaque"), Is.Null);
            Assert.That(root.settingsScreen.transform.Find("Settings Title"), Is.Null);
            Assert.That(root.settingsPanel.anchoredPosition.y, Is.GreaterThan(settingsRest.y));
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(root.settingsPanel.anchoredPosition.y, Is.EqualTo(settingsRest.y).Within(.1f));
            foreach (var slider in new[] { root.bgmSlider, root.sfxSlider })
            {
                Assert.That(slider.handleRect.name, Is.EqualTo("Slider Handle Anchor"));
                Assert.That(slider.handleRect.rect.width, Is.EqualTo(76f).Within(.1f));
                Assert.That(slider.handleRect.rect.height, Is.EqualTo(76f).Within(.1f));
                var thumb = slider.targetGraphic as UnityEngine.UI.Image;
                Assert.That(thumb, Is.Not.Null);
                Assert.That(thumb.rectTransform.parent, Is.EqualTo(slider.handleRect));
                Assert.That(thumb.rectTransform.rect.size, Is.EqualTo(new Vector2(76f, 76f)));
                Assert.That(thumb.preserveAspect, Is.True);
            }
            float originalBgm = root.bgmSlider.value;
            float changedBgm = originalBgm > .5f ? .25f : .75f;
            root.bgmSlider.value = changedBgm;
            var saved = JsonUtility.FromJson<NotebookPreferences>(PlayerPrefs.GetString("WilliamNotebookUI.Preferences"));
            Assert.That(saved.bgm, Is.EqualTo(changedBgm).Within(.001f));
            root.bgmSlider.value = originalBgm;
            root.settingsBack.onClick.Invoke();
            Assert.That(root.settingsScreen.activeSelf, Is.True);
            Assert.That(root.settingsScreenGroup.blocksRaycasts, Is.False);
            Assert.That(root.mainInputGroup.blocksRaycasts, Is.True);
            yield return new WaitForSecondsRealtime(.065f);
            Assert.That(root.settingsPanel.anchoredPosition.y, Is.GreaterThan(settingsRest.y));
            yield return new WaitForSecondsRealtime(.11f);
            Assert.That(root.settingsPanel.anchoredPosition.y, Is.LessThan(settingsRest.y));
            yield return new WaitForSecondsRealtime(.11f);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(root.settingsScreen.activeSelf, Is.False);
            Assert.That(root.profileButton.gameObject.activeSelf, Is.True);
            Assert.That(root.settingsPanel.anchoredPosition, Is.EqualTo(settingsRest));
            root.SetReducedMotion(true);
            root.OpenSettings();
            Assert.That(root.settingsPanel.anchoredPosition, Is.EqualTo(settingsRest));
            root.OpenMain();
            Assert.That(root.settingsScreen.activeSelf, Is.False);
            root.SetReducedMotion(false);
            root.OpenSettings();
            root.OpenMain();
            Assert.That(root.settingsPanel.anchoredPosition.y, Is.GreaterThan(settingsRest.y));
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(root.settingsScreen.activeSelf, Is.False);
#else
            Assert.Ignore("Unity Editor scene and portrait GameView are required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator CampaignSkyTransitionUsesActiveButtonAndSettlesAcrossInterruptions()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            var model = (NotebookBmModel)typeof(NotebookRoot).GetField("model", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            var entryIds = model.State.entryIds.ToArray();
            var rewardIds = model.State.rewardIds.ToArray();
            var clearedIds = model.State.clearedStageIds.ToArray();
            var ownedThemes = model.State.ownedThemes.ToArray();
            int startingBalance = model.State.balance;
            var transition = root.campaignSkyTransition;
            Assert.That(transition, Is.Not.Null);
            Assert.That(root.stageScreen.name, Is.EqualTo("Constellation Neon Map"));
            Assert.That(root.mapStarFrames.Length, Is.EqualTo(48));
            Assert.That(root.campaignButton.onClick.GetPersistentEventCount(), Is.Zero);
            Assert.That(transition.destinationTexture, Is.Not.Null);
            Assert.That(transition.transitionShader, Is.Not.Null);
            Assert.That(transition.transitionShader.isSupported, Is.True);
            var overlay = transition.GetComponent<UnityEngine.UI.RawImage>();
            var canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            var scope = root.GetComponent<CanvasGroup>();
            Assert.That(transition.transform.parent, Is.EqualTo(canvas.transform));
            Assert.That(transition.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(transition.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(overlay.enabled, Is.False);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            var focus = typeof(NotebookRoot).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic);
            focus.Invoke(root, new object[] { true });

            root.campaignButton.onClick.Invoke();
            root.campaignButton.onClick.Invoke();
            Assert.That(root.CampaignTransitionRunning, Is.True);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(root.stageScreen.activeSelf, Is.False);
            Assert.That(scope.interactable || scope.blocksRaycasts, Is.False);
            Assert.That(overlay.enabled && overlay.raycastTarget, Is.True);
            root.OpenSettings(); root.OpenStages();
            Assert.That(root.settingsScreen.activeSelf || root.stageScreen.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(.24f);
            Assert.That(transition.Progress, Is.LessThan(NotebookCampaignSkyTransition.MapCommitProgress));
            Assert.That(root.stageScreen.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(.52f);
            Assert.That(transition.HasCommitted, Is.True);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.MapSelectionVisible, Is.False);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            Assert.That(overlay.enabled, Is.True);
            focus.Invoke(root, new object[] { false });
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(overlay.enabled || overlay.raycastTarget, Is.False);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            Assert.That(typeof(NotebookCampaignSkyTransition).GetField("ownedMaterial", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(transition), Is.Null);
            focus.Invoke(root, new object[] { true });
            root.mapBack.onClick.Invoke();
            Assert.That(root.mainScreen.activeSelf, Is.True);

            root.campaignButton.onClick.Invoke();
            Assert.That(root.CampaignTransitionRunning, Is.True);
            focus.Invoke(root, new object[] { false });
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(root.stageScreen.activeSelf, Is.False);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            focus.Invoke(root, new object[] { true });

            var shader = transition.transitionShader;
            transition.transitionShader = null;
            root.campaignButton.onClick.Invoke();
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.CampaignTransitionRunning, Is.False);
            transition.transitionShader = shader;
            root.OpenMain();
            root.SetReducedMotion(true);
            root.campaignButton.onClick.Invoke();
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.CampaignTransitionRunning, Is.False);
            root.SetReducedMotion(false);
            root.OpenMain();
            root.campaignButton.onClick.Invoke();
            Assert.That(root.CampaignTransitionRunning, Is.True);
            transition.transitionShader = null;
            yield return null;
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            transition.transitionShader = shader;
            root.OpenMain();
            root.campaignButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.18f);
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.mainScreen.activeSelf, Is.False);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            Assert.That(overlay.enabled || overlay.raycastTarget, Is.False);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            root.OpenMain();
            var destination = transition.destinationTexture;
            transition.destinationTexture = null;
            root.campaignButton.onClick.Invoke();
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.CampaignTransitionRunning, Is.False);
            transition.destinationTexture = destination;
            root.OpenMain();
            var pause = typeof(NotebookRoot).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic);
            root.campaignButton.onClick.Invoke();
            pause.Invoke(root, new object[] { true });
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            pause.Invoke(root, new object[] { false });
            root.campaignButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.75f);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            pause.Invoke(root, new object[] { true });
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            pause.Invoke(root, new object[] { false });
            root.OpenMain();
            root.campaignButton.onClick.Invoke();
            root.enabled = false;
            Assert.That(root.CampaignTransitionRunning, Is.False);
            Assert.That(root.mainScreen.activeSelf, Is.True);
            Assert.That(scope.interactable && scope.blocksRaycasts, Is.True);
            root.enabled = true;
            CollectionAssert.AreEqual(entryIds, model.State.entryIds);
            CollectionAssert.AreEqual(rewardIds, model.State.rewardIds);
            CollectionAssert.AreEqual(clearedIds, model.State.clearedStageIds);
            CollectionAssert.AreEqual(ownedThemes, model.State.ownedThemes);
            Assert.That(model.State.balance, Is.GreaterThanOrEqualTo(startingBalance));
#else
            Assert.Ignore("Unity Editor Play Mode is required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator CampaignSkyOverlayCoversPhysicalCanvasAtPortraitAndWideSizes()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .FirstOrDefault(t => t != null);
            var overlay = root.campaignSkyTransition.GetComponent<RectTransform>();
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "CampaignSkyQA");
            Directory.CreateDirectory(folder);
            foreach (var size in new[] { new Vector2Int(360, 640), new Vector2Int(390, 844),
                new Vector2Int(445, 773), new Vector2Int(1440, 900) })
            {
                if (size.x < size.y)
                    setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { size.x, size.y, "Campaign sky overlay" });
                else SelectWidePcResolution(size.x, size.y);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width, Is.EqualTo(size.x)); Assert.That(Screen.height, Is.EqualTo(size.y));
                var corners = new Vector3[4]; overlay.GetWorldCorners(corners);
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                Assert.That(bottomLeft.x, Is.EqualTo(0).Within(1));
                Assert.That(bottomLeft.y, Is.EqualTo(0).Within(1));
                Assert.That(topRight.x, Is.EqualTo(size.x).Within(1));
                Assert.That(topRight.y, Is.EqualTo(size.y).Within(1));
                root.campaignButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.27f);
                Assert.That(root.CampaignTransitionRunning, Is.True);
                Assert.That(root.stageScreen.activeSelf, Is.False);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "Sky_" + size.x + "x" + size.y + ".png"));
                yield return null;
                root.campaignSkyTransition.Cancel();
                Assert.That(root.mainScreen.activeSelf, Is.True);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            }
#else
            Assert.Ignore("Unity Editor GameView is required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ClearedStarwayReplayAndEntryUseRealAssetsWithoutReplayCharges()
        {
#if UNITY_EDITOR
            const string clearKey = "ANIMOL.Starway.MapCleared.SW00";
            const string walletKey = "WilliamNotebookUI.Wallet";
            const string storyKey = "WilliamParadox.Story.V14.william-paradox.ranch.dialogue-preview";
            bool hadClear = PlayerPrefs.HasKey(clearKey), hadWallet = PlayerPrefs.HasKey(walletKey);
            int oldClear = PlayerPrefs.GetInt(clearKey);
            string oldWallet = PlayerPrefs.GetString(walletKey);
            bool hadStory = PlayerPrefs.HasKey(storyKey);
            string oldStory = PlayerPrefs.GetString(storyKey);
            try
            {
                PlayerPrefs.SetInt(clearKey, 1);
                EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                    new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var root = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
                Assert.That(root, Is.Not.Null);
                var gateway = root.gateway as NotebookProjectGateway;
                Assert.That(gateway, Is.Not.Null);
                Assert.That(gateway.starwayMaps.Length, Is.EqualTo(20));
                Assert.That(gateway.shortestPathCatalog, Is.Not.Null);
                Assert.That(gateway.starwayMaps[0].name, Is.EqualTo("SW00_MapData"));
                Assert.That(gateway.CanOpenStage("SW01"), Is.True);
                Assert.That(gateway.CanOpenStage("SW20"), Is.True);
                Assert.That(gateway.CanOpenStage("SW21"), Is.False);
                var model = (NotebookBmModel)typeof(NotebookRoot)
                    .GetField("model", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
                model.State.balance = 10;
                root.OpenStages();
                root.nodes.First(node => node.stageId == "SW01").button.onClick.Invoke();
                Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.True);
                int before = model.State.balance;
                GameObject storyPrefab = gateway.ranchStoryPrefab;
                gateway.ranchStoryPrefab = null;
                root.storyReplayButton.onClick.Invoke();
                Assert.That(root.modal.activeSelf, Is.True);
                Assert.That(model.State.balance, Is.EqualTo(before));
                root.modalClose.onClick.Invoke();
                gateway.ranchStoryPrefab = storyPrefab;
                bool originalMotion = root.IsReducedMotion;
                root.SetReducedMotion(true);
                root.storyReplayButton.onClick.Invoke();
                root.storyReplayButton.onClick.Invoke();
                Assert.That(UnityEngine.Object.FindObjectsByType<StoryRunner>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                root.nodes.First(node => node.stageId == "SW02").button.onClick.Invoke();
                yield return null; yield return null;
                Assert.That(root.SelectedStageId, Is.EqualTo("SW02"));
                Assert.That(root.modal.activeSelf, Is.False);
                Assert.That(UnityEngine.Object.FindObjectsByType<StoryRunner>(FindObjectsSortMode.None), Is.Empty);
                root.nodes.First(node => node.stageId == "SW01").button.onClick.Invoke();
                root.storyReplayButton.onClick.Invoke();
                yield return null; yield return null;
                var runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
                Assert.That(runner, Is.Not.Null);
                Assert.That(runner.Package, Is.Not.Null);
                Assert.That(runner.persistProgress, Is.False);
                Assert.That(runner.reducedMotion, Is.True);
                var returnButton = runner.GetComponentInChildren<Canvas>().transform.Find("Return to Constellation Map").GetComponent<UnityEngine.UI.Button>();
                Assert.That(returnButton.GetComponent<Animol.Starway.StoryCrayonFrame>(), Is.Not.Null);
                var returnOutline = returnButton.transform.Find("CrayonOutline").GetComponent<UnityEngine.UI.Image>();
                Assert.That(returnOutline.raycastTarget, Is.False);
                Assert.That(returnOutline.material.name, Is.EqualTo("StoryCrayon_Utility_Normal_Still"));
                root.SetReducedMotion(originalMotion);
                Assert.That(model.State.balance, Is.EqualTo(before));
                Assert.That(PlayerPrefs.GetInt(clearKey), Is.EqualTo(1));
                Assert.That(PlayerPrefs.HasKey(storyKey), Is.EqualTo(hadStory));
                if (hadStory) Assert.That(PlayerPrefs.GetString(storyKey), Is.EqualTo(oldStory));
                returnButton.onClick.Invoke();
                yield return null;
                Assert.That(root.launchButton.interactable, Is.True);
                var notebookCanvas = root.GetComponentInParent<Canvas>();
                Assert.That(notebookCanvas, Is.Not.Null);
                root.launchButton.onClick.Invoke();
                yield return null; yield return null;
                var bridge = UnityEngine.Object.FindFirstObjectByType<NotebookMapDataPreviewBridge>();
                Assert.That(bridge, Is.Not.Null);
                for (int i = 0; i < 30 && !bridge.Ready; i++) yield return null;
                Assert.That(bridge.Ready, Is.True);
                Assert.That(bridge.sourceMap.name, Is.EqualTo("SW00_MapData"));
                Assert.That(bridge.StageId, Is.EqualTo("SW01"));
                Assert.That(notebookCanvas.gameObject.activeSelf, Is.False);
                Assert.That(root.gameObject.activeInHierarchy, Is.False);
                Assert.That(notebookCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                    .Single(image => image.name == "Static Paper Base").gameObject.activeInHierarchy, Is.False);
                var gameplayHud = UnityEngine.Object.FindFirstObjectByType<NotebookGameplayHud>();
                Assert.That(gameplayHud, Is.Not.Null);
                Assert.That(gameplayHud.GetComponentInParent<Canvas>().gameObject.activeInHierarchy, Is.True);
                Assert.That(gameplayHud.costNumber.Value, Is.EqualTo(45));
                var livePuzzle = bridge.GetComponent<PuzzlePrototypeController>();
                Assert.That(livePuzzle.RuntimeMap.scenario.costLimit, Is.EqualTo(45));
                livePuzzle.secondsPerCell = .001f;
                for (int i = 0; i < 120 && livePuzzle.IsMoving; i++) yield return null;
                Assert.That(livePuzzle.RequestMove(Vector2Int.right), Is.True);
                yield return null;
                Assert.That(gameplayHud.costNumber.Value, Is.EqualTo(44));
                for (int i = 0; i < 120 && livePuzzle.IsMoving; i++) yield return null;
                var sessionGateway = bridge.GetComponent<NotebookStarwaySessionGateway>();
                bool approved = false;
                sessionGateway.RequestNavigation(NotebookGameplayDestination.Map, "SW01", "", "return-test", value => approved = value);
                Assert.That(approved, Is.True);
                sessionGateway.OpenApprovedNavigation(NotebookGameplayDestination.Map, "SW01", "", "return-test");
                yield return null; yield return null;
                var returned = UnityEngine.Object.FindFirstObjectByType<NotebookRoot>();
                Assert.That(returned, Is.Not.Null);
                var returnedModel = (NotebookBmModel)typeof(NotebookRoot)
                    .GetField("model", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(returned);
                Assert.That(returnedModel.State.balance, Is.EqualTo(model.UnlimitedTelescopes ? before : before - 1));
            }
            finally
            {
                if (hadClear) PlayerPrefs.SetInt(clearKey, oldClear); else PlayerPrefs.DeleteKey(clearKey);
                if (hadWallet) PlayerPrefs.SetString(walletKey, oldWallet); else PlayerPrefs.DeleteKey(walletKey);
                if (hadStory) PlayerPrefs.SetString(storyKey, oldStory); else PlayerPrefs.DeleteKey(storyKey);
                PlayerPrefs.Save();
            }
#else
            Assert.Ignore("Unity Editor scene is required."); yield break;
#endif
        }

        private static void AssertOnScreen(RectTransform rect, Vector2Int size, string label)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(point.x, Is.InRange(-1f, size.x + 1f), label);
                Assert.That(point.y, Is.InRange(-1f, size.y + 1f), label);
            }
        }

#if UNITY_EDITOR
        // The project's portrait helper intentionally rejects landscape sizes. This checks the
        // explicitly requested PC layout without changing that mobile QA contract.
        private static void SelectWidePcResolution(int width, int height)
        {
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType", true);
            var viewType = assembly.GetType("UnityEditor.GameView", true);
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singletonType.GetProperty("instance", staticFlags).GetValue(null);
            var currentGroup = sizesType.GetProperty("currentGroupType", instanceFlags).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup", instanceFlags).Invoke(sizes, new[] { currentGroup });
            var groupType = group.GetType();
            var total = groupType.GetMethod("GetTotalCount", instanceFlags);
            var at = groupType.GetMethod("GetGameViewSize", instanceFlags);
            int Find()
            {
                for (int i = 0; i < (int)total.Invoke(group, null); i++)
                {
                    var size = at.Invoke(group, new object[] { i });
                    if ((int)size.GetType().GetProperty("width", instanceFlags).GetValue(size) == width &&
                        (int)size.GetType().GetProperty("height", instanceFlags).GetValue(size) == height) return i;
                }
                return -1;
            }
            int index = Find();
            if (index < 0)
            {
                var ctor = sizeType.GetConstructor(instanceFlags, null,
                    new[] { kindType, typeof(int), typeof(int), typeof(string) }, null);
                var size = ctor.Invoke(new[] { Enum.Parse(kindType, "FixedResolution"), (object)width, height, "Notebook wide PC" });
                groupType.GetMethod("AddCustomSize", instanceFlags).Invoke(group, new[] { size });
                index = Find();
            }
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            var view = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex", instanceFlags).SetValue(view, index);
            view.Show(); view.Focus(); view.Repaint();
        }
#endif
    }
}
