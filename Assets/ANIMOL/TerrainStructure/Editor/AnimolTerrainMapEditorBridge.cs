using System;
using UnityEditor;
using UnityEngine;

namespace Animol.TerrainStructure.Editor
{
    /// <summary>
    /// Bind from CampaignMapEditorWindow after loading StageMapDefinition.
    /// read must return current data plus existing logical terrain projected as baseCells.
    /// write commits placements and changed base cells through existing authoring operations.
    /// Never migrate saved nine-direction tile values to 47 visual masks here.
    /// </summary>
    public static class AnimolTerrainMapEditorBridge
    {
        static AnimolTerrainMapEditorBridge() { Undo.undoRedoPerformed += AfterUndo; }
        public static UnityEngine.Object Owner { get; private set; }
        public static Func<AnimolTerrainSavedMap> Read { get; private set; }
        public static Action<AnimolTerrainSavedMap> Write { get; private set; }
        public static Action Changed { get; private set; }
        public static string Label { get; private set; }
        public static Action<AnimolTerrainSavedMap> Validate { get; private set; }

        public static void Bind(UnityEngine.Object owner, Func<AnimolTerrainSavedMap> read,
            Action<AnimolTerrainSavedMap> write, Action changed, string label, Action<AnimolTerrainSavedMap> validate = null)
        {
            if (owner == null || read == null || write == null)
                throw new ArgumentException("A saved map owner and read/write authoring adapter are required.");
            Owner = owner; Read = read; Write = write; Changed = changed;
            Label = string.IsNullOrEmpty(label) ? owner.name : label;
            Validate = validate;
        }

        public static void Unbind(UnityEngine.Object owner)
        {
            if (Owner != owner) return;
            Owner = null; Read = null; Write = null; Changed = null; Label = null; Validate = null;
        }

        public static bool IsBound { get { return Owner != null && Read != null && Write != null; } }

        public static void Commit(AnimolTerrainSavedMap candidate, string undoName)
        {
            if (!IsBound) throw new InvalidOperationException("Load and bind a map first.");
            Validate?.Invoke(candidate);
            // Host must record any additional touched assets in this same group and
            // run its bounds/object/path/revision validation before committing.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RecordObject(Owner, undoName);
            try
            {
                Write(candidate);
                EditorUtility.SetDirty(Owner);
                Undo.FlushUndoRecordObjects();
                if (Changed != null) Changed();
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                EditorUtility.SetDirty(Owner);
                AssetDatabase.SaveAssetIfDirty(Owner);
                if (Changed != null) Changed();
                throw;
            }
        }

        private static void AfterUndo()
        {
            if (!IsBound || Changed == null) return;
            try { EditorUtility.SetDirty(Owner); AssetDatabase.SaveAssetIfDirty(Owner); Changed(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
