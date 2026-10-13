// Run in the connected Editor after CreateDevelopmentMaps.cs.
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var results=new System.Collections.Generic.List<object>();
try
{
    foreach(var theme in new[]{"T01","T02","T03","T04","T05"})
    {
        var path="Assets/ANIMOL/Data/Development/CleanArtV2/DEV-CLEAN-V2-"+theme+".asset";
        var map=UnityEditor.AssetDatabase.LoadAssetAtPath<ANIMOL.Core.StageMapDefinition>(path);
        var root=new UnityEngine.GameObject(theme);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        try
        {
            var loader=root.AddComponent<ANIMOL.Gameplay.StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();loader.Load(map);
            var collider=loader.ResolveLayer(ANIMOL.Core.StageMapLayer.Terrain).GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();collider.ProcessTilemapChanges();UnityEngine.Physics2D.SyncTransforms();
            var cells=Animol.TerrainStructure.FreeShapeTopology.Index(map.FreeShapeTerrain);int empty=0;
            for(int y=cells.Keys.Min(p=>p.y)-1;y<=cells.Keys.Max(p=>p.y)+1;y++)for(int x=cells.Keys.Min(p=>p.x)-1;x<=cells.Keys.Max(p=>p.x)+1;x++)
            {
                var p=new UnityEngine.Vector2Int(x,y);bool occupied=cells.ContainsKey(p);
                if(collider.OverlapPoint((UnityEngine.Vector2)p+UnityEngine.Vector2.one*.5f)!=occupied)throw new System.Exception("Physics mismatch: "+theme+" "+p);
                if(!occupied)empty++;
            }
            int artColliders=root.GetComponentInChildren<ANIMOL.Gameplay.FreeShapeTerrainRenderer>().GetComponentsInChildren<UnityEngine.Collider2D>().Length;
            if(artColliders!=0)throw new System.Exception("Unexpected art collider");
            results.Add(new{path=path,cells=cells.Count,styles=cells.Values.Select(c=>c.styleId).Distinct().Count(),artVersion=map.FreeShapeTerrain.artVersion,emptyCellsChecked=empty,artColliders=artColliders});
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/SavedDevelopmentValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
return results;
