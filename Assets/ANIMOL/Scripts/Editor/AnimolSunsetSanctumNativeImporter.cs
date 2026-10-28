#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Animol.Art.SunsetSanctumNative
{
    public static class AnimolSunsetSanctumNativeImporter
    {
        [Serializable] public sealed class Manifest
        {
            public int version;
            public bool nativeResolution;
            public FrameInfo[] frames;
            
            
            public Clip[] clips;
        }
        [Serializable] public sealed class Clip
        {
            public string name, character, direction, state;
            public int frameCount;
            public float fps;
            public bool loop;
            public string[] frames;
        }

        [Serializable] public sealed class FrameInfo { public string file; public int width, height; public float pixelsPerUnit; public float[] pivot; }

        public static void ImportMenu()
        {
            string root = EditorUtility.OpenFolderPanel("Select folder containing sunset_sanctum_native_animations.json", Application.dataPath, "");
            if (!string.IsNullOrEmpty(root)) ImportFromFolder(root);
        }

        // CLI entry: Unity -batchmode -executeMethod Animol.Art.SunsetSanctumNative.AnimolSunsetSanctumNativeImporter.ImportBatch -sunsetSanctumRoot <absolute Assets folder>
        public static void ImportBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i=0; i+1<args.Length; ++i)
                if (args[i]=="-sunsetSanctumRoot") { ImportFromFolder(args[i+1]); return; }
            throw new ArgumentException("Missing -sunsetSanctumRoot");
        }

        public static void ImportFromFolder(string root)
        {
            root = Path.GetFullPath(root).Replace('\\','/').TrimEnd('/');
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\','/').TrimEnd('/');
            if (!root.StartsWith(assets+"/", StringComparison.Ordinal) && root!=assets)
                throw new ArgumentException("Import folder must be inside this project's Assets folder.");
            string manifestPath = Path.Combine(root,"sunset_sanctum_native_animations.json");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (!manifest.nativeResolution || manifest.frames==null || manifest.frames.Length!=450 || manifest.clips==null || manifest.clips.Length!=75)
                throw new InvalidDataException("Expected complete native-resolution manifest with 450 frames and 75 clips.");
            AssetDatabase.Refresh();
            var frames = new Dictionary<string, Sprite>();
            var frameInfoMap = new Dictionary<string, FrameInfo>();
            foreach (FrameInfo f in manifest.frames) frameInfoMap.Add(f.file, f);
            foreach (var character in new[] { "sein", "daon", "lumi", "soyul", "doyun" })
                Directory.CreateDirectory(Path.Combine(root, "Animations", character));
            AssetDatabase.Refresh();
            try
            {
                for (int i=0; i<manifest.clips.Length; ++i)
                {
                    Clip data=manifest.clips[i];
                    EditorUtility.DisplayProgressBar("ANIMOL Sunset Sanctum Native Resolution",data.name,(float)i/manifest.clips.Length);
                    var keys=new ObjectReferenceKeyframe[data.frames.Length+1];
                    for (int f=0; f<data.frames.Length; ++f)
                    {
                        if (!frameInfoMap.TryGetValue(data.frames[f], out FrameInfo frameInfo)) throw new InvalidDataException("Missing native frame metadata");
                        string absolute=Path.GetFullPath(Path.Combine(root,data.frames[f])).Replace('\\','/');
                        if (!absolute.StartsWith(root+"/",StringComparison.Ordinal)) throw new InvalidDataException("Frame path escapes import folder.");
                        string assetPath="Assets"+absolute.Substring(assets.Length);
                        if (!frames.TryGetValue(assetPath,out Sprite sprite))
                        {
                            var importer=AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer==null) throw new FileNotFoundException("Missing texture importer",assetPath);
                            importer.textureType=TextureImporterType.Sprite;
                            importer.spriteImportMode=SpriteImportMode.Single;
                            importer.spritePixelsPerUnit=frameInfo.pixelsPerUnit;
                            importer.filterMode=data.character=="doyun"?FilterMode.Bilinear:FilterMode.Point;
                            importer.textureCompression=TextureImporterCompression.Uncompressed;
                            importer.crunchedCompression=false;
                            importer.mipmapEnabled=false;
                            importer.npotScale=TextureImporterNPOTScale.None;
                            importer.maxTextureSize=8192;
                            importer.ClearPlatformTextureSettings("Android");
                            importer.ClearPlatformTextureSettings("iPhone");
                            importer.ClearPlatformTextureSettings("Standalone");
                            importer.ClearPlatformTextureSettings("WebGL");
                            importer.alphaIsTransparency=true;
                            importer.wrapMode=TextureWrapMode.Clamp;
                            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                            settings.spriteAlignment=(int)SpriteAlignment.Custom;
                            settings.spritePivot=new Vector2(frameInfo.pivot[0],frameInfo.pivot[1]);
                            settings.spriteMeshType=SpriteMeshType.FullRect;
                            importer.SetTextureSettings(settings); importer.SaveAndReimport();
                            sprite=AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            if (sprite==null || sprite.rect.width!=frameInfo.width || sprite.rect.height!=frameInfo.height)
                                throw new InvalidDataException("Invalid native-resolution sprite: "+assetPath);
                            frames.Add(assetPath,sprite);
                        }
                        keys[f]=new ObjectReferenceKeyframe {time=f/data.fps,value=sprite};
                    }
                    keys[keys.Length-1]=new ObjectReferenceKeyframe {time=data.frames.Length/data.fps,value=keys[keys.Length-2].value};
                    string outDir=Path.Combine(root,"Animations",data.character);
                    string clipPath="Assets"+Path.Combine(outDir,data.name+".anim").Replace('\\','/').Substring(assets.Length);
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    bool isNew=clip==null; if (isNew)clip=new AnimationClip();
                    clip.name=data.name; clip.frameRate=data.fps; clip.wrapMode=data.loop?WrapMode.Loop:WrapMode.Once;
                    var binding=new EditorCurveBinding {path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"};
                    AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
                    if (isNew)AssetDatabase.CreateAsset(clip,clipPath);
                    var serialized=new SerializedObject(clip);
                    var loopTime=serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
                    if (loopTime!=null) {loopTime.boolValue=data.loop;serialized.ApplyModifiedPropertiesWithoutUndo();}
                    EditorUtility.SetDirty(clip);
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                Debug.Log("ANIMOL: imported 450 frame sprites and 75 animation clips. Side master faces right; use SpriteRenderer.flipX for left.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}
#endif
