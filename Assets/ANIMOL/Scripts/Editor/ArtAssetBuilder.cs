using System;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ANIMOL.Presentation.Editor
{
    [Serializable] public class ImportManifest { public AssetRow[] assets; public ClipRow[] animations; public string background; }
    [Serializable] public class AssetRow { public string id,path,filter; public int width,height,pixelsPerCell; public float pivotX,pivotY; }
    [Serializable] public class ClipRow : AssetRow { public string clipPath; public bool loop; public int totalMilliseconds; public FrameRow[] frames; }
    [Serializable] public class FrameRow { public string path; public int durationMilliseconds,startMilliseconds; }
    public sealed class SpriteImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder()=>10000;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(ArtAssetBuilder.Root+"/Sprites/",StringComparison.Ordinal))return;
            // The full-resolution menu art has its own importer policy. The gameplay
            // sprite manifest must not force these illustrations to Point/32 PPU.
            if(assetPath.StartsWith(ArtAssetBuilder.Root+"/Sprites/UI/Main/",StringComparison.Ordinal))return;
            var manifest=ArtAssetBuilder.Read();
            var relative=assetPath.Substring(ArtAssetBuilder.Root.Length+1);
            AssetRow row=manifest.assets.FirstOrDefault(a=>a.path==relative);
            if(row==null)row=manifest.animations.FirstOrDefault(a=>a.frames.Any(f=>f.path==relative));
            // Device pack pivots are normalized from pixel coordinates in the canonical manifest.
            var deviceManifest=ArtAssetBuilder.Root+"/Data/device_art_import_manifest.json";
            if(row==null && File.Exists(deviceManifest))
            {
                var devices=JsonUtility.FromJson<ImportManifest>(File.ReadAllText(deviceManifest));
                row=devices.assets.FirstOrDefault(a=>a.path==relative);
                if(row==null)row=devices.animations.FirstOrDefault(a=>a.frames.Any(f=>f.path==relative));
            }
            var theaterManifest=ArtAssetBuilder.Root+"/Data/memory_theater_import_manifest.json";
            if(row==null && File.Exists(theaterManifest))
            {
                var theater=JsonUtility.FromJson<ImportManifest>(File.ReadAllText(theaterManifest));
                row=theater.assets.FirstOrDefault(a=>a.path==relative);
                if(row==null)row=theater.animations.FirstOrDefault(a=>a.frames.Any(f=>f.path==relative));
            }
            var importer=(TextureImporter)assetImporter;
            var observatoryManifest=ArtAssetBuilder.Root+"/Data/letter_observatory_import_manifest.json";
            if(row==null && File.Exists(observatoryManifest))
            {
                var observatory=JsonUtility.FromJson<ImportManifest>(File.ReadAllText(observatoryManifest));
                row=observatory.assets.FirstOrDefault(a=>a.path==relative);
                if(row==null)row=observatory.animations.FirstOrDefault(a=>a.frames.Any(f=>f.path==relative));
            }
            var seaManifest=ArtAssetBuilder.Root+"/Data/twilight_sea_import_manifest.json";
            if(row==null && File.Exists(seaManifest))
            {
                var sea=JsonUtility.FromJson<ImportManifest>(File.ReadAllText(seaManifest));
                row=sea.assets.FirstOrDefault(a=>a.path==relative);
                if(row==null)row=sea.animations.FirstOrDefault(a=>a.frames.Any(f=>f.path==relative));
            }
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaSource=TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false; importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=8192; importer.filterMode=row!=null && row.filter=="Bilinear"?FilterMode.Bilinear:FilterMode.Point;
            // Pack rows retain their logical-cell PPU; new unregistered 32px sprites use the project default.
            importer.spritePixelsPerUnit=row!=null?row.pixelsPerCell:(relative==manifest.background?80:32);
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=row!=null?new Vector2(row.pivotX,row.pivotY):new Vector2(.5f,.5f);
            importer.SetTextureSettings(settings);
            foreach(var platform in new[]{"Android","iPhone","Standalone","WebGL"})importer.ClearPlatformTextureSettings(platform);
        }
    }
    public static class ArtAssetBuilder
    {
        public const string Root="Assets/ANIMOL";
        public const string ScenePath=Root+"/Scenes/QA/ANIMOL_Starway_Art_QA.unity";
        public static ImportManifest Read()=>JsonUtility.FromJson<ImportManifest>(File.ReadAllText(Root+"/Data/art_import_manifest.json"));
        static Sprite Sprite(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+Read().assets.First(a=>a.id==id).path);
        static SpriteFrameSequence Frames(string id)=>AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Root+"/Data/AnimationSequences/"+id+".asset");
        static GameObject Visual(string id,Vector3 position,Transform parent=null)
        {
            var go=new GameObject(id); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            var child=new GameObject("Visual");child.transform.SetParent(go.transform,false);
            var renderer=child.AddComponent<SpriteRenderer>(); renderer.sprite=Sprite(id);renderer.sortingOrder=10;
            string prefix=id=="soap_bubble_pink"?"bubble":id;
            if(Frames(prefix+"_idle"))
            {
                var visual=go.AddComponent<SpriteFramePlayer>();visual.visual=renderer;
                visual.idle=Frames(prefix+"_idle");visual.touch=Frames(prefix+"_touch");
                // Position-derived visual phase consumes no gameplay RNG.
                visual.idlePhase=Mathf.Repeat(position.x*.371f+position.y*.619f,visual.idle.Duration);
            }
            return go;
        }
        [MenuItem("ANIMOL/Art QA/Rebuild art QA")]
        public static void Build()
        {
            foreach(var name in new[]{"Data/AnimationSequences","Prefabs","Scenes","Materials"}) Directory.CreateDirectory(Root+"/"+name);
            AssetDatabase.Refresh(); var manifest=Read();
            foreach(var row in manifest.animations)
            {
                string path=Root+"/Data/AnimationSequences/"+row.id+".asset";
                var sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(path);
                if(!sequence) {sequence=ScriptableObject.CreateInstance<SpriteFrameSequence>();AssetDatabase.CreateAsset(sequence,path);}
                sequence.frames=row.frames.Select(f=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+f.path)).ToArray();
                sequence.durations=row.frames.Select(f=>f.durationMilliseconds/1000f).ToArray();sequence.loop=row.loop;EditorUtility.SetDirty(sequence);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Root+"/"+row.clipPath);}
                clip.frameRate=1000; var keys=row.frames.Select((f,i)=>new ObjectReferenceKeyframe{time=f.startMilliseconds/1000f,value=sequence.frames[i]}).ToList();
                // Unity includes one sample after the final PPtr key; keep the manifest end time exact.
                keys.Add(new ObjectReferenceKeyframe{time=(row.totalMilliseconds-1)/1000f,value=sequence.frames[row.frames.Length-1]});
                AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=row.loop;settings.stopTime=row.totalMilliseconds/1000f;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }
            foreach(var row in manifest.assets)
            {
                var go=Visual(row.id,Vector3.zero);PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+row.id+".prefab");UnityEngine.Object.DestroyImmediate(go);
            }
            var fx=Visual("soap_bubble_pink",Vector3.zero);fx.name="bubble_collect_fx";
            var effect=fx.GetComponent<SpriteFramePlayer>();effect.touch=Frames("bubble_collect");effect.returnToPool=true;
            PrefabUtility.SaveAsPrefabAsset(fx,Root+"/Prefabs/bubble_collect_fx.prefab");UnityEngine.Object.DestroyImmediate(fx);
            var exit=new GameObject("exit_visual_root"); Visual("exit_arch",Vector3.zero,exit.transform);
            Visual("exit_steps",new Vector3(0,-.5f,0),exit.transform);
            var arch=manifest.assets.First(a=>a.id=="exit_arch");
            Visual("exit_star",new Vector3((112-arch.pivotX*arch.width)/80f,((1-arch.pivotY)*arch.height-82)/80f,0),exit.transform);
            PrefabUtility.SaveAsPrefabAsset(exit,Root+"/Prefabs/exit_visual_root.prefab");UnityEngine.Object.DestroyImmediate(exit);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var qa=new GameObject("Art QA controls - no puzzle logic").AddComponent<ArtQaController>();
            qa.collectionFx=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/bubble_collect_fx.prefab"));qa.collectionFx.transform.position=new Vector3(5,4,0);qa.collectionFx.SetActive(false);
            var cam=new GameObject("QA Camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=12;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.12f,.14f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
            for(int background=0;background<3;background++)
            {
                float y=8-background*6.2f;
                if(background<2)
                {
                    var panel=GameObject.CreatePrimitive(PrimitiveType.Quad);panel.name=background==0?"Light QA background":"Dark QA background";
                    UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());panel.transform.position=new Vector3(0,y-2,1);panel.transform.localScale=new Vector3(11,5.5f,1);
                    string materialPath=Root+"/Materials/"+panel.name.Replace(' ','_').ToLowerInvariant()+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!material){material=new Material(Shader.Find("Unlit/Color"));AssetDatabase.CreateAsset(material,materialPath);}
                    material.color=background==0?new Color(.92f,.92f,.86f):new Color(.04f,.05f,.08f);panel.GetComponent<MeshRenderer>().sharedMaterial=material;
                }
                else for(int x=-5;x<=5;x++)for(int row=0;row<5;row++) {var floor=Visual("grass_mid",new Vector3(x,y-row,0));floor.GetComponentInChildren<SpriteRenderer>().sortingOrder=0;}
                Visual("vine_tile",new Vector3(-4,y,0));
                for(int x=0;x<2;x++)Visual("vine_tile",new Vector3(-5+x,y-2,0));
                Visual("vine_strip",new Vector3(-2,y,0));
                Visual("vines",new Vector3(2,y,0));
                for(int x=0;x<3;x++)for(int row=0;row<3;row++)Visual("vine_tile",new Vector3(-2+x,y-2-row,0));
                for(int row=0;row<3;row++)Visual("vine_tile",new Vector3(4,y-row,0));
            }
            // Remaining imported assets are actually visible in a separate gallery, not hidden mock gameplay.
            int i=0;foreach(var row in manifest.assets.Where(a=>!a.id.StartsWith("vine")))
            { Visual(row.id,new Vector3(9+(i%6)*3,8-(i/6)*3,0));i++; }
            var sky=new GameObject("Pack night sky - runtime background, not reference image").AddComponent<SpriteRenderer>();
            sky.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+manifest.background);sky.sortingOrder=-10;sky.transform.position=new Vector3(23,0,2);sky.transform.localScale=Vector3.one*2.6f;
            var exitInstance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/exit_visual_root.prefab"));exitInstance.transform.position=new Vector3(12,-9,0);
            BuildHud();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Validate();
        }
        static void BuildHud()
        {
            var canvasObject=new GameObject("Puzzle HUD - awaiting authoritative puzzle binding",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(canvasObject.transform,false);
            var rect=safe.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var pauseObject=new GameObject("Pause",typeof(RectTransform),typeof(Image),typeof(PuzzleHudView));pauseObject.transform.SetParent(safe.transform,false);
            var hud=pauseObject.GetComponent<PuzzleHudView>();hud.pause=pauseObject.GetComponent<Image>();hud.safeArea=rect;
            hud.normal=Sprite("pause_normal");hud.press=Sprite("pause_press");hud.release=Sprite("pause_release");hud.disabled=Sprite("pause_disabled");hud.pause.sprite=hud.disabled;
            var p=pauseObject.GetComponent<RectTransform>();p.anchorMin=p.anchorMax=new Vector2(0,1);p.pivot=new Vector2(0,1);p.anchoredPosition=new Vector2(28,-28);p.sizeDelta=new Vector2(132,132);
            hud.cost=HudPanel("cost_panel_blank","COST --",safe.transform,new Vector2(300,-30));hud.bubbles=HudPanel("bubble_counter_blank","--/--",safe.transform,new Vector2(730,-30));
            PrefabUtility.SaveAsPrefabAsset(canvasObject,Root+"/Prefabs/hud_visual.prefab");
        }
        static TMP_Text HudPanel(string sprite,string value,Transform parent,Vector2 position)
        {
            var panel=new GameObject(sprite,typeof(RectTransform),typeof(Image));panel.transform.SetParent(parent,false);panel.GetComponent<Image>().sprite=Sprite(sprite);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(270,90);
            var label=new GameObject("Live state",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);
            var r=label.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(15,5);r.offsetMax=new Vector2(-15,-5);
            var text=label.GetComponent<TextMeshProUGUI>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Fonts/Pixelroborobo_UI.asset");text.fontSize=42;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.97f,.94f,.82f);text.text=value;return text;
        }
        public static void Validate()
        {
            var manifest=Read();int checkedTextures=0;
            foreach(var row in manifest.assets.Concat<AssetRow>(manifest.animations))
            {
                var paths=row is ClipRow c?c.frames.Select(f=>f.path):new[]{row.path};
                foreach(string relative in paths)
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+relative);
                    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+relative);
                    if(!sprite || sprite.rect.width!=row.width || sprite.rect.height!=row.height || importer.spritePixelsPerUnit!=row.pixelsPerCell || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed || Vector2.Distance(sprite.pivot,new Vector2(row.pivotX*row.width,row.pivotY*row.height))>.01f)throw new Exception("Invalid sprite import "+relative);
                    checkedTextures++;
                }
            }
            foreach(var row in manifest.animations)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(Mathf.Abs(clip.length-row.totalMilliseconds/1000f)>.001f || AnimationUtility.GetAnimationEvents(clip).Length!=0)throw new Exception("Invalid clip "+row.id);
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root}))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)throw new Exception("Missing script in "+prefab.name);
                foreach(var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))if(!renderer.sprite)throw new Exception("Missing sprite in "+prefab.name);
            }
            Debug.Log("ART_VALIDATED textures="+checkedTextures+" clips="+manifest.animations.Length);
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            Directory.CreateDirectory("Screenshots/ArtQA");
            foreach(var state in new[]{"static","idle","touch"})
            {
                foreach(var visual in UnityEngine.Object.FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None))
                {if(state=="touch")visual.PlayTouch();if(state!="static")visual.Advance(state=="touch"?.16f:.9f);}
                var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1080,1920,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();File.WriteAllBytes("Screenshots/ArtQA/qa_"+state+".png",image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            }
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            camera.transform.position=new Vector3(16,0,-10);camera.orthographicSize=10;
            CaptureImage(camera,1920,1080,"gallery");
            camera.transform.position=new Vector3(-1,5,-10);camera.orthographicSize=2;
            foreach(int scale in new[]{1,2,4})CaptureImage(camera,320*scale,320*scale,"foliage_"+scale+"x");
        }
        static void CaptureImage(Camera camera,int width,int height,string name)
        {
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes("Screenshots/ArtQA/qa_"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
