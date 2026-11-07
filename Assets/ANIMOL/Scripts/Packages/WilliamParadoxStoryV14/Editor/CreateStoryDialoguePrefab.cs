#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Animol.Starway.Editor
{
    public static class CreateStoryDialoguePrefab
    {
        private const string Folder = "Assets/ANIMOLGenerated/StoryUI";

        [MenuItem("Tools/ANIMOL/Story/Create Neon Dialogue Prefab")]
        public static void Create()
        {
            TMP_FontAsset chosenFont = FindGalmuri14();
            if (chosenFont == null)
            {
                EditorUtility.DisplayDialog("ANIMOL Story UI",
                    "Select your existing Galmuri14 TMP Font Asset, then run this menu again. If none exists, create a TMP Font Asset from fonts/Galmuri14.ttf first. No substitute font was applied.", "OK");
                return;
            }

            CreateAsset(chosenFont, Folder);
        }

        public static string CreateAsset(TMP_FontAsset chosenFont, string folder, bool replaceExisting = false)
        {
            if (chosenFont == null) throw new System.ArgumentNullException(nameof(chosenFont));
            string current = "Assets";
            foreach (var segment in folder.Substring(7).Split('/'))
            {
                string nextFolder = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(nextFolder)) AssetDatabase.CreateFolder(current, segment);
                current = nextFolder;
            }
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = EditorUtility.CreateGameObjectWithHideFlags("StoryDialogueUI", HideFlags.HideAndDontSave,
                    typeof(RectTransform), typeof(CanvasGroup), typeof(StoryDialogueView));
                SceneManager.MoveGameObjectToScene(root, previewScene);
                root.hideFlags = HideFlags.None;
                foreach (var component in root.GetComponents<Component>()) component.hideFlags = HideFlags.None;
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);
                rootRect.sizeDelta = Vector2.zero;
                var safe = Rect("SafeArea", root.transform);
                Stretch(safe);

                // Values are reference pixels under a 1080 x 1920 CanvasScaler.
                var panelRect = Rect("DialoguePanel", safe);
                panelRect.gameObject.AddComponent<CanvasGroup>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(1f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = new Vector2(0f, 20f);
                panelRect.sizeDelta = new Vector2(-88f, 422f);
                var panel = panelRect.gameObject.AddComponent<StoryNeonPanel>();
                panel.color = Color.white;
                panel.raycastTarget = false;

                // Invisible hit target is separate from decorative neon mesh.
                // It does not cover the rest of the game screen.
                var tapRect = Rect("DialogueTapTarget", panelRect);
                Stretch(tapRect);
                var advance = InvisibleButton(tapRect);

                var speaker = Text("Speaker", panelRect, 45f, chosenFont);
                AnchorTop(speaker.rectTransform, new Vector2(38f, -27f), new Vector2(680f, 58f));
                speaker.text = "초아";

                var body = Text("Body", panelRect, 45f, chosenFont);
                body.rectTransform.anchorMin = new Vector2(0f, 1f);
                body.rectTransform.anchorMax = new Vector2(1f, 1f);
                body.rectTransform.pivot = new Vector2(0.5f, 1f);
                body.rectTransform.anchoredPosition = new Vector2(0f, -97f);
                body.rectTransform.sizeDelta = new Vector2(-76f, 250f);
                body.text = "<line-height=135%>오늘도 진짜 힘들었다~\n다들 고생했어. 축제도 이제 끝이네.</line-height>";
                body.enableWordWrapping = true;
                body.overflowMode = TextOverflowModes.Overflow;

                var next = Text("Next", panelRect, 30f, chosenFont);
                next.rectTransform.anchorMin = next.rectTransform.anchorMax = new Vector2(1f, 0f);
                next.rectTransform.pivot = new Vector2(1f, 0f);
                next.rectTransform.anchoredPosition = new Vector2(-38f, 24f);
                next.rectTransform.sizeDelta = new Vector2(330f, 38f);
                next.alignment = TextAlignmentOptions.MidlineRight;
                next.text = "다음  ›";

                var actionRect = Rect("ActionButton", safe);
                actionRect.anchorMin = actionRect.anchorMax = new Vector2(0.5f, 0f);
                actionRect.pivot = new Vector2(0.5f, 0f);
                actionRect.anchoredPosition = new Vector2(0f, 466f);
                actionRect.sizeDelta = new Vector2(330f, 88f);
                var actionPanel = actionRect.gameObject.AddComponent<StoryNeonPanel>();
                actionPanel.color = Color.white;
                actionPanel.raycastTarget = false;
                var actionHit = Rect("TapTarget", actionRect);
                Stretch(actionHit);
                var actionButton = InvisibleButton(actionHit);
                // Keep the button component on the group root so hiding it
                // hides border and label together. Its target is the child Image.
                Object.DestroyImmediate(actionButton);
                actionButton = actionRect.gameObject.AddComponent<Button>();
                actionButton.targetGraphic = actionHit.GetComponent<Image>();
                actionButton.transition = Selectable.Transition.None;
                actionButton.navigation = new Navigation { mode = Navigation.Mode.None };
                var actionLabel = Text("Label", actionRect, 30f, chosenFont);
                Stretch(actionLabel.rectTransform);
                actionLabel.alignment = TextAlignmentOptions.Center;
                actionLabel.text = "등불 가리기";
                actionRect.gameObject.SetActive(false);

                StoryNeonPanel chronicleContinuePanel;
                var chronicle = CreateChronicle(safe, chosenFont, out chronicleContinuePanel);
                // Last sibling: >> stays above the Chronicle input layer.
                var skipRect = Rect("SkipButton", safe);
                skipRect.anchorMin = skipRect.anchorMax = new Vector2(1f, 1f);
                skipRect.pivot = new Vector2(1f, 1f);
                skipRect.anchoredPosition = new Vector2(-44f, -42f);
                skipRect.sizeDelta = new Vector2(104f, 88f);
                var skipPanel = skipRect.gameObject.AddComponent<StoryNeonPanel>();
                skipPanel.color = Color.white;
                skipPanel.raycastTarget = false;
                var skipHit = Rect("TapTarget", skipRect);
                Stretch(skipHit);
                var skipButton = InvisibleButton(skipHit);
                var skipLabel = Text("Label", skipRect, 45f, chosenFont);
                Stretch(skipLabel.rectTransform);
                skipLabel.alignment = TextAlignmentOptions.Center;
                skipLabel.text = ">>";

                var serialized = new SerializedObject(root.GetComponent<StoryDialogueView>());
                Assign(serialized, "viewGroup", root.GetComponent<CanvasGroup>());
                Assign(serialized, "safeArea", safe);
                Assign(serialized, "speakerText", speaker);
                Assign(serialized, "bodyText", body);
                Assign(serialized, "dialoguePanel", panelRect.gameObject);
                Assign(serialized, "chronicleView", chronicle);
                Assign(serialized, "dialogueJson", FindTextAsset("ranch_dialogue"));
                Assign(serialized, "nextText", next);
                Assign(serialized, "advanceButton", advance);
                Assign(serialized, "actionButton", actionButton);
                Assign(serialized, "actionText", actionLabel);
                Assign(serialized, "skipButton", skipButton);
                Assign(serialized, "skipText", skipLabel);
                Assign(serialized, "galmuri14Font", chosenFont);
                var panels = serialized.FindProperty("neonPanels");
                panels.arraySize = 4;
                panels.GetArrayElementAtIndex(0).objectReferenceValue = panel;
                panels.GetArrayElementAtIndex(1).objectReferenceValue = actionPanel;
                panels.GetArrayElementAtIndex(2).objectReferenceValue = skipPanel;
                panels.GetArrayElementAtIndex(3).objectReferenceValue = chronicleContinuePanel;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                string path = replaceExisting ? folder + "/StoryDialogueUI.prefab" : AssetDatabase.GenerateUniqueAssetPath(folder + "/StoryDialogueUI.prefab");
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("Created " + path + ". Place under your existing full-screen Canvas; Galmuri14 TMP font reused; assign dialogue TextAsset. No scene objects or EventSystem were created.");
                return path;
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static StoryChronicleView CreateChronicle(Transform parent, TMP_FontAsset font, out StoryNeonPanel continuePanel)
        {
            var root = Rect("ChronicleLayer", parent);
            Stretch(root);
            var tint = root.gameObject.AddComponent<Image>();
            tint.color = new Color(13f / 255f, 21f / 255f, 43f / 255f, 0.70f);
            tint.raycastTarget = true;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            var view = root.gameObject.AddComponent<StoryChronicleView>();

            var viewport = Rect("TextViewport", root);
            viewport.anchorMin = new Vector2(0.08f, 0.14f);
            viewport.anchorMax = new Vector2(0.92f, 0.89f);
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("ScrollingColumn", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var title = Text("RecordTitle", content, 45f, font);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.alignment = TextAlignmentOptions.Top;
            title.enableWordWrapping = true;
            title.text = string.Empty;
            var row = Text("RecordTemplate", content, 45f, font);
            row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            row.rectTransform.pivot = new Vector2(0.5f, 1f);
            row.alignment = TextAlignmentOptions.Top;
            row.enableWordWrapping = true;
            row.richText = true;
            row.text = "<line-height=160%></line-height>";
            row.gameObject.SetActive(false);

            var hint = Text("HoldHint", root, 30f, font);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 56f);
            hint.rectTransform.sizeDelta = new Vector2(700f, 48f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.text = string.Empty;

            var nextRect = Rect("ChronicleContinue", root);
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(0.5f, 0f);
            nextRect.pivot = new Vector2(0.5f, 0f);
            nextRect.anchoredPosition = new Vector2(0f, 56f);
            nextRect.sizeDelta = new Vector2(330f, 88f);
            continuePanel = nextRect.gameObject.AddComponent<StoryNeonPanel>();
            continuePanel.color = Color.white;
            continuePanel.raycastTarget = false;
            var hit = Rect("TapTarget", nextRect);
            Stretch(hit);
            var temporaryButton = InvisibleButton(hit);
            Object.DestroyImmediate(temporaryButton);
            var button = nextRect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var nextLabel = Text("Label", nextRect, 45f, font);
            Stretch(nextLabel.rectTransform);
            nextLabel.alignment = TextAlignmentOptions.Center;
            nextLabel.text = "계속";
            nextRect.gameObject.SetActive(false);

            foreach (var panel in root.GetComponentsInChildren<StoryNeonPanel>(true))
                if (panel.GetComponent<CanvasRenderer>() == null) panel.gameObject.AddComponent<CanvasRenderer>();
            var serialized = new SerializedObject(view);
            Assign(serialized, "chronicleJson", FindTextAsset("ranch_chronicle"));
            Assign(serialized, "group", group);
            Assign(serialized, "viewport", viewport);
            Assign(serialized, "content", content);
            Assign(serialized, "title", title);
            Assign(serialized, "rowTemplate", row);
            Assign(serialized, "hint", hint);
            Assign(serialized, "continueButton", button);
            Assign(serialized, "continueText", nextLabel);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TextAsset FindTextAsset(string exactName)
        {
            TextAsset result = null;
            foreach (string guid in AssetDatabase.FindAssets(exactName + " t:TextAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != exactName ||
                    !path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (result != null)
                {
                    Debug.LogWarning("Multiple " + exactName + ".json assets exist. Assign the intended TextAsset in the new prefab.");
                    return null;
                }
                result = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
            return result;
        }

        private static bool IsGalmuri14(TMP_FontAsset font)
        {
            if (font == null) return false;
            string family = font.faceInfo.familyName ?? string.Empty;
            string source = font.sourceFontFile != null ? font.sourceFontFile.name : string.Empty;
            return ContainsGalmuri14(family) || ContainsGalmuri14(source) || ContainsGalmuri14(font.name);
        }

        private static bool ContainsGalmuri14(string value)
        {
            string compact = (value ?? string.Empty).Replace(" ", "").Replace("_", "").Replace("-", "");
            return compact.IndexOf("Galmuri14", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                compact.IndexOf("갈무리14", System.StringComparison.Ordinal) >= 0;
        }

        private static TMP_FontAsset FindGalmuri14()
        {
            // The user's selected asset takes precedence over all searches.
            var selected = Selection.activeObject as TMP_FontAsset;
            if (IsGalmuri14(selected)) return selected;
            if (Selection.activeGameObject != null)
            {
                foreach (var label in Selection.activeGameObject.GetComponentsInChildren<TMP_Text>(true))
                    if (IsGalmuri14(label.font)) return label.font;
            }
            var matches = new System.Collections.Generic.List<TMP_FontAsset>();
            foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (IsGalmuri14(font)) matches.Add(font);
            }
            if (matches.Count == 1) return matches[0];
            if (matches.Count > 1)
                Debug.LogWarning("Multiple Galmuri14 TMP Font Assets exist. Select the one already used by the project before creating the story prefab.");
            return null;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            return result;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, float size, TMP_FontAsset font)
        {
            var rect = Rect(name, parent);
            var result = rect.gameObject.AddComponent<TextMeshProUGUI>();
            result.font = font;
            result.fontSize = size;
            result.fontStyle = FontStyles.Normal;
            result.fontWeight = FontWeight.Regular;
            result.fontSharedMaterial = font.material;
            result.color = new Color(245f / 255f, 243f / 255f, 233f / 255f, 1f);
            result.alignment = TextAlignmentOptions.TopLeft;
            result.raycastTarget = false;
            result.enableAutoSizing = false;
            result.margin = Vector4.zero;
            return result;
        }

        private static Button InvisibleButton(RectTransform rect)
        {
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var result = rect.gameObject.AddComponent<Button>();
            result.targetGraphic = hit;
            result.transition = Selectable.Transition.None;
            result.navigation = new Navigation { mode = Navigation.Mode.None };
            return result;
        }

        private static void AnchorTop(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Assign(SerializedObject owner, string name, Object value)
        {
            owner.FindProperty(name).objectReferenceValue = value;
        }
    }
}
#endif
