using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using ANIMOL.Presentation.Editor;
public static class ObservatoryAtlasQa
{
    public static string Main()
    {
        var m=LetterObservatoryArtBuilder.Read();
        var sprites=m.assets.Where(a=>new[]{"03","10","16","21"}.Contains(a.deviceId)).Take(45).Select(a=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+a.path))
            .Concat(m.animations.Where(a=>a.deviceId=="21"||a.id.StartsWith("d10_optics/mirror_")).Select(a=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+a.frames[a.frames.Length/2].path))).ToArray();
        var atlas=new SpriteAtlas();atlas.SetIncludeInBuild(false);
        var textures=atlas.GetTextureSettings();textures.filterMode=FilterMode.Point;textures.generateMipMaps=false;textures.readable=true;atlas.SetTextureSettings(textures);
        var packing=atlas.GetPackingSettings();packing.enableRotation=false;packing.enableTightPacking=false;packing.padding=4;atlas.SetPackingSettings(packing);
        var platform=atlas.GetPlatformSettings("DefaultTexturePlatform");platform.maxTextureSize=4096;platform.format=TextureImporterFormat.RGBA32;platform.textureCompression=TextureImporterCompression.Uncompressed;atlas.SetPlatformSettings(platform);
        atlas.Add(sprites.Cast<UnityEngine.Object>().ToArray());
        Directory.CreateDirectory("Assets/ANIMOL/Scripts/Editor/QA");AssetDatabase.Refresh();
        const string path="Assets/ANIMOL/Scripts/Editor/QA/letter_observatory_import_check.spriteatlas";
        if(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(atlas,path);SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        var allowed=new HashSet<string>(new[]{"1A1C2C","257179","29366F","333C57","38B764","3B5DC9","41A6F6","566C86","5D275D","73EFF7","94B0C2","A7F070","B13E53","EF7D57","F4F4F4","FFCD75"});
        var getter=typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        var pages=(Texture2D[])getter.Invoke(null,new object[]{atlas});int opaque=0;var unexpected=new HashSet<string>();
        foreach(var page in pages)foreach(var color in page.GetPixels32())if(color.a==255){opaque++;var rgb=$"{color.r:X2}{color.g:X2}{color.b:X2}";if(!allowed.Contains(rgb))unexpected.Add(rgb);}
        if(unexpected.Count>0||pages.Length==0)throw new Exception("Atlas RGB changed: "+string.Join(",",unexpected));
        AssetDatabase.SaveAssets();
        File.Copy(path,"Docs/Validation/LetterObservatory/Applied/atlas_snapshot.spriteatlas",true);
        for(int i=0;i<pages.Length;i++)File.WriteAllBytes($"Docs/Validation/LetterObservatory/Applied/atlas_page_{i}.png",pages[i].EncodeToPNG());
        AssetDatabase.DeleteAsset(path);
        if(Directory.GetFiles("Assets/ANIMOL/Scripts/Editor/QA").Length==0)AssetDatabase.DeleteAsset("Assets/ANIMOL/Scripts/Editor/QA");
        // An excluded packed atlas can leave Editor-only sprite bindings in Play mode. Restore source imports.
        foreach(var sprite in sprites)AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(sprite),ImportAssetOptions.ForceUpdate);
        File.WriteAllText("Docs/Validation/LetterObservatory/Applied/atlas_qa.json",$"{{\"representativeSprites\":{sprites.Length},\"pages\":{pages.Length},\"opaquePixels\":{opaque},\"unexpectedRgb\":0,\"point\":true,\"uncompressed\":true,\"mipmaps\":false,\"runtimeAtlas\":false,\"snapshotOutsideAssets\":true}}");
        return $"Transient atlas: {sprites.Length} sprites, {pages.Length} pages, no new opaque RGB; snapshot outside Assets and source bindings restored";
    }
}
