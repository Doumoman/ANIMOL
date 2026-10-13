using System;

namespace Animol.NetUiDev
{
    // Fields deliberately match the NET02 server JSON. These are service IDs,
    // never display names or assumed IDs from the fifteen portrait illustrations.
    [Serializable] public class NetUiEnvelope { public string Status; public string Reason; }
    [Serializable] public sealed class NetUiSessionRequest { public string AccountId; public string DevAccessKey; }
    [Serializable] public sealed class NetUiSessionResponse : NetUiEnvelope
    { public string SessionToken; public string AccountId; public string SessionNamespace; }
    [Serializable] public sealed class NetUiAnimalDefinition
    { public string AnimalId; public string StableArtId; public string Role; public bool Implemented; }
    [Serializable] public sealed class NetUiIntentDefinition { public string IntentId; public string Kind; }
    [Serializable] public sealed class NetUiOptionDefinition
    { public string OptionId; public string[] AllowedValues; public string DefaultValue; }
    [Serializable] public sealed class NetUiModeDefinition
    {
        public string ModeId; public string DisplayName; public string PolicyRevision;
        public string[] AllowedAnimalIds; public string RepresentativeRole;
        public NetUiOptionDefinition[] Options; public NetUiIntentDefinition[] EntryIntents;
        public int PublicStartCount; public int CustomStartMinimum;
    }
    [Serializable] public sealed class NetUiCodeFormat { public int Length; public string Alphabet; }
    [Serializable] public sealed class NetUiCatalog : NetUiEnvelope
    { public NetUiModeDefinition[] Modes; public NetUiAnimalDefinition[] Animals; public NetUiCodeFormat CodeFormat; }
    [Serializable] public sealed class NetUiLoadout
    {
        public string Ground; public string Special; public string Air;
        public NetUiLoadout Copy() { return new NetUiLoadout { Ground = Ground, Special = Special, Air = Air }; }
    }
    [Serializable] public sealed class NetUiContextRequest
    { public string ModeId; public string EntryIntent; public string RoomCode; }
    [Serializable] public sealed class NetUiContext
    {
        public string ContextId; public string ModeId; public string EntryIntent; public string PolicyRevision;
        public string[] AllowedAnimalIds; public NetUiLoadout InitialLoadout; public string RepresentativeRole; public string RoomCode;
    }
    [Serializable] public sealed class NetUiAnimalPermission
    { public string AnimalId; public bool Implemented; public bool Unlocked; public bool HasContextPermission; public bool CanUseInContext; }
    [Serializable] public sealed class NetUiSnapshot { public string Revision; public NetUiAnimalPermission[] Animals; }
    [Serializable] public sealed class NetUiContextSnapshot : NetUiEnvelope
    { public NetUiContext Context; public NetUiSnapshot Snapshot; }
    [Serializable] public sealed class NetUiLookupRequest { public string RoomCode; }
    [Serializable] public sealed class NetUiLookupResponse : NetUiEnvelope
    { public string RoomId; public string RoomCode; public string ModeId; public string PolicyRevision; public string Phase; public int ParticipantCount; public int Capacity; }
    [Serializable] public sealed class NetUiOptionValue { public string OptionId; public string Value; }
    [Serializable] public sealed class NetUiEntryRequest
    {
        public string ActionId; public string ContextId; public string SnapshotRevision; public string PolicyRevision;
        public string EntryIntent; public string RoomCode; public NetUiLoadout Loadout; public NetUiOptionValue[] Options;
    }
    [Serializable] public sealed class NetUiEntryResult : NetUiEnvelope
    {
        public string ActionId; public string SnapshotRevision; public string AcceptanceToken;
        public string AcceptedContextId; public string AcceptedPolicyRevision; public string AcceptedEntryIntent;
        public NetUiLoadout AcceptedLoadout; public string RoomId; public string RoomCode;
    }
    [Serializable] public sealed class NetUiParticipant
    { public string AccountId; public bool Ready; public bool Connected; public bool IsOwner; public NetUiLoadout Loadout; }
    [Serializable] public sealed class NetUiRoomState : NetUiEnvelope
    {
        public string RoomId; public string RoomCode; public string ModeId; public string PolicyRevision;
        public string Phase; public string Revision; public string OwnerAccountId; public NetUiParticipant[] Participants;
        public int Capacity; public bool CanReady; public bool CanStart; public bool CanLeave;
        public NetUiOptionValue[] Options;
    }
    [Serializable] public sealed class NetUiReadyRequest { public string RoomId; public bool Ready; }
    [Serializable] public sealed class NetUiRoomCommand { public string RoomId; }
    [Serializable] public sealed class NetUiEmptyRequest { }
    [Serializable] public sealed class NetUiIdMapEntry { public string StableArtId; public string AnimalId; public string Role; }
    [Serializable] public sealed class NetUiArtLoadout { public string Ground; public string Special; public string Air; }
    [Serializable] public sealed class NetUiJournal
    {
        public int SchemaVersion = 1;
        public string AccountId; public string ServerUrl; public string SessionToken; public string SessionNamespace;
        public string RequestJson; public string ResultJson; public string State;
        public bool ReceiptConsumed;
        public bool RoomLeftConfirmed;
        // No DEV password is persisted here.
    }
    [Serializable] public sealed class NetUiActiveScope { public string ServerUrl; public string AccountId; }
}
