using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Editor
{
    [InitializeOnLoad]
    public static class TerrainEditorSession
    {
        public const string ScenePath="Assets/ANIMOL/Scenes/Development/TerrainEditorV4.unity";
        private const string Key="ANIMOL.TerrainEditorV4.";
        public static bool Active => SessionState.GetBool(Key+"active",false);
        static TerrainEditorSession()
        {
            EditorApplication.playModeStateChanged += PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            EditorApplication.delayCall += () => { if(Active && EditorApplication.isPlaying) Build(); };
        }
        // Game View is only a temporary test session. Editing belongs to Scene View.
        public static void OpenForSceneTest(StageMapDefinition map)
        {
            SessionState.SetBool(Key+"sceneTest",true);
            Open(map);
        }
        public static void OpenSelected()
        {
            var map=Selection.activeObject as StageMapDefinition;
            if(map==null) map=StageMapAuthoringWorkspace.CurrentMap;
            if(map==null) { EditorUtility.DisplayDialog("맵 선택","Project에서 StageMapDefinition을 선택하거나 Campaign Map Editor에서 맵을 선택하세요.","확인");return; }
            TerrainEditorSceneEditor.Open(map);
        }
        public static void Open(StageMapDefinition map,string partId="")
        {
            if(map==null || !EditorUtility.IsPersistent(map)) throw new InvalidOperationException("저장된 StageMapDefinition이 필요합니다.");
            using(var check=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)))) check.Read();
            if(Active)
            {
                if(SessionState.GetString(Key+"guid","")==AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map))) { FocusGame();return; }
                var old=TerrainEditorScreen.Instance;if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                SessionState.SetString(Key+"guid",AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)));
                SessionState.SetString(Key+"view",JsonUtility.ToJson(new TerrainEditorViewState{partId=partId,center=map.EditorPreviewCellBounds.center*map.GetEditorPreviewUnitsPerCell()}));
                Build();return;
            }
            SessionState.SetString(Key+"guid",AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)));
            SessionState.SetString(Key+"selection",Selection.activeObject==null?"":GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeObject).ToString());
            SessionState.SetBool(Key+"startedPlay",!EditorApplication.isPlaying);
            SessionState.SetBool(Key+"background",Application.runInBackground);
            SessionState.SetString(Key+"startScene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetString(Key+"view",JsonUtility.ToJson(new TerrainEditorViewState {partId=partId,center=map.EditorPreviewCellBounds.center*map.GetEditorPreviewUnitsPerCell(),zoom=Mathf.Min(18,Mathf.Max(8,map.EditorPreviewCellBounds.height*.55f*map.GetEditorPreviewUnitsPerCell()))}));
            SessionState.SetBool(Key+"active",true);
            if(EditorApplication.isPlaying) Build();
            else
            {
                EnsureScene();
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                EditorApplication.isPlaying=true;
            }
            FocusGame();
        }
        private static void EnsureScene()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)return;
            if(!AssetDatabase.IsValidFolder("Assets/ANIMOL/Scenes/Development")) AssetDatabase.CreateFolder("Assets/ANIMOL/Scenes","Development");
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid())SceneManager.SetActiveScene(previous);
        }
        private static void PlayChanged(PlayModeStateChange state)
        {
            if(!Active)return;
            if(state==PlayModeStateChange.EnteredPlayMode) Build();
            if(state==PlayModeStateChange.ExitingPlayMode)
            { Remember(); UnityEngine.Object.FindFirstObjectByType<TerrainEditorSuspendedRoots>()?.Restore(); }
            if(state==PlayModeStateChange.EnteredEditMode) RestoreEditor();
        }
        private static void Build()
        {
            if(!Active || !EditorApplication.isPlaying)return;
            Application.runInBackground=true;
            var existing=UnityEngine.Object.FindFirstObjectByType<TerrainEditorScreen>();
            if(existing!=null && existing.Adapter!=null) { FocusGame();return; }
            var rootsKeeper=UnityEngine.Object.FindFirstObjectByType<TerrainEditorSuspendedRoots>();
            if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var scene=SceneManager.GetSceneByName("Terrain Editor live session");
            if(!scene.IsValid())scene=SceneManager.CreateScene("Terrain Editor live session");
            if(rootsKeeper==null)
            {
                var keeper=new GameObject("Terrain editor suspended roots");SceneManager.MoveGameObjectToScene(keeper,scene);
                rootsKeeper=keeper.AddComponent<TerrainEditorSuspendedRoots>();
                for(int i=0;i<SceneManager.sceneCount;i++)
                {
                    var other=SceneManager.GetSceneAt(i); if(other==scene || !other.isLoaded)continue;
                    foreach(var root in other.GetRootGameObjects().Where(r=>r.activeSelf)) {rootsKeeper.roots.Add(root);root.SetActive(false);}
                }
            }
            // The legacy workspace creates a DontSave root outside the enumerated scenes.
            // It is editor artwork, not part of the test map. Restore it when testing ends.
            foreach(var root in Resources.FindObjectsOfTypeAll<GameObject>().Where(r=>r.name=="[MAP PREVIEW - NOT SAVED]" && r.transform.parent==null && r.activeSelf && !EditorUtility.IsPersistent(r)))
            { rootsKeeper.roots.Add(root);root.SetActive(false); }
            var rootObject=new GameObject("ANIMOL Terrain Editor V4 session");SceneManager.MoveGameObjectToScene(rootObject,scene);
            var screen=rootObject.AddComponent<TerrainEditorScreen>();
            var state=JsonUtility.FromJson<TerrainEditorViewState>(SessionState.GetString(Key+"view","{}"));
            screen.Initialize(new TerrainEditorAdapter(SessionState.GetString(Key+"guid","")),state);
            if(SessionState.GetBool(Key+"sceneTest",false))
            {
                screen.ExitAfterTest=true;
                try { screen.EnterTest(); }
                catch(Exception ex) { Debug.LogError("Scene test: "+ex.Message);Close(); }
            }
            FocusGame();
        }
        private static void Remember()
        {
            var screen=TerrainEditorScreen.Instance;if(screen==null)return;
            SessionState.SetString(Key+"view",JsonUtility.ToJson(screen.State));
        }
        private static void BeforeReload()
        {
            Remember();
            var screen=TerrainEditorScreen.Instance;
            if(screen!=null)UnityEngine.Object.DestroyImmediate(screen.gameObject);
        }
        public static void Close()
        {
            Remember();
            if(SessionState.GetBool(Key+"startedPlay",false))EditorApplication.isPlaying=false;
            else
            {
                var screen=TerrainEditorScreen.Instance;if(screen!=null)UnityEngine.Object.DestroyImmediate(screen.gameObject);
                var keeper=UnityEngine.Object.FindFirstObjectByType<TerrainEditorSuspendedRoots>();
                if(keeper!=null)
                {
                    var previousPlayer=keeper.roots.Where(r=>r!=null).SelectMany(r=>r.GetComponentsInChildren<ANIMOL.Gameplay.DevPlayerController>(true)).FirstOrDefault();
                    keeper.Restore();
                    if(previousPlayer!=null)typeof(ANIMOL.Gameplay.DevPlayerController).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{previousPlayer});
                    UnityEngine.Object.DestroyImmediate(keeper.gameObject);
                }
                var scene=SceneManager.GetSceneByName("Terrain Editor live session");if(scene.IsValid())SceneManager.UnloadSceneAsync(scene);
                RestoreEditor();
            }
        }
        private static void RestoreEditor()
        {
            Application.runInBackground=SessionState.GetBool(Key+"background",false);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+"startScene",""));
            if(GlobalObjectId.TryParse(SessionState.GetString(Key+"selection",""),out var id))Selection.activeObject=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            SessionState.SetBool(Key+"active",false);
            SessionState.SetBool(Key+"sceneTest",false);
        }
        private static void FocusGame()
        { var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(type).Focus(); }
    }
}
