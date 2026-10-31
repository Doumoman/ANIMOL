using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Event-driven bridge, one per die. A project may instead implement a live read-only Source.
    public sealed class DiceRollHintFeedV01 : DiceRollHintSourceV01
    {
        [SerializeField] Transform boardSpace;
        [SerializeField] Vector2 localCellOrigin;
        [SerializeField] float cellSize = 1;
        DiceHintSnapshot current;
        bool published;

        public void ConfigureGrid(Transform space, Vector2 origin, float size)
        {
            boardSpace = space; localCellOrigin = origin; cellSize = size; Invalidate();
        }
        public void Publish(DiceHintSnapshot snapshot) { current = snapshot; published = true; }
        // Call BEFORE movement, turn resolution, Undo/Reset/load, dialogue/pause, removal and pool return.
        public void Invalidate() { published = false; }
        public override bool TryRead(out DiceHintSnapshot snapshot)
        {
            snapshot = current;
            return published && cellSize > 0 && !float.IsNaN(cellSize) && !float.IsInfinity(cellSize);
        }
        public override Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 local = localCellOrigin + (Vector2)cell * cellSize;
            return boardSpace == null ? local : boardSpace.TransformPoint(local);
        }
        void OnDisable() { Invalidate(); }
    }
}
