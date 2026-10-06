using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class CampaignMapBackupStore
    {
        public const string BackupRoot = "UserSettings/ANIMOL/MapBackups";
        private const string LegacyBackupRoot = "Assets/ANIMOL/MapBackups";

        public static string CreateBackup(StageMapDefinition map, string label = null)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.StageId)) throw new ArgumentException("Map identity is required.", nameof(map));
            var folder = $"{BackupRoot}/{Sanitize(map.StageId)}";
            Directory.CreateDirectory(Path.GetFullPath(folder));
            var safeLabel = string.IsNullOrWhiteSpace(label) ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") : Sanitize(label);
            var path = $"{folder}/{Sanitize(map.StageId)}-v{map.MapVersion}-{safeLabel}-{Guid.NewGuid():N}.json";
            File.WriteAllText(Path.GetFullPath(path), EditorJsonUtility.ToJson(map, true));
            return path;
        }

        public static bool Restore(StageMapDefinition map, string backupAssetPath)
        {
            if (map == null || string.IsNullOrWhiteSpace(backupAssetPath) || !File.Exists(Path.GetFullPath(backupAssetPath))) return false;
            var originalStageId = map.StageId;
            var json = File.ReadAllText(Path.GetFullPath(backupAssetPath));
            var scratch = UnityEngine.Object.Instantiate(map);
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, scratch);
                ANIMOL.Gameplay.FreeShapeArtRegistry.Load(scratch.FreeShapeTerrain);
                if (scratch.StageId != originalStageId || scratch.ThemeId != map.ThemeId)
                    throw new InvalidOperationException("Backup identity does not match the selected map.");
            }
            finally { UnityEngine.Object.DestroyImmediate(scratch); }
            Undo.RecordObject(map, "Restore ANIMOL Map Backup");
            EditorJsonUtility.FromJsonOverwrite(json, map);
            if (map.StageId != originalStageId) throw new InvalidOperationException("Backup stable ID does not match the selected map.");
            map.InvalidateHumanReview();
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            return true;
        }

        public static string FindLatest(StageMapDefinition map)
        {
            if (map == null) return string.Empty;
            return new[] { BackupRoot, LegacyBackupRoot }
                .Select(root => Path.GetFullPath(root + "/" + Sanitize(map.StageId)))
                .Where(Directory.Exists).SelectMany(folder => Directory.GetFiles(folder, "*.json"))
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? string.Empty;
        }

        private static string Sanitize(string value) => string.Concat(value.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
    }
}
