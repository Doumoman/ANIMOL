using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace ANIMOL.Presentation.Tests
{
    public class TwilightSeaArtPlayTests
    {
        const string Scene="Assets/ANIMOL/Scenes/ANIMOL_TwilightSea_Art_QA.unity";
        [UnityTest] public IEnumerator PortraitSceneHasEveryTextureAndPausedSynchronizedFerry()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null);type.GetMethod("Use1080x1920").Invoke(null,null);yield return null;yield return null;Assert.AreEqual(1080,Screen.width);Assert.AreEqual(1920,Screen.height);
            var qa=Object.FindFirstObjectByType<DeviceArtQaController>();Assert.AreEqual(8,qa.masters.Length);foreach(var v in qa.views){v.paused=true;foreach(var s in v.states){if(s.sprite)Assert.NotNull(s.sprite.texture);if(s.sequence)foreach(var f in s.sequence.frames)Assert.NotNull(f.texture);}Assert.AreEqual(0,v.GetComponentsInChildren<Animator>().Length);Assert.AreEqual(0,v.GetComponentsInChildren<Collider2D>().Length);}
            var ferry=Object.FindFirstObjectByType<FerryVisualView>();var position=ferry.transform.position;var phase=ferry.clock.Elapsed;yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(phase,ferry.clock.Elapsed);Assert.AreEqual(position,ferry.transform.position);Assert.IsNull(ferry.clock.visual);
            ferry.clock.paused=false;ferry.Present("row_n",true);yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(position,ferry.transform.position);Assert.AreSame(ferry.pairs.First(p=>p.state=="row_n").rearTerminal,ferry.rear.sprite);Assert.False(ferry.wake.enabled);Assert.AreEqual("COST --",Object.FindFirstObjectByType<PuzzleHudView>().cost.text);Assert.AreEqual(1,Time.timeScale);qa.ResetVisuals();Assert.AreEqual(0,Object.FindFirstObjectByType<ResolvedCellPreview>().boundary.ActiveCount);
        }
        [UnityTest] public IEnumerator ReentryAndResetClearPreviewsAndDoNotDuplicateRenderers()
        {
            for(int pass=0;pass<2;pass++){EditorSceneManager.LoadSceneInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;Assert.AreEqual(1,Object.FindObjectsByType<FerryVisualView>(FindObjectsSortMode.None).Length);var f=Object.FindFirstObjectByType<FerryVisualView>();f.ResetVisual();Assert.AreEqual("idle_e",f.clock.CurrentState);Assert.AreEqual(4,f.GetComponentsInChildren<SpriteRenderer>().Length);var p=Object.FindFirstObjectByType<ResolvedCellPreview>();Assert.AreEqual(3,p.boundary.ActiveCount);Assert.AreEqual(1,p.targets.ActiveCount);Assert.AreEqual(1,p.endpoint.ActiveCount);int before=p.GetComponentsInChildren<SpriteRenderer>().Length;p.Clear();p.PresentBlast(new[]{Vector2Int.zero},new Vector2Int[0]);Assert.AreEqual(before,p.GetComponentsInChildren<SpriteRenderer>().Length);p.gameObject.SetActive(false);Assert.AreEqual(0,p.boundary.ActiveCount);}
        }
    }
}
