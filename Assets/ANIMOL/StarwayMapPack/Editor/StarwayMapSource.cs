using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    // Editor-only staging asset. Build native project maps before adding stages to the campaign.
    public sealed class StarwayMapSource : ScriptableObject
    {
        public StarwayMapInput input;
        public TextAsset originalDesign;
        public TextAsset originalSolution;
        public TextAsset normalizedReplay;
    }
}
