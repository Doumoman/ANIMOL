using System;
using NUnit.Framework;

namespace Animol.NetUiDev.Tests
{
    // Explicit fixture IDs only. None are production ANIMOL animal/mode IDs.
    public sealed class NetUiInvariantTests
    {
        [Test] public void ConfirmedLeaveBeforeUiDeliveryDoesNotPretendReceiptWasConsumed()
        {
            var journal = Journal(Guid.NewGuid().ToString("N")); var request = Request();
            journal.Begin(request); journal.Apply(Accepted(request)); journal.ConfirmLeft();
            Assert.IsFalse(journal.BlocksNewEntry); Assert.IsFalse(journal.Record.ReceiptConsumed); Assert.IsTrue(journal.Record.RoomLeftConfirmed);
        }
        [Test] public void UnknownRoomPhaseCannotRenderAsWaiting()
        {
            Assert.Throws<InvalidOperationException>(() => NetUiValidation.ValidateRoom(new NetUiRoomState {
                RoomId = "fixture-room", Capacity = 4, Phase = "unexpected", Participants = new NetUiParticipant[0] }, Catalog(), Map()));
        }
        private NetUiEntryRequest Request(string action = "fixture-action")
        { return new NetUiEntryRequest { ActionId = action, ContextId = "fixture-mode", EntryIntent = "fixture-create", SnapshotRevision = "fixture-snapshot", PolicyRevision = "fixture-policy", RoomCode = "", Loadout = new NetUiLoadout { Ground = "fixture-ground", Special = "fixture-special", Air = "fixture-air" }, Options = new NetUiOptionValue[0] }; }
        private NetUiEntryResult Accepted(NetUiEntryRequest request)
        { return new NetUiEntryResult { ActionId = request.ActionId, Status = "Accepted", SnapshotRevision = request.SnapshotRevision, AcceptanceToken = "fixture-token", AcceptedContextId = request.ContextId, AcceptedEntryIntent = request.EntryIntent, AcceptedPolicyRevision = request.PolicyRevision, AcceptedLoadout = request.Loadout.Copy(), RoomId = "fixture-room", RoomCode = "ABC234" }; }
        private NetUiCatalog Catalog()
        { return new NetUiCatalog { Animals = new[] { Animal("fixture-ground", "Rabbit", "Ground"), Animal("fixture-special", "DreamFox", "Special"), Animal("fixture-air", "Swallow", "Air"), new NetUiAnimalDefinition { AnimalId = "fixture-unused", StableArtId = "Wolf", Role = "Ground", Implemented = false } }, Modes = new[] { new NetUiModeDefinition { ModeId = "fixture-mode", PolicyRevision = "fixture-policy", RepresentativeRole = "Ground", AllowedAnimalIds = null, EntryIntents = new[] { new NetUiIntentDefinition { IntentId = "fixture-create", Kind = "create" } }, Options = new NetUiOptionDefinition[0] } }, CodeFormat = new NetUiCodeFormat { Length = 6, Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789" } }; }
        private NetUiAnimalDefinition Animal(string id, string art, string role)
        { return new NetUiAnimalDefinition { AnimalId = id, StableArtId = art, Role = role, Implemented = true }; }
        private NetUiIdMapEntry[] Map()
        { return new[] { new NetUiIdMapEntry { AnimalId = "fixture-ground", StableArtId = "Rabbit", Role = "Ground" }, new NetUiIdMapEntry { AnimalId = "fixture-special", StableArtId = "DreamFox", Role = "Special" }, new NetUiIdMapEntry { AnimalId = "fixture-air", StableArtId = "Swallow", Role = "Air" } }; }
        private NetUiContextSnapshot Context()
        { return new NetUiContextSnapshot { Context = new NetUiContext { ContextId = "fixture-mode", ModeId = "fixture-mode", EntryIntent = "fixture-create", PolicyRevision = "fixture-policy", RoomCode = "", AllowedAnimalIds = null, RepresentativeRole = "Ground", InitialLoadout = Request().Loadout }, Snapshot = new NetUiSnapshot { Revision = "fixture-snapshot", Animals = new[] { Permission("fixture-ground"), Permission("fixture-special"), Permission("fixture-air") } } }; }
        private NetUiAnimalPermission Permission(string id)
        { return new NetUiAnimalPermission { AnimalId = id, Implemented = true, Unlocked = false, HasContextPermission = true, CanUseInContext = true }; }
        private NetUiRequestJournal Journal(string slot, string account = "fixture-account")
        { return new NetUiRequestJournal("http://127.0.0.1:8765", account, slot); }

        [Test] public void SelectedThreeCanUseContextDespiteUnlockedFalseAndUnusedUnimplementedAnimal()
        { Assert.DoesNotThrow(() => NetUiValidation.ValidateEntry(Request(), Context(), Catalog(), Map())); }

        [Test] public void EmptyAllowedPoolRejectsAllSelections()
        { var context = Context(); context.Context.AllowedAnimalIds = new string[0]; Assert.Throws<InvalidOperationException>(() => NetUiValidation.ValidateEntry(Request(), context, Catalog(), Map())); }

        [Test] public void UnlockDoesNotReplaceContextPermission()
        { var context = Context(); context.Snapshot.Animals[0].Unlocked = true; context.Snapshot.Animals[0].HasContextPermission = false; Assert.Throws<InvalidOperationException>(() => NetUiValidation.ValidateEntry(Request(), context, Catalog(), Map())); }

        [Test] public void MappingMustBeExplicitAndRoundTrip()
        { var map = Map(); map[1].StableArtId = "StarCat"; Assert.Throws<InvalidOperationException>(() => NetUiValidation.ToArt(Request().Loadout, Catalog(), map)); }

        [TestCase("action")] [TestCase("policy")] [TestCase("intent")] [TestCase("loadout")] [TestCase("snapshot")] [TestCase("token")]
        public void MismatchedReceiptCannotBeConsumed(string mutation)
        {
            var request = Request(); var result = Accepted(request);
            if (mutation == "action") result.ActionId = "another-action";
            if (mutation == "policy") result.AcceptedPolicyRevision = "another-policy";
            if (mutation == "intent") result.AcceptedEntryIntent = "another-intent";
            if (mutation == "loadout") result.AcceptedLoadout.Air = "another-air";
            if (mutation == "snapshot") result.SnapshotRevision = "another-snapshot";
            if (mutation == "token") result.AcceptanceToken = "";
            Assert.Throws<InvalidOperationException>(() => NetUiValidation.ValidateReceipt(request, result));
        }

        [Test] public void PendingRestartBecomesUnknownAndCannotCreateSecondAction()
        { string slot = Guid.NewGuid().ToString("N"); var first = Journal(slot); first.Begin(Request()); var restarted = Journal(slot); Assert.AreEqual("Unknown", restarted.Record.State); Assert.Throws<InvalidOperationException>(() => restarted.Begin(Request("another-action"))); }

        [Test] public void ActiveScopeBlocksDifferentAccountAfterProcessRestart()
        { string slot = Guid.NewGuid().ToString("N"); Journal(slot).Begin(Request()); Assert.Throws<InvalidOperationException>(() => Journal(slot, "other-account")); Assert.AreEqual("fixture-account", NetUiRequestJournal.ReadActiveScope(slot).AccountId); }

        [Test] public void JournalPublicRecordCannotChangeImmutableRequestOrClearUnknown()
        { string slot = Guid.NewGuid().ToString("N"); var journal = Journal(slot); journal.Begin(Request()); var exposed = journal.Record; exposed.RequestJson = ""; exposed.State = "Idle"; Assert.AreEqual("fixture-action", journal.ReadRequest().ActionId); Assert.IsTrue(journal.BlocksNewEntry); }

        [Test] public void ConsumedReceiptSurvivesRestartAndActiveRoomStillBlocksNewEntry()
        { string slot = Guid.NewGuid().ToString("N"); var journal = Journal(slot); var request = Request(); journal.Begin(request); journal.Apply(Accepted(request)); Assert.IsTrue(journal.Consume(request.ActionId, "fixture-token")); var restarted = Journal(slot); Assert.IsFalse(restarted.Consume(request.ActionId, "fixture-token")); Assert.IsTrue(restarted.BlocksNewEntry); restarted.ConfirmLeft(); Assert.IsFalse(restarted.BlocksNewEntry); Assert.IsNull(NetUiRequestJournal.ReadActiveScope(slot)); }

        [Test] public void WrongAcknowledgmentDoesNotConsumeAcceptedReceipt()
        { string slot = Guid.NewGuid().ToString("N"); var journal = Journal(slot); var request = Request(); journal.Begin(request); journal.Apply(Accepted(request)); Assert.Throws<InvalidOperationException>(() => journal.Consume("another-action", "fixture-token")); Assert.IsFalse(journal.Record.ReceiptConsumed); }

        [Test] public void UnknownRetryRequiresAuthoritativeNotFoundAndKeepsExactPayload()
        { string slot = Guid.NewGuid().ToString("N"); var journal = Journal(slot); journal.Begin(Request()); string original = journal.Record.RequestJson; journal.MarkUnknown("NETWORK_RESULT_UNKNOWN"); Assert.Throws<InvalidOperationException>(() => journal.MarkRetryPending()); journal.Apply(new NetUiEntryResult { ActionId = "fixture-action", Status = "Unknown", Reason = "ACTION_NOT_FOUND" }); journal.MarkRetryPending(); Assert.AreEqual(original, journal.Record.RequestJson); }

        [TestCase("http://127.0.0.1:8765")] [TestCase("http://localhost:8765")]
        public void MobileLoopbackIsRejected(string url)
        { Assert.Throws<ArgumentException>(() => NetUiValidation.NormalizeServerUrl(url, true)); }
    }
}
