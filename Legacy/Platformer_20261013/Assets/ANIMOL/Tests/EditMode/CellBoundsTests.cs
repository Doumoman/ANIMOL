using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    [Category("CellBoundsRegression")]
    public sealed class CellBoundsTests
    {
        private StageMapDefinition map;
        private TerrainEditorAdapter adapter;
        private string path;
        [SetUp] public void Setup()
        {
            map=ScriptableObject.CreateInstance<StageMapDefinition>();
            map.EditorInitializeIdentity("TEST-CELL-BOUNDS","T01");
            map.EditorInitializeBoundsFromAuthoredContent(1);
            path="Assets/TEST-CELL-BOUNDS-"+Guid.NewGuid().ToString("N")+".asset";
            AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
        }
        [TearDown] public void Cleanup(){adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);}
        [Test] public void ArbitraryNegativeBoundsSaveAndUndoWithoutSnapping()
        {
            var before=File.ReadAllText(path);var revision=map.AuthoringRevision;var hash=map.ContentHash;
            var bounds=new RectInt(-19,-7,37,23);adapter.ResizeMap(bounds);
            Assert.That(map.CellBounds,Is.EqualTo(bounds));Assert.That(map.AuthoringRevision,Is.EqualTo(revision+1));
            Assert.That(map.CollisionDataRevision,Is.EqualTo(-1));
            Assert.That(map.ContentHash,Is.Not.EqualTo(hash));Assert.That(map.ContainsCell(-19,-7),Is.True);
            Assert.That(map.ContainsCell(17,15),Is.True);Assert.That(map.ContainsCell(18,15),Is.False);
            var saved=File.ReadAllText(path);adapter.Undo();Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            adapter.Redo();Assert.That(File.ReadAllText(path),Is.EqualTo(saved));
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Assert.That(map.CellBounds,Is.EqualTo(bounds));
            adapter.ResizeMap(bounds);Assert.That(map.AuthoringRevision,Is.EqualTo(revision+1));
        }
        [Test] public void ResizeRejectsFreeShapeAndObjectFootprintsOutsideBounds()
        {
            map.EditorPlaceObject("START",StageMapObjectKind.PlayerStart,20,10,"START");
            var dto=adapter.Read();dto.revision++;dto.freeShape.cells.Add(new FreeShapeCell{x=28,y=12,styleId="T01_A"});adapter.Commit(dto,"Terrain");
            var before=File.ReadAllText(path);Assert.Throws<InvalidOperationException>(()=>adapter.ResizeMap(new RectInt(0,0,28,24)));
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(()=>adapter.ResizeMap(new RectInt(21,0,11,24)));
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [Test] public void ZeroOverflowAndPreviewFailureLeaveDiskAndBoundsIntact()
        {
            var before=File.ReadAllText(path);var bounds=map.CellBounds;
            Assert.Throws<InvalidOperationException>(()=>adapter.ResizeMap(new RectInt(0,0,0,8)));
            Assert.Throws<InvalidOperationException>(()=>adapter.ResizeMap(new RectInt(int.MaxValue,0,2,8)));
            bool fail=true;adapter.Changed+=()=>{if(fail){fail=false;throw new InvalidOperationException("Injected preview failure");}};
            Assert.Throws<InvalidOperationException>(()=>adapter.ResizeMap(new RectInt(-3,-3,35,27)));
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));Assert.That(map.CellBounds,Is.EqualTo(bounds));
        }
        [Test] public void GlobalArtUsesOneTilemapAcrossFormerSixteenCellBoundaries()
        {
            var layer=new FreeShapeLayer();foreach(int x in new[]{-17,-16,-1,0,15,16,31,32})layer.cells.Add(new FreeShapeCell{x=x,y=-17,styleId="T01_A"});
            var go=new GameObject("Global V6 terrain");try{var render=go.AddComponent<FreeShapeTerrainRenderer>();render.Rebuild(layer,1);
                Assert.That(render.TilemapCount,Is.EqualTo(1));Assert.That(go.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Length,Is.EqualTo(1));
                Assert.That(go.GetComponentsInChildren<Collider2D>(),Is.Empty);
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void LegacyVersionAndLegacyBoundsBackupsCannotRestore()
        {
            var json=EditorJsonUtility.ToJson(map);var file=Path.GetTempFileName();var before=File.ReadAllText(path);
            try{File.WriteAllText(file,json.Replace("\"artVersion\":6","\"artVersion\":4"));Assert.Throws<InvalidOperationException>(()=>CampaignMapBackupStore.Restore(map,file));
                File.WriteAllText(file,json.Replace("minCellX","minChunkX").Replace("cellWidth","chunkWidth"));Assert.Throws<InvalidOperationException>(()=>CampaignMapBackupStore.Restore(map,file));
                Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            }finally{File.Delete(file);}
        }
    }
}
