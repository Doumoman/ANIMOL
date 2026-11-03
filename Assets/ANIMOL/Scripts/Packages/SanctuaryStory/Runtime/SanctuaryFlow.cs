using System;
using System.Collections.Generic;

namespace ANIMOL.SanctuaryStory
{
    public enum SanctuaryFlowState { Idle, Line, PreparingChoice, Choice, Hesitating, Cue, Completed }

    /// <summary>
    /// UI-independent story flow. The owner must treat chapter DTOs as immutable during playback.
    /// Input actions are never accepted from inside presentation notifications. Immediate presenter
    /// acknowledgements are queued, so zero-duration cues do not recursively skip or deadlock.
    /// </summary>
    public sealed class SanctuaryFlow
    {
        private readonly SanctuaryChapter chapter;
        private readonly Dictionary<string, SanctuaryScene> scenes;
        private int stepIndex;
        private int nextCueToken;
        private float hesitationRemaining;
        private bool operating, draining, stopRequested;
        private bool? deferredPause;
        private bool deferredArm;
        private int deferredCueToken;
        private bool armOnResume;
        private int cueOnResume;

        public SanctuaryFlowState State { get; private set; }
        public SanctuaryScene CurrentScene { get; private set; }
        public SanctuaryLine CurrentLine { get; private set; }
        public SanctuaryChoiceSet CurrentChoices { get; private set; }
        public SanctuaryCue PendingCue { get; private set; }
        public int PendingCueToken { get; private set; }
        public bool IsPaused { get; private set; }
        public bool ReadOnlyReplay { get; private set; }

        public event Action<SanctuaryScene> SceneChanged;
        public event Action<SanctuaryLine> LineChanged;
        public event Action<SanctuaryChoiceSet> ChoicesPresented;
        public event Action ChoicesDismissed;
        public event Action HesitationStarted;
        public event Action<SanctuaryCue, int> CueRequested;
        public event Action<SanctuaryCheckpoint> CheckpointChanged;
        public event Action PlaybackCompleted;
        public event Action StateChanged;

        public SanctuaryFlow(SanctuaryChapter chapter)
        {
            SanctuaryChapterValidator.Validate(chapter);
            this.chapter = chapter;
            scenes = new Dictionary<string, SanctuaryScene>(StringComparer.Ordinal);
            foreach (var scene in chapter.scenes) scenes.Add(scene.id, scene);
            State = SanctuaryFlowState.Idle;
        }

        public void Begin(string sceneId = null, bool readOnlyReplay = false)
        {
            if (operating) return;
            string target = sceneId ?? chapter.initialSceneId;
            if (!scenes.ContainsKey(target)) throw new ArgumentException("Unknown scene: " + target);
            Run(delegate
            {
                ClearPending();
                ReadOnlyReplay = readOnlyReplay;
                EnterScene(target);
            });
        }

        public void Advance()
        {
            if (operating || IsPaused || State != SanctuaryFlowState.Line) return;
            Run(delegate { stepIndex++; PresentStep(); });
        }

        public void ArmChoices()
        {
            if (State != SanctuaryFlowState.PreparingChoice) return;
            if (operating) { deferredArm = true; return; }
            if (IsPaused) { armOnResume = true; return; }
            Run(delegate { State = SanctuaryFlowState.Choice; NotifyState(); });
        }

        public void Choose(int optionIndex)
        {
            if (operating || IsPaused || State != SanctuaryFlowState.Choice) return;
            if (CurrentChoices == null || optionIndex < 0 || optionIndex >= CurrentChoices.options.Length) return;
            Run(delegate
            {
                var option = CurrentChoices.options[optionIndex];
                if (option.action == "hesitate")
                {
                    hesitationRemaining = Math.Max(0f, CurrentChoices.hesitationSeconds - CurrentChoices.revealSeconds);
                    State = SanctuaryFlowState.Hesitating;
                    NotifyState();
                    if (stopRequested) return;
                    ChoicesDismissed?.Invoke();
                    if (stopRequested) return;
                    HesitationStarted?.Invoke();
                }
                else
                {
                    CurrentChoices = null;
                    State = SanctuaryFlowState.Idle;
                    NotifyState();
                    if (stopRequested) return;
                    ChoicesDismissed?.Invoke();
                    if (stopRequested) return;
                    stepIndex++;
                    PresentStep();
                }
            });
        }

        public void CompleteCue(int token)
        {
            if (State != SanctuaryFlowState.Cue || token <= 0 || token != PendingCueToken) return;
            if (operating) { deferredCueToken = token; return; }
            if (IsPaused) { cueOnResume = token; return; }
            Run(delegate
            {
                PendingCue = null;
                PendingCueToken = 0;
                cueOnResume = 0;
                stepIndex++;
                PresentStep();
            });
        }

        public void Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException("deltaSeconds", "Tick must be finite and nonnegative.");
            if (operating || IsPaused || State != SanctuaryFlowState.Hesitating) return;
            Run(delegate
            {
                hesitationRemaining = Math.Max(0f, hesitationRemaining - deltaSeconds);
                if (hesitationRemaining <= 0f) PresentChoice(CurrentChoices);
            });
        }

        public void SetPaused(bool paused)
        {
            if (operating) { deferredPause = paused; return; }
            if (IsPaused == paused) return;
            Run(delegate
            {
                IsPaused = paused;
                NotifyState();
                if (stopRequested || paused) return;
                if (cueOnResume != 0) { deferredCueToken = cueOnResume; cueOnResume = 0; }
                if (armOnResume) { deferredArm = true; armOnResume = false; }
            });
        }

        public void SkipScene()
        {
            if (operating || IsPaused || CurrentScene == null || State == SanctuaryFlowState.Completed) return;
            Run(delegate
            {
                string next = CurrentScene.nextSceneId;
                ClearPending();
                State = SanctuaryFlowState.Idle;
                NotifyState();
                if (stopRequested) return;
                ChoicesDismissed?.Invoke();
                if (stopRequested) return;
                if (string.IsNullOrEmpty(next)) CompletePlayback();
                else EnterScene(next);
            });
        }

        public void Stop()
        {
            // Cleanup observers may call Stop again while receiving the Idle/dismissed signal.
            if (State == SanctuaryFlowState.Idle && CurrentScene == null) return;
            if (operating) { stopRequested = true; return; }
            Run(StopInternal);
        }

        private void EnterScene(string sceneId)
        {
            CurrentScene = scenes[sceneId];
            stepIndex = 0;
            ClearPending();
            State = SanctuaryFlowState.Idle;
            NotifyState();
            if (stopRequested) return;
            SceneChanged?.Invoke(CurrentScene);
            if (stopRequested) return;
            if (!ReadOnlyReplay) CheckpointChanged?.Invoke(MakeCheckpoint(false));
            if (stopRequested) return;
            PresentStep();
        }

        private void PresentStep()
        {
            if (stopRequested || CurrentScene == null) return;
            if (stepIndex >= CurrentScene.steps.Length)
            {
                if (string.IsNullOrEmpty(CurrentScene.nextSceneId)) CompletePlayback();
                else EnterScene(CurrentScene.nextSceneId);
                return;
            }
            CurrentLine = null;
            CurrentChoices = null;
            PendingCue = null;
            PendingCueToken = 0;
            var step = CurrentScene.steps[stepIndex];
            switch (step.kind)
            {
                case "line":
                    CurrentLine = step.line;
                    State = SanctuaryFlowState.Line;
                    NotifyState();
                    if (!stopRequested) LineChanged?.Invoke(CurrentLine);
                    break;
                case "choice":
                    PresentChoice(step.choice);
                    break;
                case "cue":
                    if (nextCueToken == int.MaxValue) throw new InvalidOperationException("Cue token capacity exhausted; create a new flow.");
                    PendingCue = step.cue;
                    PendingCueToken = ++nextCueToken;
                    State = SanctuaryFlowState.Cue;
                    NotifyState();
                    if (!stopRequested) CueRequested?.Invoke(PendingCue, PendingCueToken);
                    break;
                default:
                    throw new InvalidOperationException("Running chapter was mutated to an unsupported step kind.");
            }
        }

        private void PresentChoice(SanctuaryChoiceSet choice)
        {
            CurrentChoices = choice;
            State = SanctuaryFlowState.PreparingChoice;
            NotifyState();
            if (!stopRequested) ChoicesPresented?.Invoke(choice);
        }

        private void CompletePlayback()
        {
            ClearPending();
            State = SanctuaryFlowState.Completed;
            // Persist the logical terminal state before UI observers may close/disable the host.
            if (!ReadOnlyReplay) CheckpointChanged?.Invoke(MakeCheckpoint(true));
            if (stopRequested) return;
            NotifyState();
            if (!stopRequested) PlaybackCompleted?.Invoke();
        }

        private SanctuaryCheckpoint MakeCheckpoint(bool completed)
        {
            return new SanctuaryCheckpoint
            {
                schemaVersion = chapter.schemaVersion,
                chapterId = chapter.chapterId,
                sceneId = CurrentScene.id,
                completed = completed
            };
        }

        private void ClearPending()
        {
            CurrentLine = null;
            CurrentChoices = null;
            PendingCue = null;
            PendingCueToken = 0;
            hesitationRemaining = 0f;
            deferredArm = false;
            deferredCueToken = 0;
            armOnResume = false;
            cueOnResume = 0;
        }

        private void StopInternal()
        {
            stopRequested = false;
            ClearPending();
            CurrentScene = null;
            State = SanctuaryFlowState.Idle;
            NotifyState();
            ChoicesDismissed?.Invoke();
        }

        private void NotifyState() { StateChanged?.Invoke(); }

        private void Run(Action action)
        {
            operating = true;
            bool succeeded = false;
            try { action(); succeeded = true; }
            finally
            {
                operating = false;
                if (succeeded) DrainDeferred();
                else { deferredArm = false; deferredCueToken = 0; deferredPause = null; }
            }
        }

        private void DrainDeferred()
        {
            if (draining) return;
            draining = true;
            try
            {
                // Iterative draining allows an immediate presenter to acknowledge many cues safely.
                for (int guard = 0; guard < 100000; guard++)
                {
                    if (stopRequested) { Stop(); continue; }
                    if (deferredPause.HasValue)
                    {
                        bool paused = deferredPause.Value;
                        deferredPause = null;
                        SetPaused(paused);
                        continue;
                    }
                    if (deferredCueToken != 0)
                    {
                        int token = deferredCueToken;
                        deferredCueToken = 0;
                        CompleteCue(token);
                        continue;
                    }
                    if (deferredArm)
                    {
                        deferredArm = false;
                        ArmChoices();
                        continue;
                    }
                    return;
                }
                throw new InvalidOperationException("Presentation callbacks did not settle; inspect recursive pause/stop handlers.");
            }
            finally { draining = false; }
        }
    }
}
