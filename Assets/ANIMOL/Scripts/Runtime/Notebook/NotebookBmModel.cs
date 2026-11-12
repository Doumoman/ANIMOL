using System;
using System.Collections.Generic;

namespace William.NotebookUI
{
    [Serializable] public sealed class NotebookWalletState
    {
        public int balance = 10;
        public long lastRecoveryUnix;
        public List<string> ownedThemes = new List<string> { "starway" };
        public List<string> clearedStageIds = new List<string>();
        public List<string> rewardIds = new List<string>();
        public List<string> entryIds = new List<string>();
    }

    /// <summary>Local UI policy. Only a trusted gateway can approve real rewards, purchases and entry.</summary>
    public sealed class NotebookBmModel
    {
        public NotebookCatalog Catalog { get; }
        public NotebookWalletState State { get; }
        public bool UnlimitedTelescopes { get; }
        public bool AllStagesCleared { get; }
        public bool CanAffordStageEntry => UnlimitedTelescopes || State.balance >= Catalog.stageEntryCost;
        private readonly bool campaignPreview;
        public NotebookBmModel(NotebookCatalog catalog, NotebookWalletState state, long now, bool campaignPreview = false,
            bool unlimitedTelescopes = false, bool allStagesCleared = false)
        {
            this.campaignPreview = campaignPreview;
            UnlimitedTelescopes = campaignPreview && unlimitedTelescopes;
            AllStagesCleared = campaignPreview && allStagesCleared;
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            State = state ?? new NotebookWalletState { balance = catalog.telescopeCapacity, lastRecoveryUnix = now };
            State.ownedThemes = State.ownedThemes ?? new List<string> { "starway" };
            State.clearedStageIds = State.clearedStageIds ?? new List<string>();
            State.rewardIds = State.rewardIds ?? new List<string>();
            State.entryIds = State.entryIds ?? new List<string>();
            State.balance = Math.Max(0, Math.Min(catalog.telescopeCapacity, State.balance));
            if (State.lastRecoveryUnix <= 0) State.lastRecoveryUnix = now;
            Tick(now);
        }
        public void Tick(long now)
        {
            if (UnlimitedTelescopes) return;
            if (now < State.lastRecoveryUnix) return;
            if (State.balance >= Catalog.telescopeCapacity) { State.lastRecoveryUnix = now; return; }
            long units = (now - State.lastRecoveryUnix) / Catalog.recoverySeconds;
            if (units <= 0) return;
            int grant = (int)Math.Min(units, Catalog.telescopeCapacity - State.balance);
            State.balance += grant;
            State.lastRecoveryUnix = State.balance == Catalog.telescopeCapacity ? now : State.lastRecoveryUnix + (long)grant * Catalog.recoverySeconds;
        }
        public long SecondsUntilRecovery(long now)
        {
            if (UnlimitedTelescopes) return 0;
            Tick(now);
            return State.balance == Catalog.telescopeCapacity ? 0 : Math.Max(1, Catalog.recoverySeconds - (now - State.lastRecoveryUnix));
        }
        public bool Owns(string themeId) => themeId == "starway" ||
            (campaignPreview && themeId == "theater") || State.ownedThemes.Contains(themeId);
        public bool IsCleared(string stageId) => State.clearedStageIds.Contains(stageId) ||
            (AllStagesCleared && Catalog.FindStage(stageId) != null &&
             NotebookCampaignStages.TryOrder(stageId, out string prefix, out _) && (prefix == "SW" || prefix == "ET-"));
        public bool CanEnter(string stageId)
        {
            NotebookStage stage = Catalog.FindStage(stageId);
            if (stage == null || !Owns(stage.themeId)) return false;
            if (IsCleared(stageId)) return true;
            NotebookStage previous = Array.Find(Catalog.stages, s => s.themeId == stage.themeId && s.order == stage.order - 1);
            return previous == null || IsCleared(previous.id);
        }
        public bool BattleUnlocked()
        {
            if (Catalog.requiredStagePolicyIsExample) return false;
            string[] required = Catalog.battleRequiredStageIds;
            return required != null && required.Length > 0 && Array.TrueForAll(required, IsCleared);
        }
        public int RewardAmount(long now) { Tick(now); return UnlimitedTelescopes ? 0 : Math.Min(Catalog.rewardedAdAmount, Catalog.telescopeCapacity - State.balance); }
        public bool AcceptVerifiedReward(string grantId, bool verified, long now, out int amount)
        {
            amount = 0;
            if (!verified || string.IsNullOrEmpty(grantId) || State.rewardIds.Contains(grantId)) return false;
            amount = RewardAmount(now);
            State.rewardIds.Add(grantId);
            State.balance += amount;
            if (State.balance == Catalog.telescopeCapacity) State.lastRecoveryUnix = now;
            return true;
        }
        public bool AcceptVerifiedPurchase(string themeId, bool verified)
        {
            NotebookTheme theme = Catalog.FindTheme(themeId);
            if (!verified || theme == null || !theme.paid) return false;
            if (!State.ownedThemes.Contains(themeId)) State.ownedThemes.Add(themeId);
            return true;
        }
        public bool AcceptApprovedEntry(string operationId, string stageId, bool approved, long now, out bool duplicate)
        {
            duplicate = State.entryIds.Contains(operationId);
            if (duplicate) return true;
            Tick(now);
            if (!approved || string.IsNullOrEmpty(operationId) || !CanEnter(stageId) || !CanAffordStageEntry) return false;
            if (!UnlimitedTelescopes)
            {
                if (State.balance == Catalog.telescopeCapacity) State.lastRecoveryUnix = now;
                State.balance -= Catalog.stageEntryCost;
            }
            State.entryIds.Add(operationId);
            return true;
        }
        public bool AcceptVerifiedClear(string stageId, bool verified)
        {
            if (!verified || Catalog.FindStage(stageId) == null) return false;
            if (!State.clearedStageIds.Contains(stageId)) State.clearedStageIds.Add(stageId);
            return true;
        }
    }
}
