// Set JointFinishV4.TargetVersion in SessionState, then invoke this explicit validation action.
var target=UnityEditor.SessionState.GetInt("JointFinishV4.TargetVersion",4);
var path="Assets/ANIMOL/Data/Development/JointFinishV4/DEV-JOINT-V4-COPY.asset";
var map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
ANIMOL.Editor.TerrainEditorSceneEditor.Open(map);ANIMOL.Editor.TerrainEditorSceneEditor.SetFreeShapeMode(true);
var adapter=ANIMOL.Editor.TerrainEditorSceneEditor.Adapter;var before=adapter.Read();var beforeRevision=map.AuthoringRevision;var beforeHash=map.ContentHash;var original=UnityEditor.EditorJsonUtility.ToJson(map);
ANIMOL.Editor.TerrainEditorSceneEditor.ChangeFreeShapeArtVersion(target);
if(before.freeShape.artVersion!=target){
 if(map.AuthoringRevision!=beforeRevision+1||map.ContentHash==beforeHash)throw new System.Exception("Invalid conversion revision/hash");
 var changed=adapter.Read();changed.freeShape.artVersion=before.freeShape.artVersion;changed.revision=before.revision;
 if(UnityEngine.JsonUtility.ToJson(changed)!=UnityEngine.JsonUtility.ToJson(before))throw new System.Exception("Conversion changed terrain data");
 adapter.Undo();if(UnityEditor.EditorJsonUtility.ToJson(map)!=original)throw new System.Exception("Conversion Undo mismatch");
 adapter.Redo();if(map.FreeShapeTerrain.artVersion!=target)throw new System.Exception("Conversion Redo mismatch");
}
adapter.Save();var bytes=System.IO.File.ReadAllBytes(path);var hash=map.ContentHash;var revision=map.AuthoringRevision;
ANIMOL.Editor.TerrainEditorSceneEditor.Close();UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceUpdate);
map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
if(map.FreeShapeTerrain.artVersion!=target||map.ContentHash!=hash||map.AuthoringRevision!=revision||!bytes.SequenceEqual(System.IO.File.ReadAllBytes(path)))throw new System.Exception("Saved map reopen mismatch");
ANIMOL.Editor.TerrainEditorSceneEditor.Open(map);ANIMOL.Editor.TerrainEditorSceneEditor.SetFreeShapeMode(true);ANIMOL.Editor.TerrainEditorSceneEditor.State.masks=false;ANIMOL.Editor.TerrainEditorSceneEditor.State.freeStyle="T01_A";
ANIMOL.Editor.TerrainEditorSceneEditor.View.position=new UnityEngine.Rect(60,60,1300,900);
ANIMOL.Editor.TerrainEditorSceneEditor.View.LookAtDirect(new UnityEngine.Vector3(14,3,0),UnityEngine.Quaternion.identity,20);
var result=new{version=target,previousVersion=before.freeShape.artVersion,cells=map.FreeShapeTerrain.cells.Count,seed=map.FreeShapeTerrain.seed,objects=map.Objects.Count,revision=revision,hash=hash,undoRedo=true,savedReopened=true};
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/SelectSaveReopen-v"+target+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
