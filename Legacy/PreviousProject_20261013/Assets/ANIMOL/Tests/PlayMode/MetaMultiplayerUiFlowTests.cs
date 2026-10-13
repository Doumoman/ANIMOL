using System.Collections;
using System.Linq;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.Tests
{
    public sealed class MetaMultiplayerUiFlowTests
    {
        [UnityTest]
        public IEnumerator OfflineMetaAndMultiplayerRoutes_ShowTruthfulUnavailableStates()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;

            Click(root, "UpgradeButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC10_UpgradeHub"));
            Click(root, "UpgradeHubAccountButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC10A_AccountUpgrade"));
            Assert.That(root.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("AccountTrack_") && !x.interactable), Is.EqualTo(3));
            Click(root, "AccountExplainButton");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Click(root, "ServiceErrorBackButton");

            navigation.Navigate("SC01_Lobby", false);
            Click(root, "StoreButton");
            Click(root, "StoreGrowthDetailsButton");
            Assert.That(FindButton(root, "ProductPurchaseButton").interactable, Is.False);
            Assert.That(Find(root, "ProductDetailModal").activeSelf, Is.True);
            Click(root, "ProductCancelButton");

            navigation.Navigate("SC01_Lobby", false);
            Click(root, "EmoteButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC12_EmoteCollection"));
            Assert.That(root.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("QuickSlot_")), Is.EqualTo(4));
            Click(root, "FreeEmote_01");

            navigation.Navigate("SC01_Lobby", false);
            Click(root, "CompetitiveButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC05_CompetitiveHub"));
            Click(root, "CompetitiveMatchButton");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Click(root, "ServiceErrorBackButton");
            Click(root, "CompetitiveDevPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC06_MatchRoom"));
            Click(root, "RoomAnimalSelectButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC07_MultiplayerAnimalSelect"));
            Assert.That(FindButton(root, "MultiplayerReadyButton").interactable, Is.False);
            Click(root, "AnimalCard_01");
            Click(root, "AnimalCard_02");
            Click(root, "MultiplayerHudPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("HUD_Competitive"));
            Click(root, "CompetitiveHudPauseButton");
            Assert.That(Find(root, "MultiplayerPauseModal").activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Online menu must not stop server/world time.");
            Click(root, "MultiplayerPauseContinueButton");
            Click(root, "CompetitiveHudEmote_01");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True, "Disconnected HUD emote must report failure instead of fake sending.");
            yield return null;
        }

        private static void Click(Transform root, string name) => FindButton(root, name).onClick.Invoke();
        private static Button FindButton(Transform root, string name) => root.GetComponentsInChildren<Button>(true).First(x => x.name == name);
        private static GameObject Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(x => x.name == name).gameObject;
    }
}
