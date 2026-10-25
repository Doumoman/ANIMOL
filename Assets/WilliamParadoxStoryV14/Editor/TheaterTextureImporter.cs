#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    // Only this chapter's newly delivered resources. No existing ranch/game texture is migrated.
    public sealed class TheaterTextureImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/WilliamParadoxStoryV14/Resources/WilliamParadox/Theater/Art";

        public static void ReimportAndValidateFromCommandLine()
        {
            try
            {
                var files = Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories);
                if (files.Length != 24) throw new InvalidDataException("Expected 24 theater PNGs, found " + files.Length);
                foreach (var file in files)
                {
                    var path = file.Replace('\\', '/');
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                        importer.spriteImportMode != SpriteImportMode.Single ||
                        importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize != 2048 ||
                        importer.mipmapEnabled || importer.isReadable || !importer.sRGBTexture ||
                        !importer.alphaIsTransparency || importer.filterMode != FilterMode.Bilinear ||
                        importer.wrapMode != TextureWrapMode.Clamp ||
                        importer.textureCompression != TextureImporterCompression.CompressedHQ)
                        throw new InvalidDataException("Theater texture import settings differ: " + path);
                }
                Debug.Log("Theater Art Reimport/Importer 검증 통과: " + files.Length + " PNG, NPOT None, 2048, Single Sprite, mipmap/readable off, sRGB, alpha, Bilinear, Clamp, CompressedHQ.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private void OnPreprocessTexture()
        {
            const string prefix = ArtRoot + "/";
            if (!assetPath.StartsWith(prefix, System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
