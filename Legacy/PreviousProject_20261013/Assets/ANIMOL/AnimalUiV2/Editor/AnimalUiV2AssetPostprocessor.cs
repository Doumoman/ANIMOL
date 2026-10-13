using UnityEditor;
using UnityEngine;

namespace ANIMOL.AnimalUiV2.Editor
{
    public sealed class AnimalUiV2AssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AnimalUiV2Builder.Root + "/Art/", System.StringComparison.Ordinal) || !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;
            Configure((TextureImporter)assetImporter, assetPath);
        }
        internal static void Configure(TextureImporter importer, string path)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.crunchedCompression = false;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.spriteBorder = path.EndsWith("UI_Common_Panel_Main.png") ? new Vector4(20, 20, 20, 20) :
                path.EndsWith("UI_Common_Button_Primary.png") ? new Vector4(14, 14, 14, 14) :
                path.EndsWith("UI_Common_Button_Secondary.png") ? new Vector4(22, 16, 22, 16) : Vector4.zero;
        }
    }
}
