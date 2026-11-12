using ANIMOL.Puzzle;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>Explicit temporary-build access, independent of saved purchase entitlements.</summary>
    public sealed class NotebookCampaignPreview : ScriptableObject
    {
        public const string AssetPath = "Assets/ANIMOL/Data/Puzzle/Resources/NotebookCampaignPreview.asset";
        public bool enabled;
        public EchoTheaterMapSet theaterMaps;
        public const string TheaterScene = "ANIMOL_Echo_Theater";
        public static NotebookCampaignPreview Current => Resources.Load<NotebookCampaignPreview>("NotebookCampaignPreview");
        public static bool IsEnabled => Current && Current.enabled;

        public static bool TryTheaterIndex(string stageId, out int index)
        {
            index = -1;
            var preview = Current;
            if (!preview || !preview.enabled || !preview.theaterMaps ||
                !NotebookCampaignStages.TryOrder(stageId, out string prefix, out int order) || prefix != "ET-") return false;
            index = order - 1;
            var entries = preview.theaterMaps.maps;
            return index < entries.Count && entries[index] != null && entries[index].stageId == stageId && entries[index].map;
        }
    }
}
