using System;
using System.Collections.Generic;

namespace ANIMOL.Core
{
    public enum CampaignRunOutcome { None, Cleared, TimeExpired, Abandoned }

    public sealed class CampaignRunTimer
    {
        public float LimitSeconds { get; }
        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds => Math.Max(0f, LimitSeconds - ElapsedSeconds);
        public bool IsExpired => ElapsedSeconds >= LimitSeconds;
        public CampaignRunTimer(float limitSeconds)
        {
            if (limitSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            LimitSeconds = limitSeconds;
        }
        public bool Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsExpired) return false;
            ElapsedSeconds = Math.Min(LimitSeconds, ElapsedSeconds + deltaSeconds);
            return IsExpired;
        }
    }

    public sealed class CheckpointRespawnState
    {
        private readonly HashSet<string> allowed;
        public string ActiveCheckpointId { get; private set; }
        public CheckpointRespawnState(IEnumerable<string> checkpointIds, string initialCheckpointId)
        {
            allowed = new HashSet<string>(checkpointIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (!allowed.Contains(initialCheckpointId)) throw new ArgumentException("Initial checkpoint must be fixed stage content.");
            ActiveCheckpointId = initialCheckpointId;
        }
        public bool TryActivate(string checkpointId)
        {
            if (!allowed.Contains(checkpointId)) return false;
            ActiveCheckpointId = checkpointId;
            return true;
        }
    }

    public readonly struct RetryLayoutIdentity : IEquatable<RetryLayoutIdentity>
    {
        public string StageId { get; }
        public int MapSeed { get; }
        public int BubbleSeed { get; }
        public RetryLayoutIdentity(string stageId, int mapSeed, int bubbleSeed)
        {
            StageId = stageId ?? string.Empty;
            MapSeed = mapSeed;
            BubbleSeed = bubbleSeed;
        }
        public bool Equals(RetryLayoutIdentity other) => StageId == other.StageId && MapSeed == other.MapSeed && BubbleSeed == other.BubbleSeed;
        public override bool Equals(object obj) => obj is RetryLayoutIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(StageId, MapSeed, BubbleSeed);
    }

    public static class BubbleVisibilityGuard
    {
        public static bool MayRevealWorldPosition(bool hasBeenDiscoveredByContact) => hasBeenDiscoveredByContact;
        public static bool MayShowEdgeArrow(bool hasBeenDiscoveredByContact) => hasBeenDiscoveredByContact;
    }

    public readonly struct CampaignCompletionRecord
    {
        public bool Accepted { get; }
        public bool FirstClear { get; }
        public bool NewBestTime { get; }
        public CampaignCompletionRecord(bool accepted, bool firstClear, bool newBestTime)
        {
            Accepted = accepted;
            FirstClear = firstClear;
            NewBestTime = newBestTime;
        }
    }
}
