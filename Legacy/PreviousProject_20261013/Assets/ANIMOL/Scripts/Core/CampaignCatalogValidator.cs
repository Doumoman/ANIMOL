using System.Collections.Generic;
using System.Linq;

namespace ANIMOL.Core
{
    public sealed class CampaignCatalogValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public int ThemeCount;
        public int StageCount;
        public int DuplicateStageIdCount;
        public bool IsValid => Errors.Count == 0;
    }

    public static class CampaignCatalogValidator
    {
        public static CampaignCatalogValidationReport Validate(CampaignCatalog catalog)
        {
            var report = new CampaignCatalogValidationReport();
            if (catalog == null)
            {
                report.Errors.Add("CampaignCatalog is null.");
                return report;
            }

            report.ThemeCount = catalog.Themes.Count;
            if (report.ThemeCount != 5) report.Errors.Add($"Expected 5 themes, found {report.ThemeCount}.");

            var seen = new HashSet<string>();
            for (var themeIndex = 1; themeIndex <= catalog.Themes.Count; themeIndex++)
            {
                var theme = catalog.Themes[themeIndex - 1];
                var expectedThemeId = $"T{themeIndex:00}";
                if (theme == null)
                {
                    report.Errors.Add($"Theme slot {themeIndex} is null.");
                    continue;
                }
                if (theme.ThemeId != expectedThemeId) report.Errors.Add($"Theme ID mismatch: expected {expectedThemeId}, found {theme.ThemeId}.");
                if (theme.Stages.Count != 20) report.Errors.Add($"{expectedThemeId} must contain 20 stages, found {theme.Stages.Count}.");

                for (var stageIndex = 1; stageIndex <= theme.Stages.Count; stageIndex++)
                {
                    var stage = theme.Stages[stageIndex - 1];
                    var expectedStageId = $"{expectedThemeId}-S{stageIndex:00}";
                    if (stage == null)
                    {
                        report.Errors.Add($"{expectedStageId} reference is null.");
                        continue;
                    }
                    report.StageCount++;
                    if (stage.StageId != expectedStageId) report.Errors.Add($"Stage ID mismatch: expected {expectedStageId}, found {stage.StageId}.");
                    if (!seen.Add(stage.StageId)) report.DuplicateStageIdCount++;
                    if (stage.TargetBubbleCount != 3) report.Errors.Add($"{stage.StageId} targetBubbleCount must be 3.");
                }
            }

            if (report.StageCount != 100) report.Errors.Add($"Expected 100 stages, found {report.StageCount}.");
            if (report.DuplicateStageIdCount != 0) report.Errors.Add($"Duplicate stage IDs: {report.DuplicateStageIdCount}.");
            return report;
        }
    }
}
