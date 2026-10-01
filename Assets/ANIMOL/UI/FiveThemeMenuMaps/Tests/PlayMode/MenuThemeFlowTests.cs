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
            string path=name=="MoonGraphicsQA" ? "Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity" : "Assets/ANIMOL/Scenes/"+name+".unity";
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() {Time.timeScale=1;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("FiveThemeCleanup_"+System.Guid.NewGuid().ToString("N")));yield return SceneManager.UnloadSceneAsync(old);}
        [UnityTest] public IEnumerator ExistingButtonsAndSharedClockSurviveSceneTransition()
        {
            yield return Load("Bootstrap");var c=Object.FindFirstObjectByType<MenuThemeCycle>();var e=Object.FindFirstObjectByType<PortraitEntryController>();
            Assert.That(c.HasAuthoredLayout && e.HasAuthoredViews,Is.True);
            Assert.That(Object.FindObjectsByType<PortraitEntryController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(c.IsVisible,Is.True);Assert.That(e.StartScreen.GetComponentsInChildren<Button>().Length,Is.EqualTo(1));
            Assert.That(((RectTransform)e.StartScreen.Play.transform).anchoredPosition.y,Is.EqualTo(72));
            double before=c.Elapsed,now=Time.realtimeSinceStartupAsDouble;e.StartScreen.Play.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
            c=Object.FindFirstObjectByType<MenuThemeCycle>();e=Object.FindFirstObjectByType<PortraitEntryController>();
            Assert.That(c.HasAuthoredLayout && e.HasAuthoredViews,Is.True);
            Assert.That(Object.FindObjectsByType<MenuThemeCycle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(e.ModeScreen.GetComponentsInChildren<Transform>(true).Count(t=>t.name.EndsWith("Readability")),Is.EqualTo(4));
            Assert.That(c.Elapsed-before,Is.EqualTo(Time.realtimeSinceStartupAsDouble-now).Within(.05));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(((RectTransform)e.ModeScreen.Campaign.transform).sizeDelta,Is.EqualTo(new Vector2(328,176)));
            Assert.That(e.ModeScreen.Ad.interactable || e.ModeScreen.Competition.interactable || e.ModeScreen.Cooperation.interactable,Is.False);
            e.ModeScreen.Settings.onClick.Invoke();yield return null;yield return null;Assert.That(c.IsVisible,Is.False);
            e.Navigation.Navigate(PortraitEntryController.ModeId,false);yield return null;yield return null;Assert.That(c.IsVisible,Is.True);
        }
        [UnityTest] public IEnumerator EveryThemeKeepsSingleRabbitGroundedWithExactScaleAndNoRaycasts()
        {
            yield return Load("Lobby");var c=Object.FindFirstObjectByType<MenuThemeCycle>();
            for(int theme=0;theme<5;theme++) foreach(bool left in new[]{false,true}) {
                c.InspectionTime=theme*5+2.25;c.InspectionLeftward=left;yield return null;yield return null;
                Assert.That(c.Rabbit.rectTransform.sizeDelta,Is.EqualTo(new Vector2(96,128)));
                Assert.That(c.Outgoing.rectTransform.sizeDelta,Is.EqualTo(new Vector2(1408,2816)));
                Assert.That(c.Rabbit.rectTransform.localScale.x,Is.EqualTo(left?-1:1));
                var pan=MenuThemeCycle.PanAt(c.DisplayTime,theme);Assert.That(-c.Rabbit.rectTransform.anchoredPosition.y+56,Is.EqualTo(1488+pan.y*4));
                Assert.That(c.Rabbit.raycastTarget || c.Outgoing.raycastTarget || c.Incoming.raycastTarget,Is.False);
                Assert.That(c.Rabbit.GetComponents<Collider2D>().Length,Is.Zero);
            }
            c.InspectionTime=4.999;yield return null;yield return null;var sprite=c.Incoming.sprite;var position=c.Incoming.rectTransform.anchoredPosition;
            c.InspectionTime=5;yield return null;yield return null;Assert.That(c.Outgoing.sprite,Is.SameAs(sprite));Assert.That(c.Outgoing.rectTransform.anchoredPosition,Is.EqualTo(position));
            c.InspectionTime=null;double before=c.Elapsed;Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.That(c.Elapsed,Is.GreaterThan(before));Assert.That(Time.timeScale,Is.Zero);
        }
        [UnityTest] public IEnumerator ProtectedGameplaySceneDoesNotInstallMenu()
        {
            yield return Load("MoonGraphicsQA");Assert.That(Object.FindFirstObjectByType<MenuThemeCycle>(),Is.Null);
        }
    }
}
