using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Derive in the host project and forward to existing progression/network services.</summary>
    public abstract class AnimalUiBackendBehaviour : MonoBehaviour, IAnimalUiBackend
    {
        public abstract Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken cancellationToken);
        public abstract Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken);
        public abstract Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken);
        public abstract Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);
        public abstract Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);
    }
}
