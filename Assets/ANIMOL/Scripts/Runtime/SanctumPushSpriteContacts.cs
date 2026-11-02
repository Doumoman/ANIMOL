using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animol.Art.SanctumRetouch
{
    /// <summary>Per-sprite palm anchors in SpriteRenderer local units, independent of the logical grid.</summary>
    public sealed class SanctumPushSpriteContacts : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Sprite sprite;
            public Vector2 handLocal;
            public float targetFaceOffsetWorld;
        }

        public Entry[] entries = new Entry[0];
        public string characterId;
        private Dictionary<Sprite, Entry> lookup;
        private Dictionary<string, Entry> byUniqueName;

        private void OnEnable() { InvalidateCache(); }
        private void OnValidate() { InvalidateCache(); }

        public bool TryGet(Sprite sprite, out Entry contact)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<Sprite, Entry>();
                byUniqueName = new Dictionary<string, Entry>(StringComparer.Ordinal);
                var duplicateNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (Entry entry in entries ?? new Entry[0])
                    if (entry.sprite != null)
                    {
                        lookup[entry.sprite] = entry;
                        string name = entry.sprite.name;
                        if (byUniqueName.ContainsKey(name)) duplicateNames.Add(name);
                        else byUniqueName.Add(name, entry);
                    }
                foreach (string name in duplicateNames) byUniqueName.Remove(name);
            }
            if (sprite != null && lookup.TryGetValue(sprite, out contact)) return true;
            // SpriteAtlas.GetSprite may return a different instance. Do not guess by name alone:
            // require a unique original name and the same local sprite geometry.
            Entry candidate;
            if (sprite != null && byUniqueName.TryGetValue(sprite.name, out candidate) && candidate.sprite != null &&
                sprite.pixelsPerUnit > 0f && candidate.sprite.pixelsPerUnit > 0f &&
                (sprite.rect.size / sprite.pixelsPerUnit - candidate.sprite.rect.size / candidate.sprite.pixelsPerUnit).sqrMagnitude < 0.00000001f &&
                (sprite.pivot / sprite.pixelsPerUnit - candidate.sprite.pivot / candidate.sprite.pixelsPerUnit).sqrMagnitude < 0.00000001f)
            {
                contact = candidate;
                return true;
            }
            contact = default(Entry);
            return false;
        }

        public void InvalidateCache() { lookup = null; byUniqueName = null; }
    }
}
