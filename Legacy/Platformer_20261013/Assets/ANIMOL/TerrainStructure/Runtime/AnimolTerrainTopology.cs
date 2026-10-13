using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Deterministic 8-neighbour blob topology. Bit order runs clockwise from north.
    /// A diagonal survives only when both of its adjoining cardinal cells are solid.
    /// Math methods do not query scenes, random state, tile assets or Sprite alpha.
    /// </summary>
    public static class AnimolTerrainTopology
    {
        public const int N = 1;
        public const int NE = 2;
        public const int E = 4;
        public const int SE = 8;
        public const int S = 16;
        public const int SW = 32;
        public const int W = 64;
        public const int NW = 128;
        public const int ShapeCount = 47;
        public const int InteriorMask = 255;

        private static readonly int[] Masks = BuildCanonicalMasks();

        /// <summary>Ascending mask values. Returns a copy so callers cannot corrupt shared topology.</summary>
        public static int[] CanonicalMasks { get { return (int[])Masks.Clone(); } }

        public static int Canonicalize(int raw)
        {
            int mask = raw & 255;
            if ((mask & (N | E)) != (N | E)) mask &= ~NE;
            if ((mask & (E | S)) != (E | S)) mask &= ~SE;
            if ((mask & (S | W)) != (S | W)) mask &= ~SW;
            if ((mask & (W | N)) != (W | N)) mask &= ~NW;
            return mask;
        }

        /// <summary>Returns the canonical sprite index, also accepting noncanonical raw masks.</summary>
        public static int IndexOfMask(int raw)
        {
            return Array.BinarySearch(Masks, Canonicalize(raw));
        }

        /// <summary>
        /// A no-gutter atlas stores index 0 at PNG top left. Unity Sprite rects start at bottom left.
        /// 'rows' is the total atlas row count, including any trailing unused slots.
        /// </summary>
        public static Rect GetAtlasRect(int index, int columns, int rows, int cellPixels)
        {
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cellPixels <= 0) throw new ArgumentOutOfRangeException(nameof(cellPixels));
            if (index < 0 || (long)index >= (long)columns * rows)
                throw new ArgumentOutOfRangeException(nameof(index));

            int column = index % columns;
            int topRow = index / columns;
            return new Rect(column * (float)cellPixels,
                WorldYFromTopRow(topRow, rows) * (float)cellPixels,
                cellPixels, cellPixels);
        }

        /// <summary>Converts a north-first row into a y-up local/global cell coordinate.</summary>
        public static int WorldYFromTopRow(int row, int height, int originY = 0)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (row < 0 || row >= height) throw new ArgumentOutOfRangeException(nameof(row));
            return checked((int)((long)originY + height - 1 - row));
        }

        /// <summary>
        /// Stable FNV-1a byte hashing plus avalanche; independent of string.GetHashCode(),
        /// UnityEngine.Random and load order. Numeric inputs are little-endian 32-bit values;
        /// salt characters are UTF-16 code units in little-endian byte order.
        /// </summary>
        public static uint Hash(int x, int y, int seed, string salt = null)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = HashInt(hash, x);
                hash = HashInt(hash, y);
                hash = HashInt(hash, seed);
                if (salt != null)
                {
                    for (int i = 0; i < salt.Length; i++)
                    {
                        hash = (hash ^ (byte)salt[i]) * 16777619u;
                        hash = (hash ^ (byte)(salt[i] >> 8)) * 16777619u;
                    }
                }
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                hash ^= hash >> 16;
                return hash;
            }
        }

        public static int ChooseInteriorVariant(int x, int y, int seed, int variantCount, string styleId = null)
        {
            if (variantCount <= 0) throw new ArgumentOutOfRangeException(nameof(variantCount));
            return (int)(Hash(x, y, seed, styleId) % (uint)variantCount);
        }

        /// <summary>Validates an imported slot mapping without silently removing duplicate masks.</summary>
        public static bool ValidateMaskOrder(int[] candidate, out string error)
        {
            if (candidate == null || candidate.Length != ShapeCount)
            {
                error = "Exactly 47 mask entries are required.";
                return false;
            }
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < candidate.Length; i++)
            {
                int mask = candidate[i];
                if (mask < 0 || mask > 255 || Canonicalize(mask) != mask)
                {
                    error = "Noncanonical mask at slot " + i + ": " + mask;
                    return false;
                }
                if (!seen.Add(mask))
                {
                    error = "Duplicate mask: " + mask;
                    return false;
                }
                if (mask != Masks[i])
                {
                    error = "Mask order must match ascending CanonicalMasks; mismatch at slot " + i;
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static int[] BuildCanonicalMasks()
        {
            SortedSet<int> unique = new SortedSet<int>();
            for (int raw = 0; raw <= 255; raw++) unique.Add(Canonicalize(raw));
            if (unique.Count != ShapeCount)
                throw new InvalidOperationException("Terrain canonical topology must contain exactly 47 masks.");
            int[] values = new int[unique.Count];
            unique.CopyTo(values);
            return values;
        }

        private static uint HashInt(uint hash, int value)
        {
            unchecked
            {
                uint bits = (uint)value;
                for (int shift = 0; shift < 32; shift += 8)
                    hash = (hash ^ (byte)(bits >> shift)) * 16777619u;
                return hash;
            }
        }
    }
}
