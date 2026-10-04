#if ANIMOL_NET_UI_DEV
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.AnimalUiV2;

namespace Animol.NetUiDev.Project
{
    // No HTTP client, journal or navigation owner here: all transactions use the coordinator.
    public sealed class NetUiPhase3Authority : IAnimalMultiplayerDevAuthority
    {
        private readonly NetUiDevCoordinator service;
        private readonly NetUiProjectContextReader reader;
        private readonly MultiplayerSelectionRequest bound;
        private readonly string route, roomCode, account, endpoint;

        public NetUiPhase3Authority(NetUiDevCoordinator service, NetUiProjectContextReader reader, NetUiBoundContext context)
        {
            this.service = service; this.reader = reader;
            bound = context.Request.Clone(); route = context.Route; roomCode = context.RoomCode;
            account = service.AccountId; endpoint = service.ServerUrl;
        }
        private void ValidateScope(MultiplayerSelectionRequest context)
        {
            NetUiHttpClient.EnsureDevelopment();
            NetUiValidation.Require(service != null && service.AccountId == account && service.ServerUrl == endpoint, "DEV_SESSION_SCOPE_CHANGED");
            NetUiValidation.Require(SameContext(bound, context), "DEV_CONTEXT_CHANGED_REOPEN_FROM_HUB");
        }
        public async Task<AnimalUiSnapshot> ReadAsync(MultiplayerSelectionRequest context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); ValidateScope(context);
            // Manual refresh makes another authoritative read. Never reuse a refused snapshot.
            var fresh = await reader.ReadAsync(route, roomCode);
            cancellationToken.ThrowIfCancellationRequested(); ValidateScope(context);
            NetUiValidation.Require(SameContext(bound, fresh.Request), "DEV_POLICY_CHANGED_REOPEN_FROM_HUB");
            return fresh.Snapshot;
        }
        public async Task<AnimalUiCommitResult> SubmitAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken)
        {
            ValidateScope(context);
            var stored = service.StoredRequest;
            NetUiEntryRequest wire;
            NetUiEntryResult reply;
            if (stored != null && (service.BlocksNewEntry || stored.ActionId == request.ActionId))
            {
                // Compare against the durable request; PrepareEntry must not mint/replace it.
                if (!Matches(request, stored) || stored.RoomCode != roomCode)
                    return Result(CommitStatus.Unknown, "이전 입장 요청 결과를 먼저 확인해 주세요.");
                wire = stored;
                reply = await service.ResolvePendingAsync();
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var actual = service.ToActualLoadout(ToArt(request.Loadout));
                    wire = service.PrepareEntry(request.ActionId, actual);
                    NetUiValidation.Require(Matches(request, wire) && wire.RoomCode == roomCode, "CONFIRMED_REQUEST_DIFFERS_FROM_SERVER_CONTEXT");
                }
                catch (Exception)
                { return Result(CommitStatus.Unavailable, "현재 정책 또는 사용 권한과 다릅니다. 최신 정보를 조회한 뒤 다시 확인해 주세요."); }
                // Once dispatch starts, cancellation cannot discard the durable result.
                reply = await service.SubmitEntryAsync(wire);
            }
            return Convert(wire, reply);
        }
        private bool Matches(SelectionCommitRequest ui, NetUiEntryRequest wire)
        {
            if (ui == null || wire == null || ui.ActionId != wire.ActionId || ui.ContextId != wire.ContextId ||
                ui.EntryIntent != wire.EntryIntent || ui.PolicyRevision != wire.PolicyRevision || ui.SnapshotRevision != wire.SnapshotRevision) return false;
            try { return NetUiValidation.SameLoadout(service.ToActualLoadout(ToArt(ui.Loadout)), wire.Loadout); }
            catch (Exception) { return false; }
        }
        private AnimalUiCommitResult Convert(NetUiEntryRequest wire, NetUiEntryResult reply)
        {
            if (reply == null || reply.ActionId != wire.ActionId) return Result(CommitStatus.Unknown, "입장 응답을 확인할 수 없습니다. 같은 요청의 결과를 다시 조회해 주세요.");
            if (reply.Status == "Accepted")
            {
                NetUiValidation.ValidateReceipt(wire, reply);
                var art = service.ToArtLoadout(reply.AcceptedLoadout);
                return new AnimalUiCommitResult { Status = CommitStatus.Accepted,
                    Message = "입장 승인 확인 · 대기방 연결 대기", AcceptanceToken = reply.AcceptanceToken,
                    AcceptedContextId = reply.AcceptedContextId, AcceptedPolicyRevision = reply.AcceptedPolicyRevision,
                    AcceptedEntryIntent = reply.AcceptedEntryIntent,
                    AcceptedLoadout = new AnimalLoadout { Ground = art.Ground, Special = art.Special, Air = art.Air } };
            }
            var status = reply.Status == "Rejected" ? CommitStatus.Rejected : reply.Status == "Unavailable" ? CommitStatus.Unavailable : CommitStatus.Unknown;
            return Result(status, (status == CommitStatus.Unknown ? "입장 결과 미확정 · 같은 요청 결과를 다시 조회해 주세요. " : "입장 불가 · 최신 정보를 조회한 뒤 다시 확인해 주세요. ") + reply.Reason);
        }
        private static AnimalUiCommitResult Result(CommitStatus status, string message) => new AnimalUiCommitResult { Status = status, Message = message };
        private static NetUiArtLoadout ToArt(AnimalLoadout value) => value == null ? null : new NetUiArtLoadout { Ground = value.Ground, Special = value.Special, Air = value.Air };
        private static bool SameContext(MultiplayerSelectionRequest a, MultiplayerSelectionRequest b) =>
            a != null && b != null && a.ModeId == b.ModeId && a.EntryIntent == b.EntryIntent && a.DisplayName == b.DisplayName &&
            a.InitialLoadout?.Fingerprint() == b.InitialLoadout?.Fingerprint() &&
            a.Requirements?.PolicyRevision == b.Requirements?.PolicyRevision && a.Requirements?.RepresentativeRole == b.Requirements?.RepresentativeRole &&
            a.Requirements?.RequiredRoles != null && b.Requirements?.RequiredRoles != null && a.Requirements.RequiredRoles.SequenceEqual(b.Requirements.RequiredRoles) &&
            ((a.AllowedAnimalIds == null && b.AllowedAnimalIds == null) || (a.AllowedAnimalIds != null && b.AllowedAnimalIds != null && a.AllowedAnimalIds.SequenceEqual(b.AllowedAnimalIds)));
    }
}
#endif
