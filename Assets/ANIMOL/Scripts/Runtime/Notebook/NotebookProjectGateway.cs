using System;
using System.Collections;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WilliamParadox.Story;

namespace William.NotebookUI
{
    /// <summary>Binds active Starway MapData and the existing ranch story runner to the notebook.</summary>
    public sealed class NotebookProjectGateway : NotebookGateway
    {
        public PuzzleMap[] starwayMaps;
        public GameObject gameplayCanvasPrefab;
        public GameObject collectibleStarPrefab;
        public GameObject ranchStoryPrefab;

        readonly Dictionary<string, PuzzleMap> approvedEntries = new Dictionary<string, PuzzleMap>();
        GameObject replayInstance;

        public static bool IsStarwayCleared(string stageId) =>
            PlayerPrefs.GetInt("ANIMOL.Starway.Cleared." + stageId, 0) == 1;

        public override bool CanOpenStage(string stageId) => FindStarwayMap(stageId);

        PuzzleMap FindStarwayMap(string stageId)
        {
            if (string.IsNullOrEmpty(stageId) || starwayMaps == null) return null;
            foreach (PuzzleMap map in starwayMaps)
                if (map && map.name == stageId) return map;
            return null;
        }

        public override void ReadVerifiedProgress(Action<NotebookProgressSnapshot> completed)
        {
            var cleared = new List<string>();
            for (int i = 1; i <= 20; i++)
            {
                string id = "SW" + i.ToString("00");
                if (IsStarwayCleared(id)) cleared.Add(id);
            }
            for (int i = 1; i <= 30; i++)
            {
                string id = "ET-" + i.ToString("00");
                if (EchoTheaterCampaignController.IsCleared(id)) cleared.Add(id);
            }
            completed?.Invoke(new NotebookProgressSnapshot
            {
                verified = true,
                ownedThemes = Array.Empty<string>(),
                clearedStageIds = cleared.ToArray()
            });
        }

        public override void RequestStageEntry(string stageId, int telescopeCost, string operationId, Action<bool> approved)
        {
            PuzzleMap map = FindStarwayMap(stageId);
            bool valid = map && map.scenario && map.theme && gameplayCanvasPrefab &&
                GetComponentInParent<Canvas>() &&
                gameplayCanvasPrefab.GetComponentInChildren<NotebookGameplayHud>(true) &&
                !string.IsNullOrEmpty(operationId) && telescopeCost == 1 &&
                map.ValidateMap().Count == 0;
            if (valid && int.TryParse(stageId.Substring(2), out int order) && order > 1)
                valid = IsStarwayCleared("SW" + (order - 1).ToString("00"));
            if (valid) approvedEntries[operationId] = map;
            approved?.Invoke(valid);
        }

        public override void OpenAcceptedStage(string stageId, string operationId)
        {
            if (!approvedEntries.TryGetValue(operationId, out PuzzleMap map)) return;
            approvedEntries.Remove(operationId);
            if (!map || map.name != stageId) return;
            NotebookRoot notebook = GetComponent<NotebookRoot>();
            Canvas notebookCanvas = notebook ? notebook.GetComponentInParent<Canvas>() : null;
            if (!notebookCanvas || !gameplayCanvasPrefab) return;
            GameObject session = null, canvas = null;
            try
            {
                session = new GameObject("Starway Ranch " + stageId);
                var controller = session.AddComponent<PuzzleController>();
                controller.stage = map;
                controller.hudPrefab = null;
                canvas = Instantiate(gameplayCanvasPrefab);
                var hud = canvas.GetComponentInChildren<NotebookGameplayHud>(true);
                if (!hud) throw new InvalidOperationException("GameplayCanvas has no NotebookGameplayHud.");
                var liveGateway = session.AddComponent<NotebookStarwaySessionGateway>();
                var bridge = session.AddComponent<NotebookMapDataPreviewBridge>();
                bridge.sourceMap = map;
                bridge.puzzle = controller;
                bridge.hud = hud;
                bridge.gateway = liveGateway;
                bridge.collectibleStarPrefab = collectibleStarPrefab;
                liveGateway.bridge = bridge;
                // The full-screen paper base is a sibling of NotebookRoot on this Canvas.
                // Hide the whole menu Canvas so it cannot cover the puzzle camera.
                notebookCanvas.gameObject.SetActive(false);
            }
            catch (Exception error)
            {
                if (session) Destroy(session);
                if (canvas) Destroy(canvas);
                Debug.LogError("Starway session could not open after approval: " + error, this);
            }
        }

        public override void RequestStoryReplay(string stageId, string operationId, Action<bool> completed)
        {
            NotebookRoot notebook = GetComponent<NotebookRoot>();
            if (!IsStarwayCleared(stageId) || !FindStarwayMap(stageId) || !ranchStoryPrefab ||
                replayInstance || !notebook || !notebook.MapSelectionVisible ||
                notebook.SelectedStageId != stageId || string.IsNullOrEmpty(operationId))
            {
                completed?.Invoke(false);
                return;
            }
            GameObject instance = null;
            try
            {
                instance = Instantiate(ranchStoryPrefab);
                var runner = instance.GetComponentInChildren<StoryRunner>(true);
                var canvas = instance.GetComponentInChildren<Canvas>(true);
                if (!runner || !canvas || !notebook.mapBack)
                    throw new InvalidOperationException("Ranch story prefab is missing its runner, canvas or return control.");
                runner.restoreOnStart = false;
                runner.persistProgress = false;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 200;
                foreach (EventSystem events in instance.GetComponentsInChildren<EventSystem>(true)) events.enabled = false;
                GameObject back = Instantiate(notebook.mapBack.gameObject, canvas.transform, false);
                back.name = "Return to Constellation Map";
                var button = back.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(CloseReplay);
                replayInstance = instance;
                StartCoroutine(ConfirmStoryStarted(notebook, stageId, runner, completed));
            }
            catch (Exception error)
            {
                if (instance) Destroy(instance);
                replayInstance = null;
                Debug.LogError("Ranch story replay could not start: " + error, this);
                completed?.Invoke(false);
            }
        }

        IEnumerator ConfirmStoryStarted(NotebookRoot notebook, string stageId, StoryRunner runner, Action<bool> completed)
        {
            yield return null;
            bool valid = replayInstance && runner && runner.Package != null &&
                runner.ValidationErrors.Count == 0 && notebook && notebook.MapSelectionVisible &&
                notebook.SelectedStageId == stageId && notebook.stageScreen.activeInHierarchy;
            if (!valid) CloseReplay();
            completed?.Invoke(valid);
        }

        public void CloseReplay()
        {
            if (!replayInstance) return;
            Destroy(replayInstance);
            replayInstance = null;
        }

        void OnDisable() => CloseReplay();

        public override void GetLocalizedPrice(string themeId, string productId, Action<string> completed) => completed?.Invoke(null);
        public override void Purchase(string themeId, string productId, string operationId, Action<NotebookPurchaseResult> completed) => completed?.Invoke(new NotebookPurchaseResult { status = NotebookOperationStatus.Failed, themeId = themeId });
        public override void Restore(string operationId, Action<NotebookRestoreResult> completed) => completed?.Invoke(new NotebookRestoreResult { status = NotebookOperationStatus.Failed });
        public override void RewardedAd(string operationId, Action<NotebookRewardResult> completed) => completed?.Invoke(new NotebookRewardResult { status = NotebookOperationStatus.Failed });
        public override void OpenBattle(string[] allFourThemes) { }
        public override void OpenCustom() { }
        public override void Login() { }
    }
}
