using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [Serializable]
    public sealed class EchoTheaterV02SpriteEntry
    {
        public string path;
        public Sprite sprite;
    }

    // Art lookup only. Paths are package-relative; gameplay and save IDs remain on PuzzleMap.
    public sealed class EchoTheaterV02Art : ScriptableObject
    {
        public EchoTheaterV02SpriteEntry[] sprites;
        public TextAsset backgroundLayout;
        Dictionary<string, Sprite> lookup;

        public Sprite Find(string path)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in sprites ?? Array.Empty<EchoTheaterV02SpriteEntry>())
                    if (entry != null && !string.IsNullOrEmpty(entry.path) && entry.sprite)
                        lookup[entry.path] = entry.sprite;
            }
            return path != null && lookup.TryGetValue(path, out var sprite) ? sprite : null;
        }

        void OnEnable() => lookup = null;
        void OnValidate() => lookup = null;
    }
}
