using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Menu
{
    // Menu presentation only. It never awards progress, purchases, ads, or matchmaking.
    public sealed class MainLobbyController : MonoBehaviour
    {
        const float HorizontalAmplitude = .02f;
        const float VerticalAmplitude = .009f;
        const float Overscan = 1.052f;
        const float DockHeight = 210f;

        public MainMenuCatalog catalog;
        public MonoBehaviour requiredStageProgress;
        public Canvas canvas;
        public RectTransform safeRoot;
        public RectTransform frame;
        public RectTransform artArea;
        public RectTransform scenery;
        public RectTransform backgroundRect;
        public Image backgroundImage;
        public RectTransform heroRect;
        public Image heroImage;
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
        public Button removeAdsButton;
        public Button campaignBackButton;
        public Button campaignContinueButton;
        public Button modalCloseButton;
        public Button[] themeButtons = Array.Empty<Button>();
        public Image[] themeCards = Array.Empty<Image>();

        readonly Dictionary<int, List<int>> shuffleBags = new Dictionary<int, List<int>>();
        readonly Dictionary<int, int> lastPerson = new Dictionary<int, int>();
        float motionTime;
        bool reducedMotion;
        bool appPaused;
        bool focused = true;
        bool initialized;
        int screenWidth = -1, screenHeight = -1;
        Rect lastSafeArea;
        int campaignTheme;
        string mode = "campaign";

        public int CurrentThemeIndex { get; private set; } = -1;
        public string CurrentCharacterId { get; private set; }
        public string CurrentArtId { get; private set; }
        public string CurrentCostumeId { get; private set; }
        public bool BattleUnlocked { get; private set; }
        public bool ReducedMotion => reducedMotion;
        public float MotionTime => motionTime;
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
            AdvanceMotion(Time.unscaledDeltaTime);
        }

        void OnApplicationPause(bool paused) => appPaused = paused;
        void OnApplicationFocus(bool hasFocus) => focused = hasFocus;

        public void EnterLobbyVisit()
        {
            var theme = UnityEngine.Random.Range(0, catalog.themes.Length);
            var personIndex = NextPerson(theme);
            var person = catalog.themes[theme].people[personIndex];
            var appearance = person.appearances[UnityEngine.Random.Range(0, person.appearances.Length)];
            PreviewAppearance(theme, person.characterId, appearance.artId);
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
            ForceLayout();
            return true;
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
            Canvas.ForceUpdateCanvases();
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
            PositionBackground();
        }

        void FitHero()
        {
            if (!heroImage.sprite) return;
            var corners = new Vector3[4];
            logoRect.GetWorldCorners(corners);
            var logoBottom = artArea.InverseTransformPoint(corners[0]).y;
            var available = logoBottom - artArea.rect.yMin - 18f - 20f;
            var sprite = heroImage.sprite.rect.size;
            var height = Mathf.Max(1, Mathf.Min(available, artArea.rect.width * .82f * sprite.y / sprite.x));
            heroRect.sizeDelta = new Vector2(height * sprite.x / sprite.y, height);
            heroRect.anchoredPosition = new Vector2(0, 20);
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
            if (value) { motionTime = 0; PositionBackground(); }
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
            battleNote.text = BattleUnlocked ? "전체 4테마" : "클리어 후 해금";
            if (!BattleUnlocked && mode == "battle") SelectMode("campaign");
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
            campaignButton.GetComponent<Image>().color = next == "campaign" ? Ink : Paper;
            customButton.GetComponent<Image>().color = next == "custom" ? Ink : Paper;
            battleButton.GetComponent<Image>().color = next == "battle" ? Ink : Paper;
            campaignButton.GetComponentInChildren<Text>().color = next == "campaign" ? Paper : Ink;
            customButton.GetComponentInChildren<Text>().color = next == "custom" ? Paper : Ink;
            battleButton.GetComponentInChildren<Text>().color = next == "battle" ? Paper : Ink;
        }

        static readonly Color Ink = new Color(.094f, .094f, .18f);
        static readonly Color Paper = new Color(1, .976f, .914f);

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
        }

        public void CloseCampaign() => campaignPanel.SetActive(false);

        public void SelectCampaignTheme(int index)
        {
            if (index < 0 || index >= catalog.themes.Length) return;
            campaignTheme = index;
            campaignSelection.text = catalog.themes[index].displayName;
            for (var i = 0; i < themeButtons.Length; i++)
                themeButtons[i].GetComponent<Outline>().effectColor = i == index ? new Color(1, .51f, .73f) : Ink;
        }

        public void OpenSettings()
        {
            OpenModal("설정", "배경 움직임을 줄일 수 있습니다.");
            settingsControls.SetActive(true);
        }

        public void OpenModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            settingsControls.SetActive(false);
            modalPanel.SetActive(true);
        }

        public void CloseModal() => modalPanel.SetActive(false);
    }
}
