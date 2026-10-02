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
        [Test] public void ReapplyingLayoutKeepsOneV7BackdropAboveItsFill()
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/ANIMOL/Scenes/Lobby.unity");
            try {
                var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MenuThemeCycle>(true)).Single();
                var output=c.Output;
                for(int i=0;i<3;i++) c.AuthorLayout(c.Catalog);
                Assert.That(c.Output,Is.SameAs(output));
                Assert.That(c.Catalog.sourceVersion,Is.EqualTo("7.0"));
                var fill=c.transform.parent.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Where(x=>x.name=="MainUiBackdropFill").ToArray();
                Assert.That(fill.Length,Is.EqualTo(1));
                Assert.That(fill[0].transform.GetSiblingIndex(),Is.LessThan(output.transform.GetSiblingIndex()));
                Assert.That(output.transform.GetSiblingIndex(),Is.LessThan(c.transform.parent.Find("SafeArea").GetSiblingIndex()));
                Assert.That(fill[0].raycastTarget,Is.False);
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
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
                Assert.That(cycle.Output.name,Is.EqualTo("MainUiV6Backdrop"));
                Assert.That(cycle.Output.raycastTarget,Is.False);
                Assert.That(nav.transform.Find("FiveThemeBackdrop"),Is.Null);
                Assert.That(cycle.Catalog.themes.Length,Is.EqualTo(5));
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Count(),Is.EqualTo(1));
                Assert.That(entry.StartScreen.gameObject.activeSelf,Is.EqualTo(name=="Bootstrap"));
                if(name=="Lobby") {
                    Assert.That(entry.ModeScreen.transform.parent,Is.EqualTo(host));
                    Assert.That(entry.ModeScreen.gameObject.activeSelf,Is.True);
                    Assert.That(host.Find("PA1_LegacyLobby"),Is.Null);
                    Assert.That(entry.ModeScreen.GetComponentsInChildren<Transform>(true).Count(t=>t.name.EndsWith("Readability")),Is.EqualTo(4));
                }
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
