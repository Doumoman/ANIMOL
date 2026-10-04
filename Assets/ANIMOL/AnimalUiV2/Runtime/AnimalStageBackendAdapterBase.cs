using System;
using System.Threading;
using System.Threading.Tasks;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Phase 2: map the existing stage authority/entry service in the two protected methods.</summary>
    public abstract class AnimalStageBackendAdapterBase : AnimalUiBackendBehaviour
    {
        public AnimalCatalog Catalog;
        private AnimalUiSnapshot _lastSnapshot;
        private StageSelectionRequest _lastContext;
        private int _readGeneration;

        protected abstract Task<AnimalUiSnapshot> ReadStageSnapshotAsync(StageSelectionRequest context, CancellationToken cancellationToken);
        protected abstract Task<AnimalUiCommitResult> SubmitFixedCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);

        public sealed override async Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken cancellationToken)
        {
            if (context == null || context.Mode != AnimalUiMode.StageAnimalSelect)
                throw new InvalidOperationException("이 어댑터는 2단계 스테이지 동물 확인만 연결합니다.");
            var stage = context.Stage?.Clone();
            if (!AnimalUiRules.ValidateFixedStageContext(Catalog, stage, out var reason)) throw new InvalidOperationException(reason);
            if (stage.StageId == "PREVIEW_ONLY" || stage.Requirements.PolicyRevision == "PREVIEW_ONLY")
                throw new InvalidOperationException("읽기 전용 예시 문맥을 실제 스테이지 서비스에 보내지 않습니다.");
            int generation = ++_readGeneration;
            _lastSnapshot = null; _lastContext = null;
            var snapshot = await ReadStageSnapshotAsync(stage.Clone(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Revision))
                throw new InvalidOperationException("실제 스테이지 사용 권한과 스냅샷 버전을 연결해주세요.");
            if (generation == _readGeneration) { _lastSnapshot = snapshot; _lastContext = stage; }
            return snapshot;
        }

        public sealed override Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken)
        {
            var stage = context?.Clone(); var commit = request?.Clone();
            if (!MatchesReadContext(stage)) return Unavailable("현재 조회한 스테이지 정책과 일치하지 않습니다.");
            if (!AnimalUiRules.ValidateFixedStageDispatch(Catalog, _lastSnapshot, stage, commit, out var reason)) return Unavailable(reason);
            // Each attempt receives a fresh copy; the presenter's stored ActionId/payload remains unchanged.
            // The existing entry service must deduplicate/reconcile that ActionId, including after Unknown.
            return SubmitFixedCampaignAsync(stage, commit, cancellationToken);
        }

        private bool MatchesReadContext(StageSelectionRequest stage)
        {
            if (stage == null || _lastContext == null || stage.StageId != _lastContext.StageId ||
                stage.Policy != _lastContext.Policy || stage.Requirements?.PolicyRevision != _lastContext.Requirements?.PolicyRevision ||
                stage.Requirements?.RepresentativeRole != _lastContext.Requirements?.RepresentativeRole ||
                stage.FixedLoadout?.Fingerprint() != _lastContext.FixedLoadout?.Fingerprint()) return false;
            var current = stage.Requirements?.RequiredRoles; var previous = _lastContext.Requirements?.RequiredRoles;
            if (current == null || previous == null || current.Length != previous.Length) return false;
            for (int i = 0; i < current.Length; i++) if (current[i] != previous[i]) return false;
            return true;
        }

        public sealed override Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken) =>
            Task.FromResult(new UpgradeQuote { AnimalId = animalId, Track = track, State = UpgradeQuoteState.Unavailable,
                Message = "동물 강화는 1단계 성장 어댑터를 사용합니다." });
        public sealed override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken) =>
            Unavailable("이 어댑터에서는 강화 요청을 보내지 않습니다.");
        public sealed override Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken) => Unavailable("멀티플레이 선택은 3단계에서 연결합니다.");
        private static Task<AnimalUiCommitResult> Unavailable(string message) => Task.FromResult(new AnimalUiCommitResult
        { Status = CommitStatus.Unavailable, Message = message });
    }
}
