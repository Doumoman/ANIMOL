using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    public enum BubbleRevealPolicy { Unconfigured, ContactOnly }
    public enum RetryLayoutPolicy { Unconfigured, ReuseStageAndBubbleLayout }

    [Serializable]
    public sealed class OptionalStageObjectiveDefinition
    {
        [SerializeField] private string objectiveId = string.Empty;
        [SerializeField] private string descriptionKey = string.Empty;
        [SerializeField] private string rewardKey = string.Empty;
        public string ObjectiveId => objectiveId;
        public string DescriptionKey => descriptionKey;
        public string RewardKey => rewardKey;
    }

    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Stage Runtime Policy", fileName = "CampaignStageRuntimePolicy")]
    public sealed class CampaignStageRuntimePolicy : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private float timeLimitSeconds;
        [SerializeField] private float fastClearThresholdSeconds;
        [SerializeField] private string[] checkpointIds = Array.Empty<string>();
        [SerializeField] private RetryLayoutPolicy retryLayoutPolicy;
        [SerializeField] private BubbleRevealPolicy bubbleRevealPolicy;
        [SerializeField] private OptionalStageObjectiveDefinition[] optionalObjectives = Array.Empty<OptionalStageObjectiveDefinition>();
        [SerializeField] private string themeCompletionStoryKey = string.Empty;
        [SerializeField] private string tutorialSequenceKey = string.Empty;
        [SerializeField] private bool autoShowTutorialOnEntry;

        public int Version => version;
        public float TimeLimitSeconds => timeLimitSeconds;
        public float FastClearThresholdSeconds => fastClearThresholdSeconds;
        public IReadOnlyList<string> CheckpointIds => checkpointIds;
        public RetryLayoutPolicy RetryLayout => retryLayoutPolicy;
        public BubbleRevealPolicy BubbleReveal => bubbleRevealPolicy;
        public IReadOnlyList<OptionalStageObjectiveDefinition> OptionalObjectives => optionalObjectives;
        public string ThemeCompletionStoryKey => themeCompletionStoryKey;
        public string TutorialSequenceKey => tutorialSequenceKey;
        public bool AutoShowTutorialOnEntry => autoShowTutorialOnEntry;
        public bool HasRequiredRuntimeRules => version > 0 && timeLimitSeconds > 0f && checkpointIds != null && checkpointIds.Length > 0 &&
                                               retryLayoutPolicy == RetryLayoutPolicy.ReuseStageAndBubbleLayout &&
                                               bubbleRevealPolicy == BubbleRevealPolicy.ContactOnly && !autoShowTutorialOnEntry;
    }

}
