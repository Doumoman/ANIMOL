using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    // Explicit operation only: installation and opening an editor never migrate maps.
    public static class ObstacleV2Migration
    {
        [Serializable] public sealed class Result
        {
            public string path,backup,beforeHash,afterHash;
            public int beforeRevision,afterRevision;
            public Removed[] removed;
        }
        [Serializable] public sealed class Removed {public string id;public int x,y;}
        public static Result RemoveC09(StageMapDefinition map,Action refresh=null)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var records=map.Objects.Where(o=>o.Kind==StageMapObjectKind.CommonC09).ToArray();
            if(records.Length==0)return null;
            string path=AssetDatabase.GetAssetPath(map),before=EditorJsonUtility.ToJson(map);
            byte[] disk=File.Exists(path)?File.ReadAllBytes(path):null;
            var result=new Result{path=path,beforeHash=map.ContentHash,beforeRevision=map.AuthoringRevision,
                removed=records.Select(o=>new Removed{id=o.StableId,x=o.X,y=o.Y}).ToArray(),
                backup=CampaignMapBackupStore.CreateBackup(map,"OVG02-before-C09-deletion")};
            // Record other objects separately, so a migration cannot accidentally rewrite them.
            var retained=map.Objects.Except(records).Select(o=>JsonUtility.ToJson(o)).ToArray();
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("OVG02 delete removed C09");Undo.RecordObject(map,"Delete C09");
            try
            {
                map.EditorBatchAuthoredChanges(()=>{foreach(var record in records)map.EditorRemoveObject(record.StableId);});
                if(map.AuthoringRevision!=result.beforeRevision+1||!retained.SequenceEqual(map.Objects.Select(o=>JsonUtility.ToJson(o))))
                    throw new InvalidOperationException("C09 migration changed other objects or revision.");
                map.EditorInvalidateTerrainDerivedCache();var validation=StageMapValidator.ValidateStructure(map);
                if(!validation.IsValid)throw new InvalidOperationException(string.Join(" | ",validation.Errors));
                EditorUtility.SetDirty(map);Undo.FlushUndoRecordObjects();refresh?.Invoke();
                if(!string.IsNullOrEmpty(path))AssetDatabase.SaveAssetIfDirty(map);
                Undo.CollapseUndoOperations(group);result.afterRevision=map.AuthoringRevision;result.afterHash=map.ContentHash;return result;
            }
            catch
            {
                Undo.FlushUndoRecordObjects();Animol.TerrainStructure.Editor.AnimolTerrainMapEditorBridge.RevertUndoGroup(group);
                EditorJsonUtility.FromJsonOverwrite(before,map);
                if(disk!=null)File.WriteAllBytes(path,disk);
                try{refresh?.Invoke();}catch{}throw;
            }
        }
        public static Result[] MigrateStoredMaps()
        {
            var results=AssetDatabase.FindAssets("t:StageMapDefinition",new[]{"Assets/ANIMOL"})
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StageMapDefinition>)
                .Select(map=>RemoveC09(map)).Where(r=>r!=null).ToArray();
            Directory.CreateDirectory("Docs/Validation/OVG02");
            File.WriteAllText("Docs/Validation/OVG02/c09-migration.json",JsonUtility.ToJson(new Results{files=results},true));return results;
        }
        [Serializable] private sealed class Results {public Result[] files;}
    }
}
