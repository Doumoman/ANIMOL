using UnityEngine;
using UnityEditor.SceneManagement;
using ANIMOL.Presentation.Editor;
public static class ObservatoryScene
{
    public static string Main()
    {
        if(EditorSceneManager.GetActiveScene().path=="" && GameObject.Find("Letter Observatory QA Camera"))EditorSceneManager.OpenScene(MemoryTheaterArtBuilder.ScenePath);
        return LetterObservatoryArtBuilder.BuildScene();
    }
}
