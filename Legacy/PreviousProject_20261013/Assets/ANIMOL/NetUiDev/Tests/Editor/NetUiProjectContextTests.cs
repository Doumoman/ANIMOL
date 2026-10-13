#if ANIMOL_NET_UI_DEV
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.AnimalUiV2;
using Animol.NetUiDev.Project;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Animol.NetUiDev.Tests
{
    public sealed class NetUiProjectContextTests
    {
        private AnimalCatalog art;
        private FakeSource source;
        private NetUiProjectSettings settings;
        private NetUiIdMapEntry[] map;
        [SetUp] public void Setup()
        {
            art = ScriptableObject.CreateInstance<AnimalCatalog>();
            art.Animals = new List<AnimalDefinition>();
            foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole)))
                art.Animals.Add(new AnimalDefinition { Id = "TEST_ART_" + role, DisplayName = role.ToString(), Role = role });
            map = art.Animals.Select(a => new NetUiIdMapEntry { AnimalId = "TEST_ID_" + a.Role, StableArtId = a.Id, Role = NetUiProjectContextReader.Role(a.Role) }).ToArray();
            source = new FakeSource(map);
            settings = new NetUiProjectSettings { NormalModeId = "TEST_N", RankedModeId = "TEST_R", CustomModeId = "TEST_N",
                Modes = new[] { Policy("TEST_N", "OwnedProgress"), Policy("TEST_R", "RankedMaximumPreset") } };
        }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(art);
        private static NetUiProjectMode Policy(string id, string growth) => new NetUiProjectMode {
            ModeId = id, GrowthPolicy = growth, PublicIntent = id + "_opaque_p", CreateIntent = id + "_opaque_c", JoinIntent = id + "_opaque_j" };
        private NetUiProjectContextReader Reader() => new NetUiProjectContextReader(source, settings, art, map);

        [TestCase("Normal", "TEST_N", "OwnedProgress", "p")]
        [TestCase("Ranked", "TEST_R", "RankedMaximumPreset", "p")]
        [TestCase("Create", "TEST_N", "OwnedProgress", "c")]
        public async Task RoutesKeepModeGrowthAndOpaqueIntent(string route, string mode, string growth, string intent)
        {
            var reply = await Reader().ReadAsync(route);
            Assert.AreEqual(mode, reply.Request.ModeId);
            Assert.AreEqual(mode + "_opaque_" + intent, reply.Request.EntryIntent);
            Assert.AreEqual(growth, reply.GrowthPolicy);
            Assert.AreEqual("TEST_ART_Special", reply.Request.InitialLoadout.Special);
            Assert.AreEqual(AnimalRole.Special, reply.Request.Requirements.RepresentativeRole);
            Assert.IsTrue(reply.Snapshot.Animals.All(a => a.ActiveLevel == -1 && a.PassiveLevel == -1));
            CollectionAssert.AreEqual(new[] { "catalog", "context" }, source.Calls);
        }
        [Test] public async Task JoinLooksUpBeforeContextAndUsesRoomsModeInsteadOfCustomMode()
        {
            var reply = await Reader().ReadAsync("Join", "ABC123");
            CollectionAssert.AreEqual(new[] { "catalog", "lookup", "context" }, source.Calls);
            Assert.AreEqual("TEST_R", reply.Request.ModeId);
            Assert.AreEqual("TEST_R_opaque_j", reply.Request.EntryIntent);
            Assert.AreEqual("ABC123", reply.RoomCode);
            Assert.AreEqual("RankedMaximumPreset", reply.GrowthPolicy);
        }
        [TestCase("full")][TestCase("started")][TestCase("stale")][TestCase("missing")]
        public void InvalidLookupNeverReadsContext(string fault)
        {
            if (fault == "full") source.Lookup.ParticipantCount = 4;
            if (fault == "started") source.Lookup.Phase = "Started";
            if (fault == "stale") source.Lookup.PolicyRevision = "old";
            if (fault == "missing") source.Lookup.RoomId = "";
            Assert.ThrowsAsync<InvalidOperationException>(() => Reader().ReadAsync("Join", "ABC123"));
            CollectionAssert.DoesNotContain(source.Calls, "context");
        }
        [TestCase("intent")][TestCase("growth")][TestCase("collision")][TestCase("missing")]
        public void MissingProjectPolicyNeverReadsContext(string fault)
        {
            if (fault == "intent") settings.Modes[0].PublicIntent = "public";
            if (fault == "growth") settings.Modes[0].GrowthPolicy = "RankedMaximumPreset";
            if (fault == "collision") settings.RankedModeId = settings.NormalModeId;
            if (fault == "missing") settings.NormalModeId = "";
            Assert.ThrowsAsync<InvalidOperationException>(() => Reader().ReadAsync("Normal"));
            CollectionAssert.DoesNotContain(source.Calls, "context");
        }
        [TestCase("permission")][TestCase("implemented")][TestCase("role")][TestCase("revision")][TestCase("emptyPool")][TestCase("modePool")]
        public void IncompleteContextNeverReturnsUsableRequest(string fault)
        {
            source.Fault = fault;
            Assert.ThrowsAsync<InvalidOperationException>(() => Reader().ReadAsync("Normal"));
        }
        [Test] public async Task NullPoolRemainsNullAndExplicitPoolIsMapped()
        {
            var reader = Reader();
            Assert.IsNull((await reader.ReadAsync("Normal")).Request.AllowedAnimalIds);
            source.Pool = map.Select(m => m.AnimalId).ToArray();
            CollectionAssert.AreEquivalent(map.Select(m => m.StableArtId), (await reader.ReadAsync("Normal")).Request.AllowedAnimalIds);
        }
        [Test] public void PendingActionBlocksAllReads()
        {
            source.BlocksNewEntry = true;
            Assert.ThrowsAsync<InvalidOperationException>(() => Reader().ReadAsync("Normal"));
            Assert.IsEmpty(source.Calls);
        }
        [Test] public async Task OverlappingReadsAreRejectedAndSettingsAreCaptured()
        {
            var reader = Reader();
            settings.NormalModeId = "mutated";
            source.Gate = new TaskCompletionSource<bool>();
            var first = reader.ReadAsync("Normal");
            Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync("Ranked"));
            source.Gate.SetResult(true);
            Assert.AreEqual("TEST_N", (await first).Request.ModeId);
        }
        [Test] public void ProductionMapContainsOnlyRabbitAndCannotUseTestFixture()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalCatalog.asset");
            var idMap = AssetDatabase.LoadAssetAtPath<AnimalMultiplayerIdMap>("Assets/ANIMOL/UI/AnimalMultiplayerPhase3/AnimalMultiplayerIdMap.asset");
            Assert.NotNull(catalog); Assert.NotNull(idMap);
            var actual = NetUiProjectContextReader.BuildProjectMap(catalog, idMap);
            Assert.AreEqual(1, actual.Length); Assert.AreEqual("RABBIT", actual[0].AnimalId);
            Assert.AreEqual("Ground", actual[0].Role);
            var reader = new NetUiProjectContextReader(source, settings, catalog, actual);
            Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync("Normal"));
        }
        private sealed class FakeSource : INetUiContextSource
        {
            public bool BlocksNewEntry { get; set; }
            public readonly List<string> Calls = new List<string>();
            public string Fault;
            public string[] Pool;
            public TaskCompletionSource<bool> Gate;
            private readonly NetUiCatalog catalog;
            public NetUiLookupResponse Lookup = new NetUiLookupResponse { RoomId = "TEST_ROOM", RoomCode = "ABC123", ModeId = "TEST_R", PolicyRevision = "r1", Phase = "Waiting", Capacity = 4, ParticipantCount = 1 };
            public FakeSource(NetUiIdMapEntry[] map)
            {
                catalog = new NetUiCatalog { Animals = map.Select(m => new NetUiAnimalDefinition { AnimalId = m.AnimalId, StableArtId = m.StableArtId, Role = m.Role, Implemented = true }).ToArray(),
                    CodeFormat = new NetUiCodeFormat { Length = 6, Alphabet = "ABC123" },
                    Modes = new[] { Mode("TEST_N"), Mode("TEST_R") } };
            }
            private static NetUiModeDefinition Mode(string id) => new NetUiModeDefinition { ModeId = id, DisplayName = id + " label", PolicyRevision = "r1", RepresentativeRole = "Special", PublicStartCount = 4, CustomStartMinimum = 2,
                EntryIntents = new[] { new NetUiIntentDefinition { Kind = "public", IntentId = id + "_opaque_p" }, new NetUiIntentDefinition { Kind = "create", IntentId = id + "_opaque_c" }, new NetUiIntentDefinition { Kind = "join", IntentId = id + "_opaque_j" } } };
            public async Task<NetUiCatalog> GetCatalogAsync()
            {
                Calls.Add("catalog"); if (Gate != null) await Gate.Task;
                if (Fault == "modePool") catalog.Modes[0].AllowedAnimalIds = new[] { "TEST_ID_Ground" };
                return catalog;
            }
            public Task<NetUiLookupResponse> LookupRoomAsync(string code) { Calls.Add("lookup"); return Task.FromResult(Lookup); }
            public Task<NetUiContextSnapshot> ReadContextAsync(string mode, string intent, string code)
            {
                Calls.Add("context");
                return Task.FromResult(new NetUiContextSnapshot { Context = new NetUiContext { ContextId = mode, ModeId = mode, EntryIntent = intent, PolicyRevision = "r1", RoomCode = code,
                    RepresentativeRole = Fault == "role" ? "Air" : "Special", AllowedAnimalIds = Fault == "emptyPool" ? Array.Empty<string>() : Pool,
                    InitialLoadout = new NetUiLoadout { Ground = "TEST_ID_Ground", Special = "TEST_ID_Special", Air = "TEST_ID_Air" } },
                    Snapshot = new NetUiSnapshot { Revision = Fault == "revision" ? "" : "snapshot1", Animals = catalog.Animals.Select(a => new NetUiAnimalPermission { AnimalId = a.AnimalId, Implemented = Fault != "implemented", Unlocked = false, HasContextPermission = true, CanUseInContext = Fault != "permission" }).ToArray() } });
            }
        }
    }
}
#endif
