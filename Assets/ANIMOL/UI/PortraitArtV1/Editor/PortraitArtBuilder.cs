using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ANIMOL.PortraitArtV1.Editor
{
    public static class PortraitArtBuilder
    {
        public const string Root="Assets/ANIMOL/UI/PortraitArtV1";
        public const string MainFontPath=Root+"/Fonts/PortraitUI_Raster.asset";
        private static readonly Color Ink=new Color32(77,43,30,255);
        private static TMP_FontAsset font;

        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root+"/Sprites","*.png")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
                importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteAlignment=9; settings.spritePivot=new Vector2(.5f,.5f);
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            string added="플레이 캠페인 경쟁전 협동 게임 모드 코인 조회 불가 검증된 코인 서버 미연결 기존 경쟁 허브 기존 협동 허브 다섯 테마 탐험 --";
            string corpus=new string((File.ReadAllText("Docs/PixelTypography/ui-corpus.txt")+added).Where(c=>!char.IsControl(c)).Distinct().OrderBy(c=>c).ToArray());
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            if(font==null) {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/pixelroborobo.otf");
                var fallback=MakeFont(source,"PortraitUI_Dynamic", "?",512,false);
                font=MakeFont(source,"PortraitUI_Raster",corpus,1024,true);
                font.fallbackFontAssetTable=new List<TMP_FontAsset>{fallback};
                foreach(string name in new[]{"Symbols_Fallback","NotoSansSymbols2-Regular_Fallback","NotoSansSymbols_Fallback"})
                    font.fallbackFontAssetTable.Add(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Typography/"+name+".asset"));
                EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font);
            }
            var missing=corpus.Where(c=>!font.HasCharacter(c,true,true)).ToArray();
            if(missing.Length>0) throw new InvalidOperationException("Missing glyphs: "+new string(missing));
            File.WriteAllText("Docs/PortraitArtV1/font-corpus.txt",corpus);
            var manifest=JObject.Parse(File.ReadAllText("Docs/PortraitArtV1/Source/ui_manifest.json"));
            Directory.CreateDirectory(Root+"/Prefabs"); Directory.CreateDirectory(Root+"/Resources"); AssetDatabase.Refresh();
            var start=BuildScreen("Start",manifest["screens"]["Start"]);
            var modes=BuildScreen("ModeSelect",manifest["screens"]["ModeSelect"]);
            var controller=new GameObject("ANIMOL Portrait Entry V1").AddComponent<PortraitEntryController>();
            controller.StartPrefab=start; controller.ModePrefab=modes;
            PrefabUtility.SaveAsPrefabAsset(controller.gameObject,Root+"/Resources/ANIMOLPortraitEntryV1.prefab");
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/PortraitArtV1/import-settings.txt","10 sprites: Single / FullRect / Point / PPU 32 / Uncompressed / Mipmap Off / Simple / PreserveAspect\nFont: RASTER_HINTED / sample16 / padding2 / Point / no mipmaps / static1024 + dynamic512\nCorpus="+corpus.Length+" static="+font.characterTable.Count+" missing="+missing.Length);
        }
        private static TMP_FontAsset MakeFont(Font source,string name,string corpus,int size,bool makeStatic)
        {
            var asset=TMP_FontAsset.CreateFontAsset(source,16,2,GlyphRenderMode.RASTER_HINTED,size,size,AtlasPopulationMode.Dynamic,!makeStatic);
            asset.name=name; asset.TryAddCharacters(corpus,out string missing);
            if(makeStatic) asset.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset,Root+"/Fonts/"+name+".asset");
            AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach(var tex in asset.atlasTextures) { tex.filterMode=FilterMode.Point; tex.wrapMode=TextureWrapMode.Clamp; tex.anisoLevel=0; AssetDatabase.AddObjectToAsset(tex,asset); }
            EditorUtility.SetDirty(asset); return asset;
        }
        private static PortraitArtScreen BuildScreen(string kind,JToken spec)
        {
            var root=Rect(kind,null); Stretch(root);
            var view=root.gameObject.AddComponent<PortraitArtScreen>();
            var slot=Rect("CharacterBackdropSlot",root); var art=spec["artSlot"];
            slot.anchorMin=V(art["anchorMin"]); slot.anchorMax=V(art["anchorMax"]);
            slot.offsetMin=new Vector2(0,(float)art["offsetBottom"]); slot.offsetMax=new Vector2(0,(float)art["offsetTop"]);
            view.CharacterBackdropSlot=slot;
            foreach(var property in ((JObject)spec["controls"]).Properties()) {
                string name=property.Name; var c=property.Value; var r=Rect(name,root);
                r.anchorMin=r.anchorMax=V(c["anchor"]);r.pivot=V(c["pivot"]);r.anchoredPosition=V(c["position"]);r.sizeDelta=V(c["size"]);
                var image=r.gameObject.AddComponent<Image>();
                image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/"+(string)c["sprite"]);
                image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=name!="CurrencyCapsule";
                if(name=="CurrencyCapsule") {
                    var coin=Rect("CoinIcon",r); Place(coin,new Vector2(0,1),new Vector2(0,1),new Vector2(25,-21),new Vector2(54,54));
                    var icon=coin.gameObject.AddComponent<Image>();icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/icon_coin.png");icon.preserveAspect=true;icon.raycastTarget=false;
                    var amount=Label("CurrencyTMP",r,"--",32,Ink);
                    Place(amount.rectTransform,new Vector2(0,1),new Vector2(0,.5f),new Vector2(102,-45),new Vector2(202,54));
                    amount.alignment=TextAlignmentOptions.MidlineLeft;view.Currency=amount;
                    var state=Label("CurrencyState",root,"코인 조회 불가",32,new Color32(242,225,195,255));
                    Place(state.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(144,-136),new Vector2(350,48));view.CurrencyState=state;
                    continue;
                }
                var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
                button.navigation=new Navigation{mode=Navigation.Mode.None};
                switch(name) {
                    case "PlayButton": view.Play=button;break; case "BackButton":view.Back=button;break;
                    case "AdButton":view.Ad=button;button.interactable=false;break;
                    case "ShopButton":view.Shop=button;break;case "SettingsButton":view.Settings=button;break;
                    case "CampaignCard":view.Campaign=button;break;
                    case "CompetitionCard":view.Competition=button;button.interactable=false;break;
                    case "CooperationCard":view.Cooperation=button;button.interactable=false;break;
                }
                if(c["label"]!=null) {
                    var label=Label("Label",r,(string)c["label"],name=="PlayButton"?80:64,Ink);
                    if(name=="PlayButton") { Stretch(label.rectTransform);label.margin=new Vector4(70,35,70,25); }
                    else {
                        Place(label.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(280,-48),new Vector2(500,92));label.alignment=TextAlignmentOptions.MidlineLeft;
                        var status=Label("Availability",r,name=="CampaignCard"?"다섯 테마 탐험":"서버 미연결",32,Ink);
                        Place(status.rectTransform,new Vector2(0,0),new Vector2(0,0),new Vector2(280,80),new Vector2(500,64));status.alignment=TextAlignmentOptions.MidlineLeft;
                        if(name=="CompetitionCard") view.CompetitionState=status;
                        if(name=="CooperationCard") view.CooperationState=status;
                    }
                }
                if(name=="AdButton") {
                    var state=Label("Unavailable",r,"미연결",32,new Color32(242,225,195,255));
                    Place(state.rectTransform,new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(0,-6),new Vector2(128,48));
                }
            }
            if(kind=="ModeSelect") {
                var title=Label("SectionHeading",root,"게임 모드",48,new Color32(242,225,195,255));
                Place(title.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,998),new Vector2(700,80));
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/"+kind+".prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject); return prefab.GetComponent<PortraitArtScreen>();
        }
        private static TextMeshProUGUI Label(string name,Transform parent,string text,int size,Color color)
        {
            var label=Rect(name,parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.text=text;label.fontSize=size;label.fontStyle=FontStyles.Normal;
            label.color=color;label.alignment=TextAlignmentOptions.Midline;label.raycastTarget=false;
            label.enableAutoSizing=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Overflow;
            return label;
        }
        private static Vector2 V(JToken token) => new Vector2((float)token[0],(float)token[1]);
        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }
        private static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; }
        private static void Place(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size) { r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size; }
        public static void OpenBootstrap()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Preserve current scene first.");
            EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Bootstrap.unity");ANIMOL.Editor.PortraitGameViewSetup.Use1080x1920();
        }
    }
}
