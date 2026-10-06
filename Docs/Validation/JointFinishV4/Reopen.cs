var adapter=ANIMOL.Editor.TerrainEditorSceneEditor.Adapter;var map=adapter.Map;adapter.Save();
var hash=map.ContentHash;var revision=map.AuthoringRevision;var file=UnityEditor.AssetDatabase.GetAssetPath(map);var bytes=System.IO.File.ReadAllBytes(file);
ANIMOL.Editor.TerrainEditorSceneEditor.Close();UnityEditor.AssetDatabase.ImportAsset(file,UnityEditor.ImportAssetOptions.ForceUpdate);
map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(file);
ANIMOL.Editor.TerrainEditorSceneEditor.Open(map);ANIMOL.Editor.TerrainEditorSceneEditor.SetFreeShapeMode(true);ANIMOL.Editor.TerrainEditorSceneEditor.State.masks=false;
ANIMOL.Editor.TerrainEditorSceneEditor.View.LookAtDirect(new UnityEngine.Vector3(14,3,0),UnityEngine.Quaternion.identity,20);
var result=new{sameHash=hash==map.ContentHash,sameRevision=revision==map.AuthoringRevision,sameFile=bytes.SequenceEqual(System.IO.File.ReadAllBytes(file)),artVersion=map.FreeShapeTerrain.artVersion,cells=map.FreeShapeTerrain.cells.Count,hash=map.ContentHash};
if(!result.sameHash || !result.sameRevision || !result.sameFile || result.artVersion!=4)throw new System.Exception("Reopen mismatch");
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/Reopen.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
