using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MapHudCrayonGpu
{
    static void Need(bool yes,string message) { if(!yes)throw new InvalidOperationException(message); }
    static string Render(Material material, RenderTexture target, float time)
    {
        Shader.SetGlobalFloat("_ANIMOLCrayonUnscaledTime",time);
        RenderTexture.active=target;GL.Clear(true,true,Color.clear);
        Graphics.Blit(material.GetTexture("_MainTex"),target,material);
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        try
        {
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            var pixels=image.GetRawTextureData<byte>().ToArray();
            Need(pixels.Any(value=>value>0),"GPU output was empty");
            using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(pixels));
        }
        finally {Object.DestroyImmediate(image);}
    }
    public static object Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode first");
        const string path="Assets/ANIMOL/MapHudCrayonUI/Generated/Materials/Gameplay_CostMedallion_Animated.mat";
        var material=new Material(AssetDatabase.LoadAssetAtPath<Material>(path));
        var target=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;float clock=Shader.GetGlobalFloat("_ANIMOLCrayonUnscaledTime");
        try
        {
            string a=Render(material,target,0),aHold=Render(material,target,.3f),b=Render(material,target,.61f),c=Render(material,target,1.21f),loop=Render(material,target,1.81f);
            Need(a==aHold&&a==loop&&a!=b&&b!=c&&c!=a,"A/B/C did not hold and loop at the expected discrete times");
            material.SetFloat("_Motion",0);Need(Render(material,target,1.21f)==a,"Reduced motion did not freeze frame A");
            return new{device=SystemInfo.graphicsDeviceType.ToString(),times=new[]{0,.3f,.61f,1.21f,1.81f},hashes=new[]{a,aHold,b,c,loop},discrete=true,stillA=true};
        }
        finally
        {
            Shader.SetGlobalFloat("_ANIMOLCrayonUnscaledTime",clock);RenderTexture.active=previous;
            Object.DestroyImmediate(target);Object.DestroyImmediate(material);
        }
    }
}
