using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Animol.TerrainStructure.Editor
{
    /// <summary>Connect the actual shown PNGs to immutable source-frame Sprite assets.</summary>
    public static class AnimolTerrainSourceArtBuilder
    {
        private const string Root="Assets/ANIMOL/TerrainStructure";
        private const string Generated=Root+"/Generated";
        [Serializable] private sealed class SourceRect { public int x,y,width,height; }
        [Serializable] private sealed class FrameEntry
        {
            public string id,styleId,role,sourceFile,sourceSHA256;
            public SourceRect sourceRect;
            public SourceRect[] excludedSourceRects;
            public int width,height;
            public float uniformScale;
            public float[] visualOffset;
        }
        [Serializable] private sealed class FrameCatalog { public FrameEntry[] frames; }
        [Serializable] private sealed class StampEntry { public string id,styleId,frameId; public int width,height; public string[] rows; }
        [Serializable] private sealed class StampCatalog { public StampEntry[] stamps; }

        [MenuItem("ANIMOL/Terrain Structure/Initialize Shown Design Art")]
        public static void Initialize()
        {
            TextAsset a=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/source_art_frames.json");
            TextAsset b=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/editor_tile_catalog.json");
            if(a==null||b==null) throw new InvalidOperationException("Source design JSON is missing. Copy the complete v2 folder.");
            FrameEntry[] frames=JsonUtility.FromJson<FrameCatalog>(a.text).frames;
            AnimolTerrainCatalogData catalog=JsonUtility.FromJson<AnimolTerrainCatalogData>(b.text);
            if(!AnimolTerrainPlacementEngine.ValidateCatalog(catalog,out string catalogError)) throw new InvalidOperationException(catalogError);
            AnimolTerrainCatalogEntry[] stamps=catalog.entries;
            if(frames==null||frames.Length!=60||stamps==null||stamps.Length!=60) throw new InvalidOperationException("Expected 60 source-art frames and stamps.");
            EnsureFolder(Generated+"/SourceFrames"); EnsureFolder(Generated+"/SourceStamps");
            Material material=GetMaterial();
            foreach(FrameEntry entry in frames)
            {
                string path=Generated+"/SourceFrames/"+entry.id+".asset";
                AnimolTerrainArtFrameDefinition existing=AssetDatabase.LoadAssetAtPath<AnimolTerrainArtFrameDefinition>(path);
                if(existing!=null)
                { if(!existing.ValidateForFootprint(entry.width,entry.height,out string oldError)) throw new InvalidOperationException(entry.id+": "+oldError); continue; }
                string sourcePath=Root+"/SourceArt/"+entry.sourceFile;
                using(SHA256 hash=SHA256.Create())
                {
                    string actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(sourcePath))).Replace("-","").ToLowerInvariant();
                    if(actual!=entry.sourceSHA256) throw new InvalidOperationException("Shown design source changed: "+entry.sourceFile);
                }
                Texture2D texture=ImportSource(sourcePath);
                SourceRect r=entry.sourceRect;
                if(r==null||r.x<0||r.y<0||r.width<=0||r.height<=0||r.x+r.width>texture.width||r.y+r.height>texture.height)
                    throw new InvalidOperationException("Source rectangle exceeds PNG: "+entry.id);
                if(entry.visualOffset==null||entry.visualOffset.Length!=3) throw new InvalidOperationException("Invalid visual offset: "+entry.id);
                AnimolTerrainArtFrameDefinition frame=ScriptableObject.CreateInstance<AnimolTerrainArtFrameDefinition>();
                frame.UniformScale=entry.uniformScale;
                frame.VisualOffset=new Vector3(entry.visualOffset[0],entry.visualOffset[1],entry.visualOffset[2]);
                frame.Material=FrameMaterial(entry,texture,material);
                Sprite sprite=Sprite.Create(texture,new Rect(r.x,texture.height-r.y-r.height,r.width,r.height),Vector2.zero,32,0,SpriteMeshType.FullRect);
                sprite.name=entry.id+"_SHOWN_ART";
                frame.Sprite=sprite;
                if(!frame.ValidateForFootprint(entry.width,entry.height,out string error)) throw new InvalidOperationException(entry.id+": "+error);
                AssetDatabase.CreateAsset(frame,path); AssetDatabase.AddObjectToAsset(sprite,frame); EditorUtility.SetDirty(frame);
            }
            foreach(AnimolTerrainCatalogEntry entry in stamps)
            {
                // V3 fill art is an InteriorOverlay. It must never become a solid stamp.
                // Its support/collision rules are owned by editor_tile_catalog.json.
                if(entry.kind != AnimolTerrainPlacementEngine.StructureKind) continue;
                string path=Generated+"/SourceStamps/"+entry.id+".asset";
                AnimolTerrainStampDefinition existing=AssetDatabase.LoadAssetAtPath<AnimolTerrainStampDefinition>(path);
                if(existing!=null)
                { if(!existing.Validate(out string oldError)) throw new InvalidOperationException(entry.id+": "+oldError); continue; }
                AnimolTerrainStampDefinition stamp=ScriptableObject.CreateInstance<AnimolTerrainStampDefinition>();
                stamp.StyleId=entry.styleId; stamp.Width=entry.width; stamp.Height=entry.height; stamp.Rows=entry.solidRows;
                stamp.SourceArtFrame=AssetDatabase.LoadAssetAtPath<AnimolTerrainArtFrameDefinition>(Generated+"/SourceFrames/"+entry.frameId+".asset");
                if(stamp.SourceArtFrame==null||!stamp.Validate(out string error)) throw new InvalidOperationException("Invalid source-art stamp: "+entry.id);
                AssetDatabase.CreateAsset(stamp,path);
            }
            if(AssetDatabase.LoadMainAssetAtPath(Generated+"/DEV_Solid.asset")==null)
            {
                Tile tile=ScriptableObject.CreateInstance<Tile>(); tile.name="DEV_Solid"; tile.colliderType=Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile,Generated+"/DEV_Solid.asset");
            }
            foreach(string guid in AssetDatabase.FindAssets("",new[]{Generated}))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            Debug.Log("ANIMOL: 5 shown design sheets connected to 60 source-art frames and 40 structural stamps. The Saved Map Editor handles 20 interior overlays without adding collision. Unity play/collision QA still required.");
        }
        private static Material GetMaterial()
        {
            string path=Generated+"/SourceArtPalette.mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            Shader shader=Shader.Find("ANIMOL/Source Art Sweetie16");
            if(shader==null||!shader.isSupported) throw new InvalidOperationException("Source Art Sweetie16 shader is missing or unsupported by this graphics setup.");
            material=new Material(shader); material.name="ANIMOL_Shown_Art_Sweetie16"; material.SetFloat("_AlphaCutoff",0.85f);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        private static Material FrameMaterial(FrameEntry entry,Texture2D texture,Material shared)
        {
            if(entry.excludedSourceRects==null||entry.excludedSourceRects.Length==0) return shared;
            if(entry.excludedSourceRects.Length!=1) throw new InvalidOperationException("Only one neighbouring-source exclusion is supported per frame.");
            string path=Generated+"/SourceFrames/"+entry.id+"_Display.mat";
            Material existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null) return existing;
            SourceRect e=entry.excludedSourceRects[0];
            Material material=new Material(shared); material.name=entry.id+"_Clean_Source_Display";
            material.SetFloat("_HasExclude",1);
            material.SetVector("_ExcludeRect",new Vector4((float)e.x/texture.width,(float)(texture.height-e.y-e.height)/texture.height,(float)(e.x+e.width)/texture.width,(float)(texture.height-e.y)/texture.height));
            AssetDatabase.CreateAsset(material,path); return material;
        }
        private static Texture2D ImportSource(string path)
        {
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing shown design PNG: "+path);
            bool change=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||
                importer.spritePixelsPerUnit!=32||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||
                importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.npotScale!=TextureImporterNPOTScale.None||importer.maxTextureSize<2048||
                importer.alphaSource!=TextureImporterAlphaSource.FromInput||!importer.alphaIsTransparency||!importer.sRGBTexture;
            if(change)
            {
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
                importer.alphaSource=TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency=true; importer.sRGBTexture=true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
