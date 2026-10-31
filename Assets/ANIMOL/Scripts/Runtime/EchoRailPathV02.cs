using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.EchoRailCartV01
{
    // Presentation geometry in logical cell units; never reads or writes gameplay occupancy.
    public sealed class EchoRailPathV02
    {
        public const float Gauge = 160f / 256f;
        public const float Wheelbase = 128f / 256f;
        public const float HeadWidth = 20f / 256f;
        public const float SleeperPitch = 64f / 256f;
        public sealed class Segment
        {
            public Vector2 start, end, center, tangent;
            public float fromDistance, length, startAngle, turn;
            public bool curved;
        }
        readonly List<Segment> segments = new List<Segment>();
        public bool Closed { get; private set; }
        public float Length { get; private set; }
        public int CellCount { get { return segments.Count; } }
        public float CellDistance(int index) { return segments[index].fromDistance + segments[index].length * .5f; }
        static bool Cardinal(Vector2Int d) { return Mathf.Abs(d.x) + Mathf.Abs(d.y) == 1; }
        public static bool TryCreate(Vector2Int[] cells, bool closed, out EchoRailPathV02 path, out string error)
        {
            path = null; error = null;
            if (cells == null || cells.Length < (closed ? 4 : 2) || cells.Length > 4096)
            { error = "Supply 2+ open or 4+ closed ordered cells (maximum 4096)."; return false; }
            var unique = new HashSet<Vector2Int>();
            for (int i = 0; i < cells.Length; i++)
            {
                if (Math.Abs((long)cells[i].x) > 100000 || Math.Abs((long)cells[i].y) > 100000 || !unique.Add(cells[i]))
                { error = "Use unique cells within the art coordinate range."; return false; }
                if (i > 0 && !Cardinal(cells[i] - cells[i - 1])) { error = "Track cells must be cardinal neighbors."; return false; }
            }
            if (closed && !Cardinal(cells[0] - cells[cells.Length - 1])) { error = "Closed track must reconnect."; return false; }
            var p = new EchoRailPathV02 { Closed = closed };
            for (int i = 0; i < cells.Length; i++)
            {
                Vector2Int incoming = i > 0 ? cells[i] - cells[i - 1] : closed ? cells[0] - cells[cells.Length - 1] : cells[1] - cells[0];
                Vector2Int outgoing = i + 1 < cells.Length ? cells[i + 1] - cells[i] : closed ? cells[0] - cells[i] : incoming;
                if (incoming.x * outgoing.x + incoming.y * outgoing.y < 0) { error = "A U-turn needs a different route, not an overlapping cell."; return false; }
                Vector2 c = cells[i]; Vector2 a = incoming, b = outgoing;
                var s = new Segment { start = c - a * .5f, end = c + b * .5f, fromDistance = p.Length };
                if (incoming == outgoing) { s.tangent = a; s.length = 1; }
                else
                {
                    s.curved = true; s.center = c - a * .5f + b * .5f;
                    Vector2 radial = s.start - s.center;
                    s.startAngle = Mathf.Atan2(radial.y, radial.x);
                    s.turn = a.x * b.y - a.y * b.x; s.length = Mathf.PI * .25f;
                }
                p.segments.Add(s); p.Length += s.length;
            }
            path = p; return true;
        }
        public void Sample(float distance, out Vector2 position, out Vector2 tangent)
        {
            float d = Closed ? Mathf.Repeat(distance, Length) : Mathf.Clamp(distance, 0, Length);
            Segment s = segments[segments.Count - 1];
            foreach (var candidate in segments) if (d < candidate.fromDistance + candidate.length) { s = candidate; break; }
            float u = Mathf.Clamp01((d - s.fromDistance) / s.length);
            if (!s.curved) { position = Vector2.Lerp(s.start, s.end, u); tangent = s.tangent; return; }
            float angle = s.startAngle + s.turn * Mathf.PI * .5f * u;
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            position = s.center + radial * .5f; tangent = s.turn * new Vector2(-radial.y, radial.x);
        }
        public int SleeperCount { get { return Closed ? Mathf.Max(1, Mathf.RoundToInt(Length / SleeperPitch)) : Mathf.Max(1, Mathf.FloorToInt(Length / SleeperPitch)); } }
        public float ActualSleeperSpacing { get { return Closed ? Length / SleeperCount : SleeperPitch; } }
        public float SleeperDistance(int index) { return (index + .5f) * ActualSleeperSpacing; }
        public bool TryStepDistance(int from, int to, float progress, out float distance)
        {
            distance = 0;
            if (from < 0 || to < 0 || from >= CellCount || to >= CellCount || float.IsNaN(progress) || float.IsInfinity(progress)) return false;
            float a = CellDistance(from), b = CellDistance(to);
            if (from == to) { distance = a; return true; }
            if (to == from + 1 || (Closed && from == CellCount - 1 && to == 0)) { if (b < a) b += Length; }
            else if (to == from - 1 || (Closed && from == 0 && to == CellCount - 1)) { if (b > a) b -= Length; }
            else return false;
            distance = Mathf.Lerp(a, b, Mathf.Clamp01(progress)); return true;
        }
    }
}
