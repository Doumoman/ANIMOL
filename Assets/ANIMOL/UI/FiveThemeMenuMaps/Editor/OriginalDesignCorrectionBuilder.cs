using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Editor
{
    public static class OriginalDesignCorrectionBuilder
    {
        public const string Source="Tools/ArtSources/ANIMOL_Original_Design_Correction_v2";
        public const string Root="Assets/ANIMOL/UI/OriginalDesignCorrectionV2/runtime/main-background";
        [Serializable] private class Manifest {public int runtimeReplacementCount,campaignRuntimeSpriteReplacementCount;public Entry[] assets;}
        [Serializable] private class Entry {public string id,path,sha256;public int[] size;}
        private static string Hash(string path) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        [MenuItem("ANIMOL/Main UI V7/Apply Original Design Correction V2 background")]
        public static void ApplyBackground()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before importing background art.");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Source+"/manifest.json"));
            var expected=Enumerable.Range(1,5).SelectMany(t=>new[]{"far","mid","platform","near"}.Select(l=>"T"+t.ToString("00")+"-"+l)).ToArray();
            if(manifest.runtimeReplacementCount!=20 || manifest.campaignRuntimeSpriteReplacementCount!=0 || manifest.assets.Length!=20 || !expected.OrderBy(x=>x).SequenceEqual(manifest.assets.Select(a=>a.id).OrderBy(x=>x)))throw new InvalidDataException("Unexpected correction manifest.");
            foreach(var a in manifest.assets)
                if(a.path!="runtime/main-background/"+a.id+".png" || a.size.Length!=2 || a.size[0]!=352 || a.size[1]!=704 || Hash(Source+"/"+a.path)!=a.sha256)throw new InvalidDataException("Background manifest mismatch: "+a.id);
            // The resource key, catalog GUID, shader, current rabbit sheets and scene bindings stay intact.
            var catalog=Resources.Load<FantasyBackgroundCatalog>("ANIMOLMainUiV7");
            if(catalog==null || catalog.themes.Length!=5 || catalog.rabbitLeft==null || catalog.rabbitRight==null)throw new InvalidOperationException("Existing V7 catalog is required.");
            Directory.CreateDirectory(Root);
            foreach(var a in manifest.assets)
            {
                var path=Root+"/"+a.id+".png";
                if(!File.Exists(path) || Hash(path)!=a.sha256)File.Copy(Source+"/"+a.path,path,true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                // Native compositor samples Texture2D; do not create sliced Sprite subassets.
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;
                importer.spritePixelsPerUnit=32;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL","Windows Store Apps"})importer.ClearPlatformTextureSettings(platform);
                importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(texture.width!=352 || texture.height!=704)throw new InvalidDataException("Unexpected imported dimensions: "+a.id);
            }
            Undo.RecordObject(catalog,"Apply original design correction v2 background");
            catalog.themes=Enumerable.Range(1,5).Select(t=>new FantasyBackgroundCatalog.Theme{far=Load(t,"far"),mid=Load(t,"mid"),platform=Load(t,"platform"),near=Load(t,"near")}).ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Original Design Correction V2: 20 backgrounds verified and connected to ANIMOLMainUiV7; current rabbits retained.");
        }
        private static Texture2D Load(int theme,string layer)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/T"+theme.ToString("00")+"-"+layer+".png");
    }
}
