using System;
using System.Collections.Generic;

namespace ANIMOL.Core
{
    [Serializable]
    public sealed class CampaignProgress
    {
        public HashSet<string> CompletedStageIds { get; } = new HashSet<string>(StringComparer.Ordinal);
        public Dictionary<string, float> BestTimes { get; } = new Dictionary<string, float>(StringComparer.Ordinal);
        public string RecentStageId { get; private set; } = string.Empty;
        public int Revision { get; private set; }

        internal bool Record(string stageId, float timeSeconds)
        {
            var firstClear = CompletedStageIds.Add(stageId);
            var bestChanged = !BestTimes.TryGetValue(stageId, out var best) || timeSeconds < best;
            if (bestChanged) BestTimes[stageId] = timeSeconds;
            RecentStageId = stageId;
            if (firstClear || bestChanged) Revision++;
            return firstClear;
        }
    }

    public sealed class CampaignProgressionService
    {
        private readonly CampaignCatalog catalog;
        public CampaignProgress Progress { get; }

        public CampaignProgressionService(CampaignCatalog catalog, CampaignProgress progress = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Progress = progress ?? new CampaignProgress();
        }

        public bool IsUnlocked(CampaignStageDefinition stage)
        {
            if (stage == null) return false;
            return string.IsNullOrWhiteSpace(stage.PrerequisiteStageId) || Progress.CompletedStageIds.Contains(stage.PrerequisiteStageId);
        }

        public string GetNextStageId(string stageId)
        {
            var stage = catalog.FindStage(stageId);
            if (stage == null) return string.Empty;
            if (stage.StageOrder < 20) return $"{stage.ThemeId}-S{stage.StageOrder + 1:00}";
            var themeNumber = int.Parse(stage.ThemeId.Substring(1));
            return themeNumber < 5 ? $"T{themeNumber + 1:00}-S01" : string.Empty;
        }

        public bool RecordValidatedCompletion(string stageId, float timeSeconds, bool validatedExit, bool developmentRun)
        {
            if (!validatedExit || developmentRun || catalog.FindStage(stageId) == null) return false;
            return Progress.Record(stageId, timeSeconds);
        }

        public CampaignCompletionRecord RecordValidatedResult(string stageId, float timeSeconds, CampaignRunOutcome outcome, bool validatedExit, bool developmentRun)
        {
            if (outcome != CampaignRunOutcome.Cleared || !validatedExit || developmentRun || catalog.FindStage(stageId) == null)
                return new CampaignCompletionRecord(false, false, false);
            var wasCompleted = Progress.CompletedStageIds.Contains(stageId);
            var previousBest = Progress.BestTimes.TryGetValue(stageId, out var best) ? best : float.PositiveInfinity;
            var firstClear = Progress.Record(stageId, timeSeconds);
            return new CampaignCompletionRecord(true, firstClear, !wasCompleted || timeSeconds < previousBest);
        }
    }

    public sealed class StageResultCommitter
    {
        private readonly CampaignProgressionService progression;
        private readonly HashSet<string> committedResults = new HashSet<string>(StringComparer.Ordinal);

        public StageResultCommitter(CampaignProgressionService progression) => this.progression = progression;

        public bool Commit(string runId, string stageId, string resultId, float timeSeconds, bool validatedExit, bool developmentRun)
        {
            var key = $"{runId}|{stageId}|{resultId}";
            if (!committedResults.Add(key)) return false;
            return progression.RecordValidatedCompletion(stageId, timeSeconds, validatedExit, developmentRun);
        }
    }
}
