using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory
{
    /// <summary>
    /// A presenter inside the existing SC18 Canvas. It never creates an EventSystem,
    /// Canvas, font, save service, audio source or second button-feedback system.
    /// World/character cues are deliberately left to the host cue presenter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SanctuaryUGUIView : MonoBehaviour
    {
        [Header("Existing SC18 references")]
        [SerializeField] private SanctuaryStoryRunner runner;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private Button advanceButton;
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private CanvasGroup choicesGroup;
        [SerializeField] private RectTransform choicesRoot;
        [Tooltip("Dedicated styled row prefab: Button + TMP label; no navigation/game action listeners.")]
        [SerializeField] private Button choiceRowPrefab;
        [Tooltip("Optional dedicated ending banner. Do not assign the persistent map title.")]
        [SerializeField] private TMP_Text endingTitleLabel;

        [Header("Reading / layout starting values")]
        [Min(0f)] [SerializeField] private float charactersPerSecond = 25f;
        [SerializeField] private bool paginateLongLines = true;
        [Tooltip("144 design pixels at a 1080px reference width is 48 units at a 360-unit viewport. Validate devices.")]
        [Min(1f)] [SerializeField] private float minimumRowHeight = 144f;
        [Min(0f)] [SerializeField] private float rowSpacing = 18f;
        [SerializeField] private bool validateRowsInsideParent = true;
        [SerializeField] private Color forcedTextColor = new Color(1f, .44f, .47f, 1f);

        private sealed class Row
        {
            public Button button;
            public TMP_Text label;
            public Color normalColor;
            public int index;
            public bool pointerActive;
            public int pointerId;
            public bool pointerCancelled;
            public bool pointerClickValid;
            public bool pointerClickThisFrame;
            public int pointerEpoch;
            public UnityEngine.Events.UnityAction click;
        }

        private enum TransitionKind { RevealChoices, HideChoices, NamingTitle, EndingSettle }
        private sealed class Transition
        {
            public TransitionKind kind;
            public float elapsed;
            public float duration;
            public float startAlpha;
            public SanctuaryChoiceSet choices;
            public int cueToken;
        }

        private readonly List<Row> rows = new List<Row>();
        private readonly List<int> graphemeEnds = new List<int>();
        private readonly List<EventTrigger.Entry> advanceTriggerEntries = new List<EventTrigger.Entry>();
        private EventTrigger advanceTrigger;
        private bool advanceTriggerWasEnabled;
        private int inputEpoch;
        private bool advancePointerActive;
        private int advancePointerId;
        private int advancePointerEpoch;
        private bool advancePointerCancelled;
        private bool advancePointerClickThisFrame;
        private bool advancePointerClickValid;
        private SanctuaryStoryRunner subscribedRunner;
        private SanctuaryLine shownLine;
        private SanctuaryChoiceSet shownChoices;
        private Transition transition;
        private bool valid;
        private bool choicesReady;
        private bool typing;
        private float typedElements;
        private int firstBoundary;
        private int endBoundary;
        private int currentPage;
        private int totalPages;
        private int lastChoiceIndex;
        private int lastAdvanceFrame = -1;
        private bool warnedMissingTitle;
        private bool previousLineInput;
        private bool previousChoiceInput;
        private bool observedPaused;
        private bool skipPresentationDelta;
        private bool capturedTextSettings;
        private bool originalBodyRichText;
        private bool originalSpeakerRichText;
        private TextOverflowModes originalOverflow;
        private int originalVisibleCharacters;
        private int originalPageToDisplay;
        private string originalEndingText;
        private Color originalEndingColor;
        private bool originalEndingRaycast;
        private Vector2 originalChoicePivot;
        private Vector2 originalChoicePosition;
        private Vector2 originalChoiceSize;
        private float originalSpacing;
        private bool originalControlHeight;
        private bool originalExpandHeight;

        public void BindRunner(SanctuaryStoryRunner value)
        {
            Unsubscribe();
            runner = value;
            if (isActiveAndEnabled) Initialize();
        }

        private void OnEnable() { Initialize(); }

        private void Initialize()
        {
            valid = ValidateReferences();
            if (!valid) { HideAll(); return; }
            CaptureTextSettings();
            Unsubscribe();
            subscribedRunner = runner;
            subscribedRunner.SceneChanged += OnSceneChanged;
            subscribedRunner.LineChanged += OnLineChanged;
            subscribedRunner.ChoicesPresented += OnChoicesPresented;
            subscribedRunner.ChoicesDismissed += OnChoicesDismissed;
            subscribedRunner.HesitationStarted += OnHesitationStarted;
            subscribedRunner.CueRequested += OnCueRequested;
            subscribedRunner.PlaybackCompleted += OnPlaybackCompleted;
            subscribedRunner.StateChanged += OnStateChanged;
            advanceButton.onClick.AddListener(OnAdvanceClicked);
            AddAdvancePointerCancellation();
            runner.LoadChapter();
            observedPaused = runner.IsPaused;
            SynchronizeCurrentState();
        }

        private bool ValidateReferences()
        {
            if (runner == null || speakerLabel == null || bodyLabel == null || advanceButton == null ||
                dialogueGroup == null || choicesGroup == null || choicesRoot == null || choiceRowPrefab == null)
            {
                Debug.LogError("SanctuaryUGUIView: assign runner, speaker/body TMP labels, advance Button, two CanvasGroups, choicesRoot and a dedicated row prefab in SC18.", this);
                return false;
            }
            if (dialogueGroup == choicesGroup || bodyLabel.font == null || speakerLabel.font == null)
            {
                Debug.LogError("SanctuaryUGUIView: dialogue/choice groups must differ and existing Korean TMP fonts must be assigned.", this);
                return false;
            }
            if (choiceRowPrefab.GetComponentInChildren<TMP_Text>(true) == null ||
                choiceRowPrefab.onClick.GetPersistentEventCount() != 0)
            {
                Debug.LogError("SanctuaryUGUIView: the row prefab needs a TMP label and zero persistent action listeners. Reuse the existing skin/UiButtonFeedback, not a menu button with an old game action.", this);
                return false;
            }
            if (advanceButton.onClick.GetPersistentEventCount() != 0)
            {
                Debug.LogError("SanctuaryUGUIView: advanceButton must have no persistent game action listener. Disconnect the former story advance handler before binding this presenter so one tap has one owner.", this);
                return false;
            }
            if (choicesRoot.GetComponent<VerticalLayoutGroup>() == null ||
                Mathf.Abs(choicesRoot.anchorMin.y - choicesRoot.anchorMax.y) > .001f)
            {
                Debug.LogError("SanctuaryUGUIView: choicesRoot needs a VerticalLayoutGroup and fixed vertical anchors so seven rows can expand upward without shrinking targets.", this);
                return false;
            }
            return true;
        }

        private void CaptureTextSettings()
        {
            if (capturedTextSettings) return;
            capturedTextSettings = true;
            originalBodyRichText = bodyLabel.richText;
            originalSpeakerRichText = speakerLabel.richText;
            originalOverflow = bodyLabel.overflowMode;
            originalVisibleCharacters = bodyLabel.maxVisibleCharacters;
            originalPageToDisplay = bodyLabel.pageToDisplay;
            originalChoicePivot = choicesRoot.pivot;
            originalChoicePosition = choicesRoot.anchoredPosition;
            originalChoiceSize = choicesRoot.sizeDelta;
            VerticalLayoutGroup layout = choicesRoot.GetComponent<VerticalLayoutGroup>();
            originalSpacing = layout.spacing;
            originalControlHeight = layout.childControlHeight;
            originalExpandHeight = layout.childForceExpandHeight;
            if (endingTitleLabel != null)
            {
                originalEndingText = endingTitleLabel.text;
                originalEndingColor = endingTitleLabel.color;
                originalEndingRaycast = endingTitleLabel.raycastTarget;
            }
        }

        private void SynchronizeCurrentState()
        {
            HideAll();
            if (runner == null || runner.Flow == null) return;
            if (runner.Flow.CurrentScene != null) OnSceneChanged(runner.Flow.CurrentScene);
            if (runner.Flow.State == SanctuaryFlowState.Line && runner.Flow.CurrentLine != null)
                OnLineChanged(runner.Flow.CurrentLine);
            else if ((runner.Flow.State == SanctuaryFlowState.PreparingChoice || runner.Flow.State == SanctuaryFlowState.Choice) && runner.Flow.CurrentChoices != null)
                OnChoicesPresented(runner.Flow.CurrentChoices);
            else if (runner.Flow.State == SanctuaryFlowState.Cue && runner.Flow.PendingCue != null)
                OnCueRequested(runner.Flow.PendingCue, runner.Flow.PendingCueToken);
            RefreshInput();
        }

        private void Update()
        {
            if (!valid || runner == null || runner.Flow == null) return;
            RefreshInput();
            if (runner.IsPaused) return;
            if (skipPresentationDelta) { skipPresentationDelta = false; return; }
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (typing && IsCurrentLine())
            {
                typedElements += dt * charactersPerSecond;
                int boundary = firstBoundary + Mathf.FloorToInt(typedElements);
                if (boundary >= endBoundary) CompleteCurrentPage();
                else if (boundary > firstBoundary) bodyLabel.maxVisibleCharacters = graphemeEnds[boundary - 1];
            }
            TickTransition(dt);
        }

        private void LateUpdate()
        {
            // PointerUp and Button.onClick happen within one EventSystem frame. A cancelled
            // drag must not consume a later keyboard submit after the pointer has gone.
            foreach (Row row in rows)
                if (!row.pointerActive) { row.pointerClickThisFrame = false; row.pointerClickValid = false; }
            if (!advancePointerActive) advancePointerClickThisFrame = advancePointerClickValid = false;
        }

        private void OnSceneChanged(SanctuaryScene scene)
        {
            InvalidatePointerCaptures();
            transition = null;
            shownLine = null;
            shownChoices = null;
            lastChoiceIndex = 0;
            HideAll();
        }

        private void OnLineChanged(SanctuaryLine line)
        {
            if (!valid || line == null) return;
            InvalidatePointerCaptures();
            transition = null;
            HideChoicesImmediately();
            shownLine = line;
            shownChoices = null;
            speakerLabel.richText = false;
            bodyLabel.richText = false;
            speakerLabel.text = line.speaker ?? string.Empty;
            bodyLabel.text = line.text ?? string.Empty;
            bodyLabel.overflowMode = paginateLongLines ? TextOverflowModes.Page : originalOverflow;
            bodyLabel.pageToDisplay = 1;
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            SetGroup(dialogueGroup, 1f, false);
            bodyLabel.ForceMeshUpdate(true, true);
            BuildGraphemeEnds();
            totalPages = paginateLongLines ? Mathf.Max(1, bodyLabel.textInfo.pageCount) : 1;
            currentPage = 0;
            BeginPage();
            RefreshInput();
        }

        private void BuildGraphemeEnds()
        {
            graphemeEnds.Clear();
            string text = bodyLabel.text;
            int[] starts = StringInfo.ParseCombiningCharacters(text);
            int glyph = 0;
            TMP_TextInfo info = bodyLabel.textInfo;
            for (int i = 0; i < starts.Length; i++)
            {
                int end = i + 1 < starts.Length ? starts[i + 1] : text.Length;
                while (glyph < info.characterCount && info.characterInfo[glyph].index < end) glyph++;
                if (graphemeEnds.Count == 0 || graphemeEnds[graphemeEnds.Count - 1] != glyph)
                    graphemeEnds.Add(glyph);
            }
        }

        private void BeginPage()
        {
            bodyLabel.pageToDisplay = currentPage + 1;
            int firstGlyph = 0;
            int lastGlyph = bodyLabel.textInfo.characterCount;
            if (paginateLongLines && bodyLabel.textInfo.pageCount > currentPage)
            {
                TMP_PageInfo page = bodyLabel.textInfo.pageInfo[currentPage];
                firstGlyph = page.firstCharacterIndex;
                lastGlyph = page.lastCharacterIndex + 1;
            }
            firstBoundary = 0;
            while (firstBoundary < graphemeEnds.Count && graphemeEnds[firstBoundary] <= firstGlyph) firstBoundary++;
            endBoundary = firstBoundary;
            while (endBoundary < graphemeEnds.Count && graphemeEnds[endBoundary] <= lastGlyph) endBoundary++;
            typedElements = 0f;
            bodyLabel.maxVisibleCharacters = firstGlyph;
            typing = charactersPerSecond > 0f && endBoundary > firstBoundary;
            if (!typing) CompleteCurrentPage();
        }

        private void CompleteCurrentPage()
        {
            typing = false;
            if (bodyLabel == null) return;
            bodyLabel.maxVisibleCharacters = endBoundary > firstBoundary ? graphemeEnds[endBoundary - 1] : int.MaxValue;
        }

        private void OnAdvanceClicked()
        {
            if (lastAdvanceFrame == Time.frameCount || !IsCurrentLine() || runner.IsPaused || transition != null) return;
            if (advancePointerClickThisFrame && !advancePointerClickValid) return;
            advancePointerClickThisFrame = advancePointerClickValid = false;
            lastAdvanceFrame = Time.frameCount;
            if (typing) { CompleteCurrentPage(); return; }
            if (currentPage + 1 < totalPages) { currentPage++; BeginPage(); return; }
            advanceButton.interactable = false;
            runner.Advance();
        }

        private bool IsCurrentLine()
        {
            return valid && runner != null && runner.Flow != null && runner.Flow.State == SanctuaryFlowState.Line &&
                   shownLine != null && ReferenceEquals(shownLine, runner.Flow.CurrentLine);
        }

        private void OnChoicesPresented(SanctuaryChoiceSet choices)
        {
            if (!valid || choices == null || choices.options == null) return;
            InvalidatePointerCaptures();
            HideDialogue();
            shownLine = null;
            shownChoices = choices;
            choicesReady = false;
            EnsureRows(choices.options.Length);
            if (!valid) return;
            bool forced = choices.mode == "forced";
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                row.button.interactable = false;
                row.label.richText = false;
                row.label.color = row.normalColor;
                row.label.text = forced ? (i == 0 ? "실험대에 오르기" : "망설이기") : choices.options[i].label;
                row.pointerActive = row.pointerCancelled = row.pointerClickValid = row.pointerClickThisFrame = false;
            }
            if (!ExpandChoicePanel()) return;
            SetGroup(choicesGroup, forced ? 1f : 0f, false);
            transition = new Transition { kind = TransitionKind.RevealChoices, duration = Mathf.Max(.001f, choices.revealSeconds), choices = choices };
        }

        private void EnsureRows(int count)
        {
            DestroyRows();
            for (int i = 0; i < count; i++)
            {
                Button button = Instantiate(choiceRowPrefab, choicesRoot, false);
                button.name = "SanctuaryChoice_" + i;
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null || label.font == null)
                {
                    Debug.LogError("SanctuaryUGUIView: each choice row needs the existing Korean TMP font.", this);
                    valid = false; HideAll(); Destroy(button.gameObject); return;
                }
                LayoutElement layout = button.GetComponent<LayoutElement>();
                if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = minimumRowHeight;
                layout.preferredHeight = minimumRowHeight;
                layout.flexibleHeight = 0f;
                Row row = new Row { button = button, label = label, normalColor = label.color, index = i };
                row.click = () => OnChoiceClicked(row);
                button.onClick.AddListener(row.click);
                AddPointerCancellation(row);
                rows.Add(row);
            }
        }

        private void AddPointerCancellation(Row row)
        {
            EventTrigger trigger = row.button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = row.button.gameObject.AddComponent<EventTrigger>();
            trigger.enabled = true; // This is a disposable row clone, never the source prefab.
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            AddTrigger(trigger, EventTriggerType.PointerDown, data =>
            {
                var p = data as PointerEventData;
                if (p == null || p.button != PointerEventData.InputButton.Left || row.pointerActive) return;
                row.pointerActive = valid && choicesReady && transition == null && runner != null && !runner.IsPaused &&
                                    runner.Flow != null && runner.Flow.State == SanctuaryFlowState.Choice && row.button.interactable;
                row.pointerId = p.pointerId;
                row.pointerEpoch = inputEpoch;
                row.pointerCancelled = !row.pointerActive; row.pointerClickValid = false;
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, data =>
            {
                var p = data as PointerEventData;
                if (p != null && row.pointerActive && row.pointerId == p.pointerId) row.pointerCancelled = true;
            });
            AddTrigger(trigger, EventTriggerType.BeginDrag, data =>
            {
                var p = data as PointerEventData;
                if (p != null && row.pointerActive && row.pointerId == p.pointerId) row.pointerCancelled = true;
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, data =>
            {
                var p = data as PointerEventData;
                if (p == null || p.button != PointerEventData.InputButton.Left) return;
                row.pointerClickThisFrame = true;
                row.pointerClickValid = row.pointerActive && row.pointerId == p.pointerId && !row.pointerCancelled && row.pointerEpoch == inputEpoch;
                if (row.pointerId == p.pointerId) row.pointerActive = false;
            });
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => action(data));
            trigger.triggers.Add(entry);
        }

        private void AddAdvancePointerCancellation()
        {
            advanceTrigger = advanceButton.GetComponent<EventTrigger>();
            if (advanceTrigger == null)
            {
                advanceTrigger = advanceButton.gameObject.AddComponent<EventTrigger>();
                // A newly added empty trigger is disabled again when this view releases it.
                advanceTriggerWasEnabled = false;
            }
            else advanceTriggerWasEnabled = advanceTrigger.enabled;
            advanceTrigger.enabled = true;
            if (advanceTrigger.triggers == null) advanceTrigger.triggers = new List<EventTrigger.Entry>();
            AddAdvanceTrigger(EventTriggerType.PointerDown, data =>
            {
                var p = data as PointerEventData;
                if (p == null || p.button != PointerEventData.InputButton.Left || advancePointerActive) return;
                advancePointerActive = IsCurrentLine() && !runner.IsPaused && transition == null && advanceButton.interactable;
                advancePointerId = p.pointerId;
                advancePointerEpoch = inputEpoch;
                advancePointerCancelled = !advancePointerActive;
                advancePointerClickValid = false;
            });
            AddAdvanceTrigger(EventTriggerType.PointerExit, data =>
            {
                var p = data as PointerEventData;
                if (p != null && advancePointerActive && advancePointerId == p.pointerId) advancePointerCancelled = true;
            });
            AddAdvanceTrigger(EventTriggerType.BeginDrag, data =>
            {
                var p = data as PointerEventData;
                if (p != null && advancePointerActive && advancePointerId == p.pointerId) advancePointerCancelled = true;
            });
            AddAdvanceTrigger(EventTriggerType.PointerUp, data =>
            {
                var p = data as PointerEventData;
                if (p == null || p.button != PointerEventData.InputButton.Left) return;
                advancePointerClickThisFrame = true;
                advancePointerClickValid = advancePointerActive && advancePointerId == p.pointerId &&
                                           !advancePointerCancelled && advancePointerEpoch == inputEpoch;
                if (advancePointerId == p.pointerId) advancePointerActive = false;
            });
        }

        private void AddAdvanceTrigger(EventTriggerType type, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => action(data));
            advanceTrigger.triggers.Add(entry);
            advanceTriggerEntries.Add(entry);
        }

        private void InvalidatePointerCaptures()
        {
            unchecked { inputEpoch++; }
            advancePointerActive = false;
            advancePointerCancelled = true;
            advancePointerClickValid = false;
            foreach (Row row in rows)
            {
                row.pointerActive = false;
                row.pointerCancelled = true;
                row.pointerClickValid = false;
            }
        }

        private bool ExpandChoicePanel()
        {
            VerticalLayoutGroup layout = choicesRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = rowSpacing;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            float height = rows.Count * minimumRowHeight + Mathf.Max(0, rows.Count - 1) * rowSpacing + layout.padding.top + layout.padding.bottom;
            float bottom = choicesRoot.anchoredPosition.y - choicesRoot.rect.height * choicesRoot.pivot.y;
            choicesRoot.pivot = new Vector2(choicesRoot.pivot.x, 0f);
            choicesRoot.anchoredPosition = new Vector2(choicesRoot.anchoredPosition.x, bottom);
            choicesRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(choicesRoot);
            if (!validateRowsInsideParent) return true;
            RectTransform parent = choicesRoot.parent as RectTransform;
            if (parent == null) return true;
            Vector3[] corners = new Vector3[4];
            choicesRoot.GetWorldCorners(corners);
            Rect available = parent.rect;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 point = parent.InverseTransformPoint(corners[i]);
                if (point.x < available.xMin - 1f || point.x > available.xMax + 1f ||
                    point.y < available.yMin - 1f || point.y > available.yMax + 1f)
                {
                    Debug.LogError("SanctuaryUGUIView: all choice rows must fit inside the assigned safe-area parent. Expand the sheet upward and reposition the character; do not shrink targets or add hidden focus rows.", this);
                    HideChoicesImmediately(); return false;
                }
            }
            return true;
        }

        private void OnChoiceClicked(Row row)
        {
            if (!valid || !choicesReady || runner == null || runner.IsPaused || runner.Flow == null ||
                runner.Flow.State != SanctuaryFlowState.Choice || !ReferenceEquals(shownChoices, runner.Flow.CurrentChoices)) return;
            if (row.pointerClickThisFrame && !row.pointerClickValid) return;
            row.pointerClickThisFrame = row.pointerClickValid = false;
            lastChoiceIndex = row.index;
            choicesReady = false;
            RefreshInput();
            runner.Choose(row.index);
        }

        private void OnChoicesDismissed()
        {
            choicesReady = false;
            RefreshInput();
            // Keep the panel visible only until the explicit choices_hide cue or
            // HesitationStarted event performs its exit. No simultaneous dialogue.
        }

        private void OnHesitationStarted()
        {
            choicesReady = false;
            transition = new Transition { kind = TransitionKind.HideChoices, duration = .12f, startAlpha = choicesGroup.alpha, cueToken = 0 };
            RefreshInput();
        }

        private void OnCueRequested(SanctuaryCue cue, int token)
        {
            if (!valid || cue == null) return;
            HideDialogue();
            if (cue.id == "choices_hide")
            {
                choicesReady = false;
                transition = new Transition { kind = TransitionKind.HideChoices, duration = Mathf.Max(.001f, cue.duration), startAlpha = choicesGroup.alpha, cueToken = token };
            }
            else if (cue.id == "naming_title")
            {
                HideChoicesImmediately();
                if (endingTitleLabel != null)
                {
                    endingTitleLabel.text = runner.Chapter.displayNameAfter;
                    SetEndingAlpha(0f);
                }
                else if (!warnedMissingTitle)
                {
                    warnedMissingTitle = true;
                    Debug.LogWarning("SanctuaryUGUIView: naming_title has no dedicated endingTitleLabel binding. The cue will finish without a visible title; bind the existing SC18 ending banner before production.", this);
                }
                transition = new Transition { kind = TransitionKind.NamingTitle, duration = Mathf.Max(.001f, cue.duration), cueToken = token };
            }
            else if (cue.id == "ending_settle")
            {
                HideChoicesImmediately();
                if (endingTitleLabel != null)
                {
                    endingTitleLabel.text = runner.Chapter.displayNameAfter;
                    SetEndingAlpha(1f);
                }
                transition = new Transition { kind = TransitionKind.EndingSettle, duration = Mathf.Max(.001f, cue.duration), cueToken = token };
            }
            else
            {
                HideChoicesImmediately();
                transition = null; // Host owns scene_settle, path, rewind, platform and return cues.
            }
            RefreshInput();
        }

        private void TickTransition(float dt)
        {
            Transition active = transition;
            if (active == null) return;
            active.elapsed += dt;
            float t = Mathf.Clamp01(active.elapsed / active.duration);
            float eased = 1f - (1f - t) * (1f - t);
            if (active.kind == TransitionKind.RevealChoices)
            {
                if (!ReferenceEquals(active.choices, shownChoices)) { transition = null; return; }
                bool forced = active.choices.mode == "forced";
                choicesGroup.alpha = forced ? 1f : eased;
                if (forced)
                {
                    for (int i = 0; i < rows.Count; i++)
                        if (t >= (float)(i + 1) / rows.Count)
                        {
                            rows[i].label.text = active.choices.options[i].label;
                            rows[i].label.color = forcedTextColor;
                        }
                }
            }
            else if (active.kind == TransitionKind.HideChoices) choicesGroup.alpha = active.startAlpha * (1f - eased);
            else if (active.kind == TransitionKind.NamingTitle) SetEndingAlpha(eased);
            if (t < 1f) return;

            // Clear BEFORE calling back: ArmChoices/CompleteCue can synchronously
            // enter a new line, scene or UI cue and install a different transition.
            transition = null;
            if (active.kind == TransitionKind.RevealChoices)
            {
                choicesGroup.alpha = 1f;
                choicesReady = true;
                if (runner.Flow != null && ReferenceEquals(active.choices, runner.Flow.CurrentChoices)) runner.ArmChoices();
                RefreshInput();
            }
            else
            {
                if (active.kind == TransitionKind.HideChoices) SetGroup(choicesGroup, 0f, false);
                if (active.cueToken != 0 && runner.Flow != null && runner.Flow.State == SanctuaryFlowState.Cue &&
                    runner.Flow.PendingCueToken == active.cueToken) runner.CompleteCue(active.cueToken);
            }
        }

        private void RestoreChoiceFocus()
        {
            if (Application.isMobilePlatform || !choicesReady || runner.IsPaused || EventSystem.current == null || rows.Count == 0) return;
            int index = Mathf.Clamp(lastChoiceIndex, 0, rows.Count - 1);
            EventSystem.current.SetSelectedGameObject(rows[index].button.gameObject);
        }

        private void OnStateChanged()
        {
            // Button.interactable does not clear EventSystem.pointerPress. Invalidate
            // the actual capture so a pre-pause or previous-line release is rejected.
            InvalidatePointerCaptures();
            if (runner == null || runner.Flow == null) { HideAll(); return; }
            if (observedPaused && !runner.IsPaused) skipPresentationDelta = true;
            observedPaused = runner.IsPaused;
            if (runner.Flow.State == SanctuaryFlowState.Idle && runner.Flow.CurrentScene == null) { HideAll(); return; }
            if (runner.Flow.State != SanctuaryFlowState.Line) HideDialogue();
            RefreshInput();
        }

        private void OnPlaybackCompleted()
        {
            transition = null;
            HideDialogue();
            HideChoicesImmediately();
            // Keep the final name until the existing SC18 host returns/closes.
        }

        private void RefreshInput()
        {
            bool paused = runner == null || runner.IsPaused;
            bool lineInput = !paused && IsCurrentLine() && transition == null && dialogueGroup != null && dialogueGroup.alpha > .99f;
            if (advanceButton != null) advanceButton.interactable = lineInput;
            if (dialogueGroup != null) { dialogueGroup.interactable = lineInput; dialogueGroup.blocksRaycasts = lineInput; }
            bool choiceInput = !paused && valid && choicesReady && transition == null && runner.Flow != null &&
                               runner.Flow.State == SanctuaryFlowState.Choice && ReferenceEquals(shownChoices, runner.Flow.CurrentChoices);
            if (choicesGroup != null) { choicesGroup.interactable = choiceInput; choicesGroup.blocksRaycasts = choiceInput; }
            foreach (Row row in rows) if (row.button != null) row.button.interactable = choiceInput;
            if (!lineInput && !choiceInput) ClearOwnedFocus();
            if (choiceInput && !previousChoiceInput) RestoreChoiceFocus();
            if (lineInput && !previousLineInput && !Application.isMobilePlatform && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(advanceButton.gameObject);
            previousLineInput = lineInput;
            previousChoiceInput = choiceInput;
        }

        private void ClearOwnedFocus()
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject == null) return;
            GameObject selected = events.currentSelectedGameObject;
            bool owned = advanceButton != null && selected == advanceButton.gameObject;
            if (!owned) foreach (Row row in rows) if (row.button != null && selected == row.button.gameObject) { owned = true; break; }
            if (owned) events.SetSelectedGameObject(null);
        }

        private void HideDialogue()
        {
            typing = false;
            SetGroup(dialogueGroup, 0f, false);
            if (advanceButton != null) advanceButton.interactable = false;
        }

        private void HideChoicesImmediately()
        {
            choicesReady = false;
            SetGroup(choicesGroup, 0f, false);
            foreach (Row row in rows) if (row.button != null) row.button.interactable = false;
        }

        private void HideAll()
        {
            transition = null;
            HideDialogue();
            HideChoicesImmediately();
            SetEndingAlpha(0f);
            ClearOwnedFocus();
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool input)
        {
            if (group == null) return;
            group.alpha = alpha; group.interactable = input; group.blocksRaycasts = input;
        }

        private void SetEndingAlpha(float alpha)
        {
            if (endingTitleLabel == null) return;
            Color color = endingTitleLabel.color; color.a = alpha;
            endingTitleLabel.color = color;
            endingTitleLabel.raycastTarget = false;
        }

        private void DestroyRows()
        {
            foreach (Row row in rows)
                if (row.button != null)
                {
                    row.button.onClick.RemoveListener(row.click);
                    // Destroy is deferred. Disable immediately so an old row cannot
                    // participate in the new layout or receive a second click.
                    row.button.gameObject.SetActive(false);
                    Destroy(row.button.gameObject);
                }
            rows.Clear();
        }

        private void Unsubscribe()
        {
            if (subscribedRunner != null)
            {
                subscribedRunner.SceneChanged -= OnSceneChanged;
                subscribedRunner.LineChanged -= OnLineChanged;
                subscribedRunner.ChoicesPresented -= OnChoicesPresented;
                subscribedRunner.ChoicesDismissed -= OnChoicesDismissed;
                subscribedRunner.HesitationStarted -= OnHesitationStarted;
                subscribedRunner.CueRequested -= OnCueRequested;
                subscribedRunner.PlaybackCompleted -= OnPlaybackCompleted;
                subscribedRunner.StateChanged -= OnStateChanged;
            }
            subscribedRunner = null;
            if (advanceButton != null) advanceButton.onClick.RemoveListener(OnAdvanceClicked);
            if (advanceTrigger != null)
            {
                foreach (EventTrigger.Entry entry in advanceTriggerEntries) advanceTrigger.triggers.Remove(entry);
                advanceTrigger.enabled = advanceTriggerWasEnabled;
            }
            advanceTriggerEntries.Clear();
            advanceTrigger = null;
        }

        private void OnDisable()
        {
            InvalidatePointerCaptures();
            Unsubscribe();
            HideAll();
            DestroyRows();
            if (capturedTextSettings)
            {
                if (bodyLabel != null)
                {
                    bodyLabel.richText = originalBodyRichText;
                    bodyLabel.overflowMode = originalOverflow;
                    bodyLabel.maxVisibleCharacters = originalVisibleCharacters;
                    bodyLabel.pageToDisplay = originalPageToDisplay;
                }
                if (speakerLabel != null) speakerLabel.richText = originalSpeakerRichText;
                if (endingTitleLabel != null)
                {
                    endingTitleLabel.text = originalEndingText;
                    endingTitleLabel.color = originalEndingColor;
                    endingTitleLabel.raycastTarget = originalEndingRaycast;
                }
                if (choicesRoot != null)
                {
                    choicesRoot.pivot = originalChoicePivot;
                    choicesRoot.anchoredPosition = originalChoicePosition;
                    choicesRoot.sizeDelta = originalChoiceSize;
                    VerticalLayoutGroup layout = choicesRoot.GetComponent<VerticalLayoutGroup>();
                    if (layout != null)
                    {
                        layout.spacing = originalSpacing;
                        layout.childControlHeight = originalControlHeight;
                        layout.childForceExpandHeight = originalExpandHeight;
                    }
                }
            }
            shownLine = null; shownChoices = null; valid = false;
            capturedTextSettings = previousLineInput = previousChoiceInput = false;
            observedPaused = skipPresentationDelta = false;
        }
    }
}
