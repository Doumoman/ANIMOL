using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace ANIMOL.Presentation.Tests
{
    public class MemoryTheaterArtPlayTests
    {
        const string Scene="Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Art_QA.unity";
        [UnityTest] public IEnumerator TheaterSceneRunsAtPortraitResolutionWithoutRootMotionOrDuplicateWriters()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null);type.GetMethod("Use1080x1920").Invoke(null,null);yield return null;yield return null;
            Assert.AreEqual(1080,Screen.width);Assert.AreEqual(1920,Screen.height);
            var qa=Object.FindFirstObjectByType<DeviceArtQaController>();Assert.AreEqual(11,qa.masters.Length);Assert.AreEqual(369,qa.views.Length);
            var roots=qa.views.Select(v=>v.transform.position).ToArray();foreach(var v in qa.views)v.paused=true;var elapsed=qa.views.Select(v=>v.Elapsed).ToArray();yield return new WaitForSecondsRealtime(.12f);
            for(int i=0;i<qa.views.Length;i++){Assert.AreEqual(elapsed[i],qa.views[i].Elapsed);qa.views[i].paused=false;qa.views[i].Present(qa.views[i].CurrentState,true);}
            yield return new WaitForSecondsRealtime(.4f);
            for(int i=0;i<qa.views.Length;i++){Assert.AreEqual(roots[i],qa.views[i].transform.position);Assert.AreEqual(1,qa.views[i].GetComponentsInChildren<DeviceVisualView>().Length);Assert.AreEqual(0,qa.views[i].GetComponentsInChildren<Animator>().Length);Assert.AreEqual(0,qa.views[i].GetComponentsInChildren<Collider2D>().Length);foreach(var state in qa.views[i].states){if(state.sprite)Assert.NotNull(state.sprite.texture,qa.views[i].name+"/"+state.name);if(state.sequence)foreach(var frame in state.sequence.frames)Assert.NotNull(frame.texture,qa.views[i].name+"/"+state.name);}qa.views[i].ResetVisual();Assert.AreEqual(0,qa.views[i].Elapsed);}
            var hud=Object.FindFirstObjectByType<PuzzleHudView>();Assert.NotNull(hud);Assert.AreEqual("COST --",hud.cost.text);Assert.AreEqual("--/--",hud.bubbles.text);Assert.AreEqual(1,Time.timeScale);
            var statue=qa.masters.First(v=>v.deviceId=="17");Assert.True(statue.Notify("turn_1","wait_for_next_valid_action","warning_hold_e"));yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual("warning_hold_e",statue.CurrentState);
        }
        [UnityTest] public IEnumerator SceneReentryResetsStateAndDoesNotRetainPreviewInstances()
        {
            for(int pass=0;pass<2;pass++)
            {
                EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                var qa=Object.FindFirstObjectByType<DeviceArtQaController>();Assert.AreEqual(1,Object.FindObjectsByType<DeviceArtQaController>(FindObjectsSortMode.None).Length);Assert.AreEqual(369,Object.FindObjectsByType<DeviceVisualView>(FindObjectsSortMode.None).Length);
                foreach(var v in qa.masters){Assert.AreEqual(v.initialState,v.CurrentState);v.Notify("same_action",v.states.FirstOrDefault(s=>s.eventName!=null)?.eventName,v.states.FirstOrDefault(s=>s.eventName!=null)?.name);}
            }
        }
    }
}
