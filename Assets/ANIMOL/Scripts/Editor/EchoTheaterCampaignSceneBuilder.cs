using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Puzzle.Editor
{
    public static class EchoTheaterCampaignSceneBuilder
    {
        public const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Echo_Theater.unity";

        [MenuItem("ANIMOL/Echo Theater V3/3 Build Campaign Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the active scene and exit Play mode before building the campaign scene.");
            var set = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(EchoTheaterV3Importer.SetPath);
            if (!set || set.maps.Count != 35) throw new InvalidOperationException("Import the 35 maps first.");
            var previous = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var host = new GameObject("Echo Theater Campaign");
                var campaign = host.AddComponent<EchoTheaterCampaignController>();
                campaign.mapSet = set;
                campaign.puzzle = host.AddComponent<PuzzlePrototypeController>();
                EditorSceneManager.SaveScene(scene, ScenePath);
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
            }
        }
    }
}
