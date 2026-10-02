using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.NamedArt.Editor
{
    [InitializeOnLoad]
    public static class NamedArtAutomation
    {
        private static bool scheduled, artChanged;
        private static double notBefore;
        private static readonly HashSet<string> saved = new HashSet<string>();
        private static readonly HashSet<string> textures = new HashSet<string>();
        public static bool Suspended;
        public static bool Pending => scheduled;
        static NamedArtAutomation()
        {
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) Schedule(); };
            EditorSceneManager.sceneSaved += scene => QueueSaved(scene.path);
            PrefabStage.prefabStageClosing += stage =>
            {
                if (Suspended || NamedArtReferences.Busy || !File.Exists(NamedArtReferences.IndexPath)) return;
                if (NamedArtIndex.Read(NamedArtReferences.IndexPath).InScope(stage.assetPath)) { artChanged = true; Schedule(); }
            };
        }
        public static void QueueSaved(string path)
        {
            if (NamedArtReferences.Busy || Suspended || !File.Exists(NamedArtReferences.IndexPath)) return;
            var index = NamedArtIndex.Read(NamedArtReferences.IndexPath);
            if (index.InScope(path) && NamedArtReferences.IsContainer(path)) { saved.Add(path); Schedule(); }
        }
        public static void QueueImports(string[] imported, string[] deleted, string[] moved)
        {
            if (NamedArtReferences.Busy || Suspended || !File.Exists(NamedArtReferences.IndexPath)) return;
            var index = NamedArtIndex.Read(NamedArtReferences.IndexPath);
            foreach (string path in imported.Concat(deleted).Concat(moved).Where(index.InScope))
            {
                if (index.Importers.Any(i => i.Path == path) || AssetImporter.GetAtPath(path) is TextureImporter) artChanged = true;
                if (File.Exists(path) && AssetImporter.GetAtPath(path) is TextureImporter) textures.Add(path);
                if (File.Exists(path) && NamedArtReferences.IsContainer(path)) saved.Add(path);
            }
            if (artChanged || saved.Count > 0) Schedule();
        }
        private static void Schedule()
        {
            if (scheduled || Suspended) return;
            scheduled = true; notBefore = EditorApplication.timeSinceStartup + .1; EditorApplication.update += Run;
        }
        private static void Run()
        {
            if (EditorApplication.timeSinceStartup < notBefore) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            scheduled = false; EditorApplication.update -= Run;
            if (Suspended || NamedArtReferences.Busy || !File.Exists(NamedArtReferences.IndexPath)) return;
            try
            {
                if (artChanged) { artChanged = false; NamedArtReferences.RepairProject(); }
                var index = NamedArtIndex.Read(NamedArtReferences.IndexPath);
                foreach (string path in textures)
                {
                    index.CaptureImporter(path);
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) index.Register(o);
                }
                textures.Clear();
                bool hadSaved = saved.Count > 0;
                foreach (string path in saved.ToArray()) NamedArtReferences.CaptureContainer(index, path, true);
                saved.Clear(); index.Write(NamedArtReferences.IndexPath);
                if (hadSaved && index.Sources.Any(s => File.Exists(s.Path) && s.Guid != AssetDatabase.AssetPathToGUID(s.Path)))
                    NamedArtReferences.RepairProject();
            }
            catch (Exception e) { Debug.LogError("[NamedArt] " + e); }
        }
        public static void ResetQueue() { saved.Clear(); textures.Clear(); artChanged = false; scheduled = false; EditorApplication.update -= Run; }
    }
    public sealed class NamedArtImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!File.Exists(NamedArtReferences.IndexPath)) return;
            var index = NamedArtIndex.Read(NamedArtReferences.IndexPath);
            if (index.InScope(assetPath)) index.RestoreImporter((TextureImporter)assetImporter);
        }
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            => NamedArtAutomation.QueueImports(imported, deleted.Concat(movedFrom).ToArray(), moved);
    }
    public sealed class NamedArtSaveProcessor : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths) NamedArtAutomation.QueueSaved(path);
            return paths;
        }
    }
    public sealed class NamedArtBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!File.Exists(NamedArtReferences.IndexPath)) throw new BuildFailedException("Index named art before building: ANIMOL/Art References/Index current named sprites");
            var repaired = NamedArtReferences.Repair();
            if (repaired.Issues.Count > 0) throw new BuildFailedException("Named art references need attention:\n" + string.Join("\n", repaired.Issues.Take(20)));
        }
    }
}
