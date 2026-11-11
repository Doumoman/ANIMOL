using System;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace William.NotebookUI
{
    /// <summary>Temporary play bridge for active Starway MapData; retry gauge remains unconnected.</summary>
    public sealed class NotebookStarwaySessionGateway : NotebookGameplayGateway
    {
        public NotebookMapDataPreviewBridge bridge;
        public PuzzleMap[] campaignMaps;
        readonly Dictionary<string, (string current, string target)> approvedNext = new Dictionary<string, (string, string)>();
        PuzzleMap NextMap(string current)
        {
            if (!bridge || !bridge.CanAdvance || bridge.StageId != current ||
                !NotebookCampaignStages.TryNext(current, out string next)) return null;
            return NotebookCampaignStages.FindStarwayMap(campaignMaps, next);
        }
        public override bool TryGetNextStage(string currentStageId, out string nextStageId)
        {
            nextStageId = null;
            return NextMap(currentStageId) && NotebookCampaignStages.TryNext(currentStageId, out nextStageId);
        }
        readonly HashSet<string> approvedMapOperations = new HashSet<string>();
        bool clearSaved;

        void Update()
        {
            if (clearSaved || !bridge || !bridge.Ready) return;
            var puzzle = GetComponent<PuzzlePrototypeController>();
            if (!puzzle || !puzzle.Session.Won || puzzle.IsMoving ||
                puzzle.Session.Cell != puzzle.RuntimeMap.exit) return;
            string stageId = bridge.StageId;
            if (NotebookCampaignStages.FindStarwayMap(campaignMaps, stageId) != bridge.sourceMap) return;
            string mapId = StarwayStageCatalog.SourceId(bridge.sourceMap);
            if (string.IsNullOrEmpty(mapId)) return;
            PlayerPrefs.SetInt(NotebookProjectGateway.MapClearKey(mapId), 1);
            PlayerPrefs.Save();
            clearSaved = true;
        }

        public override void RequestPause(bool paused, string operationId, Action<bool> completed) =>
            completed?.Invoke(bridge && bridge.TrySetPaused(paused));

        public override void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed)
        {
            if (destination == NotebookGameplayDestination.NextStage)
            {
                bool allowed = bridge && bridge.Ready && !string.IsNullOrEmpty(operationId) && !approvedNext.ContainsKey(operationId) &&
                    TryGetNextStage(currentStageId, out string next) && next == targetStageId &&
                    GetComponent<PuzzlePrototypeController>().Session.Won && !GetComponent<PuzzlePrototypeController>().IsMoving;
                if (allowed) approvedNext.Add(operationId, (currentStageId, targetStageId));
                completed?.Invoke(allowed); return;
            }
            bool approved = destination == NotebookGameplayDestination.Map && bridge && bridge.Ready &&
                bridge.StageId == currentStageId && !string.IsNullOrEmpty(operationId) &&
                approvedMapOperations.Add(operationId);
            completed?.Invoke(approved);
        }

        public override void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId)
        {
            if (destination == NotebookGameplayDestination.NextStage)
            {
                if (!approvedNext.TryGetValue(operationId, out var approved) || approved.current != currentStageId || approved.target != targetStageId) return;
                approvedNext.Remove(operationId);
                Update(); // Persist the verified source-map clear before switching sessions.
                if (bridge.OpenNextMap(NextMap(currentStageId), targetStageId)) clearSaved = false;
                return;
            }
            if (destination != NotebookGameplayDestination.Map || !approvedMapOperations.Remove(operationId)) return;
            SceneManager.LoadScene("NotebookUI");
        }
    }
}
