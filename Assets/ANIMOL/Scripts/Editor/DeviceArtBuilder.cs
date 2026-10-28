using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ANIMOL.Presentation;
namespace ANIMOL.Presentation.Editor
{
    [Serializable] public class DeviceManifest {public DeviceAsset[] assets;public DeviceClip[] animations;}
    [Serializable] public class DeviceAsset : AssetRow {public string deviceId;}
    [Serializable] public class DeviceClip : ClipRow {public string deviceId,eventName,terminalState;}
    public static class DeviceArtBuilder
    {
        public const string Root="Assets/ANIMOL";
        public const string ScenePath=Root+"/Scenes/QA/ANIMOL_Device8_Art_QA.unity";
        static string Key(string id)=>id.Replace('/','_');
        public static DeviceManifest Read()
        {
            // 'event' is a C# keyword; preserve its source spelling in JSON and deserialize an alias.
            return JsonUtility.FromJson<DeviceManifest>(File.ReadAllText(Root+"/Data/device_art_import_manifest.json").Replace("\"event\":","\"eventName\":"));
        }
        static Sprite Load(string relative)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+relative);
        static string SeqPath(string id)=>Root+"/Data/AnimationSequences/Devices/"+Key(id)+".asset";
        static string PrefabPath(string id)=>Root+"/Prefabs/Devices/"+Key(id)+".prefab";
        static DeviceVisualState State(DeviceClip row,DeviceManifest m)
        {
            string prefix=row.id.Split('/')[0],state=row.id.Split('/')[1],direction=state.Split('_').Last();
            string terminal=row.terminalState;
            var target=m.assets.FirstOrDefault(a=>a.id==prefix+"/"+terminal+"_"+direction)??m.assets.FirstOrDefault(a=>a.id==prefix+"/"+terminal);
            return new DeviceVisualState{name=state,eventName=row.eventName,sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(SeqPath(row.id)),clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath),terminal=terminal=="transparent"?null:(target!=null?Load(target.path):Load(row.frames.Last().path))};
        }
        static GameObject Create(string name,string deviceId,DeviceVisualState[] states,string initial)
        {
            var go=new GameObject(name);var child=new GameObject("Visual");child.transform.SetParent(go.transform,false);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=10;
            var view=go.AddComponent<DeviceVisualView>();view.deviceId=deviceId;view.visual=renderer;view.states=states;view.initialState=initial;view.ResetVisual();return go;
        }
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before authoring.");
            foreach(var dir in new[]{"Data/AnimationSequences/Devices","Prefabs/Devices"})Directory.CreateDirectory(Root+"/"+dir);
            var m=Read();foreach(var row in m.animations)Directory.CreateDirectory(Path.GetDirectoryName(Root+"/"+row.clipPath));
            AssetDatabase.Refresh();
            foreach(var row in m.animations)
            {
                var sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(SeqPath(row.id));
                if(!sequence){sequence=ScriptableObject.CreateInstance<SpriteFrameSequence>();AssetDatabase.CreateAsset(sequence,SeqPath(row.id));}
                sequence.frames=row.frames.Select(f=>Load(f.path)).ToArray();sequence.durations=row.frames.Select(f=>f.durationMilliseconds/1000f).ToArray();sequence.loop=row.loop;EditorUtility.SetDirty(sequence);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Root+"/"+row.clipPath);}
                clip.frameRate=1000;
                var keys=row.frames.Select((f,i)=>new ObjectReferenceKeyframe{time=f.startMilliseconds/1000f,value=sequence.frames[i]}).ToList();
                keys.Add(new ObjectReferenceKeyframe{time=(row.totalMilliseconds-1)/1000f,value=sequence.frames.Last()});
                AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
                AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=row.loop;settings.stopTime=row.totalMilliseconds/1000f;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }
            foreach(var device in m.assets.Select(a=>a.deviceId).Distinct())
            {
                string prefix=m.assets.First(a=>a.deviceId==device).id.Split('/')[0];
                var states=m.assets.Where(a=>a.deviceId==device).Select(a=>new DeviceVisualState{name=a.id.Split('/')[1],sprite=Load(a.path)}).Concat(m.animations.Where(a=>a.deviceId==device).Select(a=>State(a,m))).ToArray();
                // Static open/idle and animated open/idle share names. Prefer the timed sequence.
                states=states.GroupBy(s=>s.name).Select(g=>g.Last()).ToArray();
                var go=Create(prefix,device,states,m.assets.First(a=>a.deviceId==device).id.Split('/')[1]);
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath(prefix));UnityEngine.Object.DestroyImmediate(go);
            }
            // Each clip and each static role gets its own usable prefab; no sheets or overlapping body layers.
            foreach(var row in m.animations)
            {
                var go=Create(Key(row.id),row.deviceId,new[]{State(row,m)},row.id.Split('/')[1]);
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath(row.id));UnityEngine.Object.DestroyImmediate(go);
            }
            foreach(var row in m.assets)
            {
                var go=Create(Key(row.id)+"_static",row.deviceId,new[]{new DeviceVisualState{name="static",sprite=Load(row.path)}},"static");
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath(row.id+"_static"));UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("QA Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=18;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.14f,.16f,.21f);
            var controls=new GameObject("QA controls - presentation only").AddComponent<DeviceArtQaController>();controls.qaCamera=camera;
            var views=new List<DeviceVisualView>();
            // Three side-by-side backgrounds; the same 111 states at identical integer-cell positions.
            for(int panel=0;panel<3;panel++)
            {
                float origin=panel*24;
                var bg=GameObject.CreatePrimitive(PrimitiveType.Quad);bg.name=new[]{"Light","Dark","Grass"}[panel]+" QA panel";UnityEngine.Object.DestroyImmediate(bg.GetComponent<Collider>());
                bg.transform.position=new Vector3(origin,0,2);bg.transform.localScale=new Vector3(22,34,1);
                var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+(panel==0?"light_qa_background":"dark_qa_background")+".mat");
                if(material)bg.GetComponent<MeshRenderer>().sharedMaterial=material;
                if(panel==2)
                {
                    var baseManifest=ArtAssetBuilder.Read();var grass=baseManifest.assets.First(a=>a.id.Contains("grass") && !a.id.Contains("tuft"));
                    bg.SetActive(false);
                    for(int x=-11;x<=11;x++)for(int y=-17;y<=17;y++){var tile=new GameObject("grass").AddComponent<SpriteRenderer>();tile.sprite=Load(grass.path);tile.transform.position=new Vector3(origin+x,y,1);tile.sortingOrder=-10;}
                }
                var entries=m.animations.Select(a=>PrefabPath(a.id)).Concat(m.assets.Select(a=>PrefabPath(a.id+"_static"))).ToArray();
                for(int i=0;i<entries.Length;i++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(entries[i]));
                    go.transform.position=new Vector3(origin-9+(i%10)*2,14-(i/10)*2.4f,0);views.Add(go.GetComponent<DeviceVisualView>());
                }
            }
            // Full 128px bridge visuals on one-world-unit (80px) intervals: H/V 1,2,3 cells.
            for(int axis=0;axis<2;axis++)for(int length=1;length<=3;length++)for(int i=0;i<length;i++)
            {
                string id="d02_pressure_vine_bridge/bridge_idle_"+(axis==0?"horizontal":"vertical");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(id)));
                go.name=$"Bridge_{axis}_{length}_{i}";go.transform.position=new Vector3(72-6+length*4+(axis==0?i:0),8-axis*8+(axis==1?i:0),0);views.Add(go.GetComponent<DeviceVisualView>());
            }
            // V04 context references stay canonical and byte-identical.
            var masters=new List<DeviceVisualView>();
            foreach(var prefix in m.assets.Select(a=>a.id.Split('/')[0]).Distinct())
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefix)));
                go.transform.position=new Vector3(72-7+masters.Count*2,-6,0);var view=go.GetComponent<DeviceVisualView>();masters.Add(view);views.Add(view);
            }
            controls.masters=masters.ToArray();
            foreach(var id in new[]{"rabbit_front","soap_bubble_pink","exit_visual_root"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab");
                if(prefab){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.position=new Vector3(72-4+Array.IndexOf(new[]{"rabbit_front","soap_bubble_pink","exit_visual_root"},id)*4,-12,0);}
            }
            controls.views=views.ToArray();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            return $"52 static sprites, 402 frames, 59 sequences/clips, 119 prefabs, {views.Count} QA views";
        }
        public static string Validate()
        {
            var m=Read();int sprites=0;
            foreach(var row in m.assets.Cast<AssetRow>().Concat(m.animations))
            {
                var paths=row is DeviceClip c?c.frames.Select(f=>f.path):new[]{row.path};
                foreach(var path in paths)
                {
                    var sprite=Load(path);var importer=AssetImporter.GetAtPath(Root+"/"+path) as TextureImporter;
                    if(!sprite || !importer || importer.spritePixelsPerUnit!=80 || importer.mipmapEnabled || importer.filterMode!=FilterMode.Point || importer.textureCompression!=TextureImporterCompression.Uncompressed || sprite.rect.width!=row.width || sprite.rect.height!=row.height || Vector2.Distance(sprite.pivot,new Vector2(row.pivotX*row.width,row.pivotY*row.height))>.01f)throw new Exception("Import mismatch: "+path);
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                    if(importer.spriteImportMode!=SpriteImportMode.Single || settings.spriteMeshType!=SpriteMeshType.FullRect || importer.wrapMode!=TextureWrapMode.Clamp || importer.npotScale!=TextureImporterNPOTScale.None || importer.alphaSource!=TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency || importer.maxTextureSize<Mathf.Max(row.width,row.height) || new[]{"Android","iPhone","Standalone","WebGL"}.Any(platform=>importer.GetPlatformTextureSettings(platform).overridden))throw new Exception("Texture settings mismatch: "+path);
                    sprites++;
                }
            }
            foreach(var row in m.animations)
            {
                var seq=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(SeqPath(row.id));var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(!seq || seq.frames.Any(s=>!s) || seq.frames.Length!=row.frames.Length || seq.loop!=row.loop || !clip || Mathf.Abs(clip.length-row.totalMilliseconds/1000f)>.002f || Mathf.Abs(seq.Duration-row.totalMilliseconds/1000f)>.002f || clip.events.Length!=0)throw new Exception("Clip mismatch: "+row.id);
                for(int i=0;i<row.frames.Length;i++)if(Mathf.Abs(seq.durations[i]-row.frames[i].durationMilliseconds/1000f)>.00001f)throw new Exception("Frame duration mismatch: "+row.id);
            }
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs/Devices"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(go.GetComponent<DeviceVisualView>().states.Any(s=>s.sequence && !s.clip))throw new Exception("Missing authored clip reference: "+path);
                if(go.GetComponentsInChildren<Collider2D>(true).Length!=0 || go.GetComponentsInChildren<Collider>(true).Length!=0 || go.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>!r.sprite) || go.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Prefab mismatch: "+path);
            }
            return $"Validated {sprites} sprites, {m.animations.Length} exact-duration clips and all device prefabs";
        }
        public static string Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);var controls=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();var cam=controls.qaCamera;
            string dir="Screenshots/DeviceArt";Directory.CreateDirectory(dir);
            for(int panel=0;panel<4;panel++)for(int pose=0;pose<3;pose++)
            {
                foreach(var view in controls.views){view.ResetVisual();if(pose>0){var s=view.states.FirstOrDefault(a=>a.sequence);if(s!=null)view.Advance(s.sequence.Duration*(pose==1?.5f:1.2f));}}
                cam.transform.position=new Vector3(panel*24,0,-10);var rt=new RenderTexture(1080,1920,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var png=new Texture2D(1080,1920,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1080,1920),0,0);png.Apply();File.WriteAllBytes($"{dir}/panel_{panel}_pose_{pose}.png",png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
            }
            // Integer pixel magnification at 80 PPU: 1920/(2*12)=80 screen pixels per world cell.
            for(int scale=1;scale<=4;scale*=2)for(int panel=0;panel<4;panel++)
            {
                cam.orthographicSize=12f/scale;cam.transform.position=new Vector3(panel*24+(panel==3?7:0),panel==3?8:10,-10);
                var rt=new RenderTexture(1920,1920,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var png=new Texture2D(1920,1920,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1920,1920),0,0);png.Apply();File.WriteAllBytes($"{dir}/panel_{panel}_{scale}x.png",png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
            }
            // Isolate all bridge lengths on each background, both axes, without viewport edge clipping.
            var bridgeViews=controls.views.Where(v=>v.name.StartsWith("Bridge_")).ToArray();
            foreach(var v in controls.views)v.gameObject.SetActive(bridgeViews.Contains(v));
            for(int background=0;background<3;background++)for(int axis=0;axis<2;axis++)for(int length=1;length<=3;length++)
            {
                var group=bridgeViews.Where(v=>v.name.StartsWith($"Bridge_{axis}_{length}_")).OrderBy(v=>v.name).ToArray();
                foreach(var v in bridgeViews)v.gameObject.SetActive(group.Contains(v));
                for(int i=0;i<group.Length;i++){group[i].transform.position=new Vector3(background*24+(axis==0?i:0),axis==1?i:0,0);group[i].ResetVisual();group[i].Advance(1.2f);}
                cam.orthographicSize=3;cam.transform.position=new Vector3(background*24+(axis==0?(length-1)*.5f:0),axis==1?(length-1)*.5f:0,-10);
                var rt=new RenderTexture(1920,1920,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var png=new Texture2D(1920,1920,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1920,1920),0,0);png.Apply();File.WriteAllBytes($"{dir}/bridge_bg_{background}_axis_{axis}_length_{length}_4x.png",png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
            }
            EditorSceneManager.OpenScene(ScenePath);PortraitGameView.Use1080x1920();return "42 captures: 12 portrait poses, 12 integer 1x/2x/4x details, 18 bridge H/V 1/2/3-cell background details";
        }
    }
}
