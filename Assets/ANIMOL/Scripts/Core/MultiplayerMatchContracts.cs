using System;
using System.Collections.Generic;
using System.Linq;

namespace ANIMOL.Core
{
    public enum MultiplayerAnimalRole { Ground, Air, Special }
    public enum GrowthPresentationMode { OwnedProgress, RankedMaximumPreset }
    public enum ParticipantConnectionState { Connected, Disconnected, Reconnecting, Restored }
    public enum CompetitiveMapStatus { Pending, InProgress, Completed, TimeExpired }
    public enum BubbleSlotAuthorityStatus { None, AwaitingServerConfirmation, Claimed, RespawnPending }

    public readonly struct ParticipantCountRange
    {
        public int Minimum { get; }
        public int Maximum { get; }
        public ParticipantCountRange(int minimum, int maximum)
        {
            if (minimum <= 0 || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(minimum));
            Minimum = minimum;
            Maximum = maximum;
        }
        public bool Contains(int count) => count >= Minimum && count <= Maximum;
    }

    public static class MultiplayerModeRules
    {
        public static ParticipantCountRange CompetitiveParticipants => new ParticipantCountRange(4, 8);
        public static ParticipantCountRange CoopParticipants => new ParticipantCountRange(2, 4);
        public static int CompetitiveMapCount => 3;
        public static bool AllowCompetitiveBodyAttack => false;
        public static bool AllowCompetitiveKnockback => false;
    }

    public readonly struct CampaignAnimalUnlockRule
    {
        public string AnimalId { get; }
        public MultiplayerAnimalRole Role { get; }
        public string RequiredCompletedStageId { get; }
        public CampaignAnimalUnlockRule(string animalId, MultiplayerAnimalRole role, string requiredCompletedStageId)
        {
            AnimalId = animalId ?? string.Empty;
            Role = role;
            RequiredCompletedStageId = requiredCompletedStageId ?? string.Empty;
        }
    }

    public sealed class CampaignAnimalUnlockSnapshot
    {
        private readonly HashSet<string> unlockedIds;
        public int CampaignRevision { get; }
        public bool IsDevelopmentSimulation { get; }
        internal CampaignAnimalUnlockSnapshot(IEnumerable<string> unlockedIds, int campaignRevision, bool developmentSimulation)
        {
            this.unlockedIds = new HashSet<string>(unlockedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            CampaignRevision = campaignRevision;
            IsDevelopmentSimulation = developmentSimulation;
        }
        public bool IsUnlocked(string animalId) => !string.IsNullOrWhiteSpace(animalId) && unlockedIds.Contains(animalId);
        public static CampaignAnimalUnlockSnapshot DevelopmentOnly(params string[] animalIds) =>
            new CampaignAnimalUnlockSnapshot(animalIds, -1, true);
    }

    public sealed class CampaignAnimalUnlockResolver
    {
        private readonly CampaignAnimalUnlockRule[] rules;
        public CampaignAnimalUnlockResolver(IEnumerable<CampaignAnimalUnlockRule> rules) => this.rules = rules?.ToArray() ?? Array.Empty<CampaignAnimalUnlockRule>();
        public CampaignAnimalUnlockSnapshot Resolve(CampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            var unlocked = rules.Where(rule => string.IsNullOrWhiteSpace(rule.RequiredCompletedStageId) ||
                                               progress.CompletedStageIds.Contains(rule.RequiredCompletedStageId))
                                .Select(rule => rule.AnimalId);
            return new CampaignAnimalUnlockSnapshot(unlocked, progress.Revision, false);
        }
    }

    public sealed class CompetitiveParticipantSelectionService
    {
        private readonly Dictionary<string, Dictionary<MultiplayerAnimalRole, string>> selections;
        private readonly Dictionary<string, CampaignAnimalUnlockRule> animalRules;
        private readonly CampaignAnimalUnlockSnapshot unlocks;
        public bool AllowsSameAnimalAcrossParticipants => true;

        public CompetitiveParticipantSelectionService(IEnumerable<string> participantIds, IEnumerable<CampaignAnimalUnlockRule> rules, CampaignAnimalUnlockSnapshot unlocks)
        {
            var ids = participantIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
            if (!MultiplayerModeRules.CompetitiveParticipants.Contains(ids.Length)) throw new ArgumentOutOfRangeException(nameof(participantIds));
            selections = ids.ToDictionary(x => x, _ => new Dictionary<MultiplayerAnimalRole, string>(), StringComparer.Ordinal);
            animalRules = (rules ?? Array.Empty<CampaignAnimalUnlockRule>()).ToDictionary(x => x.AnimalId, StringComparer.Ordinal);
            this.unlocks = unlocks ?? throw new ArgumentNullException(nameof(unlocks));
        }

        public bool TrySelect(string participantId, MultiplayerAnimalRole role, string animalId)
        {
            if (!selections.TryGetValue(participantId ?? string.Empty, out var participant) ||
                !animalRules.TryGetValue(animalId ?? string.Empty, out var rule) || rule.Role != role || !unlocks.IsUnlocked(animalId)) return false;
            participant[role] = animalId;
            return true;
        }

        public string GetSelection(string participantId, MultiplayerAnimalRole role) =>
            selections.TryGetValue(participantId ?? string.Empty, out var participant) && participant.TryGetValue(role, out var animalId) ? animalId : string.Empty;

        public bool CanReady(string participantId) => selections.TryGetValue(participantId ?? string.Empty, out var participant) &&
                                                      Enum.GetValues(typeof(MultiplayerAnimalRole)).Cast<MultiplayerAnimalRole>().All(participant.ContainsKey);
    }

    public sealed class MultiplayerParticipantState
    {
        public string ParticipantId { get; }
        public string DisplayName { get; }
        public ParticipantConnectionState Connection { get; private set; }
        public MultiplayerParticipantState(string participantId, string displayName)
        {
            ParticipantId = participantId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Connection = ParticipantConnectionState.Connected;
        }
        public void SetConnection(ParticipantConnectionState state) => Connection = state;
    }

    public sealed class MultiplayerRoomPreviewState
    {
        private readonly ParticipantCountRange range;
        private readonly List<MultiplayerParticipantState> participants = new List<MultiplayerParticipantState>();
        public GameModeKind Mode { get; }
        public GrowthPresentationMode GrowthMode { get; }
        public IReadOnlyList<MultiplayerParticipantState> Participants => participants;
        public bool DevelopmentPreviewOnly => true;
        public MultiplayerRoomPreviewState(GameModeKind mode, int participantCount, bool ranked)
        {
            Mode = mode;
            range = mode == GameModeKind.Coop ? MultiplayerModeRules.CoopParticipants : MultiplayerModeRules.CompetitiveParticipants;
            GrowthMode = ranked ? GrowthPresentationMode.RankedMaximumPreset : GrowthPresentationMode.OwnedProgress;
            SetParticipantCount(participantCount);
        }
        public bool SetParticipantCount(int count)
        {
            if (!range.Contains(count)) return false;
            participants.Clear();
            for (var i = 0; i < count; i++) participants.Add(new MultiplayerParticipantState($"DEV-P{i + 1:00}", $"P{i + 1}"));
            return true;
        }
        public int ToggleMinimumMaximum()
        {
            SetParticipantCount(participants.Count == range.Minimum ? range.Maximum : range.Minimum);
            return participants.Count;
        }
    }

    public sealed class CompetitiveMapResultState
    {
        public int MapNumber { get; }
        public CompetitiveMapStatus Status { get; private set; }
        public int ServerPerformanceScore { get; private set; }
        public string Summary { get; private set; } = "서버 결과 대기";
        public CompetitiveMapResultState(int mapNumber) { MapNumber = mapNumber; Status = mapNumber == 1 ? CompetitiveMapStatus.InProgress : CompetitiveMapStatus.Pending; }
        internal void Apply(CompetitiveMapStatus status, int score, string summary)
        {
            Status = status;
            ServerPerformanceScore = Math.Max(0, score);
            Summary = string.IsNullOrWhiteSpace(summary) ? status.ToString() : summary;
        }
    }

    public sealed class CompetitiveSeriesState
    {
        private readonly CompetitiveMapResultState[] maps = Enumerable.Range(1, MultiplayerModeRules.CompetitiveMapCount).Select(x => new CompetitiveMapResultState(x)).ToArray();
        public IReadOnlyList<CompetitiveMapResultState> Maps => maps;
        public int CurrentMapNumber { get; private set; } = 1;
        public int TotalServerPerformanceScore => maps.Sum(x => x.ServerPerformanceScore);
        public bool HasAuthoritativeRanking => false;

        public bool ApplyServerMapResult(int mapNumber, CompetitiveMapStatus status, int score, string summary, bool serverConfirmed)
        {
            if (!serverConfirmed || mapNumber < 1 || mapNumber > maps.Length) return false;
            maps[mapNumber - 1].Apply(status, score, summary);
            if (mapNumber < maps.Length)
            {
                CurrentMapNumber = mapNumber + 1;
                maps[CurrentMapNumber - 1].Apply(CompetitiveMapStatus.InProgress, 0, "진행 중");
            }
            return true;
        }
    }

    public sealed class CompetitiveBubbleAuthorityState
    {
        private readonly Dictionary<string, string> confirmedClaims = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> personalCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        public string PendingSlotId { get; private set; } = string.Empty;
        public BubbleSlotAuthorityStatus Status { get; private set; }
        public bool RequestClaim(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId) || confirmedClaims.ContainsKey(slotId)) return false;
            PendingSlotId = slotId;
            Status = BubbleSlotAuthorityStatus.AwaitingServerConfirmation;
            return true;
        }
        public bool ApplyServerClaim(string slotId, string participantId, bool awarded)
        {
            if (Status != BubbleSlotAuthorityStatus.AwaitingServerConfirmation || PendingSlotId != slotId) return false;
            if (awarded)
            {
                confirmedClaims[slotId] = participantId;
                personalCounts[participantId] = GetPersonalCount(participantId) + 1;
            }
            Status = BubbleSlotAuthorityStatus.RespawnPending;
            return true;
        }
        public int GetPersonalCount(string participantId) => personalCounts.TryGetValue(participantId ?? string.Empty, out var count) ? count : 0;
        public bool HasExitEligibility(string participantId) => GetPersonalCount(participantId) >= 3;
    }

    public sealed class CompetitiveFinishWindowState
    {
        public bool FirstFinishConfirmed { get; private set; }
        public float AdditionalFinishSeconds { get; private set; }
        public bool ApplyServerFirstFinish(float seconds, bool serverConfirmed)
        {
            if (!serverConfirmed || seconds <= 0f) return false;
            FirstFinishConfirmed = true;
            AdditionalFinishSeconds = seconds;
            return true;
        }
    }

    public sealed class CoopTeamObjectiveState
    {
        public int ParticipantCount { get; }
        public int TeamBubbleCount { get; private set; }
        public int EscapedMemberCount { get; private set; }
        public CoopTeamObjectiveState(int participantCount)
        {
            if (!MultiplayerModeRules.CoopParticipants.Contains(participantCount)) throw new ArgumentOutOfRangeException(nameof(participantCount));
            ParticipantCount = participantCount;
        }
        public bool ApplyServerProgress(int teamBubbles, int escapedMembers, bool serverConfirmed)
        {
            if (!serverConfirmed) return false;
            TeamBubbleCount = Math.Clamp(teamBubbles, 0, 3);
            EscapedMemberCount = Math.Clamp(escapedMembers, 0, ParticipantCount);
            return true;
        }
    }
}
