using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.EchoRailCartV01
{
    public sealed class EchoRailCartArtCatalogV01 : ScriptableObject
    {
        [Serializable] public sealed class SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] public sealed class ClipEntry { public string key; public AnimationClip clip; }
        public string packageId, sourceSha256;
        public SpriteEntry[] sprites = new SpriteEntry[0];
        public ClipEntry[] clips = new ClipEntry[0];
        public bool Validate(out string error)
        {
            error = null;
            if (packageId != EchoRailCartPinV01.Package || sourceSha256 != EchoRailCartPinV01.Sha256
                || sprites == null || sprites.Length != EchoRailCartPinV01.SpriteCount
                || clips == null || clips.Length != EchoRailCartPinV01.ClipCount)
            { error = "A current complete rail-cart art catalog is required."; return false; }
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in sprites) if (s == null || string.IsNullOrEmpty(s.key) || s.sprite == null || !keys.Add(s.key))
            { error = "Missing or duplicate rail-cart sprite."; return false; }
            keys.Clear();
            foreach (var c in clips) if (c == null || string.IsNullOrEmpty(c.key) || c.clip == null || !keys.Add(c.key))
            { error = "Missing or duplicate rail-cart clip."; return false; }
            return true;
        }
        public bool TrySprite(string key, out Sprite sprite)
        {
            sprite = null; string error;
            if (!Validate(out error)) return false;
            foreach (var s in sprites) if (s.key == key) { sprite = s.sprite; return true; }
            return false;
        }
    }
}
