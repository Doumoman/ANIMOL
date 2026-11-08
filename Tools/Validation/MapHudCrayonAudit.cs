using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using TMPro;
using Newtonsoft.Json.Linq;
public static class MapHudCrayonAudit
{
    static void Need(bool yes,string message){if(!yes)throw new InvalidOperationException(message);}
    public static object Run()
    {
        const string root="Assets/ANIMOL/MapHudCrayonUI/";
        var entries=JObject.Parse(File.ReadAllText(root+"Data/sprite-manifest.json"))["entries"];
        int sprites=0,materials=0;
        foreach(var e in entries)
        {
            var textures=new List<Texture2D>();
            foreach(string path in e["frames"].Values<string>())
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                Need(importer.spriteImportMode==SpriteImportMode.Single&&settings.spriteMeshType==SpriteMeshType.FullRect&&importer.spritePixelsPerUnit==100,"Sprite mode/mesh/PPU: "+path);
                Need(!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.filterMode==FilterMode.Bilinear&&importer.wrapMode==TextureWrapMode.Clamp,"Texture sampling: "+path);
                Need(sprite.rect.width==(int)e["width"]&&sprite.rect.height==(int)e["height"]&&sprite.border==Vector4.one*(int)e["border"],"Dimensions/border: "+path);
                textures.Add(sprite.texture);sprites++;
            }
            foreach(bool still in new[]{false,true})
            {
                var path=root+"Generated/Materials/"+((string)e["id"]).Replace('/','_').Replace(' ','_')+(still?"_Still.mat":"_Animated.mat");
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                Need(mat&&mat.shader.isSupported,"Missing/unsupported material: "+path);
                Need(mat.GetTexture("_MainTex")==textures[0]&&mat.GetTexture("_FrameB")==textures[1]&&mat.GetTexture("_FrameC")==textures[2],"Wrong A/B/C: "+path);
                Need(mat.GetFloat("_Motion")== (still?0:1)&&Mathf.Abs(mat.GetFloat("_FrameRate")-1.666667f)<.000001f,"Wrong step clock: "+path);materials++;
            }
        }
        foreach(var guid in AssetDatabase.FindAssets("t:SpriteAtlas",new[]{"Assets"}))
            foreach(var packable in AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)).GetPackables())
            {
                var p=AssetDatabase.GetAssetPath(packable);
                Need(!p.StartsWith(root)&&!p.StartsWith("Assets/ANIMOL/CrayonNumerals/")&&
                    !(AssetDatabase.IsValidFolder(p)&&root.StartsWith(p+"/")),"Forbidden atlas inclusion: "+p);
            }
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(root+"Shaders/UI_ANIMOL_MapHudCrayonBoil.shader");
        Need(ShaderUtil.GetShaderMessages(shader).Length==0,"Shader diagnostics");
        var fonts=AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets/ANIMOL/CrayonNumerals/Generated"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Need(fonts.Length>0&&fonts.All(f=>f.atlasPopulationMode==AtlasPopulationMode.Static&&f.HasCharacters("0123456789/-+: .IV")&&
            f.material.GetTexture("_MainTex")==f.atlasTexture),"Incomplete static numeral font");
        return new{sprites,materials,staticFonts=fonts.Length,shaderMessages=0,atlasPacking=false};
    }
}
