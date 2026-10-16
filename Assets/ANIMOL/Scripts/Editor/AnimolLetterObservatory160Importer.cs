#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Animol.Art.LetterObservatory160
{
    public static class AnimolLetterObservatory160Importer
    {
        [Serializable] public sealed class Manifest
        {
            public int version, outfitRevision, frameWidth, frameHeight;
            public float[] pivot;
            public float pixelsPerUnit;
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

        [MenuItem("ANIMOL/Art/Import Letter Observatory 160px Animations")]
        public static void ImportMenu()
        {
            string root = EditorUtility.OpenFolderPanel("Select folder containing letter_observatory_animations.json", Application.dataPath, "");
            if (!string.IsNullOrEmpty(root)) ImportFromFolder(root);
        }

        // CLI entry: Unity -batchmode -executeMethod Animol.Art.LetterObservatory160.AnimolLetterObservatory160Importer.ImportBatch -observatoryRoot <absolute Assets folder>
        public static void ImportBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i=0; i+1<args.Length; ++i)
                if (args[i]=="-observatoryRoot") { ImportFromFolder(args[i+1]); return; }
            throw new ArgumentException("Missing -observatoryRoot");
        }

        public static void ImportFromFolder(string root)
        {
            root = Path.GetFullPath(root).Replace('\\','/').TrimEnd('/');
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\','/').TrimEnd('/');
            if (!root.StartsWith(assets+"/", StringComparison.Ordinal) && root!=assets)
                throw new ArgumentException("Import folder must be inside this project's Assets folder.");
            string manifestPath = Path.Combine(root,"letter_observatory_animations.json");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest.version!=2 || manifest.outfitRevision!=3 || manifest.frameWidth!=160 ||
                manifest.frameHeight!=160 || manifest.pixelsPerUnit!=160 || manifest.pivot==null ||
                manifest.pivot.Length!=2 || manifest.pivot[0]!=.5f || manifest.pivot[1]!=.125f ||
                manifest.clips==null || manifest.clips.Length!=60)
                throw new InvalidDataException("Expected Letter Observatory v2 160x160 manifest with 60 clips.");
            AssetDatabase.Refresh();
            var frames = new Dictionary<string, Sprite>();
            try
            {
                for (int i=0; i<manifest.clips.Length; ++i)
                {
                    Clip data=manifest.clips[i];
                    if (data.frames==null || data.frames.Length!=6 || data.frameCount!=6 || data.fps<=0)
                        throw new InvalidDataException("Expected six ordered frames: "+data.name);
                    EditorUtility.DisplayProgressBar("ANIMOL Letter Observatory 160px",data.name,(float)i/manifest.clips.Length);
                    var keys=new ObjectReferenceKeyframe[data.frames.Length+1];
                    for (int f=0; f<data.frames.Length; ++f)
                    {
                        string absolute=Path.GetFullPath(Path.Combine(root,data.frames[f])).Replace('\\','/');
                        if (!absolute.StartsWith(root+"/",StringComparison.Ordinal)) throw new InvalidDataException("Frame path escapes import folder.");
                        string assetPath="Assets"+absolute.Substring(assets.Length);
                        if (!frames.TryGetValue(assetPath,out Sprite sprite))
                        {
                            var importer=AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer==null) throw new FileNotFoundException("Missing texture importer",assetPath);
                            importer.textureType=TextureImporterType.Sprite;
                            importer.spriteImportMode=SpriteImportMode.Single;
                            importer.spritePixelsPerUnit=manifest.pixelsPerUnit;
                            importer.filterMode=FilterMode.Point;
                            importer.textureCompression=TextureImporterCompression.Uncompressed;
                            importer.crunchedCompression=false;
                            importer.mipmapEnabled=false;
                            importer.npotScale=TextureImporterNPOTScale.None;
                            importer.maxTextureSize=Math.Max(256,importer.maxTextureSize);
                            importer.alphaIsTransparency=true;
                            importer.wrapMode=TextureWrapMode.Clamp;
                            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                            settings.spriteAlignment=(int)SpriteAlignment.Custom;
                            settings.spritePivot=new Vector2(manifest.pivot[0],manifest.pivot[1]);
                            settings.spriteMeshType=SpriteMeshType.FullRect;
                            importer.SetTextureSettings(settings);
                            var android=importer.GetPlatformTextureSettings("Android");
                            android.name="Android"; android.overridden=true; android.maxTextureSize=2048;
                            android.format=TextureImporterFormat.RGBA32;
                            android.textureCompression=TextureImporterCompression.Uncompressed;
                            android.crunchedCompression=false;
                            importer.SetPlatformTextureSettings(android);
                            importer.SaveAndReimport();
                            sprite=AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            if (sprite==null || sprite.rect.width!=160 || sprite.rect.height!=160)
                                throw new InvalidDataException("Invalid 160x160 sprite: "+assetPath);
                            frames.Add(assetPath,sprite);
                        }
                        keys[f]=new ObjectReferenceKeyframe {time=f/data.fps,value=sprite};
                    }
                    keys[keys.Length-1]=new ObjectReferenceKeyframe {time=data.frames.Length/data.fps,value=keys[keys.Length-2].value};
                    string outDir=Path.Combine(root,"Animations",data.character); Directory.CreateDirectory(outDir);
                    AssetDatabase.Refresh();
                    string clipPath="Assets"+Path.Combine(outDir,data.name+".anim").Replace('\\','/').Substring(assets.Length);
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    bool isNew=clip==null; if (isNew)clip=new AnimationClip();
                    if (isNew)AssetDatabase.CreateAsset(clip,clipPath);
                    clip.name=data.name; clip.legacy=false; clip.frameRate=data.fps;
                    clip.wrapMode=data.loop?WrapMode.Loop:WrapMode.Once;
                    var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
                    AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
                    var serialized=new SerializedObject(clip);
                    var loopTime=serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
                    if (loopTime!=null) {loopTime.boolValue=data.loop;serialized.ApplyModifiedPropertiesWithoutUndo();}
                    EditorUtility.SetDirty(clip);
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                Debug.Log("ANIMOL: imported 360 frame sprites and 60 animation clips. Side master faces right; use SpriteRenderer.flipX for left.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}
#endif
