var path="Assets/ANIMOL/Data/Development/JointFinishV4/DEV-JOINT-V4-COPY.asset";
var map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
var before=UnityEditor.EditorJsonUtility.ToJson(map);var disk=System.IO.File.ReadAllBytes(path);var results=new System.Collections.Generic.List<object>();
using(var adapter=new ANIMOL.Editor.TerrainEditorAdapter(UnityEditor.AssetDatabase.AssetPathToGUID(path)))
foreach(int version in new[]{3,5}){
 bool commitRejected=false,runtimeRejected=false,selectionRejected=false;
 try{adapter.ChangeFreeShapeArtVersion(version);}catch(System.InvalidOperationException){selectionRejected=true;}
 var dto=adapter.Read();dto.freeShape.artVersion=version;dto.revision++;
 try{adapter.Commit(dto,"Reject unavailable art");}catch(System.InvalidOperationException){commitRejected=true;}
 var copy=UnityEngine.Object.Instantiate(map);copy.FreeShapeTerrain.artVersion=version;
 var root=new UnityEngine.GameObject("Unavailable art runtime probe");var loader=root.AddComponent<ANIMOL.Gameplay.StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();
 try{try{loader.Load(copy);}catch(System.InvalidOperationException){runtimeRejected=true;}}
 finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(copy);}
 if(!selectionRejected||!commitRejected||!runtimeRejected||before!=UnityEditor.EditorJsonUtility.ToJson(map)||!disk.SequenceEqual(System.IO.File.ReadAllBytes(path)))throw new System.Exception("Unavailable art was not rejected atomically: "+version);
 results.Add(new{version=version,selectionRejected=selectionRejected,commitRejected=commitRejected,runtimeRejected=runtimeRejected,unchanged=true});
}
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/MissingVersionRejection.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
return results;
