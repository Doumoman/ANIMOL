using System.IO;
using UnityEditor;
public static class TheaterScreenCapture
{
    public static void Main()
    {
        const string source="Assets/Screenshots/MemoryTheater/qa_playmode.png";
        File.Copy(source,"Screenshots/MemoryTheater/qa_playmode.png",true);AssetDatabase.DeleteAsset(source);
        foreach(var folder in new[]{"Assets/Screenshots/MemoryTheater","Assets/Screenshots"})if(Directory.Exists(folder)&&Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Length==0)AssetDatabase.DeleteAsset(folder);
    }
}
