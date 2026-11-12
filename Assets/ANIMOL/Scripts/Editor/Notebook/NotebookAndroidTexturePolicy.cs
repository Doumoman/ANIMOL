using System;
using UnityEditor;
using UnityEngine;

namespace William.NotebookUI.Editor
{
    /// <summary>Android GPU compression without resampling source artwork.</summary>
    public sealed class NotebookAndroidTexturePolicy : AssetPostprocessor
    {
        public static bool Manages(string path) =>
            (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) &&
            (path.StartsWith("Assets/ANIMOL/Art/", StringComparison.Ordinal) ||
             path.StartsWith("Assets/ANIMOL/MapHudCrayonUI/Sprites/", StringComparison.Ordinal) ||
             path.StartsWith("Assets/ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01/Sprites/", StringComparison.Ordinal) ||
             path.StartsWith("Assets/WilliamParadoxStoryV14/Resources/", StringComparison.Ordinal));
        public override uint GetVersion() => 1;
        public override int GetPostprocessOrder() => 10000;

        void OnPreprocessTexture()
        {
            if (!Manages(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            if (!android.overridden) android.maxTextureSize = importer.maxTextureSize;
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 50;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
