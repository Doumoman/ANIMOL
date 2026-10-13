namespace ANIMOL.Core
{
    public sealed class ExitGateController
    {
        private readonly RunSessionController session;
        private readonly BubbleObjectiveService objectives;
        private bool resolved;

        public bool IsOpen => objectives.IsExitEligible;

        public ExitGateController(RunSessionController session, BubbleObjectiveService objectives)
        {
            this.session = session;
            this.objectives = objectives;
            objectives.ExitEligible += session.MarkExitEligible;
        }

        public bool TryEnter()
        {
            if (resolved || !IsOpen || session.State != RunSessionState.ExitEligible) return false;
            resolved = true;
            session.ResolveMap();
            return true;
        }
    }
}
