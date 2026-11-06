#if UNITY_EDITOR
using System.IO;
using Animol.Starway;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    public static class RanchStoryUiInstaller
    {
        public const string UiPrefab = "Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab";
        public const string FontPath = "Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset";
        public static void Configure(GameObject root)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidDataException("Assign the existing Galmuri14 TMP asset; no substitute font is permitted.");
            if (!File.Exists(UiPrefab)) Animol.Starway.Editor.CreateStoryDialoguePrefab.CreateAsset(font, "Assets/ANIMOL/Prefabs/UI/Story");
            var uiAsset = PrefabUtility.LoadPrefabContents(UiPrefab);
            try
            {
                foreach (var neonPanel in uiAsset.GetComponentsInChildren<StoryNeonPanel>(true))
                    if (neonPanel.GetComponent<CanvasRenderer>() == null) neonPanel.gameObject.AddComponent<CanvasRenderer>();
                var savedPanel = uiAsset.transform.Find("SafeArea/DialoguePanel");
                if (savedPanel.GetComponent<CanvasGroup>() == null) savedPanel.gameObject.AddComponent<CanvasGroup>();
                var serialized = new SerializedObject(uiAsset.GetComponent<StoryDialogueView>());
                serialized.FindProperty("dialogueJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(RanchStoryArtInstaller.DataRoot + "/ranch_dialogue.json");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(uiAsset, UiPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(uiAsset); }
            var runner = root.GetComponent<StoryRunner>(); var view = runner.view;
            foreach (var previous in root.GetComponentsInChildren<StoryDialogueView>(true)) Object.DestroyImmediate(previous.gameObject);
            var canvas = root.GetComponentInChildren<Canvas>(true);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefab), canvas.transform);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            instance.transform.SetAsLastSibling();
            var ui = instance.GetComponent<StoryDialogueView>();
            var settings = new SerializedObject(ui);
            settings.FindProperty("dialogueJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(RanchStoryArtInstaller.DataRoot + "/ranch_dialogue.json");
            settings.FindProperty("hostControlsProgress").boolValue = true;
            settings.FindProperty("playFirstSceneOnStart").boolValue = false;
            settings.FindProperty("advanceBetweenScenes").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ui.SetFont(font);
            var bridge = root.GetComponent<RanchStoryDialogueBridge>() ?? root.AddComponent<RanchStoryDialogueBridge>();
            bridge.runner = runner; bridge.dialogue = ui; view.dialogueBridge = bridge;
            foreach (var name in new[] {"DialogueRoot", "ActionControls", "StatusBar", "TopControls"})
            {
                var old = canvas.transform.Find("SafeArea/" + name); if (old != null) old.gameObject.SetActive(false);
            }
            var panel = instance.transform.Find("SafeArea/DialoguePanel");
            view.dialogueGroup = panel.GetComponent<CanvasGroup>();
            view.bodyText = ui.BodyLabel; view.speakerText = ui.SpeakerLabel;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font; label.fontSharedMaterial = font.material; label.fontStyle = FontStyles.Normal;
                label.fontWeight = FontWeight.Regular; label.enableAutoSizing = false;
            }
            ui.BodyLabel.fontSize = ui.SpeakerLabel.fontSize = 45;
            var lore = root.GetComponentInChildren<RanchOptionalLore>(true); if (lore != null) lore.body.fontSize = 45;
            runner.finishLegacyCueBeforeAdvancing = false;
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(runner); EditorUtility.SetDirty(bridge);
        }
    }
}
#endif
