using System;
using System.IO;
using System.Security.Cryptography;
using ANIMOL.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Menu.Editor
{
    // Applies the independent menu skin to the existing lobby. It never rebuilds the catalog.
    public static class MainMenuSkinInstaller
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string SpriteRoot = "Assets/ANIMOL/Sprites/UI/Main";
        const string ManifestPath = "Assets/ANIMOL/Data/UI/MainMenuSpriteManifest.json";
        const string SkinPath = "Assets/ANIMOL/Data/UI/MainMenuSkin.asset";
        const string FontPath = "Assets/ANIMOL/Fonts/ANIMOLPixel14_UI.asset";
        static readonly Color Ink = new Color(7f / 255, 28f / 255, 57f / 255);
        static readonly Color Cream = new Color(1f, 242f / 255, 211f / 255);
        static readonly string[] ThemeDescriptions = {
            "별과 구름 사이의 하늘섬 목장", "커튼 너머 펼쳐지는 공중 무대",
            "빛과 그림자가 머무는 성소", "노을과 청록빛 물결이 만나는 곳"
        };

        [Serializable] sealed class SpriteManifest { public SpriteSpec[] sprites; }
        [Serializable] sealed class SpriteSpec
        {
            public string png;
            public string sha256;
            public int[] pixels;
            public int[] border;
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before changing the main UI.");
            ConfigureSprites();
            var skin = CreateSkin();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!controller || !controller.canvas || !controller.catalog)
                throw new InvalidDataException("The active main scene is missing its lobby controller.");
            ApplyScene(controller, skin);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu skin applied to existing lobby scene; catalog and scenery retained.");
        }

        static void ConfigureSprites()
        {
            var manifest = JsonUtility.FromJson<SpriteManifest>(File.ReadAllText(ManifestPath));
            if (manifest?.sprites == null || manifest.sprites.Length != 21)
                throw new InvalidDataException("Expected 21 independent menu sprites.");
            foreach (var spec in manifest.sprites)
            {
                var path = SpriteRoot + "/Chrome/" + Path.GetFileName(spec.png);
                var bytes = File.ReadAllBytes(path);
                using (var sha = SHA256.Create())
                {
                    var actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    if (actual != spec.sha256) throw new InvalidDataException("UI sprite checksum mismatch: " + path);
                }
                Configure(path, spec.pixels[0], spec.pixels[1], spec.border);
            }
            Configure(SpriteRoot + "/Branding/william-paradox-title.png", 2060, 763, null);
            Configure(SpriteRoot + "/Branding/title-blank.png", 2060, 763, null);
        }

        static void Configure(string path, int width, int height, int[] border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing UI PNG importer", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(width, height));
            importer.spriteBorder = border == null ? Vector4.zero :
                new Vector4(border[0], border[1], border[2], border[3]);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = importer.maxTextureSize;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || sprite.rect.width != width || sprite.rect.height != height)
                throw new InvalidDataException("UI sprite dimensions changed: " + path);
        }

        static MainMenuSkin CreateSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<MainMenuSkin>(SkinPath);
            if (!skin)
            {
                skin = ScriptableObject.CreateInstance<MainMenuSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (!skin.font) throw new FileNotFoundException("Missing existing Korean TMP font", FontPath);
            skin.tab = Chrome("tab");
            skin.selectedTab = Chrome("tab-selected");
            skin.disabledTab = Chrome("tab-disabled");
            skin.primary = Chrome("primary");
            skin.profile = Chrome("profile");
            skin.utility = Chrome("utility");
            skin.dialog = Chrome("dialog");
            skin.card = Chrome("card");
            skin.ad = Chrome("ad");
            skin.title = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "/Branding/william-paradox-title.png");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        static Sprite Chrome(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "/Chrome/" + name + ".png");
            if (!sprite) throw new FileNotFoundException("Missing menu sprite", name);
            return sprite;
        }

        static RectTransform Child(Transform parent, string name) => parent.Find(name) as RectTransform;

        static void ApplyScene(MainLobbyController c, MainMenuSkin skin)
        {
            c.skin = skin;
            var frame = c.frame.GetComponent<Image>();
            frame.color = Ink;
            var dockImage = c.dock.GetComponent<Image>();
            dockImage.color = Cream;
            var top = Child(c.artArea, "Top Bar");
            var guest = Child(top, "Guest");
            SetSurface(guest.GetComponent<Image>(), skin.profile);
            guest.sizeDelta = new Vector2(115, 48);
            guest.anchoredPosition = new Vector2(12, 0);
            AddIcon(guest, "Profile Icon", Chrome("profile-icon"), new Vector2(12, 0), new Vector2(23, 23), new Vector2(0, .5f));
            var guestLabel = Child(guest, "Guest Label");
            guestLabel.offsetMin = new Vector2(38, 0);
            guestLabel.offsetMax = new Vector2(-16, 0);
            guestLabel.GetComponent<Text>().text = "게스트";
            guestLabel.GetComponent<Text>().fontSize = 13;

            SetButton(c.settingsButton, skin.utility);
            var settingsRect = (RectTransform)c.settingsButton.transform;
            settingsRect.sizeDelta = new Vector2(48, 48);
            settingsRect.anchoredPosition = new Vector2(-12, 0);
            Child(settingsRect, "Label").gameObject.SetActive(false);
            AddIcon(settingsRect, "Settings Icon", Chrome("settings"), Vector2.zero, new Vector2(26, 26));
            var soundRect = Child(top, "Sound");
            if (!soundRect)
            {
                soundRect = NewRect("Sound", top, new Vector2(1, .5f), new Vector2(1, .5f),
                    new Vector2(1, .5f), new Vector2(-65, 0), new Vector2(48, 48));
                soundRect.gameObject.AddComponent<Image>();
                c.soundButton = soundRect.gameObject.AddComponent<Button>();
            }
            if (!c.soundButton) c.soundButton = soundRect.GetComponent<Button>();
            SetButton(c.soundButton, skin.utility);
            AddIcon(soundRect, "Sound Icon", Chrome("sound"), Vector2.zero, new Vector2(26, 26));

            c.logoRect.sizeDelta = new Vector2(330, 126);
            c.logoRect.anchoredPosition = new Vector2(0, -66);
            Child(c.logoRect, "Logo Eyebrow").gameObject.SetActive(false);
            Child(c.logoRect, "Wordmark").gameObject.SetActive(false);
            AddIcon(c.logoRect, "William Paradox Title", skin.title, Vector2.zero,
                new Vector2(330, 122), new Vector2(.5f, .5f), false);

            SetButton(c.campaignButton, skin.tab);
            SetButton(c.battleButton, skin.disabledTab);
            SetButton(c.customButton, skin.tab);
            var modeButtons = new[] { c.campaignButton, c.battleButton, c.customButton };
            var modeIcons = new[] { "campaign", "battle", "custom" };
            for (var i = 0; i < modeButtons.Length; i++)
            {
                var rect = (RectTransform)modeButtons[i].transform;
                rect.sizeDelta = new Vector2(-11, 44);
                rect.anchoredPosition = new Vector2(0, -4);
                var label = Child(rect, "Label");
                label.offsetMin = new Vector2(21, i == 1 ? 15 : 0);
                label.offsetMax = new Vector2(-1, 0);
                label.GetComponent<Text>().fontSize = 13;
                AddIcon(rect, "Mode Icon", Chrome(modeIcons[i]), new Vector2(4, 0),
                    new Vector2(20, 20), new Vector2(0, .5f));
            }
            c.battleNote.rectTransform.offsetMin = new Vector2(0, 2);
            c.battleNote.rectTransform.offsetMax = new Vector2(0, -26);
            c.battleNote.fontSize = 8;
            c.battleLockImage = AddIcon((RectTransform)c.battleButton.transform, "Battle Lock",
                Chrome("lock"), new Vector2(-7, -5), new Vector2(12, 14), new Vector2(1, 1));
            SetButton(c.playButton, skin.primary);
            var playRect = (RectTransform)c.playButton.transform;
            playRect.sizeDelta = new Vector2(-11, 55);
            playRect.anchoredPosition = new Vector2(0, -48);
            c.playTitle.rectTransform.offsetMin = new Vector2(18, 0);
            c.playTitle.rectTransform.offsetMax = new Vector2(-18, 0);
            c.playTitle.alignment = TextAnchor.MiddleCenter;
            c.playTitle.fontSize = 22;
            c.playNote.gameObject.SetActive(false);
            AddIcon((RectTransform)c.playButton.transform, "Play Arrow", Chrome("arrow-right"),
                new Vector2(-18, 0), new Vector2(28, 26), new Vector2(1, .5f));
            var ad = Child(c.dock, "Ad Row");
            if (ad)
            {
                ad.sizeDelta = new Vector2(0, 44);
                SetSurface(ad.GetComponent<Image>(), skin.ad);
            }
            SetButton(c.removeAdsButton, skin.tab);
            ((RectTransform)c.removeAdsButton.transform).sizeDelta = new Vector2(92, 44);
            Child(c.removeAdsButton.transform, "Label").GetComponent<Text>().fontSize = 11;

            c.campaignPanel.GetComponent<Image>().color = Cream;
            SetButton(c.campaignBackButton, skin.utility);
            var back = (RectTransform)c.campaignBackButton.transform;
            back.sizeDelta = new Vector2(48, 48);
            Child(back, "Label").gameObject.SetActive(false);
            AddIcon(back, "Back Arrow", Chrome("arrow-left"), Vector2.zero, new Vector2(26, 26));
            c.themeSurfaces = new Image[c.themeButtons.Length];
            c.themeChecks = new Image[c.themeButtons.Length];
            for (var i = 0; i < c.themeButtons.Length; i++)
            {
                var card = (RectTransform)c.themeButtons[i].transform;
                c.themeSurfaces[i] = card.GetComponent<Image>();
                SetSurface(c.themeSurfaces[i], skin.card);
                SetButton(c.themeButtons[i], skin.card);
                var art = c.themeCards[i].rectTransform;
                var window = Child(card, "Scenery Window");
                if (!window)
                {
                    window = NewRect("Scenery Window", card, Vector2.zero, Vector2.one,
                        new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                    window.offsetMin = new Vector2(12, 86);
                    window.offsetMax = new Vector2(-12, -12);
                    window.gameObject.AddComponent<RectMask2D>();
                    art.SetParent(window, false);
                }
                window.SetSiblingIndex(0);
                var caption = Child(card, "Caption");
                caption.sizeDelta = new Vector2(0, 76);
                caption.GetComponent<Image>().color = Color.clear;
                var name = Child(caption, "Theme Name");
                name.offsetMin = new Vector2(8, 35);
                name.offsetMax = new Vector2(-8, -6);
                name.GetComponent<Text>().color = Ink;
                var detail = Child(caption, "Theme Description");
                if (!detail)
                    detail = NewRect("Theme Description", caption, new Vector2(0, 0),
                        new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 5), new Vector2(-16, 30));
                var detailText = detail.GetComponent<TextMeshProUGUI>();
                if (!detailText) detailText = detail.gameObject.AddComponent<TextMeshProUGUI>();
                detailText.font = skin.font;
                detailText.fontSize = 10;
                detailText.color = Ink;
                detailText.alignment = TextAlignmentOptions.Center;
                detailText.text = ThemeDescriptions[i];
                detailText.raycastTarget = false;
                c.themeChecks[i] = AddIcon(card, "Selected Check", Chrome("check"),
                    new Vector2(-16, -17), new Vector2(24, 24), new Vector2(1, 1));
                c.themeChecks[i].gameObject.SetActive(i == 0);
            }
            SetButton(c.campaignContinueButton, skin.primary);
            ((RectTransform)c.campaignContinueButton.transform).sizeDelta = new Vector2(-50, 52);
            c.campaignSelection.rectTransform.offsetMin = new Vector2(0, 10);
            c.campaignSelection.rectTransform.offsetMax = new Vector2(-12, -27);
            c.campaignSelection.fontSize = 10;

            var sheet = Child(c.modalPanel.transform, "Modal Sheet");
            SetSurface(sheet.GetComponent<Image>(), skin.dialog);
            SetButton(c.modalCloseButton, skin.utility);
            var close = (RectTransform)c.modalCloseButton.transform;
            close.sizeDelta = new Vector2(48, 48);
            Child(close, "Label").gameObject.SetActive(false);
            AddIcon(close, "Close Icon", Chrome("close"), Vector2.zero, new Vector2(24, 24));
            var toggleRoot = Child(c.settingsControls.transform, "Reduce Motion Toggle");
            toggleRoot.sizeDelta = new Vector2(44, 44);
            SetSurface(toggleRoot.GetComponent<Image>(), skin.tab);
            var checkmark = Child(toggleRoot, "Checkmark").GetComponent<Image>();
            checkmark.sprite = Chrome("check");
            checkmark.type = Image.Type.Simple;
            checkmark.preserveAspect = true;
            checkmark.color = Ink;
            checkmark.raycastTarget = false;

            foreach (var oldText in c.canvas.GetComponentsInChildren<Text>(true)) ConvertText(oldText, skin.font);
            c.ForceLayout();
            EditorUtility.SetDirty(c);
        }

        static void SetSurface(Image image, Sprite sprite)
        {
            if (!image || !sprite) throw new InvalidDataException("UI surface is missing.");
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 5f;
            image.color = Color.white;
            var outline = image.GetComponent<Outline>();
            if (outline) outline.enabled = false;
        }

        static void SetButton(Button button, Sprite sprite)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(button.gameObject);
            var image = button.GetComponent<Image>();
            SetSurface(image, sprite);
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (!button.GetComponent<MainMenuPressFeedback>()) button.gameObject.AddComponent<MainMenuPressFeedback>();
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image AddIcon(Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 size, Vector2? anchor = null, bool preserveAspect = true)
        {
            var rect = Child(parent, name);
            var point = anchor ?? new Vector2(.5f, .5f);
            if (!rect) rect = NewRect(name, parent, point, point, point, position, size);
            rect.anchorMin = rect.anchorMax = rect.pivot = point;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.GetComponent<Image>();
            if (!image) image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        static void ConvertText(Text source, TMP_FontAsset font)
        {
            if (!source.gameObject.activeSelf) return;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source.gameObject);
            source.color = Ink;
            var mirror = source.GetComponent<MainMenuTextMirror>();
            var visible = Child(source.transform, "Visible TMP");
            if (!visible)
            {
                visible = NewRect("Visible TMP", source.transform, Vector2.zero, Vector2.one,
                    new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                visible.offsetMin = visible.offsetMax = Vector2.zero;
            }
            var tmp = visible.GetComponent<TextMeshProUGUI>();
            if (!tmp) tmp = visible.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = source.fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = source.color;
            tmp.text = source.text;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = source.alignment switch
            {
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
            if (!mirror) mirror = source.gameObject.AddComponent<MainMenuTextMirror>();
            mirror.source = source;
            mirror.visibleText = tmp;
            source.enabled = false;
        }
    }
}
