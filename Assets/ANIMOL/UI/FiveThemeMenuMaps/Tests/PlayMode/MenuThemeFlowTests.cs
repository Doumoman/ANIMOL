using System.Collections;
using System.Linq;
using ANIMOL.PortraitArtV1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.FiveThemeMenu.Tests
{
    public class MenuThemeFlowTests
    {
        private IEnumerator Load(string name) {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(name);
#endif
            yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("MenuCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator RealEntryRoutesPauseAndReuseNativeResources()
        {
            yield return Load("Bootstrap");var e=Object.FindFirstObjectByType<PortraitEntryController>();
            e.StartScreen.Play.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
            e=Object.FindFirstObjectByType<PortraitEntryController>();var c=Object.FindFirstObjectByType<MenuThemeCycle>();
            Assert.That(e.HasAuthoredViews && c.HasAuthoredLayout,Is.True);
            Assert.That(e.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));
            Assert.That(c.IsVisible,Is.True);Assert.That(c.NativeFrame.width,Is.EqualTo(352));Assert.That(c.NativeFrame.height,Is.EqualTo(704));
            Assert.That(c.Output.raycastTarget,Is.False);Assert.That(c.Output.color,Is.EqualTo(Color.white));
            var resource=c.NativeFrame;
            foreach(var route in new[]{e.ModeScreen.Settings,e.ModeScreen.Shop,e.ModeScreen.Campaign}) {
                route.onClick.Invoke();yield return null;yield return null;
                double hidden=c.Elapsed;Assert.That(c.IsVisible,Is.False);
                yield return new WaitForSecondsRealtime(.1f);Assert.That(c.Elapsed,Is.EqualTo(hidden));
                e.Navigation.Back();yield return null;yield return null;
                Assert.That(c.IsVisible,Is.True);Assert.That(c.NativeFrame,Is.SameAs(resource));
            }
            Assert.That(e.ModeScreen.Ad.interactable || e.ModeScreen.Competition.interactable || e.ModeScreen.Cooperation.interactable,Is.False);
            double before=c.Elapsed;Time.timeScale=0;yield return new WaitForSecondsRealtime(.1f);Assert.That(c.Elapsed,Is.GreaterThan(before));
            c.SendMessage("OnApplicationFocus",false);before=c.Elapsed;yield return new WaitForSecondsRealtime(.1f);Assert.That(c.Elapsed,Is.EqualTo(before));
            c.SendMessage("OnApplicationFocus",true);yield return null;yield return null;Assert.That(c.Elapsed-before,Is.LessThan(.1));
            c.enabled=false;yield return null;Assert.That(c.OwnedTextureCount,Is.Zero);
            c.enabled=true;yield return null;yield return null;Assert.That(c.OwnedTextureCount,Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<MenuThemeCycle>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RegeneratedMenuCanInstallWithoutSerializedBackdrop()
        {
            yield return Load("Lobby");
            var old=Object.FindFirstObjectByType<MenuThemeCycle>();var parent=old.transform.parent;
            Object.Destroy(old.gameObject);yield return null;yield return null;
            var go=new GameObject("Generated V7 fallback");go.transform.SetParent(parent,false);
            var replacement=go.AddComponent<MenuThemeCycle>();yield return null;yield return null;
            Assert.That(replacement.HasAuthoredLayout,Is.True);
            Assert.That(replacement.Catalog.sourceVersion,Is.EqualTo("7.0"));
            Assert.That(parent.GetComponentsInChildren<RawImage>(true).Count(i=>i.name=="MainUiBackdropFill"),Is.EqualTo(1));
            Assert.That(parent.Find("MainUiBackdropFill").GetComponent<RawImage>().raycastTarget,Is.False);
            Assert.That(replacement.IsVisible,Is.True);
            Assert.That(replacement.OwnedTextureCount,Is.EqualTo(3));
            Assert.That(parent.GetComponentsInChildren<MenuThemeCycle>(true).Length,Is.EqualTo(1));
            Assert.That(parent.GetComponentsInChildren<RawImage>(true).Count(i=>i.name=="MainUiV7Backdrop"),Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator NativeOutputUsesOnlySweetie16EvenDuringWipe()
        {
            yield return Load("Lobby");var c=Object.FindFirstObjectByType<MenuThemeCycle>();
            var allowed=new[]{"1A1C2C","5D275D","B13E53","EF7D57","FFCD75","A7F070","38B764","257179","29366F","3B5DC9","41A6F6","73EFF7","F4F4F4","94B0C2","566C86","333C57"};
            var tex=new Texture2D(352,704,TextureFormat.RGBA32,false);
            foreach(double t in new[]{0,2.25,5.18,7.25,12.25,17.25,22.25,25.18}) {
                c.InspectionTime=t;yield return null;yield return null;
                var old=RenderTexture.active;RenderTexture.active=c.NativeFrame;tex.ReadPixels(new Rect(0,0,352,704),0,0);tex.Apply();RenderTexture.active=old;
                var colors=tex.GetPixels32().Select(p=>$"{p.r:X2}{p.g:X2}{p.b:X2}").Distinct().ToArray();
                Assert.That(colors.Except(allowed),Is.Empty,"Palette at "+t);Assert.That(tex.GetPixels32().All(p=>p.a==255),Is.True);
            }
            Object.Destroy(tex);
        }
    }
}
