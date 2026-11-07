using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using William.NotebookUI;

namespace ANIMOL.Presentation.Editor
{
    [InitializeOnLoad]
    public static class PuzzlePreview
    {
        [Serializable] class Setup { public SavedScene[] scenes; public string startScene; }
        [Serializable] class SavedScene { public string path; public bool loaded, active; }
        const string Key = "ANIMOL.PuzzlePreview";
        static PuzzlePreview() { EditorApplication.playModeStateChanged += Restore; }
        public static void Play(PuzzleMap map)
        {
            var errors = map.ValidateMap();
            if (errors.Count > 0 || !map.theme) { EditorUtility.DisplayDialog("미리보기를 실행할 수 없음", string.Join("\n", errors.Select(PuzzleMapEditor.TranslateValidation)) + (!map.theme ? "\n테마를 지정하세요." : ""), "확인"); return; }
            // NewScene unloads the selected asset object; keep its authoritative path first.
            string mapPath = AssetDatabase.GetAssetPath(map);
            if (string.IsNullOrEmpty(mapPath)) throw new InvalidOperationException("Save the selected PuzzleMap asset before previewing it.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Reloading must include the map editor's current unsaved MapData and scenario edits.
            AssetDatabase.SaveAssetIfDirty(map);
            if (map.scenario) AssetDatabase.SaveAssetIfDirty(map.scenario);
            // Unsaved scenes cannot be restored across a domain reload.
            if (EditorSceneManager.GetSceneManagerSetup().Any(s => string.IsNullOrEmpty(s.path)))
            { EditorUtility.DisplayDialog("씬 저장 필요", "퍼즐 미리보기를 열기 전에 현재 씬을 저장하세요.", "확인"); return; }
            var setup = new Setup { scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray(), startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) };
            SessionState.SetString(Key, JsonUtility.ToJson(setup));
            EditorSceneManager.playModeStartScene = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var selectedMap = AssetDatabase.LoadAssetAtPath<PuzzleMap>(mapPath);
            if (!selectedMap) throw new InvalidOperationException("The selected PuzzleMap could not be reloaded: " + mapPath);
            CreateRoot(selectedMap); PortraitGameView.Use1080x1920();
            EditorApplication.isPlaying = true;
        }
        public static PuzzleController CreateRoot(PuzzleMap map)
        {
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            var light = new GameObject("Main Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
            var controller = new GameObject("Puzzle").AddComponent<PuzzleController>(); controller.stage = map;
            controller.shortestPathCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayShortestPathCatalog.AssetPath);
            string path = AssetDatabase.GetAssetPath(map);
            if (!path.StartsWith("Assets/ANIMOL/Data/Puzzle/MapData/", StringComparison.Ordinal))
            {
                controller.hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/hud_visual.prefab");
                return controller;
            }
            var canvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab");
            var star = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/CollectibleStarVisual.prefab");
            if (!canvas || !star) throw new InvalidOperationException("Build the Notebook GameplayCanvas and collectible star prefabs before previewing MapData.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(canvas);
            var hud = instance.GetComponentInChildren<NotebookGameplayHud>(true);
            if (!hud) throw new InvalidOperationException("Notebook GameplayCanvas has no gameplay HUD.");
            var bridge = controller.gameObject.AddComponent<NotebookMapDataPreviewBridge>();
            var gateway = controller.gameObject.AddComponent<NotebookMapDataPreviewGateway>();
            bridge.sourceMap = map; bridge.puzzle = controller; bridge.hud = hud;
            bridge.gateway = gateway; bridge.collectibleStarPrefab = star; gateway.bridge = bridge;
            return controller;
        }
        static void Restore(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            var json = SessionState.GetString(Key, ""); if (json.Length == 0) return;
            SessionState.EraseString(Key); var saved = JsonUtility.FromJson<Setup>(json);
            EditorSceneManager.RestoreSceneManagerSetup(saved.scenes.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(saved.startScene);
        }
    }
}
