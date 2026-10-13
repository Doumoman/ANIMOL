using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ANIMOL.Presentation.Editor
{
    public static class LetterObservatoryArtBuilder
    {
        public const string Root="Assets/ANIMOL";
        public const string ScenePath=Root+"/Scenes/ANIMOL_LetterObservatory_Art_QA.unity";
        public const string Prefabs=Root+"/Prefabs/Themes/letter_observatory";
        public const string CatalogPath=Root+"/Data/Themes/letter_observatory.asset";
        public const string RulesPath=Root+"/Data/Themes/letter_observatory_optics.asset";
        public static DeviceManifest Read()=>JsonUtility.FromJson<DeviceManifest>(File.ReadAllText(Root+"/Data/letter_observatory_import_manifest.json").Replace("\"event\":","\"eventName\":"));
        static string Key(string id)=>id.Replace('/','_');
        static Sprite Load(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+path);
        static string Seq(string id)=>Root+"/Data/AnimationSequences/letter_observatory/"+Key(id)+".asset";
        public static string Prefab(string id)=>Prefabs+"/"+Key(id)+".prefab";
        static DeviceVisualState State(DeviceClip row,DeviceManifest m)
        {
            string prefix=row.id.Split('/')[0];var terminal=m.assets.FirstOrDefault(a=>a.id==prefix+"/"+row.terminalState);
            return new DeviceVisualState{name=row.id.Split('/')[1],eventName=row.eventName,sequence=AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(Seq(row.id)),clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/"+row.clipPath),terminal=terminal!=null?Load(terminal.path):Load(row.frames.Last().path)};
        }
        static GameObject Create(string name,string id,DeviceVisualState[] states,string initial)
        {
            var root=new GameObject(name);var child=new GameObject("Visual");child.transform.SetParent(root.transform,false);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=id=="21"?10:id=="10"&&name.Contains("_beam_")?15:20;
            if(name=="foundation_observatory_backdrop_static")renderer.sortingOrder=-30;
            var view=root.AddComponent<DeviceVisualView>();view.visual=renderer;view.deviceId=id;view.states=states;view.initialState=initial;view.ResetVisual();return root;
        }
        static void Save(GameObject go,string path){PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);}
        [MenuItem("ANIMOL/Art QA/Rebuild letter observatory QA")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            foreach(var folder in new[]{Prefabs,Root+"/Data/Themes",Root+"/Data/AnimationSequences/letter_observatory"})Directory.CreateDirectory(folder);
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
                string initial=m.assets.First(a=>a.deviceId==id).id.Split('/')[1];
                GameObject go;
                if(new[]{"07","09","18","19"}.Contains(id))
                {
                    // Actual prefab variants reuse the existing component and root/visual structure.
                    go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Devices/"+prefix+".prefab"));
                    var view=go.GetComponent<DeviceVisualView>();view.states=states;view.initialState=initial;view.visual.sortingOrder=20;view.ResetVisual();
                }
                else go=Create(prefix,id,states,initial);
                Save(go,Prefab(prefix));entries.Add(new DeviceThemeEntry{deviceId=id,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(prefix))});
            }
            foreach(var row in m.animations)Save(Create(Key(row.id),row.deviceId,new[]{State(row,m)},row.id.Split('/')[1]),Prefab(row.id));
            foreach(var row in m.assets)Save(Create(Key(row.id)+"_static",row.deviceId,new[]{new DeviceVisualState{name="static",sprite=Load(row.path)}},"static"),Prefab(row.id+"_static"));
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog){catalog=ScriptableObject.CreateInstance<DeviceThemeCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}catalog.themeId="letter_observatory";catalog.devices=entries.ToArray();EditorUtility.SetDirty(catalog);
            BuildRules(m);AssetDatabase.SaveAssets();
            return BuildScene();
        }
        [Serializable] class Travel {public string e,s,w,n;public string Get(string d)=>d=="e"?e:d=="s"?s:d=="w"?w:n;}
        [Serializable] class Mirrors {public Travel slash,backslash;}
        [Serializable] class OpticsData {public Mirrors mirrorReflection;}
        static void BuildRules(DeviceManifest m)
        {
            var raw=JsonUtility.FromJson<OpticsData>(File.ReadAllText(Root+"/Data/letter_observatory_optics_rules.json"));
            var rules=AssetDatabase.LoadAssetAtPath<OpticsPresentationRules>(RulesPath);if(!rules){rules=ScriptableObject.CreateInstance<OpticsPresentationRules>();AssetDatabase.CreateAsset(rules,RulesPath);}
            var list=new List<MirrorVisualRule>();
            foreach(var mirror in new[]{"slash","backslash"})foreach(var incoming in new[]{"e","s","w","n"})
            {
                string outgoing=(mirror=="slash"?raw.mirrorReflection.slash:raw.mirrorReflection.backslash).Get(incoming);
                string opposite=incoming=="e"?"w":incoming=="w"?"e":incoming=="n"?"s":"n";
                string pair=opposite+outgoing;string corner=(pair.Contains("n")?"n":"s")+(pair.Contains("e")?"e":"w");
                list.Add(new MirrorVisualRule{mirror=mirror,incoming=incoming,outgoing=outgoing,corner=Load(m.assets.First(a=>a.id=="d10_optics/beam_"+corner).path)});
            }
            rules.reflections=list.ToArray();rules.opticsMetadata=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/letter_observatory_optics_rules.json");rules.motionMetadata=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/letter_observatory_motion_bindings.json");EditorUtility.SetDirty(rules);
        }
        public static string BuildScene()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play mode and save current scene before authoring.");
            var m=Read();var entries=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath).devices;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Letter Observatory QA Camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=12;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.102f,.11f,.173f);
            var qa=new GameObject("Existing visual QA controls").AddComponent<DeviceArtQaController>();qa.qaCamera=cam;qa.pageCount=12;qa.previewPage=0;qa.guiTop=180;qa.title="LETTER OBSERVATORY ART QA - gameplay not connected";
            var views=new List<DeviceVisualView>();var masters=new List<DeviceVisualView>();
            Func<string,Vector3,GameObject> add=(id,p)=>{var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(id)));go.transform.position=p;views.Add(go.GetComponent<DeviceVisualView>());return go;};
            var backdrop=new GameObject("Backdrop behind flat board").AddComponent<SpriteRenderer>();backdrop.sprite=Load(m.assets.First(a=>a.id=="foundation/observatory_backdrop").path);backdrop.sortingOrder=-30;backdrop.transform.position=new Vector3(0,0,2);
            for(int x=-5;x<=5;x++)for(int y=-6;y<=8;y++){var floor=new GameObject("Flat floor cell").AddComponent<SpriteRenderer>();floor.sprite=Load(m.assets.First(a=>a.id=="foundation/floor_"+(x<0?"navy":"slate")).path);floor.transform.position=new Vector3(x,y,1);floor.sortingOrder=-10;}
            for(int i=0;i<entries.Length;i++){string prefix=m.assets.First(a=>a.deviceId==entries[i].deviceId).id.Split('/')[0];var go=add(prefix,new Vector3(-4+(i%3)*4,7-(i/3)*4,0));masters.Add(go.GetComponent<DeviceVisualView>());}
            add("foundation/envelope_idle",new Vector3(-6,9,0));add("foundation/lamp_idle",new Vector3(6,9,0));add("foundation/telescope_static",new Vector3(-5,-9,0));
            add("d10_optics/source_e_on_static",new Vector3(-5,-5,0));add("d21_glass_wall/intact_square_static",new Vector3(-3,-5,0));add("d10_optics/mirror_slash_static",new Vector3(-1,-5,0));add("d10_optics/sensor_on_static",new Vector3(-1,-3,0));add("d10_optics/light_gate_horizontal_open_static",new Vector3(1,-3,0));
            var beam=new GameObject("Optics art fixture - not a logical trace").AddComponent<BeamPathView>();beam.sprites=m.assets.Where(a=>a.id.StartsWith("d10_optics/beam_")).Select(a=>new BeamVisualSprite{shape=a.id.Split('/')[1],sprite=Load(a.path)}).ToArray();
            beam.Present(new[]{new BeamVisualCell{shape="beam_horizontal",position=new Vector2(-4,-5)},new BeamVisualCell{shape="beam_horizontal",position=new Vector2(-3,-5)},new BeamVisualCell{shape="beam_horizontal",position=new Vector2(-2,-5)},new BeamVisualCell{shape="beam_nw",position=new Vector2(-1,-5)},new BeamVisualCell{shape="beam_vertical",position=new Vector2(-1,-4)},new BeamVisualCell{shape="beam_terminal_n",position=new Vector2(-1,-3)}});
            foreach(var id in new[]{"rabbit_front","soap_bubble_pink","exit_visual_root","hud_visual"}){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab"));if(id!="hud_visual")go.transform.position=new Vector3(id=="rabbit_front"?-2:id=="soap_bubble_pink"?0:3,-9.5f,0);}
            var all=m.animations.Select(a=>a.id).Concat(m.assets.Where(a=>!a.id.Contains("trajectory_")).Select(a=>a.id+"_static")).ToArray();
            for(int i=0;i<all.Length;i++){int page=1+i/24,cell=i%24;var go=add(all[i],new Vector3(page*24-4.5f+(cell%4)*3,5.5f-(cell/4)*3,0));if(go.name=="foundation_observatory_backdrop_static")go.transform.position=new Vector3(page*24,0,2);}
            foreach(var row in m.assets.Where(a=>a.id.Contains("trajectory_"))){string d=row.id.Split('_').Last();Vector2 p=d=="e"?new Vector2(-4,4):d=="w"?new Vector2(4,4):d=="s"?new Vector2(-4,-2):new Vector2(4,-5);add(row.id+"_static",new Vector3(264+p.x,p.y,0));}
            qa.views=views.ToArray();qa.masters=masters.ToArray();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();return $"727 PNG, 120 clips, 247 prefabs, {views.Count} QA views, 9 theme entries, 8 reflection mappings";
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
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(CatalogPath);if(!catalog||catalog.devices.Length!=9)throw new Exception("Theme catalog mismatch");
            return $"Validated {count} PNG, 120 timed clips, 247 single-writer prefabs, 9 theme entries";
        }
        public static string Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>();var cam=qa.qaCamera;var dir="Screenshots/LetterObservatory";Directory.CreateDirectory(dir);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;}
            for(int page=0;page<qa.pageCount;page++)for(int pose=0;pose<3;pose++)
            {
                foreach(var v in qa.views){v.ResetVisual();var s=v.states.FirstOrDefault(a=>a.sequence);if(pose>0&&s!=null){v.Present(s.name,true);v.Advance(s.sequence.Duration*(pose==1?.5f:1.2f));}}
                cam.transform.position=new Vector3(page*24,0,-10);Write(cam,1080,1920,$"{dir}/page_{page}_pose_{pose}.png");
            }
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            foreach(var v in qa.views)v.gameObject.SetActive(false);
            var glass=qa.masters.First(v=>v.deviceId=="21");glass.gameObject.SetActive(true);
            foreach(var state in glass.states.Where(s=>s.name.StartsWith("break_"))){glass.Present(state.name,true);glass.Advance(state.sequence.Duration*.5f);cam.orthographicSize=3;cam.transform.position=glass.transform.position+new Vector3(0,0,-10);Write(cam,1920,1920,$"{dir}/glass_{state.name}_mid_4x.png");}
            EditorSceneManager.OpenScene(ScenePath);PortraitGameView.Use1080x1920();return "36 portrait poses and glass full-canvas break details";
        }
        static void Write(Camera cam,int width,int height,string file)
        {
            var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var png=new Texture2D(width,height,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(file,png.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
