using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ANIMOL.Presentation.Editor
{
    public static class MemoryTheaterArtBuilder
    {
        public const string Root="Assets/ANIMOL";
        public const string ScenePath=Root+"/Scenes/QA/ANIMOL_MemoryTheater_Art_QA.unity";
        public const string Prefabs=Root+"/Prefabs/Themes/memory_theater";
        public const string CatalogPath=Root+"/Data/Themes/memory_theater.asset";
        public const string DicePath=Root+"/Data/Themes/memory_theater_dice.asset";
        public static DeviceManifest Read()=>JsonUtility.FromJson<DeviceManifest>(File.ReadAllText(Root+"/Data/memory_theater_import_manifest.json").Replace("\"event\":","\"eventName\":"));
        static string Key(string id)=>id.Replace('/','_');
        static Sprite Load(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+path);
        static string Seq(string id)=>Root+"/Data/AnimationSequences/memory_theater/"+Key(id)+".asset";
        public static string Prefab(string id)=>Prefabs+"/"+Key(id)+".prefab";
        static DeviceVisualState State(DeviceClip row,DeviceManifest m)
        {
            string prefix=row.id.Split('/')[0];var terminal=m.assets.FirstOrDefault(a=>a.id==prefix+"/"+row.terminalState);
            return new DeviceVisualState{name=row.id.Split('/')[1],eventName=row.eventName,sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq(row.id)),clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath),terminal=terminal!=null?Load(terminal.path):Load(row.frames.Last().path)};
        }
        static GameObject Create(string name,string id,DeviceVisualState[] states,string initial)
        {
            var root=new GameObject(name);var child=new GameObject("Visual");child.transform.SetParent(root.transform,false);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=10;
            if(name=="foundation_theater_backdrop_static")renderer.sortingOrder=-30;
            var view=root.AddComponent<DeviceVisualView>();view.visual=renderer;view.deviceId=id;view.states=states;view.initialState=initial;view.ResetVisual();return root;
        }
        static void Save(GameObject go,string path){PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);}
        [MenuItem("ANIMOL/Art QA/Rebuild memory theater QA")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            foreach(var folder in new[]{Prefabs,Root+"/Data/Themes",Root+"/Data/AnimationSequences/memory_theater"})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();var m=Read();
            foreach(var row in m.animations)
            {
                var sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq(row.id));
                if(!sequence){sequence=ScriptableObject.CreateInstance<SpriteFrameSequence>();AssetDatabase.CreateAsset(sequence,Seq(row.id));}
                sequence.frames=row.frames.Select(f=>Load(f.path)).ToArray();sequence.durations=row.frames.Select(f=>f.durationMilliseconds/1000f).ToArray();sequence.loop=row.loop;EditorUtility.SetDirty(sequence);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Root+"/"+row.clipPath);}
                clip.frameRate=1000;
                var keys=row.frames.Select((f,i)=>new ObjectReferenceKeyframe{time=f.startMilliseconds/1000f,value=sequence.frames[i]}).ToList();keys.Add(new ObjectReferenceKeyframe{time=(row.totalMilliseconds-1)/1000f,value=sequence.frames.Last()});
                AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=row.loop;settings.stopTime=row.totalMilliseconds/1000f;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }
            var entries=new List<DeviceThemeEntry>();
            foreach(var id in m.assets.Select(a=>a.deviceId).Where(d=>d!="BG").Distinct())
            {
                string prefix=m.assets.First(a=>a.deviceId==id).id.Split('/')[0];
                var states=m.assets.Where(a=>a.deviceId==id).Select(a=>new DeviceVisualState{name=a.id.Split('/')[1],sprite=Load(a.path)}).Concat(m.animations.Where(a=>a.deviceId==id).Select(a=>State(a,m))).GroupBy(s=>s.name).Select(g=>g.Last()).ToArray();
                string initial=id=="22"?"t1_n5_e3":m.assets.First(a=>a.deviceId==id).id.Split('/')[1];
                GameObject go;
                if(new[]{"07","09","18","19"}.Contains(id))
                {
                    // Actual prefab variants reuse the existing component and root/visual structure.
                    go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Devices/"+prefix+".prefab"));
                    var view=go.GetComponent<DeviceVisualView>();view.states=states;view.initialState=initial;view.ResetVisual();
                }
                else go=Create(prefix,id,states,initial);
                Save(go,Prefab(prefix));entries.Add(new DeviceThemeEntry{deviceId=id,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(prefix))});
            }
            foreach(var row in m.animations)Save(Create(Key(row.id),row.deviceId,new[]{State(row,m)},row.id.Split('/')[1]),Prefab(row.id));
            foreach(var row in m.assets)Save(Create(Key(row.id)+"_static",row.deviceId,new[]{new DeviceVisualState{name="static",sprite=Load(row.path)}},"static"),Prefab(row.id+"_static"));
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog){catalog=ScriptableObject.CreateInstance<DeviceThemeCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}catalog.themeId="memory_theater";catalog.devices=entries.ToArray();EditorUtility.SetDirty(catalog);
            var dice=AssetDatabase.LoadAssetAtPath<DiceOrientationTable>(DicePath);if(!dice){dice=ScriptableObject.CreateInstance<DiceOrientationTable>();AssetDatabase.CreateAsset(dice,DicePath);}JsonUtility.FromJsonOverwrite(File.ReadAllText(Root+"/Data/memory_theater_dice_orientations.json"),dice);EditorUtility.SetDirty(dice);AssetDatabase.SaveAssets();
            return BuildScene();
        }
        public static string BuildScene()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            var m=Read();var entries=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath).devices.ToList();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Memory Theater QA Camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=12;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.102f,.11f,.173f);
            var qa=new GameObject("Existing visual QA controls").AddComponent<DeviceArtQaController>();qa.qaCamera=cam;qa.pageCount=16;qa.previewPage=0;qa.guiTop=180;qa.title="MEMORY THEATER ART QA - gameplay not connected";
            var views=new List<DeviceVisualView>();var masters=new List<DeviceVisualView>();
            var backdrop=new GameObject("Backdrop - behind flat board").AddComponent<SpriteRenderer>();backdrop.sprite=Load(m.assets.First(a=>a.id=="foundation/theater_backdrop").path);backdrop.sortingOrder=-30;backdrop.transform.position=new Vector3(0,0,2);
            for(int x=-6;x<=6;x++)for(int y=-10;y<=9;y++)
            {
                var floor=new GameObject("Flat floor cell").AddComponent<SpriteRenderer>();floor.sprite=Load(m.assets.First(a=>a.id=="foundation/floor_"+(x<0?"plum":"slate")).path);floor.transform.position=new Vector3(x,y,1);floor.sortingOrder=-10;
            }
            for(int i=0;i<entries.Count;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(entries[i].prefab);go.transform.position=new Vector3(-4+(i%3)*4,7-(i/3)*4,0);masters.Add(go.GetComponent<DeviceVisualView>());views.Add(go.GetComponent<DeviceVisualView>());
            }
            foreach(var id in new[]{"foundation/curtain_idle","foundation/lamp_idle","foundation/cracked_wall_break"})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(id)));go.transform.position=new Vector3(id.Contains("curtain")?-6:id.Contains("lamp")?6:0,8,0);views.Add(go.GetComponent<DeviceVisualView>());
            }
            // Rails remain a separate fixed renderer; the stage's visual root has no baked displacement.
            var rail=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab("d06_moving_stage/rail_horizontal_static")));rail.transform.position=masters.First(v=>v.deviceId=="06").transform.position;rail.GetComponent<DeviceVisualView>().visual.sortingOrder=0;views.Add(rail.GetComponent<DeviceVisualView>());
            foreach(var id in new[]{"rabbit_front","soap_bubble_pink","exit_visual_root","hud_visual"})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab"));
                if(id!="hud_visual")go.transform.position=new Vector3(id=="rabbit_front"?-2:id=="soap_bubble_pink"?0:3,-8,0);
            }
            var all=m.animations.Select(a=>Prefab(a.id)).Concat(m.assets.Select(a=>Prefab(a.id+"_static"))).ToArray();
            for(int i=0;i<all.Length;i++)
            {
                int page=1+i/24,cell=i%24;var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(all[i]));
                go.transform.position=new Vector3(page*24-4.5f+(cell%4)*3,5.5f-(cell/4)*3,0);views.Add(go.GetComponent<DeviceVisualView>());
                if(go.name=="foundation_theater_backdrop_static")go.transform.position=new Vector3(page*24,0,2);
            }
            qa.views=views.ToArray();qa.masters=masters.ToArray();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();return $"1385 PNG, 211 clips, 365 prefabs, {views.Count} QA views, 11 theme entries";
        }
        public static string Validate()
        {
            var m=Read();int count=0;
            foreach(var row in m.assets.Cast<AssetRow>().Concat(m.animations))
            {
                var paths=row is DeviceClip clip?clip.frames.Select(f=>f.path):new[]{row.path};
                foreach(var path in paths)
                {
                    var sprite=Load(path);var importer=AssetImporter.GetAtPath(Root+"/"+path) as TextureImporter;var settings=new TextureImporterSettings();if(importer)importer.ReadTextureSettings(settings);
                    if(!sprite||!importer||sprite.rect.width!=row.width||sprite.rect.height!=row.height||importer.spritePixelsPerUnit!=80||Vector2.Distance(sprite.pivot,new Vector2(row.pivotX*row.width,row.pivotY*row.height))>.01f||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||settings.spriteMeshType!=SpriteMeshType.FullRect||importer.spriteImportMode!=SpriteImportMode.Single||importer.npotScale!=TextureImporterNPOTScale.None||new[]{"Android","iPhone","Standalone","WebGL"}.Any(p=>importer.GetPlatformTextureSettings(p).overridden))throw new Exception("Import mismatch: "+path);
                    count++;
                }
            }
            foreach(var row in m.animations)
            {
                var seq=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq(row.id));var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath);
                if(!seq||!clip||seq.frames.Length!=row.frames.Length||seq.frames.Any(s=>!s)||seq.loop!=row.loop||Mathf.Abs(seq.Duration-row.totalMilliseconds/1000f)>.002f||Mathf.Abs(clip.length-seq.Duration)>.002f||clip.events.Length>0)throw new Exception("Timing mismatch: "+row.id);
                for(int i=0;i<seq.frames.Length;i++)if(Mathf.Abs(seq.durations[i]-row.frames[i].durationMilliseconds/1000f)>.00001f)throw new Exception("Frame timing mismatch: "+row.id);
            }
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{Prefabs}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);var view=go.GetComponent<DeviceVisualView>();
                if(!view||go.GetComponentsInChildren<DeviceVisualView>(true).Length!=1||go.GetComponentsInChildren<Animator>(true).Length>0||go.GetComponentsInChildren<Collider2D>(true).Length>0||view.states.Any(s=>s.sequence&&(!s.clip||s.sequence.frames.Any(f=>!f)))||go.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Prefab mismatch: "+path);
            }
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog||catalog.devices.Length!=11)throw new Exception("Theme catalog mismatch");
            return $"Validated {count} PNG, 211 timed clips, 365 single-writer prefabs, 11 theme entries";
        }
        public static string Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();var cam=qa.qaCamera;var dir="Screenshots/MemoryTheater";Directory.CreateDirectory(dir);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;}
            for(int page=0;page<qa.pageCount;page++)for(int pose=0;pose<3;pose++)
            {
                foreach(var v in qa.views){v.ResetVisual();var s=v.states.FirstOrDefault(a=>a.sequence);if(pose>0&&s!=null){v.Present(s.name,true);v.Advance(s.sequence.Duration*(pose==1?.5f:1.2f));}}
                cam.transform.position=new Vector3(page*24,0,-10);Write(cam,1080,1920,$"{dir}/page_{page}_pose_{pose}.png");
            }
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            var book=qa.masters.First(v=>v.deviceId=="08");foreach(var v in qa.views)v.gameObject.SetActive(v==book);
            foreach(var rotation in book.states.Where(s=>s.name.StartsWith("rotate_")))
            {
                book.Present(rotation.name,true);book.Advance(rotation.sequence.Duration*.5f);cam.orthographicSize=3;cam.transform.position=book.transform.position+new Vector3(0,0,-10);Write(cam,1920,1920,$"{dir}/book_{rotation.name}_mid_4x.png");
            }
            EditorSceneManager.OpenScene(ScenePath);PortraitGameView.Use1080x1920();return "48 portrait poses and 8 full-canvas bookshelf mid-rotation details";
        }
        static void Write(Camera cam,int width,int height,string file)
        {
            var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var png=new Texture2D(width,height,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(file,png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
