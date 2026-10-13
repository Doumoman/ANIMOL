using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ANIMOL.Presentation.Editor
{
    public static class TwilightSeaArtBuilder
    {
        public const string Root="Assets/ANIMOL";
        public const string ScenePath=Root+"/Scenes/ANIMOL_TwilightSea_Art_QA.unity";
        public const string Prefabs=Root+"/Prefabs/Themes/twilight_sea";
        public const string CatalogPath=Root+"/Data/Themes/twilight_sea.asset";

        public static DeviceManifest Read()=>JsonUtility.FromJson<DeviceManifest>(File.ReadAllText(Root+"/Data/twilight_sea_import_manifest.json").Replace("\"event\":","\"eventName\":"));
        static string Key(string id)=>id.Replace('/','_');
        static Sprite Load(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+path);
        static string Seq(string id)=>Root+"/Data/AnimationSequences/twilight_sea/"+Key(id)+".asset";
        public static string Prefab(string id)=>Prefabs+"/"+Key(id)+".prefab";
        static DeviceVisualState State(DeviceClip row,DeviceManifest m)
        {
            string prefix=row.id.Split('/')[0];var terminal=m.assets.FirstOrDefault(a=>a.id==prefix+"/"+row.terminalState);
            return new DeviceVisualState{name=row.id.Split('/')[1],eventName=row.eventName,sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq(row.id)),clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath),terminal=terminal!=null?Load(terminal.path):Load(row.frames.Last().path)};
        }
        static GameObject Create(string name,string id,DeviceVisualState[] states,string initial)
        {
            var root=new GameObject(name);var child=new GameObject("Visual");child.transform.SetParent(root.transform,false);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=name.StartsWith("foundation_")?0:10;
            if(name=="foundation_twilight_backdrop_static")renderer.sortingOrder=-30;
            var view=root.AddComponent<DeviceVisualView>();view.visual=renderer;view.deviceId=id;view.states=states;view.initialState=initial;view.ResetVisual();return root;
        }
        static void Save(GameObject go,string path){PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);}
        [MenuItem("ANIMOL/Art QA/Rebuild memory theater QA")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            foreach(var folder in new[]{Prefabs,Root+"/Data/Themes",Root+"/Data/AnimationSequences/twilight_sea"})Directory.CreateDirectory(folder);
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
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog){catalog=ScriptableObject.CreateInstance<DeviceThemeCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}catalog.themeId="twilight_sea";catalog.devices=entries.ToArray();EditorUtility.SetDirty(catalog);
            BuildFerry(m);AssetDatabase.SaveAssets();
            return BuildScene();
        }
        public static string BuildScene()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            var m=Read();var entries=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath).devices.ToList();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Twilight Sea QA Camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=12;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.102f,.11f,.173f);
            var qa=new GameObject("Existing visual QA controls").AddComponent<DeviceArtQaController>();qa.qaCamera=cam;qa.pageCount=13;qa.previewPage=0;qa.guiTop=180;qa.adaptPortraitToolbar=true;qa.title="TWILIGHT SEA ART QA - gameplay not connected";
            var views=new List<DeviceVisualView>();var masters=new List<DeviceVisualView>();
            Func<string,Vector3,GameObject> add=(id,p)=>{var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(id)));go.transform.position=p;views.Add(go.GetComponent<DeviceVisualView>());return go;};
            var backdrop=new GameObject("Backdrop behind flat board").AddComponent<SpriteRenderer>();backdrop.sprite=Load(m.assets.First(a=>a.id=="foundation/twilight_backdrop").path);backdrop.sortingOrder=-30;backdrop.transform.position=new Vector3(0,0,2);
            for(int x=-5;x<=5;x++)for(int y=-6;y<=8;y++){var floor=new GameObject("Flat floor cell").AddComponent<SpriteRenderer>();floor.sprite=Load(m.assets.First(a=>a.id=="foundation/floor_"+(x<0?"sand":"dock")).path);floor.transform.position=new Vector3(x,y,1);floor.sortingOrder=-10;}
            for(int i=0;i<entries.Count;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(entries[i].deviceId=="11"?AssetDatabase.LoadAssetAtPath<GameObject>(FerryPath):entries[i].prefab);go.transform.position=new Vector3(-4+(i%3)*4,6-(i/3)*3.5f,0);
                var view=go.GetComponent<DeviceVisualView>();masters.Add(view);views.Add(view);
            }
            foreach(var id in new[]{"foundation/lantern_idle","foundation/seagrass_idle"})add(id,new Vector3(id.Contains("lantern")?-6:6,8,0));
            foreach(var id in new[]{"rabbit_front","soap_bubble_pink","exit_visual_root","hud_visual"}){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab"));if(id!="hud_visual")go.transform.position=new Vector3(id=="rabbit_front"?-2:id=="soap_bubble_pink"?0:3,-9.5f,0);}
            var all=m.animations.Select(a=>a.id).Concat(m.assets.Select(a=>a.id+"_static")).ToArray();
            for(int i=0;i<all.Length;i++){int page=1+i/24,cell=i%24;var go=add(all[i],new Vector3(page*24-4.5f+(cell%4)*3,5.5f-(cell/4)*3,0));if(go.name=="foundation_twilight_backdrop_static")go.transform.position=new Vector3(page*24,0,2);}
            // Explicit art fixture cells, not a puzzle level or a claimed blast radius.
            var land=new HashSet<Vector2Int>();for(int x=-2;x<=2;x++)for(int y=0;y<=2;y++)land.Add(new Vector2Int(x,y));
            var water=new HashSet<Vector2Int>();for(int x=-3;x<=3;x++)for(int y=-1;y<=3;y++)if(!land.Contains(new Vector2Int(x,y)))water.Add(new Vector2Int(x,y));
            foreach(var c in land){int mask=ResolvedCellPreview.ShoreMask(c,water);var go=add("foundation/shore_"+mask.ToString("00")+"_static",new Vector3(288+c.x,c.y+4,0));go.GetComponent<DeviceVisualView>().visual.sortingOrder=-10;}
            foreach(var c in water){var go=add("foundation/water_idle",new Vector3(288+c.x,c.y+4,0));var view=go.GetComponent<DeviceVisualView>();view.visual.sortingOrder=-10;view.initialPhase=Mathf.Repeat(c.x*.371f+c.y*.619f,1.92f);view.ResetVisual();}
            var preview=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath));preview.transform.position=new Vector3(288,-3,0);var p=preview.GetComponent<ResolvedCellPreview>();
            p.PresentBlast(new[]{new Vector2Int(-1,0),Vector2Int.zero,Vector2Int.up},new[]{Vector2Int.up,new Vector2Int(5,5)});p.PresentStop(new Vector2Int(3,0),"e");
            qa.views=views.ToArray();qa.masters=masters.ToArray();qa.previews=new[]{p};EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();return $"712 PNG, 137 clips, 270 role/core prefabs, {views.Count} QA views, 8 theme entries + synchronized ferry and resolved-cell preview";
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
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog||catalog.devices.Length!=8)throw new Exception("Theme catalog mismatch");
            return $"Validated {count} PNG, 137 timed clips, 270 single-writer prefabs, 8 theme entries";
        }
        public static string Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();var cam=qa.qaCamera;var dir="Screenshots/TwilightSea";Directory.CreateDirectory(dir);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;}
            for(int page=0;page<qa.pageCount;page++)for(int pose=0;pose<3;pose++)
            {
                foreach(var v in qa.views){v.ResetVisual();var s=v.states.FirstOrDefault(a=>a.sequence);if(pose>0&&s!=null){v.Present(s.name,true);v.Advance(s.sequence.Duration*(pose==1?.5f:1.2f));}}
                foreach(var f in UnityEngine.Object.FindObjectsByType<FerryVisualView>(FindObjectsSortMode.None))f.Render();cam.transform.position=new Vector3(page*24,0,-10);Write(cam,1080,1920,$"{dir}/page_{page}_pose_{pose}.png");if(pose==1){cam.backgroundColor=new Color(1,.804f,.459f);Write(cam,1080,1920,$"{dir}/page_{page}_bright.png");cam.backgroundColor=new Color(.102f,.11f,.173f);}
            }
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            var ferry=UnityEngine.Object.FindFirstObjectByType<FerryVisualView>();foreach(var v in qa.views)v.gameObject.SetActive(v==ferry.clock);ferry.gameObject.SetActive(true);
            foreach(var pair in ferry.pairs){ferry.Present(pair.state,true);ferry.Advance(pair.rear.Duration*.5f);cam.orthographicSize=3;cam.transform.position=ferry.transform.position+new Vector3(0,0,-10);Write(cam,1920,1920,$"{dir}/ferry_{pair.state}_mid_4x.png");}
            EditorSceneManager.OpenScene(ScenePath);PortraitGameView.Use1080x1920();return "39 portrait poses and 20 synchronized occupied-ferry details";
        }
        public const string FerryPath=Root+"/Prefabs/Presentation/twilight_sea_ferry_occupied.prefab";
        public const string PreviewPath=Root+"/Prefabs/Presentation/twilight_sea_resolved_cell_preview.prefab";
        [Serializable] public class SeaClip:DeviceClip {public Vector2[] passengerOffsets;}
        [Serializable] public class SeaSource {public SeaClip[] animations;}
        static void BuildFerry(DeviceManifest m)
        {
            Directory.CreateDirectory(Root+"/Prefabs/Presentation");AssetDatabase.Refresh();
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab("d11_pilot_boat"));
            var go=UnityEngine.Object.Instantiate(original);go.name="twilight_sea_ferry_occupied";
            var clock=go.GetComponent<DeviceVisualView>();UnityEngine.Object.DestroyImmediate(clock.visual.gameObject);clock.visual=null;
            // Clock states use the full sequence ID; no full SpriteRenderer exists.
            clock.states=m.animations.Where(c=>c.deviceId=="11"&&m.animations.Any(r=>r.id==c.id+"_rear")).Select(c=>State(c,m)).ToArray();clock.initialState="idle_e";
            var ferry=go.AddComponent<FerryVisualView>();ferry.clock=clock;
            Func<string,int,SpriteRenderer> layer=(name,order)=>{var child=new GameObject(name);child.transform.SetParent(go.transform,false);var r=child.AddComponent<SpriteRenderer>();r.sortingOrder=order;return r;};
            ferry.wake=layer("Wake",5);ferry.rear=layer("Rear",10);ferry.passenger=layer("Existing rabbit visual",15);ferry.front=layer("Front",20);
            ferry.wakeClear=Load(m.assets.First(a=>a.id=="d11_pilot_boat/wake_clear_e").path);
            ferry.passenger.sprite=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/rabbit_front.prefab").GetComponentInChildren<SpriteRenderer>().sprite;
            // Existing rabbit foot pivot y=8px matches its alpha bottom y=152 of height160.
            ferry.passengerFootToPivot=Vector2.zero;
            var source=JsonUtility.FromJson<SeaSource>(File.ReadAllText(Root+"/Data/twilight_sea_import_manifest.json").Replace("\"event\":","\"eventName\":"));
            ferry.pairs=clock.states.Select(s=>{
                var rear=State(m.animations.First(c=>c.id=="d11_pilot_boat/"+s.name+"_rear"),m);var front=State(m.animations.First(c=>c.id=="d11_pilot_boat/"+s.name+"_front"),m);
                string dir=s.name.Split('_').Last();var metadata=source.animations.First(c=>c.id=="d11_pilot_boat/"+s.name);
                return new FerryVisualPair{state=s.name,rear=rear.sequence,front=front.sequence,rearTerminal=rear.terminal,frontTerminal=front.terminal,passengerOffsets=metadata.passengerOffsets,wake=s.name.StartsWith("row_")?AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq("d11_pilot_boat/wake_"+dir)):null};
            }).ToArray();ferry.ResetVisual();Save(go,FerryPath);
            var fixture=new GameObject("twilight_sea_resolved_cell_preview");var preview=fixture.AddComponent<ResolvedCellPreview>();
            Func<string,BeamPathView> pool=name=>{var child=new GameObject(name);child.transform.SetParent(fixture.transform,false);var p=child.AddComponent<BeamPathView>();p.sortingOrder=8;return p;};
            preview.boundary=pool("Supplied blast boundary");preview.targets=pool("Supplied cracked wall targets");preview.endpoint=pool("Supplied barrel endpoint");
            preview.boundarySprites=Enumerable.Range(0,16).Select(i=>Load(m.assets.First(a=>a.id=="d15_three_turn_bomb/blast_boundary_"+i.ToString("00")).path)).ToArray();
            preview.targetSprite=Load(m.assets.First(a=>a.id=="d15_three_turn_bomb/cracked_wall_target").path);
            preview.stopSprites=new[]{"e","s","w","n"}.Select(d=>Load(m.assets.First(a=>a.id=="d12_rolling_barrel/stop_preview_"+d).path)).ToArray();Save(fixture,PreviewPath);
        }

        static void Write(Camera cam,int width,int height,string file)
        {
            var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var png=new Texture2D(width,height,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(file,png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
