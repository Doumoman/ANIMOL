using System;
using System.IO;
using ANIMOL.Menu;
using DiagonalPattern;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Menu.Editor
{
    // Updates the existing scene without rebuilding its catalog or character references.
    public static class MainLobbyV18Applier
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string Art = "Assets/ANIMOL/Sprites/UI/Main";
        static readonly string[] Themes = { "starway", "theater", "observatory", "sea" };

        [MenuItem("Tools/ANIMOL/Main Lobby/Apply V18 Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play mode and save the current scene before applying V18.");
            AssetDatabase.Refresh();
            foreach (var theme in Themes) Configure(Art + "/Backgrounds/Wide/" + theme + "-wide.png", false);
            Configure(Art + "/Frames/notebook-panel.png", true);
            Configure(Art + "/Chrome/telescope.png", false);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!lobby || !lobby.frame || !lobby.artArea || !lobby.dock || !lobby.logoRect || !lobby.campaignPanel)
                throw new InvalidDataException("Existing main lobby hierarchy is incomplete.");
            ApplyToScene(lobby);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ApplyToScene(MainLobbyController lobby)
        {
            var paper = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Frames/notebook-panel.png");
            var telescope = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Chrome/telescope.png");
            if (!paper || !telescope) throw new FileNotFoundException("V18 notebook or telescope sprite is missing.");
            lobby.wideScenery = new Sprite[Themes.Length];
            for (var i = 0; i < Themes.Length; i++)
            {
                lobby.wideScenery[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                    Art + "/Backgrounds/Wide/" + Themes[i] + "-wide.png");
                if (!lobby.wideScenery[i]) throw new FileNotFoundException("Missing V18 wide background: " + Themes[i]);
            }
            lobby.backgroundImage.sprite = lobby.wideScenery[0];
            lobby.backgroundTransitionImage.sprite = lobby.wideScenery[0];

            var mask = lobby.frame.GetComponent<RectMask2D>();
            if (mask) UnityEngine.Object.DestroyImmediate(mask);
            lobby.frame.anchorMin = Vector2.zero;
            lobby.frame.anchorMax = Vector2.one;
            lobby.frame.offsetMin = lobby.frame.offsetMax = Vector2.zero;

            var top = lobby.frame.Find("Top Control Paper") as RectTransform;
            if (!top)
            {
                var go = new GameObject("Top Control Paper", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                top = go.GetComponent<RectTransform>();
                top.SetParent(lobby.frame, false);
            }
            top.SetSiblingIndex(1);
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(.5f, 1);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0, 62);
            SetPaper(top.GetComponent<Image>(), paper);
            lobby.topPaper = top;
            var topBar = lobby.artArea.Find("Top Bar") as RectTransform;
            if (!topBar) topBar = top.Find("Top Bar") as RectTransform;
            if (!topBar) throw new InvalidDataException("Login, sound and settings row is missing.");
            topBar.SetParent(top, false);
            topBar.anchorMin = new Vector2(0, 1);
            topBar.anchorMax = Vector2.one;
            topBar.pivot = new Vector2(.5f, 1);
            topBar.sizeDelta = new Vector2(0, 44);
            topBar.anchoredPosition = new Vector2(0, -10);

            var dockImage = lobby.dock.GetComponent<Image>();
            if (dockImage) SetPaper(dockImage, paper);
            var ad = lobby.dock.Find("Ad Row");
            if (ad) UnityEngine.Object.DestroyImmediate(ad.gameObject);
            lobby.removeAdsButton = null;

            var dockPattern = lobby.dock.Find("Character Faces And Stars") as RectTransform;
            if (!dockPattern || !dockPattern.GetComponent<DiagonalPatternBackground>())
                throw new InvalidDataException("Existing face and star pattern is missing from the dock.");
            SetPatternOpacity(dockPattern, .085f);
            var topPattern = top.Find("Character Faces And Stars") as RectTransform;
            if (!topPattern) topPattern = UnityEngine.Object.Instantiate(dockPattern, top);
            topPattern.name = "Character Faces And Stars";
            topPattern.SetAsFirstSibling();
            topPattern.anchorMin = Vector2.zero;
            topPattern.anchorMax = Vector2.one;
            topPattern.offsetMin = new Vector2(2, 2);
            topPattern.offsetMax = new Vector2(-2, -2);
            SetPatternOpacity(topPattern, .10f);

            var campaign = lobby.campaignPanel.GetComponent<RectTransform>();
            var campaignImage = campaign.GetComponent<Image>();
            if (campaignImage)
            {
                // The campaign must cover the lobby controls even where a notebook frame is transparent.
                campaignImage.sprite = null;
                campaignImage.type = Image.Type.Simple;
                campaignImage.color = new Color(1f, .976f, .914f);
            }
            var campaignPattern = campaign.Find("Character Faces And Stars") as RectTransform;
            if (campaignPattern) SetPatternOpacity(campaignPattern, .065f);
            var telescopeImage = campaign.Find("Campaign Telescope")?.GetComponent<Image>();
            if (!telescopeImage)
            {
                var go = new GameObject("Campaign Telescope", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(campaign, false);
                telescopeImage = go.GetComponent<Image>();
            }
            var telescopeRect = telescopeImage.rectTransform;
            telescopeRect.SetSiblingIndex(Mathf.Max(1, campaign.childCount - 2));
            telescopeRect.anchorMin = telescopeRect.anchorMax = new Vector2(1, 1);
            telescopeRect.pivot = new Vector2(1, 1);
            telescopeRect.anchoredPosition = new Vector2(-14, -13);
            telescopeRect.sizeDelta = new Vector2(44, 44);
            telescopeImage.sprite = telescope;
            telescopeImage.preserveAspect = true;
            telescopeImage.raycastTarget = false;
            EditorUtility.SetDirty(lobby);
            lobby.ForceLayout();
        }

        static void SetPaper(Image image, Sprite paper)
        {
            image.sprite = paper;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        static void SetPatternOpacity(RectTransform rect, float opacity)
        {
            var effect = rect.GetComponent<DiagonalPatternBackground>();
            var image = rect.GetComponent<RawImage>();
            if (!effect || !image || !(image.texture is Texture2D texture))
                throw new InvalidDataException("Existing face and star pattern has no source texture: " + rect.name);
            effect.SetAppearance(texture, Color.clear, new Color(166f / 255f, 110f / 255f, 72f / 255f), opacity);
            image.raycastTarget = false;
        }

        static void Configure(string path, bool sliced)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing V18 sprite", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            if (sliced) importer.spriteBorder = new Vector4(48, 48, 48, 48);
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.RGBA32;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }
}
