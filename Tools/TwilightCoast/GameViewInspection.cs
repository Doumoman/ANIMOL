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

public static class TwilightCoastGameViewInspection
{
    const string Output = "Docs/Validation/TwilightCoast";
    public static string Progress = "Idle";
    [Serializable] public class Shot { public string name, scene; public int width, height, scale; public Vector2Int[] phases; }
    [Serializable] public class Report { public string source = "Actual Play mode Game View / ScreenCapture", failure; public List<Shot> shots = new List<Shot>(); public List<string> checks = new List<string>(); }
    static Report report;
    static TwilightCoastPalm host;
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play mode first");
        report = new Report(); host = new GameObject("Coast environment inspection host").AddComponent<TwilightCoastPalm>(); UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
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
    { PortraitGameView.SelectFixedResolution(w,h,"Coast inspection"); yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f); Require(Screen.width==w && Screen.height==h,"Game View " + w + "x" + h); }
    static IEnumerator Capture(string name)
    {
        Progress = name; yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png");
        var bg=UnityEngine.Object.FindFirstObjectByType<SkyIslandBackdrop>(); report.shots.Add(new Shot { name=name,scene=SceneManager.GetActiveScene().path,width=Screen.width,height=Screen.height,scale=bg.IntegerScale,phases=bg.Phases.ToArray() }); yield return new WaitForSecondsRealtime(.25f);
    }
    static IEnumerator Run()
    {
        EditorSceneManager.LoadSceneInPlayMode(TwilightCoastBuilder.InspectionScene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        var view=UnityEngine.Object.FindFirstObjectByType<TwilightCoastRenderer>(); var lens=UnityEngine.Object.FindFirstObjectByType<TwilightCoastInspection>().lens; var bg=UnityEngine.Object.FindFirstObjectByType<SkyIslandBackdrop>(); var art=view.art;
        yield return Size(880,648); yield return Capture("coast_join_1x"); yield return Size(3520,2592); yield return Capture("coast_join_4x"); view.Grid.gameObject.SetActive(false);
        var landmark=UnityEngine.Object.Instantiate(art.horizonPrefab); landmark.transform.position=Vector3.zero;
        lens.orthographicSize=2; lens.transform.position=new Vector3(6,-2,-10); yield return Size(1280,320); yield return Capture("horizon_wide");
        landmark.GetComponent<TwilightCoastHorizon>().Assemble(2,2,true); yield return null; yield return Capture("horizon_separated"); UnityEngine.Object.Destroy(landmark); yield return null;
        var palmRoot=UnityEngine.Object.Instantiate(art.palmPrefab); palmRoot.transform.position=Vector3.zero; var palm=palmRoot.GetComponent<TwilightCoastPalm>(); palm.enabled=false;
        lens.orthographicSize=1; lens.transform.position=new Vector3(0,1,-10); yield return Size(160,160);
        for(var frame=0;frame<5;frame++) { if(frame>0)palm.Advance(art.palmIdle.durations[frame-1]); yield return Capture("palm_idle_"+frame.ToString("00")); }
        // Touch asset inspection only: no runtime contact event is invented or connected.
        for(var frame=0;frame<6;frame++) { palm.visual.sprite=art.palmTouch.frames[frame]; yield return Capture("palm_touch_"+frame.ToString("00")); }
        UnityEngine.Object.Destroy(palmRoot); yield return null;
        bg.backgroundOnly=true; bg.hideBackground=false; bg.overrideSample=true; bg.SetNearAnimation(null); lens.orthographicSize=3.5f; yield return Size(400,560);
        var samples=new[] { Vector2.zero,new Vector2(640,480),new Vector2(-400,960) }; var names=new[] { "A","B","C" };
        for(var k=0;k<3;k++) { bg.sampleCameraPixels=samples[k]; yield return null; yield return Capture("background_"+names[k]); for(var j=0;j<3;j++) Require(bg.Phases[j]==SkyIslandBackdrop.Phase(samples[k],new[] {.12f,.30f,.50f}[j]),names[k]+" layer "+j+" floor-mod phase"); }
        lens.orthographicSize=10; yield return Size(2080,1600); bg.sampleCameraPixels=new Vector2(-7040,-5120); yield return null; yield return Capture("background_multi_period"); Require(bg.IntegerScale==1,"Large viewport native raster");
        bg.animateNear=false; bg.SetNearAnimation(art.waterRipples);
        for(var frame=0;frame<4;frame++) { if(frame>0)bg.AdvanceNear(.6001f); yield return Capture("ripples_multi_"+frame.ToString("00")); Require(bg.transform.Find("Near").GetComponentsInChildren<SpriteRenderer>().All(r=>r.sprite==art.waterRipples.frames[frame]),"Near replacement frame "+frame); }
        EditorSceneManager.LoadSceneInPlayMode(TwilightCoastBuilder.PreviewScene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        foreach(var dims in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1080,2400)})
        { yield return Size(dims.x,dims.y); yield return Capture("portrait_"+dims.x+"x"+dims.y); var player=UnityEngine.Object.FindFirstObjectByType<PuzzleController>(); Require(!player.BlocksInput && player.Session.Moves==0,"Idle preserves input/session "+dims); }
        // A long vertical map leaves the world horizon behind when the camera moves downward.
        var controller=UnityEngine.Object.FindFirstObjectByType<PuzzleController>(); var original=AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data+"/Maps/Vertical.asset"); var map=UnityEngine.Object.Instantiate(original); map.theme=AssetDatabase.LoadAssetAtPath<PuzzleTheme>(TwilightCoastBuilder.Data+"/PuzzleTheme.asset"); controller.Load(map); yield return null;
        var actorStart=controller.Player.position; controller.Player.position=new Vector3(3,38,0); controller.FollowCamera.Cut(); yield return null; yield return Capture("vertical_horizon_upper");
        controller.Player.position=actorStart; controller.FollowCamera.Cut(); yield return null; yield return Capture("vertical_ocean_lower");
        Require(!controller.GetComponentInChildren<TwilightCoastRenderer>().Horizon.GetComponentsInChildren<Renderer>().Any(r=>GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(controller.FollowCamera.GetComponent<Camera>()),r.bounds)),"Horizon leaves view below; ocean remains background");
        EditorSceneManager.LoadSceneInPlayMode(TwilightSeaArtBuilder.ScenePath,new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null; yield return Size(1080,1920);
        var qa=UnityEngine.Object.FindFirstObjectByType<DeviceArtQaController>(); Require(qa.views.Length==307 && qa.masters.Length==8,"Sea keeps 307 presenters and 8 masters");
        foreach(var viewState in qa.views) viewState.paused=true;
        qa.qaCamera.orthographicSize=6; qa.qaCamera.transform.position=new Vector3(0,13,-10); yield return Capture("sea_sunset_border");
        qa.qaCamera.orthographicSize=8; qa.qaCamera.transform.position=new Vector3(0,5,-10); yield return Capture("sea_devices");
        qa.qaCamera.orthographicSize=8; qa.qaCamera.transform.position=new Vector3(.7f,-9,-10); yield return Capture("sea_character_exit");
        qa.qaCamera.orthographicSize=12; qa.qaCamera.transform.position=new Vector3(0,0,-10); yield return Capture("sea_default_camera");
        qa.qaCamera.orthographicSize=4; qa.qaCamera.transform.position=new Vector3(-4.5f,12,-10); yield return Capture("sea_palm_contact");
        qa.qaCamera.orthographicSize=4; qa.qaCamera.transform.position=new Vector3(0,-13,-10); yield return Capture("sea_fence_contact");
        var environment=UnityEngine.Object.FindFirstObjectByType<TwilightCoastRenderer>(); Require(environment.Underside.transform.localPosition.y==-.1f,"Whole wet-sand body offset is 8px");
        foreach(var image in environment.GetComponentsInChildren<TwilightCoastPalm>().Select(p=>p.visual)) Require(!environment.map.terrain.Any(cell=>image.bounds.Contains(environment.transform.TransformPoint((Vector3)(Vector2)cell.position))),"Palm avoids all playable centers");
    }
}
