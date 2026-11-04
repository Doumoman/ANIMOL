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
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookUiPlayTests
    {
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
            Assert.That(root.nodes.Length, Is.EqualTo(110));
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
            root.campaignButton.onClick.Invoke();
            Assert.That(root.stageScreen.activeSelf, Is.True);
            Assert.That(root.mainScreen.activeSelf, Is.False);
            Assert.That(root.MapSelectionVisible, Is.False);
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            Assert.That(root.mapScroll.content.anchoredPosition.y, Is.GreaterThanOrEqualTo(0));
            AssertOnScreen(root.mapBack.GetComponent<RectTransform>(), new Vector2Int(Screen.width, Screen.height), "map back");
            AssertOnScreen(root.telescopeButton.GetComponent<RectTransform>(), new Vector2Int(Screen.width, Screen.height), "telescope");
            root.nodes.First(v => v.stageId == "SW01").button.onClick.Invoke();
            Assert.That(root.MapSelectionVisible, Is.True);
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
            AssertOnScreen(root.nodes.First(v => v.stageId == "sea-30").button.GetComponent<RectTransform>(),
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
            root.nodes.First(v => v.stageId == "ET-01").button.onClick.Invoke();
            Assert.That(root.modal.activeSelf, Is.False);
            Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.False);
            root.launchButton.onClick.Invoke();
            Assert.That(root.modal.activeSelf, Is.True);
            Assert.That(root.nodes.First(v => v.stageId == "ET-01").star.sprite,
                Is.EqualTo(root.mapStarFrames[(3 * 3 + NotebookRoot.MapStarVariant("ET-01")) * 4]));
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

        [UnityTest] public IEnumerator ClearedStarwayReplayAndEntryUseRealAssetsWithoutReplayCharges()
        {
#if UNITY_EDITOR
            const string clearKey = "ANIMOL.Starway.Cleared.SW01";
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
                Assert.That(gateway.CanOpenStage("SW01"), Is.True);
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
                Assert.That(model.State.balance, Is.EqualTo(before));
                Assert.That(PlayerPrefs.GetInt(clearKey), Is.EqualTo(1));
                Assert.That(PlayerPrefs.HasKey(storyKey), Is.EqualTo(hadStory));
                if (hadStory) Assert.That(PlayerPrefs.GetString(storyKey), Is.EqualTo(oldStory));
                gateway.CloseReplay();
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
                Assert.That(bridge.sourceMap.name, Is.EqualTo("SW01"));
                Assert.That(notebookCanvas.gameObject.activeSelf, Is.False);
                Assert.That(root.gameObject.activeInHierarchy, Is.False);
                Assert.That(notebookCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                    .Single(image => image.name == "Static Paper Base").gameObject.activeInHierarchy, Is.False);
                var gameplayHud = UnityEngine.Object.FindFirstObjectByType<NotebookGameplayHud>();
                Assert.That(gameplayHud, Is.Not.Null);
                Assert.That(gameplayHud.GetComponentInParent<Canvas>().gameObject.activeInHierarchy, Is.True);
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
                Assert.That(returnedModel.State.balance, Is.EqualTo(before - 1));
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
