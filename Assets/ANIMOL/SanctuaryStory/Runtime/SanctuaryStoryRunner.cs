using System;
using UnityEngine;
using UnityEngine.Events;

namespace ANIMOL.SanctuaryStory
{
    [Serializable] public sealed class SanctuaryBoolEvent : UnityEvent<bool> { }
    [Serializable] public sealed class SanctuaryStringEvent : UnityEvent<string> { }

    /// <summary>
    /// Attaches the portable flow to the existing SC18 host. This component owns no campaign
    /// persistence, rewards, Canvas, EventSystem, audio settings or Time.timeScale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SanctuaryStoryRunner : MonoBehaviour
    {
        [SerializeField] private TextAsset chapterJson;
        [SerializeField] private bool playOnStart = false;
        [Tooltip("Preview only: acknowledges cue durations without proving an animation was played. Keep disabled for production.")]
        [SerializeField] private bool timedCuePreview = false;

        [Header("Connect to the existing project owners")]
        public SanctuaryBoolEvent onStoryActiveChanged = new SanctuaryBoolEvent();
        public SanctuaryStringEvent onCheckpointJson = new SanctuaryStringEvent();
        public UnityEvent onPlaybackFinished = new UnityEvent();
        public UnityEvent onChapterCompleted = new UnityEvent();

        public SanctuaryFlow Flow { get; private set; }
        public SanctuaryChapter Chapter { get; private set; }
        public bool IsPaused { get { return hostPaused || applicationPaused; } }

        public event Action<SanctuaryScene> SceneChanged;
        public event Action<SanctuaryLine> LineChanged;
        public event Action<SanctuaryChoiceSet> ChoicesPresented;
        public event Action ChoicesDismissed;
        public event Action HesitationStarted;
        public event Action<SanctuaryCue, int> CueRequested;
        public event Action<SanctuaryCheckpoint> CheckpointChanged;
        public event Action PlaybackCompleted;
        public event Action StateChanged;

        private TextAsset loadedAsset;
        private string loadedText;
        private bool hostPaused, applicationPaused, storyActive, stopping, starting, finishing;
        private bool knownCompleted, completionEventRaised;
        private int lastAdvanceFrame = -1;
        private int previewToken;
        private float previewElapsed;
        private bool skipNextDelta;

        private void Awake() { LoadChapter(); }
        private void Start() { if (playOnStart) BeginChapter(); }

        private void Update()
        {
            if (!storyActive || Flow == null) return;
            // A resumed frame must not count the application's background interval as story time.
            if (skipNextDelta) { skipNextDelta = false; return; }
            Execute(delegate
            {
                Flow.Tick(Time.unscaledDeltaTime);
                if (!timedCuePreview || IsPaused || Flow.State != SanctuaryFlowState.Cue) return;
                int token = Flow.PendingCueToken;
                if (token != previewToken) { previewToken = token; previewElapsed = 0f; }
                previewElapsed += Time.unscaledDeltaTime;
                var cue = Flow.PendingCue;
                if (cue != null && previewElapsed >= cue.duration && token == Flow.PendingCueToken)
                    Flow.CompleteCue(token);
            });
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            ApplyPause();
        }

        private void OnDisable() { StopPlayback(); }
        private void OnDestroy() { StopPlayback(); DetachFlow(); }

        /// <summary>Idempotent for the same TextAsset/content; does not implicitly start playback.</summary>
        public void LoadChapter()
        {
            if (stopping || finishing) return;
            if (chapterJson != null && ReferenceEquals(chapterJson, loadedAsset) &&
                string.Equals(chapterJson.text, loadedText, StringComparison.Ordinal) && Flow != null) return;
            try
            {
                if (chapterJson == null) throw new ArgumentException("Assign the sanctuary chapter TextAsset.");
                var parsed = JsonUtility.FromJson<SanctuaryChapter>(chapterJson.text);
                var nextFlow = new SanctuaryFlow(parsed);
                bool sameChapter = Chapter != null && string.Equals(Chapter.chapterId, parsed.chapterId, StringComparison.Ordinal);
                StopPlayback();
                DetachFlow();
                Chapter = parsed;
                Flow = nextFlow;
                loadedAsset = chapterJson;
                loadedText = chapterJson.text;
                if (!sameChapter) { knownCompleted = false; completionEventRaised = false; }
                AttachFlow();
                Flow.SetPaused(IsPaused);
            }
            catch (Exception exception)
            {
                Report(exception);
                StopPlayback();
                DetachFlow();
                Flow = null;
                Chapter = null;
                loadedAsset = null;
                loadedText = null;
            }
        }

        public void BeginChapter()
        {
            if (storyActive || stopping || starting || finishing) return;
            // An already completed chapter is reviewed read-only, never reset to completed=false.
            StartPlayback(null, knownCompleted);
        }

        public void BeginReplay()
        {
            if (storyActive || stopping || starting || finishing) return;
            StartPlayback(null, true);
        }

        public void ResumeFromJson(string checkpointJson)
        {
            if (storyActive || stopping || starting || finishing) return;
            LoadChapter();
            if (Flow == null) return;
            Execute(delegate
            {
                if (string.IsNullOrWhiteSpace(checkpointJson)) throw new ArgumentException("Checkpoint JSON is empty.");
                var checkpoint = JsonUtility.FromJson<SanctuaryCheckpoint>(checkpointJson);
                SanctuaryChapterValidator.ValidateCheckpoint(Chapter, checkpoint);
                if (checkpoint.completed)
                {
                    // The host restores the name from its existing completed record. Do not replay
                    // S14, emit a new save, or request another completion/reward transaction.
                    knownCompleted = true;
                    completionEventRaised = true;
                    StopPlayback();
                    onPlaybackFinished?.Invoke();
                    return;
                }
                // A stale incomplete record must not undo a completion already observed by this host.
                StartPlayback(checkpoint.sceneId, knownCompleted);
            });
        }

        public void Advance()
        {
            if (!CanReceiveInput() || Flow.State != SanctuaryFlowState.Line || lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;
            Execute(delegate { Flow.Advance(); });
        }

        public void ArmChoices()
        {
            if (!storyActive || Flow == null) return;
            Execute(delegate { Flow.ArmChoices(); });
        }

        public void Choose(int index)
        {
            if (!CanReceiveInput()) return;
            Execute(delegate { Flow.Choose(index); });
        }

        public void CompleteCue(int token)
        {
            if (!storyActive || Flow == null) return;
            Execute(delegate { Flow.CompleteCue(token); });
        }

        public void SetHostPaused(bool paused)
        {
            hostPaused = paused;
            ApplyPause();
        }

        public void SkipScene()
        {
            if (!CanReceiveInput()) return;
            Execute(delegate { Flow.SkipScene(); });
        }

        public void StopPlayback()
        {
            if (stopping) return;
            stopping = true;
            try
            {
                previewToken = 0;
                previewElapsed = 0f;
                if (Flow != null) Flow.Stop();
            }
            catch (Exception exception) { Report(exception); }
            finally
            {
                // Set the internal flag first so disable/destroy/callback re-entry cannot release twice.
                try { SetStoryActive(false); }
                catch (Exception exception) { Report(exception); }
                stopping = false;
            }
        }

        private void StartPlayback(string sceneId, bool replay)
        {
            if (!isActiveAndEnabled || storyActive || stopping || starting || finishing) return;
            LoadChapter();
            if (Flow == null) return;
            starting = true;
            try
            {
                Execute(delegate
                {
                    previewToken = 0;
                    previewElapsed = 0f;
                    lastAdvanceFrame = -1;
                    Flow.SetPaused(IsPaused);
                    SetStoryActive(true);
                    if (!storyActive || !isActiveAndEnabled) return;
                    if (timedCuePreview)
                        Debug.LogWarning("[SanctuaryStory] Timed cue preview is enabled. World animation/audio playback is not verified.", this);
                    Flow.Begin(sceneId, replay);
                });
            }
            finally { starting = false; }
        }

        private bool CanReceiveInput() { return storyActive && Flow != null && !IsPaused && isActiveAndEnabled; }
        private void ApplyPause()
        {
            if (Flow == null) return;
            if (Flow.IsPaused && !IsPaused) skipNextDelta = true;
            Execute(delegate { Flow.SetPaused(IsPaused); });
        }

        private void SetStoryActive(bool active)
        {
            if (storyActive == active) return;
            storyActive = active;
            onStoryActiveChanged?.Invoke(active);
        }

        private void HandleCheckpoint(SanctuaryCheckpoint checkpoint)
        {
            if (checkpoint.completed) knownCompleted = true;
            string json = JsonUtility.ToJson(checkpoint);
            CheckpointChanged?.Invoke(checkpoint);
            onCheckpointJson?.Invoke(json);
        }

        private void HandlePlaybackCompleted()
        {
            finishing = true;
            try
            {
                bool realCompletion = Flow != null && !Flow.ReadOnlyReplay;
                SetStoryActive(false);
                if (realCompletion && !completionEventRaised)
                {
                    completionEventRaised = true;
                    knownCompleted = true;
                    onChapterCompleted?.Invoke();
                }
                PlaybackCompleted?.Invoke();
                onPlaybackFinished?.Invoke();
            }
            finally { finishing = false; }
        }

        private void AttachFlow()
        {
            Flow.SceneChanged += ForwardScene;
            Flow.LineChanged += ForwardLine;
            Flow.ChoicesPresented += ForwardChoices;
            Flow.ChoicesDismissed += ForwardDismissed;
            Flow.HesitationStarted += ForwardHesitation;
            Flow.CueRequested += ForwardCue;
            Flow.CheckpointChanged += HandleCheckpoint;
            Flow.PlaybackCompleted += HandlePlaybackCompleted;
            Flow.StateChanged += ForwardState;
        }

        private void DetachFlow()
        {
            if (Flow == null) return;
            Flow.SceneChanged -= ForwardScene;
            Flow.LineChanged -= ForwardLine;
            Flow.ChoicesPresented -= ForwardChoices;
            Flow.ChoicesDismissed -= ForwardDismissed;
            Flow.HesitationStarted -= ForwardHesitation;
            Flow.CueRequested -= ForwardCue;
            Flow.CheckpointChanged -= HandleCheckpoint;
            Flow.PlaybackCompleted -= HandlePlaybackCompleted;
            Flow.StateChanged -= ForwardState;
        }

        private void ForwardScene(SanctuaryScene scene) { SceneChanged?.Invoke(scene); }
        private void ForwardLine(SanctuaryLine line) { LineChanged?.Invoke(line); }
        private void ForwardChoices(SanctuaryChoiceSet choices) { ChoicesPresented?.Invoke(choices); }
        private void ForwardDismissed() { ChoicesDismissed?.Invoke(); }
        private void ForwardHesitation() { HesitationStarted?.Invoke(); }
        private void ForwardCue(SanctuaryCue cue, int token) { CueRequested?.Invoke(cue, token); }
        private void ForwardState() { StateChanged?.Invoke(); }

        private void Execute(Action action)
        {
            try { action(); }
            catch (Exception exception) { Report(exception); StopPlayback(); }
        }

        private void Report(Exception exception)
        {
            Debug.LogError("[SanctuaryStory] " + exception.Message, this);
            Debug.LogException(exception, this);
        }
    }
}
