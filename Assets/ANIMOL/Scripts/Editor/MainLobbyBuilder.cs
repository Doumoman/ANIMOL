using System;
using System.Collections.Generic;
using System.IO;
using ANIMOL.Menu;
using ANIMOL.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Menu.Editor
{
    public static class MainLobbyBuilder
    {
        const string Art = "Assets/ANIMOL/Sprites/UI/Main";
        const string CatalogPath = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string FontPath = "Assets/ANIMOL/Fonts/ANIMOLMenuKorean.ttf";
        const string RevealMaterialPath = "Assets/ANIMOL/Materials/MainLobbyDiagonalReveal.mat";
        static readonly Color Ink = new Color(.094f, .094f, .18f);
        static readonly Color Paper = new Color(1, .976f, .914f);
        static readonly Color Pink = new Color(1, .514f, .733f);
        static readonly Color Yellow = new Color(1, .906f, .502f);
        static readonly Color Muted = new Color(.914f, .89f, .847f);
        static Font font;

        static readonly string[] ThemeIds = { "starway", "theater", "observatory", "sea" };
        static readonly string[] ThemeNames = { "별길 목장", "잔영 극장", "낙일 성소", "별너울 해안" };
        static readonly string[] CatalogPaths = {
            "Assets/ANIMOL/Data/Art/StarwayCharacters.asset",
            "Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset",
            "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset",
            "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset"
        };
        static readonly string[][] CharacterIds = {
            new[] { "choa", "uram", "yeoul", "tori" },
            new[] { "seri", "roan", "mion", "piko" },
            new[] { "sein", "daon", "lumi", "soyul", "doyun" },
            new[] { "haerin", "stranger", "danu", "eden" }
        };

        [MenuItem("ANIMOL/UI/Build Main Lobby")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!font) throw new FileNotFoundException("Missing v7 Korean UI font", FontPath);
            var catalog = BuildCatalog();
            BuildScene(catalog, CreateRevealMaterial());
            Debug.Log("ANIMOL main UI: built active lobby with 4 scenery sprites, 16 people, 17 appearances, and fail-closed battle gate.");
        }

        public static void BuildBatch() => Build();

        [MenuItem("ANIMOL/UI/Upgrade Main Lobby Diagonal Wipe")]
        public static void UpgradeDiagonalWipe()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!controller || !controller.canvas || !controller.backgroundTransitionImage || !controller.heroTransitionImage)
                throw new InvalidOperationException("Existing main lobby scene is missing transition objects.");
            var reveal = CreateRevealMaterial();
            controller.backgroundTransitionImage.material = reveal;
            controller.backgroundTransitionImage.color = Color.white;
            controller.backgroundTransitionImage.gameObject.name = "Scenery Diagonal Reveal";
            controller.heroTransitionImage.material = reveal;
            controller.heroTransitionImage.color = Color.white;
            controller.heroTransitionImage.gameObject.name = "Character Diagonal Reveal";
            var root = (RectTransform)controller.canvas.transform;
            var line = root.Find("Full Screen Diagonal Seam") as RectTransform;
            if (!line)
            {
                line = Rect("Full Screen Diagonal Seam", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f), Vector2.zero, new Vector2(0, 2.5f));
                AddImage(line, new Color(1f, .93f, .78f, .95f));
            }
            line.gameObject.SetActive(false);
            controller.diagonalRevealMaterial = reveal;
            controller.diagonalLineRect = line;
            controller.diagonalLineImage = line.GetComponent<Image>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Material CreateRevealMaterial()
        {
            var shader = Shader.Find("ANIMOL/UI/Main Lobby Diagonal Reveal");
            if (!shader) throw new InvalidOperationException("Missing main lobby diagonal reveal shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(RevealMaterialPath);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, RevealMaterialPath);
            }
            else material.shader = shader;
            material.SetFloat("_Progress", 0);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigureTextures()
        {
            foreach (var theme in ThemeIds) Configure(Art + "/Backgrounds/" + theme + ".png", false);
        }

        static void Configure(string path, bool illustration)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing main UI PNG", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = illustration;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var expected = illustration ? new Vector2(1024, 1536) : new Vector2(1122, 1402);
            if (!sprite || sprite.rect.size != expected)
                throw new InvalidDataException("Main UI texture lost original dimensions: " + path);
        }

        static MainMenuCatalog BuildCatalog()
        {
            Directory.CreateDirectory("Assets/ANIMOL/Data/UI");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<MainMenuCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.themes = new MainMenuTheme[4];
            for (var t = 0; t < 4; t++)
            {
                var game = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPaths[t]);
                if (!game || game.characters.Length != CharacterIds[t].Length) throw new InvalidDataException("Invalid active game character catalog: " + CatalogPaths[t]);
                var theme = new MainMenuTheme {
                    id = ThemeIds[t], displayName = ThemeNames[t], gameCharacters = game,
                    resourceId = t == 1 ? "afterimage_theater" : t == 3 ? "starwave_coast" : null,
                    resourceAliases = t == 1 ? new[] { "memory_theater" } :
                        t == 3 ? new[] { "twilight_sea" } : Array.Empty<string>(),
                    scenery = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Backgrounds/" + ThemeIds[t] + ".png"),
                    people = new MainMenuPerson[CharacterIds[t].Length]
                };
                for (var i = 0; i < CharacterIds[t].Length; i++)
                {
                    var id = CharacterIds[t][i];
                    if (game.Find(id) == null) throw new InvalidDataException("Menu/game ID mismatch: " + id);
                    var person = new MainMenuPerson { characterId = id };
                    var twoCostumes = id == "eden";
                    person.appearances = new MainMenuAppearance[twoCostumes ? 2 : 1];
                    person.appearances[0] = t == 0 || t == 1 || t == 2 || t == 3
                        ? new MainMenuAppearance { artId = id, costumeId = twoCostumes ? "rabbit" : "default",
                            illustration = game.Find(id).illustration }
                        : Appearance(id, twoCostumes ? "rabbit" : "default");
                    if (twoCostumes)
                    {
                        if (!game.Find(id).HasCostume("puppy")) throw new InvalidDataException("Eden puppy costume is missing in game catalog.");
                        person.appearances[1] = new MainMenuAppearance { artId = "edenpuppy", costumeId = "puppy",
                            illustration = game.Find("eden").IllustrationFor("puppy") };
                    }
                    theme.people[i] = person;
                }
                if (!theme.scenery) throw new FileNotFoundException("Missing main scenery", theme.id);
                catalog.themes[t] = theme;
            }
            // Preserve requiredStageIds if an authoritative policy is configured later. An empty
            // list is the current project's actual state and deliberately locks battle.
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static MainMenuAppearance Appearance(string artId, string costume)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Illustrations/" + artId + ".png");
            if (!sprite) throw new FileNotFoundException("Missing main illustration", artId);
            return new MainMenuAppearance { artId = artId, costumeId = costume, illustration = sprite };
        }

        static void BuildScene(MainMenuCatalog catalog, Material revealMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.925f, .91f, .89f);
            var canvasObject = new GameObject("Main UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(414, 736);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            var safe = Rect("Safe Area", canvas.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            var frame = Rect("Portrait Frame", safe, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(414, 736));
            AddImage(frame, Ink);
            frame.gameObject.AddComponent<RectMask2D>();
            var art = Rect("Art Region", frame, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            art.offsetMin = new Vector2(0, 210);
            art.offsetMax = Vector2.zero;
            AddImage(art, Ink);
            var scenery = Fill("Scenery Mask", art);
            scenery.gameObject.AddComponent<RectMask2D>();
            var background = Rect("Integrated Scenery", scenery, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(1122, 1402));
            var backgroundImage = AddImage(background, Color.white);
            backgroundImage.sprite = catalog.themes[0].scenery;
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = false;
            var backgroundTransition = Rect("Scenery Diagonal Reveal", scenery, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(1122, 1402));
            var backgroundTransitionImage = AddImage(backgroundTransition, Color.white);
            backgroundTransitionImage.material = revealMaterial;
            backgroundTransitionImage.preserveAspect = true;
            backgroundTransition.gameObject.SetActive(false);
            var hero = Rect("Static Character Illustration", art, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(210, 315));
            var heroImage = AddImage(hero, Color.white);
            heroImage.sprite = catalog.themes[0].people[0].appearances[0].illustration;
            heroImage.preserveAspect = true;
            heroImage.raycastTarget = false;
            var heroTransition = Rect("Character Diagonal Reveal", art, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(210, 315));
            var heroTransitionImage = AddImage(heroTransition, Color.white);
            heroTransitionImage.material = revealMaterial;
            heroTransitionImage.preserveAspect = true;
            heroTransition.gameObject.SetActive(false);

            var top = Rect("Top Bar", art, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1),
                new Vector2(0, -15), new Vector2(0, 44));
            var profile = Rect("Guest", top, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(15, 0), new Vector2(95, 40));
            AddImage(profile, Paper);
            Outline(profile, Ink);
            Label("Guest Label", profile, "G  게스트", 15, Ink, TextAnchor.MiddleCenter);
            var settings = Button("Settings", top, "설정", new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(1, .5f), new Vector2(-15, 0), new Vector2(62, 40), Paper, 15);
            var logo = Rect("Logo Group", art, new Vector2(.5f, 1), new Vector2(.5f, 1),
                new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(320, 96));
            var eyebrow = Rect("Logo Eyebrow", logo, new Vector2(.5f, 1), new Vector2(.5f, 1),
                new Vector2(.5f, 1), Vector2.zero, new Vector2(195, 21));
            AddImage(eyebrow, Ink);
            Label("Eyebrow", eyebrow, "SWIPE PUZZLE BATTLE", 12, Paper, TextAnchor.MiddleCenter);
            var wordmark = Rect("Wordmark", logo, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(.5f, 0), Vector2.zero, new Vector2(315, 72));
            var brand = Label("ANIMOL", wordmark, "ANIMOL!", 60, Paper, TextAnchor.MiddleCenter);
            // This font's tall line metric otherwise culls the whole wordmark.
            brand.verticalOverflow = VerticalWrapMode.Overflow;
            var brandOutline = brand.gameObject.AddComponent<Outline>();
            brandOutline.effectColor = Ink;
            brandOutline.effectDistance = new Vector2(2, -2);

            var dock = Rect("Menu Dock", frame, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 210));
            AddImage(dock, Paper);
            var modes = new Button[3];
            var modeLabels = new[] { "캠페인", "대전", "커스텀" };
            for (var i = 0; i < 3; i++)
                modes[i] = Button("Mode " + modeLabels[i], dock, modeLabels[i], new Vector2(i / 3f, 1),
                    new Vector2((i + 1) / 3f, 1), new Vector2(.5f, 1), new Vector2(0, -14),
                    new Vector2(-11, 55), Paper, 17);
            var play = Button("Play", dock, "", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(.5f, 1), new Vector2(0, -84), new Vector2(-30, 66), Yellow, 22);
            var playTitle = Label("Play Title", play.transform, "캠페인 시작", 24, Ink, TextAnchor.MiddleLeft);
            playTitle.rectTransform.offsetMin = new Vector2(16, 22);
            playTitle.rectTransform.offsetMax = new Vector2(-20, -5);
            var playNote = Label("Play Note", play.transform, "테마 선택으로", 13, Ink, TextAnchor.MiddleLeft);
            playNote.rectTransform.offsetMin = new Vector2(16, 5);
            playNote.rectTransform.offsetMax = new Vector2(-20, -37);
            var ad = Rect("Ad Row", dock, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 49));
            AddImage(ad, Muted);
            var adLabel = Label("Ad Slot", ad, "AD    광고 영역", 12, Ink, TextAnchor.MiddleLeft);
            adLabel.rectTransform.offsetMin = new Vector2(16, 0);
            adLabel.rectTransform.offsetMax = new Vector2(-105, 0);
            var removeAds = Button("Remove Ads", ad, "광고 제거", new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(1, .5f), new Vector2(-12, 0), new Vector2(90, 42), Muted, 12);

            var campaign = Fill("Campaign Theme Selection", frame);
            AddImage(campaign, Paper);
            var campaignBack = Button("Campaign Back", campaign, "← 메인", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(14, -13), new Vector2(88, 44), Paper, 14);
            var campaignTitle = Rect("Campaign Title", campaign, new Vector2(.5f, 1), new Vector2(.5f, 1),
                new Vector2(.5f, 1), new Vector2(0, -15), new Vector2(180, 42));
            Label("Title", campaignTitle, "테마 선택", 22, Ink, TextAnchor.MiddleCenter);
            var grid = Fill("Four Theme Cards", campaign);
            grid.offsetMin = new Vector2(14, 88);
            grid.offsetMax = new Vector2(-14, -77);
            var themeButtons = new Button[4];
            var themeCards = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var col = i % 2;
                var row = i / 2;
                var card = Rect("Theme " + ThemeIds[i], grid,
                    new Vector2(col * .5f, (1 - row) * .5f), new Vector2((col + 1) * .5f, (2 - row) * .5f),
                    new Vector2(.5f, .5f), Vector2.zero, new Vector2(-12, -12));
                var cardBackground = AddImage(card, Ink);
                cardBackground.raycastTarget = true;
                Outline(card, Ink);
                card.gameObject.AddComponent<RectMask2D>();
                var cardArt = Rect("Card Scenery", card, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f), Vector2.zero, new Vector2(1122, 1402));
                themeCards[i] = AddImage(cardArt, Color.white);
                themeCards[i].sprite = catalog.themes[i].scenery;
                themeCards[i].raycastTarget = false;
                themeButtons[i] = card.gameObject.AddComponent<Button>();
                themeButtons[i].targetGraphic = cardBackground;
                var caption = Rect("Caption", card, new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 47));
                AddImage(caption, new Color(.094f, .094f, .18f, .86f));
                Label("Theme Name", caption, ThemeNames[i], 16, Paper, TextAnchor.MiddleCenter);
            }
            var campaignNext = Button("Campaign Stage Route", campaign, "스테이지 선택", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(-28, 60), Yellow, 21);
            var selectedTheme = Label("Selected Theme", campaignNext.transform, ThemeNames[0], 12, Ink, TextAnchor.LowerRight);
            selectedTheme.rectTransform.offsetMin = new Vector2(0, 3);
            selectedTheme.rectTransform.offsetMax = new Vector2(-12, -27);
            campaign.gameObject.SetActive(false);

            var modal = Fill("Modal Overlay", frame);
            AddImage(modal, new Color(.094f, .094f, .18f, .72f));
            var sheet = Rect("Modal Sheet", modal, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(365, 240));
            AddImage(sheet, Paper);
            Outline(sheet, Ink);
            var modalTitle = Label("Modal Title", sheet, "설정", 26, Ink, TextAnchor.MiddleLeft);
            modalTitle.rectTransform.offsetMin = new Vector2(18, 170);
            modalTitle.rectTransform.offsetMax = new Vector2(-78, -14);
            var modalClose = Button("Modal Close", sheet, "닫기", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(1, 1), new Vector2(-12, -10), new Vector2(56, 44), Paper, 14);
            var modalBody = Label("Modal Body", sheet, "", 15, Ink, TextAnchor.UpperLeft);
            modalBody.rectTransform.offsetMin = new Vector2(18, 65);
            modalBody.rectTransform.offsetMax = new Vector2(-18, -74);
            var settingsControls = Rect("Motion Setting", sheet, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(.5f, 0), new Vector2(0, 15), new Vector2(-34, 43));
            Label("Reduce Motion", settingsControls, "모션 줄이기", 15, Ink, TextAnchor.MiddleLeft).rectTransform.offsetMax = new Vector2(-50, 0);
            var toggleRoot = Rect("Reduce Motion Toggle", settingsControls, new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(1, .5f), new Vector2(-4, 0), new Vector2(35, 35));
            var toggleBg = AddImage(toggleRoot, Paper);
            Outline(toggleRoot, Ink);
            var check = Rect("Checkmark", toggleRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(22, 22));
            var checkImage = AddImage(check, Pink);
            var toggle = toggleRoot.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.graphic = checkImage;
            settingsControls.gameObject.SetActive(false);
            modal.gameObject.SetActive(false);

            // The seam spans the physical viewport; scenery and character use the same screen-space cut.
            var diagonalLine = Rect("Full Screen Diagonal Seam", canvas.transform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(0, 2.5f));
            var diagonalLineImage = AddImage(diagonalLine, new Color(1f, .93f, .78f, .95f));
            diagonalLine.gameObject.SetActive(false);

            new GameObject("UI Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            var controller = new GameObject("Main Lobby Controller", typeof(MainLobbyController)).GetComponent<MainLobbyController>();
            controller.catalog = catalog;
            controller.canvas = canvas;
            controller.safeRoot = safe;
            controller.frame = frame;
            controller.artArea = art;
            controller.scenery = scenery;
            controller.backgroundRect = background;
            controller.backgroundImage = backgroundImage;
            controller.backgroundTransitionRect = backgroundTransition;
            controller.backgroundTransitionImage = backgroundTransitionImage;
            controller.heroRect = hero;
            controller.heroImage = heroImage;
            controller.heroTransitionRect = heroTransition;
            controller.heroTransitionImage = heroTransitionImage;
            controller.diagonalRevealMaterial = revealMaterial;
            controller.diagonalLineRect = diagonalLine;
            controller.diagonalLineImage = diagonalLineImage;
            controller.logoRect = logo;
            controller.dock = dock;
            controller.campaignPanel = campaign.gameObject;
            controller.modalPanel = modal.gameObject;
            controller.settingsControls = settingsControls.gameObject;
            controller.modalTitle = modalTitle;
            controller.modalBody = modalBody;
            controller.playTitle = playTitle;
            controller.playNote = playNote;
            controller.battleNote = Label("Battle Note", modes[1].transform, "클리어 후 해금", 10, Ink, TextAnchor.LowerCenter);
            modes[1].GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(0, 14);
            controller.battleNote.alignment = TextAnchor.MiddleCenter;
            controller.battleNote.fontSize = 9;
            controller.battleNote.rectTransform.offsetMin = new Vector2(0, 2);
            controller.battleNote.rectTransform.offsetMax = new Vector2(0, -33);
            controller.campaignSelection = selectedTheme;
            controller.reducedMotionToggle = toggle;
            controller.campaignButton = modes[0];
            controller.battleButton = modes[1];
            controller.customButton = modes[2];
            controller.playButton = play;
            controller.settingsButton = settings;
            controller.removeAdsButton = removeAds;
            controller.campaignBackButton = campaignBack;
            controller.campaignContinueButton = campaignNext;
            controller.modalCloseButton = modalClose;
            controller.themeButtons = themeButtons;
            controller.themeCards = themeCards;
            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) buildScenes.Add(existing);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        static RectTransform Fill(string name, Transform parent)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Outline(RectTransform rect, Color color)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2, -2);
        }

        static Text Label(string name, Transform parent, string value, int size, Color color, TextAnchor alignment)
        {
            var rect = Fill(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.text = value;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return label;
        }

        static Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size, Color color, int fontSize)
        {
            var rect = Rect(name, parent, min, max, pivot, position, size);
            var image = AddImage(rect, color);
            image.raycastTarget = true;
            Outline(rect, Ink);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(value)) Label("Label", rect, value, fontSize, Ink, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
