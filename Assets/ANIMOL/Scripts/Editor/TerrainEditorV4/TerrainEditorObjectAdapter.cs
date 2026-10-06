using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed partial class TerrainEditorAdapter
    {
        public StageMapObjectPlacement ObjectCandidate(TerrainEditorObjectEdit edit)
        {
            if(edit.operation=="Place" || edit.operation=="PairAnchor" || edit.operation=="Pair")
            {
                var marker=Markers.FirstOrDefault(m=>m.id==edit.definitionId);
                if(marker!=null)return new StageMapObjectPlacement(edit.instanceId,marker.marker.Value,edit.origin.x,edit.origin.y,"manual-authoring");
                var type=Objects.Find(edit.definitionId)??throw new InvalidOperationException("Unknown object definition: "+edit.definitionId);
                var initialSettings=edit.settings??StageMapObjectAuthoringOperations.PlacementSettings(type,edit.origin);
                if(edit.operation=="PairAnchor")initialSettings.EditorSetLinkedInstanceIds(new[]{"PAIR-PREVIEW-ONLY"});
                return new StageMapObjectPlacement(edit.instanceId,type.Kind,edit.origin.x,edit.origin.y,type.StableTypeId,type.Prefab,initialSettings);
            }
            var source=Map.Objects.FirstOrDefault(o=>o.StableId==edit.instanceId)??throw new InvalidOperationException("Object instance is missing.");
            var settings=(edit.settings??source.Settings).Clone();
            var origin=edit.operation=="Move"?edit.origin:new Vector2Int(source.X,source.Y);
            if(edit.operation=="Move")settings.EditorSetPathCells(settings.PathCells.Select(p=>p+origin-new Vector2Int(source.X,source.Y)));
            return new StageMapObjectPlacement(source.StableId,source.Kind,origin.x,origin.y,source.DataKey,source.Prefab,settings);
        }
        public void ValidateObject(TerrainEditorObjectEdit edit)
        {
            var candidate=ObjectCandidate(edit);
            if(edit.operation=="Delete")return;
            if(CommonObstacleCatalog.IsRetired(candidate.Kind))throw new InvalidOperationException("폐기된 장애물입니다. 공통 C01~C10을 새로 배치하세요.");
            var type=Objects.Find(candidate);
            bool marker=T01S01ManualMapWorkflow.IsCampaignMarker(candidate.Kind);
            if(type==null && !marker || candidate.Settings.Version>3)throw new InvalidOperationException("지원되지 않는 객체 설정은 원본을 보존합니다.");
            if(marker && edit.operation=="Place" && candidate.Kind is StageMapObjectKind.PlayerStart or StageMapObjectKind.Exit && Map.Objects.Any(o=>o.Kind==candidate.Kind))throw new InvalidOperationException("START/EXIT는 하나만 배치할 수 있습니다.");
            if(type!=null && !string.IsNullOrEmpty(type.ThemeId) && type.ThemeId!="COMMON" && type.ThemeId!=Map.ThemeId)
                throw new InvalidOperationException("다른 테마 객체는 현재 맵에 배치할 수 없습니다.");
            if(ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Kind(candidate.Kind)!=null)
            {
                var devices=TerrainEditorArt.ObstaclePreview(Map);
                var old=Map.Objects.FirstOrDefault(o=>o.StableId==candidate.StableId);
                if(old!=null)devices.Remove(new Vector2Int(old.X,old.Y));
                devices[new Vector2Int(candidate.X,candidate.Y)]=ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Read(Map,candidate);
                ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Validate(Map.FreeShapeTerrain,devices);
            }
            // Placement validation only reads the map; do not clone every serialized record on hover.
            {
                var resolved=Map.ResolveTerrain(Terrain);
                if(edit.operation=="Pair")
                {
                    var pairValidation=StageMapObjectAuthoringOperations.ValidateLinkedPairPlacement(Map,type,edit.origin,edit.second);
                    if(!pairValidation.IsValid)throw new InvalidOperationException(string.Join(" | ",pairValidation.Errors));
                    foreach(var origin in new[]{edit.origin,edit.second})
                        for(int x=0;x<Mathf.CeilToInt(type.FootprintCells.x);x++)for(int y=0;y<Mathf.CeilToInt(type.FootprintCells.y);y++)
                            if(resolved.solids.TryGetValue(origin+new Vector2Int(x,y),out var s)&&!string.IsNullOrEmpty(s.ownerId))throw new InvalidOperationException("연결 객체가 구조물 # 고체를 침범합니다.");
                    return;
                }
                if(StageMapDefinition.EnumeratePlacementCells(candidate).Any(c=>resolved.solids.TryGetValue(c,out var s)&&!string.IsNullOrEmpty(s.ownerId)))
                    throw new InvalidOperationException("객체 경로/범위가 구조물 # 고체를 침범합니다.");
                var validation=StageMapObjectAuthoringOperations.ValidatePlacement(Map,candidate,edit.operation=="Place"?null:edit.instanceId);
                if(!validation.IsValid)throw new InvalidOperationException(string.Join(" | ",validation.Errors));
            }
        }
        public void RemoveRetiredObstacles()
        {
            RequireEdit();Bind();var map=Map;
            var retired=map.Objects.Where(o=>CommonObstacleCatalog.IsRetired(o.Kind)).Select(o=>o.StableId).ToArray();
            if(retired.Length==0)return;
            var before=EditorJsonUtility.ToJson(map);int revision=map.AuthoringRevision;
            UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Remove retired ANIMOL obstacles");UnityEditor.Undo.RecordObject(map,"Remove retired obstacles");
            try
            {
                map.EditorBatchAuthoredChanges(()=>{foreach(var id in retired)map.EditorRemoveObject(id);});
                if(map.AuthoringRevision!=revision+1)throw new InvalidOperationException("Retirement must be one revision.");
                map.EditorInvalidateTerrainDerivedCache();
                var validation=StageMapValidator.ValidateStructure(map);
                if(!validation.IsValid)throw new InvalidOperationException(string.Join(" | ",validation.Errors));
                map.EditorInvalidateTerrainDerivedCache();EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                UnityEditor.Undo.FlushUndoRecordObjects();Notify();UnityEditor.Undo.CollapseUndoOperations(group);
            }
            catch
            {
                UnityEditor.Undo.FlushUndoRecordObjects();Animol.TerrainStructure.Editor.AnimolTerrainMapEditorBridge.RevertUndoGroup(group);
                EditorJsonUtility.FromJsonOverwrite(before,map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                try{Notify();}catch{}throw;
            }
        }
        public void CommitObject(TerrainEditorObjectEdit edit)
        {
            RequireEdit();ValidateObject(edit);Bind();
            var map=Map;var before=EditorJsonUtility.ToJson(map);var revision=map.AuthoringRevision;
            UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("ANIMOL "+edit.operation+" Object");UnityEditor.Undo.RecordObject(map,"ANIMOL Object");
            try
            {
                // This adapter owns the transaction. Reusing the legacy convenience actions
                // imported an Assets backup and saved/recorded Undo a second time per click.
                CampaignMapBackupStore.CreateBackup(map,"autosave-before-object-"+edit.operation);
                var candidate=ObjectCandidate(edit);
                switch(edit.operation)
                {
                    case "Place": case "Move": case "Settings":
                        map.EditorPlaceObject(candidate.StableId,candidate.Kind,candidate.X,candidate.Y,
                            candidate.DataKey,candidate.Prefab,candidate.Settings);
                        break;
                    case "Delete":
                        if(!map.EditorRemoveObject(edit.instanceId))throw new InvalidOperationException("Object instance is missing.");
                        break;
                    default: throw new InvalidOperationException("Unsupported current obstacle operation: "+edit.operation);
                }
                if(map.AuthoringRevision!=revision+1)throw new InvalidOperationException("Object edit did not produce exactly one authoring revision.");
                map.EditorInvalidateTerrainDerivedCache();EditorUtility.SetDirty(map);
                UnityEditor.Undo.FlushUndoRecordObjects();Notify();AssetDatabase.SaveAssetIfDirty(map);
                UnityEditor.Undo.CollapseUndoOperations(group);
            }
            catch
            {
                UnityEditor.Undo.FlushUndoRecordObjects();Animol.TerrainStructure.Editor.AnimolTerrainMapEditorBridge.RevertUndoGroup(group);
                EditorJsonUtility.FromJsonOverwrite(before,map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                try { Notify(); } catch { /* Preserve the original failure after restoring the asset. */ }
                throw;
            }
        }
    }
}
