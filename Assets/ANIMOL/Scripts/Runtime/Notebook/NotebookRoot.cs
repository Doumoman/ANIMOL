using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Animol.UI.Pattern;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
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
        public AnimolPatternMotion settingsPatternMotion, popupPatternMotion;
        public CanvasGroup mainInputGroup;
        public NotebookCampaignSkyTransition campaignSkyTransition;
        public Image worldImage, characterImage, battleLock, muteIcon;
        public Sprite soundOnIcon, soundOffIcon;
        public NotebookWorldArt[] worlds;
        public Button campaignButton, battleButton, customButton, settingsButton, muteButton, profileButton;
        public Button settingsBack, mapBack, telescopeButton, launchButton, storyReplayButton, restoreButton, accountButton;
        public RectTransform settingsPanel;
        public CanvasGroup settingsScreenGroup, settingsPanelGroup;
        public RectTransform stageSheet;
        public NotebookMapThemeNavigation mapThemeNavigation;
        public Image launchIcon;
        public Sprite mapEntryIcon, mapLockIcon;
        public TMP_Text walletText, stageRegion, stageName, bgmValue, sfxValue;
        // Kept for the existing fresh-scene builder and older serialized canvases.
        public TMP_Text recoveryText, stageState, launchText;
        public Slider bgmSlider, sfxSlider;
        public Button soundToggle, hapticToggle;
        public Sprite toggleOn, toggleOff, starAvailable, starCurrent, starDone, starLocked;
        // State (Available, Current, Done, Locked), then variant 0..2, then frame 0..3.
        public Sprite[] mapStarFrames = Array.Empty<Sprite>();
        public const int MapStarVariantCount = 3, MapStarFrameCount = 4;
        public const float MapStarFramesPerSecond = 4f;
        public NotebookNodeView[] nodes;
        public NotebookEdgeView[] edges;
        public NotebookChapterView[] chapters = Array.Empty<NotebookChapterView>();
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
        private const float MainArtworkIntervalSeconds = 5f;
        private const float MainArtworkSwipeSeconds = .42f;
        private float mainArtworkElapsed;
        private float mainArtworkSwipeElapsed, mainArtworkSwipeWidth;
        private int currentWorldIndex = -1;
        private int[] nextCharacterByWorld;
        private Image incomingWorldImage, incomingCharacterImage;
        private Vector2 worldImageRest, characterImageRest;
        private bool started, selectionTouched, mapSelectionVisible, reducedMotion, replayPending;
        private Coroutine sheetRoutine;
        private int mapEpoch;
        private NotebookNodeView spinningMapStar;
        private int spinningMapStarState, spinningMapStarVariant, mapStarFrame;
        private float mapStarElapsed;
        private bool mapStarAppPaused, mapStarAppFocused = true;
        private bool committingCampaignMap;
        private Vector2 settingsPanelRest;
        private Coroutine settingsTransition;
        private bool settingsClosing;
        public bool CampaignTransitionRunning => campaignSkyTransition && campaignSkyTransition.IsPlaying;
        public bool MapSelectionVisible => mapSelectionVisible;
        public bool IsReducedMotion => reducedMotion;
        public string SelectedStageId => selectedStage;
        public bool CanNavigateMapThemes => isActiveAndEnabled && stageScreen && stageScreen.activeInHierarchy
            && !mapSelectionVisible && !CampaignTransitionRunning && string.IsNullOrEmpty(pendingId)
            && mapStarAppFocused && !mapStarAppPaused && (!modal || !modal.activeInHierarchy);
        private int progressEpoch, modalEpoch;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Awake()
        {
            NotebookCatalog catalog = NotebookCatalog.Parse(catalogJson);
            NotebookWalletState state = null;
            try { if (PlayerPrefs.HasKey(WalletKey)) state = JsonUtility.FromJson<NotebookWalletState>(PlayerPrefs.GetString(WalletKey)); } catch (ArgumentException) { }
            model = new NotebookBmModel(catalog, state, Now, NotebookCampaignPreview.IsEnabled);
            try { if (PlayerPrefs.HasKey(PrefsKey)) preferences = JsonUtility.FromJson<NotebookPreferences>(PlayerPrefs.GetString(PrefsKey)); } catch (ArgumentException) { }
            preferences = preferences ?? new NotebookPreferences();
            // JsonUtility ignores the old scroll value; face motion is no longer a saved setting.
            selectedStage = catalog.stages.FirstOrDefault(s => model.CanEnter(s.id) && !model.IsCleared(s.id))?.id ?? catalog.stages.FirstOrDefault()?.id;
            campaignButton.onClick.AddListener(OpenCampaign);
            battleButton.onClick.AddListener(OpenBattle);
            customButton.onClick.AddListener(() => { if (gateway && gateway.CanOpenCustom) gateway.OpenCustom(); else Notice("커스텀", "커스텀 모드는 게임 연결 후 이용할 수 있어요.", iconCustom); });
            settingsButton.onClick.AddListener(OpenSettings);
            profileButton.onClick.AddListener(OpenAccount);
            muteButton.onClick.AddListener(() => { preferences.sound = !preferences.sound; ApplyPreferences(); Save(); });
            settingsBack.onClick.AddListener(OpenMain); mapBack.onClick.AddListener(OpenMain);
            telescopeButton.onClick.AddListener(OpenTelescope); launchButton.onClick.AddListener(LaunchStage);
            if (storyReplayButton) storyReplayButton.onClick.AddListener(ReplaySelectedStory);
            restoreButton.onClick.AddListener(Restore); accountButton.onClick.AddListener(OpenAccount);
            soundToggle.onClick.AddListener(() => { preferences.sound = !preferences.sound; ApplyPreferences(); Save(); });
            hapticToggle.onClick.AddListener(() => { preferences.haptic = !preferences.haptic; ApplyPreferences(); Save(); });
            bgmSlider.onValueChanged.AddListener(v => { preferences.bgm = v; ApplyPreferences(); Save(); });
            sfxSlider.onValueChanged.AddListener(v => { preferences.sfx = v; ApplyPreferences(); Save(); });
            modalPrimary.onClick.AddListener(() => primaryAction?.Invoke());
            modalSecondary.onClick.AddListener(() => secondaryAction?.Invoke()); modalClose.onClick.AddListener(CloseModal);
            foreach (NotebookNodeView view in nodes) { string id = view.stageId; view.button.onClick.AddListener(() => SelectStage(id)); }
            if (settingsPanel) settingsPanelRest = settingsPanel.anchoredPosition;
            if (mapThemeNavigation) mapThemeNavigation.Initialize(this, model.Catalog);
            ApplyPreferences(); PickArtwork(); OpenMain(); started = true;
        }
        private void OnEnable()
        {
            if (started) { HideStageSheet(); RefreshMap(); }
        }
        private void Start() => RefreshVerifiedProgress();
        private void Update()
        {
            if (model == null) return;
            UpdateMainArtwork();
            long now = Now;
            if (lastTick == now) return;
            lastTick = now; model.Tick(now); RefreshWallet();
        }
        private void OnApplicationPause(bool paused) { mapStarAppPaused = paused; if (paused) SettleMainArtworkSwipe(); if (paused && campaignSkyTransition) campaignSkyTransition.Cancel(); if (paused) SettleMapPresentation(); ResetMapStarSpin(); if (paused) Save(); else if (started) { model.Tick(Now); RefreshWallet(); RefreshVerifiedProgress(); } }
        private void OnApplicationFocus(bool focused) { mapStarAppFocused = focused; if (!focused) SettleMainArtworkSwipe(); if (!focused && campaignSkyTransition) campaignSkyTransition.Cancel(); if (!focused) SettleMapPresentation(); ResetMapStarSpin(); }
        private void OnApplicationQuit() => Save();
        private void OnDisable()
        {
            SettleMainArtworkSwipe();
            if (campaignSkyTransition) campaignSkyTransition.Cancel();
            StopSettingsTransition();
            if (mapThemeNavigation) mapThemeNavigation.HideForExit();
            ResetMapStarSpin();
            mapEpoch++; StopSheetAnimation();
            CancelReplay();
            if (started) Save();
        }
        private void Save()
        {
            if (model == null) return;
            PlayerPrefs.SetString(WalletKey, JsonUtility.ToJson(model.State));
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(preferences)); PlayerPrefs.Save();
        }
        private void PickArtwork(bool swipe = false)
        {
            if (!worldImage || !characterImage || worlds == null || worlds.Length == 0) return;
            if (nextCharacterByWorld == null || nextCharacterByWorld.Length != worlds.Length)
            {
                nextCharacterByWorld = new int[worlds.Length];
                for (int i = 0; i < worlds.Length; i++)
                    if (worlds[i]?.characters != null && worlds[i].characters.Length > 0)
                        nextCharacterByWorld[i] = UnityEngine.Random.Range(0, worlds[i].characters.Length);
            }
            int start = currentWorldIndex < 0 ? UnityEngine.Random.Range(0, worlds.Length) : (currentWorldIndex + 1) % worlds.Length;
            for (int offset = 0; offset < worlds.Length; offset++)
            {
                int index = (start + offset) % worlds.Length;
                NotebookWorldArt art = worlds[index];
                if (art == null || !art.background || art.characters == null || art.characters.Length == 0) continue;
                int characterIndex = nextCharacterByWorld[index] % art.characters.Length;
                if (!art.characters[characterIndex]) continue;
                if (swipe && !reducedMotion && mainScreen.activeInHierarchy)
                    BeginMainArtworkSwipe(art.background, art.characters[characterIndex]);
                else
                {
                    worldImage.sprite = art.background;
                    characterImage.sprite = art.characters[characterIndex];
                }
                nextCharacterByWorld[index] = (characterIndex + 1) % art.characters.Length;
                currentWorldIndex = index;
                return;
            }
        }
        private void UpdateMainArtwork()
        {
            if (!mainScreen || !mainScreen.activeInHierarchy || settingsScreen.activeSelf || stageScreen.activeSelf
                || modal.activeSelf || CampaignTransitionRunning || mapStarAppPaused || !mapStarAppFocused)
            {
                SettleMainArtworkSwipe();
                return;
            }
            mainArtworkElapsed += Time.unscaledDeltaTime;
            if (incomingWorldImage)
            {
                AdvanceMainArtworkSwipe();
                return;
            }
            if (mainArtworkElapsed < MainArtworkIntervalSeconds) return;
            mainArtworkElapsed = Mathf.Repeat(mainArtworkElapsed, MainArtworkIntervalSeconds);
            PickArtwork(true);
        }
        private void BeginMainArtworkSwipe(Sprite nextWorld, Sprite nextCharacter)
        {
            SettleMainArtworkSwipe();
            RectTransform hero = worldImage.rectTransform.parent as RectTransform;
            float width = hero ? hero.rect.width : 0f;
            if (width <= 0f || !hero.GetComponent<RectMask2D>())
            {
                worldImage.sprite = nextWorld;
                characterImage.sprite = nextCharacter;
                return;
            }
            mainArtworkSwipeWidth = width;
            mainArtworkSwipeElapsed = 0f;
            worldImageRest = worldImage.rectTransform.anchoredPosition;
            characterImageRest = characterImage.rectTransform.anchoredPosition;
            incomingWorldImage = Instantiate(worldImage, worldImage.transform.parent);
            incomingWorldImage.name = "Incoming World Artwork";
            incomingWorldImage.transform.SetSiblingIndex(worldImage.transform.GetSiblingIndex() + 1);
            incomingWorldImage.sprite = nextWorld;
            incomingWorldImage.raycastTarget = false;
            incomingWorldImage.rectTransform.anchoredPosition = worldImageRest + Vector2.right * width;
            incomingCharacterImage = Instantiate(characterImage, characterImage.transform.parent);
            incomingCharacterImage.name = "Incoming Character Illustration";
            incomingCharacterImage.sprite = nextCharacter;
            incomingCharacterImage.raycastTarget = false;
            incomingCharacterImage.rectTransform.anchoredPosition = characterImageRest + Vector2.right * width;
        }
        private void AdvanceMainArtworkSwipe()
        {
            mainArtworkSwipeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(mainArtworkSwipeElapsed / MainArtworkSwipeSeconds));
            float offset = mainArtworkSwipeWidth * t;
            worldImage.rectTransform.anchoredPosition = worldImageRest + Vector2.left * offset;
            characterImage.rectTransform.anchoredPosition = characterImageRest + Vector2.left * offset;
            incomingWorldImage.rectTransform.anchoredPosition = worldImageRest + Vector2.right * (mainArtworkSwipeWidth - offset);
            incomingCharacterImage.rectTransform.anchoredPosition = characterImageRest + Vector2.right * (mainArtworkSwipeWidth - offset);
            if (mainArtworkSwipeElapsed >= MainArtworkSwipeSeconds) SettleMainArtworkSwipe();
        }
        private void SettleMainArtworkSwipe()
        {
            if (!incomingWorldImage) return;
            worldImage.sprite = incomingWorldImage.sprite;
            characterImage.sprite = incomingCharacterImage.sprite;
            worldImage.rectTransform.anchoredPosition = worldImageRest;
            characterImage.rectTransform.anchoredPosition = characterImageRest;
            Destroy(incomingWorldImage.gameObject);
            Destroy(incomingCharacterImage.gameObject);
            incomingWorldImage = incomingCharacterImage = null;
            mainArtworkSwipeElapsed = 0f;
        }
        private void ApplyPreferences()
        {
            bgmSlider.SetValueWithoutNotify(preferences.bgm); sfxSlider.SetValueWithoutNotify(preferences.sfx);
            bgmValue.text = Mathf.RoundToInt(preferences.bgm * 100) + "%"; sfxValue.text = Mathf.RoundToInt(preferences.sfx * 100) + "%";
            soundToggle.image.sprite = preferences.sound ? toggleOn : toggleOff;
            hapticToggle.image.sprite = preferences.haptic ? toggleOn : toggleOff;
            if (muteIcon) muteIcon.sprite = preferences.sound ? soundOnIcon : soundOffIcon;
            AudioListener.volume = preferences.sound ? 1 : 0;
            if (audioMixer)
            {
                audioMixer.SetFloat(bgmMixerParameter, preferences.bgm <= .0001f ? -80 : Mathf.Log10(preferences.bgm) * 20);
                audioMixer.SetFloat(sfxMixerParameter, preferences.sfx <= .0001f ? -80 : Mathf.Log10(preferences.sfx) * 20);
            }
            if (gateway) gateway.ApplyPreferences(preferences);
        }
        public void OpenMain()
        {
            if (CampaignTransitionRunning) return;
            if (started && settingsScreen.activeSelf && settingsPanel && !reducedMotion)
            {
                if (settingsClosing || (!string.IsNullOrEmpty(pendingId) && !replayPending)) return;
                if (settingsTransition != null) StopCoroutine(settingsTransition);
                settingsTransition = null;
                settingsClosing = true;
                if (settingsScreenGroup) settingsScreenGroup.blocksRaycasts = false;
                if (mainInputGroup) { mainInputGroup.interactable = true; mainInputGroup.blocksRaycasts = true; }
                settingsTransition = StartCoroutine(AnimateSettingsClose());
                return;
            }
            ShowPage(mainScreen);
        }
        public void OpenSettings()
        {
            if (CampaignTransitionRunning) return;
            if (!string.IsNullOrEmpty(pendingId) && !replayPending) return;
            if (settingsScreen.activeSelf && !settingsClosing) return;
            StopSettingsTransition();
            ShowPage(settingsScreen);
            if (settingsPanel && !reducedMotion)
                settingsTransition = StartCoroutine(AnimateSettingsOpen());
        }
        private IEnumerator AnimateSettingsOpen()
        {
            const float duration = .22f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float lift = t < .78f
                    ? Mathf.Lerp(64f, -6f, EaseOut(Mathf.Clamp01(t / .78f)))
                    : Mathf.Lerp(-6f, 0f, Mathf.SmoothStep(0f, 1f, (t - .78f) / .22f));
                SetSettingsPanelLift(lift);
                if (settingsPanelGroup) settingsPanelGroup.alpha = Mathf.Lerp(.94f, 1f, t);
                yield return null;
            }
            SetSettingsPanelLift(0);
            if (settingsPanelGroup) settingsPanelGroup.alpha = 1;
            settingsTransition = null;
        }
        private IEnumerator AnimateSettingsClose()
        {
            const float liftDuration = .08f, dropDuration = .16f;
            float startLift = settingsPanel.anchoredPosition.y - settingsPanelRest.y;
            float startAlpha = settingsPanelGroup ? settingsPanelGroup.alpha : 1f;
            float elapsed = 0;
            while (elapsed < liftDuration + dropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed < liftDuration)
                {
                    float t = EaseOut(Mathf.Clamp01(elapsed / liftDuration));
                    SetSettingsPanelLift(Mathf.Lerp(startLift, 18f, t));
                    if (settingsPanelGroup) settingsPanelGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                }
                else
                {
                    float t = Mathf.Clamp01((elapsed - liftDuration) / dropDuration);
                    SetSettingsPanelLift(Mathf.Lerp(18f, -90f, t * t));
                    if (settingsPanelGroup) settingsPanelGroup.alpha = 1f - t;
                }
                yield return null;
            }
            settingsTransition = null;
            settingsClosing = false;
            SetSettingsPanelLift(0);
            if (settingsPanelGroup) settingsPanelGroup.alpha = 1;
            if (settingsScreenGroup) settingsScreenGroup.blocksRaycasts = true;
            ShowPage(mainScreen);
        }
        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private void SetSettingsPanelLift(float y)
        {
            if (settingsPanel) settingsPanel.anchoredPosition = settingsPanelRest + new Vector2(0f, y);
        }
        private void StopSettingsTransition()
        {
            if (settingsTransition != null) StopCoroutine(settingsTransition);
            settingsTransition = null;
            settingsClosing = false;
            SetSettingsPanelLift(0);
            if (settingsPanelGroup) settingsPanelGroup.alpha = 1;
            if (settingsScreenGroup) settingsScreenGroup.blocksRaycasts = true;
        }
        /// <summary>Only the explicit main campaign action animates; game returns still open the map directly.</summary>
        public void OpenCampaign()
        {
            if (CampaignTransitionRunning || !string.IsNullOrEmpty(pendingId)) return;
            if (!mainScreen || !mainScreen.activeSelf || settingsScreen.activeSelf || modal.activeSelf) return;
            SettleMainArtworkSwipe();
            if (!reducedMotion && mapStarAppFocused && !mapStarAppPaused && campaignSkyTransition
                && campaignSkyTransition.Play(worldImage ? worldImage.sprite : null, CommitCampaignMap, SettleCampaignMap)) return;
            OpenStages();
        }
        private void CommitCampaignMap()
        {
            if (!this || !isActiveAndEnabled) return;
            committingCampaignMap = true;
            try { OpenStages(); }
            finally { committingCampaignMap = false; }
        }
        private void SettleCampaignMap()
        {
            if (this && isActiveAndEnabled && stageScreen.activeInHierarchy && !modal.activeSelf && mapBack && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(mapBack.gameObject);
        }
        public void OpenStages()
        {
            if (CampaignTransitionRunning && !committingCampaignMap) return;
            if (!string.IsNullOrEmpty(pendingId) && !replayPending) return;
            HideStageSheetInternal(false); ShowPage(stageScreen); RefreshMap(); Canvas.ForceUpdateCanvases();
            NotebookConstellationArtwork art = Array.Find(model.Catalog.artworks, a => a.stageIds != null && Array.IndexOf(a.stageIds, selectedStage) >= 0);
            NotebookStage stage = model.Catalog.FindStage(selectedStage);
            if (art != null && mapScroll && mapScroll.content && mapScroll.viewport)
            {
                float y = stage != null && stage.order == 1 ? model.Catalog.FindTheme(stage.themeId).startY : Mathf.Max(0, art.y - 95);
                mapScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, model.Catalog.mapContentHeight - mapScroll.viewport.rect.height)));
            }
            if (mapThemeNavigation) mapThemeNavigation.ShowRail(false);
        }
        private void ShowPage(GameObject target)
        {
            if (CampaignTransitionRunning && !committingCampaignMap) return;
            if (!string.IsNullOrEmpty(pendingId) && !replayPending) return;
            if (target != mainScreen) SettleMainArtworkSwipe();
            if (target != settingsScreen) StopSettingsTransition();
            CancelReplay();
            bool openingSettings = target == settingsScreen;
            if (target != stageScreen) HideStageSheetInternal(false);
            bool leavingSettings = settingsScreen.activeSelf && !openingSettings;
            CloseModal();
            // Settings is an overlay: retain the actual main artwork underneath the plain gray scrim.
            mainScreen.SetActive(target == mainScreen || openingSettings);
            settingsScreen.SetActive(openingSettings); stageScreen.SetActive(target == stageScreen);
            if (target != stageScreen && mapThemeNavigation) mapThemeNavigation.HideForExit();
            if (profileButton) profileButton.gameObject.SetActive(!openingSettings);
            if (mainInputGroup)
            {
                mainInputGroup.interactable = !openingSettings;
                mainInputGroup.blocksRaycasts = !openingSettings;
            }
            // The panel owns its unchanged face layer; no scrolling faces cover the gray scrim.
            if (patternMotion) patternMotion.gameObject.SetActive(!openingSettings && target != stageScreen);
            if (EventSystem.current && (openingSettings || leavingSettings))
                EventSystem.current.SetSelectedGameObject(openingSettings ? settingsBack.gameObject : target == mainScreen ? settingsButton.gameObject : null);
        }
        private void RefreshWallet()
        {
            walletText.text = model.State.balance + " / " + model.Catalog.telescopeCapacity;
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
            ResetMapStarSpin();
            foreach (NotebookNodeView v in nodes)
            {
                NotebookStage s = model.Catalog.FindStage(v.stageId); bool available = model.CanEnter(s.id), done = model.IsCleared(s.id);
                bool isSelected = mapSelectionVisible && selectedStage == s.id;
                int state = done ? 2 : available ? (isSelected ? 1 : 0) : 3;
                v.star.sprite = MapStarSprite(state, MapStarVariant(s.id), 0);
                v.selection.gameObject.SetActive(mapSelectionVisible && selectedStage == s.id);
                v.number.gameObject.SetActive(done || available); v.number.text = s.order.ToString("00");
                v.locked.gameObject.SetActive(!done && !available); v.label.text = s.label;
            }
            foreach (NotebookEdgeView e in edges) e.image.sprite = model.IsCleared(e.from) && model.IsCleared(e.to) ? e.cleared : e.future;
            NotebookStage selected = model.Catalog.FindStage(selectedStage);
            if (selected != null)
            {
                stageRegion.text = model.Catalog.FindTheme(selected.themeId).name; stageName.text = selected.id;
                bool available = string.IsNullOrEmpty(pendingId);
                bool owned = model.Owns(selected.themeId), unlocked = model.CanEnter(selected.id);
                launchButton.interactable = available && (!owned || (unlocked && CanLaunch(selected)));
                if (launchIcon) launchIcon.sprite = owned && unlocked ? mapEntryIcon : mapLockIcon;
                if (storyReplayButton)
                {
                    // No disabled placeholder before a real, verified clear.
                    storyReplayButton.gameObject.SetActive(mapSelectionVisible && model.IsCleared(selected.id));
                    storyReplayButton.interactable = available && gateway;
                }
            }
            RefreshWallet();
        }
        private void SelectStage(string id)
        {
            if (CampaignTransitionRunning) return;
            if (!string.IsNullOrEmpty(pendingId) && !replayPending) return;
            CancelReplay();
            NotebookStage stage = model.Catalog.FindStage(id); if (stage == null) return;
            selectionTouched = true; selectedStage = id;
            ShowStageSheet(); RefreshMap();
        }
        public void SetReducedMotion(bool enabled)
        {
            if (enabled && CampaignTransitionRunning) campaignSkyTransition.CompleteImmediately();
            if (enabled) SettleMainArtworkSwipe();
            bool motionChanged = reducedMotion != enabled;
            reducedMotion = enabled;
            ANIMOL.UI.Crayon.CrayonUIMotion.SetGlobalReducedMotion(enabled);
            if (mapThemeNavigation) mapThemeNavigation.SetReducedMotion(enabled);
            if (enabled && settingsTransition != null)
            {
                bool wasClosing = settingsClosing;
                StopSettingsTransition();
                if (wasClosing) ShowPage(mainScreen);
            }
            if (motionChanged || enabled) ResetMapStarSpin();
            if (patternMotion) patternMotion.SetReducedMotion(enabled);
            if (settingsPatternMotion) settingsPatternMotion.SetReducedMotion(enabled);
            if (popupPatternMotion) popupPatternMotion.SetReducedMotion(enabled);
            if (enabled && sheetRoutine != null) { StopSheetAnimation(); SetSheetBottom(24); }
        }
        private void ShowStageSheet()
        {
            bool wasVisible = mapSelectionVisible; mapSelectionVisible = true;
            if (!stageSheet || wasVisible) return;
            StopSheetAnimation();
            stageSheet.gameObject.SetActive(false);
            if (mapThemeNavigation) mapThemeNavigation.HideForSelection(PresentStageSheet);
            else PresentStageSheet();
        }
        private void PresentStageSheet()
        {
            if (!isActiveAndEnabled || !mapSelectionVisible || !stageSheet || !stageScreen.activeInHierarchy) return;
            stageSheet.gameObject.SetActive(true);
            if (reducedMotion || !mapStarAppFocused || mapStarAppPaused) SetSheetBottom(24);
            else sheetRoutine = StartCoroutine(RaiseStageSheet());
        }
        private void SettleMapPresentation()
        {
            if (mapThemeNavigation) mapThemeNavigation.CompleteMotion();
            if (sheetRoutine != null) { StopSheetAnimation(); SetSheetBottom(24); }
        }
        private IEnumerator RaiseStageSheet()
        {
            float elapsed = 0; SetSheetBottom(-264);
            while (elapsed < .2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / .2f), eased = 1 - Mathf.Pow(1 - t, 3);
                SetSheetBottom(Mathf.LerpUnclamped(-264, 24, eased)); yield return null;
            }
            SetSheetBottom(24); sheetRoutine = null;
        }
        private void SetSheetBottom(float y)
        {
            if (stageSheet) stageSheet.anchoredPosition = new Vector2(0, y);
        }
        private void StopSheetAnimation()
        {
            if (sheetRoutine != null) StopCoroutine(sheetRoutine);
            sheetRoutine = null;
        }
        public void HideStageSheet() => HideStageSheetInternal(true);
        private void HideStageSheetInternal(bool restoreTabs)
        {
            ResetMapStarSpin();
            mapEpoch++; mapSelectionVisible = false; StopSheetAnimation(); SetSheetBottom(24);
            CancelReplay();
            if (stageSheet) stageSheet.gameObject.SetActive(false);
            if (mapThemeNavigation)
            {
                if (restoreTabs && stageScreen && stageScreen.activeInHierarchy) mapThemeNavigation.ShowRail(true);
                else mapThemeNavigation.HideForExit();
            }
        }
        private void CancelReplay()
        {
            if (!replayPending) return;
            replayPending = false;
            pendingId = null;
            if (modalPrimary) modalPrimary.interactable = true;
            if (modalSecondary) modalSecondary.interactable = true;
            if (modalClose) modalClose.interactable = true;
        }
        private void ReplaySelectedStory()
        {
            NotebookStage stage = model.Catalog.FindStage(selectedStage);
            if (stage == null || !mapSelectionVisible || !model.IsCleared(stage.id) || !string.IsNullOrEmpty(pendingId)) return;
            if (!gateway) { Notice("스토리", "이야기를 불러올 수 없어요.", iconProfile); return; }
            string operation = BeginOperation(); if (operation == null) return;
            int token = mapEpoch; NotebookGateway provider = gateway; string requestedStage = stage.id;
            replayPending = true; RefreshMap();
            // Story viewing never enters a puzzle and never touches the telescope ledger.
            Action<bool> completed = opened =>
            {
                if (!this || pendingId != operation) return;
                bool current = isActiveAndEnabled && token == mapEpoch && gateway == provider
                    && mapSelectionVisible && selectedStage == requestedStage && model.IsCleared(requestedStage);
                replayPending = false;
                if (!EndOperation(operation)) return;
                RefreshMap();
                if (current && !opened) Notice("스토리", "이야기를 불러올 수 없어요.", iconProfile);
            };
            try { provider.RequestStoryReplay(requestedStage, operation, completed); }
            catch (Exception) { completed(false); }
        }
        /// <summary>Stable across sessions and runtimes; never use randomized string.GetHashCode().</summary>
        public static int MapStarVariant(string stageId)
        {
            int variant = 0;
            foreach (char codeUnit in stageId ?? string.Empty) variant = (variant + codeUnit) % MapStarVariantCount;
            return variant;
        }
        private Sprite MapStarSprite(int state, int variant, int frame)
        {
            int index = (state * MapStarVariantCount + variant) * MapStarFrameCount + frame;
            if (mapStarFrames != null && index >= 0 && index < mapStarFrames.Length && mapStarFrames[index]) return mapStarFrames[index];
            // Older prefabs remain readable until their map is refreshed by the dedicated editor menu.
            return state == 2 ? starDone : state == 3 ? starLocked : state == 1 ? starCurrent : starAvailable;
        }
        private bool CanSpinMapStar()
        {
            return isActiveAndEnabled && model != null && mapSelectionVisible && !reducedMotion
                && !mapStarAppPaused && mapStarAppFocused
                && stageScreen && stageScreen.activeInHierarchy && stageSheet && stageSheet.gameObject.activeInHierarchy
                && (!modal || !modal.activeInHierarchy);
        }
        // Kept separate from the one-second wallet Update. Exactly one selected Image changes sprites.
        private void LateUpdate()
        {
            if (!CanSpinMapStar()) { ResetMapStarSpin(); return; }
            if (spinningMapStar == null)
            {
                spinningMapStar = Array.Find(nodes, view => view != null && view.stageId == selectedStage);
                if (spinningMapStar == null || !spinningMapStar.star) { spinningMapStar = null; return; }
                spinningMapStarState = model.IsCleared(selectedStage) ? 2 : model.CanEnter(selectedStage) ? 1 : 3;
                spinningMapStarVariant = MapStarVariant(selectedStage);
                mapStarElapsed = 0; mapStarFrame = 0;
                spinningMapStar.star.sprite = MapStarSprite(spinningMapStarState, spinningMapStarVariant, 0);
                return;
            }
            mapStarElapsed = Mathf.Repeat(mapStarElapsed + Time.unscaledDeltaTime, 1f);
            int frame = Mathf.Min(MapStarFrameCount - 1, Mathf.FloorToInt(mapStarElapsed * MapStarFramesPerSecond));
            if (frame == mapStarFrame) return;
            mapStarFrame = frame;
            if (spinningMapStar.star) spinningMapStar.star.sprite = MapStarSprite(spinningMapStarState, spinningMapStarVariant, frame);
        }
        private void ResetMapStarSpin()
        {
            if (spinningMapStar != null && spinningMapStar.star)
                spinningMapStar.star.sprite = MapStarSprite(spinningMapStarState, spinningMapStarVariant, 0);
            spinningMapStar = null; mapStarElapsed = 0; mapStarFrame = 0;
        }
        private void OpenBattle()
        {
            if (!model.BattleUnlocked()) { Notice("대전 모드 잠김", "필수 스테이지를 모두 클리어하면 열려요.\n대전에서는 모든 테마를 사용해요.", iconBattle); return; }
            if (gateway && gateway.CanOpenBattle) gateway.OpenBattle(model.Catalog.themes.Select(t => t.id).ToArray());
            else Notice("대전", "지금은 대전에 입장할 수 없어요.\n잠시 후 다시 시도해 주세요.", iconBattle);
        }
        private void OpenAccount() { if (gateway && gateway.CanLogin) gateway.Login(); else Notice("게스트 계정", "계정 연결은 게임의 로그인 기능에서 진행해요.", iconProfile); }
        private bool CanLaunch(NotebookStage stage) => stage != null && gateway &&
            (gateway.CanOpenStage(stage.id) || (!string.IsNullOrEmpty(stage.sceneName) && gateway.CanApproveStageEntry));
        private void OpenModal(string kind, string title, string copy, Sprite icon)
        {
            if (CampaignTransitionRunning) return;
            if (!string.IsNullOrEmpty(pendingId) && !replayPending) return;
            CancelReplay();
            ResetMapStarSpin();
            modalEpoch++;
            modalKind = kind; modal.SetActive(true); modalTitle.text = title; modalCopy.text = copy; modalIcon.sprite = icon;
            modalNote.text = modalStatus.text = ""; modalPrimary.gameObject.SetActive(true); modalPrimary.interactable = true;
            modalSecondary.gameObject.SetActive(false); modalSecondary.interactable = true; modalClose.interactable = true;
            primaryAction = CloseModal; secondaryAction = null;
            if (patternMotion) { patternMotion.SetModalOpen(true); patternMotion.gameObject.SetActive(false); }
            if (settingsPatternMotion) settingsPatternMotion.SetModalOpen(true);
            if (popupPatternMotion) popupPatternMotion.SetModalOpen(false);
        }
        private void CloseModal()
        {
            if (!string.IsNullOrEmpty(pendingId)) return;
            modal.SetActive(false); modalKind = "";
            if (patternMotion) { patternMotion.SetModalOpen(false); patternMotion.gameObject.SetActive(!settingsScreen.activeSelf && !stageScreen.activeSelf); }
            if (settingsPatternMotion) settingsPatternMotion.SetModalOpen(false);
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
            string id = BeginOperation(); RefreshMap();
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
        public void BindGateway(NotebookGateway value) { CancelReplay(); gateway = value; if (started) { ApplyPreferences(); RefreshVerifiedProgress(); } }
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
