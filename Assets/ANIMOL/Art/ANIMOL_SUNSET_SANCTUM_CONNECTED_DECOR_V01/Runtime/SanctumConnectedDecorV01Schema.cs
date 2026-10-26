using System;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    // Data-only source JSON. These are VISUAL art records, never gameplay IDs/colliders.
    [Serializable] public sealed class SanctumConnectedDecorV01Manifest
    {
        public int schemaVersion;
        public string packageId;
        public SanctumConnectedDecorV01AssetInput[] assets;
        public SanctumConnectedDecorV01GroupInput[] groups;
    }
    [Serializable] public sealed class SanctumConnectedDecorV01AssetInput
    {
        public string file, sha256, family, kind;
        public int width, height;
        public float ppu;
        public float[] pivot;
        public int[] logicalRect, footprintCells;
    }
    [Serializable] public sealed class SanctumConnectedDecorV01GroupInput
    {
        public string key, file;
        public int[] footprintCells;
    }
}
