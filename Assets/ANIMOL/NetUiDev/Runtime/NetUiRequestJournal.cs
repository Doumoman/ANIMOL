using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Animol.NetUiDev
{
    public sealed class NetUiRequestJournal
    {
        private readonly string key;
        private readonly string activeScopeKey;
        private NetUiJournal record;
        public NetUiJournal Record { get { return JsonUtility.FromJson<NetUiJournal>(JsonUtility.ToJson(record)); } }
        public bool BlocksNewEntry { get { return record.State == "Pending" || record.State == "Unknown" || (record.State == "Accepted" && !record.RoomLeftConfirmed); } }

        public NetUiRequestJournal(string serverUrl, string accountId, string devClientSlot = "device")
        {
            ValidateSlot(devClientSlot);
            activeScopeKey = "ANIMOL.NET02.ActiveScope." + devClientSlot;
            var active = ReadActiveScope(devClientSlot);
            NetUiValidation.Require(active == null || (active.ServerUrl == serverUrl && active.AccountId == accountId), "RESTORE_LOCKED_DEV_SCOPE_BEFORE_CHANGING_ACCOUNT_OR_SERVER");
            key = "ANIMOL.NET02." + devClientSlot + "." + ScopeHash(serverUrl + "\n" + accountId);
            var saved = PlayerPrefs.GetString(key, "");
            NetUiValidation.Require(active == null || !string.IsNullOrEmpty(saved), "LOCKED_DEV_RECOVERY_JOURNAL_MISSING");
            if (string.IsNullOrEmpty(saved)) record = new NetUiJournal { ServerUrl = serverUrl, AccountId = accountId, State = "Idle" };
            else
            {
                try { record = JsonUtility.FromJson<NetUiJournal>(saved); }
                catch (Exception) { throw new InvalidOperationException("DEV_JOURNAL_CORRUPT_DO_NOT_RESUBMIT"); }
                NetUiValidation.Require(record != null && record.SchemaVersion == 1 && record.ServerUrl == serverUrl && record.AccountId == accountId, "DEV_JOURNAL_SCOPE_INVALID");
                NetUiValidation.Require(record.State == "Idle" || record.State == "Pending" || record.State == "Unknown" || record.State == "Accepted" || record.State == "Rejected" || record.State == "Unavailable", "DEV_JOURNAL_STATE_INVALID");
                NetUiValidation.Require(active == null || record.State != "Idle", "LOCKED_DEV_RECOVERY_JOURNAL_INVALID");
                if (record.State != "Idle") ReadRequest();
                if (record.State == "Accepted") NetUiValidation.ValidateReceipt(ReadRequest(), ReadResult());
                if (record.State == "Pending") { record.State = "Unknown"; Save(); }
            }
        }

        public static NetUiActiveScope ReadActiveScope(string devClientSlot = "device")
        {
            ValidateSlot(devClientSlot);
            string saved = PlayerPrefs.GetString("ANIMOL.NET02.ActiveScope." + devClientSlot, "");
            if (string.IsNullOrEmpty(saved)) return null;
            var active = JsonUtility.FromJson<NetUiActiveScope>(saved);
            NetUiValidation.Require(active != null && !string.IsNullOrWhiteSpace(active.ServerUrl) && !string.IsNullOrWhiteSpace(active.AccountId), "DEV_ACTIVE_SCOPE_CORRUPT");
            return active;
        }
        private static void ValidateSlot(string slot)
        {
            NetUiValidation.Require(!string.IsNullOrWhiteSpace(slot) && slot.Length <= 32, "DEV_CLIENT_SLOT_INVALID");
            foreach (char character in slot) NetUiValidation.Require(char.IsLetterOrDigit(character) || character == '_' || character == '-', "DEV_CLIENT_SLOT_INVALID");
        }

        public void SetSession(NetUiSessionResponse session)
        {
            NetUiValidation.Require(session != null && session.AccountId == record.AccountId && !string.IsNullOrWhiteSpace(session.SessionToken) && session.SessionNamespace == "ANIMOL_NET02_DEV", "DEV_SESSION_MISMATCH");
            record.SessionToken = session.SessionToken;
            record.SessionNamespace = session.SessionNamespace;
            Save();
        }

        public void Begin(NetUiEntryRequest request)
        {
            NetUiValidation.Require(!BlocksNewEntry, "PREVIOUS_ACTION_REQUIRES_RESULT_OR_RECEIPT");
            if (!string.IsNullOrEmpty(record.RequestJson))
                NetUiValidation.Require(ReadRequest().ActionId != request.ActionId, "ACTION_ID_MUST_NOT_BE_REUSED");
            record.RequestJson = JsonUtility.ToJson(request);
            record.ResultJson = "";
            record.State = "Pending";
            record.ReceiptConsumed = false;
            record.RoomLeftConfirmed = false;
            Save(); // Persist the full fixed payload before sending a network request.
        }

        public NetUiEntryRequest ReadRequest()
        {
            NetUiValidation.Require(!string.IsNullOrEmpty(record.RequestJson), "PENDING_REQUEST_MISSING");
            var request = JsonUtility.FromJson<NetUiEntryRequest>(record.RequestJson);
            NetUiValidation.Require(request != null && !string.IsNullOrWhiteSpace(request.ActionId), "PENDING_REQUEST_INVALID");
            return request;
        }

        public NetUiEntryResult ReadResult()
        { return string.IsNullOrEmpty(record.ResultJson) ? null : JsonUtility.FromJson<NetUiEntryResult>(record.ResultJson); }

        public void Apply(NetUiEntryResult result)
        {
            var request = ReadRequest();
            NetUiValidation.Require(result != null && result.ActionId == request.ActionId, "RESULT_ACTION_MISMATCH");
            if (result.Status == "Accepted") NetUiValidation.ValidateReceipt(request, result);
            NetUiValidation.Require(result.Status == "Accepted" || result.Status == "Rejected" || result.Status == "Unavailable" || result.Status == "Unknown", "RESULT_STATUS_INVALID");
            record.ResultJson = JsonUtility.ToJson(result);
            record.State = result.Status;
            Save();
        }

        public void MarkUnknown(string reason)
        {
            record.State = "Unknown";
            record.ResultJson = JsonUtility.ToJson(new NetUiEntryResult { ActionId = ReadRequest().ActionId, Status = "Unknown", Reason = reason });
            Save();
        }

        public void MarkRetryPending()
        {
            NetUiValidation.Require(record.State == "Unknown" && ReadResult()?.Reason == "ACTION_NOT_FOUND", "AUTHORITATIVE_NOT_FOUND_LOOKUP_REQUIRED");
            record.State = "Pending";
            Save();
        }

        public bool Consume(string actionId, string acceptanceToken)
        {
            if (record.State != "Accepted" || record.ReceiptConsumed) return false;
            var result = ReadResult();
            NetUiValidation.ValidateReceipt(ReadRequest(), result);
            NetUiValidation.Require(result.ActionId == actionId && result.AcceptanceToken == acceptanceToken, "RECEIPT_ACK_MISMATCH");
            record.ReceiptConsumed = true;
            Save();
            return true;
        }

        public void ConfirmLeft()
        {
            NetUiValidation.Require(record.State == "Accepted" && record.ReceiptConsumed, "ACCEPTED_ROOM_RECEIPT_REQUIRED");
            record.RoomLeftConfirmed = true;
            Save();
        }

        private void Save()
        {
            string existing = PlayerPrefs.GetString(activeScopeKey, "");
            var active = string.IsNullOrEmpty(existing) ? null : JsonUtility.FromJson<NetUiActiveScope>(existing);
            NetUiValidation.Require(active == null || (active.ServerUrl == record.ServerUrl && active.AccountId == record.AccountId), "ANOTHER_DEV_SCOPE_REQUIRES_RECOVERY");
            PlayerPrefs.SetString(key, JsonUtility.ToJson(record));
            if (BlocksNewEntry) PlayerPrefs.SetString(activeScopeKey, JsonUtility.ToJson(new NetUiActiveScope { ServerUrl = record.ServerUrl, AccountId = record.AccountId }));
            else if (active != null) PlayerPrefs.DeleteKey(activeScopeKey);
            PlayerPrefs.Save();
        }
        private static string ScopeHash(string scope)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(scope))).Replace("-", ""); }
    }
}
