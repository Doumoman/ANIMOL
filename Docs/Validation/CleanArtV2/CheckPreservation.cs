// Compare every initial map's complete serialized Editor JSON and all v1 Sprite IDs.
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/CleanArtV2Work/baseline-unity.json"));
var maps=new System.Collections.Generic.List<object>();
foreach(var row in baseline["maps"])
{
    var path=(string)row["path"];var map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
    maps.Add(new{path=path,jsonEqual=UnityEditor.EditorJsonUtility.ToJson(map)==(string)row["json"],hashEqual=map.ContentHash==(string)row["hash"],revisionEqual=map.AuthoringRevision==(int)row["revision"],artVersion=map.FreeShapeTerrain.artVersion});
}
var a=ANIMOL.Gameplay.FreeShapeArtRegistry.Load();
var ids=a.styles.SelectMany(s=>s.cells.Concat(new[]{s.motif})).Select(s=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()).ToArray();
var beforeIds=baseline["refs"].Select(r=>(string)r["id"]).ToArray();
var result=new{maps=maps,v1References=ids.Length,v1ReferencesEqual=ids.SequenceEqual(beforeIds)};
System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/UnityPreservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
