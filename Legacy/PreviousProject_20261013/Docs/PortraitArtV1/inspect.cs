using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SceneManagement;
using ANIMOL.Core;
public static class PortraitInspection
{
    public static string Main()
    {
        var text=new StringBuilder();
        text.AppendLine("Unity "+Application.unityVersion+" active="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/UI_CommonRoot.prefab");
        foreach(var c in root.GetComponents<Component>()) text.AppendLine("Root component: "+c.GetType().Name);
        foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.GetComponent<Canvas>()!=null || t.name=="SafeArea" || t.name=="ScreenHost") text.AppendLine(t.name+" parent="+t.parent?.name);
        foreach(var guid in AssetDatabase.FindAssets("t:ExternalServiceConfiguration")) {
            var path=AssetDatabase.GUIDToAssetPath(guid);var s=AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>(path);
            text.AppendLine(path+" account="+s.AccountServerConnected+" match="+s.MatchServerConnected+" ad="+s.RewardedAdSdkConnected+" purchase="+s.PurchaseSdkConnected);
        }
        foreach(var name in new[]{"Bootstrap","Lobby"}) {
            string path="Assets/ANIMOL/Scenes/"+name+".unity";
            text.AppendLine(path+" exists="+File.Exists(path));
        }
        File.WriteAllText("Docs/PortraitArtV1/inventory.txt",text.ToString());return text.ToString();
    }
}
