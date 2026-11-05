#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI.Editor
{
    public static partial class NotebookInstaller
    {
        [MenuItem("ANIMOL/New Notebook UI/Apply Theme Navigation To Selected Notebook")]
        public static void ApplyThemeNavigationToSelectedNotebook()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying theme navigation.");
            NotebookRoot ui = ResolveSelectedNotebook();
            if (!ui.stageScreen || !(ui.stageScreen.transform is RectTransform) || !ui.mapScroll
                || !ui.mapScroll.content || !ui.mapScroll.viewport || !ui.stageSheet || !ui.catalogJson
                || ui.stageScreen == ui.gameObject || ui.stageScreen == ui.mainScreen
                || ui.stageScreen == ui.settingsScreen || ui.stageScreen == ui.modal
                || !ui.mapScroll.transform.IsChildOf(ui.stageScreen.transform)
                || !ui.stageSheet.IsChildOf(ui.stageScreen.transform)
                || ui.mapScroll.gameObject.scene != ui.gameObject.scene || ui.stageScreen.scene != ui.gameObject.scene)
                throw new InvalidOperationException("Select the actual NotebookRoot with its existing separate stage map, ScrollRect, selection sheet and catalog.");
            NotebookCatalog catalog = NotebookCatalog.Parse(ui.catalogJson);
            if (catalog.themes == null || catalog.themes.Length != 4)
                throw new InvalidOperationException("Theme navigation requires the existing four-theme catalog.");
            NotebookMapThemeNavigation attached = ui.mapScroll.GetComponent<NotebookMapThemeNavigation>();
            NotebookMapThemeNavigation previous = ui.mapThemeNavigation ? ui.mapThemeNavigation : attached;
            if ((ui.mapThemeNavigation && attached != ui.mapThemeNavigation)
                || (previous && (previous.gameObject != ui.mapScroll.gameObject
                    || (previous.owner && previous.owner != ui) || (previous.scroll && previous.scroll != ui.mapScroll))))
                throw new InvalidOperationException("The existing navigation must belong only to this NotebookRoot's map ScrollRect.");
            if (previous && (!previous.rail || previous.rail.parent != ui.stageScreen.transform
                || previous.rail == ui.stageSheet || previous.buttons == null || previous.buttons.Length != 4
                || previous.buttons.Any(button => !button || button.transform.parent != previous.rail
                    || !button.image || !LabelOf(button)) || previous.buttons.Distinct().Count() != 4))
                throw new InvalidOperationException("The existing navigation must contain its separate owned rail and four complete buttons. Inspect or Undo an incomplete previous application first.");
            PrepareThemeNavigationAssets();

            const string action = "Apply Theme Navigation To Selected Notebook";
            bool wasDirty = ui.gameObject.scene.isDirty;
            GameObject createdRail = null;
            NotebookMapThemeNavigation createdNavigation = null;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(action);
            try
            {
                NotebookMapThemeNavigation navigation = previous;
                if (navigation)
                {
                    Undo.RecordObject(navigation, action);
                    Undo.RegisterFullObjectHierarchyUndo(navigation.rail.gameObject, action);
                }
                else
                {
                    navigation = createdNavigation = Undo.AddComponent<NotebookMapThemeNavigation>(ui.mapScroll.gameObject);
                }
                if (!navigation.rail)
                {
                    RectTransform rail = CreateThemeNavigationRail(ui);
                    createdRail = rail.gameObject;
                    Button[] buttons = CreateThemeNavigationButtons(rail);
                    // Register created objects before another object records references to them.
                    Undo.RegisterCreatedObjectUndo(createdRail, action);
                    Undo.RegisterFullObjectHierarchyUndo(createdRail, action);
                    Undo.RecordObject(navigation, action);
                    navigation.rail = rail;
                    navigation.buttons = buttons;
                }
                Undo.RecordObject(ui, action);
                ConfigureThemeNavigation(ui, navigation, catalog);
                Undo.FlushUndoRecordObjects();
                RecordPrefabOverride(ui); RecordPrefabOverride(navigation);
                foreach (UnityEngine.Object target in navigation.rail.GetComponentsInChildren<Component>(true))
                    RecordPrefabOverride(target);
                RecordPrefabOverride(navigation.rail.gameObject);
                EditorUtility.SetDirty(ui); EditorUtility.SetDirty(navigation);
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                Selection.activeGameObject = ui.gameObject;
                Debug.Log("Four equal Roman theme tabs added to the selected existing map. No map artwork, star coordinates, Main, Settings, Modal, Gateway or BM was replaced. Review and save the Scene or Prefab Stage; Undo restores the previous state.", ui);
            }
            catch
            {
                try { Undo.RevertAllDownToGroup(group); }
                finally
                {
                    if (createdRail) UnityEngine.Object.DestroyImmediate(createdRail);
                    if (createdNavigation) UnityEngine.Object.DestroyImmediate(createdNavigation);
                    // Undo restores Unity object references in both Scenes and Prefab Stages.
                    // JSON overwrite cannot resolve stage-local references and would clear them.
                    if (ui) RecordPrefabOverride(ui);
                    if (previous) RecordPrefabOverride(previous);
                    if (ui && wasDirty) EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                }
                throw;
            }
        }

        private static void PrepareThemeNavigationAssets()
        {
            // Resolve existing font/material first; this patch never regenerates either.
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedFont);
            neonTextMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Fonts/Notebook/Generated/Galmuri14_NeonReadability.mat");
            if (!font || !neonTextMaterial) throw new InvalidOperationException("The installed Galmuri14 font and neon text material are required.");
            foreach (string state in new[] { "Normal", "Highlighted", "Pressed", "Disabled", "Selected" })
                if (!File.Exists(MapTex + "MapTheme/Tab_" + state + ".png"))
                    throw new InvalidOperationException("Missing static theme-navigation sprite: " + state);
            foreach (string state in new[] { "Normal", "Highlighted", "Pressed", "Disabled", "Selected" })
            {
                string path = MapTex + "MapTheme/Tab_" + state + ".png";
                AssetDatabase.ImportAsset(path);
                ImportNeonSprite(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.spriteBorder = Vector4.one * 32;
                importer.SaveAndReimport();
            }
        }
        private static Sprite ThemeSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapTex + "MapTheme/" + name + ".png");
            if (!sprite) throw new InvalidOperationException("Missing theme sprite: " + name);
            return sprite;
        }
        private static RectTransform CreateThemeNavigationRail(NotebookRoot ui)
        {
            RectTransform rail = Rect(ui.stageScreen.transform, "Roman Theme Navigation");
            rail.gameObject.AddComponent<CanvasGroup>();
            return rail;
        }
        private static Button[] CreateThemeNavigationButtons(RectTransform rail)
        {
            string[] numerals = { "I", "II", "III", "IV" };
            var buttons = new Button[4];
            for (int index = 0; index < buttons.Length; index++)
            {
                RectTransform rect = Rect(rail, "Theme " + numerals[index]);
                Image image = rect.gameObject.AddComponent<Image>(); image.sprite = ThemeSprite("Tab_Normal");
                image.raycastTarget = true;
                Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                button.transition = Selectable.Transition.SpriteSwap;
                TMP_Text label = Text(button.transform, "Roman Theme", numerals[index], 0, 0, 234, 192, 58, TextAlignmentOptions.Center);
                StyleNeonText(label); buttons[index] = button;
            }
            return buttons;
        }
        private static void ConfigureThemeNavigation(NotebookRoot ui, NotebookMapThemeNavigation navigation, NotebookCatalog catalog)
        {
            RectTransform rail = navigation.rail;
            rail.anchorMin = rail.anchorMax = rail.pivot = new Vector2(.5f, 0);
            rail.anchoredPosition = new Vector2(0, 24); rail.sizeDelta = new Vector2(984, 240);
            rail.localScale = Vector3.one; rail.localRotation = Quaternion.identity;
            CanvasGroup group = rail.GetComponent<CanvasGroup>();
            if (!group) throw new InvalidOperationException("The theme-navigation rail requires its CanvasGroup.");
            group.alpha = 1; group.interactable = true; group.blocksRaycasts = true; rail.gameObject.SetActive(true);
            // Match the selection sheet's footprint without reserving any extra map viewport.
            int sheetSibling = ui.stageSheet ? ui.stageSheet.GetSiblingIndex() : ui.stageScreen.transform.childCount;
            if (ui.stageSheet && rail.GetSiblingIndex() < sheetSibling) sheetSibling--;
            rail.SetSiblingIndex(Mathf.Max(0, sheetSibling));
            navigation.owner = ui; navigation.scroll = ui.mapScroll;
            navigation.themeStartY = catalog.themes.Select(theme => (float)theme.startY).ToArray();
            navigation.normalSprite = ThemeSprite("Tab_Normal"); navigation.selectedSprite = ThemeSprite("Tab_Selected");
            string[] numerals = { "I", "II", "III", "IV" };
            for (int index = 0; index < navigation.buttons.Length; index++)
            {
                Button button = navigation.buttons[index];
                At(button.GetComponent<RectTransform>(), 6 + 246 * index, 24, 234, 192);
                button.image.sprite = navigation.normalSprite; button.image.color = Color.white;
                button.image.type = Image.Type.Sliced; button.image.pixelsPerUnitMultiplier = 1;
                button.image.raycastTarget = true; button.targetGraphic = button.image;
                button.transition = Selectable.Transition.SpriteSwap; button.interactable = true;
                // Keyboard focus uses the highlight state; the region controller alone owns the selected underline.
                button.spriteState = new SpriteState { highlightedSprite = ThemeSprite("Tab_Highlighted"), pressedSprite = ThemeSprite("Tab_Pressed"), selectedSprite = ThemeSprite("Tab_Highlighted"), disabledSprite = ThemeSprite("Tab_Disabled") };
                TMP_Text label = LabelOf(button); At(label.rectTransform, 0, 0, 234, 192);
                label.text = numerals[index]; label.font = font; label.fontSize = 58; label.fontStyle = FontStyles.Normal;
                label.alignment = TextAlignmentOptions.Center; label.enableAutoSizing = false;
                label.enableWordWrapping = false; label.raycastTarget = false; StyleNeonText(label);
            }
            ui.mapThemeNavigation = navigation;
        }
    }
}
#endif
