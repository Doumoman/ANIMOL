// Captures the actual Scene editor GUI framebuffer, including the palette.
var v=ANIMOL.Editor.TerrainEditorSceneEditor.View;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
var host=typeof(UnityEditor.EditorWindow).GetField("m_Parent",flags).GetValue(v);
var rt=new UnityEngine.RenderTexture((int)v.position.width,(int)v.position.height,0);rt.Create();
host.GetType().GetMethod("GrabPixels",flags).Invoke(host,new object[]{rt,new UnityEngine.Rect(0,0,v.position.width,v.position.height)});
var previous=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;
var tex=new UnityEngine.Texture2D(rt.width,rt.height,UnityEngine.TextureFormat.RGBA32,false);tex.ReadPixels(new UnityEngine.Rect(0,0,rt.width,rt.height),0,0);tex.Apply();UnityEngine.RenderTexture.active=previous;var pixels=tex.GetPixels32();for(int y=0;y<tex.height/2;y++)for(int x=0;x<tex.width;x++){int a=y*tex.width+x,b=(tex.height-1-y)*tex.width+x;var swap=pixels[a];pixels[a]=pixels[b];pixels[b]=swap;}tex.SetPixels32(pixels);tex.Apply();
System.IO.File.WriteAllBytes("Docs/Validation/JointFinishV4/Scene-"+(ANIMOL.Editor.TerrainEditorSceneEditor.FreeCandidateValid?"ghost":"v"+ANIMOL.Editor.TerrainEditorSceneEditor.Adapter.Map.FreeShapeTerrain.artVersion)+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
return new {v.position,host=host.GetType().Name,status=ANIMOL.Editor.TerrainEditorSceneEditor.Status};
