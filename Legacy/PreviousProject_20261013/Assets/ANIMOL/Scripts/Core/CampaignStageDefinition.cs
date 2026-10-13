using System;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Stage", fileName = "CampaignStageDefinition")]
    public sealed class CampaignStageDefinition : ScriptableObject
    {
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private string themeId = string.Empty;
        [SerializeField] private int stageOrder;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string prerequisiteStageId = string.Empty;
        [SerializeField] private string mapTemplateId = string.Empty;
        [SerializeField] private string variantPolicy = string.Empty;
        [SerializeField] private int variantSeed;
        [SerializeField] private int targetBubbleCount = 3;
        [SerializeField] private string[] fixedAllowedAnimalIds = Array.Empty<string>();
        [SerializeField] private string initialAnimalId = string.Empty;
        [SerializeField] private string tutorialCueKey = string.Empty;
        [SerializeField] private int contentVersion = 1;
        [SerializeField] private CampaignStageRuntimePolicy runtimePolicy;
        [Header("Content validation ports")]
        [SerializeField] private int reachableBubbleSlots;
        [SerializeField] private bool hasValidatedExitConnection;

        public string StageId => stageId;
        public string ThemeId => themeId;
        public int StageOrder => stageOrder;
        public string DisplayName => displayName;
        public string PrerequisiteStageId => prerequisiteStageId;
        public string MapTemplateId => mapTemplateId;
        public string VariantPolicy => variantPolicy;
        public int VariantSeed => variantSeed;
        public int TargetBubbleCount => targetBubbleCount;
        public System.Collections.Generic.IReadOnlyList<string> FixedAllowedAnimalIds => fixedAllowedAnimalIds;
        public string InitialAnimalId => initialAnimalId;
        public string TutorialCueKey => tutorialCueKey;
        public int ContentVersion => contentVersion;
        public CampaignStageRuntimePolicy RuntimePolicy => runtimePolicy;
        public int ReachableBubbleSlots => reachableBubbleSlots;
        public bool HasValidatedExitConnection => hasValidatedExitConnection;
    }
}
