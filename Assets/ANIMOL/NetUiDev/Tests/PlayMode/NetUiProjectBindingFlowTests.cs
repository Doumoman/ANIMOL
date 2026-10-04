#if ANIMOL_NET_UI_DEV
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.MissingUiV1.Project.Multiplayer;
using ANIMOL.UI;
using Animol.NetUiDev.Project;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Animol.NetUiDev.Tests
{
    public sealed class NetUiProjectBindingFlowTests
    {
        private UiNavigationService nav;
        private NetUiProjectEntry binding;
        private MissingMultiplayerView join;
        private GameObject service;
        private Source source;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.IsNull(NetUiProjectSettings.ReadLocal(), "These isolated UI tests require no local DEV connection settings.");
            SceneManager.LoadScene("Lobby");
            for (int i = 0; i < 10; i++) yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            var host = nav.GetComponent<AnimalMultiplayerPhase3Entry>().Host;
            Assert.IsNull(NetUiDevCoordinator.Instance);
            service = new GameObject("NET02 test service"); service.AddComponent<NetUiDevCoordinator>();
            Assert.IsFalse(NetUiDevCoordinator.Instance.BlocksNewEntry, "Existing unresolved DEV journal must be resolved outside tests.");
            var config = new NetUiProjectSettings { NormalModeId = "TEST_MODE", CustomModeId = "TEST_MODE", Modes = new[] {
                new NetUiProjectMode { ModeId = "TEST_MODE", GrowthPolicy = "OwnedProgress", PublicIntent = "TEST_PUBLIC", CreateIntent = "TEST_CREATE", JoinIntent = "TEST_JOIN" } } };
            var map = new[] {
                new NetUiIdMapEntry { AnimalId = "TEST_G", StableArtId = "Rabbit", Role = "Ground" },
                new NetUiIdMapEntry { AnimalId = "TEST_S", StableArtId = "DreamFox", Role = "Special" },
                new NetUiIdMapEntry { AnimalId = "TEST_A", StableArtId = "Swallow", Role = "Air" } };
            source = new Source(map);
            binding = nav.gameObject.AddComponent<NetUiProjectEntry>();
            Set("settings", config); Set("connected", true);
            Set("reader", new NetUiProjectContextReader(source, config, host.Presenter.Catalog, map));
            for (int i = 0; i < 8; i++) yield return null;
            join = nav.GetComponentsInChildren<MissingMultiplayerView>(true).Single(v => v.Screen == MissingMultiplayerScreen.Join);
            nav.Navigate(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Join));
            yield return null;
        }
        private void Set(string field, object value) => typeof(NetUiProjectEntry).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(binding, value);
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (binding != null) Object.Destroy(binding);
            if (service != null) Object.Destroy(service);
            yield return null;
        }
        [UnityTest] public IEnumerator InputRequiresLookupAndJoinRequeriesBeforeOpeningExistingHost()
        {
            join.Code.Field.text = "ABC123";
            Assert.IsFalse(join.Entry.interactable); Assert.AreEqual(0, source.Lookups);
            join.Lookup.onClick.Invoke(); yield return null;
            Assert.AreEqual(1, source.Lookups); Assert.IsTrue(join.Entry.interactable);
            Assert.AreEqual(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Join), nav.CurrentScreenId);
            join.Entry.onClick.Invoke(); yield return null;
            // TASK 04 also reads fresh authority when the Phase3 presenter opens.
            Assert.AreEqual(3, source.Lookups); Assert.AreEqual(3, source.Contexts);
            Assert.AreEqual(AnimalMultiplayerPhase3Host.ScreenId, nav.CurrentScreenId);
            Assert.AreEqual("TEST_MODE", nav.GetComponent<AnimalMultiplayerPhase3Entry>().Host.Context.ModeId);
            Assert.AreEqual("Unconfigured", NetUiDevCoordinator.Instance.RequestState);
        }
        [UnityTest] public IEnumerator EditingCodeDiscardsLateLookupAndDoesNotEnableJoin()
        {
            source.Gate = new TaskCompletionSource<bool>();
            join.Code.Field.text = "ABC123"; join.Lookup.onClick.Invoke();
            join.Code.Field.text = "ABC124"; source.Gate.SetResult(true);
            yield return null; yield return null;
            Assert.IsNull(binding.CurrentContext); Assert.IsFalse(join.Entry.interactable);
            Assert.AreEqual(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Join), nav.CurrentScreenId);
        }
        [UnityTest] public IEnumerator BackDiscardsLateLookupAndRemovesBindingsOnDestroy()
        {
            source.Gate = new TaskCompletionSource<bool>();
            join.Code.Field.text = "ABC123"; join.Lookup.onClick.Invoke();
            nav.Navigate(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Custom));
            source.Gate.SetResult(true); yield return null; yield return null;
            Assert.IsNull(binding.CurrentContext); Assert.IsFalse(join.Entry.interactable);
            Object.Destroy(binding); yield return null;
            Assert.IsNull(join.DevContextRequested); Assert.IsFalse(join.Lookup.interactable);
        }
        private sealed class Source : INetUiContextSource
        {
            public bool BlocksNewEntry => false;
            public int Lookups, Contexts;
            public TaskCompletionSource<bool> Gate;
            private readonly NetUiCatalog catalog;
            public Source(NetUiIdMapEntry[] map)
            {
                catalog = new NetUiCatalog { CodeFormat = new NetUiCodeFormat { Length = 6, Alphabet = "ABC1234" },
                    Animals = map.Select(m => new NetUiAnimalDefinition { AnimalId = m.AnimalId, StableArtId = m.StableArtId, Role = m.Role, Implemented = true }).ToArray(),
                    Modes = new[] { new NetUiModeDefinition { ModeId = "TEST_MODE", DisplayName = "TEST fixture only", PolicyRevision = "TEST_POLICY", RepresentativeRole = "Ground",
                        EntryIntents = new[] { new NetUiIntentDefinition { IntentId = "TEST_JOIN", Kind = "join" } } } } };
            }
            public Task<NetUiCatalog> GetCatalogAsync() => Task.FromResult(catalog);
            public async Task<NetUiLookupResponse> LookupRoomAsync(string code)
            {
                Lookups++; if (Gate != null) await Gate.Task;
                return new NetUiLookupResponse { RoomId = "TEST_ROOM", RoomCode = code, ModeId = "TEST_MODE", PolicyRevision = "TEST_POLICY", Phase = "Waiting", Capacity = 4, ParticipantCount = 1 };
            }
            public Task<NetUiContextSnapshot> ReadContextAsync(string mode, string intent, string code)
            {
                Contexts++;
                return Task.FromResult(new NetUiContextSnapshot { Context = new NetUiContext { ContextId = mode, ModeId = mode, EntryIntent = intent, PolicyRevision = "TEST_POLICY", RepresentativeRole = "Ground", RoomCode = code,
                    InitialLoadout = new NetUiLoadout { Ground = "TEST_G", Special = "TEST_S", Air = "TEST_A" } },
                    Snapshot = new NetUiSnapshot { Revision = "TEST_REV", Animals = catalog.Animals.Select(a => new NetUiAnimalPermission { AnimalId = a.AnimalId, Implemented = true, HasContextPermission = true, CanUseInContext = true }).ToArray() } });
            }
        }
    }
}
#endif
