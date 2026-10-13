using System.IO;
using UnityEditor;
public static class ObservatoryCaptureMove
{
    public static void Main()
    {
        const string source="Assets/Screenshots/LetterObservatory/qa_playmode.png";File.Copy(source,"Screenshots/LetterObservatory/qa_playmode.png",true);AssetDatabase.DeleteAsset(source);
        foreach(var folder in new[]{"Assets/Screenshots/LetterObservatory","Assets/Screenshots"})if(Directory.Exists(folder)&&Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Length==0)AssetDatabase.DeleteAsset(folder);
    }
}
