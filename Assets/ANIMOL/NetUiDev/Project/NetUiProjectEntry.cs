#if ANIMOL_NET_UI_DEV
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.MissingUiV1.Project;
using ANIMOL.MissingUiV1.Project.Multiplayer;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Animol.NetUiDev.Project
{
    // Owns read-only DEV UI bindings. The coordinator remains the only service owner.
    public sealed class NetUiProjectEntry : MonoBehaviour
    {
        private NetUiProjectSettings settings;
        private NetUiProjectContextReader reader;
        private UiNavigationService navigation;
        private MissingMultiplayerView[] views = Array.Empty<MissingMultiplayerView>();
        private bool busy, connected;
        private int generation;
        private string verifiedCode;
        public NetUiBoundContext CurrentContext { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks() => SceneManager.sceneLoaded -= Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }
        private static void Install(Scene scene, LoadSceneMode mode)
        {
            if ((!Application.isEditor && !Debug.isDebugBuild) || (scene.name != "Bootstrap" && scene.name != "Lobby")) return;
            NetUiProjectSettings config;
            try { config = NetUiProjectSettings.ReadLocal(); }
            catch (Exception) { Debug.LogWarning("NET02: local DEV connection settings are invalid; entry remains unavailable."); return; }
            if (config == null) return;
            if (NetUiDevCoordinator.Instance == null)
                new GameObject("ANIMOL NET02 DEV Service").AddComponent<NetUiDevCoordinator>();
            if (scene.name != "Lobby") return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var nav in root.GetComponentsInChildren<UiNavigationService>(true))
                if (nav.GetComponent<MultiplayerUiPresenter>() != null && nav.GetComponent<NetUiProjectEntry>() == null)
                    nav.gameObject.AddComponent<NetUiProjectEntry>().settings = config;
        }
        private IEnumerator Start()
        {
            // Existing Phase3 and Missing UI installers finish on frames 2 and 3.
            for (int i = 0; i < 6; i++) yield return null;
            navigation = GetComponent<UiNavigationService>();
            if (settings == null || navigation == null) yield break;
            views = navigation.GetComponentsInChildren<MissingMultiplayerView>(true);
            foreach (var view in views)
            {
                var target = view;
                view.DevContextRequested = action => Request(target, action);
                if (view.Code != null) view.Code.Field.onValueChanged.AddListener(CodeChanged);
                SetText(view, "PolicyUnavailable", "DEV 문맥 조회 가능 · 입장 승인 연결 대기", "DEV context queries available · Admission unavailable");
                SetText(view, "CodeGuide", "코드를 입력한 뒤 서버에서 방과 참가 조건을 조회하세요.", "Enter a code, then query the room and entry conditions.");
                SetText(view, "JoinNotice", "서버에서 코드와 경기 규칙을 조회합니다. 조회는 입장이 아닙니다.", "Query the code and match rules. A lookup is not an admission.");
                Label(view.Lookup, "방 조회", "Look up room");
                Label(view.Entry, "동물 선택", "Select animals");
            }
            navigation.ScreenChanged += ScreenChanged;
            RefreshButtons();
        }
        private void ScreenChanged(string id) { generation++; verifiedCode = null; RefreshButtons(); }
        private void CodeChanged(string value)
        {
            generation++; verifiedCode = null; CurrentContext = null;
            foreach (var view in views.Where(v => v.Code != null))
            {
                SetText(view, "CodeNotice", "코드 변경 · 다시 조회해 주세요.", "Code changed · Look up again.");
                SetText(view, "UnknownRoom", "방장 --\n방 코드 --\n현재 인원 -- / 4\n경기 유형 --\n참가 조건 --\n정책 버전 --", "Host --\nRoom code --\nPlayers -- / 4\nMatch rules --\nEntry conditions --\nPolicy revision --");
            }
            RefreshButtons();
        }
        private async Task EnsureConnected(MissingMultiplayerView view)
        {
            var service = NetUiDevCoordinator.Instance;
            NetUiValidation.Require(service != null && !service.BlocksNewEntry, "RESOLVE_CURRENT_ACTION_BEFORE_READING_CONTEXT");
            if (connected) return;
            var map = NetUiProjectContextReader.BuildProjectMap(view.Host.Presenter.Catalog, view.Host.IdMap);
            service.ConfigureConnection(settings.ServerUrl, settings.AccountId, map);
            await service.ConnectAsync(settings.DevAccessKey);
            reader = new NetUiProjectContextReader(new NetUiCoordinatorContextSource(service), settings, view.Host.Presenter.Catalog, map);
            connected = true;
        }
        private async void Request(MissingMultiplayerView view, MissingMultiplayerContextAction action)
        {
            if (busy || !view.CanNavigate) return;
            busy = true; CurrentContext = null;
            int version = generation;
            bool lookup = action == MissingMultiplayerContextAction.Lookup;
            bool join = lookup || action == MissingMultiplayerContextAction.Join;
            string code = join ? view.Code.Field.text : "";
            if (join) SetText(view, "UnknownRoom", "방장 --\n방 코드 --\n현재 인원 -- / 4\n경기 유형 --\n참가 조건 --\n정책 버전 --", "Host --\nRoom code --\nPlayers -- / 4\nMatch rules --\nEntry conditions --\nPolicy revision --");
            verifiedCode = null;
            RefreshButtons();
            SetText(view, join ? "CodeNotice" : "PolicyUnavailable", "서버 문맥 조회 중…", "Reading server context…");
            try
            {
                await EnsureConnected(view);
                var context = await reader.ReadAsync(join ? "Join" : action.ToString(), code);
                if (this == null || version != generation || !view.CanNavigate) return;
                CurrentContext = context;
                string growthKo = context.GrowthPolicy == "OwnedProgress" ? "보유 성장 정책" : "랭크 최대 프리셋 정책";
                string policyKo = context.Request.DisplayName + " · " + growthKo + "\n정책 " + context.Request.Requirements.PolicyRevision;
                string policyEn = context.Request.DisplayName + " · " + context.GrowthPolicy + "\nPolicy " + context.Request.Requirements.PolicyRevision;
                SetText(view, "PolicyUnavailable", policyKo + "\n입장 승인 연결 대기", policyEn + "\nAdmission unavailable");
                SetText(view, "ActualRoomOptions", policyKo, policyEn);
                if (join)
                {
                    verifiedCode = code;
                    var room = context.Lookup;
                    SetText(view, "CodeNotice", "서버 조회 완료 · 입장 승인 전", "Server lookup complete · Not admitted");
                    SetText(view, "UnknownRoom", "방장 --\n방 코드 " + room.RoomCode + "\n현재 인원 " + room.ParticipantCount + " / 4\n" + policyKo + "\n대기 중 · 편성 사용 권한 확인", "Host --\nRoom code " + room.RoomCode + "\nPlayers " + room.ParticipantCount + " / 4\n" + policyEn + "\nWaiting · Loadout permissions verified");
                }
                if (!lookup && !view.Host.Open(context.Request))
                    SetText(view, join ? "CodeNotice" : "PolicyUnavailable", "동물 선택 화면을 열 수 없습니다.", "Animal selection is unavailable.");
            }
            catch (Exception)
            {
                if (this == null || version != generation || !view.CanNavigate) return;
                CurrentContext = null; verifiedCode = null;
                SetText(view, join ? "CodeNotice" : "PolicyUnavailable", "조회 불가 · 연결, 모드·동물 매핑과 사용 권한을 확인해 주세요.", "Unavailable · Check connection, mode / animal mappings and permissions.");
                if (!join) view.Host.OpenUnconfigured();
            }
            finally { if (this != null) { busy = false; RefreshButtons(); } }
        }
        private void RefreshButtons()
        {
            bool available = !busy && NetUiDevCoordinator.Instance != null && !NetUiDevCoordinator.Instance.BlocksNewEntry;
            foreach (var view in views)
            {
                if (view.Lookup != null) view.Lookup.interactable = available;
                if (view.Entry != null) view.Entry.interactable = available &&
                    (view.Screen == MissingMultiplayerScreen.Create || (verifiedCode != null && view.Code.Field.text == verifiedCode));
            }
        }
        private static void Label(Button button, string ko, string en)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<MissingUtilityText>(true);
            if (text != null) { text.Korean = ko; text.English = en; text.Apply(); }
        }
        private static void SetText(MissingMultiplayerView view, string name, string ko, string en)
        {
            var text = view.GetComponentsInChildren<MissingUtilityText>(true).FirstOrDefault(t => t.name == name);
            if (text != null) { text.Korean = ko; text.English = en; text.Apply(); }
        }
        private void OnDestroy()
        {
            generation++;
            if (navigation != null) navigation.ScreenChanged -= ScreenChanged;
            foreach (var view in views)
            {
                if (view == null) continue;
                view.DevContextRequested = null;
                if (view.Code != null) view.Code.Field.onValueChanged.RemoveListener(CodeChanged);
                if (view.Lookup != null) view.Lookup.interactable = false;
                if (view.Entry != null) view.Entry.interactable = false;
            }
        }
    }
}
#endif
