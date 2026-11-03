#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Animol.UI.Pattern;
using DiagonalPattern;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace William.NotebookUI.Editor
{
    public static class NotebookInstaller
    {
        private const string Root = "Assets/ANIMOL/", Data = Root + "Data/UI/Notebook/", MainTex = Root + "Art/UI/Main/Textures/", MapTex = Root + "Art/UI/ConstellationMap/Textures/", SettingsTex = Root + "Art/UI/Settings/Textures/", GeneratedPrefab = Root + "Prefabs/UI/Main/NotebookCanvas.prefab", GeneratedScene = Root + "Scenes/Main/NotebookUI.unity", GeneratedFont = Root + "Fonts/Notebook/Galmuri14.asset";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color32(41, 38, 59, 255);
        private static readonly Color SkyText = new Color32(255, 235, 197, 255);

        [MenuItem("ANIMOL/New Notebook UI/Build Fresh Scene")]
        public static void BuildFreshScene() => BuildFreshSceneInternal(true);

        public static void BuildFreshSceneForAutomation() => BuildFreshSceneInternal(false);

        private static void BuildFreshSceneInternal(bool confirmCurrentScene)
        {
            if (confirmCurrentScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ImportArt(); EnsureFolder("Assets/ANIMOL/Prefabs/UI/Main"); EnsureFolder("Assets/ANIMOL/Scenes/Main"); font = EnsureFont();
            NotebookCatalog catalog = NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "catalog.json"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = new GameObject("William Notebook UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(NotebookCanvasLayout));
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Image paper = Picture(canvasRect, "Static Paper Base", "Paper/Base"); Stretch(paper.rectTransform, 0, 0, 0, 0);
            RectTransform patternRect = Rect(canvasRect, "ORIGINAL Character Faces And Stars"); Stretch(patternRect, 0, 0, 0, 0);
            RawImage raw = patternRect.gameObject.AddComponent<RawImage>(); raw.color = Color.white; raw.raycastTarget = false;
            DiagonalPatternBackground effect = patternRect.gameObject.AddComponent<DiagonalPatternBackground>();
            Texture2D pattern = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ANIMOL/Art/UI/Main/Pattern/Textures/CharacterFacesStars_All_2048.png");
            Shader patternShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/ANIMOL/Art/UI/Main/Pattern/Shaders/UI_AnimolMonochromePattern.shader");
            if (!pattern || !patternShader) throw new InvalidOperationException("The unchanged original scrolling assets are required.");
            effect.Configure(patternShader, pattern);
            // Exactly the original fixed ink material. No theme palette changes or generated/repainted face texture.
            effect.SetAppearance(pattern, Color.clear, new Color32(166, 110, 72, 255), .16f);
            effect.VisualVelocity = new Vector2(8, 8);
            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("tileSize").vector2Value = new Vector2(1080, 1080);
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AnimolPatternMotion motion = patternRect.gameObject.AddComponent<AnimolPatternMotion>();
            RectTransform design = Rect(canvasRect, "Design 1080 Safe Area"); design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f); design.sizeDelta = new Vector2(1080, 1920);
            NotebookCanvasLayout layout = canvasObject.GetComponent<NotebookCanvasLayout>(); layout.scaler = scaler; layout.design = design;
            NotebookRoot ui = design.gameObject.AddComponent<NotebookRoot>(); ui.catalogJson = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "catalog.json"); ui.patternMotion = motion;
            ui.gateway = design.gameObject.AddComponent<NotebookProjectGateway>();
            ui.mainScreen = Screen(design, "Main").gameObject; ui.settingsScreen = Screen(design, "Settings").gameObject; ui.stageScreen = Screen(design, "Constellation Only").gameObject;
            BuildMain(ui, catalog); BuildSettings(ui); BuildMap(ui, catalog); BuildModal(ui, design);
            ui.toggleOn = Load("Controls/Toggle_On"); ui.toggleOff = Load("Controls/Toggle_Off");
            ui.starAvailable = Load("Map/Star_Available"); ui.starCurrent = Load("Map/Star_Current"); ui.starDone = Load("Map/Star_Done"); ui.starLocked = Load("Map/Star_Locked");
            ui.iconLock = Load("Icons/lock"); ui.iconTelescope = Load("Icons/telescope"); ui.iconCheck = Load("Icons/check"); ui.iconBattle = Load("Icons/battle"); ui.iconCustom = Load("Icons/custom"); ui.iconProfile = Load("Icons/profile");
            ui.soundOnIcon = Load("Icons/sound"); ui.soundOffIcon = Load("Icons/sound-off");
            ui.settingsScreen.SetActive(false); ui.stageScreen.SetActive(false); ui.modal.SetActive(false);
            GameObject events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
            EditorUtility.SetDirty(ui);
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject, GeneratedPrefab, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, GeneratedScene);
            var remaining = EditorBuildSettings.scenes.Where(s => s.path != GeneratedScene && s.path != "Assets/ANIMOL/Scenes/Archive/PreviousMain.unity").ToList();
            remaining.Insert(0, new EditorBuildSettingsScene(GeneratedScene, true)); EditorBuildSettings.scenes = remaining.ToArray();
            AssetDatabase.SaveAssets(); Selection.activeGameObject = canvasObject;
            Debug.Log("Fresh Notebook UI scene and Canvas prefab saved. No previous menu prefab or color initializer was reused. Bind your NotebookGateway, product IDs and real stage IDs before live play.");
        }

        private static void BuildMain(NotebookRoot ui, NotebookCatalog catalog)
        {
            RectTransform host = (RectTransform)ui.mainScreen.transform;
            ui.profileButton = Button(host, "Guest", "Button", 48, 18, 330, 132, "게스트", 42); Icon(ui.profileButton.transform, "profile", 28, 33, 66, 66); MoveText(ui.profileButton, 110, 0, 196, 132);
            ui.muteButton = Button(host, "Sound", "Utility", 744, 18, 132, 132, "", 1); ui.muteIcon = Icon(ui.muteButton.transform, "sound", 33, 33, 66, 66);
            ui.settingsButton = Button(host, "Settings", "Utility", 900, 18, 132, 132, "", 1); Icon(ui.settingsButton.transform, "settings", 33, 33, 66, 66);
            RectTransform hero = Rect(host, "Static Illustrated Hero"); Stretch(hero, 0, 166, 0, 346); hero.gameObject.AddComponent<RectMask2D>();
            ui.worldImage = Picture(hero, "Static World", "Worlds/starway"); Stretch(ui.worldImage.rectTransform, 0, 0, 0, 0);
            var fit = ui.worldImage.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectRatio = 1024f / 1536f; fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            Image title = Picture(hero, "Preserved Title", "Brand/title"); At(title.rectTransform, 48, 32, 984, 360); title.preserveAspect = true;
            RectTransform person = Rect(hero, "Character Contain Area"); Stretch(person, 60, 392, 60, 24);
            ui.characterImage = Picture(person, "Full Character No Cropping", "Characters/choa"); Stretch(ui.characterImage.rectTransform, 0, 0, 0, 0); ui.characterImage.preserveAspect = true;
            RectTransform footer = Rect(host, "Main Footer"); Bottom(footer, 0, 0, 1080, 346);
            Image divider = Picture(footer, "Static Divider", "Paper/Divider"); At(divider.rectTransform, 0, 0, 1080, 12);
            ui.campaignButton = Button(footer, "Campaign Direct To Constellation", "Primary", 48, 24, 984, 150, "캠페인 시작", 60);
            Icon(ui.campaignButton.transform, "campaign", 86, 42, 66, 66); Icon(ui.campaignButton.transform, "arrow", 882, 42, 66, 66);
            ui.battleButton = Button(footer, "Battle", "Button", 48, 190, 480, 132, "대전", 42); Icon(ui.battleButton.transform, "battle", 72, 33, 66, 66); ui.battleLock = Icon(ui.battleButton.transform, "lock", 368, 44, 44, 44);
            ui.customButton = Button(footer, "Custom", "Button", 552, 190, 480, 132, "커스텀", 42); Icon(ui.customButton.transform, "custom", 63, 33, 66, 66); MoveText(ui.customButton, 136, 0, 300, 132);
            var roster = JsonUtility.FromJson<Roster>(File.ReadAllText(Data + "roster.json"));
            string[][] people = { roster.starway, roster.theater, roster.observatory, roster.sea };
            ui.worlds = catalog.themes.Select((t, i) => new NotebookWorldArt { themeId = t.id, background = Load("Worlds/" + t.id), characters = people[i].Select(p => Load("Characters/" + p)).ToArray() }).ToArray();
        }
        [Serializable] private sealed class Roster { public string[] starway, theater, observatory, sea; }

        private static void BuildSettings(NotebookRoot ui)
        {
            RectTransform host = (RectTransform)ui.settingsScreen.transform;
            ui.settingsBack = Button(host, "Back", "Utility", 48, 18, 132, 132, "", 1); Icon(ui.settingsBack.transform, "back", 33, 33, 66, 66);
            Text(host, "Settings Title", "설정", 214, 52, 700, 92, 66);
            Image panel = Picture(host, "Ruled Notebook Panel", "Paper/SettingsPanel", true); Stretch(panel.rectTransform, 48, 210, 48, 90);
            RectTransform viewport = Rect(panel.transform, "Settings Viewport"); Stretch(viewport, 104, 60, 64, 40); viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect(viewport, "Settings Content"); At(content, 0, 0, 816, 1400);
            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            Text(content, "Audio Heading", "소리와 플레이", 0, 8, 816, 70, 50);
            ui.bgmSlider = Slider(content, "BGM", "배경 음악", 106, out ui.bgmValue);
            ui.sfxSlider = Slider(content, "SFX", "효과음", 316, out ui.sfxValue);
            ui.soundToggle = Toggle(content, "Sound", "전체 소리", 526);
            ui.hapticToggle = Toggle(content, "Haptic", "진동", 662);
            ui.scrollToggle = Toggle(content, "Original Pattern Motion", "얼굴 배경 스크롤", 798);
            Text(content, "Account Heading", "계정과 구매", 0, 1006, 816, 70, 50);
            ui.accountButton = Button(content, "Account", "Button", 0, 1108, 816, 124, "게스트 계정", 42); MoveText(ui.accountButton, 40, 0, 650, 124, TextAlignmentOptions.MidlineLeft); Icon(ui.accountButton.transform, "arrow", 710, 29, 66, 66);
            ui.restoreButton = Button(content, "Restore Purchases", "Button", 0, 1252, 816, 124, "구매 내역 복원", 42); MoveText(ui.restoreButton, 40, 0, 650, 124, TextAlignmentOptions.MidlineLeft); Icon(ui.restoreButton.transform, "arrow", 710, 29, 66, 66);
        }
        private static Slider Slider(RectTransform parent, string name, string label, float top, out TMP_Text value)
        {
            Text(parent, name + " Label", label, 0, top, 600, 60, 42); value = Text(parent, name + " Value", "", 616, top, 200, 60, 36, TextAlignmentOptions.MidlineRight);
            RectTransform box = Rect(parent, name + " Slider"); At(box, 0, top + 60, 816, 136);
            Image hit = box.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            Image track = Picture(box, "Static Track", "Controls/SliderTrack", true); At(track.rectTransform, 0, 50, 816, 36); track.raycastTarget = true;
            RectTransform area = Rect(box, "Handle Area"); Stretch(area, 38, 30, 38, 30);
            Image handle = Picture(area, "Static Thumb", "Controls/SliderThumb"); handle.rectTransform.sizeDelta = new Vector2(76, 76); handle.raycastTarget = true;
            Slider slider = box.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.transition = Selectable.Transition.None; return slider;
        }
        private static Button Toggle(RectTransform parent, string name, string label, float top)
        {
            Text(parent, name + " Label", label, 0, top, 600, 136, 42);
            RectTransform area = Rect(parent, name + " Hit Area"); At(area, 640, top, 176, 136);
            Image hit = area.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            Image image = Picture(area, "Toggle PNG", "Controls/Toggle_On"); At(image.rectTransform, 8, 24, 168, 88);
            Button button = area.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None; return button;
        }

        private static void BuildMap(NotebookRoot ui, NotebookCatalog catalog)
        {
            RectTransform host = (RectTransform)ui.stageScreen.transform;
            host.gameObject.AddComponent<RectMask2D>();
            RectTransform viewport = Rect(host, "Constellation Scroll View"); Stretch(viewport, 0, 240, 0, 324); viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            RectTransform content = Rect(viewport, "Four Regions One Map"); At(content, 0, 0, 1080, catalog.mapContentHeight);
            ui.mapScroll = viewport.gameObject.AddComponent<ScrollRect>(); ui.mapScroll.viewport = viewport; ui.mapScroll.content = content; ui.mapScroll.horizontal = false; ui.mapScroll.vertical = true; ui.mapScroll.movementType = ScrollRect.MovementType.Clamped;
            // One finished night-sky PNG keeps the same palette in every region.
            // Illustrated constellation figures scroll separately beneath their star nodes.
            var backdropWindows = new List<NotebookMapBackdropWindow>();
            foreach (NotebookTheme t in catalog.themes)
            {
                RectTransform region = Rect(content, "Region Illustration Clip " + t.id);
                At(region, 0, t.startY, 1080, t.height); region.gameObject.AddComponent<RectMask2D>();
                RectTransform window = Rect(region, "Pinned Illustration Window");
                At(window, 0, 0, 1080, 1356); window.gameObject.AddComponent<RectMask2D>();
                Image illustration = Picture(window, "Static Night Sky " + t.id, "Map/NightSky");
                Stretch(illustration.rectTransform, 0, 0, 0, 0);
                var pictureFit = illustration.gameObject.AddComponent<AspectRatioFitter>();
                pictureFit.aspectRatio = illustration.sprite.rect.width / illustration.sprite.rect.height;
                pictureFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                backdropWindows.Add(new NotebookMapBackdropWindow { window = window, startY = t.startY, height = t.height });
            }
            NotebookMapBackdropLayout backdropLayout = viewport.gameObject.AddComponent<NotebookMapBackdropLayout>();
            backdropLayout.scroll = ui.mapScroll; backdropLayout.windows = backdropWindows.ToArray(); backdropLayout.RefreshLayout();
            // These are supplied transparent PNG illustrations, not runtime-generated drawings.
            // Stage links and clickable nodes are created afterward so they remain in front.
            foreach (NotebookConstellationArtwork art in catalog.artworks ?? Array.Empty<NotebookConstellationArtwork>())
            {
                if (string.IsNullOrEmpty(art.sprite) || art.width <= 0 || art.height <= 0)
                    throw new InvalidOperationException("Invalid constellation artwork: " + art.id);
                Image figure = Picture(content, "Constellation Figure " + art.id, art.sprite);
                At(figure.rectTransform, art.x, art.y, art.width, art.height);
                figure.preserveAspect = true;
                // Picture() fixes White Tint and disables raycasts. Names/meanings stay in the catalog.
                if (!string.IsNullOrEmpty(art.wireSprite))
                {
                    Image wire = Picture(content, "Static Constellation Wire " + art.id, art.wireSprite);
                    At(wire.rectTransform, art.x, art.y, art.width, art.height);
                    wire.preserveAspect = true;
                }
            }
            ui.edges = catalog.edges.Select(e => { Image im = Picture(content, e.from + " to " + e.to, e.sprite + "_Future"); At(im.rectTransform, e.x, e.y, e.width, e.height); return new NotebookEdgeView { from = e.from, to = e.to, image = im, future = Load(e.sprite + "_Future"), cleared = Load(e.sprite + "_Cleared") }; }).ToArray();
            var chapterViews = new List<NotebookChapterView>();
            foreach (NotebookTheme t in catalog.themes)
            {
                Button button = Button(content, "Region " + t.id, "Button", 48, t.startY + 24, 984, 154, "", 1);
                button.image.sprite = Load("Paper/ChapterLabel"); button.transition = Selectable.Transition.None;
                Text(button.transform, "Chapter Number", (t.chapter + 1).ToString("00"), 32, 22, 76, 72, 54);
                Text(button.transform, "Theme Title", t.name, 130, 22, 560, 64, 45);
                int stageCount = catalog.stages.Count(s => s.themeId == t.id);
                TMP_Text progress = Text(button.transform, "Progress", "0 / " + stageCount + " 클리어", 34, 105, 680, 40, 28);
                TMP_Text access = Text(button.transform, "Access", "구매로 해금", 730, 36, 222, 60, 30);
                Image locked = Icon(button.transform, "lock", 684, 42, 37, 37);
                TMP_Text note = Text(content, "Chapter Note " + t.id, t.paid ? "구매 후 이 지역의 스테이지를 이용할 수 있어요" : "클리어한 별이 이어져 별자리를 완성해요", 80, t.startY + t.height - 90, 920, 52, 30, TextAlignmentOptions.Center); note.color = SkyText;
                chapterViews.Add(new NotebookChapterView { themeId = t.id, button = button, progress = progress, access = access, locked = locked, note = note });
            }
            ui.chapters = chapterViews.ToArray();
            var nodeViews = new List<NotebookNodeView>();
            foreach (NotebookStage s in catalog.stages)
            {
                RectTransform box = Rect(content, s.id); At(box, s.x - 80, s.y - 80, 160, 198);
                Image target = box.gameObject.AddComponent<Image>(); target.color = Color.clear; target.raycastTarget = true;
                Image star = Picture(box, "Baked Star State", "Map/Star_Locked"); At(star.rectTransform, 0, 0, 160, 160);
                Button button = box.gameObject.AddComponent<Button>(); button.targetGraphic = star; button.transition = Selectable.Transition.None;
                Image selected = Picture(box, "Selected", "Map/Selection"); At(selected.rectTransform, -20, -20, 200, 200);
                TMP_Text number = Text(box, "Number", s.order.ToString("00"), 0, 52, 160, 56, 39, TextAlignmentOptions.Center);
                Image locked = Icon(box, "lock", 56, 59, 48, 48);
                Image labelPlate = Picture(box, "Static Stage Label Plate", "Map/StageLabel"); At(labelPlate.rectTransform, -10, 168, 180, 44);
                TMP_Text label = Text(box, "Stage Label", s.label, -10, 168, 180, 45, 29, TextAlignmentOptions.Center); label.color = SkyText;
                nodeViews.Add(new NotebookNodeView { stageId = s.id, button = button, star = star, selection = selected, locked = locked, number = number, label = label });
            }
            ui.nodes = nodeViews.ToArray();
            Image header = Picture(host, "Map Header", "Paper/Panel", true); At(header.rectTransform, 0, 0, 1080, 240);
            ui.mapBack = Button(header.transform, "Back", "Utility", 48, 18, 132, 132, "", 1); Icon(ui.mapBack.transform, "back", 33, 33, 66, 66);
            Text(header.transform, "Map Title", "별자리 지도", 206, 52, 510, 80, 55);
            ui.telescopeButton = Button(header.transform, "Telescope", "Button", 742, 24, 300, 112, "", 1); Icon(ui.telescopeButton.transform, "telescope", 30, 23, 66, 66);
            ui.walletText = Text(ui.telescopeButton.transform, "Balance", "10 / 10", 112, 0, 174, 112, 46, TextAlignmentOptions.Center);
            Text(header.transform, "Map Caption", "큰 별을 골라 여정을 이어가세요", 56, 164, 650, 42, 30);
            ui.recoveryText = Text(header.transform, "Recharge", "", 690, 164, 340, 42, 30, TextAlignmentOptions.MidlineRight);
            Image footer = Picture(host, "Selected Stage Footer", "Paper/Panel", true); Bottom(footer.rectTransform, 0, 0, 1080, 324);
            ui.stageRegion = Text(footer.transform, "Theme", "", 650, 35, 376, 45, 30, TextAlignmentOptions.MidlineRight);
            ui.stageName = Text(footer.transform, "Stage", "", 54, 38, 800, 64, 46);
            ui.stageState = Text(footer.transform, "Status", "", 54, 111, 960, 44, 30);
            ui.launchButton = Button(footer.transform, "Launch", "Primary", 48, 172, 984, 128, "스테이지 시작 · 망원경 1개", 49); ui.launchText = LabelOf(ui.launchButton);
        }
        private static void BuildModal(NotebookRoot ui, RectTransform design)
        {
            RectTransform overlay = Screen(design, "Modal"); ui.modal = overlay.gameObject;
            Image dim = Picture(overlay, "Static Dim", "Paper/ModalDim"); Stretch(dim.rectTransform, -3000, -3000, -3000, -3000); dim.raycastTarget = true;
            Image panel = Picture(overlay, "Notebook Modal", "Paper/Panel", true); RectTransform p = panel.rectTransform; p.anchorMin = p.anchorMax = p.pivot = new Vector2(.5f, .5f); p.sizeDelta = new Vector2(984, 1280);
            panel.raycastTarget = true;
            ui.modalClose = Button(p, "Close", "Utility", 828, 24, 132, 132, "", 1); Icon(ui.modalClose.transform, "close", 43, 43, 46, 46);
            ui.modalIcon = Picture(p, "Icon", "Icons/telescope"); At(ui.modalIcon.rectTransform, 392, 86, 200, 180); ui.modalIcon.preserveAspect = true;
            ui.modalTitle = Text(p, "Title", "", 60, 300, 864, 90, 61, TextAlignmentOptions.Center);
            ui.modalCopy = Text(p, "Copy", "", 70, 416, 844, 200, 40, TextAlignmentOptions.Center);
            ui.modalNote = Text(p, "Note", "", 70, 630, 844, 126, 32, TextAlignmentOptions.Center);
            ui.modalPrimary = Button(p, "Primary", "Primary", 70, 794, 844, 130, "확인", 42); ui.modalPrimaryText = LabelOf(ui.modalPrimary);
            ui.modalSecondary = Button(p, "Secondary", "Button", 70, 946, 844, 130, "", 42); ui.modalSecondaryText = LabelOf(ui.modalSecondary);
            ui.modalStatus = Text(p, "Status", "", 70, 1100, 844, 128, 31, TextAlignmentOptions.Center);
        }
        private static void ImportArt()
        {
            foreach (string path in Directory.GetFiles(MainTex, "*.png", SearchOption.AllDirectories)) ImportSprite(path.Replace('\\', '/'));
            foreach (string path in Directory.GetFiles(MapTex, "*.png", SearchOption.AllDirectories)) ImportSprite(path.Replace('\\', '/'));
            foreach (string path in Directory.GetFiles(SettingsTex, "*.png", SearchOption.AllDirectories)) ImportSprite(path.Replace('\\', '/'));
            string patternPath = "Assets/ANIMOL/Art/UI/Main/Pattern/Textures/CharacterFacesStars_All_2048.png";
            var importer = AssetImporter.GetAtPath(patternPath) as TextureImporter;
            if (importer) { importer.textureType = TextureImporterType.Default; importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Bilinear; importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.SaveAndReimport(); }
        }
        private static void ImportSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; if (!importer) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096; importer.spritePixelsPerUnit = 100;
            bool sliced = path.Contains("/Controls/Button_") || path.Contains("/Controls/Primary_") || path.Contains("/Controls/Utility_") || path.Contains("/Paper/Panel.") || path.Contains("/Paper/SettingsPanel.") || path.Contains("/Paper/ChapterLabel.");
            importer.spriteBorder = sliced ? Vector4.one * 36 : Vector4.zero; importer.SaveAndReimport();
        }
        private static TMP_FontAsset EnsureFont()
        {
            string destination = GeneratedFont; TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destination); if (existing) return existing;
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (!shader) throw new InvalidOperationException("Import TMP Essential Resources, then run Build Fresh Scene again.");
            Font source = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/Notebook/Galmuri14.ttf");
            TMP_FontAsset result = TMP_FontAsset.CreateFontAsset(source, 42, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (!result) throw new InvalidOperationException("Galmuri14 TMP font could not be created.");
            result.name = "Galmuri14"; result.material.shader = shader; AssetDatabase.CreateAsset(result, destination);
            AssetDatabase.AddObjectToAsset(result.material, result);
            foreach (Texture2D atlas in result.atlasTextures) if (atlas) AssetDatabase.AddObjectToAsset(atlas, result);
            EditorUtility.SetDirty(result); return result;
        }
        private static void EnsureFolder(string path) { Directory.CreateDirectory(path); AssetDatabase.Refresh(); }
        private static Sprite Load(string relative)
        {
            string textureRoot = (relative.StartsWith("Map/", StringComparison.Ordinal) || relative.StartsWith("Constellations/", StringComparison.Ordinal) || relative.StartsWith("ConstellationLines/", StringComparison.Ordinal)) ? MapTex
                : relative.StartsWith("Controls/Slider", StringComparison.Ordinal) || relative.StartsWith("Controls/Toggle", StringComparison.Ordinal) || relative == "Paper/SettingsPanel" ? SettingsTex
                : MainTex;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(textureRoot + relative + ".png");
            if (!sprite) throw new InvalidOperationException("Missing static UI sprite: " + relative); return sprite;
        }
        private static RectTransform Rect(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return obj.GetComponent<RectTransform>();
        }
        private static RectTransform Screen(Transform parent, string name) { RectTransform r = Rect(parent, name); Stretch(r, 0, 0, 0, 0); return r; }
        private static void At(RectTransform r, float x, float y, float width, float height) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); }
        private static void Bottom(RectTransform r, float x, float y, float width, float height) { r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(width, height); }
        private static void Stretch(RectTransform r, float left, float top, float right, float bottom) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f); r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); }
        private static Image Picture(Transform parent, string name, string relative, bool sliced = false)
        {
            RectTransform r = Rect(parent, name); Image image = r.gameObject.AddComponent<Image>(); image.sprite = Load(relative); image.color = Color.white; image.raycastTarget = false; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; image.pixelsPerUnitMultiplier = 1; return image;
        }
        private static Image Icon(Transform parent, string relative, float x, float y, float width, float height) { Image image = Picture(parent, relative + " Icon", "Icons/" + relative); At(image.rectTransform, x, y, width, height); image.preserveAspect = true; return image; }
        private static TMP_Text Text(Transform parent, string name, string text, float x, float y, float width, float height, float size, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            RectTransform r = Rect(parent, name); At(r, x, y, width, height); TextMeshProUGUI label = r.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = size; label.fontStyle = FontStyles.Normal; label.text = text; label.color = Ink; label.alignment = align; label.raycastTarget = false; label.enableAutoSizing = false; label.enableWordWrapping = true; return label;
        }
        private static Button Button(Transform parent, string name, string role, float x, float y, float width, float height, string text, float fontSize)
        {
            Image image = Picture(parent, name, "Controls/" + role + "_Normal", true); At(image.rectTransform, x, y, width, height); image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = Load("Controls/" + role + "_Highlighted"), pressedSprite = Load("Controls/" + role + "_Pressed"), selectedSprite = Load("Controls/" + role + "_Highlighted"), disabledSprite = Load("Controls/" + role + "_Disabled") };
            TMP_Text label = Text(image.transform, "Label", text, 20, 0, width - 40, height, fontSize, TextAlignmentOptions.Center); return button;
        }
        private static TMP_Text LabelOf(Button button) => button.GetComponentInChildren<TMP_Text>(true);
        private static void MoveText(Button button, float x, float y, float width, float height, TextAlignmentOptions alignment = TextAlignmentOptions.Center) { TMP_Text label = LabelOf(button); At(label.rectTransform, x, y, width, height); label.alignment = alignment; }
    }
}
#endif
