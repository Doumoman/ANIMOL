using UnityEngine;

namespace ANIMOL.Art.TheaterConnectedDecorV01
{
    /// <summary>Explicit authored-board provider. Example assets retain their example flag and require an opt-in binding.</summary>
    [DisallowMultipleComponent]
    public sealed class TheaterConnectedDecorV01AuthoredSnapshotSource : TheaterConnectedDecorV01SnapshotSource
    {
        [SerializeField] private TheaterConnectedDecorV01BoardAsset board;
        public TheaterConnectedDecorV01BoardAsset Board { get { return board; } }
        public override bool IsExampleSource
        {
            get { TheaterConnectedDecorV01BoardSnapshot value; string error;
                return board != null && board.TryGetSnapshot(out value, out error) && value.isExampleOnly; }
        }
        public bool AssignBoard(TheaterConnectedDecorV01BoardAsset value, out string error)
        { board = value; return NotifyBoardChanged(out error); }
        public bool NotifyBoardChanged(out string error)
        {
            TheaterConnectedDecorV01BoardSnapshot ignored;
            bool valid = TryCaptureSnapshot(out ignored, out error); RaiseChanged(); return valid;
        }
        public override bool TryCaptureSnapshot(out TheaterConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (board == null) { error = "Assign a validated authored board asset explicitly."; return false; }
            return board.TryGetSnapshot(out snapshot, out error);
        }
    }
}
