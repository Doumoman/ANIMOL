// Baseline is captured before compilation/import/conversion of v4.
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/JointFinishV4Work/baseline-unity.json"));
var maps=baseline["maps"].Select(row=>{
    var path=(string)row["path"];var map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
    return new{path=path,jsonEqual=UnityEditor.EditorJsonUtility.ToJson(map)==(string)row["json"],hashEqual=map.ContentHash==(string)row["hash"],revisionEqual=map.AuthoringRevision==(int)row["revision"],artVersion=map.FreeShapeTerrain.artVersion};
}).ToArray();
var refs=baseline["refs"].Select(row=>{
    var version=(int)row["version"];
    var ids=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,version).styles.SelectMany(s=>s.cells.Concat(new[]{s.motif})).Select(s=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()).ToArray();
    return new{version=version,count=ids.Length,equal=ids.SequenceEqual(row["ids"].Select(id=>(string)id))};
}).ToArray();
var result=new{maps=maps,references=refs};
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/UnityPreservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
if(maps.Any(m=>!m.jsonEqual||!m.hashEqual||!m.revisionEqual)||refs.Any(r=>!r.equal))throw new System.Exception("Existing map/art changed");
return new{maps=maps.Length,references=refs.Sum(r=>r.count),preserved=true};
