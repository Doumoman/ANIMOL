using System;
using System.Collections.Generic;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>The map editor preview can pause or return to authoring; it cannot grant retry charges.</summary>
    public sealed class NotebookMapDataPreviewGateway : NotebookGameplayGateway
    {
        public NotebookMapDataPreviewBridge bridge;
        readonly HashSet<string> approvedExits = new HashSet<string>();

        public override void RequestPause(bool paused, string operationId, Action<bool> completed) =>
            completed?.Invoke(bridge && bridge.TrySetPaused(paused));

        public override void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed)
        {
            bool approved = destination == NotebookGameplayDestination.Map && bridge && bridge.Ready &&
                bridge.StageId == currentStageId && !string.IsNullOrEmpty(operationId) && approvedExits.Add(operationId);
            completed?.Invoke(approved);
        }

        public override void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId)
        {
            if (destination != NotebookGameplayDestination.Map || !approvedExits.Remove(operationId)) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene("NotebookUI");
#endif
        }
    }
}
