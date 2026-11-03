using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using William.NotebookUI;
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
                new Vector2Int(390, 844), new Vector2Int(1280, 720) })
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
            Assert.That(root.mapScroll.content.anchoredPosition.y, Is.GreaterThanOrEqualTo(0));
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
                Assert.That(clearedNode.star.sprite, Is.EqualTo(root.starDone));
                Assert.That(clearedNode.locked.gameObject.activeSelf, Is.False);
                Assert.That(clearedNode.number.gameObject.activeSelf, Is.True);
                Assert.That(clearedNode.label.text, Does.Contain("완료"));
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
            Assert.That(root.modal.activeSelf, Is.True);
            Assert.That(root.modalPrimary.interactable, Is.False);
            Assert.That(root.gateway.CanRestorePurchases, Is.False);
            Assert.That(root.gateway.CanShowRewardedAd, Is.False);
            Assert.That(root.gateway.CanApproveStageEntry, Is.False);
            root.modalClose.onClick.Invoke();
            root.mapBack.onClick.Invoke();
            root.settingsButton.onClick.Invoke();
            Assert.That(root.settingsScreen.activeSelf, Is.True);
            float originalBgm = root.bgmSlider.value;
            float changedBgm = originalBgm > .5f ? .25f : .75f;
            root.bgmSlider.value = changedBgm;
            var saved = JsonUtility.FromJson<NotebookPreferences>(PlayerPrefs.GetString("WilliamNotebookUI.Preferences"));
            Assert.That(saved.bgm, Is.EqualTo(changedBgm).Within(.001f));
            root.bgmSlider.value = originalBgm;
            root.settingsBack.onClick.Invoke();
            Assert.That(root.mainScreen.activeSelf, Is.True);
#else
            Assert.Ignore("Unity Editor scene and portrait GameView are required."); yield break;
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
