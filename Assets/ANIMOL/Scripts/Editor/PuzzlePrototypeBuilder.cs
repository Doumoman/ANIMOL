using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ANIMOL.Puzzle;
namespace ANIMOL.Presentation.Editor
{
    public static class PuzzlePrototypeBuilder
    {
        public const string Root = "Assets/ANIMOL/Data/Puzzle/Prototypes";
        static readonly Dictionary<int, string> Themes = new Dictionary<int, string> {
            {1,"starway"},{2,"starway"},{3,"letter_observatory"},{4,"memory_theater"},{5,"twilight_sea"},{6,"memory_theater"},
            {7,"starway"},{8,"memory_theater"},{9,"starway"},{10,"letter_observatory"},{11,"twilight_sea"},{12,"twilight_sea"},
            {13,"memory_theater"},{14,"letter_observatory"},{15,"twilight_sea"},{16,"letter_observatory"},{17,"memory_theater"},
            {18,"starway"},{19,"starway"},{20,"starway"},{21,"letter_observatory"},{22,"memory_theater"},{23,"memory_theater"},{24,"starway"} };
        // Every device with art for a theme gets its own playable room in that theme's prototype scene.
        static readonly string[] ThemedNames = { "starway", "memory_theater", "letter_observatory", "twilight_sea" };
        static readonly int[][] ThemedRoles = {
            new[] { 1, 2, 7, 9, 18, 19, 20, 24 },
            new[] { 4, 6, 7, 8, 9, 13, 17, 18, 19, 22, 23 },
            new[] { 3, 7, 9, 10, 14, 16, 18, 19, 21 },
            new[] { 5, 7, 9, 11, 12, 15, 18, 19 }
        };
        static readonly Dictionary<string,PuzzleTheme> themeAssets = new Dictionary<string,PuzzleTheme>();
        static readonly Dictionary<string,DeviceManifest> manifests = new Dictionary<string,DeviceManifest>();
        static GameObject beam;
        static T Save<T>(T asset, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) { EditorUtility.CopySerialized(asset,existing); UnityEngine.Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static Vector2Int P(int x,int y) => new Vector2Int(x,y);
        static PrototypeDevice D(PuzzleMap m, PrototypeRole role, string id,int x,int y,string variant=null,Vector2Int? direction=null)
        {
            var d = new PrototypeDevice { role=role,id=id,position=P(x,y),variant=variant,direction=direction??Vector2Int.right,artTheme=m.scenario.themeId };
            m.scenario.devices.Add(d); return d;
        }
        static void Goal(PuzzleMap m,string id,PrototypeGoalKind kind,Vector2Int cell=default,int number=0) => m.scenario.goals.Add(new PrototypeGoal { deviceId=id,kind=kind,cell=cell,number=number });
        static PuzzleMap Room(int role)
        {
            var m=ScriptableObject.CreateInstance<PuzzleMap>(); m.Resize(9,7); m.theme=themeAssets[Themes[role]];
            m.scenario=ScriptableObject.CreateInstance<PuzzleScenario>();m.scenario.themeId=Themes[role];m.scenario.title=((PrototypeRole)role).ToString();
            m.start=P(1,2);m.exit=P(7,2);m.targetBubbles=1;m.bubbles.Add(m.exit);
            for(int y=1;y<=5;y++)for(int x=1;x<=7;x++)m.SetTerrain(P(x,y),TerrainMaterial.Grass);
            var r=(PrototypeRole)role; string route="EEEEEE";
            switch(r)
            {
                case PrototypeRole.StarSocket:
                    D(m,r,"stone",2,2);D(m,r,"socket",4,2,"socket");Goal(m,"socket",PrototypeGoalKind.Active);break;
                case PrototypeRole.VineBridge:
                    D(m,r,"plate",2,2,"plate").group="vine";D(m,r,"bridge",3,2).group="vine";m.RemoveTerrain(P(3,2));break;
                case PrototypeRole.CrescentLeap:
                    D(m,r,"leap",2,2);m.RemoveTerrain(P(3,2));m.RemoveTerrain(P(4,2));route="EEE";break;
                case PrototypeRole.Shutter:
                    var shutter=D(m,r,"shutter",3,2);shutter.orientation=1;break;
                case PrototypeRole.DepartureSpikes: D(m,r,"spikes",2,2);break;
                case PrototypeRole.MovingStage:
                    D(m,r,"stage",2,2).cells=new List<Vector2Int>{P(2,2),P(3,2)};route="EEEEE";break;
                case PrototypeRole.Conveyor:
                    D(m,r,"belt1",2,2);D(m,r,"belt2",3,2);route="EEEE";break;
                case PrototypeRole.Bookshelf:
                    D(m,r,"plate",2,2,"plate").group="shelf";D(m,r,"shelf",4,3).group="shelf";route="ESEEEEEN";
                    Goal(m,"plate",PrototypeGoalKind.Visited,P(2,2));break;
                case PrototypeRole.OneWayGate: D(m,r,"gate",3,2);route="EEEEE";break;
                case PrototypeRole.Optics:
                    D(m,r,"source",1,4,"source");D(m,r,"mirror",4,4,"mirror").orientation=1;
                    D(m,r,"sensor",4,5,"sensor").group="light";D(m,r,"lightgate",5,2,"gate").group="light";
                    D(m,PrototypeRole.Glass,"glass",2,4,"square");route="ENENESSEEEE";Goal(m,"sensor",PrototypeGoalKind.Active);break;
                case PrototypeRole.Boat:
                    for(int x=2;x<=5;x++){m.RemoveTerrain(P(x,2));m.scenario.water.Add(P(x,2));}D(m,r,"boat",2,2);break;
                case PrototypeRole.Barrel:
                    D(m,r,"barrel",2,2);D(m,PrototypeRole.Wall,"stopper",6,2);route="ESEEEEEN";Goal(m,"barrel",PrototypeGoalKind.Position,P(5,2));break;
                case PrototypeRole.Domino:
                    for(int x=2;x<=4;x++)D(m,r,"domino"+x,x,2);Goal(m,"domino4",PrototypeGoalKind.Active);break;
                case PrototypeRole.Magnet:
                    D(m,r,"magnet",6,4,null,Vector2Int.left).group="magnet";D(m,r,"switch",2,2,"switch").group="magnet";
                    D(m,PrototypeRole.MetalCrate,"metal",2,4);D(m,PrototypeRole.WoodCrate,"wood",2,5);Goal(m,"metal",PrototypeGoalKind.Position,P(5,4));break;
                case PrototypeRole.Bomb:
                    D(m,r,"bomb",2,2).blastOffsets=new List<Vector2Int>{Vector2Int.zero,Vector2Int.right};D(m,PrototypeRole.CrackedWall,"cracked",4,2);
                    route="ESEENEEE";Goal(m,"cracked",PrototypeGoalKind.Removed);break;
                case PrototypeRole.Portal:
                    D(m,PrototypeRole.Conveyor,"belt",2,2);D(m,r,"portalA",3,2,"a").target=P(6,2);D(m,r,"portalB",6,2,"b").target=P(3,2);route="E";break;
                case PrototypeRole.Statue:
                    D(m,r,"statue",6,3,null,Vector2Int.left).number=6;D(m,PrototypeRole.Glass,"glass",4,3,"square");
                    route="EEEENSEE";Goal(m,"glass",PrototypeGoalKind.Removed);break;
                case PrototypeRole.Bounce:
                    D(m,PrototypeRole.Conveyor,"belt",2,2);D(m,r,"bounce",3,2).orientation=2;D(m,PrototypeRole.Brake,"brake",3,3);route="ESEEEE";break;
                case PrototypeRole.Brake:
                    D(m,PrototypeRole.Conveyor,"belt1",2,2);D(m,PrototypeRole.Conveyor,"belt2",3,2);D(m,r,"brake",4,2);route="EEEE";break;
                case PrototypeRole.Wind:
                    D(m,r,"wind1",2,2,"field");D(m,r,"wind2",3,2,"field");D(m,r,"blower",1,4,"blower");D(m,PrototypeRole.MetalCrate,"metal",4,4);route="EEEE";break;
                case PrototypeRole.Glass:
                    D(m,r,"glass",3,2,"square");route="ESEEEEEN";break;
                case PrototypeRole.Dice:
                    D(m,r,"die",2,2);D(m,r,"target",3,2,"target").number=4;route="ESEEEEEN";Goal(m,"target",PrototypeGoalKind.Active);break;
                case PrototypeRole.CowardDoll: D(m,r,"doll",3,2);route="ESEEEEEN";break;
                case PrototypeRole.Chick: D(m,r,"chick",1,3).target=P(5,2);Goal(m,"chick",PrototypeGoalKind.Position,P(5,2));break;
            }
            m.scenario.verificationRoute=route; return m;
        }
        static GameObject Art(PrototypeDevice d)
        {
            if(d.role==PrototypeRole.Boat)return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Presentation/twilight_sea_ferry_occupied.prefab");
            var theme=d.artTheme;var role=(int)d.role;string state=null;
            if(role>=100)
            {
                state=d.role==PrototypeRole.MetalCrate?"metal_crate":d.role==PrototypeRole.WoodCrate?"wood_crate":d.role==PrototypeRole.CrackedWall?"cracked_wall":"wall";
                if(d.role==PrototypeRole.MetalCrate && theme!="starway")theme="letter_observatory";
                if(theme=="starway"&&state!="metal_crate")theme="memory_theater";
            }
            if (state == null && !manifests[theme].assets.Any(a => a.deviceId == role.ToString("00"))) theme = Themes[role];
            var manifest=manifests[theme];
            var row=state!=null?manifest.assets.FirstOrDefault(a=>a.id.EndsWith("/"+state)):manifest.assets.FirstOrDefault(a=>a.deviceId==role.ToString("00"));
            if(row==null)throw new Exception("Missing role "+role+" state "+state+" theme "+theme);
            var prefix=row.id.Split('/')[0];
            string folder=theme=="starway"
                ? (state==null && new[]{1,2,7,9,18,19,20,24}.Contains(role)
                    ? "Assets/ANIMOL/Prefabs/Themes/starway" : "Assets/ANIMOL/Prefabs/Devices")
                : "Assets/ANIMOL/Prefabs/Themes/"+theme;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+(state!=null && d.role!=PrototypeRole.MetalCrate?row.id.Replace('/','_')+"_static":prefix)+".prefab");
            if(!prefab)throw new Exception("Missing art prefab "+row.id+" in "+folder);
            d.initialVisual=state!=null?(d.role==PrototypeRole.MetalCrate?state:"static"):null;return prefab;
        }
        static void BindArt(PuzzleMap m)
        {
            foreach(var d in m.scenario.devices)d.visualPrefab=Art(d);
            m.scenario.beamPrefab=beam;
        }
        static PuzzleMap Combine(string theme,int[] roles)
        {
            var m=ScriptableObject.CreateInstance<PuzzleMap>();m.Resize(9,roles.Length*8-1);m.theme=themeAssets[theme];m.scenario=ScriptableObject.CreateInstance<PuzzleScenario>();
            m.scenario.themeId=theme;m.scenario.title=theme+" prototype";m.start=P(1,2);m.targetBubbles=3;
            var route="";
            for(int i=0;i<roles.Length;i++)
            {
                var room=Room(roles[i]);var offset=P(0,i*8);
                foreach(var t in room.terrain)m.SetTerrain(t.position+offset,t.material,t.blocked);
                foreach(var d in room.scenario.devices)
                {
                    d.id=i+"_"+d.id;d.group=i+"_"+d.group;d.artTheme=theme;d.position+=offset;
                    if(d.role==PrototypeRole.Portal || d.role==PrototypeRole.Chick && d.target!=Vector2Int.zero)d.target+=offset;
                    d.cells=d.cells.Select(p=>p+offset).ToList();m.scenario.devices.Add(d);
                }
                foreach(var p in room.scenario.water)m.scenario.water.Add(p+offset);
                foreach(var g in room.scenario.goals){g.deviceId=i+"_"+g.deviceId;g.cell+=offset;m.scenario.goals.Add(g);}
                if(i==0||i==roles.Length-1||i==roles.Length/2)m.bubbles.Add(room.exit+offset);
                route+=room.scenario.verificationRoute;m.exit=room.exit+offset;
                if(i<roles.Length-1)
                {
                    for(int y=3;y<=8;y++)m.SetTerrain(P(7,y)+offset,TerrainMaterial.Grass);
                    for(int x=1;x<=7;x++)m.SetTerrain(P(x,8)+offset,TerrainMaterial.Grass);
                    m.SetTerrain(P(1,9)+offset,TerrainMaterial.Grass);route+="NNNNNNWWWWWWNN";
                }
                UnityEngine.Object.DestroyImmediate(room.scenario);UnityEngine.Object.DestroyImmediate(room);
            }
            m.scenario.verificationRoute=route;return m;
        }
        public static Vector2Int Direction(char c) => c=='N'?Vector2Int.up:c=='E'?Vector2Int.right:c=='S'?Vector2Int.down:Vector2Int.left;
        public static string Verify(PuzzleMap map)
        {
            var session=new PuzzleDeviceSession(map);int index=0;
            foreach(char c in map.scenario.verificationRoute)
            {var action=session.Apply(Direction(c));if(!action.accepted||session.Failed)throw new Exception(map.name+" step "+index+" at "+session.Cell+": "+(session.Failure??action.reason));index++;}
            if(!session.Won)throw new Exception(map.name+" route did not win at "+session.Cell+"; goals "+string.Join(",",map.scenario.goals.Where(g=>g.kind!=PrototypeGoalKind.Visited).Select(g=>g.deviceId+":"+session.States[g.deviceId].cell+"/"+session.States[g.deviceId].active)));
            return map.name+": clear, "+session.Cost+" valid actions";
        }
        public static string InspectBindings()
        {
            var issues=new HashSet<string>();
            foreach(var m in AssetDatabase.FindAssets("t:PuzzleMap",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PuzzleMap>))
            {
                var session=new PuzzleDeviceSession(m);
                Action check=()=>{foreach(var d in session.Devices.Where(d=>!session.States[d.id].removed)){
                    var v=d.visualPrefab.GetComponent<DeviceVisualView>()??d.visualPrefab.GetComponent<FerryVisualView>().clock;
                    var idle=PuzzleDeviceView.Idle(d,session.States[d.id]);if(!v.states.Any(s=>s.name==idle))issues.Add(d.role+" idle "+idle);
                }};check();
                foreach(char c in m.scenario.verificationRoute)
                {
                    var result=session.Apply(Direction(c));foreach(var e in result.events.Where(e=>e.kind==PrototypeEventKind.Visual)){
                        var d=session.Definition(e.actor);var v=d.visualPrefab.GetComponent<DeviceVisualView>()??d.visualPrefab.GetComponent<FerryVisualView>().clock;
                        if(!v.states.Any(s=>s.name==e.state&&s.eventName==e.eventName))issues.Add(d.role+" event "+e.state+" / "+e.eventName);
                    }check();
                }
            }
            return string.Join("\n",issues);
        }
        static void Persist(PuzzleMap map,string name,bool unlimitedCost=false)
        {
            map.name=name;BindArt(map);Verify(map);map.scenario.costLimit=unlimitedCost?0:map.scenario.verificationRoute.Length+8;
            map.scenario=Save(map.scenario,Root+"/"+name+"_Scenario.asset");Save(map,Root+"/"+name+".asset");
        }
        // Updates the four referenced map assets in place. Rebuilding theme assets would discard
        // later environment art assignments, so this command only touches prototype maps/scenarios.
        [MenuItem("ANIMOL/Puzzle/Rebuild themed device test maps")]
        public static string RebuildThemedDeviceTestMaps()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit play mode and save the active scene first.");
            themeAssets.Clear();manifests.Clear();
            foreach(var name in ThemedNames)
            {
                themeAssets[name]=AssetDatabase.LoadAssetAtPath<PuzzleTheme>(Root+"/"+name+"_Theme.asset");
                if(!themeAssets[name])throw new Exception("Missing prototype theme: "+name);
                var manifestName=name=="starway"?"device_art":name;
                manifests[name]=JsonUtility.FromJson<DeviceManifest>(File.ReadAllText("Assets/ANIMOL/Data/"+manifestName+"_import_manifest.json").Replace("\"event\":","\"eventName\":"));
            }
            beam=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Puzzle/OpticsBeam.prefab");
            if(!beam)throw new Exception("Missing optics beam prefab.");
            for(int i=0;i<ThemedNames.Length;i++)Persist(Combine(ThemedNames[i],ThemedRoles[i]),ThemedNames[i],true);
            AssetDatabase.SaveAssets();
            return string.Join("; ",ThemedNames.Select((name,i)=>name+" "+ThemedRoles[i].Length+" rooms"));
        }
        [MenuItem("ANIMOL/Puzzle/Build themed prototypes")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit play mode and save scene first.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory("Assets/ANIMOL/Prefabs/Puzzle");AssetDatabase.Refresh();themeAssets.Clear();manifests.Clear();
            var sources=new[]{"Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset","Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset","Assets/ANIMOL/Data/Art/ClockDome/PuzzleTheme.asset","Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset"};
            var names=new[]{"starway","memory_theater","letter_observatory","twilight_sea"};
            for(int i=0;i<4;i++)
            {
                var theme=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PuzzleTheme>(sources[i]));theme.name=names[i];themeAssets[names[i]]=Save(theme,Root+"/"+names[i]+"_Theme.asset");
                manifests[names[i]]=JsonUtility.FromJson<DeviceManifest>(File.ReadAllText("Assets/ANIMOL/Data/"+(i==0?"device_art":names[i])+"_import_manifest.json").Replace("\"event\":","\"eventName\":"));
            }
            var go=new GameObject("Prototype optics beam");var path=go.AddComponent<BeamPathView>();
            path.sprites=manifests["letter_observatory"].assets.Where(a=>a.id.StartsWith("d10_optics/beam_")).Select(a=>new BeamVisualSprite { shape=a.id.Split('/')[1],sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+a.path) }).ToArray();
            beam=PrefabUtility.SaveAsPrefabAsset(go,"Assets/ANIMOL/Prefabs/Puzzle/OpticsBeam.prefab");UnityEngine.Object.DestroyImmediate(go);
            for(int i=1;i<=24;i++)Persist(Room(i),"Device"+i.ToString("00")+"_"+(PrototypeRole)i);
            for(int i=0;i<4;i++)
            {
                Persist(Combine(names[i],ThemedRoles[i]),names[i],true);
                var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(Root+"/"+names[i]+".asset");
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);PuzzlePreview.CreateRoot(map);
                EditorSceneManager.SaveScene(scene,"Assets/ANIMOL/Scenes/Prototype/ANIMOL_"+names[i]+"_Prototype.unity");
            }
            AssetDatabase.SaveAssets();PortraitGameView.Use1080x1920();return "24 device rooms + 4 themed prototype stages; routes verified";
        }
    }
}
