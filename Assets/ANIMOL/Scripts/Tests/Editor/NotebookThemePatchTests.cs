using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookThemePatchTests
    {
        [Test] public void SelectedRootMenuSupportsUndoRedoAndTransactionalFailureWithoutReplacingMap()
        {
            var previousSelection=Selection.activeObject;
            var previousSetup=EditorSceneManager.GetSceneManagerSetup();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                var root=instance.GetComponentInChildren<NotebookRoot>(true);
                var installed=root.mapThemeNavigation;
                UnityEngine.Object.DestroyImmediate(installed.rail.gameObject);UnityEngine.Object.DestroyImmediate(installed);
                root.mapThemeNavigation=null;
                var map=root.stageScreen;var main=root.mainScreen;var settings=root.settingsScreen;
                var modal=root.modal;var gateway=root.gateway;var sheet=root.stageSheet;var scroll=root.mapScroll;
                Selection.activeGameObject=root.gameObject;
                Assert.That(EditorApplication.ExecuteMenuItem("ANIMOL/New Notebook UI/Apply Theme Navigation To Selected Notebook"),Is.True);
                Assert.That(root.mapThemeNavigation,Is.Not.Null);
                Undo.PerformUndo();Assert.That(root.mapThemeNavigation,Is.Null);
                Assert.That(scroll.GetComponent<NotebookMapThemeNavigation>(),Is.Null);
                Assert.That(map.transform.Find("Roman Theme Navigation"),Is.Null);
                Undo.PerformRedo();var nav=root.mapThemeNavigation;
                Assert.That(nav,Is.Not.Null);Assert.That(nav.rail,Is.Not.Null);
                Assert.That(nav.owner,Is.EqualTo(root));Assert.That(nav.scroll,Is.EqualTo(scroll));
                UnityEngine.Object.DestroyImmediate(nav.rail.GetComponent<CanvasGroup>());
                var position=nav.rail.anchoredPosition;
                Assert.Throws<InvalidOperationException>(()=>William.NotebookUI.Editor.NotebookInstaller.ApplyThemeNavigationToSelectedNotebook());
                Assert.That(root.mapThemeNavigation,Is.EqualTo(nav));Assert.That(nav.rail.anchoredPosition,Is.EqualTo(position));
                Assert.That(nav.rail.GetComponent<CanvasGroup>(),Is.Null);
                Assert.That(root.stageScreen,Is.EqualTo(map));Assert.That(root.mainScreen,Is.EqualTo(main));
                Assert.That(root.settingsScreen,Is.EqualTo(settings));Assert.That(root.modal,Is.EqualTo(modal));
                Assert.That(root.gateway,Is.EqualTo(gateway));Assert.That(root.stageSheet,Is.EqualTo(sheet));
            }
            finally
            {
                if(previousSetup.Length>0 && previousSetup.All(s=>!string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                Selection.activeObject=previousSelection;
            }
        }

        [Test] public void EntryLinksAreEightShortNonCrossingPairsAndDoNotConsumeGlobalRandom()
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var generate = typeof(NotebookCampaignSkyTransition).GetMethod("GenerateEntryLinks", flags);
            var cross = typeof(NotebookCampaignSkyTransition).GetMethod("LinksCross", flags);
            var project = typeof(NotebookCampaignSkyTransition).GetMethod("LinkProjection", flags);
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            foreach (float aspect in new[] { .25f, 360f/640, 390f/844, 445f/773, 1.6f, 4f })
            {
                string previous = "";
                for (uint seed = 0; seed < 128; seed++)
                {
                    var links = (List<Vector2Int>)generate.Invoke(null, new object[] {aspect, seed, previous});
                    Assert.That(links.Count, Is.EqualTo(8));
                    Assert.That(links.Distinct().Count(), Is.EqualTo(8));
                    string signature = string.Join(",", links.Select(p=>p.x*27+p.y).OrderBy(i=>i));
                    Assert.That(signature, Is.Not.EqualTo(previous)); previous = signature;
                    var degree = new int[27];
                    for (int i=0; i<links.Count; i++)
                    {
                        var pair=links[i];
                        Assert.That(pair.x, Is.InRange(0,26)); Assert.That(pair.y, Is.InRange(pair.x+1,26));
                        Assert.That(pair.x/9, Is.EqualTo(pair.y/9)); degree[pair.x]++; degree[pair.y]++;
                        var p=(Vector2)project.Invoke(null,new object[]{pair.x,1f,aspect});
                        var q=(Vector2)project.Invoke(null,new object[]{pair.y,1f,aspect});
                        Assert.That(Vector2.Distance(p,q), Is.LessThanOrEqualTo(Mathf.Max(.68f,.45f*aspect)+.0001f));
                        for(int j=0;j<i;j++) Assert.That((bool)cross.Invoke(null,new object[]{pair,links[j],aspect}),Is.False);
                    }
                    Assert.That(degree.Max(),Is.LessThanOrEqualTo(2));
                }
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(randomBefore));
        }

        [Test] public void InstalledNavigationUsesOnlyExistingMapCatalogAndFiveStaticSprites()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab");
            var root=prefab.GetComponentInChildren<NotebookRoot>(true); var nav=root.mapThemeNavigation;
            Assert.That(nav,Is.Not.Null); Assert.That(nav.owner,Is.EqualTo(root)); Assert.That(nav.scroll,Is.EqualTo(root.mapScroll));
            Assert.That(nav.gameObject,Is.EqualTo(root.mapScroll.gameObject));
            Assert.That(nav.rail.parent,Is.EqualTo(root.stageScreen.transform));
            Assert.That(nav.rail.sizeDelta,Is.EqualTo(new Vector2(984,240)));
            Assert.That(nav.rail.anchoredPosition,Is.EqualTo(new Vector2(0,24)));
            var catalog=NotebookCatalog.Parse(root.catalogJson);
            CollectionAssert.AreEqual(catalog.themes.Select(t=>(float)t.startY),nav.themeStartY);
            for(int i=0;i<4;i++)
            {
                var button=nav.buttons[i]; var rect=(RectTransform)button.transform;
                Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(234,192)));
                Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(6+246*i,-24)));
                Assert.That(button.image.color,Is.EqualTo(Color.white)); Assert.That(button.image.type,Is.EqualTo(Image.Type.Sliced));
                Assert.That(button.onClick.GetPersistentEventCount(),Is.Zero);
                var label=button.GetComponentInChildren<TMPro.TMP_Text>();
                Assert.That(label.text,Is.EqualTo(new[]{"I","II","III","IV"}[i]));
                Assert.That(label.color,Is.EqualTo(Color.white));
                Assert.That(AssetDatabase.GetAssetPath(label.font),Is.EqualTo("Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset"));
            }
            foreach(string state in new[]{"Normal","Highlighted","Pressed","Disabled","Selected"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/ANIMOL/Art/UI/ConstellationMap/Textures/MapTheme/Tab_"+state+".png");
                Assert.That(importer.spriteBorder,Is.EqualTo(Vector4.one*32)); Assert.That(importer.spritePixelsPerUnit,Is.EqualTo(100));
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Bilinear)); Assert.That(importer.mipmapEnabled,Is.False);
                Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect));
            }
            Assert.That(ShaderUtil.GetShaderMessages(root.campaignSkyTransition.transitionShader).Where(m=>m.severity.ToString()=="Error"),Is.Empty);
            Assert.That(root.nodes.Length,Is.EqualTo(110)); Assert.That(root.edges.Length,Is.EqualTo(106)); Assert.That(root.mapStarFrames.Length,Is.EqualTo(48));
        }
    }
}
