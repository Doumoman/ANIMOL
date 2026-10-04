using System;
using System.Threading;
using System.Threading.Tasks;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Map the existing room-entry authority in these two protected methods.</summary>
    public abstract class AnimalMultiplayerBackendAdapterBase : AnimalUiBackendBehaviour
    {
        public AnimalCatalog Catalog;
        private AnimalUiSnapshot _lastSnapshot;
        private MultiplayerSelectionRequest _lastContext;
        private int _readGeneration;
        private sealed class Attempt
        {
            public MultiplayerSelectionRequest Context;
            public SelectionCommitRequest Request;
            public AnimalUiSnapshot Snapshot;
        }
        // Retain unresolved authority independently of refreshes. Never lose an uncertain ActionId.
        private Attempt _unresolved;

        protected abstract Task<AnimalUiSnapshot> ReadMultiplayerSnapshotAsync(MultiplayerSelectionRequest context, CancellationToken cancellationToken);
        protected abstract Task<AnimalUiCommitResult> SubmitRoomEntryAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);

        public sealed override async Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken cancellationToken)
        {
            if (context == null || context.Mode != AnimalUiMode.MultiplayerAnimalSelect)
                throw new InvalidOperationException("이 어댑터는 3단계 대기방 전 동물 선택만 연결합니다.");
            int generation = ++_readGeneration; _lastSnapshot = null; _lastContext = null;
            var multiplayer = context.Multiplayer?.Clone();
            if (!AnimalUiRules.ValidateMultiplayerContext(Catalog, multiplayer, out var reason)) throw new InvalidOperationException(reason);
            if (AnimalUiRules.IsReadonlyMultiplayerFixture(multiplayer))
                throw new InvalidOperationException("읽기 전용 예시 문맥은 실제 멀티플레이 서비스에 보내지 않습니다.");
            var snapshot = await ReadMultiplayerSnapshotAsync(multiplayer.Clone(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!AnimalUiRules.ValidateMultiplayerSnapshot(snapshot, out reason))
                throw new InvalidOperationException(reason);
            var retained = CopySnapshot(snapshot);
            if (generation == _readGeneration) { _lastSnapshot = retained; _lastContext = multiplayer; }
            return CopySnapshot(retained);
        }

        public sealed override async Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken)
        {
            var multiplayer = context?.Clone(); var commit = request?.Clone();
            var attempt = _unresolved;
            string reason;
            if (attempt != null)
            {
                if (!SameContext(multiplayer, attempt.Context) || !SameRequest(commit, attempt.Request))
                    return new AnimalUiCommitResult { Status = CommitStatus.Unknown,
                        Message = "이전 입장 요청 결과를 같은 요청 ID와 편성으로 확인해주세요." };
            }
            else
            {
                if (!SameContext(multiplayer, _lastContext)) return Unavailable("현재 조회한 모드 정책과 일치하지 않습니다.");
                if (!AnimalUiRules.ValidateMultiplayerDispatch(Catalog, _lastSnapshot, multiplayer, commit, out reason)) return Unavailable(reason);
                attempt = new Attempt { Context = multiplayer.Clone(), Request = commit.Clone(), Snapshot = CopySnapshot(_lastSnapshot) };
                _unresolved = attempt;
            }
            // Each attempt gets new copies. Services must deduplicate/reconcile the stable ActionId.
            // Throws/cancellation retain this attempt; the presenter treats them as Unknown.
            var result = await SubmitRoomEntryAsync(attempt.Context.Clone(), attempt.Request.Clone(), cancellationToken);
            if (result == null || !Enum.IsDefined(typeof(CommitStatus), result.Status))
                return new AnimalUiCommitResult { Status = CommitStatus.Unknown, Message = "입장 요청 결과 확인이 필요합니다." };
            if (result.Status == CommitStatus.Accepted &&
                !AnimalUiRules.IsAcceptedMultiplayerResult(Catalog, attempt.Snapshot, attempt.Context, attempt.Request, result, out reason))
                return new AnimalUiCommitResult { Status = CommitStatus.Unknown, Message = reason };
            if (result.Status == CommitStatus.Unknown) return result;
            if (ReferenceEquals(_unresolved, attempt)) _unresolved = null;
            if (result.Status == CommitStatus.Rejected || result.Status == CommitStatus.Unavailable)
            { _lastSnapshot = null; _lastContext = null; }
            return result;
        }

        private static bool SameRequest(SelectionCommitRequest current, SelectionCommitRequest previous) =>
            current != null && previous != null && current.ActionId == previous.ActionId && current.ContextId == previous.ContextId &&
            current.EntryIntent == previous.EntryIntent && current.PolicyRevision == previous.PolicyRevision &&
            current.SnapshotRevision == previous.SnapshotRevision && current.Loadout?.Fingerprint() == previous.Loadout?.Fingerprint();

        private static bool SameContext(MultiplayerSelectionRequest current, MultiplayerSelectionRequest previous)
        {
            if (current == null || previous == null || current.ModeId != previous.ModeId || current.EntryIntent != previous.EntryIntent ||
                current.DisplayName != previous.DisplayName || current.InitialLoadout?.Fingerprint() != previous.InitialLoadout?.Fingerprint() ||
                current.Requirements?.PolicyRevision != previous.Requirements?.PolicyRevision ||
                current.Requirements?.RepresentativeRole != previous.Requirements?.RepresentativeRole) return false;
            var currentRoles = current.Requirements?.RequiredRoles; var previousRoles = previous.Requirements?.RequiredRoles;
            if (currentRoles == null || previousRoles == null || currentRoles.Length != previousRoles.Length) return false;
            for (int i = 0; i < currentRoles.Length; i++) if (currentRoles[i] != previousRoles[i]) return false;
            if (current.AllowedAnimalIds == null || previous.AllowedAnimalIds == null)
                return current.AllowedAnimalIds == null && previous.AllowedAnimalIds == null;
            if (current.AllowedAnimalIds.Length != previous.AllowedAnimalIds.Length) return false;
            for (int i = 0; i < current.AllowedAnimalIds.Length; i++)
                if (current.AllowedAnimalIds[i] != previous.AllowedAnimalIds[i]) return false;
            return true;
        }

        private static AnimalUiSnapshot CopySnapshot(AnimalUiSnapshot source)
        {
            var copy = new AnimalUiSnapshot { Revision = source.Revision, CoinBalance = source.CoinBalance,
                ContextId = source.ContextId, PolicyRevision = source.PolicyRevision };
            foreach (var animal in source.Animals)
            {
                if (animal == null) throw new InvalidOperationException("동물 권한 스냅샷의 빈 항목을 확인해주세요.");
                if (string.IsNullOrWhiteSpace(animal.AnimalId) || copy.Find(animal.AnimalId) != null)
                    throw new InvalidOperationException("동물 권한 스냅샷의 ID 또는 중복 항목을 확인해주세요.");
                copy.Animals.Add(new AnimalProgress { AnimalId = animal.AnimalId, Unlocked = animal.Unlocked, Implemented = animal.Implemented,
                    HasContextPermission = animal.HasContextPermission, CanUseInContext = animal.CanUseInContext,
                    ActiveLevel = animal.ActiveLevel, PassiveLevel = animal.PassiveLevel, MasteryBalance = animal.MasteryBalance,
                    AvailabilityMessage = animal.AvailabilityMessage, ActiveDescription = animal.ActiveDescription, PassiveDescription = animal.PassiveDescription });
            }
            return copy;
        }

        public sealed override Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken) =>
            Task.FromResult(new UpgradeQuote { AnimalId = animalId, Track = track, State = UpgradeQuoteState.Unavailable,
                Message = "동물 강화는 1단계 성장 어댑터를 사용합니다." });
        public sealed override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable("이 어댑터에서는 강화 요청을 보내지 않습니다."));
        public sealed override Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken) => Task.FromResult(Unavailable("스테이지 입장은 2단계 어댑터를 사용합니다."));
        private static AnimalUiCommitResult Unavailable(string message) => new AnimalUiCommitResult
        { Status = CommitStatus.Unavailable, Message = message };
    }
}
