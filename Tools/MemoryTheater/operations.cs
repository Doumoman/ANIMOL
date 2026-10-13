using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using ANIMOL.Presentation.Editor;
public static class TheaterOperations
{
    public static string Build()=>MemoryTheaterArtBuilder.Build();
    public static string BuildScene()=>MemoryTheaterArtBuilder.BuildScene();
    public static string Capture()=>MemoryTheaterArtBuilder.Capture();
    public static string Validate()=>MemoryTheaterArtBuilder.Validate();
    public static void Refresh()=>AssetDatabase.Refresh();
    public static void Reimport()
    {
        var m=MemoryTheaterArtBuilder.Read();
        foreach(var row in m.assets)AssetDatabase.ImportAsset("Assets/ANIMOL/"+row.path,ImportAssetOptions.ForceUpdate);
        foreach(var row in m.animations)foreach(var f in row.frames)AssetDatabase.ImportAsset("Assets/ANIMOL/"+f.path,ImportAssetOptions.ForceUpdate);
    }
}
