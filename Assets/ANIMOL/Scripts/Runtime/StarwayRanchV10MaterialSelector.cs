// Pure C# path/placement selection. Does not create scenes, colliders or movement.
// Supply the FULL G/W/S/0 map. Coordinates use x-right/y-down, in logical cells.
using System;
using System.Collections.Generic;

namespace ANIMOL.Art.V10
{
    public static class StarwayRanchV10MaterialSelector
    {
        public const int PixelsPerUnit = 256;
        public const int TopCanvasPixels = 320;
        public const int LogicalPaddingPixels = 32;
        public const int BottomCanvasHeightPixels = 448;
        public const float BottomPivotY = 288f / 448f;

        public sealed class BottomFinish
        {
            // X/Y identify the first covered cell. A span MUST be positioned at
            // CenterCellX/Y, not the first cell, because its pivot is centered.
            public int X, Y, Length, RunLength, Variant, NeighborMaskBits;
            public float CenterCellX, CenterCellY;
            public char Material;
            public bool Hole, IsCollar;
            public string Kind, File;
            public StarwayRanchV10EdgeSelector.EdgeMode LeftMode, RightMode;
            public int WidthPixels, HeightPixels;
            public float PivotX, PivotY;
        }

        public static char GetMaterial(string[] rows, int x, int y)
        { return StarwayRanchV10EdgeSelector.MaterialAt(rows, x, y); }

        public static bool IsWalkable(string[] rows, int x, int y)
        { return GetMaterial(rows, x, y) != '0'; }

        // N1 NE2 E4 SE8 S16 SW32 W64 NW128. Retain EVERY diagonal bit.
        // G/W/S all connect. A material change is not a cliff or an end cap.
        public static int GetNeighborMask(string[] rows, int x, int y)
        {
            return StarwayRanchV10EdgeSelector.NeighborMask(
                (a, b) => IsWalkable(rows, a, b), x, y);
        }

        public static string GetTopSpritePath(string[] rows, int x, int y,
            int seed = 0, int originX = 0, int originY = 0)
        {
            string material = StarwayRanchV10EdgeSelector.MaterialName(GetMaterial(rows, x, y));
            return material == null ? null : "tiles/" + material + "/organic256/n"
                + GetNeighborMask(rows, x, y).ToString("D3") + ".png";
        }

        public static void GetBlobMasks(string[] rows, int x, int y,
            out int outer, out int inner)
        {
            int m = GetNeighborMask(rows, x, y);
            bool n = (m & 1) != 0, e = (m & 4) != 0;
            bool s = (m & 16) != 0, w = (m & 64) != 0;
            outer = (n ? 0 : 1) | (e ? 0 : 2) | (s ? 0 : 4) | (w ? 0 : 8);
            inner = (n && w && (m & 128) == 0 ? 1 : 0)
                | (n && e && (m & 2) == 0 ? 2 : 0)
                | (s && e && (m & 8) == 0 ? 4 : 0)
                | (s && w && (m & 32) == 0 ? 8 : 0);
        }

        // Compatibility only: 47 states cannot preserve all organic diagonals.
        // V10 aliases are ALSO 320px/PPU256; do not retain V05 W/S 256px settings.
        public static string GetCompatibilitySpritePath(string[] rows, int x, int y)
        {
            string material = StarwayRanchV10EdgeSelector.MaterialName(GetMaterial(rows, x, y));
            if (material == null) return null;
            int outer, inner;
            GetBlobMasks(rows, x, y, out outer, out inner);
            return "tiles/" + material + "/blob47/o" + outer.ToString("D2")
                + "_i" + inner.ToString("D2") + ".png";
        }

        public static string GetGrassCompatibilitySpritePath(string[] rows, int x, int y)
        {
            return GetMaterial(rows, x, y) == 'G'
                ? GetCompatibilitySpritePath(rows, x, y) : null;
        }

        // Every exposed cell receives one continuous, shallow collar. Runs then
        // receive merged bodies (short grass), spaced bodies (long grass), or
        // fully joined structures (bridges). DO NOT also render V05 skirts.
        // Draw EVERY returned finish behind EVERY top: bodies first, collars next.
        public static List<BottomFinish> GetBottomFinishes(string[] rows,
            int seed = 0, int originX = 0, int originY = 0)
        {
            List<BottomFinish> bodies = new List<BottomFinish>();
            List<BottomFinish> collars = new List<BottomFinish>();
            if (rows == null || rows.Length == 0) return bodies;
            foreach (StarwayRanchV10EdgeSelector.EdgeRun run in
                StarwayRanchV10EdgeSelector.SelectRuns(rows, seed, originX, originY))
            {
                foreach (StarwayRanchV10EdgeSelector.Span span in
                    StarwayRanchV10EdgeSelector.PlanSpans(run))
                {
                    int x = run.StartX + span.StartOffset;
                    bodies.Add(new BottomFinish {
                        X = x, Y = run.Y, Length = span.Length, RunLength = run.Length,
                        CenterCellX = x + (span.Length - 1) * 0.5f,
                        CenterCellY = run.Y, Material = run.MaterialCode,
                        Hole = run.Hole, IsCollar = false, Kind = span.Kind,
                        Variant = span.Variant, NeighborMaskBits = -1, File = span.File,
                        LeftMode = span.LeftMode, RightMode = span.RightMode,
                        WidthPixels = span.Length * PixelsPerUnit + 2 * LogicalPaddingPixels,
                        HeightPixels = BottomCanvasHeightPixels,
                        PivotX = 0.5f, PivotY = BottomPivotY
                    });
                }
                for (int i = 0; i < run.Length; i++)
                {
                    int x = run.StartX + i;
                    collars.Add(new BottomFinish {
                        X = x, Y = run.Y, Length = 1, RunLength = run.Length,
                        CenterCellX = x, CenterCellY = run.Y,
                        Material = run.MaterialCode, Hole = run.Hole,
                        IsCollar = true, Kind = "collar", Variant = 0,
                        NeighborMaskBits = run.Masks[i],
                        File = "skirts/collars/" + run.Material + "/n"
                            + run.Masks[i].ToString("D3") + ".png",
                        LeftMode = i == 0 ? run.LeftMode : StarwayRanchV10EdgeSelector.EdgeMode.Continue,
                        RightMode = i == run.Length - 1 ? run.RightMode : StarwayRanchV10EdgeSelector.EdgeMode.Continue,
                        WidthPixels = TopCanvasPixels, HeightPixels = BottomCanvasHeightPixels,
                        PivotX = 0.5f, PivotY = BottomPivotY
                    });
                }
            }
            bodies.AddRange(collars);
            return bodies;
        }
    }
}
