// Render all 47 masks in each of the four phases using actual Unity SpriteRenderers.
var art=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,4);
var style=art.Style("T01_A");var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var previous=UnityEngine.RenderTexture.active;var results=new System.Collections.Generic.List<object>();
var cameraObject=new UnityEngine.GameObject("V4 atlas comparison camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=3;camera.aspect=8f/6;camera.transform.position=new UnityEngine.Vector3(4,3,-10);camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=UnityEngine.Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
try{
 for(int v=0;v<4;v++){
  var root=new UnityEngine.GameObject("47 actual mask Sprites");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  var rt=new UnityEngine.RenderTexture(256,192,0,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);rt.Create();
  var rendered=new UnityEngine.Texture2D(256,192,UnityEngine.TextureFormat.RGBA32,false);var source=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);
  try{
   for(int i=0;i<47;i++){var go=new UnityEngine.GameObject("mask"+art.canonicalMasks[i]);go.transform.SetParent(root.transform,false);go.transform.localPosition=new UnityEngine.Vector3(i%8+.5f,5-i/8+.5f,0);var renderer=go.AddComponent<UnityEngine.SpriteRenderer>();renderer.sprite=style.cells[v*47+i];renderer.sharedMaterial=art.material;}
   camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;rendered.ReadPixels(new UnityEngine.Rect(0,0,256,192),0,0);rendered.Apply();
   source.LoadImage(System.IO.File.ReadAllBytes(ANIMOL.Editor.FreeShapeArtV4Builder.Source+"/Art/Atlases/T01_A_v"+v+".png"));
   var a=rendered.GetPixels32();var b=source.GetPixels32();int differences=0;
   for(int i=0;i<a.Length;i++)if(a[i].a!=b[i].a||a[i].a!=0&&(a[i].r!=b[i].r||a[i].g!=b[i].g||a[i].b!=b[i].b))differences++;
   results.Add(new{phase=v,masks=47,pixels=a.Length,differences=differences});
   System.IO.File.WriteAllBytes("Docs/Validation/JointFinishV4/T01_A-AllMasks-v"+v+"-Unity.png",rendered.EncodeToPNG());
   if(differences!=0)throw new System.Exception("Atlas render mismatch: phase "+v+" / "+differences);
  }finally{camera.targetTexture=null;UnityEngine.RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(rendered);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}finally{UnityEngine.Object.DestroyImmediate(cameraObject);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/AllT01AMasks.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
return results;
