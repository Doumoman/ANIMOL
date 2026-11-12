using System;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace William.NotebookUI
{
    /// <summary>Only the Echo Theater's existing pause input and map loader are available here.</summary>
    public sealed class NotebookEchoTheaterGateway : NotebookGameplayGateway
    {
        public EchoTheaterCampaignController campaign;
        public override bool UnlimitedRestarts => NotebookCampaignPreview.HasUnlimitedRerolls;
        public override bool CanRestartStage(string stageId) => UnlimitedRestarts && campaign && campaign.puzzle &&
            campaign.CurrentStageId == stageId && campaign.puzzle.Session != null &&
            !campaign.puzzle.UiPaused && !campaign.puzzle.Session.Won;
        public override bool TryRestartStage(string stageId) => CanRestartStage(stageId) && campaign.TryRestartCurrentStage();
        readonly Dictionary<string, (NotebookGameplayDestination destination, string current, string target)> approvedOperations =
            new Dictionary<string, (NotebookGameplayDestination, string, string)>();
        public override bool TryGetNextStage(string currentStageId, out string nextStageId)
        {
            nextStageId = null;
            return campaign && campaign.CurrentStageId == currentStageId && campaign.TryGetNextStage(out nextStageId);
        }

        public override void RequestPause(bool paused, string operationId, Action<bool> completed)
        {
            var puzzle = campaign ? campaign.puzzle : null;
            var input = puzzle ? puzzle.GetComponent<PuzzleSwipeInput>() : null;
            if (!puzzle || puzzle.Session == null || !input || puzzle.IsMoving || puzzle.Session.Won)
            {
                completed?.Invoke(false);
                return;
            }
            input.CancelGesture();
            puzzle.SetUiPaused(paused);
            input.enabled = !paused;
            completed?.Invoke(true);
        }

        public override void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed)
        {
            bool approved = campaign && campaign.puzzle &&
                campaign.puzzle.Session != null && currentStageId == campaign.CurrentStageId &&
                !string.IsNullOrEmpty(operationId) && !approvedOperations.ContainsKey(operationId) &&
                (destination == NotebookGameplayDestination.Map || destination == NotebookGameplayDestination.NextStage &&
                    TryGetNextStage(currentStageId, out string next) && next == targetStageId);
            if (approved) approvedOperations.Add(operationId, (destination, currentStageId, targetStageId));
            completed?.Invoke(approved);
        }

        public override void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId)
        {
            if (!approvedOperations.TryGetValue(operationId, out var approved) || approved.destination != destination ||
                approved.current != currentStageId || approved.target != targetStageId) return;
            approvedOperations.Remove(operationId);
            if (!campaign || campaign.CurrentStageId != currentStageId) return;
            if (destination == NotebookGameplayDestination.NextStage) campaign.OpenNextStage(currentStageId, targetStageId);
            else if (destination == NotebookGameplayDestination.Map) SceneManager.LoadScene("NotebookUI");
        }
    }
}
