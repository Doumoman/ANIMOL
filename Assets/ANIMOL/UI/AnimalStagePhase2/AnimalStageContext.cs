using System;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.Core;

namespace ANIMOL.AnimalStagePhase2
{
    /// <summary>Read-only projection of the selected catalog asset. No defaults fill missing species.</summary>
    public static class AnimalStageContext
    {
        public static StageSelectionRequest Read(CampaignStageDefinition stage, AnimalStageIdMap map, AnimalCatalog art)
        {
            var loadout = new AnimalLoadout();
            if (stage != null && map != null && art != null)
            {
                // Both the legacy ID array and asset references belong to the existing launch contract.
                // Disagreement is incomplete configuration, never permission to guess a species.
                var ids = stage.FixedAllowedAnimalIds ?? Array.Empty<string>();
                var definitions = stage.FixedAnimalDefinitions ?? Array.Empty<CampaignAnimalDefinition>();
                foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole)))
                {
                    var candidates = ids.Distinct().Select(id => map.ToArtId(id)).Where(id => id != null && art.Find(id)?.Role == role &&
                        definitions.Any(d => d != null && d.AnimalId == map.ToProjectAnimal(id)?.AnimalId)).ToArray();
                    if (candidates.Length == 1) loadout.Set(role, candidates[0]);
                }
            }
            string initialId = stage != null && map != null && stage.InitialAnimalDefinition != null &&
                stage.InitialAnimalDefinition.AnimalId == stage.InitialAnimalId ? map.ToArtId(stage.InitialAnimalId) : null;
            var initial = art == null ? null : art.Find(initialId);
            return new StageSelectionRequest
            {
                StageId = stage?.StageId,
                DisplayName = stage == null ? "스테이지 설정 대기" : stage.StageId + " · " + stage.DisplayName,
                Policy = CampaignSelectionPolicy.Fixed,
                FixedLoadout = loadout,
                InitialLoadout = loadout.Clone(),
                Requirements = new SelectionRequirements
                {
                    RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                    RepresentativeRole = initial != null && loadout.Get(initial.Role) == initial.Id ? initial.Role : (AnimalRole)(-1),
                    // These are real local catalog versions, NOT a server grant/snapshot revision.
                    PolicyRevision = stage != null && stage.ContentVersion > 0 && stage.RuntimePolicy != null && stage.RuntimePolicy.Version > 0
                        ? "content:" + stage.ContentVersion + ";runtime:" + stage.RuntimePolicy.Version : null
                }
            };
        }

        public static string AccessSummary(CampaignStageDefinition stage)
        {
            if (stage == null) return "스테이지 문맥 설정 대기";
            string prerequisite = string.IsNullOrWhiteSpace(stage.PrerequisiteStageId) ? "선행 스테이지 없음" : "선행 " + stage.PrerequisiteStageId;
            return prerequisite + " · " + (ContentAvailabilityResolver.Resolve(stage) == ContentAvailability.Ready ? "콘텐츠 준비됨" : "콘텐츠 준비 중") +
                "\n지정 3종·사용 권한·입장 서비스 연결 대기";
        }
    }
}
