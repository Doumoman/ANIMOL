#if UNITY_EDITOR
using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace William.NotebookUI.Editor
{
    public static class NotebookGameplaySceneInstaller
    {
        private const string TheaterScene = "Assets/ANIMOL/Scenes/Gameplay/ANIMOL_Echo_Theater.unity";
        private const string CanvasPrefab = "Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab";
        private const string StarPrefab = "Assets/ANIMOL/Prefabs/UI/Gameplay/CollectibleStarVisual.prefab";

        [MenuItem("ANIMOL/New Notebook UI/Bind Gameplay Overlay To Echo Theater")]
        public static void BindEchoTheaterScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before binding the gameplay scene.");
            var canvas = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefab);
            var star = AssetDatabase.LoadAssetAtPath<GameObject>(StarPrefab);
            if (!canvas || !star) throw new InvalidOperationException("Build Gameplay Overlay Prefab first.");

            Scene scene = SceneManager.GetSceneByPath(TheaterScene);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(TheaterScene, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var campaigns = roots.SelectMany(root => root.GetComponentsInChildren<EchoTheaterCampaignController>(true)).ToArray();
                if (campaigns.Length != 1) throw new InvalidOperationException("Expected one Echo Theater campaign controller.");
                var campaign = campaigns[0];
                var overlays = roots.SelectMany(root => root.GetComponentsInChildren<NotebookGameplayHud>(true)).ToArray();
                if (overlays.Length > 1) throw new InvalidOperationException("More than one gameplay notebook overlay exists.");
                NotebookGameplayHud hud = overlays.Length == 1 ? overlays[0] :
                    ((GameObject)PrefabUtility.InstantiatePrefab(canvas, scene)).GetComponentInChildren<NotebookGameplayHud>(true);
                if (!hud) throw new InvalidOperationException("Gameplay Canvas prefab has no NotebookGameplayHud.");
                var gateway = campaign.GetComponent<NotebookEchoTheaterGateway>();
                if (!gateway) gateway = campaign.gameObject.AddComponent<NotebookEchoTheaterGateway>();
                gateway.campaign = campaign;
                campaign.notebookHud = hud;
                campaign.notebookGateway = gateway;
                campaign.collectibleStarPrefab = star;
                if (campaign.puzzle) { campaign.puzzle.hudPrefab = null; EditorUtility.SetDirty(campaign.puzzle); }
                EditorUtility.SetDirty(hud); EditorUtility.SetDirty(gateway); EditorUtility.SetDirty(campaign);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Echo Theater scene.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif
