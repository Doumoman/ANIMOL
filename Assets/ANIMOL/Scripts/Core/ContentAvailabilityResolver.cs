using System.Linq;

namespace ANIMOL.Core
{
    public static class ContentAvailabilityResolver
    {
        public static ContentAvailability Resolve(CampaignStageDefinition stage)
        {
            if (stage == null) return ContentAvailability.Invalid;
            if (string.IsNullOrWhiteSpace(stage.MapTemplateId) ||
                stage.FixedAllowedAnimalIds == null || stage.FixedAllowedAnimalIds.Count == 0 ||
                string.IsNullOrWhiteSpace(stage.InitialAnimalId))
                return ContentAvailability.Unassigned;

            if (stage.TargetBubbleCount != 3 ||
                stage.ReachableBubbleSlots < stage.TargetBubbleCount ||
                !stage.HasValidatedExitConnection ||
                stage.RuntimePolicy == null || !stage.RuntimePolicy.HasRequiredRuntimeRules ||
                stage.FixedAllowedAnimalIds.Any(string.IsNullOrWhiteSpace) ||
                !stage.FixedAllowedAnimalIds.Contains(stage.InitialAnimalId) ||
                stage.FixedAllowedAnimalIds.Distinct().Count() != stage.FixedAllowedAnimalIds.Count)
                return ContentAvailability.Invalid;

            return ContentAvailability.Ready;
        }
    }
}
