using System;
using System.Collections.Generic;

namespace ANIMOL.Core
{
    public sealed class BubbleObjectiveService
    {
        private readonly HashSet<string> collectedKeys = new HashSet<string>(StringComparer.Ordinal);
        public int TargetCount { get; }
        public int CollectedCount => collectedKeys.Count;
        public bool IsExitEligible => CollectedCount >= TargetCount;
        public event Action<int, int> ProgressChanged;
        public event Action ExitEligible;

        public BubbleObjectiveService(int targetCount = 3)
        {
            if (targetCount <= 0) throw new ArgumentOutOfRangeException(nameof(targetCount));
            TargetCount = targetCount;
        }

        public bool TryCollect(string runId, string mapInstanceId, string slotId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(mapInstanceId) ||
                string.IsNullOrWhiteSpace(slotId) || string.IsNullOrWhiteSpace(ownerId)) return false;
            var key = $"{runId}|{mapInstanceId}|{slotId}|{ownerId}";
            if (!collectedKeys.Add(key)) return false;
            ProgressChanged?.Invoke(CollectedCount, TargetCount);
            if (CollectedCount == TargetCount) ExitEligible?.Invoke();
            return true;
        }
    }
}
