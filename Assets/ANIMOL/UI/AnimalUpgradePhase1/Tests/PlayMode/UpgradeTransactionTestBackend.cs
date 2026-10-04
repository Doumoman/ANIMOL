using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    // Isolated UI test double. Never attached to a production asset; values are test fixtures only.
    public sealed class UpgradeTransactionTestBackend : AnimalUpgradeBackendAdapterBase
    {
        public readonly List<UpgradeCommitRequest> Requests = new List<UpgradeCommitRequest>();
        public TaskCompletionSource<AnimalUiCommitResult> Completion;
        public int Reads;
        public System.Func<Task<AnimalUiSnapshot>> ReadOverride;
        public System.Func<string, UpgradeTrack, Task<UpgradeQuote>> QuoteOverride;
        public override Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest request, CancellationToken cancellationToken)
        {
            Reads++;
            if (ReadOverride != null) return ReadOverride();
            var snapshot = new AnimalUiSnapshot { CoinBalance = 100, Revision = "test-revision" };
            snapshot.Animals.Add(new AnimalProgress { AnimalId = "Rabbit", Implemented = true, Unlocked = true,
                ActiveLevel = 1, PassiveLevel = 2, MasteryBalance = 50 });
            return Task.FromResult(snapshot);
        }
        public override Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken)
        {
            if (QuoteOverride != null) return QuoteOverride(animalId, track);
            var quote = new UpgradeQuote { AnimalId = animalId, Track = track, State = UpgradeQuoteState.Ready,
                CurrentLevel = track == UpgradeTrack.Active ? 1 : 2, NextLevel = track == UpgradeTrack.Active ? 2 : 3,
                CurrentEffect = "test current effect", NextEffect = "test next effect", QuoteToken = "test-token-" + track };
            quote.Costs.Add(new UpgradeCost { CurrencyId = "test-coins", DisplayName = "테스트 코인", Amount = 25 });
            quote.Costs.Add(new UpgradeCost { CurrencyId = "test-mastery", DisplayName = "테스트 숙련도", Amount = 5 });
            return Task.FromResult(quote);
        }
        public override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(new UpgradeCommitRequest { ActionId = request.ActionId, AnimalId = request.AnimalId,
                Track = request.Track, QuoteToken = request.QuoteToken, ExpectedCurrentLevel = request.ExpectedCurrentLevel });
            Completion = new TaskCompletionSource<AnimalUiCommitResult>();
            return Completion.Task;
        }
    }
}
