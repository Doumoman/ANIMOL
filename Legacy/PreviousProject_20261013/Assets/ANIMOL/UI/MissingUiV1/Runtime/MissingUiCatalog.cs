using System;

namespace ANIMOL.MissingUiV1
{
    // Design schema only. Existing production IDs and service protocols remain project owned.
    [Serializable] public sealed class MissingUiCatalog
    {
        public int schemaVersion;
        public int competitiveCapacity;
        public MissingUiScreen[] screens;
    }
    [Serializable] public sealed class MissingUiScreen
    {
        public string id, title, subtitle, existingScreen, priority, family;
        public MissingUiBlock[] blocks;
        public MissingUiFooter footer;
    }
    [Serializable] public sealed class MissingUiBlock
    {
        public string kind, id, title, body, asset, secondary, action;
        public bool enabled;
        public MissingUiItem[] items;
    }
    [Serializable] public sealed class MissingUiItem
    {
        public string id, title, body, asset, action;
        public bool enabled;
    }
    [Serializable] public sealed class MissingUiFooter
    {
        public string label, action;
        public bool enabled;
    }
    [Serializable] public sealed class MissingUiManifest
    {
        public int schemaVersion;
        public MissingUiAsset[] assets;
    }
    [Serializable] public sealed class MissingUiAsset
    {
        public string id, path, imageType;
        public int[] size, borderLBRTop, safePadding, minDisplaySize;
        public float[] pivot;
        public bool transparent;
        public string[] states, screens;
        public string palette;
    }
    [Serializable] public sealed class MissingUiAction
    {
        public string screenId, semanticKey, action, localValue;
    }
}
