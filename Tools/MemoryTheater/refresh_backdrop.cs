using UnityEngine;
using UnityEditor;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
public static class TheaterBackdropFix
{
    public static string Main()
    {
        string path=MemoryTheaterArtBuilder.Prefab("foundation/theater_backdrop_static");
        var go=PrefabUtility.LoadPrefabContents(path);
        try{go.GetComponent<DeviceVisualView>().visual.sortingOrder=-30;PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}
        return MemoryTheaterArtBuilder.BuildScene();
    }
}
