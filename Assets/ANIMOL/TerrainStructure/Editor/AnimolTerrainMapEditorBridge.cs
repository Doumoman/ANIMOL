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
        private static bool reverting;

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
            // Undo callbacks may rebind another editor window. Pin this transaction's owner
            // and callbacks so failure recovery always restores the asset that was written.
            var owner = Owner; var write = Write; var changed = Changed;
            Validate?.Invoke(candidate);
            var snapshot = EditorJsonUtility.ToJson(owner);
            // Host must record any additional touched assets in this same group and
            // run its bounds/object/path/revision validation before committing.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RecordObject(owner, undoName);
            try
            {
                write(candidate);
                EditorUtility.SetDirty(owner);
                Undo.FlushUndoRecordObjects();
                changed?.Invoke();
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.FlushUndoRecordObjects();
                RevertUndoGroup(group);
                EditorJsonUtility.FromJsonOverwrite(snapshot, owner);
                EditorUtility.SetDirty(owner);
                AssetDatabase.SaveAssetIfDirty(owner);
                try { changed?.Invoke(); } catch { /* Keep the original render/write exception. */ }
                throw;
            }
        }

        private static void AfterUndo()
        {
            if (reverting || !IsBound || Changed == null) return;
            try { EditorUtility.SetDirty(Owner); AssetDatabase.SaveAssetIfDirty(Owner); Changed(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        // Failed transactions restore their snapshot and preview explicitly after Undo.
        // Do not run the normal Undo preview callback on the intermediate rollback state.
        public static void RevertUndoGroup(int group)
        {
            reverting = true;
            try { Undo.RevertAllDownToGroup(group); }
            finally { reverting = false; }
        }
    }
}
