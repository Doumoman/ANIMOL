using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ANIMOL.Menu
{
    // Menu presentation only. It never awards progress, purchases, ads, or matchmaking.
    public sealed class MainLobbyController : MonoBehaviour
    {
        const float HorizontalAmplitude = .02f;
        const float VerticalAmplitude = .009f;
        const float Overscan = 1.052f;
        const float DockHeight = 136f;
        public const float AppearanceInterval = 5f;
        public const float AppearanceWipeDuration = .45f;

        public MainMenuCatalog catalog;
        public MainMenuSkin skin;
        public MonoBehaviour requiredStageProgress;
        public Canvas canvas;
        public RectTransform safeRoot;
        public RectTransform frame;
        public RectTransform artArea;
        public RectTransform scenery;
        public RectTransform backgroundRect;
        public Image backgroundImage;
        public RectTransform backgroundTransitionRect;
        public Image backgroundTransitionImage;
        public RectTransform heroRect;
        public Image heroImage;
        public RectTransform heroTransitionRect;
        public Image heroTransitionImage;
        public Material diagonalRevealMaterial;
        public RectTransform diagonalLineRect;
        public Image diagonalLineImage;
        public RectTransform logoRect;
        public RectTransform dock;
        public GameObject campaignPanel;
        public GameObject modalPanel;
        public GameObject settingsControls;
        public Text modalTitle;
        public Text modalBody;
        public Text playTitle;
        public Text playNote;
        public Text battleNote;
        public Text campaignSelection;
        public Toggle reducedMotionToggle;
        public Button battleButton;
        public Button playButton;
        public Button campaignButton;
        public Button customButton;
        public Button settingsButton;
        public Button soundButton;
        public Button removeAdsButton;
        public Button campaignBackButton;
        public Button campaignContinueButton;
        public Button modalCloseButton;
        public Button[] themeButtons = Array.Empty<Button>();
        public Image[] themeCards = Array.Empty<Image>();
        public Image[] themeSurfaces = Array.Empty<Image>();
        public Image[] themeChecks = Array.Empty<Image>();
        public Image battleLockImage;

        readonly Dictionary<int, List<int>> shuffleBags = new Dictionary<int, List<int>>();
        readonly Dictionary<int, int> lastPerson = new Dictionary<int, int>();
        readonly List<int> themeBag = new List<int>();
        float motionTime;
        float appearanceTime;
        float transitionTime;
        bool transitioning;
        bool skipNextTick;
        bool reducedMotion;
        bool appPaused;
        bool focused = true;
        bool initialized;
        int screenWidth = -1, screenHeight = -1;
        Rect lastSafeArea;
        int campaignTheme;
        string mode = "campaign";
        Material activeRevealMaterial;
        Material activeHideMaterial;
        Material defaultHeroMaterial;

        public int CurrentThemeIndex { get; private set; } = -1;
        public string CurrentCharacterId { get; private set; }
        public string CurrentArtId { get; private set; }
        public string CurrentCostumeId { get; private set; }
        public bool BattleUnlocked { get; private set; }
        public bool ReducedMotion => reducedMotion;
        public float MotionTime => motionTime;
        public float AppearanceElapsed => appearanceTime;
        public bool IsAppearanceTransitioning => transitioning;
        public float AppearanceTransitionProgress => transitioning ? Mathf.Clamp01(transitionTime / AppearanceWipeDuration) : 0;
        public bool IsCampaignOpen => campaignPanel && campaignPanel.activeSelf;
        public bool IsModalOpen => modalPanel && modalPanel.activeSelf;
        public bool MotionRunning => !reducedMotion && !appPaused && focused && !IsCampaignOpen && !IsModalOpen;

        void Awake()
        {
            if (!catalog || catalog.themes == null || catalog.themes.Length != 4)
            {
                Debug.LogError("Main lobby needs the four-theme art catalog.");
                enabled = false;
                return;
            }
            playButton.onClick.AddListener(Play);
            campaignButton.onClick.AddListener(() => SelectMode("campaign"));
            customButton.onClick.AddListener(() => SelectMode("custom"));
            battleButton.onClick.AddListener(() => SelectMode("battle"));
            settingsButton.onClick.AddListener(OpenSettings);
            if (soundButton) soundButton.onClick.AddListener(OpenSettings);
            removeAdsButton.onClick.AddListener(() => OpenModal("광고 제거", "구매와 복원 서비스가 연결되지 않았습니다."));
            campaignBackButton.onClick.AddListener(CloseCampaign);
            campaignContinueButton.onClick.AddListener(() => OpenModal("스테이지 선택", "정식 캠페인 스테이지 목록이 아직 연결되지 않았습니다."));
            modalCloseButton.onClick.AddListener(CloseModal);
            reducedMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            for (var i = 0; i < themeButtons.Length; i++)
            {
                var index = i;
                themeButtons[i].onClick.AddListener(() => SelectCampaignTheme(index));
            }
            campaignPanel.SetActive(false);
            modalPanel.SetActive(false);
            if (!diagonalRevealMaterial || !diagonalRevealMaterial.HasProperty("_Progress"))
            {
                Debug.LogError("Main lobby diagonal reveal material is missing.");
                enabled = false;
                return;
            }
            var variants = new ShaderVariantCollection();
            variants.Add(new ShaderVariantCollection.ShaderVariant(diagonalRevealMaterial.shader, PassType.Normal));
            variants.Add(new ShaderVariantCollection.ShaderVariant(diagonalRevealMaterial.shader, PassType.Normal, "UNITY_UI_CLIP_RECT"));
            variants.WarmUp();
            defaultHeroMaterial = heroImage.material;
            activeRevealMaterial = new Material(diagonalRevealMaterial) { name = "Main Lobby Diagonal Reveal (Runtime)" };
            activeRevealMaterial.SetFloat("_Invert", 0);
            activeHideMaterial = new Material(diagonalRevealMaterial) { name = "Main Lobby Diagonal Hide (Runtime)" };
            activeHideMaterial.SetFloat("_Invert", 1);
            backgroundTransitionImage.material = activeRevealMaterial;
            heroTransitionImage.material = activeRevealMaterial;
            HideTransition();
        }

        void OnDestroy()
        {
            if (activeRevealMaterial) Destroy(activeRevealMaterial);
            if (activeHideMaterial) Destroy(activeHideMaterial);
        }

        void Start()
        {
            ForceLayout();
            EnterLobbyVisit();
            SelectMode("campaign");
            SelectCampaignTheme(0);
            RefreshBattleGate();
            initialized = true;
        }

        void Update()
        {
            if (!initialized) return;
            if (screenWidth != Screen.width || screenHeight != Screen.height || lastSafeArea != Screen.safeArea)
                ForceLayout();
            var elapsed = skipNextTick ? 0 : Time.unscaledDeltaTime;
            skipNextTick = false;
            AdvanceMotion(elapsed);
            RefreshDiagonalLine();
        }

        void OnApplicationPause(bool paused) { if (appPaused != paused) skipNextTick = true; appPaused = paused; RefreshDiagonalLine(); }
        void OnApplicationFocus(bool hasFocus) { if (focused != hasFocus) skipNextTick = true; focused = hasFocus; RefreshDiagonalLine(); }

        public void EnterLobbyVisit()
        {
            var theme = NextTheme();
            var personIndex = NextPerson(theme);
            var person = catalog.themes[theme].people[personIndex];
            var appearance = person.appearances[UnityEngine.Random.Range(0, person.appearances.Length)];
            PreviewAppearance(theme, person.characterId, appearance.artId);
        }

        int NextTheme()
        {
            if (themeBag.Count == 0)
            {
                for (var i = 0; i < catalog.themes.Length; i++) themeBag.Add(i);
                for (var i = themeBag.Count - 1; i > 0; i--)
                {
                    var j = UnityEngine.Random.Range(0, i + 1);
                    (themeBag[i], themeBag[j]) = (themeBag[j], themeBag[i]);
                }
                if (themeBag.Count > 1 && themeBag[themeBag.Count - 1] == CurrentThemeIndex)
                    (themeBag[0], themeBag[themeBag.Count - 1]) = (themeBag[themeBag.Count - 1], themeBag[0]);
            }
            var theme = themeBag[themeBag.Count - 1];
            themeBag.RemoveAt(themeBag.Count - 1);
            return theme;
        }

        int NextPerson(int themeIndex)
        {
            var people = catalog.themes[themeIndex].people;
            if (!shuffleBags.TryGetValue(themeIndex, out var bag) || bag.Count == 0)
            {
                bag = new List<int>();
                for (var i = 0; i < people.Length; i++) bag.Add(i);
                for (var i = bag.Count - 1; i > 0; i--)
                {
                    var j = UnityEngine.Random.Range(0, i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }
                if (bag.Count > 1 && lastPerson.TryGetValue(themeIndex, out var previous) && bag[bag.Count - 1] == previous)
                    (bag[0], bag[bag.Count - 1]) = (bag[bag.Count - 1], bag[0]);
                shuffleBags[themeIndex] = bag;
            }
            var index = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            lastPerson[themeIndex] = index;
            return index;
        }

        // QA entry point; never changes saved character identity or stage progress.
        public bool PreviewAppearance(int themeIndex, string characterId, string artId)
        {
            var person = catalog.FindPerson(themeIndex, characterId);
            var appearance = person == null ? null : Array.Find(person.appearances, item => item.artId == artId);
            if (appearance == null || !appearance.illustration || !catalog.themes[themeIndex].scenery) return false;
            CurrentThemeIndex = themeIndex;
            CurrentCharacterId = characterId;
            CurrentArtId = appearance.artId;
            CurrentCostumeId = appearance.costumeId;
            backgroundImage.sprite = catalog.themes[themeIndex].scenery;
            heroImage.sprite = appearance.illustration;
            transitioning = false;
            appearanceTime = 0;
            HideTransition();
            ForceLayout();
            return true;
        }

        void BeginNextAppearance()
        {
            var theme = NextTheme();
            var person = catalog.themes[theme].people[NextPerson(theme)];
            var appearance = person.appearances[UnityEngine.Random.Range(0, person.appearances.Length)];
            CurrentThemeIndex = theme;
            CurrentCharacterId = person.characterId;
            CurrentArtId = appearance.artId;
            CurrentCostumeId = appearance.costumeId;
            backgroundTransitionImage.sprite = catalog.themes[theme].scenery;
            heroTransitionImage.sprite = appearance.illustration;
            transitionTime = 0;
            transitioning = true;
            SetRevealProgress(0);
            heroImage.material = activeHideMaterial;
            backgroundTransitionImage.gameObject.SetActive(true);
            heroTransitionImage.gameObject.SetActive(true);
            RefreshDiagonalLine();
            FitBackground();
            FitHero();
        }

        void SetRevealProgress(float progress)
        {
            activeRevealMaterial.SetFloat("_Progress", progress);
            activeHideMaterial.SetFloat("_Progress", progress);
        }

        void HideTransition()
        {
            if (heroImage && heroImage.material == activeHideMaterial) heroImage.material = defaultHeroMaterial;
            if (backgroundTransitionImage) backgroundTransitionImage.gameObject.SetActive(false);
            if (heroTransitionImage) heroTransitionImage.gameObject.SetActive(false);
            if (diagonalLineImage) diagonalLineImage.gameObject.SetActive(false);
        }

        void RefreshDiagonalLine()
        {
            if (!diagonalLineRect || !diagonalLineImage) return;
            var visible = transitioning && MotionRunning;
            diagonalLineImage.gameObject.SetActive(visible);
            if (!visible) return;
            var progress = AppearanceTransitionProgress;
            var edge = 2f * progress;
            var first = edge <= 1f ? new Vector2(0, edge) : new Vector2(edge - 1f, 1);
            var second = edge <= 1f ? new Vector2(edge, 0) : new Vector2(1, edge - 1f);
            var canvasRect = (RectTransform)canvas.transform;
            var area = canvasRect.rect;
            var start = new Vector2(Mathf.Lerp(area.xMin, area.xMax, first.x), Mathf.Lerp(area.yMin, area.yMax, first.y));
            var end = new Vector2(Mathf.Lerp(area.xMin, area.xMax, second.x), Mathf.Lerp(area.yMin, area.yMax, second.y));
            diagonalLineRect.anchoredPosition = (start + end) * .5f;
            diagonalLineRect.sizeDelta = new Vector2(Vector2.Distance(start, end), 2.5f);
            diagonalLineRect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
        }

        void CompleteTransition()
        {
            if (!transitioning) return;
            backgroundImage.sprite = backgroundTransitionImage.sprite;
            heroImage.sprite = heroTransitionImage.sprite;
            transitioning = false;
            HideTransition();
            FitBackground();
            FitHero();
        }

        public void AdvanceAppearance(float seconds)
        {
            if (!MotionRunning || seconds <= 0) return;
            appearanceTime += seconds;
            if (transitioning)
            {
                transitionTime += seconds;
                SetRevealProgress(Mathf.Clamp01(transitionTime / AppearanceWipeDuration));
                if (transitionTime >= AppearanceWipeDuration) CompleteTransition();
                else RefreshDiagonalLine();
            }
            if (!transitioning && appearanceTime >= AppearanceInterval)
            {
                appearanceTime %= AppearanceInterval;
                BeginNextAppearance();
            }
        }

        public void ForceLayout()
        {
            if (!canvas || !safeRoot || !frame || !artArea) return;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
            if (screenWidth <= 0 || screenHeight <= 0) return;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / screenWidth, lastSafeArea.yMin / screenHeight);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / screenWidth, lastSafeArea.yMax / screenHeight);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var width = safeRoot.rect.width;
            var height = safeRoot.rect.height;
            if (width <= 0 || height <= 0) return;
            var frameWidth = width > height ? Mathf.Min(width, height * 9f / 16f) : width;
            frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameWidth);
            frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            dock.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, DockHeight);
            artArea.offsetMin = new Vector2(0, DockHeight);
            Canvas.ForceUpdateCanvases();
            if (skin)
            {
                var logoWidth = Mathf.Min(330f, Mathf.Max(120f, artArea.rect.width - 24f));
                logoRect.sizeDelta = new Vector2(logoWidth, logoWidth * 763f / 2060f + 4f);
                var title = logoRect.Find("William Paradox Title") as RectTransform;
                if (title) title.sizeDelta = new Vector2(logoWidth, logoWidth * 763f / 2060f);
            }
            FitBackground();
            FitHero();
            FitCampaignCards();
        }

        void FitBackground()
        {
            if (!backgroundImage.sprite) return;
            var area = scenery.rect.size;
            var sprite = backgroundImage.sprite.rect.size;
            if (area.x <= 0 || area.y <= 0 || sprite.x <= 0 || sprite.y <= 0) return;
            var cover = Mathf.Max(area.x / sprite.x, area.y / sprite.y) * Overscan;
            backgroundRect.sizeDelta = sprite * cover;
            if (backgroundTransitionRect) backgroundTransitionRect.sizeDelta = backgroundRect.sizeDelta;
            PositionBackground();
        }

        void FitHero()
        {
            if (!heroImage.sprite) return;
            var corners = new Vector3[4];
            logoRect.GetWorldCorners(corners);
            var logoBottom = artArea.InverseTransformPoint(corners[0]).y;
            var available = logoBottom - artArea.rect.yMin - 18f - 20f;
            FitIllustration(heroRect, heroImage.sprite, available);
            if (heroTransitionRect && heroTransitionImage.sprite)
                FitIllustration(heroTransitionRect, heroTransitionImage.sprite, available);
        }

        void FitIllustration(RectTransform target, Sprite illustration, float available)
        {
            var sprite = illustration.rect.size;
            var height = Mathf.Max(1, Mathf.Min(available, artArea.rect.width * .82f * sprite.y / sprite.x));
            target.sizeDelta = new Vector2(height * sprite.x / sprite.y, height);
            target.anchoredPosition = new Vector2(0, 20);
        }

        void FitCampaignCards()
        {
            for (var i = 0; i < themeCards.Length && i < catalog.themes.Length; i++)
            {
                var card = themeCards[i];
                if (!card || !catalog.themes[i].scenery) continue;
                card.sprite = catalog.themes[i].scenery;
                var area = ((RectTransform)card.transform.parent).rect.size;
                var sprite = card.sprite.rect.size;
                if (area.x > 0 && area.y > 0 && sprite.x > 0 && sprite.y > 0)
                    card.rectTransform.sizeDelta = sprite * Mathf.Max(area.x / sprite.x, area.y / sprite.y);
            }
        }

        void PositionBackground()
        {
            if (!backgroundRect || !scenery) return;
            backgroundRect.anchoredPosition = new Vector2(
                Mathf.Sin(motionTime * Mathf.PI * 2 / 28f) * scenery.rect.width * HorizontalAmplitude,
                Mathf.Sin(motionTime * Mathf.PI * 2 / 36f) * scenery.rect.height * VerticalAmplitude);
            if (backgroundTransitionRect) backgroundTransitionRect.anchoredPosition = backgroundRect.anchoredPosition;
        }

        public void AdvanceMotion(float seconds)
        {
            if (!MotionRunning || seconds <= 0) return;
            motionTime += seconds;
            PositionBackground();
        }

        public bool BackgroundCovers(float horizontalEnd = 0, float verticalEnd = 0)
        {
            var area = scenery.rect;
            var size = backgroundRect.rect.size;
            var offset = new Vector2(area.width * HorizontalAmplitude * Mathf.Clamp(horizontalEnd, -1, 1),
                area.height * VerticalAmplitude * Mathf.Clamp(verticalEnd, -1, 1));
            return offset.x - size.x / 2 <= area.xMin + .001f &&
                offset.x + size.x / 2 >= area.xMax - .001f &&
                offset.y - size.y / 2 <= area.yMin + .001f &&
                offset.y + size.y / 2 >= area.yMax - .001f;
        }

        public float HeroDisplayedPixels => heroRect && canvas ? heroRect.rect.height * canvas.scaleFactor : 0;

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            ANIMOL.Puzzle.StarwayRanchMotionSettings.ReducedMotion = value;
            if (value)
            {
                CompleteTransition();
                appearanceTime = 0;
                motionTime = 0;
                PositionBackground();
            }
            if (reducedMotionToggle && reducedMotionToggle.isOn != value)
                reducedMotionToggle.SetIsOnWithoutNotify(value);
        }

        public void RefreshBattleGate()
        {
            BattleUnlocked = false;
            var ids = catalog.requiredStageIds;
            var source = requiredStageProgress as IRequiredStageProgress;
            if (ids != null && ids.Length > 0 && source != null)
            {
                BattleUnlocked = true;
                foreach (var id in ids)
                    if (string.IsNullOrEmpty(id) || !source.TryGetCleared(id, out var cleared) || !cleared)
                    { BattleUnlocked = false; break; }
            }
            battleButton.interactable = BattleUnlocked;
            if (battleLockImage) battleLockImage.gameObject.SetActive(!BattleUnlocked);
            battleNote.text = BattleUnlocked ? "전체 4테마" : "클리어 후 해금";
            if (!BattleUnlocked && mode == "battle") SelectMode("campaign");
            else ApplyModeSkin();
        }

        public string[] BattleThemeIds()
        {
            if (!BattleUnlocked) return Array.Empty<string>();
            return Array.ConvertAll(catalog.themes, theme => theme.id);
        }

        public void SelectMode(string next)
        {
            if (next != "campaign" && next != "custom" && next != "battle") return;
            if (next == "battle" && !BattleUnlocked) return;
            mode = next;
            playTitle.text = next == "campaign" ? "캠페인 시작" : next == "battle" ? "대전 시작" : "커스텀 입장";
            playNote.text = next == "campaign" ? "테마 선택으로" : next == "battle" ? "서비스 연결 대기" : "방 서비스 연결 대기";
            ApplyModeSkin();
        }

        void ApplyModeSkin()
        {
            if (skin)
            {
                SetModeButton(campaignButton, mode == "campaign", false);
                SetModeButton(customButton, mode == "custom", false);
                SetModeButton(battleButton, mode == "battle", !BattleUnlocked);
                return;
            }
            campaignButton.GetComponent<Image>().color = mode == "campaign" ? Ink : Paper;
            customButton.GetComponent<Image>().color = mode == "custom" ? Ink : Paper;
            battleButton.GetComponent<Image>().color = mode == "battle" ? Ink : Paper;
            campaignButton.GetComponentInChildren<Text>().color = mode == "campaign" ? Paper : Ink;
            customButton.GetComponentInChildren<Text>().color = mode == "custom" ? Paper : Ink;
            battleButton.GetComponentInChildren<Text>().color = mode == "battle" ? Paper : Ink;
        }

        void SetModeButton(Button button, bool selected, bool locked)
        {
            if (!button) return;
            var image = button.GetComponent<Image>();
            image.sprite = locked ? skin.disabledTab : selected ? skin.selectedTab : skin.tab;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var label = button.GetComponentInChildren<Text>();
            if (label) label.color = Ink;
        }

        static readonly Color Ink = new Color(7f / 255, 28f / 255, 57f / 255);
        static readonly Color Paper = new Color(1f, 242f / 255, 211f / 255);

        public void Play()
        {
            if (mode == "campaign") { OpenCampaign(); return; }
            if (mode == "battle" && BattleUnlocked)
                OpenModal("대전", "필수 스테이지는 확인됐지만 광고·매칭 서비스가 아직 연결되지 않았습니다.");
            else if (mode == "custom")
                OpenModal("커스텀", "방 만들기와 참가 서비스가 아직 연결되지 않았습니다.");
        }

        public void OpenCampaign()
        {
            campaignPanel.SetActive(true);
            SelectCampaignTheme(campaignTheme);
            RefreshDiagonalLine();
        }

        public void CloseCampaign() { campaignPanel.SetActive(false); RefreshDiagonalLine(); }

        public void SelectCampaignTheme(int index)
        {
            if (index < 0 || index >= catalog.themes.Length) return;
            campaignTheme = index;
            campaignSelection.text = catalog.themes[index].displayName;
            for (var i = 0; i < themeButtons.Length; i++)
            {
                if (i < themeChecks.Length && themeChecks[i]) themeChecks[i].gameObject.SetActive(i == index);
                if (i < themeSurfaces.Length && themeSurfaces[i] && skin)
                    themeSurfaces[i].color = i == index ? new Color(1f, .89f, .78f) : Color.white;
                var outline = themeButtons[i].GetComponent<Outline>();
                if (outline) outline.effectColor = i == index ? new Color(1, .51f, .73f) : Ink;
            }
        }

        public void OpenSettings()
        {
            OpenModal("설정", "모션 줄이기를 켜면 배경 움직임이 멈춥니다. 효과음 설정은 아직 연결되지 않았습니다.");
            settingsControls.SetActive(true);
        }

        public void OpenModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            settingsControls.SetActive(false);
            modalPanel.SetActive(true);
            RefreshDiagonalLine();
        }

        public void CloseModal() { modalPanel.SetActive(false); RefreshDiagonalLine(); }
    }
}
