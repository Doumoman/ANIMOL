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
    public sealed class M5AcceptanceFlowTests
    {
        [UnityTest]
        public IEnumerator RootScreensModalsAndBackRoutes_RespondOnce()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;

            var multiplayer = Object.FindFirstObjectByType<MultiplayerUiPresenter>();
            multiplayer.enabled = false;
            multiplayer.enabled = true;
            var screenChangeCount = 0;
            navigation.ScreenChanged += _ => screenChangeCount++;
            Click(root, "CompetitiveButton");
            Assert.That(screenChangeCount, Is.EqualTo(1), "Re-entering/enabling a presenter must not duplicate click listeners.");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC05_CompetitiveHub"));
            Click(root, "CompetitiveBackButton");

            Click(root, "CampaignButton");
            Click(root, "ThemeCard_01");
            Assert.That(ActiveScreen(root).GetComponentsInChildren<Button>(true).Length, Is.EqualTo(21));
            Click(root, "StageCard_01");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC04_StageDetail"));
            Assert.That(ActiveScreen(root).GetComponentsInChildren<Button>(true).Count(x => x.name.Contains("Animal") || x.name.Contains("Loadout")), Is.Zero);
            Assert.That(FindButton(root, "StageStartButton").interactable, Is.False);
            Click(root, "DetailBackButton");
            Click(root, "StageBackButton");
            Click(root, "ThemeBackButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC01_Lobby"));

            Click(root, "SettingsButton");
            Click(root, "SettingsResetButton");
            Click(root, "SettingsBackButton");
            Click(root, "HelpButton");
            Click(root, "HelpBackButton");

            Click(root, "UpgradeButton");
            Click(root, "UpgradeHubCharacterButton");
            Assert.That(FindButton(root, "CharacterPurchaseButton").interactable, Is.False);
            Click(root, "CharacterExplainButton");
            Click(root, "ServiceErrorRetryButton");
            Assert.That(FindText(root, "ServiceErrorMessage").text, Does.Contain("여전히 미연결"));
            Click(root, "ServiceErrorBackButton");
            Click(root, "CharacterUpgradeBackButton");
            Click(root, "UpgradeHubBackButton");

            Click(root, "StoreButton");
            Click(root, "StoreEmoteDetailsButton");
            Assert.That(FindButton(root, "ProductPurchaseButton").interactable, Is.False);
            Click(root, "ProductCancelButton");
            Click(root, "StoreBackButton");
            Click(root, "EmoteButton");
            Click(root, "EmoteStoreButton");
            Click(root, "StoreBackButton");
            Click(root, "ProfileButton");
            Click(root, "ProfileBackButton");

            Click(root, "RewardedAdButton");
            Assert.That(Find(root, "RewardedAdModal").activeSelf, Is.True);
            Click(root, "RewardedAdCancelButton");
            Click(root, "ExitButton");
            Click(root, "ExitConfirmButton");
            Assert.That(Find(root, "ToastAndErrorOverlay").activeSelf, Is.True);
            Click(root, "ExitCancelButton");

            navigation.Navigate("SC09_MapLoading");
            Click(root, "LoadingBackButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC01_Lobby"));
            navigation.Navigate("SC18_CampaignMilestone");
            Assert.That(FindButton(root, "NextThemeButton").interactable, Is.False);
            Click(root, "MilestoneLobbyButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC01_Lobby"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoopSelectionHudAndResultActions_AreConnectedWithoutOnlineSuccess()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;
            Click(root, "CoopButton");
            Click(root, "CoopMatchButton");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Click(root, "ServiceErrorBackButton");
            Click(root, "CoopDevPreviewButton");
            Click(root, "RoomAnimalSelectButton");
            Click(root, "AnimalCard_Locked");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Click(root, "ServiceErrorBackButton");
            Click(root, "AnimalCard_01");
            Click(root, "AnimalCard_02");
            Assert.That(FindButton(root, "MultiplayerReadyButton").interactable, Is.False);
            Click(root, "MultiplayerHudPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("HUD_Coop"));
            Click(root, "CoopHudPauseButton");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Click(root, "MultiplayerPauseContinueButton");
            Click(root, "CoopHudBackButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC07_MultiplayerAnimalSelect"));

            SceneManager.LoadScene("Results");
            yield return null;
            var resultsRoot = Object.FindFirstObjectByType<Canvas>().transform;
            Click(resultsRoot, "LobbyButton");
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
        }

        private static GameObject ActiveScreen(Transform root) => root.Find("SafeArea/ScreenHost").Cast<Transform>().Single(x => x.gameObject.activeSelf).gameObject;
        private static void Click(Transform root, string name) => FindButton(root, name).onClick.Invoke();
        private static Button FindButton(Transform root, string name) => root.GetComponentsInChildren<Button>(true).First(x => x.name == name);
        private static Text FindText(Transform root, string name) => root.GetComponentsInChildren<Text>(true).First(x => x.name == name);
        private static GameObject Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(x => x.name == name).gameObject;
    }
}
