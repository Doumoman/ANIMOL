ANIMOL.Presentation.Editor.ArtAssetBuilder.Build();
ANIMOL.Presentation.Editor.ArtAssetBuilder.Validate();
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=null;
return new {scene=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path, rebuilt=true, testsCanOwnScene=true};
