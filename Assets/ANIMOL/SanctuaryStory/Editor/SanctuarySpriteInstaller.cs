#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory.Editor
{
    /// <summary>
    /// Explicit, package-local import and installation. This is deliberately not an
    /// AssetPostprocessor: unrelated project textures and SC18 UI stay under their owners.
    /// </summary>
    public static class SanctuarySpriteInstaller
    {
        private const string Root = "Assets/ANIMOL/SanctuaryStory";
        private const string ArtRoot = Root + "/Art";
        private const string ManifestPath = ArtRoot + "/ART_BINDINGS.json";
        private const string PresentationPath = Root + "/Data/sanctuary_presentation_v1.json";
        private const string ChapterPath = Root + "/Data/sanctuary_chapter_v1.json";
        private const string GeneratedRoot = Root + "/Generated";
        private const string CatalogPath = GeneratedRoot + "/SanctuaryVisualCatalog.asset";
        private const string CatalogName = "SanctuaryVisualCatalog_GENERATED";
        private const string CatalogOwnerLabel = "ANIMOL_SanctuarySpriteInstaller_v1";
        private const string StageName = "SanctuarySpriteStage";
        private static readonly string[] CharacterIds = { "doyun", "sein", "daon", "lumi", "watcher" };
        private static readonly HashSet<string> AllowedPoses = new HashSet<string>(StringComparer.Ordinal)
        {
            "portrait", "idle", "move", "jump", "prepare", "lower", "boundary", "expression"
        };

        [Serializable]
        private sealed class ArtManifest
        {
            public int formatVersion;
            public ArtBinding[] sprites;
            public string laboratoryBackground;
            public string chaosBackground;
            public ArtBackgroundBinding[] backgrounds;
        }

        [Serializable]
        private sealed class ArtBackgroundBinding
        {
            public string id;
            public string path;
        }

        [Serializable]
        private sealed class ArtBinding
        {
            public string path;
            public string actorId;
            public string pose;
            public string usage;
            public int order;
            public bool pixelArt;
            public float pixelsPerUnit;
            public bool hasCustomPivot;
            public float pivotX;
            public float pivotY;
            public string variantId;
            public bool hasContentBounds;
            public float contentMinX, contentMinY, contentMaxX, contentMaxY;
            public bool hasRegistrationX;
            public float registrationX;
        }

        [MenuItem("ANIMOL/Sanctuary Story/Install Sprite Stage Under Selection")]
        public static void InstallUnderSelection()
        {
            try
            {
                RectTransform parent = SelectedParent();
                SanctuaryStoryRunner runner = FindRunner(parent);
                SanctuarySpriteStage existingStage = FindOwnedStage(parent);
                Transform earliestUiBranch = FindUiInsertionBranch(parent, runner);
                ValidateExistingCatalogOwnership();
                ArtManifest manifest = ReadManifest();
                // Resolve every source and every required clip BEFORE changing imports or scene objects.
                ValidateSources(manifest);
                ValidateRequiredBindings(manifest);
                SanctuaryPresentationData presentation;
                SanctuaryChapter chapter;
                TextAsset presentationSource = ReadPresentation(out presentation, out chapter);
                SanctuaryPresentationValidator.Validate(presentation, chapter, null);
                ValidatePresentationManifestReferences(presentation, manifest);
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Install Sanctuary Sprite Stage");
                ConfigureImports(manifest);
                SanctuaryCharacterVisual[] characters = BuildCharacters(manifest);
                Sprite laboratory = LoadSprite(manifest.laboratoryBackground);
                Sprite chaos = LoadSprite(manifest.chaosBackground);

                SanctuaryBackgroundVisual[] backgrounds = BuildBackgrounds(manifest);
                var validationCatalog = ScriptableObject.CreateInstance<SanctuaryVisualCatalog>();
                try
                {
                    validationCatalog.characters = characters;
                    validationCatalog.laboratoryBackground = laboratory;
                    validationCatalog.chaosBackground = chaos;
                    validationCatalog.backgroundVariants = backgrounds;
                    SanctuaryPresentationValidator.Validate(presentation, chapter, validationCatalog);
                }
                finally { UnityEngine.Object.DestroyImmediate(validationCatalog); }
                SanctuaryVisualCatalog catalog = SaveCatalog(characters, laboratory, chaos, backgrounds);
                SanctuarySpriteStage stage = existingStage;
                if (stage == null)
                {
                    var child = new GameObject(StageName, typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(child, "Create Sanctuary Sprite Stage");
                    Undo.SetTransformParent(child.transform, parent, "Parent Sanctuary Sprite Stage");
                    stage = Undo.AddComponent<SanctuarySpriteStage>(child);
                }
                else Undo.RegisterFullObjectHierarchyUndo(stage.gameObject, "Update Sanctuary Sprite Stage");

                var before = new HashSet<GameObject>();
                foreach (Transform child in stage.GetComponentsInChildren<Transform>(true)) before.Add(child.gameObject);
                RectTransform rect = (RectTransform)stage.transform;
                Undo.RecordObject(rect, "Layout Sanctuary Sprite Stage");
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                // Above the existing art/background, immediately before the earliest bound UI branch.
                // Only this owned child's order changes; unowned siblings keep their relative order.
                rect.SetAsLastSibling();
                if (earliestUiBranch != null) rect.SetSiblingIndex(earliestUiBranch.GetSiblingIndex());
                Undo.RecordObject(stage, "Bind Sanctuary Sprite Stage");
                stage.Configure(runner, catalog, presentationSource);
                foreach (Transform child in stage.GetComponentsInChildren<Transform>(true))
                    if (!before.Contains(child.gameObject))
                        Undo.RegisterCreatedObjectUndo(child.gameObject, "Create Sanctuary Stage Visual");
                EditorUtility.SetDirty(stage);
                EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                ValidateCatalog(catalog, manifest);
                Debug.Log("[Sanctuary Sprites] Installed under '" + parent.name + "'. " +
                    manifest.sprites.Length + " source character/pose PNGs imported + " + manifest.backgrounds.Length + " backgrounds; " +
                    "5 story actors and explicit per-line expressions reference imported Sprites. InGame sprites remain separate from story actors. Existing Canvas, EventSystem, dialogue, " +
                    "choices and project save systems are reused. Save the scene, then verify in Unity Play Mode. " +
                    "No Unity/device playback is claimed by this installer.", stage);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Sanctuary Sprites] Installation stopped: " + exception.Message);
            }
        }

        [MenuItem("ANIMOL/Sanctuary Story/Validate Sprite Assets")]
        public static void ValidateSpriteAssets()
        {
            try
            {
                ArtManifest manifest = ReadManifest();
                ValidateSources(manifest);
                ValidateRequiredBindings(manifest);
                ValidateImportSettings(manifest);
                SanctuaryVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<SanctuaryVisualCatalog>(CatalogPath);
                if (catalog == null)
                    throw new InvalidOperationException("Generated catalog is missing. Select the existing SC18 safe-area parent and run Install Sprite Stage Under Selection first.");
                ValidateCatalog(catalog, manifest);
                SanctuaryPresentationData presentation;
                SanctuaryChapter chapter;
                ReadPresentation(out presentation, out chapter);
                SanctuaryPresentationValidator.Validate(presentation, chapter, catalog);
                ValidatePresentationManifestReferences(presentation, manifest);
                int references = CountSpriteReferences(catalog);
                Debug.Log("[Sanctuary Sprites] Asset validation passed: " + manifest.sprites.Length +
                    " listed character/pose PNGs + " + manifest.backgrounds.Length + " backgrounds, 5 story actor bindings, " + references +
                    " assigned Sprite references, no missing required frame. This checks imported assets and " +
                    "references only; run Play Mode and the acceptance checklist for scene/device behavior.", catalog);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Sanctuary Sprites] Asset validation failed: " + exception.Message);
            }
        }

        private static RectTransform SelectedParent()
        {
            GameObject selected = Selection.activeGameObject;
            RectTransform parent = selected != null ? selected.GetComponent<RectTransform>() : null;
            if (parent == null || EditorUtility.IsPersistent(selected) || !selected.scene.IsValid() || !selected.scene.isLoaded)
                throw new InvalidOperationException("Select an existing scene RectTransform under the SC18 Canvas (normally its safe-area art parent), not a Project prefab asset.");
            if (parent.GetComponentInParent<Canvas>(true) == null)
                throw new InvalidOperationException("The selected RectTransform must already be inside the existing SC18 Canvas. No new Canvas or EventSystem will be created.");
            if (parent.GetComponentInParent<SanctuarySpriteStage>(true) != null)
                throw new InvalidOperationException("Select the existing parent of SanctuarySpriteStage, rather than the stage or one of its generated children.");
            return parent;
        }

        private static SanctuaryStoryRunner FindRunner(RectTransform parent)
        {
            SanctuaryStoryRunner[] ancestors = parent.GetComponentsInParent<SanctuaryStoryRunner>(true);
            if (ancestors.Length > 1)
                throw new InvalidOperationException("More than one SanctuaryStoryRunner exists on the selected ancestor chain. Choose a parent with one unambiguous owner.");
            if (ancestors.Length == 1) return ancestors[0];
            SanctuaryStoryRunner[] descendants = parent.GetComponentsInChildren<SanctuaryStoryRunner>(true);
            if (descendants.Length != 1)
                throw new InvalidOperationException("The selected parent must contain exactly one SanctuaryStoryRunner, or have one on its ancestor chain. Found " + descendants.Length + " beneath the selection. No global runner was guessed.");
            return descendants[0];
        }

        private static SanctuarySpriteStage FindOwnedStage(RectTransform parent)
        {
            SanctuarySpriteStage result = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!string.Equals(child.name, StageName, StringComparison.Ordinal)) continue;
                SanctuarySpriteStage candidate = child.GetComponent<SanctuarySpriteStage>();
                if (candidate == null || !(child is RectTransform))
                    throw new InvalidOperationException("A child named '" + StageName + "' already exists without the stage ownership component. Rename it or select another parent; it will not be overwritten.");
                if (result != null)
                    throw new InvalidOperationException("Two owned children named '" + StageName + "' exist. Resolve the duplicate before installing.");
                result = candidate;
            }
            return result;
        }

        private static Transform FindUiInsertionBranch(RectTransform parent, SanctuaryStoryRunner runner)
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>(true);
            var uiRoots = new List<Transform>();
            int matchingViews = 0;
            foreach (SanctuaryUGUIView view in canvas.GetComponentsInChildren<SanctuaryUGUIView>(true))
            {
                var serialized = new SerializedObject(view);
                SerializedProperty runnerProperty = serialized.FindProperty("runner");
                if (runnerProperty == null || runnerProperty.objectReferenceValue != runner) continue;
                matchingViews++;
                if (!HasComponentReference(serialized, "dialogueGroup") || !HasComponentReference(serialized, "choicesGroup"))
                    throw new InvalidOperationException("The matching SanctuaryUGUIView must have its existing dialogueGroup and choicesGroup bound before safe art installation. Complete those SC18 references first.");
                AddUiRoot(serialized, "dialogueGroup", uiRoots);
                AddUiRoot(serialized, "choicesGroup", uiRoots);
                AddUiRoot(serialized, "choicesRoot", uiRoots);
                AddUiRoot(serialized, "endingTitleLabel", uiRoots);
            }
            if (matchingViews == 0)
                throw new InvalidOperationException("No SanctuaryUGUIView in this existing Canvas is bound to the selected Runner. Bind the existing SC18 View to that Runner and its dialogue/choice groups before installing, so the art can be placed safely.");

            var branches = new HashSet<Transform>();
            Transform earliest = null;
            foreach (Transform uiRoot in uiRoots)
            {
                if (uiRoot == parent || parent.IsChildOf(uiRoot))
                    throw new InvalidOperationException("The selected parent is inside a bound dialogue/choice/title group. Select its existing sibling art parent or a shared parent above those groups.");
                if (!uiRoot.IsChildOf(parent))
                {
                    ValidateOutsideUiOrder(parent, uiRoot);
                    continue;
                }
                Transform branch = uiRoot;
                while (branch.parent != parent) branch = branch.parent;
                branches.Add(branch);
                if (earliest == null || branch.GetSiblingIndex() < earliest.GetSiblingIndex()) earliest = branch;
            }

            foreach (Transform branch in branches)
                foreach (Image image in branch.GetComponentsInChildren<Image>(true))
                {
                    bool isUi = false;
                    foreach (Transform uiRoot in uiRoots)
                        if (image.transform == uiRoot || image.transform.IsChildOf(uiRoot)) { isUi = true; break; }
                    if (!isUi && IsLargeOpaqueImage(image, parent))
                        throw new InvalidOperationException("A bound UI branch also contains a large opaque background ('" + image.name + "'). A stage outside that branch could be hidden. Select or create a dedicated art RectTransform inside that branch, beneath dialogue/choices, then install there.");
                }
            // No bound UI is inside a dedicated art parent: its last sibling position is above its background.
            return earliest;
        }

        private static bool HasComponentReference(SerializedObject view, string field)
        {
            SerializedProperty property = view.FindProperty(field);
            return property != null && property.objectReferenceValue is Component;
        }

        private static void ValidateOutsideUiOrder(RectTransform parent, Transform uiRoot)
        {
            Transform common = parent.parent;
            while (common != null && uiRoot != common && !uiRoot.IsChildOf(common)) common = common.parent;
            if (common == null)
                throw new InvalidOperationException("A bound SC18 UI group is outside this Canvas hierarchy. Select the existing shared art/UI hierarchy before installing.");
            Transform artBranch = parent;
            while (artBranch.parent != common) artBranch = artBranch.parent;
            Transform uiBranch = uiRoot;
            while (uiBranch.parent != common) uiBranch = uiBranch.parent;
            if (artBranch.GetSiblingIndex() > uiBranch.GetSiblingIndex())
                throw new InvalidOperationException("The selected art parent renders above an existing dialogue/choice/title branch. Select their shared RectTransform parent so the owned stage can be inserted before UI, or choose an existing art parent that already renders behind UI. Unowned branches will not be reordered.");
        }

        private static void AddUiRoot(SerializedObject view, string field, List<Transform> roots)
        {
            SerializedProperty property = view.FindProperty(field);
            Component component = property == null ? null : property.objectReferenceValue as Component;
            if (component != null && !roots.Contains(component.transform)) roots.Add(component.transform);
        }

        private static bool IsLargeOpaqueImage(Image image, RectTransform parent)
        {
            if (!image.enabled || image.color.a < .99f) return false;
            RectTransform rect = image.rectTransform;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float left = float.PositiveInfinity, right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            foreach (Vector3 corner in corners)
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                left = Mathf.Min(left, point.x); right = Mathf.Max(right, point.x);
                bottom = Mathf.Min(bottom, point.y); top = Mathf.Max(top, point.y);
            }
            if (parent.rect.width <= 1f || parent.rect.height <= 1f)
                return rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
            return right - left >= parent.rect.width * .8f && top - bottom >= parent.rect.height * .8f;
        }

        private static ArtManifest ReadManifest()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            if (source == null)
                throw new InvalidOperationException("Missing " + ManifestPath + ". Copy the entire package Art folder, including actual PNG files and ART_BINDINGS.json, into the Unity project.");
            ArtManifest manifest = JsonUtility.FromJson<ArtManifest>(source.text);
            if (manifest == null || manifest.formatVersion != 1 || manifest.sprites == null || manifest.sprites.Length == 0 ||
                manifest.backgrounds == null || manifest.backgrounds.Length == 0)
                throw new InvalidOperationException("ART_BINDINGS.json requires formatVersion 1, explicit sprite bindings and v4 background bindings.");
            return manifest;
        }

        private static TextAsset ReadPresentation(out SanctuaryPresentationData presentation, out SanctuaryChapter chapter)
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(PresentationPath);
            TextAsset chapterSource = AssetDatabase.LoadAssetAtPath<TextAsset>(ChapterPath);
            if (source == null || chapterSource == null)
                throw new InvalidOperationException("Copy both chapter and detailed presentation JSON files under " + Root + "/Data before installing.");
            presentation = JsonUtility.FromJson<SanctuaryPresentationData>(source.text);
            chapter = JsonUtility.FromJson<SanctuaryChapter>(chapterSource.text);
            return source;
        }

        private static void ValidatePresentationManifestReferences(SanctuaryPresentationData presentation, ArtManifest manifest)
        {
            var backgrounds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ArtBackgroundBinding background in manifest.backgrounds) backgrounds.Add(background.id);
            foreach (var scene in presentation.scenes)
            {
                if (!backgrounds.Contains(scene.backgroundId))
                    throw new InvalidOperationException("Presentation background lacks an explicit PNG binding: " + scene.backgroundId);
                foreach (var actor in scene.actors)
                    RequireVariant(manifest, actor.actorId, actor.variantId);
            }
            foreach (var line in presentation.lines)
                foreach (var actor in line.actors)
                    RequireVariant(manifest, actor.actorId, actor.variantId);
        }

        private static void RequireVariant(ArtManifest manifest, string actorId, string variantId)
        {
            if (variantId == "neutral" || variantId == "idle") { RequireClip(manifest, actorId, "idle"); return; }
            if (variantId == "prepare" && actorId == "watcher") { RequireClip(manifest, actorId, "prepare"); return; }
            foreach (ArtBinding binding in manifest.sprites)
                if (IsStory(binding) && binding.actorId == actorId && binding.pose == "expression" && binding.variantId == variantId) return;
            throw new InvalidOperationException("Presentation expression lacks an explicit story PNG binding: " + actorId + "/" + variantId);
        }

        private static void ValidateSources(ArtManifest manifest)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (ArtBinding binding in manifest.sprites)
            {
                if (binding == null) throw new InvalidOperationException("Manifest contains an empty sprite binding.");
                ValidateSourcePath(binding.path);
                if (!paths.Add(binding.path)) throw new InvalidOperationException("Manifest repeats a source PNG path: " + binding.path);
                if (Array.IndexOf(CharacterIds, binding.actorId) < 0 || !AllowedPoses.Contains(binding.pose ?? string.Empty))
                    throw new InvalidOperationException("Unknown actor/pose binding: " + binding.actorId + "/" + binding.pose);
                if (!string.IsNullOrEmpty(binding.usage) && binding.usage != "story" && binding.usage != "ingame")
                    throw new InvalidOperationException("Unknown usage for " + binding.path + ": " + binding.usage);
                if (binding.order < 0 || binding.pixelsPerUnit <= 0f || float.IsNaN(binding.pixelsPerUnit) || float.IsInfinity(binding.pixelsPerUnit))
                    throw new InvalidOperationException("Frame order and pixelsPerUnit are invalid: " + binding.path);
                if (binding.hasCustomPivot && (!IsNormalizedFinite(binding.pivotX) || !IsNormalizedFinite(binding.pivotY)))
                    throw new InvalidOperationException("Custom source pivot must contain finite normalized coordinates from 0 to 1: " + binding.path);
                if (binding.pose == "expression" && (!IsStory(binding) || string.IsNullOrWhiteSpace(binding.variantId) || binding.order != 0))
                    throw new InvalidOperationException("Expressions require a story usage, explicit variantId and order 0: " + binding.path);
                if (binding.hasRegistrationX && !IsNormalizedFinite(binding.registrationX))
                    throw new InvalidOperationException("Sprite registration must be a finite normalized X coordinate: " + binding.path);
                if (binding.hasContentBounds && (!IsNormalizedFinite(binding.contentMinX) || !IsNormalizedFinite(binding.contentMinY) ||
                    !IsNormalizedFinite(binding.contentMaxX) || !IsNormalizedFinite(binding.contentMaxY) ||
                    binding.contentMinX >= binding.contentMaxX || binding.contentMinY >= binding.contentMaxY))
                    throw new InvalidOperationException("Content bounds must be finite normalized, nonempty Unity bottom-origin coordinates: " + binding.path);
                string poseSlot = binding.pose == "expression" ? "expression/" + binding.variantId : binding.pose;
                if (!slots.Add((IsStory(binding) ? "story" : "ingame") + "/" + binding.actorId + "/" + poseSlot + "/" + binding.order))
                    throw new InvalidOperationException("Two PNGs occupy the same actor/pose/frame slot: " + binding.actorId + "/" + binding.pose + "/" + binding.order);
            }
            var backgroundIds = new HashSet<string>(StringComparer.Ordinal);
            string labPath = null, chaosPath = null;
            foreach (ArtBackgroundBinding background in manifest.backgrounds)
            {
                if (background == null || string.IsNullOrWhiteSpace(background.id) || !backgroundIds.Add(background.id))
                    throw new InvalidOperationException("Every background requires one unique explicit id.");
                ValidateSourcePath(background.path);
                if (!paths.Add(background.path))
                    throw new InvalidOperationException("Background source paths must be separate and cannot duplicate character PNGs: " + background.path);
                if (background.id == "lab") labPath = background.path;
                if (background.id == "chaos") chaosPath = background.path;
            }
            if (labPath != manifest.laboratoryBackground || chaosPath != manifest.chaosBackground || labPath == null || chaosPath == null)
                throw new InvalidOperationException("Background ids lab/chaos must match laboratoryBackground/chaosBackground aliases.");
        }

        private static void ValidateSourcePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(ArtRoot + "/", StringComparison.Ordinal) ||
                path.IndexOf("..", StringComparison.Ordinal) >= 0 || path.IndexOf('\\') >= 0 ||
                !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source must be an explicit PNG under " + ArtRoot + ": " + path);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter))
                throw new InvalidOperationException("Missing or unimported source PNG: " + path + ". Copy actual PNGs, not only reference documents, then let Unity finish importing.");
        }

        private static void ValidateRequiredBindings(ArtManifest manifest)
        {
            foreach (string id in CharacterIds)
            {
                RequireClip(manifest, id, "idle");
                if (id == "watcher") RequireClip(manifest, id, "prepare");
            }
            foreach (string id in CharacterIds)
                foreach (string pose in AllowedPoses)
                {
                    if (pose == "expression") continue;
                    List<ArtBinding> frames = Frames(manifest, id, pose);
                    if (pose == "portrait" && frames.Count > 1)
                        throw new InvalidOperationException(id + " has more than one portrait binding.");
                    for (int i = 0; i < frames.Count; i++)
                        if (frames[i].order != i)
                            throw new InvalidOperationException(id + "/" + pose + " frame order must begin at 0 and have no gaps.");
                    List<ArtBinding> gameFrames = Frames(manifest, id, pose, false);
                    for (int i = 0; i < gameFrames.Count; i++)
                        if (gameFrames[i].order != i)
                            throw new InvalidOperationException("InGame " + id + "/" + pose + " frame order must begin at 0 and have no gaps.");
                }
        }

        private static void RequireClip(ArtManifest manifest, string id, string pose)
        {
            if (Frames(manifest, id, pose).Count == 0)
                throw new InvalidOperationException("Required real sprite pose is missing: " + id + "/" + pose + ". The installer does not fabricate or substitute missing artwork.");
        }

        private static bool IsStory(ArtBinding binding)
        {
            return string.IsNullOrEmpty(binding.usage) || binding.usage == "story";
        }

        private static bool IsNormalizedFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }

        private static List<ArtBinding> Frames(ArtManifest manifest, string id, string pose, bool story = true)
        {
            var result = new List<ArtBinding>();
            foreach (ArtBinding binding in manifest.sprites)
                if (binding.actorId == id && binding.pose == pose && IsStory(binding) == story) result.Add(binding);
            result.Sort((a, b) => a.order.CompareTo(b.order));
            return result;
        }

        private static void ConfigureImports(ArtManifest manifest)
        {
            foreach (ArtBinding binding in manifest.sprites)
                ConfigureImport(binding.path, binding.pixelArt, binding.pixelsPerUnit,
                    binding.hasCustomPivot, binding.pivotX, binding.pivotY);
            foreach (ArtBackgroundBinding background in manifest.backgrounds)
                ConfigureImport(background.path, false, 100f);
        }

        private static void ConfigureImport(string path, bool pixelArt, float pixelsPerUnit,
            bool hasCustomPivot = false, float pivotX = 0f, float pivotY = 0f)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Undo.RecordObject(importer, "Import Sanctuary Sprite");
            importer.textureType = TextureImporterType.Sprite;
            // Every manifest entry is an already separate PNG. Never grid-slice source frames.
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = pixelArt ? FilterMode.Point : FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (hasCustomPivot)
            {
                // Native frame PPU and custom pivot keep the approved animation registered.
                // Do not replace them with a common cell size or resample the source PNG.
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(pivotX, pivotY);
            }
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("No imported Sprite at " + path + ". The actual PNG must import as Sprite (2D and UI), Single.");
            return sprite;
        }

        private static Sprite[] LoadFrames(ArtManifest manifest, string id, string pose)
        {
            List<ArtBinding> bindings = Frames(manifest, id, pose);
            var sprites = new Sprite[bindings.Count];
            for (int i = 0; i < bindings.Count; i++) sprites[i] = LoadSprite(bindings[i].path);
            return sprites;
        }

        private static SanctuaryCharacterVisual[] BuildCharacters(ArtManifest manifest)
        {
            var result = new SanctuaryCharacterVisual[CharacterIds.Length];
            for (int i = 0; i < CharacterIds.Length; i++)
            {
                string id = CharacterIds[i];
                Sprite[] portrait = LoadFrames(manifest, id, "portrait");
                result[i] = new SanctuaryCharacterVisual
                {
                    characterId = id,
                    portrait = portrait.Length > 0 ? portrait[0] : LoadFrames(manifest, id, "idle")[0],
                    idleFrames = LoadFrames(manifest, id, "idle"),
                    moveFrames = LoadFrames(manifest, id, "move"),
                    jumpFrames = LoadFrames(manifest, id, "jump"),
                    prepareFrames = LoadFrames(manifest, id, "prepare"),
                    lowerFrames = LoadFrames(manifest, id, "lower"),
                    boundaryFrames = LoadFrames(manifest, id, "boundary"),
                    expressionVariants = BuildExpressions(manifest, id),
                    framesPerSecond = 8f,
                    designHeight = 480f
                };
            }
            return result;
        }

        private static SanctuaryExpressionVisual[] BuildExpressions(ArtManifest manifest, string id)
        {
            var result = new List<SanctuaryExpressionVisual>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            List<ArtBinding> idle = Frames(manifest, id, "idle");
            if (idle.Count > 0) { result.Add(Expression("neutral", idle[0])); ids.Add("neutral"); }
            List<ArtBinding> prepare = Frames(manifest, id, "prepare");
            if (id == "watcher" && prepare.Count > 0) { result.Add(Expression("prepare", prepare[0])); ids.Add("prepare"); }
            foreach (ArtBinding binding in manifest.sprites)
            {
                if (!IsStory(binding) || binding.actorId != id || binding.pose != "expression") continue;
                if (!ids.Add(binding.variantId))
                    throw new InvalidOperationException("Expression repeats a reserved or existing id: " + id + "/" + binding.variantId);
                result.Add(Expression(binding.variantId, binding));
            }
            return result.ToArray();
        }

        private static SanctuaryExpressionVisual Expression(string variantId, ArtBinding binding)
        {
            return new SanctuaryExpressionVisual
            {
                id = variantId,
                sprite = LoadSprite(binding.path),
                hasContentBounds = binding.hasContentBounds,
                contentMinX = binding.contentMinX,
                contentMinY = binding.contentMinY,
                contentMaxX = binding.contentMaxX,
                contentMaxY = binding.contentMaxY,
                hasRegistrationX = binding.hasRegistrationX,
                registrationX = binding.registrationX
            };
        }

        private static SanctuaryBackgroundVisual[] BuildBackgrounds(ArtManifest manifest)
        {
            var result = new SanctuaryBackgroundVisual[manifest.backgrounds.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new SanctuaryBackgroundVisual { id = manifest.backgrounds[i].id, sprite = LoadSprite(manifest.backgrounds[i].path) };
            return result;
        }

        private static void ValidateExistingCatalogOwnership()
        {
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(CatalogPath);
            if (existing != null && (!(existing is SanctuaryVisualCatalog) ||
                Array.IndexOf(AssetDatabase.GetLabels(existing), CatalogOwnerLabel) < 0))
                throw new InvalidOperationException("An asset already exists at " + CatalogPath + " without this installer's generated identity. It will not be overwritten.");
        }

        private static SanctuaryVisualCatalog SaveCatalog(SanctuaryCharacterVisual[] characters, Sprite laboratory, Sprite chaos, SanctuaryBackgroundVisual[] backgrounds)
        {
            ValidateExistingCatalogOwnership();
            if (!AssetDatabase.IsValidFolder(GeneratedRoot)) AssetDatabase.CreateFolder(Root, "Generated");
            SanctuaryVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<SanctuaryVisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SanctuaryVisualCatalog>();
                catalog.name = CatalogName;
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                AssetDatabase.SetLabels(catalog, new[] { CatalogOwnerLabel });
                Undo.RegisterCreatedObjectUndo(catalog, "Create Sanctuary Visual Catalog");
            }
            else Undo.RecordObject(catalog, "Update Sanctuary Visual Catalog");
            catalog.characters = characters;
            catalog.laboratoryBackground = laboratory;
            catalog.chaosBackground = chaos;
            catalog.backgroundVariants = backgrounds;
            // Audio references, if the project's owner assigned them, are deliberately preserved.
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static void ValidateImportSettings(ArtManifest manifest)
        {
            foreach (ArtBinding binding in manifest.sprites)
                ValidateImport(binding.path, binding.pixelArt, binding.pixelsPerUnit,
                    binding.hasCustomPivot, binding.pivotX, binding.pivotY);
            foreach (ArtBackgroundBinding background in manifest.backgrounds)
                ValidateImport(background.path, false, 100f);
        }

        private static void ValidateImport(string path, bool pixelArt, float pixelsPerUnit,
            bool hasCustomPivot = false, float pivotX = 0f, float pivotY = 0f)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) ||
                importer.filterMode != (pixelArt ? FilterMode.Point : FilterMode.Bilinear) ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled ||
                !importer.alphaIsTransparency || importer.alphaSource != TextureImporterAlphaSource.FromInput)
                throw new InvalidOperationException("Import settings differ from the package binding: " + path + ". Re-run Install Sprite Stage Under Selection to apply scoped Sprite Single/alpha/filter/PPU/uncompressed settings.");
            if (hasCustomPivot)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                    !Mathf.Approximately(settings.spritePivot.x, pivotX) ||
                    !Mathf.Approximately(settings.spritePivot.y, pivotY))
                    throw new InvalidOperationException("Native custom pivot differs from the source binding: " + path + ". Re-run Install Sprite Stage Under Selection to restore frame registration.");
            }
            LoadSprite(path);
        }

        private static void ValidateCatalog(SanctuaryVisualCatalog catalog, ArtManifest manifest)
        {
            if (catalog == null || catalog.characters == null || catalog.characters.Length != CharacterIds.Length)
                throw new InvalidOperationException("Catalog must contain the five explicit actor bindings.");
            if (catalog.laboratoryBackground != LoadSprite(manifest.laboratoryBackground) ||
                catalog.chaosBackground != LoadSprite(manifest.chaosBackground))
                throw new InvalidOperationException("Catalog background Sprite references do not match the source manifest.");
            if (catalog.backgroundVariants == null || catalog.backgroundVariants.Length != manifest.backgrounds.Length)
                throw new InvalidOperationException("Catalog detailed background count differs from manifest.");
            var backgroundIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SanctuaryBackgroundVisual background in catalog.backgroundVariants)
                if (background == null || !backgroundIds.Add(background.id))
                    throw new InvalidOperationException("Catalog has missing or duplicate detailed backgrounds.");
            foreach (ArtBackgroundBinding background in manifest.backgrounds)
                if (catalog.FindBackground(background.id) != LoadSprite(background.path))
                    throw new InvalidOperationException("Catalog background reference differs: " + background.id);
            foreach (string id in CharacterIds)
            {
                SanctuaryCharacterVisual character = null;
                foreach (SanctuaryCharacterVisual entry in catalog.characters)
                    if (entry != null && entry.characterId == id)
                    {
                        if (character != null) throw new InvalidOperationException("Duplicate actor in catalog: " + id);
                        character = entry;
                    }
                if (character == null) throw new InvalidOperationException("Catalog actor missing: " + id);
                ValidateFrameReferences(manifest, id, "idle", character.idleFrames);
                ValidateFrameReferences(manifest, id, "move", character.moveFrames);
                ValidateFrameReferences(manifest, id, "jump", character.jumpFrames);
                ValidateFrameReferences(manifest, id, "prepare", character.prepareFrames);
                ValidateFrameReferences(manifest, id, "lower", character.lowerFrames);
                ValidateFrameReferences(manifest, id, "boundary", character.boundaryFrames);
                List<ArtBinding> portrait = Frames(manifest, id, "portrait");
                Sprite expectedPortrait = portrait.Count > 0 ? LoadSprite(portrait[0].path) : LoadSprite(Frames(manifest, id, "idle")[0].path);
                if (character.portrait != expectedPortrait) throw new InvalidOperationException("Catalog portrait does not match " + id + " source.");
                SanctuaryExpressionVisual[] expectedExpressions = BuildExpressions(manifest, id);
                if (character.expressionVariants == null || character.expressionVariants.Length != expectedExpressions.Length)
                    throw new InvalidOperationException("Catalog expression count differs: " + id);
                var expressionIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (SanctuaryExpressionVisual expression in character.expressionVariants)
                    if (expression == null || !expressionIds.Add(expression.id))
                        throw new InvalidOperationException("Catalog expression id missing or duplicate: " + id);
                foreach (SanctuaryExpressionVisual expected in expectedExpressions)
                {
                    SanctuaryExpressionVisual actual = character.FindExpression(expected.id);
                    if (actual == null || actual.sprite != expected.sprite || actual.hasContentBounds != expected.hasContentBounds ||
                        actual.hasRegistrationX != expected.hasRegistrationX || !Mathf.Approximately(actual.registrationX, expected.registrationX) ||
                        !Mathf.Approximately(actual.contentMinX, expected.contentMinX) || !Mathf.Approximately(actual.contentMinY, expected.contentMinY) ||
                        !Mathf.Approximately(actual.contentMaxX, expected.contentMaxX) || !Mathf.Approximately(actual.contentMaxY, expected.contentMaxY))
                        throw new InvalidOperationException("Catalog expression reference/registration differs: " + id + "/" + expected.id);
                }
            }
        }

        private static void ValidateFrameReferences(ArtManifest manifest, string id, string pose, Sprite[] actual)
        {
            List<ArtBinding> expected = Frames(manifest, id, pose);
            if (actual == null || actual.Length != expected.Count)
                throw new InvalidOperationException("Catalog frame count differs from manifest: " + id + "/" + pose);
            for (int i = 0; i < expected.Count; i++)
                if (actual[i] == null || actual[i] != LoadSprite(expected[i].path))
                    throw new InvalidOperationException("Catalog Sprite missing or incorrect: " + id + "/" + pose + "/" + i);
        }

        private static int CountSpriteReferences(SanctuaryVisualCatalog catalog)
        {
            int count = 2 + (catalog.backgroundVariants == null ? 0 : catalog.backgroundVariants.Length);
            foreach (SanctuaryCharacterVisual actor in catalog.characters)
            {
                if (actor.portrait != null) count++;
                if (actor.expressionVariants != null) count += actor.expressionVariants.Length;
                count += actor.idleFrames.Length + actor.moveFrames.Length + actor.jumpFrames.Length +
                    actor.prepareFrames.Length + actor.lowerFrames.Length + actor.boundaryFrames.Length;
            }
            return count;
        }
    }
}
#endif
