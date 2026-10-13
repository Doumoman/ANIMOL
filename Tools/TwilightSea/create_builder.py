from pathlib import Path
s=Path('Assets/ANIMOL/Scripts/Editor/MemoryTheaterArtBuilder.cs').read_text(encoding='utf8')
s=s.replace('MemoryTheater','TwilightSea').replace('memory_theater','twilight_sea').replace('Memory Theater','Twilight Sea').replace('MEMORY THEATER','TWILIGHT SEA')
s=s.replace('        public const string DicePath=Root+"/Data/Themes/twilight_sea_dice.asset";','')
start=s.index('            var dice=');end=s.index('            return BuildScene();',start)
s=s[:start]+'            BuildFerry(m);AssetDatabase.SaveAssets();\n'+s[end:]
start=s.index('            var scene=EditorSceneManager.NewScene');end=s.index('        public static string Validate()',start)
scene='''            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
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
'''
s=s[:start]+scene+s[end:]
s=s.replace('foundation_theater_backdrop_static','foundation_twilight_backdrop_static')
s=s.replace('renderer.sortingOrder=10;','renderer.sortingOrder=name.StartsWith("foundation_")?0:10;')
s=s.replace('catalog.devices.Length!=11','catalog.devices.Length!=8').replace('211 timed clips, 365 single-writer prefabs, 11 theme entries','137 timed clips, 270 single-writer prefabs, 8 theme entries')
start=s.index('            var book=');end=s.index('            EditorSceneManager.OpenScene(ScenePath);PortraitGameView',start)
s=s[:start]+'''            var ferry=UnityEngine.Object.FindFirstObjectByType<FerryVisualView>();foreach(var v in qa.views)v.gameObject.SetActive(v==ferry.clock);ferry.gameObject.SetActive(true);
            foreach(var pair in ferry.pairs){ferry.Present(pair.state,true);ferry.Advance(pair.rear.Duration*.5f);cam.orthographicSize=3;cam.transform.position=ferry.transform.position+new Vector3(0,0,-10);Write(cam,1920,1920,$"{dir}/ferry_{pair.state}_mid_4x.png");}
'''+s[end:]
s=s.replace('48 portrait poses and 8 full-canvas bookshelf mid-rotation details','39 portrait poses and 20 synchronized occupied-ferry details')
# Drive the single ferry clock when posing all gallery views.
s=s.replace('cam.transform.position=new Vector3(page*24,0,-10);Write','foreach(var f in UnityEngine.Object.FindObjectsByType<FerryVisualView>(FindObjectsSortMode.None))f.Render();cam.transform.position=new Vector3(page*24,0,-10);Write')
s=s.replace('Write(cam,1080,1920,$"{dir}/page_{page}_pose_{pose}.png");','Write(cam,1080,1920,$"{dir}/page_{page}_pose_{pose}.png");if(pose==1){cam.backgroundColor=new Color(1,.804f,.459f);Write(cam,1080,1920,$"{dir}/page_{page}_bright.png");cam.backgroundColor=new Color(.102f,.11f,.173f);}')
extra=Path('Tools/TwilightSea/builder_extras.txt').read_text(encoding='utf8')
s=s.replace('        static void Write(Camera cam',extra+'\n        static void Write(Camera cam')
Path('Assets/ANIMOL/Scripts/Editor/TwilightSeaArtBuilder.cs').write_text(s,encoding='utf8')
print('Unity builder authored')
