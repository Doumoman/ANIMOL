using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace WilliamParadox.Story
{
    [Serializable] public sealed class StoryStringEvent : UnityEvent<string> { }

    /// <summary>Standalone, manually advanced STORY preview. It never resolves a live puzzle or grants rewards.</summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        public TextAsset storyJson;
        public TextAsset presentationJson;
        public string defaultResourcePath = "WilliamParadox/V14/Story/ranch_story";
        public string presentationResourcePath = "WilliamParadox/V14/Story/story_presentation";
        public StoryView view;
        public bool restoreOnStart = true;
        public bool persistProgress = true;
        public bool reducedMotion;
        [Tooltip("Legacy ranch behavior. The theater settles visual effects and advances with one accepted next input.")]
        public bool finishLegacyCueBeforeAdvancing = true;
        [Tooltip("Show the opening artwork on the first rendered frame of a standalone story scene.")]
        public bool settleFirstFrameOnStart;
        [Tooltip("Preview hooks only; do not connect these to live campaign rewards.")]
        public StoryStringEvent onNodeChanged = new StoryStringEvent();
        public UnityEvent onCompleted = new UnityEvent();

        private StoryGraph graph;
        private StorySession session;
        private StoryCuePlayer cuePlayer;
        private StoryCreditsPlayer creditsPlayer;
        private StoryPresentationPlayer presentationPlayer;
        private readonly List<string> errors = new List<string>();
        private bool manualPaused, hasFocus = true, applicationPaused, completedDispatched, listenersBound;
        private bool skipPresentationTick;
        private StoryAudioFeedback audioFeedback;
        private int lastInputFrame = -1;
        private float lastInputTime = -100, saveInterval;
        private float inputReadyAt;
        public StoryPackage Package { get { return graph == null ? null : graph.Package; } }
        public StoryNode CurrentNode { get { return session == null ? null : session.Current; } }
        public string CurrentNodeId { get { return CurrentNode == null ? "" : CurrentNode.id; } }
        public string CurrentSceneId { get { return CurrentNode == null ? "" : CurrentNode.sceneId; } }
        public bool IsPaused { get { return manualPaused || !hasFocus || applicationPaused; } }
        public IReadOnlyList<string> ValidationErrors { get { return errors; } }
        public bool HasFlag(string flag) { return session != null && session.HasFlag(flag); }
        public bool UsesDualChronicle => CurrentNode?.kind == "credits" && !string.IsNullOrEmpty(CurrentNode.chronicleId) && view.dialogueBridge != null;
        public void RecordChronicleResult(bool readThrough, bool skipped)
        {
            if (session != null && UsesDualChronicle && session.RecordChronicleResult(readThrough, skipped)) SaveCheckpoint();
        }
        public int CreditParagraph => creditsPlayer == null ? 0 : creditsPlayer.Paragraph;
        public bool CreditsCanContinue => creditsPlayer != null && creditsPlayer.CanContinue;

        private void Awake() { BindButtons(); }
        private void OnEnable()
        {
            skipPresentationTick = true;
            inputReadyAt = Time.unscaledTime + 0.15f;
            if (CurrentNode != null && view != null && view.ranchStage != null)
                view.ranchStage.Present(CurrentNode, reducedMotion, true);
            if (CurrentNode != null && view != null && view.dialogueBridge != null) view.dialogueBridge.Present(true);
        }
        private void OnDisable()
        {
            if (view != null && view.dialogueBridge != null) view.dialogueBridge.dialogue.Close();
            if (audioFeedback != null) audioFeedback.StopFeedback();
            if (view != null && view.ranchStage != null)
            {
                cuePlayer?.FinishAll();
                view.ranchStage.Clear();
            }
            SaveCheckpoint();
        }
        private void Start() { StartStory(restoreOnStart); }
        private void BindButtons()
        {
            if (view == null || listenersBound) return;
            if (view.advanceButton != null) view.advanceButton.onClick.AddListener(Advance);
            if (view.gateButton != null) view.gateButton.onClick.AddListener(ActivateGate);
            if (view.pauseButton != null) view.pauseButton.onClick.AddListener(TogglePause);
            if (view.restartButton != null) view.restartButton.onClick.AddListener(RestartStory);
            if (view.creditsContinueButton != null) view.creditsContinueButton.onClick.AddListener(ContinueCredits);
            if (view.reducedMotionToggle != null) view.reducedMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            listenersBound = true;
        }
        private void OnDestroy()
        {
            if (presentationPlayer != null) presentationPlayer.Dispose();
            if (view == null || !listenersBound) return;
            if (view.advanceButton != null) view.advanceButton.onClick.RemoveListener(Advance);
            if (view.gateButton != null) view.gateButton.onClick.RemoveListener(ActivateGate);
            if (view.pauseButton != null) view.pauseButton.onClick.RemoveListener(TogglePause);
            if (view.restartButton != null) view.restartButton.onClick.RemoveListener(RestartStory);
            if (view.creditsContinueButton != null) view.creditsContinueButton.onClick.RemoveListener(ContinueCredits);
            if (view.reducedMotionToggle != null) view.reducedMotionToggle.onValueChanged.RemoveListener(SetReducedMotion);
        }

        public void StartStory(bool restore)
        {
            BindButtons(); errors.Clear();
            if (view == null) { errors.Add("StoryView is not assigned."); Debug.LogError(errors[0], this); return; }
            try
            {
                if (cuePlayer != null) cuePlayer.FinishAll();
                if (presentationPlayer != null) { presentationPlayer.Dispose(); presentationPlayer = null; }
                var source = storyJson != null ? storyJson : Resources.Load<TextAsset>(defaultResourcePath);
                if (source == null) throw new InvalidOperationException("Missing story JSON: " + defaultResourcePath);
                var data = JsonUtility.FromJson<StoryPackage>(source.text);
                if (!StoryGraph.TryCreate(data, out var validated, out var validation))
                {
                    errors.AddRange(validation); view.ShowError(string.Join("\n", errors)); return;
                }
                graph = validated; session = new StorySession(graph);
                audioFeedback = GetComponent<StoryAudioFeedback>();
                if (audioFeedback != null) audioFeedback.ResetSession();
                cuePlayer = new StoryCuePlayer(view); creditsPlayer = new StoryCreditsPlayer(view);
                manualPaused = false; applicationPaused = false; hasFocus = Application.isFocused;
                completedDispatched = false; saveInterval = 0; skipPresentationTick = true;
                inputReadyAt = Time.unscaledTime + 0.15f;
                view.Initialize(graph);
                var presentationSource = presentationJson != null ? presentationJson : Resources.Load<TextAsset>(presentationResourcePath);
                var presentationData = presentationSource == null ? null : JsonUtility.FromJson<StoryPresentationPackage>(presentationSource.text);
                presentationPlayer = new StoryPresentationPlayer(view, graph, presentationData);
                StorySaveData saved = restore ? StorySaveStore.Load(data.packageId) : null;
                bool restored = saved != null && session.Restore(saved);
                if (restored)
                {
                    reducedMotion = saved.reducedMotion;
                    completedDispatched = saved.completed && session.Current.kind == "end";
                }
                else if (saved != null) Debug.LogWarning("Story save is incompatible or invalid; starting at the safe first node.", this);
                if (view.reducedMotionToggle != null) view.reducedMotionToggle.SetIsOnWithoutNotify(reducedMotion);
                PresentCurrent(restored ? saved : null);
                if (!restored && settleFirstFrameOnStart)
                {
                    cuePlayer.FinishAll();
                    SaveCheckpoint();
                }
                view.SetPaused(IsPaused);
            }
            catch (Exception exception) { Fail(exception); }
        }

        private void Update()
        {
            if (session == null || IsPaused || errors.Count > 0) return;
            try
            {
                // The first resumed frame may include wall time spent outside the app.
                var delta = skipPresentationTick ? 0 : Mathf.Max(0, Time.unscaledDeltaTime);
                skipPresentationTick = false;
                bool hadCue = cuePlayer.IsPlaying;
                cuePlayer.Tick(delta);
                presentationPlayer.Tick(delta);
                if (!UsesDualChronicle) creditsPlayer.Tick(delta);
                if (view.ranchStage != null) view.ranchStage.TickBookTransition(Mathf.Min(delta, 0.05f));
                if (view.dialogueBridge != null) view.dialogueBridge.RefreshControls();
                saveInterval += delta;
                if ((hadCue && !cuePlayer.IsPlaying) || (creditsPlayer.Active && saveInterval >= 1)) SaveCheckpoint();
            }
            catch (Exception exception) { Fail(exception); }
        }
        private void LateUpdate()
        {
            // Recompute cover-safe camera bounds after a resize without advancing animation time.
            if (presentationPlayer != null) presentationPlayer.RefreshLayout();
        }
        private bool AcceptInput()
        {
            if (!isActiveAndEnabled || session == null || IsPaused || errors.Count > 0 || Time.unscaledTime < inputReadyAt || lastInputFrame == Time.frameCount || Time.unscaledTime - lastInputTime < 0.15f) return false;
            lastInputFrame = Time.frameCount; lastInputTime = Time.unscaledTime; return true;
        }
        public void Advance()
        {
            if (!AcceptInput()) return;
            // The erasure is a fixed story event: a quick second tap cannot reveal
            // the AFTER image or move past it before its sound-to-image delay.
            if (cuePlayer.HasPendingSwapArt) return;
            try
            {
                presentationPlayer.FinishAll();
                if (audioFeedback != null) audioFeedback.PlayAdvance();
                if (cuePlayer.IsPlaying)
                {
                    cuePlayer.FinishAll();
                    if (finishLegacyCueBeforeAdvancing) { SaveCheckpoint(); return; }
                }
                if (CurrentNode.kind == "credits")
                {
                    if (UsesDualChronicle) return;
                    creditsPlayer.OnAdvance();
                    if (view.dialogueBridge != null) view.dialogueBridge.Present(false);
                    SaveCheckpoint(); return;
                }
                if (view.TryAdvanceDialoguePage()) { SaveCheckpoint(); return; }
                if (session.Acknowledge()) PresentCurrent();
            }
            catch (Exception exception) { Fail(exception); }
        }
        public void ActivateGate()
        {
            if (!AcceptInput() || CurrentNode.kind != "gate") return;
            try
            {
                presentationPlayer.FinishAll();
                if (cuePlayer.IsPlaying) { cuePlayer.FinishAll(); SaveCheckpoint(); return; }
                if (session.ActivateGate()) PresentCurrent();
            }
            catch (Exception exception) { Fail(exception); }
        }
        public void ContinueCredits()
        {
            if (UsesDualChronicle || !AcceptInput() || CurrentNode.kind != "credits" || !creditsPlayer.CanContinue) return;
            try { if (session.ContinueCredits()) PresentCurrent(); }
            catch (Exception exception) { Fail(exception); }
        }
        public void SkipCurrentScene()
        {
            if (view.dialogueBridge == null || !AcceptInput()) return;
            CancelTransientPresentation();
            if (CurrentNode.kind == "gate") { PresentCurrent(); view.dialogueBridge.Present(true); return; }
            if (UsesDualChronicle) { view.dialogueBridge.dialogue.Chronicle.SkipToEnd(); return; }
            if (CurrentNode.kind == "credits")
            {
                view.ranchStage.Present(CurrentNode, reducedMotion, true);
                creditsPlayer.SkipToLastParagraph(); view.dialogueBridge.Present(true); SaveCheckpoint(); return;
            }
            string scene = CurrentSceneId;
            // Advance the existing model without presenting or scheduling any omitted line's FX.
            for (int steps = 0; steps < Package.nodes.Length && CurrentSceneId == scene &&
                CurrentNode.kind != "gate" && CurrentNode.kind != "credits" && CurrentNode.kind != "end"; steps++)
                if (!session.Acknowledge()) break;
            PresentCurrent();
            if (CurrentNode.kind == "credits" && !UsesDualChronicle)
            {
                creditsPlayer.SkipToLastParagraph(); view.dialogueBridge.Present(true); SaveCheckpoint();
            }
        }
        public void CancelTransientPresentation()
        {
            cuePlayer?.CancelAll();
            if (view != null && view.ranchStage != null) view.ranchStage.Clear();
            if (audioFeedback != null) audioFeedback.StopFeedback();
        }
        public bool ConfirmDialogueAction(string action)
        {
            if (action == "play_chronicle")
            {
                var ui = view.dialogueBridge?.dialogue;
                var record = ui?.Chronicle;
                if (!UsesDualChronicle || record == null || !record.IsAwaitingContinue || !record.ContinueWasRequested ||
                    (!record.WasReadThrough && !record.WasSkipped) || !AcceptInput()) return false;
                if (!ui.ConfirmAction(action) || !session.ContinueCredits()) return false;
                PresentCurrent(); return true;
            }
            bool book = action == "read_chronicle" && CurrentNode?.kind == "credits" && CreditsCanContinue;
            bool gate = CurrentNode?.kind == "gate" && (action == "block_lamp" && CurrentNode.gateId == "BLOCK_LAMP" ||
                action == "chapter_complete" && CurrentNode.gateId == "PREVIEW_FINAL_CONTINUE" ||
                action == "puzzle_handoff" && CurrentNode.gateId.StartsWith("PREVIEW_ONLY_", StringComparison.Ordinal));
            if ((!book && !gate) || !AcceptInput()) return false;
            bool moved = book ? session.ContinueCredits() : session.ActivateGate();
            if (!moved) return false;
            view.dialogueBridge.dialogue.ConfirmActionFromHost(action);
            PresentCurrent(); return true;
        }
        public void TogglePause()
        {
            manualPaused = !manualPaused;
            if (!manualPaused) { skipPresentationTick = true; inputReadyAt = Time.unscaledTime + 0.15f; }
            if (view != null) view.SetPaused(IsPaused);
            SaveCheckpoint();
        }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (view != null && view.ranchStage != null) view.ranchStage.SetReducedMotion(value);
            if (cuePlayer != null && value) cuePlayer.FinishAll();
            if (presentationPlayer != null && value) presentationPlayer.FinishAll();
            if (creditsPlayer != null) creditsPlayer.SetReducedMotion(value);
            if (view != null && view.reducedMotionToggle != null) view.reducedMotionToggle.SetIsOnWithoutNotify(value);
            if (view != null && view.dialogueBridge != null) view.dialogueBridge.dialogue.SetReducedMotion(value);
            SaveCheckpoint();
        }
        public void RestartStory()
        {
            if (persistProgress && graph != null) StorySaveStore.Delete(graph.Package.packageId);
            StartStory(false);
        }
        public bool JumpTo(string nodeId)
        {
            if (!Debug.isDebugBuild || session == null) return false;
            if (!session.DebugJump(nodeId)) return false;
            cuePlayer.FinishAll(); presentationPlayer.FinishAll(); view.ResetPresentation(); completedDispatched = false;
            PresentCurrent(); return true;
        }
        private void PresentCurrent(StorySaveData saved = null)
        {
            var node = CurrentNode;
            presentationPlayer.FinishAll();
            bool keepBook = node.kind == "gate" && node.gateId == "PREVIEW_FINAL_CONTINUE";
            creditsPlayer.Stop(keepBook);
            var previousArt = view.artImage == null ? null : view.artImage.sprite;
            view.PresentNode(node, keepBook);
            if (node.kind == "credits" && !UsesDualChronicle) creditsPlayer.Begin(node.credits, reducedMotion || view.dialogueBridge != null, saved);
            else if (keepBook && saved != null)
            {
                var book = Package.nodes.FirstOrDefault(n => n.kind == "credits" && n.nextId == node.id);
                if (book != null) { creditsPlayer.Begin(book.credits, reducedMotion || view.dialogueBridge != null, saved); creditsPlayer.Stop(true); }
            }
            var cues = StoryPresentationIndex.FilterLegacyCues(node.cues, presentationPlayer.HasPresentation);
            if (view.ranchStage != null)
            {
                presentationPlayer.Present(node, reducedMotion, saved != null);
                view.ranchStage.Present(node, reducedMotion, saved != null);
            }
            if (saved == null) cuePlayer.Begin(cues, reducedMotion);
            else cuePlayer.RestoreSettled(cues, saved);
            if (view.ranchStage == null) presentationPlayer.Present(node, reducedMotion, saved != null);
            if (audioFeedback != null) audioFeedback.PlayNode(node.id, saved != null);
            if (view.dialogueBridge != null) view.dialogueBridge.Present(saved != null);
            if (UsesDualChronicle && view.ranchStage != null && saved == null)
                view.ranchStage.BeginBookTransition(previousArt, reducedMotion, view.dialogueBridge.dialogue.Chronicle.IntroDuration);
            SaveCheckpoint(); // In R17 the return cue is committed before the credits node is reachable.
            onNodeChanged.Invoke(node.id);
            if (node.kind == "end" && !completedDispatched)
            {
                completedDispatched = true;
                SaveCheckpoint();
                onCompleted.Invoke();
            }
        }
        public void SaveCheckpoint()
        {
            if (!persistProgress || session == null || errors.Count > 0) return;
            var save = new StorySaveData
            {
                packageId = graph.Package.packageId, contentVersion = graph.Package.contentVersion,
                nodeId = CurrentNodeId, flags = session.Flags, completed = completedDispatched,
                reducedMotion = reducedMotion,
                chronicleRevision = "0.28", chronicleReadThrough = session.ChronicleReadThrough, chronicleSkipped = session.ChronicleSkipped
            };
            view.Capture(save); creditsPlayer.Capture(save);
            presentationPlayer.Capture(save);
            StorySaveStore.Save(save); saveInterval = 0;
        }
        private void OnApplicationFocus(bool focus)
        {
            hasFocus = focus;
            if (focus) { skipPresentationTick = true; inputReadyAt = Time.unscaledTime + 0.15f; }
            if (view != null) view.SetPaused(IsPaused);
            if (!focus) SaveCheckpoint();
        }
        private void OnApplicationPause(bool pause)
        {
            applicationPaused = pause;
            if (!pause) { skipPresentationTick = true; inputReadyAt = Time.unscaledTime + 0.15f; }
            if (view != null) view.SetPaused(IsPaused);
            if (pause) SaveCheckpoint();
        }
        private void OnApplicationQuit() { SaveCheckpoint(); }
        private void Fail(Exception exception)
        {
            errors.Add(exception.Message);
            if (view != null) view.ShowError(exception.Message); else Debug.LogException(exception, this);
        }
    }
}
