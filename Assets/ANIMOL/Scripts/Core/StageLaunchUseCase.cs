using System;

namespace ANIMOL.Core
{
    public sealed class StageLaunchSnapshot
    {
        public string RunId { get; }
        public string StageId { get; }
        public string MapTemplateId { get; }
        public CampaignRosterSnapshot Roster { get; }

        public StageLaunchSnapshot(string runId, CampaignStageDefinition stage, CampaignRosterSnapshot roster)
        {
            RunId = runId;
            StageId = stage.StageId;
            MapTemplateId = stage.MapTemplateId;
            Roster = roster;
        }
    }

    public sealed class StageLaunchUseCase
    {
        public StageLaunchSnapshot Launch(CampaignStageDefinition stage)
        {
            if (ContentAvailabilityResolver.Resolve(stage) != ContentAvailability.Ready)
                throw new InvalidOperationException("Stage content is not ready.");
            return new StageLaunchSnapshot(Guid.NewGuid().ToString("N"), stage, CampaignRosterResolver.Resolve(stage));
        }
    }
}
