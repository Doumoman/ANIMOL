using System;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    // Data-only source JSON. These are VISUAL art records, never gameplay IDs/colliders.
    [Serializable] public sealed class RanchConnectedDecorV02Manifest
    {
        public int schemaVersion;
        public string packageId;
        public RanchConnectedDecorV02AssetInput[] assets;
        public RanchConnectedDecorV02GroupInput[] groups;
    }
    [Serializable] public sealed class RanchConnectedDecorV02AssetInput
    {
        public string file, sha256, family, kind;
        public int width, height;
        public float ppu;
        public float[] pivot;
        public int[] logicalRect, footprintCells;
    }
    [Serializable] public sealed class RanchConnectedDecorV02GroupInput
    {
        public string key, file;
        public int[] footprintCells;
    }
}
