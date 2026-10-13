using UnityEngine;
using UnityEditor;
using ANIMOL.Editor;
public static class Ovg04GuiVerifyReturn
{
public static object Main()
{
var view=TerrainEditorSceneEditor.View;var map=TerrainEditorSceneEditor.Adapter.Map;
var sameWindow=view.GetInstanceID()==SessionState.GetInt("OVG04.verifyWindow",0);
var sameMap=AssetDatabase.GetAssetPath(map)==SessionState.GetString("OVG04.verifyMap","");
var sameData=EditorJsonUtility.ToJson(map)==SessionState.GetString("OVG04.verifyMapJson","");
var focused=EditorWindow.focusedWindow==view;var selected=Selection.activeObject==map;
if(EditorApplication.isPlaying||!sameWindow||!sameMap||!sameData||!focused||!selected)throw new System.Exception("Return mismatch: "+sameWindow+","+sameMap+","+sameData+","+focused+","+selected);
return new {stopped=true,sameWindow,sameMap,sameData,focused,selected,window=view.GetInstanceID(),path=AssetDatabase.GetAssetPath(map)};
}
}
