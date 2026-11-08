using System;
using System.IO;
using ANIMOL.UI.Crayon;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using William.NotebookUI;
using Object = UnityEngine.Object;

namespace ANIMOL.UI.Crayon.Editor
{
    public static class BattleCrayonUIInstaller
    {
        public const string ScenePath = "Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity";
        public const string PrefabPath = "Assets/ANIMOL/Prefabs/UI/Battle/BattleCrayonUI_ArtPreview.prefab";
        static readonly Color Cream = new Color(.96f, .94f, .87f);
        static readonly Color Gold = new Color(1f, .81f, .35f);
        static readonly Color Pink = new Color(.91f, .45f, .72f);
        static readonly Color Mint = new Color(.59f, .94f, .80f);
        static readonly Color PaperDark = new Color(.025f, .035f, .05f, .96f);
        static readonly Vector2 Center = new Vector2(.5f, .5f), Top = new Vector2(.5f, 1f), Bottom = new Vector2(.5f, 0f);
        static TMP_FontAsset font;
        static CrayonUIMotion motion;
        static BattleCrayonArtPreview preview;

        [MenuItem("ANIMOL/Crayon UI/Build Standalone Battle Art Preview")]
        public static void BuildMenu() => Build();

        public static object Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building UI assets.");
            var errors = CrayonUIAssets.Validate();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset");
                if (!font) throw new InvalidOperationException("Missing existing Notebook font.");
                var root = new GameObject("BattleCrayonUI_ArtPreview");
                motion = root.AddComponent<CrayonUIMotion>();
                preview = root.AddComponent<BattleCrayonArtPreview>(); preview.motion = motion;
                var canvas = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas.transform.SetParent(root.transform, false);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                var background = Stretch("Static Constellation Sky", canvas.transform);
                var sky = background.gameObject.AddComponent<Image>();
                sky.sprite = CrayonUIAssets.Sprite("Scene/ConstellationSky_Dark"); sky.raycastTarget = false;
                var design = Rect("Design 1080 Safe Area", canvas.transform, 0, 0, 1080, 1920, Center);
                var layout = canvas.AddComponent<NotebookCanvasLayout>();
                layout.scaler = scaler; layout.design = design;
                BuildHeader(design); BuildHud(design); BuildArena(design); BuildFooter(design);
                BuildStart(design); BuildPause(design); BuildResults(design); BuildAugment(design);
                preview.ShowStart(); motion.SetReducedMotion(CrayonUIMotion.GlobalReducedMotion);
                layout.RefreshLayout();
                EditorUtility.SetDirty(motion); EditorUtility.SetDirty(preview);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Could not save art preview prefab.");
                int decorations = motion.bindings.Count;
                Object.DestroyImmediate(root);
                PrefabUtility.InstantiatePrefab(prefab, scene);
                var camera = new GameObject("Battle UI Camera", typeof(Camera));
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
                camera.GetComponent<Camera>().orthographic = true;
                camera.GetComponent<Camera>().backgroundColor = PaperDark;
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                new GameObject("Battle UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save battle UI scene.");
                AssetDatabase.SaveAssetIfDirty(prefab);
                return new { scene = ScenePath, prefab = PrefabPath, decorations, battleLogicConnected = false };
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                motion = null; preview = null; font = null;
            }
        }

        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h, Vector2 anchor)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Center; rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        static RectTransform Stretch(string name, Transform parent)
        {
            var rect = Rect(name, parent, 0, 0, 0, 0, Center);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        static Image Decor(string key, Transform parent, Color tint, float x, float y, float w, float h, Vector2 anchor)
        {
            var image = Rect(key, parent, x, y, w, h, anchor).gameObject.AddComponent<Image>();
            image.sprite = CrayonUIAssets.Sprite(key + "_A");
            image.type = key.StartsWith("Frame_", StringComparison.Ordinal) ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = image.type == Image.Type.Simple;
            image.material = CrayonUIAssets.Material(key); image.color = tint; image.raycastTarget = false;
            motion.bindings.Add(new CrayonUIMotion.Binding { image = image, animated = image.material, still = CrayonUIAssets.Material(key, true) });
            return image;
        }
        static TMP_Text Label(string name, Transform parent, string value, float size, Color tint,
            float x, float y, float w, float h, Vector2 anchor)
        {
            var text = Rect(name, parent, x, y, w, h, anchor).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = tint;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        static Button Button(string name, Transform parent, string label, string icon, string frame, Color tint,
            float x, float y, float w, float h, Vector2 anchor, UnityAction action, float fontSize = 36)
        {
            var rect = Rect(name, parent, x, y, w, h, anchor);
            var background = rect.gameObject.AddComponent<Image>(); background.color = PaperDark;
            rect.gameObject.AddComponent<CanvasGroup>();
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var colors = button.colors; colors.pressedColor = new Color(.55f, .6f, .68f);
            colors.highlightedColor = Color.white; colors.selectedColor = new Color(.85f, .94f, 1f);
            colors.disabledColor = new Color(.45f, .45f, .45f, .65f); colors.fadeDuration = .06f; button.colors = colors;
            Decor("Frame_" + frame, rect, tint, 0, 0, w, h, Center);
            if (!string.IsNullOrEmpty(icon))
            {
                bool iconOnly = string.IsNullOrEmpty(label);
                Decor("Icon_" + icon, rect, tint, iconOnly ? 0 : -w * .5f + 68, 0,
                    iconOnly ? 100 : 76, iconOnly ? 100 : 76, Center);
            }
            if (!string.IsNullOrEmpty(label)) Label("Label", rect, label, fontSize, tint,
                string.IsNullOrEmpty(icon) ? 0 : 28, 0, w - (string.IsNullOrEmpty(icon) ? 48 : 170), h - 20, Center);
            UnityEventTools.AddPersistentListener(button.onClick, action); return button;
        }
        static RectTransform Card(string name, Transform parent, float height)
        {
            var panel = Stretch(name + " Overlay", parent);
            var card = Rect(name, panel, 0, -20, 900, height, Center);
            var backing = card.gameObject.AddComponent<Image>(); backing.color = PaperDark; backing.raycastTarget = false;
            Decor("Frame_Card", card, Cream, 0, 0, 900, height, Center);
            return card;
        }
        static void BuildHeader(Transform parent)
        {
            Decor("Icon_Sigil", parent, Gold, 93, -76, 90, 90, new Vector2(0, 1));
            Label("Battle Title", parent, "별비", 48, Cream, 225, -63, 180, 70, new Vector2(0, 1));
            Label("Preview Badge", parent, "UI 미리보기", 22, new Color(.65f, .68f, .74f), 235, -110, 210, 38, new Vector2(0, 1));
            preview.pauseButton = Button("Pause", parent, "", "Pause", "ButtonSquare", Cream,
                -100, -78, 112, 112, new Vector2(1, 1), preview.ShowPause);
        }
        static void BuildHud(Transform parent)
        {
            var hud = Rect("Battle HUD", parent, 0, -218, 984, 144, Top);
            BuildHealth(hud, true, -252); BuildHealth(hud, false, 252);
        }
        static void BuildHealth(Transform parent, bool isol, float x)
        {
            var rect = Rect(isol ? "Isol HUD" : "Curse HUD", parent, x, 0, 480, 132, Center);
            Decor("Frame_HUD", rect, Cream, 0, 0, 480, 132, Center);
            Decor("Icon_" + (isol ? "Isol" : "Curse"), rect, isol ? Gold : Pink, -184, 0, 86, 86, Center);
            Label("Name", rect, isol ? "이솔" : "저주", 30, Cream, 35, 33, 300, 42, Center);
            var bar = Rect("Health", rect, 43, -24, 338, 46, Center);
            var fill = Rect("Static Health Fill", bar, 0, 0, 326, 34, Center).gameObject.AddComponent<Image>();
            fill.sprite = CrayonUIAssets.Sprite("Scene/Fill_" + (isol ? "Isol" : "Curse"));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0; fill.fillAmount = 1; fill.raycastTarget = false;
            Decor("Frame_Health", bar, Cream, 0, 0, 338, 46, Center);
            var value = Label("Health Value", bar, "100 / 100", 26, new Color(.08f, .07f, .12f), 0, 0, 300, 36, Center);
            if (isol) { preview.isolHealthFill = fill; preview.isolHealthText = value; }
            else { preview.curseHealthFill = fill; preview.curseHealthText = value; }
        }
        static void BuildArena(Transform parent)
        {
            var arena = Stretch("Static Arena Artwork", parent);
            arena.offsetMin = new Vector2(72, 300); arena.offsetMax = new Vector2(-72, -320);
            Decor("Icon_Sigil", arena, new Color(Gold.r, Gold.g, Gold.b, .62f), -250, -100, 148, 148, Center);
            Decor("Icon_Curse", arena, new Color(Pink.r, Pink.g, Pink.b, .8f), 230, 180, 104, 104, Center);
            Decor("Icon_Isol", arena, new Color(Gold.r, Gold.g, Gold.b, .5f), 190, -340, 88, 88, Center);
        }
        static void BuildFooter(Transform parent)
        {
            var score = Rect("Block Record", parent, 100, 170, 450, 124, Bottom);
            Decor("Frame_Score", score, Cream, 0, 0, 450, 124, Center);
            Decor("Icon_Shield", score, Cream, -150, 0, 68, 68, Center);
            preview.shieldCount = Label("Shield Count", score, "0", 34, Cream, -72, 0, 64, 56, Center);
            Decor("Icon_Misblock", score, Gold, 48, 0, 68, 68, Center);
            preview.misblockCount = Label("Misblock Count", score, "0", 34, Cream, 130, 0, 64, 56, Center);
            preview.augmentButton = Button("Augment", parent, "", "Augment", "ButtonSquare", Mint,
                -100, 170, 112, 112, new Vector2(1, 0), preview.ShowAugment);
            var toggleRect = Rect("Reduced Motion", parent, 244, 57, 376, 66, new Vector2(0, 0));
            var toggleBackground = toggleRect.gameObject.AddComponent<Image>(); toggleBackground.color = PaperDark;
            var toggle = toggleRect.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = toggleBackground;
            Decor("Frame_TopPill", toggleRect, Cream, 0, 0, 376, 66, Center);
            toggle.graphic = Decor("Icon_Sigil", toggleRect, Gold, -142, 0, 42, 42, Center);
            Label("Label", toggleRect, "움직임 줄이기", 26, Cream, 24, 0, 276, 50, Center);
            preview.reducedMotionToggle = toggle;
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, preview.SetReducedMotion);
            preview.resultsPreviewButton = Button("Preview Results", parent, "결과 미리보기", "", "TopPill", Cream,
                -205, 57, 300, 66, new Vector2(1, 0), preview.ShowResults, 26);
        }
        static void BuildStart(Transform parent)
        {
            var card = Card("Start Card", parent, 930); preview.startPanel = card.parent.gameObject;
            var portrait = Rect("Static Isol Portrait", card, 0, -225, 650, 370, Top).gameObject.AddComponent<Image>();
            portrait.sprite = CrayonUIAssets.Sprite("Scene/Isol_CrayonPortrait"); portrait.preserveAspect = true; portrait.raycastTarget = false;
            Label("Title", card, "별비", 64, Gold, 0, -468, 760, 90, Top);
            Label("Description", card, "작은 별빛을 이어, 하늘을 지켜요", 30, Cream, 0, -555, 780, 66, Top);
            Label("Preview Notice", card, "화면과 장식을 살펴보는 미리보기예요", 24, new Color(.67f, .7f, .77f), 0, -620, 780, 55, Top);
            preview.startButton = Button("Start", card, "시작", "Play", "ButtonWide", Cream, 0, -785, 740, 112, Top, preview.ShowHud);
            Label("Connection Note", card, "전투 연결 전 · HP와 기록은 표시용 예시", 22, new Color(.6f, .63f, .7f), 0, -875, 790, 42, Top);
        }
        static void BuildPause(Transform parent)
        {
            var card = Card("Pause Card", parent, 620); preview.pausePanel = card.parent.gameObject;
            Label("Title", card, "잠깐 쉬어가기", 52, Cream, 0, -115, 780, 90, Top);
            Label("Description", card, "같은 화면에서 이어서 살펴보세요", 28, Cream, 0, -198, 780, 60, Top);
            preview.resumeButton = Button("Resume", card, "계속하기", "Resume", "ButtonWide", Cream,
                0, -330, 740, 106, Top, preview.ShowHud);
            preview.retryButton = Button("Retry", card, "다시 시작", "Retry", "ButtonWide", Cream,
                0, -470, 740, 106, Top, preview.ShowStart);
        }
        static void BuildResults(Transform parent)
        {
            var card = Card("Result Card", parent, 840); preview.resultPanel = card.parent.gameObject;
            Label("Title", card, "별빛 기록", 56, Gold, 0, -116, 760, 94, Top);
            ResultRow(card, "차단 기록", "Shield", Cream, -315);
            ResultRow(card, "놓친 별빛", "Misblock", Pink, -505);
            preview.resultRetryButton = Button("Retry", card, "다시 시작", "Retry", "ButtonWide", Cream,
                0, -701, 740, 112, Top, preview.ShowStart);
            Label("Preview Notice", card, "아트 미리보기", 23, new Color(.6f, .63f, .7f), 0, -794, 760, 42, Top);
        }
        static void ResultRow(Transform card, string title, string icon, Color tint, float y)
        {
            var row = Rect(title, card, 0, y, 740, 142, Top);
            Decor("Frame_ModeCard", row, tint, 0, 0, 740, 142, Center);
            Decor("Icon_" + icon, row, tint, -275, 0, 86, 86, Center);
            Label("Name", row, title, 34, Cream, -40, 0, 320, 70, Center);
            Label("Value", row, "0", 44, Cream, 260, 0, 90, 74, Center);
        }
        static void BuildAugment(Transform parent)
        {
            var card = Card("Augment Card", parent, 720); preview.augmentPanel = card.parent.gameObject;
            Label("Title", card, "별빛 증강", 54, Mint, 0, -105, 760, 90, Top);
            var option = Rect("Mint Augment Option", card, 0, -332, 740, 242, Top);
            Decor("Frame_ModeCard", option, Mint, 0, 0, 740, 242, Center);
            Decor("Icon_Augment", option, Mint, -260, 0, 122, 122, Center);
            Label("Name", option, "증강 카드", 36, Cream, 65, 63, 410, 58, Center);
            Label("Description", option, "선택 상태 미리보기", 25, Cream, 65, 8, 430, 48, Center);
            preview.augmentSelectButton = Button("Select", option, "선택", "", "TopPill", Mint,
                70, -70, 360, 64, Center, preview.ToggleAugmentSelection, 28);
            preview.augmentSelection = Label("Selected State", option, "선택됨", 24, Mint, 282, 64, 115, 42, Center).gameObject;
            preview.augmentSelection.SetActive(false);
            preview.augmentBackButton = Button("Back", card, "돌아가기", "Resume", "ButtonWide", Cream,
                0, -585, 740, 104, Top, preview.ShowHud);
        }
    }
}
