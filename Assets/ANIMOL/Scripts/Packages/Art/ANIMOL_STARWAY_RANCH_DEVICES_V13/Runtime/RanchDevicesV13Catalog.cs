using System;
using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    [Serializable] public sealed class RanchV13Sprite
    {
        public string key;
        public Sprite sprite;
        public Vector2Int footprint;
        public string role;
    }
    [Serializable] public sealed class RanchV13Clip
    {
        public string key;
        public Sprite[] frames;
        public float fps = 12f;
        public bool loop;
    }
    public sealed class RanchDevicesV13Catalog : ScriptableObject
    {
        public string packageId;
        public string sourceSha256;
        public RanchV13Sprite[] sprites = Array.Empty<RanchV13Sprite>();
        public RanchV13Clip[] clips = Array.Empty<RanchV13Clip>();
        public bool Validate(out string error)
        {
            error = null;
            if (packageId != RanchDevicesV13Pin.Package || sourceSha256 != RanchDevicesV13Pin.Sha256)
                error = "Ranch V13 catalog pin mismatch; legacy fallback is forbidden.";
            else if (sprites == null || sprites.Length != RanchDevicesV13Pin.SpriteCount || clips == null || clips.Length != RanchDevicesV13Pin.ClipCount)
                error = "Incomplete Ranch V13 catalog.";
            else
            {
                var spriteKeys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                var clipKeys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var s in sprites)
                    if (s == null || string.IsNullOrEmpty(s.key) || s.sprite == null || !spriteKeys.Add(s.key))
                    { error = "Missing/duplicate Ranch V13 sprite."; break; }
                if (error == null) foreach (var c in clips)
                {
                    if (c == null || string.IsNullOrEmpty(c.key) || !clipKeys.Add(c.key) || c.frames == null || c.frames.Length == 0 || !Finite(c.fps) || c.fps < 1f || c.fps > 60f)
                    { error = "Missing/duplicate/invalid Ranch V13 clip."; break; }
                    foreach (var s in c.frames) if (s == null) { error = "Missing Ranch V13 frame."; break; }
                    if (error != null) break;
                }
            }
            return error == null;
        }
        public bool TryClip(string key, out RanchV13Clip clip)
        {
            if (clips != null) foreach (var c in clips) if (c != null && c.key == key) { clip = c; return true; }
            clip = null; return false;
        }
        public bool TrySprite(string key, out Sprite sprite)
        {
            if (sprites != null) foreach (var s in sprites) if (s != null && s.key == key) { sprite = s.sprite; return sprite != null; }
            sprite = null; return false;
        }
        internal static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
