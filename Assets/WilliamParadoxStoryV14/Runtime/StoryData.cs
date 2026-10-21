using System;

namespace WilliamParadox.Story
{
    // Flat public fields intentionally match Unity JsonUtility (no dictionaries in JSON).
    [Serializable]
    public sealed class StoryPackage
    {
        public int schemaVersion = 14;
        public string packageId;
        public string contentVersion;
        public string startNodeId;
        public StoryAsset[] assets;
        public StoryNode[] nodes;
    }

    [Serializable]
    public sealed class StoryAsset
    {
        public string id;
        public string resourcePath;
        public string kind;
        public StorySpriteRect rect;
    }

    [Serializable]
    public sealed class StorySpriteRect
    {
        public float x, y, width, height;
        public float pivotX = 0.5f, pivotY = 0.5f;

        // JsonUtility instantiates this nested class even when "rect" is absent
        // from a story asset. Treat that default instance as an unsliced sprite.
        public static bool IsSpecified(StorySpriteRect value)
        {
            return value != null && (value.x != 0f || value.y != 0f ||
                value.width != 0f || value.height != 0f ||
                value.pivotX != 0.5f || value.pivotY != 0.5f);
        }
    }

    [Serializable]
    public sealed class StoryNode
    {
        public string id, sceneId, beatId;
        public string kind;
        public string speaker, text, artId, nextId;
        public StoryCue[] cues;
        public string[] setFlags, requiredFlags;
        public string gateId, gateLabel;
        public string[] credits;
    }

    [Serializable]
    public sealed class StoryCue
    {
        public string type;
        public string target;
        public float duration, delay, amount;
        public string assetId;
        public float x, y;
        public float scale = 1f;
    }

    [Serializable]
    public sealed class StorySaveData
    {
        public int schemaVersion = 14;
        public string packageId, contentVersion, nodeId;
        public string[] flags;
        public bool completed;
        public bool reducedMotion;
        public float artScale = 1f, dimAlpha;
        public string insetAssetId;
        public bool insetVisible;
        public float insetX, insetY, insetScale = 1f;
        public float creditsPosition, creditsHold;
        public int creditsParagraph;
        public bool creditsReachedEnd, creditsPaused;
    }
}
