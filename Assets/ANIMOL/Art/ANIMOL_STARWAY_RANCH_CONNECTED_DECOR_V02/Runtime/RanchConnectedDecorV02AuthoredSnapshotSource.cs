using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    /// <summary>Explicit authored-board provider. Example assets retain their example flag and require an opt-in binding.</summary>
    [DisallowMultipleComponent]
    public sealed class RanchConnectedDecorV02AuthoredSnapshotSource : RanchConnectedDecorV02SnapshotSource
    {
        [SerializeField] private RanchConnectedDecorV02BoardAsset board;
        public RanchConnectedDecorV02BoardAsset Board { get { return board; } }
        public override bool IsExampleSource
        {
            get { RanchConnectedDecorV02BoardSnapshot value; string error;
                return board != null && board.TryGetSnapshot(out value, out error) && value.isExampleOnly; }
        }
        public bool AssignBoard(RanchConnectedDecorV02BoardAsset value, out string error)
        { board = value; return NotifyBoardChanged(out error); }
        public bool NotifyBoardChanged(out string error)
        {
            RanchConnectedDecorV02BoardSnapshot ignored;
            bool valid = TryCaptureSnapshot(out ignored, out error); RaiseChanged(); return valid;
        }
        public override bool TryCaptureSnapshot(out RanchConnectedDecorV02BoardSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (board == null) { error = "Assign a validated authored board asset explicitly."; return false; }
            return board.TryGetSnapshot(out snapshot, out error);
        }
    }
}
