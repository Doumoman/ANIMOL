using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Standalone authoring sample only. Production persists only the placement collection
    /// in StageMapDefinition. Its existing terrain fields supply the transient base-cell view.
    /// </summary>
    [CreateAssetMenu(fileName = "ANIMOL_TerrainMap_Sample", menuName = "ANIMOL/Terrain Structure/Saved Map Sample")]
    public sealed class AnimolTerrainMapDocument : ScriptableObject
    {
        public AnimolTerrainSavedMap data = new AnimolTerrainSavedMap { themeId = "T01" };
    }
}
