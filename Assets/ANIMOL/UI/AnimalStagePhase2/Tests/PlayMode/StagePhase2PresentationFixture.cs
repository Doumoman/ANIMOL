using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;

namespace ANIMOL.AnimalStagePhase2.Tests
{
    // Test assembly only. No scene/prefab uses this and no operational service is called.
    public sealed class StagePhase2PresentationFixture : AnimalUiBackendBehaviour
    {
        public AnimalUiSnapshot Snapshot;
        public int Submissions;
        public override Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken token) => Task.FromResult(Snapshot);
        public override Task<UpgradeQuote> QuoteUpgradeAsync(string id, UpgradeTrack track, CancellationToken token) =>
            Task.FromResult(new UpgradeQuote { State = UpgradeQuoteState.Unavailable });
        public override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken token) => Unavailable();
        public override Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken token)
        { Submissions++; return Unavailable(); }
        public override Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken token) => Unavailable();
        private static Task<AnimalUiCommitResult> Unavailable() => Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unavailable });
    }
}
