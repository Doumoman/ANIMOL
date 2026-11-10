#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Animol.UI.Pattern;
using DiagonalPattern;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace William.NotebookUI.Editor
{
    /// <summary>Additive HUD factory. Never replaces a puzzle scene, camera, input router or BM owner.</summary>
    public static class NotebookGameplayInstaller
    {
        private const string Root = "Assets/ANIMOL/", GameplayTex = Root + "Art/UI/InGame/Textures/Gameplay/",
            ModalTex = Root + "Art/UI/Common/Textures/Modal/", Generated = Root + "Prefabs/UI/Gameplay/",
            FontRoot = Root + "Fonts/Notebook/";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color32(41, 38, 59, 255);

        [MenuItem("ANIMOL/New Notebook UI/Build Gameplay Overlay Prefab")]
        public static void BuildGameplayOverlayPrefab()
        {
            Prepare();
            GameObject canvas = null, worldStar = null;
            try
            {
                canvas = BuildOverlay();
                PrefabUtility.SaveAsPrefabAsset(canvas, Generated + "GameplayCanvas.prefab");
                worldStar = BuildWorldStar();
                PrefabUtility.SaveAsPrefabAsset(worldStar, Generated + "CollectibleStarVisual.prefab");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (canvas) UnityEngine.Object.DestroyImmediate(canvas);
                if (worldStar) UnityEngine.Object.DestroyImmediate(worldStar);
            }
            Debug.Log("GameplayCanvas.prefab and CollectibleStarVisual.prefab created. Bind NotebookGameplayGateway and the existing game's confirmed events; no scene or gameplay rule was changed.");
        }

        [MenuItem("ANIMOL/New Notebook UI/Create Gameplay Overlay In Current Scene")]
        public static void CreateGameplayOverlayInCurrentScene()
        {
            Prepare(); GameObject canvas = BuildOverlay();
            Undo.RegisterCreatedObjectUndo(canvas, "Create Gameplay Notebook Overlay"); Selection.activeGameObject = canvas;
            Debug.Log("Additive gameplay overlay created. Reuse the scene's EventSystem; bind the existing input/pause/loader gateway. Remove only the superseded old HUD after checking real references.");
        }

        private static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before generating gameplay UI assets.");
            Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GameplayTex, ModalTex }))
            {
                string asset = AssetDatabase.GUIDToAssetPath(guid); TextureImporter importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (!importer) continue;
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                TextureImporterSettings textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
                importer.filterMode = FilterMode.Bilinear; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 4096; importer.alphaIsTransparency = true;
                string name = Path.GetFileNameWithoutExtension(asset);
                bool panel = asset.Contains("/Modal/");
                bool controls = asset.Contains("/Controls/") && (name.StartsWith("Button_") || name.StartsWith("Primary_") || name.StartsWith("Utility_"));
                bool ringHud = name == "CostMedallion" || name == "PauseGlyph" || name.StartsWith("Digit_")
                    || name.StartsWith("StarDock_") || name.StartsWith("PauseOrnament_") || name.StartsWith("TelescopeRetry");
                if (ringHud) importer.spriteBorder = Vector4.zero;
                else if (panel || controls) importer.spriteBorder = new Vector4(36, 36, 36, 36);
                importer.SaveAndReimport();
            }
            if (!AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ANIMOL/Art/UI/Main/Pattern/Textures/CharacterFacesStars_All_2048.png") ||
                !AssetDatabase.LoadAssetAtPath<Shader>("Assets/ANIMOL/Art/UI/Main/Pattern/Shaders/UI_AnimolMonochromePattern.shader"))
                throw new InvalidOperationException("The unchanged original character-face scrolling resource is required.");
            font = EnsureFont();
        }

        private static GameObject BuildOverlay()
        {
            GameObject obj = new GameObject("William Gameplay Notebook Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(NotebookGameplayCanvasLayout));
            Canvas canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            CanvasScaler scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            RectTransform design = Rect(obj.transform, "Gameplay Safe Area 1080");
            design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f); design.sizeDelta = new Vector2(1080, 1920);
            NotebookGameplayCanvasLayout layout = obj.GetComponent<NotebookGameplayCanvasLayout>(); layout.scaler = scaler; layout.design = design;
            NotebookGameplayHud hud = design.gameObject.AddComponent<NotebookGameplayHud>(); hud.ownerCanvas = canvas;
            // Only local corner ornaments occupy the play view. There is no header image or full-width raycast surface.
            RectTransform corner = Rect(design, "Upper Right Star Slots And Pause"); TopRight(corner, 24, 24, 600, 144); hud.headerRect = corner;
            Image pause = Picture(corner, "Pause Mobius Ornament", "Gameplay/PauseOrnament_Normal"); At(pause.rectTransform, 456, 0, 144, 144);
            pause.preserveAspect = true; pause.raycastTarget = true;
            hud.pauseButton = pause.gameObject.AddComponent<Button>(); hud.pauseButton.targetGraphic = pause;
            hud.pauseButton.transition = Selectable.Transition.SpriteSwap;
            hud.pauseButton.spriteState = new SpriteState
            {
                highlightedSprite = Load("Gameplay/PauseOrnament_Highlighted"), pressedSprite = Load("Gameplay/PauseOrnament_Pressed"),
                selectedSprite = Load("Gameplay/PauseOrnament_Highlighted"), disabledSprite = Load("Gameplay/PauseOrnament_Disabled")
            };
            Icon(hud.pauseButton.transform, "PauseGlyph", 28, 28, 88, 88);
            Image restart = Picture(design, "Lower Right Telescope Restart Five Part Gauge", "Gameplay/TelescopeRetry_0"); BottomRight(restart.rectTransform, 24, 24, 264, 264);
            hud.restartRect = restart.rectTransform;
            restart.preserveAspect = true; restart.raycastTarget = true;
            hud.restartImage = restart; hud.restartButton = restart.gameObject.AddComponent<Button>();
            hud.restartButton.targetGraphic = restart; hud.restartButton.transition = Selectable.Transition.SpriteSwap;
            hud.retryGaugeSprites = new Sprite[6]; hud.retryGaugePressedSprites = new Sprite[6];
            for (int uses = 0; uses <= 5; uses++)
            { hud.retryGaugeSprites[uses] = Load("Gameplay/TelescopeRetry_" + uses); hud.retryGaugePressedSprites[uses] = Load("Gameplay/TelescopeRetryPressed_" + uses); }
            Image cost = Picture(design, "Lower Left Original Loop Cost Medallion", "Gameplay/CostMedallion"); Bottom(cost.rectTransform, 24, 24, 300, 300);
            cost.preserveAspect = true; hud.costRect = cost.rectTransform;
            RectTransform digitArea = Rect(design, "Individual Supplied Number Sprites"); Bottom(digitArea, 84, 126, 180, 96);
            NotebookSpriteNumber number = digitArea.gameObject.AddComponent<NotebookSpriteNumber>(); number.area = digitArea;
            number.digits = new Sprite[10]; number.places = new Image[10]; number.maximumDigitHeight = 96; number.gap = 3;
            for (int digit = 0; digit <= 9; digit++)
            {
                number.digits[digit] = Load("Gameplay/Digits/Digit_" + digit);
                Image place = Picture(digitArea, "Digit Position " + digit, "Gameplay/Digits/Digit_0"); place.preserveAspect = true;
                number.places[digit] = place;
            }
            number.SetNumber(0); hud.costNumber = number;
            hud.jarImage = Picture(corner, "Objective Stars Mobius Slots", "Gameplay/StarDock_3_0"); At(hud.jarImage.rectTransform, 0, 0, 432, 144); hud.jarImage.preserveAspect = true;
            // Filled and empty star-shaped slots show progress without a second numeric counter.
            hud.collectedText = null;
            hud.hudStatus = Text(design, "HUD Connection Status", "", 348, 0, 384, 112, 30, TextAlignmentOptions.Center);
            Bottom(hud.hudStatus.rectTransform, 348, 48, 384, 112);
            hud.hudStatus.color = new Color32(255, 240, 216, 255);
            var jarStates = new List<NotebookGameplayJarState>();
            for (int required = 1; required <= 4; required++)
                for (int collected = 0; collected <= required; collected++)
                    jarStates.Add(new NotebookGameplayJarState { required = required, collected = collected, sprite = Load("Gameplay/StarDock_" + required + "_" + collected) });
            hud.jarStates = jarStates.ToArray();
            hud.starFrames = StarFrames(); hud.trailFrames = new[] { Load("Gameplay/Trail_0"), Load("Gameplay/Trail_1"), Load("Gameplay/Trail_2") };

            hud.flightLayer = Rect(design, "Pickup Presentation Only"); Stretch(hud.flightLayer);
            hud.flightTrail = Picture(hud.flightLayer, "Static PNG Comet Trail", "Gameplay/Trail_0"); Center(hud.flightTrail.rectTransform, 180, 84); hud.flightTrail.preserveAspect = true;
            hud.flightStar = Picture(hud.flightLayer, "Flying Confirmed Star", "Gameplay/CollectibleStar_0"); Center(hud.flightStar.rectTransform, 112, 112); hud.flightStar.preserveAspect = true;
            hud.flightStar.gameObject.SetActive(false); hud.flightTrail.gameObject.SetActive(false);

            // Only a neutral translucent scrim covers the game. Faces live inside each popup.
            RectTransform backdrop = Rect(design, "Plain Gray Translucent Modal Backdrop"); Stretch(backdrop);
            layout.fullScreenBackdrop = backdrop; hud.modalBackdrop = backdrop.gameObject;
            Image scrim = backdrop.gameObject.AddComponent<Image>();
            scrim.color = new Color(40f / 255, 40f / 255, 44f / 255, .58f); scrim.raycastTarget = true;
            backdrop.gameObject.SetActive(false);

            RectTransform pauseOverlay = Overlay(design, "Pause Settings Overlay"); hud.pauseOverlay = pauseOverlay.gameObject;
            Image pp = Picture(pauseOverlay, "Pause Face Pattern Popup", "Modal/PanelBase", true); Center(pp.rectTransform, 984, 760); hud.pausePanel = pp.rectTransform;
            pp.raycastTarget = true; hud.pausePatternMotion = AddPanelFaces(pp.transform);
            Text(pp.transform, "Pause Title", "잠시 쉬어가기", 60, 54, 864, 90, 64, TextAlignmentOptions.Center);
            hud.pauseStageText = Text(pp.transform, "Pause Stage", "", 60, 170, 864, 60, 40, TextAlignmentOptions.Center);
            Text(pp.transform, "Pause Copy", "준비되면 여정을 이어가세요", 60, 270, 864, 60, 38, TextAlignmentOptions.Center);
            hud.resumeButton = Button(pp.transform, "Resume", "Primary", 60, 390, 864, 144, "계속하기", 52); Icon(hud.resumeButton.transform, "ResumeIcon", 48, 32, 80, 80);
            hud.pauseMapButton = Button(pp.transform, "Pause Map", "Button", 60, 560, 864, 132, "별자리 지도", 42);
            hud.pauseStatus = Text(pp.transform, "Pause Status", "", 72, 704, 840, 44, 28, TextAlignmentOptions.Center);
            pauseOverlay.gameObject.SetActive(false);

            RectTransform clear = Overlay(design, "Verified Clear Overlay"); hud.clearOverlay = clear.gameObject;
            Image cp = Picture(clear, "Clear Face Pattern Popup", "Modal/PanelBase", true); Center(cp.rectTransform, 984, 780); hud.clearPanel = cp.rectTransform;
            cp.raycastTarget = true; hud.clearPatternMotion = AddPanelFaces(cp.transform);
            Text(cp.transform, "Clear Title", "스테이지 클리어", 60, 60, 864, 90, 62, TextAlignmentOptions.Center);
            hud.clearStageText = Text(cp.transform, "Clear Stage", "", 60, 180, 864, 60, 40, TextAlignmentOptions.Center);
            hud.clearStatus = Text(cp.transform, "Clear Outcome Or Entry Reason", "클리어 완료", 72, 280, 840, 100, 36, TextAlignmentOptions.Center);
            hud.nextButton = Button(cp.transform, "Next Stage", "Primary", 48, 430, 888, 144, "다음 스테이지로", 52); hud.nextLabel = hud.nextButton.GetComponentInChildren<TMP_Text>();
            Icon(hud.nextButton.transform, "NextIcon", 772, 34, 76, 76);
            hud.clearMapButton = Button(cp.transform, "Clear Map", "Button", 48, 600, 888, 132, "별자리 지도", 42);
            clear.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);
            return obj;
        }

        private static GameObject BuildWorldStar()
        {
            GameObject obj = new GameObject("Collectible Star Visual Only", typeof(SpriteRenderer), typeof(NotebookGameplayWorldStar));
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = Load("Gameplay/CollectibleStar_0"); renderer.color = Color.white;
            NotebookGameplayWorldStar visual = obj.GetComponent<NotebookGameplayWorldStar>(); visual.Configure(renderer, StarFrames()); visual.framesPerSecond = 4;
            // No collider, trigger, objective count, tile move, or reward logic is added.
            return obj;
        }
        private static Sprite[] StarFrames() => new[] { Load("Gameplay/CollectibleStar_0"), Load("Gameplay/CollectibleStar_1"), Load("Gameplay/CollectibleStar_2"), Load("Gameplay/CollectibleStar_3") };
        private static RectTransform Overlay(Transform parent, string name)
        {
            RectTransform r = Rect(parent, name); Stretch(r); return r;
        }
        private static AnimolPatternMotion AddPanelFaces(Transform panel)
        {
            RectTransform clip = Rect(panel, "Original Face Pattern Clip 24px"); Stretch(clip);
            clip.offsetMin = new Vector2(24, 24); clip.offsetMax = new Vector2(-24, -24);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform faces = Rect(clip, "Unchanged Faces Inside Popup Only"); Stretch(faces);
            RawImage raw = faces.gameObject.AddComponent<RawImage>(); raw.color = Color.white; raw.raycastTarget = false;
            Texture2D pattern = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ANIMOL/Art/UI/Main/Pattern/Textures/CharacterFacesStars_All_2048.png");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/ANIMOL/Art/UI/Main/Pattern/Shaders/UI_AnimolMonochromePattern.shader");
            if (!pattern || !shader) throw new InvalidOperationException("The unchanged original face texture and shader are required.");
            DiagonalPatternBackground effect = faces.gameObject.AddComponent<DiagonalPatternBackground>(); effect.Configure(shader, pattern);
            effect.SetAppearance(pattern, Color.clear, new Color32(166, 110, 72, 255), .16f); effect.VisualVelocity = new Vector2(8, 8);
            SerializedObject appearance = new SerializedObject(effect); appearance.FindProperty("tileSize").vector2Value = new Vector2(1080, 1080);
            appearance.FindProperty("useUnscaledTime").boolValue = true; appearance.ApplyModifiedPropertiesWithoutUndo();
            AnimolPatternMotion motion = faces.gameObject.AddComponent<AnimolPatternMotion>();
            Image frame = Picture(panel, "Static Clean Popup Edge", "Modal/PanelFrame", true); Stretch(frame.rectTransform);
            return motion;
        }
        private static Sprite Load(string relative)
        {
            string folder = relative.StartsWith("Gameplay/", StringComparison.Ordinal) ? GameplayTex : ModalTex;
            string name = relative.StartsWith("Gameplay/", StringComparison.Ordinal) ? relative.Substring(9) : relative.Substring(6);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + name + ".png");
            if (!sprite) throw new InvalidOperationException("Missing supplied sprite: " + relative); return sprite;
        }
        private static TMP_FontAsset EnsureFont()
        {
            string destination = FontRoot + "Galmuri14.asset"; TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destination); if (existing) return existing;
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field"); if (!shader) throw new InvalidOperationException("Import TMP Essential Resources first.");
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "Galmuri14.ttf"); if (!source) throw new InvalidOperationException("Galmuri14 source font is missing.");
            TMP_FontAsset result = TMP_FontAsset.CreateFontAsset(source, 42, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (!result) throw new InvalidOperationException("Could not create Galmuri14 TMP font.");
            result.name = "Galmuri14"; result.material.shader = shader; AssetDatabase.CreateAsset(result, destination); AssetDatabase.AddObjectToAsset(result.material, result);
            foreach (Texture2D atlas in result.atlasTextures) if (atlas) AssetDatabase.AddObjectToAsset(atlas, result); EditorUtility.SetDirty(result); return result;
        }
        private static RectTransform Rect(Transform parent, string name) { GameObject o = new GameObject(name, typeof(RectTransform)); o.transform.SetParent(parent, false); return o.GetComponent<RectTransform>(); }
        private static void At(RectTransform r, float x, float y, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
        private static void TopRight(RectTransform r, float right, float top, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = Vector2.one; r.anchoredPosition = new Vector2(-right, -top); r.sizeDelta = new Vector2(w, h); }
        private static void Bottom(RectTransform r, float x, float y, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); }
        private static void BottomRight(RectTransform r, float right, float bottom, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 0); r.anchoredPosition = new Vector2(-right, bottom); r.sizeDelta = new Vector2(w, h); }
        private static void Center(RectTransform r, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(w, h); }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f); r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Image Picture(Transform parent, string name, string sprite, bool sliced = false)
        { RectTransform r = Rect(parent, name); Image image = r.gameObject.AddComponent<Image>(); image.sprite = Load(sprite); image.color = Color.white; image.raycastTarget = false; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; return image; }
        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, TextAlignmentOptions align)
        { RectTransform r = Rect(parent, name); At(r, x, y, w, h); TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.fontSize = size; t.text = value; t.color = Ink; t.alignment = align; t.raycastTarget = false; t.enableAutoSizing = false; t.enableWordWrapping = true; return t; }
        private static Button Button(Transform parent, string name, string role, float x, float y, float w, float h, string label, float size)
        { Image image = Picture(parent, name, "Modal/" + role + "_Normal", true); At(image.rectTransform, x, y, w, h); image.raycastTarget = true; Button b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image; b.transition = Selectable.Transition.SpriteSwap; b.spriteState = new SpriteState { highlightedSprite = Load("Modal/" + role + "_Highlighted"), pressedSprite = Load("Modal/" + role + "_Pressed"), selectedSprite = Load("Modal/" + role + "_Highlighted"), disabledSprite = Load("Modal/" + role + "_Disabled") }; Text(image.transform, "Label", label, 20, 0, w - 40, h, size, TextAlignmentOptions.Center); return b; }
        private static void Icon(Transform parent, string sprite, float x, float y, float w, float h) { Image image = Picture(parent, sprite, "Gameplay/" + sprite); At(image.rectTransform, x, y, w, h); image.preserveAspect = true; }
    }
}
#endif
