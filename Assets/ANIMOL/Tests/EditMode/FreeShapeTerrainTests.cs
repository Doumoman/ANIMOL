using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Tests
{
    [TestFixture(6)]
    [Category("CellBoundsRegression")]
    public sealed class FreeShapeTerrainTests
    {
        private readonly int artVersion;
        public FreeShapeTerrainTests(int artVersion){this.artVersion=artVersion;}
        private StageMapDefinition map;
        private TerrainEditorAdapter adapter;
        private string path,backup;
        [Serializable] private class VectorRecord {public string style;public int x,y,seed,variant;public uint hash;}
        [Serializable] private class Vectors {public int[] raw;public VectorRecord[] vectors;}
        [SetUp] public void Setup()
        {
            map=ScriptableObject.CreateInstance<StageMapDefinition>();var id="TEST-FREESHAPE-"+Guid.NewGuid().ToString("N");
            map.EditorInitializeIdentity(id,"T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            map.EditorTrySetCellBounds(new RectInt(-48,-48,128,128),false,out _);map.EditorMarkCollisionDataSynchronized();
            map.FreeShapeTerrain.artVersion=artVersion;
            path="Assets/"+id+".asset";backup=StageMapBackupService.BackupRoot+"/"+id;AssetDatabase.CreateAsset(map,path);AssetDatabase.SaveAssetIfDirty(map);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
        }
        [TearDown] public void Cleanup(){adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);if(AssetDatabase.IsValidFolder(backup))AssetDatabase.DeleteAsset(backup);}
        private void Edit(IEnumerable<Vector2Int> p,bool erase=false,string style="T01_A")
        {Assert.That(Engine.TryEditFreeShape(adapter.Terrain.Catalog,adapter.Read(),p,erase,style,out var c,out var error),Is.True,error);adapter.Commit(c,"FreeShape test");}
        private IEnumerable<Vector2Int> Rectangle(int x,int y,int width,int height)
        {for(int dy=0;dy<height;dy++)for(int dx=0;dx<width;dx++)yield return new Vector2Int(x+dx,y+dy);}
        [TestCase(1),TestCase(2),TestCase(3),TestCase(4),TestCase(5),TestCase(99)]
        public void RetiredArtVersionsCannotBeCommittedOrRendered(int retired)
        {
            Edit(Rectangle(-1,-1,3,3));
            var before=File.ReadAllText(path);var hash=map.ContentHash;
            var candidate=adapter.Read();candidate.freeShape.artVersion=retired;candidate.revision++;
            Assert.Throws<InvalidOperationException>(()=>adapter.Commit(candidate,"Rejected retired art"));
            Assert.Throws<InvalidOperationException>(()=>FreeShapeArtRegistry.Load(candidate.freeShape));
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));Assert.That(map.ContentHash,Is.EqualTo(hash));
        }
        [Test] public void IncompleteAndDuplicateRegistryCannotValidate()
        {
            var copy=UnityEngine.Object.Instantiate(FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion));
            try{copy.styles[0].cells[0]=null;Assert.Throws<InvalidOperationException>(()=>copy.ValidateComplete());}
            finally{UnityEngine.Object.DestroyImmediate(copy);}
            copy=UnityEngine.Object.Instantiate(FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion));
            try{copy.styles[1].styleId=copy.styles[0].styleId;Assert.Throws<InvalidOperationException>(()=>copy.ValidateComplete());}
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        [Test] public void V6WriteExceptionRollsBackSavedAsset()
        {
            Edit(new[]{new Vector2Int(-1,16)});var before=File.ReadAllText(path);var snapshot=EditorJsonUtility.ToJson(map);
            var candidate=adapter.Read();candidate.freeShape.seed=17;candidate.revision++;
            Animol.TerrainStructure.Editor.AnimolTerrainMapEditorBridge.Bind(map,adapter.Read,c=>{StageTerrainStructureIntegration.Write(map,c);throw new IOException("Injected persistence failure");},()=>{},map.StageId,adapter.Validate);
            Assert.Throws<IOException>(()=>Animol.TerrainStructure.Editor.AnimolTerrainMapEditorBridge.Commit(candidate,"Fail art save"));
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(snapshot));
        }
        [Test] public void All3760SpritesHaveCorrectStableRectDirectionAndSettings()
        {
            var a=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion);Assert.That(a.styles.Length,Is.EqualTo(20));var ids=new HashSet<string>();var textures=new HashSet<Texture2D>();
            foreach(var style in a.styles)
            {
                Assert.That(style.cells.Length,Is.EqualTo(188));Assert.That(style.motif.rect.size,Is.EqualTo(new Vector2(128,128)));Assert.That(style.motif.pivot,Is.EqualTo(Vector2.zero));
                for(int v=0;v<4;v++)for(int i=0;i<47;i++)
                {
                    var sprite=style.cells[v*47+i];Assert.That(sprite,Is.Not.Null);Assert.That(sprite.name,Is.EqualTo(style.styleId+"_v"+v+"_mask"+a.canonicalMasks[i].ToString("D3")));
                    Assert.That(sprite.rect,Is.EqualTo(new Rect(i%8*32,192-(i/8+1)*32,32,32)));Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));Assert.That(sprite.pivot,Is.EqualTo(new Vector2(16,16)));
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long local);Assert.That(ids.Add(guid+":"+local),Is.True);textures.Add(sprite.texture);
                }
            }
            Assert.That(ids.Count,Is.EqualTo(3760));Assert.That(textures.Count,Is.EqualTo(80));
            foreach(var t in textures){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));Assert.That(importer.sRGBTexture,Is.True);}
        }
        [Test] public void CSharpMatchesPackageJavaScript256RawAnd567PhaseHashVectors()
        {
            var vectors=JsonUtility.FromJson<Vectors>(File.ReadAllText("Docs/Validation/ThemeFinishV6/Expected/ReferenceVectors.json"));var art=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion);
            for(int i=0;i<256;i++){Assert.That(FreeShapeTopology.Canonical(i),Is.EqualTo(vectors.raw[i]));Assert.That(art.rawToCanonical[i],Is.EqualTo(vectors.raw[i]));Assert.That(art.canonicalMasks[art.rawToIndex[i]],Is.EqualTo(vectors.raw[i]));}
            foreach(var v in vectors.vectors){Assert.That(FreeShapeTopology.Variant(v.x,v.y,v.seed),Is.EqualTo(v.variant));Assert.That(FreeShapeTopology.Hash(v.style,v.x,v.y,v.seed),Is.EqualTo(v.hash));}
        }
        [Test] public void All16FixturesResolveOccupancyAndOperationalColliderIncludingEmptyHoles()
        {
            var registry=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion);var root=new GameObject("Free shape physical fixture");var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();
            try {foreach(var fixture in registry.fixtures)
            {
                var points=FreeShapeTopology.FromRows(fixture.rows,new Vector2Int(-1,-1)).ToHashSet();var dto=adapter.Read();dto.freeShape.cells.Clear();dto.revision++;
                dto.freeShape.cells=points.Select(p=>new FreeShapeCell{x=p.x,y=p.y,styleId="T01_A"}).ToList();adapter.Commit(dto,"Fixture");loader.Load(map);
                var collider=loader.ResolveLayer(StageMapLayer.Terrain).GetComponent<TilemapCollider2D>();collider.ProcessTilemapChanges();Physics2D.SyncTransforms();
                for(int y=-1;y<fixture.rows.Length-1;y++)for(int x=-1;x<fixture.rows[0].Length-1;x++)Assert.That(collider.OverlapPoint(new Vector2(x+.5f,y+.5f)),Is.EqualTo(points.Contains(new Vector2Int(x,y))),fixture.id+" "+x+","+y);
                var runtime=root.GetComponent<StageTerrainStructureRuntime>();Assert.That(runtime.LogicalGrid.solids.Count,Is.EqualTo(points.Count));
                var render=root.GetComponentInChildren<FreeShapeTerrainRenderer>();Assert.That(render.GetComponentsInChildren<Collider2D>(),Is.Empty);
                Assert.That(render.GetComponentsInChildren<Tilemap>().Sum(t=>t.GetUsedTilesCount()>0?t.GetTilesBlock(t.cellBounds).Count(v=>v!=null):0),Is.EqualTo(points.Count));
            }}finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void PaintEraseMoveDeleteUndoAndDiskRoundTripAreSingleTransactions()
        {
            var r=map.AuthoringRevision;Edit(Rectangle(14,14,5,5));Assert.That(map.AuthoringRevision,Is.EqualTo(r+1));Assert.That(map.Cells,Is.Empty);
            Edit(new[]{new Vector2Int(16,16)},true);Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(24));Assert.That(map.AuthoringRevision,Is.EqualTo(r+2));adapter.Undo();Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(25));adapter.Redo();
            Assert.That(Engine.TryMoveFreeShape(adapter.Terrain.Catalog,adapter.Read(),new Vector2Int(14,14),new Vector2Int(-16,-16),false,out var moved,out var error),Is.True,error);adapter.Commit(moved,"Move");
            var hash=map.ContentHash;var saved=File.ReadAllText(path);Assert.That(map.FreeShapeTerrain.cells.Any(c=>c.x==-2&&c.y==-2),Is.True);
            adapter.Undo();adapter.Redo();Assert.That(map.ContentHash,Is.EqualTo(hash));Assert.That(File.ReadAllText(path),Is.EqualTo(saved));
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Assert.That(adapter.Read().freeShape.cells.Count,Is.EqualTo(24));
            Engine.TryMoveFreeShape(adapter.Terrain.Catalog,adapter.Read(),new Vector2Int(-2,-2),Vector2Int.zero,true,out var deleted,out _);adapter.Commit(deleted,"Delete");Assert.That(map.FreeShapeTerrain.cells,Is.Empty);adapter.Undo();Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(24));
        }
        [Test] public void DiagonalAndDifferentStylesRemainSeparateAndGlobalSeamConnects()
        {
            Edit(new[]{new Vector2Int(15,15),new Vector2Int(16,15),new Vector2Int(17,16)});Edit(new[]{new Vector2Int(15,16)},false,"T01_B");
            var cells=FreeShapeTopology.Index(map.FreeShapeTerrain);Assert.That(FreeShapeTopology.Raw(cells,new Vector2Int(15,15)),Is.EqualTo(4));
            Assert.That(FreeShapeTopology.Component(map.FreeShapeTerrain,new Vector2Int(15,15)).Count,Is.EqualTo(2));Assert.That(FreeShapeTopology.Component(map.FreeShapeTerrain,new Vector2Int(17,16)).Count,Is.EqualTo(1));
            Assert.That(FreeShapeTopology.AffectedCells(new[]{new Vector2Int(15,15)}).Count,Is.EqualTo(9));
        }
        [Test] public void MotifMarginStyleLossAndV3OverlayCascadeAreDerivedWithUndo()
        {
            Edit(Rectangle(0,0,20,12));var cells=FreeShapeTopology.Index(map.FreeShapeTerrain);var origin=new Vector2Int(10,2);
            Assert.That(FreeShapeTopology.Hash("T01_A",10,2,0)%4,Is.Not.EqualTo(0));Assert.That(FreeShapeTopology.Motif(cells,origin,"T01_A",0),Is.True);
            Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),"T01_A_Fill_Source",origin,out var overlay,out var error);Assert.That(overlay,Is.Not.Null,error);adapter.Commit(overlay,"Overlay");
            Edit(new[]{new Vector2Int(9,1)},true);Assert.That(map.TerrainPlacements.placements,Is.Empty);Assert.That(FreeShapeTopology.Motif(FreeShapeTopology.Index(map.FreeShapeTerrain),origin,"T01_A",0),Is.False);
            adapter.Undo();Assert.That(map.TerrainPlacements.placements.Count,Is.EqualTo(1));Edit(new[]{new Vector2Int(9,1)},false,"T01_B");Assert.That(FreeShapeTopology.Motif(FreeShapeTopology.Index(map.FreeShapeTerrain),origin,"T01_A",0),Is.False);
        }
        [Test] public void V3OwnedCellsAndExistingOrdinaryCellsAreProtectedAndArchVoidAllowed()
        {
            Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),"T01_D_Source",Vector2Int.zero,out var stamp,out _);adapter.Commit(stamp,"Stamp");var before=map.ContentHash;
            foreach(bool erase in new[]{false,true})Assert.That(Engine.TryEditFreeShape(adapter.Terrain.Catalog,adapter.Read(),new[]{Vector2Int.zero},erase,"T01_A",out _,out _),Is.False);
            Assert.That(map.ContentHash,Is.EqualTo(before));Edit(new[]{new Vector2Int(5,0)});Assert.That(map.TerrainPlacements.placements.Single().instanceId,Is.EqualTo(stamp.placements.Single().instanceId));
        }
        [Test] public void InvalidContractsDuplicateStylesBoundsObjectsAndPreviewFailureLeaveMapIntact()
        {
            Edit(new[]{new Vector2Int(2,2)});var before=File.ReadAllText(path);var hash=map.ContentHash;
            foreach(var mutate in new Action<AnimolTerrainSavedMap>[] {c=>c.freeShape.schemaVersion=2,c=>c.freeShape.artVersion=5,c=>c.freeShape.artContract="wrong",c=>c.freeShape.cells.Add(c.freeShape.cells[0]),c=>c.freeShape.cells[0].styleId="T02_A",c=>c.freeShape.cells[0].x=9999})
            {var c=adapter.Read();c.revision++;mutate(c);Assert.Throws<InvalidOperationException>(()=>adapter.Commit(c,"Invalid"));Assert.That(map.ContentHash,Is.EqualTo(hash));Assert.That(File.ReadAllText(path),Is.EqualTo(before));}
            Engine.TryEditFreeShape(adapter.Terrain.Catalog,adapter.Read(),new[]{new Vector2Int(3,2)},false,"T01_A",out var candidate,out _);adapter.Changed+=()=>throw new InvalidOperationException("Injected free shape preview failure");
            Assert.Throws<InvalidOperationException>(()=>adapter.Commit(candidate,"Rollback"));Assert.That(map.ContentHash,Is.EqualTo(hash));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [Test] public void IncrementalRendererUpdatesThreeByThreeAndDependentMotifsOnly()
        {
            Edit(Rectangle(-2,-2,36,12));var go=new GameObject("Free visual");var render=go.AddComponent<FreeShapeTerrainRenderer>();
            try{render.Rebuild(map.FreeShapeTerrain,1);Assert.That(render.TilemapCount,Is.EqualTo(1));Edit(new[]{new Vector2Int(15,5)},true);render.Rebuild(map.FreeShapeTerrain,1);Assert.That(render.LastUpdatedCells,Is.EqualTo(9));Assert.That(render.LastUpdatedMotifs,Is.LessThanOrEqualTo(1));Assert.That(render.TilemapCount,Is.EqualTo(1));Assert.That(go.GetComponentsInChildren<Collider2D>(),Is.Empty);}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ObjectFootprintAndSweptPathSeeFreeShapeAndRejectBeforeSave()
        {
            map.EditorPlaceObject("START",StageMapObjectKind.PlayerStart,6,6,"START");EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
            Engine.TryEditFreeShape(adapter.Terrain.Catalog,adapter.Read(),new[]{new Vector2Int(6,6)},false,"T01_A",out var overlap,out _);var before=File.ReadAllText(path);Assert.Throws<InvalidOperationException>(()=>adapter.Commit(overlap,"Object overlap"));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            Edit(new[]{new Vector2Int(9,9)});var type=adapter.Objects.Find(StageMapObjectKind.RailPlatform);var settings=type.DefaultSettings.Clone();settings.EditorSetPathCells(new[]{new Vector2Int(5,9),new Vector2Int(12,9)});
            var item=new StageMapObjectPlacement("RAIL",type.Kind,5,9,type.StableTypeId,type.Prefab,settings);var validation=StageMapObjectAuthoringOperations.ValidatePlacement(map,item);Assert.That(validation.Errors.Any(e=>e.Contains("terrain")),Is.True,string.Join(";",validation.Errors));
        }
        [Test] public void PersistentSignedSeedChangesHashAndRestoresWithUndoAndReimport()
        {
            Edit(Rectangle(0,0,3,3));var hash=map.ContentHash;var c=adapter.Read();c.freeShape.seed=int.MinValue;c.revision++;adapter.Commit(c,"Seed");Assert.That(map.ContentHash,Is.Not.EqualTo(hash));adapter.Undo();Assert.That(map.FreeShapeTerrain.seed,Is.Zero);adapter.Redo();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Assert.That(adapter.Read().freeShape.seed,Is.EqualTo(int.MinValue));
        }
        [Test] public void SceneCandidateCancelAndCommitNeverModifySourceDuringDrag()
        {
            var previous=TerrainEditorSceneEditor.Adapter?.Map;try{
                TerrainEditorSceneEditor.Open(map);TerrainEditorSceneEditor.State.paletteHeight=.06f;TerrainEditorSceneEditor.SetFreeShapeMode(true);var state=TerrainEditorSceneEditor.State;Assert.That(state.paletteHeight,Is.GreaterThanOrEqualTo(.25f));state.freeStyle="T01_A";state.freeTool="Rect";
                var hash=map.ContentHash;var rev=map.AuthoringRevision;TerrainEditorSceneEditor.BeginFreeStroke(new Vector2Int(-1,-1));TerrainEditorSceneEditor.UpdateFreeStroke(new Vector2Int(18,8));Assert.That(TerrainEditorSceneEditor.FreeCandidateValid,Is.True,TerrainEditorSceneEditor.Status);Assert.That(map.ContentHash,Is.EqualTo(hash));TerrainEditorSceneEditor.Cancel(false);Assert.That(map.ContentHash,Is.EqualTo(hash));
                TerrainEditorSceneEditor.BeginFreeStroke(new Vector2Int(-1,-1));TerrainEditorSceneEditor.UpdateFreeStroke(new Vector2Int(18,8));TerrainEditorSceneEditor.CommitFreeStroke();Assert.That(map.AuthoringRevision,Is.EqualTo(rev+1));Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(200));
                TerrainEditorSceneEditor.Adapter.Undo();Assert.That(map.FreeShapeTerrain.cells,Is.Empty);TerrainEditorSceneEditor.Adapter.Redo();Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(200));
            }finally{TerrainEditorSceneEditor.Close();if(previous!=null)TerrainEditorSceneEditor.Open(previous);}
        }
        [Test] public void CommitWritesLocalBeforeChangeBackupWithoutImportingAndSavesEveryStroke()
        {
            var before=EditorJsonUtility.ToJson(map,true);var revision=map.AuthoringRevision;
            Edit(new[]{new Vector2Int(2,2)});
            var first=CampaignMapBackupStore.FindLatest(map);
            Assert.That(File.ReadAllText(first),Is.EqualTo(before));
            Assert.That(map.AuthoringRevision,Is.EqualTo(revision+1));
            before=EditorJsonUtility.ToJson(map,true);
            Edit(new[]{new Vector2Int(3,2)});
            var second=CampaignMapBackupStore.FindLatest(map);
            Assert.That(second,Is.Not.EqualTo(first));
            Assert.That(File.ReadAllText(second),Is.EqualTo(before));
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            Assert.That(adapter.Read().freeShape.cells.Count,Is.EqualTo(2));
            adapter.Undo();Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(1));
        }
    }
}
