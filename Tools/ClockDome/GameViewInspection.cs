using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClockDomeGameViewInspection
{
    const string Output = "Docs/Validation/ClockDome";
    public static string Progress = "Idle";
    [Serializable] public class Shot { public string name, scene; public int width, height, scale; public Vector2Int[] phases; }
    [Serializable] public class Report { public string source = "Actual Play mode Game View / ScreenCapture", failure; public List<Shot> shots = new List<Shot>(); public List<string> checks = new List<string>(); }
    static Report report;
    static ClockDomeAnimation host;
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play mode first");
        report = new Report(); host = new GameObject("Clock environment inspection host").AddComponent<ClockDomeAnimation>(); UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
        host.StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(run);
        while (stack.Count > 0)
        {
            object current = null; bool more = false;
            try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
            catch (Exception e) { report.failure = e.ToString(); break; }
            if (!more) { stack.Pop(); continue; }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
        Progress = report.failure == null ? "Complete" : "Failed"; File.WriteAllText(Output + "/inspection_gameview.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { report.source, report.failure, shots=report.shots.Select(s=>new { s.name,s.scene,s.width,s.height,s.scale,phases=s.phases.Select(p=>new {p.x,p.y}) }).ToArray(), report.checks }, Newtonsoft.Json.Formatting.Indented));
        if (report.failure != null) Debug.LogError(report.failure); UnityEngine.Object.Destroy(host.gameObject);
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); report.checks.Add(message); }
    static IEnumerator Size(int w, int h)
    { PortraitGameView.SelectFixedResolution(w,h,"Clock inspection"); yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f); Require(Screen.width==w && Screen.height==h,"Game View " + w + "x" + h); }
    static IEnumerator Capture(string name)
    {
        Progress = name; yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png");
        var bg=UnityEngine.Object.FindFirstObjectByType<SkyIslandBackdrop>(); report.shots.Add(new Shot { name=name,scene=SceneManager.GetActiveScene().path,width=Screen.width,height=Screen.height,scale=bg.IntegerScale,phases=bg.Phases.ToArray() }); yield return new WaitForSecondsRealtime(.25f);
    }
    static IEnumerator Run()
    {
        EditorSceneManager.LoadSceneInPlayMode(ClockDomeBuilder.InspectionScene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        var view=UnityEngine.Object.FindFirstObjectByType<ClockDomeRenderer>(); var lens=UnityEngine.Object.FindFirstObjectByType<ClockDomeInspection>().lens; var bg=UnityEngine.Object.FindFirstObjectByType<SkyIslandBackdrop>();
        yield return Size(880,652); yield return Capture("floor_join_1x"); yield return Size(3520,2608); yield return Capture("floor_join_4x");
        view.Grid.gameObject.SetActive(false); GameObject roomRoot=null;
        foreach (var rows in new[] { 0,4,12 })
        {
            if (roomRoot) UnityEngine.Object.Destroy(roomRoot); yield return null;
            roomRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ClockDomeBuilder.Prefabs + "/Room_"+rows+".prefab")); roomRoot.transform.position=Vector3.zero;
            foreach(var image in roomRoot.GetComponentsInChildren<SpriteRenderer>()) if(image.name=="clock_240x240" || image.name=="pendulum_80x240") image.forceRenderingOff=true;
            var height=480+80*rows; lens.orthographicSize=height/160f; lens.transform.position=new Vector3(6,-height/160f,-10);
            yield return Size(960,height); yield return Capture("room_"+rows);
        }
        UnityEngine.Object.Destroy(roomRoot); yield return null;
        roomRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ClockDomeBuilder.Prefabs+"/Room_4.prefab")); roomRoot.transform.position=Vector3.zero; roomRoot.GetComponent<ClockDomeRoom>().Assemble(4,true); yield return null;
        foreach(var image in roomRoot.GetComponentsInChildren<SpriteRenderer>()) if(image.name=="clock_240x240" || image.name=="pendulum_80x240") image.forceRenderingOff=true;
        lens.orthographicSize=5; lens.transform.position=new Vector3(6,-5,-10); yield return Size(960,800); yield return Capture("room_split_4");
        UnityEngine.Object.Destroy(roomRoot); yield return null;
        roomRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ClockDomeBuilder.Prefabs+"/Room_4.prefab")); roomRoot.transform.position=Vector3.zero;
        var pendulum=roomRoot.GetComponentsInChildren<ClockDomeAnimation>().First();
        foreach(var image in roomRoot.GetComponentsInChildren<SpriteRenderer>()) image.forceRenderingOff=image!=pendulum.visual;
        foreach(var animation in roomRoot.GetComponentsInChildren<ClockDomeAnimation>()) animation.enabled=false;
        lens.orthographicSize=1.5f; lens.transform.position=new Vector3(.8f,-6.5f,-10); yield return Size(80,240);
        for(var frame=0;frame<8;frame++) { pendulum.visual.sprite=pendulum.sequence.frames[frame]; yield return Capture("pendulum_frame_"+frame.ToString("00")); }
        UnityEngine.Object.Destroy(roomRoot); yield return null;
        bg.backgroundOnly=true; bg.hideBackground=false; bg.overrideSample=true; lens.orthographicSize=3.5f; yield return Size(400,560);
        var samples=new[] { Vector2.zero,new Vector2(640,480),new Vector2(-400,960) }; var names=new[] { "A","B","C" };
        for(var k=0;k<3;k++) { bg.sampleCameraPixels=samples[k]; yield return null; yield return Capture("background_"+names[k]); for(var j=0;j<3;j++) Require(bg.Phases[j]==SkyIslandBackdrop.Phase(samples[k],new[] {.15f,.30f,.45f}[j]),names[k]+" layer "+j+" floor-mod phase"); }
        // Cross several repeat periods on a large viewport, including negative coordinates.
        lens.orthographicSize=10; yield return Size(2080,1600); bg.sampleCameraPixels=new Vector2(-7040,-5120); yield return null; yield return Capture("background_multi_period"); Require(bg.IntegerScale==1,"Large viewport keeps native integer raster");
        EditorSceneManager.LoadSceneInPlayMode(ClockDomeBuilder.PreviewScene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        foreach(var dims in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1080,2400)})
        { yield return Size(dims.x,dims.y); yield return Capture("portrait_"+dims.x+"x"+dims.y); var player=UnityEngine.Object.FindFirstObjectByType<PuzzleController>(); Require(!player.BlocksInput,"Portrait input not blocked "+dims); Require(player.Session.Moves==0,"Environment does not commit moves "+dims); }
        EditorSceneManager.LoadSceneInPlayMode(LetterObservatoryArtBuilder.ScenePath,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null; yield return Size(1080,1920);
        var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>(); Require(qa.views.Length==255 && qa.masters.Length==9,"Letter Observatory keeps 255 presenters / 9 masters");
        foreach(var viewState in qa.views) viewState.paused=true;
        qa.qaCamera.orthographicSize=6; qa.qaCamera.transform.position=new Vector3(0,13,-10); yield return Capture("observatory_dome_clock");
        qa.qaCamera.orthographicSize=8; qa.qaCamera.transform.position=new Vector3(0,5,-10); yield return Capture("observatory_devices");
        qa.qaCamera.orthographicSize=8; qa.qaCamera.transform.position=new Vector3(.7f,-9,-10); yield return Capture("observatory_character_exit");
        qa.qaCamera.transform.position=new Vector3(0,12,-10); yield return Capture("observatory_telescope_deck");
        qa.qaCamera.orthographicSize=12; qa.qaCamera.transform.position=new Vector3(0,0,-10); yield return Capture("observatory_default_camera");
        var environment=UnityEngine.Object.FindFirstObjectByType<ClockDomeRenderer>(); Require(environment.Room.transform.localScale==Vector3.one,"Dome keeps fixed scale in wide scene"); Require(environment.Underside.transform.localPosition.y==-.15f,"Whole underside layer offset is 12 pixels");
    }
}
