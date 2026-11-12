#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
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
    public sealed class NotebookTemporaryPlayModeTests
    {
        const string Wallet = "WilliamNotebookUI.Wallet", Prefs = "WilliamNotebookUI.Preferences";
        bool hadWallet, hadPrefs, hadSelection;
        string wallet, prefs;
        int selection;

        [SetUp] public void PreserveSavedState()
        {
            hadWallet = PlayerPrefs.HasKey(Wallet); wallet = PlayerPrefs.GetString(Wallet);
            hadPrefs = PlayerPrefs.HasKey(Prefs); prefs = PlayerPrefs.GetString(Prefs);
            hadSelection = PlayerPrefs.HasKey(EchoTheaterCampaignController.SelectionKey);
            selection = PlayerPrefs.GetInt(EchoTheaterCampaignController.SelectionKey);
            PlayerPrefs.SetString(Wallet, JsonUtility.ToJson(new NotebookWalletState {
                balance = 0, lastRecoveryUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }));
        }

        [UnityTearDown] public IEnumerator RestoreSavedState()
        {
            try
            {
                foreach (var root in Object.FindObjectsByType<NotebookRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var canvas = root.GetComponentInParent<Canvas>(true);
                    if (canvas) Object.Destroy(canvas.rootCanvas.gameObject);
                }
                yield return null;
            }
            finally
            {
                if (hadWallet) PlayerPrefs.SetString(Wallet, wallet); else PlayerPrefs.DeleteKey(Wallet);
                if (hadPrefs) PlayerPrefs.SetString(Prefs, prefs); else PlayerPrefs.DeleteKey(Prefs);
                if (hadSelection) PlayerPrefs.SetInt(EchoTheaterCampaignController.SelectionKey, selection);
                else PlayerPrefs.DeleteKey(EchoTheaterCampaignController.SelectionKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest] public IEnumerator ExistingStarwayButtonRestartsMoreThanFiveTimesWithoutReloadingMaps()
        {
            yield return Enter("SW01");
            var bridge = Object.FindFirstObjectByType<NotebookMapDataPreviewBridge>();
            for (int i = 0; i < 90 && (!bridge || !bridge.Ready); i++) { yield return null; bridge = Object.FindFirstObjectByType<NotebookMapDataPreviewBridge>(); }
            Assert.That(bridge && bridge.Ready, Is.True);
            yield return ExerciseRestarts(bridge.GetComponent<PuzzlePrototypeController>(), bridge.hud, "SW01");
        }

        [UnityTest] public IEnumerator ExistingTheaterButtonRestartsMoreThanFiveTimesWithoutReloadingMaps()
        {
            yield return Enter("ET-01");
            var campaign = Object.FindFirstObjectByType<EchoTheaterCampaignController>();
            Assert.That(campaign, Is.Not.Null);
            yield return ExerciseRestarts(campaign.puzzle, campaign.notebookHud, "ET-01");
        }

        static IEnumerator Enter(string stageId)
        {
            Assert.That(NotebookCampaignPreview.HasUnlimitedTelescopes && NotebookCampaignPreview.HasUnlimitedRerolls &&
                NotebookCampaignPreview.HasAllStagesCleared, Is.True);
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var root = Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(root, Is.Not.Null);
            root.SetReducedMotion(true);
            Assert.That(root.walletText.text, Is.EqualTo("무제한"));
            var gateway = (NotebookProjectGateway)root.gateway;
            NotebookProgressSnapshot saved = null;
            gateway.ReadVerifiedProgress(snapshot => saved = snapshot);
            Assert.That(saved, Is.Not.Null);
            foreach (string id in saved.clearedStageIds)
                Assert.That(id.StartsWith("SW", StringComparison.Ordinal) ? NotebookProjectGateway.IsStarwaySavedCleared(id) :
                    EchoTheaterCampaignController.IsSavedCleared(id), Is.True, "Only real clears may enter the saved-progress snapshot.");
            for (int i = 1; i <= 20; i++)
                foreach (string prefix in new[] { "SW", "ET-" })
                    Assert.That(NotebookCampaignPreview.IsTemporarilyCleared(prefix + i.ToString("00")), Is.True);
            root.OpenStages();
            foreach (var node in root.nodes.Where(n => n.stageId.StartsWith("SW", StringComparison.Ordinal) ||
                n.stageId.StartsWith("ET-", StringComparison.Ordinal)))
            {
                Assert.That(node.locked.gameObject.activeSelf, Is.False, node.stageId);
                Assert.That(node.star.sprite, Is.EqualTo(root.mapStarFrames[(2 * 3 + NotebookRoot.MapStarVariant(node.stageId)) * 4]), node.stageId);
            }
            root.nodes.First(n => n.stageId == stageId).button.onClick.Invoke();
            Assert.That(root.launchButton.interactable, Is.True);
            root.launchButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(JsonUtility.FromJson<NotebookWalletState>(PlayerPrefs.GetString(Wallet)).balance, Is.Zero);
        }

        static IEnumerator ExerciseRestarts(PuzzlePrototypeController puzzle, NotebookGameplayHud hud, string stageId)
        {
            Assert.That(puzzle && puzzle.Session != null && hud, Is.True);
            var map = puzzle.RuntimeMap;
            var view = puzzle.View;
            var session = puzzle.Session;
            var spawn = session.Cell;
            int initialStars = session.Collected;
            Assert.That(hud.gateway.TryRestartStage("wrong-stage"), Is.False);
            for (int i = 0; i < 12; i++)
            {
                bool moved = false;
                foreach (var direction in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down })
                    if (puzzle.RequestMove(direction)) { moved = true; break; }
                Assert.That(moved, Is.True, stageId + " has no initial move");
                // Restart during movement, including an input queued for its end.
                puzzle.RequestMove(Vector2Int.left);
                Assert.That(hud.restartButton.interactable, Is.True);
                hud.restartButton.onClick.Invoke();
                yield return null; yield return null;
                Assert.That(puzzle.RuntimeMap, Is.SameAs(map));
                Assert.That(puzzle.View, Is.SameAs(view));
                Assert.That(puzzle.Session, Is.SameAs(session));
                Assert.That(session.Cell, Is.EqualTo(spawn));
                Assert.That(session.Cost, Is.Zero);
                Assert.That(session.Collected, Is.EqualTo(initialStars));
                Assert.That(session.Won || session.Failed || puzzle.IsMoving, Is.False);
                Assert.That(hud.AuthoritativeCollected, Is.EqualTo(initialStars));
                Assert.That(hud.RetryGaugeReady, Is.True);
                Assert.That(hud.RetryGaugeRemaining, Is.EqualTo(NotebookGameplayHud.RetryGaugeCapacity));
                Assert.That(hud.restartButton.interactable, Is.True);
                Assert.That(hud.clearOverlay.activeSelf, Is.False);
            }
            hud.RequestPaused(true);
            Assert.That(puzzle.UiPaused, Is.True);
            Assert.That(hud.gateway.TryRestartStage(stageId), Is.False);
            hud.RequestPaused(false);
            Assert.That(puzzle.UiPaused, Is.False);
            Assert.That(JsonUtility.FromJson<NotebookWalletState>(PlayerPrefs.GetString(Wallet)).balance, Is.Zero);
        }
    }
}
#endif
