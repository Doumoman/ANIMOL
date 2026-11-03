#if UNITY_EDITOR
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Editor-only ownership record. It is deliberately excluded from the player build.</summary>
    public sealed class StoryPreviewBuildStamp : ScriptableObject
    {
        public string builderVersion;
        public string fingerprint;
        public string sourceFolder;
        public string fontGuid;
        public string sceneGuid;
        public string prefabGuid;
        public string[] ownedAssetPaths = new string[0];
    }
}
#endif
