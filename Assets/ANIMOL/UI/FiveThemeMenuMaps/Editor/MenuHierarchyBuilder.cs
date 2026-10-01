using System;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.FiveThemeMenu.Editor
{
    /// <summary>Author scene overrides only. Never applies changes to legacy UI prefabs.</summary>
    public static class MenuHierarchyBuilder
    {
        [MenuItem("ANIMOL/Five Theme Menu/Author Bootstrap and Lobby hierarchy")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your scene edits before authoring.");
            foreach(string name in new[]{"Bootstrap","Lobby"}) {
                var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/"+name+".unity");
                // OpenScene unloads unused resource assets; resolve after each open.
                var catalog=Resources.Load<MenuThemeCatalog>("ANIMOLFiveThemeMenu");
                var prefab=Resources.Load<PortraitEntryController>("ANIMOLPortraitEntryV1");
                if(catalog==null || prefab==null) throw new InvalidOperationException("Missing existing menu resources.");
                var nav=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UiNavigationService>(true)).Single();
                var host=nav.transform.Find("SafeArea/ScreenHost");
                var entry=nav.GetComponentInChildren<PortraitEntryController>(true);
                if(entry==null) {
                    entry=((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,nav.transform)).GetComponent<PortraitEntryController>();
                    entry.name="PortraitEntryController";
                }
                var start=entry.StartScreen;
                if(start==null) {
                    start=((GameObject)PrefabUtility.InstantiatePrefab(entry.StartPrefab.gameObject,host)).GetComponent<PortraitArtScreen>();
                    start.name=PortraitEntryController.StartId;
                }
                var mode=entry.ModeScreen;
                if(name=="Lobby" && mode==null) {
                    var old=host.Find(PortraitEntryController.ModeId);
                    if(old!=null) {old.name="PA1_LegacyLobby";old.gameObject.SetActive(false);}
                    mode=((GameObject)PrefabUtility.InstantiatePrefab(entry.ModePrefab.gameObject,host)).GetComponent<PortraitArtScreen>();
                    mode.name=PortraitEntryController.ModeId;
                }
                entry.BindAuthoredViews(start,mode);
                MenuThemeSkin.Apply(entry,catalog);
                if(mode!=null) {
                    mode.Ad.interactable=mode.Competition.interactable=mode.Cooperation.interactable=false;
                    mode.Currency.text="--";mode.CurrencyState.text="코인 조회 불가";
                    mode.CompetitionState.text=mode.CooperationState.text="서버 미연결";
                }
                foreach(Transform child in host) child.gameObject.SetActive(child==(name=="Bootstrap" ? start.transform : mode.transform));
                var navData=new SerializedObject(nav);navData.FindProperty("initialScreenId").stringValue=name=="Bootstrap" ? PortraitEntryController.StartId : PortraitEntryController.ModeId;navData.ApplyModifiedPropertiesWithoutUndo();
                var cycle=nav.GetComponentInChildren<MenuThemeCycle>(true);
                if(cycle==null) {var go=new GameObject("FiveThemeMenuCycle");go.transform.SetParent(nav.transform,false);cycle=go.AddComponent<MenuThemeCycle>();}
                cycle.AuthorLayout(catalog);
                // Record every changed property on the scene's prefab instances, not on
                // the shared prefab assets (which can contain unrelated user work).
                foreach(var component in nav.GetComponentsInChildren<Component>(true))
                    if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                foreach(var t in nav.GetComponentsInChildren<Transform>(true))
                    if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save "+scene.path);
            }
            EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Bootstrap.unity");
            Selection.activeGameObject=UnityEngine.Object.FindFirstObjectByType<MenuThemeCycle>().gameObject;
        }
    }
}
