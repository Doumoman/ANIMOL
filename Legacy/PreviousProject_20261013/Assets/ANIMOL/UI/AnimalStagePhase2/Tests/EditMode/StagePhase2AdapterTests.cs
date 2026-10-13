using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalStagePhase2.Editor;
using ANIMOL.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ANIMOL.AnimalStagePhase2.Tests
{
    public sealed class StagePhase2AdapterTests
    {
        private GameObject root;
        private AnimalCatalog art;
        [SetUp] public void Setup()
        {
            root = new GameObject("Stage adapter test only");
            art = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalStagePhase2Builder.CatalogPath);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

        [Test] public async Task ActualHundredStagesNeverGrantOrLaunchAndAllUnsupportedMethodsRemainUnavailable()
        {
            var adapter = root.AddComponent<AnimalStageProjectAdapter>(); adapter.Catalog = art;
            adapter.IdMap = AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(AnimalStagePhase2Builder.MapPath);
            adapter.StageCatalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset");
            var pending = CampaignLaunchContext.Pending;
            Assert.That(adapter.StageCatalog.EnumerateStages().Count(), Is.EqualTo(100));
            foreach (var stage in adapter.StageCatalog.EnumerateStages())
            {
                string before = JsonUtility.ToJson(stage);
                var context = AnimalStageContext.Read(stage, adapter.IdMap, art);
                Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(context), CancellationToken.None));
                Assert.That((await adapter.SubmitCampaignAsync(context, Commit(context), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
                Assert.That(JsonUtility.ToJson(stage), Is.EqualTo(before));
            }
            foreach (var animal in art.Animals)
            {
                Assert.That((await adapter.QuoteUpgradeAsync(animal.Id, UpgradeTrack.Active, CancellationToken.None)).State, Is.EqualTo(UpgradeQuoteState.Unavailable));
                Assert.That((await adapter.TryUpgradeAsync(new UpgradeCommitRequest { AnimalId = animal.Id }, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            }
            Assert.That((await adapter.SubmitMultiplayerAsync(null, null, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(CampaignLaunchContext.Pending, Is.SameAs(pending));
        }

        [Test] public void ArtFixtureCannotReplaceActualCatalogRosterAndVersion()
        {
            var adapter = root.AddComponent<AnimalStageProjectAdapter>(); adapter.Catalog = art;
            adapter.IdMap = AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(AnimalStagePhase2Builder.MapPath);
            adapter.StageCatalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset");
            var fixture = Stage("T01-S01");
            Assert.That(adapter.DescribeContext(fixture), Does.Contain("변경"));
            Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(fixture), CancellationToken.None));
            fixture.StageId = "t01-s01";
            Assert.That(adapter.DescribeContext(fixture), Does.Contain("실제 스테이지 ID"));
        }

        [Test] public async Task BaseAllowsOnlyDesignatedThreeIncludingExplicitTrialAndCopiesRequest()
        {
            var adapter = Fixture(); var stage = Stage("FIXTURE_A");
            var snapshot = await adapter.ReadSnapshotAsync(Read(stage), CancellationToken.None);
            Assert.IsFalse(snapshot.Find("Rabbit").Unlocked);
            Assert.IsFalse(snapshot.Find("Wolf").Implemented);
            Assert.IsTrue(AnimalUiRules.CanConfirmFixedStage(art, snapshot, stage.FixedLoadout, stage, true, true, out _));
            var request = Commit(stage); adapter.MutateReceivedRequest = true;
            Assert.That((await adapter.SubmitCampaignAsync(stage, request, CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.EqualTo(1));
            Assert.That(request.ActionId, Is.EqualTo("FIXTURE_ACTION"));
            Assert.That(request.Loadout.Fingerprint(), Is.EqualTo("Rabbit|DreamFox|Swallow"));
            snapshot.Find("Rabbit").Implemented = false;
            Assert.IsFalse(AnimalUiRules.CanConfirmFixedStage(art, snapshot, stage.FixedLoadout, stage, true, true, out _));
        }

        [Test] public async Task InvalidReadRevokesPreviouslyCachedAuthority()
        {
            var adapter = Fixture(); var stage = Stage("FIXTURE_A");
            await adapter.ReadSnapshotAsync(Read(stage), CancellationToken.None);
            var invalid = Stage("");
            Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(invalid), CancellationToken.None));
            Assert.That((await adapter.SubmitCampaignAsync(stage, Commit(stage), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.Zero);
        }

        [Test] public async Task LateReadCannotReplaceNewContextAuthority()
        {
            var adapter = Fixture(); var a = Stage("FIXTURE_A"); var b = Stage("FIXTURE_B");
            var delayed = new TaskCompletionSource<AnimalUiSnapshot>();
            adapter.ReadHandler = s => s.StageId == a.StageId ? delayed.Task : Task.FromResult(Snapshot(s));
            var old = adapter.ReadSnapshotAsync(Read(a), CancellationToken.None);
            await adapter.ReadSnapshotAsync(Read(b), CancellationToken.None);
            delayed.SetResult(Snapshot(a)); await old;
            Assert.That((await adapter.SubmitCampaignAsync(a, Commit(a), CancellationToken.None)).Status, Is.EqualTo(CommitStatus.Unavailable));
            Assert.That(adapter.Calls, Is.Zero);
            await adapter.SubmitCampaignAsync(b, Commit(b), CancellationToken.None);
            Assert.That(adapter.Calls, Is.EqualTo(1));
        }

        [TestCase("context")][TestCase("policy")][TestCase("revision")]
        public void MissingOrMismatchedSnapshotNeverAuthorizesDispatch(string fault)
        {
            var adapter = Fixture(); var stage = Stage("FIXTURE_A"); var snapshot = Snapshot(stage);
            if (fault == "context") snapshot.ContextId = "OTHER";
            if (fault == "policy") snapshot.PolicyRevision = "OTHER";
            if (fault == "revision") snapshot.Revision = " ";
            adapter.ReadHandler = _ => Task.FromResult(snapshot);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.ReadSnapshotAsync(Read(stage), CancellationToken.None));
            Assert.IsFalse(AnimalUiRules.CanConfirmFixedStage(art, snapshot, stage.FixedLoadout, stage, true, true, out _));
            Assert.That(adapter.Calls, Is.Zero);
        }

        private StageAuthorityFixture Fixture()
        {
            var adapter = root.AddComponent<StageAuthorityFixture>(); adapter.Catalog = art;
            adapter.ReadHandler = s => Task.FromResult(Snapshot(s)); return adapter;
        }
        private static StageSelectionRequest Stage(string id) => new StageSelectionRequest
        {
            StageId = id, Policy = CampaignSelectionPolicy.Fixed,
            FixedLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" },
            Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                RepresentativeRole = AnimalRole.Ground, PolicyRevision = "FIXTURE_POLICY" }
        };
        private AnimalUiSnapshot Snapshot(StageSelectionRequest s)
        {
            var snapshot = new AnimalUiSnapshot { ContextId = s.StageId, PolicyRevision = s.Requirements.PolicyRevision, Revision = "FIXTURE_READ" };
            foreach (var a in art.Animals)
            {
                bool designated = s.FixedLoadout.Get(a.Role) == a.Id;
                snapshot.Animals.Add(new AnimalProgress { AnimalId = a.Id, Implemented = designated,
                    HasContextPermission = designated, CanUseInContext = designated, Unlocked = false });
            }
            return snapshot;
        }
        private static AnimalUiReadRequest Read(StageSelectionRequest s) => new AnimalUiReadRequest { Mode = AnimalUiMode.StageAnimalSelect, Stage = s };
        private static SelectionCommitRequest Commit(StageSelectionRequest s) => new SelectionCommitRequest
        { ActionId = "FIXTURE_ACTION", ContextId = s.StageId, PolicyRevision = s.Requirements.PolicyRevision, SnapshotRevision = "FIXTURE_READ", Loadout = s.FixedLoadout.Clone() };
    }

    // Test-only authority response shape. Never installed in an operational prefab.
    public sealed class StageAuthorityFixture : AnimalStageBackendAdapterBase
    {
        public Func<StageSelectionRequest, Task<AnimalUiSnapshot>> ReadHandler;
        public int Calls;
        public bool MutateReceivedRequest;
        protected override Task<AnimalUiSnapshot> ReadStageSnapshotAsync(StageSelectionRequest context, CancellationToken token) => ReadHandler(context);
        protected override Task<AnimalUiCommitResult> SubmitFixedCampaignAsync(StageSelectionRequest context, SelectionCommitRequest request, CancellationToken token)
        {
            Calls++;
            if (MutateReceivedRequest) { request.ActionId = "CHANGED"; request.Loadout.Ground = "Wolf"; }
            return Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unavailable });
        }
    }
}
