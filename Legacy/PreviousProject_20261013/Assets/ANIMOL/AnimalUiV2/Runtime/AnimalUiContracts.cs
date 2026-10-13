using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ANIMOL.AnimalUiV2
{
    public enum AnimalRole { Ground, Special, Air }
    public enum AnimalUiMode { CharacterUpgrade, StageAnimalSelect, MultiplayerAnimalSelect }
    public enum CampaignSelectionPolicy { Fixed, AllowedPool, Free }
    public enum UpgradeTrack { Active, Passive }
    public enum UpgradeQuoteState { Ready, InsufficientFunds, Maximum, Unconfigured, Locked, Unavailable }
    public enum CommitStatus { Accepted, Rejected, Unavailable, Unknown }

    [Serializable]
    public sealed class AnimalLoadout
    {
        public string Ground;
        public string Special;
        public string Air;
        public string Get(AnimalRole role) => role == AnimalRole.Ground ? Ground : role == AnimalRole.Special ? Special : Air;
        public void Set(AnimalRole role, string id)
        {
            if (role == AnimalRole.Ground) Ground = id;
            else if (role == AnimalRole.Special) Special = id;
            else Air = id;
        }
        public AnimalLoadout Clone() => new AnimalLoadout { Ground = Ground, Special = Special, Air = Air };
        public string Fingerprint() => (Ground ?? "") + "|" + (Special ?? "") + "|" + (Air ?? "");
    }

    [Serializable]
    public sealed class AnimalProgress
    {
        public string AnimalId;
        public bool Unlocked;
        public bool Implemented;
        // Resolved per-context authority; e.g. a stage can grant a locked animal as a trial.
        public bool HasContextPermission;
        public bool CanUseInContext;
        // -1 means unconfigured; art presence never grants ownership or gameplay support.
        public int ActiveLevel = -1;
        public int PassiveLevel = -1;
        public long? MasteryBalance;
        public string AvailabilityMessage;
        public string ActiveDescription;
        public string PassiveDescription;
    }

    public sealed class AnimalUiSnapshot
    {
        public readonly List<AnimalProgress> Animals = new List<AnimalProgress>();
        public string Revision;
        // Selection authority identifies the context it actually read. Not used for upgrade snapshots.
        public string ContextId;
        public string PolicyRevision;
        public string EntryIntent; // Multiplayer authority echo; never inferred from the UI request.
        public long? CoinBalance;
        public AnimalProgress Find(string id) => Animals.Find(a => string.Equals(a.AnimalId, id, StringComparison.Ordinal));
    }

    [Serializable]
    public sealed class SelectionRequirements
    {
        // Supply the game's actual required slots. null/empty is unconfigured, not "all animals".
        public AnimalRole[] RequiredRoles;
        public AnimalRole RepresentativeRole = AnimalRole.Ground;
        public string PolicyRevision;
        public SelectionRequirements Clone() => new SelectionRequirements
        {
            RequiredRoles = RequiredRoles == null ? null : (AnimalRole[])RequiredRoles.Clone(),
            RepresentativeRole = RepresentativeRole, PolicyRevision = PolicyRevision
        };
        public bool Requires(AnimalRole role) => RequiredRoles != null && Array.IndexOf(RequiredRoles, role) >= 0;
    }

    [Serializable]
    public sealed class StageSelectionRequest
    {
        public string StageId;
        public string DisplayName;
        public CampaignSelectionPolicy Policy = CampaignSelectionPolicy.Fixed;
        public string[] AllowedAnimalIds = Array.Empty<string>();
        public AnimalLoadout FixedLoadout = new AnimalLoadout();
        public AnimalLoadout InitialLoadout = new AnimalLoadout();
        public SelectionRequirements Requirements = new SelectionRequirements();
        public StageSelectionRequest Clone() => new StageSelectionRequest
        {
            StageId = StageId, DisplayName = DisplayName, Policy = Policy,
            AllowedAnimalIds = AllowedAnimalIds == null ? Array.Empty<string>() : (string[])AllowedAnimalIds.Clone(),
            FixedLoadout = (FixedLoadout ?? new AnimalLoadout()).Clone(),
            InitialLoadout = (InitialLoadout ?? new AnimalLoadout()).Clone(),
            Requirements = (Requirements ?? new SelectionRequirements()).Clone()
        };
    }

    [Serializable]
    public sealed class MultiplayerSelectionRequest
    {
        public string ModeId;
        public string DisplayName;
        // null = no client pool restriction; an empty array = no allowed animals.
        // The backend still validates its current authoritative pool.
        public string[] AllowedAnimalIds;
        public AnimalLoadout InitialLoadout = new AnimalLoadout();
        public SelectionRequirements Requirements = new SelectionRequirements();
        // Existing host service interprets this opaque intent (create/find/invite). UI does not invent room fields.
        public string EntryIntent;
        public MultiplayerSelectionRequest Clone() => new MultiplayerSelectionRequest
        {
            ModeId = ModeId, DisplayName = DisplayName,
            AllowedAnimalIds = AllowedAnimalIds == null ? null : (string[])AllowedAnimalIds.Clone(),
            InitialLoadout = (InitialLoadout ?? new AnimalLoadout()).Clone(),
            Requirements = (Requirements ?? new SelectionRequirements()).Clone(), EntryIntent = EntryIntent
        };
    }

    public sealed class UpgradeCost
    {
        public string CurrencyId;
        public string DisplayName;
        public long Amount;
    }

    public sealed class UpgradeQuote
    {
        public string AnimalId;
        public UpgradeTrack Track;
        public UpgradeQuoteState State = UpgradeQuoteState.Unconfigured;
        public int CurrentLevel = -1;
        public int NextLevel = -1;
        public string CurrentEffect;
        public string NextEffect;
        public string Message;
        // Opaque backend-issued version token; UI must not manufacture prices or effects.
        public string QuoteToken;
        // Ready quotes must either contain explicit costs (including zero) or declare an intentional free upgrade.
        public bool IsFree;
        public readonly List<UpgradeCost> Costs = new List<UpgradeCost>();
    }

    public sealed class UpgradeCommitRequest
    {
        public string ActionId;
        public string AnimalId;
        public UpgradeTrack Track;
        public string QuoteToken;
        public int ExpectedCurrentLevel;
    }

    public sealed class SelectionCommitRequest
    {
        public string ActionId;
        public string ContextId;
        public string SnapshotRevision;
        public string PolicyRevision;
        public AnimalLoadout Loadout;
        // Captured opaque host intent; retries keep this value unchanged.
        public string EntryIntent;
        // The presenter keeps its request private; services receive copies, including on retry.
        public SelectionCommitRequest Clone() => new SelectionCommitRequest
        {
            ActionId = ActionId, ContextId = ContextId, SnapshotRevision = SnapshotRevision,
            PolicyRevision = PolicyRevision, Loadout = Loadout?.Clone(), EntryIntent = EntryIntent
        };
    }

    public sealed class AnimalUiCommitResult
    {
        public CommitStatus Status = CommitStatus.Unavailable;
        public string Message;
        public AnimalUiSnapshot Snapshot;
        // Backend-owned stage-entry / room-entry receipt. Only Accepted emits navigation.
        public string AcceptanceToken;
        public AnimalLoadout AcceptedLoadout;
        // Phase 2 maps these from the actual stage-entry receipt, never from a guessed UI success.
        public string AcceptedContextId;
        public string AcceptedPolicyRevision;
        // Map this from the actual room-entry receipt, never from the UI request.
        public string AcceptedEntryIntent;
    }

    public sealed class AnimalUiReadRequest
    {
        public AnimalUiMode Mode;
        public StageSelectionRequest Stage;
        public MultiplayerSelectionRequest Multiplayer;
    }

    public interface IAnimalUiBackend
    {
        Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest context, CancellationToken cancellationToken);
        Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken);
        Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken);
        Task<AnimalUiCommitResult> SubmitCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);
        Task<AnimalUiCommitResult> SubmitMultiplayerAsync(MultiplayerSelectionRequest context, SelectionCommitRequest request, CancellationToken cancellationToken);
    }
}
