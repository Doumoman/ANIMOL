using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ANIMOL.MissingUiV1.Editor
{
    public sealed class MissingUiV1BuilderWindow : EditorWindow
    {
        private Object _font;
        [MenuItem("ANIMOL/Missing UI V1/Build Missing UI V1")]
        public static void Open() { GetWindow<MissingUiV1BuilderWindow>("Missing UI V1"); }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("신규 납품물의 독립 읽기 전용 시안입니다. 기존 운영 화면은 수정하지 않습니다. 프로젝트 PixelTypography의 pixelroborobo Font/TMP_FontAsset을 지정하세요.", MessageType.Info);
            _font = EditorGUILayout.ObjectField("Existing pixel font", _font, typeof(Object), false);
            if (GUILayout.Button("Build Missing UI V1"))
            {
                MissingUiV1Builder.ConfiguredFont = _font;
                MissingUiV1Builder.BuildMissingUiPreviewWithConfiguredFont();
            }
            if (GUILayout.Button("Validate Missing UI V1")) MissingUiV1Builder.ValidateMissingUiV1();
        }
    }

    public static class MissingUiV1Builder
    {
        public const string Root = "Assets/ANIMOL/UI/MissingUiV1";
        public const string Generated = Root + "/Generated";
        public const string PreviewScene = Generated + "/ANIMOL_MissingUiV1_ReadOnlyPreview.unity";
        public static Object ConfiguredFont;
        private static Object _font;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly Color Ink = Hex("1a1c2c"), Panel = Hex("333c57"), TextColor = Hex("f4f4f4"), Muted = Hex("94b0c2"), Accent = Hex("73eff7");

        public static void BuildMissingUiPreviewWithConfiguredFont()
        {
            _font = ResolveFont(); // Fail before writing when the existing project font is unavailable.
            var catalog = ReadCatalog();
            var manifest = ReadManifest();
            ImportNewSprites(manifest);
            Sprites.Clear();
            ValidateRequiredReferences(catalog);
            EnsureFolder(Generated + "/Prefabs");
            var prefabPaths = new List<string>();
            var originalScene = SceneManager.GetActiveScene();
            var stagingScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(stagingScene);
                foreach (var screen in catalog.screens)
                {
                    GameObject root = null;
                    try
                    {
                        root = BuildScreen(screen);
                        var path = Generated + "/Prefabs/" + screen.id + ".prefab";
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabPaths.Add(path);
                    }
                    finally { if (root != null) Object.DestroyImmediate(root); }
                }
            }
            finally { if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene); EditorSceneManager.CloseScene(stagingScene, true); }
            // Unity 6 cannot add a second scene while the temporary untitled staging scene is open.
            BuildIndependentScene(prefabPaths, catalog);
            // Prefabs/scenes/importers are saved explicitly above. Never flush unrelated dirty project assets.
            ValidateMissingUiV1();
            Debug.Log("Missing UI V1: " + catalog.screens.Length + " independent design prefabs and read-only preview scene generated. Existing production scenes/prefabs were not modified.");
        }

        [MenuItem("ANIMOL/Missing UI V1/Validate Missing UI V1")]
        public static void ValidateMissingUiV1()
        {
            var catalog = ReadCatalog();
            foreach (var screen in catalog.screens)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Generated + "/Prefabs/" + screen.id + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Build is required: " + screen.id);
                var view = prefab.GetComponent<MissingUiScreenView>();
                if (view == null || view.ScreenId != screen.id || !view.ReadOnlyDesignPreview || view.Host != null)
                    throw new InvalidOperationException("Unsafe or mismatched preview view: " + screen.id);
                if (prefab.GetComponentInChildren<MissingUiSafeArea>(true) == null || prefab.GetComponentInChildren<ScrollRect>(true) == null)
                    throw new InvalidOperationException("SafeArea/scroll body missing: " + screen.id);
                foreach (var binding in view.Bindings)
                    if (binding != null && binding.RequiresHost && binding.Control != null && binding.Control.interactable)
                        throw new InvalidOperationException("Unconnected service action must be disabled: " + screen.id + "/" + binding.SemanticKey);
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScene) == null) throw new InvalidOperationException("Preview scene is missing.");
            Debug.Log("Missing UI V1 static prefab structure validation passed. Device SafeArea, TMP/pixel typography, keyboard, host/service integration and production navigation still require Play Mode/device tests.");
        }

        private static MissingUiCatalog ReadCatalog()
        {
            var path = Root + "/screen-catalog.json";
            if (!File.Exists(path)) throw new FileNotFoundException("Import the package catalog first.", path);
            var catalog = JsonUtility.FromJson<MissingUiCatalog>(File.ReadAllText(path));
            if (catalog == null || catalog.schemaVersion != 1 || catalog.competitiveCapacity != 4 || catalog.screens == null || catalog.screens.Length == 0)
                throw new InvalidOperationException("Catalog must be schemaVersion 1 with competitiveCapacity 4 and nonempty screens.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var screen in catalog.screens)
                if (screen == null || string.IsNullOrEmpty(screen.id) || screen.id.Any(c => !(char.IsLetterOrDigit(c) || c == '-')) || !ids.Add(screen.id))
                    throw new InvalidOperationException("Screen IDs must be unique safe filename keys.");
            return catalog;
        }
        private static MissingUiManifest ReadManifest()
        {
            var path = Root + "/manifest.json";
            if (!File.Exists(path)) throw new FileNotFoundException("Import the sprite manifest first.", path);
            var manifest = JsonUtility.FromJson<MissingUiManifest>(File.ReadAllText(path));
            if (manifest == null || manifest.schemaVersion != 1 || manifest.assets == null) throw new InvalidOperationException("Malformed Missing UI manifest.");
            return manifest;
        }
        private static Object ResolveFont()
        {
            var selected = ConfiguredFont;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-missingUiFontPath") selected = AssetDatabase.LoadMainAssetAtPath(args[i + 1]);
            if (selected == null)
            {
                var candidates = AssetDatabase.FindAssets("pixelroborobo").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadMainAssetAtPath).Where(IsSupportedFont).Distinct().ToArray();
                if (candidates.Length == 1) selected = candidates[0];
            }
            if (!IsSupportedFont(selected))
                throw new InvalidOperationException("기존 pixelroborobo Font/TMP_FontAsset을 선택하세요. 새 폰트/Arial 대체는 생성하지 않습니다. batch: -missingUiFontPath Assets/<existing-font>.asset");
            var path = AssetDatabase.GetAssetPath(selected);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith(Root + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("Font must reference an existing project asset outside MissingUiV1.");
            return selected;
        }
        private static bool IsSupportedFont(Object font)
        {
            return font != null && (font is Font || font.GetType().FullName == "TMPro.TMP_FontAsset");
        }
        private static void ImportNewSprites(MissingUiManifest manifest)
        {
            foreach (var entry in manifest.assets)
            {
                if (entry == null || string.IsNullOrEmpty(entry.path) || !entry.path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                var relative = entry.path.StartsWith("sprites/", StringComparison.Ordinal) ? entry.path.Substring(8) : entry.path;
                if (relative.Contains("..") || Path.IsPathRooted(relative)) throw new InvalidOperationException("Unsafe manifest path.");
                var path = Root + "/Sprites/" + relative;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("New PNG not imported: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                if (entry.pivot != null && entry.pivot.Length == 2) { settings.spriteAlignment = 9; settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]); }
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritePixelsPerUnit = 32;
                if (entry.borderLBRTop != null && entry.borderLBRTop.Length == 4)
                    importer.spriteBorder = new Vector4(entry.borderLBRTop[0], entry.borderLBRTop[1], entry.borderLBRTop[2], entry.borderLBRTop[3]);
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (entry.size != null && entry.size.Length == 2 && (texture.width != entry.size[0] || texture.height != entry.size[1]))
                    throw new InvalidOperationException("Manifest size mismatch: " + path);
            }
        }
        private static void ValidateRequiredReferences(MissingUiCatalog catalog)
        {
            var names = new HashSet<string>(StringComparer.Ordinal) {
                "UI_Common_Panel_Main.png", "UI_Common_Button_Primary.png", "UI_Common_Button_Secondary.png", "UI_Icon_Back.png",
                "UI_Common_RowFrame.png", "UI_Common_InputField_Default.png", "UI_Common_ToggleTrack_Off.png", "UI_Common_ToggleTrack_On.png", "UI_Common_ToggleKnob.png",
                "UI_Common_Radio_Unselected.png", "UI_Common_Radio_Selected.png",
                "UI_Common_SliderRail.png", "UI_Common_SliderFill.png", "UI_Common_SliderThumb.png", "UI_Common_ScrollTrack.png", "UI_Common_ScrollThumb.png"
            };
            foreach (var screen in catalog.screens) foreach (var block in screen.blocks ?? new MissingUiBlock[0])
            {
                if (!string.IsNullOrEmpty(block.asset)) names.Add(block.asset);
                foreach (var item in block.items ?? new MissingUiItem[0]) if (!string.IsNullOrEmpty(item.asset)) names.Add(item.asset);
            }
            foreach (var name in names) Sprite(name, true);
        }
        private static Sprite Sprite(string name, bool required = false)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Sprite cached;
            if (Sprites.TryGetValue(name, out cached)) return cached;
            var matches = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(name) + " t:Sprite", new[] { Root + "/Sprites" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p) == name).Distinct().ToArray();
            Sprite found = matches.Length == 1 ? AssetDatabase.LoadAssetAtPath<Sprite>(matches[0]) : null;
            if (found == null) found = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/UI/ProductionV1/Art/" + name);
            if (found == null && matches.Length > 1) throw new InvalidOperationException("Ambiguous new sprite filename: " + name);
            if (found == null && required) throw new InvalidOperationException("필수 Sprite 참조 없음: " + name + ". 기존 ProductionV1/Art 또는 신규 MissingUiV1/Sprites 경로를 확인하세요. 기존 GUID/복제 폴더는 자동 선택하지 않습니다.");
            Sprites[name] = found;
            return found;
        }

        private static GameObject BuildScreen(MissingUiScreen screen)
        {
            var root = new GameObject("MissingUi_" + screen.id, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MissingUiScreenView));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 0;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            var view = root.GetComponent<MissingUiScreenView>(); view.ScreenId = screen.id; view.ExistingScreenReference = screen.existingScreen;
            var bindings = new List<MissingUiBinding>();
            var backdrop = Node("Backdrop", root.transform); Stretch(backdrop); Image(backdrop, null, Ink);
            var backgroundName = screen.family == "store" ? "BG_UI_Store.png" : screen.family == "multiplayer" ? "BG_UI_Multiplayer_Common.png" : "BG_UI_Utility_Common.png";
            var background = Sprite(backgroundName); if (background != null) { var bg = Node("SubscreenBackground", backdrop); Stretch(bg); Image(bg, background, Color.white).preserveAspect = true; }
            var safe = Node("SafeArea", root.transform); Stretch(safe); safe.gameObject.AddComponent<MissingUiSafeArea>();
            var header = Node("HeaderFixed", safe); Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -190), Vector2.zero);
            var back = Node("BackTouch96", header); back.anchorMin = back.anchorMax = new Vector2(0, 1); back.pivot = new Vector2(0, 1); back.anchoredPosition = new Vector2(32, -28); back.sizeDelta = new Vector2(96, 96);
            Image(back, Sprite("UI_Common_Button_Secondary.png"), Color.white, true); var backButton = back.gameObject.AddComponent<Button>();
            var backIcon = Node("Icon", back); Stretch(backIcon, 20); Image(backIcon, Sprite("UI_Icon_Back.png"), Color.white).preserveAspect = true;
            Bind(backButton, "header/back", "menu", "back", true, bindings);
            var heading = Node("Heading", header); Anchor(heading, Vector2.zero, Vector2.one, new Vector2(160, 38), new Vector2(-30, -24));
            var headerLayout = heading.gameObject.AddComponent<VerticalLayoutGroup>(); headerLayout.childControlHeight = true; headerLayout.childForceExpandHeight = false; headerLayout.spacing = 4;
            Text(heading, screen.title, 44, TextColor); Text(heading, screen.subtitle, 26, Muted); Text(heading, "디자인 시안 · 실제 데이터/서비스 연결 전", 22, Accent);
            var body = Node("BodyScroll", safe); Anchor(body, Vector2.zero, Vector2.one, new Vector2(28, 178), new Vector2(-28, -200));
            var scroll = body.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Node("Viewport", body); Stretch(viewport); viewport.offsetMax = new Vector2(-96, 0); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node("Content", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>(); contentLayout.spacing = 20; contentLayout.padding = new RectOffset(4, 4, 4, 16); contentLayout.childControlHeight = true; contentLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            BuildScrollbar(body, scroll);
            foreach (var block in screen.blocks ?? new MissingUiBlock[0]) BuildBlock(content, block, bindings);
            var footer = Node("FooterFixed", safe); Anchor(footer, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 166)); Image(footer, null, Ink);
            var footerButton = Node("FooterTouch", footer); Stretch(footerButton); footerButton.offsetMin = new Vector2(32, 26); footerButton.offsetMax = new Vector2(-32, -24);
            Image(footerButton, Sprite("UI_Common_Button_Primary.png"), Color.white, true);
            var footerControl = footerButton.gameObject.AddComponent<Button>(); var label = Text(footerButton, screen.footer == null ? "연결 대기" : screen.footer.label, 32, TextColor); Stretch((RectTransform)label.transform, 14);
            Bind(footerControl, "footer", "footer", screen.footer == null ? "" : screen.footer.action, screen.footer != null && screen.footer.enabled, bindings);
            BuildResetPanel(safe, view, bindings);
            BuildImagePanel(safe, view, bindings);
            view.ControlDiagram = content.GetComponentInChildren<MissingUiControlDiagram>(true);
            view.Bindings = bindings.ToArray();
            return root;
        }

        private static void BuildBlock(Transform parent, MissingUiBlock block, List<MissingUiBinding> bindings)
        {
            var card = Node(block.id + "_" + block.kind, parent);
            var frame = block.kind == "product" ? "UI_Store_ProductFrame.png" : "UI_Common_RowFrame.png";
            if (block.kind == "participants") frame = "UI_Multi_PlayerCardFrame.png";
            Image(card, Sprite(frame), Color.white, true);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(28, 28, 24, 24); layout.spacing = 14; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            var heading = Node("LabelRow", card); var row = heading.gameObject.AddComponent<HorizontalLayoutGroup>(); row.spacing = 20; row.childControlHeight = true; row.childForceExpandHeight = false; row.childForceExpandWidth = false;
            if (!string.IsNullOrEmpty(block.asset) && block.asset != "UI_Control_LayoutPreview.png")
            {
                var art = Node("FullRectArt", heading); var size = block.kind == "product" ? 176 : block.kind == "help" ? 216 : 76;
                Image(art, Sprite(block.asset), Color.white).preserveAspect = true;
                var artLayout = art.gameObject.AddComponent<LayoutElement>(); artLayout.preferredWidth = size; artLayout.preferredHeight = size;
            }
            var words = Node("Words", heading); words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var wordLayout = words.gameObject.AddComponent<VerticalLayoutGroup>(); wordLayout.spacing = 10; wordLayout.childControlHeight = true; wordLayout.childForceExpandHeight = false;
            Text(words, block.title, 34, Ink); if (!string.IsNullOrEmpty(block.body)) Text(words, block.body, 28, Panel);
            var binding = card.gameObject.AddComponent<MissingUiBinding>(); binding.SemanticKey = block.id; binding.Kind = block.kind; binding.Action = block.action; binding.DesignEnabled = block.enabled; binding.RequiresHost = IsService(block.action); bindings.Add(binding);
            if (block.kind == "slider") BuildSlider(card, block, binding);
            else if (block.kind == "toggle") BuildToggle(card, block, binding);
            else if (block.kind == "input") BuildInput(card, block, binding);
            else if (block.kind == "choice") BuildChoices(card, block, bindings);
            else if (block.items != null) foreach (var item in block.items) BuildItem(card, block, item, bindings);
            if (block.asset == "UI_Control_LayoutPreview.png") BuildControlDiagram(card);
            if (!string.IsNullOrEmpty(block.secondary)) Text(card, block.secondary, 25, Panel);
            if (!string.IsNullOrEmpty(block.action) && block.kind != "slider" && block.kind != "toggle" && block.kind != "input" && block.kind != "choice")
            {
                binding.Control = card.gameObject.AddComponent<Button>(); binding.Control.interactable = block.enabled && !binding.RequiresHost;
            }
        }
        private static void BuildItem(Transform parent, MissingUiBlock block, MissingUiItem item, List<MissingUiBinding> bindings)
        {
            var row = Node(item.id, parent); Image(row, Sprite("UI_Common_RowFrame.png"), Color.white, true);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.padding = new RectOffset(16, 16, 16, 16); layout.spacing = 16; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(item.asset))
            {
                var icon = Node("Art", row); Image(icon, Sprite(item.asset), Color.white).preserveAspect = true; var le = icon.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 88; le.preferredHeight = 88;
            }
            var text = Node("Words", row); text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var words = text.gameObject.AddComponent<VerticalLayoutGroup>(); words.spacing = 8; words.childForceExpandHeight = false;
            Text(text, item.title, 30, Ink); Text(text, item.body, 25, Panel);
            if (block.kind == "emotes" && item.id.StartsWith("EMOTE_", StringComparison.Ordinal) && !string.IsNullOrEmpty(item.asset))
            {
                var binding = Bind(row.gameObject.AddComponent<Button>(), block.id + "/" + item.id, "emote-inspect", "local:inspect-image", true, bindings); binding.InspectSprite = Sprite(item.asset);
            }
            else if (!string.IsNullOrEmpty(item.action)) Bind(row.gameObject.AddComponent<Button>(), block.id + "/" + item.id, block.kind, item.action, item.enabled, bindings);
            else { var b = row.gameObject.AddComponent<MissingUiBinding>(); b.SemanticKey = block.id + "/" + item.id; b.Kind = block.kind; bindings.Add(b); }
        }
        private static void BuildChoices(Transform parent, MissingUiBlock block, List<MissingUiBinding> bindings)
        {
            foreach (var item in block.items ?? new MissingUiItem[0])
            {
                var row = Node(item.id, parent); Image(row, Sprite("UI_Common_Button_Secondary.png"), Color.white, true);
                var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 112;
                var radio = Node("RadioState", row); radio.anchorMin = radio.anchorMax = new Vector2(0, .5f); radio.pivot = new Vector2(0, .5f); radio.anchoredPosition = new Vector2(24, 0); radio.sizeDelta = new Vector2(48, 48); var radioImage = Image(radio, Sprite("UI_Common_Radio_Unselected.png"), Color.white);
                var text = Text(row, item.title + (string.IsNullOrEmpty(item.body) ? "" : " · " + item.body), 28, TextColor); Stretch((RectTransform)text.transform, 18); ((RectTransform)text.transform).offsetMin = new Vector2(92, 18);
                var visual = row.gameObject.AddComponent<MissingUiChoiceVisual>(); visual.Radio = radioImage; visual.Unselected = Sprite("UI_Common_Radio_Unselected.png"); visual.Selected = Sprite("UI_Common_Radio_Selected.png"); string designValue; visual.Apply(MissingUiDesignDefaults.Create().TryGetValue(block.id, out designValue) && designValue == item.id);
                var binding = Bind(row.gameObject.AddComponent<Button>(), block.id + "/" + item.id, "choice", block.action, block.enabled, bindings); binding.LocalChoiceValue = item.id; binding.LocalGroupKey = block.id;
            }
        }
        private static void BuildSlider(Transform parent, MissingUiBlock block, MissingUiBinding binding)
        {
            var touch = Node("SliderTouch96", parent); touch.gameObject.AddComponent<LayoutElement>().minHeight = 104;
            Image(touch, null, Color.clear);
            var slider = touch.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1; string designValue; float designNumber; slider.SetValueWithoutNotify(MissingUiDesignDefaults.Create().TryGetValue(block.id, out designValue) && float.TryParse(designValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out designNumber) ? designNumber : .5f);
            var rail = Node("Rail", touch); Anchor(rail, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(24, -8), new Vector2(-24, 8)); Image(rail, Sprite("UI_Common_SliderRail.png"), Color.white, true);
            var fillArea = Node("FillArea", touch); Stretch(fillArea); fillArea.offsetMin = new Vector2(24, 44); fillArea.offsetMax = new Vector2(-24, -44);
            var fill = Node("Fill", fillArea); Stretch(fill); Image(fill, Sprite("UI_Common_SliderFill.png"), Color.white, true); slider.fillRect = fill;
            var handleArea = Node("HandleArea", touch); Stretch(handleArea); handleArea.offsetMin = new Vector2(24, 0); handleArea.offsetMax = new Vector2(-24, 0);
            var handle = Node("Thumb", handleArea); handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1); handle.sizeDelta = new Vector2(64, -8);
            var handleImage = Image(handle, Sprite("UI_Common_SliderThumb.png"), Color.white); handleImage.preserveAspect = true; slider.handleRect = handle; slider.targetGraphic = handleImage;
            binding.Control = slider; slider.interactable = block.enabled;
            Text(parent, "시안 조작값 · 저장/실제 설정 적용 없음", 22, Muted);
        }
        private static void BuildToggle(Transform parent, MissingUiBlock block, MissingUiBinding binding)
        {
            var touch = Node("ToggleTouch96", parent); touch.gameObject.AddComponent<LayoutElement>().minHeight = 104;
            Image(touch, null, Color.clear);
            var toggle = touch.gameObject.AddComponent<Toggle>();
            var track = Node("Track", touch); track.anchorMin = track.anchorMax = new Vector2(.5f, .5f); track.sizeDelta = new Vector2(160, 80); var trackImage = Image(track, Sprite("UI_Common_ToggleTrack_Off.png"), Color.white);
            var knob = Node("Knob", track); knob.anchorMin = knob.anchorMax = new Vector2(.25f, .5f); knob.sizeDelta = new Vector2(56, 56); Image(knob, Sprite("UI_Common_ToggleKnob.png"), Color.white);
            toggle.graphic = null; toggle.targetGraphic = trackImage; toggle.isOn = block.id == "vibration";
            var visual = touch.gameObject.AddComponent<MissingUiToggleVisual>(); visual.Track = trackImage; visual.Knob = knob; visual.OffSprite = Sprite("UI_Common_ToggleTrack_Off.png"); visual.OnSprite = Sprite("UI_Common_ToggleTrack_On.png");
            binding.Control = toggle; toggle.interactable = block.enabled;
            Text(parent, "OFF / ON · 시안 상태, 실제 설정 미연결", 22, Muted);
        }
        private static void BuildInput(Transform parent, MissingUiBlock block, MissingUiBinding binding)
        {
            var touch = Node("FlexibleCodeInput", parent); touch.gameObject.AddComponent<LayoutElement>().minHeight = 120; Image(touch, Sprite("UI_Common_InputField_Default.png"), Color.white, true);
            var textArea = Node("TextArea", touch); Stretch(textArea, 20); textArea.gameObject.AddComponent<RectMask2D>();
            var inputText = Text(textArea, "", 30, TextColor); Stretch((RectTransform)inputText.transform);
            var placeholder = Text(textArea, "서비스가 발급한 코드를 입력", 28, Muted); Stretch((RectTransform)placeholder.transform);
            if (_font is Font)
            {
                var input = touch.gameObject.AddComponent<InputField>(); input.textComponent = (Text)inputText; input.placeholder = (Graphic)placeholder; input.characterLimit = 0; input.lineType = InputField.LineType.SingleLine; binding.Control = input;
            }
            else
            {
                var type = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                if (type == null) throw new InvalidOperationException("Existing TMP input component is unavailable.");
                var input = touch.gameObject.AddComponent(type); Set(input, "textComponent", inputText); Set(input, "placeholder", placeholder); Set(input, "textViewport", textArea); Set(input, "characterLimit", 0); binding.Control = (Selectable)input;
            }
            binding.Control.interactable = block.enabled;
            Text(parent, "길이/문자 규칙은 서비스 계약으로 검증 · 붙여넣기 정책은 미연결", 22, Muted);
        }
        private static void BuildScrollbar(Transform parent, ScrollRect scroll)
        {
            var touch = Node("ScrollTouch96", parent); Anchor(touch, new Vector2(1, 0), Vector2.one, new Vector2(-96, 0), Vector2.zero);
            Image(touch, null, Color.clear);
            var track = Node("Track", touch); Stretch(track); track.offsetMin = new Vector2(36, 0); track.offsetMax = new Vector2(-36, 0); Image(track, Sprite("UI_Common_ScrollTrack.png"), Color.white, true);
            var area = Node("SlidingArea", touch); Stretch(area);
            var handle = Node("Thumb", area); Stretch(handle); handle.offsetMin = new Vector2(32, 0); handle.offsetMax = new Vector2(-32, 0);
            var graphic = Image(handle, Sprite("UI_Common_ScrollThumb.png"), Color.white, true);
            var bar = touch.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = graphic; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        private static void BuildResetPanel(Transform safe, MissingUiScreenView view, List<MissingUiBinding> bindings)
        {
            var overlay = Node("DesignResetConfirmation", safe); Stretch(overlay); Image(overlay, null, new Color(Ink.r, Ink.g, Ink.b, .96f));
            var box = Node("Panel", overlay); Anchor(box, new Vector2(0, .3f), new Vector2(1, .7f), new Vector2(40, 0), new Vector2(-40, 0)); Image(box, Sprite("UI_Common_Panel_Main.png"), Color.white, true);
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(32, 32, 40, 40); layout.spacing = 24; layout.childForceExpandHeight = false;
            Text(box, "시안 설정 초기화", 38, TextColor); Text(box, "미리보기의 로컬 위젯만 초기화합니다. 실제 설정과 계정 데이터는 변경되지 않습니다.", 30, Muted);
            foreach (var pair in new[] { new[] { "취소", "local:cancel-reset" }, new[] { "시안만 초기화", "local:confirm-reset" } })
            {
                var row = Node(pair[1], box); row.gameObject.AddComponent<LayoutElement>().minHeight = 112; Image(row, Sprite("UI_Common_Button_Secondary.png"), Color.white, true);
                var text = Text(row, pair[0], 30, TextColor); Stretch((RectTransform)text.transform, 20); Bind(row.gameObject.AddComponent<Button>(), pair[1], "preview-modal", pair[1], true, bindings);
            }
            view.PreviewResetPanel = overlay.gameObject; overlay.gameObject.SetActive(false);
        }
        private static void BuildImagePanel(Transform safe, MissingUiScreenView view, List<MissingUiBinding> bindings)
        {
            var overlay = Node("ReadOnlyImageViewer", safe); Stretch(overlay); Image(overlay, null, new Color(Ink.r, Ink.g, Ink.b, .96f));
            var box = Node("Panel", overlay); Anchor(box, new Vector2(0, .15f), new Vector2(1, .85f), new Vector2(40, 0), new Vector2(-40, 0)); Image(box, Sprite("UI_Common_Panel_Main.png"), Color.white, true);
            var words = Text(box, "이모티콘 그림 보기 · 장착/보유 변경 없음", 30, TextColor); Anchor((RectTransform)words.transform, new Vector2(0, 1), Vector2.one, new Vector2(36, -120), new Vector2(-36, -30));
            var art = Node("WholeImage", box); Anchor(art, new Vector2(0, 0), Vector2.one, new Vector2(80, 170), new Vector2(-80, -150)); view.PreviewImage = Image(art, null, Color.white); view.PreviewImage.preserveAspect = true;
            var close = Node("CloseTouch112", box); Anchor(close, Vector2.zero, new Vector2(1, 0), new Vector2(32, 28), new Vector2(-32, 140)); Image(close, Sprite("UI_Common_Button_Secondary.png"), Color.white, true);
            var label = Text(close, "닫기", 30, TextColor); Stretch((RectTransform)label.transform, 20); Bind(close.gameObject.AddComponent<Button>(), "image-viewer/close", "preview-modal", "local:close-image", true, bindings);
            view.PreviewImagePanel = overlay.gameObject; overlay.gameObject.SetActive(false);
        }
        private static void BuildControlDiagram(Transform parent)
        {
            var device = Node("LiveDesignLayout", parent); device.gameObject.AddComponent<LayoutElement>().preferredHeight = 460;
            var outline = Node("DeviceOutline", device); outline.anchorMin = outline.anchorMax = new Vector2(.5f, .5f); outline.sizeDelta = new Vector2(300, 450); Image(outline, Sprite("UI_Control_LayoutPreview.png"), Color.white).preserveAspect = true;
            var controls = Node("IndependentControlLayers", outline); controls.anchorMin = controls.anchorMax = new Vector2(.5f, .5f); controls.sizeDelta = new Vector2(180, 96); var opacity = controls.gameObject.AddComponent<CanvasGroup>(); opacity.blocksRaycasts = false;
            var diagram = device.gameObject.AddComponent<MissingUiControlDiagram>(); diagram.Controls = controls; diagram.ControlsOpacity = opacity;
            var i = 0;
            foreach (var iconName in new[] { "UI_Icon_HandLeft.png", "UI_Icon_HandRight.png", "UI_Icon_TouchControls.png" })
            {
                var point = Node("LayoutPoint" + i, controls); point.anchorMin = point.anchorMax = new Vector2(.5f, .5f); point.sizeDelta = new Vector2(52, 52); point.anchoredPosition = new Vector2((i - 1) * 60, i == 2 ? -56 : 0); Image(point, Sprite("UI_Common_Button_Secondary.png"), Color.white, true);
                var icon = Node("Icon", point); Stretch(icon, 10); Image(icon, Sprite(iconName), Color.white).preserveAspect = true; if (i == 2) { diagram.DownButton = point.gameObject; point.gameObject.SetActive(false); } i++;
            }
            Text(parent, "조작 배치 시안 · 실제 게임 HUD 버튼과 기능은 기존 프로젝트를 재사용", 22, Muted);
        }

        private static MissingUiBinding Bind(Selectable control, string key, string kind, string action, bool enabled, List<MissingUiBinding> bindings)
        {
            var b = control.gameObject.AddComponent<MissingUiBinding>(); b.SemanticKey = key; b.Kind = kind; b.Action = action; b.DesignEnabled = enabled; b.RequiresHost = IsService(action); b.Control = control;
            control.interactable = enabled && !b.RequiresHost; bindings.Add(b); return b;
        }
        private static bool IsService(string action)
        {
            return !string.IsNullOrEmpty(action) && action != "back" && action != "reset-settings" && action != "reset-controls" && !action.StartsWith("navigate:", StringComparison.Ordinal) && !action.StartsWith("local:", StringComparison.Ordinal);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static Image Image(RectTransform rect, Sprite sprite, Color color, bool sliced = false)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.type = sliced && sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; image.raycastTarget = true; return image;
        }
        private static Component Text(Transform parent, string value, int size, Color color)
        {
            var rect = Node("Text", parent);
            if (_font is Font)
            {
                var label = rect.gameObject.AddComponent<Text>(); label.font = (Font)_font; label.text = value ?? ""; label.fontSize = size; label.color = color; label.raycastTarget = false; label.alignment = TextAnchor.UpperLeft; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow; return label;
            }
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (type == null) throw new InvalidOperationException("Existing TMP component is unavailable.");
            var tmp = rect.gameObject.AddComponent(type); Set(tmp, "font", _font); Set(tmp, "text", value ?? ""); Set(tmp, "fontSize", (float)size); Set(tmp, "color", color); Set(tmp, "enableAutoSizing", false); Set(tmp, "raycastTarget", false); return tmp;
        }
        private static void Set(Component component, string propertyName, object value)
        {
            var property = component.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite) throw new InvalidOperationException(component.GetType().Name + "." + propertyName + " property unavailable; inspect actual TMP package API.");
            property.SetValue(component, value, null);
        }
        private static void Stretch(RectTransform rect, float padding = 0) { Anchor(rect, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding)); }
        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        private static Color Hex(string value) { Color color; ColorUtility.TryParseHtmlString("#" + value, out color); return color; }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void BuildIndependentScene(List<string> prefabPaths, MissingUiCatalog catalog)
        {
            var previous = SceneManager.GetActiveScene();
            var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(preview);
                var screens = new List<MissingUiScreenView>();
                foreach (var path in prefabPaths)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), preview);
                    var view = instance.GetComponent<MissingUiScreenView>(); screens.Add(view); instance.SetActive(false);
                }
                var index = BuildScreen(new MissingUiScreen { id = "preview-index", title = "미완성 UI 제작 시안", subtitle = "화면을 선택해 구조와 신규 부품을 확인하세요", family = "utility",
                    blocks = catalog.screens.Select(s => new MissingUiBlock { id = s.id, kind = "menu", title = s.title, body = s.subtitle, action = "navigate:" + s.id, enabled = true }).ToArray(),
                    footer = new MissingUiFooter { label = "읽기 전용 제작 시안 · 운영 서비스 미연결", enabled = false } });
                screens.Add(index.GetComponent<MissingUiScreenView>());
                foreach (var screen in screens) screen.gameObject.SetActive(screen.ScreenId == "preview-index");
                var navigator = new GameObject("ReadOnlyPreviewNavigator", typeof(MissingUiPreviewNavigator)).GetComponent<MissingUiPreviewNavigator>(); navigator.Screens = screens.ToArray(); navigator.InitialScreenId = "preview-index";
                var eventRoot = new GameObject("PreviewEventSystem", typeof(EventSystem), typeof(MissingUiPreviewEventSystem));
#if ENABLE_INPUT_SYSTEM
                var inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                {
                    var module = eventRoot.AddComponent(inputSystemType);
                    var assign = inputSystemType.GetMethod("AssignDefaultActions", BindingFlags.Public | BindingFlags.Instance);
                    if (assign != null) assign.Invoke(module, null);
                }
                else throw new InvalidOperationException("Active Input Handling enables Input System, but InputSystemUIInputModule is unavailable.");
#else
                eventRoot.AddComponent<StandaloneInputModule>();
#endif
                if (!EditorSceneManager.SaveScene(preview, PreviewScene)) throw new InvalidOperationException("Preview scene save failed.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(preview, true);
            }
        }
    }
}
