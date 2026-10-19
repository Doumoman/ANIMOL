#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03
{
    public static class EchoTheaterV03AnimationBuilder
    {
        [Serializable] private sealed class Manifest
        {
            public int schemaVersion;
            public string packageId;
            public SpriteRow[] sprites;
            public ClipRow[] clips;
            public PartRow[] parts;
            public DiceMapRef diceOrientationMap;
        }
        [Serializable] private sealed class SpriteRow
        { public string file, sha256; public int width, height; public float ppu; public float[] pivot; }
        [Serializable] private sealed class ClipRow
        { public string key, family, heading, state; public float fps; public bool loop; public string[] frames; }
        [Serializable] private sealed class PartRow { public string key, file; }
        [Serializable] private sealed class DiceMapRef { public string file, sha256; }
        [Serializable] private sealed class Report
        {
            public bool success, projectBindingsVerified, legacyReferenceAuditComplete;
            public string packageId, sourceRoot, manifestSha256, diceMapSha256, catalogFingerprint, error;
            public int spriteCount, clipCount, partCount;
            public string[] generatedClipAssets;
            public string scope = "Asset builder only. No simulation adapters, Animator Controller, scene migration or gameplay execution is generated/verified.";
        }
        private const string ManifestName = "animation_catalog.json";
        private const string GeneratedFolder = "_Generated";
        private const string CatalogName = "EchoTheaterV03VisualCatalog.asset";

        [MenuItem("Tools/ANIMOL/Echo Theater V03/Build selected animation_catalog.json")]
        private static void BuildSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            try
            {
                EchoTheaterV03VisualCatalog catalog = BuildFromManifest(path);
                Selection.activeObject = catalog;
                Debug.Log("[Echo Theater V03] Verified catalog and frame clips built. Bind actual simulation events and audit project references separately.", catalog);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // Zero-argument Unity -executeMethod entry. Deliberately no catch/Exit(0): failures stay failures.
        public static void BuildFromCommandLine()
        {
            const string flag = "-theaterV03Manifest";
            string[] args = Environment.GetCommandLineArgs();
            string path = null;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] != flag) continue;
                if (path != null) throw new ArgumentException("Duplicate " + flag + " argument.");
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException("Missing value for " + flag + ".");
                path = args[++i];
            }
            if (path == null) throw new ArgumentException("Required argument: " + flag + " Assets/.../animation_catalog.json");
            BuildFromManifest(path);
        }

        // Public entry for a project's existing Editor automation; paths are Unity Assets-relative.
        public static EchoTheaterV03VisualCatalog BuildFromManifest(string manifestAssetPath)
        {
            string root = ValidateManifestPath(manifestAssetPath);
            var report = new Report { packageId = EchoTheaterV03VisualCatalog.RequiredPackageId, sourceRoot = root };
            try
            {
                byte[] manifestBytes = File.ReadAllBytes(FullPath(manifestAssetPath));
                report.manifestSha256 = EchoTheaterV03VisualCatalog.Sha256Of(manifestBytes);
                string jsonText = Encoding.UTF8.GetString(manifestBytes);
                EchoTheaterV03JsonSchema.Validate(jsonText);
                Manifest input;
                try { input = JsonUtility.FromJson<Manifest>(jsonText); }
                catch (Exception e) { throw new InvalidDataException("Malformed animation catalog JSON.", e); }
                Require(input != null && input.schemaVersion == EchoTheaterV03VisualCatalog.RequiredSchemaVersion
                    && input.packageId == EchoTheaterV03VisualCatalog.RequiredPackageId, "Wrong/missing schemaVersion or packageId.");
                Require(input.sprites != null && input.sprites.Length > 0 && input.clips != null && input.clips.Length > 0,
                    "sprites[] and clips[] must be nonempty.");

                var byFile = new Dictionary<string, EchoTheaterV03VisualCatalog.Frame>(StringComparer.Ordinal);
                var portableFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var allFrames = new List<EchoTheaterV03VisualCatalog.Frame>();
                foreach (SpriteRow row in input.sprites)
                {
                    Require(row != null && EchoTheaterV03VisualCatalog.IsRelativePngPath(row.file), "Invalid relative PNG path.");
                    Require(!byFile.ContainsKey(row.file) && portableFiles.Add(row.file), "Duplicate/case-colliding sprite path: " + row.file);
                    Require(row.width > 0 && row.height > 0 && EchoTheaterV03VisualCatalog.Finite(row.ppu) && row.ppu > 0
                        && row.pivot != null && row.pivot.Length == 2
                        && EchoTheaterV03VisualCatalog.Finite(row.pivot[0]) && EchoTheaterV03VisualCatalog.Finite(row.pivot[1])
                        && row.pivot[0] >= 0 && row.pivot[0] <= 1 && row.pivot[1] >= 0 && row.pivot[1] <= 1
                        && EchoTheaterV03VisualCatalog.IsHash(row.sha256, 64), "Invalid sprite metadata: " + row.file);
                    string assetPath = root + "/" + row.file;
                    Require(File.Exists(FullPath(assetPath)), "Missing latest PNG: " + assetPath);
                    string actualHash = EchoTheaterV03VisualCatalog.Sha256Of(File.ReadAllBytes(FullPath(assetPath)));
                    Require(string.Equals(actualHash, row.sha256, StringComparison.OrdinalIgnoreCase), "PNG SHA256 mismatch: " + row.file);
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    Require(importer != null && importer.textureType == TextureImporterType.Sprite
                        && importer.spriteImportMode == SpriteImportMode.Single,
                        "PNG must already be imported as Sprite Single using the V03 exact importer: " + row.file);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    string guid; long localId;
                    Require(sprite != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out localId),
                        "Cannot resolve imported latest Sprite/GUID/fileID: " + row.file);
                    // Out variables are explicitly obtained again for compiler definite-assignment clarity.
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out localId);
                    var frame = new EchoTheaterV03VisualCatalog.Frame(sprite, row.file, actualHash, guid, localId,
                        row.width, row.height, row.ppu, new Vector2(row.pivot[0], row.pivot[1]));
                    string error;
                    Require(frame.Validate(out error), error);
                    byFile.Add(row.file, frame); allFrames.Add(frame);
                }

                var clips = new List<EchoTheaterV03VisualCatalog.Clip>();
                bool hasDice = false;
                var clipKeys = new HashSet<string>(StringComparer.Ordinal);
                var directions = new HashSet<string>(StringComparer.Ordinal);
                var outputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ClipRow row in input.clips)
                {
                    Require(row != null && Token(row.key) && Token(row.family) && Token(row.heading) && Token(row.state), "Invalid clip identity.");
                    Require(clipKeys.Add(row.key), "Duplicate clip key: " + row.key);
                    Require(directions.Add(row.family + "\u001f" + row.heading + "\u001f" + row.state),
                        "Duplicate exact family/heading/state: " + row.key);
                    Require(EchoTheaterV03VisualCatalog.Finite(row.fps) && row.fps > 0 && row.frames != null && row.frames.Length > 0,
                        "Missing/invalid FPS or frames: " + row.key);
                    bool walk = row.state.Equals("walk", StringComparison.OrdinalIgnoreCase) || row.state.Equals("move", StringComparison.OrdinalIgnoreCase);
                    Require(!walk || (row.fps >= 10 && row.frames.Length >= 2), "Walk/move must contain >=2 frames at >=10 FPS: " + row.key);
                    var frames = new EchoTheaterV03VisualCatalog.Frame[row.frames.Length];
                    for (int i = 0; i < frames.Length; ++i)
                        Require(row.frames[i] != null && byFile.TryGetValue(row.frames[i], out frames[i]),
                            "Missing/unregistered latest frame in " + row.key + ": " + row.frames[i]);
                    // Repeated paths are legitimate holds/endpoints/reverse reuse, not duplicate assets.
                    var clip = new EchoTheaterV03VisualCatalog.Clip(row.key, row.family, row.heading, row.state, row.fps, row.loop, frames);
                    Require(outputPaths.Add(ClipAssetPath(root, clip.Key)), "Generated clip filename collision: " + clip.Key);
                    clips.Add(clip);
                    if (clip.Family == "dice_block") hasDice = true;
                }
                var parts = new List<EchoTheaterV03VisualCatalog.Part>();
                var partKeys = new HashSet<string>(StringComparer.Ordinal);
                if (input.parts != null) foreach (PartRow row in input.parts)
                {
                    EchoTheaterV03VisualCatalog.Frame frame;
                    Require(row != null && Token(row.key) && row.file != null && partKeys.Add(row.key), "Invalid/duplicate part key.");
                    Require(byFile.TryGetValue(row.file, out frame), "Missing/unregistered latest part: " + row.file);
                    parts.Add(new EchoTheaterV03VisualCatalog.Part(row.key, frame));
                }

                // Complete validation precedes asset mutation. No old asset/controller is deleted.
                string rootGuid = AssetDatabase.AssetPathToGUID(root);
                byte[] diceMapBytes = null;
                if (hasDice)
                {
                    string mapPath = root + "/dice_orientation_map.json";
                    Require(File.Exists(FullPath(mapPath)), "Dice clips require dice_orientation_map.json in the latest root.");
                    diceMapBytes = File.ReadAllBytes(FullPath(mapPath));
                    report.diceMapSha256 = EchoTheaterV03VisualCatalog.Sha256Of(diceMapBytes);
                    if (input.diceOrientationMap != null)
                        Require(input.diceOrientationMap.file == "dice_orientation_map.json"
                            && EchoTheaterV03VisualCatalog.IsHash(input.diceOrientationMap.sha256, 64)
                            && string.Equals(input.diceOrientationMap.sha256, report.diceMapSha256, StringComparison.OrdinalIgnoreCase),
                            "animation_catalog diceOrientationMap path/SHA256 differs from actual latest map.");
                }
                var candidate = ScriptableObject.CreateInstance<EchoTheaterV03VisualCatalog>();
                try
                {
                    candidate.SetEditorData(root, rootGuid, report.manifestSha256, allFrames.ToArray(), clips.ToArray(), parts.ToArray());
                    if (hasDice) candidate.SetEditorDiceMapSha256(report.diceMapSha256);
                    string error;
                    Require(candidate.ValidateIntegrity(report.manifestSha256, out error), error);
                    if (hasDice)
                    {
                        EchoTheaterV03DiceVisualMap map;
                        EchoTheaterV03JsonSchema.ValidateDiceMap(Encoding.UTF8.GetString(diceMapBytes));
                        Require(EchoTheaterV03DiceVisualMap.TryParse(diceMapBytes, report.diceMapSha256,
                            candidate, report.manifestSha256, out map, out error), error);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(candidate); }
                EnsureGeneratedFolder(root);
                var generatedPaths = new List<string>();
                foreach (var clip in clips)
                {
                    string assetPath = ClipAssetPath(root, clip.Key);
                    UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    Require(existing == null || existing is AnimationClip, "Refusing to overwrite unrelated asset: " + assetPath);
                    var animation = existing as AnimationClip;
                    if (animation == null) { animation = new AnimationClip(); AssetDatabase.CreateAsset(animation, assetPath); }
                    animation.name = clip.Key; animation.frameRate = clip.Fps;
                    animation.ClearCurves();
                    var keys = new ObjectReferenceKeyframe[clip.FrameCount + 1];
                    for (int i = 0; i < clip.FrameCount; ++i)
                        keys[i] = new ObjectReferenceKeyframe { time = i / clip.Fps, value = clip.GetFrame(i).Sprite };
                    keys[clip.FrameCount] = new ObjectReferenceKeyframe { time = clip.FrameCount / clip.Fps,
                        value = clip.GetFrame(clip.Loop ? 0 : clip.FrameCount - 1).Sprite };
                    AnimationUtility.SetObjectReferenceCurve(animation,
                        new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
                    AnimationUtility.SetAnimationEvents(animation, new AnimationEvent[0]);
                    var settings = AnimationUtility.GetAnimationClipSettings(animation);
                    settings.loopTime = clip.Loop;
                    AnimationUtility.SetAnimationClipSettings(animation, settings);
                    EditorUtility.SetDirty(animation); generatedPaths.Add(assetPath);
                }
                string catalogPath = root + "/" + GeneratedFolder + "/" + CatalogName;
                UnityEngine.Object oldCatalog = AssetDatabase.LoadMainAssetAtPath(catalogPath);
                Require(oldCatalog == null || oldCatalog is EchoTheaterV03VisualCatalog, "Refusing to overwrite unrelated catalog asset.");
                var output = oldCatalog as EchoTheaterV03VisualCatalog;
                if (output == null) { output = ScriptableObject.CreateInstance<EchoTheaterV03VisualCatalog>(); AssetDatabase.CreateAsset(output, catalogPath); }
                output.SetEditorData(root, rootGuid, report.manifestSha256, allFrames.ToArray(), clips.ToArray(), parts.ToArray());
                if (hasDice) output.SetEditorDiceMapSha256(report.diceMapSha256);
                EditorUtility.SetDirty(output); AssetDatabase.SaveAssets();
                report.success = true; report.catalogFingerprint = output.Fingerprint;
                report.spriteCount = allFrames.Count; report.clipCount = clips.Count; report.partCount = parts.Count;
                report.generatedClipAssets = generatedPaths.ToArray(); WriteReport(root, report);
                return output;
            }
            catch (Exception e)
            {
                report.success = false; report.error = e.Message; WriteReport(root, report);
                throw;
            }
        }

        // Editor/build validation can inspect original bytes and GUID/fileID; runtime cannot hash unreadable imported PNG bytes.
        public static void ValidateBuiltCatalog(EchoTheaterV03VisualCatalog catalog)
        {
            Require(catalog != null, "Missing latest V03 catalog.");
            string root = catalog.SourceRootPath;
            string manifest = root + "/" + ManifestName;
            Require(ValidateManifestPath(manifest) == root, "Catalog root differs from latest package root.");
            Require(AssetDatabase.GetAssetPath(catalog) == root + "/" + GeneratedFolder + "/" + CatalogName,
                "Catalog asset is outside its authoritative latest _Generated root.");
            Require(AssetDatabase.AssetPathToGUID(root) == catalog.SourceRootGuid, "Latest source root GUID changed.");
            string hash = EchoTheaterV03VisualCatalog.Sha256Of(File.ReadAllBytes(FullPath(manifest)));
            string error;
            Require(catalog.ValidateIntegrity(hash, out error), error);
            if (!string.IsNullOrEmpty(catalog.DiceOrientationMapSha256))
            {
                byte[] mapBytes = File.ReadAllBytes(FullPath(root + "/dice_orientation_map.json"));
                EchoTheaterV03DiceVisualMap map;
                EchoTheaterV03JsonSchema.ValidateDiceMap(Encoding.UTF8.GetString(mapBytes));
                Require(EchoTheaterV03DiceVisualMap.TryParse(mapBytes, catalog.DiceOrientationMapSha256,
                    catalog, hash, out map, out error), error);
            }
            for (int i = 0; i < catalog.SpriteCount; ++i)
            {
                var f = catalog.GetSpriteRecord(i);
                string expectedPath = root + "/" + f.File;
                Require(AssetDatabase.GetAssetPath(f.Sprite) == expectedPath, "Sprite reference points outside latest root: " + f.File);
                string guid; long fileId;
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f.Sprite, out guid, out fileId)
                    && guid == f.AssetGuid && fileId == f.LocalFileId, "Sprite GUID/fileID mismatch: " + f.File);
                Require(EchoTheaterV03VisualCatalog.Sha256Of(File.ReadAllBytes(FullPath(expectedPath))) == f.Sha256,
                    "Latest PNG bytes changed after catalog build: " + f.File);
            }
            for (int i = 0; i < catalog.ClipCount; ++i)
            {
                var clip = catalog.GetClipAt(i);
                var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipAssetPath(root, clip.Key));
                Require(asset != null && Mathf.Abs(asset.frameRate - clip.Fps) < .001f, "Missing/stale generated animation: " + clip.Key);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(asset);
                Require(bindings.Length == 1 && bindings[0].path == "" && bindings[0].type == typeof(SpriteRenderer)
                    && bindings[0].propertyName == "m_Sprite", "Unexpected generated animation binding: " + clip.Key);
                var values = AnimationUtility.GetObjectReferenceCurve(asset, bindings[0]);
                Require(values.Length == clip.FrameCount + 1 && AnimationUtility.GetAnimationEvents(asset).Length == 0
                    && AnimationUtility.GetAnimationClipSettings(asset).loopTime == clip.Loop, "Generated animation events/length/loop mismatch: " + clip.Key);
                for (int j = 0; j < values.Length; ++j)
                {
                    int expectedIndex = j < clip.FrameCount ? j : clip.Loop ? 0 : clip.FrameCount - 1;
                    Require(values[j].value == clip.GetFrame(expectedIndex).Sprite && Mathf.Abs(values[j].time - j / clip.Fps) < .0001f,
                        "Generated animation frame/timing mismatch: " + clip.Key);
                }
                Require(AnimationUtility.GetCurveBindings(asset).Length == 0, "Generated clip must not animate transforms/gameplay properties: " + clip.Key);
            }
        }

        public static string ClipAssetPath(string root, string key)
        {
            var b = new StringBuilder();
            foreach (char c in key) b.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            string prefix = b.ToString(); if (prefix.Length > 70) prefix = prefix.Substring(0, 70);
            string suffix = EchoTheaterV03VisualCatalog.Sha256Of(Encoding.UTF8.GetBytes(key)).Substring(0, 12);
            return root + "/" + GeneratedFolder + "/" + prefix + "_" + suffix + ".anim";
        }
        private static string ValidateManifestPath(string path)
        {
            Require(!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal)
                && path.IndexOf('\\') < 0 && path.IndexOf("/../", StringComparison.Ordinal) < 0
                && Path.GetFileName(path) == ManifestName, "Select the latest package's Assets-relative animation_catalog.json.");
            string root = Path.GetDirectoryName(path).Replace('\\', '/');
            Require(root.EndsWith("/" + EchoTheaterV03VisualCatalog.RequiredPackageId, StringComparison.Ordinal)
                && AssetDatabase.IsValidFolder(root) && File.Exists(FullPath(path)), "Wrong/missing latest source root or manifest.");
            return root;
        }
        private static string FullPath(string assetPath) { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)); }
        private static bool Token(string s) { return !string.IsNullOrWhiteSpace(s) && s == s.Trim() && s.IndexOf('\u001f') < 0; }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
        private static void EnsureGeneratedFolder(string root)
        {
            string folder = root + "/" + GeneratedFolder;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(root, GeneratedFolder);
        }
        private static void WriteReport(string root, Report report)
        {
            EnsureGeneratedFolder(root);
            string path = root + "/" + GeneratedFolder + "/animation_build_report.json";
            File.WriteAllText(FullPath(path), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }
    }

    // Asset integrity gate only; this does not claim project-wide reference migration or simulation bindings.
    public sealed class EchoTheaterV03AnimationBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 1000; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:EchoTheaterV03VisualCatalog");
            if (guids.Length != 1) throw new BuildFailedException("Exactly one authoritative Echo Theater V03 animation catalog is required; found " + guids.Length + ".");
            foreach (string guid in guids)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                try { EchoTheaterV03AnimationBuilder.ValidateBuiltCatalog(catalog); }
                catch (Exception e) { throw new BuildFailedException("Echo Theater V03 latest catalog failed: " + e.Message); }
            }
        }
    }
}
#endif
