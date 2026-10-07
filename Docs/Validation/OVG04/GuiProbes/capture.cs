using UnityEngine;
using UnityEditor;
using ANIMOL.Editor;
public static class Ovg04GuiCapture
{
public static object Main()
{
var view=TerrainEditorSceneEditor.View;var rect=view.position;
var w=(int)rect.width;var h=(int)rect.height;
var target=new RenderTexture(w,h,24);target.Create();
if(!UnityEditorInternal.InternalEditorUtility.CaptureEditorWindow(view,target))throw new System.Exception("Scene View must own focus.");
var previous=RenderTexture.active;RenderTexture.active=target;
var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();
var path="C:/Users/user/Documents/GitHub/ANIMOL/Docs/Validation/OVG04/gui-scene-v6.png";
System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);
System.IO.File.WriteAllText("C:/Users/user/Documents/GitHub/ANIMOL/Docs/Validation/OVG04/gui-authored-map-v6.json",EditorJsonUtility.ToJson(TerrainEditorSceneEditor.Adapter.Map,true));
return new {path,width=w,height=h,window=view.GetInstanceID(),focused=EditorWindow.focusedWindow==view};
}
}
