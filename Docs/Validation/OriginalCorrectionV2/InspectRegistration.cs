var background=UnityEngine.Resources.Load<ANIMOL.FiveThemeMenu.FantasyBackgroundCatalog>("ANIMOLMainUiV7");
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/OriginalCorrectionV2Work/baseline-unity.json"));
var rows=new System.Collections.Generic.List<object>();
foreach(var texture in background.themes.SelectMany(t=>new[]{t.far,t.mid,t.platform,t.near}))
{
    string path=UnityEditor.AssetDatabase.GetAssetPath(texture);var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);
    if(texture.width!=352||texture.height!=704||importer.filterMode!=UnityEngine.FilterMode.Point||importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed||importer.mipmapEnabled||importer.spritePixelsPerUnit!=32||settings.spriteMeshType!=UnityEngine.SpriteMeshType.FullRect||importer.npotScale!=UnityEditor.TextureImporterNPOTScale.None)throw new System.Exception(path);
    foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL","Windows Store Apps"})if(importer.GetPlatformTextureSettings(platform).overridden)throw new System.Exception("Platform override "+path);
    rows.Add(new{path,id=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(texture).ToString(),type=importer.textureType.ToString(),ppu=importer.spritePixelsPerUnit,mesh=settings.spriteMeshType.ToString(),width=texture.width,height=texture.height});
}
var current=UnityEngine.Resources.Load<ANIMOL.ProductionV1.ProductionCatalog>("ANIMOLProductionV1/Catalog");
foreach(var row in baseline["campaignArt"])
    if(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(current.Find((string)row["name"])).ToString()!=(string)row["id"])throw new System.Exception("Campaign Sprite reference changed");
var rabbits=new[]{background.rabbitLeft,background.rabbitRight}.Select(t=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t).ToString()).ToArray();
if(!rabbits.SequenceEqual(baseline["rabbits"].Select(t=>(string)t["id"])))throw new System.Exception("Rabbit reference changed");
var result=new{backgrounds=rows,preservedCampaignSpriteIds=current.Art.Length,preservedRabbitIds=rabbits.Length,resourceKey="ANIMOLMainUiV7",compositor=UnityEditor.AssetDatabase.GetAssetPath(background.compositor)};
System.IO.File.WriteAllText("Docs/Validation/OriginalCorrectionV2/Registration.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
