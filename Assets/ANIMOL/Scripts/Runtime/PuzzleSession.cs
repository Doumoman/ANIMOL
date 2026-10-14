using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Authoritative grid state. Renderers/camera cannot collect, win, or decide collisions.
    public sealed class PuzzleSession
    {
        public PuzzleMap Map { get; }
        public Vector2Int Cell { get; private set; }
        public Vector2Int Direction { get; private set; } = Vector2Int.up;
        public int Collected => collected.Count;
        public int Moves { get; private set; }
        public bool Won { get; private set; }
        public bool Sliding { get; private set; }
        readonly HashSet<Vector2Int> collected = new HashSet<Vector2Int>();
        public event Action Changed;
        public static bool IsCardinal(Vector2Int d) => Mathf.Abs(d.x) + Mathf.Abs(d.y) == 1;
        public PuzzleSession(PuzzleMap map)
        {
            Map = map; Cell = map.start;
            if (!map.Walkable(Cell)) throw new ArgumentException("Invalid puzzle start.");
            Arrive(Cell);
        }
        public bool IsCollected(Vector2Int p) => collected.Contains(p);
        public bool BeginMove(Vector2Int direction)
        {
            if (Won || !IsCardinal(direction) || !Map.Walkable(Cell + direction)) return false;
            Direction = direction; Sliding = false; Moves++; Changed?.Invoke(); return true;
        }
        public void Arrive(Vector2Int p)
        {
            if (!Map.Walkable(p)) throw new InvalidOperationException("Cannot enter blocked cell " + p);
            Cell = p;
            if (Map.bubbles.Contains(p)) collected.Add(p);
            Won = Cell == Map.exit && Collected >= Map.targetBubbles;
            Changed?.Invoke();
        }
        // A portal arrival is evaluated once with the incoming momentum intact.
        public bool NextAutomatic(bool portalArrival, out Vector2Int destination, out bool teleport)
        {
            destination = Cell; teleport = false;
            if (Won) return false;
            var d = Map.DeviceAt(Cell);
            if (d.kind == DeviceKind.Portal && !portalArrival)
            {
                if (!Map.Walkable(d.portalTarget) || d.portalTarget == Cell) return false;
                destination = d.portalTarget; teleport = true; return true;
            }
            if (d.kind == DeviceKind.Brake) { Sliding = false; return false; }
            if (d.kind == DeviceKind.Conveyor || d.kind == DeviceKind.Wind)
            { Direction = d.direction; Sliding = false; }
            else if (d.kind == DeviceKind.Ice) Sliding = true;
            else if (!(portalArrival && Sliding)) { Sliding = false; return false; }
            destination = Cell + Direction;
            return IsCardinal(Direction) && Map.Walkable(destination);
        }
        public void StopSliding() { Sliding = false; }
    }
}
