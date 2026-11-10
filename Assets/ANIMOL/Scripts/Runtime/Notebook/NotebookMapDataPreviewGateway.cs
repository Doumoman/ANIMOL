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
        readonly Dictionary<string, (string current, string target)> approvedNext = new Dictionary<string, (string, string)>();
        ANIMOL.Puzzle.PuzzleMap NextMap(string current)
        {
#if UNITY_EDITOR
            if (!bridge || !bridge.CanAdvance || bridge.StageId != current ||
                !NotebookCampaignStages.TryNext(current, out string next)) return null;
            string path = UnityEditor.AssetDatabase.GetAssetPath(bridge.sourceMap);
            if (current.StartsWith("ET-"))
            {
                var set = UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Puzzle.EchoTheaterMapSet>(
                    "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
                if (set) foreach (var entry in set.maps) if (entry.stageId == next) return entry.map;
            }
            if (NotebookProjectGateway.TryStarwayMapId(next, out string sourceId) && path.Contains("/MapData/starway/"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Puzzle.PuzzleMap>(
                    System.IO.Path.GetDirectoryName(path).Replace('\\', '/') + "/" + sourceId + "_MapData.asset");
#endif
            return null;
        }
        public override bool TryGetNextStage(string currentStageId, out string nextStageId)
        {
            nextStageId = null;
            return NextMap(currentStageId) && NotebookCampaignStages.TryNext(currentStageId, out nextStageId);
        }

        public override void RequestPause(bool paused, string operationId, Action<bool> completed) =>
            completed?.Invoke(bridge && bridge.TrySetPaused(paused));

        public override void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed)
        {
            if (destination == NotebookGameplayDestination.NextStage)
            {
                bool allowed = bridge && bridge.Ready && !string.IsNullOrEmpty(operationId) && !approvedNext.ContainsKey(operationId) &&
                    TryGetNextStage(currentStageId, out string next) && next == targetStageId;
                if (allowed) approvedNext.Add(operationId, (currentStageId, targetStageId));
                completed?.Invoke(allowed); return;
            }
            bool approved = destination == NotebookGameplayDestination.Map && bridge && bridge.Ready &&
                bridge.StageId == currentStageId && !string.IsNullOrEmpty(operationId) && approvedExits.Add(operationId);
            completed?.Invoke(approved);
        }

        public override void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId)
        {
            if (destination == NotebookGameplayDestination.NextStage)
            {
                if (!approvedNext.TryGetValue(operationId, out var approved) || approved.current != currentStageId || approved.target != targetStageId) return;
                approvedNext.Remove(operationId);
                bridge.OpenNextMap(NextMap(currentStageId), targetStageId); return;
            }
            if (destination != NotebookGameplayDestination.Map || !approvedExits.Remove(operationId)) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene("NotebookUI");
#endif
        }
    }
}
