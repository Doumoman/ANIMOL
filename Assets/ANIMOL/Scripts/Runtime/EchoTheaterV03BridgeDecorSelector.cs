// Pure C# port of selectBridgeDecor(rows) in build_reference_bridge_decor.cjs.
// Display only: no occupancy, movement or collider changes.
// XPixel/YPixel are PNG top-left pixels from the logical map top-left.
// With PPU256/pivot(0,1): world=(origin.x+XPixel/256,origin.y-YPixel/256).
using System;
using System.Collections.Generic;

namespace ANIMOL.Art.EchoTheaterV03
{
    public static class EchoTheaterV03BridgeDecorSelector
    {
        public const int PixelsPerUnit = 256;
        public const int PostIntervalCells = 4;
        public const string HorizontalRailFile = "bridge_decor/theater_rope_rail_horizontal.png";
        public const string VerticalRailFile = "bridge_decor/theater_rope_rail_vertical.png";
        public const string PostFile = "bridge_decor/theater_brass_wood_post.png";

        public sealed class Decoration
        {
            public string File, Kind, Direction, RunId;
            public int XPixel, YPixel, Layer, WidthPixels, HeightPixels;
            public float PivotX, PivotY;
            // Vertex is valid only for Kind == "post"; logical grid corners.
            public int VertexX, VertexY;
        }

        private sealed class Edge
        {
            public char Axis, Direction;
            public int Line, Position;
        }

        private sealed class Run
        {
            public char Axis, Direction;
            public int Line, Start, End;
        }

        private static bool Land(string[] rows, int x, int y)
        { return EchoTheaterV03EdgeSelector.MaterialAt(rows, x, y) != '0'; }

        public static List<Decoration> Select(string[] rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            List<Edge> edges = new List<Edge>();
            for (int y = 0; y < rows.Length; y++)
            {
                int width = rows[y] == null ? 0 : rows[y].Length;
                for (int x = 0; x < width; x++)
                {
                    char material = EchoTheaterV03EdgeSelector.MaterialAt(rows, x, y);
                    if (material != 'W') continue;
                    if (!Land(rows, x, y - 1)) edges.Add(new Edge { Axis = 'h', Line = y, Position = x, Direction = 'N' });
                    if (!Land(rows, x + 1, y)) edges.Add(new Edge { Axis = 'v', Line = x + 1, Position = y, Direction = 'E' });
                    if (!Land(rows, x, y + 1)) edges.Add(new Edge { Axis = 'h', Line = y + 1, Position = x, Direction = 'S' });
                    if (!Land(rows, x - 1, y)) edges.Add(new Edge { Axis = 'v', Line = x, Position = y, Direction = 'W' });
                }
            }
            edges.Sort((a, b) => {
                int order = a.Axis.CompareTo(b.Axis);
                if (order != 0) return order;
                order = a.Line.CompareTo(b.Line);
                if (order != 0) return order;
                order = a.Direction.CompareTo(b.Direction);
                return order != 0 ? order : a.Position.CompareTo(b.Position);
            });

            List<Run> runs = new List<Run>();
            foreach (Edge edge in edges)
            {
                Run previous = runs.Count == 0 ? null : runs[runs.Count - 1];
                if (previous != null && previous.Axis == edge.Axis && previous.Line == edge.Line
                    && previous.Direction == edge.Direction && previous.End == edge.Position)
                    previous.End++;
                else runs.Add(new Run {
                    Axis = edge.Axis, Line = edge.Line, Direction = edge.Direction,
                    Start = edge.Position, End = edge.Position + 1
                });
            }

            List<Decoration> output = new List<Decoration>();
            Dictionary<string, Decoration> posts = new Dictionary<string, Decoration>(StringComparer.Ordinal);
            foreach (Run run in runs)
            {
                bool horizontal = run.Axis == 'h';
                string runId = run.Direction + "_" + run.Line + "_" + run.Start + "_" + run.End;
                int layer = run.Direction == 'N' ? 12 : run.Direction == 'S' ? 16 : 14;
                for (int p = run.Start; p < run.End; p++)
                    output.Add(new Decoration {
                        File = horizontal ? HorizontalRailFile : VerticalRailFile,
                        XPixel = horizontal ? p * PixelsPerUnit : run.Line * PixelsPerUnit - 20,
                        YPixel = horizontal ? run.Line * PixelsPerUnit - 20 : p * PixelsPerUnit,
                        Layer = layer, Kind = "rail", Direction = run.Direction.ToString(), RunId = runId,
                        WidthPixels = horizontal ? 256 : 40, HeightPixels = horizontal ? 40 : 256,
                        PivotX = 0f, PivotY = 1f
                    });
                // Shared run endpoints/turns receive one post. Long straight
                // rails get one more every four cells, never every cell.
                AddPost(posts, run, run.Start, runId);
                AddPost(posts, run, run.End, runId);
                for (int p = run.Start + PostIntervalCells; p < run.End; p += PostIntervalCells)
                    AddPost(posts, run, p, runId);
            }
            output.AddRange(posts.Values);
            output.Sort((a, b) => {
                int order = a.Layer.CompareTo(b.Layer);
                if (order != 0) return order;
                order = a.YPixel.CompareTo(b.YPixel);
                if (order != 0) return order;
                order = a.XPixel.CompareTo(b.XPixel);
                return order != 0 ? order : string.CompareOrdinal(a.File, b.File);
            });
            return output;
        }

        private static void AddPost(Dictionary<string, Decoration> posts, Run run, int p, string runId)
        {
            int vx = run.Axis == 'h' ? p : run.Line;
            int vy = run.Axis == 'h' ? run.Line : p;
            string key = vx + "," + vy;
            if (posts.ContainsKey(key)) return;
            posts.Add(key, new Decoration {
                File = PostFile, XPixel = vx * PixelsPerUnit - 40,
                YPixel = vy * PixelsPerUnit - 58,
                Layer = 18, Kind = "post", Direction = run.Direction.ToString(), RunId = runId,
                WidthPixels = 80, HeightPixels = 136, PivotX = 0f, PivotY = 1f,
                VertexX = vx, VertexY = vy
            });
        }
    }
}
