using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    /// <summary>Explicit authored-board provider. Example assets retain their example flag and require an opt-in binding.</summary>
    [DisallowMultipleComponent]
    public sealed class SanctumConnectedDecorV01AuthoredSnapshotSource : SanctumConnectedDecorV01SnapshotSource
    {
        [SerializeField] private SanctumConnectedDecorV01BoardAsset board;
        public SanctumConnectedDecorV01BoardAsset Board { get { return board; } }
        public override bool IsExampleSource
        {
            get { SanctumConnectedDecorV01BoardSnapshot value; string error;
                return board != null && board.TryGetSnapshot(out value, out error) && value.isExampleOnly; }
        }
        public bool AssignBoard(SanctumConnectedDecorV01BoardAsset value, out string error)
        { board = value; return NotifyBoardChanged(out error); }
        public bool NotifyBoardChanged(out string error)
        {
            SanctumConnectedDecorV01BoardSnapshot ignored;
            bool valid = TryCaptureSnapshot(out ignored, out error); RaiseChanged(); return valid;
        }
        public override bool TryCaptureSnapshot(out SanctumConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (board == null) { error = "Assign a validated authored board asset explicitly."; return false; }
            return board.TryGetSnapshot(out snapshot, out error);
        }
    }
}
