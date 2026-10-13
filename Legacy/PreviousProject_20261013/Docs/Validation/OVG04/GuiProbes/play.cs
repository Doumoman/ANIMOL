using UnityEngine;
using UnityEditor;
using ANIMOL.Editor;
public static class Ovg04GuiPlay
{
public static object Main()
{
var view=TerrainEditorSceneEditor.View;var map=TerrainEditorSceneEditor.Adapter.Map;
SessionState.SetInt("OVG04.verifyWindow",view.GetInstanceID());SessionState.SetString("OVG04.verifyMap",AssetDatabase.GetAssetPath(map));
SessionState.SetString("OVG04.verifyMapJson",EditorJsonUtility.ToJson(map));
view.Focus();
var point=new Vector2(590,12+26);
view.SendEvent(new Event{type=EventType.MouseDown,button=0,mousePosition=point});
view.SendEvent(new Event{type=EventType.MouseUp,button=0,mousePosition=point});
if(!EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Save + Local Play GUI button did not start Play Mode.");
return new {actualToolbarClick=true,window=view.GetInstanceID(),path=AssetDatabase.GetAssetPath(map),enteringPlay=EditorApplication.isPlayingOrWillChangePlaymode};
}
}
