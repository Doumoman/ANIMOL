import pathlib
root=pathlib.Path.cwd();s=(root/'Assets/ANIMOL/Scripts/Editor/MemoryTheaterArtBuilder.cs').read_text(encoding='utf8')
s=s.replace('MemoryTheaterArtBuilder','LetterObservatoryArtBuilder').replace('MemoryTheater','LetterObservatory').replace('memory_theater','letter_observatory').replace('Memory Theater','Letter Observatory')
s=s.replace('Rebuild memory theater QA','Rebuild letter observatory QA')
s=s.replace('public const string DicePath=Root+"/Data/Themes/letter_observatory_dice.asset";','public const string RulesPath=Root+"/Data/Themes/letter_observatory_optics.asset";')
s=s.replace('renderer.sortingOrder=10;','renderer.sortingOrder=id=="21"?10:20;').replace('foundation_theater_backdrop_static','foundation_observatory_backdrop_static')
s=s.replace('renderer.sortingOrder=id=="21"?10:20;','renderer.sortingOrder=id=="21"?10:id=="10"&&name.Contains("_beam_")?15:20;')
s=s.replace('var view=go.GetComponent<DeviceVisualView>();view.states=states;view.initialState=initial;view.ResetVisual();','var view=go.GetComponent<DeviceVisualView>();view.states=states;view.initialState=initial;view.visual.sortingOrder=20;view.ResetVisual();')
s=s.replace('string initial=id=="22"?"t1_n5_e3":m.assets.First(a=>a.deviceId==id).id.Split(\'/\')[1];','string initial=m.assets.First(a=>a.deviceId==id).id.Split(\'/\')[1];')
start=s.index('            var dice=');end=s.index('            return BuildScene();',start)
s=s[:start]+'            BuildRules(m);AssetDatabase.SaveAssets();\n'+s[end:]
start=s.index('        public static string BuildScene()');end=s.index('        public static string Validate()',start)
scene='''        [Serializable] class Travel {public string e,s,w,n;public string Get(string d)=>d=="e"?e:d=="s"?s:d=="w"?w:n;}
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
'''
s=s[:start]+scene+s[end:]
s=s.replace('catalog.devices.Length!=11','catalog.devices.Length!=9').replace('211 timed clips, 365 single-writer prefabs, 11 theme entries','120 timed clips, 247 single-writer prefabs, 9 theme entries')
start=s.index('            var book=');end=s.index('            EditorSceneManager.OpenScene(ScenePath);PortraitGameView',start)
s=s[:start]+'''            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            foreach(var v in qa.views)v.gameObject.SetActive(false);
            var glass=qa.masters.First(v=>v.deviceId=="21");glass.gameObject.SetActive(true);
            foreach(var state in glass.states.Where(s=>s.name.StartsWith("break_"))){glass.Present(state.name,true);glass.Advance(state.sequence.Duration*.5f);cam.orthographicSize=3;cam.transform.position=glass.transform.position+new Vector3(0,0,-10);Write(cam,1920,1920,$"{dir}/glass_{state.name}_mid_4x.png");}
'''+s[end:]
s=s.replace('48 portrait poses and 8 full-canvas bookshelf mid-rotation details','36 portrait poses and glass full-canvas break details').replace('Screenshots/MemoryTheater','Screenshots/LetterObservatory')
(root/'Assets/ANIMOL/Scripts/Editor/LetterObservatoryArtBuilder.cs').write_text(s,encoding='utf8')
print('Wrote observatory Unity authoring builder using existing presentation types')
