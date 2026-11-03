using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    /// <summary>The real host publishes ONLY after its authoritative edit/load/undo/restart has committed.</summary>
    [DisallowMultipleComponent]
    public sealed class SanctumConnectedDecorV01PublishedSnapshotSource : SanctumConnectedDecorV01SnapshotSource
    {
        private SanctumConnectedDecorV01BoardSnapshot current;
        private string unavailableReason = "The authoritative host has not published a whole decoration snapshot.";
        public override bool IsExampleSource { get { return false; } }
        protected override void OnDisable()
        { InvalidateSnapshot("Authoritative source disabled; the host must publish a fresh snapshot before reuse."); }
        public bool PublishSnapshot(SanctumConnectedDecorV01BoardSnapshot value, out string error)
        {
            if (value == null) { error = "Missing authoritative snapshot."; InvalidateSnapshot(error); return false; }
            var next = value.DeepCopy();
            if (!next.TryValidate(out error)) { InvalidateSnapshot(error); return false; }
            if (next.isExampleOnly) { error = "Example-only data cannot be published as authoritative game state."; InvalidateSnapshot(error); return false; }
            current = next; unavailableReason = null; RaiseChanged(); return true;
        }
        public void InvalidateSnapshot(string reason)
        { current = null; unavailableReason = string.IsNullOrWhiteSpace(reason) ? "Authoritative decoration snapshot invalidated." : reason; RaiseChanged(); }
        public override bool TryCaptureSnapshot(out SanctumConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (current == null) { error = unavailableReason; return false; }
            var copy = current.DeepCopy();
            if (!copy.TryValidate(out error) || copy.isExampleOnly) { error = error ?? "Example-only authoritative snapshot rejected."; return false; }
            snapshot = copy; return true;
        }
    }
}
