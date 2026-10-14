using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            if (errors.Count > 0 || !map.theme) { EditorUtility.DisplayDialog("Cannot preview", string.Join("\n", errors) + (!map.theme ? "\nAssign a theme." : ""), "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Unsaved scenes cannot be restored across a domain reload.
            if (EditorSceneManager.GetSceneManagerSetup().Any(s => string.IsNullOrEmpty(s.path)))
            { EditorUtility.DisplayDialog("Save scene first", "Save the current scene before opening the isolated puzzle preview.", "OK"); return; }
            var setup = new Setup { scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray(), startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) };
            SessionState.SetString(Key, JsonUtility.ToJson(setup));
            EditorSceneManager.playModeStartScene = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateRoot(map); PortraitGameView.Use1080x1920();
            EditorApplication.isPlaying = true;
        }
        public static PuzzleController CreateRoot(PuzzleMap map)
        {
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            var light = new GameObject("Main Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
            var controller = new GameObject("Puzzle").AddComponent<PuzzleController>(); controller.stage = map;
            controller.hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/hud_visual.prefab"); return controller;
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
