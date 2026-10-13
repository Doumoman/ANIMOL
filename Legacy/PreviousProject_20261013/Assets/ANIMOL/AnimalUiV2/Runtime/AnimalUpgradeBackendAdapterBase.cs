using System.Threading;
using System.Threading.Tasks;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Phase 1 adapter base. Implement the three inherited growth methods in the host project.</summary>
    public abstract class AnimalUpgradeBackendAdapterBase : AnimalUiBackendBehaviour
    {
        public sealed override Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken) => UnavailableSelection();
        public sealed override Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken) => UnavailableSelection();
        private static Task<AnimalUiCommitResult> UnavailableSelection() => Task.FromResult(new AnimalUiCommitResult
        {
            Status = CommitStatus.Unavailable, Message = "이 어댑터는 1단계 동물 강화만 연결합니다."
        });
    }
}
