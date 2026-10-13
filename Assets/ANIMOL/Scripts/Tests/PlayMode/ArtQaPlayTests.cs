using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
namespace ANIMOL.Presentation.Tests
{
    public class ArtQaPlayTests
    {
        [UnityTest] public IEnumerator RealQaScenePlaysTouchAndPooledFxWithoutMovingRoots()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_Starway_Art_QA.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).FirstOrDefault(t=>t!=null);
            Assert.That(setup,Is.Not.Null);setup.GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{1080,1920,"Art QA 1080x1920"});yield return null;yield return null;
            Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(1920));
            var vines=UnityEngine.Object.FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None);
            Assert.That(vines.Length,Is.GreaterThan(50));
            var positions=new Vector3[vines.Length];for(int i=0;i<vines.Length;i++){positions[i]=vines[i].transform.position;vines[i].PlayTouch();}
            yield return new WaitForSecondsRealtime(.5f);
            for(int i=0;i<vines.Length;i++){Assert.That(vines[i].IsTouching,Is.False);Assert.That(vines[i].transform.position,Is.EqualTo(positions[i]));}
            var fx=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/bubble_collect_fx.prefab"));fx.GetComponent<SpriteFramePlayer>().PlayTouch();yield return new WaitForSecondsRealtime(.3f);Assert.That(fx.activeSelf,Is.False);
            fx.SetActive(true);Assert.That(fx.GetComponent<SpriteFramePlayer>().visual.enabled,Is.True);UnityEngine.Object.Destroy(fx);
            var hud=UnityEngine.Object.FindFirstObjectByType<PuzzleHudView>();Assert.That(hud.cost.text,Is.EqualTo("COST --"));hud.Present(17,2,3,true);Assert.That(hud.cost.text,Is.EqualTo("COST 17"));Assert.That(hud.bubbles.text,Is.EqualTo("2/3"));
            int requests=0;hud.PauseRequested=()=>requests++;hud.OnPointerDown(null);Assert.That(hud.pause.sprite,Is.EqualTo(hud.press));hud.OnPointerUp(null);Assert.That(hud.pause.sprite,Is.EqualTo(hud.release));Assert.That(requests,Is.EqualTo(1));hud.OnPointerExit(null);Assert.That(hud.pause.sprite,Is.EqualTo(hud.normal));hud.Unbind();Assert.That(hud.pause.sprite,Is.EqualTo(hud.disabled));
            setup.GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{360,640,"Art QA small portrait"});yield return null;yield return null;
            Assert.That(Screen.width,Is.EqualTo(360));Assert.That(Screen.height,Is.EqualTo(640));
            hud.ApplySafeArea(new Rect(0,24,360,592),new Vector2(360,640));Canvas.ForceUpdateCanvases();
            Assert.That(hud.safeArea.anchorMin.y,Is.EqualTo(24f/640).Within(.001f));Assert.That(hud.safeArea.anchorMax.y,Is.EqualTo(616f/640).Within(.001f));
            foreach(var label in new[]{hud.cost,hud.bubbles}){var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(hud.safeArea,label.transform);Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(hud.safeArea.rect.xMin));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(hud.safeArea.rect.xMax));}
#else
            Assert.Ignore("Editor-authored scene inspection requires Editor runner.");yield break;
#endif
        }
    }
}
