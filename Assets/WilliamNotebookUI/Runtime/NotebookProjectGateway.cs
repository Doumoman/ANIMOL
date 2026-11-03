using System;
using System.Collections.Generic;
using ANIMOL.Puzzle;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>Reads the active Echo Theater clear record. Commerce, ads and entry need real services.</summary>
    public sealed class NotebookProjectGateway : NotebookGateway
    {
        public override void ReadVerifiedProgress(Action<NotebookProgressSnapshot> completed)
        {
            var cleared = new List<string>();
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

        public override void GetLocalizedPrice(string themeId, string productId, Action<string> completed) => completed?.Invoke(null);
        public override void Purchase(string themeId, string productId, string operationId, Action<NotebookPurchaseResult> completed) => completed?.Invoke(new NotebookPurchaseResult { status = NotebookOperationStatus.Failed, themeId = themeId });
        public override void Restore(string operationId, Action<NotebookRestoreResult> completed) => completed?.Invoke(new NotebookRestoreResult { status = NotebookOperationStatus.Failed });
        public override void RewardedAd(string operationId, Action<NotebookRewardResult> completed) => completed?.Invoke(new NotebookRewardResult { status = NotebookOperationStatus.Failed });
        public override void RequestStageEntry(string stageId, int telescopeCost, string operationId, Action<bool> approved) => approved?.Invoke(false);
        public override void OpenAcceptedStage(string stageId, string operationId) { }
        public override void OpenBattle(string[] allFourThemes) { }
        public override void OpenCustom() { }
        public override void Login() { }
    }
}
