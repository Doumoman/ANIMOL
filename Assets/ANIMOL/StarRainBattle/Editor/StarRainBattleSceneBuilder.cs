using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ANIMOL.StarRainBattle.Editor
{
    /// <summary>
    /// Assembles existing PNGs, materials and gameplay components into a new test scene.
    /// It does not draw textures, create a shader, or modify ANIMOL's PvP gateway.
    /// </summary>
    public static class StarRainBattleSceneBuilder
    {
        private const string Root = "Assets/ANIMOL/StarRainBattle";
        private const string Ui = "Assets/ANIMOL/BattleCrayonUI";
        private const string KoreanFontPath = "Assets/WilliamNotebookUI/Generated/Galmuri14.asset";
        private const string PackageFontPath = Root + "/Fonts/Galmuri14.ttf";
        private const string BattleFontPath = Root + "/Fonts/Galmuri14_Battle.asset";
        private const string ScenePath = Root + "/Scenes/StarRainBossBattle.unity";
        private static TMP_FontAsset battleFont;
        private static readonly List<Image> motionImages = new List<Image>();
        private static readonly List<Material> animatedMaterials = new List<Material>();
        private static readonly List<Material> stillMaterials = new List<Material>();

        [MenuItem("ANIMOL/Star Rain Battle/Create Standalone Preview Scene")]
        public static void CreateStandalonePreviewScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null || File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Star Rain Battle", "StarRainBossBattle.unity already exists. The builder will not overwrite it.", "OK");
                return;
            }
            try { ValidateAssets(); }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Star Rain Battle: missing art", ex.Message, "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try { battleFont = EnsureKoreanFont(); }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Star Rain Battle: Korean font", ex.Message, "OK");
                return;
            }

            motionImages.Clear();
            animatedMaterials.Clear();
            stillMaterials.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEventSystem();
            var canvasObject = new GameObject("StarRainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            var background = CreateImage("NightSky", canvasRect, SceneSprite("ConstellationSky_Dark"), null,
                Vector2.zero, Vector2.zero, false);
            Stretch(background.rectTransform);
            background.color = new Color(0.55f, 0.57f, 0.65f, 1f);

            var safeObject = new GameObject("Battle Safe Area", typeof(RectTransform), typeof(StarRainBattleSafeArea));
            safeObject.transform.SetParent(canvasRect, false);
            Stretch(safeObject.GetComponent<RectTransform>());
            var designObject = new GameObject("Battle Design 1080x1920", typeof(RectTransform));
            designObject.transform.SetParent(safeObject.transform, false);
            canvasRect = designObject.GetComponent<RectTransform>();
            Place(canvasRect, new Vector2(1080, 1920), Vector2.zero);
            safeObject.GetComponent<StarRainBattleSafeArea>().Design = canvasRect;
            Canvas.ForceUpdateCanvases();
            safeObject.GetComponent<StarRainBattleSafeArea>().Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));

            var boardObject = new GameObject("BattleBoard", typeof(RectTransform), typeof(RectMask2D));
            boardObject.transform.SetParent(canvasRect, false);
            var board = boardObject.GetComponent<RectTransform>();
            Place(board, new Vector2(1000, 1550), Vector2.zero);
            var controller = boardObject.AddComponent<StarRainBattleController>();

            var upperCore = ArtImage("CurseCore", board, "Core_Curse", new Vector2(256, 256), BoardPoint(200, 76), false);
            var lowerCore = ArtImage("IsolCore", board, "Core_Isol", new Vector2(256, 256), BoardPoint(200, 544), false);
            var link = ArtImage("Connection", board, "Link", new Vector2(260, 48), Vector2.zero, false);
            link.type = Image.Type.Sliced;
            link.enabled = false;
            var dragLine = ArtImage("DragPreview", board, "Link", new Vector2(260, 48), Vector2.zero, false);
            dragLine.type = Image.Type.Sliced;
            dragLine.color = new Color(1, 1, 1, 0.45f);
            dragLine.enabled = false;

            var shotViews = new List<(Image head, Image trail)>();
            for (int i = 0; i < 24; i++)
            {
                var trail = ArtImage("MeteorTrail_" + i, board, "Meteor_Trail",
                    new Vector2(170, 45), Vector2.zero, false, false);
                var head = ArtImage("MeteorHead_" + i, board, "Meteor_IsolHead",
                    new Vector2(64, 64), Vector2.zero, false, false);
                trail.enabled = false;
                head.enabled = false;
                shotViews.Add((head, trail));
            }
            var nodes = new Image[5];
            var selected = new Image[5];
            var dimmed = new Image[5];
            var initialX = new[] {84f, 198f, 313f, 124f, 293f};
            var initialY = new[] {213f, 193f, 236f, 381f, 395f};
            for (int i = 0; i < 5; i++)
            {
                var p = BoardPoint(initialX[i], initialY[i]);
                nodes[i] = ArtImage("LinkStar_" + i, board, "Node_" + i, new Vector2(128, 128), p, false);
                selected[i] = ArtImage("Selection_" + i, board, "SelectedRing", new Vector2(148, 148), p, false);
                dimmed[i] = ArtImage("Dim_" + i, board, "DimHatch", new Vector2(120, 120), p, false);
                selected[i].enabled = false;
                dimmed[i].enabled = false;
            }
            var fxViews = new Image[24];
            for (int i = 0; i < fxViews.Length; i++)
            {
                fxViews[i] = ArtImage("Impact_" + i, board, "FX_Block", new Vector2(128, 128), Vector2.zero, false, false);
                fxViews[i].enabled = false;
            }
            // The transparent graphic receives touch/drag events; art never receives a raycast.
            var input = CreateImage("InputSurface", board, null, null, new Vector2(1000, 1550), Vector2.zero, true);
            input.color = new Color(0, 0, 0, 0);

            var isolHud = UiImage("IsolHUD", canvasRect, "Frame_HUD", new Vector2(510, 132), new Vector2(-270, 810));
            var curseHud = UiImage("CurseHUD", canvasRect, "Frame_HUD", new Vector2(510, 132), new Vector2(270, 810));
            UiImage("IsolEmblem", isolHud.rectTransform, "Icon_Isol", new Vector2(85, 85), new Vector2(-204, 0)).color =
                new Color(0.965f, 0.804f, 0.482f);
            UiImage("CurseEmblem", curseHud.rectTransform, "Icon_Curse", new Vector2(85, 85), new Vector2(-204, 0)).color =
                new Color(0.91f, 0.545f, 0.776f);
            var isolFill = CreateImage("IsolFill", isolHud.rectTransform, SceneSprite("Fill_Isol"), null,
                new Vector2(320, 42), new Vector2(65, 0), false);
            var curseFill = CreateImage("CurseFill", curseHud.rectTransform, SceneSprite("Fill_Curse"), null,
                new Vector2(320, 42), new Vector2(65, 0), false);
            isolFill.type = Image.Type.Filled; isolFill.fillMethod = Image.FillMethod.Horizontal; isolFill.fillOrigin = 0;
            curseFill.type = Image.Type.Filled; curseFill.fillMethod = Image.FillMethod.Horizontal; curseFill.fillOrigin = 0;
            UiImage("IsolHealthFrame", isolHud.rectTransform, "Frame_Health", new Vector2(345, 58), new Vector2(65, 0));
            UiImage("CurseHealthFrame", curseHud.rectTransform, "Frame_Health", new Vector2(345, 58), new Vector2(65, 0));
            // Keep white HP digits on the dark gap beside the bright health fill.
            var isolText = Text("IsolHP", isolHud.rectTransform, "7", new Vector2(64, 44), new Vector2(-129, 0), 30);
            var curseText = Text("CurseHP", curseHud.rectTransform, "11", new Vector2(64, 44), new Vector2(-129, 0), 30);

            // Keep the three footer controls clear of the top HUD and phone cutouts.
            var pause = Button("Pause", canvasRect, "Frame_ButtonSquare", "Icon_Pause",
                new Vector2(110, 110), new Vector2(-465, -855), out var pauseIcon);
            var score = UiImage("BlockRecord", canvasRect, "Frame_Score",
                new Vector2(310, 112), new Vector2(0, -855));
            UiImage("BlockIcon", score.rectTransform, "Icon_Shield", new Vector2(70, 70), new Vector2(-105, 0));
            var blocked = Text("BlockedCount", score.rectTransform, "0", new Vector2(45, 54), new Vector2(-52, 0), 32);
            UiImage("MistakeIcon", score.rectTransform, "Icon_Misblock", new Vector2(66, 66), new Vector2(20, 0));
            var misblocked = Text("MisblockedCount", score.rectTransform, "0", new Vector2(45, 54), new Vector2(91, 0), 32);
            var augment = Button("Augment", canvasRect, "Frame_ButtonSquare", "Icon_Augment",
                new Vector2(110, 110), new Vector2(465, -855), out var augmentIcon);

            var overlay = CreateImage("BattleOverlay", canvasRect, null, null, Vector2.zero, Vector2.zero, true);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0.01f, 0.015f, 0.03f, 0.68f);
            var cardFill = CreateImage("CardInk", overlay.rectTransform, null, null,
                new Vector2(900, 1060), Vector2.zero, false);
            cardFill.color = new Color(0.015f, 0.018f, 0.03f, 0.95f);
            var card = UiImage("CrayonCard", overlay.rectTransform, "Frame_Card",
                new Vector2(900, 1060), Vector2.zero);
            var portrait = CreateImage("IsolPortrait", card.rectTransform, SceneSprite("Isol_CrayonPortrait"), null,
                new Vector2(470, 565), new Vector2(0, 170), false);
            portrait.preserveAspect = true;
            ArtImage("DiagramLink", card.rectTransform, "Link", new Vector2(210, 35), new Vector2(0, -128), false).type = Image.Type.Sliced;
            ArtImage("DiagramLeftStar", card.rectTransform, "Node_0", new Vector2(105, 105), new Vector2(-145, -128), false);
            ArtImage("DiagramRightStar", card.rectTransform, "Node_2", new Vector2(105, 105), new Vector2(145, -128), false);
            var title = Text("BattleState", card.rectTransform, "별을 이어 막아",
                new Vector2(650, 70), new Vector2(0, -220), 46);
            var start = Button("Start", card.rectTransform, "Frame_ButtonWide", "Icon_Play",
                new Vector2(760, 120), new Vector2(0, -330), out _);
            var replay = Button("ReplaySameSeed", card.rectTransform, "Frame_ButtonWide", "Icon_Retry",
                new Vector2(760, 120), new Vector2(0, -330), out _);
            var fresh = Button("NewSeed", card.rectTransform, "Frame_ButtonWide", "Icon_Play",
                new Vector2(760, 82), new Vector2(0, -440), out _);
            replay.gameObject.SetActive(false);
            fresh.gameObject.SetActive(false);

            WireController(controller, board, input, nodes, selected, dimmed, lowerCore.rectTransform,
                upperCore.rectTransform, link, dragLine, shotViews, fxViews, isolFill, curseFill,
                isolText, curseText, blocked, misblocked, title, start, replay, fresh, pause, augment,
                overlay.gameObject, pauseIcon, augmentIcon);
            if (!AssetDatabase.IsValidFolder(Root + "/Scenes")) AssetDatabase.CreateFolder(Root, "Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                EditorUtility.DisplayDialog("Star Rain Battle", "Could not save scene at " + ScenePath, "OK");
                return;
            }
            Selection.activeGameObject = boardObject;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("[ANIMOL] Standalone Star Rain battle preview saved: " + ScenePath +
                ". It does not replace NotebookRoot.OpenBattle() or add itself to Build Settings.");
        }

        private static void ValidateAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath) == null &&
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BattleFontPath) == null)
            {
                Require<Font>(PackageFontPath);
                if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                    throw new InvalidOperationException("TMP Essential Resources are missing. " +
                        "Import them before creating the battle scene.");
            }
            var shader = Require<Shader>(Ui + "/Shaders/UI_ANIMOL_CrayonFrameBoil.shader");
            var art = new[] {"Node_0", "Node_1", "Node_2", "Node_3", "Node_4", "SelectedRing",
                "DimHatch", "Core_Isol", "Core_Curse", "Meteor_IsolHead", "Meteor_CurseHead",
                "Meteor_Trail", "Link", "FX_Block", "FX_Reflect", "FX_CoreHit"};
            var ui = new[] {"Frame_HUD", "Frame_Health", "Frame_ButtonSquare", "Frame_ButtonWide",
                "Frame_Card", "Frame_Score", "Icon_Isol", "Icon_Curse", "Icon_Pause",
                "Icon_Resume", "Icon_Augment", "Icon_Shield", "Icon_Misblock", "Icon_Play", "Icon_Retry"};
            foreach (string name in art) ValidatePaintedSet(Root, name, shader);
            foreach (string name in ui) ValidatePaintedSet(Ui, name, shader);
            foreach (string name in new[] {"ConstellationSky_Dark", "Isol_CrayonPortrait", "Fill_Isol", "Fill_Curse"})
                Require<Sprite>(Ui + "/Sprites/Scene/" + name + ".png");
        }

        private static TMP_FontAsset EnsureKoreanFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (existing != null) return existing;
            existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BattleFontPath);
            if (existing != null)
            {
                PrepareBattleFont(existing);
                return existing;
            }
            if (File.Exists(BattleFontPath))
                throw new InvalidOperationException("A non-font asset occupies " + BattleFontPath +
                    ". Resolve it before creating the battle scene.");

            var source = Require<Font>(PackageFontPath);
            var shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null)
                throw new InvalidOperationException("TMP Essential Resources are missing: " +
                    "TextMeshPro/Mobile/Distance Field shader could not be found.");
            var result = TMP_FontAsset.CreateFontAsset(source, 42, 5, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (result == null || result.material == null)
                throw new InvalidOperationException("Could not prepare the bundled Galmuri14 TMP font.");
            result.name = "Galmuri14_Battle";
            result.material.shader = shader;
            AssetDatabase.CreateAsset(result, BattleFontPath);
            AssetDatabase.AddObjectToAsset(result.material, result);
            foreach (Texture2D atlas in result.atlasTextures)
                if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, result);
            PrepareBattleFont(result);
            return result;
        }

        private static void PrepareBattleFont(TMP_FontAsset font)
        {
            // Populate this module's font in the Editor, including every HP/count digit.
            // Never modify the existing Notebook font through this helper.
            const string characters = "0123456789 별을 이어 막아잠시 멈춤별빛이 닿았다별이 어두워졌다";
            if (!font.HasCharacters(characters) && !font.TryAddCharacters(characters, out var missing))
                throw new InvalidOperationException("Bundled battle font is missing characters: " + missing);
            EditorUtility.SetDirty(font);
            foreach (var atlas in font.atlasTextures)
                if (atlas != null) EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(font.material);
            AssetDatabase.SaveAssetIfDirty(font);
        }

        private static void ValidatePaintedSet(string folder, string name, Shader shader)
        {
            var prefix = folder + "/Sprites/" + name;
            var a = Require<Sprite>(prefix + "_A.png");
            var b = Require<Texture2D>(prefix + "_B.png");
            var c = Require<Texture2D>(prefix + "_C.png");
            if (a.texture.width != b.width || a.texture.height != b.height ||
                a.texture.width != c.width || a.texture.height != c.height)
                throw new InvalidOperationException("Prepainted A/B/C dimensions differ: " + prefix);
            foreach (string motion in new[] {"Animated", "Still"})
            {
                var path = folder + "/Materials/M_" + name + "_" + motion + ".mat";
                var mat = Require<Material>(path);
                if (mat.shader != shader || !mat.HasProperty("_FrameB") || !mat.HasProperty("_FrameC") ||
                    mat.GetTexture("_FrameB") != b || mat.GetTexture("_FrameC") != c)
                    throw new InvalidOperationException("Invalid shader or B/C texture bindings: " + path +
                        "\nReimport both complete packages before creating the scene.");
                if (!mat.HasProperty("_Motion") ||
                    (motion == "Animated" && mat.GetFloat("_Motion") < 0.5f) ||
                    (motion == "Still" && mat.GetFloat("_Motion") > 0.5f))
                    throw new InvalidOperationException("Invalid motion setting: " + path);
            }
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException("Missing or not imported: " + path +
                    (path == PackageFontPath
                        ? "\nImport the complete battle patch including Fonts/Galmuri14.ttf."
                        : "\nImport ANIMOL_Crayon_UI_UnityPatch_v1 and ANIMOL_StarRain_Battle_v1 first."));
            return asset;
        }

        private static Sprite ArtSprite(string name) => Require<Sprite>(Root + "/Sprites/" + name + "_A.png");
        private static Material ArtMaterial(string name, bool motion) =>
            Require<Material>(Root + "/Materials/M_" + name + (motion ? "_Animated.mat" : "_Still.mat"));
        private static Sprite UiSprite(string name) => Require<Sprite>(Ui + "/Sprites/" + name + "_A.png");
        private static Material UiMaterial(string name, bool motion) =>
            Require<Material>(Ui + "/Materials/M_" + name + (motion ? "_Animated.mat" : "_Still.mat"));
        private static Sprite SceneSprite(string name) => Require<Sprite>(Ui + "/Sprites/Scene/" + name + ".png");

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type == null)
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule", false);
                    if (type != null) break;
                }
            if (type != null && typeof(BaseInputModule).IsAssignableFrom(type))
            {
                var module = go.AddComponent(type);
                var assign = type.GetMethod("AssignDefaultActions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (assign != null) assign.Invoke(module, null);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
                Debug.LogWarning("[ANIMOL] Input System UI module was not found. Using StandaloneInputModule; " +
                    "projects configured for New Input System only need the com.unity.inputsystem package.");
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        private static Vector2 BoardPoint(float x, float y) => new Vector2((x - 200f) * 2.5f, (310f - y) * 2.5f);
        private static Image CreateImage(string name, Transform parent, Sprite sprite, Material material,
            Vector2 size, Vector2 position, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite; image.material = material;
            image.raycastTarget = raycast;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            Place(image.rectTransform, size, position);
            return image;
        }
        private static void Register(Image image, Material animated, Material still)
        {
            image.material = animated;
            motionImages.Add(image); animatedMaterials.Add(animated); stillMaterials.Add(still);
        }
        private static void RegisterDynamicMaterial(string set)
        {
            motionImages.Add(null);
            animatedMaterials.Add(ArtMaterial(set, true));
            stillMaterials.Add(ArtMaterial(set, false));
        }
        private static Image ArtImage(string name, Transform parent, string set, Vector2 size, Vector2 position,
            bool raycast, bool registerMotion = true)
        {
            var image = CreateImage(name, parent, ArtSprite(set), ArtMaterial(set, true), size, position, raycast);
            if (registerMotion) Register(image, ArtMaterial(set, true), ArtMaterial(set, false));
            return image;
        }
        private static Image UiImage(string name, Transform parent, string set, Vector2 size, Vector2 position)
        {
            var image = CreateImage(name, parent, UiSprite(set), UiMaterial(set, true), size, position, false);
            image.type = set.StartsWith("Frame_", StringComparison.Ordinal) ? Image.Type.Sliced : Image.Type.Simple;
            Register(image, UiMaterial(set, true), UiMaterial(set, false));
            return image;
        }
        private static TMP_Text Text(string name, Transform parent, string text, Vector2 size, Vector2 position, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = battleFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.95f, 0.92f, 1f);
            label.raycastTarget = false;
            Place(label.rectTransform, size, position);
            return label;
        }
        private static Button Button(string name, Transform parent, string frame, string icon,
            Vector2 size, Vector2 position, out Image iconImage)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Place(rect, size, position);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.025f, 0.029f, 0.04f, 0.88f);
            image.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var frameImage = UiImage("CrayonFrame", rect, frame, size, Vector2.zero);
            Stretch(frameImage.rectTransform);
            var iconSize = frame == "Frame_ButtonSquare" ? new Vector2(74, 74) : new Vector2(82, 82);
            iconImage = UiImage("Symbol", rect, icon, iconSize, Vector2.zero);
            if (icon == "Icon_Pause")
            {
                // The icon swaps to Resume at runtime. Keep its sprite/material in sync.
                var last = motionImages.Count - 1;
                motionImages.RemoveAt(last);
                animatedMaterials.RemoveAt(last);
                stillMaterials.RemoveAt(last);
                iconImage.material = UiMaterial(icon, false);
            }
            return button;
        }
        private static void Set(SerializedObject objectData, string name, UnityEngine.Object value)
        {
            var property = objectData.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Missing controller field: " + name);
            property.objectReferenceValue = value;
        }
        private static void SetArray(SerializedObject objectData, string name, IList<UnityEngine.Object> values)
        {
            var property = objectData.FindProperty(name);
            if (property == null || !property.isArray) throw new InvalidOperationException("Missing controller array: " + name);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        private static List<UnityEngine.Object> Objects<T>(IEnumerable<T> values) where T : UnityEngine.Object
        {
            var result = new List<UnityEngine.Object>();
            foreach (T value in values) result.Add(value);
            return result;
        }
        private static void WireController(StarRainBattleController controller, RectTransform board, Image input,
            Image[] nodes, Image[] selected, Image[] dimmed, RectTransform isolCore, RectTransform curseCore,
            Image link, Image dragLine, IList<(Image head, Image trail)> shots, Image[] fx,
            Image isolFill, Image curseFill, TMP_Text isolText, TMP_Text curseText,
            TMP_Text blocked, TMP_Text misblocked, TMP_Text title, Button start, Button replay, Button fresh,
            Button pause, Button augment, GameObject overlay, Image pauseIcon, Image augmentIcon)
        {
            var data = new SerializedObject(controller);
            Set(data, "Board", board); Set(data, "InputSurface", input);
            SetArray(data, "Nodes", Objects(nodes));
            SetArray(data, "SelectedRings", Objects(selected));
            SetArray(data, "DimHatches", Objects(dimmed));
            Set(data, "IsolCore", isolCore); Set(data, "CurseCore", curseCore);
            Set(data, "Link", link); Set(data, "DragLine", dragLine);
            var shotArray = data.FindProperty("ShotViews");
            if (shotArray == null || !shotArray.isArray) throw new InvalidOperationException("Missing controller ShotViews");
            shotArray.arraySize = shots.Count;
            for (int i = 0; i < shots.Count; i++)
            {
                var item = shotArray.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Head").objectReferenceValue = shots[i].head;
                item.FindPropertyRelative("Trail").objectReferenceValue = shots[i].trail;
            }
            var fxArray = data.FindProperty("FxViews");
            if (fxArray == null || !fxArray.isArray) throw new InvalidOperationException("Missing controller FxViews");
            fxArray.arraySize = fx.Length;
            for (int i = 0; i < fx.Length; i++)
                fxArray.GetArrayElementAtIndex(i).FindPropertyRelative("Image").objectReferenceValue = fx[i];
            Set(data, "IsolFill", isolFill); Set(data, "CurseFill", curseFill);
            Set(data, "IsolHpText", isolText); Set(data, "CurseHpText", curseText);
            Set(data, "BlockedText", blocked); Set(data, "MisblockedText", misblocked);
            Set(data, "OverlayTitle", title);
            Set(data, "StartButton", start); Set(data, "ReplayButton", replay); Set(data, "FreshButton", fresh);
            Set(data, "PauseButton", pause); Set(data, "AugmentButton", augment);
            Set(data, "Overlay", overlay); Set(data, "PauseIcon", pauseIcon); Set(data, "AugmentIcon", augmentIcon);
            Set(data, "PauseSprite", UiSprite("Icon_Pause")); Set(data, "ResumeSprite", UiSprite("Icon_Resume"));
            Set(data, "IsolHeadSprite", ArtSprite("Meteor_IsolHead"));
            Set(data, "CurseHeadSprite", ArtSprite("Meteor_CurseHead"));
            Set(data, "TrailSprite", ArtSprite("Meteor_Trail"));
            Set(data, "IsolHeadMaterial", ArtMaterial("Meteor_IsolHead", true));
            Set(data, "CurseHeadMaterial", ArtMaterial("Meteor_CurseHead", true));
            Set(data, "TrailMaterial", ArtMaterial("Meteor_Trail", true));
            Set(data, "BlockFxSprite", ArtSprite("FX_Block"));
            Set(data, "ReflectFxSprite", ArtSprite("FX_Reflect"));
            Set(data, "CoreHitFxSprite", ArtSprite("FX_CoreHit"));
            Set(data, "BlockFxMaterial", ArtMaterial("FX_Block", true));
            Set(data, "ReflectFxMaterial", ArtMaterial("FX_Reflect", true));
            Set(data, "CoreHitFxMaterial", ArtMaterial("FX_CoreHit", true));
            foreach (string set in new[] {"Meteor_IsolHead", "Meteor_CurseHead", "Meteor_Trail",
                "FX_Block", "FX_Reflect", "FX_CoreHit"})
                RegisterDynamicMaterial(set);
            SetArray(data, "MotionImages", Objects(motionImages));
            SetArray(data, "AnimatedMaterials", Objects(animatedMaterials));
            SetArray(data, "StillMaterials", Objects(stillMaterials));
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }
}
