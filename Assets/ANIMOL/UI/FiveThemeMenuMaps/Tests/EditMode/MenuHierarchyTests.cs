using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Tests
{
    public class MenuHierarchyTests
    {
        [TestCase("Bootstrap")]
        [TestCase("Lobby")]
        public void SavedScenesContainViewsAndBackdropBeforePlay(string name)
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/ANIMOL/Scenes/"+name+".unity");
            try {
                var roots=scene.GetRootGameObjects();
                var entry=roots.SelectMany(r=>r.GetComponentsInChildren<PortraitEntryController>(true)).Single();
                var cycle=roots.SelectMany(r=>r.GetComponentsInChildren<MenuThemeCycle>(true)).Single();
                Assert.That(entry.HasAuthoredViews && cycle.HasAuthoredLayout,Is.True);
                var nav=entry.GetComponentInParent<UiNavigationService>();
                var host=nav.transform.Find("SafeArea/ScreenHost");
                Assert.That(entry.StartScreen.transform.parent,Is.EqualTo(host));
                Assert.That(cycle.Rabbit.transform.parent.name,Is.EqualTo("FiveThemeBackdrop"));
                Assert.That(cycle.Outgoing.sprite,Is.Not.Null);
                Assert.That(cycle.Outgoing.rectTransform.sizeDelta,Is.EqualTo(new Vector2(1408,2816)));
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Count(),Is.EqualTo(1));
                Assert.That(entry.StartScreen.gameObject.activeSelf,Is.EqualTo(name=="Bootstrap"));
                if(name=="Lobby") {
                    Assert.That(entry.ModeScreen.transform.parent,Is.EqualTo(host));
                    Assert.That(entry.ModeScreen.gameObject.activeSelf,Is.True);
                    Assert.That(host.Find("PA1_LegacyLobby").gameObject.activeSelf,Is.False);
                    Assert.That(entry.ModeScreen.GetComponentsInChildren<Transform>(true).Count(t=>t.name.EndsWith("Readability")),Is.EqualTo(4));
                }
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void HorizontalTravelIsFiveTimesLargerWithUnchangedGroundTravel()
        {
            var first=MenuThemeCycle.PanAt(4.5,1);var last=MenuThemeCycle.PanAt(10,1);
            Assert.That(Mathf.Abs(last.x-first.x)*4,Is.EqualTo(240));
            Assert.That(Mathf.Abs(last.y-first.y)*4,Is.EqualTo(32));
        }
    }
}
