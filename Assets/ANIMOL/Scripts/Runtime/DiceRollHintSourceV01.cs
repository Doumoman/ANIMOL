using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Implement against the actual session, or publish through DiceRollHintFeedV01.
    // Queries must not commit moves, spend cost, advance RNG/turns, or activate a goal.
    public abstract class DiceRollHintSourceV01 : MonoBehaviour
    {
        public abstract bool TryRead(out DiceHintSnapshot snapshot);
        public abstract Vector3 CellToWorld(Vector2Int cell);
    }
}
