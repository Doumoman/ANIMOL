using System;

namespace ANIMOL.Art.RanchConnectedDecorV01
{
    // Data-only source JSON. These are VISUAL art records, never gameplay IDs/colliders.
    [Serializable] public sealed class RanchConnectedDecorV01Manifest
    {
        public int schemaVersion;
        public string packageId;
        public RanchConnectedDecorV01AssetInput[] assets;
        public RanchConnectedDecorV01GroupInput[] groups;
    }
    [Serializable] public sealed class RanchConnectedDecorV01AssetInput
    {
        public string file, sha256, family, kind;
        public int width, height;
        public float ppu;
        public float[] pivot;
        public int[] logicalRect, footprintCells;
    }
    [Serializable] public sealed class RanchConnectedDecorV01GroupInput
    {
        public string key, file;
        public int[] footprintCells;
    }
}
