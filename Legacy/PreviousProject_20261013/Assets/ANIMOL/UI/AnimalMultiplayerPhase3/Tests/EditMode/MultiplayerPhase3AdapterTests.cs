using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalMultiplayerPhase3.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    public sealed class MultiplayerPhase3AdapterTests
    {
        private GameObject root;
        private AnimalCatalog catalog;
        [SetUp] public void Setup()
        {
            root = new GameObject("TEST ONLY multiplayer authority");
            catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalMultiplayerPhase3Builder.CatalogPath);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

        [Test] public async Task ProjectAdapterCannotInventModeAuthorityReceiptOrOtherOperations()
        {
            var adapter = root.AddComponent<AnimalMultiplayerProjectAdapter>(); adapter.Catalog = catalog;
            adapter.IdMap = AssetDatabase.LoadAssetAtPath<AnimalMultiplayerIdMap>(AnimalMultiplayerPhase3Builder.MapPath);
            string before = JsonUtility.ToJson(adapter.IdMap);
            var context = Context();
            Assert.That(adapter.DescribeContext(context), Is.EqualTo(AnimalMultiplayerProjectAdapter.MissingService));
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None));
            Assert.That(error.Message, Is.EqualTo(AnimalMultiplayerProjectAdapter.MissingService));
            Assert.That((await adapter.SubmitMultiplayerAsync(context, Request(context), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That((await adapter.SubmitCampaignAsync(null, null, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            foreach (var animal in catalog.Animals)
            {
                Assert.That((await adapter.QuoteUpgradeAsync(animal.Id, UpgradeTrack.Active, CancellationToken.None)).State, Is.EqualTo(UpgradeQuoteState.Unavailable));
                Assert.That((await adapter.TryUpgradeAsync(new UpgradeCommitRequest { AnimalId = animal.Id }, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            }
            Assert.That(JsonUtility.ToJson(adapter.IdMap), Is.EqualTo(before));
        }

        [TestCase("context")][TestCase("policy")][TestCase("intent")][TestCase("revision")]
        public void WrongOrMissingAuthorityContextCannotDispatch(string fault)
        {
            var adapter = Fixture(); var context = Context(); var snapshot = Snapshot(context);
            if (fault == "context") snapshot.ContextId = "WRONG";
            if (fault == "policy") snapshot.PolicyRevision = "WRONG";
            if (fault == "intent") snapshot.EntryIntent = "WRONG";
            if (fault == "revision") snapshot.Revision = " ";
            adapter.OnRead = _ => Task.FromResult(snapshot);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None));
            Assert.IsFalse(AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, context.InitialLoadout, context, true, true, out _));
            Assert.That(adapter.Calls, Is.Zero);
        }

        [Test] public async Task UnownedExplicitPermissionAllowsThreeAndOtherUnimplementedAnimalsDoNotBlock()
        {
            var adapter = Fixture(); var context = Context();
            var snapshot = await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            Assert.IsFalse(snapshot.Find("Rabbit").Unlocked); Assert.IsFalse(snapshot.Find("Wolf").Implemented);
            Assert.IsTrue(AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, context.InitialLoadout, context, true, true, out _));
            var response = await adapter.SubmitMultiplayerAsync(context, Request(context), CancellationToken.None);
            Assert.That(response.Status, Is.EqualTo(CommitStatus.Accepted)); Assert.That(adapter.Calls, Is.EqualTo(1));
        }

        [TestCase("implemented")][TestCase("permission")][TestCase("canUse")][TestCase("emptyPool")]
        public async Task MissingPermissionOrEmptyPoolDispatchesZero(string fault)
        {
            var adapter = Fixture(); var context = Context(); var snapshot = Snapshot(context);
            var rabbit = snapshot.Find("Rabbit"); rabbit.Unlocked = true;
            if (fault == "implemented") rabbit.Implemented = false;
            if (fault == "permission") rabbit.HasContextPermission = false;
            if (fault == "canUse") rabbit.CanUseInContext = false;
            if (fault == "emptyPool") context.AllowedAnimalIds = Array.Empty<string>();
            adapter.OnRead = _ => Task.FromResult(snapshot);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            Assert.That((await adapter.SubmitMultiplayerAsync(context, Request(context), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.Zero);
            Assert.That(context.Clone().AllowedAnimalIds, fault == "emptyPool" ? Is.Empty : Is.Null);
        }

        [Test] public async Task InvalidReadIncludingWrongModeRevokesCachedAuthority()
        {
            var adapter = Fixture(); var context = Context();
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(new AnimalUiReadRequest { Mode = AnimalUiMode.CharacterUpgrade }, CancellationToken.None));
            Assert.That((await adapter.SubmitMultiplayerAsync(context, Request(context), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.Zero);
        }

        [Test] public async Task LateOldReadCannotAuthorizeOldMode()
        {
            var adapter = Fixture(); var a = Context(); var b = Context(); b.ModeId = "TEST_MODE_B";
            var delayed = new TaskCompletionSource<AnimalUiSnapshot>();
            adapter.OnRead = c => c.ModeId == a.ModeId ? delayed.Task : Task.FromResult(Snapshot(c));
            var oldRead = adapter.ReadSnapshotAsync(Read(a), CancellationToken.None);
            await adapter.ReadSnapshotAsync(Read(b), CancellationToken.None);
            delayed.SetResult(Snapshot(a)); await oldRead;
            Assert.That((await adapter.SubmitMultiplayerAsync(a, Request(a), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.Zero);
            Assert.That((await adapter.SubmitMultiplayerAsync(b, Request(b), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Accepted));
        }

        [Test] public async Task PendingCallsAreNotDispatchedTwiceAndTerminalReceiptIsCopiedOnReplay()
        {
            var adapter = Fixture(); var context = Context(); var request = Request(context);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            var pending = new TaskCompletionSource<AnimalUiCommitResult>(); adapter.OnSubmit = (_, __) => pending.Task;
            var first = adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None);
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            var receipt = Receipt(request); pending.SetResult(receipt);
            var accepted = await first;
            receipt.AcceptedLoadout.Ground = "Wolf"; accepted.AcceptedLoadout.Air = "Bat";
            var replay = await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None);
            Assert.That(replay.AcceptedLoadout.Fingerprint(), Is.EqualTo("Rabbit|DreamFox|Swallow"));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            request.Loadout.Ground = "Wolf";
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.EqualTo(1));
        }

        [Test] public async Task UnknownRetainsOriginalAuthorityDespiteMutatedCopiesAndLaterRead()
        {
            var adapter = Fixture(); var context = Context(); var original = Request(context);
            var read = await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            read.Find("Rabbit").CanUseInContext = false;
            adapter.OnSubmit = (c, r) =>
            {
                c.AllowedAnimalIds = Array.Empty<string>(); c.InitialLoadout.Ground = "Wolf";
                c.Requirements.RequiredRoles[0] = AnimalRole.Air; r.Loadout.Ground = "Wolf"; r.EntryIntent = "MUTATED";
                return Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unknown });
            };
            Assert.That((await adapter.SubmitMultiplayerAsync(context, original, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            var other = Context(); other.ModeId = "TEST_OTHER";
            await adapter.ReadSnapshotAsync(Read(other), CancellationToken.None);
            Assert.That((await adapter.SubmitMultiplayerAsync(other, Request(other), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            adapter.OnSubmit = (c, r) =>
            {
                Assert.That(c.ModeId, Is.EqualTo(context.ModeId)); Assert.IsNull(c.AllowedAnimalIds);
                Assert.That(c.Requirements.RequiredRoles[0], Is.EqualTo(AnimalRole.Ground));
                Assert.That(r.ActionId, Is.EqualTo(original.ActionId)); Assert.That(r.EntryIntent, Is.EqualTo(original.EntryIntent));
                Assert.That(r.Loadout.Fingerprint(), Is.EqualTo(original.Loadout.Fingerprint()));
                return Task.FromResult(Receipt(r));
            };
            Assert.That((await adapter.SubmitMultiplayerAsync(context, original, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Accepted));
            Assert.That(adapter.Calls, Is.EqualTo(2));
        }

        [Test] public async Task ExceptionPreservesActionAndBlocksDifferentIntent()
        {
            var adapter = Fixture(); var context = Context(); var request = Request(context);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            adapter.OnSubmit = (_, __) => Task.FromException<AnimalUiCommitResult>(new TimeoutException("TEST ONLY"));
            Assert.ThrowsAsync<TimeoutException>(async () => await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None));
            var changed = context.Clone(); changed.EntryIntent = "OTHER";
            Assert.That((await adapter.SubmitMultiplayerAsync(changed, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            adapter.OnSubmit = (_, r) => Task.FromResult(Receipt(r));
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Accepted));
        }

        [Test] public async Task UnsupportedReconciliationNeverResubmitsUnknownOrAllowsNewAction()
        {
            var adapter = Fixture(); adapter.ReconciliationAvailable = false;
            var context = Context(); var request = Request(context);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            adapter.OnSubmit = (_, __) => Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unknown });
            await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None);
            var result = await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None);
            Assert.That(result.Status, Is.EqualTo(CommitStatus.Unknown)); Assert.That(result.Message, Does.Contain("멱등"));
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            request.ActionId = "TEST_NEW_ACTION";
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            Assert.That(adapter.Calls, Is.EqualTo(1));
        }

        [TestCase(CommitStatus.Rejected)][TestCase(CommitStatus.Unavailable)]
        public async Task DefinitiveRefusalNeedsNewReadAndNewAction(CommitStatus status)
        {
            var adapter = Fixture(); var context = Context(); var request = Request(context);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            var pending = new TaskCompletionSource<AnimalUiCommitResult>(); adapter.OnSubmit = (_, __) => pending.Task;
            var submit = adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None);
            var delayed = new TaskCompletionSource<AnimalUiSnapshot>(); adapter.OnRead = _ => delayed.Task;
            var read = adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            pending.SetResult(new AnimalUiCommitResult { Status = status, Message = "TEST_PERMISSION_CHANGED" }); await submit;
            delayed.SetResult(Snapshot(context)); await read;
            var next = request.Clone(); next.ActionId = "TEST_ACTION_2";
            Assert.That((await adapter.SubmitMultiplayerAsync(context, next, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            adapter.OnRead = c => Task.FromResult(Snapshot(c)); await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            request.Loadout.Ground = "Wolf";
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            adapter.OnSubmit = (_, r) => Task.FromResult(Receipt(r));
            Assert.That((await adapter.SubmitMultiplayerAsync(context, next, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Accepted));
        }

        [TestCase("token")][TestCase("context")][TestCase("policy")][TestCase("intent")][TestCase("loadout")][TestCase("null")][TestCase("enum")]
        public async Task MalformedReceiptRemainsUnknownUntilExactOriginalReconciles(string fault)
        {
            var adapter = Fixture(); var context = Context(); var request = Request(context);
            await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None);
            var receipt = Receipt(request);
            if (fault == "token") receipt.AcceptanceToken = " \t";
            if (fault == "context") receipt.AcceptedContextId = "WRONG";
            if (fault == "policy") receipt.AcceptedPolicyRevision = "WRONG";
            if (fault == "intent") receipt.AcceptedEntryIntent = "WRONG";
            if (fault == "loadout") receipt.AcceptedLoadout.Ground = "Wolf";
            if (fault == "enum") receipt.Status = (CommitStatus)999;
            if (fault == "null") receipt = null;
            adapter.OnSubmit = (_, __) => Task.FromResult(receipt);
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            var changed = request.Clone(); changed.ActionId = "TEST_NEW_ACTION";
            Assert.That((await adapter.SubmitMultiplayerAsync(context, changed, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unknown));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            adapter.OnSubmit = (_, r) => Task.FromResult(Receipt(r));
            Assert.That((await adapter.SubmitMultiplayerAsync(context, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Accepted));
        }

        private MultiplayerAuthorityFixture Fixture()
        {
            var adapter = root.AddComponent<MultiplayerAuthorityFixture>(); adapter.Catalog = catalog;
            adapter.OnRead = c => Task.FromResult(Snapshot(c)); adapter.OnSubmit = (_, r) => Task.FromResult(Receipt(r)); return adapter;
        }
        private static MultiplayerSelectionRequest Context() => new MultiplayerSelectionRequest
        {
            ModeId = "TEST_MODE", DisplayName = "TEST ONLY", EntryIntent = "TEST_OPAQUE_INTENT",
            InitialLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" },
            Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                RepresentativeRole = AnimalRole.Air, PolicyRevision = "TEST_POLICY" }
        };
        private AnimalUiSnapshot Snapshot(MultiplayerSelectionRequest c)
        {
            var s = new AnimalUiSnapshot { Revision = "TEST_READ", ContextId = c.ModeId, PolicyRevision = c.Requirements.PolicyRevision, EntryIntent = c.EntryIntent };
            foreach (var a in catalog.Animals)
            {
                bool selected = c.InitialLoadout.Get(a.Role) == a.Id;
                s.Animals.Add(new AnimalProgress { AnimalId = a.Id, Implemented = selected, HasContextPermission = selected, CanUseInContext = selected, Unlocked = false,
                    ActiveLevel = 2, PassiveLevel = 4, ActiveDescription = "TEST ACTIVE", PassiveDescription = "TEST PASSIVE" });
            }
            return s;
        }
        private static AnimalUiReadRequest Read(MultiplayerSelectionRequest c) => new AnimalUiReadRequest { Mode = AnimalUiMode.MultiplayerAnimalSelect, Multiplayer = c };
        private static SelectionCommitRequest Request(MultiplayerSelectionRequest c) => new SelectionCommitRequest
        { ActionId = "TEST_ACTION", ContextId = c.ModeId, SnapshotRevision = "TEST_READ", PolicyRevision = c.Requirements.PolicyRevision, EntryIntent = c.EntryIntent, Loadout = c.InitialLoadout.Clone() };
        // Synthetic receipt ONLY; production adapter never constructs approval from request fields.
        private static AnimalUiCommitResult Receipt(SelectionCommitRequest r) => new AnimalUiCommitResult
        { Status = CommitStatus.Accepted, AcceptanceToken = "TEST_RECEIPT", AcceptedContextId = r.ContextId,
            AcceptedPolicyRevision = r.PolicyRevision, AcceptedEntryIntent = r.EntryIntent, AcceptedLoadout = r.Loadout.Clone() };
    }
    public sealed class MultiplayerAuthorityFixture : AnimalMultiplayerBackendAdapterBase
    {
        public bool ReconciliationAvailable = true;
        protected override bool SupportsRoomEntryReconciliation => ReconciliationAvailable;
        public Func<MultiplayerSelectionRequest, Task<AnimalUiSnapshot>> OnRead;
        public Func<MultiplayerSelectionRequest, SelectionCommitRequest, Task<AnimalUiCommitResult>> OnSubmit;
        public int Calls;
        protected override Task<AnimalUiSnapshot> ReadMultiplayerSnapshotAsync(MultiplayerSelectionRequest c, CancellationToken token) => OnRead(c);
        protected override Task<AnimalUiCommitResult> SubmitRoomEntryAsync(MultiplayerSelectionRequest c, SelectionCommitRequest r, CancellationToken token)
        { Calls++; return OnSubmit(c, r); }
    }
}
