using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI.StyleV19.Editor
{
    public sealed class AnimolUiV19TextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AnimolUiV19Installer.Root + "/Textures/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            bool pattern = assetPath.Contains("/Pattern/");
            importer.textureType = pattern ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = pattern ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            if (pattern) return;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            bool frame = assetPath.Contains("/Buttons/") || assetPath.Contains("/Panels/");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            settings.spriteBorder = frame ? new Vector4(36, 36, 36, 36) : Vector4.zero;
            importer.SetTextureSettings(settings);
        }
    }

    public static class AnimolUiV19Installer
    {
        public const string Root = "Assets/ANIMOL/UI/StyleV19";
        public const string SkinPath = Root + "/Generated/ANIMOL_UI_V19.asset";

        [MenuItem("Tools/ANIMOL/UI V19/1 Import sprites and build skin")]
        public static void ImportAndBuildSkin()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(Root + "/Generated");
            AssetDatabase.Refresh();
            var skin = AssetDatabase.LoadAssetAtPath<AnimolUiV19Skin>(SkinPath);
            if (!skin)
            {
                skin = ScriptableObject.CreateInstance<AnimolUiV19Skin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.mode = LoadSet("Mode"); skin.primary = LoadSet("Primary");
            skin.utility = LoadSet("Utility"); skin.profile = LoadSet("Profile");
            skin.paperStrip = LoadSprite("Panels/PaperStrip");
            skin.paperPanel = LoadSprite("Panels/PaperPanel");
            EditorUtility.SetDirty(skin); AssetDatabase.SaveAssets();
            Debug.Log("V19 skin ready: 22 sprites, Border 36, PPU 100. Assign this skin to explicit existing bindings.", skin);
        }

        private static PaperButtonSprites LoadSet(string role)
        {
            return new PaperButtonSprites
            {
                normal = LoadSprite("Buttons/" + role + "_Normal"),
                highlighted = LoadSprite("Buttons/" + role + "_Highlighted"),
                pressed = LoadSprite("Buttons/" + role + "_Pressed"),
                disabled = LoadSprite("Buttons/" + role + "_Disabled"),
                active = LoadSprite("Buttons/" + role + "_Active")
            };
        }
        private static Sprite LoadSprite(string relative)
        {
            var path = Root + "/Textures/" + relative + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) throw new InvalidOperationException("Missing imported V19 sprite: " + path);
            return sprite;
        }

        [MenuItem("Tools/ANIMOL/UI V19/2 Apply explicit bindings on selected object")]
        public static void ApplySelectedBindings()
        {
            var go = Selection.activeGameObject;
            var bindings = go ? go.GetComponent<AnimolUiV19Bindings>() : null;
            if (!bindings) { Debug.LogError("Select the GameObject with AnimolUiV19Bindings. Assign its panel/button references first."); return; }
            Undo.RegisterFullObjectHierarchyUndo(go, "Apply ANIMOL UI V19");
            if (!bindings.skin) bindings.skin = AssetDatabase.LoadAssetAtPath<AnimolUiV19Skin>(SkinPath);
            if (!bindings.skin) { Debug.LogError("Run Import sprites and build skin first."); return; }
            bindings.Apply();
            var layout = go.GetComponent<AnimolUiV19ChromeLayout>();
            if (layout) layout.RefreshLayout();
            foreach (var component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            if (go.scene.IsValid()) EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("V19 applied to explicit references. Existing objects, onClick listeners and gameplay state were retained.", go);
        }

        [MenuItem("Tools/ANIMOL/UI V19/3 Validate selected bindings")]
        public static void ValidateSelectedBindings()
        {
            var go = Selection.activeGameObject;
            var b = go ? go.GetComponent<AnimolUiV19Bindings>() : null;
            if (!b || !b.skin) { Debug.LogError("Select assigned V19 bindings first."); return; }
            int errors = 0;
            foreach (var binding in b.buttons)
            {
                if (binding == null || !binding.control) { errors++; continue; }
                var image = binding.control.targetGraphic as Image;
                var set = b.skin.Get(binding.role);
                if (set == null) { errors++; continue; }
                bool expected = image && (image.sprite == set.normal || image.sprite == set.active);
                bool state = binding.control.transition == Selectable.Transition.SpriteSwap && binding.control.spriteState.disabledSprite == set.disabled;
                var shown = image ? image.overrideSprite : null;
                bool shownExpected = !shown || shown == set.normal || shown == set.highlighted || shown == set.pressed || shown == set.disabled || shown == set.active;
                if (!expected || !state || !shownExpected || image.color != Color.white || image.type != Image.Type.Sliced || !Mathf.Approximately(image.pixelsPerUnitMultiplier, 1))
                { Debug.LogError("V19 frame/state/tint mismatch (another runtime skin may be overwriting it): " + binding.control.name, binding.control); errors++; }
            }
            foreach (var panel in new[] { b.topPaper, b.bottomPaper })
                if (!panel || panel.sprite != b.skin.paperStrip || panel.type != Image.Type.Sliced || panel.color != Color.white) errors++;
            Debug.Log("V19 validation mismatches: " + errors + ". Run again in Play Mode after the current project has initialized.", b);
        }

        [MenuItem("Tools/ANIMOL/UI V19/4 Create reference chrome prefab")]
        public static void CreateReferencePrefab()
        {
            var fonts = AssetDatabase.FindAssets("Galmuri14 t:TMP_FontAsset")
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).Where(f => f).ToArray();
            if (fonts.Length != 1)
            {
                Debug.LogError("V19: choose the existing Galmuri14 TMP font and call CreateReferencePrefabWithFont(font). No replacement font is installed.");
                return;
            }
            CreateReferencePrefabWithFont(fonts[0]);
        }

        public static void CreateReferencePrefabWithFont(TMP_FontAsset font)
        {
            if (!font) throw new ArgumentNullException(nameof(font));
            ImportAndBuildSkin();
            var skin = AssetDatabase.LoadAssetAtPath<AnimolUiV19Skin>(SkinPath);
            var temporaryParent = Rect("V19ReferenceBuildContext", null);
            temporaryParent.sizeDelta = new Vector2(1080, 1920);
            var root = Rect("ANIMOL_MainChrome_V19_Reference", temporaryParent);
            try
            {
                root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;
                var layout = root.gameObject.AddComponent<AnimolUiV19ChromeLayout>();
                layout.useSafeArea = false;
                var binding = root.gameObject.AddComponent<AnimolUiV19Bindings>(); binding.skin = skin;
                var top = Panel("TopPaper", root, skin.paperStrip);
                var bottom = Panel("BottomPaper", root, skin.paperStrip);
                binding.topPaper = top; binding.bottomPaper = bottom;
                layout.topPaper = (RectTransform)top.transform; layout.bottomPaper = (RectTransform)bottom.transform;
                layout.topContent = Rect("TopContent", root); layout.bottomContent = Rect("BottomContent", root);
                layout.modeRow = Rect("Modes", layout.bottomContent);
                layout.profile = MakeButton("Profile", layout.topContent, "게스트", "profile", font, binding, PaperButtonRole.Profile);
                layout.sound = MakeButton("Sound", layout.topContent, "", "sound", font, binding, PaperButtonRole.Utility);
                layout.settings = MakeButton("Settings", layout.topContent, "", "settings", font, binding, PaperButtonRole.Utility);
                layout.modes = new[] {
                    MakeButton("Campaign", layout.modeRow, "캠페인", "campaign", font, binding, PaperButtonRole.Mode, true),
                    MakeButton("Battle", layout.modeRow, "대전", "battle", font, binding, PaperButtonRole.Mode),
                    MakeButton("Custom", layout.modeRow, "커스텀방", "custom", font, binding, PaperButtonRole.Mode)
                };
                layout.modes[1].GetComponent<Button>().interactable = false;
                layout.play = MakeButton("Play", layout.bottomContent, "캠페인 시작", "arrow-right", font, binding, PaperButtonRole.Primary);
                binding.Apply(); layout.RefreshLayout();
                layout.useSafeArea = true;
                var path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Generated/ANIMOL_MainChrome_V19_Reference.prefab");
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                Debug.Log("Reference chrome prefab created (no gameplay/auth/purchase actions): " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporaryParent.gameObject); }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static Image Panel(string name, Transform parent, Sprite sprite)
        {
            var r = Rect(name, parent); var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.color = Color.white; image.raycastTarget = false;
            return image;
        }
        private static RectTransform MakeButton(string name, Transform parent, string label, string icon, TMP_FontAsset font,
            AnimolUiV19Bindings bindings, PaperButtonRole role, bool active = false)
        {
            var r = Rect(name, parent); var image = r.gameObject.AddComponent<Image>();
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            bindings.buttons.Add(new PaperButtonBinding { control = button, role = role, active = active });
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Textures/Icons/" + icon + ".png");
            if (iconSprite)
            {
                var ir = Rect("Icon", r); var ii = ir.gameObject.AddComponent<Image>();
                ii.sprite = iconSprite; ii.preserveAspect = true; ii.raycastTarget = false;
                ir.anchorMin = ir.anchorMax = new Vector2(role == PaperButtonRole.Utility ? .5f : role == PaperButtonRole.Primary ? 1 : 0, .5f);
                float iconSize = role == PaperButtonRole.Primary || role == PaperButtonRole.Utility ? 72 : 60;
                ir.pivot = new Vector2(.5f, .5f); ir.sizeDelta = new Vector2(iconSize, iconSize);
                ir.anchoredPosition = role == PaperButtonRole.Utility ? Vector2.zero : new Vector2(role == PaperButtonRole.Primary ? -60 : 54, 0);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var lr = Rect("Label", r); lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = new Vector2(96, 12);
                lr.offsetMax = new Vector2(role == PaperButtonRole.Primary ? -96 : -24, -12);
                var text = lr.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = label;
                text.fontSize = role == PaperButtonRole.Primary ? 78 : 45;
                text.alignment = TextAlignmentOptions.Center; text.color = new Color32(41, 38, 59, 255); text.raycastTarget = false;
            }
            return r;
        }
    }
}
