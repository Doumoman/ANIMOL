using System;
using System.IO;
using System.Linq;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Editor
{
    public static class MainUiV7Builder
    {
        public const string Root="Assets/ANIMOL/UI/MainUiV7";
        [MenuItem("ANIMOL/Main UI V7/Import and apply to existing menus")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene changes.");
            OriginalDesignCorrectionBuilder.ApplyBackground();
            foreach(string name in new[]{"Bootstrap","Lobby"}) {
                var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/"+name+".unity");
                var nav=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UiNavigationService>(true)).Single();
                Apply(nav);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException(scene.path);
            }
            EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Bootstrap.unity");
        }
        public static void Apply(UiNavigationService nav)
        {
            // Physically retire the old presentation from these scene instances only.
            foreach(string name in new[]{"FiveThemeBackdrop","SafeArea/ScreenHost/PA1_LegacyLobby"}) {
                var old=nav.transform.Find(name);if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var cycle=nav.GetComponentInChildren<MenuThemeCycle>(true);
            if(cycle==null) {var go=new GameObject("MainUiV7Cycle");go.transform.SetParent(nav.transform,false);cycle=go.AddComponent<MenuThemeCycle>();}
            cycle.gameObject.name="MainUiV7Cycle";
            cycle.AuthorLayout(Resources.Load<FantasyBackgroundCatalog>("ANIMOLMainUiV7"));
            EditorUtility.SetDirty(cycle);
        }
    }
}
