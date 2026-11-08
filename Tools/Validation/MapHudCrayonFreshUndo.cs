using System;
using System.Linq;
using Animol.MapHudCrayonUI;
using Animol.MapHudCrayonUI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using William.NotebookUI;
using Object = UnityEngine.Object;

public static class MapHudCrayonFreshUndo
{
    static void Need(bool yes, string message) { if (!yes) throw new InvalidOperationException(message); }
    public static object Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode first");
        var selection = Selection.objects;
        var active = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var clone = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab"));
            SceneManager.MoveGameObjectToScene(clone, scene);
            foreach (var binding in clone.GetComponentsInChildren<CrayonImageBinding>(true))
            {
                binding.RestoreOriginal();
                if (binding.decoration) Object.DestroyImmediate(binding.decoration.gameObject);
                Object.DestroyImmediate(binding);
            }
            foreach (var scope in clone.GetComponentsInChildren<CrayonSkinScope>(true)) Object.DestroyImmediate(scope);
            var hud = clone.GetComponentInChildren<NotebookGameplayHud>(true);
            Selection.objects = new Object[] { hud.gameObject };
            CrayonMapHudInstaller.ApplyToSelectedGameplayHud();
            int expected = hud.GetComponentsInChildren<CrayonImageBinding>(true).Length;
            Need(expected > 0, "Fresh application created no bindings");
            Undo.PerformUndo();
            Need(hud.GetComponentsInChildren<CrayonImageBinding>(true).Length == 0, "Fresh Undo left bindings");
            Need(hud.GetComponentsInChildren<Transform>(true).All(t => t.name != "Prepainted Crayon Decoration"), "Fresh Undo left decorations");
            Undo.PerformRedo();
            Check(hud, expected);
            CrayonMapHudInstaller.ApplyToSelectedGameplayHud();
            Check(hud, expected);
            return new { freshBindings = expected, undoRemovedChildren = true, redoSingleChild = true, reapplySingleChild = true, saved = false };
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            Selection.objects = selection;
        }
    }
    static void Check(NotebookGameplayHud hud, int expected)
    {
        var bindings = hud.GetComponentsInChildren<CrayonImageBinding>(true);
        Need(bindings.Length == expected, "Binding count changed");
        foreach (var binding in bindings)
        {
            Need(binding.sourceImage && binding.decoration && binding.decoration.sprite && !binding.decoration.raycastTarget,
                "Fresh Redo lost image state: " + binding.name + "; source=" + (bool)binding.sourceImage + "; decoration=" + (bool)binding.decoration
                + "; sprite=" + (binding.decoration ? binding.decoration.sprite?.name : "null") + "; raycast=" + (binding.decoration && binding.decoration.raycastTarget)
                + "; children=" + string.Join(",", binding.transform.Cast<Transform>().Select(t=>t.name)));
            Need(binding.sourceImage.transform.Cast<Transform>().Count(t => t.name == "Prepainted Crayon Decoration") == 1, "Fresh Redo duplicated a decoration");
        }
    }
}
