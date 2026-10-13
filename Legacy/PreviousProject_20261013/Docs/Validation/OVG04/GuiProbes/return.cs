using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using ANIMOL.Development;
public static class Ovg04GuiReturn
{
public static object Main()
{
var screen=TerrainEditorScreen.Instance;
screen.GetComponentsInChildren<Button>().Single(b=>b.name=="Test").onClick.Invoke();
return new {returnButtonInvoked=true,expectedWindow=SessionState.GetInt("OVG04.verifyWindow",0),expectedMap=SessionState.GetString("OVG04.verifyMap","")};
}
}
