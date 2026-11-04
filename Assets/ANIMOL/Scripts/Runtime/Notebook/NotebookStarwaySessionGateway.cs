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
        readonly HashSet<string> approvedMapOperations = new HashSet<string>();
        bool clearSaved;

        void Update()
        {
            if (clearSaved || !bridge || !bridge.Ready) return;
            var puzzle = GetComponent<PuzzlePrototypeController>();
            if (!puzzle || !puzzle.Session.Won || puzzle.IsMoving ||
                puzzle.Session.Cell != puzzle.RuntimeMap.exit) return;
            string stageId = bridge.StageId;
            if (string.IsNullOrEmpty(stageId) || bridge.sourceMap.name != stageId) return;
            PlayerPrefs.SetInt("ANIMOL.Starway.Cleared." + stageId, 1);
            PlayerPrefs.Save();
            clearSaved = true;
        }

        public override void RequestPause(bool paused, string operationId, Action<bool> completed) =>
            completed?.Invoke(bridge && bridge.TrySetPaused(paused));

        public override void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed)
        {
            bool approved = destination == NotebookGameplayDestination.Map && bridge && bridge.Ready &&
                bridge.StageId == currentStageId && !string.IsNullOrEmpty(operationId) &&
                approvedMapOperations.Add(operationId);
            completed?.Invoke(approved);
        }

        public override void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId)
        {
            if (destination != NotebookGameplayDestination.Map || !approvedMapOperations.Remove(operationId)) return;
            SceneManager.LoadScene("NotebookUI");
        }
    }
}
