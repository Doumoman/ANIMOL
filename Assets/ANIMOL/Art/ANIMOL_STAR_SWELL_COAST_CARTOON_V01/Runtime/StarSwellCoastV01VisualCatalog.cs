using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ANIMOL.Art.StarSwellCoastV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Star Swell Coast V01 Visual Catalog")]
    public sealed class StarSwellCoastV01VisualCatalog : ScriptableObject
    {
        public const int RequiredSchemaVersion = 1;
        public const string RequiredPackageId = "ANIMOL_STAR_SWELL_COAST_CARTOON_V01";

        [Serializable]
        public sealed class Frame
        {
            [SerializeField] private Sprite sprite;
            [SerializeField] private string file, sha256, assetGuid, spriteName;
            [SerializeField] private long localFileId;
            [SerializeField] private int width, height;
            [SerializeField] private float ppu;
            [SerializeField] private Vector2 pivot;
            public Sprite Sprite { get { return sprite; } }
            public string File { get { return file; } }
            public string Sha256 { get { return sha256; } }
            public string AssetGuid { get { return assetGuid; } }
            public long LocalFileId { get { return localFileId; } }
            public int Width { get { return width; } }
            public int Height { get { return height; } }
            public float Ppu { get { return ppu; } }
            public Vector2 Pivot { get { return pivot; } }

            public Frame(Sprite value, string path, string hash, string guid, long fileId,
                int pixelWidth, int pixelHeight, float pixelsPerUnit, Vector2 normalizedPivot)
            {
                sprite = value; file = path; sha256 = hash; assetGuid = guid; localFileId = fileId;
                width = pixelWidth; height = pixelHeight; ppu = pixelsPerUnit; pivot = normalizedPivot;
                spriteName = value != null ? value.name : null;
            }

            public bool Validate(out string error)
            {
                error = null;
                if (!IsRelativePngPath(file) || !IsHash(sha256, 64) || !IsHash(assetGuid, 32)
                    || localFileId == 0 || width <= 0 || height <= 0 || !Finite(ppu) || ppu <= 0
                    || !Finite(pivot.x) || !Finite(pivot.y) || pivot.x < 0 || pivot.x > 1
                    || pivot.y < 0 || pivot.y > 1 || sprite == null)
                { error = "Invalid or missing sprite metadata: " + file; return false; }
                Rect r = sprite.rect;
                Vector2 actualPivot = new Vector2(sprite.pivot.x / r.width, sprite.pivot.y / r.height);
                if (sprite.name != spriteName || Mathf.Abs(r.width - width) > .01f
                    || Mathf.Abs(r.height - height) > .01f || Mathf.Abs(sprite.pixelsPerUnit - ppu) > .001f
                    || Mathf.Abs(actualPivot.x - pivot.x) > .0001f || Mathf.Abs(actualPivot.y - pivot.y) > .0001f)
                { error = "Imported sprite differs from baked metadata: " + file; return false; }
                return true;
            }

            internal void AppendFingerprint(StringBuilder b)
            {
                Append(b, file); Append(b, sha256); Append(b, assetGuid); Append(b, localFileId.ToString(CultureInfo.InvariantCulture));
                Append(b, spriteName); Append(b, width.ToString(CultureInfo.InvariantCulture));
                Append(b, height.ToString(CultureInfo.InvariantCulture)); Append(b, Float(ppu));
                Append(b, Float(pivot.x)); Append(b, Float(pivot.y));
            }
        }

        [Serializable]
        public sealed class Clip
        {
            [SerializeField] private string key, family, heading, state;
            [SerializeField] private float fps;
            [SerializeField] private bool loop;
            [SerializeField] private Frame[] frames;
            public string Key { get { return key; } }
            public string Family { get { return family; } }
            public string Heading { get { return heading; } }
            public string State { get { return state; } }
            public float Fps { get { return fps; } }
            public bool Loop { get { return loop; } }
            public int FrameCount { get { return frames == null ? 0 : frames.Length; } }
            public Frame GetFrame(int index) { return frames[index]; }
            public Clip(string clipKey, string deviceFamily, string exactHeading, string visualState,
                float framesPerSecond, bool isLoop, Frame[] clipFrames)
            {
                key = clipKey; family = deviceFamily; heading = exactHeading; state = visualState;
                fps = framesPerSecond; loop = isLoop;
                frames = clipFrames == null ? null : (Frame[])clipFrames.Clone();
            }
        }

        [Serializable]
        public sealed class Part
        {
            [SerializeField] private string key;
            [SerializeField] private Frame frame;
            public string Key { get { return key; } }
            public Frame Frame { get { return frame; } }
            public Part(string partKey, Frame spriteFrame) { key = partKey; frame = spriteFrame; }
        }

        [SerializeField] private int schemaVersion;
        [SerializeField] private string packageId, sourceRootPath, sourceRootGuid, sourceManifestSha256, fingerprint;
        [SerializeField] private Frame[] sprites = new Frame[0];
        [SerializeField] private Clip[] clips = new Clip[0];
        [SerializeField] private Part[] parts = new Part[0];
        [NonSerialized] private Dictionary<string, Clip> byKey, byDirection;
        [NonSerialized] private Dictionary<string, Part> byPart;
        [NonSerialized] private bool validated;
        [NonSerialized] private string validatedExpectedHash;
        public string PackageId { get { return packageId; } }
        public string SourceRootPath { get { return sourceRootPath; } }
        public string SourceRootGuid { get { return sourceRootGuid; } }
        public string SourceManifestSha256 { get { return sourceManifestSha256; } }
        public string Fingerprint { get { return fingerprint; } }
        public int SpriteCount { get { return sprites == null ? 0 : sprites.Length; } }
        public int ClipCount { get { return clips == null ? 0 : clips.Length; } }
        public int PartCount { get { return parts == null ? 0 : parts.Length; } }
        public Frame GetSpriteRecord(int i) { return sprites[i]; }
        public Clip GetClipAt(int i) { return clips[i]; }
        public Part GetPartAt(int i) { return parts[i]; }

        private void OnEnable() { Invalidate(); }
        private void OnValidate() { Invalidate(); }
        private void Invalidate() { validated = false; byKey = null; byDirection = null; byPart = null; }

        public bool TryGetClip(string key, string expectedManifestSha256, out Clip clip, out string error)
        {
            clip = null;
            if (!EnsureValid(expectedManifestSha256, out error)) return false;
            if (string.IsNullOrWhiteSpace(key) || !byKey.TryGetValue(key, out clip))
            { error = "Missing latest clip key: " + key; return false; }
            return true;
        }

        // Exact matching only. A missing direction NEVER resolves to None or another heading.
        public bool TryGetDirectionalClip(string family, string heading, string state,
            string expectedManifestSha256, out Clip clip, out string error)
        {
            clip = null;
            if (!EnsureValid(expectedManifestSha256, out error)) return false;
            if (!ValidToken(family) || !ValidToken(heading) || !ValidToken(state)
                || !byDirection.TryGetValue(DirectionKey(family, heading, state), out clip))
            { error = "Missing latest directional clip: " + family + "/" + heading + "/" + state; return false; }
            return true;
        }

        public bool TryGetPart(string key, string expectedManifestSha256, out Part part, out string error)
        {
            part = null;
            if (!EnsureValid(expectedManifestSha256, out error)) return false;
            if (string.IsNullOrWhiteSpace(key) || !byPart.TryGetValue(key, out part))
            { error = "Missing latest part key: " + key; return false; }
            return true;
        }

        private bool EnsureValid(string expected, out string error)
        {
            if (validated && string.Equals(expected, validatedExpectedHash, StringComparison.OrdinalIgnoreCase))
            { error = null; return true; }
            return ValidateIntegrity(expected, out error);
        }

        public bool ValidateIntegrity(string expectedManifestSha256, out string error)
        {
            Invalidate(); error = null;
            if (schemaVersion != RequiredSchemaVersion || packageId != RequiredPackageId
                || string.IsNullOrEmpty(sourceRootPath)
                || !sourceRootPath.StartsWith("Assets/", StringComparison.Ordinal)
                || sourceRootPath.IndexOf('\\') >= 0 || sourceRootPath.IndexOf("/../", StringComparison.Ordinal) >= 0
                || sourceRootPath.IndexOf("/./", StringComparison.Ordinal) >= 0
                || !sourceRootPath.Replace('\\', '/').EndsWith("/" + RequiredPackageId, StringComparison.Ordinal)
                || !IsHash(sourceRootGuid, 32) || !IsHash(sourceManifestSha256, 64)
                || !IsHash(expectedManifestSha256, 64)
                || !string.Equals(sourceManifestSha256, expectedManifestSha256, StringComparison.OrdinalIgnoreCase))
            { error = "Wrong/missing Coast V01 package, source root, schema or pinned manifest SHA256."; return false; }
            if (sprites == null || sprites.Length == 0 || clips == null || clips.Length == 0 || parts == null)
            { error = "Latest sprite or clip catalog is empty/malformed."; return false; }
            var files = new Dictionary<string, Frame>(StringComparer.Ordinal);
            foreach (Frame f in sprites)
            {
                if (f == null || !f.Validate(out error)) { if (error == null) error = "Null sprite record."; return false; }
                if (files.ContainsKey(f.File)) { error = "Duplicate sprite path: " + f.File; return false; }
                files.Add(f.File, f);
            }
            var keys = new Dictionary<string, Clip>(StringComparer.Ordinal);
            var directions = new Dictionary<string, Clip>(StringComparer.Ordinal);
            foreach (Clip c in clips)
            {
                if (c == null || !ValidToken(c.Key) || !ValidToken(c.Family) || !ValidToken(c.Heading)
                    || !ValidToken(c.State) || !Finite(c.Fps) || c.Fps <= 0 || c.FrameCount == 0)
                { error = "Invalid clip key/metadata."; return false; }
                bool walk = c.State.Equals("walk", StringComparison.OrdinalIgnoreCase)
                    || c.State.Equals("move", StringComparison.OrdinalIgnoreCase);
                if (walk && (c.Fps < 10f || c.FrameCount < 2))
                { error = "Walk/move requires >=10 fps and at least two real frames: " + c.Key; return false; }
                string direction = DirectionKey(c.Family, c.Heading, c.State);
                if (keys.ContainsKey(c.Key) || directions.ContainsKey(direction))
                { error = "Duplicate clip key or exact family/heading/state: " + c.Key; return false; }
                for (int i = 0; i < c.FrameCount; ++i)
                {
                    Frame f = c.GetFrame(i); Frame registered;
                    if (f == null || !files.TryGetValue(f.File, out registered) || f.Sprite != registered.Sprite
                        || f.Sha256 != registered.Sha256 || !f.Validate(out error))
                    { if (error == null) error = "Unregistered/mismatched clip frame: " + c.Key; return false; }
                }
                keys.Add(c.Key, c); directions.Add(direction, c);
            }
            var partKeys = new Dictionary<string, Part>(StringComparer.Ordinal);
            foreach (Part p in parts)
            {
                Frame registered;
                if (p == null || !ValidToken(p.Key) || p.Frame == null || partKeys.ContainsKey(p.Key)
                    || !files.TryGetValue(p.Frame.File, out registered) || p.Frame.Sprite != registered.Sprite
                    || p.Frame.Sha256 != registered.Sha256 || !p.Frame.Validate(out error))
                { if (error == null) error = "Invalid/duplicate/unregistered part."; return false; }
                partKeys.Add(p.Key, p);
            }
            if (!IsHash(fingerprint, 64) || !string.Equals(fingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            { error = "Catalog data fingerprint mismatch. Rebuild from the verified latest manifest."; return false; }
            byKey = keys; byDirection = directions; byPart = partKeys;
            validatedExpectedHash = expectedManifestSha256; validated = true;
            return true;
        }

        private string ComputeFingerprint()
        {
            var b = new StringBuilder();
            Append(b, schemaVersion.ToString(CultureInfo.InvariantCulture)); Append(b, packageId);
            Append(b, sourceRootPath); Append(b, sourceRootGuid); Append(b, sourceManifestSha256);
            Append(b, sprites.Length.ToString(CultureInfo.InvariantCulture));
            Append(b, clips.Length.ToString(CultureInfo.InvariantCulture));
            Append(b, parts.Length.ToString(CultureInfo.InvariantCulture));
            foreach (Frame f in sprites) f.AppendFingerprint(b);
            foreach (Clip c in clips)
            {
                Append(b, c.Key); Append(b, c.Family); Append(b, c.Heading); Append(b, c.State);
                Append(b, Float(c.Fps)); Append(b, c.Loop ? "1" : "0");
                Append(b, c.FrameCount.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < c.FrameCount; ++i) c.GetFrame(i).AppendFingerprint(b);
            }
            foreach (Part p in parts) { Append(b, p.Key); p.Frame.AppendFingerprint(b); }
            return Sha256Of(Encoding.UTF8.GetBytes(b.ToString()));
        }

#if UNITY_EDITOR
        // Editor builder only. Runtime has no mutator or legacy fallback setter.
        public void SetEditorData(string rootPath, string rootGuid, string manifestHash,
            Frame[] allSprites, Clip[] allClips, Part[] allParts)
        {
            schemaVersion = RequiredSchemaVersion; packageId = RequiredPackageId;
            sourceRootPath = rootPath; sourceRootGuid = rootGuid; sourceManifestSha256 = manifestHash;
            sprites = allSprites == null ? new Frame[0] : (Frame[])allSprites.Clone();
            clips = allClips == null ? new Clip[0] : (Clip[])allClips.Clone();
            parts = allParts == null ? new Part[0] : (Part[])allParts.Clone();
            fingerprint = ComputeFingerprint(); Invalidate();
        }
#endif
        public static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
        public static bool IsHash(string value, int length)
        {
            if (value == null || value.Length != length) return false;
            foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }
        public static bool IsRelativePngPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path == path.Trim() && path.IndexOf('\\') < 0
                && !path.StartsWith("/", StringComparison.Ordinal) && path.IndexOf(':') < 0
                && !path.StartsWith("../", StringComparison.Ordinal) && path.IndexOf("/../", StringComparison.Ordinal) < 0
                && !path.StartsWith("./", StringComparison.Ordinal) && path.IndexOf("/./", StringComparison.Ordinal) < 0
                && path.IndexOf("//", StringComparison.Ordinal) < 0 && path.EndsWith(".png", StringComparison.Ordinal);
        }
        public static string Sha256Of(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes); var b = new StringBuilder(64);
                foreach (byte n in hash) b.Append(n.ToString("x2", CultureInfo.InvariantCulture));
                return b.ToString();
            }
        }
        private static bool ValidToken(string s) { return !string.IsNullOrWhiteSpace(s) && s == s.Trim() && s.IndexOf('\u001f') < 0; }
        private static string DirectionKey(string family, string heading, string state) { return family + "\u001f" + heading + "\u001f" + state; }
        private static string Float(float f)
        { return BitConverter.ToUInt32(BitConverter.GetBytes(f), 0).ToString("x8", CultureInfo.InvariantCulture); }
        private static void Append(StringBuilder b, string s) { s = s ?? ""; b.Append(s.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(s); }
    }

    // Pure timing math. The frame player's completion is visual state, never a simulation event.
    public static class StarSwellCoastV01ClipTiming
    {
        public static int FrameIndex(int count, float fps, double elapsed, bool loop, bool reverse, float durationSeconds = -1f)
        {
            if (count < 1 || !StarSwellCoastV01VisualCatalog.Finite(fps) || fps <= 0
                || double.IsNaN(elapsed) || double.IsInfinity(elapsed)
                || !StarSwellCoastV01VisualCatalog.Finite(durationSeconds) || (durationSeconds != -1f && durationSeconds <= 0))
                throw new ArgumentOutOfRangeException("Invalid clip timing.");
            double duration = durationSeconds > 0 ? durationSeconds : count / (double)fps;
            double positiveTime = Math.Max(0, elapsed);
            double progress = loop ? (positiveTime % duration) / duration : Math.Min(1, positiveTime / duration);
            return FromNormalized(count, progress, reverse);
        }
        public static int FromNormalized(int count, double progress, bool reverse)
        {
            if (count < 1 || double.IsNaN(progress) || double.IsInfinity(progress)) throw new ArgumentOutOfRangeException("Invalid progress.");
            int index = Math.Min(count - 1, (int)Math.Floor(Math.Max(0, Math.Min(1, progress)) * count));
            return reverse ? count - 1 - index : index;
        }
    }
}
