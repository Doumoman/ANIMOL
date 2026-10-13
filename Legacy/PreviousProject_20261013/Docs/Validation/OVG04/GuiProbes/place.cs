using UnityEngine;
using UnityEditor;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
public static class Ovg04GuiPlace
{
public static object Main()
{
var view=TerrainEditorSceneEditor.View;var map=TerrainEditorSceneEditor.Adapter.Map;
TerrainEditorSceneEditor.SelectPart("marker:CHECK");TerrainEditorSceneEditor.State.information=false;
// SceneView.SendEvent includes its native 26-point toolbar; duringSceneGui does not.
var point=TerrainEditorSceneEditor.ProbeGui+new Vector2(0,26);
var before=map.Objects.Count(o=>o.Kind==StageMapObjectKind.Checkpoint);
view.SendEvent(new Event{type=EventType.MouseDown,button=0,mousePosition=point});
view.SendEvent(new Event{type=EventType.MouseUp,button=0,mousePosition=point});
var placed=map.Objects.Single(o=>o.Kind==StageMapObjectKind.Checkpoint&&o.X==8);
var id=placed.StableId;
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Z,modifiers=EventModifiers.Control});
var undo=!map.Objects.Any(o=>o.StableId==id);
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Y,modifiers=EventModifiers.Control});
var redo=map.Objects.Any(o=>o.StableId==id);
TerrainEditorSceneEditor.State.instanceId=id;
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Delete});
var deleted=!map.Objects.Any(o=>o.StableId==id);
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Z,modifiers=EventModifiers.Control});
var deleteUndo=map.Objects.Any(o=>o.StableId==id);
view.SendEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Y,modifiers=EventModifiers.Control});
if(!undo||!redo||!deleted||!deleteUndo||map.Objects.Count(o=>o.Kind==StageMapObjectKind.Checkpoint)!=before)throw new System.Exception("Scene View event roundtrip failed");
TerrainEditorSceneEditor.State.tool="Select";
TerrainEditorSceneEditor.ProbeWorld=new Vector2(21.5f,1.5f);view.Repaint();
return new {placedId=id,undo,redo,deleted,deleteUndo,checkpointCount=before,next="C02 actual mouse drag"};
}
}
