using ANIMOL.Puzzle;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>Explicit temporary-build access, independent of saved purchase entitlements.</summary>
    public sealed class NotebookCampaignPreview : ScriptableObject
    {
        public const string AssetPath = "Assets/ANIMOL/Data/Puzzle/Resources/NotebookCampaignPreview.asset";
        public bool enabled;
        public string[] theaterStageIds = System.Array.Empty<string>();
        [System.NonSerialized] EchoTheaterMapSet mapOverride;
        [System.NonSerialized] bool hasMapOverride;
        public EchoTheaterMapSet theaterMaps
        {
            get => hasMapOverride ? mapOverride : NotebookDeferredAssets.Load(NotebookDeferredAssets.TheaterResource)?.theaterMaps;
            set { mapOverride = value; hasMapOverride = true; }
        }
        public const string TheaterScene = "ANIMOL_Echo_Theater";
        public static NotebookCampaignPreview Current => Resources.Load<NotebookCampaignPreview>("NotebookCampaignPreview");
        public static bool IsEnabled => Current && Current.enabled;

        public static bool TryTheaterIndex(string stageId, out int index)
        {
            index = -1;
            var preview = Current;
            if (!preview || !preview.enabled ||
                !NotebookCampaignStages.TryOrder(stageId, out string prefix, out int order) || prefix != "ET-") return false;
            index = order - 1;
            if (preview.hasMapOverride)
                return preview.mapOverride && index < preview.mapOverride.maps.Count &&
                    preview.mapOverride.maps[index] != null && preview.mapOverride.maps[index].stageId == stageId &&
                    preview.mapOverride.maps[index].map;
            return index < preview.theaterStageIds.Length && preview.theaterStageIds[index] == stageId;
        }
    }
}
