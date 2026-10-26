using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Ranch Connected Decor V01/Catalog")]
    public sealed class RanchConnectedDecorV01Catalog : ScriptableObject
    {
        public const string PackageId = "ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01";
        public const string ManifestName = "manifest.json";
        public const string PinName = "manifest_sha256.txt";
        public const int SchemaVersion = 1;

        [Serializable] public sealed class SpriteRecord
        {
            [SerializeField] private string file, sha256, assetGuid, family, kind;
            [SerializeField] private long localFileId;
            [SerializeField] private int width, height, logicalX, logicalY, logicalWidth, logicalHeight, cellsX, cellsY;
            [SerializeField] private float ppu, pivotX, pivotY;
            [SerializeField] private Sprite sprite;
            public string File { get { return file; } }
            public string Sha256 { get { return sha256; } }
            public string AssetGuid { get { return assetGuid; } }
            public long LocalFileId { get { return localFileId; } }
            public Sprite Sprite { get { return sprite; } }
            public int Width { get { return width; } }
            public int Height { get { return height; } }
            public float Ppu { get { return ppu; } }
            public Vector2 Pivot { get { return new Vector2(pivotX, pivotY); } }
            public RectInt LogicalRect { get { return new RectInt(logicalX, logicalY, logicalWidth, logicalHeight); } }
            public int CellsX { get { return cellsX; } }
            public int CellsY { get { return cellsY; } }
            public string Family { get { return family; } }
            public string Kind { get { return kind; } }

            public SpriteRecord(RanchConnectedDecorV01AssetInput row, Sprite value, string guid, long fileId)
            {
                file = row.file; sha256 = row.sha256; assetGuid = guid; localFileId = fileId;
                width = row.width; height = row.height; ppu = row.ppu; pivotX = row.pivot[0]; pivotY = row.pivot[1];
                logicalX = row.logicalRect[0]; logicalY = row.logicalRect[1]; logicalWidth = row.logicalRect[2]; logicalHeight = row.logicalRect[3];
                cellsX = row.footprintCells[0]; cellsY = row.footprintCells[1]; family = row.family; kind = row.kind; sprite = value;
            }
            internal bool Valid(out string error)
            {
                error = null;
                if (!RelativePng(file) || !IsHash(sha256, 64) || !IsHash(assetGuid, 32) || localFileId == 0 || sprite == null)
                    return Error("Missing/invalid exact current sprite identity: " + file, out error);
                if (width < 1 || height < 1 || !Finite(ppu) || ppu != 256f || !Finite(pivotX) || !Finite(pivotY)
                    || pivotX < 0 || pivotX > 1 || pivotY < 0 || pivotY > 1 || cellsX < 1 || cellsY < 1 || cellsX > 64 || cellsY > 64
                    || logicalX < 0 || logicalY < 0 || logicalWidth != cellsX * 256 || logicalHeight != cellsY * 256
                    || (long)logicalX + logicalWidth > width || (long)logicalY + logicalHeight > height)
                    return Error("Invalid current sprite canvas/ground metadata: " + file, out error);
                if (Mathf.Abs(pivotX * width - (logicalX + logicalWidth * .5f)) > .01f
                    || Mathf.Abs((1f - pivotY) * height - (logicalY + logicalHeight * .5f)) > .01f)
                    return Error("Sprite pivot is not its declared logical ground centre: " + file, out error);
                if (sprite.rect.width != width || sprite.rect.height != height || Mathf.Abs(sprite.pixelsPerUnit - ppu) > .001f
                    || Mathf.Abs(sprite.pivot.x - pivotX * width) > .02f || Mathf.Abs(sprite.pivot.y - pivotY * height) > .02f)
                    return Error("Imported Sprite metadata differs from current source: " + file, out error);
                return true;
            }
            internal void Append(StringBuilder b)
            {
                Add(b, file); Add(b, sha256); Add(b, assetGuid); Add(b, localFileId.ToString(CultureInfo.InvariantCulture));
                Add(b, family); Add(b, kind); Add(b, width.ToString()); Add(b, height.ToString()); Add(b, FloatBits(ppu)); Add(b, FloatBits(pivotX)); Add(b, FloatBits(pivotY));
                Add(b, logicalX.ToString()); Add(b, logicalY.ToString()); Add(b, logicalWidth.ToString()); Add(b, logicalHeight.ToString()); Add(b, cellsX.ToString()); Add(b, cellsY.ToString());
                Add(b, sprite == null ? "null" : sprite.name);
            }
        }

        [Serializable] public sealed class GroupRecord
        {
            [SerializeField] private string key, file;
            [SerializeField] private int widthCells, heightCells;
            public string Key { get { return key; } }
            public string File { get { return file; } }
            public int WidthCells { get { return widthCells; } }
            public int HeightCells { get { return heightCells; } }
            public GroupRecord(string key, string file, int width, int height)
            { this.key = key; this.file = file; widthCells = width; heightCells = height; }
            internal void Append(StringBuilder b) { Add(b, key); Add(b, file); Add(b, widthCells.ToString()); Add(b, heightCells.ToString()); }
        }

        [SerializeField] private string packageId, sourceRootPath, sourceRootGuid, sourceManifestSha256, fingerprint;
        [SerializeField] private SpriteRecord[] sprites = new SpriteRecord[0];
        [SerializeField] private GroupRecord[] groups = new GroupRecord[0];
        [NonSerialized] private Dictionary<string, SpriteRecord> byFile;
        [NonSerialized] private Dictionary<string, GroupRecord> byGroup;
        [NonSerialized] private bool validated;
        public string SourceRootPath { get { return sourceRootPath; } }
        public string SourceRootGuid { get { return sourceRootGuid; } }
        public string SourceManifestSha256 { get { return sourceManifestSha256; } }
        public int SpriteCount { get { return sprites == null ? 0 : sprites.Length; } }
        public int GroupCount { get { return groups == null ? 0 : groups.Length; } }
        public SpriteRecord GetSpriteAt(int index) { return sprites[index]; }
        public GroupRecord GetGroupAt(int index) { return groups[index]; }
        private void OnEnable() { Invalidate(); }
        private void OnValidate() { Invalidate(); }
        public void Invalidate() { validated = false; byFile = null; byGroup = null; }

        public bool ValidateIntegrity(string expectedManifestSha256, out string error)
        {
            error = null;
            if (!IsHash(expectedManifestSha256, 64) || !string.Equals(expectedManifestSha256, sourceManifestSha256, StringComparison.OrdinalIgnoreCase))
                return Error("Missing/mismatching current manifest SHA pin.", out error);
            if (validated) return true;
            if (packageId != PackageId || !IsHash(sourceRootGuid, 32) || string.IsNullOrEmpty(sourceRootPath)
                || !sourceRootPath.StartsWith("Assets/", StringComparison.Ordinal) || !sourceRootPath.EndsWith("/" + PackageId, StringComparison.Ordinal)
                || sprites == null || sprites.Length == 0 || groups == null || groups.Length == 0 || !IsHash(fingerprint, 64))
                return Error("Missing or incompatible latest decor catalog.", out error);
            if (!string.Equals(ComputeFingerprint(), fingerprint, StringComparison.Ordinal)) return Error("Current catalog fingerprint differs.", out error);
            var files = new Dictionary<string, SpriteRecord>(StringComparer.Ordinal);
            var caseFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SpriteRecord row in sprites)
            {
                if (row == null) return Error("Missing current sprite record.", out error);
                if (!row.Valid(out error)) return false;
                if (files.ContainsKey(row.File) || !caseFiles.Add(row.File)) return Error("Duplicate/case-colliding sprite path.", out error);
                files.Add(row.File, row);
            }
            var groupLookup = new Dictionary<string, GroupRecord>(StringComparer.Ordinal);
            foreach (GroupRecord group in groups)
            {
                SpriteRecord art;
                if (group == null || string.IsNullOrWhiteSpace(group.Key) || groupLookup.ContainsKey(group.Key)
                    || !files.TryGetValue(group.File, out art) || group.WidthCells != art.CellsX || group.HeightCells != art.CellsY)
                    return Error("Invalid current multi-cell group metadata.", out error);
                groupLookup.Add(group.Key, group);
            }
            byFile = files; byGroup = groupLookup; validated = true; return true;
        }
        public bool TryGetExact(string file, string pin, out SpriteRecord record, out string error)
        {
            record = null;
            if (!ValidateIntegrity(pin, out error)) return false;
            if (!RelativePng(file) || !byFile.TryGetValue(file, out record) || record.Sprite == null)
                return Error("Missing exact current decor sprite: " + file, out error);
            return true;
        }
        public bool TryGetGroup(string key, string pin, out GroupRecord group, out SpriteRecord record, out string error)
        {
            group = null; record = null;
            if (!ValidateIntegrity(pin, out error)) return false;
            if (string.IsNullOrEmpty(key) || !byGroup.TryGetValue(key, out group)) return Error("Missing exact current group: " + key, out error);
            return TryGetExact(group.File, pin, out record, out error);
        }
        private string ComputeFingerprint()
        {
            var b = new StringBuilder(); Add(b, packageId); Add(b, sourceRootPath); Add(b, sourceRootGuid); Add(b, sourceManifestSha256);
            if (sprites != null) foreach (SpriteRecord row in sprites) { if (row == null) Add(b, "null"); else row.Append(b); }
            if (groups != null) foreach (GroupRecord row in groups) { if (row == null) Add(b, "null"); else row.Append(b); }
            return Sha256(Encoding.UTF8.GetBytes(b.ToString()));
        }
#if UNITY_EDITOR
        public void SetEditorData(string rootPath, string rootGuid, string sourceHash, SpriteRecord[] records, GroupRecord[] groupRecords)
        { packageId = PackageId; sourceRootPath = rootPath; sourceRootGuid = rootGuid; sourceManifestSha256 = sourceHash;
            sprites = records; groups = groupRecords; fingerprint = ComputeFingerprint(); Invalidate(); }
#endif
        public static bool RelativePng(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.Ordinal) || path.StartsWith("/", StringComparison.Ordinal)
                || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0) return false;
            foreach (string part in path.Split('/')) if (part.Length == 0 || part == "." || part == "..") return false;
            return path.StartsWith("decor/", StringComparison.Ordinal);
        }
        public static bool IsHash(string value, int length)
        { if (value == null || value.Length != length) return false; foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false; return true; }
        public static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
        public static string Sha256(byte[] bytes)
        { using (SHA256 algorithm = SHA256.Create()) { byte[] hash = algorithm.ComputeHash(bytes); var b = new StringBuilder(hash.Length * 2); foreach (byte value in hash) b.Append(value.ToString("x2", CultureInfo.InvariantCulture)); return b.ToString(); } }
        private static string FloatBits(float f) { return BitConverter.ToUInt32(BitConverter.GetBytes(f), 0).ToString("x8", CultureInfo.InvariantCulture); }
        private static void Add(StringBuilder b, string value) { value = value ?? ""; b.Append(value.Length).Append(':').Append(value).Append(';'); }
        private static bool Error(string value, out string error) { error = value; return false; }
    }
}
