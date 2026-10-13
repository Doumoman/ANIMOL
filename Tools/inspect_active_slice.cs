var scenes = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i => new { path = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path, dirty = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty }).ToArray();
var deps = UnityEditor.AssetDatabase.GetDependencies("Assets/ANIMOL/Art/StarwayPasture/V04/Scenes/ANIMOL_Starway_Art_QA.unity", true);
System.IO.Directory.CreateDirectory("Docs/Validation/ActiveSlice");
System.IO.File.WriteAllLines("Docs/Validation/ActiveSlice/dependencies_before.txt", deps);
var graphics = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
var graphicsProperty = graphics ? new UnityEditor.SerializedObject(graphics).FindProperty("m_RenderPipelineGlobalSettingsMap") : null;
return new { play = UnityEditor.EditorApplication.isPlaying, scenes = scenes, graphicsMap = graphicsProperty == null ? "absent" : graphicsProperty.type + " array=" + graphicsProperty.isArray, globalSettingsPath=UnityEditor.AssetDatabase.GUIDToAssetPath("93b439a37f63240aca3dd4e01d978a9f"), preloaded = UnityEditor.PlayerSettings.GetPreloadedAssets().Select(a => UnityEditor.AssetDatabase.GetAssetPath(a)).ToArray(), external = deps.Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/ANIMOL/Art/StarwayPasture/V04/") && !p.StartsWith("Assets/ANIMOL/StarwayV04/")).ToArray() };
