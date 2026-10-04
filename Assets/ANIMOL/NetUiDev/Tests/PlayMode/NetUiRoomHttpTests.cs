#if ANIMOL_NET_UI_DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.MissingUiV1.Project;
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
    public sealed partial class NetUiAdmissionHttpTests
    {
        private readonly List<Tuple<NetUiHttpClient, string>> remoteRooms = new List<Tuple<NetUiHttpClient, string>>();
        private MissingMultiplayerView Room => navigation.GetComponentsInChildren<MissingMultiplayerView>(true).Single(v => v.Screen == MissingMultiplayerScreen.Room);
        private NetUiRoomController InstallRoom()
        {
            var controller = navigation.gameObject.AddComponent<NetUiRoomController>();
            controller.Initialize(service, () => map); controller.SetReturnScreen("MissingUiV1_Create"); return controller;
        }
        private async Task<NetUiEntryResult> RemoteJoin(int account, string code)
        {
            var client = new NetUiHttpClient(endpoint);
            var session = await client.ConnectAsync("TEST_ACCOUNT_" + account, "TEST_KEY_" + account); client.SessionToken = session.SessionToken;
            var context = await client.ReadContextAsync(new NetUiContextRequest { ModeId = "TEST_COMPETITION", EntryIntent = "TEST_JOIN_INTENT", RoomCode = code });
            var request = new NetUiEntryRequest { ActionId = Guid.NewGuid().ToString("N"), ContextId = context.Context.ContextId, SnapshotRevision = context.Snapshot.Revision,
                PolicyRevision = context.Context.PolicyRevision, EntryIntent = context.Context.EntryIntent, RoomCode = code, Loadout = context.Context.InitialLoadout, Options = Array.Empty<NetUiOptionValue>() };
            var submit = (Task<NetUiEntryResult>)typeof(NetUiHttpClient).GetMethod("SubmitJsonAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(client, new object[] { JsonUtility.ToJson(request) });
            var result = await submit;
            if (result.Status == "Accepted") remoteRooms.Add(Tuple.Create(client, result.RoomId));
            return result;
        }
        [UnityTest] public IEnumerator RoomFourPlayersCopyReadyStartAndConfirmedLeaveUseExistingSc06()
        {
            var controller = InstallRoom(); Confirm();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.ReceiptConsumeCount); Assert.AreEqual(1, controller.NavigationCount);
            Room.Copy.onClick.Invoke(); Assert.AreEqual(service.CurrentRoom.RoomCode, GUIUtility.systemCopyBuffer);
            for (int i = 2; i <= 4; i++) { var join = RemoteJoin(i, service.CurrentRoom.RoomCode); yield return Wait(join); Assert.AreEqual("Accepted", join.Result.Status); }
            var fifth = RemoteJoin(5, service.CurrentRoom.RoomCode); yield return Wait(fifth); Assert.AreEqual("Rejected", fifth.Result.Status);
            yield return Until(() => service.CurrentRoom.Participants.Length == 4);
            Assert.AreEqual(4, Room.ParticipantSlots.Length); Assert.IsFalse(Room.Invite.interactable); Assert.IsFalse(Room.StartMatch.interactable);
            foreach (var slot in Room.ParticipantSlots) Assert.IsTrue(slot.GetComponentsInChildren<MissingUtilityText>().Any(t => t.name == "ParticipantState" && t.English.Contains("TEST_ACCOUNT_")));
            Room.Ready.onClick.Invoke(); Room.Ready.onClick.Invoke();
            yield return Until(() => !controller.IsBusy && service.CurrentRoom.Participants.Single(p => p.AccountId == service.AccountId).Ready);
            foreach (var remote in remoteRooms) yield return Wait(remote.Item1.ReadyAsync(remote.Item2, true));
            yield return Until(() => Room.StartMatch.interactable);
            Room.StartMatch.onClick.Invoke(); Room.StartMatch.onClick.Invoke();
            yield return Until(() => !controller.IsBusy && service.CurrentRoom.Phase == "Started");
            Assert.AreEqual("SC06_MatchRoom", navigation.CurrentScreenId);
            Assert.IsTrue(Room.GetComponentsInChildren<MissingUtilityText>().Any(t => t.English == "Start approved · Gameplay connection pending"));
            Assert.IsFalse(Room.Ready.interactable); Assert.IsFalse(Room.StartMatch.interactable);
            Room.Leave.onClick.Invoke(); yield return Until(() => !controller.IsBusy && !service.HasActiveRoomReceipt);
            Assert.AreEqual("MissingUiV1_Create", navigation.CurrentScreenId); Assert.IsFalse(service.IsRoomPolling);
        }
        [UnityTest] public IEnumerator ConsumedRoomRestoresWithoutSecondSubmissionOrConsumption()
        {
            var controller = InstallRoom(); Confirm();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            int consumed = service.ReceiptConsumeCount;
            SceneManager.LoadScene("Lobby"); for (int i = 0; i < 10; i++) yield return null;
            navigation = Object.FindFirstObjectByType<UiNavigationService>();
            Object.Destroy(navigation.GetComponent<NetUiProjectEntry>()); Object.Destroy(navigation.GetComponent<NetUiRoomController>());
            yield return null; while (service.IsRoomOperationInFlight) yield return null;
            host = navigation.GetComponent<AnimalMultiplayerPhase3Entry>().Host; host.Presenter.Catalog = fixtureArt;
            navigation.ScreenChanged += Navigated; controller = InstallRoom();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            Assert.AreEqual(consumed, service.ReceiptConsumeCount); Assert.AreEqual(1, service.SubmissionCount);
            Assert.AreEqual(1, controller.NavigationCount); Assert.IsTrue(service.IsRoomPolling);
            var retry = controller.RestoreAsync(); yield return Wait(retry);
            Assert.AreEqual(1, controller.NavigationCount); Assert.AreEqual(consumed, service.ReceiptConsumeCount);
        }
        [UnityTest] public IEnumerator LostLeaveKeepsReceiptAndReconcilesSavedRoomAfter404()
        {
            var controller = InstallRoom(); Confirm();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            File.WriteAllText(faultPath, "lost-leave");
            var leaving = controller.LeaveSavedAsync(); yield return Wait(leaving); Assert.IsFalse(leaving.Result);
            Assert.IsTrue(service.HasActiveRoomReceipt); Assert.AreEqual("SC06_MatchRoom", navigation.CurrentScreenId);
            var restore = controller.RestoreAsync(); yield return Wait(restore); Assert.IsFalse(restore.Result);
            Assert.IsTrue(service.HasActiveRoomReceipt); Assert.IsFalse(Room.Ready.interactable);
            Assert.IsTrue(navigation.Back()); yield return Until(() => !service.HasActiveRoomReceipt && !controller.IsBusy);
            Assert.AreEqual("MissingUiV1_Create", navigation.CurrentScreenId);
        }
        [UnityTest] public IEnumerator PollFailureDisablesActionsAndReconnectRestoresAuthoritativeState()
        {
            var controller = InstallRoom(); Confirm();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            File.WriteAllText(faultPath, "state-unavailable");
            yield return Until(() => !Room.Ready.interactable);
            Assert.IsTrue(Room.Leave.interactable); Assert.IsTrue(service.HasActiveRoomReceipt);
            File.Delete(faultPath); yield return Until(() => Room.Ready.interactable);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.ReceiptConsumeCount);
            navigation.Navigate("SC05_CompetitiveHub"); Assert.IsFalse(service.IsRoomPolling);
        }
        [UnityTest] public IEnumerator OsBackCannotEscapeUnknownAdmissionBeforeCentralRecovery()
        {
            var controller = InstallRoom(); File.WriteAllText(faultPath, "missing-response"); Confirm();
            yield return Until(() => service.RequestState == "Unknown" && presenter.View.ModalConfirm.interactable);
            Assert.IsTrue(navigation.Back()); Assert.AreEqual(AnimalMultiplayerPhase3Host.ScreenId, navigation.CurrentScreenId);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(0, navigations);
            presenter.View.ModalConfirm.onClick.Invoke();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            Assert.AreEqual(1, service.SubmissionCount); Assert.AreEqual(1, service.ReceiptConsumeCount);
        }
    }
}
#endif
