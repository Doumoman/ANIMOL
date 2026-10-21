using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Menu;
using DiagonalPattern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.Menu.Tests
{
    public class MainLobbyPatternPlayTests
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";

#if UNITY_EDITOR
        static void UseGameView(int width, int height)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .First(found => found != null);
            type.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { width, height, "Pattern v10 QA" });
        }

        static string Capture(string name)
        {
            var directory = Path.Combine(Path.GetTempPath(), "ANIMOL_MainUiPatternV10");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            return path;
        }
#endif

        [UnityTest] public IEnumerator MainCampaignSettingsAndTallGameViewRender()
        {
#if UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Unity Game View capture requires a graphics device.");
                yield break;
            }
            UseGameView(1080, 1920);
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            var main = Capture("main_1080x1920");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(File.Exists(main), Is.True);
            lobby.OpenCampaign();
            yield return null;
            var campaign = Capture("campaign_1080x1920");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(File.Exists(campaign), Is.True);
            lobby.OpenSettings();
            yield return null;
            var settings = Capture("settings_1080x1920");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(File.Exists(settings), Is.True);
            lobby.CloseModal();
            lobby.CloseCampaign();
            UseGameView(1080, 2400);
            yield return null;
            yield return null;
            lobby.ForceLayout();
            var tall = Capture("main_1080x2400");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(File.Exists(tall), Is.True);
            Assert.That(lobby.frame.rect.width, Is.LessThanOrEqualTo(lobby.safeRoot.rect.width));
#else
            Assert.Ignore("Requires Unity Editor Game View.");
            yield break;
#endif
        }

        [UnityTest] public IEnumerator CampaignPatternCrossesTileAndResumesInPlaceAroundModalAndReducedMotion()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            var initialHero = lobby.heroImage.sprite;
            var initialScenery = lobby.backgroundImage.sprite;
            lobby.OpenCampaign();
            var pattern = lobby.campaignPanel.GetComponentInChildren<DiagonalPatternBackground>(true);
            Assert.That(pattern, Is.Not.Null);
            Assert.That(pattern.IsScrolling, Is.True);
            Assert.That(pattern.GetComponent<RawImage>().raycastTarget, Is.False);
            Assert.That(lobby.campaignBackButton.interactable, Is.True);
            var phase = pattern.CurrentPhase;
            pattern.AdvancePattern(120.25);
            Assert.That(Mathf.Repeat(pattern.CurrentPhase.x - phase.x, 1), Is.EqualTo(.25f / 120f).Within(.0001f));
            Assert.That(Mathf.Repeat(pattern.CurrentPhase.y - phase.y, 1), Is.EqualTo(.25f / 120f).Within(.0001f));
            var held = pattern.CurrentPhase;
            var sceneryPosition = lobby.backgroundRect.anchoredPosition;
            lobby.AdvanceMotion(10);
            Assert.That(lobby.backgroundRect.anchoredPosition, Is.EqualTo(sceneryPosition));
            Assert.That(lobby.heroImage.sprite, Is.SameAs(initialHero));
            Assert.That(lobby.backgroundImage.sprite, Is.SameAs(initialScenery));
            lobby.CloseCampaign();
            lobby.OpenCampaign();
            Assert.That(pattern.CurrentPhase, Is.EqualTo(held));
            Assert.That(pattern.IsScrolling, Is.True);
            lobby.OpenSettings();
            Assert.That(pattern.IsScrolling, Is.False);
            held = pattern.CurrentPhase;
            pattern.AdvancePattern(10);
            Assert.That(pattern.CurrentPhase, Is.EqualTo(held));
            lobby.CloseModal();
            Assert.That(pattern.IsScrolling, Is.True);
            lobby.reducedMotionToggle.isOn = true;
            Assert.That(lobby.ReducedMotion, Is.True);
            Assert.That(pattern.IsScrolling, Is.False);
            pattern.AdvancePattern(10);
            Assert.That(pattern.CurrentPhase, Is.EqualTo(held));
            lobby.reducedMotionToggle.isOn = false;
            Assert.That(lobby.ReducedMotion, Is.False);
            Assert.That(pattern.IsScrolling, Is.True);
            pattern.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(pattern.IsScrolling, Is.False);
            pattern.gameObject.SendMessage("OnApplicationPause", false);
            Assert.That(pattern.IsScrolling, Is.True);
            pattern.gameObject.SendMessage("OnApplicationFocus", false);
            Assert.That(pattern.IsScrolling, Is.False);
            pattern.gameObject.SendMessage("OnApplicationFocus", true);
            Assert.That(pattern.IsScrolling, Is.True);
            Assert.That(lobby.BattleUnlocked, Is.False);
            Assert.That(lobby.BattleThemeIds(), Is.Empty);
#else
            Assert.Ignore("Requires Unity Editor scene loading.");
            yield break;
#endif
        }
    }
}
