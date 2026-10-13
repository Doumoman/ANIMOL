using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Animol.NetUiDev
{
    public sealed class NetUiServiceException : Exception
    {
        public readonly string Status;
        public readonly long HttpStatus;
        public NetUiServiceException(string status, string reason, long httpStatus = 0) : base(reason)
        { Status = status; HttpStatus = httpStatus; }
    }

    // Invoke on Unity's main thread. No scene, UI, account gateway, or NGO dependency.
    public sealed class NetUiHttpClient
    {
        public string ServerUrl { get; private set; }
        public string SessionToken { get; set; }
        public int TimeoutSeconds { get; set; } = 12;

        public NetUiHttpClient(string serverUrl)
        {
            EnsureDevelopment();
            ServerUrl = NetUiValidation.NormalizeServerUrl(serverUrl, Application.isMobilePlatform);
        }

        public static void EnsureDevelopment()
        { if (!Application.isEditor && !Debug.isDebugBuild) throw new InvalidOperationException("NET02 is available only in Editor/development builds."); }

        public Task<NetUiSessionResponse> ConnectAsync(string accountId, string accessKey)
        { return SendAsync<NetUiSessionResponse>("POST", "/v1/dev/session", JsonUtility.ToJson(new NetUiSessionRequest { AccountId = accountId, DevAccessKey = accessKey }), false); }
        public Task<NetUiCatalog> GetCatalogAsync() { return SendAsync<NetUiCatalog>("GET", "/v1/catalog", null); }
        public Task<NetUiContextSnapshot> ReadContextAsync(NetUiContextRequest request)
        { return SendAsync<NetUiContextSnapshot>("POST", "/v1/multiplayer/context", JsonUtility.ToJson(request)); }
        public Task<NetUiLookupResponse> LookupAsync(string code)
        { return SendAsync<NetUiLookupResponse>("POST", "/v1/room/lookup", JsonUtility.ToJson(new NetUiLookupRequest { RoomCode = code })); }
        internal Task<NetUiEntryResult> SubmitJsonAsync(string immutableRequestJson)
        { return SendAsync<NetUiEntryResult>("POST", "/v1/room/entry", immutableRequestJson); }
        internal Task<NetUiEntryResult> ResolveJsonAsync(string immutableRequestJson)
        { return SendAsync<NetUiEntryResult>("POST", "/v1/room/result", immutableRequestJson); }
        public Task<NetUiRoomState> RoomStateAsync() { return SendAsync<NetUiRoomState>("GET", "/v1/room/state", null); }
        public Task<NetUiRoomState> ReadyAsync(string roomId, bool ready)
        { return SendAsync<NetUiRoomState>("POST", "/v1/room/ready", JsonUtility.ToJson(new NetUiReadyRequest { RoomId = roomId, Ready = ready })); }
        public Task<NetUiRoomState> StartAsync(string roomId)
        { return SendAsync<NetUiRoomState>("POST", "/v1/room/start", JsonUtility.ToJson(new NetUiRoomCommand { RoomId = roomId })); }
        public Task<NetUiEnvelope> LeaveAsync(string roomId)
        { return SendAsync<NetUiEnvelope>("POST", "/v1/room/leave", JsonUtility.ToJson(new NetUiRoomCommand { RoomId = roomId })); }

        private async Task<T> SendAsync<T>(string method, string path, string json, bool authenticated = true) where T : NetUiEnvelope
        {
            EnsureDevelopment();
            if (authenticated && string.IsNullOrWhiteSpace(SessionToken)) throw new NetUiServiceException("Unavailable", "DEV_SESSION_REQUIRED");
            using (var request = new UnityWebRequest(ServerUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = TimeoutSeconds;
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                if (authenticated) request.SetRequestHeader("Authorization", "Bearer " + SessionToken);
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                string body = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
                    throw new NetUiServiceException("Unknown", "NETWORK_RESULT_UNKNOWN", request.responseCode);
                T reply;
                try { reply = JsonUtility.FromJson<T>(body); }
                catch (Exception) { throw new NetUiServiceException("Unknown", "INVALID_SERVER_JSON", request.responseCode); }
                if (reply == null) throw new NetUiServiceException("Unknown", "EMPTY_SERVER_RESPONSE", request.responseCode);
                // Entry/result callers need complete typed final results to validate ActionId.
                if (typeof(T) != typeof(NetUiEntryResult) && (request.result == UnityWebRequest.Result.ProtocolError || reply.Status == "Rejected" || reply.Status == "Unavailable" || reply.Status == "Unknown"))
                    throw new NetUiServiceException(reply.Status ?? "Unknown", reply.Reason ?? "HTTP_ERROR", request.responseCode);
                return reply;
            }
        }
    }
}
