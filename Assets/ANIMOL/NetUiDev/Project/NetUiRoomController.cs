#if ANIMOL_NET_UI_DEV
using System;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.MissingUiV1.Project;
using ANIMOL.MissingUiV1.Project.Multiplayer;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.NetUiDev.Project
{
    // One consumer of the coordinator receipt. Does not submit admissions or run a second poll loop.
    public sealed class NetUiRoomController : MonoBehaviour
    {
        private NetUiDevCoordinator service;
        private UiNavigationService navigation;
        private MissingMultiplayerView view;
        private AnimalMultiplayerPhase3Host host;
        private Func<NetUiIdMapEntry[]> map;
        private Func<bool> previousBack;
        private bool initialized, busy, restoreRequested, connected;
        private int generation;
        private string boundRoom, returnScreen = "SC05_CompetitiveHub";
        public int NavigationCount { get; private set; }
        public bool IsBusy => busy;

        public void Initialize(NetUiDevCoordinator coordinator, Func<NetUiIdMapEntry[]> projectMap)
        {
            if (initialized) return;
            service = coordinator; map = projectMap; navigation = GetComponent<UiNavigationService>();
            view = navigation.GetComponentsInChildren<MissingMultiplayerView>(true).Single(v => v.Screen == MissingMultiplayerScreen.Room);
            host = navigation.GetComponent<AnimalMultiplayerPhase3Entry>().Host;
            previousBack = navigation.BackOverride; navigation.BackOverride = Back;
            service.AcceptedReceiptReady += Receipt;
            service.RoomStateChanged += RoomChanged;
            service.OperationStateChanged += Operation;
            navigation.ScreenChanged += ScreenChanged;
            initialized = true; restoreRequested = service.HasActiveRoomReceipt;
        }
        public void SetReturnScreen(string screen)
        { if (screen != AnimalMultiplayerPhase3Host.ScreenId && screen != "SC06_MatchRoom") returnScreen = screen; }
        private void Receipt() { restoreRequested = true; }
        private async void Update()
        {
            if (!initialized || busy || !restoreRequested || !service.HasActiveRoomReceipt) return;
            // Let the existing presenter finish displaying the validated result before navigation.
            if (host.Presenter.isActiveAndEnabled && host.Presenter.IsCommitting) return;
            restoreRequested = false;
            await RestoreAsync();
        }
        public async Task<bool> RestoreAsync()
        {
            if (!initialized || busy || !service.HasActiveRoomReceipt) return false;
            busy = true; connected = false; int version = generation;
            service.StopRoomPolling();
            try
            {
                while (service.IsRoomOperationInFlight) await Task.Yield();
                await service.GetCatalogAsync();
                if (!Alive(version)) return false;
                service.BindRecoveryProjectMap(map());
                var state = await service.PollRoomAsync();
                if (!Alive(version)) return false;
                bool delivered = service.RequiresReceiptDelivery
                    ? service.TryDeliverAccepted((receipt, loadout) => BindAndNavigate(state))
                    : BindAndNavigate(state); // Restoring consumed membership is not another receipt transaction.
                if (delivered) { connected = true; restoreRequested = false; service.BeginRoomPolling(); }
                return delivered;
            }
            catch (Exception)
            {
                if (Alive(version)) ShowRecoveryFailure();
                return false;
            }
            finally { if (this != null) { busy = false; RenderButtons(); } }
        }
        private bool Alive(int version) => this != null && initialized && service != null && generation == version;
        private bool BindAndNavigate(NetUiRoomState state)
        {
            if (!service.HasActiveRoomReceipt || state == null) return false;
            boundRoom = state.RoomId; connected = true; view.DevRoomRequested = Command;
            foreach (var item in navigation.GetComponentsInChildren<MissingMultiplayerView>(true))
                if (item.DevLeaveSavedEntry) { item.DevLeaveSavedEntry = false; Label(item.Entry, "동물 선택", "Select animals"); }
            Render(state);
            if (navigation.CurrentScreenId == "SC06_MatchRoom") return true;
            bool success = navigation.Navigate("SC06_MatchRoom", false);
            if (success) NavigationCount++;
            return success;
        }
        private void ScreenChanged(string screen)
        {
            // A receipt delivery intentionally changes screen; a user leaving it invalidates old reads.
            if (screen != "SC06_MatchRoom") { generation++; restoreRequested = false; service.StopRoomPolling(); }
            else if (!busy && service.HasActiveRoomReceipt && boundRoom != null)
            { connected = false; restoreRequested = true; RenderButtons(); }
        }
        private bool Back()
        {
            if (navigation.CurrentScreenId == AnimalMultiplayerPhase3Host.ScreenId && host.Presenter.IsCommitting) return true;
            if (navigation.CurrentScreenId != "SC06_MatchRoom" || !service.HasActiveRoomReceipt)
                return previousBack?.Invoke() ?? false;
            Command(MissingMultiplayerRoomAction.Leave); return true;
        }
        private void RoomChanged(NetUiRoomState state)
        {
            if (boundRoom == null || state.RoomId != boundRoom || navigation.CurrentScreenId != "SC06_MatchRoom") return;
            connected = true; Render(state);
        }
        private void Operation(string state, string reason)
        {
            if (state != "RoomUnknown" || navigation.CurrentScreenId != "SC06_MatchRoom") return;
            connected = false; Words(view.transform, "RoomUnavailable", "연결 확인 불가 · 다시 조회 중\n나가기로 이전 방 탈퇴를 다시 확인할 수 있습니다.", "Connection unavailable · Retrying query\nLeave can reconcile the saved room membership.");
            RenderButtons();
        }
        private void ShowRecoveryFailure()
        {
            connected = false;
            // Do not fabricate a room or clear the receipt on 404. Explicit leave uses its saved RoomId.
            foreach (var item in navigation.GetComponentsInChildren<MissingMultiplayerView>(true))
            {
                Words(item.transform, "PolicyUnavailable", "승인된 방 상태를 확인할 수 없습니다. 결과를 다시 확인하거나 이전 방에서 나가세요.", "Approved room unavailable. Retry recovery or leave the saved room.");
                if (item.Screen == MissingMultiplayerScreen.Create || item.Screen == MissingMultiplayerScreen.Join)
                { item.DevLeaveSavedEntry = true; item.Entry.interactable = true; Label(item.Entry, "이전 방 나가기", "Leave previous room"); }
            }
            Words(view.transform, "RoomUnavailable", "방 상태 확인 불가 · 나가기를 다시 시도할 수 있습니다.", "Room state unavailable · Leave may be retried.");
        }
        public async Task<bool> LeaveSavedAsync()
        {
            if (busy || !service.HasActiveRoomReceipt) return false;
            // Leaving a server-approved RoomId is also valid before successful UI delivery.
            busy = true; int version = generation; service.StopRoomPolling(); RenderButtons();
            try
            {
                while (service.IsRoomOperationInFlight) await Task.Yield();
                await service.LeaveRoomAsync();
                if (!Alive(version)) return true;
                boundRoom = null; connected = false; restoreRequested = false; view.DevRoomRequested = null;
                foreach (var item in navigation.GetComponentsInChildren<MissingMultiplayerView>(true))
                    if (item.DevLeaveSavedEntry) { item.DevLeaveSavedEntry = false; Label(item.Entry, "동물 선택", "Select animals"); }
                if (navigation.Navigate(returnScreen, false)) NavigationCount++;
                return true;
            }
            catch (Exception)
            {
                if (Alive(version)) { connected = false; Words(view.transform, "RoomUnavailable", "나가기 결과 미확정 · 다시 나가기로 확인해 주세요.", "Leave result unknown · Retry leave to confirm."); }
                return false;
            }
            finally { if (this != null) { busy = false; RenderButtons(); } }
        }
        private async void Command(MissingMultiplayerRoomAction action)
        {
            if (busy || !view.CanNavigate || !service.HasActiveRoomReceipt) return;
            if (action == MissingMultiplayerRoomAction.Leave) { await LeaveSavedAsync(); return; }
            var state = service.CurrentRoom;
            if (!connected || state == null || state.RoomId != boundRoom) return;
            if (action == MissingMultiplayerRoomAction.Copy) { GUIUtility.systemCopyBuffer = state.RoomCode; return; }
            if (action == MissingMultiplayerRoomAction.Ready && !state.CanReady || action == MissingMultiplayerRoomAction.Start && !state.CanStart) return;
            var self = state.Participants.Single(p => p.AccountId == service.AccountId);
            busy = true; int version = generation; service.StopRoomPolling(); RenderButtons();
            try
            {
                while (service.IsRoomOperationInFlight) await Task.Yield();
                if (action == MissingMultiplayerRoomAction.Ready) await service.SetReadyAsync(!self.Ready);
                else await service.StartRoomAsync();
            }
            catch (Exception) { if (Alive(version)) Operation("RoomUnknown", "ROOM_COMMAND_UNCONFIRMED"); }
            finally
            {
                if (Alive(version)) { busy = false; RenderButtons(); if (navigation.CurrentScreenId == "SC06_MatchRoom") service.BeginRoomPolling(); }
                else if (this != null) busy = false;
            }
        }
        private void Render(NetUiRoomState state)
        {
            var mode = NetUiValidation.FindMode(service.CurrentCatalog, state.ModeId);
            Words(view.transform, "RoomIdentityText", "방 코드 " + state.RoomCode + " · 방장 " + state.OwnerAccountId + "\n현재 인원 " + state.Participants.Length + " / 4 · " + mode.DisplayName + "\n정책 " + state.PolicyRevision,
                "Code " + state.RoomCode + " · Host " + state.OwnerAccountId + "\nPlayers " + state.Participants.Length + " / 4 · " + mode.DisplayName + "\nPolicy " + state.PolicyRevision);
            for (int i = 0; i < view.ParticipantSlots.Length; i++)
            {
                var slot = view.ParticipantSlots[i]; slot.gameObject.SetActive(true);
                if (i >= state.Participants.Length) { Words(slot, "HeadingText", "빈 자리", "Empty slot"); Words(slot, "ParticipantState", "--", "--"); continue; }
                var p = state.Participants[i]; var art = service.ToArtLoadout(p.Loadout);
                Words(slot, "HeadingText", p.AccountId, p.AccountId);
                string loadout = string.Join(" · ", new[] { art.Ground, art.Special, art.Air }.Select(id => host.Presenter.Catalog.Find(id)?.DisplayName ?? "--"));
                Words(slot, "ParticipantState", p.AccountId + (p.AccountId == service.AccountId ? " · 본인" : "") + (p.IsOwner ? " · 방장" : "") +
                    "\n" + (p.Connected ? "연결됨" : "연결 끊김") + " · " + (p.Ready ? "준비 완료" : "준비 전") + "\n" + loadout,
                    p.AccountId + (p.AccountId == service.AccountId ? " · You" : "") + (p.IsOwner ? " · Host" : "") + "\n" + (p.Connected ? "Connected" : "Disconnected") + " · " + (p.Ready ? "Ready" : "Not ready") + "\n" + loadout);
            }
            bool started = state.Phase == "Started";
            Words(view.transform, "RoomUnavailable", started ? "시작 승인 · 경기 연결 대기" : state.Phase == "Aborted" ? "방 진행 중단 · 나가기를 눌러 복귀해 주세요." : "서버 대기방 · 각자 준비 후 방장이 시작할 수 있습니다.", started ? "Start approved · Gameplay connection pending" : state.Phase == "Aborted" ? "Room aborted · Leave to return." : "Server room · Ready up, then the host may start.");
            RenderButtons();
        }
        private void RenderButtons()
        {
            if (view == null || service == null) return;
            var state = service.CurrentRoom; bool live = !busy && connected && state != null && state.RoomId == boundRoom && service.HasActiveRoomReceipt;
            view.Ready.interactable = live && state.CanReady; view.StartMatch.interactable = live && state.CanStart;
            view.Copy.interactable = live; view.Invite.interactable = false;
            view.Leave.interactable = !busy && service.HasActiveRoomReceipt && (state == null || !connected || state.CanLeave);
            var self = state?.Participants.FirstOrDefault(p => p.AccountId == service.AccountId);
            Label(view.Ready, self?.Ready == true ? "준비 취소" : "준비", self?.Ready == true ? "Unready" : "Ready");
            Label(view.StartMatch, "시작", "Start"); Label(view.Leave, "방 나가기", "Leave room"); Label(view.Copy, "코드 복사", "Copy code");
        }
        private static void Label(Button button, string ko, string en)
        { var t = button.GetComponentInChildren<MissingUtilityText>(true); if (t != null) { t.Korean = ko; t.English = en; t.Apply(); } }
        private static void Words(Transform root, string name, string ko, string en)
        { var t = root.GetComponentsInChildren<MissingUtilityText>(true).FirstOrDefault(x => x.name == name); if (t != null) { t.Korean = ko; t.English = en; t.Apply(); } }
        private void OnDestroy()
        {
            generation++; initialized = false;
            if (service != null) { service.StopRoomPolling(); service.AcceptedReceiptReady -= Receipt; service.RoomStateChanged -= RoomChanged; service.OperationStateChanged -= Operation; }
            if (navigation != null) { navigation.ScreenChanged -= ScreenChanged; if (navigation.BackOverride == Back) navigation.BackOverride = previousBack; }
            if (view != null) view.DevRoomRequested = null;
        }
    }
}
#endif
