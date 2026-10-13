using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [CreateAssetMenu(menuName = "ANIMOL/Development/Test Run", fileName = "DevTestRunDefinition")]
    public sealed class DevTestRunDefinition : ScriptableObject
    {
        [SerializeField] private string stageId = "DEV-TEST-01";
        [SerializeField] private string mapInstanceId = "DEV-GRAYBOX-16X16";
        [SerializeField] private int targetBubbleCount = 3;
        [SerializeField] private string[] fixedAllowedAnimalIds = { "DEV_GROUND", "DEV_GLIDER" };
        [SerializeField] private string initialAnimalId = "DEV_GROUND";
        [SerializeField] private float timeLimitSeconds = 45f;
        [SerializeField] private string[] checkpointIds = { "CHECKPOINT_START", "CHECKPOINT_MID" };
        [SerializeField] private int mapLayoutSeed = 601;
        [SerializeField] private int bubbleLayoutSeed = 603;

        public string StageId => stageId;
        public string MapInstanceId => mapInstanceId;
        public int TargetBubbleCount => targetBubbleCount;
        public IReadOnlyList<string> FixedAllowedAnimalIds => fixedAllowedAnimalIds;
        public string InitialAnimalId => initialAnimalId;
        public float TimeLimitSeconds => timeLimitSeconds;
        public IReadOnlyList<string> CheckpointIds => checkpointIds;
        public int MapLayoutSeed => mapLayoutSeed;
        public int BubbleLayoutSeed => bubbleLayoutSeed;
        public ANIMOL.Core.RetryLayoutIdentity LayoutIdentity => new ANIMOL.Core.RetryLayoutIdentity(stageId, mapLayoutSeed, bubbleLayoutSeed);
    }

    public static class DevRunResultState
    {
        public static bool HasResult { get; private set; }
        public static string StageId { get; private set; } = string.Empty;
        public static float TimeSeconds { get; private set; }
        public static int BubbleCount { get; private set; }
        public static ANIMOL.Core.CampaignRunOutcome Outcome { get; private set; }
        public static ANIMOL.Core.RetryLayoutIdentity LayoutIdentity { get; private set; }

        public static void Record(string stageId, float timeSeconds, int bubbleCount, ANIMOL.Core.CampaignRunOutcome outcome, ANIMOL.Core.RetryLayoutIdentity layoutIdentity)
        {
            HasResult = true;
            StageId = stageId;
            TimeSeconds = timeSeconds;
            BubbleCount = bubbleCount;
            Outcome = outcome;
            LayoutIdentity = layoutIdentity;
        }

        public static void Clear()
        {
            HasResult = false;
            StageId = string.Empty;
            TimeSeconds = 0f;
            BubbleCount = 0;
            Outcome = ANIMOL.Core.CampaignRunOutcome.None;
            LayoutIdentity = default;
        }
    }
}
