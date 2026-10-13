using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Animol.NetUiDev
{
    // One service owner attached by the existing project bootstrap. This component
    // creates no UI and never opens SC06 or SC07 itself. The existing host owns UI.
    public sealed class NetUiDevCoordinator : MonoBehaviour
    {
        public static NetUiDevCoordinator Instance { get; private set; }
        [SerializeField] private string serverUrl = "";
        [SerializeField] private string accountId = "";
        [SerializeField] private NetUiIdMapEntry[] explicitProjectIdMap = new NetUiIdMapEntry[0];
        [SerializeField] private float roomPollSeconds = 2f;
        private NetUiHttpClient client;
        private NetUiRequestJournal journal;
        private NetUiCatalog catalog;
        private NetUiContextSnapshot context;
        private NetUiRoomState room;
        private bool admissionInFlight, contextInFlight, roomMutationInFlight, roomPollInFlight, deliveringReceipt;
        private bool polling;
        private int serviceReads, pollGeneration;
        private bool sessionChanging;
        private float nextPoll;
        private string lastNotifiedReceipt;
        private string devClientSlot = "device";

        public event Action<string, string> OperationStateChanged;
        public event Action<NetUiRoomState> RoomStateChanged;
        // This is an advisory signal. Its subscriber calls TryDeliverAccepted,
        // which alone coordinates acknowledgment with the existing SC06 consumer.
        public event Action AcceptedReceiptReady;
        public string AccountId { get { return accountId; } }
        public string ServerUrl => serverUrl;
        // Redacted, process-local diagnostics. No credentials, action IDs or receipt tokens.
        public int SubmissionCount { get; private set; }
        public int ResultQueryCount { get; private set; }
        public int ReceiptNotificationCount { get; private set; }
        public int ReceiptConsumeCount { get; private set; }
        public int AcceptedPresentationCount { get; private set; }
        public void RecordAcceptedPresentation() { AcceptedPresentationCount++; }
        public NetUiEntryRequest StoredRequest => journal == null || string.IsNullOrEmpty(journal.Record.RequestJson) ? null : Copy(journal.ReadRequest());
        public bool BlocksNewEntry { get { return admissionInFlight || (journal != null && journal.BlocksNewEntry); } }
        public string PendingActionId { get { return journal == null || string.IsNullOrEmpty(journal.Record.RequestJson) ? null : journal.ReadRequest().ActionId; } }
        public string RequestState { get { return journal == null ? "Unconfigured" : (journal.Record.RoomLeftConfirmed ? "Left" : journal.Record.State); } }
        public bool HasActiveRoomReceipt { get { return journal != null && journal.Record.State == "Accepted" && !journal.Record.RoomLeftConfirmed; } }
        public bool RequiresReceiptDelivery { get { return HasActiveRoomReceipt && !journal.Record.ReceiptConsumed; } }
        public bool IsRoomOperationInFlight => roomPollInFlight || roomMutationInFlight;
        public bool IsRoomPolling => polling;
        public NetUiRoomState CurrentRoom { get { return Copy(room); } }
        public NetUiCatalog CurrentCatalog { get { return Copy(catalog); } }

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) { enabled = false; return; }
            if (Instance != null && Instance != this) { Destroy(this); return; }
            NetUiValidation.Require(transform.parent == null && transform.childCount == 0 && GetComponents<Component>().Length == 2, "ATTACH_NET02_ONLY_TO_A_DEDICATED_EMPTY_ROOT_SERVICE_OBJECT");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            devClientSlot = ResolveDevClientSlot();
            if (!Application.isMobilePlatform)
            {
                serverUrl = ReadCommandLine("-animolNet02Server") ?? serverUrl;
                accountId = ReadCommandLine("-animolNet02Account") ?? accountId;
            }
            var active = NetUiRequestJournal.ReadActiveScope(devClientSlot);
            if (active != null) { serverUrl = active.ServerUrl; accountId = active.AccountId; }
            if (!string.IsNullOrWhiteSpace(serverUrl) && !string.IsNullOrWhiteSpace(accountId))
                ConfigureConnection(serverUrl, accountId, explicitProjectIdMap);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; StopRoomPolling(); }

        public void ConfigureConnection(string url, string devAccountId, NetUiIdMapEntry[] projectMap)
        {
            NetUiHttpClient.EnsureDevelopment();
            NetUiValidation.Require(serviceReads == 0 && !admissionInFlight && !contextInFlight && !roomMutationInFlight && !roomPollInFlight && !BlocksNewEntry, "RESOLVE_CURRENT_ACTION_BEFORE_SWITCHING_SESSION");
            NetUiValidation.Require(!string.IsNullOrWhiteSpace(devAccountId), "DEV_ACCOUNT_ID_REQUIRED");
            // Validate recovery scope before mutating any live account/endpoint fields.
            var normalized = NetUiValidation.NormalizeServerUrl(url, Application.isMobilePlatform);
            var nextJournal = new NetUiRequestJournal(normalized, devAccountId, devClientSlot);
            var nextClient = new NetUiHttpClient(normalized);
            serverUrl = normalized; accountId = devAccountId;
            explicitProjectIdMap = Copy(projectMap) ?? new NetUiIdMapEntry[0];
            client = nextClient; journal = nextJournal;
            client.SessionToken = journal.Record.SessionToken;
            context = null; catalog = null; room = null; polling = false; lastNotifiedReceipt = null;
            PublishState(RequestState, "");
        }

        public async Task<NetUiCatalog> ConnectAsync(string devAccessKey)
        {
            RequireConfigured();
            NetUiValidation.Require(!sessionChanging && !admissionInFlight && !contextInFlight && !roomMutationInFlight && !roomPollInFlight, "CURRENT_SERVICE_OPERATION_BUSY");
            sessionChanging = true; serviceReads++;
            try
            {
                var session = await client.ConnectAsync(accountId, devAccessKey);
                journal.SetSession(session);
                client.SessionToken = session.SessionToken;
                return await GetCatalogAsync();
            }
            finally { serviceReads--; sessionChanging = false; }
        }

        public async Task<NetUiCatalog> GetCatalogAsync()
        {
            RequireConfigured();
            serviceReads++;
            try
            {
                var reply = await client.GetCatalogAsync();
                NetUiValidation.ValidateCatalog(reply);
                catalog = Copy(reply);
                NotifyReceipt();
                return Copy(reply);
            }
            finally { serviceReads--; }
        }

        public async Task<NetUiContextSnapshot> ReadContextAsync(string modeId, string opaqueEntryIntent, string confirmedRoomCode = "")
        {
            RequireConfigured();
            NetUiValidation.Require(!BlocksNewEntry && !contextInFlight, "CURRENT_ACTION_OR_CONTEXT_IS_BUSY");
            NetUiValidation.Require(catalog != null, "READ_CATALOG_FIRST");
            contextInFlight = true;
            context = null;
            try
            {
                var request = new NetUiContextRequest { ModeId = modeId, EntryIntent = opaqueEntryIntent, RoomCode = confirmedRoomCode ?? "" };
                var reply = await client.ReadContextAsync(request);
                NetUiValidation.ValidateContext(reply, request, catalog);
                // Ability/growth descriptions are NOT supplied by this lobby API.
                // Existing ability/growth UI must retain its unavailable markers.
                context = Copy(reply);
                return Copy(reply);
            }
            finally { contextInFlight = false; }
        }

        public async Task<NetUiLookupResponse> LookupRoomAsync(string userInput)
        {
            RequireConfigured();
            NetUiValidation.Require(catalog != null && catalog.CodeFormat != null, "READ_CODE_POLICY_FIRST");
            string code = (userInput ?? "").Trim().ToUpperInvariant();
            NetUiValidation.Require(code.Length == catalog.CodeFormat.Length, "ROOM_CODE_LENGTH_INVALID");
            foreach (char character in code) NetUiValidation.Require(catalog.CodeFormat.Alphabet.IndexOf(character) >= 0, "ROOM_CODE_CHARACTER_INVALID");
            serviceReads++;
            try
            {
                var reply = await client.LookupAsync(code);
                NetUiValidation.Require(reply != null && reply.RoomCode == code && !string.IsNullOrWhiteSpace(reply.RoomId) && reply.Capacity == 4, "ROOM_LOOKUP_INVALID");
                // A valid format is never treated as an existing room without this reply.
                return Copy(reply);
            }
            finally { serviceReads--; }
        }

        public NetUiEntryRequest PrepareEntry(string actionId, NetUiLoadout actualLoadout, NetUiOptionValue[] confirmedOptions = null)
        {
            RequireConfigured();
            NetUiValidation.Require(!BlocksNewEntry && context != null, "CURRENT_ACTION_OR_CONTEXT_IS_UNAVAILABLE");
            var request = new NetUiEntryRequest
            {
                ActionId = actionId, ContextId = context.Context.ContextId, SnapshotRevision = context.Snapshot.Revision,
                PolicyRevision = context.Context.PolicyRevision, EntryIntent = context.Context.EntryIntent,
                RoomCode = context.Context.RoomCode, Loadout = Copy(actualLoadout), Options = Copy(confirmedOptions) ?? new NetUiOptionValue[0]
            };
            NetUiValidation.ValidateEntry(request, context, catalog, explicitProjectIdMap);
            return request;
        }

        public NetUiLoadout ToActualLoadout(NetUiArtLoadout artLoadout)
        { return NetUiValidation.ToActual(artLoadout, catalog, explicitProjectIdMap); }
        public NetUiArtLoadout ToArtLoadout(NetUiLoadout actualLoadout)
        { return NetUiValidation.ToArt(actualLoadout, catalog, explicitProjectIdMap); }

        public void BindRecoveryProjectMap(NetUiIdMapEntry[] projectMap)
        {
            RequireConfigured();
            NetUiValidation.Require(catalog != null && StoredRequest != null, "READ_CATALOG_AND_RESTORE_REQUEST_FIRST");
            var candidate = Copy(projectMap);
            NetUiValidation.ToArt(StoredRequest.Loadout, catalog, candidate);
            explicitProjectIdMap = candidate; // Rebind identity only; never replace scope, session or payload.
        }

        public async Task<NetUiEntryResult> SubmitEntryAsync(NetUiEntryRequest confirmedRequest)
        {
            RequireConfigured();
            NetUiValidation.Require(!BlocksNewEntry && !admissionInFlight && !contextInFlight, "RESULT_REQUIRED_DO_NOT_SUBMIT_AGAIN");
            var immutable = Copy(confirmedRequest);
            NetUiValidation.ValidateEntry(immutable, context, catalog, explicitProjectIdMap);
            journal.Begin(immutable);
            admissionInFlight = true;
            PublishState("Pending", "");
            try { SubmissionCount++; return ApplyResult(await client.SubmitJsonAsync(journal.Record.RequestJson)); }
            catch (Exception exception) { return SetUnknown(exception); }
            finally { admissionInFlight = false; }
        }

        public async Task<NetUiEntryResult> ResolvePendingAsync()
        {
            RequireConfigured();
            NetUiValidation.Require(!admissionInFlight && journal.Record.State != "Idle", "NO_RESOLVABLE_ACTION_OR_REQUEST_BUSY");
            if (journal.Record.State == "Accepted" || journal.Record.State == "Rejected" || journal.Record.State == "Unavailable")
            { NotifyReceipt(); return Copy(journal.ReadResult()); }
            admissionInFlight = true;
            try { ResultQueryCount++; return ApplyResult(await client.ResolveJsonAsync(journal.Record.RequestJson)); }
            catch (Exception exception) { return SetUnknown(exception); }
            finally { admissionInFlight = false; }
        }

        public async Task<NetUiEntryResult> ResubmitUnknownAsync()
        {
            RequireConfigured();
            NetUiValidation.Require(!admissionInFlight && journal.Record.State == "Unknown", "UNKNOWN_ACTION_REQUIRED");
            // Available only after a same-body result lookup explicitly says absent.
            // This resends the original ActionId AND exact persisted JSON, never a
            // new confirmation/new room action and never a changed current loadout.
            journal.MarkRetryPending();
            admissionInFlight = true;
            PublishState("Pending", "RETRYING_ORIGINAL_ACTION");
            try { SubmissionCount++; return ApplyResult(await client.SubmitJsonAsync(journal.Record.RequestJson)); }
            catch (Exception exception) { return SetUnknown(exception); }
            finally { admissionInFlight = false; }
        }

        private NetUiEntryResult ApplyResult(NetUiEntryResult result)
        {
            journal.Apply(result);
            context = null; // A new confirmation requires a new authoritative context.
            PublishState(result.Status, result.Reason);
            NotifyReceipt();
            return Copy(result);
        }
        private NetUiEntryResult SetUnknown(Exception exception)
        {
            // Avoid exposing transport credentials or raw HTTP bodies to UI/logs.
            string reason = exception is NetUiServiceException ? exception.Message : "RESULT_VALIDATION_OR_TRANSPORT_UNKNOWN";
            journal.MarkUnknown(reason);
            PublishState("Unknown", reason);
            return Copy(journal.ReadResult());
        }

        public bool TryDeliverAccepted(Func<NetUiEntryResult, NetUiArtLoadout, bool> existingSc06Consumer)
        {
            RequireConfigured();
            if (deliveringReceipt || !HasActiveRoomReceipt || journal.Record.ReceiptConsumed) return false;
            NetUiValidation.Require(existingSc06Consumer != null && catalog != null, "EXISTING_SC06_CONSUMER_OR_CATALOG_REQUIRED");
            var result = journal.ReadResult();
            NetUiValidation.ValidateReceipt(journal.ReadRequest(), result);
            NetUiValidation.Require(room != null && room.RoomId == result.RoomId, "READ_APPROVED_ROOM_STATE_BEFORE_SC06_DELIVERY");
            var artLoadout = ToArtLoadout(result.AcceptedLoadout);
            deliveringReceipt = true;
            try
            {
                // Existing consumer must bind the real RoomId/receipt, then navigate.
                // Return true only after its own idempotent room delivery succeeds.
                if (!existingSc06Consumer(Copy(result), Copy(artLoadout))) return false;
                bool consumed = journal.Consume(result.ActionId, result.AcceptanceToken);
                if (consumed) ReceiptConsumeCount++;
                return consumed;
            }
            finally { deliveringReceipt = false; }
        }

        private void NotifyReceipt()
        {
            if (!HasActiveRoomReceipt || catalog == null || journal.Record.ReceiptConsumed) return;
            var result = journal.ReadResult();
            if (lastNotifiedReceipt == result.AcceptanceToken) return;
            lastNotifiedReceipt = result.AcceptanceToken;
            ReceiptNotificationCount++;
            try { AcceptedReceiptReady?.Invoke(); }
            catch (Exception) { Debug.LogWarning("NET02 receipt subscriber failed; receipt remains available for explicit delivery retry."); }
        }
        public void ReplayCurrentState()
        { RequireConfigured(); PublishState(RequestState, journal.ReadResult()?.Reason); if (room != null) PublishRoom(room); lastNotifiedReceipt = null; NotifyReceipt(); }

        public async Task<NetUiRoomState> PollRoomAsync()
        {
            RequireConfigured();
            NetUiValidation.Require(HasActiveRoomReceipt && !roomPollInFlight && catalog != null, "ACCEPTED_ACTIVE_ROOM_RECEIPT_REQUIRED_OR_ROOM_QUERY_BUSY");
            roomPollInFlight = true;
            int generation = pollGeneration;
            try
            {
                var reply = await client.RoomStateAsync();
                NetUiValidation.Require(generation == pollGeneration, "STALE_ROOM_QUERY_DISCARDED");
                return BindRoom(reply);
            }
            finally { roomPollInFlight = false; }
        }
        private NetUiRoomState BindRoom(NetUiRoomState state)
        {
            NetUiValidation.ValidateRoom(state, catalog, explicitProjectIdMap);
            long incomingRevision, currentRevision;
            NetUiValidation.Require(long.TryParse(state.Revision, out incomingRevision) && incomingRevision >= 0, "ROOM_REVISION_INVALID");
            if (room != null && room.RoomId == state.RoomId && long.TryParse(room.Revision, out currentRevision) && incomingRevision < currentRevision) return Copy(room);
            var receipt = journal.ReadResult();
            if (receipt != null && receipt.Status == "Accepted")
            {
                NetUiValidation.Require(state.RoomId == receipt.RoomId && state.RoomCode == receipt.RoomCode && state.ModeId == receipt.AcceptedContextId && state.PolicyRevision == receipt.AcceptedPolicyRevision, "ROOM_RECEIPT_MISMATCH");
                int selfCount = 0;
                foreach (var participant in state.Participants)
                    if (participant.AccountId == accountId) { selfCount++; NetUiValidation.Require(NetUiValidation.SameLoadout(participant.Loadout, receipt.AcceptedLoadout), "ROOM_SELF_LOADOUT_MISMATCH"); }
                NetUiValidation.Require(selfCount == 1, "ROOM_SELF_MEMBERSHIP_MISSING");
            }
            room = Copy(state);
            PublishRoom(state);
            return Copy(state);
        }

        public async Task<NetUiRoomState> SetReadyAsync(bool ready)
        {
            RequireRoomMutation(room != null && room.CanReady, "READY_NOT_ALLOWED");
            roomMutationInFlight = true;
            try { return BindRoom(await client.ReadyAsync(room.RoomId, ready)); }
            finally { roomMutationInFlight = false; }
        }
        public async Task<NetUiRoomState> StartRoomAsync()
        {
            RequireRoomMutation(room != null && room.CanStart, "START_NOT_ALLOWED");
            roomMutationInFlight = true;
            try { return BindRoom(await client.StartAsync(room.RoomId)); }
            finally { roomMutationInFlight = false; }
        }
        public async Task LeaveRoomAsync()
        {
            RequireConfigured();
            NetUiValidation.Require(HasActiveRoomReceipt && !roomMutationInFlight && !roomPollInFlight && !admissionInFlight &&
                (room == null || room.CanLeave), "LEAVE_NOT_ALLOWED_OR_REQUEST_BUSY");
            roomMutationInFlight = true;
            try
            {
                StopRoomPolling();
                // An interrupted leave may already have removed membership. The
                // persisted approved RoomId permits idempotent re-confirmation
                // even after restart, when a room state query returns NOT_IN_ROOM.
                var result = await client.LeaveAsync(journal.ReadResult().RoomId);
                NetUiValidation.Require(result.Status == "Accepted", "LEAVE_UNCONFIRMED");
                journal.ConfirmLeft();
                polling = false; room = null; context = null;
                PublishState("Left", result.Reason);
                // The existing navigation owner returns to its original entry screen.
            }
            finally { roomMutationInFlight = false; }
        }
        private void RequireRoomMutation(bool permitted, string reason)
        { RequireConfigured(); NetUiValidation.Require(HasActiveRoomReceipt && journal.Record.ReceiptConsumed && !roomMutationInFlight && !roomPollInFlight && !admissionInFlight && permitted, reason); }

        public void BeginRoomPolling()
        { RequireConfigured(); NetUiValidation.Require(HasActiveRoomReceipt, "ACCEPTED_ACTIVE_ROOM_REQUIRED"); pollGeneration++; polling = true; nextPoll = Time.unscaledTime; }
        public void StopRoomPolling() { polling = false; pollGeneration++; }
        private async void Update()
        {
            if (!polling || roomPollInFlight || roomMutationInFlight || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + Mathf.Max(1f, roomPollSeconds);
            int generation = pollGeneration;
            try { await PollRoomAsync(); }
            catch (Exception exception)
            {
                // Closing/rebinding the room intentionally discards old polls.
                // Their late errors must not appear in a subsequent screen.
                if (polling && generation == pollGeneration)
                    PublishState("RoomUnknown", exception is NetUiServiceException ? exception.Message : "ROOM_QUERY_FAILED");
            }
        }
        private void OnApplicationPause(bool paused)
        { if (!paused && polling) nextPoll = Time.unscaledTime; }
        private void RequireConfigured()
        { NetUiHttpClient.EnsureDevelopment(); NetUiValidation.Require(client != null && journal != null, "CONFIGURE_DEV_SERVICE_FIRST"); }
        private void PublishState(string state, string reason)
        { try { OperationStateChanged?.Invoke(state, reason ?? ""); } catch (Exception) { Debug.LogWarning("NET02 state subscriber failed."); } }
        private void PublishRoom(NetUiRoomState state)
        { try { RoomStateChanged?.Invoke(Copy(state)); } catch (Exception) { Debug.LogWarning("NET02 room subscriber failed."); } }
        public static string ResolveDevClientSlot()
        {
            if (Application.isMobilePlatform) return "device";
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index + 1 < args.Length; index++) if (args[index] == "-animolNet02Slot") return args[index + 1];
            return "device";
        }
        private static string ReadCommandLine(string key)
        { string[] args = Environment.GetCommandLineArgs(); for (int index = 0; index + 1 < args.Length; index++) if (args[index] == key) return args[index + 1]; return null; }
        private static T Copy<T>(T value)
        {
            if (value == null) return default(T);
            // JsonUtility does not support a top-level array.
            if (value is NetUiIdMapEntry[] map) return (T)(object)CopyArray(map);
            if (value is NetUiOptionValue[] options) return (T)(object)CopyArray(options);
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }
        [Serializable] private sealed class ArrayBox<T> { public T[] Values; }
        private static T[] CopyArray<T>(T[] values)
        { return JsonUtility.FromJson<ArrayBox<T>>(JsonUtility.ToJson(new ArrayBox<T> { Values = values })).Values; }
    }
}
