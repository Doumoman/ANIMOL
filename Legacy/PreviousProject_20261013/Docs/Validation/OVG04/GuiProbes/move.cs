using UnityEngine;
using UnityEditor;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Development;
public static class Ovg04GuiMove
{
public static object Main()
{
var view=TerrainEditorSceneEditor.View;var map=TerrainEditorSceneEditor.Adapter.Map;
TerrainEditorSceneEditor.State.tool="Select";TerrainEditorSceneEditor.State.information=false;
var side=map.Objects.Single(o=>o.Kind==StageMapObjectKind.CommonC02);var id=side.StableId;
var point=TerrainEditorSceneEditor.ProbeGui+new Vector2(0,26);
var scale=view.camera.WorldToScreenPoint(new Vector3(1,0,0)).x-view.camera.WorldToScreenPoint(Vector3.zero).x;
var end=point+new Vector2(scale,0);
view.SendEvent(new Event{type=EventType.MouseDown,button=0,mousePosition=point});
view.SendEvent(new Event{type=EventType.MouseDrag,button=0,mousePosition=end,delta=new Vector2(scale,0)});
view.SendEvent(new Event{type=EventType.MouseUp,button=0,mousePosition=end});
var moved=map.Objects.Single(o=>o.StableId==id).X==22;
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Z,modifiers=EventModifiers.Control});
var undo=map.Objects.Single(o=>o.StableId==id).X==21;
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Y,modifiers=EventModifiers.Control});
var redo=map.Objects.Single(o=>o.StableId==id).X==22;
var settings=map.Objects.Single(o=>o.StableId==id).Settings.Clone();settings.EditorSetObstacleFacing(ObstacleFacing.Left);
TerrainEditorSceneEditor.Adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=id,settings=settings});
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.S,modifiers=EventModifiers.Control});
var path=AssetDatabase.GetAssetPath(map);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
var reopened=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
var saved=reopened.Objects.Single(o=>o.StableId==id);
if(!moved||!undo||!redo||saved.X!=22||saved.Settings.ObstacleFacing!=ObstacleFacing.Left)throw new System.Exception("Mouse drag/save identity check failed: "+TerrainEditorSceneEditor.Status);
TerrainEditorSceneEditor.State.information=false;TerrainEditorSceneEditor.SelectPart("C02");TerrainEditorSceneEditor.State.instanceId=id;TerrainEditorSceneEditor.State.tool="Select";
TerrainEditorSceneEditor.View.Repaint();
return new {id,moved,undo,redo,reopenedX=saved.X,facing=saved.Settings.ObstacleFacing.ToString(),window=view.GetInstanceID(),map=path};
}
}
