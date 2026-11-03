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
        readonly HashSet<string> approvedMapOperations = new HashSet<string>();

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
            bool approved = destination == NotebookGameplayDestination.Map && campaign && campaign.puzzle &&
                campaign.puzzle.Session != null && currentStageId == campaign.CurrentStageId &&
                !string.IsNullOrEmpty(operationId);
            if (approved) approvedMapOperations.Add(operationId);
            // Next-stage admission and the five-use retry ledger have no connected authority.
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
