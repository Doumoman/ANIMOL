using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class Ovg04EditorJoinTests
    {
        private StageMapDefinition map;
        private string path;
        private TerrainEditorAdapter adapter;
        private UnityEngine.Object priorSelection;
        [SetUp] public void Setup()
        {
            priorSelection=Selection.activeObject;
            map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StageMapDefinition>(Ovg04ValidationSetup.MapPath));
            path="Assets/TEST-OVG04-"+Guid.NewGuid().ToString("N")+".asset";AssetDatabase.CreateAsset(map,path);
            TerrainEditorSceneEditor.Open(map);adapter=TerrainEditorSceneEditor.Adapter;
        }
        [TearDown] public void Cleanup()
        {
            TerrainEditorSceneEditor.Close();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);Selection.activeObject=priorSelection;
        }

        [Test] public void SceneMoveDirectionUndoDeleteDuplicateAndReopenUseSameAuthoredIds()
        {
            var side=map.Objects.Single(o=>o.Kind==StageMapObjectKind.CommonC02);
            var id=side.StableId;TerrainEditorSceneEditor.SelectPart("C02");TerrainEditorSceneEditor.State.instanceId=id;
            TerrainEditorSceneEditor.UpdateCandidate(new Vector2Int(22,1),true);
            Assert.That(TerrainEditorSceneEditor.GhostValid,Is.True,TerrainEditorSceneEditor.Status);
            TerrainEditorSceneEditor.Commit();Assert.That(map.Objects.Single(o=>o.StableId==id).X,Is.EqualTo(22));
            adapter.Undo();Assert.That(map.Objects.Single(o=>o.StableId==id).X,Is.EqualTo(21));
            adapter.Redo();Assert.That(map.Objects.Single(o=>o.StableId==id).X,Is.EqualTo(22));
            var settings=side.Settings.Clone();settings.EditorSetObstacleFacing(ObstacleFacing.Left);
            adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=id,settings=settings});
            Assert.That(ObstacleSnapshotAdapter.Read(map,map.Objects.Single(o=>o.StableId==id)).Facing,Is.EqualTo("LEFT"));
            TerrainEditorSceneEditor.State.instanceId=id;TerrainEditorSceneEditor.DuplicateSelected();
            var duplicate=TerrainEditorSceneEditor.State.instanceId;Assert.That(duplicate,Is.Not.EqualTo(id));
            Assert.That(map.Objects.Single(o=>o.StableId==duplicate).Settings.ObstacleFacing,Is.EqualTo(ObstacleFacing.Left));
            TerrainEditorSceneEditor.DeleteSelected();Assert.That(map.Objects.Any(o=>o.StableId==duplicate),Is.False);
            adapter.Undo();Assert.That(map.Objects.Any(o=>o.StableId==duplicate),Is.True);
            adapter.Redo();Assert.That(map.Objects.Any(o=>o.StableId==duplicate),Is.False);
            adapter.Save();var json=EditorJsonUtility.ToJson(map);TerrainEditorSceneEditor.Close();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);TerrainEditorSceneEditor.Open(map);
            Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(json));
            Assert.That(map.Objects.Single(o=>o.StableId==id).Settings.ObstacleFacing,Is.EqualTo(ObstacleFacing.Left));
            Ovg04ValidationSetup.CaptureEditor(map,Ovg04ValidationSetup.Evidence+"/scene-saved-reopened.png");
            Assert.That(Directory.GetFiles(CampaignMapBackupStore.BackupRoot+"/"+map.StageId,"*.json").Length,Is.GreaterThan(0));
        }

        [Test] public void CheckpointSafeFeetAndTerrainDeviceOwnershipAreValidatedBeforeCommit()
        {
            var cp=map.Objects.Single(o=>o.Kind==StageMapObjectKind.Checkpoint);
            var settings=cp.Settings.Clone();settings.EditorSetCheckpointRespawnOffset(new Vector2(0,-1));
            Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=cp.StableId,settings=settings}));
            var type=adapter.Objects.Find(StageMapObjectKind.CommonC04);
            var hazard=new TerrainEditorObjectEdit{operation="Place",definitionId=type.StableTypeId,instanceId="unsafe-hazard",origin=new Vector2Int(6,1),settings=type.DefaultSettings.Clone()};
            hazard.settings.EditorSetObstacleStyle("T01_A");Assert.Throws<InvalidOperationException>(()=>adapter.CommitObject(hazard));
            var terrain=adapter.Read();terrain.freeShape.cells.Add(new Animol.TerrainStructure.FreeShapeCell{x=6,y=1,styleId="T01_A"});
            Assert.Throws<InvalidOperationException>(()=>adapter.Commit(terrain,"unsafe respawn terrain"));
            Assert.That(map.Objects.Any(o=>o.StableId=="unsafe-hazard"),Is.False);
            Assert.That(StageMapMarkerValidation.ValidateLocalPlay(map).IsValid,Is.True);
        }

        [Test] public void MarkerGhostAndCountLimitsUseCurrentPalette()
        {
            TerrainEditorSceneEditor.SelectPart("marker:START");TerrainEditorSceneEditor.UpdateCandidate(new Vector2Int(2,0),false);
            Assert.That(TerrainEditorSceneEditor.GhostValid,Is.False);
            TerrainEditorSceneEditor.SelectPart("marker:CHECK");TerrainEditorSceneEditor.UpdateCandidate(new Vector2Int(8,0),false);
            Assert.That(TerrainEditorSceneEditor.GhostValid,Is.True,TerrainEditorSceneEditor.Status);
            TerrainEditorSceneEditor.Commit();
            Assert.That(map.Objects.Count(o=>o.Kind==StageMapObjectKind.Checkpoint),Is.EqualTo(2));
            foreach(var part in adapter.Markers.Where(p=>CommonObstacleCatalog.IsMarker(p.marker.Value)))
            {
                Assert.That(adapter.Thumbnail(part).color,Is.Not.Null);
                Assert.That(CampaignMapMarkerGeometry.ArtSprite(part.marker.Value).texture,Is.Not.Null);
            }
            Assert.That(adapter.Objects.Types.Select(t=>t.StableTypeId),Is.EquivalentTo(new[]{"C01","C02","C03","C04","C05","C06","C07","C08","C10"}));
        }

        [Test] public void IndependentSpringsHaveNoSyntheticTerrainBodyAndKeepGlobalTopology()
        {
            foreach(var kind in new[]{StageMapObjectKind.CommonC02,StageMapObjectKind.CommonC03})
            {
                var item=map.Objects.Single(o=>o.Kind==kind);var cell=ObstacleSnapshotAdapter.Read(map,item);
                var plan=ObstacleVisualResolver.Resolve((x,y)=>x==item.X&&y==item.Y?cell:null,item.X,item.Y,37);
                Assert.That(plan.DrawBody,Is.False);Assert.That(plan.CapPatch,Is.False);
                Assert.That(plan.ReplaceCap,Is.EqualTo(kind==StageMapObjectKind.CommonC03));
            }
            var affected=ObstacleVisualResolver.Affected(-17,0);Assert.That(affected.Cells.Count,Is.EqualTo(9));Assert.That(affected.MotifAnchors.Count,Is.EqualTo(36));
            Assert.That(ObstacleArtRegistry.Load().visualVersion,Is.EqualTo(3));
            Assert.That(ObstacleArtRegistry.Load().terrainArtVersion,Is.EqualTo(6));
            Assert.That(File.ReadAllText(ObstacleArtV1Builder.Root+"/Data/mechanism_catalog.json"),Does.Contain("\"terrainArtVersion\": 6"));
        }

        [Test] public void CheckpointStartsUnsetAndRepeatedActivationDoesNotResnapshotBubbles()
        {
            var checkpoints=new CheckpointRespawnState(new[]{"CP"},string.Empty);
            Assert.That(checkpoints.ActiveCheckpointId,Is.Empty);Assert.That(checkpoints.TryActivate("missing"),Is.False);
            var bubbles=new CheckpointBubbleCollectionState();bubbles.TryCollect("A");
            Assert.That(checkpoints.TryActivate("CP"),Is.True);bubbles.ActivateCheckpoint();bubbles.TryCollect("B");
            Assert.That(checkpoints.ActiveCheckpointId,Is.EqualTo("CP"));
            Assert.That(bubbles.Respawn(),Is.EquivalentTo(new[]{"B"}));
            Assert.That(bubbles.CollectedSlots,Is.EquivalentTo(new[]{"A"}));
            Assert.That(new CheckpointRespawnState(Array.Empty<string>(),string.Empty).ActiveCheckpointId,Is.Empty);
        }

        [Test] public void FocusedWorkspaceEntryAndMapSwitchPreserveAuthoredMap()
        {
            var original=EditorJsonUtility.ToJson(map);Selection.activeObject=map;
            var menu=(MenuItem)Attribute.GetCustomAttribute(typeof(TerrainEditorSceneEditor).GetMethod("OpenSelected"),typeof(MenuItem));
            Assert.That(menu.menuItem,Is.EqualTo("ANIMOL/Map Editor/Open Focused Workspace"));
            // Native menu lookup failed in this batch Editor. Exercise the registered
            // handler here; desktop menu click remains a separate manual check.
            TerrainEditorSceneEditor.OpenSelected();
            Assert.That(TerrainEditorSceneEditor.Adapter.Map,Is.SameAs(map));
            var other=UnityEngine.Object.Instantiate(map);var otherPath="Assets/TEST-OVG04-switch-"+Guid.NewGuid().ToString("N")+".asset";
            AssetDatabase.CreateAsset(other,otherPath);
            try
            {
                TerrainEditorSceneEditor.Open(other);Assert.That(TerrainEditorSceneEditor.Adapter.Map,Is.SameAs(other));
                TerrainEditorSceneEditor.Open(map);Assert.That(EditorJsonUtility.ToJson(map),Is.EqualTo(original));
            }
            finally{AssetDatabase.DeleteAsset(otherPath);}
        }
        [Test] public void CapturedSceneDragSurvivesInformationPanelAppearingUnderPointer()
        {
            var method=typeof(TerrainEditorSceneEditor).GetMethod("CanvasOwnsPointer",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var canvas=new Rect(0,40,1280,592);var information=new Rect(962,48,310,576);
            var point=new Vector2(1071,469);
            Assert.That((bool)method.Invoke(null,new object[]{canvas,information,point,false}),Is.False,"Panel owns a new gesture.");
            Assert.That((bool)method.Invoke(null,new object[]{canvas,information,point,true}),Is.True,"Existing canvas drag remains captured when selection opens its panel.");
            Assert.That((bool)method.Invoke(null,new object[]{canvas,information,new Vector2(1071,800),true}),Is.False,"Leaving the canvas still cancels the gesture.");
        }
    }
}
