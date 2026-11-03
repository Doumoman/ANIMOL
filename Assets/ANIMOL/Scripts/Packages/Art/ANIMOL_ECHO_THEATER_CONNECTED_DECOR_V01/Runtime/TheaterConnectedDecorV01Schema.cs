using System;

namespace ANIMOL.Art.TheaterConnectedDecorV01
{
    // Data-only source JSON. These are VISUAL art records, never gameplay IDs/colliders.
    [Serializable] public sealed class TheaterConnectedDecorV01Manifest
    {
        public int schemaVersion;
        public string packageId;
        public TheaterConnectedDecorV01AssetInput[] assets;
        public TheaterConnectedDecorV01GroupInput[] groups;
    }
    [Serializable] public sealed class TheaterConnectedDecorV01AssetInput
    {
        public string file, sha256, family, kind;
        public int width, height;
        public float ppu;
        public float[] pivot;
        public int[] logicalRect, footprintCells;
    }
    [Serializable] public sealed class TheaterConnectedDecorV01GroupInput
    {
        public string key, file;
        public int[] footprintCells;
    }
}
