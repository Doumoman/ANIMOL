using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace ANIMOL.Presentation.Tests
{
    public class LetterObservatoryArtPlayTests
    {
        const string Scene="Assets/ANIMOL/Scenes/QA/ANIMOL_LetterObservatory_Art_QA.unity";
        [UnityTest] public IEnumerator ObservatoryRunsInPortraitWithAllTexturesAndLayers()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null);type.GetMethod("Use1080x1920").Invoke(null,null);yield return null;yield return null;Assert.AreEqual(1080,Screen.width);Assert.AreEqual(1920,Screen.height);
            var qa=Object.FindFirstObjectByType<DeviceArtQaController>();Assert.AreEqual(9,qa.masters.Length);Assert.AreEqual(255,qa.views.Length);var positions=qa.views.Select(v=>v.transform.position).ToArray();
            foreach(var v in qa.views)v.paused=true;var elapsed=qa.views.Select(v=>v.Elapsed).ToArray();yield return new WaitForSecondsRealtime(.12f);
            for(int i=0;i<qa.views.Length;i++){Assert.AreEqual(elapsed[i],qa.views[i].Elapsed);qa.views[i].paused=false;qa.views[i].Present(qa.views[i].CurrentState,true);}yield return new WaitForSecondsRealtime(.4f);
            for(int i=0;i<qa.views.Length;i++){var v=qa.views[i];Assert.AreEqual(positions[i],v.transform.position);foreach(var s in v.states){if(s.sprite)Assert.NotNull(s.sprite.texture,v.name);if(s.sequence)foreach(var f in s.sequence.frames)Assert.NotNull(f.texture,v.name);}Assert.AreEqual(0,v.GetComponentsInChildren<Animator>().Length);Assert.AreEqual(0,v.GetComponentsInChildren<Collider2D>().Length);v.ResetVisual();Assert.AreEqual(0,v.Elapsed);}
            var beam=Object.FindFirstObjectByType<BeamPathView>();Assert.AreEqual(6,beam.ActiveCount);Assert.AreEqual(15,beam.sortingOrder);var glass=qa.views.First(v=>v.name=="d21_glass_wall_intact_square_static");Assert.Less(glass.visual.sortingOrder,beam.sortingOrder);Assert.Greater(qa.masters.First(v=>v.deviceId=="07").visual.sortingOrder,beam.sortingOrder);
            beam.Clear();Assert.AreEqual(0,beam.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled));var hud=Object.FindFirstObjectByType<PuzzleHudView>();Assert.AreEqual("COST --",hud.cost.text);Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator SceneReentryRestoresBeamPoolWithoutDuplicateObjects()
        {
            for(int pass=0;pass<2;pass++){EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;Assert.AreEqual(1,Object.FindObjectsByType<DeviceArtQaController>(FindObjectsSortMode.None).Length);var beam=Object.FindFirstObjectByType<BeamPathView>();Assert.AreEqual(6,beam.ActiveCount);Assert.AreEqual(6,beam.GetComponentsInChildren<SpriteRenderer>().Length);beam.Clear();Assert.AreEqual(0,beam.ActiveCount);}
        }
    }
}
