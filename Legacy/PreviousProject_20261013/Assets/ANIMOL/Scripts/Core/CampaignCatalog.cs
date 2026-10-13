using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Catalog", fileName = "CampaignCatalog")]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [SerializeField] private ThemeDefinition[] themes = System.Array.Empty<ThemeDefinition>();

        public IReadOnlyList<ThemeDefinition> Themes => themes;

        public CampaignStageDefinition FindStage(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return null;
            foreach (var theme in themes)
            {
                if (theme == null) continue;
                foreach (var stage in theme.Stages)
                {
                    if (stage != null && stage.StageId == stageId) return stage;
                }
            }
            return null;
        }

        public IEnumerable<CampaignStageDefinition> EnumerateStages()
        {
            foreach (var theme in themes)
            {
                if (theme == null) continue;
                foreach (var stage in theme.Stages)
                    if (stage != null) yield return stage;
            }
        }
    }
}
