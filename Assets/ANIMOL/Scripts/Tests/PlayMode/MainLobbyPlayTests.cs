using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Menu.Tests
{
    public sealed class MainLobbyProgressProbe : MonoBehaviour, IRequiredStageProgress
    {
        public bool known;
        public bool cleared;
        public bool TryGetCleared(string stageId, out bool result)
        {
            result = cleared;
            return known;
        }
    }

    public class MainLobbyPlayTests
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string Portrait = "Docs/Validation/MainUiV7/Portrait";

#if UNITY_EDITOR
        static void UseGameView(int width, int height)
        {
            var setup = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .First(type => type != null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { width, height, "Main UI art QA" });
        }

        static IEnumerator OpenLobby()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }
#endif

        [UnityTearDown] public IEnumerator LeaveEmptyScene()
        {
#if UNITY_EDITOR
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Main lobby test cleanup");
            SceneManager.SetActiveScene(empty);
            if (previous.IsValid())
            {
                var unload = SceneManager.UnloadSceneAsync(previous);
                if (unload != null) while (!unload.isDone) yield return null;
            }
#endif
            yield return null;
        }

        [UnityTest] public IEnumerator AllEighteenAppearancesRenderAtPortraitAndSceneryCoversEveryMotionEnd()
        {
#if UNITY_EDITOR
            UseGameView(1080, 1920);
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.CurrentThemeIndex, Is.InRange(0, 3));
            Assert.That(lobby.BattleUnlocked, Is.False);
            Assert.That(lobby.battleButton.interactable, Is.False);
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            Directory.CreateDirectory(Portrait);
            var count = 0;
            foreach (var theme in lobby.catalog.themes.Select((value, index) => (value, index)))
                foreach (var person in theme.value.people)
                    foreach (var appearance in person.appearances)
                    {
                        Assert.That(lobby.PreviewAppearance(theme.index, person.characterId, appearance.artId), Is.True);
                        yield return null;
                        Assert.That(lobby.CurrentCharacterId, Is.EqualTo(person.characterId));
                        Assert.That(lobby.CurrentArtId, Is.EqualTo(appearance.artId));
                        Assert.That(lobby.heroImage.sprite, Is.SameAs(appearance.illustration));
                        Assert.That(lobby.backgroundImage.sprite, Is.SameAs(theme.value.scenery));
                        Assert.That(lobby.heroImage.preserveAspect, Is.True);
                        Assert.That(lobby.HeroDisplayedPixels, Is.GreaterThan(0).And.LessThan(1536));
                        foreach (var x in new[] { -1f, 1f })
                            foreach (var y in new[] { -1f, 1f })
                                Assert.That(lobby.BackgroundCovers(x, y), Is.True,
                                    theme.value.id + "/" + appearance.artId + " at " + x + "," + y);
                        var hero = new Vector3[4];
                        var logo = new Vector3[4];
                        lobby.heroRect.GetWorldCorners(hero);
                        lobby.logoRect.GetWorldCorners(logo);
                        var heroTop = lobby.artArea.InverseTransformPoint(hero[1]).y;
                        var logoBottom = lobby.artArea.InverseTransformPoint(logo[0]).y;
                        Assert.That(heroTop, Is.LessThanOrEqualTo(logoBottom - 17f), appearance.artId);
                        Assert.That(lobby.artArea.InverseTransformPoint(hero[0]).y,
                            Is.GreaterThanOrEqualTo(lobby.artArea.rect.yMin + 19f), appearance.artId);
                        ScreenCapture.CaptureScreenshot(Portrait + "/" + appearance.artId + ".png");
                        yield return new WaitForSecondsRealtime(.15f);
                        count++;
                    }
            Assert.That(count, Is.EqualTo(18));
            UseGameView(1440, 900);
            yield return null;
            yield return null;
            lobby.ForceLayout();
            Assert.That(lobby.frame.rect.width / lobby.frame.rect.height, Is.EqualTo(9f / 16f).Within(.001f));
            Assert.That(lobby.BackgroundCovers(-1, -1) && lobby.BackgroundCovers(1, 1), Is.True);
            ScreenCapture.CaptureScreenshot(Portrait + "/pc_window.png");
            yield return new WaitForSecondsRealtime(.2f);
            UseGameView(1080, 2400);
            yield return null;
            lobby.ForceLayout();
            var titleCorners = new Vector3[4];
            var frameCorners = new Vector3[4];
            lobby.logoRect.Find("William Paradox Title").GetComponent<RectTransform>().GetWorldCorners(titleCorners);
            lobby.frame.GetWorldCorners(frameCorners);
            Assert.That(titleCorners[0].x, Is.GreaterThan(frameCorners[0].x + 1f));
            Assert.That(titleCorners[2].x, Is.LessThan(frameCorners[2].x - 1f));
            Assert.That(lobby.BackgroundCovers(-1, -1) && lobby.BackgroundCovers(1, 1), Is.True);
            UseGameView(1080, 1920);
            yield return null;
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var reloaded = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(reloaded.CurrentThemeIndex, Is.InRange(0, 3));
            Assert.That(reloaded.heroImage.sprite, Is.Not.Null);
            Assert.That(reloaded.BackgroundCovers(-1, -1), Is.True);
#else
            Assert.Ignore("Requires Unity Editor Game View scene loading."); yield break;
#endif
        }

        [UnityTest] public IEnumerator CampaignPopupHiddenAppAndReducedMotionPauseWithoutReroll()
        {
#if UNITY_EDITOR
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            lobby.SendMessage("OnApplicationFocus", true);
            var theme = lobby.CurrentThemeIndex;
            var person = lobby.CurrentCharacterId;
            var art = lobby.CurrentArtId;
            var start = lobby.MotionTime;
            lobby.AdvanceMotion(1);
            Assert.That(lobby.MotionTime, Is.GreaterThan(start));
            lobby.OpenCampaign();
            Assert.That(lobby.IsCampaignOpen, Is.True);
            var held = lobby.MotionTime;
            var heldAppearance = lobby.AppearanceElapsed;
            lobby.AdvanceMotion(9);
            lobby.AdvanceAppearance(9);
            Assert.That(lobby.MotionTime, Is.EqualTo(held));
            Assert.That(lobby.AppearanceElapsed, Is.EqualTo(heldAppearance));
            for (var i = 0; i < 4; i++) lobby.SelectCampaignTheme(i);
            Assert.That(lobby.themeCards.Length, Is.EqualTo(4));
            ScreenCapture.CaptureScreenshot(Portrait + "/campaign.png");
            yield return new WaitForSecondsRealtime(.15f);
            lobby.campaignContinueButton.onClick.Invoke();
            Assert.That(lobby.IsModalOpen, Is.True);
            lobby.CloseModal();
            lobby.CloseCampaign();
            lobby.AdvanceMotion(1);
            Assert.That(lobby.MotionTime, Is.GreaterThan(held));
            lobby.OpenSettings();
            held = lobby.MotionTime;
            heldAppearance = lobby.AppearanceElapsed;
            lobby.AdvanceMotion(9);
            lobby.AdvanceAppearance(9);
            Assert.That(lobby.MotionTime, Is.EqualTo(held));
            Assert.That(lobby.AppearanceElapsed, Is.EqualTo(heldAppearance));
            ScreenCapture.CaptureScreenshot(Portrait + "/settings.png");
            yield return new WaitForSecondsRealtime(.15f);
            lobby.CloseModal();
            lobby.SetReducedMotion(true);
            Assert.That(lobby.MotionTime, Is.Zero);
            lobby.AdvanceMotion(9);
            lobby.AdvanceAppearance(9);
            Assert.That(lobby.MotionTime, Is.Zero);
            Assert.That(lobby.AppearanceElapsed, Is.Zero);
            lobby.SetReducedMotion(false);
            lobby.SendMessage("OnApplicationPause", true);
            lobby.AdvanceMotion(9);
            lobby.AdvanceAppearance(9);
            Assert.That(lobby.MotionTime, Is.Zero);
            Assert.That(lobby.AppearanceElapsed, Is.Zero);
            lobby.SendMessage("OnApplicationPause", false);
            lobby.SendMessage("OnApplicationFocus", false);
            lobby.AdvanceAppearance(9);
            Assert.That(lobby.AppearanceElapsed, Is.Zero);
            lobby.SendMessage("OnApplicationFocus", true);
            lobby.AdvanceMotion(1);
            lobby.AdvanceAppearance(1);
            Assert.That(lobby.MotionTime, Is.GreaterThan(0));
            Assert.That(lobby.AppearanceElapsed, Is.GreaterThan(0));
            Assert.That(lobby.CurrentThemeIndex, Is.EqualTo(theme));
            Assert.That(lobby.CurrentCharacterId, Is.EqualTo(person));
            Assert.That(lobby.CurrentArtId, Is.EqualTo(art));
#else
            Assert.Ignore("Requires Unity Editor scene loading."); yield break;
#endif
        }

        [UnityTest] public IEnumerator FiveSecondDiagonalWipeCyclesAllFourThemesWithoutBlockingInput()
        {
#if UNITY_EDITOR
            UseGameView(1080, 1920);
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            lobby.SendMessage("OnApplicationFocus", true);
            var themes = new System.Collections.Generic.HashSet<int> { lobby.CurrentThemeIndex };
            for (var turn = 0; turn < 3; turn++)
            {
                var previousTheme = lobby.CurrentThemeIndex;
                var previousArt = lobby.CurrentArtId;
                var oldHero = lobby.heroImage.sprite;
                var oldHeroMaterial = lobby.heroImage.material;
                lobby.AdvanceAppearance(MainLobbyController.AppearanceInterval - lobby.AppearanceElapsed - .01f);
                Assert.That(lobby.CurrentThemeIndex, Is.EqualTo(previousTheme));
                Assert.That(lobby.CurrentArtId, Is.EqualTo(previousArt));
                lobby.AdvanceAppearance(.02f);
                Assert.That(lobby.IsAppearanceTransitioning, Is.True);
                Assert.That(lobby.CurrentThemeIndex, Is.Not.EqualTo(previousTheme));
                Assert.That(lobby.CurrentArtId, Is.Not.EqualTo(previousArt));
                Assert.That(lobby.heroImage.sprite, Is.SameAs(oldHero), "old art remains on the unrevealed side");
                Assert.That(lobby.backgroundTransitionImage.raycastTarget, Is.False);
                Assert.That(lobby.heroTransitionImage.raycastTarget, Is.False);
                Assert.That(lobby.diagonalLineImage.raycastTarget, Is.False);
                Assert.That(lobby.campaignButton.interactable, Is.True);
                lobby.AdvanceAppearance(MainLobbyController.AppearanceWipeDuration * .5f);
                Assert.That(lobby.AppearanceTransitionProgress, Is.EqualTo(.5f).Within(.01f));
                Assert.That(lobby.heroTransitionImage.color.a, Is.EqualTo(1f));
                Assert.That(lobby.backgroundTransitionImage.color.a, Is.EqualTo(1f));
                Assert.That(lobby.heroTransitionImage.material, Is.SameAs(lobby.backgroundTransitionImage.material));
                Assert.That(lobby.heroTransitionImage.material.shader.name, Is.EqualTo("ANIMOL/UI/Main Lobby Diagonal Reveal"));
                Assert.That(lobby.heroTransitionImage.material.GetFloat("_Progress"), Is.EqualTo(.5f).Within(.01f));
                Assert.That(lobby.heroImage.material.shader.name, Is.EqualTo("ANIMOL/UI/Main Lobby Diagonal Reveal"));
                Assert.That(lobby.heroImage.material.GetFloat("_Invert"), Is.EqualTo(1f));
                Assert.That(lobby.heroImage.material.GetFloat("_Progress"), Is.EqualTo(.5f).Within(.01f),
                    "the outgoing character must be cut by the same moving diagonal");
                Assert.That(lobby.heroTransitionImage.material.GetFloat("_Invert"), Is.Zero);
                Assert.That(lobby.heroTransitionRect.rect.width / lobby.heroTransitionRect.rect.height,
                    Is.EqualTo(lobby.heroTransitionImage.sprite.rect.width /
                        lobby.heroTransitionImage.sprite.rect.height).Within(.001f));
                Assert.That(lobby.heroTransitionImage.mainTexture, Is.SameAs(lobby.heroTransitionImage.sprite.texture));
                Assert.That(lobby.backgroundTransitionImage.mainTexture, Is.SameAs(lobby.backgroundTransitionImage.sprite.texture));
                Assert.That(lobby.diagonalLineImage.gameObject.activeSelf, Is.True);
                Assert.That(lobby.diagonalLineRect.rect.width,
                    Is.EqualTo(((RectTransform)lobby.canvas.transform).rect.size.magnitude).Within(2f));
                Assert.That(lobby.BackgroundCovers(-1, -1) && lobby.BackgroundCovers(1, 1), Is.True);
                if (turn == 0)
                {
                    ScreenCapture.CaptureScreenshot(Portrait + "/diagonal_wipe.png");
                    yield return new WaitForSecondsRealtime(.05f);
                    var heldProgress = lobby.AppearanceTransitionProgress;
                    lobby.OpenSettings();
                    Assert.That(lobby.diagonalLineImage.gameObject.activeSelf, Is.False);
                    lobby.AdvanceAppearance(1f);
                    Assert.That(lobby.AppearanceTransitionProgress, Is.EqualTo(heldProgress));
                    UseGameView(1440, 900);
                    yield return null;
                    lobby.ForceLayout();
                    lobby.CloseModal();
                    Assert.That(lobby.diagonalLineImage.gameObject.activeSelf, Is.True);
                    Assert.That(lobby.diagonalLineRect.rect.width,
                        Is.EqualTo(2f * Mathf.Min(heldProgress, 1f - heldProgress) *
                            ((RectTransform)lobby.canvas.transform).rect.size.magnitude).Within(2f));
                    UseGameView(1080, 1920);
                    yield return null;
                    lobby.ForceLayout();
                }
                lobby.AdvanceAppearance(MainLobbyController.AppearanceWipeDuration);
                Assert.That(lobby.IsAppearanceTransitioning, Is.False);
                Assert.That(lobby.diagonalLineImage.gameObject.activeSelf, Is.False);
                Assert.That(lobby.heroImage.sprite, Is.SameAs(lobby.heroTransitionImage.sprite));
                Assert.That(lobby.heroImage.material, Is.SameAs(oldHeroMaterial));
                Assert.That(lobby.heroRect.rect.width, Is.EqualTo(lobby.heroTransitionRect.rect.width).Within(.01f));
                Assert.That(lobby.heroRect.rect.height, Is.EqualTo(lobby.heroTransitionRect.rect.height).Within(.01f));
                Assert.That(lobby.backgroundImage.sprite, Is.SameAs(lobby.backgroundTransitionImage.sprite));
                themes.Add(lobby.CurrentThemeIndex);
            }
            Assert.That(themes.Count, Is.EqualTo(4), "four themes appear once per shuffle cycle");
            var lastTheme = lobby.CurrentThemeIndex;
            lobby.AdvanceAppearance(MainLobbyController.AppearanceInterval - lobby.AppearanceElapsed + .01f);
            Assert.That(lobby.CurrentThemeIndex, Is.Not.EqualTo(lastTheme), "cycle boundary cannot repeat the same backdrop");
            Assert.That(lobby.IsAppearanceTransitioning, Is.True);
            lobby.SetReducedMotion(true);
            Assert.That(lobby.IsAppearanceTransitioning, Is.False);
            Assert.That(lobby.backgroundImage.sprite, Is.SameAs(lobby.backgroundTransitionImage.sprite));
            Assert.That(lobby.heroImage.sprite, Is.SameAs(lobby.heroTransitionImage.sprite));
            var reducedArt = lobby.CurrentArtId;
            lobby.AdvanceAppearance(10);
            Assert.That(lobby.CurrentArtId, Is.EqualTo(reducedArt));
            Assert.That(lobby.AppearanceElapsed, Is.Zero);
#else
            Assert.Ignore("Requires Unity Editor scene loading."); yield break;
#endif
        }

        [UnityTest] public IEnumerator LiveUpdateChangesBothImagesAfterFiveSeconds()
        {
#if UNITY_EDITOR
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            lobby.SendMessage("OnApplicationFocus", true);
            var startingTheme = lobby.CurrentThemeIndex;
            var startingArt = lobby.CurrentArtId;
            var deadline = Time.realtimeSinceStartup + 5.6f;
            while (Time.realtimeSinceStartup < deadline)
            {
                // The Editor test runner can take Game View focus; keep this simulated lobby visit foregrounded.
                lobby.SendMessage("OnApplicationFocus", true);
                yield return null;
            }
            Assert.That(lobby.CurrentThemeIndex, Is.Not.EqualTo(startingTheme));
            Assert.That(lobby.CurrentArtId, Is.Not.EqualTo(startingArt));
            Assert.That(lobby.IsAppearanceTransitioning, Is.False);
            Assert.That(lobby.backgroundImage.sprite, Is.SameAs(lobby.catalog.themes[lobby.CurrentThemeIndex].scenery));
            Assert.That(lobby.heroImage.sprite,
                Is.SameAs(lobby.catalog.FindPerson(lobby.CurrentThemeIndex, lobby.CurrentCharacterId)
                    .appearances.First(item => item.artId == lobby.CurrentArtId).illustration));
#else
            Assert.Ignore("Requires Unity Editor scene loading."); yield break;
#endif
        }

        [UnityTest] public IEnumerator PressMovesOnlyAnInteractableButtonAndReleaseRestoresIt()
        {
#if UNITY_EDITOR
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            var start = lobby.playButton.GetComponent<RectTransform>().anchoredPosition;
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(lobby.playButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(lobby.playButton.GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(start.y - 2f).Within(.01f));
            ExecuteEvents.Execute(lobby.playButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(lobby.playButton.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(start));
            var lockedStart = lobby.battleButton.GetComponent<RectTransform>().anchoredPosition;
            ExecuteEvents.Execute(lobby.battleButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(lobby.battleButton.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(lockedStart));
#else
            Assert.Ignore("Requires Unity Editor scene loading."); yield break;
#endif
        }

        [UnityTest] public IEnumerator BattleUnknownFailureIncompleteCompleteAndRelockUseAllThemes()
        {
#if UNITY_EDITOR
            yield return OpenLobby();
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            var original = lobby.catalog;
            var ephemeral = Object.Instantiate(original);
            ephemeral.requiredStageIds = new[] { "test-only-a", "test-only-b" };
            lobby.catalog = ephemeral;
            var probe = lobby.gameObject.AddComponent<MainLobbyProgressProbe>();
            lobby.requiredStageProgress = probe;
            lobby.RefreshBattleGate();
            Assert.That(lobby.BattleUnlocked, Is.False, "unknown progress is locked");
            probe.known = true;
            probe.cleared = false;
            lobby.RefreshBattleGate();
            Assert.That(lobby.BattleUnlocked, Is.False, "incomplete progress is locked");
            probe.cleared = true;
            lobby.RefreshBattleGate();
            Assert.That(lobby.BattleUnlocked, Is.True);
            Assert.That(lobby.BattleThemeIds(), Is.EqualTo(new[] { "starway", "theater", "observatory", "sea" }));
            lobby.SelectMode("battle");
            lobby.Play();
            Assert.That(lobby.IsModalOpen, Is.True);
            lobby.CloseModal();
            probe.known = false;
            lobby.RefreshBattleGate();
            Assert.That(lobby.BattleUnlocked, Is.False, "unknown again relocks battle");
            Assert.That(lobby.BattleThemeIds(), Is.Empty);
            lobby.catalog = original;
            Object.Destroy(ephemeral);
#else
            Assert.Ignore("Requires Unity Editor scene loading."); yield break;
#endif
        }
    }
}
