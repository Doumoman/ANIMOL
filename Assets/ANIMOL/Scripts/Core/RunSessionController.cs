using System;

namespace ANIMOL.Core
{
    public enum RunSessionState
    {
        Idle,
        Loading,
        Countdown,
        Playing,
        ExitEligible,
        TimeExpired,
        MapResolved,
        Result
    }

    public sealed class RunSessionController
    {
        public string RunId { get; private set; } = string.Empty;
        public string StageId { get; private set; } = string.Empty;
        public RunSessionState State { get; private set; } = RunSessionState.Idle;
        public bool IsDevelopmentRun { get; private set; }

        public void BeginLoading(string runId, string stageId, bool isDevelopmentRun)
        {
            if (State != RunSessionState.Idle && State != RunSessionState.Result)
                throw new InvalidOperationException($"Cannot load from {State}.");
            RunId = runId;
            StageId = stageId;
            IsDevelopmentRun = isDevelopmentRun;
            State = RunSessionState.Loading;
        }

        public void BeginCountdown() => Transition(RunSessionState.Loading, RunSessionState.Countdown);
        public void BeginPlaying() => Transition(RunSessionState.Countdown, RunSessionState.Playing);

        public void MarkExitEligible()
        {
            if (State == RunSessionState.ExitEligible) return;
            Transition(RunSessionState.Playing, RunSessionState.ExitEligible);
        }

        public void ResolveMap() => Transition(RunSessionState.ExitEligible, RunSessionState.MapResolved);
        public void MarkTimeExpired()
        {
            if (State != RunSessionState.Playing && State != RunSessionState.ExitEligible)
                throw new InvalidOperationException($"Cannot expire from {State}.");
            State = RunSessionState.TimeExpired;
        }
        public void ShowResult()
        {
            if (State != RunSessionState.MapResolved && State != RunSessionState.TimeExpired)
                throw new InvalidOperationException($"Cannot show result from {State}.");
            State = RunSessionState.Result;
        }

        private void Transition(RunSessionState expected, RunSessionState next)
        {
            if (State != expected) throw new InvalidOperationException($"Expected {expected}, found {State}.");
            State = next;
        }
    }
}
