var screen=ANIMOL.Development.TerrainEditorScreen.Instance;
if(screen==null||!screen.IsTesting)throw new System.Exception("Local Play not active");
var map=screen.Adapter.Map;var version=map.FreeShapeTerrain.artVersion;
var loader=screen.TestLoader;
var collider=loader.ResolveLayer(ANIMOL.Core.StageMapLayer.Terrain).GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();collider.ProcessTilemapChanges();UnityEngine.Physics2D.SyncTransforms();
var cells=Animol.TerrainStructure.FreeShapeTopology.Index(map.FreeShapeTerrain);int empty=0;
for(int y=cells.Keys.Min(p=>p.y)-1;y<=cells.Keys.Max(p=>p.y)+1;y++)for(int x=cells.Keys.Min(p=>p.x)-1;x<=cells.Keys.Max(p=>p.x)+1;x++)
{
    var p=new UnityEngine.Vector2Int(x,y);bool occupied=cells.ContainsKey(p);
    if(collider.OverlapPoint((UnityEngine.Vector2)p+UnityEngine.Vector2.one*.5f)!=occupied)throw new System.Exception("Physics mismatch: "+p);
    if(!occupied)empty++;
}
var renderer=loader.GetComponentInChildren<ANIMOL.Gameplay.FreeShapeTerrainRenderer>();
var registry=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(map.FreeShapeTerrain);
var allowed=registry.styles.SelectMany(s=>s.cells).ToHashSet();
bool references=renderer.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().SelectMany(t=>t.GetTilesBlock(t.cellBounds)).OfType<UnityEngine.Tilemaps.Tile>().All(t=>allowed.Contains(t.sprite));
var player=screen.TestPlayer;
var result=new{isTesting=screen.IsTesting,artVersion=version,hash=map.ContentHash,runtimeHash=loader.Map.ContentHash,cells=cells.Count,emptyCellsChecked=empty,grounded=player.IsGrounded,position=player.transform.position.ToString(),artColliders=renderer.GetComponentsInChildren<UnityEngine.Collider2D>().Length,correctRegistry=references};
if(!references||result.artColliders!=0||result.hash!=result.runtimeHash||!result.grounded)throw new System.Exception("Local Play registry/physics/landing mismatch");
var camera=(UnityEngine.Camera)screen.GetType().GetField("testCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(screen);var rt=camera.targetTexture;
camera.Render();var previous=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;
var tex=new UnityEngine.Texture2D(rt.width,rt.height,UnityEngine.TextureFormat.RGBA32,false);tex.ReadPixels(new UnityEngine.Rect(0,0,rt.width,rt.height),0,0);tex.Apply();UnityEngine.RenderTexture.active=previous;
System.IO.File.WriteAllBytes("Docs/Validation/JointFinishV4/RuntimeRender-v"+version+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/LocalPlay-v"+version+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
