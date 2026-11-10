using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    public static class BookshelfRotationRules
    {
        // The authored H->V frames turn counterclockwise; V->H is the same
        // frames backwards. Time reversal alone cannot select the other sweep.
        public static bool MirrorAnimation(int phase, BookshelfRotationDirection direction) =>
            ((phase & 1) == 0) == (direction == BookshelfRotationDirection.Clockwise);

        public static int NextPhase(int phase, BookshelfRotationDirection direction, bool twoState)
        {
            if (twoState) return 1 - (phase & 1);
            return ((phase + (direction == BookshelfRotationDirection.Clockwise ? 1 : -1)) % 4 + 4) % 4;
        }

        // A three-cell shelf sweeps two opposite quadrants in a quarter turn.
        // The center, old/new ends and these two corners form seven board cells.
        public static IEnumerable<Vector2Int> SweptCells(Vector2Int center, int phase, BookshelfRotationDirection direction)
        {
            yield return center;
            yield return center + Vector2Int.left; yield return center + Vector2Int.right;
            yield return center + Vector2Int.up; yield return center + Vector2Int.down;
            var clockwise = direction == BookshelfRotationDirection.Clockwise;
            var diagonal = new Vector2Int(1, ((phase & 1) == 0) == clockwise ? -1 : 1);
            yield return center + diagonal; yield return center - diagonal;
        }
    }
}
