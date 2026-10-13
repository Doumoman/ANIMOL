using UnityEditor;
using ANIMOL.Presentation.Editor;
public static class ObservatoryOperations
{
    public static string Build()=>LetterObservatoryArtBuilder.Build();
    public static string Validate()=>LetterObservatoryArtBuilder.Validate();
    public static string Capture()=>LetterObservatoryArtBuilder.Capture();
    public static void Reimport()
    {
        var m=LetterObservatoryArtBuilder.Read();
        foreach(var row in m.assets)AssetDatabase.ImportAsset("Assets/ANIMOL/"+row.path,ImportAssetOptions.ForceUpdate);
        foreach(var row in m.animations)foreach(var f in row.frames)AssetDatabase.ImportAsset("Assets/ANIMOL/"+f.path,ImportAssetOptions.ForceUpdate);
    }
}
