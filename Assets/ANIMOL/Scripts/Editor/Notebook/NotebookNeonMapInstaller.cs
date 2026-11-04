#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI.Editor
{
    public static partial class NotebookInstaller
    {
        private static Material neonTextMaterial;
        [MenuItem("ANIMOL/New Notebook UI/Apply Neon Map To Selected Notebook")]
        public static void ApplyNeonMapToSelectedNotebook()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the neon map.");
            NotebookRoot ui = ResolveSelectedNotebook();
            NotebookCanvasLayout layout = ui.GetComponentInParent<NotebookCanvasLayout>();
            GameObject oldMap = ui.stageScreen;
            if (oldMap && oldMap.name == "Constellation Neon Map")
                throw new InvalidOperationException("The selected NotebookRoot already uses the neon map.");
            if (!layout || !layout.design || !oldMap || !(oldMap.transform is RectTransform)
                || oldMap == ui.gameObject || ui.transform.IsChildOf(oldMap.transform)
                || oldMap == ui.mainScreen || oldMap == ui.settingsScreen || oldMap == ui.modal)
                throw new InvalidOperationException("Select a NotebookRoot with a separate existing stage screen and NotebookCanvasLayout.");
            if (oldMap.scene != ui.gameObject.scene)
                throw new InvalidOperationException("The selected NotebookRoot and its stage screen must belong to the same Scene or Prefab Stage.");
            if (!ui.catalogJson) throw new InvalidOperationException("The selected NotebookRoot requires its existing catalogJson.");
            NotebookCatalog catalog = NotebookCatalog.Parse(ui.catalogJson);
            PrepareNeonMapAssets();

            const string action = "Apply Neon Map To Selected Notebook";
            var scene = ui.gameObject.scene;
            bool wasDirty = scene.isDirty, oldActive = oldMap.activeSelf;
            Transform oldParent = oldMap.transform.parent; int oldSibling = oldMap.transform.GetSiblingIndex();
            string rootSnapshot = EditorJsonUtility.ToJson(ui), layoutSnapshot = EditorJsonUtility.ToJson(layout);
            GameObject main = ui.mainScreen, settings = ui.settingsScreen, modal = ui.modal; NotebookGateway provider = ui.gateway;
            var existingRects = new[] { layout.design, layout.fullScreenSettingsBackdrop, layout.fullScreenModalBackdrop }
                .Where(rect => rect).Distinct().ToArray();
            string[] rectSnapshots = existingRects.Select(rect => EditorJsonUtility.ToJson(rect)).ToArray();
            GameObject newMap = null;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(action);
            try
            {
                Undo.RecordObject(ui, action); Undo.RecordObject(layout, action);
                Undo.RegisterFullObjectHierarchyUndo(oldMap, action);
                Undo.RecordObject(oldMap.transform, action);
                if (oldParent) Undo.RecordObject(oldParent, action);
                foreach (RectTransform rect in existingRects) Undo.RecordObject(rect, action);

                newMap = new GameObject("Constellation Neon Map", typeof(RectTransform));
                if (newMap.scene != scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newMap, scene);
                newMap.transform.SetParent(oldParent, false); newMap.transform.SetSiblingIndex(oldSibling);
                if (newMap.transform.parent != oldParent || newMap.transform.GetSiblingIndex() != oldSibling)
                    throw new InvalidOperationException("Unity could not preserve the map sibling position. Open this Prefab in Prefab Mode and apply there.");
                ui.stageScreen = newMap;
                BuildNeonMap(ui, catalog);
                if (ui.mainScreen != main || ui.settingsScreen != settings || ui.modal != modal || ui.gateway != provider)
                    throw new InvalidOperationException("The map factory changed a reference outside the selected stage screen.");
                oldMap.SetActive(false); newMap.SetActive(oldActive);
                layout.RefreshLayout();
                Undo.FlushUndoRecordObjects();
                // Register after construction so the complete new child hierarchy participates in Undo/Redo.
                Undo.RegisterCreatedObjectUndo(newMap, action);
                Undo.RegisterFullObjectHierarchyUndo(newMap, action);
                RecordPrefabOverride(ui); RecordPrefabOverride(layout); RecordPrefabOverride(oldMap);
                RecordPrefabOverride(oldMap.transform);
                if (oldParent) RecordPrefabOverride(oldParent);
                foreach (RectTransform rect in existingRects) RecordPrefabOverride(rect);
                EditorUtility.SetDirty(ui); EditorUtility.SetDirty(layout);
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = ui.gameObject;
                Debug.Log("Neon map applied to the selected NotebookRoot. The previous map is inactive and preserved; Undo restores it. Main, Settings, Modal, Gateway, cameras, EventSystem and Build Settings were not replaced. Review and save the Scene or Prefab Stage yourself.", ui);
            }
            catch
            {
                // Revert only this operation; never reopen or save a Scene to recover its state.
                try { Undo.RevertAllDownToGroup(group); }
                finally
                {
                    if (newMap) UnityEngine.Object.DestroyImmediate(newMap);
                    if (ui) EditorJsonUtility.FromJsonOverwrite(rootSnapshot, ui);
                    if (layout) EditorJsonUtility.FromJsonOverwrite(layoutSnapshot, layout);
                    for (int i = 0; i < existingRects.Length; i++)
                        if (existingRects[i]) EditorJsonUtility.FromJsonOverwrite(rectSnapshots[i], existingRects[i]);
                    RecordPrefabOverride(ui); RecordPrefabOverride(layout);
                    foreach (RectTransform rect in existingRects) RecordPrefabOverride(rect);
                    if (oldMap)
                    {
                        oldMap.SetActive(oldActive); oldMap.transform.SetSiblingIndex(oldSibling);
                        RecordPrefabOverride(oldMap); RecordPrefabOverride(oldMap.transform);
                    }
                    // Undo owns the clean-scene savepoint. An already-dirty Scene must stay dirty.
                    if (wasDirty) EditorSceneManager.MarkSceneDirty(scene);
                }
                throw;
            }
        }
        private static NotebookRoot ResolveSelectedNotebook()
        {
            GameObject selected = Selection.activeGameObject;
            if (!selected || Selection.gameObjects.Length != 1)
                throw new InvalidOperationException("Select exactly one NotebookRoot GameObject or one of its parents/children in a Scene or Prefab Stage.");
            if (EditorUtility.IsPersistent(selected))
                throw new InvalidOperationException("Do not apply directly to a Prefab asset. Open it in Prefab Mode, then select its NotebookRoot.");
            var scene = selected.scene;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (!scene.IsValid() || !scene.isLoaded || (EditorSceneManager.IsPreviewScene(scene) && (stage == null || stage.scene != scene)))
                throw new InvalidOperationException("The selection must be in a loaded Scene or the current Prefab Stage.");
            var candidates = new HashSet<NotebookRoot>(selected.GetComponentsInParent<NotebookRoot>(true));
            foreach (NotebookRoot candidate in selected.GetComponentsInChildren<NotebookRoot>(true)) candidates.Add(candidate);
            if (candidates.Count != 1)
                throw new InvalidOperationException("The selection must resolve to exactly one NotebookRoot; no Scene-wide automatic search is performed.");
            return candidates.First();
        }
        private static void PrepareNeonMapAssets()
        {
            // Only these dedicated map sprites and the generated font/material are prepared.
            string[] names = new[] { "PanelFrame", "Icon_Back", "Icon_Telescope", "Icon_Enter", "Icon_StoryReplay", "Icon_Lock" }
                .Concat(new[] { "Utility", "Wallet", "Action" }.SelectMany(role => new[] { "Normal", "Highlighted", "Pressed", "Disabled" }.Select(state => role + "_" + state))).ToArray();
            foreach (string name in names)
            {
                string path = MapTex + "MapNeon/" + name + ".png";
                if (!File.Exists(path)) throw new InvalidOperationException("Missing neon map sprite: " + path);
                AssetDatabase.ImportAsset(path); ImportNeonSprite(path);
            }
            foreach (string state in MapStarStates)
            {
                string compatibility = MapTex + "Map/Star_" + state + ".png";
                if (!File.Exists(compatibility)) throw new InvalidOperationException("Missing map star sprite: " + compatibility);
                AssetDatabase.ImportAsset(compatibility); ImportSprite(compatibility);
                for (int variant = 0; variant < NotebookRoot.MapStarVariantCount; variant++)
                    for (int frame = 0; frame < NotebookRoot.MapStarFrameCount; frame++)
                    {
                        string path = MapTex + "Map/Stars/Star_" + state + "_V" + variant + "_F" + frame + ".png";
                        if (!File.Exists(path)) throw new InvalidOperationException("Missing selected-star frame: " + path);
                        AssetDatabase.ImportAsset(path); ImportNeonSprite(path);
                    }
            }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedFont);
            if (!font) throw new InvalidOperationException("The active Galmuri14 TMP font is missing: " + GeneratedFont);
            neonTextMaterial = EnsureNeonTextMaterial();
        }
        private static void RecordPrefabOverride(UnityEngine.Object target)
        {
            if (target && PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static readonly string[] MapStarStates = { "Available", "Current", "Done", "Locked" };
        private static Sprite[] LoadMapStarFrames()
        {
            var result = new List<Sprite>();
            foreach (string state in MapStarStates)
                for (int variant = 0; variant < NotebookRoot.MapStarVariantCount; variant++)
                    for (int frame = 0; frame < NotebookRoot.MapStarFrameCount; frame++)
                        result.Add(Load("Map/Stars/Star_" + state + "_V" + variant + "_F" + frame));
            return result.ToArray();
        }
        private static void BuildNeonMap(NotebookRoot ui, NotebookCatalog catalog)
        {
            ui.mapStarFrames = LoadMapStarFrames();
            RectTransform host = (RectTransform)ui.stageScreen.transform;
            NotebookCanvasLayout layout = ui.GetComponentInParent<NotebookCanvasLayout>(); layout.mapScreen = host;
            // A single supplied sky fills the physical screen, including wide and notched gutters.
            RectTransform skyBackdrop = Rect(host, "Full Physical Night Sky Backdrop"); Stretch(skyBackdrop, 0, 0, 0, 0);
            layout.fullScreenMapBackdrop = skyBackdrop;
            Image sky = Picture(skyBackdrop, "Static Night Sky", "Map/NightSky"); Stretch(sky.rectTransform, 0, 0, 0, 0);
            var skyFit = sky.gameObject.AddComponent<AspectRatioFitter>();
            skyFit.aspectRatio = sky.sprite.rect.width / sky.sprite.rect.height; skyFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            RectTransform viewport = Rect(host, "Full View Constellation Scroll"); Stretch(viewport, 0, 0, 0, 0); viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            RectTransform content = Rect(viewport, "Four Regions One Map");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(1080, catalog.mapContentHeight);
            ui.mapScroll = viewport.gameObject.AddComponent<ScrollRect>(); ui.mapScroll.viewport = viewport; ui.mapScroll.content = content; ui.mapScroll.horizontal = false; ui.mapScroll.vertical = true; ui.mapScroll.movementType = ScrollRect.MovementType.Clamped;
            // These are supplied transparent PNG illustrations, not runtime-generated drawings.
            // Stage links and clickable nodes are created afterward so they remain in front.
            foreach (NotebookConstellationArtwork art in catalog.artworks ?? Array.Empty<NotebookConstellationArtwork>())
            {
                if (string.IsNullOrEmpty(art.sprite) || art.width <= 0 || art.height <= 0)
                    throw new InvalidOperationException("Invalid constellation artwork: " + art.id);
                Image figure = Picture(content, "Constellation Figure " + art.id, art.sprite);
                At(figure.rectTransform, art.x, art.y, art.width, art.height);
                figure.preserveAspect = true;
                // Wire PNG includes the same contain-fit padding as this picture.
                // Stage x/y are exact wire vertices, not a separate zigzag layout.
                // Picture() fixes White Tint and disables raycasts. Names/meanings stay in the catalog.
                if (!string.IsNullOrEmpty(art.wireSprite))
                {
                    Image wire = Picture(content, "Static Constellation Wire " + art.id, art.wireSprite);
                    At(wire.rectTransform, art.x, art.y, art.width, art.height);
                    wire.preserveAspect = false;
                }
            }
            ui.edges = catalog.edges.Select(e => { Image im = Picture(content, e.from + " to " + e.to, e.sprite + "_Future"); At(im.rectTransform, e.x, e.y, e.width, e.height); return new NotebookEdgeView { from = e.from, to = e.to, image = im, future = Load(e.sprite + "_Future"), cleared = Load(e.sprite + "_Cleared") }; }).ToArray();
            // Region names belong to the selected star's sheet, never an opaque chapter plate.
            ui.chapters = Array.Empty<NotebookChapterView>();
            var nodeViews = new List<NotebookNodeView>();
            foreach (NotebookStage s in catalog.stages)
            {
                RectTransform box = Rect(content, s.id); At(box, s.x - 80, s.y - 80, 160, 198);
                Image target = box.gameObject.AddComponent<Image>(); target.color = Color.clear; target.raycastTarget = true;
                Image star = Picture(box, "Baked Asymmetric Star State", "Map/Stars/Star_Locked_V" + NotebookRoot.MapStarVariant(s.id) + "_F0"); At(star.rectTransform, 0, 0, 160, 160);
                Button button = box.gameObject.AddComponent<Button>(); button.targetGraphic = star; button.transition = Selectable.Transition.None;
                Image selected = Picture(box, "Selected", "Map/Selection"); At(selected.rectTransform, -20, -20, 200, 200);
                TMP_Text number = Text(box, "Number", s.order.ToString("00"), 0, 52, 160, 56, 39, TextAlignmentOptions.Center);
                StyleNeonText(number);
                Image locked = NeonIcon(box, "Lock", 56, 59, 48, 48);
                TMP_Text label = Text(box, "Stage Label", s.label, -10, 168, 180, 45, 29, TextAlignmentOptions.Center); StyleNeonText(label);
                nodeViews.Add(new NotebookNodeView { stageId = s.id, button = button, star = star, selection = selected, locked = locked, number = number, label = label });
            }
            ui.nodes = nodeViews.ToArray();
            ui.mapBack = NeonButton(host, "Back", "Utility", 32, 24, 132, 132);
            NeonIcon(ui.mapBack.transform, "Back", 27, 27, 78, 78);
            ui.telescopeButton = NeonButton(host, "Telescope Wallet", "Wallet", 0, 0, 300, 112);
            TopRight(ui.telescopeButton.GetComponent<RectTransform>(), 32, 24, 300, 112);
            NeonIcon(ui.telescopeButton.transform, "Telescope", 22, 23, 66, 66);
            ui.walletText = Text(ui.telescopeButton.transform, "Telescope Balance", "10 / 10", 100, 0, 182, 112, 42, TextAlignmentOptions.Center); StyleNeonText(ui.walletText);

            Image sheet = NeonPicture(host, "Selected Star Transparent Neon Sheet", "PanelFrame", true);
            ui.stageSheet = sheet.rectTransform;
            ui.stageSheet.anchorMin = ui.stageSheet.anchorMax = ui.stageSheet.pivot = new Vector2(.5f, 0);
            ui.stageSheet.anchoredPosition = new Vector2(0, 24); ui.stageSheet.sizeDelta = new Vector2(984, 240); sheet.raycastTarget = true;
            ui.stageName = Text(sheet.transform, "Selected Stage ID", "", 32, 35, 570, 72, 50); StyleNeonText(ui.stageName);
            ui.stageRegion = Text(sheet.transform, "Selected Theme", "", 32, 117, 570, 60, 34); StyleNeonText(ui.stageRegion);
            ui.storyReplayButton = NeonButton(sheet.transform, "Story Replay", "Action", 648, 48, 144, 144);
            NeonIcon(ui.storyReplayButton.transform, "StoryReplay", 30, 30, 84, 84);
            ui.storyReplayButton.gameObject.SetActive(false);
            ui.launchButton = NeonButton(sheet.transform, "Stage Entry", "Action", 816, 48, 144, 144);
            ui.launchIcon = NeonIcon(ui.launchButton.transform, "Enter", 30, 30, 84, 84);
            ui.mapEntryIcon = NeonLoad("Icon_Enter"); ui.mapLockIcon = NeonLoad("Icon_Lock");
            ui.stageSheet.gameObject.SetActive(false);
        }
        private static Material EnsureNeonTextMaterial()
        {
            // One cached SDF material protects white text against bright constellation figures.
            // The shared source font material and every non-map text label remain unchanged.
            string destination = "Assets/ANIMOL/Fonts/Notebook/Generated/Galmuri14_NeonReadability.mat";
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            AssetDatabase.Refresh();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(destination);
            if (!material)
            {
                material = new Material(font.material) { name = "Galmuri14 Neon Readability" };
                AssetDatabase.CreateAsset(material, destination);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, .12f);
            EditorUtility.SetDirty(material); return material;
        }
        private static void StyleNeonText(TMP_Text label)
        {
            label.color = Color.white; label.fontSharedMaterial = neonTextMaterial;
            label.UpdateMeshPadding();
        }
        private static Sprite NeonLoad(string name)
        {
            string path = MapTex + "MapNeon/" + name + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) throw new InvalidOperationException("Missing neon sprite: " + path);
            return sprite;
        }
        private static Image NeonPicture(Transform parent, string name, string spriteName, bool sliced = false)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = NeonLoad(spriteName); image.color = Color.white;
            image.raycastTarget = false; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1;
            return image;
        }
        private static void TopRight(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-right, -top); rect.sizeDelta = new Vector2(width, height);
        }
        private static void ImportNeonSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing texture importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (path.Contains("/Map/Stars/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(.5f, .5f);
            }
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.spritePixelsPerUnit = 100;
            string name = Path.GetFileNameWithoutExtension(path);
            importer.spriteBorder = path.Contains("/MapNeon/") && !name.StartsWith("Icon_", StringComparison.Ordinal)
                ? Vector4.one * (name == "PanelFrame" ? 36 : 32) : Vector4.zero;
            importer.SaveAndReimport();
        }
        private static Button NeonButton(Transform parent, string name, string role, float x, float y, float width, float height)
        {
            Image image = NeonPicture(parent, name, role + "_Normal", true); At(image.rectTransform, x, y, width, height); image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = NeonLoad(role + "_Highlighted"), pressedSprite = NeonLoad(role + "_Pressed"), selectedSprite = NeonLoad(role + "_Highlighted"), disabledSprite = NeonLoad(role + "_Disabled") };
            return button;
        }
        private static Image NeonIcon(Transform parent, string name, float x, float y, float width, float height)
        {
            Image icon = NeonPicture(parent, "White Neon " + name, "Icon_" + name); At(icon.rectTransform, x, y, width, height); icon.preserveAspect = true; return icon;
        }
    }
}
#endif
