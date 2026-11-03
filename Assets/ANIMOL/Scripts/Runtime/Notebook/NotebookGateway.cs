using System;
using UnityEngine;

namespace William.NotebookUI
{
    public enum NotebookOperationStatus { Failed, Cancelled, Verified, Unknown }
    public sealed class NotebookPurchaseResult { public NotebookOperationStatus status; public string themeId; }
    public sealed class NotebookRewardResult { public NotebookOperationStatus status; public string grantId; }
    public sealed class NotebookRestoreResult { public NotebookOperationStatus status; public string[] ownedThemes; }
    public sealed class NotebookProgressSnapshot
    {
        public bool verified;
        public string[] ownedThemes, clearedStageIds;
    }
    [Serializable] public sealed class NotebookPreferences
    {
        public float bgm = .7f, sfx = .85f;
        public bool sound = true, haptic = true;
    }

    /// <summary>
    /// Implement in the actual game. Call callbacks once, on Unity's main thread, after verification.
    /// Never return Verified for a button click, ad close, unvalidated receipt, or a DEV response.
    /// RequestStageEntry approves entry only; OpenAcceptedStage performs the scene hand-off AFTER wallet persistence.
    /// </summary>
    public abstract class NotebookGateway : MonoBehaviour
    {
        public virtual bool CanApproveStageEntry => false;
        public virtual bool CanOpenBattle => false;
        public virtual bool CanOpenCustom => false;
        public virtual bool CanLogin => false;
        public virtual bool CanShowRewardedAd => false;
        public virtual bool CanRestorePurchases => false;
        public abstract void GetLocalizedPrice(string themeId, string productId, Action<string> completed);
        public abstract void Purchase(string themeId, string productId, string operationId, Action<NotebookPurchaseResult> completed);
        public abstract void Restore(string operationId, Action<NotebookRestoreResult> completed);
        public abstract void RewardedAd(string operationId, Action<NotebookRewardResult> completed);
        public abstract void RequestStageEntry(string stageId, int telescopeCost, string operationId, Action<bool> approved);
        public abstract void OpenAcceptedStage(string stageId, string operationId);
        public abstract void ReadVerifiedProgress(Action<NotebookProgressSnapshot> completed);
        public abstract void OpenBattle(string[] allFourThemes);
        public abstract void OpenCustom();
        public abstract void Login();
        public virtual void ApplyPreferences(NotebookPreferences preferences) { }
    }
}
