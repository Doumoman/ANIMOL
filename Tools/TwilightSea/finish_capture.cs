using System.IO;
using UnityEditor;
public static class SeaCaptureMove
{
    public static void Main()
    {
        foreach(var name in new[]{"qa_playmode.png","qa_playmode_720.png"}){var source="Assets/Screenshots/TwilightSea/"+name;if(File.Exists(source)){File.Copy(source,"Screenshots/TwilightSea/"+name,true);AssetDatabase.DeleteAsset(source);}}
        foreach(var folder in new[]{"Assets/Screenshots/TwilightSea","Assets/Screenshots"})if(Directory.Exists(folder)&&Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Length==0)AssetDatabase.DeleteAsset(folder);
    }
}
