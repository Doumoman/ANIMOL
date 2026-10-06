using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Tests
{
    [Category("ObstacleGraphics")]
    public sealed class ObstacleGraphicsTests
    {
        private static GraphicCell Cell(string kind="terrain",string style="T01_A",bool surface=true)=>new GraphicCell{Kind=kind,StyleId=style,ThemeId=style.Substring(0,3),SurfaceEnabled=surface,Facing=kind is "C02" or "C08"?"LEFT":"UP",Pose=kind=="C09"?"active":"idle"};
        private static GraphicPlan Resolve(Dictionary<Vector2Int,GraphicCell> cells,Vector2Int p,int seed=0)=>ObstacleVisualResolver.Resolve((x,y)=>cells.GetValueOrDefault(new Vector2Int(x,y)),p.x,p.y,seed);
        [Test] public void TwentyStylesAllRawMasksAndPhasesUseV6Registry()
        {
            var art=FreeShapeArtRegistry.Load();var dirs=new[]{Vector2Int.up,new Vector2Int(1,1),Vector2Int.right,new Vector2Int(1,-1),Vector2Int.down,new Vector2Int(-1,-1),Vector2Int.left,new Vector2Int(-1,1)};
            foreach(var style in art.styles)for(int raw=0;raw<256;raw++)for(int phase=0;phase<4;phase++)
            {
                var p=new Vector2Int(-17+phase%2,-17+phase/2);var cells=new Dictionary<Vector2Int,GraphicCell>{{p,Cell("C10",style.styleId)}};
                for(int i=0;i<8;i++)if((raw&(1<<i))!=0)cells[p+dirs[i]]=Cell("terrain",style.styleId);
                var plan=Resolve(cells,p);
                Assert.That(plan.BodyMask,Is.EqualTo(FreeShapeTopology.Canonical(raw)));
                Assert.That(plan.Variant,Is.EqualTo(FreeShapeTopology.Variant(p.x,p.y,0)));
                Assert.That(art.Cell(style.styleId,plan.BodyRaw,plan.Variant),Is.SameAs(art.Style(style.styleId).cells[plan.Variant*47+art.rawToIndex[raw]]));
            }
        }
        [Test] public void TwentyStylesTenKindsResolveRegisteredMechanisms()
        {
            foreach(var style in FreeShapeArtRegistry.Load().styles)for(int i=1;i<=10;i++)
            {
                var c=Cell("C"+i.ToString("D2"),style.styleId);var plan=Resolve(new(){{Vector2Int.zero,c}},Vector2Int.zero);
                Assert.That(ObstacleArtRegistry.Load().Sprite(plan.OverlayKey),Is.Not.Null);
                Assert.That(plan.DrawBody,Is.EqualTo(i is 2 or 3 or 8 or 10));
                Assert.That(plan.CapOnly,Is.EqualTo(i is 1 or 5 or 6 or 9));
            }
        }
        [Test] public void FullTopAirAndInactiveHaveBidirectionalGlobalJoins()
        {
            var p=new Vector2Int(-16,-1);var q=p+Vector2Int.right;
            var cells=new Dictionary<Vector2Int,GraphicCell>{{p,Cell()},{q,Cell("C10")}};
            Assert.That(Resolve(cells,p).BodyMask,Is.EqualTo(4));Assert.That(Resolve(cells,q).BodyMask,Is.EqualTo(64));
            cells[q]=Cell("C05");var full=Resolve(cells,p);Assert.That(full.BodyMask,Is.Zero);Assert.That(full.CapPatch,Is.True);Assert.That(full.CapMask,Is.EqualTo(4));
            Assert.That(Resolve(cells,q).CapOnly,Is.True);
            cells[q].SurfaceEnabled=false;Assert.That(Resolve(cells,p).CapPatch,Is.False);Assert.That(Resolve(cells,q).CapOnly,Is.False);
            cells[q]=Cell("C07");Assert.That(Resolve(cells,p).CapMask,Is.Zero);Assert.That(Resolve(cells,q).DrawBody,Is.False);
            cells[q]=Cell("C10","T01_B");Assert.That(Resolve(cells,p).BodyMask,Is.Zero);
            cells[q]=Cell("C10");cells[q].JoinGroup="other";Assert.That(Resolve(cells,p).BodyMask,Is.Zero);
        }
        [Test] public void PendingSurfaceAndBlockedFacesAreNotInferredFromPose()
        {
            var cells=new Dictionary<Vector2Int,GraphicCell>{{Vector2Int.zero,Cell("C09",surface:false)}};
            cells[Vector2Int.zero].Pose="pending";var pending=Resolve(cells,Vector2Int.zero);Assert.That(pending.Pose,Is.EqualTo("warn"));Assert.That(pending.CapOnly,Is.False);
            cells[Vector2Int.zero].Pose="warn";cells[Vector2Int.zero].SurfaceEnabled=true;Assert.That(Resolve(cells,Vector2Int.zero).CapOnly,Is.True);
            cells[Vector2Int.up]=Cell();Assert.That(Resolve(cells,Vector2Int.zero).Problems,Is.Not.Empty);
            cells[Vector2Int.zero].ArtVersion=4;Assert.Throws<ArgumentException>(()=>Resolve(cells,Vector2Int.zero));
            Assert.Throws<InvalidOperationException>(()=>ObstacleArtRegistry.Load().Sprite("T01/C01/UP/warn"));
        }
        [Test] public void IncrementalRendererRestoresEndcapsWithoutPhysicsOrMotifSupport()
        {
            var root=new GameObject("graphics-test");try
            {
                var renderer=root.AddComponent<FreeShapeTerrainRenderer>();var layer=new FreeShapeLayer();layer.cells.Add(new FreeShapeCell{x=-16,y=0,styleId="T01_A"});
                var devices=new Dictionary<Vector2Int,GraphicCell>{{new Vector2Int(-15,0),Cell("C05")}};
                renderer.Rebuild(layer,1,devices);Assert.That(renderer.LastUpdatedCells,Is.EqualTo(12));Assert.That(root.GetComponentsInChildren<Collider2D>(),Is.Empty);
                Assert.That(renderer.MotifCount,Is.Zero);var first=root.GetComponentInChildren<Tilemap>().GetSprite(new Vector3Int(-16,0,0));Assert.That(first.rect.height,Is.EqualTo(24));
                renderer.Rebuild(layer,1,devices);Assert.That(renderer.LastUpdatedCells,Is.Zero);
                devices[new Vector2Int(-15,0)].SurfaceEnabled=false;devices[new Vector2Int(-15,0)].Pose="inactive";renderer.Rebuild(layer,1,devices);
                Assert.That(renderer.LastUpdatedCells,Is.EqualTo(9));Assert.That(ObstacleVisualResolver.Affected(-15,0).MotifAnchors.Count,Is.EqualTo(36));Assert.That(renderer.LastUpdatedMotifs,Is.EqualTo(FreeShapeTopology.AffectedMotifs(new[]{new Vector2Int(-15,0)}).Count));Assert.That(root.GetComponentInChildren<Tilemap>().GetSprite(new Vector3Int(-16,0,0)).rect.height,Is.EqualTo(32));
                devices.Clear();renderer.Rebuild(layer,1,devices);Assert.That(root.GetComponentsInChildren<Collider2D>(),Is.Empty);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void DeviceOnlyLayerReinterpretsSeedAndScale()
        {
            var root=new GameObject("device-only");try
            {
                var layer=new FreeShapeLayer();var p=new Vector2Int(-16,0);var devices=new Dictionary<Vector2Int,GraphicCell>{{p,Cell("C10")}};
                var renderer=root.AddComponent<FreeShapeTerrainRenderer>();renderer.Rebuild(layer,1,devices);
                var tilemap=root.GetComponentInChildren<Tilemap>();var before=tilemap.GetSprite(new Vector3Int(p.x,p.y,0));
                layer.seed=1;renderer.Rebuild(layer,1,devices);Assert.That(tilemap.GetSprite(new Vector3Int(p.x,p.y,0)),Is.Not.SameAs(before));Assert.That(renderer.LastUpdatedCells,Is.EqualTo(9));
                renderer.Rebuild(layer,2,devices);Assert.That(root.GetComponent<Grid>().cellSize.x,Is.EqualTo(2));Assert.That(renderer.LastUpdatedCells,Is.EqualTo(9));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void InstalledAtlasHasExact163RectsAndNoPhysicsShapes()
        {
            var art=ObstacleArtRegistry.Load();var path=ObstacleArtV1Builder.Root+"/Art/MechanismAtlas.png";
            Assert.That(FreeShapeArtV6Builder.HashFile(path),Is.EqualTo(FreeShapeArtV6Builder.HashFile(ObstacleArtV1Builder.Source+"/Art/MechanismAtlas.png")));
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.sRGBTexture,Is.True);
            foreach(var e in art.entries){Assert.That(e.sprite.rect.size,Is.EqualTo(new Vector2(32,32)));Assert.That(e.sprite.pixelsPerUnit,Is.EqualTo(32));Assert.That(e.sprite.pivot,Is.EqualTo(new Vector2(16,16)));Assert.That(e.sprite.GetPhysicsShapeCount(),Is.Zero);}
        }
        [Test] public void ExistingDeviceKindsAreNotSilentlyChangedIntoCommonFunctions()
        {
            Assert.That(ObstacleSnapshotAdapter.Kind(StageMapObjectKind.SideSpring),Is.Null);
            Assert.That(ObstacleSnapshotAdapter.Kind(StageMapObjectKind.CloudSheepStep),Is.Null);
            Assert.That(ObstacleSnapshotAdapter.Kind(StageMapObjectKind.LibPopupStair),Is.Null);
            Assert.That(ObstacleSnapshotAdapter.Kind(StageMapObjectKind.CommonC01),Is.EqualTo("C01"));
        }
        [Test] public void UnboundCurrentDevicesReportErrorsWithoutLegacyArtwork()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();var id="TEST-OVG-LEGACY-"+Guid.NewGuid().ToString("N");var path="Assets/"+id+".asset";
            TerrainEditorAdapter adapter=null;var root=new GameObject("legacy fixture");
            try
            {
                map.EditorInitializeIdentity(id,"T01");map.EditorInitializeBoundsFromAuthoredContent(1);map.EditorTrySetCellBounds(new RectInt(-16,-16,64,32),false,out _);
                map.FreeShapeTerrain.cells.Add(new FreeShapeCell{x=-4,y=0,styleId="T01_A"});map.FreeShapeTerrain.cells.Add(new FreeShapeCell{x=4,y=0,styleId="T01_B"});
                AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));var type=adapter.Objects.Find(StageMapObjectKind.CommonC01);
                map.EditorPlaceObject("first",type.Kind,0,0,type.StableTypeId,type.Prefab,type.DefaultSettings);
                map.EditorPlaceObject("second",type.Kind,2,0,type.StableTypeId,type.Prefab,type.DefaultSettings);
                var settings=map.Objects.First().Settings.Clone();settings.EditorSetObstacleStyle("T01_A");
                adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId="first",settings=settings});
                Assert.That(map.Objects.Single(o=>o.StableId=="second").Settings.ObstacleStyleId,Is.Empty);Assert.That(TerrainEditorArt.ObstaclePreview(map).Count,Is.EqualTo(1));
                var objects=new GameObject("operational owners");objects.transform.SetParent(root.transform,false);var visuals=new Dictionary<string,SpriteRenderer>();
                foreach(var p in map.Objects)
                {
                    var go=new GameObject(p.StableId,typeof(BoxCollider2D),typeof(PlatformEffector2D),typeof(CommonObstacleObject),typeof(SpriteRenderer));go.transform.SetParent(objects.transform,false);
                    StageMapRuntimeFactory.Configure(go,p,1);visuals.Add(p.StableId,go.GetComponent<SpriteRenderer>());
                }
                var artRoot=new GameObject("graphic cache");artRoot.transform.SetParent(root.transform,false);var art=artRoot.AddComponent<FreeShapeTerrainRenderer>();
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("ANIMOL obstacle graphics: second:"));
                var observer=root.AddComponent<ObstacleRuntimeGraphics>();observer.Configure(map,objects.transform,art);
                Assert.That(visuals["first"].forceRenderingOff,Is.True);Assert.That(visuals["second"].forceRenderingOff,Is.False);Assert.That(observer.LastIssues,Does.Contain("second"));
                var count=art.GetComponentsInChildren<SpriteRenderer>().Length;observer.Refresh();Assert.That(art.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(count));
            }
            finally{ObjectDestroy(root);adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);var backup=StageMapBackupService.BackupRoot+"/"+id;if(AssetDatabase.IsValidFolder(backup))AssetDatabase.DeleteAsset(backup);}
        }
        [Test] public void RetiredRecordsCleanupIsOneUndoableRevisionAndRollsBackFailure()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();var id="TEST-RETIRE-"+Guid.NewGuid().ToString("N");var path="Assets/"+id+".asset";
            TerrainEditorAdapter adapter=null;
            try
            {
                map.EditorInitializeIdentity(id,"T01");map.EditorInitializeBoundsFromAuthoredContent(1);
                map.EditorPlaceObject("old1",StageMapObjectKind.DropPlatform,0,0);
                map.EditorPlaceObject("old2",StageMapObjectKind.SideSpring,1,0);
                map.EditorPlaceObject("start",StageMapObjectKind.PlayerStart,2,0);map.EditorMarkCollisionDataSynchronized();
                AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                int revision=map.AuthoringRevision;adapter.RemoveRetiredObstacles();
                Assert.That(map.Objects.Count,Is.EqualTo(1));Assert.That(map.AuthoringRevision,Is.EqualTo(revision+1));
                adapter.Undo();Assert.That(map.Objects.Count,Is.EqualTo(3));adapter.Redo();Assert.That(map.Objects.Count,Is.EqualTo(1));adapter.Undo();
                var before=File.ReadAllBytes(path);var hash=map.ContentHash;
                Action failure=()=>throw new IOException("retirement preview failure");adapter.Changed+=failure;
                Assert.Throws<IOException>(()=>adapter.RemoveRetiredObstacles());adapter.Changed-=failure;
                Assert.That(map.ContentHash,Is.EqualTo(hash));Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
            }
            finally{adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);var backup=StageMapBackupService.BackupRoot+"/"+id;if(AssetDatabase.IsValidFolder(backup))AssetDatabase.DeleteAsset(backup);}
        }
        [Test] public void AllCurrentBehaviorSettingsContributeToContentHash()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();
            try
            {
                map.EditorInitializeIdentity("HASH-COMMON","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
                var type=CommonObstacleCatalog.Load().Find(StageMapObjectKind.CommonC04);
                map.EditorPlaceObject("hazard",type.Kind,0,0,type.StableTypeId,type.Prefab,type.DefaultSettings);
                var settings=map.Objects.Single().Settings;var previous=map.ContentHash;
                settings.EditorConfigureDesignBehavior(.9f,2,3,MapObjectPassengerPolicy.DeferStateChangeWhileOccupied,MapObjectTriggerMode.OnOccupancy,0,0,9,true);
                Assert.That(map.ContentHash,Is.Not.EqualTo(previous));previous=map.ContentHash;
                settings.EditorConfigureAuthoring(MapObjectImplementationLevel.DevPlayable,Array.Empty<string>(),13,MapObjectResetPolicy.MapReload,MapObjectRouteRole.Required,0,"");
                Assert.That(map.ContentHash,Is.Not.EqualTo(previous));previous=map.ContentHash;
                settings.EditorSetObstacleFacing(ObstacleFacing.Left);Assert.That(map.ContentHash,Is.Not.EqualTo(previous));
            }
            finally{ObjectDestroy(map);}
        }
        private static void ObjectDestroy(UnityEngine.Object value)=>UnityEngine.Object.DestroyImmediate(value);
        [Test] public void DeviceCommitMoveDeleteUndoRedoAndFailedValidationPreserveDisk()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();var path="Assets/TEST-OVG01-"+Guid.NewGuid().ToString("N")+".asset";
            TerrainEditorAdapter adapter=null;
            try
            {
                map.EditorInitializeIdentity(Path.GetFileNameWithoutExtension(path),"T01");map.EditorInitializeBoundsFromAuthoredContent(1);map.EditorTrySetCellBounds(new RectInt(-32,-16,64,32),false,out _);map.EditorMarkCollisionDataSynchronized();
                AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                var type=adapter.Objects.Find(StageMapObjectKind.CommonC01);var settings=type.DefaultSettings.Clone();settings.EditorSetObstacleStyle("T01_B");
                int revision=map.AuthoringRevision;var before=map.ContentHash;
                adapter.CommitObject(new TerrainEditorObjectEdit{operation="Place",definitionId=type.StableTypeId,instanceId="drop",origin=new Vector2Int(-16,0),settings=settings});
                Assert.That(map.AuthoringRevision,Is.EqualTo(revision+1));Assert.That(map.ContentHash,Is.Not.EqualTo(before));
                adapter.Undo();Assert.That(map.Objects,Is.Empty);adapter.Redo();Assert.That(map.Objects.Single().Settings.ObstacleStyleId,Is.EqualTo("T01_B"));
                adapter.CommitObject(new TerrainEditorObjectEdit{operation="Move",instanceId="drop",origin=new Vector2Int(16,0)});
                var disk=File.ReadAllBytes(path);var hash=map.ContentHash;
                Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Move",instanceId="drop",origin=new Vector2Int(500,0)}));
                Assert.That(File.ReadAllBytes(path),Is.EqualTo(disk));Assert.That(map.ContentHash,Is.EqualTo(hash));
                var changedSettings=map.Objects.Single().Settings.Clone();changedSettings.EditorSetObstacleStyle("T01_C");
                Action injected=()=>throw new IOException("Injected preview/save transaction failure");adapter.Changed+=injected;
                Assert.Throws<IOException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId="drop",settings=changedSettings}));
                adapter.Changed-=injected;Assert.That(File.ReadAllBytes(path),Is.EqualTo(disk));Assert.That(map.ContentHash,Is.EqualTo(hash));
                adapter.CommitObject(new TerrainEditorObjectEdit{operation="Delete",instanceId="drop"});adapter.Undo();Assert.That(map.Objects.Single().X,Is.EqualTo(16));
                adapter.Save();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Assert.That(AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path).Objects.Single().Settings.ObstacleStyleId,Is.EqualTo("T01_B"));
            }
            finally{adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);var backup=StageMapBackupService.BackupRoot+"/"+Path.GetFileNameWithoutExtension(path);if(AssetDatabase.IsValidFolder(backup))AssetDatabase.DeleteAsset(backup);}
        }
    }
}
