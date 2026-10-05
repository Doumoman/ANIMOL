using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Tests
{
    public sealed class TerrainEditorV4Tests
    {
        private StageMapDefinition map;
        private TerrainEditorAdapter adapter;
        private string path,backup;
        [SetUp] public void Setup()
        {
            map=ScriptableObject.CreateInstance<StageMapDefinition>();
            var id="TEST-UIV4-"+Guid.NewGuid().ToString("N");
            map.EditorInitializeIdentity(id,"T01");map.EditorInitializeVariableChunksFromAuthoredContent(1);
            map.EditorTrySetChunkBounds(new RectInt(-2,-2,5,5),false,out _);map.EditorMarkCollisionDataSynchronized();
            path="Assets/"+id+".asset";backup=StageMapBackupService.BackupRoot+"/"+id;
            AssetDatabase.CreateAsset(map,path);AssetDatabase.SaveAssetIfDirty(map);
            adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
        }
        [TearDown] public void Cleanup()
        {
            adapter?.Dispose();if(map!=null)Undo.ClearUndo(map);
            AssetDatabase.DeleteAsset(path);if(AssetDatabase.IsValidFolder(backup))AssetDatabase.DeleteAsset(backup);
        }
        private string Place(string id,Vector2Int p)
        {
            Assert.That(Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),id,p,out var candidate,out var error),Is.True,error);
            adapter.Commit(candidate,"V4 test placement");return candidate.placements.Last().instanceId;
        }
        [Test] public void PlacementMoveDeleteAreSingleRevisionWithNativeUndoAndDisk()
        {
            var rev=map.AuthoringRevision;var id=Place("T01_D_Source",new Vector2Int(-1,-1));
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev+1));Assert.That(map.CollisionDataRevision,Is.EqualTo(-1));
            Assert.That(map.ResolveTerrain(adapter.Terrain).solids.Count,Is.EqualTo(52));
            Assert.That(Engine.TryMove(adapter.Terrain.Catalog,adapter.Read(),id,new Vector2Int(15,15),out var move,out _),Is.True);
            adapter.Commit(move,"Move");var saved=File.ReadAllText(path);
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev+2));
            var e=adapter.Terrain.Catalog.entries.Single(x=>x.id=="T01_D_Source");
            Assert.That(Engine.GetCoveredChunks(e,new Vector2Int(15,15)).Length,Is.EqualTo(4));
            adapter.Undo();Assert.That(adapter.Read().placements.Single().x,Is.EqualTo(-1));
            adapter.Redo();Assert.That(adapter.Read().placements.Single().instanceId,Is.EqualTo(id));Assert.That(File.ReadAllText(path),Is.EqualTo(saved));
            Assert.That(Engine.TryDelete(adapter.Terrain.Catalog,adapter.Read(),id,out var delete,out _),Is.True);adapter.Commit(delete,"Delete");
            Assert.That(adapter.Read().placements,Is.Empty);adapter.Undo();Assert.That(adapter.Read().placements.Single().maskHash,Is.EqualTo(e.maskHash));
        }
        [Test] public void OrdinaryBrushProtectsOwnedCellsAndPaintsArchVoid()
        {
            Place("T01_D_Source",Vector2Int.zero);
            Assert.That(Engine.TrySetBaseCell(adapter.Terrain.Catalog,adapter.Read(),Vector2Int.zero,false,null,out _,out _),Is.False);
            Assert.That(Engine.TrySetBaseCell(adapter.Terrain.Catalog,adapter.Read(),new Vector2Int(5,0),true,"T01_A",out var candidate,out var error),Is.True,error);
            adapter.Commit(candidate,"Arch step");Assert.That(map.ResolveTerrain(adapter.Terrain).solids.Count,Is.EqualTo(53));
            var id=adapter.Read().placements.Single().instanceId;Engine.TryDelete(adapter.Terrain.Catalog,adapter.Read(),id,out candidate,out _);adapter.Commit(candidate,"Remove arch");
            Assert.That(adapter.Read().baseCells.Count,Is.EqualTo(1));
        }
        [Test] public void OverlayCascadeIsOneTransactionAndUndoRestoresIdentities()
        {
            var mass=Place("T01_Mass_Source",Vector2Int.zero);var overlay=Place("T01_A_Fill_Source",new Vector2Int(4,3));
            var before=adapter.Read();var count=map.ResolveTerrain(adapter.Terrain).solids.Count;var rev=map.AuthoringRevision;
            Engine.TryDelete(adapter.Terrain.Catalog,before,mass,out var candidate,out _);Assert.That(candidate.placements,Is.Empty);
            adapter.Commit(candidate,"Cascade");Assert.That(map.AuthoringRevision,Is.EqualTo(rev+1));
            adapter.Undo();Assert.That(adapter.Read().placements.Select(p=>p.instanceId),Is.EquivalentTo(new[]{mass,overlay}));Assert.That(map.ResolveTerrain(adapter.Terrain).solids.Count,Is.EqualTo(count));
            Engine.TryDelete(adapter.Terrain.Catalog,adapter.Read(),overlay,out candidate,out _);adapter.Commit(candidate,"Overlay only");Assert.That(map.ResolveTerrain(adapter.Terrain).solids.Count,Is.EqualTo(count));
        }
        [Test] public void HostFootprintFailureDoesNotWriteOrRevise()
        {
            map.EditorPlaceObject("START",StageMapObjectKind.PlayerStart,0,0,"START");EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
            var before=File.ReadAllText(path);var rev=map.AuthoringRevision;
            Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),"T01_D_Source",Vector2Int.zero,out var candidate,out _);
            Assert.Throws<InvalidOperationException>(()=>adapter.Commit(candidate,"Must fail"));
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [Test] public void PreviewFailureRestoresMemoryDiskAndRevision()
        {
            var before=EditorJsonUtility.ToJson(map);var bytes=File.ReadAllText(path);
            Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),"T01_D_Source",Vector2Int.zero,out var candidate,out _);
            adapter.Changed+=()=>throw new InvalidOperationException("Injected render failure");
            Assert.Throws<InvalidOperationException>(()=>adapter.Commit(candidate,"Failure injection"));
            Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(bytes));
        }
        [Test] public void PinnedHashVersionOrientationAndDuplicateIdsFailBeforeSave()
        {
            Place("T01_D_Source",Vector2Int.zero);var before=File.ReadAllText(path);
            foreach(var mutation in new Action<AnimolTerrainSavedMap>[]
            {c=>c.placements[0].maskHash="wrong",c=>c.placements[0].catalogVersion=4,c=>c.placements[0].orientation="Rotate90",c=>c.placements.Add(c.placements[0]),c=>c.schemaVersion=999})
            {var c=adapter.Read();c.revision++;mutation(c);Assert.Throws<InvalidOperationException>(()=>adapter.Commit(c,"Invalid"));}
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [Test] public void ObjectPreviewFailureRestoresMemoryDiskAndRevision()
        {
            var before=EditorJsonUtility.ToJson(map);var bytes=File.ReadAllText(path);
            var def=adapter.Objects.Find(StageMapObjectKind.HalfBlock);
            adapter.Changed+=()=>throw new InvalidOperationException("Injected object render failure");
            Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Place",definitionId=def.StableTypeId,instanceId="FAIL",origin=new Vector2Int(4,4)}));
            Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(bytes));
        }
        [Test] public void UnknownVariantBlocksReadAndUnchangedKnownVariantIsRetained()
        {
            var binding=adapter.Terrain.bindings.First(b=>b.themeId=="T01" && b.styleId=="T01_A" && b.variantId.Contains("@"));
            map.EditorSetCell(-12,-12,StageMapLayer.Terrain,binding.tileId,binding.variantId);
            Place("T01_D_Source",Vector2Int.zero);
            Assert.That(map.FindCell(-12,-12,StageMapLayer.Terrain).VariantId,Is.EqualTo(binding.variantId));
            var serialized=new SerializedObject(map);serialized.FindProperty("cells").GetArrayElementAtIndex(0).FindPropertyRelative("variantId").stringValue="UNKNOWN-RAW-VARIANT";serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(()=>adapter.Read());Assert.That(map.Cells[0].VariantId,Is.EqualTo("UNKNOWN-RAW-VARIANT"));
        }
        [Test] public void ObjectAdapterUsesStableRegistryIdentityAndOneRevisionPerEdit()
        {
            var def=adapter.Objects.Find(StageMapObjectKind.HalfBlock);var rev=map.AuthoringRevision;
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Place",definitionId=def.StableTypeId,instanceId="HALF",origin=new Vector2Int(4,4)});
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev+1));
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Move",instanceId="HALF",origin=new Vector2Int(5,6)});
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev+2));Assert.That(map.Objects.Single().DataKey,Is.EqualTo(def.StableTypeId));
            var settings=map.Objects.Single().Settings.Clone();settings.EditorSetPathCells(new[]{new Vector2Int(5,6),new Vector2Int(8,6)});
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId="HALF",settings=settings});
            Assert.That(map.AuthoringRevision,Is.EqualTo(rev+3));Assert.That(map.CollisionDataRevision,Is.EqualTo(-1));
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Delete",instanceId="HALF"});adapter.Undo();Assert.That(map.Objects.Single().Settings.PathCells.Count,Is.EqualTo(2));
        }
        [Test] public void LinkedPairAndMutualDeleteUseOneRevisionAndOneUndo()
        {
            var def=adapter.Objects.Types.First(t=>t.RequiresLinkedPair && t.ThemeId=="T01");var rev=map.AuthoringRevision;
            var edit=new TerrainEditorObjectEdit{operation="Pair",definitionId=def.StableTypeId,instanceId="PREVIEW",origin=new Vector2Int(-6,6),second=new Vector2Int(6,6)};
            adapter.CommitObject(edit);Assert.That(map.Objects.Count,Is.EqualTo(2));Assert.That(map.AuthoringRevision,Is.EqualTo(rev+1));
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Delete",instanceId=edit.instanceId});Assert.That(map.Objects,Is.Empty);Assert.That(map.AuthoringRevision,Is.EqualTo(rev+2));
            adapter.Undo();Assert.That(map.Objects.Count,Is.EqualTo(2));Assert.That(StageMapValidator.ValidateStructure(map).IsValid,Is.True);
        }
        [Test] public void All105ThumbnailsAreTransparentAndCachedBySourceHash()
        {
            var parts=adapter.Terrain.Catalog.entries.Select(e=>new TerrainEditorPart{id=e.id,terrain=e})
                .Concat(adapter.Objects.Types.Select(o=>new TerrainEditorPart{id=o.StableTypeId,obj=o})).ToArray();
            Assert.That(parts.Length,Is.EqualTo(105));Assert.That(parts.Select(p=>p.id).Distinct().Count(),Is.EqualTo(105));
            foreach(var p in parts)
            {var thumbnail=adapter.Thumbnail(p);Assert.That(thumbnail.diagnostic,Is.Null,p.id);var count=thumbnail.silhouette.GetPixels32().Count(c=>c.a>0);Assert.That(count,Is.InRange(1,35000),p.id);Assert.That(adapter.Thumbnail(p),Is.SameAs(thumbnail));}
            Assert.That(adapter.ThumbnailBuildCount,Is.EqualTo(105));
        }
        [Test] public void OpenReadSaveDisposeWithoutEditDoesNotChangeMapFile()
        {var bytes=File.ReadAllText(path);var rev=map.AuthoringRevision;adapter.Read();adapter.Save();Assert.That(File.ReadAllText(path),Is.EqualTo(bytes));Assert.That(map.AuthoringRevision,Is.EqualTo(rev));}
        [Test] public void ExistingSixMarkerKindsRemainAuthorableWithUniqueStart()
        {
            Assert.That(adapter.Markers.Count,Is.EqualTo(6));
            var edit=new TerrainEditorObjectEdit{operation="Place",definitionId="marker:START",instanceId="PREVIEW",origin=new Vector2Int(3,3)};
            adapter.CommitObject(edit);Assert.That(map.Objects.Single().Kind,Is.EqualTo(StageMapObjectKind.PlayerStart));
            Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Place",definitionId="marker:START",instanceId="SECOND",origin=new Vector2Int(8,8)}));
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Move",instanceId=edit.instanceId,origin=new Vector2Int(4,4)});
            Assert.That(map.Objects.Single().X,Is.EqualTo(4));adapter.Undo();Assert.That(map.Objects.Single().X,Is.EqualTo(3));
        }
        [Test] public void WrongThemeAndUnsupportedOwnerCannotBeSilentlyMapped()
        {
            Assert.That(Engine.TryPlace(adapter.Terrain.Catalog,adapter.Read(),"T02_D_Source",Vector2Int.zero,out _,out _),Is.False);
            var def=adapter.Objects.Types.First(t=>t.ThemeId=="T02");
            Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Place",definitionId=def.StableTypeId,instanceId="WRONG",origin=Vector2Int.zero}));
            Assert.Throws<InvalidOperationException>(()=>new TerrainEditorAdapter("invalid-guid"));
        }
    }
}
