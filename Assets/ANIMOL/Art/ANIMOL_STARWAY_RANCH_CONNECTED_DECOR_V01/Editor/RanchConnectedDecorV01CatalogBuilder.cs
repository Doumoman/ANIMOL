#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV01
{
    public static class RanchConnectedDecorV01CatalogBuilder
    {
        public const string CatalogName = "RanchConnectedDecorV01Catalog.asset";
        [Serializable] private sealed class Report
        {
            public bool generatedCatalogAssetSaved, actualGameplayBindingsVerified = false, productionScenesModified = false;
            public int sprites, groups;
            public string sourceManifest, manifestSha256, catalogAsset;
            public string scope = "Asset catalog build only. No map generation, collision/actor changes, scene migration or actual gameplay verification.";
        }
        [MenuItem("Tools/ANIMOL/Ranch Connected Decor V01/Build selected manifest.json")]
        private static void BuildSelected()
        {
            try { Selection.activeObject = BuildFromManifest(AssetDatabase.GetAssetPath(Selection.activeObject)); }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static void BuildFromCommandLine()
        {
            const string flag = "-ranchDecorManifest";
            string path = null; string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != flag) continue;
                if (path != null || i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException("Missing or duplicate " + flag + " Assets/.../manifest.json");
                path = args[++i];
            }
            if (path == null) throw new ArgumentException("Required " + flag + " Assets/.../manifest.json");
            BuildFromManifest(path);
        }
        public static RanchConnectedDecorV01Catalog BuildFromManifest(string manifestAssetPath)
        {
            string root = ValidateManifestPath(manifestAssetPath);
            string pin; RanchConnectedDecorV01Manifest input = LoadManifest(root, out pin);
            RanchConnectedDecorV01SpriteImporter.InvalidateRoot(root);
            // Complete JSON/path/metadata/source SHA preflight before modifying even addon imports.
            foreach (var row in input.assets) ValidateSourcePng(root, row);
            string generated = root + "/_Generated";
            if (!AssetDatabase.IsValidFolder(generated))
            {
                Require(!File.Exists(FullPath(generated)), "Generated folder path is occupied by a file.");
                AssetDatabase.CreateFolder(root, "_Generated");
            }
            string catalogPath = generated + "/" + CatalogName;
            UnityEngine.Object prior = AssetDatabase.LoadMainAssetAtPath(catalogPath);
            Require(prior == null || prior is RanchConnectedDecorV01Catalog, "Refusing to overwrite an unrelated catalog asset.");

            var records = new List<RanchConnectedDecorV01Catalog.SpriteRecord>();
            foreach (var row in input.assets)
            {
                string path = root + "/" + row.file;
                // This exact root's postprocessor reads only declared latest assets. No global Refresh.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                ValidateImporter(path, row);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                string guid; long fileId;
                Require(sprite != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out fileId),
                    "Latest imported Sprite/GUID/fileID is missing: " + row.file);
                // Explicitly reacquire for compiler definite-assignment clarity.
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out fileId);
                records.Add(new RanchConnectedDecorV01Catalog.SpriteRecord(row, sprite, guid, fileId));
            }
            var groups = new List<RanchConnectedDecorV01Catalog.GroupRecord>();
            foreach (var row in input.groups) groups.Add(new RanchConnectedDecorV01Catalog.GroupRecord(row.key, row.file, row.footprintCells[0], row.footprintCells[1]));
            var candidate = ScriptableObject.CreateInstance<RanchConnectedDecorV01Catalog>();
            try
            {
                candidate.SetEditorData(root, AssetDatabase.AssetPathToGUID(root), pin, records.ToArray(), groups.ToArray());
                string error; Require(candidate.ValidateIntegrity(pin, out error), error);
                var output = prior as RanchConnectedDecorV01Catalog;
                if (output == null) { output = ScriptableObject.CreateInstance<RanchConnectedDecorV01Catalog>(); AssetDatabase.CreateAsset(output, catalogPath); }
                EditorUtility.CopySerialized(candidate, output); EditorUtility.SetDirty(output); AssetDatabase.SaveAssetIfDirty(output);
                ValidateBuiltCatalog(output);
                var report = new Report { generatedCatalogAssetSaved = true, sprites = records.Count, groups = groups.Count,
                    sourceManifest = manifestAssetPath, manifestSha256 = pin, catalogAsset = catalogPath };
                string reportPath = generated + "/decor_catalog_build_report.json";
                File.WriteAllText(FullPath(reportPath), JsonUtility.ToJson(report, true), new UTF8Encoding(false)); AssetDatabase.ImportAsset(reportPath);
                Debug.Log("[Ranch Connected Decor V01] Current catalog built: " + catalogPath + ". Actual host bindings and Play Mode remain unverified.", output);
                return output;
            }
            finally { UnityEngine.Object.DestroyImmediate(candidate); }
        }

        public static void ValidateBuiltCatalog(RanchConnectedDecorV01Catalog catalog)
        {
            Require(catalog != null, "Missing latest decor catalog.");
            string root = catalog.SourceRootPath;
            ValidateManifestPath(root + "/" + RanchConnectedDecorV01Catalog.ManifestName);
            string pin; RanchConnectedDecorV01Manifest source = LoadManifest(root, out pin);
            Require(AssetDatabase.AssetPathToGUID(root) == catalog.SourceRootGuid, "Catalog source-root GUID differs.");
            catalog.Invalidate(); string error; Require(catalog.ValidateIntegrity(pin, out error), error);
            Require(source.assets.Length == catalog.SpriteCount && source.groups.Length == catalog.GroupCount, "Catalog count differs from current source.");
            var current = new Dictionary<string, RanchConnectedDecorV01AssetInput>(StringComparer.Ordinal);
            foreach (var row in source.assets) current.Add(row.file, row);
            for (int i = 0; i < catalog.SpriteCount; i++)
            {
                var row = catalog.GetSpriteAt(i); RanchConnectedDecorV01AssetInput input;
                Require(current.TryGetValue(row.File, out input), "Catalog has an undeclared current sprite.");
                ValidateSourcePng(root, input); string path = root + "/" + row.File; ValidateImporter(path, input);
                Require(string.Equals(input.sha256, row.Sha256, StringComparison.OrdinalIgnoreCase), "Catalog PNG SHA differs.");
                Sprite actual = AssetDatabase.LoadAssetAtPath<Sprite>(path); string guid; long fileId;
                Require(actual != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out guid, out fileId), "Current sprite identity missing.");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out guid, out fileId);
                Require(actual == row.Sprite && guid == row.AssetGuid && fileId == row.LocalFileId, "Catalog references a different Sprite/GUID/fileID: " + row.File);
            }
            for (int i = 0; i < source.groups.Length; i++)
            {
                var currentGroup = source.groups[i]; RanchConnectedDecorV01Catalog.GroupRecord group; RanchConnectedDecorV01Catalog.SpriteRecord art;
                Require(catalog.TryGetGroup(currentGroup.key, pin, out group, out art, out error), error);
                Require(group.File == currentGroup.file && group.WidthCells == currentGroup.footprintCells[0] && group.HeightCells == currentGroup.footprintCells[1], "Current group metadata differs.");
            }
        }
        public static RanchConnectedDecorV01Manifest LoadManifest(string root, out string pin)
        {
            ValidateManifestPath(root + "/" + RanchConnectedDecorV01Catalog.ManifestName);
            string path = root + "/" + RanchConnectedDecorV01Catalog.ManifestName;
            byte[] bytes = File.ReadAllBytes(FullPath(path));
            string actual = RanchConnectedDecorV01Catalog.Sha256(bytes);
            pin = File.ReadAllText(FullPath(root + "/" + RanchConnectedDecorV01Catalog.PinName)).Trim();
            Require(RanchConnectedDecorV01Catalog.IsHash(pin, 64) && string.Equals(actual, pin, StringComparison.OrdinalIgnoreCase), "Current manifest bytes differ from manifest_sha256.txt.");
            string json = Encoding.UTF8.GetString(bytes); RanchConnectedDecorV01JsonSchema.Validate(json);
            var input = JsonUtility.FromJson<RanchConnectedDecorV01Manifest>(json);
            Require(input != null && input.schemaVersion == 1 && input.packageId == RanchConnectedDecorV01Catalog.PackageId
                && input.assets != null && input.assets.Length > 0 && input.groups != null && input.groups.Length > 0, "Invalid addon manifest schema/package/assets/groups.");
            var files = new Dictionary<string, RanchConnectedDecorV01AssetInput>(StringComparer.Ordinal);
            var portable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in input.assets)
            {
                ValidateAssetRow(row);
                Require(!files.ContainsKey(row.file) && portable.Add(row.file), "Duplicate/case-colliding current PNG path."); files.Add(row.file, row);
            }
            foreach (string family in new[] { RanchConnectedDecorV01Planner.CreamFence, RanchConnectedDecorV01Planner.StoneWall, RanchConnectedDecorV01Planner.Meadow })
            {
                int count = family == RanchConnectedDecorV01Planner.Meadow ? 256 : 16;
                for (int mask = 0; mask < count; mask++)
                {
                    string file = RanchConnectedDecorV01Planner.NetworkFile(family, mask);
                    Require(files.ContainsKey(file), "Missing current connected variant: " + file);
                    var row = files[file]; Require(row.family == family && row.footprintCells[0] == 1 && row.footprintCells[1] == 1, "Connected variant metadata differs: " + file);
                }
                if (count == 16) Require(files.ContainsKey("decor/connected/" + family + "/periodic_post.png"), "Missing current optional-post art for declared rail family.");
            }
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in input.groups)
            {
                Require(group != null && !string.IsNullOrWhiteSpace(group.key) && keys.Add(group.key) && group.footprintCells != null && group.footprintCells.Length == 2, "Invalid/duplicate group record.");
                RanchConnectedDecorV01AssetInput art;
                Require(files.TryGetValue(group.file, out art) && group.footprintCells[0] == art.footprintCells[0] && group.footprintCells[1] == art.footprintCells[1], "Group footprint differs from its exact art metadata.");
            }
            return input;
        }
        public static void ValidateAssetRow(RanchConnectedDecorV01AssetInput row)
        {
            Require(row != null && RanchConnectedDecorV01Catalog.RelativePng(row.file) && RanchConnectedDecorV01Catalog.IsHash(row.sha256, 64), "Invalid current PNG file/SHA.");
            Require(row.width > 0 && row.height > 0 && row.width <= 8192 && row.height <= 8192 && row.ppu == 256f
                && row.pivot != null && row.pivot.Length == 2 && RanchConnectedDecorV01Catalog.Finite(row.pivot[0]) && RanchConnectedDecorV01Catalog.Finite(row.pivot[1])
                && row.pivot[0] >= 0 && row.pivot[0] <= 1 && row.pivot[1] >= 0 && row.pivot[1] <= 1
                && row.logicalRect != null && row.logicalRect.Length == 4 && row.footprintCells != null && row.footprintCells.Length == 2
                && !string.IsNullOrWhiteSpace(row.family) && !string.IsNullOrWhiteSpace(row.kind), "Missing/invalid current art metadata: " + row.file);
            int[] r = row.logicalRect, c = row.footprintCells;
            Require(c[0] >= 1 && c[0] <= 64 && c[1] >= 1 && c[1] <= 64 && r[0] >= 0 && r[1] >= 0
                && r[2] == c[0] * 256 && r[3] == c[1] * 256 && (long)r[0] + r[2] <= row.width && (long)r[1] + r[3] <= row.height,
                "Ground footprint differs from current image logical rectangle: " + row.file);
            Require(Mathf.Abs(row.pivot[0] * row.width - (r[0] + r[2] * .5f)) <= .01f
                && Mathf.Abs((1f - row.pivot[1]) * row.height - (r[1] + r[3] * .5f)) <= .01f, "Pivot must be the exact logical ground centre: " + row.file);
        }
        public static void ValidateSourcePng(string root, RanchConnectedDecorV01AssetInput row)
        {
            string path = FullPath(root + "/" + row.file); byte[] bytes = File.ReadAllBytes(path);
            Require(string.Equals(RanchConnectedDecorV01Catalog.Sha256(bytes), row.sha256, StringComparison.OrdinalIgnoreCase), "Current PNG SHA mismatch: " + row.file);
            Require(bytes.Length > 24 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71
                && UInt32BE(bytes, 16) == row.width && UInt32BE(bytes, 20) == row.height, "Current PNG canvas dimensions differ: " + row.file);
        }
        public static void ValidateImporter(string path, RanchConnectedDecorV01AssetInput row)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && importer.filterMode == FilterMode.Bilinear && importer.textureCompression == TextureImporterCompression.Uncompressed
                && !importer.crunchedCompression && !importer.mipmapEnabled && importer.npotScale == TextureImporterNPOTScale.None
                && importer.wrapMode == TextureWrapMode.Clamp && Mathf.Abs(importer.spritePixelsPerUnit - row.ppu) < .001f,
                "Current addon Sprite importer settings differ: " + path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            Require(settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spriteMeshType == SpriteMeshType.FullRect
                && !settings.spriteGenerateFallbackPhysicsShape && Mathf.Abs(settings.spritePivot.x - row.pivot[0]) < .0001f && Mathf.Abs(settings.spritePivot.y - row.pivot[1]) < .0001f,
                "Current pivot/mesh/physics import settings differ: " + path);
            foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" })
                Require(!importer.GetPlatformTextureSettings(platform).overridden, "Current addon platform override must be cleared: " + path + " / " + platform);
        }
        public static string ValidateManifestPath(string path)
        {
            Require(!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal) && path.IndexOf('\\') < 0
                && path.EndsWith("/" + RanchConnectedDecorV01Catalog.PackageId + "/" + RanchConnectedDecorV01Catalog.ManifestName, StringComparison.Ordinal), "Select the exact new addon Assets/.../manifest.json.");
            foreach (string segment in path.Split('/')) Require(segment != "." && segment != ".." && segment.Length > 0, "Invalid source Assets path.");
            return path.Substring(0, path.LastIndexOf('/'));
        }
        public static string FullPath(string assetPath)
        { return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath)); }
        private static long UInt32BE(byte[] b, int i) { return ((long)b[i] << 24) | ((long)b[i + 1] << 16) | ((long)b[i + 2] << 8) | b[i + 3]; }
        private static void Require(bool value, string error) { if (!value) throw new InvalidDataException(error); }
    }

    public sealed class RanchConnectedDecorV01BuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            // Only this addon's generated catalogs are inspected. No production scenes,
            // Build Settings, Prefabs, other themes or old catalogs are rewritten.
            string[] guids = AssetDatabase.FindAssets("t:RanchConnectedDecorV01Catalog");
            foreach (string guid in guids)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<RanchConnectedDecorV01Catalog>(AssetDatabase.GUIDToAssetPath(guid));
                try { RanchConnectedDecorV01CatalogBuilder.ValidateBuiltCatalog(catalog); }
                catch (Exception e) { throw new BuildFailedException("Latest ranch decoration catalog failed: " + e.Message); }
            }
        }
    }
}
#endif
