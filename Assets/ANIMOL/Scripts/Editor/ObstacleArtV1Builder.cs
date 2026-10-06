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
        public const string Source = "Tools/ArtSources/ANIMOL_Obstacle_V4_Design_Join_v2";
        public const string Root = "Assets/ANIMOL/ObstacleGraphicsV1";
        public const string RegistryPath = "Assets/ANIMOL/Resources/ANIMOL_ObstacleGraphicsV1.asset";
        [Serializable] private class Manifest { public Record[] entries; }
        [Serializable] private class Record { public string path, sha256; public long bytes; }
        [Serializable] private class Catalog { public int schemaVersion, visualVersion, terrainArtVersion, atlasWidth, atlasHeight; public Entry[] entries; }
        [Serializable] private class Entry { public string id; public Rectangle atlasRect; }
        [Serializable] private class Rectangle { public int x,y,width,height; }
        [Serializable] private class Fragments {public int visualVersion;public FragmentEntry[] entries;}
        [Serializable] private class FragmentEntry {public string themeId,file;public int id,pixelsPerUnit;public Rectangle sourceRect;public Vector2 pivot;}
        public static void Install()
        {
            bool previous=ANIMOL.NamedArt.Editor.NamedArtAutomation.Suspended;
            ANIMOL.NamedArt.Editor.NamedArtAutomation.Suspended=true;
            try{InstallCore();CommonObstacleV6Builder.Install();}
            finally{ANIMOL.NamedArt.Editor.NamedArtAutomation.Suspended=previous;}
        }
        private static void InstallCore()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Source + "/PACKAGE_MANIFEST.json"));
            if (manifest.entries.Length != 605) throw new InvalidOperationException("Incomplete source package.");
            foreach (var f in manifest.entries)
            {
                var path = Path.GetFullPath(Source + "/" + f.path);
                if (!path.StartsWith(Path.GetFullPath(Source) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(path) || new FileInfo(path).Length != f.bytes || FreeShapeArtV6Builder.HashFile(path) != f.sha256)
                    throw new InvalidOperationException("Source manifest mismatch: " + f.path);
            }
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Source + "/Data/mechanism_catalog.json"));
            if (catalog.schemaVersion != 1 || catalog.visualVersion != 2 || catalog.terrainArtVersion != 4 || catalog.entries.Length != 393 || catalog.atlasWidth != 576 || catalog.atlasHeight != 900)
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
            registry.visualVersion=2; registry.terrainArtVersion=6; registry.sourceHash=FreeShapeArtV6Builder.HashFile(Source+"/PACKAGE_MANIFEST.json");
            registry.entries=catalog.entries.Select(e=>new MechanismSprite{key=e.id,sprite=sprites[e.id.Replace('/','_')]}).ToArray();
            var fragments=JsonUtility.FromJson<Fragments>(File.ReadAllText(Source+"/Data/c05_fragment_catalog.json"));
            if(fragments.visualVersion!=2||fragments.entries.Length!=20)throw new InvalidOperationException("Invalid fragment catalog.");
            registry.fragments=fragments.entries.Select(f=>
            {
                var path=Root+"/"+f.file;Directory.CreateDirectory(Path.GetDirectoryName(path));File.Copy(Source+"/"+f.file,path,true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=f.pixelsPerUnit;
                importer.spritePivot=f.pivot;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.alphaIsTransparency=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})importer.ClearPlatformTextureSettings(platform);
                var config=new TextureImporterSettings();importer.ReadTextureSettings(config);config.spriteAlignment=0;config.spritePivot=f.pivot;config.spriteMeshType=SpriteMeshType.FullRect;config.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(config);
                importer.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite.rect.width!=f.sourceRect.width||sprite.rect.height!=f.sourceRect.height||f.pixelsPerUnit!=32)throw new InvalidOperationException("Fragment registration mismatch: "+f.file);
                return new FragmentSprite{themeId=f.themeId,piece=f.id,sprite=sprite};
            }).ToArray();
            foreach(var file in new[]{"c05_fragment_catalog.json","c05_fragment_effect.json"})File.Copy(Source+"/Data/"+file,Root+"/Data/"+file,true);
            registry.crumble=JsonUtility.FromJson<CrumbleEffectSettings>(File.ReadAllText(Source+"/Data/c05_fragment_effect.json"));
            registry.ValidateComplete(); EditorUtility.SetDirty(registry); AssetDatabase.SaveAssetIfDirty(registry); AssetDatabase.Refresh();
        }
    }
}
