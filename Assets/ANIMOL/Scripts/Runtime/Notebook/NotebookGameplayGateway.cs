using System;
using UnityEngine;

namespace William.NotebookUI
{
    public enum NotebookGameplayDestination { Retry, Map, NextStage }
    public enum NotebookGameplayFeedback { StarTouched, StarArrived, StageClear, ButtonPressed }

    /// <summary>The existing game owns input, pause, verified progress, entry approval and every BM charge.</summary>
    public abstract class NotebookGameplayGateway : MonoBehaviour
    {
        public virtual bool UnlimitedRestarts => false;
        public virtual bool CanRestartStage(string stageId) => false;
        public virtual bool TryRestartStage(string stageId) => false;
        public virtual bool TryGetNextStage(string currentStageId, out string nextStageId)
        { nextStageId = null; return false; }
        // Complete once, on Unity's main thread, after the game's pause/input state is updated.
        public abstract void RequestPause(bool paused, string operationId, Action<bool> completed);
        // Approve with the existing loader/entry policy; do not open the destination until OpenApprovedNavigation.
        public abstract void RequestNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId, Action<bool> completed);
        public abstract void OpenApprovedNavigation(NotebookGameplayDestination destination, string currentStageId,
            string targetStageId, string operationId);
        // A restart uses the host's five-part gauge. This UI never owns or charges a telescope wallet.
        // Default is fail-closed so existing providers are not silently treated as retry approvals.
        public virtual void RequestRestart(string currentStageId, string sessionId, long expectedRevision,
            string operationId, Action<NotebookGameplayRestartApproval> completed)
        {
            completed?.Invoke(new NotebookGameplayRestartApproval { approved = false, operationId = operationId, sessionId = sessionId });
        }
        // Route into the game's existing audio/haptic preferences. No sounds or haptics are forced by this package.
        public virtual void Feedback(NotebookGameplayFeedback feedback) { }
    }

    [Serializable]
    public sealed class NotebookGameplayRetryGaugeSnapshot
    {
        public string sessionId;
        public int remaining;
        public long revision;
    }
    [Serializable]
    public sealed class NotebookGameplayRestartApproval
    {
        public bool approved;
        public string operationId, sessionId;
        public int remaining;
        public long revision;
    }

    [Serializable]
    public sealed class NotebookGameplayJarState
    {
        public int required, collected;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class NotebookGameplayClearSnapshot
    {
        public string stageId, stageLabel, nextStageId, nextBlockedReason;
        public bool verified, exitReached, hasNextStage, canEnterNext;
        public int collectedStars;
        public int requiredStars = 3;
    }
}
