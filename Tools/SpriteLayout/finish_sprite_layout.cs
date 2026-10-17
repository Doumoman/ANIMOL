var scenePath="Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity";
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Wait for tests to leave Play Mode.");
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
ANIMOL.Presentation.Editor.ArtAssetBuilder.Validate();
int missingScripts=0,missingSprites=0;
foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
{
    foreach(var transform in root.GetComponentsInChildren<UnityEngine.Transform>(true))missingScripts+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
    foreach(var renderer in root.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))if(!renderer.sprite)missingSprites++;
}
var camera=UnityEngine.Object.FindFirstObjectByType<UnityEngine.Camera>();
var canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None);
foreach(var canvas in canvases){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
var target=new UnityEngine.RenderTexture(1080,1920,24);camera.targetTexture=target;camera.Render();UnityEngine.RenderTexture.active=target;
var image=new UnityEngine.Texture2D(1080,1920,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,1080,1920),0,0);image.Apply();
System.IO.Directory.CreateDirectory("Screenshots/ArtQA");System.IO.File.WriteAllBytes("Screenshots/ArtQA/qa_clean_project.png",UnityEngine.ImageConversion.EncodeToPNG(image));
camera.targetTexture=null;UnityEngine.RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
ANIMOL.Presentation.Editor.PortraitGameView.Use1080x1920();
return new {missingScripts,missingSprites,scene=scenePath,startScene=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene.name,dependencies=UnityEditor.AssetDatabase.GetDependencies(scenePath,true).Where(p=>p.StartsWith("Assets/")).ToArray()};
