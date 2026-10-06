using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class ObstacleV2LifecycleTests
    {
        [Test] public void C09DeletionPreservesOtherDataUndoRedoAndFailedSave()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();string path="Assets/OVG02-MIGRATION-TEST.asset";
            try
            {
                map.EditorInitializeIdentity("OVG02-MIGRATION-TEST","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
                map.EditorPlaceObject("retained",StageMapObjectKind.PlayerStart,0,0);
                map.EditorPlaceObject("removed",StageMapObjectKind.CommonC09,1,0);map.EditorMarkCollisionDataSynchronized();
                AssetDatabase.CreateAsset(map,path);var before=EditorJsonUtility.ToJson(map);var disk=File.ReadAllBytes(path);
                Assert.That(StageMapValidator.ValidateStructure(map).Errors.Any(e=>e.Contains("C09")),Is.True);
                Assert.Throws<InvalidOperationException>(()=>map.ReadTerrain(StageTerrainStructureRegistry.Load()));
                Assert.Throws<IOException>(()=>ObstacleV2Migration.RemoveC09(map,()=>throw new IOException("Injected refresh failure")));
                Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before));Assert.That(File.ReadAllBytes(path),Is.EqualTo(disk));
                var result=ObstacleV2Migration.RemoveC09(map);Assert.That(result.removed.Single().id,Is.EqualTo("removed"));
                Assert.That(map.Objects.Single().StableId,Is.EqualTo("retained"));Assert.That(result.afterRevision,Is.EqualTo(result.beforeRevision+1));
                Undo.PerformUndo();Assert.That(map.Objects.Count,Is.EqualTo(2));Assert.Throws<InvalidOperationException>(()=>map.ReadTerrain(StageTerrainStructureRegistry.Load()));
                Undo.PerformRedo();Assert.That(map.Objects.Single().StableId,Is.EqualTo("retained"));
            }
            finally{Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);}
        }
        [Test] public void C05PoolIsIndependentSnappedFadesAndDeduplicatesConcurrentEvents()
        {
            var root=new GameObject("Stage independent VFX");try
            {
                var pool=root.AddComponent<StageCrumbleVfxPool>();
                Assert.That(pool.TryPlay(1,1,1,"T01",new Vector3(-16,0),10),Is.True);
                Assert.That(pool.TryPlay(1,1,1,"T01",Vector3.zero,10),Is.False);
                Assert.That(pool.TryPlay(2,1,1,"T05",new Vector3(16,0),10),Is.True);Assert.That(pool.ActiveCount,Is.EqualTo(2));
                var pieces=root.GetComponentsInChildren<SpriteRenderer>();Assert.That(pieces.Length,Is.EqualTo(8));
                pool.Sample(10.08);Assert.That(pieces.All(r=>r.color.a==1),Is.True);
                pool.Sample(10.24);Assert.That(pieces.All(r=>Mathf.Abs(r.color.a-.5f)<.0001f),Is.True);
                foreach(var p in pieces){Assert.That(p.transform.position.x*32,Is.EqualTo(Mathf.Round(p.transform.position.x*32)));Assert.That(p.transform.position.y*32,Is.EqualTo(Mathf.Round(p.transform.position.y*32)));Assert.That(p.transform.rotation,Is.EqualTo(Quaternion.identity));Assert.That(p.transform.localScale,Is.EqualTo(Vector3.one));}
                Assert.That(root.GetComponentsInChildren<Collider2D>(),Is.Empty);Assert.That(root.GetComponentsInChildren<Rigidbody2D>(),Is.Empty);
                pool.Sample(10.401);Assert.That(pool.ActiveCount,Is.Zero);Assert.That(pool.PooledCount,Is.EqualTo(2));
                Assert.That(pool.TryPlay(1,1,1,"T01",Vector3.zero,11),Is.False);
                Assert.That(pool.TryPlay(1,1,2,"T01",Vector3.zero,11),Is.True);Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(8));
                pool.ClearOwner(1);Assert.That(pool.ActiveCount,Is.Zero);
                pool.ClearStage();Assert.That(pool.EventOwnerCount,Is.Zero);Assert.That(pool.TryPlay(1,2,1,"T01",Vector3.zero,12),Is.True);
                pool.ClearStage();Assert.That(pool.ActiveCount,Is.Zero);Assert.That(pool.EventOwnerCount,Is.Zero);
                Assert.That(ObstacleArtRegistry.Load().CrumbleFeedback.BlocksInput,Is.False);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void FrameOnlyChangeUpdatesOneSpriteWithoutRebuildingBodiesOrMotifs()
        {
            var root=new GameObject("Incremental animation");try
            {
                var r=root.AddComponent<FreeShapeTerrainRenderer>();var layer=new FreeShapeLayer();
                var c=new GraphicCell{Kind="C03",ThemeId="T01",StyleId="T01_A",Pose="active",SurfaceEnabled=true};
                var devices=new Dictionary<Vector2Int,GraphicCell>{{new Vector2Int(-16,0),c}};r.Rebuild(layer,1,devices);
                var renderers=r.GetComponentsInChildren<SpriteRenderer>();var mechanism=renderers.Single(x=>x.name=="Mechanism source pixels");int id=mechanism.GetInstanceID();
                c.Frame=3;r.UpdateAnimationFrames(devices);Assert.That(r.LastUpdatedCells,Is.EqualTo(1));Assert.That(r.LastUpdatedMotifs,Is.Zero);
                Assert.That(mechanism.GetInstanceID(),Is.EqualTo(id));Assert.That(mechanism.sprite,Is.SameAs(ObstacleArtRegistry.Load().Sprite("T01/C03/UP/active/f3")));
                Assert.That(r.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(renderers.Length));Assert.That(r.GetComponentsInChildren<Collider2D>(),Is.Empty);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
