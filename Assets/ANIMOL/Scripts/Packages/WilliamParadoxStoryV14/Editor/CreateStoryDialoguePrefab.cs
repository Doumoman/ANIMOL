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

        public static string CreateAsset(TMP_FontAsset chosenFont, string folder)
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
                panelRect.anchoredPosition = new Vector2(0f, 54f);
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

                var progress = Text("Progress", panelRect, 30f, chosenFont);
                progress.color = new Color(0.76f, 0.83f, 0.94f, 0.8f);
                progress.rectTransform.anchorMin = progress.rectTransform.anchorMax = Vector2.zero;
                progress.rectTransform.pivot = Vector2.zero;
                progress.rectTransform.anchoredPosition = new Vector2(38f, 24f);
                progress.rectTransform.sizeDelta = new Vector2(240f, 38f);
                progress.text = "1 / 20";

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
                actionRect.anchoredPosition = new Vector2(0f, 500f);
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

                // Top-right, inside SafeArea. No scene ID badge is created.
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
                Assign(serialized, "progressText", progress);
                Assign(serialized, "nextText", next);
                Assign(serialized, "advanceButton", advance);
                Assign(serialized, "actionButton", actionButton);
                Assign(serialized, "actionText", actionLabel);
                Assign(serialized, "skipButton", skipButton);
                Assign(serialized, "skipText", skipLabel);
                Assign(serialized, "galmuri14Font", chosenFont);
                var panels = serialized.FindProperty("neonPanels");
                panels.arraySize = 3;
                panels.GetArrayElementAtIndex(0).objectReferenceValue = panel;
                panels.GetArrayElementAtIndex(1).objectReferenceValue = actionPanel;
                panels.GetArrayElementAtIndex(2).objectReferenceValue = skipPanel;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/StoryDialogueUI.prefab");
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
