using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Core
{
    public enum AnimalGrowthTrackKind { Active, Passive }
    public enum EmoteAcquisitionKind { Free, CoinExchange, Paid }
    public enum AccountIdentityState { Guest, GoogleLinkRequired, Linked }
    public enum SaveRecordSource { Local, Cloud }
    public enum RewardedAdRewardType { LobbyGeneralCoinDraft, CampaignBaseCoinDouble }
    public enum RewardGrantStatus { Pending, Approved, Failed, Cancelled, Unavailable, Duplicate, Busy, LoginRequired, QuotaExceeded, Invalid }

    [Serializable]
    public sealed class AnimalGrowthTrackContract
    {
        [SerializeField] private AnimalGrowthTrackKind kind;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private string effectKey = string.Empty;
        [SerializeField] private int[] sharedCoinCostByLevel = Array.Empty<int>();
        [SerializeField] private int[] masteryCostByLevel = Array.Empty<int>();
        public AnimalGrowthTrackKind Kind => kind;
        public int MaxLevel => maxLevel;
        public string EffectKey => effectKey;
        public IReadOnlyList<int> SharedCoinCostByLevel => sharedCoinCostByLevel;
        public IReadOnlyList<int> MasteryCostByLevel => masteryCostByLevel;
        public bool IsConfigured => maxLevel == 5 && !string.IsNullOrWhiteSpace(effectKey) &&
                                    sharedCoinCostByLevel?.Length == maxLevel && masteryCostByLevel?.Length == maxLevel;
    }

    [Serializable]
    public sealed class AnimalGrowthContract
    {
        [SerializeField] private string animalId = string.Empty;
        [SerializeField] private string requiredCompletedStageId = string.Empty;
        [SerializeField] private string masteryGainPolicyKey = string.Empty;
        [SerializeField] private AnimalGrowthTrackContract[] tracks = Array.Empty<AnimalGrowthTrackContract>();
        public string AnimalId => animalId;
        public string RequiredCompletedStageId => requiredCompletedStageId;
        public string MasteryGainPolicyKey => masteryGainPolicyKey;
        public IReadOnlyList<AnimalGrowthTrackContract> Tracks => tracks;
        public bool HasTwoLevelFiveTracks => tracks != null && tracks.Length == 2 &&
                                             tracks.Any(x => x != null && x.Kind == AnimalGrowthTrackKind.Active && x.MaxLevel == 5) &&
                                             tracks.Any(x => x != null && x.Kind == AnimalGrowthTrackKind.Passive && x.MaxLevel == 5);
        public bool IsUnlocked(CampaignProgress progress) => string.IsNullOrWhiteSpace(requiredCompletedStageId) ||
                                                             progress != null && progress.CompletedStageIds.Contains(requiredCompletedStageId);
        public bool IsOperationallyConfigured => HasTwoLevelFiveTracks && !string.IsNullOrWhiteSpace(masteryGainPolicyKey) && tracks.All(x => x.IsConfigured);
    }

    [Serializable]
    public sealed class GrowthCompletionPackContract
    {
        [SerializeField] private string offerKey = "ACCOUNT_GROWTH_COMPLETE_PACK";
        [SerializeField] private string[] includedAccountTrackIds = Array.Empty<string>();
        [SerializeField] private bool includesAnimalGrowth;
        [SerializeField] private bool includesAnimalUnlocks;
        [SerializeField] private bool cosmeticCompensationFormulaConfigured;
        [SerializeField] private bool authenticatedSaleEnabled;
        public string OfferKey => offerKey;
        public IReadOnlyList<string> IncludedAccountTrackIds => includedAccountTrackIds;
        public bool IncludesAnimalGrowth => includesAnimalGrowth;
        public bool IncludesAnimalUnlocks => includesAnimalUnlocks;
        public bool CosmeticCompensationFormulaConfigured => cosmeticCompensationFormulaConfigured;
        public bool AuthenticatedSaleEnabled => authenticatedSaleEnabled;
        public bool CanSell => includedAccountTrackIds?.Length == 3 && !includesAnimalGrowth && !includesAnimalUnlocks &&
                               cosmeticCompensationFormulaConfigured && authenticatedSaleEnabled;
    }

    [Serializable]
    public sealed class EmoteAcquisitionContract
    {
        [SerializeField] private string emoteId = string.Empty;
        [SerializeField] private EmoteAcquisitionKind acquisitionKind;
        [SerializeField] private string authenticatedOfferKey = string.Empty;
        public string EmoteId => emoteId;
        public EmoteAcquisitionKind AcquisitionKind => acquisitionKind;
        public string AuthenticatedOfferKey => authenticatedOfferKey;
        public bool RequiresAuthenticatedSupply => acquisitionKind != EmoteAcquisitionKind.Free;
    }

    public readonly struct AuthenticatedOfferSnapshot
    {
        public string OfferKey { get; }
        public string DisplayPrice { get; }
        public bool Owned { get; }
        public bool IsAuthenticatedSupply { get; }
        public AuthenticatedOfferSnapshot(string offerKey, string displayPrice, bool owned, bool isAuthenticatedSupply)
        {
            OfferKey = offerKey ?? string.Empty;
            DisplayPrice = displayPrice ?? string.Empty;
            Owned = owned;
            IsAuthenticatedSupply = isAuthenticatedSupply;
        }
        public bool MayDisplayOperationalPriceOrOwnership => IsAuthenticatedSupply && !string.IsNullOrWhiteSpace(OfferKey);
    }

    public readonly struct AccountAccessSnapshot
    {
        public AccountIdentityState IdentityState { get; }
        public string AccountId { get; }
        public bool AccountServerConnected { get; }
        public bool PurchaseSdkConnected { get; }
        public bool CanStartGuestCampaign => true;
        public bool CanUseOnline => IdentityState == AccountIdentityState.Linked && AccountServerConnected;
        public bool CanPurchase => CanUseOnline && PurchaseSdkConnected;
        public AccountAccessSnapshot(AccountIdentityState identityState, string accountId, bool accountServerConnected, bool purchaseSdkConnected)
        {
            IdentityState = identityState;
            AccountId = accountId ?? string.Empty;
            AccountServerConnected = accountServerConnected;
            PurchaseSdkConnected = purchaseSdkConnected;
        }
        public static AccountAccessSnapshot GuestDisconnected => new AccountAccessSnapshot(AccountIdentityState.Guest, string.Empty, false, false);
    }

    public readonly struct CampaignRecordSummary
    {
        public SaveRecordSource Source { get; }
        public bool IsAvailable { get; }
        public bool IsAuthoritative { get; }
        public int CompletedStages { get; }
        public int BestTimeCount { get; }
        public int Revision { get; }
        public CampaignRecordSummary(SaveRecordSource source, bool available, bool authoritative, int completedStages, int bestTimeCount, int revision)
        {
            Source = source;
            IsAvailable = available;
            IsAuthoritative = authoritative;
            CompletedStages = Math.Max(0, completedStages);
            BestTimeCount = Math.Max(0, bestTimeCount);
            Revision = Math.Max(0, revision);
        }
    }

    public sealed class SaveConflictSelectionService
    {
        public CampaignRecordSummary Local { get; }
        public CampaignRecordSummary Cloud { get; }
        public SaveRecordSource? SelectedSource { get; private set; }
        public bool DevelopmentPreviewOnly { get; }
        public SaveConflictSelectionService(CampaignRecordSummary local, CampaignRecordSummary cloud, bool developmentPreviewOnly)
        {
            Local = local;
            Cloud = cloud;
            DevelopmentPreviewOnly = developmentPreviewOnly;
        }
        public bool TrySelect(SaveRecordSource source, bool irreversibleScopeAcknowledged)
        {
            var record = source == SaveRecordSource.Local ? Local : Cloud;
            if (!irreversibleScopeAcknowledged || !record.IsAvailable) return false;
            SelectedSource = source;
            return true;
        }
    }

    public readonly struct CampaignCoinRewardBreakdown
    {
        public int BaseCoinB { get; }
        public int TimeBonusT { get; }
        public int OptionalBonusO { get; }
        public bool IsFirstClear { get; }
        public int TotalWithoutAd => BaseCoinB + TimeBonusT + OptionalBonusO;
        public int AdAdditionalBaseB => BaseCoinB;
        public int TotalWithApprovedAd => 2 * BaseCoinB + TimeBonusT + OptionalBonusO;
        public CampaignCoinRewardBreakdown(int baseCoinB, int timeBonusT, int optionalBonusO, bool firstClear)
        {
            if (baseCoinB < 0 || timeBonusT < 0 || optionalBonusO < 0) throw new ArgumentOutOfRangeException(nameof(baseCoinB));
            BaseCoinB = baseCoinB;
            TimeBonusT = timeBonusT;
            OptionalBonusO = optionalBonusO;
            IsFirstClear = firstClear;
        }
    }

    public readonly struct DailyAdQuotaSnapshot
    {
        public bool IsServerAuthoritative { get; }
        public int UsedCount { get; }
        public int Limit { get; }
        public string ResetAtServerIso8601 { get; }
        public int? RemainingCount => IsServerAuthoritative ? Math.Max(0, Limit - UsedCount) : (int?)null;
        public DailyAdQuotaSnapshot(bool serverAuthoritative, int usedCount, int limit, string resetAtServerIso8601)
        {
            IsServerAuthoritative = serverAuthoritative;
            UsedCount = Math.Max(0, usedCount);
            Limit = Math.Max(0, limit);
            ResetAtServerIso8601 = resetAtServerIso8601 ?? string.Empty;
        }
        public static DailyAdQuotaSnapshot Unavailable(int limit) => new DailyAdQuotaSnapshot(false, 0, limit, string.Empty);
    }

    public readonly struct RewardedAdGrantRequest
    {
        public string AccountId { get; }
        public string ResultId { get; }
        public RewardedAdRewardType RewardType { get; }
        public string AdProofId { get; }
        public string GrantRequestId { get; }
        public int RequestedCoinAmount { get; }
        public RewardedAdGrantRequest(string accountId, string resultId, RewardedAdRewardType rewardType, string adProofId, string grantRequestId, int requestedCoinAmount)
        {
            AccountId = accountId ?? string.Empty;
            ResultId = resultId ?? string.Empty;
            RewardType = rewardType;
            AdProofId = adProofId ?? string.Empty;
            GrantRequestId = grantRequestId ?? string.Empty;
            RequestedCoinAmount = requestedCoinAmount;
        }
        public string ResultRewardKey => $"{AccountId}|{ResultId}|{RewardType}";
        public bool IsValid => !string.IsNullOrWhiteSpace(AccountId) && !string.IsNullOrWhiteSpace(ResultId) &&
                               !string.IsNullOrWhiteSpace(AdProofId) && !string.IsNullOrWhiteSpace(GrantRequestId) && RequestedCoinAmount > 0;
    }

    public readonly struct RewardGrantRecord
    {
        public string GrantRequestId { get; }
        public string ResultRewardKey { get; }
        public RewardGrantStatus Status { get; }
        public int GrantedCoins { get; }
        public RewardGrantRecord(string grantRequestId, string resultRewardKey, RewardGrantStatus status, int grantedCoins)
        {
            GrantRequestId = grantRequestId ?? string.Empty;
            ResultRewardKey = resultRewardKey ?? string.Empty;
            Status = status;
            GrantedCoins = Math.Max(0, grantedCoins);
        }
    }

    public interface IRewardLedgerAdapter
    {
        bool IsDevelopmentOnly { get; }
        bool IsAvailable { get; }
        int? VerifiedCoinBalance { get; }
        DailyAdQuotaSnapshot Quota { get; }
        RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request);
    }

    public sealed class DisconnectedRewardLedgerAdapter : IRewardLedgerAdapter
    {
        private readonly DailyAdQuotaSnapshot quota;
        public DisconnectedRewardLedgerAdapter(int configuredLimit = 3) => quota = DailyAdQuotaSnapshot.Unavailable(configuredLimit);
        public bool IsDevelopmentOnly => false;
        public bool IsAvailable => false;
        public int? VerifiedCoinBalance => null;
        public DailyAdQuotaSnapshot Quota => quota;
        public RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request) => RewardGrantStatus.Unavailable;
    }

    public sealed class DevRewardLedgerAdapter : IRewardLedgerAdapter
    {
        private int verifiedCoinBalance;
        private DailyAdQuotaSnapshot quota;
        public DevRewardLedgerAdapter(int initialBalance, int dailyLimit, string resetAtServerIso8601)
        {
            verifiedCoinBalance = Math.Max(0, initialBalance);
            quota = new DailyAdQuotaSnapshot(true, 0, dailyLimit, resetAtServerIso8601);
        }
        public bool IsDevelopmentOnly => true;
        public bool IsAvailable => true;
        public int? VerifiedCoinBalance => verifiedCoinBalance;
        public DailyAdQuotaSnapshot Quota => quota;
        public RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request)
        {
            if (!quota.IsServerAuthoritative || quota.RemainingCount.GetValueOrDefault() <= 0) return RewardGrantStatus.QuotaExceeded;
            verifiedCoinBalance += request.RequestedCoinAmount;
            quota = new DailyAdQuotaSnapshot(true, quota.UsedCount + 1, quota.Limit, quota.ResetAtServerIso8601);
            return RewardGrantStatus.Approved;
        }
    }

    public sealed class RewardedAdGrantCoordinator
    {
        private readonly IRewardLedgerAdapter adapter;
        private readonly Dictionary<string, RewardedAdGrantRequest> pendingByRequest = new Dictionary<string, RewardedAdGrantRequest>(StringComparer.Ordinal);
        private readonly HashSet<string> pendingResultKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> completedRequestIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> awardedResultKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<RewardGrantRecord> history = new List<RewardGrantRecord>();
        public IReadOnlyList<RewardGrantRecord> History => history;
        public DailyAdQuotaSnapshot Quota => adapter.Quota;
        public int? VerifiedCoinBalance => adapter.VerifiedCoinBalance;
        public bool DevelopmentAdapterOnly => adapter.IsDevelopmentOnly;

        public RewardedAdGrantCoordinator(IRewardLedgerAdapter adapter) => this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

        public RewardGrantStatus Begin(RewardedAdGrantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccountId)) return RewardGrantStatus.LoginRequired;
            if (!request.IsValid) return RewardGrantStatus.Invalid;
            if (!adapter.IsAvailable || !adapter.Quota.IsServerAuthoritative) return RewardGrantStatus.Unavailable;
            if (completedRequestIds.Contains(request.GrantRequestId) || awardedResultKeys.Contains(request.ResultRewardKey)) return RewardGrantStatus.Duplicate;
            if (pendingByRequest.ContainsKey(request.GrantRequestId) || pendingResultKeys.Contains(request.ResultRewardKey)) return RewardGrantStatus.Busy;
            if (adapter.Quota.RemainingCount.GetValueOrDefault() <= 0) return RewardGrantStatus.QuotaExceeded;
            pendingByRequest.Add(request.GrantRequestId, request);
            pendingResultKeys.Add(request.ResultRewardKey);
            return RewardGrantStatus.Pending;
        }

        public RewardGrantStatus Complete(string grantRequestId, RewardGrantStatus providerOutcome)
        {
            if (!pendingByRequest.TryGetValue(grantRequestId ?? string.Empty, out var request)) return RewardGrantStatus.Invalid;
            pendingByRequest.Remove(request.GrantRequestId);
            pendingResultKeys.Remove(request.ResultRewardKey);
            completedRequestIds.Add(request.GrantRequestId);

            var finalStatus = providerOutcome;
            var granted = 0;
            if (providerOutcome == RewardGrantStatus.Approved)
            {
                finalStatus = adapter.ConfirmApprovedGrant(request);
                if (finalStatus == RewardGrantStatus.Approved)
                {
                    awardedResultKeys.Add(request.ResultRewardKey);
                    granted = request.RequestedCoinAmount;
                }
            }
            else if (providerOutcome != RewardGrantStatus.Failed && providerOutcome != RewardGrantStatus.Cancelled)
            {
                finalStatus = RewardGrantStatus.Invalid;
            }
            history.Add(new RewardGrantRecord(request.GrantRequestId, request.ResultRewardKey, finalStatus, granted));
            return finalStatus;
        }
    }
}
