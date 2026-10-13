// Run inside the connected Editor with unity command eval_file --file <this path>.
// Explicit validation action only; initialization/window opening never calls this.
var folder="Assets/ANIMOL/Data/Development/CleanArtV2";
if(!UnityEditor.AssetDatabase.IsValidFolder(folder))UnityEditor.AssetDatabase.CreateFolder("Assets/ANIMOL/Data/Development","CleanArtV2");
var source="Assets/ANIMOL/Data/Development/FreeShape/DEV-FREESHAPE.asset";
var copyPath=folder+"/DEV-CLEAN-V2-COPY.asset";
if(UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(copyPath)!=null)throw new System.InvalidOperationException("Validation maps already exist; do not overwrite authored data.");
if(!UnityEditor.AssetDatabase.CopyAsset(source,copyPath))throw new System.InvalidOperationException("Could not copy development map.");
var copy=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(copyPath);
// Preserve the copied map's stable identity. This isolated asset is never assigned to a stage.
UnityEditor.EditorUtility.SetDirty(copy);UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
var before=UnityEditor.EditorJsonUtility.ToJson(copy);
using(var adapter=new ANIMOL.Editor.TerrainEditorAdapter(UnityEditor.AssetDatabase.AssetPathToGUID(copyPath)))adapter.ChangeFreeShapeArtVersion(2);
System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/DevelopmentCopyConversion.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source=source,path=copyPath,before=before,after=UnityEditor.EditorJsonUtility.ToJson(copy)},Newtonsoft.Json.Formatting.Indented));
var art=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,2);
var records=new System.Collections.Generic.List<object>();
foreach(var theme in new[]{"T01","T02","T03","T04","T05"})
{
    var map=UnityEngine.ScriptableObject.CreateInstance<ANIMOL.Core.StageMapDefinition>();
    var id="DEV-CLEAN-V2-"+theme;var path=folder+"/"+id+".asset";
    map.EditorInitializeIdentity(id,theme);map.EditorInitializeVariableChunksFromAuthoredContent(1);map.EditorTrySetChunkBounds(new UnityEngine.RectInt(-2,-2,24,10),false,out _);
    UnityEditor.AssetDatabase.CreateAsset(map,path);
    using(var adapter=new ANIMOL.Editor.TerrainEditorAdapter(UnityEditor.AssetDatabase.AssetPathToGUID(path)))
    {
        var dto=adapter.Read();int row=0;
        foreach(var style in art.styles.Where(s=>s.themeId==theme))
        {
            int column=0;
            foreach(var fixture in art.fixtures)
            {
                var origin=new UnityEngine.Vector2Int(-17+column*22,-17+row*26);
                foreach(var p in Animol.TerrainStructure.FreeShapeTopology.FromRows(fixture.rows,origin))dto.freeShape.cells.Add(new Animol.TerrainStructure.FreeShapeCell{x=p.x,y=p.y,styleId=style.styleId});
                column++;
            }
            row++;
        }
        dto.revision++;adapter.Commit(dto,"Create isolated art validation fixtures");adapter.ChangeFreeShapeArtVersion(2);
        records.Add(new{path=path,cells=map.FreeShapeTerrain.cells.Count,artVersion=map.FreeShapeTerrain.artVersion,seed=map.FreeShapeTerrain.seed});
    }
}
UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/DevelopmentMaps.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
return records;
