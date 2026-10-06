using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using Animol.TerrainStructure.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Engine = Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Tests
{
    public sealed class TerrainStructureIntegrationTests
    {
        private StageTerrainStructureRegistry registry;
        private readonly List<UnityEngine.Object> cleanup = new List<UnityEngine.Object>();
        [SetUp] public void Setup() { registry = StageTerrainStructureRegistry.Load(); }
        [TearDown] public void Teardown()
        {
            if (AnimolTerrainMapEditorBridge.Owner != null) AnimolTerrainMapEditorBridge.Unbind(AnimolTerrainMapEditorBridge.Owner);
            foreach (var value in cleanup.AsEnumerable().Reverse()) if (value != null) UnityEngine.Object.DestroyImmediate(value);
            cleanup.Clear();
        }
        private StageMapDefinition Map(string theme = "T01")
        {
            var map = ScriptableObject.CreateInstance<StageMapDefinition>(); cleanup.Add(map);
            map.EditorInitializeIdentity("TERRAIN-INTEGRATION-TEST", theme);
            map.EditorTrySetChunkBounds(new RectInt(-2, -2, 6, 6), false, out _);
            var so = new SerializedObject(map); so.FindProperty("worldUnitsPerCell").floatValue = 1; so.ApplyModifiedPropertiesWithoutUndo();
            map.EditorMarkCollisionDataSynchronized(); return map;
        }
        private AnimolTerrainSavedMap Place(StageMapDefinition map, string id, Vector2Int origin)
        {
            Assert.That(Engine.TryPlace(registry.Catalog, map.ReadTerrain(registry), id, origin, out var candidate, out var error), Is.True, error);
            return candidate;
        }
        private StageMapRuntimeLoader Loader()
        {
            var go = new GameObject("OperationalGrid"); cleanup.Add(go); go.AddComponent<Grid>();
            var terrain = new GameObject("Terrain"); terrain.transform.SetParent(go.transform);
            var tm = terrain.AddComponent<Tilemap>(); terrain.AddComponent<TilemapRenderer>(); terrain.AddComponent<TilemapCollider2D>();
            var loader = go.AddComponent<StageMapRuntimeLoader>();
            var so = new SerializedObject(loader); so.FindProperty("terrain").objectReferenceValue = tm; so.ApplyModifiedPropertiesWithoutUndo();
            return loader;
        }

        [Test] public void AllSixtyFramesAndFortyStampsUseCanonicalMaskAndOriginalSheets()
        {
            Assert.That(Engine.ValidateCatalog(registry.Catalog, out var error), Is.True, error);
            Assert.That(registry.frames.Length, Is.EqualTo(60));
            Assert.That(registry.Catalog.entries.Count(e => e.kind == "Structure"), Is.EqualTo(40));
            foreach (var entry in registry.Catalog.entries)
            {
                var frame = registry.Frame(entry.frameId);
                Assert.That(frame.ValidateForFootprint(entry.width, entry.height, out error), Is.True, entry.id + error);
                Assert.That(AssetDatabase.GetAssetPath(frame.Sprite.texture), Does.EndWith(entry.themeId + "_Shown_Design.png"));
                if (entry.kind == "Structure")
                {
                    var stamp = AssetDatabase.LoadAssetAtPath<AnimolTerrainStampDefinition>(StageTerrainStructureIntegration.Root + "/Generated/SourceStamps/" + entry.id + ".asset");
                    Assert.That(stamp, Is.Not.Null, entry.id); CollectionAssert.AreEqual(entry.solidRows, stamp.Rows);
                }
                else Assert.That(entry.solidRows.Sum(r => r.Count(c => c == '#')), Is.Zero);
            }
            foreach (var theme in new[] { "T01", "T02", "T03", "T04", "T05" })
                foreach (var letter in new[] { "A", "B", "C", "D" })
                    Assert.That(registry.Catalog.entries.Any(e => e.id == theme + "_" + letter + "_Source"), Is.True);
            var arch = registry.Catalog.entries.Single(e => e.id == "T01_D_Source");
            Assert.That((arch.width, arch.height, arch.solidRows.Sum(r => r.Count(c => c == '#'))), Is.EqualTo((12, 5, 52)));
            var library = registry.Catalog.entries.Single(e => e.id == "T03_D_Source");
            Assert.That(library.solidRows.Last().All(c => c == '#'), Is.True, "Library D has a closed bottom.");
        }

        [Test] public void AllFortyStructuresUseOperationalCellColliderOnly()
        {
            var loader = Loader(); var tm = loader.ResolveLayer(StageMapLayer.Terrain);
            foreach (var entry in registry.Catalog.entries.Where(e => e.kind == "Structure"))
            {
                var map = Map(entry.themeId); map.EditorApplyTerrainCandidate(registry, Place(map, entry.id, new Vector2Int(-1, -1)));
                loader.Load(map);
                var runtime = loader.GetComponent<StageTerrainStructureRuntime>();
                Assert.That(runtime.LogicalGrid.solids.Count, Is.EqualTo(entry.solidRows.Sum(r => r.Count(c => c == '#'))), entry.id);
                Assert.That(runtime.Instances.Count, Is.EqualTo(1));
                Assert.That(runtime.Instances.Values.Single().GetComponentsInChildren<Collider2D>().Length, Is.Zero);
                Physics2D.SyncTransforms();
                for (int row = 0; row < entry.height; row++) for (int x = 0; x < entry.width; x++)
                {
                    int y = entry.height - 1 - row;
                    var position = new Vector3Int(x - 1, y - 1, 0); bool solid = entry.solidRows[row][x] == '#';
                    Assert.That(tm.HasTile(position), Is.EqualTo(solid), entry.id + position);
                    Assert.That(tm.GetComponent<TilemapCollider2D>().OverlapPoint(new Vector2(position.x + .5f, position.y + .5f)), Is.EqualTo(solid), entry.id + " physical " + position);
                }
            }
        }

        [Test] public void EveryOverlayRequiresInteriorSupportAndDeletionPreservesGround()
        {
            foreach (var e in registry.Catalog.entries.Where(e => e.kind == "InteriorOverlay"))
            {
                var map = Map(e.themeId); var dto = map.ReadTerrain(registry);
                Assert.That(Engine.TryPlace(registry.Catalog, dto, e.id, Vector2Int.zero, out _, out _), Is.False);
                for (int y = -1; y <= e.height; y++) for (int x = -1; x <= e.width; x++)
                    dto.baseCells.Add(new AnimolTerrainBaseCell { x = x, y = y, themeId = e.themeId, styleId = e.styleId });
                dto.revision++; map.EditorApplyTerrainCandidate(registry, dto);
                map.EditorApplyTerrainCandidate(registry, Place(map, e.id, Vector2Int.zero));
                var before = map.ResolveTerrain(registry).solids.Count;
                Assert.That(Engine.TryDelete(registry.Catalog, map.ReadTerrain(registry), map.TerrainPlacements.placements[0].instanceId, out var deleted, out var error), Is.True, error);
                map.EditorApplyTerrainCandidate(registry, deleted);
                Assert.That(map.ResolveTerrain(registry).solids.Count, Is.EqualTo(before));
            }
        }

        [Test] public void ExistingBrushRejectsOwnedCellsButAllowsVisibleArchPlatform()
        {
            var map = Map(); map.EditorApplyTerrainCandidate(registry, Place(map, "T01_D_Source", Vector2Int.zero));
            var before = EditorJsonUtility.ToJson(map);
            Assert.Throws<InvalidOperationException>(() => StageMapAuthoringOperations.Erase(map, Vector2Int.zero, StageMapLayer.Terrain));
            Assert.Throws<InvalidOperationException>(() => StageMapAuthoringOperations.Paint(map, Vector2Int.zero, StageMapLayer.Terrain, "moon_C", "ANIMOL@CENTER"));
            Assert.That(EditorJsonUtility.ToJson(map), Is.EqualTo(before));
            StageMapAuthoringOperations.Paint(map, new Vector2Int(4, 0), StageMapLayer.Terrain, "moon_N", "ANIMOL@TOP");
            var loader = Loader(); loader.Load(map);
            var runtime = loader.GetComponent<StageTerrainStructureRuntime>();
            Assert.That(runtime.LogicalGrid.solids.Count, Is.EqualTo(53));
            Assert.That(loader.ResolveLayer(StageMapLayer.Terrain).GetComponent<TilemapRenderer>().sortingOrder,
                Is.GreaterThan(runtime.Instances.Values.Single().GetComponentInChildren<SpriteRenderer>().sortingOrder));
            Assert.That(loader.ResolveLayer(StageMapLayer.Terrain).GetSprite(new Vector3Int(4, 0, 0)), Is.Not.Null);
        }

        [Test] public void ChunkOwnershipSurvivesOriginUnloadMoveAndDelete()
        {
            Assert.That(new[] { Engine.FloorDiv(-1,16), Engine.FloorDiv(-16,16), Engine.FloorDiv(-17,16) }, Is.EqualTo(new[] {-1,-1,-2}));
            var map = Map(); map.EditorApplyTerrainCandidate(registry, Place(map, "T01_D_Source", new Vector2Int(15,15)));
            var loader = Loader(); loader.Load(map); var runtime = loader.GetComponent<StageTerrainStructureRuntime>();
            var id = map.TerrainPlacements.placements[0].instanceId;
            Assert.That(runtime.ChunkOwners.Count, Is.EqualTo(4));
            var instance = runtime.Instances[id]; runtime.SetChunkVisible(Vector2Int.zero, false);
            Assert.That(instance.activeSelf, Is.True); Assert.That(runtime.Instances[id], Is.SameAs(instance));
            foreach (var chunk in runtime.ChunkOwners.Keys.ToArray()) runtime.SetChunkVisible(chunk, false);
            Assert.That(instance.activeSelf, Is.False); runtime.SetChunkVisible(Vector2Int.one, true);
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(Engine.TryMove(registry.Catalog, map.ReadTerrain(registry), id, new Vector2Int(-1,-1), out var moved, out var error), Is.True, error);
            map.EditorApplyTerrainCandidate(registry, moved); loader.Load(map);
            Assert.That(runtime.ChunkOwners.Keys.Any(p => p.x < 0 && p.y < 0), Is.True);
            Assert.That(Engine.TryDelete(registry.Catalog, map.ReadTerrain(registry), id, out var deleted, out error), Is.True, error);
            map.EditorApplyTerrainCandidate(registry, deleted); loader.Load(map);
            Assert.That(runtime.Instances, Is.Empty); Assert.That(runtime.ChunkOwners, Is.Empty);
        }

        [Test] public void HostTransactionsUndoRedoOnceAndChangedDoesNotChangeRevision()
        {
            var map = Map(); int changes = 0;
            AnimolTerrainMapEditorBridge.Bind(map, () => map.ReadTerrain(registry), c => StageTerrainStructureIntegration.Write(map,c), () => changes++, "Test");
            var before = EditorJsonUtility.ToJson(map); int revision = map.AuthoringRevision;
            AnimolTerrainMapEditorBridge.Commit(Place(map,"T01_D_Source",Vector2Int.zero),"Place");
            var placed = EditorJsonUtility.ToJson(map); var id = map.TerrainPlacements.placements.Single().instanceId;
            Assert.That(map.AuthoringRevision, Is.EqualTo(revision+1)); Assert.That(map.CollisionDataRevision, Is.EqualTo(-1));
            Undo.PerformUndo(); Assert.That(EditorJsonUtility.ToJson(map), Is.EqualTo(before));
            Undo.PerformRedo(); Assert.That(EditorJsonUtility.ToJson(map), Is.EqualTo(placed));
            Assert.That(Engine.TryMove(registry.Catalog,map.ReadTerrain(registry),id,new Vector2Int(15,15),out var moved,out var error),Is.True,error);
            AnimolTerrainMapEditorBridge.Commit(moved,"Move"); var afterMove=EditorJsonUtility.ToJson(map);
            Undo.PerformUndo(); Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(placed));
            Undo.PerformRedo(); Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(afterMove));
            Assert.That(Engine.TryDelete(registry.Catalog,map.ReadTerrain(registry),id,out var deleted,out error),Is.True,error);
            AnimolTerrainMapEditorBridge.Commit(deleted,"Delete");
            Undo.PerformUndo(); Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(afterMove));
            Undo.PerformRedo(); Assert.That(map.TerrainPlacements.placements,Is.Empty);
            Assert.That(changes,Is.EqualTo(9));
        }

        [Test] public void GroundEraseAndOverlayCascadeUndoTogether()
        {
            var map=Map();
            for(int y=0;y<6;y++) for(int x=0;x<6;x++) map.EditorSetCell(x,y,StageMapLayer.Terrain,"moon_C","ANIMOL@CENTER");
            map.EditorApplyTerrainCandidate(registry,Place(map,"T01_A_Fill_Source",new Vector2Int(2,2)));
            AnimolTerrainMapEditorBridge.Bind(map,()=>map.ReadTerrain(registry),c=>StageTerrainStructureIntegration.Write(map,c),()=>{},"Test");
            var before=EditorJsonUtility.ToJson(map);
            Assert.That(Engine.TrySetBaseCell(registry.Catalog,map.ReadTerrain(registry),new Vector2Int(1,1),false,null,out var candidate,out var error),Is.True,error);
            AnimolTerrainMapEditorBridge.Commit(candidate,"Cascade"); Assert.That(map.TerrainPlacements.placements,Is.Empty); Assert.That(map.Cells.Count,Is.EqualTo(35));
            Undo.PerformUndo(); Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before));
            Undo.PerformRedo(); Assert.That(map.TerrainPlacements.placements,Is.Empty); Assert.That(map.Cells.Count,Is.EqualTo(35));
        }

        [Test] public void InvalidCandidatesLeaveCanonicalAndRuntimeCacheUnchanged()
        {
            var map=Map(); map.EditorApplyTerrainCandidate(registry,Place(map,"T01_D_Source",Vector2Int.zero));
            var loader=Loader(); loader.Load(map); var runtime=loader.GetComponent<StageTerrainStructureRuntime>();
            var instance=runtime.Instances.Values.Single(); var before=EditorJsonUtility.ToJson(map);
            var mutations=new Action<AnimolTerrainSavedMap>[] {
                c=>c.placements[0].catalogVersion++, c=>c.placements[0].maskHash=new string('0',64),
                c=>c.placements[0].orientation="FlipX", c=>c.placements[0].themeId="T02",
                c=>c.placements.Add(c.placements[0]), c=>c.schemaVersion=99,
                c=>c.baseCells.Add(new AnimolTerrainBaseCell{x=0,y=0,themeId="T01",styleId="T01_A"}) };
            foreach(var mutate in mutations) {
                var dto=map.ReadTerrain(registry); dto.revision++; mutate(dto);
                Assert.Throws<InvalidOperationException>(()=>StageTerrainStructureIntegration.Write(map,dto));
                Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before)); Assert.That(runtime.Instances.Values.Single(),Is.SameAs(instance));
            }
        }

        [Test] public void BoundsObjectFootprintAndSweptPathBlockCandidateBeforeSave()
        {
            var map=Map(); var outside=Place(map,"T01_D_Source",new Vector2Int(63,0));
            Assert.Throws<InvalidOperationException>(()=>StageTerrainStructureIntegration.Write(map,outside));
            var settings=new StageMapObjectSettings(); settings.EditorSetPathCells(new[]{new Vector2Int(-5,0),new Vector2Int(20,0)});
            map.EditorPlaceObject("rail",StageMapObjectKind.RailPlatform,-5,0,"RAIL",null,settings);
            var candidate=Place(map,"T01_D_Source",Vector2Int.zero);
            Assert.Throws<InvalidOperationException>(()=>StageTerrainStructureIntegration.Write(map,candidate));
        }

        [Test] public void ExistingM9LocalPlayGetsOnePhysicsOwnerAndKeepsOneWayEffector()
        {
            var map=Map(); map.EditorSetCell(-10,0,StageMapLayer.Terrain,"M9_MOON_SOLID_16PX_PLACEHOLDER");
            map.EditorSetCell(-8,0,StageMapLayer.Terrain,"M9_MOON_ONE_WAY_16PX_PLACEHOLDER");
            map.EditorApplyTerrainCandidate(registry,Place(map,"T01_D_Source",Vector2Int.zero));
            var root=new GameObject("M9_T01_S01_Greybox");cleanup.Add(root);
            var solid=new GameObject("Solid_0_-10_-10",typeof(BoxCollider2D));solid.transform.SetParent(root.transform);
            var oneWay=new GameObject("OneWay_0_-8_-8",typeof(BoxCollider2D));oneWay.transform.SetParent(root.transform);
            var go=new GameObject("M9_StageMapRuntimeLoader");go.transform.SetParent(root.transform);var loader=go.AddComponent<StageMapRuntimeLoader>();
            loader.ConfigureDefinition(map,null,true);loader.Load(map);
            Assert.That(solid.activeSelf,Is.False);Assert.That(oneWay.activeSelf,Is.False);
            Assert.That(loader.ResolveLayer(StageMapLayer.Terrain).GetComponent<TilemapCollider2D>(),Is.Not.Null);
            Assert.That(loader.ResolveLayer(StageMapLayer.Terrain).HasTile(new Vector3Int(-8,0,0)),Is.False);
            var effector=go.GetComponentInChildren<PlatformEffector2D>();Assert.That(effector.useOneWay,Is.True);
            Assert.That(effector.GetComponent<Tilemap>().HasTile(new Vector3Int(-8,0,0)),Is.True);
            Assert.That(go.GetComponent<StageTerrainStructureRuntime>().LogicalGrid.solids.Count,Is.EqualTo(54));
            loader.Load(map);Assert.That(go.GetComponentsInChildren<Grid>().Length,Is.EqualTo(1));
            var legacy=Map();loader.Load(legacy);Assert.That(solid.activeSelf,Is.True);Assert.That(oneWay.activeSelf,Is.True);
        }

        [Test] public void ExactLegacyVariantsObjectsAndUnknownSettingsSurviveRoundTrip()
        {
            var map=Map();
            foreach(TerrainTileTopology direction in Enum.GetValues(typeof(TerrainTileTopology)))
                map.EditorSetCell(-20+(int)direction,-20,StageMapLayer.Terrain,"moon_C",StageMapScenePalette.ComposeVariantId("ANIMOL",direction));
            var settings=new StageMapObjectSettings(); JsonUtility.FromJsonOverwrite("{\"version\":999}",settings);
            map.EditorPlaceObject("unknown",StageMapObjectKind.EventTrigger,-25,-25,"FUTURE_UNKNOWN_TYPE",null,settings);
            map.EditorSetCell(-20,-20,StageMapLayer.Decoration,"UNKNOWN_DECOR","raw@variant");
            var original=map.Cells.Select(c=>JsonUtility.ToJson(c)).ToArray(); var obj=JsonUtility.ToJson(map.Objects.Single());
            StageTerrainStructureIntegration.Write(map,Place(map,"T01_D_Source",new Vector2Int(-1,-1)));
            CollectionAssert.AreEquivalent(original,map.Cells.Select(c=>JsonUtility.ToJson(c)).ToArray());
            Assert.That(JsonUtility.ToJson(map.Objects.Single()),Is.EqualTo(obj));
            var clone=Map(); EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(map),clone);
            Assert.That(JsonUtility.ToJson(clone.ReadTerrain(registry)),Is.EqualTo(JsonUtility.ToJson(map.ReadTerrain(registry))));
            var loader=Loader(); loader.Load(clone);
            Assert.That(loader.GetComponent<StageTerrainStructureRuntime>().LogicalGrid.solids.Count,Is.EqualTo(map.ResolveTerrain(registry).solids.Count));
        }

        [Test] public void UnmappedCellAndUnknownSchemaAreReportedWithoutRewritingData()
        {
            var map=Map(); map.EditorSetCell(0,0,StageMapLayer.Terrain,"UNKNOWN_TILE","future@9");
            var before=EditorJsonUtility.ToJson(map); Assert.Throws<InvalidOperationException>(()=>map.ReadTerrain(registry)); Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(before));
            var empty=Map(); empty.TerrainPlacements.schemaVersion=99;
            var unknown=EditorJsonUtility.ToJson(empty); Assert.Throws<InvalidOperationException>(()=>empty.ReadTerrain(registry)); Assert.That(EditorJsonUtility.ToJson(empty),Is.EqualTo(unknown));
        }

        [Test] public void AssetSaveReimportEditorReopenAndFailedChangedCallbackRestoreDisk()
        {
            var map=Map(); var path=AssetDatabase.GenerateUniqueAssetPath(StageTerrainStructureIntegration.Root+"/Generated/IntegrationRoundTrip.asset");
            AssetDatabase.CreateAsset(map,path);
            var backupBefore=System.IO.Directory.Exists("Assets/ANIMOL/MapBackups/"+map.StageId)
                ? System.IO.Directory.GetFiles("Assets/ANIMOL/MapBackups/"+map.StageId,"*.json") : Array.Empty<string>();
            try {
                var window=CampaignMapEditorWindow.OpenForWorkspace(map);
                Assert.That(AnimolTerrainMapEditorBridge.IsBound,Is.False);
                var adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                Assert.That(AnimolTerrainMapEditorBridge.Owner,Is.EqualTo(map));
                AnimolTerrainMapEditorBridge.Commit(Place(map,"T01_D_Source",new Vector2Int(-1,-1)),"Saved placement");
                var expected=JsonUtility.ToJson(map.ReadTerrain(registry));
                window.Close(); Assert.That(AnimolTerrainMapEditorBridge.Owner,Is.EqualTo(map));
                adapter.Dispose(); Assert.That(AnimolTerrainMapEditorBridge.IsBound,Is.False);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var reloaded=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
                window=CampaignMapEditorWindow.OpenForWorkspace(reloaded);
                adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                Assert.That(JsonUtility.ToJson(AnimolTerrainMapEditorBridge.Read()),Is.EqualTo(expected));
                var originalDisk=System.IO.File.ReadAllText(path);
                var id=reloaded.TerrainPlacements.placements.Single().instanceId;
                Assert.That(Engine.TryMove(registry.Catalog,reloaded.ReadTerrain(registry),id,new Vector2Int(15,15),out var moved,out var error),Is.True,error);
                int changedCalls=0;
                AnimolTerrainMapEditorBridge.Bind(reloaded,()=>reloaded.ReadTerrain(registry),c=>StageTerrainStructureIntegration.Write(reloaded,c),()=>{if(changedCalls++==0) throw new InvalidOperationException("Injected preview failure");},"Failure");
                Assert.Throws<InvalidOperationException>(()=>AnimolTerrainMapEditorBridge.Commit(moved,"Must rollback"));
                Assert.That(JsonUtility.ToJson(reloaded.ReadTerrain(registry)),Is.EqualTo(expected));
                Assert.That(System.IO.File.ReadAllText(path),Is.EqualTo(originalDisk));
                window.Close(); adapter.Dispose();
            } finally {
                AssetDatabase.DeleteAsset(path);
                if(System.IO.Directory.Exists("Assets/ANIMOL/MapBackups/TERRAIN-INTEGRATION-TEST"))
                    foreach(var file in System.IO.Directory.GetFiles("Assets/ANIMOL/MapBackups/TERRAIN-INTEGRATION-TEST","*.json").Except(backupBefore)) AssetDatabase.DeleteAsset(file.Replace('\\','/'));
            }
        }
    }
}
