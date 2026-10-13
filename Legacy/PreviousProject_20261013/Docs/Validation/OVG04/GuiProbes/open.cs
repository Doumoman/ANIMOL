using UnityEngine;
using UnityEditor;
using ANIMOL.Core;
using ANIMOL.Editor;
public static class Ovg04GuiOpen
{
public static object Main()
{
var source=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(Ovg04ValidationSetup.MapPath);
var path="Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OVG04-GUI.asset";
var map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
if(map==null){map=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(map,path);}
Selection.activeObject=map;
var menu=EditorApplication.ExecuteMenuItem("ANIMOL/Map Editor/Open Focused Workspace");
if(!TerrainEditorSceneEditor.Active)TerrainEditorSceneEditor.Open(map);
var view=TerrainEditorSceneEditor.View;
view.position=new Rect(50,50,1280,900);
view.LookAtDirect(new Vector3(11,2,0),Quaternion.identity,12);
TerrainEditorSceneEditor.SelectPart("marker:CHECK");
TerrainEditorSceneEditor.State.information=false;
TerrainEditorSceneEditor.ProbeWorld=new Vector2(8.5f,.5f);
view.Focus();view.Repaint();
return new {menu,active=TerrainEditorSceneEditor.Active,map=AssetDatabase.GetAssetPath(TerrainEditorSceneEditor.Adapter.Map),window=view.GetInstanceID(),batch=Application.isBatchMode};
}
}
