#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using William.NotebookUI;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public sealed class AndroidCampaignPlayTests
    {
        const string MainScene = "Assets/ANIMOL/Scenes/Main/NotebookUI.unity";
        const string Evidence = "Docs/Validation/AndroidCampaign";
        string wallet, preferences;
        bool hadWallet, hadPreferences, hadSelection;
        int selection;
        [SetUp] public void PreserveProgress()
        {
            hadWallet = PlayerPrefs.HasKey("WilliamNotebookUI.Wallet");
            wallet = PlayerPrefs.GetString("WilliamNotebookUI.Wallet");
            hadPreferences = PlayerPrefs.HasKey("WilliamNotebookUI.Preferences");
            preferences = PlayerPrefs.GetString("WilliamNotebookUI.Preferences");
            hadSelection = PlayerPrefs.HasKey(EchoTheaterCampaignController.SelectionKey);
            selection = PlayerPrefs.GetInt(EchoTheaterCampaignController.SelectionKey);
        }
        [UnityTearDown] public IEnumerator RestoreProgress()
        {
            foreach (var root in Object.FindObjectsByType<NotebookRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(root.GetComponentInParent<Canvas>(true).rootCanvas.gameObject);
            yield return null;
            if (hadWallet) PlayerPrefs.SetString("WilliamNotebookUI.Wallet", wallet); else PlayerPrefs.DeleteKey("WilliamNotebookUI.Wallet");
            if (hadPreferences) PlayerPrefs.SetString("WilliamNotebookUI.Preferences", preferences); else PlayerPrefs.DeleteKey("WilliamNotebookUI.Preferences");
            if (hadSelection) PlayerPrefs.SetInt(EchoTheaterCampaignController.SelectionKey, selection); else PlayerPrefs.DeleteKey(EchoTheaterCampaignController.SelectionKey);
            PlayerPrefs.Save();
            SelectSize(1080, 1920);
        }
        static void SelectSize(int width, int height)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t => t != null);
            type.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { width, height, "Android campaign " + width + "x" + height });
        }
        static IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Evidence);
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Evidence + "/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image);
        }
        static void AssertSkyCovers(Camera camera)
        {
            Assert.That(camera.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            var sky = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .FirstOrDefault(s => s.name == "Sky base" || s.name == "sky_base_native");
            Assert.That(sky, Is.Not.Null, "The opaque sky must be present.");
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            Bounds bounds = sky.bounds;
            Assert.That(bounds.min.x, Is.LessThanOrEqualTo(camera.transform.position.x - halfWidth));
            Assert.That(bounds.max.x, Is.GreaterThanOrEqualTo(camera.transform.position.x + halfWidth));
            Assert.That(bounds.min.y, Is.LessThanOrEqualTo(camera.transform.position.y - halfHeight));
            Assert.That(bounds.max.y, Is.GreaterThanOrEqualTo(camera.transform.position.y + halfHeight));
        }

        [UnityTest] public IEnumerator RealLobbyEntersBothWorldsAndAllFortyMapsLoadWithFullScreenSkies()
        {
            EditorSceneManager.LoadSceneInPlayMode(MainScene, new LoadSceneParameters(LoadSceneMode.Single));
            SelectSize(1080, 1920);
            yield return null; yield return null;
            Assert.That(NotebookCampaignPreview.IsEnabled, Is.True);
            var root = Object.FindFirstObjectByType<NotebookRoot>();
            yield return Capture("Lobby_1080x1920");
            var gateway = (NotebookProjectGateway)root.gateway;
            bool approved = false;
            gateway.RequestStageEntry("ET-01", 1, "android-theater-test", ok => approved = ok);
            Assert.That(approved, Is.True);
            gateway.OpenAcceptedStage("ET-01", "android-theater-test");
            yield return null; yield return null;
            var campaign = Object.FindFirstObjectByType<EchoTheaterCampaignController>();
            Assert.That(campaign, Is.Not.Null);
            Assert.That(campaign.CurrentStageId, Is.EqualTo("ET-01"));
            Assert.That(campaign.notebookHud && campaign.notebookGateway, Is.True);
            for (int i = 0; i < 20; i++)
            {
                campaign.SelectStage(i);
                yield return null; yield return null;
                Assert.That(campaign.puzzle.Session, Is.Not.Null, "ET-" + (i + 1));
                AssertSkyCovers(campaign.puzzle.FollowCamera.GetComponent<Camera>());
                Assert.That(campaign.puzzle.BlocksInput, Is.False);
                if (i == 0) yield return Capture("Theater01_1080x1920");
            }
            SelectSize(1080, 2400); yield return null; yield return null;
            AssertSkyCovers(campaign.puzzle.FollowCamera.GetComponent<Camera>());
            yield return Capture("Theater20_1080x2400");
            SceneManager.LoadScene("NotebookUI"); yield return null; yield return null;
            root = Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            yield return Capture("Lobby_1080x2400");
            gateway = (NotebookProjectGateway)root.gateway;
            gateway.RequestStageEntry("SW01", 1, "android-starway-test", ok => approved = ok);
            Assert.That(approved, Is.True);
            gateway.OpenAcceptedStage("SW01", "android-starway-test");
            yield return null; yield return null; yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(root.GetComponentInParent<Canvas>(true).gameObject.activeSelf, Is.False);
            for (int i = 1; i <= 20; i++)
            {
                var map = NotebookCampaignStages.FindStarwayMap(gateway.starwayMaps, "SW" + i.ToString("00"));
                puzzle.Load(map);
                yield return null; yield return null;
                Assert.That(puzzle.Session, Is.Not.Null, "SW" + i);
                AssertSkyCovers(puzzle.FollowCamera.GetComponent<Camera>());
                Assert.That(puzzle.BlocksInput, Is.False);
                if (i == 1) yield return Capture("Starway01_1080x2400");
            }
            SelectSize(1080, 1920); yield return null; yield return null;
            AssertSkyCovers(puzzle.FollowCamera.GetComponent<Camera>());
            yield return Capture("Starway20_1080x1920");
        }
    }
}
#endif
