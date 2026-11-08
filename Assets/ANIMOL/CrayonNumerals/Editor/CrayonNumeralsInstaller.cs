#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using William.NotebookUI;

namespace Animol.CrayonNumerals.Editor
{
    /// <summary>Replaces only existing numeric artwork/font references in a selected map or puzzle HUD.</summary>
    public static class CrayonNumeralsInstaller
    {
        private const string Root = "Assets/ANIMOL/CrayonNumerals/";
        private const string FontPath = Root + "Fonts/ANIMOL-Crayon-Numerals.ttf";
        private const string Generated = Root + "Generated/";
        private const string FontFolder = Generated + "Fonts/", MaterialFolder = Generated + "Materials/";
        private const string Characters = "0123456789/-+: .IV";
        private static readonly string[] Roman = { "I", "II", "III", "IV" };
        private static readonly string[] ColorProperties = { "_FaceColor", "_OutlineColor", "_UnderlayColor" };
        private static readonly string[] FloatProperties = {
            "_FaceDilate", "_OutlineWidth", "_OutlineSoftness", "_WeightNormal", "_WeightBold",
            "_UnderlayOffsetX", "_UnderlayOffsetY", "_UnderlayDilate", "_UnderlaySoftness"
        };
        private static readonly string[] Keywords = { "OUTLINE_ON", "UNDERLAY_ON", "UNDERLAY_INNER" };

        [MenuItem("ANIMOL/Crayon Numerals/Apply To Selected Gameplay HUD")]
        public static void ApplyToSelectedGameplayHud()
        {
            NotebookGameplayHud hud = ResolveSelected<NotebookGameplayHud>();
            NotebookSpriteNumber number = hud.costNumber;
            if (!number || !number.area || number.gameObject.scene != hud.gameObject.scene
                || !number.transform.IsChildOf(hud.transform)
                || IsWithin(number.transform, hud.pauseOverlay) || IsWithin(number.transform, hud.clearOverlay)
                || IsWithin(number.transform, hud.modalBackdrop))
                throw new InvalidOperationException("Select the actual puzzle HUD with its separate remaining-turn number display.");
            if (number.digits == null || number.digits.Length < 10 || number.places == null)
                throw new InvalidOperationException("The existing turn display needs its ten digit references and original place Images.");
            foreach (Image place in number.places)
                if (place && (!place.transform.IsChildOf(number.transform) || place.gameObject.scene != hud.gameObject.scene))
                    throw new InvalidOperationException("All digit places must remain in this exact turn-number hierarchy.");

            Sprite[] next = LoadDigits();
            Sprite[] previous = (Sprite[])number.digits.Clone();
            string hudState = EditorJsonUtility.ToJson(hud), rectState = EditorJsonUtility.ToJson(number.area);
            string[] placeGeometry = number.places.Select(image => image ? EditorJsonUtility.ToJson(image.rectTransform) : null).ToArray();
            float height = number.maximumDigitHeight, gap = number.gap;
            int value = number.Value;
            bool dirtyBefore = hud.gameObject.scene.isDirty;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            const string action = "Apply Original Crayon Turn Numerals"; Undo.SetCurrentGroupName(action);
            try
            {
                Undo.RecordObject(number, action); number.digits = next;
                foreach (Image place in number.places)
                {
                    if (!place) continue;
                    int index = Array.IndexOf(previous, place.sprite);
                    if (index >= 0 && index < 10)
                    {
                        // Preserve the number already displayed in Edit Mode; never call SetNumber(0).
                        Undo.RecordObject(place, action); place.sprite = next[index];
                        RecordOverride(place); EditorUtility.SetDirty(place);
                    }
                }
                if (EditorJsonUtility.ToJson(hud) != hudState || EditorJsonUtility.ToJson(number.area) != rectState
                    || number.maximumDigitHeight != height || number.gap != gap || number.Value != value)
                    throw new InvalidOperationException("A protected HUD reference, numeric value or layout changed.");
                for (int i = 0; i < number.places.Length; i++)
                    if (number.places[i] && EditorJsonUtility.ToJson(number.places[i].rectTransform) != placeGeometry[i])
                        throw new InvalidOperationException("The original digit-place geometry changed.");
                RecordOverride(number); EditorUtility.SetDirty(number);
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                Selection.activeGameObject = hud.gameObject;
                Debug.Log("Original crayon digits connected only to the selected remaining-turn display. Value, places, size, gauge, buttons and pause/clear UI were retained. Review and save the Scene or Prefab Stage; Undo restores the previous digit references.", hud);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                if (dirtyBefore) EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                throw;
            }
        }

        [MenuItem("ANIMOL/Crayon Numerals/Apply To Selected Map Numbers")]
        public static void ApplyToSelectedMapNumbers()
        {
            NotebookRoot ui = ResolveSelected<NotebookRoot>();
            if (!ui.stageScreen || ui.stageScreen == ui.gameObject || ui.stageScreen == ui.mainScreen
                || ui.stageScreen == ui.settingsScreen || ui.stageScreen == ui.modal
                || ui.stageScreen.scene != ui.gameObject.scene
                || Contains(ui.stageScreen.transform, ui.mainScreen) || Contains(ui.stageScreen.transform, ui.settingsScreen)
                || Contains(ui.stageScreen.transform, ui.modal) || IsWithin(ui.stageScreen.transform, ui.mainScreen)
                || IsWithin(ui.stageScreen.transform, ui.settingsScreen) || IsWithin(ui.stageScreen.transform, ui.modal))
                throw new InvalidOperationException("Select the actual NotebookRoot with its separate existing constellation map.");

            var targets = new HashSet<TMP_Text>();
            AddNumeric(targets, ui.walletText, ui, true); AddNumeric(targets, ui.stageName, ui, true);
            foreach (NotebookNodeView node in ui.nodes ?? Array.Empty<NotebookNodeView>())
            {
                if (node == null) continue;
                AddNumeric(targets, node.label, ui, true); AddNumeric(targets, node.number, ui, false);
            }
            foreach (TMP_Text text in ui.stageScreen.GetComponentsInChildren<TMP_Text>(true))
                if (Roman.Contains((text.text ?? "").Trim())) AddNumeric(targets, text, ui, false);
            if (targets.Count == 0)
                throw new InvalidOperationException("No numeric stage/wallet labels or I–IV theme labels were found in the selected map.");

            Font source = LoadSourceFont();
            // Prepare owned font resources before touching the selected map's references.
            var fonts = new Dictionary<TMP_Text, TMP_FontAsset>();
            var materials = new Dictionary<TMP_Text, Material>();
            foreach (TMP_Text text in targets)
            {
                TMP_FontAsset font = EnsureFont(source, text.font);
                fonts.Add(text, font); materials.Add(text, EnsureStyleMaterial(font, text.fontSharedMaterial));
            }
            string uiState = EditorJsonUtility.ToJson(ui);
            var geometry = targets.ToDictionary(text => text, TextGeometry);
            bool dirtyBefore = ui.gameObject.scene.isDirty;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            const string action = "Apply Original Crayon Map Numerals"; Undo.SetCurrentGroupName(action);
            try
            {
                foreach (TMP_Text text in targets)
                {
                    Undo.RecordObject(text, action);
                    text.font = fonts[text]; text.fontSharedMaterial = materials[text]; text.SetAllDirty();
                    if (TextGeometry(text) != geometry[text])
                        throw new InvalidOperationException("A numeric label's text, color, size or original rectangle changed.");
                    RecordOverride(text); EditorUtility.SetDirty(text);
                }
                if (EditorJsonUtility.ToJson(ui) != uiState)
                    throw new InvalidOperationException("A protected NotebookRoot reference changed.");
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                Selection.activeGameObject = ui.gameObject;
                Debug.Log("Original numeric font connected to " + targets.Count + " selected-map numeric/I–IV labels only. Korean theme labels, Main, Settings, Modal, layout and all game logic were retained. Review and save; Undo restores old text font/material references. Generated font resources remain reusable.", ui);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                if (dirtyBefore) EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                throw;
            }
        }

        private static void AddNumeric(HashSet<TMP_Text> targets, TMP_Text text, NotebookRoot ui, bool allowEmpty)
        {
            if (!text || !text.transform.IsChildOf(ui.stageScreen.transform)
                || IsWithin(text.transform, ui.mainScreen) || IsWithin(text.transform, ui.settingsScreen)
                || IsWithin(text.transform, ui.modal)) return;
            string value = (text.text ?? "").Trim();
            if (value.Length == 0) { if (allowEmpty) targets.Add(text); return; }
            // The limited numeric font is never assigned to Korean/Latin prose or rich-text markup.
            if (value.All(character => Characters.IndexOf(character) >= 0)
                && (value.Any(char.IsDigit) || Roman.Contains(value))) targets.Add(text);
        }

        private static string TextGeometry(TMP_Text text) => EditorJsonUtility.ToJson(text.rectTransform)
            + "|" + text.text + "|" + text.fontSize.ToString("R", CultureInfo.InvariantCulture)
            + "|" + text.enableAutoSizing + "|" + text.fontSizeMin.ToString("R", CultureInfo.InvariantCulture)
            + "|" + text.fontSizeMax.ToString("R", CultureInfo.InvariantCulture) + "|" + text.alignment
            + "|" + text.margin.ToString("R") + "|" + text.color.ToString("R") + "|" + text.raycastTarget;

        private static Sprite[] LoadDigits()
        {
            var digits = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
                string path = Root + "Sprites/Digit_" + i + ".png";
                if (!File.Exists(path)) throw new InvalidOperationException("Missing finished numeric sprite: " + path);
                AssetDatabase.ImportAsset(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) throw new InvalidOperationException("Cannot import numeric sprite: " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100; importer.spritePivot = new Vector2(.5f, .5f);
                importer.spriteBorder = Vector4.zero; importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 1024;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.SaveAndReimport(); digits[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!digits[i] || Mathf.RoundToInt(digits[i].rect.width) != 220 || Mathf.RoundToInt(digits[i].rect.height) != 280)
                    throw new InvalidOperationException("The numeric sprite must keep its supplied 220×280 canvas: " + path);
            }
            return digits;
        }

        private static Font LoadSourceFont()
        {
            if (!File.Exists(FontPath)) throw new InvalidOperationException("Missing original numeric font: " + FontPath);
            AssetDatabase.ImportAsset(FontPath);
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!source) throw new InvalidOperationException("Unity could not import the original numeric TTF.");
            return source;
        }

        private static TMP_FontAsset EnsureFont(Font source, TMP_FontAsset previous)
        {
            string previousPath = previous ? AssetDatabase.GetAssetPath(previous) : "";
            if (previous && previousPath.StartsWith(FontFolder, StringComparison.Ordinal))
            {
                ValidateOwnedFont(previous);
                return previous;
            }
            if (previous && string.IsNullOrEmpty(previousPath))
                throw new InvalidOperationException("Save the existing map font as an asset before assigning a persistent numeric fallback.");
            string key = previous ? AssetDatabase.AssetPathToGUID(previousPath) : "NoFallback";
            string path = FontFolder + "ANIMOL_Crayon_Numerals_" + key + ".asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing)
            {
                ValidateOwnedFont(existing);
                return existing;
            }
            if (AssetDatabase.LoadMainAssetAtPath(path) || File.Exists(path))
                throw new InvalidOperationException("A foreign asset occupies the dedicated numeric font path; it will not be overwritten.");
            EnsureFolder(FontFolder.TrimEnd('/'));
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (!shader) throw new InvalidOperationException("Import TMP Essential Resources before applying numeric map typography.");
            TMP_FontAsset created = null;
            Material createdMaterial = null;
            Texture2D[] createdAtlases = Array.Empty<Texture2D>();
            try
            {
                created = TMP_FontAsset.CreateFontAsset(source, 96, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
                if (!created || !created.material) throw new InvalidOperationException("The original numeric TMP font could not be created.");
                createdMaterial = created.material; createdAtlases = created.atlasTextures ?? Array.Empty<Texture2D>();
                string missing;
                if (!created.TryAddCharacters(Characters, out missing) || !string.IsNullOrEmpty(missing))
                    throw new InvalidOperationException("The numeric source font is missing required glyphs: " + missing);
                createdAtlases = created.atlasTextures ?? Array.Empty<Texture2D>();
                created.name = "ANIMOL Crayon Numerals";
                created.material.name = "ANIMOL Crayon Numerals Base"; created.material.shader = shader;
                created.fallbackFontAssetTable = previous ? new List<TMP_FontAsset> { previous } : new List<TMP_FontAsset>();
                created.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.AddObjectToAsset(created.material, created);
                foreach (Texture2D atlas in created.atlasTextures)
                    if (atlas) { atlas.name = "ANIMOL Crayon Numerals Atlas"; AssetDatabase.AddObjectToAsset(atlas, created); }
                EditorUtility.SetDirty(created); AssetDatabase.SaveAssetIfDirty(created);
                ValidateOwnedFont(created);
                return created;
            }
            catch
            {
                // Only remove this invocation's newly created main asset, never an older or foreign resource.
                if (created && AssetDatabase.Contains(created) && AssetDatabase.GetAssetPath(created) == path
                    && AssetDatabase.LoadMainAssetAtPath(path) == created) AssetDatabase.DeleteAsset(path);
                if (createdMaterial && !AssetDatabase.Contains(createdMaterial)) UnityEngine.Object.DestroyImmediate(createdMaterial);
                foreach (Texture2D atlas in createdAtlases)
                    if (atlas && !AssetDatabase.Contains(atlas)) UnityEngine.Object.DestroyImmediate(atlas);
                if (created && !AssetDatabase.Contains(created)) UnityEngine.Object.DestroyImmediate(created);
                throw;
            }
        }

        private static void ValidateOwnedFont(TMP_FontAsset font)
        {
            string path = font ? AssetDatabase.GetAssetPath(font) : "";
            if (!font || !path.StartsWith(FontFolder, StringComparison.Ordinal) || !AssetDatabase.Contains(font)
                || AssetDatabase.LoadMainAssetAtPath(path) != font || !font.HasCharacters(Characters)
                || !font.material || !font.material.shader || !AssetDatabase.IsSubAsset(font.material)
                || AssetDatabase.GetAssetPath(font.material) != path)
                throw new InvalidOperationException("The generated numeric font or its material is incomplete or not persistent; existing resources were retained.");
            Texture2D[] atlases = font.atlasTextures;
            if (atlases == null || atlases.Length == 0 || !font.atlasTexture || font.atlasTexture != atlases[0]
                || atlases.Any(atlas => !atlas || !AssetDatabase.Contains(atlas) || !AssetDatabase.IsSubAsset(atlas)
                    || AssetDatabase.GetAssetPath(atlas) != path)
                || !font.material.HasProperty("_MainTex") || font.material.GetTexture("_MainTex") != atlases[0])
                throw new InvalidOperationException("The generated numeric font has an incomplete atlas or an incorrect material/atlas binding; existing resources were retained.");
        }

        private static Material EnsureStyleMaterial(TMP_FontAsset font, Material previous)
        {
            string previousPath = previous ? AssetDatabase.GetAssetPath(previous) : "";
            if (previous && previousPath.StartsWith(MaterialFolder, StringComparison.Ordinal)
                && previous.GetTexture("_MainTex") == font.atlasTexture) return previous;
            EnsureFolder(MaterialFolder.TrimEnd('/'));
            string fontKey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
            var style = new StringBuilder();
            if (previous)
            {
                foreach (string property in ColorProperties)
                    if (previous.HasProperty(property)) style.Append(property).Append(previous.GetColor(property).ToString("R"));
                foreach (string property in FloatProperties)
                    if (previous.HasProperty(property)) style.Append(property).Append(previous.GetFloat(property).ToString("R", CultureInfo.InvariantCulture));
                foreach (string keyword in Keywords) style.Append(keyword).Append(previous.IsKeywordEnabled(keyword));
            }
            string hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(style.ToString()))).Replace("-", "").Substring(0, 16);
            string path = MaterialFolder + "MapNumbers_" + fontKey + "_" + hash + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(font.material) { name = "ANIMOL Crayon Map Number Style " + hash };
                if (previous)
                {
                    foreach (string property in ColorProperties)
                        if (previous.HasProperty(property) && material.HasProperty(property)) material.SetColor(property, previous.GetColor(property));
                    foreach (string property in FloatProperties)
                        if (previous.HasProperty(property) && material.HasProperty(property)) material.SetFloat(property, previous.GetFloat(property));
                    foreach (string keyword in Keywords) if (previous.IsKeywordEnabled(keyword)) material.EnableKeyword(keyword); else material.DisableKeyword(keyword);
                }
                AssetDatabase.CreateAsset(material, path); EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            }
            if (material.GetTexture("_MainTex") != font.atlasTexture)
                throw new InvalidOperationException("A foreign or stale text material occupies the dedicated numeric style path.");
            return material;
        }

        private static T ResolveSelected<T>() where T : Component
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying numeric UI art.");
            if (Selection.gameObjects.Length != 1 || !Selection.activeGameObject)
                throw new InvalidOperationException("Select exactly one actual map/HUD object, parent or child.");
            GameObject selected = Selection.activeGameObject;
            if (EditorUtility.IsPersistent(selected)) throw new InvalidOperationException("Open this Prefab in Prefab Mode before applying.");
            var scene = selected.scene; var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (!scene.IsValid() || !scene.isLoaded || (EditorSceneManager.IsPreviewScene(scene) && (stage == null || stage.scene != scene)))
                throw new InvalidOperationException("The selection must belong to a loaded Scene or the current Prefab Stage.");
            var candidates = new HashSet<T>(selected.GetComponentsInParent<T>(true));
            foreach (T component in selected.GetComponentsInChildren<T>(true)) candidates.Add(component);
            if (candidates.Count != 1) throw new InvalidOperationException("The selection must resolve to exactly one " + typeof(T).Name + "; no Scene-wide guessing is performed.");
            return candidates.First();
        }
        private static bool IsWithin(Transform target, GameObject owner) => owner && (target == owner.transform || target.IsChildOf(owner.transform));
        private static bool Contains(Transform parent, GameObject child) => child && (parent == child.transform || child.transform.IsChildOf(parent));
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void RecordOverride(UnityEngine.Object item)
        {
            if (item && PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
    }
}
#endif
