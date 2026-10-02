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
            // Retain the existing GUIDs while replacing the complete runtime bundle.
            const string source="Docs/Inbox/ANIMOL_main_ui_v7/runtime/assets";
            var originals=Directory.GetFiles(source,"*.png");
            if(originals.Length!=22) throw new InvalidDataException("V7 requires all 22 original PNGs.");
            foreach(var file in originals) File.Copy(file,Root+"/Textures/"+Path.GetFileName(file),true);
            AssetDatabase.Refresh();
            foreach(var file in Directory.GetFiles(Root+"/Textures","*.png")) {
                var path=file.Replace('\\','/');var t=(TextureImporter)AssetImporter.GetAtPath(path);
                t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaSource=TextureImporterAlphaSource.FromInput;
                t.alphaIsTransparency=false;t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;
                t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.maxTextureSize=1024;
                foreach(string platform in new[]{"Standalone","Android","iPhone","WebGL","Windows Store Apps"}) t.ClearPlatformTextureSettings(platform);
                t.SaveAndReimport();
            }
            Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
            const string pathCatalog=Root+"/Resources/ANIMOLMainUiV7.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<FantasyBackgroundCatalog>(pathCatalog);
            if(catalog==null) {catalog=ScriptableObject.CreateInstance<FantasyBackgroundCatalog>();AssetDatabase.CreateAsset(catalog,pathCatalog);}
            catalog.sourceVersion="7.0";
            catalog.themes=Enumerable.Range(1,5).Select(i=>new FantasyBackgroundCatalog.Theme {
                far=Texture($"T{i:00}-far"),mid=Texture($"T{i:00}-mid"),platform=Texture($"T{i:00}-platform"),near=Texture($"T{i:00}-near")
            }).ToArray();
            catalog.rabbitRight=Texture("rabbit-run-right");catalog.rabbitLeft=Texture("rabbit-run-left");
            catalog.compositor=AssetDatabase.LoadAssetAtPath<Shader>("Assets/ANIMOL/UI/FiveThemeMenuMaps/FantasyBackground.shader");
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
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
        private static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+".png");
    }
}
