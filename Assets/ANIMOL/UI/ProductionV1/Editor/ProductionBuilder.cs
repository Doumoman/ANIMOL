using System.IO;
using System.Linq;
using ANIMOL.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ANIMOL.UI;

namespace ANIMOL.ProductionV1.Editor
{
    [InitializeOnLoad]
    public static class ProductionBuilder
    {
        public const string Root="Assets/ANIMOL/UI/ProductionV1";
        // Existing M1/M3/M5 and menu hierarchy generators save these scenes.
        // Author the extension before serialization, so regeneration cannot omit it.
        static ProductionBuilder(){EditorSceneManager.sceneSaving-=BeforeSceneSave;EditorSceneManager.sceneSaving+=BeforeSceneSave;}
        private static void BeforeSceneSave(Scene scene,string path)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&(scene.name=="Lobby"||scene.name=="Bootstrap"))Apply(scene);
        }
        public static void Apply(Scene scene)
        {
            var catalog=Resources.Load<ProductionCatalog>("ANIMOLProductionV1/Catalog");if(catalog==null)return;
            foreach(var nav in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UiNavigationService>(true)))
            {
                var host=nav.transform.Find("SafeArea/ScreenHost");
                bool missing=ProductionController.Screens.Any(id=>host.Find(id)!=null&&host.Find(id+"/ProductionV1")==null);
                if(nav.transform.Find("ProductionV1Background")!=null)
                {
                    if(!missing)continue;
                    // A generator replaced one or more existing screens. Re-author only this module.
                    foreach(var t in nav.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ProductionV1"||t.name=="ProductionV1Background"||t.name=="ProductionV1Modal"||t.name=="PV1_LobbyUpgrade").ToArray())Object.DestroyImmediate(t.gameObject);
                    var previous=nav.GetComponent<ProductionController>();if(previous!=null)Object.DestroyImmediate(previous);
                }
                var controller=nav.GetComponent<ProductionController>();if(controller==null)controller=nav.gameObject.AddComponent<ProductionController>();
                controller.Author(catalog);
                foreach(var component in nav.GetComponentsInChildren<Component>(true))
                    if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                foreach(var t in nav.GetComponentsInChildren<Transform>(true))
                    if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
        }
        public static void AuthorScenes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new System.InvalidOperationException("Unsaved scene edits must be preserved.");
            var previous=SceneManager.GetActiveScene().path;
            foreach(var name in new[]{"Bootstrap","Lobby"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/"+name+".unity");Apply(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }
        public static void RebuildOwnedHierarchy()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new System.InvalidOperationException("Unsaved scene edits must be preserved.");
            foreach(string name in new[]{"Bootstrap","Lobby"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/"+name+".unity");
                foreach(var nav in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UiNavigationService>(true)))
                {
                    foreach(var t in nav.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ProductionV1"||t.name=="ProductionV1Background"||t.name=="ProductionV1Modal"||t.name=="PV1_LobbyUpgrade").ToArray())Object.DestroyImmediate(t.gameObject);
                    var c=nav.GetComponent<ProductionController>();if(c!=null)Object.DestroyImmediate(c);
                }
                Apply(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }
        public static void AuthorGameTheme(int theme)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new System.InvalidOperationException("Unsaved scene.");
            string id="T"+theme.ToString("00");
            string source="Docs/Inbox/ANIMOL_UI_Background_Production_v1/ANIMOL_ui_production_v1/game-art-provisional/";
            Directory.CreateDirectory(Root+"/GameArt");
            foreach(string layer in new[]{"Far","Mid","Decor"})
            {
                string name="BG_Game_"+id+"_"+layer+".png",path=Root+"/GameArt/"+name;
                if(!File.Exists(path))File.Copy(source+name,path);
                AssetDatabase.ImportAsset(path);var t=(TextureImporter)AssetImporter.GetAtPath(path);
                t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=16;
                t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;t.mipmapEnabled=false;
                t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(settings);t.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Campaign/ThemeRuntime_"+id+".unity");
            if(scene.GetRootGameObjects().Any(x=>x.name=="ProductionV1_GameBackdrop"))return;
            var root=new GameObject("ProductionV1_GameBackdrop");var backdrop=root.AddComponent<ProductionGameBackdrop>();
            backdrop.WorldCamera=Object.FindFirstObjectByType<Camera>();backdrop.Layers=new SpriteRenderer[3];
            for(int i=0;i<3;i++)
            {
                string layer=new[]{"Far","Mid","Decor"}[i];var go=new GameObject(layer);go.transform.SetParent(root.transform,false);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/GameArt/BG_Game_"+id+"_"+layer+".png");
                renderer.sortingLayerName="Default";renderer.sortingOrder=-30+i*10;
                // Reduced contrast keeps non-collidable decoration distinct from actual terrain.
                renderer.color=i==0?Color.white:new Color(.72f,.72f,.80f,i==2?.38f:.62f);
                go.transform.position=new Vector3(0,0,10+i);backdrop.Layers[i]=renderer;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play before importing.");
            var manifest=JObject.Parse(File.ReadAllText(Root+"/manifest.json"));
            foreach(var a in manifest["runtimeAssets"])
            {
                string path=Root+"/Art/"+Path.GetFileName((string)a["path"]);
                AssetDatabase.ImportAsset(path);
                var t=(TextureImporter)AssetImporter.GetAtPath(path);
                t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
                t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;
                t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;
                t.maxTextureSize=2048;t.spritePixelsPerUnit=100;t.wrapMode=TextureWrapMode.Clamp;
                var b=a["sliceLBRTop"].Values<float>().ToArray();t.spriteBorder=new Vector4(b[0],b[1],b[2],b[3]);
                var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Custom;
                settings.spritePivot=new Vector2((float)a["pivot"][0],(float)a["pivot"][1]);t.SetTextureSettings(settings);
                foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})t.ClearPlatformTextureSettings(platform);
                t.SaveAndReimport();
            }
            string cp=Root+"/Resources/ANIMOLProductionV1/Catalog.asset";
            var c=AssetDatabase.LoadAssetAtPath<ProductionCatalog>(cp);
            if(c==null){c=ScriptableObject.CreateInstance<ProductionCatalog>();AssetDatabase.CreateAsset(c,cp);}
            c.Art=manifest["runtimeAssets"].Select(a=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+Path.GetFileName((string)a["path"]))).ToArray();
            if(c.LoadingFrames==null||c.LoadingFrames.Length!=8)
            {
                c.LoadingFrames=new Sprite[8];var tex=c.Find("UI_Common_Loading_Animation").texture;
                for(int i=0;i<8;i++){var s=Sprite.Create(tex,new Rect(i%4*32,(1-i/4)*32,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);s.name="Loading_"+i;AssetDatabase.AddObjectToAsset(s,c);c.LoadingFrames[i]=s;}
            }
            c.Font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Typography/Pixelroborobo_UI.asset");
            c.ApprovedCoin=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/ANIMOL/UI/FiveThemeMenuMaps/Sprites"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.ToLowerInvariant().Contains("coin")).Select(AssetDatabase.LoadAssetAtPath<Sprite>).FirstOrDefault();
            c.Campaign=AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset");
            c.Account=AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            c.Growth=AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>("Assets/ANIMOL/Data/Meta/GrowthEconomyPolicyCatalog.asset");
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
            Debug.Log("[ProductionV1] 60 separate sprites imported. Use Apply authored hierarchy to menus to save static screen objects.");
        }
    }
}
