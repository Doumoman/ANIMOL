using System;
using UnityEditor;

namespace ANIMOL.Presentation.Editor
{
    // The pinned add-on importer handles its PNG allowlist. Unity also imports GIF
    // inspection examples as textures, so keep those outside the Sprite palette.
    public sealed class RanchConnectedDecorNonRuntimeImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            const string root = "Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_CONNECTED_DECOR_V01/";
            var path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(root, StringComparison.Ordinal) ||
                path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            ((TextureImporter)assetImporter).textureType = TextureImporterType.Default;
        }
    }
}
