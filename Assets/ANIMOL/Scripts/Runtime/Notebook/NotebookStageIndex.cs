using System;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>Progress and availability without deserializing puzzles or their art.</summary>
    public sealed class NotebookStageIndex : ScriptableObject
    {
        public const string AssetPath = "Assets/ANIMOL/Data/Puzzle/Resources/NotebookStageIndex.asset";
        [Serializable] public sealed class Entry
        {
            public string stageId, sourceId, assetName;
        }
        public Entry[] stages = Array.Empty<Entry>();
        public static NotebookStageIndex Current => Resources.Load<NotebookStageIndex>("NotebookStageIndex");

        public Entry Find(string stageId)
        {
            Entry found = null;
            foreach (var entry in stages)
            {
                if (entry == null || entry.stageId != stageId) continue;
                if (found != null) return null;
                found = entry;
            }
            return found;
        }
    }
}
