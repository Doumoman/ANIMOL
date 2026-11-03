using System;

namespace ANIMOL.Art.CoastConnectedDecorV01
{
    // Data-only source JSON. These are VISUAL art records, never gameplay IDs/colliders.
    [Serializable] public sealed class CoastConnectedDecorV01Manifest
    {
        public int schemaVersion;
        public string packageId;
        public CoastConnectedDecorV01AssetInput[] assets;
        public CoastConnectedDecorV01GroupInput[] groups;
    }
    [Serializable] public sealed class CoastConnectedDecorV01AssetInput
    {
        public string file, sha256, family, kind;
        public int width, height;
        public float ppu;
        public float[] pivot;
        public int[] logicalRect, footprintCells;
    }
    [Serializable] public sealed class CoastConnectedDecorV01GroupInput
    {
        public string key, file;
        public int[] footprintCells;
    }
}
