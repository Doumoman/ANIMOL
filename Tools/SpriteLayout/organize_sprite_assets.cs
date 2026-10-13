using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
public static class OrganizeSpriteAssets
{
    static List<KeyValuePair<string,string>> completed=new List<KeyValuePair<string,string>>();
    static string Current(string source){foreach(var move in completed)if(source==move.Key||source.StartsWith(move.Key+"/",StringComparison.OrdinalIgnoreCase))source=move.Value+source.Substring(move.Key.Length);return source;}
    static string Guid(string path)=>System.Text.RegularExpressions.Regex.Match(File.ReadAllText(path+".meta"),"(?m)^guid: ([0-9a-f]{32})").Groups[1].Value;
    static void Ensure(string path){if(AssetDatabase.IsValidFolder(path)||Directory.Exists(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Ensure(parent);string guid=AssetDatabase.CreateFolder(parent,Path.GetFileName(path));if(string.IsNullOrEmpty(guid))throw new Exception("Create folder failed "+path);}
    static void Move(string original,string destination)
    {
        string source=Current(original);if(source==destination)return;
        Ensure(Path.GetDirectoryName(destination).Replace('\\','/'));
        string guid=Guid(source);
        if(string.IsNullOrEmpty(guid))throw new Exception("Missing source "+source);
        if(string.Equals(source,destination,StringComparison.OrdinalIgnoreCase))
        {string temp=source+"_case_move";string error=AssetDatabase.MoveAsset(source,temp);if(error!="")throw new Exception(error);error=AssetDatabase.MoveAsset(temp,destination);if(error!="")throw new Exception(error);}
        else {string error=AssetDatabase.MoveAsset(source,destination);if(error!="")throw new Exception(error);}
        if(Guid(destination)!=guid)throw new Exception("GUID changed "+source);
        completed.Add(new KeyValuePair<string,string>(source,destination));
    }
    public static object Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Leave Play Mode first.");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene before moving.");
        var plan=JObject.Parse(File.ReadAllText("C:/Users/user/AppData/Local/Temp/animol-sprite-layout/plan.json"));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        bool editing=false;
        try
        {
            foreach(var row in plan["folders"])
            {
                string source=(string)row["source"],destination=(string)row["destination"];
                if(!Directory.Exists(Current(source)) && Directory.Exists(destination))completed.Add(new KeyValuePair<string,string>(source,destination));
                else Move(source,destination);
                AssetDatabase.Refresh();
            }
            foreach(var row in plan["leaves"])Ensure(Path.GetDirectoryName((string)row["destination"]).Replace('\\','/'));
            AssetDatabase.Refresh();AssetDatabase.StartAssetEditing();editing=true;
            foreach(var row in plan["leaves"])Move((string)row["source"],(string)row["destination"]);
            File.WriteAllText("Assets/ANIMOL/Data/art_import_manifest.json",plan["manifest"].ToString());
            File.WriteAllText("Assets/ANIMOL/Data/art_manifest.json",plan["full_manifest"].ToString());
            foreach(string file in Directory.GetFiles("Assets/ANIMOL/Scripts","*.cs",SearchOption.AllDirectories))
            {
                string code=File.ReadAllText(file);foreach(var pair in (JObject)plan["renames"])code=code.Replace(pair.Key,(string)pair.Value);
                code=code.Replace("ANIMOL.StarwayV04","ANIMOL.Presentation")
                    .Replace("Assets/ANIMOL/Art/StarwayPasture/V04","Assets/ANIMOL")
                    .Replace("Root+\"/unity_import_manifest.json\"","Root+\"/Data/art_import_manifest.json\"")
                    .Replace("Root+\"/Sequences/\"","Root+\"/Data/AnimationSequences/\"")
                    .Replace("Root+\"/QaMaterials/\"","Root+\"/Materials/\"")
                    .Replace("Root+\"/Clips/\"+row.id+\".anim\"","Root+\"/\"+row.clipPath")
                    .Replace("new[]{\"Sequences\",\"Clips\",\"Prefabs\",\"Scenes\",\"QaMaterials\"}","new[]{\"Data/AnimationSequences\",\"Prefabs\",\"Scenes\",\"Materials\"}")
                    .Replace("assetPath.StartsWith(ArtAssetBuilder.Root+\"/\",","assetPath.StartsWith(ArtAssetBuilder.Root+\"/Sprites/\",")
                    .Replace("public bool loop; public int totalMilliseconds;","public string clipPath; public bool loop; public int totalMilliseconds;")
                    .Replace("V04 HUD","Puzzle HUD").Replace("V04 ART QA","ART QA").Replace("V04 QA","Art QA").Replace("Starway V04","Art QA").Replace("V04_VALIDATED","ART_VALIDATED").Replace("Invalid V04 import","Invalid sprite import")
                    .Replace("Screenshots/StarwayV04","Screenshots/ArtQA");
                string originalName=((JObject)plan["renames"]).Properties().FirstOrDefault(p=>(string)p.Value==Path.GetFileNameWithoutExtension(file))?.Name;
                if(file.Contains("/Runtime/") && originalName!=null)
                    code=code.Replace("public sealed class ","[UnityEngine.Scripting.APIUpdating.MovedFrom(true, \"ANIMOL.StarwayV04\", \"ANIMOL.StarwayV04\", \""+originalName+"\")]\n    public sealed class ");
                File.WriteAllText(file,code);
            }
            foreach(string file in Directory.GetFiles("Assets/ANIMOL/Scripts","*.asmdef",SearchOption.AllDirectories))
            {
                var definition=JObject.Parse(File.ReadAllText(file));definition["name"]=Path.GetFileNameWithoutExtension(file);
                foreach(var reference in ((JArray)definition["references"]).ToArray())if(((string)reference).StartsWith("ANIMOL.StarwayV04"))reference.Replace(((string)reference).Replace("ANIMOL.StarwayV04","ANIMOL.Presentation"));
                File.WriteAllText(file,definition.ToString());
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/ANIMOL/Scenes/ANIMOL_Starway_Art_QA.unity",true)};
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/ANIMOL/Scenes/ANIMOL_Starway_Art_QA.unity");
            string old="Assets/ANIMOL/Art",archive="Legacy/PreviousProject_20261013/RetiredEmptyFolders/"+old;
            if(Directory.GetFiles(old,"*",SearchOption.AllDirectories).Any(p=>!p.EndsWith(".meta")))throw new Exception("Old Art still contains assets; do not archive.");
            Directory.CreateDirectory(Path.GetDirectoryName(archive));Directory.Move(old,archive);File.Move(old+".meta",archive+".meta");
            Directory.CreateDirectory("Docs/Validation/SpriteLayout");File.WriteAllLines("Docs/Validation/SpriteLayout/move_operations.tsv",completed.Select(p=>p.Key+"\t"+p.Value));
        }
        finally{if(editing)AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();}
        return new {moves=completed.Count,root="Assets/ANIMOL/Sprites",guidPreserved=true};
    }
}
