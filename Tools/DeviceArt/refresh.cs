UnityEditor.AssetDatabase.Refresh();
return new { playing=UnityEditor.EditorApplication.isPlaying, dirty=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty };
