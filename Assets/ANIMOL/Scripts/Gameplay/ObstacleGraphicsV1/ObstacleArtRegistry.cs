using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    [Serializable] public sealed class MechanismSprite { public string key; public Sprite sprite; }
    public sealed class ObstacleArtRegistry : ScriptableObject
    {
        public int visualVersion = 1, terrainArtVersion = 6;
        public string sourceHash;
        public MechanismSprite[] entries;
        private Dictionary<string, Sprite> lookup;
        public static ObstacleArtRegistry Load()
        {
            var value = Resources.Load<ObstacleArtRegistry>("ANIMOL_ObstacleGraphicsV1");
            if (value == null) throw new InvalidOperationException("Obstacle graphics V1 is not installed.");
            if (value.lookup == null) value.ValidateComplete();
            return value;
        }
        public void ValidateComplete()
        {
            lookup = null;
            if (visualVersion != 1 || terrainArtVersion != 6 || entries?.Length != 163 ||
                entries.Any(e => e == null || e.sprite == null || string.IsNullOrEmpty(e.key)) || entries.Select(e => e.key).Distinct().Count() != 163)
                throw new InvalidOperationException("Incomplete obstacle graphics registry.");
            lookup = entries.ToDictionary(e => e.key, e => e.sprite, StringComparer.Ordinal);
        }
        public Sprite Sprite(string key)
        {
            if (lookup == null) ValidateComplete();
            return lookup.TryGetValue(key, out var sprite) ? sprite : throw new InvalidOperationException("Unregistered mechanism Sprite: " + key);
        }
        private void OnEnable() => lookup = null;
    }
}
