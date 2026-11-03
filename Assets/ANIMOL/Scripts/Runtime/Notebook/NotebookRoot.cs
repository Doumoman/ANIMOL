using System;
using System.Linq;
using System.Collections.Generic;
using Animol.UI.Pattern;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace William.NotebookUI
{
    [Serializable] public sealed class NotebookNodeView
    {
        public string stageId;
        public Button button;
        public Image star, selection, locked;
        public TMP_Text number, label;
    }
    [Serializable] public sealed class NotebookEdgeView { public string from, to; public Image image; public Sprite future, cleared; }
    [Serializable] public sealed class NotebookChapterView { public string themeId; public Button button; public TMP_Text progress, access, note; public Image locked; }
    [Serializable] public sealed class NotebookWorldArt { public string themeId; public Sprite background; public Sprite[] characters; }

    /// <summary>Single fresh screen stack. No legacy menu discovery, runtime painting, palette tween or generated UI.</summary>
    public sealed class NotebookRoot : MonoBehaviour
    {
        public TextAsset catalogJson;
        public NotebookGateway gateway;
        public AudioMixer audioMixer;
        public string bgmMixerParameter = "BGM", sfxMixerParameter = "SFX";
        public GameObject mainScreen, settingsScreen, stageScreen, modal;
        public AnimolPatternMotion patternMotion;
        public Image worldImage, characterImage, battleLock, muteIcon;
        public Sprite soundOnIcon, soundOffIcon;
        public NotebookWorldArt[] worlds;
        public Button campaignButton, battleButton, customButton, settingsButton, muteButton, profileButton;
        public Button settingsBack, mapBack, telescopeButton, launchButton, restoreButton, accountButton;
        public TMP_Text walletText, recoveryText, stageRegion, stageName, stageState, launchText, bgmValue, sfxValue;
        public Slider bgmSlider, sfxSlider;
        public Button soundToggle, hapticToggle, scrollToggle;
        public Sprite toggleOn, toggleOff, starAvailable, starCurrent, starDone, starLocked;
        public NotebookNodeView[] nodes;
        public NotebookEdgeView[] edges;
        public NotebookChapterView[] chapters;
        public ScrollRect mapScroll;
        public Image modalIcon;
        public Sprite iconLock, iconTelescope, iconCheck, iconBattle, iconCustom, iconProfile;
        public TMP_Text modalTitle, modalCopy, modalNote, modalStatus, modalPrimaryText, modalSecondaryText;
        public Button modalPrimary, modalSecondary, modalClose;

        private const string WalletKey = "WilliamNotebookUI.Wallet", PrefsKey = "WilliamNotebookUI.Preferences";
        private NotebookBmModel model;
        private NotebookPreferences preferences;
        private string selectedStage, pendingId, modalKind, purchaseTheme;
        private Action primaryAction, secondaryAction;
        private long lastTick;
        private bool started, selectionTouched;
        private int progressEpoch, modalEpoch;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Awake()
        {
            NotebookCatalog catalog = NotebookCatalog.Parse(catalogJson);
            NotebookWalletState state = null;
            try { if (PlayerPrefs.HasKey(WalletKey)) state = JsonUtility.FromJson<NotebookWalletState>(PlayerPrefs.GetString(WalletKey)); } catch (ArgumentException) { }
            model = new NotebookBmModel(catalog, state, Now);
            try { if (PlayerPrefs.HasKey(PrefsKey)) preferences = JsonUtility.FromJson<NotebookPreferences>(PlayerPrefs.GetString(PrefsKey)); } catch (ArgumentException) { }
            preferences = preferences ?? new NotebookPreferences();
            selectedStage = catalog.stages.FirstOrDefault(s => model.CanEnter(s.id) && !model.IsCleared(s.id))?.id ?? catalog.stages.FirstOrDefault()?.id;
            campaignButton.onClick.AddListener(OpenStages);
            battleButton.onClick.AddListener(OpenBattle);
            customButton.onClick.AddListener(() => { if (gateway && gateway.CanOpenCustom) gateway.OpenCustom(); else Notice("커스텀", "커스텀 모드는 게임 연결 후 이용할 수 있어요.", iconCustom); });
            settingsButton.onClick.AddListener(OpenSettings);
            profileButton.onClick.AddListener(OpenAccount);
            muteButton.onClick.AddListener(() => { preferences.sound = !preferences.sound; ApplyPreferences(); Save(); });
            settingsBack.onClick.AddListener(OpenMain); mapBack.onClick.AddListener(OpenMain);
            telescopeButton.onClick.AddListener(OpenTelescope); launchButton.onClick.AddListener(LaunchStage);
            restoreButton.onClick.AddListener(Restore); accountButton.onClick.AddListener(OpenAccount);
            soundToggle.onClick.AddListener(() => { preferences.sound = !preferences.sound; ApplyPreferences(); Save(); });
            hapticToggle.onClick.AddListener(() => { preferences.haptic = !preferences.haptic; ApplyPreferences(); Save(); });
            scrollToggle.onClick.AddListener(() => { preferences.scroll = !preferences.scroll; ApplyPreferences(); Save(); });
            bgmSlider.onValueChanged.AddListener(v => { preferences.bgm = v; ApplyPreferences(); Save(); });
            sfxSlider.onValueChanged.AddListener(v => { preferences.sfx = v; ApplyPreferences(); Save(); });
            modalPrimary.onClick.AddListener(() => primaryAction?.Invoke());
            modalSecondary.onClick.AddListener(() => secondaryAction?.Invoke()); modalClose.onClick.AddListener(CloseModal);
            foreach (NotebookNodeView view in nodes) { string id = view.stageId; view.button.onClick.AddListener(() => SelectStage(id)); }
            foreach (NotebookChapterView view in chapters) { string id = view.themeId; view.button.onClick.AddListener(() => { if (!model.Owns(id)) OpenPurchase(id); else { selectionTouched = true; selectedStage = catalog.stages.FirstOrDefault(s => s.themeId == id && model.CanEnter(s.id) && !model.IsCleared(s.id))?.id ?? catalog.stages.First(s => s.themeId == id).id; RefreshMap(); } }); }
            ApplyPreferences(); PickArtwork(); OpenMain(); started = true;
        }
        private void Start() => RefreshVerifiedProgress();
        private void Update()
        {
            if (model == null || lastTick == Now) return;
            lastTick = Now; model.Tick(lastTick); RefreshWallet();
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); else if (started) { model.Tick(Now); RefreshWallet(); RefreshVerifiedProgress(); } }
        private void OnApplicationQuit() => Save();
        private void OnDisable() { if (started) Save(); }
        private void Save()
        {
            if (model == null) return;
            PlayerPrefs.SetString(WalletKey, JsonUtility.ToJson(model.State));
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(preferences)); PlayerPrefs.Save();
        }
        private void PickArtwork()
        {
            if (worlds == null || worlds.Length == 0) return;
            NotebookWorldArt art = worlds[UnityEngine.Random.Range(0, worlds.Length)];
            worldImage.sprite = art.background;
            if (art.characters != null && art.characters.Length > 0) characterImage.sprite = art.characters[UnityEngine.Random.Range(0, art.characters.Length)];
        }
        private void ApplyPreferences()
        {
            bgmSlider.SetValueWithoutNotify(preferences.bgm); sfxSlider.SetValueWithoutNotify(preferences.sfx);
            bgmValue.text = Mathf.RoundToInt(preferences.bgm * 100) + "%"; sfxValue.text = Mathf.RoundToInt(preferences.sfx * 100) + "%";
            soundToggle.image.sprite = preferences.sound ? toggleOn : toggleOff;
            hapticToggle.image.sprite = preferences.haptic ? toggleOn : toggleOff;
            scrollToggle.image.sprite = preferences.scroll ? toggleOn : toggleOff;
            if (muteIcon) muteIcon.sprite = preferences.sound ? soundOnIcon : soundOffIcon;
            AudioListener.volume = preferences.sound ? 1 : 0;
            if (audioMixer)
            {
                audioMixer.SetFloat(bgmMixerParameter, preferences.bgm <= .0001f ? -80 : Mathf.Log10(preferences.bgm) * 20);
                audioMixer.SetFloat(sfxMixerParameter, preferences.sfx <= .0001f ? -80 : Mathf.Log10(preferences.sfx) * 20);
            }
            if (patternMotion) patternMotion.SetReducedMotion(!preferences.scroll);
            if (gateway) gateway.ApplyPreferences(preferences);
        }
        public void OpenMain() => ShowPage(mainScreen);
        public void OpenSettings() => ShowPage(settingsScreen);
        public void OpenStages()
        {
            ShowPage(stageScreen); RefreshMap(); Canvas.ForceUpdateCanvases();
            NotebookConstellationArtwork artwork = Array.Find(model.Catalog.artworks,
                a => a.stageIds != null && Array.IndexOf(a.stageIds, selectedStage) >= 0);
            NotebookStage stage = model.Catalog.FindStage(selectedStage);
            if (artwork == null || !mapScroll || !mapScroll.content || !mapScroll.viewport) return;
            float y = stage != null && stage.order == 1
                ? model.Catalog.FindTheme(stage.themeId).startY : Mathf.Max(0, artwork.y - 95);
            float max = Mathf.Max(0, model.Catalog.mapContentHeight - mapScroll.viewport.rect.height);
            mapScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, max));
        }
        private void ShowPage(GameObject target)
        {
            if (!string.IsNullOrEmpty(pendingId)) return;
            CloseModal(); mainScreen.SetActive(target == mainScreen); settingsScreen.SetActive(target == settingsScreen); stageScreen.SetActive(target == stageScreen);
        }
        private void RefreshWallet()
        {
            walletText.text = model.State.balance + " / " + model.Catalog.telescopeCapacity;
            long seconds = model.SecondsUntilRecovery(Now);
            recoveryText.text = seconds == 0 ? "가득 찼어요" : "다음 충전 " + seconds / 60 + ":" + (seconds % 60).ToString("00");
            battleLock.gameObject.SetActive(!model.BattleUnlocked());
            if (modalKind == "telescope" && string.IsNullOrEmpty(pendingId))
            {
                int amount = model.RewardAmount(Now);
                modalCopy.text = model.State.balance + " / " + model.Catalog.telescopeCapacity + "개 보유\n3분마다 1개씩 충전돼요.";
                modalNote.text = amount == 0 ? "이미 가득 찼어요." : amount < model.Catalog.rewardedAdAmount ? "남은 공간만큼 " + amount + "개를 받을 수 있어요." : "광고를 끝까지 보면 망원경 5개를 받아요.";
                modalPrimaryText.text = amount == 0 ? "가득 찼어요" : gateway && gateway.CanShowRewardedAd ? "광고 보고 +" + amount + "개" : "광고 연결 대기 중";
                modalPrimary.interactable = amount > 0 && gateway && gateway.CanShowRewardedAd;
            }
        }
        private void RefreshMap()
        {
            foreach (NotebookNodeView v in nodes)
            {
                NotebookStage s = model.Catalog.FindStage(v.stageId); bool available = model.CanEnter(s.id) && CanLaunch(s), done = model.IsCleared(s.id);
                v.star.sprite = done ? starDone : available ? (selectedStage == s.id ? starCurrent : starAvailable) : starLocked;
                v.selection.gameObject.SetActive(selectedStage == s.id);
                v.number.gameObject.SetActive(done || available); v.number.text = s.order.ToString("00");
                v.locked.gameObject.SetActive(!done && !available); v.label.text = s.label + (done ? " 완료" : "");
            }
            foreach (NotebookEdgeView e in edges) e.image.sprite = model.IsCleared(e.from) && model.IsCleared(e.to) ? e.cleared : e.future;
            foreach (NotebookChapterView v in chapters)
            {
                NotebookTheme t = model.Catalog.FindTheme(v.themeId); NotebookStage[] list = Array.FindAll(model.Catalog.stages, s => s.themeId == t.id);
                v.progress.text = list.Count(s => model.IsCleared(s.id)) + " / " + list.Length + " 클리어";
                v.access.text = model.Owns(t.id) ? "" : "구매로 해금"; v.locked.gameObject.SetActive(!model.Owns(t.id));
                if (v.note) v.note.text = model.Owns(t.id) ? "클리어한 별이 이어져 별자리를 완성해요" : "구매 후 이 지역의 스테이지를 이용할 수 있어요";
            }
            NotebookStage selected = model.Catalog.FindStage(selectedStage);
            if (selected != null)
            {
                stageRegion.text = model.Catalog.FindTheme(selected.themeId).name; stageName.text = "STAGE " + selected.label;
                stageState.text = !model.Owns(selected.themeId) ? "지역 구매가 필요해요" : string.IsNullOrEmpty(selected.sceneName) ? "이 스테이지의 게임 연결을 준비 중이에요" : !gateway || !gateway.CanApproveStageEntry ? "입장 승인 서비스가 연결되지 않았어요" : model.IsCleared(selected.id) ? "완성한 별 · 다시 도전할 수 있어요" : model.CanEnter(selected.id) ? "새로운 별을 이어보세요" : "앞 스테이지 클리어 후 열려요";
                launchText.text = !model.Owns(selected.themeId) ? "구매로 지역 열기" : CanLaunch(selected) ? "스테이지 시작 · 망원경 " + model.Catalog.stageEntryCost + "개" : "입장 준비 중";
                launchButton.interactable = string.IsNullOrEmpty(pendingId) && (!model.Owns(selected.themeId) || (model.CanEnter(selected.id) && CanLaunch(selected)));
            }
            RefreshWallet();
        }
        private void SelectStage(string id)
        {
            NotebookStage stage = model.Catalog.FindStage(id);
            if (!model.Owns(stage.themeId)) { OpenPurchase(stage.themeId); return; }
            selectionTouched = true; selectedStage = id; RefreshMap();
            if (!model.CanEnter(id)) Notice("아직 이어지지 않은 별", "앞 스테이지를 클리어하면\n다음 별이 열려요.", iconLock);
        }
        private void OpenBattle()
        {
            if (!model.BattleUnlocked()) { Notice("대전 모드 잠김", "필수 스테이지를 모두 클리어하면 열려요.\n대전에서는 모든 테마를 사용해요.", iconBattle); return; }
            if (gateway && gateway.CanOpenBattle) gateway.OpenBattle(model.Catalog.themes.Select(t => t.id).ToArray());
            else Notice("대전", "지금은 대전에 입장할 수 없어요.\n잠시 후 다시 시도해 주세요.", iconBattle);
        }
        private void OpenAccount() { if (gateway && gateway.CanLogin) gateway.Login(); else Notice("게스트 계정", "계정 연결은 게임의 로그인 기능에서 진행해요.", iconProfile); }
        private bool CanLaunch(NotebookStage stage) => stage != null && !string.IsNullOrEmpty(stage.sceneName) && gateway && gateway.CanApproveStageEntry;
        private void OpenModal(string kind, string title, string copy, Sprite icon)
        {
            if (!string.IsNullOrEmpty(pendingId)) return;
            modalEpoch++;
            modalKind = kind; modal.SetActive(true); modalTitle.text = title; modalCopy.text = copy; modalIcon.sprite = icon;
            modalNote.text = modalStatus.text = ""; modalPrimary.gameObject.SetActive(true); modalPrimary.interactable = true;
            modalSecondary.gameObject.SetActive(false); modalSecondary.interactable = true; modalClose.interactable = true;
            primaryAction = CloseModal; secondaryAction = null; patternMotion.SetModalOpen(true);
        }
        private void CloseModal()
        {
            if (!string.IsNullOrEmpty(pendingId)) return;
            modal.SetActive(false); modalKind = ""; if (patternMotion) patternMotion.SetModalOpen(false);
        }
        private void Notice(string title, string copy, Sprite icon) { OpenModal("notice", title, copy, icon); modalPrimaryText.text = "확인"; }
        private string BeginOperation()
        {
            if (!string.IsNullOrEmpty(pendingId)) return null;
            pendingId = Guid.NewGuid().ToString("N"); modalPrimary.interactable = modalSecondary.interactable = modalClose.interactable = false;
            return pendingId;
        }
        private bool EndOperation(string id)
        {
            if (!this || pendingId != id) return false;
            pendingId = null; modalPrimary.interactable = modalSecondary.interactable = modalClose.interactable = true; return true;
        }
        private void OpenPurchase(string themeId)
        {
            purchaseTheme = themeId; NotebookTheme theme = model.Catalog.FindTheme(themeId);
            OpenModal("purchase", theme.name + " 열기", "이 지역의 캠페인을 영구적으로 이용해요.\n구매 후에도 별은 차례대로 이어가요.", iconLock);
            modalNote.text = "구매해도 필수 스테이지 클리어 조건은 유지돼요.";
            modalPrimaryText.text = "가격을 확인하는 중"; modalPrimary.interactable = false;
            modalSecondary.gameObject.SetActive(true); modalSecondaryText.text = "구매 내역 복원"; secondaryAction = Restore;
            int priceEpoch = modalEpoch;
            Action<string> price = localized =>
            {
                if (!this || modalKind != "purchase" || purchaseTheme != themeId || priceEpoch != modalEpoch) return;
                modalPrimary.interactable = true;
                if (!string.IsNullOrEmpty(localized)) { modalPrimaryText.text = localized + " · 구매로 해금"; primaryAction = () => Purchase(themeId); }
                else { modalPrimaryText.text = "가격 다시 확인"; modalStatus.text = "스토어에서 가격을 불러오지 못했어요."; primaryAction = () => OpenPurchase(themeId); }
            };
            if (gateway && !string.IsNullOrEmpty(theme.productId)) gateway.GetLocalizedPrice(theme.id, theme.productId, price);
            else
            {
                modalPrimaryText.text = "구매 서비스 미연결";
                modalPrimary.interactable = false;
                modalStatus.text = "실제 상품 ID와 결제 서비스가 연결되지 않았어요.";
            }
        }
        private void Purchase(string themeId)
        {
            if (!gateway) return;
            string id = BeginOperation(); if (id == null) return;
            modalStatus.text = "구매 결과를 확인하고 있어요.";
            NotebookTheme theme = model.Catalog.FindTheme(themeId);
            gateway.Purchase(themeId, theme.productId, id, result =>
            {
                if (!EndOperation(id)) return;
                if (result != null && result.status == NotebookOperationStatus.Verified && result.themeId == themeId && model.AcceptVerifiedPurchase(themeId, true))
                { progressEpoch++; Save(); RefreshMap(); Notice("지역이 열렸어요", theme.name + "의 별을 이어가세요.", iconCheck); }
                else modalStatus.text = result?.status == NotebookOperationStatus.Cancelled ? "구매를 취소했어요." : "구매 결과를 확인하지 못했어요. 구매 내역 복원을 이용해 주세요.";
            });
        }
        private void Restore()
        {
            if (!modal.activeSelf) OpenModal("restore", "구매 내역 복원", "구매한 지역을 다시 확인해요.", iconLock);
            if (!gateway || !gateway.CanRestorePurchases) { modalStatus.text = "구매 복원 서비스가 연결되지 않았어요."; modalPrimaryText.text = "확인"; primaryAction = CloseModal; return; }
            string id = BeginOperation(); if (id == null) return;
            modalStatus.text = "구매 내역을 확인하고 있어요.";
            gateway.Restore(id, result =>
            {
                if (!EndOperation(id)) return;
                if (result != null && result.status == NotebookOperationStatus.Verified && result.ownedThemes != null)
                { progressEpoch++; foreach (string t in result.ownedThemes) model.AcceptVerifiedPurchase(t, true); Save(); RefreshMap(); Notice("구매 내역 확인", result.ownedThemes.Length > 0 ? "구매한 지역을 복원했어요." : "복원할 구매 내역이 없어요.", iconCheck); }
                else { modalStatus.text = "구매 내역을 확인하지 못했어요. 잠시 후 다시 시도해 주세요."; modalPrimaryText.text = "확인"; primaryAction = CloseModal; }
            });
        }
        private void OpenTelescope()
        {
            RefreshWallet(); int amount = model.RewardAmount(Now);
            OpenModal("telescope", "망원경 충전", model.State.balance + " / " + model.Catalog.telescopeCapacity + "개 보유\n3분마다 1개씩 충전돼요.", iconTelescope);
            modalNote.text = amount == 0 ? "이미 가득 찼어요." : amount < model.Catalog.rewardedAdAmount ? "남은 공간만큼 " + amount + "개를 받을 수 있어요." : "광고를 끝까지 보면 망원경 5개를 받아요.";
            modalPrimaryText.text = amount == 0 ? "가득 찼어요" : gateway && gateway.CanShowRewardedAd ? "광고 보고 +" + amount + "개" : "광고 연결 대기 중";
            modalPrimary.interactable = amount > 0 && gateway && gateway.CanShowRewardedAd;
            if (amount > 0 && (!gateway || !gateway.CanShowRewardedAd)) modalStatus.text = "광고 서비스가 연결되지 않았어요.";
            primaryAction = Reward;
        }
        private void Reward()
        {
            if (model.RewardAmount(Now) <= 0) { OpenTelescope(); return; }
            if (!gateway) { modalStatus.text = "지금은 광고를 불러올 수 없어요. 잠시 후 다시 시도해 주세요."; return; }
            string id = BeginOperation(); if (id == null) return;
            modalStatus.text = "광고를 불러오고 있어요.";
            gateway.RewardedAd(id, result =>
            {
                if (!EndOperation(id)) return;
                int amount;
                if (result != null && model.AcceptVerifiedReward(result.grantId, result.status == NotebookOperationStatus.Verified, Now, out amount))
                { Save(); RefreshWallet(); Notice(amount > 0 ? "망원경을 받았어요" : "망원경이 가득 찼어요", amount > 0 ? "망원경 " + amount + "개가 충전됐어요." : "최대 보유량에 도달해 더 충전되지 않았어요.", iconTelescope); }
                else modalStatus.text = result?.status == NotebookOperationStatus.Cancelled ? "시청이 완료되지 않아 망원경은 지급되지 않았어요." : "광고를 불러오지 못했어요. 다시 시도해 주세요.";
            });
        }
        private void LaunchStage()
        {
            NotebookStage stage = model.Catalog.FindStage(selectedStage);
            if (stage == null || !string.IsNullOrEmpty(pendingId)) return;
            if (!model.Owns(stage.themeId)) { OpenPurchase(stage.themeId); return; }
            if (!model.CanEnter(stage.id) || !CanLaunch(stage)) return;
            model.Tick(Now);
            if (model.State.balance < model.Catalog.stageEntryCost) { OpenTelescope(); return; }
            if (!gateway) { Notice("입장할 수 없어요", "잠시 후 다시 시도해 주세요.\n망원경은 소모되지 않았어요.", iconTelescope); return; }
            string id = BeginOperation(); launchButton.interactable = false; launchText.text = "입장 확인 중";
            gateway.RequestStageEntry(stage.id, model.Catalog.stageEntryCost, id, approved =>
            {
                if (!EndOperation(id)) return;
                bool duplicate;
                if (model.AcceptApprovedEntry(id, stage.id, approved, Now, out duplicate))
                { Save(); RefreshMap(); if (!duplicate) gateway.OpenAcceptedStage(stage.id, id); }
                else { RefreshMap(); Notice("입장할 수 없어요", "잠시 후 다시 시도해 주세요.\n망원경은 소모되지 않았어요.", iconTelescope); }
            });
        }
        public void ReportVerifiedStageClear(string stageId)
        {
            if (model.AcceptVerifiedClear(stageId, true)) { progressEpoch++; Save(); RefreshMap(); }
        }
        public void BindGateway(NotebookGateway value) { gateway = value; if (started) { ApplyPreferences(); RefreshVerifiedProgress(); } }
        private void RefreshVerifiedProgress()
        {
            if (!gateway) return;
            int epoch = ++progressEpoch; NotebookGateway provider = gateway;
            provider.ReadVerifiedProgress(snapshot =>
            {
                if (!this || epoch != progressEpoch || gateway != provider || snapshot == null || !snapshot.verified) return;
                model.State.ownedThemes = new List<string> { "starway" };
                foreach (string id in snapshot.ownedThemes ?? Array.Empty<string>()) model.AcceptVerifiedPurchase(id, true);
                model.State.clearedStageIds.Clear();
                foreach (string id in snapshot.clearedStageIds ?? Array.Empty<string>()) model.AcceptVerifiedClear(id, true);
                if (!selectionTouched)
                    selectedStage = model.Catalog.stages.FirstOrDefault(s => model.CanEnter(s.id) && !model.IsCleared(s.id))?.id ?? model.Catalog.stages.FirstOrDefault()?.id;
                Save(); RefreshMap();
            });
        }
    }
}
