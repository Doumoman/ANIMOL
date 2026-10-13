using System;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;

namespace ANIMOL.AnimalMultiplayerPhase3
{
#if ANIMOL_NET_UI_DEV
    // Project-neutral port; the optional NET02 assembly owns the implementation.
    public interface IAnimalMultiplayerDevAuthority
    {
        Task<AnimalUiSnapshot> ReadAsync(MultiplayerSelectionRequest context, CancellationToken cancellationToken);
        Task<AnimalUiCommitResult> SubmitAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);
    }
#endif
    /// <summary>
    /// Project boundary, deliberately unavailable until an operational authority exists.
    /// IMatchGateway.RequestReady is not room admission and has no receipt or ActionId contract.
    /// Never promote development previews, unlocks, or catalog entries to context permission.
    /// </summary>
    public sealed class AnimalMultiplayerProjectAdapter : AnimalMultiplayerBackendAdapterBase
    {
        public AnimalMultiplayerIdMap IdMap;
        public const string MissingService = "멀티플레이 모드·계정 권한·성장 조회 및 방 입장 승인 서비스 미연결";
#if ANIMOL_NET_UI_DEV
        public IAnimalMultiplayerDevAuthority DevAuthority { get; set; }
        protected override bool SupportsRoomEntryReconciliation => DevAuthority != null;
#endif

        public string DescribeContext(MultiplayerSelectionRequest context)
        {
            if (Catalog == null || IdMap == null) return "동물 카탈로그·ID 매핑 연결 전";
            if (!AnimalUiRules.ValidateMultiplayerContext(Catalog, context, out var reason)) return reason;
            // There is no operational ModeId/intent/policy owner to verify this request against.
            // CampaignAnimalDefinition only owns species identity and implementation flags;
            // CharacterUpgradeCatalog does not supply account levels or multiplayer permissions.
            return MissingService;
        }

        protected override Task<AnimalUiSnapshot> ReadMultiplayerSnapshotAsync(
            MultiplayerSelectionRequest context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if ANIMOL_NET_UI_DEV
            if (DevAuthority != null) return DevAuthority.ReadAsync(context, cancellationToken);
#endif
            // No fabricated revision, levels, descriptions, unlock, or context grant.
            return Task.FromException<AnimalUiSnapshot>(new InvalidOperationException(DescribeContext(context)));
        }

        protected override Task<AnimalUiCommitResult> SubmitRoomEntryAsync(
            MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken)
        {
#if ANIMOL_NET_UI_DEV
            if (DevAuthority != null) return DevAuthority.SubmitAsync(context, request, cancellationToken);
#endif
            return Task.FromResult(new AnimalUiCommitResult
            {
                Status = CommitStatus.Unavailable,
                Message = MissingService + " · 방 생성·참가·Ready 요청을 보내지 않았습니다."
            });
        }
    }
}
