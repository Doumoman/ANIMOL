UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ANIMOL.Presentation.Editor.DeviceArtBuilder.ScenePath);
ANIMOL.Presentation.Editor.PortraitGameView.Use1080x1920();
ANIMOL.Presentation.Editor.ArtAssetBuilder.Validate();
var validation=ANIMOL.Presentation.Editor.DeviceArtBuilder.Validate();
var scene=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
int missing=0;int badMaterial=0;
foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
    foreach(var r in t.GetComponents<UnityEngine.Renderer>())foreach(var material in r.sharedMaterials)if(!material || !material.shader || !material.shader.isSupported)badMaterial++;
}
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ANIMOL.Presentation.Editor.DeviceArtBuilder.ScenePath);
return new {validation,missingScripts=missing,unsupportedMaterials=badMaterial,scene=scene.path,deviceViews=UnityEngine.Object.FindObjectsByType<ANIMOL.Presentation.DeviceVisualView>(UnityEngine.FindObjectsSortMode.None).Length,playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling};
