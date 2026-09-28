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
    public sealed class M6FullAcceptanceFlowTests
    {
        [UnityTest]
        public IEnumerator BootstrapPrimaryRoutes_BackAndReentry_ReturnToLobbyWithoutDuplicateNavigation()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            var bootstrapRoot = Object.FindFirstObjectByType<Canvas>().transform;
            Click(bootstrapRoot, "ContinueButton");
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));

            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;
            var routes = new[]
            {
                new[] { "CampaignButton", "ThemeBackButton", "SC02_ThemeSelect" },
                new[] { "CompetitiveButton", "CompetitiveBackButton", "SC05_CompetitiveHub" },
                new[] { "CoopButton", "CoopBackButton", "SC08_CoopHub" },
                new[] { "UpgradeButton", "UpgradeHubBackButton", "SC10_UpgradeHub" },
                new[] { "StoreButton", "StoreBackButton", "SC11_Store" },
                new[] { "SettingsButton", "SettingsBackButton", "SC13_Settings" }
            };

            foreach (var route in routes)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    Click(root, route[0]);
                    Assert.That(navigation.CurrentScreenId, Is.EqualTo(route[2]), route[0] + " re-entry failed.");
                    Click(root, route[1]);
                    Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC01_Lobby"), route[1] + " did not return to Lobby.");
                }
            }

            SceneManager.LoadScene("Results");
            yield return null;
            var resultsRoot = Object.FindFirstObjectByType<Canvas>().transform;
            Click(resultsRoot, "LobbyButton");
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
        }

        [UnityTest]
        public IEnumerator CommonRoot_UsesSafeAreaReferenceResolutionAndPreservesBackAfterLanguageRoundTrip()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;
            var safeArea = root.GetComponentsInChildren<SafeAreaLayout>(true).Single();
            var safeRect = (RectTransform)safeArea.transform;
            safeArea.Apply();
            var expected = Screen.safeArea;
            Assert.That(safeRect.anchorMin.x, Is.EqualTo(expected.xMin / Screen.width).Within(.001f));
            Assert.That(safeRect.anchorMin.y, Is.EqualTo(expected.yMin / Screen.height).Within(.001f));
            Assert.That(safeRect.anchorMax.x, Is.EqualTo(expected.xMax / Screen.width).Within(.001f));
            Assert.That(safeRect.anchorMax.y, Is.EqualTo(expected.yMax / Screen.height).Within(.001f));

            var scaler = root.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            Click(root, "SettingsButton");
            Click(root, "ControlSettingsButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC19_ControlSettings"));
            Click(root, "LanguageToggleButton");
            Assert.That(MobileControlPreferences.Language, Is.EqualTo(UiLanguage.English));
            Click(root, "LanguageToggleButton");
            Assert.That(MobileControlPreferences.Language, Is.EqualTo(UiLanguage.Korean));
            Click(root, "ControlSettingsBackButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC13_Settings"));
            Click(root, "SettingsBackButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC01_Lobby"));
        }

        private static void Click(Transform root, string name) =>
            root.GetComponentsInChildren<Button>(true).First(x => x.name == name).onClick.Invoke();
    }
}
