using System;
using System.IO;
using System.Linq;
using ANIMOL.Gameplay.ObstacleGraphics;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class ObstacleArtV1Builder
    {
        public const string Source = "Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v1";
        public const string Root = "Assets/ANIMOL/ObstacleGraphicsV1";
        public const string RegistryPath = "Assets/ANIMOL/Resources/ANIMOL_ObstacleGraphicsV1.asset";
        [Serializable] private class Manifest { public Record[] entries; }
        [Serializable] private class Record { public string path, sha256; public long bytes; }
        [Serializable] private class Catalog { public int schemaVersion, visualVersion, terrainArtVersion, atlasWidth, atlasHeight; public Entry[] entries; }
        [Serializable] private class Entry { public string id; public Rectangle atlasRect; }
        [Serializable] private class Rectangle { public int x,y,width,height; }
        public static void Install()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Source + "/PACKAGE_MANIFEST.json"));
            if (manifest.entries.Length != 284) throw new InvalidOperationException("Incomplete source package.");
            foreach (var f in manifest.entries)
            {
                var path = Path.GetFullPath(Source + "/" + f.path);
                if (!path.StartsWith(Path.GetFullPath(Source) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(path) || new FileInfo(path).Length != f.bytes || FreeShapeArtV6Builder.HashFile(path) != f.sha256)
                    throw new InvalidOperationException("Source manifest mismatch: " + f.path);
            }
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Source + "/Data/mechanism_catalog.json"));
            if (catalog.schemaVersion != 1 || catalog.visualVersion != 1 || catalog.terrainArtVersion != 4 || catalog.entries.Length != 163 || catalog.atlasWidth != 576 || catalog.atlasHeight != 396)
                throw new InvalidOperationException("Unexpected source mechanism contract.");
            Directory.CreateDirectory(Root + "/Art"); Directory.CreateDirectory(Root + "/Data");
            var pathAtlas = Root + "/Art/MechanismAtlas.png";
            File.Copy(Source + "/Art/MechanismAtlas.png", pathAtlas, true);
            // Preserve source provenance; V6 adaptation is explicit in the installed Registry.
            File.Copy(Source + "/Data/mechanism_catalog.json", Root + "/Data/mechanism_catalog.json", true);
            AssetDatabase.ImportAsset(pathAtlas, ImportAssetOptions.ForceSynchronousImport);
            var t = (TextureImporter)AssetImporter.GetAtPath(pathAtlas);
            t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Multiple;
            t.spritePixelsPerUnit = 32; t.filterMode = FilterMode.Point; t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false; t.sRGBTexture = true; t.alphaIsTransparency = false;
            t.npotScale = TextureImporterNPOTScale.None; t.maxTextureSize = 2048;
            foreach (var platform in new[]{"Standalone","Android","iPhone","WebGL"}) t.ClearPlatformTextureSettings(platform);
            var settings = new TextureImporterSettings(); t.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false; t.SetTextureSettings(settings);
            t.spritesheet = catalog.entries.Select(e => new SpriteMetaData { name=e.id.Replace('/','_'), alignment=0, pivot=new Vector2(.5f,.5f),
                rect=new Rect(e.atlasRect.x, catalog.atlasHeight-e.atlasRect.y-e.atlasRect.height,e.atlasRect.width,e.atlasRect.height) }).ToArray();
            t.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(pathAtlas).OfType<Sprite>().ToDictionary(s=>s.name);
            var registry = AssetDatabase.LoadAssetAtPath<ObstacleArtRegistry>(RegistryPath);
            if (registry == null) { registry=ScriptableObject.CreateInstance<ObstacleArtRegistry>(); AssetDatabase.CreateAsset(registry,RegistryPath); }
            registry.visualVersion=1; registry.terrainArtVersion=6; registry.sourceHash=FreeShapeArtV6Builder.HashFile(Source+"/PACKAGE_MANIFEST.json");
            registry.entries=catalog.entries.Select(e=>new MechanismSprite{key=e.id,sprite=sprites[e.id.Replace('/','_')]}).ToArray();
            registry.ValidateComplete(); EditorUtility.SetDirty(registry); AssetDatabase.SaveAssetIfDirty(registry); AssetDatabase.Refresh();
        }
    }
}
