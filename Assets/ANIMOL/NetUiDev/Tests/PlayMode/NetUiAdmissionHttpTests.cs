#if ANIMOL_NET_UI_DEV
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.AnimalUiV2;
using ANIMOL.UI;
using Animol.NetUiDev.Project;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Animol.NetUiDev.Tests
{
    public sealed partial class NetUiAdmissionHttpTests
    {
        private NetUiDevCoordinator service;
        private AnimalMultiplayerProjectAdapter adapter;
        private AnimalMultiplayerPhase3Host host;
        private AnimalUiPresenter presenter;
        private AnimalCatalog fixtureArt;
        private NetUiIdMapEntry[] map;
        private NetUiProjectContextReader reader;
        private NetUiBoundContext bound;
        private string endpoint, configPath, configBefore, faultPath;
        private int accepted, navigations;
        private UiNavigationService navigation;
        [Serializable] private sealed class Counters
        {
            public string Test, Scope = "Isolated real HTTP fixture; no production IDs", State, ActionTagSha256;
            public int Submissions, ResultQueries, ReceiptNotifications, AcceptedPresentations, ReceiptConsumptions, WaitingRoomNavigations;
        }

        private static IEnumerator Wait(Task task)
        {
            float until = Time.realtimeSinceStartup + 30;
            while (!task.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(task.IsCompleted, "HTTP fixture operation timed out");
            if (task.IsFaulted) throw task.Exception.GetBaseException();
            Assert.IsFalse(task.IsCanceled);
        }
        private static IEnumerator Until(Func<bool> condition)
        {
            float until = Time.realtimeSinceStartup + 30;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(condition(), "Expected UI state was not reached");
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            accepted = 0; navigations = 0;
            endpoint = Environment.GetEnvironmentVariable("ANIMOL_NET02_TEST_URL");
            if (string.IsNullOrEmpty(endpoint)) Assert.Ignore("Run Tools/ANIMOLNet02/Tools/run_unity_admission_tests.py for isolated HTTP tests.");
            Assert.IsTrue(new Uri(endpoint).IsLoopback);
            Assert.IsTrue(NetUiDevCoordinator.ResolveDevClientSlot().StartsWith("test_"), "Use an isolated journal slot");
            Assert.IsNull(NetUiProjectSettings.ReadLocal(), "Do not connect user DEV settings in fixture tests");
            configPath = Environment.GetEnvironmentVariable("ANIMOL_NET02_TEST_CONFIG");
            configBefore = File.ReadAllText(configPath); faultPath = Environment.GetEnvironmentVariable("ANIMOL_NET02_TEST_FAULT");
            SceneManager.LoadScene("Lobby"); for (int i = 0; i < 10; i++) yield return null;
            navigation = Object.FindFirstObjectByType<UiNavigationService>();
            host = navigation.GetComponent<AnimalMultiplayerPhase3Entry>().Host; presenter = host.Presenter;
            adapter = (AnimalMultiplayerProjectAdapter)presenter.Backend;
            // Clone art IN MEMORY only. Real IdMap and catalog assets remain untouched.
            fixtureArt = ScriptableObject.CreateInstance<AnimalCatalog>();
            foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole)))
            {
                var original = presenter.Catalog.Animals.First(a => a.Role == role);
                fixtureArt.Animals.Add(new AnimalDefinition { Id = "TEST_ART_" + role, Role = role, DisplayName = "TEST " + role, Portrait = original.Portrait });
            }
            presenter.Catalog = fixtureArt; adapter.Catalog = fixtureArt;
            map = fixtureArt.Animals.Select(a => new NetUiIdMapEntry { AnimalId = "TEST_" + a.Role.ToString().ToUpperInvariant(), StableArtId = a.Id, Role = a.Role.ToString() }).ToArray();
            Assert.IsNull(NetUiDevCoordinator.Instance);
            service = new GameObject("NET02 isolated HTTP service").AddComponent<NetUiDevCoordinator>();
            service.ConfigureConnection(endpoint, "TEST_ACCOUNT_1", map);
            yield return Wait(service.ConnectAsync("TEST_KEY_1"));
            var settings = new NetUiProjectSettings { CustomModeId = "TEST_COMPETITION", Modes = new[] { new NetUiProjectMode {
                ModeId = "TEST_COMPETITION", GrowthPolicy = "OwnedProgress", CreateIntent = "TEST_CREATE_INTENT" } } };
            reader = new NetUiProjectContextReader(new NetUiCoordinatorContextSource(service), settings, fixtureArt, map);
            var reading = reader.ReadAsync("Create"); yield return Wait(reading); bound = reading.Result;
            adapter.DevAuthority = new NetUiPhase3Authority(service, reader, bound);
            presenter.MultiplayerAccepted += Presented; navigation.ScreenChanged += Navigated;
            navigation.Navigate("SC05_CompetitiveHub"); Assert.IsTrue(host.Open(bound.Request));
            yield return Until(() => presenter.View.Primary.interactable);
        }
        private void Presented(AnimalUiCommitResult result) { accepted++; service.RecordAcceptedPresentation(); }
        private void Navigated(string screen) { if (screen == "SC06_MatchRoom") navigations++; }
        private void Confirm() { presenter.View.Primary.onClick.Invoke(); presenter.View.ModalConfirm.onClick.Invoke(); }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (service != null)
            {
                string output = Path.Combine(Application.dataPath, "../Logs/NET02Task" + (Environment.GetEnvironmentVariable("ANIMOL_NET02_TEST_STAGE") ?? "04")); Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, TestContext.CurrentContext.Test.Name + ".json"), JsonUtility.ToJson(new Counters {
                    Test = TestContext.CurrentContext.Test.Name, State = service.RequestState, Submissions = service.SubmissionCount,
                    ActionTagSha256 = ActionTag(service.StoredRequest?.ActionId),
                    ResultQueries = service.ResultQueryCount, ReceiptNotifications = service.ReceiptNotificationCount,
                    AcceptedPresentations = service.AcceptedPresentationCount, ReceiptConsumptions = service.ReceiptConsumeCount,
                    WaitingRoomNavigations = navigations }, true));
            }
            if (configBefore != null) File.WriteAllText(configPath, configBefore);
            if (faultPath != null && File.Exists(faultPath)) File.Delete(faultPath);
            if (presenter != null) presenter.MultiplayerAccepted -= Presented;
            if (navigation != null) navigation.ScreenChanged -= Navigated;
            if (navigation != null && navigation.GetComponent<NetUiRoomController>() != null) Object.Destroy(navigation.GetComponent<NetUiRoomController>());
            yield return null;
            if (service != null) { service.StopRoomPolling(); while (service.IsRoomOperationInFlight) yield return null; }
            foreach (var remote in remoteRooms) yield return Wait(remote.Item1.LeaveAsync(remote.Item2));
            remoteRooms.Clear();
            if (service != null && service.BlocksNewEntry)
            {
                yield return Wait(service.GetCatalogAsync()); service.BindRecoveryProjectMap(map);
                yield return Wait(service.ResolvePendingAsync());
                if (service.HasActiveRoomReceipt)
                {
                    yield return Wait(service.PollRoomAsync()); service.TryDeliverAccepted((receipt, loadout) => true);
                    yield return Wait(service.LeaveRoomAsync());
                }
            }
            if (service != null) Object.Destroy(service.gameObject);
            if (fixtureArt != null) Object.Destroy(fixtureArt);
            yield return null;
        }
        private static string ActionTag(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return "";
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(actionId))).Replace("-", "").Substring(0, 12);
        }
        [UnityTest] public IEnumerator CancelZeroConfirmOnceAndSingleCentralConsumption()
        {
            presenter.View.Primary.onClick.Invoke(); presenter.View.ModalCancel.onClick.Invoke();
            Assert.AreEqual(0, service.SubmissionCount);
            Confirm(); string draft = presenter.Draft.Fingerprint();
            for (int i = 0; i < 5; i++) { presenter.View.ModalConfirm.onClick.Invoke(); presenter.Back(); presenter.View.RoleTabs[2].onClick.Invoke(); }
            Assert.AreEqual(draft, presenter.Draft.Fingerprint());
            yield return Until(() => accepted == 1);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.AcceptedPresentationCount);
            Assert.AreEqual(0, navigations); Assert.IsTrue(service.BlocksNewEntry);
            var stored = service.StoredRequest; Assert.AreEqual("TEST_COMPETITION", stored.ContextId);
            Assert.AreEqual("TEST_GROUND", stored.Loadout.Ground);
            Assert.Throws<InvalidOperationException>(() => service.ConfigureConnection(endpoint, "TEST_ACCOUNT_2", map));
            Assert.Throws<InvalidOperationException>(() => service.ConfigureConnection("http://127.0.0.1:1", "TEST_ACCOUNT_1", map));
            Assert.AreEqual(endpoint, service.ServerUrl); Assert.AreEqual("TEST_ACCOUNT_1", service.AccountId);
            yield return Wait(service.PollRoomAsync()); int deliveries = 0;
            Assert.IsTrue(service.TryDeliverAccepted((receipt, art) => { deliveries++; Assert.AreNotEqual(receipt.RoomId, receipt.AcceptedContextId); Assert.AreEqual(draft, new AnimalLoadout { Ground = art.Ground, Special = art.Special, Air = art.Air }.Fingerprint()); return true; }));
            Assert.IsFalse(service.TryDeliverAccepted((receipt, art) => { deliveries++; return true; }));
            Assert.AreEqual(1, deliveries); Assert.AreEqual(1, service.ReceiptConsumeCount);
            Assert.AreEqual(1, service.ReceiptNotificationCount);
        }
        [UnityTest] public IEnumerator LostResponseReconcilesSameDurablePayloadWithoutResubmit()
        {
            File.WriteAllText(faultPath, "missing-response"); Confirm();
            yield return Until(() => service.RequestState == "Unknown" && presenter.View.ModalConfirm.interactable);
            string original = JsonUtility.ToJson(service.StoredRequest);
            Assert.IsTrue(presenter.IsCommitting); Assert.AreEqual(0, accepted);
            presenter.View.ModalConfirm.onClick.Invoke();
            yield return Until(() => accepted == 1);
            Assert.AreEqual(original, JsonUtility.ToJson(service.StoredRequest));
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.ResultQueryCount); Assert.AreEqual(0, navigations);
        }
        [UnityTest] public IEnumerator TamperedReceiptStaysUnknownUntilOriginalServerReceipt()
        {
            File.WriteAllText(faultPath, "tamper-receipt"); Confirm();
            yield return Until(() => service.RequestState == "Unknown" && presenter.View.ModalConfirm.interactable);
            Assert.AreEqual(0, accepted); Assert.AreEqual(0, service.ReceiptNotificationCount);
            presenter.View.ModalConfirm.onClick.Invoke(); yield return Until(() => accepted == 1);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.ResultQueryCount);
        }
        [UnityTest] public IEnumerator ServerPolicyChangeRejectsAndRequiresManualRefreshAndNewAction()
        {
            File.WriteAllText(configPath, configBefore.Replace("TEST_POLICY_1", "TEST_POLICY_2")); Confirm();
            yield return Until(() => service.RequestState == "Rejected" && !presenter.IsCommitting);
            string refused = service.StoredRequest.ActionId;
            Assert.AreEqual(0, accepted); Assert.IsFalse(presenter.View.Primary.interactable);
            presenter.View.Primary.onClick.Invoke(); Assert.AreEqual(1, service.SubmissionCount);
            File.WriteAllText(configPath, configBefore); presenter.Refresh();
            yield return Until(() => presenter.View.Primary.interactable);
            Confirm(); yield return Until(() => accepted == 1);
            Assert.AreNotEqual(refused, service.StoredRequest.ActionId); Assert.AreEqual(2, service.SubmissionCount);
        }
        [UnityTest] public IEnumerator CoordinatorRestartRestoresUnknownAndRejectsAccountSwitch()
        {
            File.WriteAllText(faultPath, "missing-response"); Confirm();
            yield return Until(() => service.RequestState == "Unknown");
            string original = JsonUtility.ToJson(service.StoredRequest);
            Object.Destroy(service.gameObject); yield return null;
            SceneManager.LoadScene("Lobby"); for (int i = 0; i < 10; i++) yield return null;
            service = NetUiDevCoordinator.Instance; Assert.NotNull(service);
            Assert.AreEqual("Unknown", service.RequestState); Assert.AreEqual(original, JsonUtility.ToJson(service.StoredRequest));
            Assert.Throws<InvalidOperationException>(() => service.ConfigureConnection(endpoint, "TEST_ACCOUNT_2", map));
            Assert.AreEqual("TEST_ACCOUNT_1", service.AccountId);
            navigation = Object.FindFirstObjectByType<UiNavigationService>();
            navigation.Navigate("SC05_CompetitiveHub");
            var hub = navigation.GetComponentsInChildren<ANIMOL.MissingUiV1.Project.Multiplayer.MissingMultiplayerView>(true)
                .Single(v => v.Screen == ANIMOL.MissingUiV1.Project.Multiplayer.MissingMultiplayerScreen.Hub);
            hub.NormalBrowse.onClick.Invoke(); yield return Until(() => service.HasActiveRoomReceipt);
            Assert.AreEqual("SC05_CompetitiveHub", navigation.CurrentScreenId);
            Assert.IsTrue(service.HasActiveRoomReceipt); Assert.AreEqual(0, service.SubmissionCount); Assert.AreEqual(1, service.ResultQueryCount);
            Assert.AreEqual(original, JsonUtility.ToJson(service.StoredRequest)); Assert.AreEqual(0, navigations);
        }
    }
}
#endif
