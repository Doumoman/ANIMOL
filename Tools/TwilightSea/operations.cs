using UnityEditor;
using ANIMOL.Presentation.Editor;
public static class SeaOperations
{
    public static string Build()=>TwilightSeaArtBuilder.Build();
    public static string Scene()=>TwilightSeaArtBuilder.BuildScene();
    public static string Validate()=>TwilightSeaArtBuilder.Validate();
    public static string Capture()=>TwilightSeaArtBuilder.Capture();
    public static void Reimport()
    {
        var m=TwilightSeaArtBuilder.Read();
        foreach(var row in m.assets)AssetDatabase.ImportAsset("Assets/ANIMOL/"+row.path,ImportAssetOptions.ForceUpdate);
        foreach(var row in m.animations)foreach(var f in row.frames)AssetDatabase.ImportAsset("Assets/ANIMOL/"+f.path,ImportAssetOptions.ForceUpdate);
    }
}
