using System.Linq;
using ANIMOL.Core;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests
{
    public sealed class M6GrowthEconomyContractTests
    {
        private const string AccountPath = "Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset";
        private const string PolicyPath = "Assets/ANIMOL/Data/Meta/GrowthEconomyPolicyCatalog.asset";

        [Test]
        public void AccountCommonTracks_ArePrototypePlusEightPlusEightMinusSix()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountPath);
            Assert.That(catalog.PrototypeValues, Is.True);
            Assert.That(catalog.Tracks.Count, Is.EqualTo(3));
            Assert.That(catalog.Tracks.Select(x => x.TotalPercentDelta).ToArray(), Is.EqualTo(new[] { 8f, 8f, -6f }).Within(.01f));
            Assert.That(AccountUpgradeStateResolver.Resolve(catalog.Tracks[0], 0, null, false).State, Is.EqualTo(AccountUpgradeUiState.Unavailable));
        }

        [Test]
        public void AnimalGrowth_HasTwoLevelFiveTracksAndLocksUnsetEffectsMasteryAndPrices()
        {
            var policy = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(PolicyPath);
            Assert.That(policy.AnimalGrowth.Count, Is.EqualTo(3));
            Assert.That(policy.AnimalGrowth.All(x => x.HasTwoLevelFiveTracks), Is.True);
            Assert.That(policy.AnimalGrowth.All(x => !x.IsOperationallyConfigured), Is.True);
            Assert.That(policy.AnimalGrowth.SelectMany(x => x.Tracks).All(x => !x.IsConfigured), Is.True);
        }

        [Test]
        public void AnimalUnlocks_AreDrivenByCampaignProgress()
        {
            var policy = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(PolicyPath);
            var progress = new CampaignProgress();
            Assert.That(policy.FindAnimal("DEV_GROUND").IsUnlocked(progress), Is.True);
            Assert.That(policy.FindAnimal("DEV_GLIDER").IsUnlocked(progress), Is.False);
            progress.CompletedStageIds.Add("T01-S01");
            Assert.That(policy.FindAnimal("DEV_GLIDER").IsUnlocked(progress), Is.True);
        }

        [Test]
        public void GrowthCompletionPack_ContainsOnlyCommonTracksAndCannotBeSold()
        {
            var pack = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(PolicyPath).GrowthCompletionPack;
            Assert.That(pack.IncludedAccountTrackIds.Count, Is.EqualTo(3));
            Assert.That(pack.IncludesAnimalGrowth, Is.False);
            Assert.That(pack.IncludesAnimalUnlocks, Is.False);
            Assert.That(pack.CosmeticCompensationFormulaConfigured, Is.False);
            Assert.That(pack.CanSell, Is.False);
        }

        [Test]
        public void EmoteRoutes_AreSeparatedAndOperationalSupplyMustBeAuthenticated()
        {
            var emotes = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(PolicyPath).EmoteAcquisitions;
            Assert.That(emotes.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.Free), Is.EqualTo(3));
            Assert.That(emotes.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.CoinExchange), Is.EqualTo(1));
            Assert.That(emotes.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.Paid), Is.EqualTo(1));
            Assert.That(new AuthenticatedOfferSnapshot("OFFER", "₩1,100", false, false).MayDisplayOperationalPriceOrOwnership, Is.False);
            Assert.That(new AuthenticatedOfferSnapshot("OFFER", "₩1,100", false, true).MayDisplayOperationalPriceOrOwnership, Is.True);
        }

        [Test]
        public void GuestAndSaveContracts_KeepOnlineCoinAndCloudAuthoritySeparate()
        {
            var guest = AccountAccessSnapshot.GuestDisconnected;
            Assert.That(guest.CanStartGuestCampaign, Is.True);
            Assert.That(guest.CanUseOnline, Is.False);
            Assert.That(guest.CanPurchase, Is.False);
            var selector = new SaveConflictSelectionService(
                new CampaignRecordSummary(SaveRecordSource.Local, true, false, 4, 3, 7),
                new CampaignRecordSummary(SaveRecordSource.Cloud, true, true, 6, 5, 9), true);
            Assert.That(selector.TrySelect(SaveRecordSource.Cloud, false), Is.False);
            Assert.That(selector.TrySelect(SaveRecordSource.Cloud, true), Is.True);
            Assert.That(selector.SelectedSource, Is.EqualTo(SaveRecordSource.Cloud));
        }

        [Test]
        public void CampaignAdReward_AddsOnlyApprovedBaseBAgain()
        {
            var firstClear = new CampaignCoinRewardBreakdown(100, 25, 10, true);
            var reclear = new CampaignCoinRewardBreakdown(60, 25, 10, false);
            Assert.That(firstClear.TotalWithoutAd, Is.EqualTo(135));
            Assert.That(firstClear.TotalWithApprovedAd, Is.EqualTo(235));
            Assert.That(reclear.TotalWithApprovedAd, Is.EqualTo(155));
            Assert.That(firstClear.AdAdditionalBaseB, Is.EqualTo(firstClear.BaseCoinB));
        }

        [Test]
        public void DisconnectedLedger_DoesNotInventBalanceQuotaOrGrant()
        {
            var coordinator = new RewardedAdGrantCoordinator(new DisconnectedRewardLedgerAdapter(3));
            var request = Request("A", "R", RewardedAdRewardType.LobbyGeneralCoinDraft, "Q", 150);
            Assert.That(coordinator.Begin(request), Is.EqualTo(RewardGrantStatus.Unavailable));
            Assert.That(coordinator.VerifiedCoinBalance, Is.Null);
            Assert.That(coordinator.Quota.RemainingCount, Is.Null);
            Assert.That(coordinator.History, Is.Empty);
        }

        [Test]
        public void DevLedger_VerifiesBalanceSharedLimitDuplicateConcurrentFailCancelAndPrelogin()
        {
            var coordinator = new RewardedAdGrantCoordinator(new DevRewardLedgerAdapter(0, 3, "2030-01-02T00:00:00Z"));
            Assert.That(coordinator.Begin(Request(string.Empty, "PRE", RewardedAdRewardType.LobbyGeneralCoinDraft, "PRE", 150)), Is.EqualTo(RewardGrantStatus.LoginRequired));

            var result = Request("DEV", "RESULT-1", RewardedAdRewardType.CampaignBaseCoinDouble, "RESULT", 100);
            Assert.That(coordinator.Begin(result), Is.EqualTo(RewardGrantStatus.Pending));
            Assert.That(coordinator.Complete(result.GrantRequestId, RewardGrantStatus.Approved), Is.EqualTo(RewardGrantStatus.Approved));
            Assert.That(coordinator.Begin(Request("DEV", "RESULT-1", RewardedAdRewardType.CampaignBaseCoinDouble, "DUP", 100)), Is.EqualTo(RewardGrantStatus.Duplicate));

            var pending = Request("DEV", "CONCURRENT", RewardedAdRewardType.LobbyGeneralCoinDraft, "A", 150);
            Assert.That(coordinator.Begin(pending), Is.EqualTo(RewardGrantStatus.Pending));
            Assert.That(coordinator.Begin(Request("DEV", "CONCURRENT", RewardedAdRewardType.LobbyGeneralCoinDraft, "B", 150)), Is.EqualTo(RewardGrantStatus.Busy));
            Assert.That(coordinator.Complete(pending.GrantRequestId, RewardGrantStatus.Cancelled), Is.EqualTo(RewardGrantStatus.Cancelled));

            var failed = Request("DEV", "FAILED", RewardedAdRewardType.LobbyGeneralCoinDraft, "FAIL", 150);
            coordinator.Begin(failed);
            Assert.That(coordinator.Complete(failed.GrantRequestId, RewardGrantStatus.Failed), Is.EqualTo(RewardGrantStatus.Failed));
            Assert.That(coordinator.Quota.UsedCount, Is.EqualTo(1));
            Assert.That(coordinator.VerifiedCoinBalance, Is.EqualTo(100));

            foreach (var id in new[] { "GENERAL-1", "GENERAL-2" })
            {
                var request = Request("DEV", id, RewardedAdRewardType.LobbyGeneralCoinDraft, id, 150);
                Assert.That(coordinator.Begin(request), Is.EqualTo(RewardGrantStatus.Pending));
                Assert.That(coordinator.Complete(request.GrantRequestId, RewardGrantStatus.Approved), Is.EqualTo(RewardGrantStatus.Approved));
            }
            Assert.That(coordinator.VerifiedCoinBalance, Is.EqualTo(400));
            Assert.That(coordinator.Quota.UsedCount, Is.EqualTo(3));
            Assert.That(coordinator.Quota.RemainingCount, Is.EqualTo(0));
            Assert.That(coordinator.Begin(Request("DEV", "GENERAL-3", RewardedAdRewardType.LobbyGeneralCoinDraft, "LIMIT", 150)), Is.EqualTo(RewardGrantStatus.QuotaExceeded));
            Assert.That(coordinator.History.Count(x => x.Status == RewardGrantStatus.Approved), Is.EqualTo(3));
            Assert.That(coordinator.History.Count(x => x.Status == RewardGrantStatus.Failed), Is.EqualTo(1));
            Assert.That(coordinator.History.Count(x => x.Status == RewardGrantStatus.Cancelled), Is.EqualTo(1));
        }

        private static RewardedAdGrantRequest Request(string account, string result, RewardedAdRewardType type, string id, int amount) =>
            new RewardedAdGrantRequest(account, result, type, "PROOF-" + id, "GRANT-" + id, amount);
    }
}
