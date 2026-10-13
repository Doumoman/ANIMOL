using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalUpgradePhase1.Editor;
using ANIMOL.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    public sealed class UpgradePhase1BackendTests
    {
        private AnimalUpgradeProjectAdapter Backend => AssetDatabase.LoadAssetAtPath<GameObject>(AnimalUpgradePhase1Builder.PrefabPath)
            .GetComponent<AnimalUpgradeProjectAdapter>();

        [Test] public async Task AllSpeciesRemainUnknownAndCannotPurchaseWithoutGrowthService()
        {
            var backend = Backend;
            var snapshot = await backend.ReadSnapshotAsync(new AnimalUiReadRequest(), CancellationToken.None);
            Assert.That(snapshot.Animals.Count, Is.EqualTo(15));
            Assert.IsNull(snapshot.CoinBalance);
            Assert.That(backend.IdMap.Entries.Single(e => e.ArtId == "Rabbit").CampaignAnimal.AnimalId, Is.EqualTo("RABBIT"));
            foreach (var progress in snapshot.Animals)
            {
                Assert.IsFalse(progress.Unlocked || progress.Implemented || progress.HasContextPermission || progress.CanUseInContext);
                Assert.IsNull(progress.MasteryBalance);
                Assert.That(progress.ActiveLevel, Is.EqualTo(-1)); Assert.That(progress.PassiveLevel, Is.EqualTo(-1));
                foreach (UpgradeTrack track in System.Enum.GetValues(typeof(UpgradeTrack)))
                {
                    var quote = await backend.QuoteUpgradeAsync(progress.AnimalId, track, CancellationToken.None);
                    Assert.That(quote.State, Is.EqualTo(UpgradeQuoteState.Unconfigured));
                    Assert.IsFalse(quote.IsFree); Assert.IsEmpty(quote.Costs); Assert.IsNull(quote.QuoteToken);
                    Assert.That(quote.CurrentLevel, Is.EqualTo(-1)); Assert.That(quote.NextLevel, Is.EqualTo(-1));
                    Assert.IsFalse(AnimalUiRules.CanPurchaseUpgrade(backend.GetComponent<AnimalUiPresenter>().Catalog,
                        snapshot, quote, true, true, out _));
                }
            }
        }

        [Test] public void CoinsReadOnlyFromAvailableNonDevelopmentLedger()
        {
            var ledger = new TestLedger();
            Assert.IsNull(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(null));
            Assert.IsNull(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(ledger));
            ledger.Available = true; ledger.Development = true;
            Assert.IsNull(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(ledger));
            ledger.Development = false;
            Assert.That(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(ledger), Is.EqualTo(125L));
            ledger.Balance = 0;
            Assert.That(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(ledger), Is.EqualTo(0L));
            ledger.Balance = null;
            Assert.IsNull(AnimalUpgradeProjectAdapter.ReadVerifiedCoinBalance(ledger));
            Assert.That(ledger.Grants, Is.Zero);
        }

        [Test] public async Task ForcedAndRepeatedCommitsAndSelectionNeverDispatchOrAccept()
        {
            var backend = Backend;
            var request = new UpgradeCommitRequest { ActionId = "same-test-action", AnimalId = "Rabbit", QuoteToken = "forged", ExpectedCurrentLevel = 0 };
            for (var i = 0; i < 2; i++)
                Assert.That((await backend.TryUpgradeAsync(request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That((await backend.SubmitCampaignAsync(null, null, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That((await backend.SubmitMultiplayerAsync(null, null, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.IsNull((await backend.ReadSnapshotAsync(new AnimalUiReadRequest(), CancellationToken.None)).CoinBalance);
        }

        [Test] public async Task UnknownOrWrongCaseIdsNeverFallBackToRabbitOrDevForm()
        {
            foreach (var id in new[] { "rabbit", "RABBIT", "DEV_GROUND", "missing", null })
            {
                var quote = await Backend.QuoteUpgradeAsync(id, UpgradeTrack.Active, CancellationToken.None);
                Assert.That(quote.State, Is.EqualTo(UpgradeQuoteState.Unconfigured));
                Assert.IsNull(quote.QuoteToken);
            }
        }

        private sealed class TestLedger : IRewardLedgerAdapter
        {
            public bool Available, Development;
            public int? Balance = 125;
            public int Grants;
            public bool IsDevelopmentOnly => Development;
            public bool IsAvailable => Available;
            public int? VerifiedCoinBalance => Balance;
            public DailyAdQuotaSnapshot Quota => DailyAdQuotaSnapshot.Unavailable(3);
            public RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request) { Grants++; return RewardGrantStatus.Unavailable; }
        }
    }
}
