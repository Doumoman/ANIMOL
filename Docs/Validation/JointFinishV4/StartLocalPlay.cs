if(UnityEditor.AssetDatabase.GetAssetPath(ANIMOL.Editor.TerrainEditorSceneEditor.Adapter?.Map)!="Assets/ANIMOL/Data/Development/JointFinishV4/DEV-JOINT-V4-COPY.asset")throw new System.InvalidOperationException("Validation requires the isolated v4 development copy.");
// Actual Scene toolbar event at the observed 1300x900 window position.
var view=ANIMOL.Editor.TerrainEditorSceneEditor.View;view.Focus();
view.SendEvent(new UnityEngine.Event{type=UnityEngine.EventType.MouseDown,button=0,mousePosition=new UnityEngine.Vector2(520,36)});
view.SendEvent(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,button=0,mousePosition=new UnityEngine.Vector2(520,36)});
if(!ANIMOL.Editor.TerrainEditorSession.Active)throw new System.Exception("Scene toolbar did not open Local Play");
return new{nativeSceneToolbar=true,active=ANIMOL.Editor.TerrainEditorSession.Active};
