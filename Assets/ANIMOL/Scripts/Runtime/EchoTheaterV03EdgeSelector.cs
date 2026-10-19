// Pure C# topology and deterministic span selection. No UnityEditor dependency.
// Full map rows: x-right/y-down, G=stage floor, W=catwalk, S=carpet on existing floor, anything else=void.
using System;
using System.Collections.Generic;

namespace ANIMOL.Art.EchoTheaterV03
{
    public static class EchoTheaterV03EdgeSelector
    {
        public const int N = 1, NE = 2, E = 4, SE = 8;
        public const int S = 16, SW = 32, W = 64, NW = 128;
        public const int ExportedVariantCount = 2;
        public enum EdgeMode { Continue, End, Concave }

        public sealed class EdgeRun
        {
            public int StartX, Y, Length, Seed, WorldX, WorldY;
            public char MaterialCode;
            public string Material, Strategy;
            public bool Hole;
            public EdgeMode LeftMode, RightMode;
            public int[] Masks;
        }

        public sealed class Span
        {
            public int StartOffset, Length, Variant;
            public string Kind, DepthClass, File;
            public EdgeMode LeftMode, RightMode;
        }

        public static char MaterialAt(string[] rows, int x, int y)
        {
            if (rows == null || y < 0 || y >= rows.Length || rows[y] == null
                || x < 0 || x >= rows[y].Length) return '0';
            char value = rows[y][x];
            return value == 'G' || value == 'W' || value == 'S' ? value : '0';
        }

        // Carpet changes the top appearance, never the supporting structure.
        private static char SupportMaterialAt(string[] rows, int x, int y)
        {
            char material = MaterialAt(rows, x, y);
            return material == 'S' ? 'G' : material;
        }

        public static string MaterialName(char code)
        {
            return code == 'G' ? "stage_floor" : code == 'W' ? "wood_catwalk"
                : code == 'S' ? "velvet_carpet" : null;
        }

        public static int NeighborMask(Func<int, int, bool> at, int x, int y)
        {
            if (at == null) throw new ArgumentNullException(nameof(at));
            return (at(x, y - 1) ? N : 0) | (at(x + 1, y - 1) ? NE : 0)
                | (at(x + 1, y) ? E : 0) | (at(x + 1, y + 1) ? SE : 0)
                | (at(x, y + 1) ? S : 0) | (at(x - 1, y + 1) ? SW : 0)
                | (at(x - 1, y) ? W : 0) | (at(x - 1, y - 1) ? NW : 0);
        }

        // Cardinal flood fill of EMPTY space, using ALL G/W/S as solid ground.
        // Every run is maximal along an exposed SOUTH face, split by material
        // and exterior/hole class. Do not pass a camera crop or an isolated chunk.
        public static List<EdgeRun> SelectRuns(string[] rows, int seed = 0,
            int originX = 0, int originY = 0)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            int height = rows.Length, width = 0;
            for (int y = 0; y < height; y++)
                width = Math.Max(width, rows[y] == null ? 0 : rows[y].Length);
            Func<int, int, bool> land = (x, y) => MaterialAt(rows, x, y) != '0';
            Func<int, int, bool> exposed = (x, y) => land(x, y) && !land(x, y + 1);
            int ew = width + 2, eh = height + 2;
            bool[,] exterior = new bool[ew, eh];
            Queue<int> queue = new Queue<int>();
            exterior[0, 0] = true;
            queue.Enqueue(0);
            int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
            while (queue.Count > 0)
            {
                int p = queue.Dequeue(), x = p % ew, y = p / ew;
                for (int d = 0; d < 4; d++)
                {
                    int a = x + dx[d], b = y + dy[d];
                    if (a < 0 || b < 0 || a >= ew || b >= eh
                        || exterior[a, b] || land(a - 1, b - 1)) continue;
                    exterior[a, b] = true;
                    queue.Enqueue(b * ew + a);
                }
            }

            List<EdgeRun> result = new List<EdgeRun>();
            for (int y = 0; y < height; y++) for (int x = 0; x < width;)
            {
                if (!exposed(x, y)) { x++; continue; }
                int startX = x;
                char material = SupportMaterialAt(rows, x, y);
                bool hole = !exterior[x + 1, y + 2];
                while (x < width && exposed(x, y)
                    && SupportMaterialAt(rows, x, y) == material
                    && (!exterior[x + 1, y + 2] == hole)) x++;
                int length = x - startX;
                int[] masks = new int[length];
                for (int i = 0; i < length; i++)
                    masks[i] = NeighborMask(land, startX + i, y);
                result.Add(new EdgeRun {
                    StartX = startX, Y = y, Length = length, Seed = seed,
                    WorldX = unchecked(startX + originX),
                    WorldY = unchecked(y + originY), MaterialCode = material,
                    Material = MaterialName(material), Hole = hole, Masks = masks,
                    LeftMode = SideMode(land, startX, y, -1),
                    RightMode = SideMode(land, x - 1, y, 1),
                    Strategy = hole && material != 'W' ? "shallow_collar"
                        : length <= 4 ? "fused"
                        : material != 'W' ? "spaced_clusters" : "continuous_structure"
                });
            }
            return result;
        }

        private static EdgeMode SideMode(Func<int, int, bool> land,
            int x, int y, int direction)
        {
            if (!land(x + direction, y)) return EdgeMode.End;
            if (land(x + direction, y + 1)) return EdgeMode.Concave;
            return EdgeMode.Continue;
        }

        public static string FileForSpan(string material, bool hole, int length,
            EdgeMode leftMode, EdgeMode rightMode, int variant)
        {
            if ((material != "stage_floor" && material != "wood_catwalk"
                && material != "velvet_carpet") || length < 1 || length > 4)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (variant < 0 || variant >= ExportedVariantCount)
                throw new ArgumentOutOfRangeException(nameof(variant));
            return "skirts/spans/" + material + "/" + (hole ? "hole" : "exterior")
                + "/l" + length + "_L" + leftMode.ToString().ToLowerInvariant()
                + "_R" + rightMode.ToString().ToLowerInvariant() + "_v" + variant + ".png";
        }

        // 1..4 stage-floor cells -> ONE fused body. Longer stage floor -> 1..3-cell bodies
        // with 1..2-cell gaps; the shallow collar remains continuous everywhere.
        // Enclosed stage-floor holes -> collar alone. Catwalks never have structural gaps.
        // Call order and unsigned PRNG match the exported JavaScript planner.
        public static List<Span> PlanSpans(EdgeRun run, int variantCount = 2)
        {
            if (run == null || run.Length < 1)
                throw new ArgumentException("run must have a positive length", nameof(run));
            if (variantCount < 1 || variantCount > ExportedVariantCount)
                throw new ArgumentOutOfRangeException(nameof(variantCount));
            List<Span> result = new List<Span>();
            if (run.MaterialCode != 'W' && run.Hole) return result;
            RandomStream random = new RandomStream(run);
            Action<int, int, string> add = (offset, length, kind) => {
                EdgeMode left = offset == 0 ? run.LeftMode : EdgeMode.Continue;
                EdgeMode right = offset + length == run.Length
                    ? run.RightMode : EdgeMode.Continue;
                int variant = random.Next(variantCount);
                string depth = run.Hole ? "shallow"
                    : random.Next(2) == 0 ? "medium" : "deep";
                result.Add(new Span {
                    StartOffset = offset, Length = length, Variant = variant,
                    LeftMode = left, RightMode = right, Kind = kind, DepthClass = depth,
                    File = FileForSpan(run.Material, run.Hole, length, left, right, variant)
                });
            };
            if (run.Length <= 4) { add(0, run.Length, "fused"); return result; }
            if (run.MaterialCode == 'W')
            {
                int cursor = 0;
                while (cursor < run.Length)
                {
                    int remaining = run.Length - cursor;
                    List<int> possible = new List<int>();
                    for (int n = 2; n <= 4; n++)
                        if (n <= remaining && (remaining - n == 0 || remaining - n >= 2))
                            possible.Add(n);
                    int length = possible[random.Next(possible.Count)];
                    add(cursor, length, "continuous_structure");
                    cursor += length;
                }
                return result;
            }
            int position = random.Next(Math.Min(2, run.Length - 3));
            while (position < run.Length)
            {
                int remaining = run.Length - position;
                if (remaining < 1) break;
                int maxLength = Math.Min(3, result.Count == 0 ? remaining - 2 : remaining);
                int length = 1 + random.Next(Math.Max(1, maxLength));
                add(position, length, "hanging_cluster");
                position += length;
                int after = run.Length - position;
                if (after < 2) break;
                position += 1 + random.Next(Math.Min(2, after - 1));
            }
            return result;
        }

        private sealed class RandomStream
        {
            private uint state;
            public RandomStream(EdgeRun run)
            {
                state = StableHash(run.WorldX, run.WorldY,
                    unchecked(run.Seed ^ (run.Length * (int)0x27d4eb2d) ^ run.MaterialCode));
            }
            public int Next(int maximum)
            {
                if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
                unchecked
                {
                    state += 0x9e3779b9u;
                    uint z = state;
                    z ^= z >> 16; z *= 0x7feb352du; z ^= z >> 15;
                    z *= 0x846ca68bu;
                    return (int)((z ^ (z >> 16)) % (uint)maximum);
                }
            }
        }

        public static uint StableHash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed ^ ((uint)x * 0x9e3779b9u) ^ ((uint)y * 0x85ebca6bu);
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15;
                h *= 0x846ca68bu; return h ^ (h >> 16);
            }
        }
    }
}
