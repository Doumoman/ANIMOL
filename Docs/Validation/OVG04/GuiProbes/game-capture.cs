using UnityEngine;
using UnityEditor;
using System.Linq;
using ANIMOL.Development;
public static class Ovg04GuiGameCapture
{
public static object Main()
{
var screen=TerrainEditorScreen.Instance;
if(screen.TestSession.SelectedBubbleSlots.Count()!=3)throw new System.Exception("Expected three active selected slots after Start.");
var window=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));window.Focus();
var w=(int)window.position.width;var h=(int)window.position.height;var target=new RenderTexture(w,h,24);target.Create();
if(!UnityEditorInternal.InternalEditorUtility.CaptureEditorWindow(window,target))throw new System.Exception("Game View focus missing");
var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(w,h,TextureFormat.RGB24,false);
texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();
var path="C:/Users/user/Documents/GitHub/ANIMOL/Docs/Validation/OVG04/gui-mobile-full-v6.png";
System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);
return new {selectedBubbles=3,collected=screen.TestSession.CollectedCount,screenWidth=Screen.width,screenHeight=Screen.height,path};
}
}
