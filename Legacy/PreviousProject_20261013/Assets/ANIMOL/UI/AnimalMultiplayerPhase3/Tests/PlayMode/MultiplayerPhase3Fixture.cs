using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    // Test assembly only; never attached to an operational asset.
    public sealed class MultiplayerPhase3Fixture : AnimalUiBackendBehaviour
    {
        public AnimalUiSnapshot Snapshot;
        public int Requests;
        public override Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken token) => Task.FromResult(Snapshot);
        public override Task<UpgradeQuote> QuoteUpgradeAsync(string id, UpgradeTrack track, CancellationToken token) => Task.FromResult(new UpgradeQuote { State = UpgradeQuoteState.Unavailable });
        public override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken token) => Unavailable();
        public override Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken token) => Unavailable();
        public override Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken token)
        { Requests++; return Unavailable(); }
        private static Task<AnimalUiCommitResult> Unavailable() => Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unavailable, Message = "TEST_ONLY" });
    }
}
