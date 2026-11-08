using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Animol.Starway
{
    /// <summary>
    /// Dialogue presentation only. The host owns shots, puzzles, save flags and
    /// actual character animation. Uses assigned TextAsset data; no file paths.
    /// </summary>
    [AddComponentMenu("ANIMOL/Story Dialogue View")]
    public sealed class StoryDialogueView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TextAsset dialogueJson;
        [SerializeField] private bool playFirstSceneOnStart;
        [Tooltip("For a pure cutscene preview. Leave off when the host controls stage transitions.")]
        [SerializeField] private bool advanceBetweenScenes;
        [SerializeField] private bool hostControlsProgress;
        public UnityEvent AdvanceRequested = new UnityEvent();
        public UnityEvent SceneSkipRequested = new UnityEvent();

        [Header("View references")]
        [SerializeField] private CanvasGroup viewGroup;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField, HideInInspector] private TMP_Text progressText;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private StoryChronicleView chronicleView;
        [SerializeField] private TMP_Text nextText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text skipText;
        [SerializeField] private TMP_FontAsset galmuri14Font;
        [SerializeField, HideInInspector] private StoryNeonPanel[] neonPanels;
        [SerializeField] private StoryCrayonSkin crayonSkin;

        [Header("Readability")]
        [SerializeField, Min(0f)] private float charactersPerSecond = 36f;
        [SerializeField, Range(110f, 160f)] private float lineHeightPercent = 135f;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool haloEnabled = true;

        [Header("Host hooks — these do not change gameplay or save flags")]
        [SerializeField] private StoryStringEvent onSceneEntered = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onLineShown = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onShotRequested = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onCueRequested = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onEmotionRequested = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onActionRequested = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onActionConfirmed = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onSceneFinished = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onSequenceFinished = new StoryStringEvent();
        [SerializeField] private StoryStringEvent onClosed = new StoryStringEvent();

        private StoryDialogueData data;
        private int sceneIndex = -1;
        private int lineIndex = -1;
        private int revision;
        private bool isOpen;
        private bool isTyping;
        private bool sceneFinished;
        private string pendingAction = string.Empty;
        private Coroutine typing;
        private int visibleCharacterCount;
        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool hostPaused, hostActionReady = true;

        public bool IsOpen { get { return isOpen; } }
        public bool IsTyping { get { return isTyping; } }
        public TextAsset DialogueJson => dialogueJson;
        public TMP_Text BodyLabel => bodyText;
        public TMP_Text SpeakerLabel => speakerText;
        public TMP_Text ProgressLabel => progressText;
        public StoryChronicleView Chronicle => chronicleView;
        public Button SkipButton => skipButton;
        public Button ActionButton => actionButton;
        public string PendingAction { get { return pendingAction; } }
        public string CurrentSceneId { get { return CurrentScene != null ? CurrentScene.id : string.Empty; } }
        public StoryStringEvent SceneEntered { get { return onSceneEntered; } }
        public StoryStringEvent LineShown { get { return onLineShown; } }
        public StoryStringEvent ShotRequested { get { return onShotRequested; } }
        public StoryStringEvent CueRequested { get { return onCueRequested; } }
        public StoryStringEvent EmotionRequested { get { return onEmotionRequested; } }
        public StoryStringEvent ActionRequested { get { return onActionRequested; } }
        public StoryStringEvent ActionConfirmed { get { return onActionConfirmed; } }
        public StoryStringEvent SceneFinished { get { return onSceneFinished; } }
        public StoryStringEvent SequenceFinished { get { return onSequenceFinished; } }
        public StoryStringEvent Closed { get { return onClosed; } }
        public StoryDialogueLine CurrentLine
        {
            get
            {
                var scene = CurrentScene;
                return scene != null && lineIndex >= 0 && lineIndex < scene.lines.Length ? scene.lines[lineIndex] : null;
            }
        }
        private StoryDialogueScene CurrentScene
        {
            get { return data != null && sceneIndex >= 0 && sceneIndex < data.scenes.Length ? data.scenes[sceneIndex] : null; }
        }

        private void Awake()
        {
            if (viewGroup == null) viewGroup = GetComponent<CanvasGroup>();
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            if (actionButton != null) actionButton.onClick.AddListener(RequestCurrentAction);
            if (skipButton != null) skipButton.onClick.AddListener(SkipCurrentScene);
            if (chronicleView != null)
            {
                chronicleView.ContinueRequested.AddListener(OnChronicleContinue);
                chronicleView.SetReducedMotion(reducedMotion);
            }
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (galmuri14Font != null) SetFont(galmuri14Font);
            if (skipText != null) skipText.text = ">>";
            if (!isOpen) SetVisible(false);
            ApplyVisualPreferences();
        }

        private void Start()
        {
            // SetData/OpenScene may already have been called by the host's Awake.
            if (data == null && dialogueJson != null) LoadJson(dialogueJson);
            if (!isOpen && playFirstSceneOnStart && data != null) OpenScene(data.scenes[0].id);
        }

        private void LateUpdate()
        {
            ApplySafeArea();
        }

        private void OnDisable()
        {
            // Disabling this view must not leave a half-typed, actionable line.
            Close();
        }

        private void OnDestroy()
        {
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (actionButton != null) actionButton.onClick.RemoveListener(RequestCurrentAction);
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipCurrentScene);
            if (chronicleView != null) chronicleView.ContinueRequested.RemoveListener(OnChronicleContinue);
        }

        public bool LoadJson(TextAsset json)
        {
            if (json == null) return Fail("Assign a dialogue JSON TextAsset first.");
            try
            {
                return SetData(JsonUtility.FromJson<StoryDialogueData>(json.text));
            }
            catch (ArgumentException exception)
            {
                return Fail("Dialogue JSON could not be parsed: " + exception.Message);
            }
        }

        public bool SetData(StoryDialogueData newData)
        {
            if (newData == null || newData.schemaVersion != 1 || newData.scenes == null || newData.scenes.Length == 0)
                return Fail("Dialogue needs schemaVersion 1 and at least one scene.");
            var sceneIds = new HashSet<string>(StringComparer.Ordinal);
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var scene in newData.scenes)
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.id) || !sceneIds.Add(scene.id) ||
                    scene.lines == null || scene.lines.Length == 0)
                    return Fail("Every scene needs a unique ID and at least one line.");
                foreach (var line in scene.lines)
                    if (line == null || string.IsNullOrWhiteSpace(line.id) || !lineIds.Add(line.id))
                        return Fail("Every dialogue line needs a unique ID.");
            }
            bool wasOpen = isOpen;
            string replacedSceneId = CurrentSceneId;
            ResetPresentation();
            data = newData;
            sceneIndex = 0;
            lineIndex = -1;
            // Replacement is complete before notifying the host. A listener
            // may safely open a scene from the new data or replace it again.
            if (wasOpen) onClosed.Invoke(replacedSceneId);
            return true;
        }

        // Void wrapper is convenient for a UnityEvent<string> in the Inspector.
        public void ShowScene(string sceneId) { OpenScene(sceneId); }

        public bool OpenScene(string sceneId)
        {
            if (!isActiveAndEnabled) return Fail("Enable the dialogue view before opening a scene.");
            if (data == null) return Fail("LoadJson or SetData before opening a scene.");
            if (bodyText == null || speakerText == null || viewGroup == null)
                return Fail("The dialogue view is missing required UI references.");
            int found = Array.FindIndex(data.scenes, scene => string.Equals(scene.id, sceneId, StringComparison.Ordinal));
            if (found < 0) return Fail("Unknown scene ID: " + sceneId);
            StopTyping();
            CancelChronicle();
            sceneIndex = found;
            lineIndex = 0;
            sceneFinished = false;
            pendingAction = string.Empty;
            isOpen = true;
            SetVisible(true);
            ApplySafeArea();
            int ticket = ++revision;
            onSceneEntered.Invoke(CurrentScene.id);
            if (ticket != revision || !isOpen) return true;
            ShowLine();
            return true;
        }

        public void Advance()
        {
            if (hostPaused || !isOpen || sceneFinished || CurrentLine == null) return;
            // A tap during typing only reveals the current line. It never also
            // advances or confirms an action in the same input.
            if (isTyping) { RevealAll(); return; }
            if (!string.IsNullOrEmpty(pendingAction)) return;
            if (hostControlsProgress) { AdvanceRequested.Invoke(); return; }
            MoveAfterLine();
        }

        /// <summary>
        /// Skip only the current scene's remaining dialogue. Mandatory actions
        /// stop the skip and still require their separate explicit input.
        /// Intermediate lines never dispatch shot/cue/emotion callbacks.
        /// </summary>
        public void SkipCurrentScene()
        {
            if (hostPaused || !isOpen || sceneFinished || CurrentScene == null || CurrentLine == null) return;
            if (pendingAction == "play_chronicle") { chronicleView?.SkipToEnd(); return; }
            if (hostControlsProgress) { RevealAll(); SceneSkipRequested.Invoke(); return; }
            for (int index = lineIndex; index < CurrentScene.lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(CurrentScene.lines[index].action)) continue;
                if (index == lineIndex) { RevealAll(); return; }
                lineIndex = index;
                int expectedRevision = revision + 1;
                ShowLine();
                // A callback may replace/close/reopen the sequence. Do not
                // reveal a different line opened by such a callback.
                if (StillCurrent(expectedRevision)) RevealAll();
                return;
            }
            StopTyping();
            lineIndex = CurrentScene.lines.Length - 1;
            MoveAfterLine();
        }

        /// <summary>Reuse the project's assigned Galmuri14 TMP Font Asset.</summary>
        public void SetFont(TMP_FontAsset font)
        {
            if (font == null)
            {
                Debug.LogWarning("ANIMOL Story UI: assign the project's Galmuri14 TMP Font Asset. Existing fonts were preserved.", this);
                return;
            }
            galmuri14Font = font;
            var labels = new[] { speakerText, bodyText, progressText, nextText, actionText, skipText };
            foreach (var label in labels)
            {
                if (label == null) continue;
                label.font = font;
                // Galmuri14 has one regular face. Avoid synthesized bold/italic.
                label.fontStyle = FontStyles.Normal;
                label.fontWeight = FontWeight.Regular;
                label.fontSharedMaterial = font.material;
            }
            chronicleView?.SetFont(font);
        }

        private void OnChronicleContinue()
        {
            if (hostControlsProgress) onActionRequested.Invoke("play_chronicle");
            else ConfirmAction("play_chronicle");
        }
        private void CancelChronicle()
        {
            chronicleView?.Cancel();
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
        }

        public void RequestCurrentAction()
        {
            if (hostPaused || !hostActionReady || !isOpen || isTyping || sceneFinished || string.IsNullOrEmpty(pendingAction)) return;
            if (pendingAction == "play_chronicle") return;
            if (hostControlsProgress) { onActionRequested.Invoke(pendingAction); return; }
            if (pendingAction == "chapter_complete")
            {
                // This is a separate, explicit Continue button. The host hook
                // is the place to commit CHAPTER_R_COMPLETE, if appropriate.
                ConfirmAction("chapter_complete");
                return;
            }
            // In particular, block_lamp is only a request. The gameplay host
            // validates/completes the lamp action then calls ConfirmAction.
            onActionRequested.Invoke(pendingAction);
        }

        public bool ConfirmAction(string action)
        {
            if (hostPaused || !hostActionReady || !isOpen || isTyping || sceneFinished || string.IsNullOrEmpty(pendingAction) ||
                !string.Equals(action, pendingAction, StringComparison.Ordinal)) return false;
            if (action == "play_chronicle" && (chronicleView == null || !chronicleView.IsAwaitingContinue || !chronicleView.ContinueWasRequested)) return false;
            string confirmed = pendingAction;
            pendingAction = string.Empty;
            if (confirmed == "play_chronicle") chronicleView.Cancel();
            UpdateButtons();
            int ticket = ++revision;
            onActionConfirmed.Invoke(confirmed);
            // A listener may close the view or open another scene.
            if ((!hostControlsProgress || confirmed == "play_chronicle") && ticket == revision && isOpen) MoveAfterLine();
            return true;
        }

        public void ConfirmActionFromHost(string action) { ConfirmAction(action); }

        // The live project keeps its validated StorySession authoritative. Standalone preview is unchanged.
        public bool ShowHostedLine(string sceneId, string lineId, string action, bool reveal)
        {
            if (!hostControlsProgress || !isActiveAndEnabled || data == null) return false;
            int found = Array.FindIndex(data.scenes, s => s.id == sceneId);
            if (found < 0) return false;
            int line = Array.FindIndex(data.scenes[found].lines, l => l.id == lineId);
            if (line < 0) return false;
            bool changedScene = isOpen && CurrentSceneId != sceneId;
            string oldScene = CurrentSceneId;
            if (changedScene)
            {
                int finishedTicket = ++revision; onSceneFinished.Invoke(oldScene);
                if (finishedTicket != revision) return false;
            }
            bool entered = !isOpen || CurrentSceneId != sceneId;
            StopTyping(); CancelChronicle(); sceneIndex = found; lineIndex = line; isOpen = true; sceneFinished = false;
            SetVisible(true); ApplySafeArea();
            if (entered)
            {
                int enteredTicket = ++revision; onSceneEntered.Invoke(sceneId);
                if (!StillCurrent(enteredTicket)) return false;
            }
            int shownTicket = revision + 1; ShowLine();
            if (!StillCurrent(shownTicket)) return false;
            pendingAction = action ?? string.Empty;
            if (reveal) RevealAll(); else UpdateButtons();
            return true;
        }
        public void SetHostState(bool paused, bool actionReady)
        {
            hostPaused = paused; hostActionReady = actionReady;
            chronicleView?.SetPaused(paused);
            UpdateButtons();
        }
        public void SetContextLabel(string unused)
        {
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        public void OpenNextScene()
        {
            if (data == null || !sceneFinished || sceneIndex + 1 >= data.scenes.Length) return;
            OpenScene(data.scenes[sceneIndex + 1].id);
        }

        public void Replay()
        {
            if (data == null) return;
            OpenScene(CurrentScene != null ? CurrentScene.id : data.scenes[0].id);
        }

        public void Close()
        {
            bool wasOpen = isOpen;
            string closedId = CurrentSceneId;
            ResetPresentation();
            if (wasOpen) onClosed.Invoke(closedId);
        }

        private void ResetPresentation()
        {
            ++revision;
            StopTyping();
            CancelChronicle();
            isOpen = false;
            sceneFinished = false;
            lineIndex = -1;
            pendingAction = string.Empty;
            if (bodyText != null) { bodyText.text = string.Empty; bodyText.maxVisibleCharacters = int.MaxValue; }
            if (speakerText != null) speakerText.text = string.Empty;
            if (progressText != null) progressText.text = string.Empty;
            if (nextText != null) nextText.text = string.Empty;
            if (actionButton != null) actionButton.gameObject.SetActive(false);
            SetVisible(false);
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            chronicleView?.SetReducedMotion(enabled);
            crayonSkin?.SetReducedMotion(enabled);
            if (enabled && isTyping) RevealAll();
        }

        public void SetHaloEnabled(bool enabled)
        {
            haloEnabled = enabled;
            ApplyVisualPreferences();
        }

        private void ShowLine()
        {
            StopTyping();
            var line = CurrentLine;
            if (line == null) return;
            int ticket = ++revision;
            pendingAction = (line.action ?? string.Empty).Trim();
            speakerText.text = line.speaker ?? string.Empty;
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));
            bodyText.richText = true;
            bodyText.text = "<line-height=" + lineHeightPercent.ToString("0", System.Globalization.CultureInfo.InvariantCulture) +
                "%>" + (line.text ?? string.Empty) + "</line-height>";
            bodyText.maxVisibleCharacters = int.MaxValue;
            bodyText.ForceMeshUpdate(true, true);
            visibleCharacterCount = bodyText.textInfo.characterCount;
            isTyping = !reducedMotion && charactersPerSecond > 0f && visibleCharacterCount > 0;
            if (isTyping) bodyText.maxVisibleCharacters = 0;
            if (progressText != null) progressText.gameObject.SetActive(false);
            // Prepare text before asking the host to change shot/cue. Blank cue
            // values let it clear transient state; persistent effects stay host-owned.
            onLineShown.Invoke(line.id);
            if (!StillCurrent(ticket)) return;
            onShotRequested.Invoke(line.shot ?? string.Empty);
            if (!StillCurrent(ticket)) return;
            onCueRequested.Invoke(line.cue ?? string.Empty);
            if (!StillCurrent(ticket)) return;
            onEmotionRequested.Invoke(line.emotion ?? string.Empty);
            if (!StillCurrent(ticket)) return;
            if (pendingAction == "play_chronicle")
            {
                StopTyping();
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
                UpdateButtons();
                if (chronicleView == null || !chronicleView.Begin()) Fail("Missing Chronicle layer or data.");
                return;
            }
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (isTyping)
            {
                typing = StartCoroutine(TypeLine(ticket));
            }
            UpdateButtons();
        }

        private IEnumerator TypeLine(int ticket)
        {
            float visible = 0f;
            while (StillCurrent(ticket) && visible < visibleCharacterCount)
            {
                if (hostPaused) { yield return null; continue; }
                visible += Time.unscaledDeltaTime * charactersPerSecond;
                bodyText.maxVisibleCharacters = Mathf.Min(visibleCharacterCount, Mathf.FloorToInt(visible));
                yield return null;
            }
            if (!StillCurrent(ticket)) yield break;
            bodyText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            typing = null;
            UpdateButtons();
        }

        private bool StillCurrent(int ticket) { return isOpen && revision == ticket; }

        private void RevealAll()
        {
            StopTyping();
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
            UpdateButtons();
        }

        private void StopTyping()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            isTyping = false;
        }

        private void MoveAfterLine()
        {
            if (CurrentScene == null) return;
            if (lineIndex + 1 < CurrentScene.lines.Length)
            {
                lineIndex++;
                ShowLine();
                return;
            }
            sceneFinished = true;
            pendingAction = string.Empty;
            UpdateButtons();
            string completedId = CurrentScene.id;
            int ticket = ++revision;
            onSceneFinished.Invoke(completedId);
            if (!StillCurrent(ticket)) return;
            if (sceneIndex + 1 >= data.scenes.Length) onSequenceFinished.Invoke(completedId);
            else if (advanceBetweenScenes) OpenNextScene();
        }

        private void UpdateButtons()
        {
            bool hasAction = !string.IsNullOrEmpty(pendingAction);
            bool book = pendingAction == "play_chronicle";
            if (skipButton != null) skipButton.interactable = isOpen && !sceneFinished && !hostPaused;
            if (skipText != null) skipText.text = ">>";
            if (advanceButton != null) advanceButton.interactable = isOpen && !sceneFinished && !hostPaused && (isTyping || !hasAction);
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(isOpen && !sceneFinished && hasAction && !isTyping && !book);
                actionButton.interactable = isOpen && !sceneFinished && hasAction && !isTyping && !book && !hostPaused && hostActionReady;
            }
            if (actionText != null)
                actionText.text = pendingAction == "block_lamp" ? "등불 가리기" :
                    pendingAction == "chapter_complete" ? "계속" : pendingAction == "read_chronicle" ? "기록 읽기 완료" : "계속";
            if (nextText != null)
                nextText.text = !isOpen || sceneFinished || hasAction ? string.Empty : isTyping ? "눌러서 펼치기" : "다음  ›";
        }

        private void SetVisible(bool visible)
        {
            if (viewGroup == null) return;
            viewGroup.alpha = visible ? 1f : 0f;
            viewGroup.interactable = visible;
            viewGroup.blocksRaycasts = visible;
        }

        private void ApplyVisualPreferences()
        {
            if (crayonSkin == null) crayonSkin = GetComponent<StoryCrayonSkin>();
            if (crayonSkin != null)
            {
                crayonSkin.SetReducedMotion(reducedMotion);
                foreach (var old in GetComponentsInChildren<StoryNeonPanel>(true)) old.enabled = false;
                return;
            }
            if (neonPanels == null) return;
            foreach (var panel in neonPanels) if (panel != null) panel.HaloEnabled = haloEnabled;
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect screenSafeArea = Screen.safeArea;
            if (screenSafeArea == lastSafeArea && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastSafeArea = screenSafeArea;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            // Prefab root must stretch to a full-screen Canvas. Inset only the
            // dialogue controls; the art can remain full bleed behind this UI.
            safeArea.anchorMin = new Vector2(screenSafeArea.xMin / Screen.width, screenSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(screenSafeArea.xMax / Screen.width, screenSafeArea.yMax / Screen.height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private bool Fail(string message)
        {
            Debug.LogWarning("ANIMOL Story UI: " + message, this);
            return false;
        }
    }
}
