using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Animol.Starway
{
    /// <summary>
    /// A book remains on the host's background. Only this masked text column
    /// scrolls, rewinds and is overwritten. This component never writes saves.
    /// </summary>
    [AddComponentMenu("ANIMOL/Story Chronicle View")]
    public sealed class StoryChronicleView : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        public enum Phase { Closed, Intro, AsteranRoll, AsteranPause, RewindRewrite, RewritePause, HigherRoll, StaticPages, AwaitContinue }

        [SerializeField] private TextAsset chronicleJson;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text rowTemplate;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueText;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private Color asteranColor = new Color(245f / 255f, 243f / 255f, 233f / 255f, 1f);
        [SerializeField] private Color higherColor = new Color(205f / 255f, 219f / 255f, 243f / 255f, 1f);
        [Header("Host notifications; no save flags are changed here")]
        [SerializeField] private UnityEvent onReadThrough = new UnityEvent();
        [SerializeField] private UnityEvent onSkipped = new UnityEvent();
        [SerializeField] private UnityEvent onContinueRequested = new UnityEvent();

        private sealed class Row
        {
            public TMP_Text label;
            public RectTransform rect;
            public float offset;
            public float height;
            public bool higher;
            public TMP_Text overlap;
        }
        private readonly List<Row> rows = new List<Row>();
        private readonly HashSet<int> pointers = new HashSet<int>();
        private StoryChronicleData data;
        private Phase phase = Phase.Closed;
        private float elapsed;
        private float scrollY;
        private float contentHeight;
        private float viewportHeight;
        private float pointerStartedAt;
        private bool hasFocus = true;
        private bool applicationPaused;
        private bool layoutDirty;
        private bool hostPaused;
        private bool skipNextTick;
        public UnityEvent RewindStarted = new UnityEvent();
        public bool IsHolding => pointers.Count > 0;
        public int StaticPage => staticPage;
        public float ScrollPosition => scrollY;
        private int staticPage;
        private int revision;
        private Vector2 lastViewportSize;

        public float IntroDuration => data == null ? 0.6f : data.introDuration;
        public bool IsOpen { get { return phase != Phase.Closed; } }
        public bool IsAwaitingContinue { get { return phase == Phase.AwaitContinue; } }
        public bool WasSkipped { get; private set; }
        public bool WasReadThrough { get; private set; }
        public bool ContinueWasRequested { get; private set; }
        public Phase CurrentPhase { get { return phase; } }
        public UnityEvent ReadThrough { get { return onReadThrough; } }
        public UnityEvent Skipped { get { return onSkipped; } }
        public UnityEvent ContinueRequested { get { return onContinueRequested; } }
        private float StartY { get { return -viewportHeight - 32f; } }
        private float EndY { get { return contentHeight + 32f; } }

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (continueButton != null) continueButton.onClick.AddListener(RequestContinue);
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (!IsOpen) SetVisible(false);
        }
        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(RequestContinue);
        }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool value) { hasFocus = value; skipNextTick = true; ReleaseHold(); }
        private void OnApplicationPause(bool value) { applicationPaused = value; skipNextTick = true; ReleaseHold(); }
        private void OnRectTransformDimensionsChange() { layoutDirty = true; }

        public bool SetData(StoryChronicleData value)
        {
            if (value == null || value.schemaVersion != 1 || value.records == null || value.records.Length != 6 || value.titles == null ||
                string.IsNullOrWhiteSpace(value.titles.asteran) || string.IsNullOrWhiteSpace(value.titles.higher) ||
                !ValidPositive(value.referenceHeight) || !ValidPositive(value.scrollSpeed) || !ValidPositive(value.holdMultiplier) ||
                !ValidPositive(value.introDuration) || !ValidPositive(value.afterAsteranPause) ||
                !ValidPositive(value.rewindDuration) || !ValidPositive(value.afterRewritePause))
                return Fail("Chronicle needs schemaVersion 1, titles and at least one paired record.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in value.records)
                if (record == null || string.IsNullOrWhiteSpace(record.id) || !ids.Add(record.id) ||
                    string.IsNullOrWhiteSpace(record.asteran) || string.IsNullOrWhiteSpace(record.higher))
                    return Fail("Every chronicle record needs a unique ID and both viewpoints.");
            Cancel();
            data = value;
            return true;
        }

        private static bool ValidPositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        public void SetPaused(bool value)
        {
            if (hostPaused == value) return;
            hostPaused = value; skipNextTick = true; ReleaseHold();
        }

        public bool LoadJson(TextAsset json)
        {
            if (json == null) return Fail("Assign data/ranch_chronicle.json as a TextAsset.");
            try { return SetData(JsonUtility.FromJson<StoryChronicleData>(json.text)); }
            catch (ArgumentException exception) { return Fail(exception.Message); }
        }

        public bool Begin()
        {
            if (!isActiveAndEnabled || group == null || viewport == null || content == null || title == null || rowTemplate == null || continueButton == null)
                return Fail("Enable the chronicle and assign all UI references first.");
            if (data == null && !LoadJson(chronicleJson)) return false;
            Cancel();
            WasSkipped = false;
            WasReadThrough = false;
            ContinueWasRequested = false;
            BuildRows();
            SetVisible(true);
            Canvas.ForceUpdateCanvases();
            RebuildLayout(false);
            StartSequence();
            return true;
        }

        public void Cancel()
        {
            revision++;
            phase = Phase.Closed;
            elapsed = 0f;
            ReleaseHold();
            ContinueWasRequested = false;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            SetVisible(false);
            // Result properties remain available to host callbacks until Begin.
        }

        public void SetFont(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (var label in new[] { title, rowTemplate, hint, continueText })
                if (label != null) { label.font = font; label.fontStyle = FontStyles.Normal; label.fontWeight = FontWeight.Regular; label.fontSharedMaterial = font.material; }
            foreach (var row in rows) { row.label.font = font; row.label.fontStyle = FontStyles.Normal; row.label.fontWeight = FontWeight.Regular; row.label.fontSharedMaterial = font.material; row.overlap.font = font; row.overlap.fontSharedMaterial = font.material; }
            layoutDirty = true;
        }

        public void SetReducedMotion(bool enabled)
        {
            if (reducedMotion == enabled) return;
            reducedMotion = enabled;
            ReleaseHold();
            // Changing reading mode restarts both records; it never silently
            // marks pages that were not seen as read.
            if (IsOpen && !IsAwaitingContinue) StartSequence();
        }

        public void SkipToEnd()
        {
            if (!CanReadInput || !IsOpen || IsAwaitingContinue) return;
            WasSkipped = true;
            WasReadThrough = false;
            EnterAwaitContinue(false);
        }

        public void RequestContinue()
        {
            if (!CanReadInput || !IsAwaitingContinue || pointers.Count != 0 || ContinueWasRequested) return;
            ContinueWasRequested = true;
            int ticket = revision;
            onContinueRequested.Invoke();
            if (ticket == revision)
            {
                // A host may reject a debounced request. Keep the button usable.
                ContinueWasRequested = false; UpdateContinueButton();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanReadInput || !IsOpen || IsAwaitingContinue || eventData.button != PointerEventData.InputButton.Left) return;
            if (pointers.Count == 0) pointerStartedAt = Time.unscaledTime;
            pointers.Add(eventData.pointerId);
            UpdateContinueButton();
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            bool hadPointer = pointers.Remove(eventData.pointerId);
            UpdateContinueButton();
            if (CanReadInput && hadPointer && pointers.Count == 0 && phase == Phase.StaticPages && Time.unscaledTime - pointerStartedAt < 0.35f)
                AdvanceStaticPage();
        }
        public void OnPointerExit(PointerEventData eventData) { pointers.Remove(eventData.pointerId); UpdateContinueButton(); }
        public void OnCancel(BaseEventData eventData) { ReleaseHold(); }
        public void ReleaseHold() { pointers.Clear(); UpdateContinueButton(); }

        private void StartSequence()
        {
            revision++;
            elapsed = 0f;
            WasSkipped = false;
            WasReadThrough = false;
            ContinueWasRequested = false;
            staticPage = 0;
            ReleaseHold();
            foreach (var row in rows) row.rect.gameObject.SetActive(true);
            title.rectTransform.anchoredPosition = Vector2.zero;
            SetAllText(false);
            scrollY = StartY;
            SetScrollPosition();
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (hint != null) hint.text = reducedMotion ? "화면을 누르면 다음 문단" : "길게 누르면 빠르게";
            group.alpha = reducedMotion ? 1f : 0f;
            if (reducedMotion) { phase = Phase.StaticPages; ShowStaticPage(); }
            else phase = Phase.Intro;
        }

        private bool CanReadInput => hasFocus && !applicationPaused && !hostPaused;
        private void Update() { Tick(Time.unscaledDeltaTime); }
        public void Tick(float deltaTime)
        {
            if (!IsOpen || !CanReadInput) return;
            if (skipNextTick) { skipNextTick = false; return; }
            if (viewport != null && (layoutDirty || viewport.rect.size != lastViewportSize)) RebuildLayout(true);
            if (phase == Phase.StaticPages || phase == Phase.AwaitContinue) return;
            float multiplier = pointers.Count > 0 ? Mathf.Max(1f, data.holdMultiplier) : 1f;
            float dt = Mathf.Clamp(deltaTime, 0f, 0.05f) * multiplier;
            elapsed += dt;
            float scale = Mathf.Max(0.1f, ((RectTransform)transform).rect.height / Mathf.Max(1f, data.referenceHeight));
            float speed = Mathf.Max(1f, data.scrollSpeed) * scale;
            switch (phase)
            {
                case Phase.Intro:
                    group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, data.introDuration));
                    if (elapsed >= Mathf.Max(0f, data.introDuration)) SwitchPhase(Phase.AsteranRoll);
                    break;
                case Phase.AsteranRoll:
                    scrollY = Mathf.Min(EndY, scrollY + speed * dt);
                    SetScrollPosition();
                    if (hint != null && elapsed > 2f) hint.text = string.Empty;
                    if (scrollY >= EndY) SwitchPhase(Phase.AsteranPause);
                    break;
                case Phase.AsteranPause:
                    if (elapsed >= Mathf.Max(0f, data.afterAsteranPause)) SwitchPhase(Phase.RewindRewrite);
                    break;
                case Phase.RewindRewrite:
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, data.rewindDuration));
                    scrollY = Mathf.Lerp(EndY, StartY, t);
                    SetScrollPosition();
                    RewriteAtCenter();
                    if (t >= 1f) { SetAllText(true); SwitchPhase(Phase.RewritePause); }
                    break;
                case Phase.RewritePause:
                    if (elapsed >= Mathf.Max(0f, data.afterRewritePause)) SwitchPhase(Phase.HigherRoll);
                    break;
                case Phase.HigherRoll:
                    scrollY = Mathf.Min(EndY, scrollY + speed * dt);
                    SetScrollPosition();
                    if (scrollY >= EndY) EnterAwaitContinue(true);
                    break;
            }
        }

        private void SwitchPhase(Phase next)
        {
            phase = next; elapsed = 0f;
            if (next == Phase.RewindRewrite) RewindStarted.Invoke();
        }
        private void SetScrollPosition() { content.anchoredPosition = new Vector2(0f, scrollY); }

        private void RewriteAtCenter()
        {
            float middle = -viewportHeight * 0.5f;
            int stamp = Mathf.FloorToInt(elapsed * 20f);
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                float center = scrollY - row.offset - row.height * 0.5f;
                if (center <= middle) row.higher = true;
                bool noisy = Mathf.Abs(center - middle) < Mathf.Min(row.height * 0.65f, 140f);
                string value = row.higher ? data.records[i].higher : data.records[i].asteran;
                row.label.text = Format(noisy ? Corrupt(value, stamp + i * 3) : value);
                row.label.color = row.higher ? higherColor : asteranColor;
                row.overlap.gameObject.SetActive(noisy);
                row.overlap.text = Format(value);
                row.overlap.color = new Color(row.label.color.r, row.label.color.g, row.label.color.b, 0.28f);
                row.overlap.rectTransform.anchoredPosition = new Vector2(4f, 0f);
                row.rect.anchoredPosition = new Vector2(noisy ? Mathf.Sin(stamp * 2.1f + i) * 6f : 0f, -row.offset);
            }
            if (scrollY - 60f <= middle) { title.text = data.titles.higher; title.color = higherColor; }
        }

        private static string Corrupt(string text, int stamp)
        {
            char[] chars = text.ToCharArray();
            // Avoid TMP markup delimiters: generated noise must stay text.
            const string noise = "/_~=";
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsWhiteSpace(chars[i]) && (i + stamp) % 9 == 0) chars[i] = noise[(i + stamp) % noise.Length];
            return new string(chars);
        }

        private void SetAllText(bool higher)
        {
            title.text = higher ? data.titles.higher : data.titles.asteran;
            title.color = higher ? higherColor : asteranColor;
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].higher = higher;
                rows[i].overlap.gameObject.SetActive(false);
                rows[i].label.text = Format(higher ? data.records[i].higher : data.records[i].asteran);
                rows[i].label.color = higher ? higherColor : asteranColor;
                rows[i].rect.anchoredPosition = new Vector2(0f, -rows[i].offset);
            }
        }

        public void AdvanceStaticPage()
        {
            if (!CanReadInput || phase != Phase.StaticPages || pointers.Count != 0) return;
            staticPage++;
            if (staticPage >= rows.Count * 2) { EnterAwaitContinue(true); return; }
            ShowStaticPage();
        }

        private void ShowStaticPage()
        {
            bool higher = staticPage >= rows.Count;
            int index = staticPage % rows.Count;
            SetAllText(higher);
            content.anchoredPosition = Vector2.zero;
            title.rectTransform.anchoredPosition = new Vector2(0f, -viewportHeight * 0.17f);
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].rect.gameObject.SetActive(i == index);
                if (i == index) rows[i].rect.anchoredPosition = new Vector2(0f, -viewportHeight * 0.36f);
            }
        }

        private void EnterAwaitContinue(bool readThrough)
        {
            phase = Phase.AwaitContinue;
            group.alpha = 1f;
            elapsed = 0f;
            WasReadThrough = readThrough && !WasSkipped;
            if (hint != null) hint.text = string.Empty;
            foreach (var row in rows) row.rect.gameObject.SetActive(false);
            title.text = string.Empty;
            continueButton.gameObject.SetActive(true);
            if (continueText != null) continueText.text = "계속";
            UpdateContinueButton();
            // If a hold ended the roll, the release only releases the hold.
            // Continue remains a new button press, never a pointer-up shortcut.
            if (WasReadThrough) onReadThrough.Invoke();
            else onSkipped.Invoke();
        }

        private void UpdateContinueButton()
        {
            if (continueButton != null) continueButton.interactable = CanReadInput && phase == Phase.AwaitContinue && pointers.Count == 0 && !ContinueWasRequested;
        }

        private void BuildRows()
        {
            foreach (var row in rows) { row.rect.gameObject.SetActive(false); Destroy(row.rect.gameObject); }
            rows.Clear();
            rowTemplate.gameObject.SetActive(false);
            for (int i = 0; i < data.records.Length; i++)
            {
                TMP_Text label = Instantiate(rowTemplate, content);
                label.name = "Record_" + data.records[i].id;
                label.gameObject.SetActive(true);
                label.richText = true;
                label.fontStyle = FontStyles.Normal;
                label.raycastTarget = false;
                label.enableAutoSizing = false;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
                var overlap = Instantiate(label, label.transform);
                overlap.name = "LocalRewriteOverlap";
                overlap.rectTransform.anchorMin = Vector2.zero; overlap.rectTransform.anchorMax = Vector2.one;
                overlap.rectTransform.offsetMin = overlap.rectTransform.offsetMax = Vector2.zero;
                overlap.gameObject.SetActive(false);
                rows.Add(new Row { label = label, rect = label.rectTransform, overlap = overlap });
            }
        }

        private void RebuildLayout(bool preserve)
        {
            if (data == null || rows.Count == 0 || viewport == null) return;
            float fraction = Mathf.InverseLerp(StartY, EndY, scrollY);
            viewportHeight = Mathf.Max(1f, viewport.rect.height);
            lastViewportSize = viewport.rect.size;
            float width = Mathf.Max(1f, viewport.rect.width);
            content.sizeDelta = new Vector2(width, 0f);
            float titleHeight = Mathf.Max(title.GetPreferredValues(data.titles.asteran, width, Mathf.Infinity).y,
                title.GetPreferredValues(data.titles.higher, width, Mathf.Infinity).y) + 12f;
            title.rectTransform.sizeDelta = new Vector2(width, titleHeight);
            title.rectTransform.anchoredPosition = Vector2.zero;
            float offset = titleHeight + 50f;
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                row.offset = offset;
                // Fixed shared slots preserve line alignment during overwrite,
                // even if the two versions have different measured heights.
                row.height = Mathf.Max(row.label.GetPreferredValues(Format(data.records[i].asteran), width, Mathf.Infinity).y,
                    row.label.GetPreferredValues(Format(data.records[i].higher), width, Mathf.Infinity).y) + 12f;
                row.rect.anchorMin = row.rect.anchorMax = new Vector2(0.5f, 1f);
                row.rect.pivot = new Vector2(0.5f, 1f);
                row.rect.sizeDelta = new Vector2(width, row.height);
                row.rect.anchoredPosition = new Vector2(0f, -offset);
                offset += row.height + 85f;
            }
            contentHeight = offset + 64f;
            content.sizeDelta = new Vector2(width, contentHeight);
            scrollY = preserve ? Mathf.Lerp(StartY, EndY, fraction) : StartY;
            SetScrollPosition();
            if (phase == Phase.StaticPages) ShowStaticPage();
            layoutDirty = false;
        }

        private static string Format(string value) { return "<line-height=160%>" + value + "</line-height>"; }
        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
        private bool Fail(string message) { Debug.LogWarning("ANIMOL Chronicle: " + message, this); return false; }
    }
}
