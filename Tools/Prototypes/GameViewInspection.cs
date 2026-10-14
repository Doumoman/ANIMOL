using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
public static class PrototypeGameViewInspection
{
    const string Output="Docs/Validation/Prototypes";
    static PuzzlePrototypeController controller;
    static TwilightCoastPalm host;
    static readonly List<object> checks=new List<object>();
    static readonly List<object> shots=new List<object>();
    static string failure;
    public static void Start()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode is required.");
        Directory.CreateDirectory(Output);checks.Clear();shots.Clear();failure=null;
        controller=UnityEngine.Object.FindFirstObjectByType<PuzzlePrototypeController>();
        if(!controller)throw new Exception("Open a prototype scene first.");
        host=new GameObject("Prototype inspection host").AddComponent<TwilightCoastPalm>();host.StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);Write("running");
        while(stack.Count>0)
        {
            bool more=false;object current=null;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
            catch(Exception e){failure=e.ToString();break;}
            if(!more){stack.Pop();continue;}if(current is IEnumerator){stack.Push((IEnumerator)current);continue;}yield return current;
        }
        Write(failure==null?"complete":"failed");if(failure!=null)Debug.LogError(failure);UnityEngine.Object.Destroy(host.gameObject);
    }
    static void Write(string status)=>File.WriteAllText(Output+"/inspection_gameview.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source="Actual Play mode Game View / ScreenCapture",status,failure,checks,shots},Newtonsoft.Json.Formatting.Indented));
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static IEnumerator Size(int w,int h)
    {
        PortraitGameView.SelectFixedResolution(w,h,"Prototype inspection");yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);
        Require(Screen.width==w&&Screen.height==h,"Incorrect Game View resolution");
    }
    static IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+name+".png");yield return null;
        var bg=controller.GetComponentInChildren<SkyIslandBackdrop>();
        shots.Add(new{name,width=Screen.width,height=Screen.height,integerScale=bg.IntegerScale,cell=new{x=controller.Session.Cell.x,y=controller.Session.Cell.y},cost=controller.Session.Cost});Write("running");
    }
    static IEnumerator Load(PuzzleMap map)
    {
        controller.secondsPerCell=.015f;controller.Load(map);yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);
        Require(controller.Devices.MissingBindings==0,"Missing initial art: "+map.name);
    }
    static IEnumerator Move(char c)
    {
        var input=controller.GetComponent<PuzzleSwipeInput>();var direction=PuzzlePrototypeBuilder.Direction(c);
        input.BeginGesture(new Vector2(Screen.width*.4f,Screen.height*.4f),false);
        Require(input.EndGesture(new Vector2(Screen.width*.4f,Screen.height*.4f)+(Vector2)direction*Screen.width*.25f),"Swipe rejected at "+controller.Session.Cell);
        var deadline=Time.realtimeSinceStartup+8;while(controller.IsMoving&&Time.realtimeSinceStartup<deadline)yield return null;
        Require(!controller.IsMoving&&!controller.Session.Failed,"Movement failed");Require(!controller.BlocksInput&&Time.timeScale==1,"Input/time changed");
        Require((Vector2)controller.Player.position==(Vector2)controller.Session.Cell,"Actor diverged from grid");
    }
    static IEnumerator Run()
    {
        yield return Size(1080,1920);
        foreach(var theme in new[]{"starway","memory_theater","letter_observatory","twilight_sea"})
        {
            var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzlePrototypeBuilder.Root+"/"+theme+".asset");yield return Load(map);yield return Capture(theme+"_start");
            foreach(char c in map.scenario.verificationRoute)yield return Move(c);
            Require(controller.Session.Won&&controller.Devices.MissingBindings==0,"Prototype did not clear: "+theme);
            yield return new WaitForSecondsRealtime(.2f);yield return Capture(theme+"_clear");
            checks.Add(new{map=map.name,width=map.Width,height=map.Height,won=controller.Session.Won,cost=controller.Session.Cost,collected=controller.Session.Collected,portalCuts=controller.Session.PortalCuts,missingBindings=controller.Devices.MissingBindings});Write("running");
        }
        var captureIds=new[]{1,2,3,4,8,10,11,15,16,17,22,24};
        for(int i=1;i<=24;i++)
        {
            var map=AssetDatabase.FindAssets("t:PuzzleMap",new[]{PuzzlePrototypeBuilder.Root}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Device"+i.ToString("00")+"_")).Select(AssetDatabase.LoadAssetAtPath<PuzzleMap>).Single();
            yield return Load(map);int step=0;
            foreach(char c in map.scenario.verificationRoute)
            {
                yield return Move(c);step++;
                if(step==(i==10?6:i==17?6:1)&&captureIds.Contains(i))yield return Capture("device"+i.ToString("00")+"_action");
            }
            Require(controller.Session.Won&&controller.Devices.MissingBindings==0,"Room did not clear: "+map.name);
            checks.Add(new{map=map.name,won=true,cost=controller.Session.Cost,missingBindings=controller.Devices.MissingBindings});Write("running");
        }
        var island=AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzlePrototypeBuilder.Root+"/starway.asset");yield return Load(island);
        yield return Size(540,960);yield return Capture("starway_1x");yield return Size(2160,3840);yield return Capture("starway_4x");yield return Size(1080,1920);
        var backdrop=controller.GetComponentInChildren<SkyIslandBackdrop>();backdrop.overrideSample=true;
        foreach(var p in new[]{Vector2.zero,new Vector2(640,480),new Vector2(-400,960)})
        {
            backdrop.sampleCameraPixels=p;yield return null;yield return Capture("background_"+(int)p.x+"_"+(int)p.y);
            for(int i=0;i<3;i++)Require(backdrop.Phases[i].x>=0&&backdrop.Phases[i].x<960&&backdrop.Phases[i].y>=0&&backdrop.Phases[i].y<960,"Invalid floor-mod phase");
        }
        backdrop.overrideSample=false;checks.Add(new{backgroundSamples=3,negativePhaseChecked=true});
    }
}
