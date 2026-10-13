var source="Assets/Screenshots/DeviceArt/qa_playmode.png";
System.IO.File.Copy(source,"Screenshots/DeviceArt/qa_playmode.png",true);
UnityEditor.AssetDatabase.DeleteAsset(source);
foreach(var folder in new[]{"Assets/Screenshots/DeviceArt","Assets/Screenshots"})
    if(System.IO.Directory.Exists(folder) && System.IO.Directory.GetFiles(folder,"*",System.IO.SearchOption.AllDirectories).All(p=>p.EndsWith(".meta")))UnityEditor.AssetDatabase.DeleteAsset(folder);
return "Moved inspection screenshot outside runtime Assets";
