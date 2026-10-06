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
    public static class FreeShapeArtV6Builder
    {
        public const string Source="Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6";
        public const string Root="Assets/ANIMOL/TerrainFreeShape/V6";
        public const string RegistryPath="Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV6.asset";
        [Serializable] private class Manifest {public FileRecord[] files; public string cellPngDigestSha256;}
        [Serializable] private class FileRecord {public string path,sha256;public long bytes;}
        [Serializable] private class RectData {public int x,y,width,height;}
        [Serializable] private class CellData {public string file;public int mask,index;public RectData rect;}
        [Serializable] private class Variant {public string id,atlas;public int[] atlasPixels;public CellData[] cells;}
        [Serializable] private class Panels {public string motif;}
        [Serializable] private class Style {public string styleId,themeId,themeName,displayName;public Variant[] variants;public Panels panels;}
        [Serializable] private class Catalog {public string contractId;public int schemaVersion,artVersion,cellPixels,pixelsPerUnit;public int[] canonicalMasks,rawToCanonical,rawToIndex;public Style[] styles;}
        [Serializable] private class Fixtures {public FreeShapeFixture[] fixtures;}
        public static string HashFile(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static T Read<T>(string source,string path)=>JsonUtility.FromJson<T>(File.ReadAllText(source+"/"+path));
        public static void Initialize()
        {
            const int artVersion=FreeShapeLayer.ArtVersion;
            const string source=Source,root=Root,registryPath=RegistryPath;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            var manifest=Read<Manifest>(source,"PACKAGE_MANIFEST.json");
            foreach(var f in manifest.files){var path=Path.GetFullPath(source+"/"+f.path);if(!path.StartsWith(Path.GetFullPath(source)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(path)||new FileInfo(path).Length!=f.bytes||HashFile(path)!=f.sha256)throw new InvalidOperationException("Package SHA256 mismatch: "+f.path);}
            var catalog=Read<Catalog>(source,"Data/sprite_lookup.json");var topology=Read<Catalog>(source,"Data/topology_catalog.json");var names=Read<Catalog>(source,"Data/style_catalog.json");
            if(catalog.schemaVersion!=1||catalog.artVersion!=artVersion||catalog.cellPixels!=32||catalog.pixelsPerUnit!=32||catalog.contractId!=FreeShapeLayer.Contract||catalog.styles.Length!=20||catalog.canonicalMasks.Length!=47)throw new InvalidOperationException("Invalid free-shape package.");
            for(int i=0;i<256;i++)if(catalog.rawToCanonical[i]!=FreeShapeTopology.Canonical(i)||topology.rawToCanonical[i]!=catalog.rawToCanonical[i]||catalog.rawToIndex[i]!=Array.IndexOf(catalog.canonicalMasks,catalog.rawToCanonical[i])||topology.rawToIndex[i]!=catalog.rawToIndex[i])throw new InvalidOperationException("Topology mapping mismatch.");
            ValidateCatalog(catalog,names);
            // Verify every source cell before installing any runtime asset.
            {
                using(var digest=IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    var cells=catalog.styles.SelectMany(s=>s.variants).SelectMany(v=>v.cells).Select(c=>c.file).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                    if(cells.Distinct().Count()!=3760)throw new InvalidOperationException("Incomplete V6 cell inventory.");
                    foreach(var path in cells){digest.AppendData(System.Text.Encoding.UTF8.GetBytes(path+"\n"));digest.AppendData(File.ReadAllBytes(source+"/"+path));}
                    if(BitConverter.ToString(digest.GetHashAndReset()).Replace("-","").ToLowerInvariant()!=manifest.cellPngDigestSha256)
                        throw new InvalidOperationException("V6 cell PNG digest mismatch.");
                }
            }
            Directory.CreateDirectory(root+"/Art/Atlases");Directory.CreateDirectory(root+"/Art/Motifs");Directory.CreateDirectory(root+"/Data");
            foreach(var file in new[]{"sprite_lookup.json","topology_catalog.json","style_catalog.json","logical_fixtures.json","sweetie-16.hex"})CopyExact(source+"/Data/"+file,root+"/Data/"+file);
            var palette=new HashSet<string>(File.ReadAllLines(source+"/Data/sweetie-16.hex").Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Trim().TrimStart('#').ToLowerInvariant()));
            var output=new List<FreeShapeStyleArt>();
            foreach(var s in catalog.styles)
            {
                var name=names.styles.Single(n=>n.styleId==s.styleId);
                var art=new FreeShapeStyleArt{styleId=s.styleId,themeId=name.themeId,themeName=name.themeName,displayName=name.displayName,cells=new Sprite[188]};
                foreach(var v in s.variants)
                {
                    int vi=int.Parse(v.id.Substring(1),System.Globalization.CultureInfo.InvariantCulture);
                    string path=root+"/Art/Atlases/"+Path.GetFileName(v.atlas);CopyExact(source+"/"+v.atlas,path);ValidatePixels(path,256,192,palette);
                    var rects=v.cells.Select(c=>new SpriteMetaData{name=SpriteName(s,v,c),rect=new Rect(c.rect.x,v.atlasPixels[1]-c.rect.y-c.rect.height,c.rect.width,c.rect.height),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
                    Import(path,rects);
                    var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(p=>p.name);
                    foreach(var c in v.cells){if(catalog.canonicalMasks[c.index]!=c.mask)throw new InvalidOperationException("Mask/index mismatch.");art.cells[vi*47+c.index]=sprites[SpriteName(s,v,c)];}
                }
                var motifPath=root+"/Art/Motifs/"+s.styleId+".png";CopyExact(source+"/"+s.panels.motif,motifPath);ValidatePixels(motifPath,128,128,palette);Import(motifPath,null);art.motif=AssetDatabase.LoadAssetAtPath<Sprite>(motifPath);output.Add(art);
            }
            AssetDatabase.Refresh();
            var registry=AssetDatabase.LoadAssetAtPath<FreeShapeArtRegistry>(registryPath);
            if(registry==null){registry=ScriptableObject.CreateInstance<FreeShapeArtRegistry>();AssetDatabase.CreateAsset(registry,registryPath);}
            var matPath=root+"/Data/FreeShapeSprites.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,matPath);}
            registry.contractId=FreeShapeLayer.Contract;registry.version=catalog.artVersion;registry.sourceHash=HashFile(source+"/Data/sprite_lookup.json");
            registry.canonicalMasks=catalog.canonicalMasks;registry.rawToCanonical=catalog.rawToCanonical;registry.rawToIndex=catalog.rawToIndex;registry.styles=output.ToArray();registry.material=material;registry.fixtures=Read<Fixtures>(source,"Data/logical_fixtures.json").fixtures;
            registry.ValidateComplete();EditorUtility.SetDirty(registry);AssetDatabase.SaveAssetIfDirty(registry);
            File.WriteAllText("Library/FreeShape-v"+artVersion+"-import-result.json","{\"verifiedPackageFiles\":"+manifest.files.Length+",\"atlases\":80,\"cellSprites\":3760,\"motifs\":20,\"milliseconds\":"+timer.ElapsedMilliseconds+"}");
            Debug.Log("ANIMOL Free Shape V"+artVersion+": SHA256 verified; 80 atlases, 3760 cell Sprites, 20 motifs initialized.");
        }
        // Lookup exposes the canonical cell name through its PNG path, not an array ordinal.
        private static string SpriteName(Style style,Variant variant,CellData cell)=>style.styleId+"_"+variant.id+"_"+Path.GetFileNameWithoutExtension(cell.file);
        private static void ValidateCatalog(Catalog catalog,Catalog names)
        {
            var expected=Enumerable.Range(1,5).SelectMany(t=>"ABCD".Select(s=>"T"+t.ToString("D2")+"_"+s)).ToHashSet();
            if(names.styles.Length!=20 || !expected.SetEquals(names.styles.Select(s=>s.styleId)) || !expected.SetEquals(catalog.styles.Select(s=>s.styleId)))throw new InvalidOperationException("Missing/duplicate style mapping.");
            var paths=new HashSet<string>();var spriteNames=new HashSet<string>();
            foreach(var s in catalog.styles)
            {
                if(s.variants.Length!=4 || !new HashSet<string>{"v0","v1","v2","v3"}.SetEquals(s.variants.Select(v=>v.id)))throw new InvalidOperationException("Invalid phase mapping: "+s.styleId);
                foreach(var v in s.variants)
                {
                    if(!paths.Add(v.atlas) || v.atlasPixels.Length!=2 || v.atlasPixels[0]!=256 || v.atlasPixels[1]!=192 || v.cells.Length!=47 || v.cells.Select(c=>c.index).Distinct().Count()!=47)throw new InvalidOperationException("Invalid atlas: "+v.atlas);
                    foreach(var c in v.cells)
                        if(c.index<0 || c.index>=47 || catalog.canonicalMasks[c.index]!=c.mask || Path.GetFileNameWithoutExtension(c.file)!="mask"+c.mask.ToString("D3") || !spriteNames.Add(SpriteName(s,v,c)) || c.rect.width!=32 || c.rect.height!=32 || c.rect.x!=c.index%8*32 || c.rect.y!=c.index/8*32)
                            throw new InvalidOperationException("Invalid lookup cell: "+c.file);
                }
                if(!paths.Add(s.panels.motif))throw new InvalidOperationException("Duplicate motif mapping.");
            }
        }
        private static void CopyExact(string from,string to)
        {if(File.Exists(to)){if(HashFile(from)!=HashFile(to))throw new InvalidOperationException("Installed art differs: "+to);}else File.Copy(from,to);}
        private static void Import(string path,SpriteMetaData[] rects)
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;
            if(t==null){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);t=(TextureImporter)AssetImporter.GetAtPath(path);}
            var mode=rects==null?SpriteImportMode.Single:SpriteImportMode.Multiple;
            bool changed=t.textureType!=TextureImporterType.Sprite||t.spriteImportMode!=mode||t.spritePixelsPerUnit!=32||t.filterMode!=FilterMode.Point||t.mipmapEnabled||t.textureCompression!=TextureImporterCompression.Uncompressed||!t.sRGBTexture;
            if(rects!=null)changed|=t.spritesheet.Length!=47||!t.spritesheet.Zip(rects,(a,b)=>a.name==b.name&&a.rect==b.rect&&a.pivot==b.pivot).All(v=>v);
            var previousSettings=new TextureImporterSettings();t.ReadTextureSettings(previousSettings);
            changed|=previousSettings.spriteMeshType!=SpriteMeshType.FullRect || previousSettings.spriteAlignment!=(rects==null?9:0) || previousSettings.spritePivot!=(rects==null?Vector2.zero:new Vector2(.5f,.5f)) || t.alphaIsTransparency || t.isReadable || t.npotScale!=TextureImporterNPOTScale.None;
            foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})
                if(t.GetPlatformTextureSettings(platform).overridden){t.ClearPlatformTextureSettings(platform);changed=true;}
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
