using System;
using System.IO;
using System.Linq;
using ANIMOL.Typography;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.MissingUiV1.Project.Editor
{
    public static class MissingUiProjectBuilder
    {
        public const string Root="Assets/ANIMOL/UI/MissingUiV1";
        public const string ResourcesPath=Root+"/Project/Resources/ANIMOLMissingUiV1";
        public static void Build()
        {
            var profile=AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>("Assets/ANIMOL/Typography/PixelTypographyProfile.asset");
            if(profile==null || profile.Font==null)throw new InvalidOperationException("Existing pixel typography is required.");
            ANIMOL.MissingUiV1.Editor.MissingUiV1Builder.ConfiguredFont=profile.Font;
            ANIMOL.MissingUiV1.Editor.MissingUiV1Builder.BuildMissingUiPreviewWithConfiguredFont();
            BuildOperational();
        }
        public static void BuildOperational()
        {
            var profile=AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>("Assets/ANIMOL/Typography/PixelTypographyProfile.asset");
            Directory.CreateDirectory(ResourcesPath);AssetDatabase.Refresh();
            var path=ResourcesPath+"/Art.asset";
            var art=AssetDatabase.LoadAssetAtPath<MissingUiArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<MissingUiArt>();AssetDatabase.CreateAsset(art,path);}
            art.Typography=profile;
            art.LegacyFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/ANIMOL/UI/PortraitArtV1/Fonts/pixelroborobo.otf");
            if(art.LegacyFont==null)throw new InvalidOperationException("Existing legacy font source required.");
            art.Panel=Existing("UI_Common_Panel_Main");art.Primary=Existing("UI_Common_Button_Primary");art.Secondary=Existing("UI_Common_Button_Secondary");art.Back=Existing("UI_Icon_Back");
            art.Row=New("common/UI_Common_RowFrame");art.Product=New("store/UI_Store_ProductFrame");art.Price=New("store/UI_Store_PricePlate");
            art.Growth=New("store/UI_Store_Art_AccountGrowthPack");art.Emotes=New("store/UI_Store_Art_EmoteBundle");art.Restore=New("store/UI_Icon_RestorePurchase");
            art.ScrollTrack=New("common/UI_Common_ScrollTrack");art.ScrollThumb=New("common/UI_Common_ScrollThumb");
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssetIfDirty(art);
            var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            GameObject root=null;
            try
            {
                var parent=new GameObject("Task1BuildRoot");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent,preview);root=parent;
                var view=MissingStoreView.Build(parent.transform,art);
                PrefabUtility.SaveAsPrefabAsset(view.gameObject,ResourcesPath+"/Store.prefab");
            }
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
            Debug.Log("MISSING_UI_TASK1: production Store prefab and Art saved; legacy scenes/prefabs untouched. Scene-load installer retains SC11 navigation and common modal listeners.");
        }
        private static Sprite Existing(string name)=>Required("Assets/ANIMOL/UI/ProductionV1/Art/"+name+".png");
        private static Sprite New(string path)=>Required(Root+"/Sprites/"+path+".png");
        private static Sprite Required(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path)??throw new InvalidOperationException("Missing sprite "+path);
    }
}
