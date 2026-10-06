using System;
using System.Collections.Generic;

namespace Animol.GraphicJoin
{
    // Pure graphics contract: no Unity component, collider or authored map is mutated.
    public sealed class GraphicCell
    {
        public string Kind, ThemeId, StyleId, JoinGroup = "default", Facing = "UP", Pose = "idle";
        public int ArtVersion = 4;
        public bool? SurfaceEnabled;
    }

    public sealed class GraphicPlan
    {
        public int X, Y, BodyRaw, BodyMask, BodyIndex, CapRaw, CapMask, CapIndex, Variant;
        public bool DrawBody, DrawCap, CapOnly, CapPatch;
        public const int CapPixels = 8;
        public string OverlayKey, CacheKey, Pose, Facing;
        public readonly List<string> Problems = new List<string>();
    }

    public readonly struct GraphicCoordinate
    {
        public readonly int X, Y;
        public GraphicCoordinate(int x, int y) { X = x; Y = y; }
    }

    public sealed class GraphicInvalidation
    {
        public readonly List<GraphicCoordinate> Cells = new List<GraphicCoordinate>();
        public readonly List<GraphicCoordinate> MotifAnchors = new List<GraphicCoordinate>();
    }

    public static class ObstacleVisualResolver
    {
        static readonly HashSet<string> Full = new HashSet<string> { "C02", "C03", "C08", "C10", "R03" };
        static readonly HashSet<string> Top = new HashSet<string> { "C01", "C05", "C06", "C09", "M02", "R01" };
        static readonly HashSet<string> Air = new HashSet<string> { "C04", "C07" };
        static readonly int[] Masks = { 0,1,4,5,7,16,17,20,21,23,28,29,31,64,65,68,69,71,80,81,84,85,87,92,93,95,112,113,116,117,119,124,125,127,193,197,199,209,213,215,221,223,241,245,247,253,255 };
        static readonly int[] Dx = { 0,1,1,1,0,-1,-1,-1 };
        static readonly int[] Dy = { 1,1,0,-1,-1,-1,0,1 };

        public static bool SameMaterial(GraphicCell a, GraphicCell b) => a != null && b != null &&
            a.ThemeId == b.ThemeId && a.StyleId == b.StyleId && a.ArtVersion == b.ArtVersion &&
            (a.JoinGroup ?? "default") == (b.JoinGroup ?? "default");
        public static bool IsFull(GraphicCell c) => c != null && (c.Kind == "terrain" || Full.Contains(c.Kind));
        public static bool HasTop(GraphicCell c) => IsFull(c) ||
            (c != null && Top.Contains(c.Kind) && c.SurfaceEnabled == true);
        public static int Canonicalize(int raw)
        {
            if (raw < 0 || raw > 255) throw new ArgumentOutOfRangeException(nameof(raw));
            int result = raw;
            if ((raw & 1) == 0 || (raw & 4) == 0) result &= ~2;
            if ((raw & 16) == 0 || (raw & 4) == 0) result &= ~8;
            if ((raw & 16) == 0 || (raw & 64) == 0) result &= ~32;
            if ((raw & 1) == 0 || (raw & 64) == 0) result &= ~128;
            return result;
        }
        static int FloorMod(int x, int d) => ((x % d) + d) % d;
        public static int Variant(int x, int y, int seed) => FloorMod(x + (seed & 1), 2) +
            2 * FloorMod(-y + ((seed >> 1) & 1), 2);
        static string Pose(GraphicCell c)
        {
            var value = c.Pose ?? "idle";
            if (value == "pending") return "warn";
            if (value != "idle" && value != "warn" && value != "active" && value != "inactive")
                throw new ArgumentException("Unknown visual pose");
            return value;
        }
        static void Validate(GraphicCell c)
        {
            if (c.ArtVersion != 4 || c.StyleId == null || c.StyleId.Length != 5 ||
                c.StyleId[0] != 'T' || c.StyleId[1] != '0' || c.StyleId[2] < '1' || c.StyleId[2] > '5' ||
                c.StyleId[3] != '_' || c.StyleId[4] < 'A' || c.StyleId[4] > 'D' ||
                c.ThemeId != c.StyleId.Substring(0, 3)) throw new ArgumentException("Matching v4 style/theme required");
            if (c.Kind != "terrain" && !Full.Contains(c.Kind) && !Top.Contains(c.Kind) && !Air.Contains(c.Kind))
                throw new ArgumentException("Unknown kind");
            if (Top.Contains(c.Kind) && !c.SurfaceEnabled.HasValue) throw new ArgumentException("TOP requires authoritative surface state");
            if (c.Kind == "M02" && c.ThemeId != "T01") throw new ArgumentException("M02 is a moon candidate");
            if ((c.Kind == "R01" || c.Kind == "R03") && c.ThemeId != "T05") throw new ArgumentException("Mine candidate required");
            if ((c.Kind == "C02" || c.Kind == "C08") && c.Facing != "LEFT" && c.Facing != "RIGHT")
                throw new ArgumentException("Side facing required");
            if (c.Kind == "C07" && c.Facing != "LEFT" && c.Facing != "RIGHT" && c.Facing != "UP")
                throw new ArgumentException("Wind direction required");
        }

        public static GraphicPlan Resolve(Func<int, int, GraphicCell> read, int x, int y, int seed)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            var c = read(x, y); if (c == null) return null; Validate(c);
            Func<int, int, bool> bodyAt = (a, b) => SameMaterial(c, read(a,b)) && IsFull(read(a,b));
            Func<int, int, bool> topAt = (a, b) => SameMaterial(c, read(a,b)) && HasTop(read(a,b));
            int raw = 0;
            if (IsFull(c)) for (int i = 0; i < 8; i++) if (bodyAt(x + Dx[i], y + Dy[i])) raw |= 1 << i;
            int capRaw = raw;
            if (HasTop(c)) { if (topAt(x - 1, y)) capRaw |= 64; if (topAt(x + 1, y)) capRaw |= 4; }
            int bodyMask = Canonicalize(raw), capMask = Canonicalize(capRaw);
            bool exposedTop = !bodyAt(x,y + 1);
            string facing = c.Kind == "C02" || c.Kind == "C07" || c.Kind == "C08" ? c.Facing : "UP";
            string pose = c.Kind == "terrain" ? "idle" : Pose(c);
            int variant = Variant(x,y,seed);
            var plan = new GraphicPlan {
                X=x, Y=y, BodyRaw=raw, BodyMask=bodyMask, BodyIndex=Array.IndexOf(Masks,bodyMask),
                CapRaw=capRaw, CapMask=capMask, CapIndex=Array.IndexOf(Masks,capMask), Variant=variant,
                DrawBody=IsFull(c), DrawCap=HasTop(c) && exposedTop, CapOnly=Top.Contains(c.Kind) && c.SurfaceEnabled == true,
                CapPatch=IsFull(c) && exposedTop && capMask != bodyMask, Pose=pose, Facing=facing,
                OverlayKey=c.Kind == "terrain" ? null : c.ThemeId+"/"+c.Kind+"/"+facing+"/"+pose,
                CacheKey=string.Join("|",4,c.StyleId,c.JoinGroup??"default",c.Kind,bodyMask,capMask,variant,facing,pose,c.SurfaceEnabled==true?1:0)
            };
            if ((Top.Contains(c.Kind)||c.Kind=="C03"||c.Kind=="C08"||c.Kind=="C10") && !exposedTop)
                plan.Problems.Add("Functional top face blocked");
            if (c.Kind=="C02" && bodyAt(x+(facing=="LEFT"?-1:1),y)) plan.Problems.Add("Reflect face blocked");
            return plan;
        }

        public static GraphicInvalidation Affected(int x, int y)
        {
            var result = new GraphicInvalidation();
            for (int dy=-1; dy<=1; dy++) for (int dx=-1; dx<=1; dx++) result.Cells.Add(new GraphicCoordinate(x+dx,y+dy));
            for (int dy=-4; dy<=1; dy++) for (int dx=-4; dx<=1; dx++) result.MotifAnchors.Add(new GraphicCoordinate(x+dx,y+dy));
            return result;
        }
    }
}
