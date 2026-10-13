using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.Core;

namespace ANIMOL.AnimalStagePhase2
{
    /// <summary>
    /// Project catalog boundary. The current project has no stage permission snapshot or admission
    /// receipt service. A local StageLaunchSnapshot.RunId is not an admission receipt.
    /// </summary>
    public sealed class AnimalStageProjectAdapter : AnimalStageBackendAdapterBase
    {
        public CampaignCatalog StageCatalog;
        public AnimalStageIdMap IdMap;
        public const string MissingService = "스테이지 권한 조회·입장 승인 서비스 미연결";

        public string DescribeContext(StageSelectionRequest requested)
        {
            if (Catalog == null || IdMap == null || StageCatalog == null) return "스테이지 카탈로그·ID 매핑 연결 전";
            var stage = StageCatalog.FindStage(requested?.StageId);
            if (stage == null) return "실제 스테이지 ID를 확인해주세요.";
            var current = AnimalStageContext.Read(stage, IdMap, Catalog);
            if (requested.Policy != current.Policy || requested.FixedLoadout?.Fingerprint() != current.FixedLoadout.Fingerprint() ||
                requested.Requirements?.PolicyRevision != current.Requirements.PolicyRevision ||
                requested.Requirements?.RepresentativeRole != current.Requirements.RepresentativeRole ||
                requested.Requirements?.RequiredRoles == null || !requested.Requirements.RequiredRoles.SequenceEqual(current.Requirements.RequiredRoles))
                return "스테이지 편성·정책이 변경되었습니다. 새로고침해주세요.";
            if (!AnimalUiRules.ValidateFixedStageContext(Catalog, current, out var reason)) return reason;
            if (ContentAvailabilityResolver.Resolve(stage) != ContentAvailability.Ready)
                return ContentAvailabilityResolver.GetBlockingReason(stage);
            // IsUnlocked is session progression, not per-animal context permission or a receipt issuer.
            // No empty account progression, artificial snapshot revision, grant, or run is created here.
            return MissingService;
        }

        protected override Task<AnimalUiSnapshot> ReadStageSnapshotAsync(StageSelectionRequest context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Even complete catalog data cannot substitute for the missing authority/growth service.
            // Keep unknown levels/effects in the presenter; never fabricate a successful snapshot.
            return Task.FromException<AnimalUiSnapshot>(new InvalidOperationException(DescribeContext(context)));
        }

        protected override Task<AnimalUiCommitResult> SubmitFixedCampaignAsync(StageSelectionRequest context,
            SelectionCommitRequest request, CancellationToken cancellationToken) => Task.FromResult(new AnimalUiCommitResult
        {
            Status = CommitStatus.Unavailable,
            Message = MissingService + " · 런 생성·맵 로딩 요청을 보내지 않았습니다."
        });
    }
}
