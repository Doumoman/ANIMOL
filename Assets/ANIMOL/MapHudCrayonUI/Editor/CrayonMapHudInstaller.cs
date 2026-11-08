#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

namespace Animol.MapHudCrayonUI.Editor
{
    /// <summary>Adds prepainted decoration to explicit existing map/HUD images. Never rebuilds a screen.</summary>
    public static class CrayonMapHudInstaller
    {
        private const string Root = "Assets/ANIMOL/MapHudCrayonUI/";
        private const string ManifestPath = Root + "Data/sprite-manifest.json";
        private const string ShaderPath = Root + "Shaders/UI_ANIMOL_MapHudCrayonBoil.shader";
        private const string Materials = Root + "Generated/Materials/";

        [Serializable] private sealed class Manifest { public Entry[] entries = Array.Empty<Entry>(); }
        [Serializable] private sealed class Entry
        {
            public string id = "";
            public int width = 0, height = 0, border = 0;
            public string[] frames = Array.Empty<string>(), originalPaths = Array.Empty<string>();
        }
        private sealed class Prepared
        {
            public readonly Dictionary<Sprite, string> familyBySource = new Dictionary<Sprite, string>();
            public readonly Dictionary<string, List<CrayonSpriteSet>> setsByFamily = new Dictionary<string, List<CrayonSpriteSet>>();
            public CrayonSpriteSet[] Family(Sprite sprite)
            {
                return sprite && familyBySource.TryGetValue(sprite, out string family)
                    ? setsByFamily[family].ToArray() : Array.Empty<CrayonSpriteSet>();
            }
            public CrayonSpriteSet[] FlightStates => setsByFamily
                .Where(pair => pair.Key == "FlightStar" || pair.Key == "FlightTrail")
                .SelectMany(pair => pair.Value).ToArray();
        }
        private sealed class Target
        {
            public Image image;
            public CrayonSpriteSet[] states;
        }

        [MenuItem("ANIMOL/Crayon Map And HUD/Apply To Selected Map")]
        public static void ApplyToSelectedMap()
        {
            NotebookRoot ui = ResolveSelected<NotebookRoot>();
            if (!ui.stageScreen || ui.stageScreen == ui.gameObject || ui.stageScreen == ui.mainScreen
                || ui.stageScreen == ui.settingsScreen || ui.stageScreen == ui.modal
                || ui.stageScreen.scene != ui.gameObject.scene || !(ui.stageScreen.transform is RectTransform))
                throw new InvalidOperationException("Select the actual NotebookRoot with a separate stage screen in the same Scene or Prefab Stage.");
            Transform stage = ui.stageScreen.transform;
            if (Contains(stage, ui.mainScreen) || Contains(stage, ui.settingsScreen) || Contains(stage, ui.modal)
                || IsWithin(stage, ui.mainScreen) || IsWithin(stage, ui.settingsScreen) || IsWithin(stage, ui.modal))
                throw new InvalidOperationException("The map hierarchy overlaps Main, Settings or Modal. Resolve the actual map before applying this skin.");
            Prepared assets = PrepareAssets();
            List<Target> targets = FindTargets(ui.stageScreen.GetComponentsInChildren<Image>(true), assets,
                image => !IsWithin(image.transform, ui.mainScreen) && !IsWithin(image.transform, ui.settingsScreen)
                    && !IsWithin(image.transform, ui.modal));
            if (targets.Count == 0) throw new InvalidOperationException("No mapped existing map sprites were found. Inspect the actual map's sprite references; no screen was changed.");
            string rootState = EditorJsonUtility.ToJson(ui);
            Apply(ui.stageScreen, ui, null, targets, assets, "Apply Crayon Map Skin", () =>
            {
                if (EditorJsonUtility.ToJson(ui) != rootState)
                    throw new InvalidOperationException("A NotebookRoot reference changed. The skin operation has been rolled back.");
            });
            Debug.Log("Crayon decoration applied only to the selected map: " + targets.Count
                + " existing images. Main, Settings, Modal, artwork, node coordinates and all button events were retained. Review and save the Scene or Prefab Stage; Undo reverses this skin operation.", ui);
        }

        [MenuItem("ANIMOL/Crayon Map And HUD/Apply To Selected Gameplay HUD")]
        public static void ApplyToSelectedGameplayHud()
        {
            NotebookGameplayHud hud = ResolveSelected<NotebookGameplayHud>();
            if (!hud.costRect || !hud.jarImage || !hud.pauseButton || !hud.restartImage)
                throw new InvalidOperationException("Select the actual gameplay HUD with its existing cost frame, star slots, pause and telescope restart references.");
            var candidates = new HashSet<Image>();
            AddImages(candidates, hud.costRect);
            AddImages(candidates, hud.headerRect);
            AddImages(candidates, hud.restartRect);
            AddImages(candidates, hud.pauseButton.transform);
            if (hud.jarImage) candidates.Add(hud.jarImage);
            if (hud.restartImage) candidates.Add(hud.restartImage);
            if (hud.flightStar) candidates.Add(hud.flightStar);
            if (hud.flightTrail) candidates.Add(hud.flightTrail);
            AddImages(candidates, hud.flightLayer);
            foreach (Image image in candidates)
                if (image && (!image.transform.IsChildOf(hud.transform) || image.gameObject.scene != hud.gameObject.scene))
                    throw new InvalidOperationException("HUD art references must belong to this HUD hierarchy and Scene or Prefab Stage.");
            Prepared assets = PrepareAssets();
            List<Target> targets = FindTargets(candidates, assets, image =>
                !IsWithin(image.transform, hud.pauseOverlay) && !IsWithin(image.transform, hud.clearOverlay)
                && !IsWithin(image.transform, hud.modalBackdrop)
                && (!hud.costNumber || !image.transform.IsChildOf(hud.costNumber.transform)));
            if (targets.Count == 0) throw new InvalidOperationException("No mapped existing gameplay HUD art was found. No HUD or popup was changed.");
            string hudState = EditorJsonUtility.ToJson(hud);
            Apply(hud.gameObject, null, hud, targets, assets, "Apply Crayon Gameplay HUD Skin", () =>
            {
                if (EditorJsonUtility.ToJson(hud) != hudState)
                    throw new InvalidOperationException("A gameplay HUD data or service reference changed. The skin operation has been rolled back.");
            });
            Debug.Log("Crayon decoration applied only to " + targets.Count
                + " existing corner HUD and UI pickup images. Numeric glyphs, turn values, buttons, retry approval, pause/settings and clear popups were retained. Review and save; Undo reverses this skin operation.", hud);
        }

        private static bool Contains(Transform parent, GameObject child) => child && child.transform.IsChildOf(parent);
        private static bool IsWithin(Transform item, GameObject owner) => owner && item.IsChildOf(owner.transform);
        private static void AddImages(HashSet<Image> images, Transform root)
        {
            if (!root) return;
            foreach (Image image in root.GetComponentsInChildren<Image>(true)) images.Add(image);
        }

        private static List<Target> FindTargets(IEnumerable<Image> images, Prepared assets, Func<Image, bool> allowed)
        {
            var targets = new List<Target>();
            foreach (Image image in images.Where(image => image).Distinct())
            {
                if (!allowed(image)) continue;
                CrayonImageBinding ancestor = image.GetComponentInParent<CrayonImageBinding>();
                if (ancestor && ancestor.decoration == image) continue;
                Sprite sprite = image.overrideSprite ? image.overrideSprite : image.sprite;
                CrayonSpriteSet[] states = assets.Family(sprite);
                if (states.Length == 0) states = assets.Family(image.sprite);
                if (states.Length > 0) targets.Add(new Target { image = image, states = states });
            }
            return targets;
        }

        private static void Apply(GameObject host, NotebookRoot map, NotebookGameplayHud hud, List<Target> targets,
            Prepared assets, string action, Action assertProtectedReferences)
        {
            CrayonSkinScope previousScope = host.GetComponent<CrayonSkinScope>();
            if (previousScope && ((previousScope.mapOwner && previousScope.mapOwner != map)
                || (previousScope.gameplayOwner && previousScope.gameplayOwner != hud)))
                throw new InvalidOperationException("An existing skin scope belongs to another map or HUD.");
            if (previousScope && previousScope.bindings != null)
            {
                var allowedSources = new HashSet<Image>(targets.Select(target => target.image));
                foreach (CrayonImageBinding binding in previousScope.bindings)
                    if (binding && binding.sourceImage && !allowedSources.Contains(binding.sourceImage))
                        throw new InvalidOperationException("The existing skin scope contains an out-of-scope source image. Inspect that binding before reapplying; it was not changed.");
            }
            foreach (Target target in targets)
            {
                CrayonImageBinding existing = target.image.GetComponent<CrayonImageBinding>();
                if (existing && ((existing.scope && existing.scope != previousScope)
                    || (existing.sourceImage && existing.sourceImage != target.image)
                    || (existing.decoration && existing.decoration.transform.parent != target.image.transform)))
                    throw new InvalidOperationException("An existing decoration binding is not owned by this exact map/HUD image.");
            }

            bool dirtyBefore = host.scene.isDirty;
            var created = new List<GameObject>();
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(action);
            try
            {
                CrayonSkinScope scope = previousScope ? previousScope : Undo.AddComponent<CrayonSkinScope>(host);
                Undo.RecordObject(scope, action);
                scope.mapOwner = map; scope.gameplayOwner = hud;
                scope.dynamicFlightRoot = hud ? hud.flightLayer : null;
                scope.dynamicFlightStates = hud ? assets.FlightStates : Array.Empty<CrayonSpriteSet>();
                var bindings = new List<CrayonImageBinding>();
                foreach (Target target in targets)
                {
                    Image source = target.image;
                    Undo.RecordObject(source, action); Undo.RecordObject(source.transform, action);
                    CrayonImageBinding binding = source.GetComponent<CrayonImageBinding>();
                    if (!binding) binding = Undo.AddComponent<CrayonImageBinding>(source.gameObject);
                    Image decoration = binding.decoration;
                    var ownedChildren = source.transform.Cast<Transform>()
                        .Where(t => t.name == "Prepainted Crayon Decoration")
                        .Select(t => t.GetComponent<Image>()).Where(image => image && !image.raycastTarget
                            && image.GetComponent<LayoutElement>() && image.GetComponent<LayoutElement>().ignoreLayout
                            && image.sprite && AssetDatabase.GetAssetPath(image.sprite).StartsWith(Root + "Sprites/", StringComparison.Ordinal))
                        .ToArray();
                    if (!decoration) decoration = ownedChildren.FirstOrDefault();
                    foreach (Image orphan in ownedChildren)
                        if (orphan != decoration) Undo.DestroyObjectImmediate(orphan.gameObject);
                    if (!decoration)
                    {
                        var child = new GameObject("Prepainted Crayon Decoration", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                        child.layer = source.gameObject.layer;
                        if (child.scene != host.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(child, host.scene);
                        child.transform.SetParent(source.transform, false); created.Add(child);
                        // Register creation before recording cross-object references. Redo must
                        // recreate this Image before restoring binding.decoration and scope arrays.
                        Undo.RegisterCreatedObjectUndo(child, action);
                        decoration = child.GetComponent<Image>();
                    }
                    Undo.RecordObject(source, action); Undo.RecordObject(binding, action);
                    Undo.RecordObject(decoration, action); Undo.RecordObject(decoration.rectTransform, action);
                    LayoutElement layout = decoration.GetComponent<LayoutElement>();
                    if (!layout) layout = Undo.AddComponent<LayoutElement>(decoration.gameObject);
                    Undo.RecordObject(layout, action); layout.ignoreLayout = true;
                    RectTransform rect = decoration.rectTransform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity; rect.SetAsFirstSibling();
                    decoration.raycastTarget = false; decoration.maskable = source.maskable;
                    binding.Configure(source, decoration, target.states, scope);
                    bindings.Add(binding);
                    RecordOverride(source); RecordOverride(source.transform); RecordOverride(binding);
                    RecordOverride(decoration); RecordOverride(rect); RecordOverride(layout);
                    EditorUtility.SetDirty(binding); EditorUtility.SetDirty(source); EditorUtility.SetDirty(decoration);
                }
                Undo.RecordObject(scope, action);
                scope.bindings = bindings.ToArray(); scope.RefreshBindings(); scope.RefreshNow();
                assertProtectedReferences();
                Undo.FlushUndoRecordObjects();
                RecordOverride(scope); EditorUtility.SetDirty(scope);
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(host.scene);
                Selection.activeGameObject = map ? map.gameObject : hud.gameObject;
            }
            catch
            {
                try { Undo.RevertAllDownToGroup(group); }
                finally
                {
                    foreach (GameObject child in created) if (child) UnityEngine.Object.DestroyImmediate(child);
                    if (dirtyBefore) EditorSceneManager.MarkSceneDirty(host.scene);
                }
                throw;
            }
        }

        private static T ResolveSelected<T>() where T : Component
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying a crayon skin.");
            if (Selection.gameObjects.Length != 1 || !Selection.activeGameObject)
                throw new InvalidOperationException("Select exactly one actual map/HUD object, parent or child.");
            GameObject selected = Selection.activeGameObject;
            if (EditorUtility.IsPersistent(selected))
                throw new InvalidOperationException("Open the Prefab in Prefab Mode instead of applying directly to its Project asset.");
            var scene = selected.scene; var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (!scene.IsValid() || !scene.isLoaded || (EditorSceneManager.IsPreviewScene(scene) && (stage == null || stage.scene != scene)))
                throw new InvalidOperationException("The selected object must belong to a loaded Scene or the current Prefab Stage.");
            var candidates = new HashSet<T>(selected.GetComponentsInParent<T>(true));
            foreach (T item in selected.GetComponentsInChildren<T>(true)) candidates.Add(item);
            if (candidates.Count != 1)
                throw new InvalidOperationException("The selection must resolve to exactly one " + typeof(T).Name + "; no Scene-wide search is performed.");
            return candidates.First();
        }

        private static Prepared PrepareAssets()
        {
            if (!File.Exists(ManifestPath)) throw new InvalidOperationException("Missing crayon sprite manifest: " + ManifestPath);
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.entries == null || manifest.entries.Length == 0)
                throw new InvalidOperationException("The crayon sprite manifest has no finished asset entries.");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!shader) throw new InvalidOperationException("Import the dedicated crayon frame shader first: " + ShaderPath);
            EnsureFolder(Materials.TrimEnd('/'));
            var prepared = new Prepared();
            foreach (Entry entry in manifest.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.frames == null || entry.frames.Length != 3
                    || entry.originalPaths == null || entry.width <= 0 || entry.height <= 0 || entry.border < 0)
                    throw new InvalidOperationException("Invalid crayon A/B/C entry in manifest.");
                Sprite[] frames = entry.frames.Select(path => ImportSprite(path, entry)).ToArray();
                Material animated = EnsureMaterial(entry.id, shader, frames, true);
                Material still = EnsureMaterial(entry.id, shader, frames, false);
                string family = FamilyOf(entry.id);
                if (!prepared.setsByFamily.TryGetValue(family, out List<CrayonSpriteSet> familySets))
                    prepared.setsByFamily[family] = familySets = new List<CrayonSpriteSet>();
                foreach (string sourcePath in entry.originalPaths)
                {
                    Sprite original = LoadOriginalSprite(sourcePath);
                    if (!original) continue;
                    if (prepared.familyBySource.ContainsKey(original))
                        throw new InvalidOperationException("A source sprite is mapped twice: " + sourcePath);
                    var state = new CrayonSpriteSet { source = original, frameA = frames[0], frameB = frames[1], frameC = frames[2], animatedMaterial = animated, stillMaterial = still };
                    prepared.familyBySource.Add(original, family); familySets.Add(state);
                }
            }
            return prepared;
        }

        private static Sprite LoadOriginalSprite(string sourcePath)
        {
            Sprite original = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
            if (original) return original;
            const string previousRoot = "Assets/WilliamNotebookUI/Textures/";
            if (!sourcePath.StartsWith(previousRoot, StringComparison.Ordinal)) return null;
            string suffix = "/Textures/" + sourcePath.Substring(previousRoot.Length);
            string[] matches = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(sourcePath) + " t:Sprite",
                    new[] { "Assets/ANIMOL/Art/UI" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(suffix, StringComparison.Ordinal)).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("Ambiguous relocated source sprite: " + sourcePath);
            return matches.Length == 1 ? AssetDatabase.LoadAssetAtPath<Sprite>(matches[0]) : null;
        }

        private static string FamilyOf(string id)
        {
            if (id.StartsWith("Map/Stars/", StringComparison.Ordinal)) return "MapStars";
            if (id.StartsWith("MapNeon/Icon_", StringComparison.Ordinal)) return "MapIcons";
            if (id.StartsWith("MapTheme/", StringComparison.Ordinal)) return "MapTheme";
            if (id.StartsWith("Gameplay/StarDock_", StringComparison.Ordinal)) return "StarDock";
            if (id.StartsWith("Gameplay/TelescopeRetry", StringComparison.Ordinal)) return "RetryGauge";
            if (id.StartsWith("Gameplay/PauseOrnament_", StringComparison.Ordinal)) return "PauseOrnament";
            if (id.StartsWith("Gameplay/CollectibleStar_", StringComparison.Ordinal)) return "FlightStar";
            if (id.StartsWith("Gameplay/Trail_", StringComparison.Ordinal)) return "FlightTrail";
            string[] states = { "_Normal", "_Highlighted", "_Pressed", "_Disabled", "_Selected" };
            foreach (string state in states)
                if (id.EndsWith(state, StringComparison.Ordinal)) return id.Substring(0, id.Length - state.Length);
            return id;
        }

        private static Sprite ImportSprite(string path, Entry entry)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(Root + "Sprites/", StringComparison.Ordinal)
                || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Missing finished crayon sprite: " + path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (!importer) throw new InvalidOperationException("Cannot import crayon texture: " + path);
            var previousSettings = new TextureImporterSettings(); importer.ReadTextureSettings(previousSettings);
            Sprite imported = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (imported && importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && previousSettings.spriteMeshType == SpriteMeshType.FullRect && importer.spritePixelsPerUnit == 100
                && importer.spriteBorder == Vector4.one * entry.border && importer.alphaIsTransparency && !importer.mipmapEnabled
                && !importer.isReadable && importer.npotScale == TextureImporterNPOTScale.None
                && importer.filterMode == FilterMode.Bilinear && importer.wrapMode == TextureWrapMode.Clamp
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && Mathf.RoundToInt(imported.rect.width) == entry.width && Mathf.RoundToInt(imported.rect.height) == entry.height)
                return imported;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.spritePivot = new Vector2(.5f, .5f);
            importer.spriteBorder = Vector4.one * entry.border; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || Mathf.RoundToInt(sprite.rect.width) != entry.width || Mathf.RoundToInt(sprite.rect.height) != entry.height)
                throw new InvalidOperationException("Sprite size does not match its A/B/C manifest: " + path);
            return sprite;
        }

        private static Material EnsureMaterial(string id, Shader shader, Sprite[] frames, bool animated)
        {
            string name = id.Replace('/', '_').Replace(' ', '_') + (animated ? "_Animated" : "_Still");
            string path = Materials + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material && material.shader != shader)
                throw new InvalidOperationException("A foreign material occupies the dedicated crayon path: " + path);
            if (material && material.GetTexture("_MainTex") == frames[0].texture
                && material.GetTexture("_FrameB") == frames[1].texture && material.GetTexture("_FrameC") == frames[2].texture
                && material.GetFloat("_Motion") == (animated ? 1 : 0)
                && Mathf.Abs(material.GetFloat("_FrameRate") - 1.666667f) < .000001f
                && material.GetColor("_Color") == Color.white)
                return material;
            if (!material)
            {
                material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_MainTex", frames[0].texture);
            material.SetTexture("_FrameB", frames[1].texture); material.SetTexture("_FrameC", frames[2].texture);
            material.SetFloat("_Motion", animated ? 1 : 0); material.SetFloat("_FrameRate", 1.666667f);
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void RecordOverride(UnityEngine.Object item)
        {
            if (item && PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
    }
}
#endif
