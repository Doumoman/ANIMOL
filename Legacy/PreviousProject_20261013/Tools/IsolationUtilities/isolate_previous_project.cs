using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class IsolatePreviousProject
{
    const string Archive="Legacy/PreviousProject_20261013";
    const string V04="Assets/ANIMOL/Art/StarwayPasture/V04";
    static List<string> moved=new List<string>();
    static void Move(string source)
    {
        if(!File.Exists(source) && !Directory.Exists(source))return;
        string full=Path.GetFullPath(source), destination=Path.GetFullPath(Archive+"/"+source);
        if(!full.StartsWith(Path.GetFullPath(".")+Path.DirectorySeparatorChar) || !destination.StartsWith(Path.GetFullPath(Archive)+Path.DirectorySeparatorChar))throw new Exception("Unsafe path");
        if(File.Exists(destination)||Directory.Exists(destination))throw new Exception("Archive collision "+destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if(Directory.Exists(source))Directory.Move(source,destination);else File.Move(source,destination);
        moved.Add(source.Replace('\\','/'));
        if(!source.EndsWith(".meta") && File.Exists(source+".meta"))Move(source+".meta");
    }
    static void Children(string root,params string[] keep)
    {if(!Directory.Exists(root))return;foreach(string path in Directory.GetFileSystemEntries(root).OrderBy(p=>p))if(!keep.Contains(Path.GetFileName(path)))Move(path.Replace('\\','/'));}
    public static object Execute()
    {
        if(File.Exists("Docs/Validation/ActiveSlice/moved_roots.txt"))throw new Exception("Isolation already completed. Use the manifest to restore; do not run the migration twice.");
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before isolation.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene: preserve it before isolation.");
        var automation=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.NamedArt.Editor.NamedArtAutomation")).FirstOrDefault(t=>t!=null);
        if(automation!=null){automation.GetField("Suspended").SetValue(null,true);automation.GetMethod("ResetQueue").Invoke(null,null);}
        Directory.CreateDirectory(Archive);Directory.CreateDirectory(V04+"/Fonts");
        AssetDatabase.Refresh();
        var dependencies=AssetDatabase.GetDependencies(V04+"/Scenes/ANIMOL_Starway_Art_QA.unity",true).Where(p=>p.StartsWith("Assets/ANIMOL/Typography/")||p.StartsWith("Assets/ANIMOL/Fonts/")).ToArray();
        var fontMoves=new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(string path in dependencies)
            {
                string snapshot=Archive+"/SharedDependenciesSnapshot/"+path;Directory.CreateDirectory(Path.GetDirectoryName(snapshot));
                if(!File.Exists(snapshot))File.Copy(path,snapshot,false);else if(!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(snapshot)))throw new Exception("Snapshot mismatch "+path);
                if(!File.Exists(snapshot+".meta"))File.Copy(path+".meta",snapshot+".meta",false);else if(!File.ReadAllBytes(path+".meta").SequenceEqual(File.ReadAllBytes(snapshot+".meta")))throw new Exception("Snapshot meta mismatch "+path);
                string destination=V04+"/Fonts/"+Path.GetFileName(path);string error=AssetDatabase.MoveAsset(path,destination);if(!string.IsNullOrEmpty(error))throw new Exception(error);
                fontMoves.Add(path+"\t"+destination);
            }
            EditorSceneManager.OpenScene(V04+"/Scenes/ANIMOL_Starway_Art_QA.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(V04+"/Scenes/ANIMOL_Starway_Art_QA.unity",true)};
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(V04+"/Scenes/ANIMOL_Starway_Art_QA.unity");
            var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if(graphics){var settings=new SerializedObject(graphics);var map=settings.FindProperty("m_RenderPipelineGlobalSettingsMap");if(map!=null && map.isArray){map.ClearArray();settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(graphics);}}
            var quality=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if(quality){var settings=new SerializedObject(quality);var levels=settings.FindProperty("m_QualitySettings");for(int i=0;i<levels.arraySize;i++){var pipeline=levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");if(pipeline!=null)pipeline.objectReferenceValue=null;}settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(quality);}
            AssetDatabase.SaveAssets();
            Children("Assets/ANIMOL","Art","Art.meta","StarwayV04","StarwayV04.meta");
            Children("Assets/ANIMOL/Art","StarwayPasture","StarwayPasture.meta");
            Children("Assets/ANIMOL/Art/StarwayPasture","V04","V04.meta");
            Children("Assets","ANIMOL","ANIMOL.meta","TextMesh Pro","TextMesh Pro.meta");
            Children("Docs/Inbox","ANIMOL_REFERENCE_SPRITES_V04");
            Children("Docs/Validation","StarwayV04","ActiveSlice");
            Children("Docs","Inbox","Validation","ANIMOL_SPRITE_PATHS_AND_NAMES.md","ANIMOL_ACTIVE_PROJECT_LAYOUT.md");
            Children("Screenshots","StarwayV04");
            Children("MCP_INBOX","ANIMOL_REFERENCE_SPRITES_V04.zip");
            Children("Tools","isolate_previous_project.cs","inspect_active_slice.cs","verify_active_slice.py","stage_active_slice.py");
            Move("AgentScripts");Move("Builds");
            foreach(string path in Directory.GetFiles("ProjectSettings","ANIMOL*"))Move(path.Replace('\\','/'));
            foreach(string path in Directory.GetFiles("."))
            {
                string name=Path.GetFileName(path);
                if(name=="ANIMOL_V04_ART_APPLY_RESULT.md")continue;
                if(name.StartsWith("ANIMOL_")||name.StartsWith("UNITY_APPLY_")||name.StartsWith("OVG")||name.StartsWith("animol-op"))Move(name);
            }
            File.WriteAllLines("Docs/Validation/ActiveSlice/moved_roots.txt",moved);
            File.WriteAllLines("Docs/Validation/ActiveSlice/font_moves.tsv",fontMoves);
        }
        finally{AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();}
        return new {archivedRoots=moved.Count,fontAssets=fontMoves.Count,activeScene=EditorSceneManager.GetActiveScene().path};
    }
}
