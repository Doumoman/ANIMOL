#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.Starway.Editor
{
    /// <summary>Uses finished PNGs and pre-authored materials; creates no stroke geometry.</summary>
    public static class StoryCrayonSkinInstaller
    {
        private static string assetRoot;

        [MenuItem("Tools/ANIMOL/Story/Apply Crayon Skin To Selected Story View")]
        public static void ApplySelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the story skin.");
            if (Selection.gameObjects.Length != 1)
                throw new InvalidOperationException("Select exactly one actual StoryDialogueView object, parent or child.");
            var selected = Selection.activeGameObject;
            var candidates = selected.GetComponentsInParent<StoryDialogueView>(true)
                .Concat(selected.GetComponentsInChildren<StoryDialogueView>(true)).Distinct().ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException("The selection must identify exactly one StoryDialogueView.");
            var view = candidates[0];
            if (view == null || EditorUtility.IsPersistent(view))
            {
                Debug.LogWarning("Select a StoryDialogueView in the open scene or Prefab Stage. Open a prefab before editing its instance.");
                return;
            }
            if (!ValidateAssets()) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply story crayon skin");
            // Individual changes and created children share one group. Registering the
            // whole hierarchy as well duplicates newly created children on Undo/Redo.
            foreach (var component in view.GetComponentsInChildren<Component>(true))
                if (component != null) Undo.RecordObject(component, "Apply story crayon skin");
            Apply(view, true);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("Applied crayon sprite decoration to the selected StoryDialogueView. Existing layout, text, Button events and story data were preserved. Save the scene/prefab after review.");
        }

        public static bool ValidateAssets()
        {
            assetRoot = null;
            foreach (string id in AssetDatabase.FindAssets("UI_ANIMOL_StoryCrayonBoil t:Shader"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (Path.GetFileName(path) != "UI_ANIMOL_StoryCrayonBoil.shader") continue;
                if (assetRoot != null) { Debug.LogError("Multiple StoryCrayon copies found. Keep one package installation."); return false; }
                assetRoot = Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
            }
            if (assetRoot == null) { Debug.LogError("Import Unity/Crayon with its .meta files before applying the story skin."); return false; }
            foreach (string group in new[] { "PanelFrame", "Utility_Normal", "Utility_Highlighted", "Utility_Pressed", "Utility_Disabled", "Wallet_Normal", "Wallet_Highlighted", "Wallet_Pressed", "Wallet_Disabled" })
            {
                foreach (string frame in new[] { "A", "B", "C" })
                {
                    string path = assetRoot + "/Sprites/" + group + "_" + frame + ".png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    float border = group == "PanelFrame" ? 36f : 32f;
                    if (sprite == null || importer == null || importer.spriteBorder != Vector4.one * border || importer.spritePixelsPerUnit != 100f || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    { Debug.LogError("Invalid or missing crayon sprite import settings: " + path + ". Keep the supplied .meta file."); return false; }
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    if (settings.spriteMeshType != SpriteMeshType.FullRect || !string.IsNullOrEmpty(importer.spritePackingTag))
                    { Debug.LogError("Crayon A/B/C requires FullRect and no atlas packing: " + path); return false; }
                }
                foreach (string mode in new[] { "Animated", "Still" })
                    if (AssetDatabase.LoadAssetAtPath<Material>(assetRoot + "/Materials/StoryCrayon_" + group + "_" + mode + ".mat") == null)
                    { Debug.LogError("Missing crayon material for " + group); return false; }
            }
            return true;
        }

        public static void Apply(StoryDialogueView view, bool useUndo)
        {
            if (view == null || (assetRoot == null && !ValidateAssets())) return;
            var scope = view.GetComponent<StoryCrayonSkin>();
            if (scope == null) scope = useUndo ? Undo.AddComponent<StoryCrayonSkin>(view.gameObject) : view.gameObject.AddComponent<StoryCrayonSkin>();
            if (useUndo) Undo.RecordObject(scope, "Configure story crayon scope");
            foreach (var old in view.GetComponentsInChildren<StoryNeonPanel>(true))
            { if (useUndo) Undo.RecordObject(old, "Disable legacy neon"); old.enabled = false; }
            var serialized = new SerializedObject(view);
            var dialogue = serialized.FindProperty("dialoguePanel").objectReferenceValue as GameObject;
            if (dialogue != null) Decorate((RectTransform)dialogue.transform, null, "PanelFrame", scope, useUndo);
            foreach (var button in view.GetComponentsInChildren<Button>(true))
            {
                // The tap area already lies inside the independently decorated dialogue panel.
                if (button == serialized.FindProperty("advanceButton").objectReferenceValue) continue;
                string family = button == serialized.FindProperty("skipButton").objectReferenceValue ? "Utility" : "Wallet";
                Decorate((RectTransform)button.transform, button, family, scope, useUndo);
            }
            serialized.FindProperty("crayonSkin").objectReferenceValue = scope;
            serialized.ApplyModifiedProperties();
            scope.SetReducedMotion(serialized.FindProperty("reducedMotion").boolValue);
            if (useUndo)
            {
                // Direct Image/binding changes on a prefab instance also need overrides,
                // not only the root view reference. Undo already captures the hierarchy.
                foreach (var component in view.GetComponentsInChildren<Component>(true))
                {
                    if (component is StoryDialogueView || component is StoryCrayonSkin ||
                        component is StoryCrayonFrame || component is StoryNeonPanel || component is Image)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        EditorUtility.SetDirty(component);
                    }
                }
            }
        }

        private static void Decorate(RectTransform host, Button control, string family, StoryCrayonSkin scope, bool undo)
        {
            var fill = ChildImage(host, "CrayonQuietFill", undo);
            if (undo) { Undo.RecordObject(fill, "Style story crayon fill"); Undo.RecordObject(fill.rectTransform, "Inset story crayon fill"); }
            fill.color = new Color(5f / 255f, 6f / 255f, 9f / 255f, 0.73f);
            fill.rectTransform.offsetMin = new Vector2(9f, 9f);
            fill.rectTransform.offsetMax = new Vector2(-9f, -9f);
            fill.raycastTarget = false;
            var border = ChildImage(host, "CrayonOutline", undo);
            if (undo) { Undo.RecordObject(border, "Style story crayon outline"); Undo.RecordObject(border.rectTransform, "Order story crayon outline"); }
            border.color = Color.white; border.type = Image.Type.Sliced; border.raycastTarget = false;
            border.transform.SetAsFirstSibling(); fill.transform.SetAsFirstSibling();
            var binding = host.GetComponent<StoryCrayonFrame>();
            if (binding == null) binding = undo ? Undo.AddComponent<StoryCrayonFrame>(host.gameObject) : host.gameObject.AddComponent<StoryCrayonFrame>();
            if (undo) Undo.RecordObject(binding, "Set story crayon sprites");
            // AddComponent flushes pending Undo records. Record the Image again before
            // Configure assigns its sprite/material, so Redo restores the painted state.
            if (undo) Undo.RecordObject(border, "Connect story crayon sprite and material");
            var states = family == "PanelFrame" ? new[] { State(family) } : new[] { State(family + "_Normal"), State(family + "_Highlighted"), State(family + "_Pressed"), State(family + "_Disabled") };
            binding.Configure(border, control, scope, states);
            // Keep input/state/event components intact; only their old painted background is hidden.
            if (control != null && control.targetGraphic is Image oldImage && oldImage != border && oldImage != fill)
            {
                if (undo) Undo.RecordObject(oldImage, "Hide superseded story button background");
                Color color = oldImage.color; color.a = 0f; oldImage.color = color;
            }
        }

        /// <summary>Decorates existing host-owned popup controls without changing their input or event wiring.</summary>
        public static StoryCrayonFrame ApplyExternal(RectTransform host, Button control, StoryCrayonSkin scope, bool useUndo, Image existingBackground = null)
        {
            if (host == null || scope == null || (assetRoot == null && !ValidateAssets())) return null;
            Decorate(host, control, control ? "Wallet" : "PanelFrame", scope, useUndo);
            Image background = existingBackground ? existingBackground : host.GetComponent<Image>();
            if (!control && background)
            {
                if (useUndo) Undo.RecordObject(background, "Hide superseded story popup background");
                var color = background.color; color.a = 0; background.color = color;
            }
            return host.GetComponent<StoryCrayonFrame>();
        }

        private static StoryCrayonState State(string id)
        {
            return new StoryCrayonState
            {
                frameA = AssetDatabase.LoadAssetAtPath<Sprite>(assetRoot + "/Sprites/" + id + "_A.png"),
                animated = AssetDatabase.LoadAssetAtPath<Material>(assetRoot + "/Materials/StoryCrayon_" + id + "_Animated.mat"),
                still = AssetDatabase.LoadAssetAtPath<Material>(assetRoot + "/Materials/StoryCrayon_" + id + "_Still.mat")
            };
        }
        private static Image ChildImage(RectTransform parent, string name, bool undo)
        {
            var old = parent.Find(name);
            if (old != null) return old.GetComponent<Image>();
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            if (go.scene != parent.gameObject.scene)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (undo) Undo.RegisterCreatedObjectUndo(go, "Create story crayon decoration");
            return go.GetComponent<Image>();
        }
    }
}
#endif
