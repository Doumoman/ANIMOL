using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Editor
{
    public static class MenuThemeBuilder
    {
        public const string Root="Assets/ANIMOL/UI/FiveThemeMenuMaps";
        [MenuItem("ANIMOL/Main UI V7/Rebuild control sprites")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building assets.");
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Root+"/Sprites","*.png")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,.5f);
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
            const string assetPath=Root+"/Resources/ANIMOLFiveThemeMenu.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<MenuThemeCatalog>(assetPath);
            if(catalog==null) {catalog=ScriptableObject.CreateInstance<MenuThemeCatalog>();AssetDatabase.CreateAsset(catalog,assetPath);}
            catalog.back=Sprite("button_back_48.png");catalog.ad=Sprite("button_ad_48.png");catalog.shop=Sprite("button_shop_48.png");catalog.settings=Sprite("button_settings_48.png");catalog.coin=Sprite("icon_coin_hud_24.png");
            catalog.campaign=Sprite("Mode_campaign_164x88.png");catalog.competition=Sprite("Mode_competition_164x88.png");catalog.cooperation=Sprite("Mode_cooperation_164x88.png");
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/"+file);
    }
}
