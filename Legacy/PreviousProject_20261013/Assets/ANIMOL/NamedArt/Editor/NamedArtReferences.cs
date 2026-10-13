using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ANIMOL.NamedArt.Editor
{
    [Serializable] public sealed class ArtRepairReport
    {
        public int Sources, Containers, Slots, ChangedSlots;
        public List<string> ChangedAssets = new List<string>();
        public List<string> Issues = new List<string>();
    }

    public static class NamedArtReferences
    {
        public const string DefaultIndexPath = "ProjectSettings/ANIMOLNamedArtReferences.json";
        // Test fixtures use an isolated root/index and never replace production art.
        public static string IndexPath = DefaultIndexPath;
        public static bool Busy { get; private set; }
        public static ArtRepairReport LastReport { get; private set; }
        public static readonly string[] ContainerExtensions = { ".unity", ".prefab", ".asset", ".anim", ".mat", ".controller", ".overrideController" };
        public static bool IsContainer(string path) => ContainerExtensions.Contains(Path.GetExtension(path));

        private static IEnumerable<KeyValuePair<string, Object>> HierarchyObjects(GameObject root, string prefix)
        {
            var components = root.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++) if (components[i] != null)
                yield return new KeyValuePair<string, Object>(prefix + "|" + i + "|" + components[i].GetType().FullName, components[i]);
            for (int i = 0; i < root.transform.childCount; i++)
                foreach (var pair in HierarchyObjects(root.transform.GetChild(i).gameObject, prefix + "/" + i)) yield return pair;
        }
        private static Dictionary<string, Object> Objects(string path, GameObject prefab, Scene scene)
        {
            var result = new Dictionary<string, Object>();
            if (prefab != null) return HierarchyObjects(prefab, "0").ToDictionary(p => p.Key, p => p.Value);
            if (scene.IsValid())
            {
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++) foreach (var pair in HierarchyObjects(roots[i], i.ToString())) result[pair.Key] = pair.Value;
                return result;
            }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o == null || o is Texture2D) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long id);
                result[id + "|" + o.GetType().FullName] = o;
            }
            return result;
        }
        // Use Unity APIs; never rewrite scene/prefab YAML. Existing dirty views are not saved or discarded.
        private static bool Visit(string path, bool repair, Action<Dictionary<string, Object>, Action> action, List<string> issues)
        {
            if (!File.Exists(path)) { issues.Add("Missing owner: " + path); return false; }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (repair && stage != null && stage.assetPath == path) { issues.Add("Open prefab deferred: " + path); return false; }
            if (path.EndsWith(".unity", StringComparison.Ordinal))
            {
                var scene = SceneManager.GetSceneByPath(path); bool temporary = !scene.IsValid() || !scene.isLoaded;
                if (!temporary && scene.isDirty && repair) { issues.Add("Unsaved scene deferred: " + path); return false; }
                if (temporary && Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty && string.IsNullOrEmpty(s.path)))
                { issues.Add("Untitled scene must be saved first: " + path); return false; }
                if (temporary) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { action(Objects(path, null, scene), () => { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }); }
                finally { if (temporary) EditorSceneManager.CloseScene(scene, true); }
            }
            else if (path.EndsWith(".prefab", StringComparison.Ordinal))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { action(Objects(path, root, default), () => PrefabUtility.SaveAsPrefabAsset(root, path)); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            else
            {
                var objects = Objects(path, null, default);
                if (repair && objects.Values.Any(EditorUtility.IsDirty)) { issues.Add("Unsaved asset deferred: " + path); return false; }
                action(objects, () => { foreach (var o in objects.Values) if (EditorUtility.IsDirty(o)) AssetDatabase.SaveAssetIfDirty(o); });
            }
            return true;
        }
        public static void CaptureContainer(NamedArtIndex index, string path, bool explicitSave = false)
        {
            if (!index.InScope(path) || !IsContainer(path)) return;
            var existing = index.Containers.FirstOrDefault(c => c.Path == path);
            var captured = new ArtContainer { Path = path, Guid = AssetDatabase.AssetPathToGUID(path) };
            var issues = new List<string>();
            Visit(path, false, (objects, save) =>
            {
                foreach (var pair in objects)
                {
                    using (var so = new SerializedObject(pair.Value))
                    {
                        var p = so.GetIterator();
                        while (p.Next(true))
                        {
                            if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                            string key = index.Register(p.objectReferenceValue);
                            if (key != null) captured.Slots.Add(new ArtSlot { Owner = pair.Key, Property = p.propertyPath, Key = key });
                            else if (p.objectReferenceValue == null && existing != null)
                            {
                                // Retain missing references after deletion. An explicitly saved null is a user's removal.
                                var old = existing.Slots.FirstOrDefault(s => s.Owner == pair.Key && s.Property == p.propertyPath);
                                var source = old == null ? null : index.Sources.First(s => s.Key == old.Key);
                                if (old != null && (!explicitSave || p.objectReferenceInstanceIDValue != 0 ||
                                    !File.Exists(source.Path) || source.Guid != AssetDatabase.AssetPathToGUID(source.Path))) captured.Slots.Add(old);
                            }
                        }
                    }
                }
            }, issues);
            if (issues.Count != 0) return;
            index.Containers.RemoveAll(c => c.Path == path);
            if (captured.Slots.Count != 0) index.Containers.Add(captured);
        }
        public static NamedArtIndex CaptureAll(string root = "Assets/ANIMOL/")
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before indexing.");
            var index = NamedArtIndex.Read(IndexPath); index.Root = root;
            var paths = AssetDatabase.GetAllAssetPaths().Where(index.InScope).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            Busy = true;
            try
            {
                foreach (string path in paths.Where(p => AssetImporter.GetAtPath(p) is TextureImporter))
                {
                    // Do not overwrite the original settings before a replacement has been repaired.
                    var old = index.Importers.FirstOrDefault(i => i.Path == path);
                    if (old == null || old.Guid == AssetDatabase.AssetPathToGUID(path)) index.CaptureImporter(path);
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) index.Register(o);
                }
                foreach (string path in paths.Where(IsContainer)) CaptureContainer(index, path);
                index.Sources = index.Sources.OrderBy(s => s.Key, StringComparer.Ordinal).ToList();
                index.Importers = index.Importers.OrderBy(i => i.Path, StringComparer.Ordinal).ToList();
                index.Containers = index.Containers.OrderBy(c => c.Path, StringComparer.Ordinal).ToList();
                index.Write(IndexPath);
                return index;
            }
            finally { Busy = false; }
        }
        public static ArtRepairReport Repair(bool persist = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before replacing authored art.");
            var index = NamedArtIndex.Read(IndexPath);
            var paths = AssetDatabase.GetAllAssetPaths().Where(index.InScope).ToArray();
            var report = new ArtRepairReport { Sources = index.Sources.Count, Containers = index.Containers.Count, Slots = index.Containers.Sum(c => c.Slots.Count) };
            var resolved = new Dictionary<string, Object>();
            Busy = true;
            try
            {
                foreach (var source in index.Sources)
                {
                    var value = index.Resolve(source, paths, out string reason);
                    if (value != null) resolved[source.Key] = value;
                    else if (index.Containers.Any(c => File.Exists(c.Path) && c.Slots.Any(s => s.Key == source.Key))) report.Issues.Add(reason + ": " + source.Key);
                }
                foreach (var container in index.Containers)
                {
                    string path = File.Exists(container.Path) ? container.Path : AssetDatabase.GUIDToAssetPath(container.Guid);
                    if (string.IsNullOrEmpty(path)) { report.Issues.Add("Missing owner: " + container.Path); continue; }
                    Visit(path, true, (objects, save) =>
                    {
                        int changes = 0;
                        foreach (var group in container.Slots.GroupBy(s => s.Owner))
                        {
                            if (!objects.TryGetValue(group.Key, out var owner)) { report.Issues.Add("Owner changed; save/reindex: " + path + "/" + group.Key); continue; }
                            using (var so = new SerializedObject(owner))
                            {
                                foreach (var slot in group)
                                {
                                    var p = so.FindProperty(slot.Property);
                                    if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) { report.Issues.Add("Property changed: " + path + "/" + slot.Property); continue; }
                                    if (!resolved.TryGetValue(slot.Key, out var replacement)) continue;
                                    if (p.objectReferenceValue == replacement) continue;
                                    // A user-assigned, valid different sprite takes precedence over the previous name.
                                    if (p.objectReferenceValue != null)
                                    {
                                        string key = index.Register(p.objectReferenceValue);
                                        if (key != null) slot.Key = key;
                                        continue;
                                    }
                                    if (persist) p.objectReferenceValue = replacement;
                                    changes++;
                                }
                                if (persist && so.hasModifiedProperties) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(owner); }
                            }
                        }
                        if (changes > 0)
                        {
                            report.ChangedSlots += changes; report.ChangedAssets.Add(path);
                            if (persist) save();
                        }
                    }, report.Issues);
                }
                if (persist)
                {
                    if (report.Issues.Count == 0)
                    {
                        foreach (var s in index.Sources)
                            if (resolved.TryGetValue(s.Key, out var o)) { s.Path = AssetDatabase.GetAssetPath(o); s.Guid = AssetDatabase.AssetPathToGUID(s.Path); }
                        foreach (var path in index.Sources.Select(s => s.Path).Distinct().ToArray()) index.CaptureImporter(path);
                    }
                    index.Write(IndexPath);
                }
                LastReport = report;
                return report;
            }
            finally { Busy = false; }
        }
        public static void IndexProject()
        {
            var i = CaptureAll(); Debug.Log($"[NamedArt] Indexed {i.Sources.Count} named sources and {i.Containers.Sum(c => c.Slots.Count)} reference slots.");
        }
        public static void RepairProject()
        {
            var r = Repair(); Debug.Log($"[NamedArt] Rebound {r.ChangedSlots} slots; {r.Issues.Count} issues. See Library/ANIMOLNamedArtLastReport.json.");
            File.WriteAllText("Library/ANIMOLNamedArtLastReport.json", Newtonsoft.Json.JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
