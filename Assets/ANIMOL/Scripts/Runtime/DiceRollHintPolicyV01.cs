using System;
using UnityEngine;
using Die = ANIMOL.Art.EchoTheaterV03.EchoTheaterV03DiceOrientation;

namespace ANIMOL.Presentation.DiceHintV01
{
    [Flags]
    public enum DiceRollMask { None = 0, North = 1, East = 2, South = 4, West = 8 }

    // A read-only copy from the host. LegalRolls must come from the actual push resolver.
    public struct DiceHintSnapshot
    {
        public long Revision;
        public bool DiceExists, InputReady, DiceSettled;
        public Vector2Int PlayerCell, DiceCell;
        public Die.State Faces;
        public bool MirroredNorthSouth;
        public DiceRollMask LegalRolls;
        public DiceGoalFaceViewV02 DestinationGoal; // display link for the actual destination goal, or null
    }

    public struct DiceRollHint
    {
        public Vector2Int Destination;
        public Die.Heading Direction;
        public int NextTop;
    }

    public static class DiceRollHintPolicyV01
    {
        public static bool TryEvaluate(DiceHintSnapshot snapshot, out DiceRollHint hint)
        {
            hint = default(DiceRollHint);
            if (!snapshot.DiceExists || !snapshot.InputReady || !snapshot.DiceSettled) return false;
            Vector2Int delta = snapshot.DiceCell - snapshot.PlayerCell;
            // Only an input the player can perform NOW, never a hypothetical push from another side.
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1) return false;
            Die.Heading heading;
            DiceRollMask flag;
            if (delta == Vector2Int.up) { heading = Die.Heading.North; flag = DiceRollMask.North; }
            else if (delta == Vector2Int.right) { heading = Die.Heading.East; flag = DiceRollMask.East; }
            else if (delta == Vector2Int.down) { heading = Die.Heading.South; flag = DiceRollMask.South; }
            else { heading = Die.Heading.West; flag = DiceRollMask.West; }
            if ((snapshot.LegalRolls & flag) == 0) return false;
            Die.Heading artHeading = snapshot.MirroredNorthSouth && heading == Die.Heading.North ? Die.Heading.South :
                snapshot.MirroredNorthSouth && heading == Die.Heading.South ? Die.Heading.North : heading;
            Die.State after; string error;
            if (!Die.TryPredictVisualRoll(snapshot.Faces, artHeading, true, false, out after, out error)) return false;
            hint = new DiceRollHint { Direction = heading, Destination = snapshot.DiceCell + delta, NextTop = after.Up };
            return true;
        }
    }
}
