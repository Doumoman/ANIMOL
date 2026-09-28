using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Theme", fileName = "ThemeDefinition")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [SerializeField] private string themeId = string.Empty;
        [SerializeField] private int displayOrder;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private string thumbnailRef = string.Empty;
        [SerializeField] private string lockedDescription = string.Empty;
        [SerializeField] private string completedDescription = string.Empty;
        [SerializeField] private CampaignStageDefinition[] stages = System.Array.Empty<CampaignStageDefinition>();

        public string ThemeId => themeId;
        public int DisplayOrder => displayOrder;
        public string DisplayName => displayName;
        public string Description => description;
        public string ThumbnailRef => thumbnailRef;
        public string LockedDescription => lockedDescription;
        public string CompletedDescription => completedDescription;
        public System.Collections.Generic.IReadOnlyList<CampaignStageDefinition> Stages => stages;
    }
}
