using System.Globalization;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.PortraitArtV1
{
    /// <summary>Installs only in the existing Bootstrap/Lobby canvases. No scene or legacy prefab rewrite.</summary>
    public sealed class PortraitEntryController : MonoBehaviour
    {
        public const string StartId="PA1_Start";
        public const string ModeId="SC01_Lobby";
        public PortraitArtScreen StartPrefab, ModePrefab;
        public PortraitArtScreen StartScreen { get; private set; }
        public PortraitArtScreen ModeScreen { get; private set; }
        public UiNavigationService Navigation { get; private set; }
        public IRewardLedgerAdapter Ledger { get; private set; }=new DisconnectedRewardLedgerAdapter();
        public bool OnlineModesAvailable => multiplayer != null && multiplayer.OperationalReadyEnabled;
        // No operational rewarded-ad provider exists. Configuration flags or DEV approval are not proof.
        public bool RewardedAdAvailable => false;
        private MultiplayerUiPresenter multiplayer;
        private float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => SceneManager.sceneLoaded-=InstallScene;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe() { SceneManager.sceneLoaded-=InstallScene; SceneManager.sceneLoaded+=InstallScene; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrent() => InstallScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        private static void InstallScene(Scene scene, LoadSceneMode mode)
        {
            if(scene.name!="Bootstrap" && scene.name!="Lobby") return;
            var prefab=Resources.Load<PortraitEntryController>("ANIMOLPortraitEntryV1");
            if(prefab==null) return;
            foreach(var root in scene.GetRootGameObjects()) foreach(var nav in root.GetComponentsInChildren<UiNavigationService>(true)) {
                if(nav.GetComponent<Canvas>()==null || nav.transform.Find("SafeArea/ScreenHost")==null) continue;
                if(nav.GetComponentInChildren<PortraitEntryController>(true)==null) Instantiate(prefab,nav.transform,false);
            }
        }
        private void Awake()
        {
            Navigation=GetComponentInParent<UiNavigationService>();
            var host=Navigation.transform.Find("SafeArea/ScreenHost");
            multiplayer=Navigation.GetComponent<MultiplayerUiPresenter>();
            bool boot=gameObject.scene.name=="Bootstrap";
            StartScreen=Instantiate(StartPrefab,host,false); StartScreen.name=StartId;
            StartScreen.Play.onClick.AddListener(()=> {
                if(boot) Navigation.GetComponent<BootstrapPresenter>().Continue();
                else Navigation.Navigate(ModeId,false);
            });
            if(!boot) {
                // Preserve the hidden legacy instance and all presenter references, but let
                // existing SC01_Lobby return routes resolve to the new mode-selection view.
                var old=host.Find(ModeId);
                if(old!=null) { old.gameObject.SetActive(false); old.name="PA1_LegacyLobby"; }
                ModeScreen=Instantiate(ModePrefab,host,false); ModeScreen.name=ModeId;
                ModeScreen.Back.onClick.AddListener(()=>Navigation.Navigate(StartId,false));
                ModeScreen.Campaign.onClick.AddListener(()=>Navigation.Navigate("SC02_ThemeSelect"));
                ModeScreen.Shop.onClick.AddListener(()=>Navigation.Navigate("SC11_Store"));
                ModeScreen.Settings.onClick.AddListener(()=>Navigation.Navigate("SC13_Settings"));
                ModeScreen.Competition.onClick.AddListener(()=> { if(OnlineModesAvailable) Navigation.Navigate("SC05_CompetitiveHub"); });
                ModeScreen.Cooperation.onClick.AddListener(()=> { if(OnlineModesAvailable) Navigation.Navigate("SC08_CoopHub"); });
                // Intentionally no Ad.onClick handler: never synthesize an approval/grant.
            }
            Navigation.RebuildIndex(); RefreshState();
            Navigation.Navigate(boot ? StartId : ModeId,false);
        }
        private void Update()
        {
            if(Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+.25f; RefreshState();
        }
        public bool BindVerifiedLedger(IRewardLedgerAdapter ledger)
        {
            if(ledger==null || ledger.IsDevelopmentOnly) return false;
            Ledger=ledger; RefreshState(); return true;
        }
        public void RefreshState()
        {
            if(ModeScreen==null) return;
            int? balance=Ledger.IsAvailable && !Ledger.IsDevelopmentOnly ? Ledger.VerifiedCoinBalance : null;
            ModeScreen.Currency.text=balance.HasValue ? balance.Value.ToString("N0",CultureInfo.InvariantCulture) : "--";
            ModeScreen.CurrencyState.text=balance.HasValue ? "검증된 코인" : "코인 조회 불가";
            // Integer raster sizes, including Int32.MaxValue, without changing the capsule/hit rect.
            ModeScreen.Currency.fontSize=ModeScreen.Currency.text.Length>10 ? 16 : 32;
            ModeScreen.Ad.interactable=RewardedAdAvailable;
            ModeScreen.Competition.interactable=OnlineModesAvailable;
            ModeScreen.Cooperation.interactable=OnlineModesAvailable;
            ModeScreen.CompetitionState.text=OnlineModesAvailable ? "기존 경쟁 허브" : "서버 미연결";
            ModeScreen.CooperationState.text=OnlineModesAvailable ? "기존 협동 허브" : "서버 미연결";
        }
    }
}
