using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AnimolTopdownCutoverBatch
{
    [Serializable] private sealed class Validation { public string activeScene; public string[] scenes; public string[] missing; public string[] legacyMenus; public string[] p01; }
    [Serializable] private sealed class SpriteMove { public string source; public string destination; }
    [Serializable] private sealed class Plan { public string[] archive; public SpriteMove[] sprites; }
    private static string Journal => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp/animol-cutover-journal");
    private static void TransformNamedArt(ANIMOL.NamedArt.Editor.NamedArtIndex index, Plan plan)
    {
        var archived = new HashSet<string>(plan.archive);
        var removedKeys = new HashSet<string>(index.Sources.Where(s => archived.Contains(s.Path)).Select(s => s.Key));
        index.Sources.RemoveAll(s => archived.Contains(s.Path));
        index.Importers.RemoveAll(s => archived.Contains(s.Path));
        index.Containers.RemoveAll(s => archived.Contains(s.Path));
        var keys = new Dictionary<string, string>();
        foreach (var move in plan.sprites)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(move.destination);
            foreach (var source in index.Sources.Where(s => s.Path == move.source))
            {
                string oldKey = source.Key;
                var matches = assets.Where(a => a != null && (source.Kind == "Sprite" ? a is Sprite : a is Texture2D)).ToArray();
                if (matches.Length == 1) source.Name = matches[0].name;
                source.Path = move.destination;
                source.Key = source.Path.Substring(index.Root.Length) + "::" + source.Kind + "/" + source.Name;
                keys[oldKey] = source.Key;
            }
            foreach (var importer in index.Importers.Where(i => i.Path == move.source))
            { importer.Path = move.destination; importer.FileName = Path.GetFileName(move.destination); }
        }
        foreach (var container in index.Containers)
        {
            container.Slots.RemoveAll(s => removedKeys.Contains(s.Key));
            foreach (var slot in container.Slots) if (keys.TryGetValue(slot.Key, out var key)) slot.Key = key;
        }
    }
    public static void ReindexNamedArt()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(Journal, "plan.json")));
        ANIMOL.NamedArt.Editor.NamedArtAutomation.Suspended = true;
        ANIMOL.NamedArt.Editor.NamedArtAutomation.ResetQueue();
        string active = "ProjectSettings/ANIMOLNamedArtReferences.json";
        var index = ANIMOL.NamedArt.Editor.NamedArtIndex.Read(active);
        TransformNamedArt(index, plan);
        index.Write(active);
        string staged = Path.Combine(Journal, "namedart-head-transformed.json");
        var head = ANIMOL.NamedArt.Editor.NamedArtIndex.Read(staged);
        TransformNamedArt(head, plan);
        head.Write(staged);
        var forbidden = new HashSet<string>(plan.archive.Concat(plan.sprites.Select(s => s.source)));
        int stale = index.Sources.Count(s => forbidden.Contains(s.Path)) + index.Importers.Count(s => forbidden.Contains(s.Path)) + index.Containers.Count(s => forbidden.Contains(s.Path));
        File.WriteAllText(Path.Combine(Journal, "namedart-validation.json"), "{\"stale_migrated_paths\":" + stale + "}");
        if (stale != 0) throw new InvalidOperationException("Stale NamedArt paths remain.");
        Debug.Log("ANIMOL_NAMEDART_CUTOVER_COMPLETE");
    }
    public static void Execute()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(Journal, "plan.json")));
        string root = Directory.GetParent(Application.dataPath).FullName;
        string staging = "Assets/__AnimolCutoverStaging";
        var moved = File.Exists(Path.Combine(Journal, "moved.txt")) ? File.ReadAllLines(Path.Combine(Journal, "moved.txt")).ToList() : new List<string>();
        foreach (var source in plan.archive) Directory.CreateDirectory(Path.GetDirectoryName(staging + "/" + source));
        foreach (var move in plan.sprites) Directory.CreateDirectory(Path.GetDirectoryName(move.destination));
        AssetDatabase.Refresh();
        foreach (var source in plan.archive)
        {
            if (moved.Contains(source)) continue;
            string temporary = staging + "/" + source;
            if (!File.Exists(temporary))
            {
                string guid = AssetDatabase.AssetPathToGUID(source);
                if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("No imported GUID: " + source);
                string error = AssetDatabase.MoveAsset(source, temporary);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(source + ": " + error);
                if (AssetDatabase.AssetPathToGUID(temporary) != guid) throw new InvalidOperationException("GUID changed: " + source);
            }
            string destination = Path.Combine(root, "Legacy/Platformer_20261013", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Move(temporary, destination);
            File.Move(temporary + ".meta", destination + ".meta");
            string original = Path.Combine(Journal, "originals", source);
            if (File.Exists(original)) File.Copy(original, destination, true);
            moved.Add(source);
            File.WriteAllLines(Path.Combine(Journal, "moved.txt"), moved);
        }
        AssetDatabase.DeleteAsset(staging);
        foreach (var move in plan.sprites)
        {
            if (!File.Exists(move.source) && File.Exists(move.destination)) continue;
            string guid = AssetDatabase.AssetPathToGUID(move.source);
            Directory.CreateDirectory(Path.GetDirectoryName(move.destination));
            string error = AssetDatabase.MoveAsset(move.source, move.destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (AssetDatabase.AssetPathToGUID(move.destination) != guid) throw new InvalidOperationException("Sprite GUID changed: " + move.source);
        }
        var archived = new HashSet<string>(plan.archive);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => File.Exists(s.path) && !s.path.StartsWith(staging + "/", StringComparison.Ordinal) && !archived.Contains(s.path)
            && !s.path.Contains("MapAuthoringWorkspace") && !s.path.Contains("ThemeRuntime_")).ToArray();
        EditorSceneManager.playModeStartScene = null;
        SessionState.SetBool("ANIMOL.SceneTerrainV4.active", false);
        SessionState.SetBool("ANIMOL.TerrainEditorV4.active", false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(Path.Combine(Journal, "executed.txt"), "AssetDatabase moves completed; " + moved.Count);
        Debug.Log("ANIMOL_CUTOVER_MOVES_COMPLETE " + moved.Count);
    }

    public static void Validate()
    {
        var missing = new List<string>();
        var checkedScenes = new List<string>();
        var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        var p01 = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/ANIMOL" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileName(p).IndexOf("P01", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        paths.AddRange(p01);
        foreach (string path in paths.Distinct())
        {
            var scene = EditorSceneManager.OpenScene(path);
            checkedScenes.Add(path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        if (component == null) { missing.Add(path + ": missing component on " + transform.name); continue; }
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                missing.Add(path + ": " + transform.name + "." + property.propertyPath);
                    }
        }
        // Leave the application entry scene active, rather than any archived workspace.
        EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Bootstrap.unity");
        var legacyMenus = new List<string>();
        foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            foreach (MenuItem menu in method.GetCustomAttributes(typeof(MenuItem), false))
                if (menu.menuItem.StartsWith("ANIMOL/", StringComparison.Ordinal) &&
                    (menu.menuItem.Contains("Campaign Maps") || menu.menuItem.Contains("Map Editor") || menu.menuItem.Contains("Run Prototype")))
                    legacyMenus.Add(menu.menuItem);
        var report = new Validation { activeScene = "Assets/ANIMOL/Scenes/Bootstrap.unity", scenes = checkedScenes.ToArray(),
            missing = missing.ToArray(), legacyMenus = legacyMenus.ToArray(), p01 = p01 };
        File.WriteAllText(Path.Combine(Journal, "unity-validation.json"), JsonUtility.ToJson(report, true));
        Debug.Log("ANIMOL_CUTOVER_VALIDATION " + JsonUtility.ToJson(report));
        if (missing.Count > 0 || legacyMenus.Count > 0) throw new InvalidOperationException("Cutover validation failed; see journal.");
    }
}
