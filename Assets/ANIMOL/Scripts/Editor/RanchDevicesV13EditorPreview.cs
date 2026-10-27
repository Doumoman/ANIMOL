using System.Collections.Generic;
using ANIMOL.Art.RanchDevicesV13;
using ANIMOL.Puzzle;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    // Uses the same resolved key as gameplay without instantiating a device for every IMGUI repaint.
    public static class RanchDevicesV13EditorPreview
    {
        static RanchDevicesV13Catalog cachedCatalog;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Vector2Int[] neighborOffsets = { Vector2Int.up, new Vector2Int(1, 1), Vector2Int.right,
            new Vector2Int(1, -1), Vector2Int.down, new Vector2Int(-1, -1),
            Vector2Int.left, new Vector2Int(-1, 1) };
        static bool catalogValid;

        public static bool Covers(PuzzleTheme theme, PrototypeDevice device) =>
            theme && theme.starwayRanch && device != null && device.role != PrototypeRole.Chick &&
            RanchDevicesV13PuzzleBinding.Covers(device);

        public static bool TryDevice(PuzzleTheme theme, PrototypeDevice device, out Sprite sprite, out string key)
        {
            sprite = null;
            key = null;
            if (!Covers(theme, device)) return false;
            var state = new PrototypeDeviceState
            {
                cell = device.position,
                direction = device.direction,
                active = device.active,
                phase = device.orientation
            };
            return RanchDevicesV13PuzzleBinding.TrySettledKey(device, state, out key, out _) &&
                TryClip(theme, key, out sprite);
        }

        public static bool TryClip(PuzzleTheme theme, string key, out Sprite sprite)
        {
            sprite = null;
            var catalog = theme && theme.starwayRanch ? theme.starwayRanch.v13Devices : null;
            if (!catalog || string.IsNullOrEmpty(key)) return false;
            if (!EnsureCatalog(catalog)) return false;
            if (sprites.TryGetValue(key, out sprite)) return sprite;
            if (!catalog.TryClip(key, out var clip) || clip.frames == null || clip.frames.Length == 0 || !clip.frames[0])
                return false;
            sprite = clip.frames[0];
            sprites.Add(key, sprite);
            return true;
        }

        public static bool TryPart(PuzzleTheme theme, string key, out Sprite sprite)
        {
            sprite = null;
            var catalog = theme && theme.starwayRanch ? theme.starwayRanch.v13Devices : null;
            if (!catalog || string.IsNullOrEmpty(key) || !EnsureCatalog(catalog)) return false;
            var cacheKey = "part:" + key;
            if (sprites.TryGetValue(cacheKey, out sprite)) return sprite;
            if (!catalog.TrySprite(key, out sprite)) return false;
            sprites.Add(cacheKey, sprite);
            return true;
        }

        static bool EnsureCatalog(RanchDevicesV13Catalog catalog)
        {
            if (cachedCatalog != catalog)
            {
                cachedCatalog = catalog;
                sprites.Clear();
                catalogValid = catalog.Validate(out _);
            }
            return catalogValid;
        }

        public static int WaterNeighborMask(ISet<Vector2Int> sameState, Vector2Int cell)
        {
            if (sameState == null) return 0;
            int mask = 0;
            for (int i = 0; i < neighborOffsets.Length; i++)
                if (sameState.Contains(cell + neighborOffsets[i])) mask |= 1 << i;
            return mask;
        }

        public static void ClearCache()
        {
            cachedCatalog = null;
            catalogValid = false;
            sprites.Clear();
        }
    }
}
