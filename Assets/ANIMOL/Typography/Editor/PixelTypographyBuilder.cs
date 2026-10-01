using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ANIMOL.Typography.Editor
{
    public static class PixelTypographyBuilder
    {
        public const string Root = "Assets/ANIMOL/Typography";
        public const string ProfilePath = Root + "/PixelTypographyProfile.asset";
        public const string FontPath = Root + "/Pixelroborobo_UI.asset";
        [MenuItem("ANIMOL/Pixel Typography/Build pilot fonts")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var corpus = new SortedSet<char>(Enumerable.Range(32,95).Select(i=>(char)i));
            var strings = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ANIMOL/Prefabs/UI"}))
                foreach (var text in AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponentsInChildren<Text>(true)) strings.Add(text.text);
            foreach (var file in Directory.GetFiles("Assets/ANIMOL/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (Match match in Regex.Matches(File.ReadAllText(file), "\"(?:[^\"\\\\]|\\\\.)*\""))
                    strings.Add(match.Value);
            foreach (var value in strings) foreach (char c in value) if (!char.IsControl(c) && !char.IsSurrogate(c)) corpus.Add(c);
            Directory.CreateDirectory("Docs/PixelTypography");
            File.WriteAllText("Docs/PixelTypography/ui-corpus.txt",new string(corpus.ToArray()));
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/ANIMOL/Fonts/pixelroborobo.otf");
            var dynamic = Create(source, "Pixelroborobo_Fallback", "?",512,false);
            var symbolsSource=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            var symbols=Create(symbolsSource,"Symbols_Fallback","?",512,false);
            var main = Create(source,"Pixelroborobo_UI",new string(corpus.ToArray()),1024,true);
            main.fallbackFontAssetTable = new List<TMP_FontAsset>{dynamic,symbols};
            var profile=AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>(ProfilePath);
            if(profile == null) { profile=ScriptableObject.CreateInstance<PixelTypographyProfile>(); AssetDatabase.CreateAsset(profile,ProfilePath); }
            profile.Font=main; profile.CommonUiApproved=false;
            EditorUtility.SetDirty(main); EditorUtility.SetDirty(profile);
            Directory.CreateDirectory(Root+"/Resources"); AssetDatabase.Refresh();
            var go=new GameObject("ANIMOL Pixel Typography",typeof(PixelTypographyInstaller)); go.GetComponent<PixelTypographyInstaller>().Profile=profile;
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Resources/ANIMOLPixelTypography.prefab"); UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            FontEngine.LoadFontFace(source,16);
            int hangul=Enumerable.Range(0xAC00,11172).Count(c=>HasSourceGlyph((uint)c));
            var unsupported=corpus.Where(c=>!main.HasCharacter(c)&&!HasSourceGlyph(c)).ToArray();
            var missing=new List<char>(); foreach(var c in unsupported) if(!symbols.HasCharacter(c,true,true)) missing.Add(c);
            profile.EnsurePointSampling();
            File.WriteAllText("Docs/PixelTypography/font-build.json",JsonUtility.ToJson(new BuildInfo {
                corpusCharacters=corpus.Count,staticCharacters=main.characterTable.Count,sourceHangul=hangul,
                primaryMissing=new string(unsupported),unsupportedEverywhere=new string(missing.ToArray()),
                renderMode=main.atlasRenderMode.ToString(),pointSize=main.faceInfo.pointSize,atlasWidth=main.atlasWidth,
                atlasHeight=main.atlasHeight,atlasCount=main.atlasTextureCount,mipCount=main.atlasTexture.mipmapCount,
                filter=main.atlasTexture.filterMode.ToString(),shader=main.material.shader.name },true));
            AssetDatabase.SaveAssets();
            CompleteSymbols();
        }
        public static void CompleteSymbols()
        {
            AssetDatabase.Refresh();
            var main=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            string corpus=File.ReadAllText("Docs/PixelTypography/ui-corpus.txt");
            string missing=new string(corpus.Where(c=>!main.HasCharacter(c,true,true)).ToArray());
            foreach(string family in new[]{"NotoSansSymbols2-Regular","NotoSansSymbols"}) {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/ThirdParty/"+family+".ttf");
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/"+family+"_Fallback.asset");
                if(font==null) font=Create(source,family+"_Fallback",missing,512,false);
                if(!main.fallbackFontAssetTable.Contains(font)) main.fallbackFontAssetTable.Add(font);
            }
            var remaining=corpus.Where(c=>!main.HasCharacter(c,true,true)).ToArray();
            AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>(ProfilePath).EnsurePointSampling();
            EditorUtility.SetDirty(main);AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/PixelTypography/final-coverage.txt", "Corpus: "+corpus.Length+"\nMissing with fallbacks: "+new string(remaining)+"\nMissing count: "+remaining.Length);
        }
        private static bool HasSourceGlyph(uint c) => FontEngine.TryGetGlyphWithUnicodeValue(c,GlyphLoadFlags.LOAD_DEFAULT,out _);
        private static TMP_FontAsset Create(Font source,string name,string chars,int size,bool makeStatic)
        {
            string path=Root+"/"+name+".asset";
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null) throw new InvalidOperationException("Font already exists; do not silently replace: "+path);
            var font=TMP_FontAsset.CreateFontAsset(source,16,2,GlyphRenderMode.RASTER_HINTED,size,size,AtlasPopulationMode.Dynamic,!makeStatic);
            font.name=name; font.TryAddCharacters(chars,out string missing);
            if(makeStatic) font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,path);
            font.material.name=name+" Bitmap"; AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures) { texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;texture.anisoLevel=0;AssetDatabase.AddObjectToAsset(texture,font); }
            EditorUtility.SetDirty(font); return font;
        }
        [MenuItem("ANIMOL/Pixel Typography/Approve common UI after visual QA")]
        public static void ApproveCommon()
        {
            var profile=AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>(ProfilePath);
            profile.CommonUiApproved=true; EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        }
        public static void OpenMoon() { EnsureClean(); EditorSceneManager.OpenScene("Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity"); ANIMOL.Editor.PortraitGameViewSetup.Use1080x1920(); }
        public static void OpenResults() { EnsureClean(); EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Results.unity"); }
        public static void OpenLobby() { EnsureClean(); EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Lobby.unity"); }
        public static void CaptureCommonPrefabs()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play first.");
            var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ANIMOL/Prefabs/UI"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>!p.EndsWith("UI_CommonRoot.prefab")).Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
            PixelTypographyCapture.CaptureCommon(prefabs);
        }
        public static void ShowResultsFixture()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play Moon QA first.");
            ANIMOL.Gameplay.DevRunResultState.Record("MOON-QA-TYPOGRAPHY",123.4f,3,ANIMOL.Core.CampaignRunOutcome.Cleared,new ANIMOL.Core.RetryLayoutIdentity("MOON-QA-TYPOGRAPHY",0,0));
            UnityEngine.SceneManagement.SceneManager.LoadScene("Results");
        }
        private static void EnsureClean()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop play first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene first.");
        }
        [Serializable] private sealed class BuildInfo {
            public int corpusCharacters,staticCharacters,sourceHangul,atlasWidth,atlasHeight,atlasCount,mipCount;
            public float pointSize; public string primaryMissing,unsupportedEverywhere,renderMode,filter,shader;
        }
    }
}
