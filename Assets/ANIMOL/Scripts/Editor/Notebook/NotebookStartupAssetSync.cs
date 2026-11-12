using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace William.NotebookUI.Editor
{
    public sealed class NotebookStartupAssetSync : IPreprocessBuildWithReport
    {
        public const string StoryPrefab = "Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab";
        public int callbackOrder => 5;

        // Keep authored puzzles and source IDs intact. Only these generated
        // containers and string-only indexes change when maps are renamed.
        public static void Sync()
        {
            if (EditorApplication.isPlaying) return;
            var catalog = StarwayStageCatalog.Current;
            if (!catalog) return;
            var starway = Get<NotebookDeferredAssets>(NotebookDeferredAssets.StarwayResource);
            SaveIfChanged(starway, () => starway.maps = catalog.maps.Select(e => e.map).ToArray());
            var theater = Get<NotebookDeferredAssets>(NotebookDeferredAssets.TheaterResource);
            SaveIfChanged(theater, () => theater.theaterMaps = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(NotebookAndroidCampaignBuild.TheaterMaps));
            var story = Get<NotebookDeferredAssets>(NotebookDeferredAssets.StoryResource);
            SaveIfChanged(story, () => story.storyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryPrefab));
            var index = Get<NotebookStageIndex>("NotebookStageIndex");
            SaveIfChanged(index, () => index.stages = catalog.maps.Select(e => new NotebookStageIndex.Entry {
                stageId = NotebookCampaignStages.TryStarwayStageId(e.map, out string id) ? id : null,
                sourceId = e.sourceId, assetName = e.assetName
            }).ToArray());
            var preview = AssetDatabase.LoadAssetAtPath<NotebookCampaignPreview>(NotebookCampaignPreview.AssetPath);
            if (preview) SaveIfChanged(preview, () => preview.theaterStageIds = theater.theaterMaps
                ? theater.theaterMaps.maps.Take(NotebookCampaignStages.PerTheme).Select(e => e?.map ? e.stageId : null).ToArray()
                : Array.Empty<string>());
        }

        static T Get<T>(string name) where T : ScriptableObject
        {
            string path = "Assets/ANIMOL/Data/Puzzle/Resources/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void SaveIfChanged(UnityEngine.Object asset, Action update)
        {
            string before = EditorJsonUtility.ToJson(asset);
            update();
            if (EditorJsonUtility.ToJson(asset) == before) return;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        public void OnPreprocessBuild(BuildReport report) => Sync();
    }
}
