var summary=new System.Collections.Generic.List<object>();var allIds=new System.Collections.Generic.HashSet<string>();
foreach(int version in new[]{1,2})
{
    var art=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,version);art.ValidateComplete();
    var textures=new System.Collections.Generic.HashSet<UnityEngine.Texture2D>();int cells=0;
    foreach(var style in art.styles)
    {
        if(style.motif.pivot!=UnityEngine.Vector2.zero || style.motif.pixelsPerUnit!=32)throw new System.Exception("Motif pivot/PPU mismatch");
        foreach(var sprite in style.cells.Concat(new[]{style.motif}))
        {
            if(!allIds.Add(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(sprite).ToString()))throw new System.Exception("Duplicate reference across art versions");
            if(sprite.vertices.Length!=4)throw new System.Exception("Not FullRect: "+sprite.name);
            textures.Add(sprite.texture);
        }
        cells+=style.cells.Length;
    }
    foreach(var texture in textures)
    {
        var path=UnityEditor.AssetDatabase.GetAssetPath(texture);var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);
        if(importer.filterMode!=UnityEngine.FilterMode.Point || importer.mipmapEnabled || importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed || importer.spritePixelsPerUnit!=32 || !importer.sRGBTexture || settings.spriteMeshType!=UnityEngine.SpriteMeshType.FullRect)throw new System.Exception("Import mismatch: "+path);
        if(importer.spriteImportMode!=(path.Contains("/Atlases/")?UnityEditor.SpriteImportMode.Multiple:UnityEditor.SpriteImportMode.Single))throw new System.Exception("Sprite mode mismatch: "+path);
    }
    summary.Add(new{version=version,styles=art.styles.Length,cells=cells,motifs=art.styles.Length,textures=textures.Count});
}
System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/Registration.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{versions=summary,uniqueSpriteIds=allIds.Count},Newtonsoft.Json.Formatting.Indented));
return summary;
