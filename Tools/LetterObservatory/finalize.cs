using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
public static class ObservatoryFinalize
{
    public static object Main()
    {
        var validation=LetterObservatoryArtBuilder.Validate();MemoryTheaterArtBuilder.Validate();DeviceArtBuilder.Validate();ArtAssetBuilder.Validate();
        EditorSceneManager.OpenScene(LetterObservatoryArtBuilder.ScenePath);PortraitGameView.Use1080x1920();EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(LetterObservatoryArtBuilder.ScenePath);
        var scene=EditorSceneManager.GetActiveScene();int scripts=0,materials=0;
        foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true)){scripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);foreach(var r in t.GetComponents<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)materials++;}
        var qa=Object.FindFirstObjectByType<DeviceArtQaController>();return new {validation,missingScripts=scripts,unsupportedMaterials=materials,scene=scene.path,views=qa.views.Length,devices=qa.masters.Select(v=>v.deviceId).ToArray(),gridComponents=Object.FindObjectsByType<Grid>(FindObjectsSortMode.None).Length,compiling=EditorApplication.isCompiling,playing=EditorApplication.isPlaying};
    }
}
