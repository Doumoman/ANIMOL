using System;
using UnityEngine;

namespace ANIMOL.StarwayMaps
{
    // Shared by the existing runtime camera and editor camera-frame preview.
    // Coordinates: unrotated XY grid; origin is the world CENTER of cell (0,0).
    public static class StarwayCameraMath
    {
        public static Vector2 FocusWorldSize(Vector2 worldUnitsPerCell, int columns = 7, int rows = 11)
        {
            RequirePositive(worldUnitsPerCell.x, "cell width");
            RequirePositive(worldUnitsPerCell.y, "cell height");
            if (columns <= 0 || rows <= 0) throw new ArgumentOutOfRangeException("focus cells");
            return new Vector2(worldUnitsPerCell.x * columns, worldUnitsPerCell.y * rows);
        }

        // Fit exact focus aspect INSIDE the available game area, without stretching tiles.
        public static Rect FitInsidePixels(Rect available, float focusAspect)
        {
            RequireRect(available, "available gameplay pixels");
            RequirePositive(focusAspect, "focus aspect");
            float width = available.width;
            float height = width / focusAspect;
            if (height > available.height) { height = available.height; width = height * focusAspect; }
            return new Rect(available.center.x - width * .5f, available.center.y - height * .5f, width, height);
        }

        public static Rect Intersect(Rect a, Rect b)
        {
            float x0 = Mathf.Max(a.xMin, b.xMin), y0 = Mathf.Max(a.yMin, b.yMin);
            float x1 = Mathf.Min(a.xMax, b.xMax), y1 = Mathf.Min(a.yMax, b.yMax);
            if (x1 <= x0 || y1 <= y0) throw new ArgumentException("No usable gameplay viewport after safe area/HUD.");
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        public static Rect NormalizePixels(Rect pixels, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) throw new ArgumentOutOfRangeException("screen size");
            return new Rect(pixels.x / screenWidth, pixels.y / screenHeight,
                pixels.width / screenWidth, pixels.height / screenHeight);
        }

        public static Rect CellBounds(StarwayCell[] region, Vector2 cellZeroCenter, Vector2 worldUnitsPerCell)
        {
            FocusWorldSize(worldUnitsPerCell);
            if (region == null || region.Length == 0) throw new ArgumentException("regionCells is empty.");
            int x0 = region[0].x, y0 = region[0].y, x1 = x0, y1 = y0;
            foreach (StarwayCell c in region)
            {
                x0 = Math.Min(x0, c.x); y0 = Math.Min(y0, c.y);
                x1 = Math.Max(x1, c.x); y1 = Math.Max(y1, c.y);
            }
            return Rect.MinMaxRect(cellZeroCenter.x + (x0 - .5f) * worldUnitsPerCell.x,
                cellZeroCenter.y + (y0 - .5f) * worldUnitsPerCell.y,
                cellZeroCenter.x + (x1 + .5f) * worldUnitsPerCell.x,
                cellZeroCenter.y + (y1 + .5f) * worldUnitsPerCell.y);
        }

        public static Vector2 ClampCenter(Vector2 desired, Rect mapBounds, Vector2 focusWorldSize)
        {
            RequireRect(mapBounds, "map bounds");
            RequirePositive(focusWorldSize.x, "focus width");
            RequirePositive(focusWorldSize.y, "focus height");
            return new Vector2(ClampAxis(desired.x, mapBounds.xMin, mapBounds.xMax, focusWorldSize.x),
                ClampAxis(desired.y, mapBounds.yMin, mapBounds.yMax, focusWorldSize.y));
        }

        private static float ClampAxis(float value, float min, float max, float visible)
        {
            return max - min <= visible ? (min + max) * .5f : Mathf.Clamp(value, min + visible * .5f, max - visible * .5f);
        }

        private static void RequireRect(Rect r, string name)
        {
            RequirePositive(r.width, name + " width"); RequirePositive(r.height, name + " height");
        }

        private static void RequirePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Expected a finite positive value.");
        }
    }
}
