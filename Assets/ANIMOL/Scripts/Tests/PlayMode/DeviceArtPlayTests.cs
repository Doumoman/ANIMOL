using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ANIMOL.Presentation;
namespace ANIMOL.Presentation.Tests
{
    public class DeviceArtPlayTests
    {
        [UnityTest] public IEnumerator EveryDeviceRunsPausesAndResetsWithoutMovingRoots()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_Device8_Art_QA.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var views=Object.FindObjectsByType<DeviceVisualView>(FindObjectsSortMode.None);Assert.AreEqual(353,views.Length);
            var roots=views.Select(v=>v.transform.position).ToArray();
            foreach(var v in views)v.paused=true;
            var elapsed=views.Select(v=>v.Elapsed).ToArray();yield return new WaitForSecondsRealtime(.1f);
            for(int i=0;i<views.Length;i++){Assert.AreEqual(elapsed[i],views[i].Elapsed);Assert.AreEqual(roots[i],views[i].transform.position);Assert.False(views[i].BlocksInput);}
            foreach(var v in views){v.paused=false;v.Present(v.CurrentState,true);}yield return new WaitForSecondsRealtime(.5f);
            for(int i=0;i<views.Length;i++){Assert.AreEqual(roots[i],views[i].transform.position);Assert.AreEqual(0,views[i].GetComponentsInChildren<Collider2D>().Length);views[i].ResetVisual();Assert.AreEqual(0,views[i].Elapsed);Assert.AreEqual(views[i].initialState,views[i].CurrentState);}
            var bridge=views.Where(v=>v.name.StartsWith("Bridge_")).GroupBy(v=>string.Join("_",v.name.Split('_').Take(3)));
            foreach(var group in bridge){var ordered=group.OrderBy(v=>v.name).ToArray();for(int i=1;i<ordered.Length;i++)Assert.AreEqual(1,Vector3.Distance(ordered[i-1].transform.position,ordered[i].transform.position),.001f);}
            Assert.AreEqual(1,Time.timeScale);
            var master=Object.FindFirstObjectByType<DeviceArtQaController>().masters.First(v=>v.deviceId=="09");
            Assert.True(master.Notify("allowed_passage_begin","open_e"));yield return new WaitForSecondsRealtime(.25f);var open=master.visual.sprite;yield return new WaitForSecondsRealtime(.25f);Assert.AreEqual(open,master.visual.sprite);
            Assert.True(master.Notify("allowed_passage_clear","close_e"));yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(master.states.First(s=>s.name=="closed_e").sprite,master.visual.sprite);
        }
    }
}
