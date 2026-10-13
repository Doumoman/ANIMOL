return new {
    unityVersion=UnityEngine.Application.unityVersion,
    activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    playing=UnityEditor.EditorApplication.isPlaying,
    compiling=UnityEditor.EditorApplication.isCompiling,
    gridComponents=UnityEngine.Object.FindObjectsByType<UnityEngine.Grid>(UnityEngine.FindObjectsSortMode.None).Length,
    buildScenes=UnityEditor.EditorBuildSettings.scenes.Select(s=>new {s.path,s.enabled}).ToArray(),
    themeCatalogs=UnityEditor.AssetDatabase.FindAssets("t:DeviceThemeCatalog",new[]{"Assets/ANIMOL"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray()
};
