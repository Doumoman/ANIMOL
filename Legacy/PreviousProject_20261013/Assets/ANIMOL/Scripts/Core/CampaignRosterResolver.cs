using System;
using System.Collections.Generic;
using System.Linq;

namespace ANIMOL.Core
{
    public sealed class CampaignRosterSnapshot
    {
        private readonly HashSet<string> allowed;
        public string InitialAnimalId { get; }
        public IReadOnlyCollection<string> AllowedAnimalIds => allowed;

        public CampaignRosterSnapshot(IEnumerable<string> allowedAnimalIds, string initialAnimalId)
        {
            allowed = new HashSet<string>(allowedAnimalIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (allowed.Count == 0 || !allowed.Contains(initialAnimalId))
                throw new InvalidOperationException("Campaign roster must be non-empty and contain its initial animal.");
            InitialAnimalId = initialAnimalId;
        }

        public bool CanTransformTo(string animalId) => !string.IsNullOrWhiteSpace(animalId) && allowed.Contains(animalId);
    }

    public static class CampaignRosterResolver
    {
        public static CampaignRosterSnapshot Resolve(CampaignStageDefinition stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            return new CampaignRosterSnapshot(stage.FixedAllowedAnimalIds, stage.InitialAnimalId);
        }
    }
}
