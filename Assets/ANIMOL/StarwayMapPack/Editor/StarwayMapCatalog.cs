using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    public sealed class StarwayMapCatalog : ScriptableObject
    {
        public int designVersion = 7;
        public StarwayMapSource[] maps;
    }
}
