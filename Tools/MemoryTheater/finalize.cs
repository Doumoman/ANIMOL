using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
public static class TheaterFinalize
{
    public static object Main()
    {
        var validation=MemoryTheaterArtBuilder.Validate();DeviceArtBuilder.Validate();ArtAssetBuilder.Validate();
        EditorSceneManager.OpenScene(MemoryTheaterArtBuilder.ScenePath);PortraitGameView.Use1080x1920();
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MemoryTheaterArtBuilder.ScenePath);
        var scene=EditorSceneManager.GetActiveScene();int scripts=0,materials=0;
        foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))
        {
            scripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach(var r in t.GetComponents<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)materials++;
        }
        var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();
        return new {validation,missingScripts=scripts,unsupportedMaterials=materials,scene=scene.path,views=qa.views.Length,devices=qa.masters.Select(v=>v.deviceId).ToArray(),activeBuildTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),gridComponents=UnityEngine.Object.FindObjectsByType<Grid>(FindObjectsSortMode.None).Length,compiling=EditorApplication.isCompiling,playing=EditorApplication.isPlaying};
    }
}
