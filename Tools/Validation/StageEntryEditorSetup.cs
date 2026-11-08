using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StageEntryEditorSetup
{
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first");
        const string mainPath="Assets/ANIMOL/Scenes/Main/NotebookUI.unity";
        const string previewPath="Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity";
        var main=SceneManager.GetSceneByPath(mainPath);
        if(!main.IsValid()||!main.isLoaded)main=EditorSceneManager.OpenScene(mainPath,OpenSceneMode.Additive);
        var preview=SceneManager.GetSceneByPath(previewPath);
        if(preview.IsValid()&&preview.isLoaded&&preview.isDirty)
            throw new InvalidOperationException("The preview has unsaved edits; keep them and inspect before closing it.");
        SceneManager.SetActiveScene(main);
        if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.CloseScene(preview,true);
        return new{active=SceneManager.GetActiveScene().path,loadedScenes=SceneManager.sceneCount,previewClosed=true,sceneFilesSaved=false,buildSettingsChanged=false};
    }
}
