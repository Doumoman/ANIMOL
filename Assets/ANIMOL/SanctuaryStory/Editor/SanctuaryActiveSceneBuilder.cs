#if UNITY_EDITOR
using System;
using ANIMOL.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory.Editor
{
    // Builds the missing story screen inside the active project's existing Canvas/EventSystem.
    // The package's own sprite installer remains the owner of the art stage and catalog.
    public static class SanctuaryActiveSceneBuilder
    {
        private const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        private const string RowPath = "Assets/ANIMOL/SanctuaryStory/Generated/SanctuaryChoiceRow.prefab";
        private static readonly Color Ink = new Color(.035f, .055f, .09f, 1f);
        private static readonly Color Paper = new Color(.97f, .94f, .84f, 1f);
        private static readonly Color Rose = new Color(.85f, .36f, .42f, 1f);

        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building SC18.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved ANIMOL_Main scene before building SC18. Do not overwrite an unsaved scene.");
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!lobby || !lobby.canvas || !lobby.safeRoot || !lobby.campaignContinueButton)
                throw new InvalidOperationException("The active main lobby Canvas and campaign route button must exist.");
            if (UnityEngine.Object.FindFirstObjectByType<SanctuaryMainStoryBridge>())
                throw new InvalidOperationException("A Sanctuary story bridge already exists. Inspect the scene before rebuilding.");
            var eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            if (eventSystems.Length != 1)
                throw new InvalidOperationException("Expected exactly one existing EventSystem; found " + eventSystems.Length);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Fonts/ANIMOLPixel14_UI.asset");
            var chapter = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ANIMOL/SanctuaryStory/Data/sanctuary_chapter_v1.json");
            if (!font || !chapter) throw new InvalidOperationException("The active Korean TMP font or chapter data is missing.");

            var rowPrefab = CreateRowPrefab(font);
            var root = Rect("SC18_SanctuaryStory", lobby.safeRoot);
            Stretch(root);
            root.gameObject.SetActive(false);
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = Ink;
            backdrop.raycastTarget = true;
            var runner = root.gameObject.AddComponent<SanctuaryStoryRunner>();
            Set(runner, "chapterJson", chapter);
            Set(runner, "playOnStart", false);
            Set(runner, "timedCuePreview", false);

            var header = Rect("Story Header", root);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -8f);
            header.sizeDelta = new Vector2(0f, 52f);
            Label("Story Name", header, font, "낙일 성소", 25, TextAlignmentOptions.Center, Paper);
            var close = Button("Story Close", header, font, "닫기", 14, new Vector2(1f, .5f), new Vector2(-37f, 0f), new Vector2(58f, 39f));
            var pause = Button("Story Pause", header, font, "일시정지", 13, new Vector2(0f, .5f), new Vector2(49f, 0f), new Vector2(80f, 39f));

            var choiceLayer = Rect("Choice Layer", root);
            Stretch(choiceLayer);
            var choiceGroup = choiceLayer.gameObject.AddComponent<CanvasGroup>();
            choiceGroup.alpha = 0f;
            choiceGroup.interactable = choiceGroup.blocksRaycasts = false;
            var bounds = Rect("Choice Safe Bounds", choiceLayer);
            bounds.anchorMin = bounds.anchorMax = new Vector2(.5f, 0f);
            bounds.pivot = new Vector2(.5f, 0f);
            bounds.anchoredPosition = new Vector2(0f, 19f);
            bounds.sizeDelta = new Vector2(374f, 515f);
            var choices = Rect("Choice Rows", bounds);
            choices.anchorMin = choices.anchorMax = new Vector2(.5f, 0f);
            choices.pivot = new Vector2(.5f, 0f);
            choices.anchoredPosition = Vector2.zero;
            choices.sizeDelta = new Vector2(360f, 50f);
            var layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var dialogue = Rect("Dialogue", root);
            dialogue.anchorMin = new Vector2(0f, 0f);
            dialogue.anchorMax = new Vector2(1f, 0f);
            dialogue.pivot = new Vector2(.5f, 0f);
            dialogue.anchoredPosition = new Vector2(0f, 12f);
            dialogue.sizeDelta = new Vector2(-24f, 177f);
            var dialogueImage = dialogue.gameObject.AddComponent<Image>();
            dialogueImage.color = new Color(.06f, .09f, .15f, .96f);
            var advance = dialogue.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.ColorTint;
            var dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
            dialogueGroup.alpha = 0f;
            dialogueGroup.interactable = dialogueGroup.blocksRaycasts = false;
            var speaker = Label("Speaker", dialogue, font, "", 22, TextAlignmentOptions.TopLeft, Rose);
            speaker.rectTransform.anchorMin = new Vector2(0f, 1f);
            speaker.rectTransform.anchorMax = new Vector2(1f, 1f);
            speaker.rectTransform.pivot = new Vector2(.5f, 1f);
            speaker.rectTransform.anchoredPosition = new Vector2(0f, -15f);
            speaker.rectTransform.sizeDelta = new Vector2(-34f, 34f);
            var body = Label("Story Text", dialogue, font, "", 21, TextAlignmentOptions.TopLeft, Paper);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = new Vector2(17f, 16f);
            body.rectTransform.offsetMax = new Vector2(-17f, -51f);
            body.textWrappingMode = TextWrappingModes.Normal;

            var ending = Label("Ending Title", root, font, "", 28, TextAlignmentOptions.Center, Paper);
            ending.rectTransform.anchorMin = ending.rectTransform.anchorMax = new Vector2(.5f, .73f);
            ending.rectTransform.sizeDelta = new Vector2(350f, 80f);

            var view = root.gameObject.AddComponent<SanctuaryUGUIView>();
            Set(view, "runner", runner);
            Set(view, "speakerLabel", speaker);
            Set(view, "bodyLabel", body);
            Set(view, "advanceButton", advance);
            Set(view, "dialogueGroup", dialogueGroup);
            Set(view, "choicesGroup", choiceGroup);
            Set(view, "choicesRoot", choices);
            Set(view, "choiceRowPrefab", rowPrefab);
            Set(view, "endingTitleLabel", ending);
            Set(view, "minimumRowHeight", 50f);
            Set(view, "rowSpacing", 6f);

            var bridge = lobby.gameObject.AddComponent<SanctuaryMainStoryBridge>();
            Set(bridge, "lobby", lobby);
            Set(bridge, "storyScreen", root.gameObject);
            Set(bridge, "runner", runner);
            Set(bridge, "closeButton", close);
            Set(bridge, "pauseButton", pause);
            Set(bridge, "openLegacyLabel", lobby.campaignContinueButton.GetComponentInChildren<Text>(true));
            Set(bridge, "pauseLabel", pause.GetComponentInChildren<TMP_Text>(true));
            var routeLabel = lobby.campaignContinueButton.GetComponentInChildren<Text>(true);
            if (routeLabel) routeLabel.text = "낙일 성소 · 이야기";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Sanctuary] Built SC18_SanctuaryStory under the existing Main UI Canvas/Safe Area. Now run Install Sprite Stage Under Selection.", root);
        }

        public static void BindStage()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open ANIMOL_Main before binding the story stage.");
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            var bridge = lobby ? lobby.GetComponent<SanctuaryMainStoryBridge>() : null;
            var root = lobby && lobby.safeRoot ? lobby.safeRoot.Find("SC18_SanctuaryStory") : null;
            var stage = root ? root.GetComponentInChildren<SanctuarySpriteStage>(true) : null;
            if (!bridge || !stage || !stage.Catalog || !stage.PresentationJson)
                throw new InvalidOperationException("Run Install Sprite Stage Under Selection before binding the active SC18 bridge.");
            Set(bridge, "stage", stage);
            Set(stage, "choiceFeetHeight", .665f);
            var matte = lobby.safeRoot.Find("Sanctuary Unsafe Area Matte") as RectTransform;
            if (!matte) matte = lobby.canvas.transform.Find("Sanctuary Unsafe Area Matte") as RectTransform;
            if (!matte)
            {
                matte = Rect("Sanctuary Unsafe Area Matte", lobby.safeRoot);
                var image = matte.gameObject.AddComponent<Image>();
                image.color = Ink;
                image.raycastTarget = false;
                matte.gameObject.SetActive(false);
            }
            matte.SetParent(lobby.safeRoot, false);
            matte.anchorMin = new Vector2(-.5f, -.5f);
            matte.anchorMax = new Vector2(1.5f, 1.5f);
            matte.offsetMin = matte.offsetMax = Vector2.zero;
            matte.SetSiblingIndex(root.GetSiblingIndex());
            Set(bridge, "unsafeAreaMatte", matte.gameObject);
            var header = root.Find("Story Header");
            if (header) header.SetAsLastSibling();
            var ending = root.Find("Ending Title");
            if (ending) ending.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Sanctuary] Bound the installed Stage to the active SC18 bridge.", stage);
        }

        private static Button CreateRowPrefab(TMP_FontAsset font)
        {
            var folder = "Assets/ANIMOL/SanctuaryStory/Generated";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ANIMOL/SanctuaryStory", "Generated");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPath))
                throw new InvalidOperationException("Existing choice row prefab must be reviewed before replacement.");
            var row = new GameObject("SanctuaryChoiceRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MainMenuPressFeedback), typeof(LayoutElement));
            var rect = (RectTransform)row.transform;
            rect.sizeDelta = new Vector2(360f, 50f);
            row.GetComponent<Image>().color = new Color(.91f, .88f, .79f, 1f);
            var button = row.GetComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 50f;
            var label = Label("Choice Label", rect, font, "선택", 19, TextAlignmentOptions.Center, Ink);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(7f, 0f);
            label.rectTransform.offsetMax = new Vector2(-7f, 0f);
            PrefabUtility.SaveAsPrefabAsset(row, RowPath);
            UnityEngine.Object.DestroyImmediate(row);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
            return prefab ? prefab.GetComponent<Button>() : null;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent, TMP_FontAsset font, string caption, float fontSize, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MainMenuPressFeedback));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(.91f, .88f, .79f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Label(name + " Label", rect, font, caption, fontSize, TextAlignmentOptions.Center, Ink);
            return button;
        }

        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(UnityEngine.Object target, string name, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(UnityEngine.Object target, string name, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
