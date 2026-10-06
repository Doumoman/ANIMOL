using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class FreeShapeArtBuilder
    {
        public const string Source="Tools/ArtSources/ANIMOL_FreeShape_Sprites_v1";
        public const string Root="Assets/ANIMOL/TerrainFreeShape";
        public const string RegistryPath="Assets/ANIMOL/Resources/ANIMOL_FreeShapeArt.asset";
        [Serializable] private class Manifest {public FileRecord[] files;}
        [Serializable] private class FileRecord {public string path,sha256;public long bytes;}
        [Serializable] private class RectData {public int x,y,width,height;}
        [Serializable] private class CellData {public int mask,index;public RectData rect;}
        [Serializable] private class Variant {public string id,atlas;public int[] atlasPixels;public CellData[] cells;}
        [Serializable] private class Panels {public string motif;}
        [Serializable] private class Style {public string styleId,themeId,themeName,displayName;public Variant[] variants;public Panels panels;}
        [Serializable] private class Catalog {public string contractId;public int[] canonicalMasks,rawToCanonical,rawToIndex;public Style[] styles;}
        [Serializable] private class Fixtures {public FreeShapeFixture[] fixtures;}
        public static string HashFile(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static T Read<T>(string path)=>JsonUtility.FromJson<T>(File.ReadAllText(Source+"/"+path));
        public static void Initialize()
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            var manifest=Read<Manifest>("PACKAGE_MANIFEST.json");
            foreach(var f in manifest.files){var path=Path.GetFullPath(Source+"/"+f.path);if(!path.StartsWith(Path.GetFullPath(Source)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(path)||new FileInfo(path).Length!=f.bytes||HashFile(path)!=f.sha256)throw new InvalidOperationException("Package SHA256 mismatch: "+f.path);}
            var catalog=Read<Catalog>("Data/sprite_lookup.json");var topology=Read<Catalog>("Data/topology_catalog.json");var names=Read<Catalog>("Data/style_catalog.json");
            if(catalog.contractId!=FreeShapeLayer.Contract||catalog.styles.Length!=20||catalog.canonicalMasks.Length!=47)throw new InvalidOperationException("Invalid free-shape package.");
            for(int i=0;i<256;i++)if(catalog.rawToCanonical[i]!=FreeShapeTopology.Canonical(i)||topology.rawToCanonical[i]!=catalog.rawToCanonical[i]||catalog.rawToIndex[i]!=Array.IndexOf(catalog.canonicalMasks,catalog.rawToCanonical[i])||topology.rawToIndex[i]!=catalog.rawToIndex[i])throw new InvalidOperationException("Topology mapping mismatch.");
            Directory.CreateDirectory(Root+"/Art/Atlases");Directory.CreateDirectory(Root+"/Art/Motifs");Directory.CreateDirectory(Root+"/Data");
            foreach(var file in new[]{"sprite_lookup.json","topology_catalog.json","style_catalog.json","logical_fixtures.json","sweetie-16.hex"})CopyExact(Source+"/Data/"+file,Root+"/Data/"+file);
            var palette=new HashSet<string>(File.ReadAllLines(Source+"/Data/sweetie-16.hex").Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Trim().TrimStart('#').ToLowerInvariant()));
            var output=new List<FreeShapeStyleArt>();
            foreach(var s in catalog.styles)
            {
                var name=names.styles.Single(n=>n.styleId==s.styleId);
                var art=new FreeShapeStyleArt{styleId=s.styleId,themeId=name.themeId,themeName=name.themeName,displayName=name.displayName,cells=new Sprite[188]};
                foreach(var v in s.variants)
                {
                    int vi=int.Parse(v.id.Substring(1),System.Globalization.CultureInfo.InvariantCulture);
                    string path=Root+"/Art/Atlases/"+Path.GetFileName(v.atlas);CopyExact(Source+"/"+v.atlas,path);ValidatePixels(path,256,192,palette);
                    var rects=v.cells.Select(c=>new SpriteMetaData{name=s.styleId+"_"+v.id+"_mask"+c.mask.ToString("D3"),rect=new Rect(c.rect.x,192-c.rect.y-c.rect.height,c.rect.width,c.rect.height),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
                    Import(path,rects);
                    var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(p=>p.name);
                    foreach(var c in v.cells){if(catalog.canonicalMasks[c.index]!=c.mask)throw new InvalidOperationException("Mask/index mismatch.");art.cells[vi*47+c.index]=sprites[s.styleId+"_"+v.id+"_mask"+c.mask.ToString("D3")];}
                }
                var motifPath=Root+"/Art/Motifs/"+s.styleId+".png";CopyExact(Source+"/"+s.panels.motif,motifPath);ValidatePixels(motifPath,128,128,palette);Import(motifPath,null);art.motif=AssetDatabase.LoadAssetAtPath<Sprite>(motifPath);output.Add(art);
            }
            AssetDatabase.Refresh();
            var registry=AssetDatabase.LoadAssetAtPath<FreeShapeArtRegistry>(RegistryPath);
            if(registry==null){registry=ScriptableObject.CreateInstance<FreeShapeArtRegistry>();AssetDatabase.CreateAsset(registry,RegistryPath);}
            var matPath=Root+"/Data/FreeShapeSprites.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,matPath);}
            registry.contractId=FreeShapeLayer.Contract;registry.version=1;registry.sourceHash=HashFile(Source+"/Data/sprite_lookup.json");
            registry.canonicalMasks=catalog.canonicalMasks;registry.rawToCanonical=catalog.rawToCanonical;registry.rawToIndex=catalog.rawToIndex;registry.styles=output.ToArray();registry.material=material;registry.fixtures=Read<Fixtures>("Data/logical_fixtures.json").fixtures;
            EditorUtility.SetDirty(registry);AssetDatabase.SaveAssetIfDirty(registry);
            File.WriteAllText("Library/FreeShape-import-result.json","{\"verifiedPackageFiles\":"+manifest.files.Length+",\"atlases\":80,\"cellSprites\":3760,\"motifs\":20,\"milliseconds\":"+timer.ElapsedMilliseconds+"}");
            Debug.Log("ANIMOL Free Shape: SHA256 verified; 80 atlases, 3760 cell Sprites, 20 motifs initialized.");
        }
        private static void CopyExact(string from,string to)
        {if(File.Exists(to)){if(HashFile(from)!=HashFile(to))throw new InvalidOperationException("Installed art differs: "+to);}else File.Copy(from,to);}
        private static void Import(string path,SpriteMetaData[] rects)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);var mode=rects==null?SpriteImportMode.Single:SpriteImportMode.Multiple;
            bool changed=t.textureType!=TextureImporterType.Sprite||t.spriteImportMode!=mode||t.spritePixelsPerUnit!=32||t.filterMode!=FilterMode.Point||t.mipmapEnabled||t.textureCompression!=TextureImporterCompression.Uncompressed||!t.sRGBTexture;
            if(rects!=null)changed|=t.spritesheet.Length!=47||!t.spritesheet.Zip(rects,(a,b)=>a.name==b.name&&a.rect==b.rect&&a.pivot==b.pivot).All(v=>v);
            if(!changed)return;
            t.textureType=TextureImporterType.Sprite;t.spriteImportMode=mode;t.spritePixelsPerUnit=32;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;t.sRGBTexture=true;t.alphaIsTransparency=false;t.isReadable=false;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=rects==null?9:0;settings.spritePivot=rects==null?Vector2.zero:new Vector2(.5f,.5f);t.SetTextureSettings(settings);
            if(rects!=null)t.spritesheet=rects;t.SaveAndReimport();
        }
        private static void ValidatePixels(string path,int width,int height,HashSet<string> palette)
        {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);try{
                if(!t.LoadImage(File.ReadAllBytes(path))||t.width!=width||t.height!=height)throw new InvalidOperationException("PNG size mismatch: "+path);
                foreach(var p in t.GetPixels32())if(p.a!=0&&p.a!=255||p.a==255&&!palette.Contains(p.r.ToString("x2")+p.g.ToString("x2")+p.b.ToString("x2")))throw new InvalidOperationException("Non Sweetie16/binary-alpha pixel: "+path);
            }finally{UnityEngine.Object.DestroyImmediate(t);}
        }
    }
}
