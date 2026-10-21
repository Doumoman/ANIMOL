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
        public bool reducedMotion;
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
        private int lastInputFrame = -1;
        private float lastInputTime = -100, saveInterval;
        public StoryPackage Package { get { return graph == null ? null : graph.Package; } }
        public StoryNode CurrentNode { get { return session == null ? null : session.Current; } }
        public string CurrentNodeId { get { return CurrentNode == null ? "" : CurrentNode.id; } }
        public string CurrentSceneId { get { return CurrentNode == null ? "" : CurrentNode.sceneId; } }
        public bool IsPaused { get { return manualPaused || !hasFocus || applicationPaused; } }
        public IReadOnlyList<string> ValidationErrors { get { return errors; } }
        public bool HasFlag(string flag) { return session != null && session.HasFlag(flag); }

        private void Awake() { BindButtons(); }
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
                cuePlayer = new StoryCuePlayer(view); creditsPlayer = new StoryCreditsPlayer(view);
                manualPaused = false; applicationPaused = false; hasFocus = Application.isFocused;
                completedDispatched = false; saveInterval = 0; skipPresentationTick = true;
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
                creditsPlayer.Tick(delta);
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
            if (session == null || IsPaused || errors.Count > 0 || lastInputFrame == Time.frameCount || Time.unscaledTime - lastInputTime < 0.15f) return false;
            lastInputFrame = Time.frameCount; lastInputTime = Time.unscaledTime; return true;
        }
        public void Advance()
        {
            if (!AcceptInput()) return;
            try
            {
                presentationPlayer.FinishAll();
                if (cuePlayer.IsPlaying) { cuePlayer.FinishAll(); SaveCheckpoint(); return; }
                if (CurrentNode.kind == "credits") { creditsPlayer.OnAdvance(); SaveCheckpoint(); return; }
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
            if (!AcceptInput() || CurrentNode.kind != "credits" || !creditsPlayer.CanContinue) return;
            try { if (session.ContinueCredits()) PresentCurrent(); }
            catch (Exception exception) { Fail(exception); }
        }
        public void TogglePause()
        {
            manualPaused = !manualPaused;
            if (!manualPaused) skipPresentationTick = true;
            if (view != null) view.SetPaused(IsPaused);
            SaveCheckpoint();
        }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (cuePlayer != null && value) cuePlayer.FinishAll();
            if (presentationPlayer != null && value) presentationPlayer.FinishAll();
            if (creditsPlayer != null) creditsPlayer.SetReducedMotion(value);
            if (view != null && view.reducedMotionToggle != null) view.reducedMotionToggle.SetIsOnWithoutNotify(value);
            SaveCheckpoint();
        }
        public void RestartStory()
        {
            if (graph != null) StorySaveStore.Delete(graph.Package.packageId);
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
            view.PresentNode(node, keepBook);
            if (node.kind == "credits") creditsPlayer.Begin(node.credits, reducedMotion, saved);
            else if (keepBook && saved != null)
            {
                var book = Package.nodes.FirstOrDefault(n => n.kind == "credits" && n.nextId == node.id);
                if (book != null) { creditsPlayer.Begin(book.credits, reducedMotion, saved); creditsPlayer.Stop(true); }
            }
            var cues = StoryPresentationIndex.FilterLegacyCues(node.cues, presentationPlayer.HasPresentation);
            if (saved == null) cuePlayer.Begin(cues, reducedMotion);
            else cuePlayer.RestoreSettled(cues, saved);
            presentationPlayer.Present(node, reducedMotion, saved != null);
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
            if (session == null || errors.Count > 0) return;
            var save = new StorySaveData
            {
                packageId = graph.Package.packageId, contentVersion = graph.Package.contentVersion,
                nodeId = CurrentNodeId, flags = session.Flags, completed = completedDispatched,
                reducedMotion = reducedMotion
            };
            view.Capture(save); creditsPlayer.Capture(save);
            presentationPlayer.Capture(save);
            StorySaveStore.Save(save); saveInterval = 0;
        }
        private void OnApplicationFocus(bool focus)
        {
            hasFocus = focus;
            if (focus) skipPresentationTick = true;
            if (view != null) view.SetPaused(IsPaused);
            if (!focus) SaveCheckpoint();
        }
        private void OnApplicationPause(bool pause)
        {
            applicationPaused = pause;
            if (!pause) skipPresentationTick = true;
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
