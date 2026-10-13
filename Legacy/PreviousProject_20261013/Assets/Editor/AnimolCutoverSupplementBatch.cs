using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ANIMOL.NamedArt.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AnimolCutoverSupplementBatch
{
    [Serializable] private sealed class Move { public string source; public string destination; public string guid; }
    [Serializable] private sealed class Plan { public Move[] sprites; }
    [Serializable] private sealed class ImportCheck { public string path; public string guid; public int width, height; public float ppu; public int spriteMode; public string filter, compression; public bool mipmaps; public int sprites; }
    [Serializable] private sealed class Report { public string unity; public string activeTarget; public int moved; public ImportCheck[] imports; public string[] clips; public string[] entryPoints; }
    [Serializable] private sealed class ReferenceReport { public int checkedAssets, checkedGuidReferences; public string[] missingGuidReferences; public string[] compiledLegacyTypes; public string androidDefines; }
    private static string Journal => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp/animol-cutover-supplement");

    private static void UpdateNamedArt(NamedArtIndex index, Plan plan)
    {
        var keys = new Dictionary<string, string>();
        foreach (var move in plan.sprites)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(move.destination);
            foreach (var source in index.Sources.Where(s => s.Path == move.source))
            {
                string old = source.Key;
                var matching = assets.Where(a => a != null && (source.Kind == "Sprite" ? a is Sprite : a is Texture2D)).ToArray();
                if (matching.Length == 1) source.Name = matching[0].name;
                source.Path = move.destination;
                source.Key = move.destination.Substring(index.Root.Length) + "::" + source.Kind + "/" + source.Name;
                keys[old] = source.Key;
            }
            foreach (var importer in index.Importers.Where(i => i.Path == move.source))
            { importer.Path = move.destination; importer.FileName = Path.GetFileName(move.destination); }
        }
        foreach (var slot in index.Containers.SelectMany(c => c.Slots))
            if (keys.TryGetValue(slot.Key, out var key)) slot.Key = key;
    }

    public static void RenameUiSprites()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(Journal, "plan.json")));
        NamedArtAutomation.Suspended = true;
        NamedArtAutomation.ResetQueue();
        foreach (var move in plan.sprites) Directory.CreateDirectory(Path.GetDirectoryName(move.destination));
        AssetDatabase.Refresh();
        foreach (var move in plan.sprites)
        {
            if (AssetDatabase.AssetPathToGUID(move.destination) == move.guid && !File.Exists(move.source)) continue;
            if (AssetDatabase.AssetPathToGUID(move.source) != move.guid) throw new InvalidOperationException("Source GUID mismatch: " + move.source);
            string error = AssetDatabase.MoveAsset(move.source, move.destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (AssetDatabase.AssetPathToGUID(move.destination) != move.guid) throw new InvalidOperationException("GUID changed: " + move.destination);
        }
        var index = NamedArtIndex.Read(NamedArtReferences.DefaultIndexPath);
        UpdateNamedArt(index, plan);
        index.Write(NamedArtReferences.DefaultIndexPath);
        string stagedPath = Path.Combine(Journal, "namedart-head.json");
        var staged = NamedArtIndex.Read(stagedPath);
        UpdateNamedArt(staged, plan);
        staged.Write(stagedPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Audit();
        File.WriteAllText(Path.Combine(Journal, "renamed.txt"), plan.sprites.Length.ToString());
        Debug.Log("ANIMOL_SUPPLEMENT_RENAMED " + plan.sprites.Length);
    }

    public static void Audit()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(Journal, "plan.json")));
        var imports = new List<ImportCheck>();
        foreach (var move in plan.sprites)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(move.destination);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(move.destination);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(move.destination).OfType<Sprite>().ToArray();
            if (texture == null || sprites.Length == 0 || AssetDatabase.AssetPathToGUID(move.destination) != move.guid)
                throw new InvalidOperationException("Missing texture/Sprite or changed GUID: " + move.destination);
            imports.Add(new ImportCheck { path = move.destination, guid = move.guid, width = texture.width, height = texture.height,
                ppu = importer.spritePixelsPerUnit, spriteMode = (int)importer.spriteImportMode, filter = importer.filterMode.ToString(),
                compression = importer.textureCompression.ToString(), mipmaps = importer.mipmapEnabled, sprites = sprites.Length });
        }
        var entryPoints = new List<string>();
        foreach (var type in TypeCache.GetTypesWithAttribute<InitializeOnLoadAttribute>())
            if (type.FullName.StartsWith("ANIMOL.Editor", StringComparison.Ordinal) &&
                (type.Name.Contains("TerrainEditor") || type.Name.Contains("StageMapAuthoring"))) entryPoints.Add(type.FullName);
        foreach (var method in TypeCache.GetMethodsWithAttribute<InitializeOnLoadMethodAttribute>())
            if (method.DeclaringType.FullName.StartsWith("ANIMOL", StringComparison.Ordinal) &&
                (method.DeclaringType.Name.Contains("TerrainEditor") || method.DeclaringType.Name.Contains("StageMapAuthoring"))) entryPoints.Add(method.ToString());
        var report = new Report { unity = Application.unityVersion, activeTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            moved = plan.sprites.Length, imports = imports.ToArray(), entryPoints = entryPoints.ToArray(),
            clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/ANIMOL/Phase1" }.Where(AssetDatabase.IsValidFolder).ToArray())
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.StartsWith("Assets/ANIMOL/Phase1/", StringComparison.Ordinal)).ToArray() };
        Directory.CreateDirectory("Docs/Validation/TopdownCutover");
        File.WriteAllText("Docs/Validation/TopdownCutover/supplement-import-audit.json", JsonUtility.ToJson(report, true));
        if (entryPoints.Count != 0) throw new InvalidOperationException("Legacy editor startup entry remains");
        Debug.Log("ANIMOL_SUPPLEMENT_AUDIT_COMPLETE");
    }

    public static void BuildAndroidSmoke()
    {
        string output = Path.Combine(Journal, "android/animol-cutover.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        bool installed = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
        if (!installed)
        {
            File.WriteAllText("Docs/Validation/TopdownCutover/mobile-build.json", "{\"attempted\":false,\"reason\":\"Android build target module unavailable\"}");
            return;
        }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output, target = BuildTarget.Android, options = BuildOptions.Development });
        var result = "{\"attempted\":true,\"result\":\"" + report.summary.result + "\",\"errors\":" + report.summary.totalErrors +
            ",\"warnings\":" + report.summary.totalWarnings + ",\"bytes\":" + report.summary.totalSize + ",\"device_display_test\":\"not_run\"}";
        File.WriteAllText("Docs/Validation/TopdownCutover/mobile-build.json", result);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android smoke build: " + report.summary.result);
    }

    public static void ValidateAssets()
    {
        var extensions = new HashSet<string> { ".unity", ".prefab", ".asset", ".anim", ".controller", ".overrideController", ".mat", ".spriteatlas", ".spriteatlasv2" };
        var missing = new List<string>();
        int checkedAssets = 0, references = 0;
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/ANIMOL/", StringComparison.Ordinal) && extensions.Contains(Path.GetExtension(p))))
        {
            string text = File.ReadAllText(path);
            if (!text.StartsWith("%YAML", StringComparison.Ordinal)) continue;
            checkedAssets++;
            foreach (Match match in Regex.Matches(text, @"guid: ([0-9a-f]{32})"))
            {
                string guid = match.Groups[1].Value;
                if (guid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue; // Unity built-in objects.
                references++;
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) missing.Add(path + ": " + guid);
            }
        }
        var legacyNames = new[] { "ANIMOL.Editor.TerrainEditorSceneEditor", "ANIMOL.Editor.TerrainEditorSession", "ANIMOL.Editor.StageMapAuthoringWorkspace", "ANIMOL.Editor.CampaignMapEditorWindow", "ANIMOL.Editor.RunPrototypeBuilder", "ANIMOL.Editor.UiBuildPipeline" };
        var compiled = legacyNames.Where(n => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(n) != null)).ToArray();
#pragma warning disable CS0618
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
#pragma warning restore CS0618
        var report = new ReferenceReport { checkedAssets = checkedAssets, checkedGuidReferences = references,
            missingGuidReferences = missing.Distinct().ToArray(), compiledLegacyTypes = compiled, androidDefines = defines };
        File.WriteAllText("Docs/Validation/TopdownCutover/supplement-reference-audit.json", JsonUtility.ToJson(report, true));
        if (missing.Count > 0 || compiled.Length > 0 || defines.Split(';').Contains("ANIMOL_LEGACY_MAP_EDITOR"))
            throw new InvalidOperationException("Active reference or legacy entry check failed");
        Debug.Log("ANIMOL_SUPPLEMENT_REFERENCES_COMPLETE " + references);
    }

    public static void RemoveRetiredNamedArtOwners()
    {
        NamedArtAutomation.Suspended = true;
        NamedArtAutomation.ResetQueue();
        var retired = new[] { "Assets/ANIMOL/Resources/ANIMOL_FreeShapeArt.asset", "Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV2.asset", "Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV4.asset" };
        foreach (string path in retired)
            if (File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) throw new InvalidOperationException("Retired path exists: " + path);
        string font = "Assets/ANIMOL/Typography/NotoSansSymbols2-Regular_Fallback.asset";
        AssetDatabase.ImportAsset(font, ImportAssetOptions.ForceSynchronousImport);
        var index = NamedArtIndex.Read(NamedArtReferences.DefaultIndexPath);
        index.Containers.RemoveAll(c => retired.Contains(c.Path));
        // Capture the current authored font slots; a stale cache must not populate its intentional empty slots.
        NamedArtReferences.CaptureContainer(index, font);
        index.Write(NamedArtReferences.DefaultIndexPath);
        string stagedPath = Path.Combine(Journal, "namedart-head.json");
        var staged = NamedArtIndex.Read(stagedPath);
        staged.Containers.RemoveAll(c => retired.Contains(c.Path));
        staged.Containers.RemoveAll(c => c.Path == font);
        var current = index.Containers.FirstOrDefault(c => c.Path == font);
        if (current != null) staged.Containers.Add(current);
        staged.Write(stagedPath);
        var preview = NamedArtReferences.Repair(false);
        File.WriteAllText("Docs/Validation/TopdownCutover/namedart-build-preflight.json", JsonUtility.ToJson(preview, true));
        if (preview.Issues.Count > 0 || preview.ChangedSlots != 0) throw new InvalidOperationException("NamedArt preflight still requires repair");
        Debug.Log("ANIMOL_NAMEDART_PREFLIGHT_CLEAN");
    }
}
