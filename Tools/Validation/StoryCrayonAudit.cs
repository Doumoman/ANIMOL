using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using William.NotebookUI;
using Animol.Starway;
using Object=UnityEngine.Object;

public static class StoryCrayonAudit
{
    const string Root="Assets/ANIMOL/Art/UI/Story/Crayon/";
    static void Need(bool yes,string message){if(!yes)throw new InvalidOperationException(message);}
    public static object Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode first");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"Shaders/UI_ANIMOL_StoryCrayonBoil.shader");
        Need(shader&&shader.isSupported&&ShaderUtil.GetShaderMessages(shader).Length==0,"Story shader diagnostics");
        int sprites=0,materials=0;
        foreach(string group in new[]{"PanelFrame","Utility_Normal","Utility_Highlighted","Utility_Pressed","Utility_Disabled","Wallet_Normal","Wallet_Highlighted","Wallet_Pressed","Wallet_Disabled"})
        {
            var frames=new[]{"A","B","C"}.Select(letter=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Sprites/"+group+"_"+letter+".png")).ToArray();
            foreach(var sprite in frames)
            {
                Need(sprite,"Missing PNG");var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                Need(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&settings.spriteMeshType==SpriteMeshType.FullRect
                    &&importer.spritePixelsPerUnit==100&&importer.filterMode==FilterMode.Bilinear&&importer.wrapMode==TextureWrapMode.Clamp
                    &&importer.alphaIsTransparency&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Wrong PNG importer");
                var size=group=="PanelFrame"?new Vector2(984,240):group.StartsWith("Utility")?new Vector2(132,132):new Vector2(300,112);
                Need(sprite.rect.size==size&&sprite.border==Vector4.one*(group=="PanelFrame"?36:32),"Wrong dimensions/border");sprites++;
            }
            foreach(bool still in new[]{false,true})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/StoryCrayon_"+group+(still?"_Still.mat":"_Animated.mat"));
                Need(mat&&mat.shader==shader&&mat.GetTexture("_MainTex")==frames[0].texture&&mat.GetTexture("_FrameB")==frames[1].texture&&mat.GetTexture("_FrameC")==frames[2].texture,"Wrong material frame references");
                Need(mat.GetFloat("_Motion")== (still?0:1)&&Mathf.Abs(mat.GetFloat("_FrameRate")-1.666667f)<.000001f,"Wrong material clock");materials++;
            }
        }
        foreach(var guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
            foreach(var packable in AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)).GetPackables())
            {
                string path=AssetDatabase.GetAssetPath(packable);
                Need(!path.StartsWith(Root)&&!(AssetDatabase.IsValidFolder(path)&&Root.StartsWith(path+"/")),"Story sprites are atlas-packed");
            }
        var gateway=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab").GetComponentInChildren<NotebookProjectGateway>(true);
        for(int i=1;i<=20;i++)
        {
            Need(NotebookProjectGateway.TryStarwayMapId("SW"+i.ToString("00"),out string id)&&id=="SW"+(i-1).ToString("00"),"Stage/map offset changed");
            Need(gateway.starwayMaps.Any(map=>map&&(map.name==id||map.name==id+"_MapData")),"Source map is not connected: "+id);
        }
        var frameCounts=new System.Collections.Generic.Dictionary<string,int>();
        foreach(string path in new[]{"Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab","Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab"})
        {
            var host=AssetDatabase.LoadAssetAtPath<GameObject>(path);var frames=host.GetComponentsInChildren<StoryCrayonFrame>(true);frameCounts[path]=frames.Length;
            foreach(var transform in host.GetComponentsInChildren<Transform>(true))
                Need(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script: "+transform.name);
            foreach(var frame in frames)
            {
                var serialized=new SerializedObject(frame);
                Need(serialized.FindProperty("decoration").objectReferenceValue&&serialized.FindProperty("scope").objectReferenceValue,"Missing frame binding");
                foreach(string state in new[]{"normal","highlighted","pressed","disabled"})
                    foreach(string field in new[]{"frameA","animated","still"})
                        Need(serialized.FindProperty(state).FindPropertyRelative(field).objectReferenceValue,"Missing story state: "+frame.name+"/"+state+"/"+field);
            }
        }
        var gpu=Gpu();return new{sprites,materials,shaderMessages=0,atlasPacking=false,connectedSourceMaps=20,frameCounts,missingSkinReferences=0,missingScripts=0,gpu};
    }
    static string Render(Material material,RenderTexture target,float time)
    {
        Shader.SetGlobalFloat("_ANIMOLStoryCrayonUnscaledTime",time);RenderTexture.active=target;GL.Clear(true,true,Color.clear);
        Graphics.Blit(material.GetTexture("_MainTex"),target,material);
        var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try
        {
            image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();var bytes=image.GetRawTextureData<byte>().ToArray();Need(bytes.Any(b=>b>0),"GPU output empty");
            using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(bytes));
        }
        finally{Object.DestroyImmediate(image);}
    }
    static object Gpu()
    {
        var material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/StoryCrayon_PanelFrame_Animated.mat"));
        var target=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;float clock=Shader.GetGlobalFloat("_ANIMOLStoryCrayonUnscaledTime");
        try
        {
            var times=new[]{0f,.3f,.61f,1.21f,1.81f};var hashes=times.Select(t=>Render(material,target,t)).ToArray();
            Need(hashes[0]==hashes[1]&&hashes[0]==hashes[4]&&hashes[0]!=hashes[2]&&hashes[0]!=hashes[3]&&hashes[2]!=hashes[3],"A/B/C hold/loop failed");
            material.SetFloat("_Motion",0);Need(Render(material,target,1.21f)==hashes[0],"Still mode did not use A");
            return new{device=SystemInfo.graphicsDeviceType.ToString(),times,hashes,discrete=true,stillA=true};
        }
        finally{RenderTexture.active=previous;Shader.SetGlobalFloat("_ANIMOLStoryCrayonUnscaledTime",clock);Object.DestroyImmediate(target);Object.DestroyImmediate(material);}
    }
}
