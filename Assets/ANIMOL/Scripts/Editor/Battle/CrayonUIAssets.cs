using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ANIMOL.UI.Crayon.Editor
{
    public static class CrayonUIAssets
    {
        public const string Root = "Assets/ANIMOL/BattleCrayonUI";
        [Serializable] public sealed class Manifest { public int[] designResolution; public Entry[] entries; }
        [Serializable] public sealed class Entry { public string name; public int width, height, borderPx; public string[] variants; }
        public static Manifest ReadManifest() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/manifest.json"));
        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Sprites/" + name + ".png");
        public static Material Material(string name, bool still = false) => AssetDatabase.LoadAssetAtPath<Material>(
            Root + "/Materials/M_" + name + (still ? "_Still.mat" : "_Animated.mat"));

        public static object Import()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int changed = 0;
            var entries = ReadManifest().entries;
            foreach (var path in Directory.GetFiles(Root + "/Sprites", "*.png", SearchOption.AllDirectories))
            {
                var assetPath = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                var entry = entries.FirstOrDefault(e => e.variants.Contains(Path.GetFileName(path)));
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool needs = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                    settings.spriteMeshType != SpriteMeshType.FullRect || importer.spritePixelsPerUnit != 100f ||
                    importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp ||
                    importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    !importer.alphaIsTransparency || !string.IsNullOrEmpty(importer.spritePackingTag) ||
                    entry != null && importer.spriteBorder != Vector4.one * entry.borderPx;
                if (!needs) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteExtrude = 0;
                importer.SetTextureSettings(settings);
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.spritePackingTag = "";
                if (entry != null) importer.spriteBorder = Vector4.one * entry.borderPx;
                importer.SaveAndReimport();
                changed++;
            }
            return new { importedSprites = 58, correctedImporters = changed, errors = Validate() };
        }

        public static string[] Validate()
        {
            var errors = new List<string>();
            foreach (var entry in ReadManifest().entries)
            {
                foreach (var variant in entry.variants)
                {
                    string path = Root + "/Sprites/" + variant;
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!importer || !sprite) { errors.Add("Missing sprite: " + path); continue; }
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                        settings.spriteMeshType != SpriteMeshType.FullRect || importer.spritePixelsPerUnit != 100f ||
                        importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp ||
                        importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                        !importer.alphaIsTransparency || !string.IsNullOrEmpty(importer.spritePackingTag)) errors.Add("Import settings: " + path);
                    if (sprite.rect.width != entry.width || sprite.rect.height != entry.height ||
                        sprite.border != Vector4.one * entry.borderPx || sprite.vertices.Length != 4) errors.Add("Size/border/Full Rect: " + path);
                }
                foreach (bool still in new[] { false, true })
                {
                    var material = Material(entry.name, still);
                    if (!material || !material.shader || !material.shader.isSupported) { errors.Add("Missing/unsupported material: " + entry.name); continue; }
                    if (material.GetTexture("_FrameB") != Sprite(entry.name + "_B").texture ||
                        material.GetTexture("_FrameC") != Sprite(entry.name + "_C").texture ||
                        material.GetFloat("_Motion") != (still ? 0f : 1f) ||
                        Mathf.Abs(material.GetFloat("_FrameRate") - 1.666667f) > .000001f)
                        errors.Add("Material frames/motion/rate: " + material.name);
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" }))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var packable in atlas.GetPackables())
                {
                    var path = AssetDatabase.GetAssetPath(packable);
                    if (path.StartsWith(Root, StringComparison.Ordinal) ||
                        AssetDatabase.IsValidFolder(path) && Root.StartsWith(path + "/", StringComparison.Ordinal))
                        errors.Add("Atlas contains crayon assets: " + atlas.name);
                }
            }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/UI_ANIMOL_CrayonFrameBoil.shader");
            if (!shader) errors.Add("Missing shader");
            else errors.AddRange(ShaderUtil.GetShaderMessages(shader).Select(m => m.severity + ": " + m.message));
            return errors.ToArray();
        }
    }
}
