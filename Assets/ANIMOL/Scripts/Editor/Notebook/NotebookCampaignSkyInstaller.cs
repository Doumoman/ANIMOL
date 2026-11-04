#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI.Editor
{
    public static partial class NotebookInstaller
    {
        [MenuItem("ANIMOL/New Notebook UI/Apply Campaign Sky Transition To Selected Notebook")]
        public static void ApplyCampaignSkyTransitionToSelectedNotebook()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the campaign sky transition.");
            NotebookRoot ui = ResolveSelectedNotebook();
            Canvas parentCanvas = ui.GetComponentInParent<Canvas>();
            Canvas canvas = parentCanvas ? parentCanvas.rootCanvas : null;
            if (!canvas || canvas.gameObject.scene != ui.gameObject.scene)
                throw new InvalidOperationException("The selected NotebookRoot needs a root Canvas in the same Scene or Prefab Stage.");
            if (ui.campaignSkyTransition && (!ui.campaignSkyTransition.transform.IsChildOf(canvas.transform)
                || ui.campaignSkyTransition.gameObject.scene != ui.gameObject.scene
                || ui.campaignSkyTransition.transform == ui.transform || ui.transform.IsChildOf(ui.campaignSkyTransition.transform)))
                throw new InvalidOperationException("The existing campaign transition must be a separate overlay in this NotebookRoot's Canvas.");
            // Resolve all assets before changing any hierarchy or serialized reference.
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Art/UI/ConstellationMap/Shaders/UI_WilliamCampaignSkyTransition.shader");
            Texture destination = AssetDatabase.LoadAssetAtPath<Texture>(MapTex + "Map/NightSky.png");
            if (!shader || !destination)
                throw new InvalidOperationException("The campaign sky shader and original Map/NightSky.png are required.");

            const string action = "Apply Campaign Sky Transition To Selected Notebook";
            bool wasDirty = ui.gameObject.scene.isDirty;
            string rootSnapshot = EditorJsonUtility.ToJson(ui);
            NotebookCampaignSkyTransition previous = ui.campaignSkyTransition;
            string transitionSnapshot = previous ? EditorJsonUtility.ToJson(previous) : null;
            RawImage previousImage = previous ? previous.GetComponent<RawImage>() : null;
            RectTransform previousRect = previous ? previous.GetComponent<RectTransform>() : null;
            if (previous && (!previousImage || !previousRect))
                throw new InvalidOperationException("The existing campaign overlay requires its RawImage and RectTransform components.");
            string imageSnapshot = previousImage ? EditorJsonUtility.ToJson(previousImage) : null;
            string rectSnapshot = previousRect ? EditorJsonUtility.ToJson(previousRect) : null;
            int previousSibling = previous ? previous.transform.GetSiblingIndex() : 0;
            bool previousActive = previous && previous.gameObject.activeSelf;
            GameObject created = null;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(action);
            try
            {
                CanvasGroup scope = ui.GetComponent<CanvasGroup>();
                if (!scope) scope = Undo.AddComponent<CanvasGroup>(ui.gameObject);
                if (previous)
                {
                    Undo.RecordObject(previous, action); Undo.RecordObject(previousImage, action); Undo.RecordObject(previousRect, action);
                    Undo.RecordObject(previous.gameObject, action);
                }
                else
                {
                    created = new GameObject("Campaign Sky Transition Overlay", typeof(RectTransform), typeof(RawImage), typeof(NotebookCampaignSkyTransition));
                    if (created.scene != ui.gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(created, ui.gameObject.scene);
                    created.transform.SetParent(canvas.transform, false);
                    // Register creation before the root records a reference to this object.
                    Undo.RegisterCreatedObjectUndo(created, action);
                }
                Undo.RecordObject(ui, action);
                NotebookCampaignSkyTransition transition = previous ? previous : created.GetComponent<NotebookCampaignSkyTransition>();
                ConfigureCampaignSkyTransition(ui, transition, canvas, scope, shader, destination);
                Undo.FlushUndoRecordObjects();
                RecordPrefabOverride(ui); RecordPrefabOverride(scope); RecordPrefabOverride(transition);
                RecordPrefabOverride(transition.GetComponent<RawImage>()); RecordPrefabOverride(transition.transform);
                RecordPrefabOverride(transition.gameObject);
                EditorUtility.SetDirty(ui); EditorUtility.SetDirty(transition);
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                Selection.activeGameObject = ui.gameObject;
                Debug.Log("Campaign sky transition connected to the selected NotebookRoot. Its existing Main, Settings, Map, Modal, Gateway, artwork and BM were preserved. Review and save the Scene or Prefab Stage; Undo removes this change.", ui);
            }
            catch
            {
                try { Undo.RevertAllDownToGroup(group); }
                finally
                {
                    if (created) UnityEngine.Object.DestroyImmediate(created);
                    if (ui)
                    {
                        EditorJsonUtility.FromJsonOverwrite(rootSnapshot, ui);
                        // Undo may clear an existing object reference before JSON restoration sees it.
                        ui.campaignSkyTransition = previous;
                    }
                    if (previous)
                    {
                        EditorJsonUtility.FromJsonOverwrite(transitionSnapshot, previous);
                        if (previousImage) EditorJsonUtility.FromJsonOverwrite(imageSnapshot, previousImage);
                        if (previousRect) EditorJsonUtility.FromJsonOverwrite(rectSnapshot, previousRect);
                        previous.transform.SetSiblingIndex(previousSibling);
                        previous.gameObject.SetActive(previousActive);
                        RecordPrefabOverride(previous); RecordPrefabOverride(previousImage); RecordPrefabOverride(previousRect); RecordPrefabOverride(previous.gameObject);
                    }
                    if (ui) { RecordPrefabOverride(ui); if (wasDirty) EditorSceneManager.MarkSceneDirty(ui.gameObject.scene); }
                }
                throw;
            }
        }

        private static void BuildCampaignSkyTransition(NotebookRoot ui, Canvas canvas, CanvasGroup scope)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Art/UI/ConstellationMap/Shaders/UI_WilliamCampaignSkyTransition.shader");
            Texture destination = AssetDatabase.LoadAssetAtPath<Texture>(MapTex + "Map/NightSky.png");
            if (!shader || !destination) throw new InvalidOperationException("The campaign sky shader and original night sky texture are required.");
            if (!scope) scope = ui.GetComponent<CanvasGroup>();
            if (!scope) scope = ui.gameObject.AddComponent<CanvasGroup>();
            RectTransform rect = Rect(canvas.transform, "Campaign Sky Transition Overlay");
            rect.gameObject.AddComponent<RawImage>();
            NotebookCampaignSkyTransition transition = rect.gameObject.AddComponent<NotebookCampaignSkyTransition>();
            ConfigureCampaignSkyTransition(ui, transition, canvas, scope, shader, destination);
        }

        private static void ConfigureCampaignSkyTransition(NotebookRoot ui, NotebookCampaignSkyTransition transition, Canvas canvas, CanvasGroup scope, Shader shader, Texture destination)
        {
            RectTransform rect = transition.GetComponent<RectTransform>();
            if (!rect || rect.parent != canvas.transform)
                throw new InvalidOperationException("The campaign overlay must be a direct child of the physical root Canvas, outside the safe-area design.");
            Stretch(rect, 0, 0, 0, 0); rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            RawImage image = transition.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture; image.material = null; image.color = Color.white; image.raycastTarget = false; image.enabled = false;
            transition.transitionShader = shader; transition.destinationTexture = destination; transition.inputScope = scope;
            transition.enabled = true; transition.gameObject.SetActive(true);
            ui.campaignSkyTransition = transition;
        }

    }
}
#endif
