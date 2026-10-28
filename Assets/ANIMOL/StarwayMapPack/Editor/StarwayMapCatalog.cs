using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    public sealed class StarwayMapCatalog : ScriptableObject
    {
        public int designVersion = 3;
        public StarwayMapSource[] maps;
    }
}
