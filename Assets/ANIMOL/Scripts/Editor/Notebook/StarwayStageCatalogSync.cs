using System;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace William.NotebookUI.Editor
{
    [InitializeOnLoad]
    public sealed class StarwayStageCatalogSync : AssetPostprocessor, IPreprocessBuildWithReport
    {
        const string MapRoot = "Assets/ANIMOL/Data/Puzzle/MapData/starway/";
        public int callbackOrder => 0;
        static bool queued;

        static StarwayStageCatalogSync()
        {
            QueueSync();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) Sync();
            };
        }

        static void QueueSync()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Sync(); };
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(path =>
                path.StartsWith(MapRoot, StringComparison.Ordinal) && path.EndsWith(".asset", StringComparison.Ordinal))) QueueSync();
        }

        public static void Sync()
        {
            var catalog = StarwayStageCatalog.Current;
            if (!catalog || EditorApplication.isPlaying) return;
            bool changed = false;
            foreach (var entry in catalog.maps)
            {
                if (!entry.map) continue;
                string path = AssetDatabase.GetAssetPath(entry.map);
                string name = Path.GetFileNameWithoutExtension(path);
                string hash;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                if (entry.assetName == name && entry.assetSha256 == hash) continue;
                entry.assetName = name;
                entry.assetSha256 = hash;
                changed = true;
            }
            if (!changed) return;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Sync();
            var catalog = StarwayStageCatalog.Current;
            if (!catalog) throw new BuildFailedException("StarwayStageCatalog is missing.");
            var maps = catalog.maps.Select(entry => entry.map).ToArray();
            for (int i = 1; i <= NotebookCampaignStages.PerTheme; i++)
                if (!NotebookCampaignStages.FindStarwayMap(maps, "SW" + i.ToString("00")))
                    throw new BuildFailedException("Starway stage " + i + " needs exactly one MapData file named SW" +
                        (i - 1).ToString("00") + "_MapData. Check missing/duplicate names.");
        }
    }

    [CustomEditor(typeof(StarwayStageCatalog))]
    sealed class StarwayStageCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Project 창에서 MapData 파일명을 바꾸면 스테이지가 바뀝니다. " +
                "SW00_MapData = 1번, SW19_MapData = 20번. 클리어 기록은 원본 맵을 따라갑니다.", MessageType.Info);
            var catalog = (StarwayStageCatalog)target;
            var maps = catalog.maps.Select(entry => entry.map).ToArray();
            using (new EditorGUI.DisabledScope(true))
                for (int i = 1; i <= NotebookCampaignStages.PerTheme; i++)
                {
                    string id = "SW" + i.ToString("00");
                    EditorGUILayout.ObjectField(id, NotebookCampaignStages.FindStarwayMap(maps, id), typeof(PuzzleMap), false);
                }
            if (Enumerable.Range(1, NotebookCampaignStages.PerTheme).Any(i =>
                !NotebookCampaignStages.FindStarwayMap(maps, "SW" + i.ToString("00"))))
                EditorGUILayout.HelpBox("이름이 중복되거나 빠진 스테이지가 있습니다. 이름 교환을 마친 뒤 확인하세요.", MessageType.Warning);
        }
    }
}
